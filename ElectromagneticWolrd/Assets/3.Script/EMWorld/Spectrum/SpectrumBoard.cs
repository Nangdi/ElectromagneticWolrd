using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 7개 파장 칸으로 이루어진 체험 영역.
// 카드 이미지와 문구는 시작할 때 StreamingAssets/Cards에서 읽어 온다 (CardLibrary).
// 카드마다 본체(CardController) 1개를 CardLayer에 만들고, 그림은 Bands 아래 카드별 묶음(Visual_<ID>)에 만든다.
//  Visual_<ID> = 사진 아래 장식(CardUnderlayView) + 파장 칸 마스크 7개(각각 파장별 뷰 CardBandView 1개)
//               + 사진 위 장식(CardChromeView)
// 카드 단위로 묶어야 장식이 그 카드의 7개 뷰 아래/위에, 다른 카드와는 앞뒤 순서대로 그려진다.
// 파장 칸 마스크는 Bands/Band_xxx(위치 기준)를 따라 경계 너머로 조금 넓히고 가장자리를 부드럽게(RectMask2D.softness) 해서
// 경계에서 이웃 파장 이미지가 섞이며 바뀌게 한다.
public class SpectrumBoard : MonoBehaviour
{
    [SerializeField] private CardController cardPrefab;
    [SerializeField] private CardBandView bandViewPrefab;
    [SerializeField] private CardUnderlayView underlayPrefab;
    [SerializeField] private CardChromeView chromePrefab;
    [SerializeField] private RailController rail;
    [SerializeField] private CardBackBaker backBaker;
    [Tooltip("카드 본체가 들어가는 영역 (체험 영역 전체, 투명)")]
    [SerializeField] private RectTransform cardLayer;
    [Tooltip("파장 칸 위치 기준 (RectMask2D). BandType 순서: 라디오파 → 감마선. 카드마다 이 칸들을 복제해 쓴다")]
    [SerializeField] private RectTransform[] bandRoots = new RectTransform[BandInfo.Count];

    private readonly List<CardController> cards = new List<CardController>();
    private readonly Dictionary<CardController, Transform> visualByCard = new Dictionary<CardController, Transform>();
    private readonly List<Texture2D> backTextures = new List<Texture2D>();
    private List<CardData> cardData = new List<CardData>();
    // 파장 칸별 마스크 (위치 기준 칸 + 카드마다 복제한 칸). 블렌딩 폭을 바꾸면 모두 다시 맞춘다.
    private readonly List<RectMask2D>[] bandMasks = new List<RectMask2D>[BandInfo.Count];
    private Vector2[] bandBasePos, bandBaseSize; // 블렌딩으로 넓히기 전 칸 자리

    public RailController Rail => rail;
    public Vector2 AreaHalfSize => cardLayer.rect.size * 0.5f;

    private void Start()
    {
        bandBasePos = new Vector2[bandRoots.Length];
        bandBaseSize = new Vector2[bandRoots.Length];
        for (int i = 0; i < bandRoots.Length; i++)
        {
            bandBasePos[i] = bandRoots[i].anchoredPosition;
            bandBaseSize[i] = bandRoots[i].sizeDelta;
            bandMasks[i] = new List<RectMask2D> { bandRoots[i].GetComponent<RectMask2D>() };
        }
        ApplyBandBlend(EMWorldConfig.Current.bandBlendWidth);
        EMWorldConfig.Changed += OnConfigChanged;

        cardData = CardLibrary.Load();
        if (cardData.Count == 0)
        {
            Debug.LogWarning($"카드가 없습니다. {CardLibrary.RootPath}에 카드 폴더를 넣으세요.");
            return;
        }

        foreach (CardData data in cardData)
        {
            CardController card = Instantiate(cardPrefab, cardLayer);
            card.name = $"Card_{data.id}";

            RectTransform visual = CreateVisual(data.id);
            CardUnderlayView underlay = Instantiate(underlayPrefab, visual);
            underlay.name = $"{data.id}_Underlay";
            var views = new CardBandView[BandInfo.Count];
            for (int i = 0; i < BandInfo.Count; i++)
            {
                RectTransform mask = CloneBandMask(bandRoots[i], visual);
                bandMasks[i].Add(mask.GetComponent<RectMask2D>());
                views[i] = Instantiate(bandViewPrefab, mask);
                views[i].name = $"{data.id}_{(BandType)i}";
            }
            CardChromeView chrome = Instantiate(chromePrefab, visual);
            chrome.name = $"{data.id}_Chrome";

            Texture2D back = backBaker.Bake(data);
            backTextures.Add(back);
            card.Init(this, data, views, underlay, chrome, back);
            card.PickedUp += OnCardPickedUp;
            card.ReturnFinished += OnCardReturnFinished;
            cards.Add(card);
            visualByCard.Add(card, visual);
        }

        rail.Setup(cards, cardLayer.rect.width);
    }

    private void OnDestroy()
    {
        EMWorldConfig.Changed -= OnConfigChanged;

        foreach (Texture2D tex in backTextures)
            if (tex != null)
                Destroy(tex);
        backTextures.Clear();

        foreach (CardData data in cardData)
            CardLibrary.Unload(data);
        cardData.Clear();
    }

    // 칸은 왼쪽부터 차례로 그려지므로 오른쪽 칸이 위에 그려진다.
    // 경계 B에서 오른쪽 칸은 [B - w/2, B + w/2]에서 서서히 나타나고(위), 왼쪽 칸은 그 구간에서 불투명하게 깔린 뒤
    // [B + w/2, B + 3w/2]에서 사라진다(오른쪽 칸에 가려 보이지 않음). 그래서 경계를 중심으로 폭 w만큼 섞인다.
    // 첫 칸의 왼쪽 가장자리는 화면 밖으로 밀어 화면 끝이 어두워지지 않게 하고,
    // 마지막 칸의 오른쪽 가장자리는 위에 그려지는 정보검색 칸 밑으로 들어간다.
    // RectMask2D.softness는 가장자리에서 안쪽으로 softness의 "절반" 폭에 걸쳐 흐려지므로(실측) 2w를 넣어야 폭 w로 흐려진다.
    // 설정창에서 바꾸면 바로 다시 맞춘다.
    private void ApplyBandBlend(float width)
    {
        width = Mathf.Max(0f, width);
        var softness = new Vector2Int(Mathf.RoundToInt(width * 2f), 0);
        for (int i = 0; i < bandRoots.Length; i++)
        {
            float center = bandBasePos[i].x;
            float half = bandBaseSize[i].x * 0.5f;
            float left = center - half - (i == 0 ? width * 2f : width * 0.5f);
            float right = center + half + width * 1.5f;
            var size = new Vector2(right - left, bandBaseSize[i].y);
            var pos = new Vector2((left + right) * 0.5f, bandBasePos[i].y);

            foreach (RectMask2D mask in bandMasks[i])
            {
                var rt = (RectTransform)mask.transform;
                rt.sizeDelta = size;
                rt.anchoredPosition = pos;
                mask.softness = softness;
            }
        }
    }

    private void OnConfigChanged(string field)
    {
        if (field == nameof(EMWorldConfig.bandBlendWidth))
            ApplyBandBlend(EMWorldConfig.Current.bandBlendWidth);
    }

    // 카드 그림 묶음: Bands와 같은 영역을 채운다.
    private RectTransform CreateVisual(string id)
    {
        var parent = (RectTransform)bandRoots[0].parent;
        var visual = new GameObject($"Visual_{id}", typeof(RectTransform)).GetComponent<RectTransform>();
        visual.gameObject.layer = parent.gameObject.layer;
        visual.SetParent(parent, false);
        visual.anchorMin = Vector2.zero;
        visual.anchorMax = Vector2.one;
        visual.offsetMin = visual.offsetMax = Vector2.zero;
        return visual;
    }

    // 위치 기준 칸과 같은 자리, 같은 부드러운 가장자리의 마스크를 카드 묶음 안에 만든다.
    private static RectTransform CloneBandMask(RectTransform source, RectTransform parent)
    {
        var mask = new GameObject(source.name, typeof(RectTransform)).GetComponent<RectTransform>();
        mask.gameObject.layer = source.gameObject.layer;
        mask.SetParent(parent, false);
        mask.anchorMin = source.anchorMin;
        mask.anchorMax = source.anchorMax;
        mask.pivot = source.pivot;
        mask.anchoredPosition = source.anchoredPosition;
        mask.sizeDelta = source.sizeDelta;
        mask.gameObject.AddComponent<RectMask2D>().softness = source.GetComponent<RectMask2D>().softness;
        return mask;
    }

    // 터치한 카드를 맨 앞으로. 본체(터치 순서)와 그림 묶음(그리는 순서)을 같이 옮긴다.
    public void BringToFront(CardController card)
    {
        card.transform.SetAsLastSibling();
        visualByCard[card].SetAsLastSibling();
    }

    // 무입력 대기 복귀: 놓아 둔 카드를 모두 레일로 돌려보낸다.
    public void ReturnAllFree()
    {
        foreach (CardController card in cards)
            card.BeginReturn();
    }

    private void OnCardPickedUp(CardController card)
    {
        rail.Detach(card);
    }

    private void OnCardReturnFinished(CardController card)
    {
        rail.Enqueue(card);
    }
}

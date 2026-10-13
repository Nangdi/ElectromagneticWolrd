using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 7개 파장 칸으로 이루어진 체험 영역.
// 카드 이미지와 문구는 시작할 때 StreamingAssets/Cards에서 읽어 온다 (CardLibrary).
// 카드마다 본체(CardController) 1개를 CardLayer에, 파장별 뷰(CardBandView) 7개를 각 파장 칸에 만든다.
// 파장 칸 마스크는 경계 너머로 조금 넓히고 가장자리를 부드럽게(RectMask2D.softness) 해서
// 경계에서 이웃 파장 이미지가 섞이며 바뀌게 한다.
public class SpectrumBoard : MonoBehaviour
{
    [SerializeField] private CardController cardPrefab;
    [SerializeField] private CardBandView bandViewPrefab;
    [SerializeField] private RailController rail;
    [SerializeField] private CardBackBaker backBaker;
    [Tooltip("카드 본체가 들어가는 영역 (체험 영역 전체, 투명)")]
    [SerializeField] private RectTransform cardLayer;
    [Tooltip("파장 칸 (RectMask2D). BandType 순서: 라디오파 → 감마선")]
    [SerializeField] private RectTransform[] bandRoots = new RectTransform[BandInfo.Count];

    private readonly List<CardController> cards = new List<CardController>();
    private readonly Dictionary<CardController, CardBandView[]> viewsByCard = new Dictionary<CardController, CardBandView[]>();
    private readonly List<Texture2D> backTextures = new List<Texture2D>();
    private List<CardData> cardData = new List<CardData>();

    public RailController Rail => rail;
    public Vector2 AreaHalfSize => cardLayer.rect.size * 0.5f;

    private void Start()
    {
        ApplyBandBlend(EMWorldConfig.Current.bandBlendWidth);

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

            var views = new CardBandView[BandInfo.Count];
            for (int i = 0; i < BandInfo.Count; i++)
            {
                views[i] = Instantiate(bandViewPrefab, bandRoots[i]);
                views[i].name = $"{data.id}_{(BandType)i}";
            }

            Texture2D back = backBaker.Bake(data);
            backTextures.Add(back);
            card.Init(this, data, views, back);
            card.PickedUp += OnCardPickedUp;
            card.ReturnFinished += OnCardReturnFinished;
            cards.Add(card);
            viewsByCard.Add(card, views);
        }

        rail.Setup(cards, cardLayer.rect.width);
    }

    private void OnDestroy()
    {
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
    private void ApplyBandBlend(float width)
    {
        width = Mathf.Max(0f, width);
        for (int i = 0; i < bandRoots.Length; i++)
        {
            RectTransform band = bandRoots[i];
            float center = band.anchoredPosition.x;
            float half = band.sizeDelta.x * 0.5f;
            float left = center - half - (i == 0 ? width * 2f : width * 0.5f);
            float right = center + half + width * 1.5f;

            band.sizeDelta = new Vector2(right - left, band.sizeDelta.y);
            band.anchoredPosition = new Vector2((left + right) * 0.5f, band.anchoredPosition.y);
            band.GetComponent<RectMask2D>().softness = new Vector2Int(Mathf.RoundToInt(width), 0);
        }
    }

    // 터치한 카드를 맨 앞으로. 본체와 7개 칸의 뷰 순서를 똑같이 맞춰야 칸마다 앞뒤가 달라지지 않는다.
    public void BringToFront(CardController card)
    {
        card.transform.SetAsLastSibling();
        foreach (CardBandView view in viewsByCard[card])
            view.transform.SetAsLastSibling();
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

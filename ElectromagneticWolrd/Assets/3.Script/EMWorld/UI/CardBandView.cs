using UnityEngine;
using UnityEngine.UI;

// 카드 한 장의 "한 파장 칸용" 모습. 파장 칸(RectMask2D) 아래에 들어가서 칸 밖은 잘리고, 경계에서는 이웃 칸과 섞인다.
// 위치, 회전, 크기는 CardController가 매 프레임 맞춰 준다. 터치는 받지 않는다.
//
// 경계에서 섞일 때 위 칸의 뷰는 요소마다 반투명하게 겹쳐 그려진다. 그래서 겹친 요소 아래가 비치지 않도록
//  - 앞면 사진, 뒷면(미리 구운 이미지 한 장)은 각각 불투명한 단일 이미지로 두고
//  - 테두리(속이 빈 9-slice)와 뒤집기 아이콘은 그 위에 그린다 (모든 칸에서 같은 모습이라 겹쳐도 그대로 보인다).
public class CardBandView : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private GameObject front;
    [SerializeField] private Image picture;
    [SerializeField] private NoImageView noImage;
    [Tooltip("레일 위에서 앞면 위에 덮이는 원본 이미지. 모든 칸에서 같아서 경계를 넘어도 그대로 보인다")]
    [SerializeField] private CanvasGroup original;
    [SerializeField] private Image originalPicture;
    [SerializeField] private RawImage back;
    [SerializeField] private CanvasGroup flipIcon;
    [SerializeField] private GameObject infoIcon;
    [SerializeField] private GameObject imageIcon;

    private bool showingBack;
    private bool originalOpaque;

    public RectTransform Rect => (RectTransform)transform;

    public void Set(CardData data, BandType band, Texture backTexture)
    {
        Sprite sprite = data?.GetSprite(band);
        picture.sprite = sprite;
        picture.enabled = sprite != null;

        bool showNoImage = data != null && sprite == null;
        noImage.gameObject.SetActive(showNoImage);
        if (showNoImage)
            noImage.Set(data);

        Sprite originalSprite = data?.GetOriginal();
        originalPicture.sprite = originalSprite;
        originalPicture.enabled = originalSprite != null;

        back.texture = backTexture;
        SetFace(false);
        SetFlipIconAlpha(0f);
    }

    public void SetFace(bool showBack)
    {
        showingBack = showBack;
        RefreshFront();
        back.gameObject.SetActive(showBack);
        infoIcon.SetActive(!showBack);
        imageIcon.SetActive(showBack);
    }

    public void SetOriginalAlpha(float alpha)
    {
        original.alpha = alpha;
        original.gameObject.SetActive(alpha > 0f);

        // 원본이 완전히 덮고 있을 때는 아래 앞면을 끈다. 켜 두면 경계에서 섞일 때 아래 사진이 비친다.
        bool opaque = alpha >= 1f && originalPicture.enabled;
        if (opaque != originalOpaque)
        {
            originalOpaque = opaque;
            RefreshFront();
        }
    }

    private void RefreshFront()
    {
        front.SetActive(!showingBack && !originalOpaque);
    }

    public void SetFlipIconAlpha(float alpha)
    {
        flipIcon.alpha = alpha;
    }

    public void SyncTo(RectTransform master, float alpha)
    {
        Rect.position = master.position;
        Rect.rotation = master.rotation;
        Rect.localScale = master.localScale;
        group.alpha = alpha;
    }
}

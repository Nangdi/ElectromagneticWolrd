using UnityEngine;
using UnityEngine.UI;

// 카드 한 장의 "한 파장 칸용" 모습. 파장 칸(RectMask2D) 아래에 들어가서 칸 밖은 잘리고, 경계에서는 이웃 칸과 섞인다.
// 위치, 회전, 크기는 CardController가 매 프레임 맞춰 준다. 터치는 받지 않는다.
//
// 경계에서 섞일 때 위 칸의 뷰는 요소마다 반투명하게 겹쳐 그려진다. 그래서 겹친 요소 아래가 비치지 않도록
//  - 앞면 사진, 뒷면(미리 구운 이미지 한 장)은 각각 불투명한 단일 이미지로 두고
//  - 칸과 상관없이 모양이 같은 장식은 칸으로 나누지 않고 카드마다 한 번만 그린다:
//    사진 아래 테두리와 그림자는 CardUnderlayView, 사진 위 제목 탭과 뒤집기 아이콘은 CardChromeView.
public class CardBandView : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private GameObject front;
    [Tooltip("사진은 사진 영역(둥근 마스크)을 가득 채우도록 비율에 맞춰 늘어난다")]
    [SerializeField] private Image picture;
    [SerializeField] private AspectRatioFitter pictureFitter;
    [SerializeField] private NoImageView noImage;
    [Tooltip("레일 위에서 앞면 위에 덮이는 원본 이미지. 모든 칸에서 같아서 경계를 넘어도 그대로 보인다")]
    [SerializeField] private CanvasGroup original;
    [SerializeField] private Image originalPicture;
    [SerializeField] private AspectRatioFitter originalFitter;
    [SerializeField] private RawImage back;


    private bool showingBack;
    private bool originalOpaque;

    public RectTransform Rect => (RectTransform)transform;

    public void Set(CardData data, BandType band, Texture backTexture)
    {
        Sprite sprite = data?.GetSprite(band);
        SetPicture(picture, pictureFitter, sprite);

        bool showNoImage = data != null && sprite == null;
        noImage.gameObject.SetActive(showNoImage);
        if (showNoImage)
            noImage.Set(data);

        SetPicture(originalPicture, originalFitter, data?.GetOriginal());

        back.texture = backTexture;
        SetFace(false);
    }

    private static void SetPicture(Image image, AspectRatioFitter fitter, Sprite sprite)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
        if (sprite != null && sprite.rect.height > 0f)
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
    }

    public void SetFace(bool showBack)
    {
        showingBack = showBack;
        RefreshFront();
        back.gameObject.SetActive(showBack);
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

    public void SyncTo(RectTransform master, float alpha)
    {
        Rect.position = master.position;
        Rect.rotation = master.rotation;
        Rect.localScale = master.localScale;
        group.alpha = alpha;
    }
}

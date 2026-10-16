using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 이미지 없음 안내 안의 썸네일 하나 (둥근 사진 + 파장 이름).
public class NoImageThumbView : MonoBehaviour
{
    [Tooltip("사진 영역 (둥근 마스크). 위쪽에 붙어 있고 높이는 SetSize로 정한다")]
    [SerializeField] private RectTransform pictureArea;
    [SerializeField] private Image picture;
    [SerializeField] private AspectRatioFitter pictureFitter;
    [SerializeField] private TMP_Text label;
    [Tooltip("사진 아래 파장 이름이 차지하는 높이(px)")]
    [SerializeField] private float labelHeight = 36f;

    public RectTransform Rect => (RectTransform)transform;

    public void Set(Sprite sprite, string bandName)
    {
        picture.sprite = sprite;
        picture.enabled = sprite != null;
        if (sprite != null && sprite.rect.height > 0f)
            pictureFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        label.text = bandName ?? string.Empty;
    }

    // 썸네일 개수에 따라 NoImageView가 사진 크기를 정한다.
    public void SetSize(Vector2 pictureSize)
    {
        Rect.sizeDelta = new Vector2(pictureSize.x, pictureSize.y + labelHeight);
        pictureArea.sizeDelta = new Vector2(0f, pictureSize.y);
    }
}

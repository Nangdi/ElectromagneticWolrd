using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 이미지 없음 안내 안의 썸네일 하나 (이미지 + 파장 이름).
public class NoImageThumbView : MonoBehaviour
{
    [SerializeField] private Image picture;
    [SerializeField] private TMP_Text label;

    public void Set(Sprite sprite, string bandName)
    {
        picture.sprite = sprite;
        picture.enabled = sprite != null;
        label.text = bandName ?? string.Empty;
    }
}

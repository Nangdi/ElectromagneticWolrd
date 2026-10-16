using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 해당 파장 이미지가 없을 때 카드 안에 보이는 안내.
// 안내 문구는 스프라이트에 구워져 있고, 이미지가 있는 파장의 썸네일만 런타임에 채운다.
// 디자인: 3장 이하는 124x138 (간격 22), 4장 이상은 103x115 (간격 11). 폭이 모자라면 더 줄인다.
public class NoImageView : MonoBehaviour
{
    [SerializeField] private NoImageThumbView thumbPrefab;
    [SerializeField] private RectTransform thumbRoot;
    [SerializeField] private HorizontalLayoutGroup layout;
    [SerializeField] private Vector2 largeThumbSize = new Vector2(124, 138);
    [SerializeField] private float largeSpacing = 22f;
    [SerializeField] private Vector2 smallThumbSize = new Vector2(103, 115);
    [SerializeField] private float smallSpacing = 11f;
    [Tooltip("이 개수까지는 큰 썸네일을 쓴다")]
    [SerializeField] private int largeMaxCount = 3;

    private readonly List<NoImageThumbView> thumbs = new List<NoImageThumbView>();

    public void Set(CardData data)
    {
        int count = 0;
        for (int i = 0; i < BandInfo.Count; i++)
        {
            var band = (BandType)i;
            Sprite sprite = data?.GetSprite(band);
            if (sprite == null) continue;

            if (thumbs.Count <= count)
                thumbs.Add(Instantiate(thumbPrefab, thumbRoot));
            thumbs[count].gameObject.SetActive(true);
            thumbs[count].Set(sprite, BandInfo.DisplayName(band));
            count++;
        }

        for (int i = count; i < thumbs.Count; i++)
            thumbs[i].gameObject.SetActive(false);

        bool large = count <= largeMaxCount;
        Vector2 size = large ? largeThumbSize : smallThumbSize;
        float spacing = large ? largeSpacing : smallSpacing;
        if (count > 0)
        {
            float fit = (thumbRoot.rect.width - spacing * (count - 1)) / count;
            if (fit < size.x)
                size *= fit / size.x;
        }

        layout.spacing = spacing;
        for (int i = 0; i < count; i++)
            thumbs[i].SetSize(size);
    }
}

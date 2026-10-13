using System.Collections.Generic;
using UnityEngine;

// 해당 파장 이미지가 없을 때 카드 안에 보이는 안내.
// 안내 문구는 스프라이트에 구워져 있고, 이미지가 있는 파장의 썸네일만 런타임에 채운다.
public class NoImageView : MonoBehaviour
{
    [SerializeField] private NoImageThumbView thumbPrefab;
    [SerializeField] private RectTransform thumbRoot;

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
    }
}

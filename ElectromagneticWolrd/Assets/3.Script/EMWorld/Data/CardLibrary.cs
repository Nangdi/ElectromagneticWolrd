using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// card.json 내용: 카드 라벨(제목 탭)에 들어가는 이름만 둔다. 파장별 화면은 이미지만 쓴다.
[Serializable]
public class CardInfoFile
{
    public string title;
}

// 체험용 이미지 카드 한 장. 파장별 이미지가 없으면(null) 그 칸에서는 "이미지 없음" 안내가 보인다.
public class CardData
{
    public string id;
    public string title;

    // BandType 순서(라디오파 → 감마선)
    public readonly Sprite[] bandSprites = new Sprite[BandInfo.Count];

    // 레일 위에서 보이는 원본(맨눈으로 본) 이미지. 없으면 가시광선 이미지를 쓴다.
    public Sprite originalSprite;

    public Sprite GetSprite(BandType band)
    {
        int index = (int)band;
        return index < bandSprites.Length ? bandSprites[index] : null;
    }

    // 원본 이미지. 없으면 가시광선, 그것도 없으면 처음 찾은 파장 이미지.
    public Sprite GetOriginal()
    {
        if (originalSprite != null) return originalSprite;
        Sprite visible = GetSprite(BandType.Visible);
        if (visible != null) return visible;
        return bandSprites.FirstOrDefault(sprite => sprite != null);
    }
}

// 카드 이미지와 문구를 StreamingAssets/Cards에서 읽어 온다. 현장에서 파일만 바꾸면 다시 빌드하지 않고 반영된다.
//  Cards/<카드ID>/<파장>.png   파장: Radio, Microwave, Infrared, Visible, Ultraviolet, XRay, Gamma
//  Cards/<카드ID>/Original.png 레일 위 원본 이미지 (선택)
//  Cards/<카드ID>/card.json    라벨에 들어가는 이름(title)
// 카드는 폴더 이름 순서로 나온다. 파장 이미지가 하나도 없는 폴더는 건너뛴다.
public static class CardLibrary
{
    public const string FolderName = "Cards";
    public const string InfoFileName = "card.json";
    public const string OriginalName = "Original";

    private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg" };

    public static string RootPath => Path.Combine(Application.streamingAssetsPath, FolderName);

    public static List<CardData> Load()
    {
        var cards = new List<CardData>();
        if (!Directory.Exists(RootPath))
        {
            Debug.LogWarning($"카드 폴더가 없습니다: {RootPath}");
            return cards;
        }

        foreach (string dir in Directory.GetDirectories(RootPath).OrderBy(d => d, StringComparer.Ordinal))
        {
            try
            {
                CardData card = LoadCard(dir);
                if (card != null)
                    cards.Add(card);
            }
            catch (Exception e)
            {
                Debug.LogError($"카드를 읽지 못해 건너뜁니다: {dir}\n{e.Message}");
            }
        }

        Debug.Log($"카드 {cards.Count}장을 읽었습니다: {RootPath}");
        return cards;
    }

    // 읽어 온 이미지 텍스처를 해제한다.
    public static void Unload(CardData card)
    {
        if (card == null) return;

        foreach (Sprite sprite in card.bandSprites.Append(card.originalSprite))
        {
            if (sprite == null) continue;
            UnityEngine.Object.Destroy(sprite.texture);
            UnityEngine.Object.Destroy(sprite);
        }
    }

    private static CardData LoadCard(string dir)
    {
        string id = Path.GetFileName(dir);
        var card = new CardData { id = id, title = id };
        ReadInfo(dir, card);

        for (int i = 0; i < BandInfo.Count; i++)
            card.bandSprites[i] = LoadSprite(dir, ((BandType)i).ToString());
        card.originalSprite = LoadSprite(dir, OriginalName);

        if (card.bandSprites.All(sprite => sprite == null))
        {
            Debug.LogWarning($"파장 이미지가 하나도 없어 건너뜁니다: {dir}");
            Unload(card);
            return null;
        }
        return card;
    }

    private static void ReadInfo(string dir, CardData card)
    {
        string path = Path.Combine(dir, InfoFileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"{InfoFileName}이 없어 폴더 이름을 제목으로 씁니다: {dir}");
            return;
        }

        var info = JsonUtility.FromJson<CardInfoFile>(File.ReadAllText(path));
        if (info == null) return;

        if (!string.IsNullOrWhiteSpace(info.title))
            card.title = info.title.Trim();
    }

    private static Sprite LoadSprite(string dir, string name)
    {
        string path = Extensions.Select(ext => Path.Combine(dir, name + ext)).FirstOrDefault(File.Exists);
        if (path == null) return null;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            name = $"{Path.GetFileName(dir)}_{name}",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        if (!tex.LoadImage(File.ReadAllBytes(path), true)) // 시스템 메모리 사본은 버린다
        {
            UnityEngine.Object.Destroy(tex);
            Debug.LogError($"이미지를 읽지 못했습니다: {path}");
            return null;
        }

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = tex.name;
        return sprite;
    }
}

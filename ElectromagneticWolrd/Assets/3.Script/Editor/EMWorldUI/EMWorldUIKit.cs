using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 전자기파로 보는 세상 UI 에디터 도구들이 같이 쓰는 경로, 규격, 색, 생성 헬퍼.
public static class EMWorldUIKit
{
    public const string MenuRoot = "Tools/전자기파로 보는 세상/";

    #region 경로

    public const string SpriteRoot = "Assets/4.Sprite";
    public const string StaticDir = SpriteRoot + "/Static";
    public const string PartsDir = SpriteRoot + "/Parts";
    public const string GuideDir = SpriteRoot + "/_Guide";

    public const string PrefabRoot = "Assets/2.Model/Prefabs/EMWorld";
    public const string PartPrefabDir = PrefabRoot + "/Parts";
    public const string PanelPrefabDir = PrefabRoot + "/Panels";
    public const string CanvasPrefabPath = PrefabRoot + "/Canvas_EMWorld.prefab";

    // 카드 사진과 문구는 현장에서 바꿀 수 있도록 StreamingAssets에 둔다 (CardLibrary가 실행할 때 읽음)
    public const string CardsDir = "Assets/StreamingAssets/" + CardLibrary.FolderName;

    public const string StaticLayerName = "StaticLayer";

    private const string BoldFontPath = "Assets/10.Font/defaultFont/NotoSansKR-Bold SDF.asset";
    private const string MediumFontPath = "Assets/10.Font/defaultFont/NotoSansKR-Medium SDF.asset";

    public static string StaticSpritePath(string panel) => $"{StaticDir}/Static_{panel}.png";
    public static string GuidePath(string panel) => $"{GuideDir}/Guide_{panel}.png";
    public static string PanelPrefabPath(string panel) => $"{PanelPrefabDir}/Panel_{panel}.prefab";
    public static string PartSpritePath(string name) => $"{PartsDir}/{name}.png";
    public static string PartPrefabPath(string name) => $"{PartPrefabDir}/{name}.prefab";
    public static string CardImagePath(string cardId, BandType band) => $"{CardsDir}/{cardId}/{band}.png";
    public static string CardInfoPath(string cardId) => $"{CardsDir}/{cardId}/{CardLibrary.InfoFileName}";

    #endregion

    #region 화면 규격

    // 55형 4K 모니터 2대를 가로로 이어 붙인 터치 테이블
    public const int ScreenWidth = 7680;
    public const int ScreenHeight = 2160;
    public static readonly Vector2 ReferenceResolution = new Vector2(ScreenWidth, ScreenHeight);

    // 화면을 8칸(칸당 960x2160)으로 나눠 왼쪽 7칸은 파장 영역, 맨 오른쪽 1칸은 정보검색
    public const int ColumnWidth = 960;
    public const int SpectrumWidth = ColumnWidth * BandInfo.Count;              // 6720
    public static readonly Vector2 SpectrumSize = new Vector2(SpectrumWidth, ScreenHeight);
    public static readonly Vector2 SpectrumCenter = new Vector2(-ColumnWidth * 0.5f, 0);   // 화면 중앙 기준
    public static readonly Vector2 InfoColumnCenter = new Vector2(ScreenWidth * 0.5f - ColumnWidth * 0.5f, 0);
    // 정보검색 칸은 90° 돌려 가로 화면(2160x960)으로 쓴다. 테이블 오른쪽 끝에 선 한 사람이 탐색한다.
    public static readonly Vector2 InfoPanelSize = new Vector2(ScreenHeight, ColumnWidth);
    public const float InfoPanelAngle = 90f; // 화면 위쪽이 테이블 왼쪽(-x)을 향함 = 오른쪽 끝에 선 사람 기준으로 바로 보임
    // 디자인(키요스크 위치 샘플)에서 키오스크 화면은 칸보다 조금 작게(2081x925) 칸 왼쪽에 붙어 있다
    public const float InfoPanelScale = 2081f / ScreenHeight;
    public static readonly Vector2 InfoPanelOffset = new Vector2(-21.5f, 0.5f); // InfoBoard 중앙 기준

    // 파장 칸 중심 x (Panel_Spectrum 중앙 기준)
    public static float BandCenterX(int index) => -SpectrumWidth * 0.5f + ColumnWidth * (index + 0.5f);

    // 레일 (RailController 기본값과 맞춘다). 디자인 배경의 레일 띠: y 944~1216
    public const float RailHalfHeight = 136f;
    // 레일 오른쪽 끝(키오스크 바로 왼쪽) 카드 위에 덮이는 그림자
    public static readonly Vector2 RailEndShadowPos = new Vector2(6672.5f - SpectrumWidth * 0.5f, -1f); // Panel_Spectrum 중앙 기준
    public static readonly Vector2 RailEndShadowSize = new Vector2(87, 272);

    // 카드 (꺼냈을 때 크기 = 디자인 팝업의 이미지 영역. 레일 위에서는 EMWorldConfig.railCardScale 배 = 216x240)
    // 아래 좌표는 모두 사진 영역 중앙 기준이다.
    public static readonly Vector2 CardSize = new Vector2(500, 558);
    public const int CardSpriteSize = 512;                // 임시 카드 이미지 크기
    public const float CardCornerRadius = 27f;
    public static readonly Vector2 CardPopupPos = new Vector2(10.5f, 9f);   // 회색 테두리 + X 탭 (Card_Popup.png)
    public static readonly Vector2 CardPopupSize = new Vector2(595, 628);
    public static readonly Vector2 CloseButtonPos = new Vector2(259, 289);  // 테두리 오른쪽 위 X
    public static readonly Vector2 CloseButtonSize = new Vector2(80, 80);
    public const float TitleTabLeft = -293f;                                // 제목 탭 (왼쪽 아래, 글자 길이만큼 늘어남)
    public const float TitleTabY = -219f;
    public static readonly Vector2 TitleTabMinSize = new Vector2(221, 70);
    public static readonly Vector2 FlipButtonSize = new Vector2(88, 88);
    public static readonly Vector2 FlipButtonPos = new Vector2(CardSize.x * 0.5f - 60f, -CardSize.y * 0.5f + 60f); // 사진 오른쪽 아래
    // 레일 위 카드 그림자: 디자인의 216x240 사진 기준 269x293 그림자를 카드 크기(1/0.432배)로 환산
    private const float RailDesignScale = 240f / 558f;
    public static readonly Vector2 RailShadowPos = new Vector2(6.5f, -5.5f) / RailDesignScale;
    public static readonly Vector2 RailShadowSize = new Vector2(269, 293) / RailDesignScale;

    // 정적 레이어 패널: 이름과 크기
    public static readonly (string name, Vector2Int size)[] Panels =
    {
        ("Spectrum", new Vector2Int(SpectrumWidth, ScreenHeight)),
        ("InfoMenu", new Vector2Int(ScreenHeight, ColumnWidth)),
        ("InfoDetail01", new Vector2Int(ScreenHeight, ColumnWidth)),
        ("InfoDetail02", new Vector2Int(ScreenHeight, ColumnWidth)),
        ("InfoDetail03", new Vector2Int(ScreenHeight, ColumnWidth)),
        ("InfoDetail04", new Vector2Int(ScreenHeight, ColumnWidth)),
    };

    public static readonly string[] InfoDetailPanels = { "InfoDetail01", "InfoDetail02", "InfoDetail03", "InfoDetail04" };

    #endregion

    #region 색

    // 디자인 PSD(06-전자기파로 보는 세상)에서 뽑은 값
    public static readonly Color BgColor = new Color32(20, 20, 20, 255);
    public static readonly Color CardColor = new Color32(58, 58, 58, 255);       // 팝업 테두리 위쪽 색
    public static readonly Color SoftBoxColor = new Color32(255, 255, 255, 24);
    public static readonly Color AccentColor = new Color32(250, 114, 85, 255);   // 키오스크 제목 주황
    public static readonly Color TextColor = Color.white;
    public static readonly Color SubTextColor = new Color32(170, 170, 170, 255);
    public static readonly Color DarkTextColor = new Color32(51, 51, 51, 255);   // 흰 제목 탭 위 글자

    public static readonly Color LabelBoxColor = new Color32(46, 46, 46, 255);
    public static readonly Color ColumnLineColor = new Color32(58, 58, 64, 255);
    public static readonly Color WaveColor = new Color32(120, 120, 126, 255);
    public static readonly Color NoImageBoxColor = new Color32(46, 46, 46, 255);

    // 정보검색 메뉴 버튼 4개 (01 전자기파란 / 02 파장과 주파수 / 03 전리‧비전리 / 04 60Hz)
    public static readonly Color[] InfoMenuColors =
    {
        new Color32(18, 70, 92, 255),
        new Color32(24, 80, 52, 255),
        new Color32(62, 40, 96, 255),
        new Color32(96, 60, 20, 255),
    };

    // 임시 카드 이미지 배경 (BandType 순서). 실제 이미지로 덮어쓰면 쓰이지 않는다.
    public static readonly Color[] TempBandColors =
    {
        new Color32(30, 60, 170, 255),   // 라디오파
        new Color32(90, 90, 96, 255),    // 마이크로파
        new Color32(200, 70, 30, 255),   // 적외선
        new Color32(60, 140, 80, 255),   // 가시광선
        new Color32(110, 40, 180, 255),  // 자외선
        new Color32(20, 120, 130, 255),  // X선
        new Color32(170, 30, 110, 255),  // 감마선
    };

    #endregion

    #region 리소스

    private static TMP_FontAsset boldFont;
    private static TMP_FontAsset mediumFont;
    private static Sprite builtinRound;
    private static Sprite circle;

    public static TMP_FontAsset BoldFont => boldFont != null ? boldFont : boldFont = LoadFont(BoldFontPath);
    public static TMP_FontAsset MediumFont => mediumFont != null ? mediumFont : mediumFont = LoadFont(MediumFontPath);

    // 정적 레이어 렌더링용 둥근 박스 (유니티 기본 UISprite)
    public static Sprite BuiltinRound => builtinRound != null
        ? builtinRound
        : builtinRound = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    // 임시 디자인 렌더링용 안티에일리어싱된 흰 원 (256px)
    public static Sprite Circle
    {
        get
        {
            if (circle != null) return circle;

            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude;
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d));
            }
            tex.SetPixels(pixels);
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            circle.hideFlags = HideFlags.HideAndDontSave;
            return circle;
        }
    }

    private static TMP_FontAsset LoadFont(string path)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font != null) return font;

        Debug.LogWarning($"폰트를 찾지 못해 TMP 기본 폰트를 사용합니다: {path}");
        return TMP_Settings.defaultFontAsset;
    }

    public static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogError($"스프라이트를 찾지 못했습니다: {path}");
        return sprite;
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    #endregion

    #region 생성 헬퍼

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        if (parent != null)
            go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // 부모 중앙 기준 위치/크기 배치
    public static RectTransform Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    // 앵커 비율로 늘려 배치. inset이 음수면 바깥으로 커진다.
    public static RectTransform Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, float inset = 0f)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        return rt;
    }

    public static RectTransform StretchFull(RectTransform rt) => Stretch(rt, Vector2.zero, Vector2.one);

    public static Image AddImage(RectTransform rt, Color color)
    {
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // 스프라이트 이미지. 9-slice 테두리가 있으면 Sliced로 늘린다.
    public static Image AddSprite(RectTransform rt, Sprite sprite, Color? tint = null)
    {
        Image image = AddImage(rt, tint ?? Color.white);
        image.sprite = sprite;
        image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        return image;
    }

    // 정적 레이어 렌더링용 둥근 박스 (기본 UISprite의 둥글기를 화면 규격에 맞게 키움)
    public static Image AddRoundBox(RectTransform rt, Color color)
    {
        Image image = AddImage(rt, color);
        image.sprite = BuiltinRound;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 0.4f;
        return image;
    }

    public static RectTransform CreateRoundBox(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform box = Place(CreateRect(name, parent), pos, size);
        AddRoundBox(box, color);
        return box;
    }

    public static TextMeshProUGUI AddText(RectTransform rt, string text, float fontSize, Color color, bool bold = false,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = bold ? BoldFont : MediumFont;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static TextMeshProUGUI CreateText(RectTransform parent, string name, string text, Vector2 pos, Vector2 size,
        float fontSize, Color color, bool bold = false, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        return AddText(Place(CreateRect(name, parent), pos, size), text, fontSize, color, bold, alignment);
    }

    // 부모 영역을 가득 채우는 텍스트
    public static TextMeshProUGUI FillText(RectTransform parent, string name, string text, float fontSize, Color color, bool bold = false)
    {
        return AddText(Stretch(CreateRect(name, parent), Vector2.zero, Vector2.one, 10), text, fontSize, color, bold);
    }

    #endregion

    #region 인스펙터 연결

    public static void Wire(Object target, params (string field, Object value)[] pairs)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in pairs)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"{target.GetType().Name}에 '{field}' 필드가 없습니다.");
                continue;
            }
            property.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void WireList(Object target, string field, IList<Object> values)
    {
        var so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null || !property.isArray)
        {
            Debug.LogError($"{target.GetType().Name}에 '{field}' 리스트 필드가 없습니다.");
            return;
        }

        property.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    #endregion
}

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static EMWorldUIKit;

// 정적 레이어(패널당 PNG 한 장), 글자가 들어간 부품 스프라이트, 임시 카드 이미지의 "임시 디자인".
// 여기 있는 내용은 PNG로 구워질 뿐 씬/프리팹에는 오브젝트로 남지 않는다.
// 문구나 배치를 바꾸려면 여기를 고친 뒤 "임시 스프라이트 다시 내보내기"를 실행하거나, 디자인 PNG로 덮어쓴다.
public static class EMWorldStaticLayouts
{
    #region 정보검색 칸 (2160x960 가로 화면, 한 사람용)

    public static readonly Dictionary<string, Action<RectTransform>> InfoPanels = new Dictionary<string, Action<RectTransform>>
    {
        { "InfoMenu", InfoMenu },
        { "InfoDetail01", root => InfoDetail(root, 0) },
        { "InfoDetail02", root => InfoDetail(root, 1) },
        { "InfoDetail03", root => InfoDetail(root, 2) },
        { "InfoDetail04", root => InfoDetail(root, 3) },
    };

    // 메뉴 버튼 자리 (PrefabBuilder와 같이 쓴다). 디자인: 그림자 포함 510x610 카드 4장을 가로로 배치
    public static readonly Vector2 InfoMenuButtonSize = new Vector2(510, 610);
    public static readonly Vector2[] InfoMenuButtonPos =
    {
        new Vector2(-743, -101), new Vector2(-243, -100), new Vector2(257, -101), new Vector2(757, -101),
    };
    // 그림자는 누르는 영역에서 뺀다 (left, bottom, right, top)
    public static readonly Vector4 InfoMenuButtonRaycastPadding = new Vector4(9, 25, 21, 5);
    // 세부 화면 오른쪽 위 [HOME] 버튼 (메뉴로 돌아감)
    public static readonly Vector2 BackButtonPos = new Vector2(914, 378.5f);
    public static readonly Vector2 BackButtonSize = new Vector2(230, 91);

    public static readonly string[] InfoTitles =
    {
        "전자기파란 무엇일까?",
        "파장과 주파수는 어떤 관계일까?",
        "전리‧비전리 전자기파는 무엇이 다를까?",
        "전력설비의 60Hz는 어디에 있을까?",
    };

    private static string Accent(string text) => $"<color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>{text}</color>";

    private static readonly string[] InfoBodies =
    {
        "전자기파는 변화하는 전기장과 자기장이 서로를 만들어 내며 공간을 따라 전달되는 파동입니다.\n\n" +
        "빛도 전자기파의 한 종류입니다.\n\n" +
        "전자기파는 물질이 없어도 진공을 통해 전달될 수 있습니다.",

        "파장이 짧을수록 주파수는 높아지고, 에너지는 커집니다.\n\n" +
        "‧ 파장: 짧아짐\n‧ 주파수: 높아짐\n‧ 에너지: 커짐\n\n" +
        $"{Accent("<b>E = h x f</b>")}\n<size=80%>(E: 에너지, h: 플랑크 상수, f: 주파수)</size>",

        "에너지가 매우 높은 전자기파는 원자의 전자를 떼어낼 수 있습니다. 이를 전리작용이라고 합니다.\n\n" +
        $"{Accent("<b>비전리 전자기파</b>")}\n라디오파, 마이크로파, 적외선, 가시광선 (전자를 떼어낼 수 없음)\n\n" +
        "<b>전리 전자기파</b>\nX선, 감마선, 에너지가 높은 일부 자외선 (전자를 떼어낼 수 있음)\n\n" +
        "<size=85%>※ 자외선 중에서도 에너지가 높은 일부 영역부터 전리작용이 가능합니다.</size>",

        "우리나라의 교류 전력은 1초에 60번 방향이 바뀌는 60Hz를 사용합니다.\n\n" +
        $"{Accent("<b>발전소 → 송전선 → 변전소 → 우리 집</b>")}\n\n" +
        "교류가 흐르는 전력설비 주변에는 60Hz로 변화하는 전기장과 자기장이 형성됩니다. " +
        "이는 매우 낮은 주파수의 비전리 영역 전자기파에 해당합니다.",
    };

    private static void InfoFrame(RectTransform root)
    {
        AddImage(StretchFull(CreateRect("Background", root)), BgColor);
        CreateRoundBox(root, "Box_Frame", Vector2.zero, new Vector2(InfoPanelSize.x - 48, InfoPanelSize.y - 48), CardColor);
    }

    private static void InfoMenu(RectTransform root)
    {
        InfoFrame(root);
        CreateText(root, "Title", "전자기파 알아보기", new Vector2(0, 360), new Vector2(1900, 100), 72, TextColor, true);
        CreateText(root, "Subtitle", "전자기파의 원리와 전력설비의 60Hz를 알아보아요!", new Vector2(0, 275), new Vector2(1900, 60),
            40, SubTextColor);
    }

    private static void InfoDetail(RectTransform root, int index)
    {
        InfoFrame(root);
        CreateText(root, "Title", $"{Accent($"{index + 1:00}")}  {InfoTitles[index]}", new Vector2(0, 370), new Vector2(1900, 90),
            60, TextColor, true);

        RectTransform box = CreateRoundBox(root, "Box_Body", new Vector2(0, -10), new Vector2(1960, 580), SoftBoxColor);
        TextMeshProUGUI body = AddText(Stretch(CreateRect("Body", box), Vector2.zero, Vector2.one, 44), InfoBodies[index], 44,
            TextColor, false, TextAlignmentOptions.TopLeft);
        body.enableAutoSizing = true;
        body.fontSizeMin = 30;
        body.fontSizeMax = 44;
        body.lineSpacing = 12;
    }

    #endregion

    #region 파장 영역 (6720x2160, 픽셀로 직접 그림)

    // 배경, 칸 구분선, 가시광선 무지개, 가운데 레일, 왼쪽(긴 파장) → 오른쪽(짧은 파장)으로 촘촘해지는 사인파
    public static Texture2D SpectrumTexture()
    {
        const int width = SpectrumWidth;
        const int height = ScreenHeight;
        const float centerY = height * 0.5f;
        const float amplitude = 300f;
        const float thickness = 9f;
        const float lambdaStart = 1500f; // 왼쪽 끝 파장(px)
        const float lambdaEnd = 48f;     // 오른쪽 끝 파장(px)

        var pixels = new Color32[width * height];
        float k = Mathf.Log(lambdaEnd / lambdaStart) / width; // λ(x) = λ0 * e^(kx)
        int visibleIndex = (int)BandType.Visible;

        for (int x = 0; x < width; x++)
        {
            int column = x / ColumnWidth;
            Color bg = BgColor;
            if (column == visibleIndex)
            {
                float t = (x - column * ColumnWidth) / (float)ColumnWidth;
                bg = Color.HSVToRGB(Mathf.Lerp(0f, 0.78f, t), 0.85f, 0.85f);
            }
            if (x % ColumnWidth < 2 || x % ColumnWidth >= ColumnWidth - 2)
                bg = column == visibleIndex ? bg : ColumnLineColor;

            // 위상 = ∫ 2π/λ(x) dx, 기울기 = A cos(위상) * 2π/λ(x)
            float lambda = lambdaStart * Mathf.Exp(k * x);
            float phase = 2f * Mathf.PI / lambdaStart * (1f - Mathf.Exp(-k * x)) / k;
            float waveY = centerY + amplitude * Mathf.Sin(phase);
            float slope = amplitude * Mathf.Cos(phase) * 2f * Mathf.PI / lambda;
            float slopeScale = 1f / Mathf.Sqrt(1f + slope * slope);
            Color waveColor = column == visibleIndex ? Color.white : WaveColor;

            for (int y = 0; y < height; y++)
            {
                Color c = bg;

                float fromRail = Mathf.Abs(y + 0.5f - centerY);
                if (fromRail <= RailHalfHeight)
                    c = Color.Lerp(c, Color.white, 0.05f);
                if (Mathf.Abs(fromRail - RailHalfHeight) < 1.5f)
                    c = Color.Lerp(c, Color.white, 0.25f);

                float dist = Mathf.Abs(y + 0.5f - waveY) * slopeScale;
                float coverage = Mathf.Clamp01(thickness * 0.5f + 0.5f - dist);
                if (coverage > 0f)
                    c = Color.Lerp(c, waveColor, coverage * 0.85f);

                pixels[y * width + x] = c;
            }
        }

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    #endregion

    #region 글자가 들어간 부품 스프라이트 (배경 투명)

    public static readonly List<(string name, Vector2Int size, Action<RectTransform> draw)> TextParts = BuildTextParts();

    public static string InfoMenuButtonSprite(int index) => $"Button_Info{index + 1:00}";

    private static List<(string, Vector2Int, Action<RectTransform>)> BuildTextParts()
    {
        var parts = new List<(string, Vector2Int, Action<RectTransform>)>();

        for (int i = 0; i < InfoTitles.Length; i++)
        {
            int index = i;
            parts.Add((InfoMenuButtonSprite(i), Vector2Int.RoundToInt(InfoMenuButtonSize), root => InfoMenuButton(root, index)));
        }

        parts.Add(("Button_Back", Vector2Int.RoundToInt(BackButtonSize), root =>
        {
            AddRoundBox(root, Color.white);
            FillText(root, "Text", "HOME", 40, DarkTextColor, true);
        }));

        // 카드 오른쪽 아래 뒤집기 버튼: 앞면에서는 "정보", 뒷면에서는 "사진"
        parts.Add(("Button_CardInfo", Vector2Int.RoundToInt(FlipButtonSize), root =>
            CircleButton(root, "정보", AccentColor, DarkTextColor)));
        parts.Add(("Button_CardImage", Vector2Int.RoundToInt(FlipButtonSize), root =>
            CircleButton(root, "사진", LabelBoxColor, TextColor)));

        parts.Add(("NoImage_Message", new Vector2Int(347, 193), root =>
            FillText(root, "Text", "해당 파장대에서 촬영한\n이미지가 존재하지 않습니다.", 30, TextColor, true)));

        return parts;
    }

    private static void CircleButton(RectTransform root, string text, Color box, Color textColor)
    {
        AddImage(root, box).sprite = Circle;
        FillText(root, "Text", text, 26, textColor, true);
    }

    private static void InfoMenuButton(RectTransform root, int index)
    {
        AddRoundBox(root, InfoMenuColors[index]);
        CreateText(root, "Number", $"{index + 1:00}", new Vector2(-150, 230), new Vector2(130, 80), 56, AccentColor, true);
        TextMeshProUGUI title = CreateText(root, "Title", InfoTitles[index], new Vector2(0, -170), new Vector2(420, 140), 48,
            TextColor, true);
        title.enableAutoSizing = true;
        title.fontSizeMin = 34;
        title.fontSizeMax = 48;
    }

    #endregion

    #region 임시 카드 이미지 (512x512)

    // 자연‧사물은 적외선‧가시광‧자외선‧X선 4장, 천체는 7장 모두. 나머지 파장 칸은 "이미지 없음" 안내가 보인다.
    // 설명은 임시 문구다. 실제 문구는 StreamingAssets/Cards/<카드ID>/card.json에서 고친다.
    public static readonly (string id, string title, CardCategory category, string description)[] TempCards =
    {
        ("Card_01", "꽃", CardCategory.Nature,
            "꽃잎에는 자외선으로만 보이는 무늬가 있어, 자외선을 볼 수 있는 곤충에게 꿀이 있는 곳을 알려 줍니다."),
        ("Card_02", "고양이", CardCategory.Nature,
            "적외선으로 보면 체온이 높은 눈과 귀 주변은 밝게, 털로 덮인 부분은 어둡게 보입니다."),
        ("Card_03", "나무", CardCategory.Nature,
            "식물의 잎은 근적외선을 강하게 반사해서, 적외선 사진에서는 잎이 하얗게 빛나 보입니다."),
        ("Card_04", "게임 컨트롤러", CardCategory.Object,
            "X선으로 보면 플라스틱 껍데기 속의 회로 기판과 금속 부품이 드러납니다."),
        ("Card_05", "리모컨", CardCategory.Object,
            "리모컨은 눈에 보이지 않는 적외선 신호로 텔레비전에 명령을 보냅니다."),
        ("Card_06", "주택", CardCategory.Object,
            "적외선 열화상으로 보면 창문과 틈새로 열이 새어 나가는 곳을 찾을 수 있습니다."),
        ("Card_07", "은하", CardCategory.Space,
            "은하는 라디오파부터 감마선까지 모든 파장으로 빛을 냅니다. 파장마다 별, 먼지, 가스 등 서로 다른 모습이 보입니다."),
        ("Card_08", "태양", CardCategory.Space,
            "자외선과 X선으로 보면 태양 표면 위의 뜨거운 코로나와 폭발(플레어)이 뚜렷하게 보입니다."),
        ("Card_09", "게 성운", CardCategory.Space,
            "1054년에 폭발한 초신성의 잔해입니다. 중심의 펄서가 라디오파부터 감마선까지 강한 전자기파를 냅니다."),
    };

    private static readonly BandType[] EverydayBands = { BandType.Infrared, BandType.Visible, BandType.Ultraviolet, BandType.XRay };

    public static bool HasTempImage(CardCategory category, BandType band) =>
        category == CardCategory.Space || Array.IndexOf(EverydayBands, band) >= 0;

    public static void TempCard(RectTransform root, string id, string title, CardCategory category, BandType band)
    {
        AddImage(StretchFull(CreateRect("Background", root)), TempBandColors[(int)band]);
        CreateText(root, "Category", CardCategoryInfo.DisplayName(category), new Vector2(0, 200), new Vector2(480, 50), 32, TextColor);
        CreateText(root, "Number", id.Replace("Card_", ""), new Vector2(0, 50), new Vector2(480, 220), 200,
            new Color(1f, 1f, 1f, 0.9f), true);
        CreateText(root, "Title", title, new Vector2(0, -110), new Vector2(480, 70), 52, TextColor, true);
        RectTransform box = CreateRoundBox(root, "Box_Band", new Vector2(0, -195), new Vector2(300, 70), new Color(0f, 0f, 0f, 0.35f));
        FillText(box, "Band", BandInfo.DisplayName(band), 40, TextColor, true);
    }

    #endregion
}

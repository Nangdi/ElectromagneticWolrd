using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EMWorldUIKit;

// ESC 설정창(Canvas_Settings)과 F1 단축키 도움말(Canvas_Help) 프리팹을 만들고 씬에 적용한다.
// 메뉴: Tools > 전자기파로 보는 세상 > 8. 설정창·도움말 다시 만들기 (덮어쓰기)
//
//  Canvas_Settings (sortingOrder 100)  왼쪽 모니터 가운데
//   └ Panel_Settings  화면(항상 위, 커서) / 운영 / 레일 | 연출  — 바꾸면 바로 적용하고 persistentDataPath에 저장
//  Canvas_Help (sortingOrder 110)      오른쪽 모니터 가운데
//   └ Panel_Help      HotkeyHelp에 등록된 키 목록 (줄은 Parts/HelpRow 프리팹으로 실행할 때 만든다)
//
// 설정창에 항목을 추가하려면 아래 목록에 한 줄 추가하고 메뉴를 다시 실행한다.
public static class EMWorldSettingsBuilder
{
    private const string SettingsCanvasName = "Canvas_Settings";
    private const string HelpCanvasName = "Canvas_Help";
    private const string OldSettingsCanvasName = "SettingsCanvas"; // 스타트팩 기본 설정창 (교체 대상)
    private static string SettingsPrefabPath => PrefabRoot + "/Canvas_Settings.prefab";
    private static string HelpPrefabPath => PrefabRoot + "/Canvas_Help.prefab";

    // 화면 설정 (gameSettingData.json, SettingsPanelUI가 처리)
    private static readonly (string field, string label)[] ScreenToggles =
    {
        ("useUnityOnTop", "항상 맨 위에 띄우기"),
        ("showMouseCursor", "마우스 커서 표시"),
    };

    private enum Column { Left, Right }

    private struct Item
    {
        public Column column;
        public string section;   // 비어 있지 않으면 이 항목 위에 섹션 제목을 넣는다
        public string field;     // EMWorldConfig 필드
        public string label;
        public string unit;      // null이면 토글
        public float min, max;
        public int decimals;
    }

    private static Item Number(Column column, string section, string field, string label, string unit, float min, float max, int decimals = 0) =>
        new Item { column = column, section = section, field = field, label = label, unit = unit, min = min, max = max, decimals = decimals };

    private static Item Toggle(Column column, string section, string field, string label) =>
        new Item { column = column, section = section, field = field, label = label };

    // 운영값 (emworld.json, EMWorldSettingsSection이 처리)
    private static readonly Item[] Items =
    {
        Number(Column.Left, "운영", "cardReturnSeconds", "꺼낸 카드 자동 회수", "초", 5, 600),
        Number(Column.Left, null, "idleResetSeconds", "무입력 대기 복귀", "초", 10, 3600),
        Number(Column.Left, "레일", "railSpeed", "흐르는 속도", "px/초", 0, 1000),
        Number(Column.Left, null, "railMinSpacing", "카드 최소 간격", "px", 250, 2000),
        Number(Column.Left, null, "railCardScale", "카드 크기 (꺼낸 크기 대비)", "배", 0.2f, 1f, 2),
        Toggle(Column.Left, null, "railShowsOriginal", "레일 위 카드는 원본 사진으로"),
        Number(Column.Right, "연출", "bandBlendWidth", "파장 경계 블렌딩 폭", "px", 0, 400),
        Number(Column.Right, null, "pickupSeconds", "꺼낼 때 커지는 시간", "초", 0, 2, 2),
        Number(Column.Right, null, "rotateSeconds", "관람객 쪽으로 도는 시간", "초", 0, 2, 2),
        Toggle(Column.Right, null, "cardFlipEnabled", "카드 뒤집기 버튼 사용"),
        Number(Column.Right, null, "flipSeconds", "카드 뒤집기 시간", "초", 0.1f, 3, 2),
        Number(Column.Right, null, "returnAnimSeconds", "돌아갈 때 사라지는 시간", "초", 0.05f, 3, 2),
        Number(Column.Right, null, "originalFadeSeconds", "원본 → 파장 사진 전환 시간", "초", 0, 3, 2),
        Number(Column.Right, null, "glideDamping", "미끄러짐 감속 (클수록 짧게)", "", 0.5f, 20, 1),
        Number(Column.Right, null, "glideMaxSpeed", "미끄러짐 최대 속도", "px/초", 500, 20000),
    };

    // 배치 (7680x2160 기준)
    private static readonly Vector2 SettingsPanelPos = new Vector2(-ScreenWidth * 0.25f, 0);   // 왼쪽 모니터 가운데
    private static readonly Vector2 SettingsPanelSize = new Vector2(2700, 2000);
    private static readonly Vector2 HelpPanelPos = new Vector2(ScreenWidth * 0.25f - 300f, 0); // 오른쪽 모니터 (정보검색 칸 피함)
    private const float HelpPanelWidth = 1900f;
    private const float ColumnWidthPx = 1250f;
    private const float RowHeight = 96f;
    private const float RowSpacing = 18f;

    private const float TitleSize = 110f;
    private const float SectionSize = 72f;
    private const float BodySize = 56f;
    private const float SubSize = 44f;

    private static readonly Color PanelColor = new Color32(36, 36, 38, 245);
    private static readonly Color FieldColor = new Color32(70, 70, 74, 255);
    private static readonly Color KeyBoxColor = new Color32(250, 114, 85, 255); // = AccentColor

    [MenuItem(MenuRoot + "8. 설정창·도움말 다시 만들기 (덮어쓰기)", priority = 8)]
    public static void BuildAll()
    {
        EnsureFolder(PartPrefabDir);
        GameObject rowPrefab = Save(CreateHelpRow(), PartPrefabPath("HelpRow"));
        GameObject settings = Save(CreateSettingsCanvas(), SettingsPrefabPath);
        GameObject help = Save(CreateHelpCanvas(rowPrefab), HelpPrefabPath);

        ApplyToScene(settings, help);
        AssetDatabase.SaveAssets();
        Debug.Log("설정창·도움말 적용 완료. 씬을 저장(Ctrl+S)하세요.");
    }

    private static GameObject Save(GameObject temp, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return prefab;
    }

    #region 공통

    private static GameObject CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static VerticalLayoutGroup AddVertical(RectTransform rt, float spacing, RectOffset padding = null)
    {
        var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding ?? new RectOffset();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static HorizontalLayoutGroup AddHorizontal(RectTransform rt, float spacing)
    {
        var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static LayoutElement Size(Component c, float width = -1, float height = -1, float flexibleWidth = -1)
    {
        var element = c.gameObject.GetComponent<LayoutElement>(); // Unity 객체라 ??를 쓰지 않는다
        if (element == null)
            element = c.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = height;
        element.flexibleWidth = flexibleWidth;
        return element;
    }

    private static TextMeshProUGUI Text(RectTransform parent, string name, string text, float size, Color color, bool bold = false,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        TextMeshProUGUI tmp = AddText(CreateRect(name, parent), text, size, color, bold, alignment);
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    #endregion

    #region 설정창

    private static GameObject CreateSettingsCanvas()
    {
        GameObject go = CreateCanvas(SettingsCanvasName, 100); // 본 화면(0)보다 위, 도움말(110)보다 아래
        var root = (RectTransform)go.transform;

        RectTransform panel = Place(CreateRect("Panel_Settings", root), SettingsPanelPos, SettingsPanelSize);
        AddRoundBox(panel, PanelColor).raycastTarget = true; // 뒤 화면으로 터치가 새지 않게
        AddVertical(panel, 40, new RectOffset(90, 90, 70, 60));

        TextMeshProUGUI title = Text(panel, "Title", "설정", TitleSize, TextColor, true);
        Size(title, height: 170);
        TextMeshProUGUI hint = Text(panel, "Hint", "ESC로 열고 닫기. 바꾼 값은 바로 적용되고 저장됩니다 (숫자는 입력 후 Enter)", SubSize, SubTextColor);
        Size(hint, height: 70);

        RectTransform columns = CreateRect("Columns", panel);
        AddHorizontal(columns, 100).childAlignment = TextAnchor.UpperLeft;
        Size(columns, height: 1400);
        RectTransform left = CreateRect("Column_Left", columns);
        AddVertical(left, RowSpacing);
        Size(left, ColumnWidthPx, 1400);
        RectTransform right = CreateRect("Column_Right", columns);
        AddVertical(right, RowSpacing);
        Size(right, ColumnWidthPx, 1400);

        // 화면 (gameSettingData.json)
        SectionHeader(left, "화면");
        var screenToggles = new List<SettingsPanelUI.BoolSetting>();
        foreach (var (field, label) in ScreenToggles)
            screenToggles.Add(new SettingsPanelUI.BoolSetting { fieldName = field, toggle = CreateToggleRow(left, field, label) });

        // 운영값 (emworld.json)
        var numbers = new List<EMWorldSettingsSection.NumberSetting>();
        var toggles = new List<EMWorldSettingsSection.ToggleSetting>();
        foreach (Item item in Items)
        {
            RectTransform column = item.column == Column.Left ? left : right;
            if (!string.IsNullOrEmpty(item.section))
                SectionHeader(column, item.section);

            if (item.unit == null)
            {
                toggles.Add(new EMWorldSettingsSection.ToggleSetting { fieldName = item.field, toggle = CreateToggleRow(column, item.field, item.label) });
            }
            else
            {
                numbers.Add(new EMWorldSettingsSection.NumberSetting
                {
                    fieldName = item.field,
                    input = CreateNumberRow(column, item),
                    min = item.min,
                    max = item.max,
                    decimals = item.decimals,
                });
            }
        }

        TextMeshProUGUI path = Text(panel, "FilePath", "설정 파일 폴더: (실행하면 표시)", SubSize, SubTextColor);
        Size(path, height: 70);

        var settings = go.AddComponent<SettingsPanelUI>();
        var so = new SerializedObject(settings);
        so.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
        so.FindProperty("pathText").objectReferenceValue = path;
        SerializedProperty bools = so.FindProperty("boolSettings");
        bools.arraySize = screenToggles.Count;
        for (int i = 0; i < screenToggles.Count; i++)
        {
            SerializedProperty e = bools.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("fieldName").stringValue = screenToggles[i].fieldName;
            e.FindPropertyRelative("toggle").objectReferenceValue = screenToggles[i].toggle;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        var section = go.AddComponent<EMWorldSettingsSection>();
        so = new SerializedObject(section);
        so.FindProperty("panel").objectReferenceValue = settings;
        SerializedProperty numberList = so.FindProperty("numbers");
        numberList.arraySize = numbers.Count;
        for (int i = 0; i < numbers.Count; i++)
        {
            SerializedProperty e = numberList.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("fieldName").stringValue = numbers[i].fieldName;
            e.FindPropertyRelative("input").objectReferenceValue = numbers[i].input;
            e.FindPropertyRelative("min").floatValue = numbers[i].min;
            e.FindPropertyRelative("max").floatValue = numbers[i].max;
            e.FindPropertyRelative("decimals").intValue = numbers[i].decimals;
        }
        SerializedProperty toggleList = so.FindProperty("toggles");
        toggleList.arraySize = toggles.Count;
        for (int i = 0; i < toggles.Count; i++)
        {
            SerializedProperty e = toggleList.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("fieldName").stringValue = toggles[i].fieldName;
            e.FindPropertyRelative("toggle").objectReferenceValue = toggles[i].toggle;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false); // 기본은 숨김
        return go;
    }

    private static void SectionHeader(RectTransform column, string text)
    {
        TextMeshProUGUI header = Text(column, $"Section_{text}", text, SectionSize, AccentColor, true, TextAlignmentOptions.BottomLeft);
        Size(header, height: 120);
    }

    // [■] 이름표   (줄 어디를 눌러도 켜고 끈다)
    private static Toggle CreateToggleRow(RectTransform column, string field, string label)
    {
        RectTransform row = CreateRect($"Toggle_{field}", column);
        AddHorizontal(row, 28);
        Size(row, height: RowHeight);

        RectTransform box = CreateRect("Box", row);
        Image boxImage = AddRoundBox(box, FieldColor);
        boxImage.raycastTarget = true;
        Size(box, 76, 76);
        RectTransform check = Stretch(CreateRect("Check", box), Vector2.zero, Vector2.one, 14);
        Image checkImage = AddRoundBox(check, AccentColor);

        TextMeshProUGUI text = Text(row, "Label", label, BodySize, TextColor);
        text.raycastTarget = true;
        Size(text, flexibleWidth: 1);

        var toggle = row.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = false;
        return toggle;
    }

    // 이름표 ......... [ 입력칸 ] 단위
    private static TMP_InputField CreateNumberRow(RectTransform column, Item item)
    {
        RectTransform row = CreateRect($"Number_{item.field}", column);
        AddHorizontal(row, 24);
        Size(row, height: RowHeight);

        TextMeshProUGUI label = Text(row, "Label", item.label, BodySize, TextColor);
        label.overflowMode = TextOverflowModes.Ellipsis;
        Size(label, flexibleWidth: 1);

        RectTransform inputRect = CreateRect("Input", row);
        Image bg = AddRoundBox(inputRect, FieldColor);
        bg.raycastTarget = true;
        Size(inputRect, 300, 86);

        RectTransform area = Stretch(CreateRect("TextArea", inputRect), Vector2.zero, Vector2.one, 0);
        area.offsetMin = new Vector2(24, 6);
        area.offsetMax = new Vector2(-24, -6);
        area.gameObject.AddComponent<RectMask2D>();
        TextMeshProUGUI placeholder = AddText(StretchFull(CreateRect("Placeholder", area)), "", BodySize, SubTextColor, false,
            TextAlignmentOptions.MidlineRight);
        TextMeshProUGUI value = AddText(StretchFull(CreateRect("Text", area)), "0", BodySize, TextColor, true,
            TextAlignmentOptions.MidlineRight);
        value.enableWordWrapping = false;

        var input = inputRect.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = bg;
        input.textViewport = area;
        input.textComponent = value;
        input.placeholder = placeholder;
        input.fontAsset = BoldFont;
        input.pointSize = BodySize;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = item.decimals > 0 ? TMP_InputField.ContentType.DecimalNumber : TMP_InputField.ContentType.IntegerNumber;
        input.caretWidth = 4;
        input.customCaretColor = true;
        input.caretColor = TextColor;
        input.selectionColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.5f);

        TextMeshProUGUI unit = Text(row, "Unit", item.unit, SubSize, SubTextColor);
        Size(unit, 150);
        return input;
    }

    #endregion

    #region 도움말

    private static GameObject CreateHelpRow()
    {
        RectTransform row = CreateRect("HelpRow", null);
        row.sizeDelta = new Vector2(HelpPanelWidth - 180, RowHeight);
        AddHorizontal(row, 40);
        Size(row, height: RowHeight);

        RectTransform keyBox = CreateRect("KeyBox", row);
        AddRoundBox(keyBox, KeyBoxColor);
        Size(keyBox, 220, 86);
        TextMeshProUGUI key = AddText(StretchFull(CreateRect("Key", keyBox)), "ESC", BodySize, DarkTextColor, true);
        key.enableWordWrapping = false;

        TextMeshProUGUI description = Text(row, "Description", "설정창 열기/닫기", BodySize, TextColor);
        Size(description, flexibleWidth: 1);
        TextMeshProUGUI scope = Text(row, "Scope", "모든 화면", SubSize, SubTextColor, false, TextAlignmentOptions.MidlineRight);
        Size(scope, 420);

        var view = row.gameObject.AddComponent<HelpRowView>();
        Wire(view, ("keyText", key), ("descriptionText", description), ("scopeText", scope));
        return row.gameObject;
    }

    private static GameObject CreateHelpCanvas(GameObject rowPrefab)
    {
        GameObject go = CreateCanvas(HelpCanvasName, 110); // 설정창(100)보다 위
        var root = (RectTransform)go.transform;

        RectTransform panel = Place(CreateRect("Panel_Help", root), HelpPanelPos, new Vector2(HelpPanelWidth, 600));
        AddRoundBox(panel, PanelColor);
        AddVertical(panel, 30, new RectOffset(90, 90, 70, 80));
        var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Size(Text(panel, "Title", "단축키 도움말", TitleSize, TextColor, true), height: 170);
        Size(Text(panel, "Hint", "F1로 열고 닫기. 운영자/개발자용 키입니다", SubSize, SubTextColor), height: 70);

        RectTransform header = CreateRect("Header", panel);
        AddHorizontal(header, 40);
        Size(header, height: 80);
        Size(Text(header, "Key", "키", SubSize, SubTextColor, false, TextAlignmentOptions.Midline), 220);
        Size(Text(header, "Description", "기능", SubSize, SubTextColor), flexibleWidth: 1);
        Size(Text(header, "Scope", "동작 화면", SubSize, SubTextColor, false, TextAlignmentOptions.MidlineRight), 420);

        RectTransform rows = CreateRect("Rows", panel);
        AddVertical(rows, RowSpacing);

        var help = go.AddComponent<HelpPanelUI>();
        Wire(help, ("panelRoot", panel.gameObject), ("rowPrefab", rowPrefab.GetComponent<HelpRowView>()), ("rowRoot", rows));
        panel.gameObject.SetActive(false); // 기본은 숨김
        return go;
    }

    #endregion

    #region 씬

    private static void ApplyToScene(GameObject settingsPrefab, GameObject helpPrefab)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == OldSettingsCanvasName || root.name == SettingsCanvasName || root.name == HelpCanvasName)
                Undo.DestroyObjectImmediate(root);
        }

        var settings = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, scene);
        var help = (GameObject)PrefabUtility.InstantiatePrefab(helpPrefab, scene);
        Undo.RegisterCreatedObjectUndo(settings, "Create Settings Canvas");
        Undo.RegisterCreatedObjectUndo(help, "Create Help Canvas");
        EditorSceneManager.MarkSceneDirty(scene);
    }

    #endregion
}

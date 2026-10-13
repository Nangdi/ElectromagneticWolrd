using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EMWorldUIKit;

// 전자기파로 보는 세상 UI 프리팹을 만들고 현재 씬에 배치한다.
// 메뉴: Tools > 전자기파로 보는 세상 > 1. 프리팹 생성 및 씬 적용
//
// 구조
//  Canvas_EMWorld.prefab (7680x2160)
//   ├ Panel_Spectrum.prefab        왼쪽 7칸 (6720x2160)
//   │  ├ StaticLayer               배경, 사인파, 레일 바탕 (Static_Spectrum.png)
//   │  ├ Bands/Band_xxx            파장 칸 7개 (RectMask2D). 카드의 파장별 뷰가 런타임에 들어간다
//   │  ├ CardLayer                 카드 본체(투명, 터치 담당)가 런타임에 들어간다
//   │  └ Labels/Label_Top_xxx …    파장 이름 (위쪽은 180° 회전)
//   └ InfoBoard                    맨 오른쪽 1칸 (960x2160)
//      └ Info (90° 회전, 2160x960)  Panel_InfoMenu + Panel_InfoDetail01~04. 테이블 오른쪽 끝의 한 사람용
//
// 이미 있는 프리팹은 덮어쓰지 않는다. 직접 수정한 프리팹을 지키기 위해서다.
// 프리팹을 처음 상태로 되돌리고 싶으면 해당 .prefab 파일을 지운 뒤 다시 실행한다.
public static class EMWorldPrefabBuilder
{
    private const string CanvasName = "Canvas_EMWorld";
    private const string SystemName = "EMWorldSystem";

    private static GameObject cardPrefab;
    private static GameObject bandViewPrefab;
    private static GameObject thumbPrefab;
    private static GameObject backPrefab;

    [MenuItem(MenuRoot + "1. 프리팹 생성 및 씬 적용", priority = 1)]
    public static void BuildAll()
    {
        EMWorldSpriteExporter.Export(overwrite: false);
        EnsureFolder(PartPrefabDir);
        EnsureFolder(PanelPrefabDir);

        // 부품
        thumbPrefab = GetOrCreate(PartPrefabPath("NoImageThumb"), CreateNoImageThumb);
        backPrefab = GetOrCreate(PartPrefabPath("CardBack"), CreateCardBack);
        bandViewPrefab = GetOrCreate(PartPrefabPath("CardBandView"), CreateCardBandView);
        cardPrefab = GetOrCreate(PartPrefabPath("Card"), CreateCard);

        // 패널
        GameObject spectrum = GetOrCreate(PanelPrefabPath("Spectrum"), CreateSpectrumPanel);
        GameObject infoMenu = GetOrCreate(PanelPrefabPath("InfoMenu"), CreateInfoMenuPanel);
        var infoDetails = new List<GameObject>();
        for (int i = 0; i < InfoDetailPanels.Length; i++)
        {
            int index = i;
            infoDetails.Add(GetOrCreate(PanelPrefabPath(InfoDetailPanels[i]), () => CreateInfoDetailPanel(index)));
        }

        GameObject canvasPrefab = GetOrCreate(CanvasPrefabPath, () => CreateCanvas(spectrum, infoMenu, infoDetails));

        ApplyToScene(canvasPrefab);
        EMWorldSpriteExporter.ExportGuides();
        AssetDatabase.SaveAssets();
        Debug.Log("전자기파로 보는 세상 프리팹 적용 완료. 씬을 저장(Ctrl+S)하세요.");
    }

    // overwrite가 true면 같은 경로에 덮어써서 다시 만든다 (GUID 유지).
    private static GameObject GetOrCreate(string path, Func<GameObject> build, bool overwrite = false)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null && !overwrite) return existing;

        GameObject temp = build();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        UnityEngine.Object.DestroyImmediate(temp);
        Debug.Log($"프리팹 {(existing != null ? "다시 생성" : "생성")}: {path}");
        return prefab;
    }

    // 정보검색 칸 정적 이미지, 메뉴 버튼, 패널 프리팹, 캔버스를 다시 만든다 (레이아웃을 바꿨을 때).
    public static void RebuildInfoPrefabs()
    {
        EMWorldSpriteExporter.ExportInfoStatics();
        var buttons = new List<string>();
        for (int i = 0; i < EMWorldStaticLayouts.InfoTitles.Length; i++)
            buttons.Add(EMWorldStaticLayouts.InfoMenuButtonSprite(i));
        EMWorldSpriteExporter.ExportTextParts(buttons.ToArray());

        GameObject infoMenu = GetOrCreate(PanelPrefabPath("InfoMenu"), CreateInfoMenuPanel, true);
        var infoDetails = new List<GameObject>();
        for (int i = 0; i < InfoDetailPanels.Length; i++)
        {
            int index = i;
            infoDetails.Add(GetOrCreate(PanelPrefabPath(InfoDetailPanels[i]), () => CreateInfoDetailPanel(index), true));
        }
        GameObject spectrum = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath("Spectrum"));
        GameObject canvasPrefab = GetOrCreate(CanvasPrefabPath, () => CreateCanvas(spectrum, infoMenu, infoDetails), true);

        ApplyToScene(canvasPrefab);
        EMWorldSpriteExporter.ExportGuides();
        AssetDatabase.SaveAssets();
        Debug.Log("정보검색 칸 다시 만들기 완료");
    }

    [MenuItem(MenuRoot + "6. 카드 프리팹 다시 만들기 (덮어쓰기)", priority = 6)]
    private static void RebuildCardPrefabsMenu()
    {
        bool ok = EditorUtility.DisplayDialog("전자기파로 보는 세상",
            "Parts/Card, CardBandView, CardBack, NoImageThumb 프리팹을 코드 기준으로 다시 만듭니다.\n직접 고친 내용은 사라집니다. 계속할까요?",
            "다시 만들기", "취소");
        if (ok) RebuildCardPrefabs();
    }

    // 카드 부품 프리팹을 덮어써서 다시 만들고, Panel_Spectrum의 연결을 다시 잇는다.
    public static void RebuildCardPrefabs()
    {
        EMWorldSpriteExporter.Export(overwrite: false);
        thumbPrefab = GetOrCreate(PartPrefabPath("NoImageThumb"), CreateNoImageThumb, true);
        backPrefab = GetOrCreate(PartPrefabPath("CardBack"), CreateCardBack, true);
        bandViewPrefab = GetOrCreate(PartPrefabPath("CardBandView"), CreateCardBandView, true);
        cardPrefab = GetOrCreate(PartPrefabPath("Card"), CreateCard, true);

        string panelPath = PanelPrefabPath("Spectrum");
        GameObject contents = PrefabUtility.LoadPrefabContents(panelPath);
        try
        {
            var baker = contents.GetComponent<CardBackBaker>();
            if (baker == null) baker = contents.AddComponent<CardBackBaker>();
            Wire(baker, ("backPrefab", backPrefab.GetComponent<CardBackView>()));
            Wire(contents.GetComponent<SpectrumBoard>(),
                ("cardPrefab", cardPrefab.GetComponent<CardController>()),
                ("bandViewPrefab", bandViewPrefab.GetComponent<CardBandView>()),
                ("backBaker", baker));
            PrefabUtility.SaveAsPrefabAsset(contents, panelPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("카드 프리팹 다시 만들기 완료");
    }

    private static Sprite Part(string name) => LoadSprite(PartSpritePath(name));

    // 글자가 구워진 스프라이트 버튼 (스프라이트 크기 = 배치 크기)
    private static Button CreateSpriteButton(RectTransform parent, string name, string sprite, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Place(CreateRect(name, parent), pos, size);
        Image image = AddSprite(rt, Part(sprite));
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    #region 카드 데이터

    // 카드 이미지와 문구는 실행할 때 StreamingAssets/Cards에서 읽으므로 따로 갱신할 필요가 없다. 폴더만 열어 준다.
    [MenuItem(MenuRoot + "5. 카드 폴더 열기", priority = 5)]
    private static void OpenCardsFolder()
    {
        EnsureFolder(CardsDir);
        EditorUtility.RevealInFinder(Path.GetFullPath(CardsDir) + Path.DirectorySeparatorChar);
    }

    #endregion

    #region 부품 프리팹

    // 이미지 없음 안내 안의 썸네일: Picture / Label
    private static GameObject CreateNoImageThumb()
    {
        RectTransform root = CreateRect("NoImageThumb", null);
        root.sizeDelta = new Vector2(104, 150);

        Image picture = AddImage(Place(CreateRect("Picture", root), new Vector2(0, 22), new Vector2(104, 104)), Color.white);
        picture.preserveAspect = true;
        TMP_Text label = CreateText(root, "Label", "적외선", new Vector2(0, -52), new Vector2(124, 40), 24, SubTextColor);
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;

        var view = root.gameObject.AddComponent<NoImageThumbView>();
        Wire(view, ("picture", picture), ("label", label));
        return root.gameObject;
    }

    // 카드 뒷면(정보면) 원본. 실행 시 CardBackBaker가 카드마다 텍스처 한 장으로 구워 파장별 뷰에 넣는다.
    private static GameObject CreateCardBack()
    {
        RectTransform root = Place(CreateRect("CardBack", null), Vector2.zero, new Vector2(CardContentSize, CardContentSize));
        AddImage(StretchFull(CreateRect("Background", root)), CardColor);

        TMP_Text category = CreateText(root, "Category", "천체", new Vector2(0, 196), new Vector2(440, 44), 28, AccentColor, true);
        TMP_Text title = CreateText(root, "Title", "게 성운", new Vector2(0, 140), new Vector2(440, 70), 44, TextColor, true);
        title.enableAutoSizing = true;
        title.fontSizeMin = 30;
        title.fontSizeMax = 44;
        AddImage(Place(CreateRect("Divider", root), new Vector2(0, 96), new Vector2(440, 2)), SoftBoxColor);

        // 오른쪽 아래는 뒤집기 버튼 자리라 비워 둔다
        TMP_Text description = CreateText(root, "Description",
            "1054년에 폭발한 초신성의 잔해입니다. 중심의 펄서가 라디오파부터 감마선까지 강한 전자기파를 냅니다.",
            new Vector2(-14, -40), new Vector2(412, 240), 30, TextColor, false, TextAlignmentOptions.TopLeft);
        description.enableAutoSizing = true;
        description.fontSizeMin = 22;
        description.fontSizeMax = 30;

        var view = root.gameObject.AddComponent<CardBackView>();
        Wire(view, ("categoryText", category), ("titleText", title), ("descriptionText", description));
        return root.gameObject;
    }

    // 카드의 파장별 뷰: Front(사진 또는 이미지 없음 안내) / Back(구운 뒷면) / Frame(속이 빈 테두리) / FlipIcon
    private static GameObject CreateCardBandView()
    {
        RectTransform root = CreateRect("CardBandView", null);
        root.sizeDelta = new Vector2(CardSize, CardSize);
        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform front = StretchFull(CreateRect("Front", root));
        Image picture = AddImage(Stretch(CreateRect("Picture", front), Vector2.zero, Vector2.one, CardFrameInset), Color.white);
        picture.preserveAspect = true;

        RectTransform noImage = Stretch(CreateRect("NoImage", front), Vector2.zero, Vector2.one, CardFrameInset);
        AddImage(StretchFull(CreateRect("Background", noImage)), NoImageBoxColor);
        AddSprite(Place(CreateRect("Message", noImage), new Vector2(0, 130), new Vector2(460, 130)), Part("NoImage_Message"))
            .preserveAspect = true;
        AddSprite(Place(CreateRect("Sub", noImage), new Vector2(0, 40), new Vector2(460, 44)), Part("NoImage_Sub"))
            .preserveAspect = true;

        RectTransform thumbRoot = Place(CreateRect("ThumbRoot", noImage), new Vector2(0, -110), new Vector2(470, 170));
        var layout = thumbRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var noImageView = noImage.gameObject.AddComponent<NoImageView>();
        Wire(noImageView, ("thumbPrefab", thumbPrefab.GetComponent<NoImageThumbView>()), ("thumbRoot", thumbRoot));
        noImage.gameObject.SetActive(false);

        // 레일 위에서 덮이는 원본 이미지
        RectTransform original = Stretch(CreateRect("Original", root), Vector2.zero, Vector2.one, CardFrameInset);
        var originalGroup = original.gameObject.AddComponent<CanvasGroup>();
        originalGroup.blocksRaycasts = false;
        Image originalPicture = AddImage(original, Color.white);
        originalPicture.preserveAspect = true;

        RectTransform backRect = Stretch(CreateRect("Back", root), Vector2.zero, Vector2.one, CardFrameInset);
        var back = backRect.gameObject.AddComponent<RawImage>();
        back.raycastTarget = false;
        backRect.gameObject.SetActive(false);

        // 속이 빈 테두리. 사진 위에 그려서 사진 모서리를 덮는다.
        Image frame = AddSprite(StretchFull(CreateRect("Frame", root)), Part("Card_Frame"), CardFrameColor);
        frame.fillCenter = false;
        frame.pixelsPerUnitMultiplier = CardFrameSpriteScale;

        RectTransform flipIcon = Place(CreateRect("FlipIcon", root), FlipButtonPos, FlipButtonSize);
        var flipIconGroup = flipIcon.gameObject.AddComponent<CanvasGroup>();
        flipIconGroup.alpha = 0f;
        flipIconGroup.blocksRaycasts = false;
        RectTransform infoIcon = StretchFull(CreateRect("Icon_Info", flipIcon));
        AddSprite(infoIcon, Part("Button_CardInfo")).preserveAspect = true;
        RectTransform imageIcon = StretchFull(CreateRect("Icon_Image", flipIcon));
        AddSprite(imageIcon, Part("Button_CardImage")).preserveAspect = true;
        imageIcon.gameObject.SetActive(false);

        var view = root.gameObject.AddComponent<CardBandView>();
        Wire(view,
            ("group", group),
            ("front", front.gameObject),
            ("picture", picture),
            ("noImage", noImageView),
            ("original", originalGroup),
            ("originalPicture", originalPicture),
            ("back", back),
            ("flipIcon", flipIconGroup),
            ("infoIcon", infoIcon.gameObject),
            ("imageIcon", imageIcon.gameObject));
        return root.gameObject;
    }

    // 카드 본체: 투명 Image(터치 영역) + 오른쪽 아래 투명 뒤집기 버튼 + CardController. 그림은 CardBandView들이 그린다.
    private static GameObject CreateCard()
    {
        RectTransform root = Place(CreateRect("Card", null), Vector2.zero, new Vector2(CardSize, CardSize));
        Image hit = AddImage(root, new Color(1f, 1f, 1f, 0f));
        hit.raycastTarget = true;

        RectTransform button = Place(CreateRect("FlipButton", root), FlipButtonPos, FlipButtonSize);
        var buttonGroup = button.gameObject.AddComponent<CanvasGroup>();
        buttonGroup.blocksRaycasts = false;
        buttonGroup.interactable = false;
        Image buttonHit = AddImage(button, new Color(1f, 1f, 1f, 0f));
        buttonHit.raycastTarget = true;
        var flipButton = button.gameObject.AddComponent<Button>();
        flipButton.targetGraphic = buttonHit;
        flipButton.transition = Selectable.Transition.None;

        var controller = root.gameObject.AddComponent<CardController>();
        Wire(controller, ("flipButton", flipButton), ("flipButtonGroup", buttonGroup));
        return root.gameObject;
    }

    #endregion

    #region 패널 프리팹

    private static Image AddStaticLayer(RectTransform panel, string name)
    {
        Image staticLayer = AddSprite(StretchFull(CreateRect(StaticLayerName, panel)), LoadSprite(StaticSpritePath(name)));
        staticLayer.type = Image.Type.Simple;
        return staticLayer;
    }

    private static GameObject CreateSpectrumPanel()
    {
        RectTransform panel = Place(CreateRect("Panel_Spectrum", null), SpectrumCenter, SpectrumSize);
        AddStaticLayer(panel, "Spectrum");

        // 파장 칸: 칸 밖으로 나간 카드 뷰를 잘라낸다
        RectTransform bands = StretchFull(CreateRect("Bands", panel));
        var bandRoots = new UnityEngine.Object[BandInfo.Count];
        for (int i = 0; i < BandInfo.Count; i++)
        {
            RectTransform band = Place(CreateRect($"Band_{(BandType)i}", bands), new Vector2(BandCenterX(i), 0),
                new Vector2(ColumnWidth, ScreenHeight));
            band.gameObject.AddComponent<RectMask2D>();
            bandRoots[i] = band;
        }

        RectTransform cardLayer = StretchFull(CreateRect("CardLayer", panel));

        // 파장 이름: 아래쪽 관람객용(0°)과 위쪽 관람객용(180°)
        RectTransform labels = StretchFull(CreateRect("Labels", panel));
        for (int i = 0; i < BandInfo.Count; i++)
        {
            Sprite sprite = Part(EMWorldStaticLayouts.BandLabelSprite((BandType)i));
            RectTransform bottom = Place(CreateRect($"Label_Bottom_{(BandType)i}", labels),
                new Vector2(BandCenterX(i), -BandLabelY), BandLabelSize);
            AddSprite(bottom, sprite).preserveAspect = true;

            RectTransform top = Place(CreateRect($"Label_Top_{(BandType)i}", labels),
                new Vector2(BandCenterX(i), BandLabelY), BandLabelSize);
            top.localRotation = Quaternion.Euler(0, 0, 180);
            AddSprite(top, sprite).preserveAspect = true;
        }

        var rail = panel.gameObject.AddComponent<RailController>();
        var baker = panel.gameObject.AddComponent<CardBackBaker>();
        Wire(baker, ("backPrefab", backPrefab.GetComponent<CardBackView>()));
        var board = panel.gameObject.AddComponent<SpectrumBoard>();
        Wire(board,
            ("cardPrefab", cardPrefab.GetComponent<CardController>()),
            ("bandViewPrefab", bandViewPrefab.GetComponent<CardBandView>()),
            ("rail", rail),
            ("backBaker", baker),
            ("cardLayer", cardLayer));
        WireList(board, "bandRoots", bandRoots);
        return panel.gameObject;
    }

    private static GameObject CreateInfoMenuPanel()
    {
        RectTransform panel = StretchFull(CreateRect("Panel_InfoMenu", null));
        AddStaticLayer(panel, "InfoMenu");

        var buttons = new UnityEngine.Object[EMWorldStaticLayouts.InfoTitles.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i] = CreateSpriteButton(panel, $"Menu_{i + 1:00}", EMWorldStaticLayouts.InfoMenuButtonSprite(i),
                EMWorldStaticLayouts.InfoMenuButtonPos[i], EMWorldStaticLayouts.InfoMenuButtonSize);
        }

        var view = panel.gameObject.AddComponent<InfoMenuPanel>();
        WireList(view, "menuButtons", buttons);
        return panel.gameObject;
    }

    private static GameObject CreateInfoDetailPanel(int index)
    {
        string name = InfoDetailPanels[index];
        RectTransform panel = StretchFull(CreateRect($"Panel_{name}", null));
        AddStaticLayer(panel, name);

        Button back = CreateSpriteButton(panel, "BackButton", "Button_Back",
            EMWorldStaticLayouts.BackButtonPos, EMWorldStaticLayouts.BackButtonSize);

        var view = panel.gameObject.AddComponent<InfoDetailPanel>();
        Wire(view, ("backButton", back));
        return panel.gameObject;
    }

    #endregion

    #region 캔버스와 씬

    private static GameObject CreateCanvas(GameObject spectrum, GameObject infoMenu, List<GameObject> infoDetails)
    {
        var go = new GameObject(CanvasName, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0; // 설정창(SettingsCanvas, 100)보다 아래

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        PrefabUtility.InstantiatePrefab(spectrum, go.transform);

        RectTransform infoBoard = Place(CreateRect("InfoBoard", go.transform), InfoColumnCenter, new Vector2(ColumnWidth, ScreenHeight));
        CreateInfo(infoBoard, infoMenu, infoDetails);
        return go;
    }

    // 정보검색 칸: 960x2160 칸을 90° 돌려 2160x960 가로 화면 하나로 쓴다.
    private static void CreateInfo(RectTransform parent, GameObject infoMenu, List<GameObject> infoDetails)
    {
        RectTransform half = Place(CreateRect("Info", parent), Vector2.zero, InfoPanelSize);
        half.localRotation = Quaternion.Euler(0, 0, InfoPanelAngle);

        var menu = (GameObject)PrefabUtility.InstantiatePrefab(infoMenu, half);
        var details = new UnityEngine.Object[infoDetails.Count];
        for (int i = 0; i < infoDetails.Count; i++)
        {
            var detail = (GameObject)PrefabUtility.InstantiatePrefab(infoDetails[i], half);
            detail.SetActive(false); // 편집하기 쉽도록 메뉴만 켜 둔다
            details[i] = detail.GetComponent<InfoDetailPanel>();
        }

        var controller = half.gameObject.AddComponent<InfoPanelController>();
        Wire(controller, ("menuPanel", menu.GetComponent<InfoMenuPanel>()));
        WireList(controller, "detailPanels", details);
    }

    private static void ApplyToScene(GameObject canvasPrefab)
    {
        Undo.SetCurrentGroupName("Apply EMWorld UI");
        int undoGroup = Undo.GetCurrentGroup();

        GameObject canvas = FindRoot(CanvasName);

        // 프리팹이 아닌 캔버스는 프리팹으로 교체한다
        if (canvas != null && PrefabUtility.GetCorrespondingObjectFromSource(canvas) != canvasPrefab)
        {
            Undo.DestroyObjectImmediate(canvas);
            canvas = null;
        }

        if (canvas == null)
        {
            canvas = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab, SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(canvas, "Create EMWorld Canvas");
        }

        GameObject system = FindRoot(SystemName);
        if (system == null)
        {
            system = new GameObject(SystemName);
            Undo.RegisterCreatedObjectUndo(system, "Create EMWorld System");
        }

        var idle = system.GetComponent<IdleResetController>();
        if (idle == null) idle = Undo.AddComponent<IdleResetController>(system);
        Wire(idle, ("board", canvas.GetComponentInChildren<SpectrumBoard>(true)));
        WireList(idle, "infoPanels", canvas.GetComponentsInChildren<InfoPanelController>(true).Cast<UnityEngine.Object>().ToList());

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Selection.activeGameObject = canvas;
    }

    private static GameObject FindRoot(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }

    #endregion
}

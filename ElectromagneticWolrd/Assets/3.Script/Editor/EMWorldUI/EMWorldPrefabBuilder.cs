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
//  Canvas_EMWorld.prefab (7680x2160, Expand: 비율이 달라도 잘리지 않고 남는 곳은 카메라 배경색)
//   └ Frame                           기준 해상도 고정 틀 (7680x2160). 패널은 모두 이 안에 둔다
//      ├ Panel_Spectrum.prefab        왼쪽 7칸 (6720x2160)
//      │  ├ StaticLayer               배경, 사인파, 레일 바탕, 파장 이름 라벨 (Static_Spectrum.png)
//      │  ├ Bands/Band_xxx            파장 칸 7개 (RectMask2D). 카드의 파장별 뷰가 런타임에 들어간다
//      │  ├ CardLayer                 카드 본체(투명, 터치 담당)가 런타임에 들어간다
//      │  └ RailEndShadow             레일 오른쪽 끝 그림자 (카드 위)
//      └ InfoBoard                    맨 오른쪽 1칸 (960x2160). StaticLayer = Static_InfoBoard.png
//         └ Info (90° 회전, 2160x960, 0.96배)  Panel_InfoMenu + Panel_InfoDetail01~04. 테이블 오른쪽 끝의 한 사람용
//
// 이미 있는 프리팹은 덮어쓰지 않는다. 직접 수정한 프리팹을 지키기 위해서다.
// 프리팹을 처음 상태로 되돌리고 싶으면 해당 .prefab 파일을 지운 뒤 다시 실행한다.
public static class EMWorldPrefabBuilder
{
    private const string CanvasName = "Canvas_EMWorld";
    private const string SystemName = "EMWorldSystem";

    private static GameObject cardPrefab;
    private static GameObject bandViewPrefab;
    private static GameObject underlayPrefab;
    private static GameObject chromePrefab;
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
        underlayPrefab = GetOrCreate(PartPrefabPath("CardUnderlay"), CreateCardUnderlay);
        chromePrefab = GetOrCreate(PartPrefabPath("CardChrome"), CreateCardChrome);
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

    [MenuItem(MenuRoot + "7. 전체 프리팹 다시 만들기 (덮어쓰기)", priority = 7)]
    private static void RebuildAllMenu()
    {
        bool ok = EditorUtility.DisplayDialog("전자기파로 보는 세상",
            "카드 부품, 패널, 캔버스 프리팹을 모두 코드 기준으로 다시 만들고 씬에 적용합니다.\n프리팹을 직접 고친 내용은 사라집니다 (스프라이트 PNG는 그대로). 계속할까요?",
            "다시 만들기", "취소");
        if (ok) RebuildAll();
    }

    // 디자인이 바뀌어 배치가 달라졌을 때: 모든 프리팹을 같은 경로에 덮어써서(GUID 유지) 다시 만든다.
    // 스프라이트 PNG는 없는 것만 임시로 만들고, 디자이너가 넣은 PNG는 건드리지 않는다.
    public static void RebuildAll()
    {
        EMWorldSpriteExporter.Export(overwrite: false);
        EnsureFolder(PartPrefabDir);
        EnsureFolder(PanelPrefabDir);

        thumbPrefab = GetOrCreate(PartPrefabPath("NoImageThumb"), CreateNoImageThumb, true);
        backPrefab = GetOrCreate(PartPrefabPath("CardBack"), CreateCardBack, true);
        bandViewPrefab = GetOrCreate(PartPrefabPath("CardBandView"), CreateCardBandView, true);
        underlayPrefab = GetOrCreate(PartPrefabPath("CardUnderlay"), CreateCardUnderlay, true);
        chromePrefab = GetOrCreate(PartPrefabPath("CardChrome"), CreateCardChrome, true);
        cardPrefab = GetOrCreate(PartPrefabPath("Card"), CreateCard, true);

        GameObject spectrum = GetOrCreate(PanelPrefabPath("Spectrum"), CreateSpectrumPanel, true);
        GameObject infoMenu = GetOrCreate(PanelPrefabPath("InfoMenu"), CreateInfoMenuPanel, true);
        var infoDetails = new List<GameObject>();
        for (int i = 0; i < InfoDetailPanels.Length; i++)
        {
            int index = i;
            infoDetails.Add(GetOrCreate(PanelPrefabPath(InfoDetailPanels[i]), () => CreateInfoDetailPanel(index), true));
        }
        GameObject canvasPrefab = GetOrCreate(CanvasPrefabPath, () => CreateCanvas(spectrum, infoMenu, infoDetails), true);

        ApplyToScene(canvasPrefab);
        EMWorldSpriteExporter.ExportGuides();
        AssetDatabase.SaveAssets();
        Debug.Log("전체 프리팹 다시 만들기 완료. 씬을 저장(Ctrl+S)하세요.");
    }

    // 정보검색 칸 패널 프리팹과 캔버스를 다시 만든다 (레이아웃을 바꿨을 때). 정적 PNG와 버튼 PNG는 디자인 파일이라 덮어쓰지 않는다.
    public static void RebuildInfoPrefabs()
    {
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
            "Parts/Card, CardBandView, CardUnderlay, CardChrome, CardBack, NoImageThumb 프리팹을 코드 기준으로 다시 만듭니다.\n직접 고친 내용은 사라집니다. 계속할까요?",
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
        underlayPrefab = GetOrCreate(PartPrefabPath("CardUnderlay"), CreateCardUnderlay, true);
        chromePrefab = GetOrCreate(PartPrefabPath("CardChrome"), CreateCardChrome, true);
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
                ("underlayPrefab", underlayPrefab.GetComponent<CardUnderlayView>()),
                ("chromePrefab", chromePrefab.GetComponent<CardChromeView>()),
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

    // 둥근 사진 영역: 마스크(Card_PictureMask) 안에서 사진이 비율을 유지한 채 영역을 가득 채운다.
    // radiusScale은 마스크 모서리 둥글기 배율 (썸네일처럼 작은 영역은 작게).
    private static RectTransform AddPictureMask(RectTransform rt, float radiusScale = 1f)
    {
        Image mask = AddSprite(rt, Part("Card_PictureMask"));
        mask.pixelsPerUnitMultiplier = 1f / radiusScale;
        rt.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        return rt;
    }

    private static (Image image, AspectRatioFitter fitter) CreateCoverPicture(RectTransform parent, string name)
    {
        RectTransform rt = StretchFull(CreateRect(name, parent));
        Image image = AddImage(rt, Color.white);
        var fitter = rt.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 1f;
        return (image, fitter);
    }

    private static CanvasGroup AddGroup(RectTransform rt, float alpha = 1f)
    {
        var group = rt.gameObject.AddComponent<CanvasGroup>();
        group.alpha = alpha;
        group.blocksRaycasts = false;
        group.interactable = false;
        return group;
    }

    // 이미지 없음 안내 안의 썸네일: PictureArea(둥근 마스크, 위쪽) / Label(아래쪽). 크기는 NoImageView가 정한다.
    private static GameObject CreateNoImageThumb()
    {
        RectTransform root = CreateRect("NoImageThumb", null);
        root.sizeDelta = new Vector2(124, 138 + 36);

        RectTransform area = CreateRect("PictureArea", root);
        area.anchorMin = new Vector2(0, 1);
        area.anchorMax = Vector2.one;
        area.pivot = new Vector2(0.5f, 1f);
        area.anchoredPosition = Vector2.zero;
        area.sizeDelta = new Vector2(0, 138);
        AddPictureMask(area, 0.3f);
        var (picture, fitter) = CreateCoverPicture(area, "Picture");

        RectTransform labelRect = CreateRect("Label", root);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = new Vector2(1, 0);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(20, 30);
        TMP_Text label = AddText(labelRect, "적외선", 20, TextColor);
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;

        var view = root.gameObject.AddComponent<NoImageThumbView>();
        Wire(view, ("pictureArea", area), ("picture", picture), ("pictureFitter", fitter), ("label", label));
        return root.gameObject;
    }

    // 카드 뒷면(정보면) 원본. 실행 시 CardBackBaker가 카드마다 텍스처 한 장으로 구워 파장별 뷰에 넣는다.
    // 사진 영역과 같은 크기라 같은 둥근 마스크로 잘린다. 왼쪽 아래는 제목 탭, 오른쪽 아래는 뒤집기 버튼 자리라 비워 둔다.
    private static GameObject CreateCardBack()
    {
        RectTransform root = Place(CreateRect("CardBack", null), Vector2.zero, CardSize);
        AddImage(StretchFull(CreateRect("Background", root)), CardColor);

        TMP_Text category = CreateText(root, "Category", "천체", new Vector2(0, 220), new Vector2(420, 44), 28, AccentColor, true);
        TMP_Text title = CreateText(root, "Title", "게 성운", new Vector2(0, 165), new Vector2(420, 70), 44, TextColor, true);
        title.enableAutoSizing = true;
        title.fontSizeMin = 30;
        title.fontSizeMax = 44;
        AddImage(Place(CreateRect("Divider", root), new Vector2(0, 120), new Vector2(420, 2)), SoftBoxColor);

        TMP_Text description = CreateText(root, "Description",
            "1054년에 폭발한 초신성의 잔해입니다. 중심의 펄서가 라디오파부터 감마선까지 강한 전자기파를 냅니다.",
            new Vector2(0, -30), new Vector2(420, 270), 30, TextColor, false, TextAlignmentOptions.TopLeft);
        description.enableAutoSizing = true;
        description.fontSizeMin = 22;
        description.fontSizeMax = 30;

        var view = root.gameObject.AddComponent<CardBackView>();
        Wire(view, ("categoryText", category), ("titleText", title), ("descriptionText", description));
        return root.gameObject;
    }

    // 카드의 파장별 뷰 (크기 = 사진 영역 500x558): PictureMask(앞면 사진·이미지 없음 안내, 원본, 뒷면)
    private static GameObject CreateCardBandView()
    {
        RectTransform root = CreateRect("CardBandView", null);
        root.sizeDelta = CardSize;
        CanvasGroup group = AddGroup(root);

        RectTransform mask = AddPictureMask(StretchFull(CreateRect("PictureMask", root)));

        RectTransform front = StretchFull(CreateRect("Front", mask));
        var (picture, pictureFitter) = CreateCoverPicture(front, "Picture");

        RectTransform noImage = StretchFull(CreateRect("NoImage", front));
        AddImage(StretchFull(CreateRect("Background", noImage)), NoImageBoxColor);
        AddSprite(Place(CreateRect("Message", noImage), new Vector2(1.5f, 142.5f), new Vector2(347, 193)), Part("NoImage_Message"))
            .preserveAspect = true;

        // 썸네일 줄: 위쪽 끝을 사진 영역 중앙에서 8px 위에 맞춘다 (디자인 y 1671~1677)
        RectTransform thumbRoot = CreateRect("ThumbRoot", noImage);
        thumbRoot.anchorMin = thumbRoot.anchorMax = new Vector2(0.5f, 0.5f);
        thumbRoot.pivot = new Vector2(0.5f, 1f);
        thumbRoot.anchoredPosition = new Vector2(0, 8);
        thumbRoot.sizeDelta = new Vector2(460, 180);
        var layout = thumbRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 22;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var noImageView = noImage.gameObject.AddComponent<NoImageView>();
        Wire(noImageView, ("thumbPrefab", thumbPrefab.GetComponent<NoImageThumbView>()), ("thumbRoot", thumbRoot),
            ("layout", layout));
        noImage.gameObject.SetActive(false);

        // 레일 위에서 덮이는 원본 이미지
        RectTransform original = StretchFull(CreateRect("Original", mask));
        CanvasGroup originalGroup = AddGroup(original);
        var (originalPicture, originalFitter) = CreateCoverPicture(original, "Picture");

        RectTransform backRect = StretchFull(CreateRect("Back", mask));
        var back = backRect.gameObject.AddComponent<RawImage>();
        back.raycastTarget = false;
        backRect.gameObject.SetActive(false);

        var view = root.gameObject.AddComponent<CardBandView>();
        Wire(view,
            ("group", group),
            ("front", front.gameObject),
            ("picture", picture),
            ("pictureFitter", pictureFitter),
            ("noImage", noImageView),
            ("original", originalGroup),
            ("originalPicture", originalPicture),
            ("originalFitter", originalFitter),
            ("back", back));
        return root.gameObject;
    }

    // 사진 아래 장식 (카드마다 하나, 그 카드의 파장별 뷰 7개 아래에 그림): RailShadow(레일 위에서만) / PopupFrame(꺼냈을 때만)
    private static GameObject CreateCardUnderlay()
    {
        RectTransform root = CreateRect("CardUnderlay", null);
        root.sizeDelta = CardSize;
        CanvasGroup group = AddGroup(root);

        RectTransform railShadow = Place(CreateRect("RailShadow", root), RailShadowPos, RailShadowSize);
        AddSprite(railShadow, Part("Card_RailShadow"));
        CanvasGroup railShadowGroup = AddGroup(railShadow);

        RectTransform popup = Place(CreateRect("PopupFrame", root), CardPopupPos, CardPopupSize);
        AddSprite(popup, Part("Card_Popup"));
        CanvasGroup popupGroup = AddGroup(popup, 0f);

        var view = root.gameObject.AddComponent<CardUnderlayView>();
        Wire(view, ("group", group), ("popupFrame", popupGroup), ("railShadow", railShadowGroup));
        return root.gameObject;
    }

    // 사진 위 장식 (카드마다 하나, 그 카드의 파장별 뷰 7개 위에 그림): TitleTab / FlipIcon
    private static GameObject CreateCardChrome()
    {
        RectTransform root = CreateRect("CardChrome", null);
        root.sizeDelta = CardSize;
        CanvasGroup group = AddGroup(root);

        // 제목 탭: 왼쪽 끝을 고정하고 글자 길이만큼 오른쪽으로 늘어난다 (Card_TitleTab은 가로 9-slice)
        RectTransform tab = CreateRect("TitleTab", root);
        tab.anchorMin = tab.anchorMax = new Vector2(0.5f, 0.5f);
        tab.pivot = new Vector2(0f, 0.5f);
        tab.anchoredPosition = new Vector2(TitleTabLeft, TitleTabY);
        tab.sizeDelta = TitleTabMinSize;
        AddSprite(tab, Part("Card_TitleTab"));
        CanvasGroup tabGroup = AddGroup(tab, 0f);
        var tabLayout = tab.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.padding = new RectOffset(52, 60, 0, 6); // 아래 6px는 탭 그림자
        tabLayout.childAlignment = TextAnchor.MiddleLeft;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = false;
        tabLayout.childForceExpandHeight = true;
        var tabFitter = tab.gameObject.AddComponent<ContentSizeFitter>();
        tabFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        tab.gameObject.AddComponent<LayoutElement>().minWidth = TitleTabMinSize.x;
        TMP_Text title = AddText(CreateRect("Title", tab), "스마트폰", 30, DarkTextColor, false, TextAlignmentOptions.MidlineLeft);
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Overflow;

        RectTransform flipIcon = Place(CreateRect("FlipIcon", root), FlipButtonPos, FlipButtonSize);
        CanvasGroup flipIconGroup = AddGroup(flipIcon, 0f);
        RectTransform infoIcon = StretchFull(CreateRect("Icon_Info", flipIcon));
        AddSprite(infoIcon, Part("Button_CardInfo")).preserveAspect = true;
        RectTransform imageIcon = StretchFull(CreateRect("Icon_Image", flipIcon));
        AddSprite(imageIcon, Part("Button_CardImage")).preserveAspect = true;
        imageIcon.gameObject.SetActive(false);

        var view = root.gameObject.AddComponent<CardChromeView>();
        Wire(view,
            ("group", group),
            ("titleTab", tabGroup),
            ("titleText", title),
            ("flipIcon", flipIconGroup),
            ("infoIcon", infoIcon.gameObject),
            ("imageIcon", imageIcon.gameObject));
        return root.gameObject;
    }

    // 카드 위의 투명 버튼 (그림은 CardBandView가 그린다). 꺼낸 카드에서만 눌리도록 CanvasGroup으로 막아 둔다.
    private static (Button button, CanvasGroup group) CreateHitButton(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Place(CreateRect(name, parent), pos, size);
        CanvasGroup group = AddGroup(rt);
        Image hit = AddImage(rt, new Color(1f, 1f, 1f, 0f));
        hit.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = hit;
        button.transition = Selectable.Transition.None;
        return (button, group);
    }

    // 카드 본체: 투명 Image(터치 영역) + 투명 뒤집기/닫기 버튼 + CardController. 그림은 CardBandView들이 그린다.
    private static GameObject CreateCard()
    {
        RectTransform root = Place(CreateRect("Card", null), Vector2.zero, CardSize);
        Image hit = AddImage(root, new Color(1f, 1f, 1f, 0f));
        hit.raycastTarget = true;

        var (flipButton, flipGroup) = CreateHitButton(root, "FlipButton", FlipButtonPos, FlipButtonSize);
        var (closeButton, closeGroup) = CreateHitButton(root, "CloseButton", CloseButtonPos, CloseButtonSize);

        var controller = root.gameObject.AddComponent<CardController>();
        Wire(controller,
            ("flipButton", flipButton), ("flipButtonGroup", flipGroup),
            ("closeButton", closeButton), ("closeButtonGroup", closeGroup));
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

        // 파장 이름 라벨은 디자인 배경(StaticLayer)에 그려져 있다.
        // 레일 오른쪽 끝 그림자: 카드가 키오스크 밑으로 들어가는 곳. 카드 위에 덮인다.
        AddSprite(Place(CreateRect("RailEndShadow", panel), RailEndShadowPos, RailEndShadowSize), Part("Rail_EndShadow"));

        var rail = panel.gameObject.AddComponent<RailController>();
        var baker = panel.gameObject.AddComponent<CardBackBaker>();
        Wire(baker, ("backPrefab", backPrefab.GetComponent<CardBackView>()));
        var board = panel.gameObject.AddComponent<SpectrumBoard>();
        Wire(board,
            ("cardPrefab", cardPrefab.GetComponent<CardController>()),
            ("bandViewPrefab", bandViewPrefab.GetComponent<CardBandView>()),
            ("underlayPrefab", underlayPrefab.GetComponent<CardUnderlayView>()),
            ("chromePrefab", chromePrefab.GetComponent<CardChromeView>()),
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
            Button button = CreateSpriteButton(panel, $"Menu_{i + 1:00}", EMWorldStaticLayouts.InfoMenuButtonSprite(i),
                EMWorldStaticLayouts.InfoMenuButtonPos[i], EMWorldStaticLayouts.InfoMenuButtonSize);
            ((Image)button.targetGraphic).raycastPadding = EMWorldStaticLayouts.InfoMenuButtonRaycastPadding;
            buttons[i] = button;
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
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        go.AddComponent<GraphicRaycaster>();

        RectTransform frame = CreateFrame(go.transform);
        PrefabUtility.InstantiatePrefab(spectrum, frame);

        // 정보검색 칸 바탕(배경 디자인의 오른쪽 칸). 레일 카드가 이 밑으로 지나가며 가려진다.
        RectTransform infoBoard = Place(CreateRect("InfoBoard", frame), InfoColumnCenter, new Vector2(ColumnWidth, ScreenHeight));
        AddStaticLayer(infoBoard, "InfoBoard");
        CreateInfo(infoBoard, infoMenu, infoDetails);
        return go;
    }

    // 정보검색 칸: 960x2160 칸을 90° 돌려 2160x960 가로 화면 하나로 쓴다. 디자인대로 칸보다 조금 작게 왼쪽에 붙인다.
    private static void CreateInfo(RectTransform parent, GameObject infoMenu, List<GameObject> infoDetails)
    {
        RectTransform half = Place(CreateRect("Info", parent), InfoPanelOffset, InfoPanelSize);
        half.localRotation = Quaternion.Euler(0, 0, InfoPanelAngle);
        half.localScale = new Vector3(InfoPanelScale, InfoPanelScale, 1f);

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

using System.Collections.Generic;
using UnityEngine;

// F1로 열고 닫는 단축키 도움말. 기본은 숨김, 설정창보다 위에 그린다(Canvas_Help sortingOrder 110).
// 키 목록을 여기에 적지 않는다. 각 기능이 HotkeyHelp에 등록한 실제 키와 설명을 그대로 보여 준다.
public class HelpPanelUI : MonoBehaviour
{
    [Tooltip("F1로 켜고 끌 도움말 루트 오브젝트. 이 스크립트는 항상 켜져 있는 오브젝트(캔버스 등)에 둔다.")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private HelpRowView rowPrefab;
    [Tooltip("줄이 들어갈 곳 (VerticalLayoutGroup)")]
    [SerializeField] private RectTransform rowRoot;

    private readonly List<HelpRowView> rows = new List<HelpRowView>();

    private void OnEnable()
    {
        HotkeyHelp.Register(this, toggleKey, "단축키 도움말 열기/닫기", "모든 화면");
        HotkeyHelp.Changed += OnHelpChanged;
    }

    private void OnDisable()
    {
        HotkeyHelp.Changed -= OnHelpChanged;
        HotkeyHelp.Unregister(this);
    }

    private void Start()
    {
        SetVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetVisible(!panelRoot.activeSelf);
    }

    public void SetVisible(bool show)
    {
        panelRoot.SetActive(show);
        if (show)
            Refresh();
    }

    private void OnHelpChanged()
    {
        if (panelRoot != null && panelRoot.activeSelf)
            Refresh();
    }

    private void Refresh()
    {
        IReadOnlyList<HotkeyHelp.Entry> entries = HotkeyHelp.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            if (rows.Count <= i)
                rows.Add(Instantiate(rowPrefab, rowRoot));
            rows[i].gameObject.SetActive(true);
            rows[i].Set(entries[i]);
        }
        for (int i = entries.Count; i < rows.Count; i++)
            rows[i].gameObject.SetActive(false);
    }
}

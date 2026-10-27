using System;
using UnityEngine;

// 정보검색 칸(테이블 오른쪽 끝, 한 사람용 가로 화면)의 흐름: 메뉴 ↔ 세부 정보.
public class InfoPanelController : MonoBehaviour
{
    [SerializeField] private InfoMenuPanel menuPanel;
    [Tooltip("메뉴 버튼 순서와 같은 순서의 세부 정보 화면")]
    [SerializeField] private InfoDetailPanel[] detailPanels;

    // 관람객 동작 알림 (소리 등 연출 연결용)
    public event Action<int> MenuSelected;
    public event Action BackPressed;

    private void Awake()
    {
        menuPanel.Selected += index => { MenuSelected?.Invoke(index); ShowDetail(index); };
        foreach (InfoDetailPanel detail in detailPanels)
            detail.BackRequested += () => { BackPressed?.Invoke(); ShowMenu(); };
    }

    private void Start()
    {
        ShowMenu();
    }

    public void ShowMenu()
    {
        menuPanel.Show();
        foreach (InfoDetailPanel detail in detailPanels)
            detail.Hide();
    }

    private void ShowDetail(int index)
    {
        if (index < 0 || index >= detailPanels.Length) return;

        menuPanel.Hide();
        for (int i = 0; i < detailPanels.Length; i++)
        {
            if (i == index) detailPanels[i].Show();
            else detailPanels[i].Hide();
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;

// 정보검색 칸의 메뉴 화면. 메뉴 버튼 4개 중 하나를 누르면 그 번호(0부터)를 알린다.
public class InfoMenuPanel : PanelBase
{
    [SerializeField] private Button[] menuButtons;

    public event Action<int> Selected;

    private void Awake()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            int index = i;
            menuButtons[i].onClick.AddListener(() => Selected?.Invoke(index));
        }
    }
}

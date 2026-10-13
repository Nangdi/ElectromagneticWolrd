using System;
using UnityEngine;
using UnityEngine.UI;

// 정보검색 칸의 세부 정보 화면. 내용은 정적 레이어 PNG에 있고 [이전으로] 버튼만 동적 요소다.
public class InfoDetailPanel : PanelBase
{
    [SerializeField] private Button backButton;

    public event Action BackRequested;

    private void Awake()
    {
        backButton.onClick.AddListener(() => BackRequested?.Invoke());
    }
}

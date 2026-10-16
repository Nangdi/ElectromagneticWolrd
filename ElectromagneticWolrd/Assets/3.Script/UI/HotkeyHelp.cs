using System;
using System.Collections.Generic;
using UnityEngine;

// 운영‧개발용 단축키 목록. 단축키를 쓰는 기능이 실제로 설정된 키와 설명을 직접 등록하고,
// F1 도움말(HelpPanelUI)은 등록된 목록을 그대로 보여 준다. 키를 바꾸거나 추가해도 안내가 어긋나지 않는다.
//
// [단축키를 추가하는 방법]
//  OnEnable에서 HotkeyHelp.Register(this, 키, "설명", "동작 화면")을 부르고, OnDisable에서 Unregister(this)를 부른다.
public static class HotkeyHelp
{
    public class Entry
    {
        public UnityEngine.Object owner;
        public KeyCode key;
        public string description;
        public string scope;
    }

    private static readonly List<Entry> entries = new List<Entry>();

    public static IReadOnlyList<Entry> Entries => entries;

    // 목록이 바뀌면 알린다 (도움말이 열려 있으면 다시 그린다).
    public static event Action Changed;

    public static void Register(UnityEngine.Object owner, KeyCode key, string description, string scope)
    {
        entries.RemoveAll(e => e.owner == owner && e.key == key);
        entries.Add(new Entry { owner = owner, key = key, description = description, scope = scope });
        entries.Sort((a, b) => KeyOrder(a.key).CompareTo(KeyOrder(b.key)));
        Changed?.Invoke();
    }

    public static void Unregister(UnityEngine.Object owner)
    {
        if (entries.RemoveAll(e => e.owner == owner) > 0)
            Changed?.Invoke();
    }

    public static string KeyName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Escape: return "ESC";
            case KeyCode.Return: return "Enter";
            case KeyCode.BackQuote: return "`";
            default:
                if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) return ((int)(key - KeyCode.Alpha0)).ToString();
                return key.ToString();
        }
    }

    // F1~F15를 앞에, ESC를 맨 앞에 둔다. 나머지는 키 코드 순서.
    private static int KeyOrder(KeyCode key)
    {
        if (key == KeyCode.Escape) return -1000;
        if (key >= KeyCode.F1 && key <= KeyCode.F15) return -900 + (key - KeyCode.F1);
        return (int)key;
    }
}

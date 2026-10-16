using TMPro;
using UnityEngine;

// F1 도움말의 한 줄: 키 / 기능 / 동작 화면
public class HelpRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text scopeText;

    public void Set(HotkeyHelp.Entry entry)
    {
        keyText.text = entry != null ? HotkeyHelp.KeyName(entry.key) : string.Empty;
        descriptionText.text = entry?.description ?? string.Empty;
        scopeText.text = entry?.scope ?? string.Empty;
    }
}

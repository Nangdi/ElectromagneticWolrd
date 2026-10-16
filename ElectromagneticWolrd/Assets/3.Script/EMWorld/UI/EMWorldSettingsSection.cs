using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 설정창(ESC) 안의 "전자기파로 보는 세상" 운영값. EMWorldConfig(persistentDataPath/emworld.json)의 필드를
// 숫자 입력칸과 토글로 보여 주고, 바꾸면 바로 적용하고 파일에 저장한다.
//
// [항목을 추가하는 방법]
//  1) EMWorldConfig에 public float/int/bool 필드를 추가한다.
//  2) EMWorldSettingsBuilder의 항목 목록에 (필드 이름, 이름표, 단위, 최소, 최대)를 추가하고 "설정창 다시 만들기"를 실행한다.
//  시작할 때 설정창에 없는 필드를 경고로 알려 준다.
public class EMWorldSettingsSection : MonoBehaviour
{
    [Serializable]
    public class NumberSetting
    {
        [Tooltip("EMWorldConfig 안의 float 또는 int 필드 이름")]
        public string fieldName;
        public TMP_InputField input;
        public float min;
        public float max = 100f;
        [Tooltip("표시할 소수 자리 수")]
        public int decimals;

        [NonSerialized] public FieldInfo field;
    }

    [Serializable]
    public class ToggleSetting
    {
        [Tooltip("EMWorldConfig 안의 bool 필드 이름")]
        public string fieldName;
        public Toggle toggle;

        [NonSerialized] public FieldInfo field;
    }

    [SerializeField] private SettingsPanelUI panel;
    [SerializeField] private List<NumberSetting> numbers = new List<NumberSetting>();
    [SerializeField] private List<ToggleSetting> toggles = new List<ToggleSetting>();

    private bool syncing;

    private static EMWorldConfig Config => EMWorldConfig.Current;

    private void Start()
    {
        Bind();

        foreach (NumberSetting setting in numbers)
        {
            if (setting.field == null) continue;
            NumberSetting captured = setting;
            setting.input.onEndEdit.AddListener(text => OnNumberEdited(captured, text));
        }
        foreach (ToggleSetting setting in toggles)
        {
            if (setting.field == null) continue;
            ToggleSetting captured = setting;
            setting.toggle.onValueChanged.AddListener(value => OnToggleChanged(captured, value));
        }

        if (panel != null)
            panel.OnPanelVisibilityChanged += OnPanelVisibilityChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (panel != null)
            panel.OnPanelVisibilityChanged -= OnPanelVisibilityChanged;
    }

    private void OnPanelVisibilityChanged(bool open)
    {
        if (open)
            Refresh(); // 파일을 직접 고쳤을 수도 있으니 열 때마다 현재 값을 다시 보여 준다
    }

    // 필드 이름을 EMWorldConfig 필드와 연결한다. 오타, 타입 불일치, 설정창에 없는 필드는 로그로 알린다.
    private void Bind()
    {
        var bound = new HashSet<string>();
        foreach (NumberSetting setting in numbers)
        {
            setting.field = Find(setting.fieldName, setting.input, t => t == typeof(float) || t == typeof(int));
            if (setting.field != null) bound.Add(setting.fieldName);
        }
        foreach (ToggleSetting setting in toggles)
        {
            setting.field = Find(setting.fieldName, setting.toggle, t => t == typeof(bool));
            if (setting.field != null) bound.Add(setting.fieldName);
        }

        foreach (FieldInfo field in typeof(EMWorldConfig).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!bound.Contains(field.Name))
                Debug.LogWarning($"[Settings] emworld.json의 '{field.Name}'이 설정창에 없습니다. EMWorldSettingsBuilder 항목 목록에 추가하세요.", this);
        }
    }

    private FieldInfo Find(string fieldName, UnityEngine.Object control, Func<Type, bool> typeOk)
    {
        FieldInfo field = typeof(EMWorldConfig).GetField(fieldName ?? string.Empty, BindingFlags.Public | BindingFlags.Instance);
        if (field == null)
        {
            Debug.LogError($"[Settings] EMWorldConfig에 '{fieldName}' 필드가 없습니다.", this);
            return null;
        }
        if (!typeOk(field.FieldType))
        {
            Debug.LogError($"[Settings] '{fieldName}'은 이 칸으로 다룰 수 없는 {field.FieldType.Name} 타입입니다.", this);
            return null;
        }
        if (control == null)
        {
            Debug.LogWarning($"[Settings] '{fieldName}'에 연결된 입력칸/토글이 없습니다.", this);
            return null;
        }
        return field;
    }

    public void Refresh()
    {
        syncing = true;
        foreach (NumberSetting setting in numbers)
        {
            if (setting.field == null) continue;
            setting.input.SetTextWithoutNotify(Format(setting, GetNumber(setting)));
        }
        foreach (ToggleSetting setting in toggles)
        {
            if (setting.field == null) continue;
            setting.toggle.SetIsOnWithoutNotify((bool)setting.field.GetValue(Config));
        }
        syncing = false;
    }

    private void OnNumberEdited(NumberSetting setting, string text)
    {
        if (syncing) return;

        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            Debug.LogWarning($"[Settings] {setting.fieldName}: 숫자가 아니라서 무시합니다 ({text})");
            setting.input.SetTextWithoutNotify(Format(setting, GetNumber(setting)));
            return;
        }

        value = Mathf.Clamp(value, setting.min, setting.max);
        if (setting.field.FieldType == typeof(int))
            setting.field.SetValue(Config, Mathf.RoundToInt(value));
        else
            setting.field.SetValue(Config, value);

        setting.input.SetTextWithoutNotify(Format(setting, GetNumber(setting)));
        EMWorldConfig.Save(setting.fieldName);
        Debug.Log($"[Settings] {setting.fieldName} = {GetNumber(setting).ToString(CultureInfo.InvariantCulture)}");
    }

    private void OnToggleChanged(ToggleSetting setting, bool value)
    {
        if (syncing) return;

        setting.field.SetValue(Config, value);
        EMWorldConfig.Save(setting.fieldName);
        Debug.Log($"[Settings] {setting.fieldName} = {value}");
    }

    private static float GetNumber(NumberSetting setting)
    {
        object value = setting.field.GetValue(Config);
        return value is int i ? i : (float)value;
    }

    private static string Format(NumberSetting setting, float value) =>
        value.ToString("F" + Mathf.Clamp(setting.decimals, 0, 4), CultureInfo.InvariantCulture);
}

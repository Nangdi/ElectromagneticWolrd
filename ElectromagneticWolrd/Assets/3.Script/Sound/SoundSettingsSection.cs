using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 설정창(ESC) 안의 소리 항목. SoundSettings(persistent soundSetting.json)의 볼륨을 조절바로, 음소거를 토글로 보여 준다.
// 조절바를 움직이는 동안 바로 들리게 반영하고, 손을 떼거나 토글을 바꾸면 파일에 저장한다.
// 음소거해도 볼륨 값은 그대로 둔다. (서브 콘텐츠는 전체·SFX 볼륨과 SFX 음소거만 둔다)
public class SoundSettingsSection : MonoBehaviour
{
    public enum Volume { Master, Bgm, Sfx }
    public enum Mute { Bgm, Sfx }

    [Serializable]
    public class VolumeSetting
    {
        public Volume target;
        public Slider slider;
        [Tooltip("현재 값(%)을 보여 줄 글자")]
        public TMP_Text valueText;
    }

    [Serializable]
    public class MuteSetting
    {
        public Mute target;
        public Toggle toggle;
    }

    [SerializeField] private SettingsPanelUI panel;
    [SerializeField] private List<VolumeSetting> volumes = new List<VolumeSetting>();
    [SerializeField] private List<MuteSetting> mutes = new List<MuteSetting>();
    [Tooltip("조절바를 움직이는 동안 들려줄 효과음 키 (비우면 없음)")]
    [SerializeField] private string previewSfxKey = "button";

    private const float PreviewIntervalSeconds = 0.2f;

    private bool syncing;
    private bool dirty;
    private float lastPreviewTime = float.NegativeInfinity;

    private static SoundSettings Settings => SoundSettings.Current;

    private void Start()
    {
        foreach (VolumeSetting setting in volumes)
        {
            if (setting.slider == null) continue;
            VolumeSetting captured = setting;
            setting.slider.minValue = 0f;
            setting.slider.maxValue = 1f;
            setting.slider.onValueChanged.AddListener(value => OnVolumeChanged(captured, value));
        }
        foreach (MuteSetting setting in mutes)
        {
            if (setting.toggle == null) continue;
            MuteSetting captured = setting;
            setting.toggle.onValueChanged.AddListener(value => OnMuteChanged(captured, value));
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

    // 조절바는 움직일 때마다 값이 바뀌므로, 손을 뗀 뒤(마우스/터치가 없는 프레임) 한 번만 저장한다.
    private void Update()
    {
        if (dirty && !Input.GetMouseButton(0) && Input.touchCount == 0)
        {
            dirty = false;
            SoundSettings.Save();
            Debug.Log($"[Settings] 소리: 전체 {Percent(Settings.masterVolume)}, BGM {Percent(Settings.bgmVolume)}, SFX {Percent(Settings.sfxVolume)}");
        }
    }

    private void OnPanelVisibilityChanged(bool open)
    {
        if (open)
            Refresh();
        else if (dirty)
        {
            dirty = false;
            SoundSettings.Save();
        }
    }

    public void Refresh()
    {
        syncing = true;
        foreach (VolumeSetting setting in volumes)
        {
            float value = Get(setting.target);
            if (setting.slider != null) setting.slider.SetValueWithoutNotify(value);
            if (setting.valueText != null) setting.valueText.text = Percent(value);
        }
        foreach (MuteSetting setting in mutes)
        {
            if (setting.toggle != null)
                setting.toggle.SetIsOnWithoutNotify(setting.target == Mute.Bgm ? Settings.bgmMute : Settings.sfxMute);
        }
        syncing = false;
    }

    private void OnVolumeChanged(VolumeSetting setting, float value)
    {
        if (syncing) return;

        value = Mathf.Clamp01(value);
        switch (setting.target)
        {
            case Volume.Master: Settings.masterVolume = value; break;
            case Volume.Bgm: Settings.bgmVolume = value; break;
            case Volume.Sfx: Settings.sfxVolume = value; break;
        }
        if (setting.valueText != null) setting.valueText.text = Percent(value);
        SoundSettings.NotifyChanged();
        dirty = true;

        if (setting.target != Volume.Bgm && !string.IsNullOrEmpty(previewSfxKey) && SoundManager.Instance != null
            && Time.unscaledTime - lastPreviewTime >= PreviewIntervalSeconds)
        {
            lastPreviewTime = Time.unscaledTime;
            SoundManager.Instance.PlaySfx(previewSfxKey);
        }
    }

    private void OnMuteChanged(MuteSetting setting, bool value)
    {
        if (syncing) return;

        if (setting.target == Mute.Bgm) Settings.bgmMute = value;
        else Settings.sfxMute = value;
        SoundSettings.Save();
        Debug.Log($"[Settings] {(setting.target == Mute.Bgm ? "bgmMute" : "sfxMute")} = {value}");
    }

    private static float Get(Volume target)
    {
        switch (target)
        {
            case Volume.Master: return Settings.masterVolume;
            case Volume.Bgm: return Settings.bgmVolume;
            default: return Settings.sfxVolume;
        }
    }

    private static string Percent(float value) => $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
}

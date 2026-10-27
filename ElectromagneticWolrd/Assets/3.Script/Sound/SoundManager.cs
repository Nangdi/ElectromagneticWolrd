using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// 배경음악(BGM)·효과음(SFX) 공통 매니저. 코드는 키로만 부른다: PlaySfx("button"), PlayBgm("idle").
// 소리 파일은 현장에서 바꿀 수 있도록 StreamingAssets/Audio/에 두고 시작할 때 읽어 온다(wav/ogg/mp3).
// 키 → 파일·개별 볼륨 목록은 같은 폴더의 sound.json. 파일이나 키가 없으면 그 소리만 나지 않고 경고를 한 번만 남긴다.
// 크기(전체/BGM/SFX, 음소거)는 SoundSettings(persistent soundSetting.json)를 따르고, 바뀌면 바로 반영한다.
// 게임 진행 → 소리 연결은 프로젝트별 스크립트(EMWorldSoundHooks 등)에서 한다.
public class SoundManager : MonoBehaviour
{
    [Serializable]
    public class SoundEntry
    {
        public string key;
        public string file;          // Audio 폴더 기준 파일 이름
        public float volume = 1f;    // 이 소리만의 크기 (0~1)
    }

    [Serializable]
    private class SoundList
    {
        public List<SoundEntry> sounds = new List<SoundEntry>();
    }

    public const string FolderName = "Audio";
    public const string ListFileName = "sound.json";

    [Tooltip("같은 효과음이 이 시간(초) 안에 다시 불리면 무시한다. 여러 번 겹쳐 소리가 커지는 것을 막는다")]
    [SerializeField] private float sfxRepeatGuardSeconds = 0.06f;

    public static SoundManager Instance { get; private set; }

    public static string FolderPath => Path.Combine(Application.streamingAssetsPath, FolderName);

    private readonly Dictionary<string, SoundEntry> entries = new Dictionary<string, SoundEntry>();
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastSfxTime = new Dictionary<string, float>();
    private readonly HashSet<string> warned = new HashSet<string>();

    private AudioSource sfxSource;
    private AudioSource[] bgmSources;
    private int bgmIndex;
    private string bgmKey;
    private float bgmEntryVolume = 1f;
    private float bgmFade = 1f; // 0 → 1 로 새 곡이 올라오고 이전 곡은 내려간다

    public bool IsLoaded { get; private set; }

    private static SoundSettings Settings => SoundSettings.Current;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        bgmSources = new[] { gameObject.AddComponent<AudioSource>(), gameObject.AddComponent<AudioSource>() };
        foreach (AudioSource source in bgmSources)
        {
            source.playOnAwake = false;
            source.loop = true;
        }

        SoundSettings.Changed += ApplyVolumes;
        ApplyVolumes();
        StartCoroutine(LoadAll());
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        Instance = null;
        SoundSettings.Changed -= ApplyVolumes;
        foreach (AudioClip clip in clips.Values)
            if (clip != null)
                Destroy(clip);
        clips.Clear();
    }

    #region 재생

    public void PlaySfx(string key)
    {
        if (!TryGetClip(key, out AudioClip clip, out SoundEntry entry)) return;

        float now = Time.unscaledTime;
        if (lastSfxTime.TryGetValue(key, out float last) && now - last < sfxRepeatGuardSeconds) return;
        lastSfxTime[key] = now;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(entry.volume));
    }

    // 배경음악을 키로 바꾼다. 같은 곡이면 그대로 둔다. 이전 곡과 bgmFadeSeconds 동안 겹쳐 넘어간다.
    public void PlayBgm(string key)
    {
        if (key == bgmKey) return;
        if (!TryGetClip(key, out AudioClip clip, out SoundEntry entry)) return;

        bgmKey = key;
        bgmEntryVolume = Mathf.Clamp01(entry.volume);
        bgmIndex = 1 - bgmIndex;
        AudioSource next = bgmSources[bgmIndex];
        next.clip = clip;
        next.Play();
        bgmFade = 0f;
        ApplyVolumes();
    }

    public void StopBgm()
    {
        bgmKey = null;
        bgmIndex = 1 - bgmIndex;
        bgmSources[bgmIndex].Stop();
        bgmSources[bgmIndex].clip = null;
        bgmFade = 0f;
    }

    private bool TryGetClip(string key, out AudioClip clip, out SoundEntry entry)
    {
        clip = null;
        if (!entries.TryGetValue(key ?? string.Empty, out entry))
        {
            if (IsLoaded) WarnOnce(key, $"[Sound] '{key}' 키가 {ListFileName}에 없습니다.");
            return false;
        }
        if (!clips.TryGetValue(key, out clip) || clip == null)
        {
            if (IsLoaded) WarnOnce(key, $"[Sound] '{key}' 소리 파일을 읽지 못해 재생하지 않습니다 ({entry.file}).");
            return false;
        }
        return true;
    }

    private void WarnOnce(string key, string message)
    {
        if (warned.Add(key ?? string.Empty))
            Debug.LogWarning(message, this);
    }

    #endregion

    #region 크기

    private void Update()
    {
        if (bgmFade >= 1f) return;

        bgmFade = Mathf.MoveTowards(bgmFade, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, Settings.bgmFadeSeconds));
        ApplyVolumes();
        if (bgmFade >= 1f)
        {
            AudioSource previous = bgmSources[1 - bgmIndex];
            previous.Stop();
            previous.clip = null;
        }
    }

    // 효과음은 PlayOneShot(개별 볼륨) × sfxSource.volume 이라서, 조절바를 움직이면 나고 있는 소리에도 바로 반영된다.
    private void ApplyVolumes()
    {
        float master = Mathf.Clamp01(Settings.masterVolume);
        sfxSource.volume = Settings.sfxMute ? 0f : master * Mathf.Clamp01(Settings.sfxVolume);

        float bgm = Settings.bgmMute ? 0f : master * Mathf.Clamp01(Settings.bgmVolume);
        bgmSources[bgmIndex].volume = bgm * bgmEntryVolume * bgmFade;
        bgmSources[1 - bgmIndex].volume = bgmSources[1 - bgmIndex].clip != null ? bgm * (1f - bgmFade) : 0f;
    }

    #endregion

    #region 읽기

    private IEnumerator LoadAll()
    {
        string listPath = Path.Combine(FolderPath, ListFileName);
        SoundList list = null;
        try
        {
            if (File.Exists(listPath))
                list = JsonUtility.FromJson<SoundList>(File.ReadAllText(listPath));
            else
                Debug.LogWarning($"[Sound] 소리 목록이 없어 소리 없이 실행합니다: {listPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Sound] 소리 목록을 읽지 못해 소리 없이 실행합니다: {listPath} ({e.Message})");
        }

        if (list?.sounds != null)
        {
            foreach (SoundEntry entry in list.sounds)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
                entries[entry.key] = entry;
                yield return LoadClip(entry);
            }
        }

        IsLoaded = true;
        Debug.Log($"[Sound] 소리 {clips.Count}/{entries.Count}개를 읽었습니다 ({FolderPath})");
    }

    private IEnumerator LoadClip(SoundEntry entry)
    {
        string path = Path.Combine(FolderPath, entry.file ?? string.Empty);
        if (!File.Exists(path))
        {
            warned.Add(entry.key);
            Debug.LogWarning($"[Sound] '{entry.key}' 소리 파일이 없습니다: {path}");
            yield break;
        }

        AudioType type = AudioTypeOf(path);
        if (type == AudioType.UNKNOWN)
        {
            warned.Add(entry.key);
            Debug.LogWarning($"[Sound] '{entry.key}' 지원하지 않는 형식입니다 (wav/ogg/mp3): {path}");
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, type))
        {
            yield return request.SendWebRequest();

            AudioClip clip = null;
            if (request.result == UnityWebRequest.Result.Success)
                clip = DownloadHandlerAudioClip.GetContent(request);

            if (clip == null)
            {
                warned.Add(entry.key);
                Debug.LogWarning($"[Sound] '{entry.key}' 소리 파일을 읽지 못했습니다: {path} ({request.error})");
                yield break;
            }

            clip.name = entry.key;
            clips[entry.key] = clip;
        }
    }

    private static AudioType AudioTypeOf(string path)
    {
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".wav": return AudioType.WAV;
            case ".ogg": return AudioType.OGGVORBIS;
            case ".mp3": return AudioType.MPEG;
            default: return AudioType.UNKNOWN;
        }
    }

    #endregion
}

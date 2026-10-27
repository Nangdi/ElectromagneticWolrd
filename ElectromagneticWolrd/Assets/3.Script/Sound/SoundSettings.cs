using System;
using UnityEngine;

// 소리 크기 설정 (persistentDataPath/soundSetting.json).
// 파일이 없으면 StreamingAssets의 같은 이름 파일을 복사해 오고, 그것도 없으면 아래 기본값으로 만든다.
// 설정창에서 바꾸면 바로 저장한다. 음소거해도 볼륨 값은 그대로 두어, 풀면 원래 크기로 돌아온다.
[Serializable]
public class SoundSettings
{
    public const string FileName = "soundSetting.json";

    public float masterVolume = 1f;   // 전체 (0~1)
    public float bgmVolume = 0.7f;    // 배경음악 (0~1). 이 프로젝트는 서브 콘텐츠라 BGM을 쓰지 않지만 구조는 남겨 둔다
    public float sfxVolume = 0.6f;    // 효과음 (0~1). 서브 콘텐츠라 다른 전시물 소리를 가리지 않게 낮게 둔다
    public bool bgmMute;
    public bool sfxMute;
    public float bgmFadeSeconds = 1.5f; // BGM이 겹쳐 넘어가는 시간

    private static SoundSettings current;

    public static SoundSettings Current => current ??= JsonManager.LoadData(FilePath, new SoundSettings());

    public static string FilePath => JsonManager.PersistentPath(FileName);

    public static event Action Changed;

    // 값만 바로 반영한다 (조절바를 움직이는 동안).
    public static void NotifyChanged() => Changed?.Invoke();

    // 바로 반영하고 파일에 저장한다.
    public static void Save()
    {
        try
        {
            JsonManager.SaveData(Current, FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Sound] 소리 설정을 저장하지 못했습니다: {e.Message}");
        }
        Changed?.Invoke();
    }
}

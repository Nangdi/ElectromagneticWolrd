using System;
using System.IO;
using UnityEngine;

// 현장에서 파일을 열어 고치는 운영값 (persistentDataPath/emworld.json).
// 파일이 없으면 StreamingAssets의 같은 이름 파일을 복사해 오고, 그것도 없으면 아래 기본값으로 만든다.
[Serializable]
public class EMWorldConfig
{
    public const string FileName = "emworld.json";

    // 레일
    public float railSpeed = 80f;          // 레일이 흐르는 속도 (px/초, 7680x2160 기준)
    public float railMinSpacing = 380f;    // 레일 위 카드 사이 최소 간격 (px)
    public float railCardScale = 0.43f;    // 레일 위 카드 크기 (꺼냈을 때 크기 대비. 디자인: 500x558 → 216x240)
    public bool railShowsOriginal = true;  // 레일 위 카드는 파장과 관계없이 원본 이미지로 보이고, 꺼내면 파장별 이미지로 바뀐다
    public float originalFadeSeconds = 0.4f; // 꺼낼 때 원본에서 파장별 이미지로 바뀌는 시간

    // 카드
    public float cardReturnSeconds = 30f;  // 꺼낸 카드를 아무도 안 만지면 레일로 돌아가기까지의 시간
    public float pickupSeconds = 0.25f;    // 꺼낼 때 커지는 연출 시간
    public float rotateSeconds = 0.35f;    // 관람객 방향으로 도는 연출 시간
    public float returnAnimSeconds = 0.4f; // 돌아갈 때 작아지며 사라지는 연출 시간
    public float flipSeconds = 0.5f;       // 카드가 뒤집혀 정보면/사진면으로 바뀌는 연출 시간
    public float glideDamping = 3.5f;      // 놓은 뒤 미끄러지는 속도가 줄어드는 정도 (클수록 빨리 멈춤. 미끄러지는 거리 = 속도 / 이 값)
    public float glideMaxSpeed = 6000f;    // 미끄러지기 시작 속도 상한 (px/초)

    // 파장 칸 경계
    public float bandBlendWidth = 160f;    // 이웃한 파장 이미지가 섞이며 바뀌는 폭 (px). 0이면 경계에서 딱 끊긴다

    // 무입력 대기 복귀
    public float idleResetSeconds = 120f;

    private static EMWorldConfig current;

    public static EMWorldConfig Current => current ??= Load();

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // 설정창에서 값을 바꾸면 (필드 이름)을 알린다. 시작할 때만 읽는 기능은 여기에 반응해 다시 적용한다.
    public static event Action<string> Changed;

    // 설정창에서 바꾼 값을 바로 파일에 저장하고 알린다.
    public static void Save(string changedField)
    {
        try
        {
            JsonManager.SaveData(Current, FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"운영값 파일을 저장하지 못했습니다: {e.Message}");
        }
        Changed?.Invoke(changedField);
    }

    private static EMWorldConfig Load()
    {
        var config = new EMWorldConfig();
        try
        {
            string path = FilePath;
            if (!File.Exists(path))
            {
                string source = Path.Combine(Application.streamingAssetsPath, FileName);
                if (File.Exists(source))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.Copy(source, path);
                }
            }

            if (File.Exists(path))
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), config);

            // 새 버전에서 추가된 필드가 기존 파일에도 기본값으로 채워지도록 다시 저장한다.
            JsonManager.SaveData(config, path);
        }
        catch (Exception e)
        {
            Debug.LogError($"운영값 파일을 읽지 못해 기본값으로 실행합니다: {e.Message}");
        }
        return config;
    }
}

using UnityEngine;

// 「전자기파로 보는 세상」 진행 → 효과음 연결. 서브 콘텐츠라 BGM 없이 관람객 동작에만 짧은 효과음을 낸다.
// 키와 파일은 StreamingAssets/Audio/sound.json, 어느 키가 언제 나오는지는 같은 폴더 README에 적는다.
// 시간이 지나 저절로 레일로 돌아가는 카드와 무입력 대기 복귀는 관람객 동작이 아니라 소리를 내지 않는다.
public class EMWorldSoundHooks : MonoBehaviour
{
    public const string ButtonKey = "button"; // 정보검색 [이전으로]
    public const string SelectKey = "select"; // 정보검색 메뉴 선택
    public const string PickupKey = "pickup"; // 레일에서 카드 꺼내기
    public const string CloseKey = "close";   // 카드 X (레일로 돌려보내기)
    public const string FlipKey = "flip";     // 카드 뒤집기

    [SerializeField] private SpectrumBoard board;
    [SerializeField] private InfoPanelController[] infoPanels;

    private void OnEnable()
    {
        if (board != null)
        {
            board.CardPickedUp += OnPickedUp;
            board.CardClosed += OnClosed;
            board.CardFlipped += OnFlipped;
        }
        foreach (InfoPanelController info in infoPanels)
        {
            if (info == null) continue;
            info.MenuSelected += OnMenuSelected;
            info.BackPressed += OnBackPressed;
        }
    }

    private void OnDisable()
    {
        if (board != null)
        {
            board.CardPickedUp -= OnPickedUp;
            board.CardClosed -= OnClosed;
            board.CardFlipped -= OnFlipped;
        }
        foreach (InfoPanelController info in infoPanels)
        {
            if (info == null) continue;
            info.MenuSelected -= OnMenuSelected;
            info.BackPressed -= OnBackPressed;
        }
    }

    private static void Play(string key)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(key);
    }

    private void OnPickedUp(CardController card) => Play(PickupKey);
    private void OnClosed(CardController card) => Play(CloseKey);
    private void OnFlipped(CardController card) => Play(FlipKey);
    private void OnMenuSelected(int index) => Play(SelectKey);
    private void OnBackPressed() => Play(ButtonKey);
}

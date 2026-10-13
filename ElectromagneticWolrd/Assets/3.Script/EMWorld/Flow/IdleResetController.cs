using UnityEngine;

// 일정 시간 아무 입력이 없으면 처음 상태로 돌린다.
// 놓아 둔 카드는 레일로 돌아가고, 정보검색 칸은 메뉴로 돌아간다.
public class IdleResetController : MonoBehaviour
{
    [SerializeField] private SpectrumBoard board;
    [SerializeField] private InfoPanelController[] infoPanels;

    private float lastInputTime;
    private bool isReset = true;

    private void Update()
    {
        if (Input.touchCount > 0 || Input.GetMouseButton(0) || Input.anyKeyDown)
        {
            lastInputTime = Time.unscaledTime;
            isReset = false;
            return;
        }

        if (isReset || Time.unscaledTime - lastInputTime < EMWorldConfig.Current.idleResetSeconds) return;

        isReset = true;
        board.ReturnAllFree();
        foreach (InfoPanelController info in infoPanels)
            info.ShowMenu();
        Debug.Log("무입력 대기 복귀");
    }
}

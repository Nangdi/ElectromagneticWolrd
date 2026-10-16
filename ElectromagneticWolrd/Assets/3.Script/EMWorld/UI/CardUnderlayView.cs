using UnityEngine;

// 카드 한 장의 사진 아래 장식: 회색 팝업 테두리와 X(꺼낸 카드), 그림자(레일 위 카드).
// 칸으로 나누지 않고 카드마다 하나만, 그 카드의 파장별 뷰 7개 아래에 그린다.
// 칸마다 그리면 경계에서 섞일 때 다음 칸의 테두리가 앞 칸의 사진 위에 반투명하게 겹쳐 사진에 회색 띠가 생긴다.
// 위치, 회전, 크기는 CardController가 매 프레임 맞춰 준다. 터치는 받지 않는다 (X는 카드 본체의 투명 버튼이 받는다).
public class CardUnderlayView : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private CanvasGroup popupFrame;
    [SerializeField] private CanvasGroup railShadow;

    public RectTransform Rect => (RectTransform)transform;

    // 1 = 꺼낸 카드(팝업 테두리가 보임), 0 = 레일 위(그림자만 보임)
    public void SetPopupAlpha(float alpha)
    {
        popupFrame.alpha = alpha;
        railShadow.alpha = 1f - alpha;
    }

    public void SyncTo(RectTransform master, float alpha)
    {
        Rect.position = master.position;
        Rect.rotation = master.rotation;
        Rect.localScale = master.localScale;
        group.alpha = alpha;
    }
}

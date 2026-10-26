using TMPro;
using UnityEngine;

// 카드 한 장의 사진 위 장식: 왼쪽 아래 제목 탭, 오른쪽 아래 뒤집기 아이콘. 꺼낸 카드에서만 보인다.
// 파장별 뷰(CardBandView)와 달리 칸으로 나뉘지 않고 카드마다 하나만, 그 카드의 파장별 뷰 7개 위에 그린다.
// 칸마다 그리면 경계에서 섞일 때 다음 칸의 사진이 앞 칸의 탭 위에 반투명하게 겹쳐 흰 탭에 사진 색이 비친다.
// 위치, 회전, 크기는 CardController가 매 프레임 맞춰 준다. 터치는 받지 않는다.
public class CardChromeView : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private CanvasGroup titleTab;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private CanvasGroup flipIcon;
    [SerializeField] private GameObject infoIcon;
    [SerializeField] private GameObject imageIcon;

    public RectTransform Rect => (RectTransform)transform;

    public void Set(CardData data)
    {
        titleText.text = data?.title ?? string.Empty;
        SetFace(false);
        SetPopupAlpha(0f);
    }

    public void SetFace(bool showBack)
    {
        infoIcon.SetActive(!showBack);
        imageIcon.SetActive(showBack);
    }

    // showFlip: 뒤집기 기능(EMWorldConfig.cardFlipEnabled)을 끄면 뒤집기 아이콘은 숨긴다
    public void SetPopupAlpha(float alpha, bool showFlip = true)
    {
        titleTab.alpha = alpha;
        flipIcon.alpha = showFlip ? alpha : 0f;
    }

    public void SyncTo(RectTransform master, float alpha)
    {
        Rect.position = master.position;
        Rect.rotation = master.rotation;
        Rect.localScale = master.localScale;
        group.alpha = alpha;
    }
}

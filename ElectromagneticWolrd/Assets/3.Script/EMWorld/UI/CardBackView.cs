using TMPro;
using UnityEngine;

// 카드 뒷면(정보면): 제목(card.json의 title). 7개 파장 뷰 모두 같은 내용이라 경계를 지나도 이어져 보인다.
public class CardBackView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;

    public void Set(CardData data)
    {
        titleText.text = data?.title ?? string.Empty;
    }
}

using TMPro;
using UnityEngine;

// 카드 뒷면(정보면): 분류, 제목, 설명. 7개 파장 뷰 모두 같은 내용이라 경계를 지나도 이어져 보인다.
public class CardBackView : MonoBehaviour
{
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    public void Set(CardData data)
    {
        categoryText.text = data != null ? CardCategoryInfo.DisplayName(data.category) : string.Empty;
        titleText.text = data?.title ?? string.Empty;
        descriptionText.text = data?.description ?? string.Empty;
    }
}

using System.Collections.Generic;
using UnityEngine;

// 화면 가운데 한 줄 레일. 카드가 왼쪽(라디오파)에서 오른쪽(감마선)으로 흐른다.
// 같은 간격의 슬롯들이 함께 움직이고, 오른쪽 끝(화면 밖)에 닿은 슬롯은 왼쪽 끝(화면 밖)으로 돌아간다.
// 관람객이 꺼내 간 슬롯은 비어 있다가, 돌아갈 때 대기열에서 돌아온 카드로 채워진다.
public class RailController : MonoBehaviour
{
    [Tooltip("레일 중심선 y (CardLayer 기준, px)")]
    [SerializeField] private float railY;
    [Tooltip("레일 영역의 절반 높이(px). 놓은 카드가 이 영역에 걸치면 밖으로 밀어낸다")]
    [SerializeField] private float halfHeight = 150f;
    [Tooltip("카드가 화면 밖에서 돌아 들어오는 여유 거리(px). 레일 카드 대각선의 절반보다 커야 한다")]
    [SerializeField] private float edgeMargin = 220f;

    private class Slot
    {
        public CardController card;
        public float angle; // 한 줄 레일이라 양쪽 관람객이 모두 보도록 0°/180°를 번갈아 둔다
    }

    private readonly List<Slot> slots = new List<Slot>();
    private readonly Queue<CardController> waiting = new Queue<CardController>();
    private float spacing;
    private float startX;
    private float offset;

    public float RailY => railY;
    public float HalfHeight => halfHeight;

    private static EMWorldConfig Config => EMWorldConfig.Current;

    public void Setup(IList<CardController> cards, float areaWidth)
    {
        slots.Clear();
        waiting.Clear();
        offset = 0f;

        float totalLength = areaWidth + edgeMargin * 2f;
        int maxSlots = Mathf.FloorToInt(totalLength / Mathf.Max(1f, Config.railMinSpacing));
        int count = Mathf.Clamp(maxSlots, 1, Mathf.Max(1, cards.Count));
        spacing = totalLength / count;
        startX = -areaWidth * 0.5f - edgeMargin;

        for (int i = 0; i < count; i++)
            slots.Add(new Slot { angle = i % 2 == 0 ? 0f : 180f });

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < count)
            {
                slots[i].card = cards[i];
                cards[i].EnterRail();
            }
            else
            {
                waiting.Enqueue(cards[i]);
            }
        }
        UpdatePoses();
    }

    // 관람객이 꺼내 간 카드의 슬롯을 비운다.
    public void Detach(CardController card)
    {
        foreach (Slot slot in slots)
            if (slot.card == card)
                slot.card = null;
    }

    // 회수가 끝난 카드를 대기열에 넣는다. 다음에 빈 슬롯이 왼쪽 끝으로 돌아올 때 레일에 오른다.
    public void Enqueue(CardController card)
    {
        if (!waiting.Contains(card))
            waiting.Enqueue(card);
    }

    private void Update()
    {
        if (slots.Count == 0) return;

        offset += Config.railSpeed * Time.unscaledDeltaTime;
        while (offset >= spacing)
        {
            offset -= spacing;
            Slot last = slots[slots.Count - 1];
            slots.RemoveAt(slots.Count - 1);
            slots.Insert(0, last);

            if (last.card == null && waiting.Count > 0)
            {
                last.card = waiting.Dequeue();
                last.card.EnterRail();
            }
        }
        UpdatePoses();
    }

    private void UpdatePoses()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            CardController card = slots[i].card;
            if (card != null)
                card.SetRailPose(new Vector2(startX + i * spacing + offset, railY), slots[i].angle);
        }
    }
}

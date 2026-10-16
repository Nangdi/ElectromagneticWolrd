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
    [SerializeField] private float halfHeight = 136f;
    [Tooltip("카드가 화면 밖에서 돌아 들어오는 여유 거리(px). 레일 카드 대각선의 절반보다 커야 한다")]
    [SerializeField] private float edgeMargin = 220f;
    [Tooltip("레일 위 카드 각도(도). 디자인: 시계방향 90° 눕혀 흐름 방향을 향하게 해서 양쪽 관람객 모두 같은 조건으로 본다")]
    [SerializeField] private float cardAngle = -90f;

    private class Slot
    {
        public CardController card;
    }

    private readonly List<Slot> slots = new List<Slot>();
    private readonly Queue<CardController> waiting = new Queue<CardController>();
    private float spacing;
    private float startX;
    private float offset;
    private float areaWidth;
    private int cardCount;

    public float RailY => railY;
    public float HalfHeight => halfHeight;

    private static EMWorldConfig Config => EMWorldConfig.Current;

    public void Setup(IList<CardController> cards, float width)
    {
        waiting.Clear();
        areaWidth = width;
        cardCount = cards.Count;
        BuildSlots();

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < slots.Count)
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

    private void OnEnable()
    {
        EMWorldConfig.Changed += OnConfigChanged;
    }

    private void OnDisable()
    {
        EMWorldConfig.Changed -= OnConfigChanged;
    }

    // 같은 간격의 슬롯을 레일 길이(화면 밖 여유 포함)에 맞춰 만든다. 카드 수보다 많이 만들지 않는다.
    private void BuildSlots()
    {
        slots.Clear();
        offset = 0f;

        float totalLength = areaWidth + edgeMargin * 2f;
        int maxSlots = Mathf.FloorToInt(totalLength / Mathf.Max(1f, Config.railMinSpacing));
        int count = Mathf.Clamp(maxSlots, 1, Mathf.Max(1, cardCount));
        spacing = totalLength / count;
        startX = -areaWidth * 0.5f - edgeMargin;

        for (int i = 0; i < count; i++)
            slots.Add(new Slot());
    }

    // 설정창에서 카드 간격을 바꾸면 레일 위 카드를 새 슬롯에 다시 놓는다. 자리가 모자라면 남는 카드는 대기열로 보낸다.
    private void OnConfigChanged(string field)
    {
        if (field != nameof(EMWorldConfig.railMinSpacing) || slots.Count == 0) return;

        var onRail = new List<CardController>();
        foreach (Slot slot in slots)
            if (slot.card != null)
                onRail.Add(slot.card);

        BuildSlots();
        for (int i = 0; i < onRail.Count; i++)
        {
            if (i < slots.Count)
            {
                slots[i].card = onRail[i];
            }
            else
            {
                onRail[i].LeaveRail();
                waiting.Enqueue(onRail[i]);
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
                card.SetRailPose(new Vector2(startX + i * spacing + offset, railY), cardAngle);
        }
    }
}

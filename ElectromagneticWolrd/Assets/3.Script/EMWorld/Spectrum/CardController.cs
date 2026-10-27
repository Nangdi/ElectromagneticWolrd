using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 카드 본체. 눈에 보이지 않고(투명 Image) 터치와 위치‧회전‧크기만 담당한다.
// 실제 그림은 파장 칸마다 하나씩 있는 CardBandView 7개가 이 본체를 그대로 따라 그리고,
// 칸의 RectMask2D가 칸 밖을 잘라내서 경계에 걸친 카드가 파장별로 나뉘어 보인다.
// 사진 아래 장식(테두리, 그림자)은 CardUnderlayView 하나가 7개 뷰 아래에,
// 사진 위 장식(제목 탭, 뒤집기 아이콘)은 CardChromeView 하나가 7개 뷰 위에 그린다.
// 꺼낸 카드는 오른쪽 아래 버튼을 누르면 뒤집혀 정보면(뒷면)과 사진면(앞면)이 바뀌고, 오른쪽 위 X를 누르면 레일로 돌아간다.
// 드래그하다 놓으면 손을 뗄 때의 속도로 미끄러지다 서서히 멈춘다.
public class CardController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum CardState
    {
        Waiting,   // 레일 밖 대기열 (화면에 없음)
        OnRail,    // 레일 위에서 흐르는 중
        Held,      // 관람객이 잡고 있음
        Free,      // 놓아 둔 상태. 일정 시간 안 만지면 레일로 돌아감
        Returning, // 작아지며 사라지는 중
    }

    private const int NoPointer = int.MinValue;

    [Tooltip("관람객 방향이 바뀌는 레일 중심선으로부터의 거리(px). 중심선 근처에서 방향이 흔들리지 않게 한다")]
    [SerializeField] private float facingHysteresis = 40f;

    [Header("버튼 (꺼낸 카드에서만 눌림. 그림은 파장별 뷰가 그린다)")]
    [Tooltip("사진 오른쪽 아래: 정보면 ↔ 사진면 뒤집기")]
    [SerializeField] private Button flipButton;
    [SerializeField] private CanvasGroup flipButtonGroup;
    [Tooltip("테두리 오른쪽 위 X: 카드를 바로 레일로 돌려보냄")]
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup closeButtonGroup;
    [Tooltip("팝업 장식(테두리, 제목 탭, 버튼)이 나타나고 사라지는 시간(초)")]
    [SerializeField] private float buttonFadeSeconds = 0.2f;

    public event Action<CardController> PickedUp;
    public event Action<CardController> ReturnFinished;
    public event Action<CardController> Closed;  // X 버튼으로 돌려보냄
    public event Action<CardController> Flipped; // 뒤집기 시작

    public CardData Data { get; private set; }
    public CardState State { get; private set; } = CardState.Waiting;
    public RectTransform Rect => (RectTransform)transform;

    private SpectrumBoard board;
    private readonly List<CardBandView> views = new List<CardBandView>();
    private CardUnderlayView underlay;
    private CardChromeView chrome;
    private int pointerId = NoPointer;
    private Vector2 grabOffset;
    private float lastTouchTime;

    private float angle, targetAngle, angleVelocity;
    private float scale = 1f, targetScale = 1f, scaleVelocity;
    private float alpha = 1f;
    private Vector2? nudgeTarget;
    private Vector2 nudgeVelocity;

    // 미끄러지기: 드래그 중 손가락 속도를 따라가다가, 놓으면 그 속도로 움직이며 감속한다.
    private const float GlideMinSpeed = 120f;  // 이보다 느리게 놓으면 미끄러지지 않고 그 자리에 둔다 (px/초)
    private const float GlideStopSpeed = 25f;  // 이보다 느려지면 멈춘다
    private const float EdgeBounce = 0.35f;    // 체험 영역 끝에 닿으면 이 비율로 튕겨 나온다
    private Vector2 velocity;
    private Vector2 lastDragPos;
    private float lastDragTime;
    private float returnElapsed, returnStartScale;

    private enum FlipPhase { None, Closing, Opening }
    private FlipPhase flipPhase;
    private float flipElapsed;
    private bool showingBack;
    private float popupAlpha; // 팝업 장식이 보이는 정도 (1 = 꺼낸 카드, 0 = 레일 위)
    private float originalAlpha; // 레일 위 원본 이미지가 덮이는 정도 (1 = 원본만 보임)

    private static EMWorldConfig Config => EMWorldConfig.Current;

    private void Awake()
    {
        flipButton.onClick.AddListener(Flip);
        closeButton.onClick.AddListener(Close);
    }

    public void Init(SpectrumBoard owner, CardData data, IList<CardBandView> bandViews, CardUnderlayView underlayView,
        CardChromeView chromeView, Texture backTexture)
    {
        board = owner;
        Data = data;
        views.Clear();
        views.AddRange(bandViews);
        for (int i = 0; i < views.Count; i++)
            views[i].Set(data, (BandType)i, backTexture);
        underlay = underlayView;
        underlay.SetPopupAlpha(0f);
        chrome = chromeView;
        chrome.Set(data);
        SetWaiting();
    }

    #region 레일, 회수

    // 대기열에서 레일 슬롯으로 올라간다. 화면 밖에서 호출된다.
    public void EnterRail()
    {
        State = CardState.OnRail;
        pointerId = NoPointer;
        nudgeTarget = null;
        velocity = Vector2.zero;
        alpha = 1f;
        scale = targetScale = Config.railCardScale;
        scaleVelocity = 0f;
        flipPhase = FlipPhase.None;
        SetFace(false);
        popupAlpha = 0f;
        originalAlpha = Config.railShowsOriginal ? 1f : 0f;
        SetVisible(true);
    }

    // 레일 위에 있을 때 RailController가 매 프레임 위치와 방향을 정한다.
    public void SetRailPose(Vector2 position, float railAngle)
    {
        if (State != CardState.OnRail) return;

        Rect.anchoredPosition = position;
        angle = targetAngle = railAngle;
        angleVelocity = 0f;
    }

    // 작아지며 사라진 뒤 대기열로 돌아간다.
    public void BeginReturn()
    {
        if (State != CardState.Free) return;

        State = CardState.Returning;
        returnElapsed = 0f;
        returnStartScale = scale;
        nudgeTarget = null;
    }

    // 레일 자리가 줄어 대기열로 돌아갈 때 (화면에서 바로 사라짐).
    public void LeaveRail()
    {
        if (State == CardState.OnRail)
            SetWaiting();
    }

    // X 버튼: 다른 손가락이 잡고 있어도 놓고 바로 레일로 돌려보낸다.
    public void Close()
    {
        if (State != CardState.Held && State != CardState.Free) return;

        pointerId = NoPointer;
        velocity = Vector2.zero;
        State = CardState.Free;
        BeginReturn();
        Closed?.Invoke(this);
    }

    private void SetWaiting()
    {
        State = CardState.Waiting;
        pointerId = NoPointer;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        foreach (CardBandView view in views)
            view.gameObject.SetActive(visible);
        underlay.gameObject.SetActive(visible);
        chrome.gameObject.SetActive(visible);
    }

    #endregion

    #region 터치

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerId != NoPointer) return; // 이미 다른 손가락이 잡고 있음
        if (State != CardState.OnRail && State != CardState.Free) return;
        // 레일 양 끝의 화면 밖 여유 구간(정보검색 칸 아래 포함)에서는 잡히지 않게 한다.
        if (Mathf.Abs(LocalPoint(eventData).x) > board.AreaHalfSize.x) return;

        bool fromRail = State == CardState.OnRail;
        pointerId = eventData.pointerId;
        State = CardState.Held;
        nudgeTarget = null;
        lastTouchTime = Time.unscaledTime;
        grabOffset = Rect.anchoredPosition - LocalPoint(eventData);
        velocity = Vector2.zero;
        lastDragPos = Rect.anchoredPosition;
        lastDragTime = Time.unscaledTime;
        board.BringToFront(this);

        if (fromRail)
        {
            targetScale = 1f;
            PickedUp?.Invoke(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;

        Vector2 pos = ClampToArea(LocalPoint(eventData) + grabOffset);
        Rect.anchoredPosition = pos;

        // 최근 움직임에 무게를 둔 손가락 속도
        float now = Time.unscaledTime;
        float dt = now - lastDragTime;
        if (dt > 0.0001f)
        {
            Vector2 current = (pos - lastDragPos) / dt;
            velocity = Vector2.Lerp(velocity, current, Mathf.Clamp01(dt / 0.05f));
            lastDragPos = pos;
            lastDragTime = now;
        }
        lastTouchTime = now;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;

        pointerId = NoPointer;
        State = CardState.Free;
        lastTouchTime = Time.unscaledTime;

        // 손가락을 멈췄다가 떼면(마지막 움직임이 오래전이면) 미끄러지지 않는다.
        if (Time.unscaledTime - lastDragTime > 0.08f || velocity.magnitude < GlideMinSpeed)
            velocity = Vector2.zero;
        else
            velocity = Vector2.ClampMagnitude(velocity, Config.glideMaxSpeed);

        if (velocity == Vector2.zero)
            NudgeOutOfRail();
    }

    // 레일에 걸쳐 멈추면 흐르는 카드와 겹치지 않도록 가까운 쪽으로 밀어낸다.
    private void NudgeOutOfRail()
    {
        Vector2 pos = Rect.anchoredPosition;
        float dy = pos.y - board.Rail.RailY;
        float keepOut = board.Rail.HalfHeight + Rect.rect.height * 0.5f;
        if (Mathf.Abs(dy) < keepOut)
        {
            float side = Mathf.Approximately(dy, 0f) ? (targetAngle > 90f ? 1f : -1f) : Mathf.Sign(dy);
            nudgeTarget = ClampToArea(new Vector2(pos.x, board.Rail.RailY + side * keepOut));
            nudgeVelocity = Vector2.zero;
        }
    }

    private Vector2 LocalPoint(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent, eventData.position,
            eventData.pressEventCamera, out Vector2 local);
        return local;
    }

    // 카드 중심이 체험 영역 안에 남도록 제한한다. (카드 일부는 화면 밖으로 나가도 됨)
    private Vector2 ClampToArea(Vector2 pos)
    {
        Vector2 half = board.AreaHalfSize;
        float margin = Rect.rect.width * 0.25f;
        pos.x = Mathf.Clamp(pos.x, -half.x + margin, half.x - margin);
        pos.y = Mathf.Clamp(pos.y, -half.y + margin, half.y - margin);
        return pos;
    }

    #endregion

    #region 뒤집기

    // 오른쪽 아래 버튼: 꺼낸 카드를 뒤집는다. 반쯤(옆으로 선 순간) 접혔을 때 앞‧뒷면을 바꾸고, 살짝 튕기며 자리 잡는다.
    public void Flip()
    {
        if (!Config.cardFlipEnabled) return;
        if (flipPhase != FlipPhase.None) return;
        if (State != CardState.Held && State != CardState.Free) return;

        flipPhase = FlipPhase.Closing;
        flipElapsed = 0f;
        lastTouchTime = Time.unscaledTime;
        board.BringToFront(this);
        Flipped?.Invoke(this);
    }

    private void SetFace(bool back)
    {
        showingBack = back;
        foreach (CardBandView view in views)
            view.SetFace(back);
        chrome.SetFace(back);
    }

    // 뒤집기 연출 중의 가로/세로 배율
    private Vector2 UpdateFlip(float dt)
    {
        if (flipPhase == FlipPhase.None) return Vector2.one;

        float half = Mathf.Max(0.01f, Config.flipSeconds * 0.5f);
        flipElapsed += dt;
        float t = Mathf.Clamp01(flipElapsed / half);

        if (flipPhase == FlipPhase.Closing)
        {
            if (t >= 1f)
            {
                SetFace(!showingBack);
                flipPhase = FlipPhase.Opening;
                flipElapsed = 0f;
                return new Vector2(0f, 1f);
            }
            return new Vector2(1f - t * t, 1f);
        }

        if (t >= 1f)
        {
            flipPhase = FlipPhase.None;
            return Vector2.one;
        }
        // 펼쳐질 때 살짝 넘쳤다가 돌아오며(EaseOutBack) 세로로도 조금 부풀어 "자리 잡는" 느낌을 준다
        return new Vector2(EaseOutBack(t), 1f + 0.06f * Mathf.Sin(Mathf.PI * t));
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    // 팝업 장식과 버튼은 꺼낸 카드(잡고 있거나 놓아 둔 상태)에서만 보이고 눌린다. 레일 위에서는 그림자만 보인다.
    private void UpdatePopup(float dt)
    {
        bool show = State == CardState.Held || State == CardState.Free;
        float speed = 1f / Mathf.Max(0.01f, buttonFadeSeconds);
        popupAlpha = Mathf.MoveTowards(popupAlpha, show ? 1f : 0f, speed * dt);
        underlay.SetPopupAlpha(popupAlpha);
        bool flipEnabled = Config.cardFlipEnabled;
        chrome.SetPopupAlpha(popupAlpha, flipEnabled);
        bool usable = show && popupAlpha > 0.5f;
        flipButtonGroup.interactable = usable && flipEnabled;
        flipButtonGroup.blocksRaycasts = usable && flipEnabled;
        closeButtonGroup.interactable = usable;
        closeButtonGroup.blocksRaycasts = usable;

        // 뒤집기를 끄면 정보면으로 뒤집혀 있던 카드도 사진면으로 되돌린다
        if (!flipEnabled && showingBack && flipPhase == FlipPhase.None)
            SetFace(false);
    }

    #endregion

    #region 연출

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        switch (State)
        {
            case CardState.Held:
                UpdateFacing();
                break;

            case CardState.Free:
                UpdateFacing();
                if (velocity != Vector2.zero)
                    UpdateGlide(dt);
                else if (nudgeTarget.HasValue)
                {
                    Rect.anchoredPosition = Vector2.SmoothDamp(Rect.anchoredPosition, nudgeTarget.Value, ref nudgeVelocity,
                        SmoothTime(Config.pickupSeconds), Mathf.Infinity, dt);
                    if ((Rect.anchoredPosition - nudgeTarget.Value).sqrMagnitude < 1f)
                        nudgeTarget = null;
                }
                if (Time.unscaledTime - lastTouchTime > Config.cardReturnSeconds)
                    BeginReturn();
                break;

            case CardState.Returning:
                returnElapsed += dt;
                float t = Mathf.Clamp01(returnElapsed / Mathf.Max(0.01f, Config.returnAnimSeconds));
                scale = Mathf.Lerp(returnStartScale, 0f, t * t);
                alpha = 1f - t;
                if (t >= 1f)
                {
                    SetWaiting();
                    ReturnFinished?.Invoke(this);
                    return;
                }
                break;
        }

        if (State == CardState.OnRail)
            targetScale = Config.railCardScale; // 설정창에서 바꾸면 레일 위 카드에 바로 반영
        if (State != CardState.Returning)
            scale = Mathf.SmoothDamp(scale, targetScale, ref scaleVelocity, SmoothTime(Config.pickupSeconds), Mathf.Infinity, dt);
        if (State != CardState.OnRail)
            angle = Mathf.SmoothDampAngle(angle, targetAngle, ref angleVelocity, SmoothTime(Config.rotateSeconds), Mathf.Infinity, dt);

        Vector2 flip = UpdateFlip(dt);
        UpdatePopup(dt);
        UpdateOriginal(dt);

        Rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        Rect.localScale = new Vector3(scale * flip.x, scale * flip.y, 1f);
    }

    private void LateUpdate()
    {
        foreach (CardBandView view in views)
        {
            view.SyncTo(Rect, alpha);
            view.SetOriginalAlpha(originalAlpha);
        }
        underlay.SyncTo(Rect, alpha);
        chrome.SyncTo(Rect, alpha);
    }

    // 레일 위에서는 원본, 꺼내면 파장별 이미지로 서서히 바뀐다. 회수되는 동안은 그대로 둔다.
    private void UpdateOriginal(float dt)
    {
        if (State == CardState.Returning) return;

        float target = State == CardState.OnRail && Config.railShowsOriginal ? 1f : 0f;
        originalAlpha = Mathf.MoveTowards(originalAlpha, target, dt / Mathf.Max(0.01f, Config.originalFadeSeconds));
    }

    private void UpdateGlide(float dt)
    {
        Vector2 pos = Rect.anchoredPosition + velocity * dt;
        velocity *= Mathf.Exp(-Config.glideDamping * dt);

        // 체험 영역 끝에 닿으면 살짝 튕겨 나온다
        Vector2 clamped = ClampToArea(pos);
        if (!Mathf.Approximately(clamped.x, pos.x)) velocity.x = -velocity.x * EdgeBounce;
        if (!Mathf.Approximately(clamped.y, pos.y)) velocity.y = -velocity.y * EdgeBounce;
        Rect.anchoredPosition = clamped;

        if (velocity.magnitude < GlideStopSpeed)
        {
            velocity = Vector2.zero;
            NudgeOutOfRail();
        }
    }

    // 레일 중심선 위쪽이면 위쪽 관람객(180°), 아래쪽이면 아래쪽 관람객(0°)을 향한다.
    private void UpdateFacing()
    {
        float dy = Rect.anchoredPosition.y - board.Rail.RailY;
        if (dy > facingHysteresis) targetAngle = 180f;
        else if (dy < -facingHysteresis) targetAngle = 0f;
    }

    // SmoothDamp의 smoothTime은 "대략 도달 시간"보다 짧게 잡아야 설정한 시간 안에 거의 도착한다.
    private static float SmoothTime(float seconds) => Mathf.Max(0.01f, seconds * 0.5f);

    #endregion
}

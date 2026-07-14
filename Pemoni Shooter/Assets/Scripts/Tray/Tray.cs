using System.Collections;
using System.Collections.Generic;
using AudioSystem;
using DG.Tweening;
using UnityEngine;

public class Tray : MonoBehaviour
{
    [Header("Grid")]
    public TrayType TrayType;
    public Vector2Int OriginCell;
    public int Layer;

    [Header("Color")]
    public TrayColor TrayColor;

    [Header("Cup Slots")]
    [Tooltip("Tự động thu thập tất cả CupSlot con nếu để trống")]
    [SerializeField] private List<CupSlot> _cupSlots = new();

    /// Số cốc khay có thể đựng = số cell trong shape
    public int Capacity => TrayShapeUtility.GetShape(TrayType)?.Length ?? 0;

    private int _filledCount;
    public bool IsTrayFull => _filledCount >= Capacity;

    // -------------------------------------------------------

    [Header("Runtime")]
    [SerializeField]
    private bool _isCovered;

    [Header("UI")]
    [SerializeField] private Sprite _originSprite;
    [SerializeField] private Sprite _hideSprite;
    [SerializeField] private Color _coveredColor = new(205f / 255f, 205f / 255f, 205f / 255f, 1f);

    [Header("Disappear")]
    [Tooltip("Thời gian chờ sau khi bay ra khỏi màn hình trước khi Destroy")]
    [SerializeField] private float _disappearDuration = 2f;

    public bool IsCovered
    {
        get => _isCovered;
        set
        {
            bool wasCovered = _isCovered;
            _isCovered = value;

            if (wasCovered && !_isCovered)
                PlayUncoverAnimation();

            UpdateVisual();
        }
    }

    private void PlayUncoverAnimation()
    {
        if (_animator != null && _animator.isActiveAndEnabled)
            _animator.SetTrigger(UncoverHash);
    }

    public static bool AnyTrayFlying { get; private set; }

    public bool CanClick =>
        !_isCovered &&
        !AnyTrayFlying &&
        !(GameManager.Instance?.IsInputBlocked ?? false) &&
        !(SkillManager.Instance?.IsGridSwapModeActive ?? false) &&
        !(SkillManager.Instance?.IsGridSwapping ?? false) &&
        !TableSlotManager.Instance.IsFull &&
        (TutorialManager.Instance == null || TutorialManager.Instance.CanClickTray(this));

    private SpriteRenderer _renderer;
    private Animator _animator;
    private TrayFlyAnim _flyAnim;

    private static readonly int UncoverHash = Animator.StringToHash("Uncover");

    // -------------------------------------------------------

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _flyAnim = GetComponent<TrayFlyAnim>();

        if (_renderer != null)
            _originSprite = _renderer.sprite;

        AutoCollectCupSlots();
    }

    private void Start()
    {
        UpdateVisual();
    }

    // -------------------------------------------------------
    // CupSlot management

    private void AutoCollectCupSlots()
    {
        if (_cupSlots.Count > 0) return; // Đã assign tay trong Inspector

        _cupSlots.Clear();
        foreach (Transform child in transform)
        {
            var slot = child.GetComponent<CupSlot>();
            if (slot != null)
                _cupSlots.Add(slot);
        }
    }

    /// <summary>
    /// Lấy CupSlot trống tiếp theo (theo thứ tự list).
    /// Trả về null nếu khay đã đầy.
    /// </summary>
    public CupSlot GetNextEmptyCupSlot()
    {
        foreach (var slot in _cupSlots)
        {
            if (!slot.IsOccupied)
                return slot;
        }
        return null;
    }

    /// <summary>
    /// Gọi sau khi Cup bay vào slot thành công.
    /// </summary>
    public void ReceiveCup(Cup cup)
    {
        _filledCount++;
        cup.transform.SetParent(transform); // Cup là con của Tray
        MissionManager.Instance?.AddProgress(MissionType.CollectCups, 1);


        if (IsTrayFull)
        {
            OnFullFilled();
            ComboManager.Instance.OnTrayFilled();
            MoneyManager.Instance.OnTrayCompleted(transform);
            SeasonPass.SeasonPassManager.Instance?.AddPoints(10);
        }
        //else
        //{
        //    ComboManager.Instance.OnNonFillingCupReceived();
        //}
    }

    /// <summary>
    /// Gọi khi khay đầy: giải phóng TableSlot, nhấc khay lên rồi bay ra
    /// ngoài màn hình bên phải, đợi <see cref="_disappearDuration"/> giây rồi Destroy.
    /// </summary>
    private void OnFullFilled()
    {
        Debug.Log($"[Tray] {name} đầy! Biến mất.");

        // Giải phóng TableSlot ngay để có thể đón Tray mới
        TableSlotManager.Instance.FreeSlotOf(this);
        AudioManager.Instance.PlaySFX("Done");
        // Tắt collider để không bị click trong lúc bay đi
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (_flyAnim != null)
        {
            _flyAnim.PlayDisappearAnim(() =>
            {
                StartCoroutine(DestroyAfterDelay(_disappearDuration));
            });
        }
        else
        {
            // Fallback nếu không có TrayFlyAnim: giữ hành vi scale cũ
            transform
                .DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InBack)
                .OnComplete(() => Destroy(gameObject));
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    // -------------------------------------------------------
    // Click

    private void OnMouseDown()
    {
        if (SkillManager.Instance != null && SkillManager.Instance.IsGridSwapModeActive)
        {
            Vector3 worldPos = Camera.main != null
                ? Camera.main.ScreenToWorldPoint(Input.mousePosition)
                : transform.position;

            if (SkillManager.Instance.TryHandleGridTrayClick(worldPos))
                return;
            return;
        }

        if (!CanClick) return;

        TableSlot slot = TableSlotManager.Instance.GetNextEmptySlot();
        if (slot == null) return;

        // Xóa khỏi grid ngay để RefreshCoveredState đúng
        GridMapManager.Instance.UnregisterTray(this);
        GridMapManager.Instance.RefreshCoveredState();
        AudioManager.Instance.PlaySFX("Box");
        AnyTrayFlying = true;

        //Đẩy layer lên cao để không bị tray khác che
        _renderer.sortingOrder = 14;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (_flyAnim != null)
        {
            _flyAnim.FlyToSlot(slot, () =>
            {
                TableSlotManager.Instance.OccupySlot(slot, this);
                AnyTrayFlying = false;

                // Thông báo CupQueue kiểm tra dispatch
                CupQueue.Instance.TryDispatchFront();
            });
        }
        else
        {
            transform.position = slot.WorldPosition;
            transform.rotation = slot.WorldRotation;
            TableSlotManager.Instance.OccupySlot(slot, this);
            AnyTrayFlying = false;
            CupQueue.Instance.TryDispatchFront();
        }

        TutorialManager.Instance?.OnTrayClicked(this);
        //Debug.Log($"Clicked on Tray at cell: {OriginCell} of type: {TrayType} color: {TrayColor}");
    }

    // -------------------------------------------------------
    // Visual

    public void UpdateVisual()
    {
        if (_renderer == null) return;

        _renderer.sortingOrder = Layer;

        if (IsCovered)
        {
            if (_hideSprite != null)
                _renderer.sprite = _hideSprite;
            _renderer.color = _coveredColor;
        }
        else
        {
            if (_originSprite != null)
                _renderer.sprite = _originSprite;
            _renderer.color = Color.white;
        }
    }

    /// <summary>Áp dụng lại sprite/sorting sau khi đổi layer hoặc trạng thái covered.</summary>
    public void ApplyGridVisual()
    {
        UpdateVisual();
    }

    private void OnValidate()
    {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        if (_originSprite == null && _renderer != null) _originSprite = _renderer.sprite;

        GridPositioner positioner = GetComponent<GridPositioner>();
        if (positioner != null)
        {
            positioner.UpdatePosition();
            UpdateVisual();
        }
    }
}
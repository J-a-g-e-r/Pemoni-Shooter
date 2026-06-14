using System.Collections.Generic;
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
    [SerializeField] private float _disappearDuration = 0.25f;

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

    public bool CanClick => !_isCovered && !AnyTrayFlying && !TableSlotManager.Instance.IsFull;

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

        if (IsTrayFull)
            OnFullFilled();
    }

    /// <summary>
    /// Gọi khi khay đầy: giải phóng TableSlot, scale về 0 rồi Destroy.
    /// </summary>
    private void OnFullFilled()
    {
        Debug.Log($"[Tray] {name} đầy! Biến mất.");

        // Giải phóng TableSlot ngay để có thể đón Tray mới
        TableSlotManager.Instance.FreeSlotOf(this);

        // DOTween scale về 0 rồi Destroy
        transform
            .DOScale(Vector3.zero, _disappearDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    // -------------------------------------------------------
    // Click

    private void OnMouseDown()
    {
        if (!CanClick) return;

        TableSlot slot = TableSlotManager.Instance.GetNextEmptySlot();
        if (slot == null) return;

        // Xóa khỏi grid ngay để RefreshCoveredState đúng
        GridMapManager.Instance.UnregisterTray(this);
        GridMapManager.Instance.RefreshCoveredState();

        AnyTrayFlying = true;

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

        Debug.Log($"Clicked on Tray at cell: {OriginCell} of type: {TrayType} color: {TrayColor}");
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
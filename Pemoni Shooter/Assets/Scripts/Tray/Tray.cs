using UnityEngine;

public class Tray : MonoBehaviour
{
    [Header("Grid")]
    public TrayType TrayType;
    public Vector2Int OriginCell;
    public int Layer;

    [Header("Runtime")]
    [SerializeField]
    private bool _isCovered;

    [Header("UI")]
    [SerializeField] private Sprite _originSprite; // Lưu lại sprite gốc ban đầu lúc Awake
    [SerializeField] private Sprite _hideSprite;
    [SerializeField] private Color _coveredColor = new(205f / 255f, 205f / 255f, 205f / 255f, 1f);

    public bool IsCovered
    {
        get => _isCovered;
        set
        {
            bool wasCovered = _isCovered;
            _isCovered = value;

            // Trigger animation khi tray được lộ ra (covered → uncovered)
            if (wasCovered && !_isCovered)
            {
                PlayUncoverAnimation();
            }

            UpdateVisual();
        }
    }

    private void PlayUncoverAnimation()
    {
        if (_animator != null && _animator.isActiveAndEnabled)
        {
            _animator.SetTrigger(UncoverHash);
        }
    }

    /// Tray đang bay thì không cho click tray khác
    public static bool AnyTrayFlying { get; private set; }

    public bool CanClick => !_isCovered && !AnyTrayFlying && !TableSlotManager.Instance.IsFull;

    private SpriteRenderer _renderer;
    private Animator _animator;
    private TrayFlyAnim _flyAnim;

    private static readonly int UncoverHash = Animator.StringToHash("Uncover");

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _flyAnim = GetComponent<TrayFlyAnim>();
        if (_renderer != null)
        {
            _originSprite = _renderer.sprite; // Lưu giữ sprite gốc
        }
    }

    private void Start()
    {
        UpdateVisual();
    }

    //private void Update()
    //{
    //    if (Input.GetMouseButtonDown(0))
    //        OnMouseDown();
    //}

    public void UpdateVisual()
    {
        if (_renderer == null)
            return;

        // ĐỒNG BỘ LAYER
        _renderer.sortingOrder = Layer;

        // CẬP NHẬT HÌNH ẢNH & MÀU SẮC
        if (IsCovered)
        {
            // Nếu có sprite ẩn riêng thì đổi, nếu không thì giữ nguyên sprite gốc nhưng đổi màu tối
            if (_hideSprite != null)
            {
                _renderer.sprite = _hideSprite;
            }

            // Đổi màu thành màu xám tối (giống như trong ảnh bạn cấu hình)
            _renderer.color = _coveredColor;

            //Color c = Color.white;
            //c.a = CanClick ? 1f : 0.5f;
            //_renderer.color = c;
        }
        else
        {
            // Khi không bị che: Trả lại sprite gốc và màu trắng nguyên bản
            if (_originSprite != null)
            {
                _renderer.sprite = _originSprite;
            }
            _renderer.color = Color.white;
        }
    }

    private void OnMouseDown()
    {
        if (!CanClick) return;

        // Lấy slot tiếp theo trên bàn
        TableSlot slot = TableSlotManager.Instance.GetNextEmptySlot();
        if (slot == null) return; // Bàn đầy

        // Xóa khỏi grid ngay lập tức để RefreshCoveredState chạy đúng
        GridMapManager.Instance.UnregisterTray(this);
        GridMapManager.Instance.RefreshCoveredState();

        // Lock click toàn bộ tray trong lúc bay
        AnyTrayFlying = true;

        // Disable collider để không bị click lại
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Bay vào slot
        if (_flyAnim != null)
        {
            _flyAnim.FlyToSlot(slot, () =>
            {
                TableSlotManager.Instance.OccupySlot(slot);
                AnyTrayFlying = false;
                //Destroy(gameObject);
            });
        }
        else
        {
            // Fallback nếu không có TrayFlyAnimation
            transform.position = slot.WorldPosition;
            transform.rotation = slot.WorldRotation;
            TableSlotManager.Instance.OccupySlot(slot);
            AnyTrayFlying = false;
            Destroy(gameObject);
        }

        Debug.Log("Clicked on Tray at cell: " + OriginCell + " of type: " + TrayType.ToString());
    }

    private void OnValidate()
    {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();

        // Đoạn này giúp trong Editor không bị mất Sprite khi bạn test OnValidate
        if (_originSprite == null && _renderer != null) _originSprite = _renderer.sprite;


        // Cập nhật vị trí trên editor
        GridPositioner positioner = GetComponent<GridPositioner>();
        if (positioner != null)
        {
            positioner.UpdatePosition();
            UpdateVisual();
        }
    }
}
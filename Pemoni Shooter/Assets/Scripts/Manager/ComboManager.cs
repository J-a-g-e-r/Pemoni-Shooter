using DG.Tweening;
using UnityEngine;

/// <summary>
/// Quản lý chuỗi combo khi các khay (Tray) được lấp đầy liên tiếp không bị ngắt quãng.
///
/// Cách dùng (gọi từ nơi khác, ví dụ Tray.cs / CupQueue.cs):
///   - Khi 1 cup bay vào khay làm khay ĐẦY (tray vừa full)  -> ComboManager.Instance.OnTrayFilled();
///   - Khi 1 cup bay vào khay nhưng KHÔNG làm khay đầy       -> ComboManager.Instance.OnNonFillingCupReceived();
///
/// Quy tắc combo:
///   2 khay liên tiếp -> Nice
///   3 khay liên tiếp -> Cool
///   4 khay liên tiếp -> Epic
///   5 khay liên tiếp (hoặc nhiều hơn) -> Awesome (giữ nguyên ở mức cao nhất)
///   Bất kỳ cup nào bay vào khay mà không làm khay đầy -> reset combo về 0
/// </summary>
public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [Header("Combo Text Objects")]
    [Tooltip("GameObject hiển thị chữ 'Nice' (combo = 2)")]
    [SerializeField] private GameObject _niceObject;
    [Tooltip("GameObject hiển thị chữ 'Cool' (combo = 3)")]
    [SerializeField] private GameObject _coolObject;
    [Tooltip("GameObject hiển thị chữ 'Epic' (combo = 4)")]
    [SerializeField] private GameObject _epicObject;
    [Tooltip("GameObject hiển thị chữ 'Awesome' (combo >= 5)")]
    [SerializeField] private GameObject _awesomeObject;

    [Header("Animation")]
    [Tooltip("Thời gian zoom từ nhỏ (0) lên to (1)")]
    [SerializeField] private float _popInDuration = 0.3f;
    [Tooltip("Thời gian giữ nguyên kích thước sau khi zoom to, trước khi tự ẩn (nếu không có combo mới thay thế)")]
    [SerializeField] private float _holdDuration = 1f;
    [Tooltip("Thời gian zoom nhỏ lại trước khi ẩn, khi không có combo mới thay thế")]
    [SerializeField] private float _popOutDuration = 0.2f;
    [SerializeField] private Ease _popInEase = Ease.OutBack;
    [SerializeField] private Ease _popOutEase = Ease.InBack;

    // -------------------------------------------------------

    private int _comboCount;

    private GameObject _activeObject;
    private Sequence _activeSeq;

    private const int MinComboToShow = 2;
    private const int MaxComboLevel = 5; // 5 trở lên vẫn là Awesome

    // -------------------------------------------------------

    private void Awake()
    {
        Instance = this;

        // Đảm bảo tất cả text combo đều tắt và scale 0 lúc khởi đầu
        InitHidden(_niceObject);
        InitHidden(_coolObject);
        InitHidden(_epicObject);
        InitHidden(_awesomeObject);
    }

    private void InitHidden(GameObject go)
    {
        if (go == null) return;
        go.transform.DOKill();
        go.transform.localScale = Vector3.zero;
        go.SetActive(false);
    }

    // -------------------------------------------------------
    // Public API

    /// <summary>
    /// Gọi khi 1 khay vừa được lấp đầy (cup cuối cùng khiến khay full).
    /// Tăng combo lên 1 và hiển thị text tương ứng nếu combo >= 2.
    /// </summary>
    public void OnTrayFilled()
    {
        _comboCount++;

        if (_comboCount >= MinComboToShow)
        {
            GameObject target = GetComboObject(_comboCount);
            ShowCombo(target);
        }
    }

    /// <summary>
    /// Gọi khi có 1 cup bay vào khay nhưng KHÔNG làm khay đầy.
    /// Reset chuỗi combo về 0 (không hiển thị gì thêm cho tới lần fill tiếp theo).
    /// </summary>
    public void OnNonFillingCupReceived()
    {
        _comboCount = 0;
    }

    /// <summary>
    /// Reset hoàn toàn combo (ví dụ khi bắt đầu màn mới), kèm ẩn text đang hiện (nếu có).
    /// </summary>
    public void ResetCombo()
    {
        _comboCount = 0;
        HideActiveImmediate();
    }

    // -------------------------------------------------------
    // Internal

    private GameObject GetComboObject(int comboCount)
    {
        int level = Mathf.Min(comboCount, MaxComboLevel);
        return level switch
        {
            2 => _niceObject,
            3 => _coolObject,
            4 => _epicObject,
            _ => _awesomeObject, // >= 5
        };
    }

    /// <summary>
    /// Hiển thị 1 combo text bằng animation zoom in. Nếu đang có text khác hiển thị,
    /// ẩn ngay (không zoom out) rồi hiện text mới đè lên.
    /// </summary>
    private void ShowCombo(GameObject target)
    {
        if (target == null) return;

        // Huỷ animation cũ đang chạy (nếu có)
        _activeSeq?.Kill();

        // Nếu đang có object khác hiển thị -> ẩn ngay lập tức (không zoom out)
        // để nhường chỗ cho combo mới thay thế.
        if (_activeObject != null && _activeObject != target)
        {
            _activeObject.transform.DOKill();
            _activeObject.transform.localScale = Vector3.zero;
            _activeObject.SetActive(false);
        }

        _activeObject = target;

        target.transform.DOKill();
        target.SetActive(true);
        target.transform.localScale = Vector3.zero;

        _activeSeq = DOTween.Sequence();

        // Phase 1: Zoom from nhỏ lên to
        _activeSeq.Append(
            target.transform.DOScale(Vector3.one, _popInDuration)
                  .SetEase(_popInEase)
        );

        // Phase 2: Giữ nguyên trong _holdDuration giây
        _activeSeq.AppendInterval(_holdDuration);

        // Phase 3: Zoom nhỏ lại rồi ẩn (chỉ chạy nếu không có combo mới thay thế trước đó)
        _activeSeq.Append(
            target.transform.DOScale(Vector3.zero, _popOutDuration)
                  .SetEase(_popOutEase)
        );

        _activeSeq.OnComplete(() =>
        {
            target.SetActive(false);
            if (_activeObject == target)
                _activeObject = null;
            _activeSeq = null;
        });
    }

    /// <summary>
    /// Ẩn ngay text combo đang hiển thị (nếu có), không animation.
    /// </summary>
    private void HideActiveImmediate()
    {
        _activeSeq?.Kill();
        _activeSeq = null;

        if (_activeObject != null)
        {
            _activeObject.transform.DOKill();
            _activeObject.transform.localScale = Vector3.zero;
            _activeObject.SetActive(false);
            _activeObject = null;
        }
    }

    private void OnDestroy()
    {
        _activeSeq?.Kill();
    }
}
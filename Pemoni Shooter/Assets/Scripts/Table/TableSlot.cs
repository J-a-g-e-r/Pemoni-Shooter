using DG.Tweening;
using UnityEngine;

/// <summary>
/// Gắn vào từng GameObject slot con của Table.
/// </summary>
public class TableSlot : MonoBehaviour
{
    [HideInInspector] public bool IsOccupied;

    public Vector3 WorldPosition => transform.position;
    public Quaternion WorldRotation => transform.rotation;

    [Header("Warning")]
    [Tooltip("Kéo child Warning vào đây")]
    [SerializeField] private GameObject _warningObject;
    [SerializeField] private float _warningDuration = 2f;

    private Tween _warningTween;

    // -------------------------------------------------------

    /// <summary>Hiển thị warning trong <see cref="_warningDuration"/> giây rồi tự tắt.</summary>
    public void ShowWarning()
    {
        if (_warningObject == null) return;

        // Huỷ timer cũ nếu đang chạy
        _warningTween?.Kill();

        _warningObject.SetActive(true);

        _warningTween = DOVirtual.DelayedCall(_warningDuration, () =>
        {
            if (_warningObject != null)
                _warningObject.SetActive(false);

            _warningTween = null;
        });
    }

    public void HideWarning()
    {
        _warningTween?.Kill();
        _warningTween = null;

        if (_warningObject != null)
            _warningObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _warningTween?.Kill();
    }
}
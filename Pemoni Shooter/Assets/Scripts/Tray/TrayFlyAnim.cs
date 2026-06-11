using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn vào cùng GameObject với Tray.
/// Xử lý animation bay vào slot bằng DOTween:
///   Phase 1 – Xoay nghiêng ~30° theo trục Z (tilt out)
///   Phase 2 – Bay thẳng tới slot
///   Phase 3 – Xoay về góc của slot (snap rotation)
/// </summary>
public class TrayFlyAnim : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float _tiltDuration = 0.12f;
    [SerializeField] private float _flyDuration = 0.30f;
    [SerializeField] private float _snapDuration = 0.10f;

    [Header("Tilt")]
    [SerializeField] private float _tiltAngleZ = 30f;

    [Header("Easing")]
    [SerializeField] private Ease _flyEase = Ease.InOutQuad;
    [SerializeField] private Ease _tiltEase = Ease.OutQuad;
    [SerializeField] private Ease _snapEase = Ease.OutQuad;

    // -------------------------------------------------------

    private Sequence _seq;

    /// <summary>
    /// Bắt đầu animation bay tới <paramref name="slot"/>.
    /// <paramref name="onComplete"/> được gọi sau khi animation kết thúc.
    /// </summary>
    public void FlyToSlot(TableSlot slot, System.Action onComplete = null)
    {
        // Huỷ sequence cũ nếu có
        _seq?.Kill();

        Vector3 startRot = transform.eulerAngles;
        Vector3 tiltRot = startRot + new Vector3(0f, 0f, _tiltAngleZ);
        Vector3 targetRot = slot.WorldRotation.eulerAngles;
        Vector3 targetPos = slot.WorldPosition;

        _seq = DOTween.Sequence();

        // Phase 1: Tilt ra
        _seq.Append(
            transform.DORotate(tiltRot, _tiltDuration)
                     .SetEase(_tiltEase)
        );

        // Phase 2: Bay thẳng (rotation giữ nguyên tư thế nghiêng)
        _seq.Append(
            transform.DOMove(targetPos, _flyDuration)
                     .SetEase(_flyEase)
        );

        // Phase 3: Snap về rotation của slot
        _seq.Append(
            transform.DORotate(targetRot, _snapDuration)
                     .SetEase(_snapEase)
        );

        _seq.OnComplete(() =>
        {
            _seq = null;
            onComplete?.Invoke();
        });
    }

    private void OnDestroy()
    {
        _seq?.Kill();
    }
}


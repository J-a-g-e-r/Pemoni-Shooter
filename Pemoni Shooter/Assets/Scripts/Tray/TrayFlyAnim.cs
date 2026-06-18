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

    [Header("Disappear (Full Tray)")]
    [Tooltip("Thời gian nhấc khay lên trước khi bay sang phải")]
    [SerializeField] private float _liftDuration = 0.2f;
    [Tooltip("Khoảng cách nhấc lên theo trục Y")]
    [SerializeField] private float _liftHeight = 0.5f;
    [Tooltip("Thời gian bay ra ngoài màn hình bên phải")]
    [SerializeField] private float _exitDuration = 0.45f;
    [Tooltip("Khoảng dư thêm ra ngoài biên phải màn hình (world units)")]
    [SerializeField] private float _exitExtraMargin = 2f;
    [SerializeField] private Ease _liftEase = Ease.OutQuad;
    [SerializeField] private Ease _exitEase = Ease.InQuad;

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

    /// <summary>
    /// Animation khi khay đầy cốc: nhấc khay lên rồi bay ra ngoài
    /// màn hình về phía bên phải (giữ nguyên rotation).
    /// <paramref name="onComplete"/> được gọi sau khi animation kết thúc
    /// (KHÔNG bao gồm thời gian chờ trước khi Destroy, việc đó do nơi gọi xử lý).
    /// </summary>
    public void PlayDisappearAnim(System.Action onComplete = null)
    {
        _seq?.Kill();

        Vector3 liftPos = transform.position + new Vector3(0f, _liftHeight, 0f);
        Vector3 exitPos = liftPos + new Vector3(GetExitDistanceX(), 0f, 0f);

        _seq = DOTween.Sequence();

        // Phase 1: Nhấc khay lên
        _seq.Append(
            transform.DOMove(liftPos, _liftDuration)
                     .SetEase(_liftEase)
        );

        // Phase 2: Bay ra ngoài màn hình bên phải (giữ nguyên rotation)
        _seq.Append(
            transform.DOMove(exitPos, _exitDuration)
                     .SetEase(_exitEase)
        );

        _seq.OnComplete(() =>
        {
            _seq = null;
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Tính khoảng cách theo trục X cần di chuyển để ra khỏi biên phải
    /// màn hình (theo camera chính), cộng thêm margin dự phòng.
    /// </summary>
    private float GetExitDistanceX()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return _exitExtraMargin + 5f; // fallback nếu không tìm thấy camera

        // Biên phải màn hình tại độ sâu (z) hiện tại của tray, theo world space
        Vector3 viewportRightEdge = cam.ViewportToWorldPoint(
            new Vector3(1f, 0.5f, cam.WorldToViewportPoint(transform.position).z));

        float distance = (viewportRightEdge.x - transform.position.x) + _exitExtraMargin;
        return Mathf.Max(distance, 0.1f);
    }

    private void OnDestroy()
    {
        _seq?.Kill();
    }
}
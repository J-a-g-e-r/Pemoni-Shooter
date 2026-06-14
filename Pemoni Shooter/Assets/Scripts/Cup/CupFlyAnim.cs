using DG.Tweening;
using UnityEngine;

/// <summary>
/// Gắn vào cùng GameObject với Cup.
/// Animation bay vào CupSlot bằng DOTween:
///   Phase 1 – Bật lên theo arc (liftUp)
///   Phase 2 – Bay ngang tới vị trí trên slot
///   Phase 3 – Rơi xuống vào slot + scale bounce nhẹ
/// </summary>
public class CupFlyAnim : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float _liftDuration = 0.15f;
    [SerializeField] private float _flyDuration = 0.28f;
    [SerializeField] private float _dropDuration = 0.14f;

    [Header("Arc")]
    [SerializeField] private float _arcHeight = 1.8f;   // độ cao bật lên

    [Header("Bounce")]
    [SerializeField] private float _squishY = 0.75f; // scale Y lúc đổ xuống
    [SerializeField] private float _squishX = 1.25f; // scale X bù lại
    [SerializeField] private float _bounceDuration = 0.12f;

    [Header("Easing")]
    [SerializeField] private Ease _liftEase = Ease.OutQuad;
    [SerializeField] private Ease _flyEase = Ease.InOutQuad;
    [SerializeField] private Ease _dropEase = Ease.InQuad;

    // -------------------------------------------------------

    private Sequence _seq;

    /// <summary>
    /// Bắt đầu animation bay tới <paramref name="slot"/>.
    /// </summary>
    public void FlyToSlot(CupSlot slot, System.Action onComplete = null)
    {
        _seq?.Kill();

        Vector3 startPos = transform.position;
        Vector3 targetPos = slot.WorldPosition;

        // Điểm đỉnh arc: giữa đường bay, nâng lên _arcHeight
        Vector3 peakPos = new Vector3(
            (startPos.x + targetPos.x) * 0.5f,
            Mathf.Max(startPos.y, targetPos.y) + _arcHeight,
            startPos.z);

        // Điểm hover: ngay phía trên slot, trước khi rơi xuống
        Vector3 hoverPos = new Vector3(
            targetPos.x,
            targetPos.y + _arcHeight * 0.5f,
            targetPos.z);

        Vector3 originalScale = transform.localScale;

        _seq = DOTween.Sequence();

        // Phase 1: Bật lên tới đỉnh arc
        _seq.Append(
            transform.DOMove(peakPos, _liftDuration)
                     .SetEase(_liftEase)
        );

        // Phase 2: Bay ngang tới hover point
        _seq.Append(
            transform.DOMove(hoverPos, _flyDuration)
                     .SetEase(_flyEase)
        );

        // Phase 3a: Rơi xuống slot
        _seq.Append(
            transform.DOMove(targetPos, _dropDuration)
                     .SetEase(_dropEase)
        );

        // Phase 3b: Squish bounce (cùng lúc với drop)
        _seq.Join(
            transform.DOScaleY(_squishY * originalScale.y, _dropDuration)
                     .SetEase(_dropEase)
        );
        _seq.Join(
            transform.DOScaleX(_squishX * originalScale.x, _dropDuration)
                     .SetEase(_dropEase)
        );

        // Phase 4: Trả lại scale gốc
        _seq.Append(
            transform.DOScale(originalScale, _bounceDuration)
                     .SetEase(Ease.OutElastic)
        );

        // Snap rotation về slot
        _seq.Join(
            transform.DORotateQuaternion(slot.WorldRotation, _bounceDuration)
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
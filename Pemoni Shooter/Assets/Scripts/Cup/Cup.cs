using UnityEngine;

/// <summary>
/// Gắn vào từng Cup prefab.
/// Lưu màu sắc và điều phối animation bay vào CupSlot của Tray.
/// </summary>
[RequireComponent(typeof(CupFlyAnim))]
public class Cup : MonoBehaviour
{
    [Header("Data")]
    public TrayColor Color;

    private CupFlyAnim _flyAnim;

    private void Awake()
    {
        _flyAnim = GetComponent<CupFlyAnim>();
    }

    /// <summary>
    /// Bắt đầu animation bay vào <paramref name="slot"/>.
    /// <paramref name="onComplete"/> được gọi sau khi animation kết thúc.
    /// </summary>
    public void FlyToSlot(CupSlot slot, System.Action onComplete = null)
    {
        if (_flyAnim != null)
        {
            _flyAnim.FlyToSlot(slot, onComplete);
        }
        else
        {
            // Fallback không có anim
            transform.position = slot.WorldPosition;
            transform.rotation = slot.WorldRotation;
            onComplete?.Invoke();
        }
    }
}
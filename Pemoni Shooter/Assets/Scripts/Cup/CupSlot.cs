using UnityEngine;

/// <summary>
/// Gắn vào từng GameObject slot con của Tray prefab.
/// Mỗi slot đại diện cho một vị trí đựng cốc bên trong khay.
/// </summary>
public class CupSlot : MonoBehaviour
{
    [HideInInspector] public bool IsOccupied;

    /// Vị trí thế giới mà Cup sẽ snap tới
    public Vector3 WorldPosition => transform.position;

    /// Rotation của slot (Cup sẽ khớp khi đã tới nơi)
    public Quaternion WorldRotation => transform.rotation;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? new Color(1f, 0.3f, 0.3f, 0.8f) : new Color(0.3f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.08f);
    }
#endif
}
using UnityEngine;

/// <summary>
/// Gắn vào từng GameObject slot con của Table.
/// Slot tự expose vị trí + rotation để Tray bay vào.
/// </summary>
public class TableSlot : MonoBehaviour
{
    [HideInInspector] public bool IsOccupied;

    /// Vị trí thế giới mà Tray sẽ snap tới
    public Vector3 WorldPosition => transform.position;

    /// Góc của slot (Tray sẽ xoay theo góc này khi đã tới nơi)
    public Quaternion WorldRotation => transform.rotation;
}
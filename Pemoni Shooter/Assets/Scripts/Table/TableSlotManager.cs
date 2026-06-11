using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn vào GameObject Table.
/// Tự động tìm tất cả TableSlot con và quản lý thứ tự điền từ trái sang phải.
/// </summary>
public class TableSlotManager : MonoBehaviour
{
    public static TableSlotManager Instance { get; private set; }

    [Header("Slots")]
    [Tooltip("Để trống thì tự động lấy tất cả TableSlot con theo thứ tự từ trái sang phải")]
    [SerializeField] private List<TableSlot> _slots = new();

    // Số slot hiện đang bị chiếm
    private int _occupiedCount;

    public bool IsFull => _occupiedCount >= _slots.Count;

    // -------------------------------------------------------

    private void Awake()
    {
        Instance = this;
        AutoCollectSlots();
    }

    /// Tự động thu thập và sắp xếp slot theo vị trí X (trái → phải)
    private void AutoCollectSlots()
    {
        if (_slots.Count > 0) return; // Đã assign tay trong Inspector thì thôi

        _slots.Clear();
        foreach (Transform child in transform)
        {
            var slot = child.GetComponent<TableSlot>();
            if (slot != null)
                _slots.Add(slot);
        }

        // Sắp xếp trái → phải theo worldPosition.x
        _slots.Sort((a, b) => a.WorldPosition.x.CompareTo(b.WorldPosition.x));
    }

    // -------------------------------------------------------

    /// <summary>
    /// Lấy slot trống tiếp theo (trái → phải).
    /// Trả về null nếu đã đầy.
    /// </summary>
    public TableSlot GetNextEmptySlot()
    {
        foreach (var slot in _slots)
        {
            if (!slot.IsOccupied)
                return slot;
        }
        return null;
    }

    /// <summary>
    /// Đánh dấu slot là đã có Tray.
    /// Gọi sau khi animation bay vào hoàn tất.
    /// </summary>
    public void OccupySlot(TableSlot slot)
    {
        slot.IsOccupied = true;
        _occupiedCount++;
    }

    // -------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        AutoCollectSlots();
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] == null) continue;
            Gizmos.color = _slots[i].IsOccupied ? Color.red : Color.green;
            Gizmos.DrawWireSphere(_slots[i].WorldPosition, 0.15f);
            UnityEditor.Handles.Label(
                _slots[i].WorldPosition + Vector3.up * 0.25f,
                $"Slot {i}\n{(_slots[i].IsOccupied ? "OCCUPIED" : "FREE")}");
        }
    }
#endif
}
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn vào GameObject Table.
/// Quản lý các TableSlot và Tray đang ngồi trên bàn.
/// Hiển thị warning ở slot cuối cùng còn trống.
/// </summary>
public class TableSlotManager : MonoBehaviour
{
    public static TableSlotManager Instance { get; private set; }

    [Header("Slots")]
    [Tooltip("Để trống thì tự động lấy tất cả TableSlot con theo thứ tự từ trái sang phải")]
    [SerializeField] private List<TableSlot> _slots = new();

    private readonly Dictionary<TableSlot, Tray> _slotToTray = new();

    private int _occupiedCount;
    public bool IsFull => _occupiedCount >= _slots.Count;

    // Số slot trống hiện tại
    public int EmptyCount => _slots.Count - _occupiedCount;

    // -------------------------------------------------------

    private void Awake()
    {
        Instance = this;
        AutoCollectSlots();
    }

    private void AutoCollectSlots()
    {
        if (_slots.Count > 0) return;

        _slots.Clear();
        foreach (Transform child in transform)
        {
            var slot = child.GetComponent<TableSlot>();
            if (slot != null)
                _slots.Add(slot);
        }

        _slots.Sort((a, b) => a.WorldPosition.x.CompareTo(b.WorldPosition.x));
    }

    // -------------------------------------------------------

    public TableSlot GetNextEmptySlot()
    {
        foreach (var slot in _slots)
        {
            if (!slot.IsOccupied)
                return slot;
        }
        return null;
    }

    public void OccupySlot(TableSlot slot, Tray tray)
    {
        slot.IsOccupied = true;
        _slotToTray[slot] = tray;
        _occupiedCount++;

        // Sau khi chiếm slot, kiểm tra còn lại bao nhiêu slot trống
        CheckLastSlotWarning();
    }

    public void FreeSlotOf(Tray tray)
    {
        foreach (var slot in _slots)
        {
            if (_slotToTray.TryGetValue(slot, out Tray occupant) && occupant == tray)
            {
                slot.IsOccupied = false;
                _slotToTray.Remove(slot);
                _occupiedCount--;

                // Slot vừa giải phóng → tắt warning ở slot đó (nếu có)
                slot.HideWarning();
                return;
            }
        }
    }

    // -------------------------------------------------------
    // Warning logic

    /// <summary>
    /// Nếu chỉ còn đúng 1 slot trống → hiện warning ở slot đó trong 2 giây.
    /// </summary>
    private void CheckLastSlotWarning()
    {
        if (EmptyCount != 1) return;

        // Tìm slot trống duy nhất còn lại
        foreach (var slot in _slots)
        {
            if (!slot.IsOccupied)
            {
                slot.ShowWarning();
                return;
            }
        }
    }

    // -------------------------------------------------------

    public Tray GetTrayByColor(TrayColor color)
    {
        foreach (var slot in _slots)
        {
            if (!slot.IsOccupied) continue;
            if (!_slotToTray.TryGetValue(slot, out Tray tray)) continue;
            if (tray == null) continue;
            if (tray.TrayColor != color) continue;
            if (tray.GetNextEmptyCupSlot() == null) continue;
            return tray;
        }
        return null;
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
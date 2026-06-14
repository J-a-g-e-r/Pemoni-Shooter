using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CupQueue : MonoBehaviour
{
    public static CupQueue Instance { get; private set; }

    [Header("Conveyor Layout")]
    [Tooltip("slot[0] = đầu ra (gần bàn), slot[N-1] = cuối hàng (bên trong)")]
    [SerializeField] private List<Transform> _slotPositions = new();

    [Header("Shift Animation")]
    [SerializeField] private float _shiftDuration = 0.12f;
    [SerializeField] private Ease _shiftEase = Ease.OutQuad;

    [Header("Intro Animation")]
    [SerializeField] private float _introStagger = 0.08f;
    [SerializeField] private float _introDuration = 0.14f;
    [SerializeField] private Ease _introEase = Ease.OutCubic;

    [Header("Dispatch")]
    [SerializeField] private float _dispatchStagger = 0.15f;

    // -------------------------------------------------------

    private Queue<Cup> _pending = new();
    private List<Cup> _visible = new();   // [0]=đầu ra, [N-1]=cuối hàng
    private int _flyingCount = 0;
    private bool _introPlaying = false;
    private bool _dispatchScheduled = false;

    public int TotalRemaining => _pending.Count + _visible.Count + _flyingCount;

    private void Awake() => Instance = this;

    // -------------------------------------------------------

    public void Initialize(List<Cup> allCups)
    {
        _pending.Clear();
        _visible.Clear();
        _flyingCount = 0;
        _introPlaying = false;
        _dispatchScheduled = false;

        foreach (var cup in allCups)
            _pending.Enqueue(cup);

        PlayIntro();
    }

    // -------------------------------------------------------
    // Intro — băng chuyền đúng chiều:
    //
    // Mỗi step: 1 cup mới xuất hiện tại slot CUỐI (slot[last]),
    // toàn bộ hàng shift tiến về slot[0] (đầu ra).
    //
    // Step 0: cup[0] tại slot[last]          → hàng: [..., cup0]
    // Step 1: cup[1] tại slot[last]          → shift: cup0→slot[last-1], cup1→slot[last]
    // Step 2: cup[2] tại slot[last]          → shift: cup0→slot[last-2], cup1→slot[last-1], cup2→slot[last]
    // ...
    // Kết thúc: cup[0] ở slot[0] (đầu ra), cup[N-1] ở slot[last] (cuối hàng)

    private void PlayIntro()
    {
        _introPlaying = true;

        int totalSlots = _slotPositions.Count;
        int count = Mathf.Min(totalSlots, _pending.Count);
        int lastSlot = totalSlots - 1;

        // Dequeue batch, ẩn hết
        var batch = new List<Cup>(count);
        for (int i = 0; i < count; i++)
        {
            Cup c = _pending.Dequeue();
            c.gameObject.SetActive(false);
            batch.Add(c);
        }

        // batch[0] = cup sẽ ở đầu ra khi xong → cần vào trước nhất
        // batch[count-1] = cup sẽ ở cuối hàng → vào sau cùng
        //
        // _visible được build từ đầu ra đến cuối hàng:
        //   _visible[0]       = cup đầu ra   = batch[0]
        //   _visible[count-1] = cup cuối hàng = batch[count-1]
        //
        // Step s: thêm batch[s] vào _visible (ADD vào cuối)
        //   → _visible[s] = batch[s]
        //   → cup này hiện ở slot[lastSlot] (cuối hàng)
        //   → các cup trước shift: _visible[i] → slot[lastSlot - (s - i)]
        //                                       = slot[lastSlot - s + i]

        for (int step = 0; step < count; step++)
        {
            int s = step;

            DOVirtual.DelayedCall(s * _introStagger, () =>
            {
                // Cup mới luôn xuất hiện tại slot cuối
                Cup newCup = batch[s];
                newCup.transform.position = _slotPositions[lastSlot].position;
                newCup.transform.rotation = _slotPositions[lastSlot].rotation;
                newCup.gameObject.SetActive(true);
                _visible.Add(newCup); // _visible[s] = newCup

                // Shift: sau step s, có s+1 cup trong _visible
                // _visible[i] phải về slot[lastSlot - s + i]
                //   i=s (cup mới)   → slot[lastSlot]         ✓ đã đúng vị trí
                //   i=s-1           → slot[lastSlot - 1]     shift lên 1
                //   i=0 (cup cũ nhất) → slot[lastSlot - s]   shift ra gần đầu ra
                for (int i = 0; i <= s; i++)
                {
                    int targetSlot = lastSlot - s + i;

                    // slot[0] = đầu ra = sorting order cao nhất (hiện trên cùng)
                    SetSortingOrder(_visible[i], totalSlots - targetSlot);

                    _visible[i].transform.DOKill(false);
                    _visible[i].transform
                        .DOMove(_slotPositions[targetSlot].position, _introDuration)
                        .SetEase(_introEase);
                }
            });
        }

        float totalTime = (count - 1) * _introStagger + _introDuration + 0.05f;
        DOVirtual.DelayedCall(totalTime, () => { _introPlaying = false; });
    }

    // -------------------------------------------------------
    // Vị trí spawn phía sau slot cuối (dùng khi ShiftQueue)

    private Vector3 GetSpawnPositionBehind()
    {
        int last = _slotPositions.Count - 1;
        if (_slotPositions.Count >= 2)
        {
            // Hướng từ đầu ra (slot[0]) đến cuối hàng (slot[last])
            Vector3 dir = (_slotPositions[last].position - _slotPositions[0].position).normalized;
            return _slotPositions[last].position + dir * 0.8f;
        }
        return _slotPositions[last].position + Vector3.up * 0.8f;
    }

    // -------------------------------------------------------

    public void TryDispatchFront()
    {
        if (_introPlaying) return;
        DispatchNext(0);
    }

    private void DispatchNext(float delay)
    {
        if (_dispatchScheduled) return;

        if (delay > 0f)
        {
            _dispatchScheduled = true;
            DOVirtual.DelayedCall(delay, () =>
            {
                _dispatchScheduled = false;
                DispatchOne();
            });
        }
        else
        {
            DispatchOne();
        }
    }

    private void DispatchOne()
    {
        if (_visible.Count == 0) return;

        // _visible[0] = cup đầu ra
        Cup frontCup = _visible[0];

        Tray targetTray = TableSlotManager.Instance.GetTrayByColor(frontCup.Color);
        if (targetTray == null) return;

        CupSlot cupSlot = targetTray.GetNextEmptyCupSlot();
        if (cupSlot == null) return;

        cupSlot.IsOccupied = true;
        _visible.RemoveAt(0);
        _flyingCount++;

        Cup cup = frontCup;
        Tray tray = targetTray;
        CupSlot slot = cupSlot;

        cup.FlyToSlot(slot, () =>
        {
            _flyingCount--;
            tray.ReceiveCup(cup);
            CheckLevelComplete();
        });

        ShiftQueue();
        DispatchNext(_dispatchStagger);
    }

    // -------------------------------------------------------
    // Shift sau dispatch: cup mới vào từ cuối hàng, toàn bộ tiến về slot[0]

    private void ShiftQueue()
    {
        // Thêm cup mới vào CUỐI _visible (cuối hàng)
        if (_visible.Count < _slotPositions.Count && _pending.Count > 0)
        {
            Cup cup = _pending.Dequeue();
            cup.transform.position = GetSpawnPositionBehind();
            cup.transform.rotation = _slotPositions[_slotPositions.Count - 1].rotation;
            cup.gameObject.SetActive(true);
            _visible.Add(cup);
        }

        // Tween: _visible[i] → slot[i]
        // slot[0]=đầu ra, slot[N-1]=cuối hàng
        for (int i = 0; i < _visible.Count; i++)
        {
            SetSortingOrder(_visible[i], _slotPositions.Count - i);
            _visible[i].transform.DOKill(false);
            _visible[i].transform
                .DOMove(_slotPositions[i].position, _shiftDuration)
                .SetEase(_shiftEase);
        }
    }

    // -------------------------------------------------------

    private void SetSortingOrder(Cup cup, int order)
    {
        var sr = cup.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = order;
    }

    private void CheckLevelComplete()
    {
        if (TotalRemaining == 0)
        {
            Debug.Log("[CupQueue] Level hoàn thành!");
        }
    }
}
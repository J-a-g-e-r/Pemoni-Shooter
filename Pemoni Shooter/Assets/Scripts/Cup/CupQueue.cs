using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using System.Linq;
using AudioSystem;

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
    private List<Cup> _visible = new();
    private int _flyingCount = 0;
    private bool _introPlaying = false;
    private bool _dispatchScheduled = false;

    public int TotalRemaining => _pending.Count + _visible.Count + _flyingCount;

    /// Trả về cup đầu hàng (để GameManager kiểm tra lose), null nếu hàng rỗng
    public Cup GetFrontCup() => _visible.Count > 0 ? _visible[0] : null;

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

        // Cập nhật UI ngay khi biết tổng số cốc
        GameManager.Instance.UpdateCupLeftUI(TotalRemaining);

        PlayIntro();
    }

    // -------------------------------------------------------
    // Intro — băng chuyền đúng chiều:
    // Cup mới xuất hiện tại slot[last], đẩy cả hàng tiến về slot[0]

    private void PlayIntro()
    {
        _introPlaying = true;

        int totalSlots = _slotPositions.Count;
        int count = Mathf.Min(totalSlots, _pending.Count);
        int lastSlot = totalSlots - 1;

        var batch = new List<Cup>(count);
        for (int i = 0; i < count; i++)
        {
            Cup c = _pending.Dequeue();
            c.gameObject.SetActive(false);
            batch.Add(c);
        }

        for (int step = 0; step < count; step++)
        {
            int s = step;

            DOVirtual.DelayedCall(s * _introStagger, () =>
            {
                Cup newCup = batch[s];
                newCup.transform.position = _slotPositions[lastSlot].position;
                newCup.transform.rotation = _slotPositions[lastSlot].rotation;
                newCup.gameObject.SetActive(true);
                _visible.Add(newCup);

                for (int i = 0; i <= s; i++)
                {
                    int targetSlot = lastSlot - s + i;
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

    private Vector3 GetSpawnPositionBehind()
    {
        int last = _slotPositions.Count - 1;
        if (_slotPositions.Count >= 2)
        {
            Vector3 dir = (_slotPositions[last].position - _slotPositions[0].position).normalized;
            return _slotPositions[last].position + dir * 0.8f;
        }
        return _slotPositions[last].position + Vector3.up * 0.8f;
    }

    // -------------------------------------------------------
    // Dispatch

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

        Cup frontCup = _visible[0];

        Tray targetTray = TableSlotManager.Instance.GetTrayByColor(frontCup.Color);
        if (targetTray == null)
        {
            // Không match → kiểm tra thua
            GameManager.Instance.CheckLose();
            ComboManager.Instance.OnNonFillingCupReceived();
            return;
        }

        CupSlot cupSlot = targetTray.GetNextEmptyCupSlot();
        if (cupSlot == null)
        {
            GameManager.Instance.CheckLose();
            return;
        }

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

            // Cập nhật UI sau mỗi cup vào khay
            GameManager.Instance.UpdateCupLeftUI(TotalRemaining);

            // Kiểm tra thắng
            if (TotalRemaining == 0)
            {
                StartCoroutine(WaitForCheckWin());
                //GameManager.Instance.OnWin();
                return;
            }

            GameManager.Instance.CheckLose();
        });
        AudioManager.Instance.PlaySFX("CupIn");
        ShiftQueue();
        DispatchNext(_dispatchStagger);
    }

    // -------------------------------------------------------

    private IEnumerator WaitForCheckWin()
    {
        yield return new WaitForSeconds(1f);
        GameManager.Instance.OnWin();
    }

    private void ShiftQueue()
    {
        if (_visible.Count < _slotPositions.Count && _pending.Count > 0)
        {
            Cup cup = _pending.Dequeue();
            cup.transform.position = GetSpawnPositionBehind();
            cup.transform.rotation = _slotPositions[_slotPositions.Count - 1].rotation;
            cup.gameObject.SetActive(true);
            _visible.Add(cup);
        }

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


    //public void SortNextCups(int count = 50)
    //{
    //    if (_introPlaying) return;
    //    if (count <= 0) return;
    //    _dispatchScheduled = false; // hủy dispatch đang chờ nếu có

    //    int visibleCount = _visible.Count;
    //    if (visibleCount == 0 && _pending.Count == 0) return;
    //    // 1. Gom cốc theo thứ tự sẽ được dispatch
    //    var block = new List<Cup>(visibleCount + count);
    //    block.AddRange(_visible);
    //    int need = count - block.Count;
    //    while (need > 0 && _pending.Count > 0)
    //    {
    //        block.Add(_pending.Dequeue());
    //        need--;
    //    }
    //    if (block.Count <= 1) return;
    //    // 2. Sắp xếp theo màu (cùng màu gom nhóm)
    //    block.Sort((a, b) => a.Color.CompareTo(b.Color));
    //    // 3. Gán lại visible
    //    _visible.Clear();
    //    for (int i = 0; i < visibleCount && i < block.Count; i++)
    //        _visible.Add(block[i]);
    //    // 4. Gán lại pending: phần dư của block + phần pending chưa đụng tới
    //    var newPending = new List<Cup>();
    //    for (int i = visibleCount; i < block.Count; i++)
    //        newPending.Add(block[i]);
    //    while (_pending.Count > 0)
    //        newPending.Add(_pending.Dequeue());
    //    _pending.Clear();
    //    foreach (var cup in newPending)
    //        _pending.Enqueue(cup);
    //    // 5. Cập nhật vị trí trên màn hình
    //    RepositionVisibleCups(instant: true);
    //}
    private void RepositionVisibleCups(bool instant)
    {
        for (int i = 0; i < _visible.Count; i++)
        {
            Cup cup = _visible[i];
            Transform slot = _slotPositions[i];
            SetSortingOrder(cup, _slotPositions.Count - i);
            cup.transform.DOKill(false);
            if (instant)
            {
                cup.transform.position = slot.position;
                // Không đổi rotation — giống ShiftQueue
            }
            else
            {
                cup.transform
                    .DOMove(slot.position, _shiftDuration)
                    .SetEase(_shiftEase);
            }
        }
    }

    public void SortVisibleCups()
    {
        if (_introPlaying) return;
        if (_visible.Count <= 1) return;
        _dispatchScheduled = false;
        // Sort trực tiếp list visible
        _visible.Sort((a, b) => a.Color.CompareTo(b.Color));
        // Snap lại đúng vị trí slot trên ray
        RepositionVisibleCups(instant: true);
        TryDispatchFront();
        AudioManager.Instance.PlaySFX("RightPos");
    }
}
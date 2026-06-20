using AudioSystem;
using DG.Tweening;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [Header("Skill 1: Add Table Slot")]
    [SerializeField] private int _addSlotCharges = 1;
    [SerializeField] private ParticleSystem _addSlotEffect;

    [Header("Skill 2: Sort Cups")]
    [SerializeField] private int _sortCupsCharges = 1;

    [Header("Skill 3: Swap Grid Trays")]
    [SerializeField] private int _swapGridCharges = 1;
    [SerializeField] private float _selectLiftHeight = 0.2f;
    [SerializeField] private float _selectLiftDuration = 0.12f;
    [SerializeField] private float _swapDuration = 0.35f;
    [SerializeField] private Ease _swapEase = Ease.InOutQuad;

    public bool IsGridSwapModeActive { get; private set; }
    public bool IsGridSwapping { get; private set; }

    private Tray _firstTray;
    private Vector3 _firstTrayRestPos;
    private Tween _selectTween;
    private Sequence _swapSequence;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        _selectTween?.Kill();
        _swapSequence?.Kill();
    }

    /// <summary>Gán vào OnClick của nút skill / UIBonusSlot.</summary>
    public void UseAddTableSlot()
    {
        if (_addSlotCharges <= 0) return;
        if (!TableSlotManager.Instance.CanUnlockBonusSlot) return;

        _addSlotCharges--;
        AudioManager.Instance.PlaySFX("BoostComplete");
        if (_addSlotEffect != null)
            _addSlotEffect.Play();
        TableSlotManager.Instance.UnlockBonusSlot();
    }

    public void UseSortCups()
    {
        if (_sortCupsCharges <= 0) return;
        if (CupQueue.Instance == null) return;
        _sortCupsCharges--;
        CupQueue.Instance.SortVisibleCups();
    }

    /// <summary>Gán vào OnClick của nút skill đổi vị trí 2 khay trên grid.</summary>
    public void UseSwapGridTrays()
    {
        if (_swapGridCharges <= 0 || IsGridSwapModeActive || IsGridSwapping) return;
        if (GridMapManager.Instance == null || GridMapManager.Instance.CountOnGrid < 2) return;

        CancelGridSwapMode();
        IsGridSwapModeActive = true;
    }

    public void CancelGridSwapMode()
    {
        if (!IsGridSwapModeActive && _firstTray == null) return;

        DeselectFirstTray();
        IsGridSwapModeActive = false;
    }

    /// <returns>true nếu click được xử lý bởi skill swap.</returns>
    public bool TryHandleGridTrayClick(Vector3 worldPos)
    {
        if (!IsGridSwapModeActive || IsGridSwapping) return false;
        if (GridMapManager.Instance == null) return false;
        if (Tray.AnyTrayFlying) return false;

        Tray tray = PickTrayAt(worldPos);
        if (tray == null || !GridMapManager.Instance.IsOnGrid(tray)) return false;

        if (_firstTray == null)
        {
            SelectFirstTray(tray);
            return true;
        }

        if (_firstTray == tray)
        {
            DeselectFirstTray();
            return true;
        }

        if (_firstTray.TrayType != tray.TrayType)
        {
            AudioManager.Instance.PlaySFX("Fail");
            return true;
        }

        if (!GridMapManager.Instance.CanSwapTrays(_firstTray, tray))
        {
            AudioManager.Instance.PlaySFX("Fail");
            return true;
        }

        ExecuteGridSwap(_firstTray, tray);
        return true;
    }

    private static Tray PickTrayAt(Vector3 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(worldPos.x, worldPos.y));
        Tray best = null;
        int bestLayer = int.MinValue;

        foreach (Collider2D hit in hits)
        {
            var tray = hit.GetComponent<Tray>();
            if (tray == null || GridMapManager.Instance == null || !GridMapManager.Instance.IsOnGrid(tray))
                continue;

            if (tray.Layer > bestLayer)
            {
                bestLayer = tray.Layer;
                best = tray;
            }
        }

        return best;
    }

    private void SelectFirstTray(Tray tray)
    {
        _firstTray = tray;
        _firstTrayRestPos = tray.transform.position;

        _selectTween?.Kill();
        _selectTween = tray.transform
            .DOMove(_firstTrayRestPos + Vector3.up * _selectLiftHeight, _selectLiftDuration)
            .SetEase(Ease.OutQuad);

        AudioManager.Instance.PlaySFX("Click");
    }

    private void DeselectFirstTray()
    {
        if (_firstTray == null) return;

        _selectTween?.Kill();
        _firstTray.transform.DOMove(_firstTrayRestPos, _selectLiftDuration).SetEase(Ease.OutQuad);
        _firstTray = null;
    }

    private void ExecuteGridSwap(Tray trayA, Tray trayB)
    {
        IsGridSwapping = true;
        IsGridSwapModeActive = false;

        _selectTween?.Kill();
        trayA.transform.position = _firstTrayRestPos;

        GridMapManager.Instance.SwapTrays(trayA, trayB);

        var poserA = trayA.GetComponent<GridPositioner>();
        var poserB = trayB.GetComponent<GridPositioner>();

        Vector3 targetA = poserA != null ? poserA.ComputeWorldPosition() : trayA.transform.position;
        Vector3 targetB = poserB != null ? poserB.ComputeWorldPosition() : trayB.transform.position;

        ApplySwapVisuals(trayA, trayB);

        _swapSequence?.Kill();
        _swapSequence = DOTween.Sequence();
        _swapSequence.Join(trayA.transform.DOMove(targetA, _swapDuration).SetEase(_swapEase));
        _swapSequence.Join(trayB.transform.DOMove(targetB, _swapDuration).SetEase(_swapEase));
        _swapSequence.OnComplete(() =>
        {
            if (poserA != null) poserA.UpdatePosition();
            if (poserB != null) poserB.UpdatePosition();

            GridMapManager.Instance.RefreshAllGridTrayVisuals();

            _swapGridCharges--;
            _firstTray = null;
            IsGridSwapping = false;
            AudioManager.Instance.PlaySFX("BoostComplete");
        });
    }

    private static void ApplySwapVisuals(Tray trayA, Tray trayB)
    {
        trayA.ApplyGridVisual();
        trayB.ApplyGridVisual();
    }
}

using AudioSystem;
using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

public enum SkillType
{
    AddSlot,
    SortCups,
    SwapGrid
}

[System.Serializable]
public class SkillEntry
{
    public SkillType type;
    [Header("Stack")]
    public int charges = 1;

    [Header("HUD Badge")]
    public GameObject badgeCount;          // Image đỏ (có child Text TMP)
    public TextMeshProUGUI countText;
    public GameObject badgePlus;           // Image xanh dấu +

    [Header("Purchase Panel")]
    public GameObject purchasePanel;

    [Header("Price")]
    public int priceSingle = 1500;
    public int priceBundle = 3600;
    public int bundleAmount = 3;
}

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }
    [Header("Skills")]
    [SerializeField] private SkillEntry[] _skills;
    [Header("Skill 1: Add Table Slot")]
    [SerializeField] private ParticleSystem _addSlotEffect;
    [Header("Skill 3: Swap Grid Trays")]
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
    private SkillType? _openPurchaseType;
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        foreach (var skill in _skills)
        {
            skill.charges = SkillInventory.Instance?.GetCharges(skill.type) ?? skill.charges;
            RefreshUI(skill.type);
        }
    }
    private void OnDestroy()
    {
        _selectTween?.Kill();
        _swapSequence?.Kill();
    }
    // ===================== PUBLIC API CHO UI =====================
    // --- Nút skill chính (BtnAdd / BtnSort / Image) ---
    public void OnClickAddSlot() => TryUseSkill(SkillType.AddSlot);
    public void OnClickSortCups() => TryUseSkill(SkillType.SortCups);
    public void OnClickSwapGrid() => TryUseSkill(SkillType.SwapGrid);
    // --- Nút dấu + trên badge ---
    public void OnPlusAddSlot() => OpenPurchasePanel(SkillType.AddSlot);
    public void OnPlusSortCups() => OpenPurchasePanel(SkillType.SortCups);
    public void OnPlusSwapGrid() => OpenPurchasePanel(SkillType.SwapGrid);
    // --- Nút mua trong panel ---
    public void BuySingleAddSlot() => BuySkill(SkillType.AddSlot, 1);
    public void BuyBundleAddSlot() => BuySkill(SkillType.AddSlot, GetEntry(SkillType.AddSlot).bundleAmount);
    public void BuySingleSortCups() => BuySkill(SkillType.SortCups, 1);
    public void BuyBundleSortCups() => BuySkill(SkillType.SortCups, GetEntry(SkillType.SortCups).bundleAmount);
    public void BuySingleSwapGrid() => BuySkill(SkillType.SwapGrid, 1);
    public void BuyBundleSwapGrid() => BuySkill(SkillType.SwapGrid, GetEntry(SkillType.SwapGrid).bundleAmount);
    // --- Nút đóng panel ---
    public void ClosePurchaseAddSlot() => ClosePurchasePanel(SkillType.AddSlot);
    public void ClosePurchaseSortCups() => ClosePurchasePanel(SkillType.SortCups);
    public void ClosePurchaseSwapGrid() => ClosePurchasePanel(SkillType.SwapGrid);
    // ===================== STACK + UI =====================
    private void TryUseSkill(SkillType type)
    {
        if (GetCharges(type) <= 0)
        {
            OpenPurchasePanel(type);
            return;
        }
        switch (type)
        {
            case SkillType.AddSlot:
                UseAddTableSlot();
                break;
            case SkillType.SortCups:
                UseSortCups();
                break;
            case SkillType.SwapGrid:
                UseSwapGridTrays();
                break;
        }
    }
    private int GetCharges(SkillType type) => GetEntry(type).charges;
    private void SetCharges(SkillType type, int value)
    {
        GetEntry(type).charges = Mathf.Max(0, value);
        RefreshUI(type);
    }
    private void AddCharges(SkillType type, int amount)
    {
        if (amount <= 0) return;
        GetEntry(type).charges += amount;
        RefreshUI(type);
    }
    private void RefreshUI(SkillType type)
    {
        SkillEntry entry = GetEntry(type);
        bool hasCharge = entry.charges > 0;
        if (entry.badgeCount != null)
            entry.badgeCount.SetActive(hasCharge);
        if (entry.badgePlus != null)
            entry.badgePlus.SetActive(!hasCharge);
        if (entry.countText != null)
            entry.countText.text = entry.charges.ToString();
    }
    private SkillEntry GetEntry(SkillType type)
    {
        foreach (var skill in _skills)
        {
            if (skill.type == type)
                return skill;
        }
        Debug.LogError($"[SkillManager] Missing SkillEntry for {type}");
        return null;
    }
    // ===================== PURCHASE PANEL =====================
    private void OpenPurchasePanel(SkillType type)
    {
        SkillEntry entry = GetEntry(type);
        if (entry == null || entry.purchasePanel == null) return;
        _openPurchaseType = type;
        if (GameManager.Instance != null)
            GameManager.Instance.SetInputBlocked(true);
        GameManager.Instance?.OpenOverlayFade();
        GameManager.Instance?.PopInPanel(entry.purchasePanel);
        AudioManager.Instance.PlaySFX("Click");
    }
    private void ClosePurchasePanel(SkillType type)
    {
        SkillEntry entry = GetEntry(type);
        if (entry == null || entry.purchasePanel == null) return;
        StartCoroutine(ClosePurchaseFlow(entry.purchasePanel));
    }
    private IEnumerator ClosePurchaseFlow(GameObject panel)
    {
        yield return GameManager.Instance.PopOutPanel(panel);
        if (_openPurchaseType.HasValue)
        {
            _openPurchaseType = null;
            GameManager.Instance?.CloseOverlayFade();
            GameManager.Instance?.SetInputBlocked(false);
        }
        AudioManager.Instance.PlaySFX("Click");
    }
    private void BuySkill(SkillType type, int amount)
    {
        SkillEntry entry = GetEntry(type);
        if (entry == null) return;
        int price = amount == 1 ? entry.priceSingle : entry.priceBundle;
        if (MoneyManager.Instance == null || !MoneyManager.Instance.TrySpend(price))
        {
            return;
        }
        AddCharges(type, amount);
        ClosePurchasePanel(type);
    }
    private static void HidePanelInstant(GameObject panel)
    {
        if (panel == null) return;
        panel.transform.DOKill();
        panel.transform.localScale = Vector3.zero;
        panel.SetActive(false);
    }
    // ===================== SKILL LOGIC (GIỮ NGUYÊN) =====================
    public void UseAddTableSlot()
    {
        if (GetCharges(SkillType.AddSlot) <= 0) return;
        if (!TableSlotManager.Instance.CanUnlockBonusSlot) return;
        SetCharges(SkillType.AddSlot, GetCharges(SkillType.AddSlot) - 1);
        TableSlotManager.Instance.UnlockBonusSlot();
        AudioManager.Instance.PlaySFX("BoostComplete");
        if (_addSlotEffect != null)
            _addSlotEffect.Play();
        NotifyBoosterUsed();
    }
    public void UseSortCups()
    {
        if (GetCharges(SkillType.SortCups) <= 0) return;
        if (CupQueue.Instance == null) return;
        SetCharges(SkillType.SortCups, GetCharges(SkillType.SortCups) - 1);
        CupQueue.Instance.SortVisibleCups();
        NotifyBoosterUsed();
    }
    public void UseSwapGridTrays()
    {
        if (GetCharges(SkillType.SwapGrid) <= 0 || IsGridSwapModeActive || IsGridSwapping) return;
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
            SetCharges(SkillType.SwapGrid, GetCharges(SkillType.SwapGrid) - 1);
            NotifyBoosterUsed();
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

    public void GrantCharges(SkillType type, int amount)
    {
        AddCharges(type, amount);
        SkillInventory.Instance?.AddCharges(type, amount); // sync save
    }

    public void NotifyBoosterUsed()
    {
        MissionManager.Instance?.AddProgress(MissionType.UseBoosters, 1);
    }
}

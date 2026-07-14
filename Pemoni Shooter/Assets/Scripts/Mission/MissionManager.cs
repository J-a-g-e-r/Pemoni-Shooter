using System;
using RewardSystem;
using Unity.VisualScripting;
using UnityEngine;

public enum MissionType
{
    CompleteLevels,
    CollectCups,
    CollectCoins,
    UseBoosters
}

[Serializable]
public class MissionConfig
{
    public MissionType type;
    public int target = 1;
    [TextArea] public string description;   // "Complete 4 levels"
    public Sprite icon;
    public RewardEntry reward;              // phần thưởng bên trong card
    public Sprite closedCardIcon;           // sprite card đóng (animation CardWait)
}

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [SerializeField] private MissionConfig[] missions = new MissionConfig[4];

    [Header("Milestone (rương tím)")]
    [SerializeField] private int milestoneTarget = 4;
    [SerializeField] private RewardData milestoneReward;

    private int[] _progress;
    private bool[] _readyToClaim;
    private int _milestoneProgress;
    private bool _milestoneReady;

    public event Action OnStateChanged;

    private const string ProgressKey = "mission_progress_";
    private const string ReadyKey = "mission_ready_";
    private const string MilestoneProgressKey = "mission_milestone_progress";
    private const string MilestoneReadyKey = "mission_milestone_ready";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        int count = missions != null ? missions.Length : 0;
        _progress = new int[count];
        _readyToClaim = new bool[count];
        Load();
    }

    public int MissionCount => missions.Length;

    public MissionConfig GetConfig(int index) => missions[index];

    public int GetProgress(int index) => _progress[index];

    public int GetTarget(int index) => missions[index].target;

    public float GetProgress01(int index)
    {
        int target = Mathf.Max(1, missions[index].target);
        return (float)_progress[index] / target;
    }

    public string GetProgressText(int index) =>
        $"{_progress[index]}/{missions[index].target}";

    public bool CanClaim(int index) =>
        index >= 0 && index < _readyToClaim.Length && _readyToClaim[index];

    public int MilestoneProgress => _milestoneProgress;
    public int MilestoneTarget => milestoneTarget;
    public bool CanClaimMilestone => _milestoneReady;

    /// <summary>Cộng tiến trình cho mọi mission cùng loại (thường chỉ có 1).</summary>
    public void AddProgress(MissionType type, int amount = 1)
    {
        if (amount <= 0 || missions == null) return;

        bool changed = false;
        for (int i = 0; i < missions.Length; i++)
        {
            if (missions[i].type != type) continue;
            if (_readyToClaim[i]) continue;

            int target = missions[i].target;
            int next = Mathf.Min(_progress[i] + amount, target);
            if (next == _progress[i]) continue;

            _progress[i] = next;
            if (_progress[i] >= target)
                _readyToClaim[i] = true;

            changed = true;
        }

        if (changed)
        {
            Save();
            OnStateChanged?.Invoke();
        }
    }

    public bool TryClaim(int index)
    {
        if (!CanClaim(index)) return false;

        var cfg = missions[index];
        var grant = BuildCardReward(cfg);

        _progress[index] = 0;
        _readyToClaim[index] = false;

        _milestoneProgress = Mathf.Min(_milestoneProgress + 1, milestoneTarget);
        if (_milestoneProgress >= milestoneTarget)
            _milestoneReady = true;

        Save();
        OnStateChanged?.Invoke();

        RewardManager.Instance?.GrantReward(grant);
        return true;
    }

    public bool TryClaimMilestone()
    {
        if (!_milestoneReady) return false;

        _milestoneProgress = 0;
        _milestoneReady = false;
        Save();
        OnStateChanged?.Invoke();

        RewardManager.Instance?.GrantReward(CloneReward(milestoneReward));
        return true;
    }

    private static RewardData BuildCardReward(MissionConfig cfg)
    {
        var grant = new RewardData
        {
            type = RewardType.Card,
            icon = cfg.closedCardIcon,
            closedCardIcon = cfg.closedCardIcon,
            cardContents = new System.Collections.Generic.List<RewardEntry>()
        };

        if (cfg.reward != null)
            grant.cardContents.Add(new RewardEntry(cfg.reward.type, cfg.reward.amount, cfg.reward.icon));

        RewardData.Sanitize(grant);
        return grant;
    }

    private static RewardData CloneReward(RewardData source)
    {
        if (source == null) return null;

        var clone = new RewardData
        {
            type = source.type,
            amount = source.amount,
            icon = source.icon,
            displayName = source.displayName,
            closedChestIcon = source.closedChestIcon,
            closedCardIcon = source.closedCardIcon,
            chestContents = new System.Collections.Generic.List<RewardEntry>(),
            cardContents = new System.Collections.Generic.List<RewardEntry>()
        };

        if (source.chestContents != null)
        {
            foreach (var e in source.chestContents)
                clone.chestContents.Add(new RewardEntry(e.type, e.amount, e.icon));
        }

        if (source.cardContents != null)
        {
            foreach (var e in source.cardContents)
                clone.cardContents.Add(new RewardEntry(e.type, e.amount, e.icon));
        }

        RewardData.Sanitize(clone);
        return clone;
    }

    private void Save()
    {
        for (int i = 0; i < missions.Length; i++)
        {
            PlayerPrefs.SetInt(ProgressKey + i, _progress[i]);
            PlayerPrefs.SetInt(ReadyKey + i, _readyToClaim[i] ? 1 : 0);
        }

        PlayerPrefs.SetInt(MilestoneProgressKey, _milestoneProgress);
        PlayerPrefs.SetInt(MilestoneReadyKey, _milestoneReady ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        for (int i = 0; i < missions.Length; i++)
        {
            _progress[i] = PlayerPrefs.GetInt(ProgressKey + i, 0);
            _readyToClaim[i] = PlayerPrefs.GetInt(ReadyKey + i, 0) == 1;
        }

        _milestoneProgress = PlayerPrefs.GetInt(MilestoneProgressKey, 0);
        _milestoneReady = PlayerPrefs.GetInt(MilestoneReadyKey, 0) == 1;
    }

    [ContextMenu("Reset Mission Progress")]
    public void ResetProgress()
    {
        for (int i = 0; i < _progress.Length; i++)
        {
            _progress[i] = 0;
            _readyToClaim[i] = false;
        }

        _milestoneProgress = 0;
        _milestoneReady = false;
        Save();
        OnStateChanged?.Invoke();
    }
}

//public float GetProgress01(int index) =>
//        (float)runtime[index].progress / missions[index].target;

//    public string GetProgressText(int index) =>
//        $"{runtime[index].progress}/{missions[index].target}";

//    public bool CanClaim(int index) => runtime[index].readyToClaim;

//    // Save/Load giống SeasonPassManager...
//}
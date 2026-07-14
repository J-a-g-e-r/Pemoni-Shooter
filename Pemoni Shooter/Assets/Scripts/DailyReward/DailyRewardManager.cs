using System;
using System.Collections.Generic;
using UnityEngine;

namespace RewardSystem
{
    [Serializable]
    public class DailyRewardEntry
    {
        public RewardData reward;
        public bool requireAd;
        public List<RewardEntry> chestContents = new List<RewardEntry>();
    }

    public class DailyRewardManager : MonoBehaviour
    {
        public static DailyRewardManager Instance { get; private set; }

        private const string CycleEndKey = "daily_reward_cycle_end";
        private const string ClaimedListKey = "daily_reward_claimed_list"; // Thay đổi cách lưu danh sách đã nhận
        private const string NextFreeKey = "daily_reward_next_free";

        private const int CycleSeconds = 86400;   // 24h
        private const int FreeWaitSeconds = 300;  // 5 phút (Bạn có thể đổi thành 600 nếu muốn 10 phút)

        [Header("Rewards (top → bottom)")]
        [SerializeField] private List<DailyRewardEntry> entries = new();

        [Header("UI")]
        [SerializeField] private DailyRewardItemUI[] itemViews;
        [SerializeField] private TMPro.TMP_Text resetCountdownText;
        [SerializeField] private TMPro.TMP_Text globalFreeCountdownText;

        [Header("Ads")]
        [Tooltip("Gắn callback quảng cáo thật sau. Tạm thời để trống = auto success.")]
        [SerializeField] private bool simulateAdSuccess = true;

        private long cycleEndUnix;
        private HashSet<int> claimedIndices = new HashSet<int>(); // Lưu các index đã nhận độc lập
        private long nextFreeUnix;
        private int lastDisplayedResetSecond = -1;
        private int lastDisplayedFreeSecond = -1;

        public int TotalRewards => entries.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadData();
            CheckCycleReset();

            for (int i = 0; i < itemViews.Length; i++)
            {
                if (itemViews[i] != null)
                    itemViews[i].Setup(i);
            }

            RefreshAll();
        }

        private void Update()
        {
            CheckCycleReset();
            RefreshTimersOnly();
        }

        public void OnPanelOpened()
        {
            CheckCycleReset();
            RefreshAll();
        }

        // ---------- State ----------

        // Kiểm tra xem index này đã nhận chưa
        public bool IsCollected(int index)
        {
            if (index == 0) return false; // Phần thưởng free đầu tiên không bao giờ bị "biến mất" hay đánh dấu thu thập vĩnh viễn, nó reset liên tục
            return claimedIndices.Contains(index);
        }

        // Các phần thưởng xem quảng cáo luôn được mở khóa, không cần chờ phần thưởng trước
        public bool IsUnlocked(int index)
        {
            // Phần thưởng Free đầu tiên (index 0) luôn luôn được mở khóa độc lập
            if (index == 0) return true;

            // Phần thưởng Ad đầu tiên (index 1) luôn mở khóa để người chơi có thể xem Ad nhận luôn
            if (index == 1) return true;

            // Các phần thưởng Ad tiếp theo (index >= 2) chỉ mở khóa KHI phần thưởng Ad ngay trước nó (index - 1) đã được nhận (IsCollected)
            return IsCollected(index - 1);
        }

        public bool CanClaimFreeReward()
        {
            return GetNowUnix() >= nextFreeUnix;
        }

        public long GetRemainingFreeSeconds()
        {
            return Math.Max(0, nextFreeUnix - GetNowUnix());
        }

        public long GetRemainingCycleSeconds()
        {
            return Math.Max(0, cycleEndUnix - GetNowUnix());
        }

        public DailyRewardEntry GetEntry(int index)
        {
            if (index < 0 || index >= entries.Count) return null;
            return entries[index];
        }

        // ---------- Claim ----------

        public void TryClaim(int index)
        {
            CheckCycleReset();

            if (index == 0)
            {
                if (!CanClaimFreeReward()) return;
            }
            else
            {
                if (IsCollected(index)) return;
            }

            var entry = GetEntry(index);
            if (entry == null || entry.reward == null)
                return;

            if (entry.requireAd)
            {
                ShowRewardedAd(
                    onSuccess: () => CompleteClaim(index),
                    onFailed: () => { });
            }
            else
            {
                CompleteClaim(index);
            }
        }

        private void CompleteClaim(int index)
        {
            var entry = GetEntry(index);
            if (entry == null) return;

            var grantReward = BuildGrantReward(entry);
            RewardManager.Instance?.GrantReward(grantReward);

            if (index == 0)
            {
                // Nếu là phần thưởng Free đầu tiên, tính thời gian cho lần nhận tiếp theo
                nextFreeUnix = GetNowUnix() + FreeWaitSeconds;
            }
            else
            {
                // Nếu là phần thưởng quảng cáo, lưu vào danh sách đã nhận
                claimedIndices.Add(index);
            }

            SaveData();
            RefreshAll();
        }

        private RewardData BuildGrantReward(DailyRewardEntry entry)
        {
            var source = entry.reward;
            if (!source.IsChest)
                return source;
            var grant = new RewardData
            {
                type = source.type,
                amount = source.amount,
                icon = source.icon,
                displayName = source.displayName,
                closedChestIcon = source.closedChestIcon != null ? source.closedChestIcon : source.icon,
                chestContents = new List<RewardEntry>()
            };
            if (entry.chestContents != null)
            {
                foreach (var item in entry.chestContents)
                {
                    if (item == null || item.type == RewardType.Chest)
                        continue;
                    grant.chestContents.Add(new RewardEntry(item.type, item.amount, item.icon));
                }
            }
            if (grant.chestContents.Count == 0)
                Debug.LogWarning($"DailyReward chest at index has no chestContents configured!");
            RewardData.Sanitize(grant);
            return grant;
        }

        private void ShowRewardedAd(Action onSuccess, Action onFailed)
        {
            AdMobManager.Instance.ShowRewarded(onSuccess, onFailed);
        }

        // ---------- 24h cycle ----------

        private void CheckCycleReset()
        {
            long now = GetNowUnix();

            if (cycleEndUnix == 0)
            {
                StartNewCycle(now);
                return;
            }

            if (now >= cycleEndUnix)
                StartNewCycle(now);
        }

        private void StartNewCycle(long now)
        {
            cycleEndUnix = now + CycleSeconds;
            claimedIndices.Clear(); // Xóa sạch các mốc quảng cáo đã nhận khi qua ngày mới
            nextFreeUnix = now;     // Đầu ngày mới cho nhận Free luôn
            SaveData();
            RefreshAll();
        }

        // ---------- UI ----------

        public void RefreshAll()
        {
            lastDisplayedResetSecond = -1;
            lastDisplayedFreeSecond = -1;

            if (itemViews != null)
            {
                foreach (var view in itemViews)
                {
                    if (view != null)
                        view.Refresh();
                }
            }

            RefreshResetText();
            RefreshGlobalFreeText();
        }

        private void RefreshTimersOnly()
        {
            RefreshResetText();
            RefreshGlobalFreeText();

            if (itemViews != null && itemViews.Length > 0 && itemViews[0] != null)
            {
                int sec = (int)GetRemainingFreeSeconds();
                if (sec != lastDisplayedFreeSecond)
                {
                    lastDisplayedFreeSecond = sec;
                    itemViews[0].Refresh();
                }
            }
        }

        private void RefreshResetText()
        {
            if (resetCountdownText == null) return;

            int sec = (int)GetRemainingCycleSeconds();
            if (sec == lastDisplayedResetSecond) return;

            lastDisplayedResetSecond = sec;
            resetCountdownText.text = $"Reset in {FormatTime(sec)}";
        }

        private void RefreshGlobalFreeText()
        {
            if (globalFreeCountdownText == null) return;

            if (CanClaimFreeReward())
            {
                globalFreeCountdownText.text = "Free"; // Hoặc "Get Free!" tùy bạn
            }
            else
            {
                long sec = GetRemainingFreeSeconds();
                globalFreeCountdownText.text = $"{FormatTime(sec)}";
            }
        }
        // ---------- Save / Load ----------

        private void LoadData()
        {
            cycleEndUnix = long.Parse(PlayerPrefs.GetString(CycleEndKey, "0"));
            nextFreeUnix = long.Parse(PlayerPrefs.GetString(NextFreeKey, "0"));

            claimedIndices.Clear();
            string claimedData = PlayerPrefs.GetString(ClaimedListKey, "");
            if (!string.IsNullOrEmpty(claimedData))
            {
                string[] split = claimedData.Split(',');
                foreach (var s in split)
                {
                    if (int.TryParse(s, out int idx))
                    {
                        claimedIndices.Add(idx);
                    }
                }
            }
        }

        private void SaveData()
        {
            PlayerPrefs.SetString(CycleEndKey, cycleEndUnix.ToString());
            PlayerPrefs.SetString(NextFreeKey, nextFreeUnix.ToString());

            string claimedData = string.Join(",", claimedIndices);
            PlayerPrefs.SetString(ClaimedListKey, claimedData);
            PlayerPrefs.Save();
        }

        private static long GetNowUnix() =>
            DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static string FormatTime(long totalSeconds)
        {
            totalSeconds = Math.Max(0, totalSeconds);
            var time = TimeSpan.FromSeconds(totalSeconds);

            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";

            return $"{time.Minutes:00}:{time.Seconds:00}";
        }
    }
}
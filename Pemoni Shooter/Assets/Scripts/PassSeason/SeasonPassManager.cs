using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeasonPass
{
    /// <summary>
    /// Quản lý toàn bộ trạng thái của Season Pass: điểm hiện tại, đã kích hoạt Gold Pass hay chưa,
    /// mốc nào đã claim (free/gold). Lưu bằng PlayerPrefs (có thể đổi sang save file/cloud sau).
    /// </summary>
    public class SeasonPassManager : MonoBehaviour
    {
        public static SeasonPassManager Instance { get; private set; }

        [SerializeField] private SeasonPassData data;

        public int CurrentPoints { get; private set; }
        public bool IsGoldActive { get; private set; }

        // index theo level, true nếu đã nhận
        private bool[] freeClaimed;
        private bool[] goldClaimed;

        public event Action OnStateChanged;

        private const string KEY_POINTS = "sp_points";
        private const string KEY_GOLD_ACTIVE = "sp_gold_active";
        private const string KEY_FREE_CLAIMED = "sp_free_claimed";
        private const string KEY_GOLD_CLAIMED = "sp_gold_claimed";
        private const string KEY_SEASON_END = "sp_season_end_unix";
        private long _seasonEndUnix;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            int count = data != null ? data.levels.Count : 0;
            freeClaimed = new bool[count];
            goldClaimed = new bool[count];

            Load();
            InitSeasonEndTime();
        }

        public SeasonPassData Data => data;

        /// <summary>Gọi khi người chơi kiếm được điểm season pass (chơi game, hoàn thành nhiệm vụ, ...)</summary>
        public void AddPoints(int amount)
        {
            CurrentPoints += amount;
            Save();
            OnStateChanged?.Invoke();
        }

        /// <summary>Kích hoạt Gold Pass (gọi sau khi trừ tiền/gems thành công ở lớp mua hàng).</summary>
        public void ActivateGoldPass()
        {
            if (IsGoldActive) return;
            IsGoldActive = true;
            Save();
            OnStateChanged?.Invoke();
        }

        public bool IsLevelUnlocked(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= data.levels.Count) return false;
            return CurrentPoints >= data.levels[levelIndex].requiredPoints;
        }

        public bool IsFreeClaimed(int levelIndex) => freeClaimed[levelIndex];
        public bool IsGoldClaimed(int levelIndex) => goldClaimed[levelIndex];

        /// <summary>Trạng thái ô Gold: đã mở (Gold Pass active) và đủ điểm và chưa nhận.</summary>
        public bool CanClaimGold(int levelIndex)
        {
            return IsGoldActive && IsLevelUnlocked(levelIndex) && !goldClaimed[levelIndex];
        }

        public bool CanClaimFree(int levelIndex)
        {
            return IsLevelUnlocked(levelIndex) && !freeClaimed[levelIndex];
        }

        public bool TryClaimFree(int levelIndex, out RewardInfo reward)
        {
            reward = null;
            if (!CanClaimFree(levelIndex)) return false;
            freeClaimed[levelIndex] = true;
            reward = data.levels[levelIndex].freeReward;
            Save();
            OnStateChanged?.Invoke();
            return true;
        }

        public bool TryClaimGold(int levelIndex, out RewardInfo reward)
        {
            reward = null;
            if (!CanClaimGold(levelIndex)) return false;
            goldClaimed[levelIndex] = true;
            reward = data.levels[levelIndex].goldReward;
            Save();
            OnStateChanged?.Invoke();
            return true;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(KEY_POINTS, CurrentPoints);
            PlayerPrefs.SetInt(KEY_GOLD_ACTIVE, IsGoldActive ? 1 : 0);
            PlayerPrefs.SetString(KEY_FREE_CLAIMED, BoolArrayToString(freeClaimed));
            PlayerPrefs.SetString(KEY_GOLD_CLAIMED, BoolArrayToString(goldClaimed));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            CurrentPoints = PlayerPrefs.GetInt(KEY_POINTS, 0);
            IsGoldActive = PlayerPrefs.GetInt(KEY_GOLD_ACTIVE, 0) == 1;
            StringToBoolArray(PlayerPrefs.GetString(KEY_FREE_CLAIMED, ""), freeClaimed);
            StringToBoolArray(PlayerPrefs.GetString(KEY_GOLD_CLAIMED, ""), goldClaimed);
        }

        private static string BoolArrayToString(bool[] arr)
        {
            var chars = new char[arr.Length];
            for (int i = 0; i < arr.Length; i++) chars[i] = arr[i] ? '1' : '0';
            return new string(chars);
        }

        private static void StringToBoolArray(string s, bool[] target)
        {
            for (int i = 0; i < target.Length; i++)
                target[i] = i < s.Length && s[i] == '1';
        }

        /// <summary>Dùng cho nút reset khi test.</summary>
        [ContextMenu("Reset Progress")]
        public void ResetProgress()
        {
            CurrentPoints = 0;
            IsGoldActive = false;
            Array.Clear(freeClaimed, 0, freeClaimed.Length);
            Array.Clear(goldClaimed, 0, goldClaimed.Length);
            Save();
            OnStateChanged?.Invoke();
        }

        private void InitSeasonEndTime()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (PlayerPrefs.HasKey(KEY_SEASON_END))
            {
                _seasonEndUnix = long.Parse(PlayerPrefs.GetString(KEY_SEASON_END, "0"));
            }
            else if (data != null)
            {
                _seasonEndUnix = now + (long)data.seasonDurationSeconds;
                PlayerPrefs.SetString(KEY_SEASON_END, _seasonEndUnix.ToString());
                PlayerPrefs.Save();
            }
        }
        public double GetSeasonSecondsRemaining()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Math.Max(0, _seasonEndUnix - now);
        }

        public int GetCurrentLevelIndex()
        {
            if (data == null || data.levels == null || data.levels.Count == 0)
                return 0;

            int currentLevel = 0;
            for (int i = 0; i < data.levels.Count; i++)
            {
                if (CurrentPoints >= data.levels[i].requiredPoints)
                    currentLevel = i;
                else
                    break;
            }

            return currentLevel;
        }

        public int GetCurrentDisplayLevel()
        {
            return GetCurrentLevelIndex() + 1;
        }

        public int GetCurrentLevelStartPoints()
        {
            if (data == null || data.levels == null || data.levels.Count == 0)
                return 0;

            int currentIndex = GetCurrentLevelIndex();
            return data.levels[currentIndex].requiredPoints;
        }

        public int GetNextLevelRequiredPoints()
        {
            if (data == null || data.levels == null || data.levels.Count == 0)
                return 0;

            int currentIndex = GetCurrentLevelIndex();
            if (currentIndex >= data.levels.Count - 1)
                return data.levels[currentIndex].requiredPoints;

            return data.levels[currentIndex + 1].requiredPoints;
        }

        public int GetPointsInCurrentLevel()
        {
            return Mathf.Max(0, CurrentPoints - GetCurrentLevelStartPoints());
        }

        public int GetPointsRequiredForCurrentLevelProgress()
        {
            int currentStart = GetCurrentLevelStartPoints();
            int nextRequired = GetNextLevelRequiredPoints();
            return Mathf.Max(1, nextRequired - currentStart);
        }

        public float GetCurrentLevelProgress01()
        {
            if (data == null || data.levels == null || data.levels.Count == 0)
                return 0f;

            int currentIndex = GetCurrentLevelIndex();

            if (currentIndex >= data.levels.Count - 1)
                return 1f; // max level thì full thanh

            float current = GetPointsInCurrentLevel();
            float required = GetPointsRequiredForCurrentLevelProgress();
            return Mathf.Clamp01(current / required);
        }

        public bool HasNextMilestone()
        {
            return data != null
                && data.levels != null
                && GetCurrentLevelIndex() < data.levels.Count - 1;
        }

        public int GetNextMilestoneIndex()
        {
            if (!HasNextMilestone()) return -1;
            return GetCurrentLevelIndex() + 1;
        }

        public RewardInfo GetNextFreeReward()
        {
            int nextIndex = GetNextMilestoneIndex();
            if (nextIndex < 0) return null;
            return data.levels[nextIndex].freeReward;
        }
    }
}
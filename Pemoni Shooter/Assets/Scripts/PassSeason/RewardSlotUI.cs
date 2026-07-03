using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeasonPass
{
    /// <summary>
    /// 1 ô thưởng trong hàng (dùng chung cho cả cột Free và cột Gold).
    /// Gắn prefab: Image icon, Text/TMP amount, GameObject lockOverlay, GameObject checkMark, Button claimButton.
    /// </summary>
    public class RewardSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text amountText;      // đổi sang TMP_Text nếu dùng TextMeshPro
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject checkMark;
        [SerializeField] private Button claimButton;
        //[SerializeField] private TMP_Text _levelIndex;

        private int levelIndex;
        private bool isGoldColumn;

        public void Setup(int levelIndex, bool isGoldColumn)
        {
            this.levelIndex = levelIndex;
            //_levelIndex.text = levelIndex.ToString();
            this.isGoldColumn = isGoldColumn;
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClickClaim);
            Refresh();
        }

        public void Refresh()
        {
            var manager = SeasonPassManager.Instance;
            if (manager == null || manager.Data == null) return;
            var data = manager.Data;
            var levelData = data.levels[levelIndex];
            RewardInfo reward = isGoldColumn ? levelData.goldReward : levelData.freeReward;

            if (iconImage != null) iconImage.sprite = reward.icon;
            if (amountText != null) amountText.text = $"x{reward.amount}";

            bool unlocked = manager.IsLevelUnlocked(levelIndex);
            bool claimed = isGoldColumn ? manager.IsGoldClaimed(levelIndex) : manager.IsFreeClaimed(levelIndex);
            // Cột Gold còn phụ thuộc việc đã kích hoạt Gold Pass hay chưa
            bool locked = isGoldColumn ? (!manager.IsGoldActive || !unlocked) : !unlocked;

            if (lockOverlay != null) lockOverlay.SetActive(locked && !claimed);
            if (checkMark != null) checkMark.SetActive(claimed);

            bool canClaim = isGoldColumn ? manager.CanClaimGold(levelIndex) : manager.CanClaimFree(levelIndex);
            //claimButton.interactable = canClaim;
        }

        private void OnClickClaim()
        {
            Debug.Log($"Clicked reward level={levelIndex}, isGold={isGoldColumn}");
            var manager = SeasonPassManager.Instance;
            bool success = isGoldColumn
                ? manager.TryClaimGold(levelIndex, out RewardInfo rewardInfo)
                : manager.TryClaimFree(levelIndex, out rewardInfo);

            if (!success) return;

            // Chuyển sang RewardManager để hiển thị panel nhận thưởng (kèm animation mở rương nếu là Chest)
            var rewardData = new RewardSystem.RewardData
            {
                type = ConvertType(rewardInfo.type),
                amount = rewardInfo.amount,
                icon = rewardInfo.icon,
            };
            RewardSystem.RewardManager.Instance.GrantReward(rewardData);

            Refresh();
        }

        private static RewardSystem.RewardType ConvertType(RewardType t)
        {
            return t switch
            {
                RewardType.Coins => RewardSystem.RewardType.Coins,
                RewardType.Gems => RewardSystem.RewardType.Gems,
                RewardType.Chest => RewardSystem.RewardType.Chest,
                RewardType.BoosterAdd => RewardSystem.RewardType.Booster,
                RewardType.RandomSkin => RewardSystem.RewardType.Item,
                RewardType.SkillAddSlot => RewardSystem.RewardType.SkillAddSlot,
                RewardType.SkillSort => RewardSystem.RewardType.SkillSort,
                RewardType.SkillSwap => RewardSystem.RewardType.SkillSwap,
                RewardType.InfiniteHeart => RewardSystem.RewardType.InfiniteHeart,
                _ => RewardSystem.RewardType.Other,
            };
        }
    }
}
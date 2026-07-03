using System;
using System.Collections.Generic;
using UnityEngine;

namespace RewardSystem
{
    /// <summary>
    /// Điểm vào duy nhất để "phát thưởng" trong game. Bất kỳ hệ thống nào (Season Pass, quiz, mở rương...)
    /// chỉ cần gọi RewardManager.Instance.GrantReward(...) hoặc GrantRewards(...).
    /// Manager sẽ xếp hàng và hiển thị panel lần lượt, xử lý cả trường hợp rương (chest) cần chơi animation mở
    /// trước khi hiện phần thưởng bên trong.
    /// </summary>
    public class RewardManager : MonoBehaviour
    {
        public static RewardManager Instance { get; private set; }

        [SerializeField] private RewardPanelUI rewardPanel;

        private readonly Queue<RewardData> queue = new Queue<RewardData>();
        private bool isShowingPanel;

        /// <summary>Bắn ra mỗi khi 1 reward thực sự được cộng vào tài khoản người chơi (dùng để +coin, +gem thật...).</summary>
        public event Action<RewardData> OnRewardGranted;

        /// <summary>Bắn ra khi toàn bộ hàng đợi đã hiển thị và claim xong.</summary>
        public event Action OnQueueCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (rewardPanel != null)
            {
                rewardPanel.Initialize(this);
                rewardPanel.gameObject.SetActive(false);
            }
        }

        /// <summary>Thêm 1 phần thưởng vào hàng đợi hiển thị.</summary>
        public void GrantReward(RewardData reward)
        {
            queue.Enqueue(reward);
            TryShowNext();
        }

        /// <summary>Thêm nhiều phần thưởng cùng lúc (ví dụ claim nhiều mốc Season Pass 1 lần).</summary>
        public void GrantRewards(IEnumerable<RewardData> rewards)
        {
            foreach (var r in rewards) queue.Enqueue(r);
            TryShowNext();
        }

        private void TryShowNext()
        {
            if (isShowingPanel) return;
            if (queue.Count == 0)
            {
                OnQueueCompleted?.Invoke();
                return;
            }

            isShowingPanel = true;
            var next = queue.Dequeue();
            rewardPanel.gameObject.SetActive(true);
            rewardPanel.Show(next);
        }

        /// <summary>Gọi bởi RewardPanelUI khi 1 reward (không phải chest, hoặc nội dung bên trong chest) đã được claim thật sự.</summary>
        internal void NotifyRewardClaimed(RewardData reward)
        {
            OnRewardGranted?.Invoke(reward);
            // TODO: chỗ này gọi vào hệ thống Currency/Inventory thật của bạn, ví dụ:
            // CurrencyManager.Instance.Add(reward.type, reward.amount);
        }

        /// <summary>Gọi bởi RewardPanelUI khi panel hiện tại đã đóng hoàn toàn, để hiện thưởng tiếp theo trong hàng đợi.</summary>
        internal void NotifyPanelClosed()
        {
            isShowingPanel = false;
            rewardPanel.gameObject.SetActive(false);
            TryShowNext();
        }

        private void ApplyReward(RewardData reward)
        {
            switch (reward.type)
            {
                case RewardType.Coins:
                    MoneyManager.Instance?.AddMoney(reward.amount);
                    break;
                case RewardType.Gems:
                    // TODO: GemManager khi có
                    break;
                case RewardType.InfiniteHeart:
                    HeartManager.Instance?.GrantInfiniteHeart(reward.amount);
                    break;
                case RewardType.SkillAddSlot:
                    SkillInventory.Instance?.AddCharges(SkillType.AddSlot, reward.amount);
                    break;
                case RewardType.SkillSort:
                    SkillInventory.Instance?.AddCharges(SkillType.SortCups, reward.amount);
                    break;
                case RewardType.SkillSwap:
                    SkillInventory.Instance?.AddCharges(SkillType.SwapGrid, reward.amount);
                    break;
            }
        }
    }
}
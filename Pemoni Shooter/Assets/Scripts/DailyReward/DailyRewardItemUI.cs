using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RewardSystem
{
    public class DailyRewardItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text buttonText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject collectedMark;
        [SerializeField] private GameObject adIcon;

        private int index;

        public void Setup(int rewardIndex)
        {
            index = rewardIndex;

            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnClick);
            }

            Refresh();
        }

        public void Refresh()
        {
            var manager = DailyRewardManager.Instance;
            if (manager == null) return;

            var entry = manager.GetEntry(index);
            if (entry == null || entry.reward == null) return;

            var reward = entry.reward;

            if (iconImage != null)
                iconImage.sprite = reward.icon;

            if (amountText != null)
                amountText.text = FormatAmount(reward);

            bool collected = manager.IsCollected(index);
            bool unlocked = manager.IsUnlocked(index);

            // Hiển thị dấu tick xanh (V) nếu là phần thưởng Ad đã nhận
            if (collectedMark != null)
                collectedMark.SetActive(collected);

            if (lockOverlay != null)
                lockOverlay.SetActive(!unlocked && !collected);

            bool showAd = entry.requireAd;
            if (adIcon != null)
                adIcon.SetActive(showAd && unlocked && !collected);

            if (buttonText == null || actionButton == null) return;

            // ĐẢM BẢO NÚT LUÔN HIỆN DIỆN (Không dùng SetActive(false) nữa)
            actionButton.gameObject.SetActive(true);

            // TRƯỜNG HỢP 1: Phần thưởng Ad đã nhận rồi -> Làm mờ nút
            if (collected)
            {
                actionButton.interactable = false;
                //buttonText.text = "Claimed";
                return;
            }

            // TRƯỜNG HỢP 2: Nếu item bị khóa (Hiện tại theo logic mới là luôn mở)
            if (!unlocked)
            {
                actionButton.interactable = false;
                buttonText.text = "Free";
                return;
            }

            // TRƯỜNG HỢP 3: Reward đầu tiên (Phần thưởng Free lặp lại mỗi 5-10 phút)
            if (index == 0 && !entry.requireAd)
            {
                if (manager.CanClaimFreeReward())
                {
                    actionButton.interactable = true;
                    buttonText.text = "Collect";
                }
                else
                {
                    // Đang chờ đếm ngược: nút chuyển sang trạng thái tắt tương tác (bị đen/làm mờ đi)
                    actionButton.interactable = false;
                    buttonText.text = DailyRewardManager.FormatTime(manager.GetRemainingFreeSeconds());
                }
                return;
            }

            // TRƯỜNG HỢP 4: Các phần thưởng xem quảng cáo chưa nhận
            actionButton.interactable = true;
            buttonText.text = "Free";
        }

        private void OnClick()
        {
            DailyRewardManager.Instance?.TryClaim(index);
        }

        private static string FormatAmount(RewardData reward)
        {
            if (reward.type == RewardType.InfiniteHeart)
                return FormatDuration(reward.amount);

            return reward.amount > 1 ? $"x{reward.amount}" : "";
        }

        private static string FormatDuration(int seconds)
        {
            if (seconds >= 86400) return $"{seconds / 86400}d";
            if (seconds >= 3600) return $"{seconds / 3600}h";
            if (seconds >= 60) return $"{seconds / 60}m";
            return $"{seconds}s";
        }
    }
}
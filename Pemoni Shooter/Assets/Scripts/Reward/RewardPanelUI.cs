using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace RewardSystem
{
    /// <summary>
    /// Panel full-screen hiển thị phần thưởng, giống ảnh "Rewards" mẫu:
    /// - Reward thường: hiện icon + số lượng ngay, tap bất kỳ đâu để nhận & đóng.
    /// - Reward là Chest: hiện rương đóng trước, tap để chơi animation mở rương,
    ///   animation xong mới hiện icon/số lượng phần thưởng bên trong, tap tiếp để nhận & đóng.
    /// </summary>
    public class RewardPanelUI : MonoBehaviour
    {
        private enum State
        {
            Idle,
            WaitingTapToOpenChest,
            PlayingChestAnimation,
            WaitingTapToClaim
        }

        [Header("Vùng bắt tap toàn màn hình")]
        [SerializeField] private Button fullScreenTapCatcher;

        [Header("Hiển thị nội dung")]
        [SerializeField] private Image mainIcon;           // icon rương (đóng) hoặc icon reward đơn
        [SerializeField] private Text amountText;          // "500" — đổi sang TMP_Text nếu cần
        [SerializeField] private Text tapToContinueText;

        [Header("Danh sách nhiều phần thưởng (khi rương chứa >1 item)")]
        [SerializeField] private Transform multiRewardContainer;
        [SerializeField] private RewardItemView multiRewardItemPrefab;

        [Header("Animation rương")]
        [SerializeField] private Animator chestAnimator;   // Animator gắn trên mainIcon hoặc object rương riêng
        [SerializeField] private string chestOpenTrigger = "Open";
        [SerializeField] private float chestAnimFallbackDuration = 1.0f; // dùng nếu không có Animation Event

        private RewardManager manager;
        private RewardData currentReward;
        private State state;
        private readonly List<RewardItemView> spawnedItems = new List<RewardItemView>();

        public void Initialize(RewardManager manager)
        {
            this.manager = manager;
            fullScreenTapCatcher.onClick.RemoveAllListeners();
            fullScreenTapCatcher.onClick.AddListener(OnScreenTapped);
        }

        public void Show(RewardData reward)
        {
            currentReward = reward;
            ClearMultiRewardItems();

            if (reward.IsChest)
            {
                // Bước 1: hiện rương đóng, chờ tap để mở
                state = State.WaitingTapToOpenChest;
                SetSingleDisplay(reward.closedChestIcon != null ? reward.closedChestIcon : reward.icon, "");
                SetTapCatcherEnabled(true);
                tapToContinueText.text = "Tap to open";
            }
            else
            {
                // Reward thường: hiện luôn, chờ tap để nhận
                state = State.WaitingTapToClaim;
                SetSingleDisplay(reward.icon, reward.amount > 1 ? $"{reward.amount}" : "");
                SetTapCatcherEnabled(true);
                tapToContinueText.text = "Tap to continue";
            }
        }

        private void OnScreenTapped()
        {
            switch (state)
            {
                case State.WaitingTapToOpenChest:
                    PlayChestOpenAnimation();
                    break;

                case State.WaitingTapToClaim:
                    ClaimAndClose();
                    break;

                // Trong lúc đang chạy animation thì bỏ qua tap
                case State.PlayingChestAnimation:
                default:
                    break;
            }
        }

        private void PlayChestOpenAnimation()
        {
            state = State.PlayingChestAnimation;
            SetTapCatcherEnabled(false);
            tapToContinueText.text = "";

            if (chestAnimator != null)
            {
                chestAnimator.SetTrigger(chestOpenTrigger);
                // Nếu dùng Animation Event, gọi OnChestAnimationFinished() ở frame cuối của clip "Open".
                // Fallback: nếu không có Animation Event, tự đóng bằng coroutine timer.
                StartCoroutine(FallbackChestFinishTimer());
            }
            else
            {
                StartCoroutine(FallbackChestFinishTimer());
            }
        }

        private IEnumerator FallbackChestFinishTimer()
        {
            yield return new WaitForSeconds(chestAnimFallbackDuration);
            // Nếu Animation Event đã gọi trước rồi thì state sẽ khác PlayingChestAnimation, tránh gọi 2 lần
            if (state == State.PlayingChestAnimation)
                OnChestAnimationFinished();
        }

        /// <summary>Gọi bằng Animation Event ở keyframe cuối cùng của clip mở rương (khuyến nghị, chính xác hơn timer).</summary>
        public void OnChestAnimationFinished()
        {
            if (state != State.PlayingChestAnimation) return;

            var contents = currentReward.chestContents;
            if (contents == null || contents.Count == 0)
            {
                // Rương rỗng (không nên xảy ra), coi như đóng luôn
                ClaimAndClose();
                return;
            }

            if (contents.Count == 1)
            {
                SetSingleDisplay(contents[0].icon, contents[0].amount > 1 ? $"{contents[0].amount}" : "");
            }
            else
            {
                ShowMultiRewardDisplay(contents);
            }

            state = State.WaitingTapToClaim;
            tapToContinueText.text = "Tap to continue";
            SetTapCatcherEnabled(true);
        }

        private void ClaimAndClose()
        {
            SetTapCatcherEnabled(false);

            if (currentReward.IsChest)
            {
                foreach (var item in currentReward.chestContents)
                    manager.NotifyRewardClaimed(item);
            }
            else
            {
                manager.NotifyRewardClaimed(currentReward);
            }

            state = State.Idle;
            manager.NotifyPanelClosed();
        }

        // ---------- Helpers hiển thị ----------

        private void SetSingleDisplay(Sprite icon, string amount)
        {
            multiRewardContainer.gameObject.SetActive(false);
            mainIcon.gameObject.SetActive(true);
            mainIcon.sprite = icon;
            amountText.gameObject.SetActive(!string.IsNullOrEmpty(amount));
            amountText.text = amount;
        }

        private void ShowMultiRewardDisplay(List<RewardData> items)
        {
            mainIcon.gameObject.SetActive(false);
            amountText.gameObject.SetActive(false);
            multiRewardContainer.gameObject.SetActive(true);

            foreach (var item in items)
            {
                var view = Instantiate(multiRewardItemPrefab, multiRewardContainer);
                view.Setup(item.icon, item.amount);
                spawnedItems.Add(view);
            }
        }

        private void ClearMultiRewardItems()
        {
            foreach (var v in spawnedItems) if (v != null) Destroy(v.gameObject);
            spawnedItems.Clear();
        }

        private void SetTapCatcherEnabled(bool enabled)
        {
            fullScreenTapCatcher.interactable = enabled;
        }
    }
}
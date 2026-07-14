using AudioSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
            WaitingTapToOpenCard,
            PlayingCardAnimation,
            WaitingTapToClaim
        }

        [Header("Vùng bắt tap toàn màn hình")]
        [SerializeField] private Button fullScreenTapCatcher;

        [Header("Hiển thị nội dung")]
        [SerializeField] private Image mainIcon;           // icon rương (đóng) hoặc icon reward đơn
        [SerializeField] private TMP_Text amountText;          // "500" — đổi sang TMP_Text nếu cần
        [SerializeField] private TMP_Text tapToContinueText;

        [Header("Danh sách nhiều phần thưởng (khi rương chứa >1 item)")]
        [SerializeField] private Transform multiRewardContainer;
        [SerializeField] private RewardItemView multiRewardItemPrefab;

        [Header("Animation rương")]
        [SerializeField] private GameObject chestRoot;
        [SerializeField] private Animator chestAnimator;   // Animator gắn trên mainIcon hoặc object rương riêng
        [SerializeField] private string chestOpenTrigger = "Open";
        [SerializeField] private string chestCloseTrigger = "Close";
        [SerializeField] private float chestAnimFallbackDuration = 1.0f; // dùng nếu không có Animation Event


        [Header("Animation card")]
        [SerializeField] private GameObject cardRoot;
        [SerializeField] private Animator cardAnimator;   // Animator gắn trên mainIcon hoặc object card
        [SerializeField] private string cardOpenTrigger = "Open";
        [SerializeField] private string cardCloseTrigger = "Close";
        [SerializeField] private float cardAnimFallbackDuration = 1.0f;



        private RewardManager manager;
        [System.NonSerialized] private RewardData currentReward;
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
            AudioManager.Instance.PlaySFX("ClaimReward");

            if (reward.IsChest)
            {
                state = State.WaitingTapToOpenChest;
                SetChestDisplay();                          // ← dùng Chest, không dùng IconReward
                SetTapCatcherEnabled(true);
                tapToContinueText.text = "Tap to open";
            }
            else if (reward.IsCard)
            {
                state = State.WaitingTapToOpenCard;
                SetCardDisplay();
                SetTapCatcherEnabled(true);
                tapToContinueText.text = "Tap to open";
            }
            else
            {
                state = State.WaitingTapToClaim;
                SetSingleDisplay(reward.icon, FormatAmount(reward));
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

                case State.WaitingTapToOpenCard:
                    PlayCardOpenAnimation();
                    break;
                case State.PlayingCardAnimation:
                    break;

                default:
                    break;
            }
        }

        private void PlayChestOpenAnimation()
        {
            state = State.PlayingChestAnimation;
            SetTapCatcherEnabled(false);
            AudioManager.Instance.PlaySFX("OpenChest");
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
            chestAnimator.SetTrigger(chestCloseTrigger);
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
                var item = contents[0];
                SetSingleDisplay(item.icon, item.amount > 1 ? $"{item.amount}" : "");
            }
            else
            {
                ShowMultiRewardDisplay(contents);
            }

            state = State.WaitingTapToClaim;
            tapToContinueText.text = "Tap to continue";
            SetTapCatcherEnabled(true);
        }


        private void PlayCardOpenAnimation()
        {
            state = State.PlayingCardAnimation;
            SetTapCatcherEnabled(false);
            AudioManager.Instance.PlaySFX("OpenChest"); // hoặc SFX riêng cho card
            tapToContinueText.text = "";

            if (cardAnimator != null)
            {
                cardAnimator.SetTrigger(cardOpenTrigger);
                StartCoroutine(FallbackCardFinishTimer());
            }
            else
            {
                StartCoroutine(FallbackCardFinishTimer());
            }
        }

        private IEnumerator FallbackCardFinishTimer()
        {
            yield return new WaitForSeconds(cardAnimFallbackDuration);
            if (cardAnimator != null)
                cardAnimator.SetTrigger(cardCloseTrigger);
            if (state == State.PlayingCardAnimation)
                OnCardAnimationFinished();
        }

        public void OnCardAnimationFinished()
        {
            if (state != State.PlayingCardAnimation) return;

            var contents = currentReward.cardContents;
            if (contents == null || contents.Count == 0)
            {
                ClaimAndClose();
                return;
            }

            if (contents.Count == 1)
            {
                var item = contents[0];
                SetSingleDisplay(item.icon, item.amount > 1 ? $"{item.amount}" : "");
            }
            else
            {
                ShowMultiRewardDisplay(contents);
            }

            state = State.WaitingTapToClaim;
            tapToContinueText.text = "Tap to continue";
            SetTapCatcherEnabled(true);
        }

        private void SetCardDisplay()
        {
            if (cardRoot != null)
                cardRoot.SetActive(true);
            if (chestRoot != null)
                chestRoot.SetActive(false);

            mainIcon.gameObject.SetActive(false);
            amountText.gameObject.SetActive(false);
            multiRewardContainer.gameObject.SetActive(false);

            if (cardAnimator != null)
            {
                cardAnimator.Rebind();
                cardAnimator.Update(0f);
            }
        }

        private void ClaimAndClose()
        {
            SetTapCatcherEnabled(false);

            if (currentReward.IsChest)
            {
                foreach (var item in currentReward.chestContents)
                    manager.NotifyRewardClaimed(item.ToRewardData());
            }
            else if (currentReward.IsCard)
            {
                foreach (var item in currentReward.cardContents)
                    manager.NotifyRewardClaimed(item.ToRewardData());
            }
            else
            {
                manager.NotifyRewardClaimed(currentReward);
            }
            if (chestRoot != null) chestRoot.SetActive(false);
            if (cardRoot != null) cardRoot.SetActive(false);

            state = State.Idle;
            currentReward = null;
            manager.NotifyPanelClosed();
        }

        private void OnDisable()
        {
            currentReward = null;
            StopAllCoroutines();
        }

        // ---------- Helpers hiển thị ----------

        private void SetSingleDisplay(Sprite icon, string amount)
        {
            if (chestRoot != null)
                chestRoot.SetActive(false);
            if (cardRoot != null) cardRoot.SetActive(false);
            multiRewardContainer.gameObject.SetActive(false);
            mainIcon.gameObject.SetActive(true);
            mainIcon.sprite = icon;
            amountText.gameObject.SetActive(!string.IsNullOrEmpty(amount));
            amountText.text = amount;
        }

        private void ShowMultiRewardDisplay(List<RewardEntry> items)
        {
            if (chestRoot != null)
                chestRoot.SetActive(false);
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

        private void SetChestDisplay()
        {
            if (chestRoot != null)
                chestRoot.SetActive(true);
            mainIcon.gameObject.SetActive(false);
            amountText.gameObject.SetActive(false);
            multiRewardContainer.gameObject.SetActive(false);
            // Reset animator về trạng thái rương đóng (ChestWait)
            if (chestAnimator != null)
            {
                chestAnimator.Rebind();
                chestAnimator.Update(0f);
            }
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
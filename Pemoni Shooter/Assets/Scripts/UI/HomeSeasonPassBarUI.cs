using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeasonPass
{
    public class HomeSeasonPassBarUI : MonoBehaviour
    {
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image nextRewardIcon;

        private void OnEnable()
        {
            if (SeasonPassManager.Instance != null)
                SeasonPassManager.Instance.OnStateChanged += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (SeasonPassManager.Instance != null)
                SeasonPassManager.Instance.OnStateChanged -= Refresh;
        }

        private void Refresh()
        {
            var manager = SeasonPassManager.Instance;
            if (manager == null) return;

            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.interactable = false; // chỉ hiển thị, không kéo
                progressSlider.value = manager.GetCurrentLevelProgress01();
            }

            if (progressText != null)
            {
                int current = manager.GetPointsInCurrentLevel();
                int required = manager.GetPointsRequiredForCurrentLevelProgress();
                progressText.text = $"{current}/{required}";
            }

            if (nextRewardIcon != null)
            {
                var reward = manager.GetNextFreeReward();
                bool hasNext = reward != null && reward.icon != null;

                nextRewardIcon.gameObject.SetActive(hasNext);
                if (hasNext)
                    nextRewardIcon.sprite = reward.icon;
            }
        }
    }
}
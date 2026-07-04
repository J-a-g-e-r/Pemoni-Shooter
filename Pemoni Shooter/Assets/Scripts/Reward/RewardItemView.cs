using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RewardSystem
{
    /// <summary>1 icon + số lượng, dùng khi hiển thị nhiều phần thưởng cùng lúc (ví dụ rương chứa 3 loại item).</summary>
    public class RewardItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountText;

        public void Setup(Sprite sprite, int amount)
        {
            icon.sprite = sprite;

            if (amount < 1000)
            {
                amountText.text = amount > 1 ? $"x{amount}" : "";
            }
            else
            {
                amountText.text = FormatDuration(amount);
            }
        }

        private string FormatDuration(int seconds)
        {
            if (seconds >= 86400)
                return $"{seconds / 86400}d";

            if (seconds >= 3600)
                return $"{seconds / 3600}h";

            if (seconds >= 60)
                return $"{seconds / 60}m";

            return $"{seconds}s";
        }
    }
}
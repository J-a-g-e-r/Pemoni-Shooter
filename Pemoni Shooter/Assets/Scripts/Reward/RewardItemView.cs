using UnityEngine;
using UnityEngine.UI;

namespace RewardSystem
{
    /// <summary>1 icon + số lượng, dùng khi hiển thị nhiều phần thưởng cùng lúc (ví dụ rương chứa 3 loại item).</summary>
    public class RewardItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text amountText;

        public void Setup(Sprite sprite, int amount)
        {
            icon.sprite = sprite;
            amountText.text = amount > 1 ? $"x{amount}" : "";
        }
    }
}
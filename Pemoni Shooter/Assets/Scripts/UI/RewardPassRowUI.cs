using TMPro;
using UnityEngine;

namespace SeasonPass
{
    public class RewardPassRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text levelText;

        public void Setup(int levelIndex)
        {
            if (levelText != null)
                levelText.text = (levelIndex + 1).ToString();
        }
    }
}
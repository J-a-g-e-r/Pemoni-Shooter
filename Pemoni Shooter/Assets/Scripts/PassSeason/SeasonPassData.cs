using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeasonPass
{
    public enum RewardType
    {
        Coins,
        Gems,
        Chest,
        BoosterAdd,   // ví dụ item "+1"
        RandomSkin,
        SkillAddSlot,
        SkillSort,
        SkillSwap,
        InfiniteHeart,
        Other
    }

    [Serializable]
    public class RewardInfo
    {
        public RewardType type;
        public int amount = 1;
        public Sprite icon;
    }

    [Serializable]
    public class SeasonPassLevel
    {
        [Tooltip("Số điểm/EXP cần để mở khóa mốc này")]
        public int requiredPoints;

        [Header("Phần thưởng Free Pass (bên trái)")]
        public RewardInfo freeReward;

        [Header("Phần thưởng Gold Pass (bên phải)")]
        public RewardInfo goldReward;
    }

    [CreateAssetMenu(fileName = "NewSeasonPass", menuName = "SeasonPass/Season Pass Data")]
    public class SeasonPassData : ScriptableObject
    {
        [Header("Thông tin chung")]
        public string seasonName = "Bean Lucky";
        public Sprite bannerImage;

        [Tooltip("Thời gian mùa (giây), dùng để hiển thị đếm ngược kiểu 3d 15h")]
        public double seasonDurationSeconds;

        [Header("Giá kích hoạt Gold Pass")]
        public int goldPassPriceGems = 300;

        [Header("Danh sách các mốc thưởng, theo thứ tự tăng dần")]
        public List<SeasonPassLevel> levels = new List<SeasonPassLevel>();
    }
}
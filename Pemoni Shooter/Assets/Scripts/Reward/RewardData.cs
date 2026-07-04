using System;
using System.Collections.Generic;
using UnityEngine;

namespace RewardSystem
{
    public enum RewardType
    {
        Coins,
        Gems,
        Chest,
        Booster,
        Ticket,
        Item,
        SkillAddSlot,
        SkillSort,
        SkillSwap,
        InfiniteHeart,
        Other
    }

    /// <summary>Phần thưởng lá — không có chestContents, an toàn khi serialize.</summary>
    [Serializable]
    public class RewardEntry
    {
        public RewardType type;
        public int amount = 1;
        public Sprite icon;
        //public string displayName;

        public RewardEntry() { }

        public RewardEntry(RewardType type, int amount, Sprite icon)
        {
            this.type = type;
            this.amount = amount;
            this.icon = icon;
            //this.displayName = displayName;
        }

        public RewardData ToRewardData() => new RewardData(type, amount, icon);
    }

    [Serializable]
    public class RewardData
    {
        public RewardType type;
        public int amount = 1;
        public Sprite icon;
        public string displayName;

        [Tooltip("Chỉ dùng khi type = Chest: sprite rương lúc đóng")]
        public Sprite closedChestIcon;

        [Tooltip("Chỉ dùng khi type = Chest: phần thưởng bên trong (chỉ leaf, không lồng rương)")]
        [NonSerialized] public List<RewardEntry> chestContents = new List<RewardEntry>();

        public bool IsChest => type == RewardType.Chest;

        public RewardData() { }

        public RewardData(RewardType type, int amount, Sprite icon, string displayName = "")
        {
            this.type = type;
            this.amount = amount;
            this.icon = icon;
            this.displayName = displayName;
        }

        /// <summary>Loại bỏ phần thưởng lồng rương / tham chiếu lỗi trước khi grant.</summary>
        public static void Sanitize(RewardData reward)
        {
            if (reward == null || !reward.IsChest || reward.chestContents == null) return;

            for (int i = reward.chestContents.Count - 1; i >= 0; i--)
            {
                var entry = reward.chestContents[i];
                if (entry == null || entry.type == RewardType.Chest)
                    reward.chestContents.RemoveAt(i);
            }
        }
    }
}
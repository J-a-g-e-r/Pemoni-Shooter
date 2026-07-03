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

    [Serializable]
    public class RewardData
    {
        public RewardType type;
        public int amount = 1;
        public Sprite icon;
        public string displayName;

        [Tooltip("Chỉ dùng khi type = Chest: sprite rương lúc đóng")]
        public Sprite closedChestIcon;

        [Tooltip("Chỉ dùng khi type = Chest: danh sách phần thưởng bên trong rương")]
        public List<RewardData> chestContents = new List<RewardData>();

        public bool IsChest => type == RewardType.Chest;

        public RewardData() { }

        public RewardData(RewardType type, int amount, Sprite icon, string displayName = "")
        {
            this.type = type;
            this.amount = amount;
            this.icon = icon;
            this.displayName = displayName;
        }
    }
}
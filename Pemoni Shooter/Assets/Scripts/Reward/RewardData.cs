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
        Card,
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

        [Tooltip("Chỉ dùng khi type = Card: sprite card lúc đóng")]
        public Sprite closedCardIcon;
        [Tooltip("Chỉ dùng khi type = Card: phần thưởng bên trong")]
        [NonSerialized] public List<RewardEntry> cardContents = new List<RewardEntry>();

        public bool IsChest => type == RewardType.Chest;
        public bool IsCard => type == RewardType.Card;



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
            if (reward == null) return;

            if (reward.IsChest && reward.chestContents != null)
            {
                for (int i = reward.chestContents.Count - 1; i >= 0; i--)
                {
                    var entry = reward.chestContents[i];
                    if (entry == null || entry.type == RewardType.Chest || entry.type == RewardType.Card)
                        reward.chestContents.RemoveAt(i);
                }
            }

            if (reward.IsCard && reward.cardContents != null)
            {
                for (int i = reward.cardContents.Count - 1; i >= 0; i--)
                {
                    var entry = reward.cardContents[i];
                    if (entry == null || entry.type == RewardType.Chest || entry.type == RewardType.Card)
                        reward.cardContents.RemoveAt(i);
                }
            }
        }
    }
}
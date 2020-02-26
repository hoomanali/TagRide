using System;
using System.ComponentModel;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TagRides.Shared.Game
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GameItem : INotifyPropertyChanged
    {
        /// <summary>
        /// What does this item effect when used
        /// </summary>
        public enum Effect
        {
            /// <summary>
            /// Has no effect; can't be consumed
            /// </summary>
            None,
            /// <summary>
            /// Boosts exp gain
            /// </summary>
            Level,
            /// <summary>
            /// Boosts exp gain for the kingdom 
            /// </summary>
            Kingdom
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// A unique name for this item stack
        /// </summary>
        [JsonProperty]
        public readonly string Name;
        [JsonProperty]
        public readonly Effect EffectType;
        /// <summary>
        /// A multiplier used to impact the effected stat
        /// </summary>
        [JsonProperty]
        public readonly double EffectMultiplier;
        /// <summary>
        /// The max number of this item that can be had. -1 means there is no limit
        /// </summary>
        [JsonProperty]
        public readonly int MaxCount;
        /// <summary>
        /// The path to the icon after TagRideProperties.ThemeResourceBase
        /// </summary>
        [JsonProperty]
        public readonly string IconPath;
        /// <summary>
        /// Flavor description of the item
        /// </summary>
        [JsonProperty]
        public readonly string FlavorText;
            
        public int Count
        {
            get => count;
            set
            {
                if (MaxCount != -1 && (value < 0 || value > MaxCount))
                    throw new ArgumentOutOfRangeException();

                count = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            }
        }

        public GameItem(
            string name, 
            Effect effectType, 
            double effectMultiplier, 
            int maxCount = -1, 
            int startCount = 0, 
            string iconPath = null, 
            string flavorText = null)
        {
            Name = name;
            EffectType = effectType;
            EffectMultiplier = effectMultiplier;
            MaxCount = maxCount;
            Count = startCount;
            IconPath = iconPath;
            FlavorText = flavorText;
        }

        [JsonProperty]
        int count = 0;
    }

    public class GameMultipliers
    {
        public double Level = 1;
        public double Kingdom = 1;
        
        public GameMultipliers(GameItem item)
        {
            ApplyItem(item);
        }

        public GameMultipliers(IEnumerable<GameItem> items)
        {
            foreach (var i in items)
                ApplyItem(i);
        }

        void ApplyItem(GameItem item)
        {
            if (item == null) return;

            switch (item.EffectType)
            {
                case GameItem.Effect.Level:
                    Level *= item.EffectMultiplier;
                    break;
                case GameItem.Effect.Kingdom:
                    Kingdom *= item.EffectMultiplier;
                    break;
            }
        }
    }
}

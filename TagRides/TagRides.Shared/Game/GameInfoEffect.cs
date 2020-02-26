using System;
using System.Collections.Generic;
using System.Text;
using TagRides.Shared.UserProfile;

namespace TagRides.Shared.Game
{
    public static class GameInfoEffectUtils
    {
        public static void ApplyGameInfoEffect(this GameInfo gameInfo, GameInfoEffectBase effect)
        {
            switch (effect)
            {
                case LevelEffect levelEffect:
                    gameInfo.Level.Points += levelEffect.ExpGain;
                    break;
                case KingdomEffect kingdomEffect:
                    gameInfo.Kingdom.Points += kingdomEffect.ExpGain;
                    break;
                case ItemEffect itemEffect:
                    gameInfo.Inventory.AddOrUpdate(itemEffect.Item);
                    break;
                case CompoundEffect compoundEffect:
                    foreach (var e in compoundEffect.Effects)
                        gameInfo.ApplyGameInfoEffect(e);
                    break;
            }
        }
    }

    /// <summary>
    /// Used to track why a User's game stats are changed
    /// </summary>
    public class GameInfoEffectBase
    {
        public readonly string AffectedEntity;
        /// <summary>
        /// The ridesharing related reason the stat was changed
        /// </summary>
        public readonly string Justification;
        /// <summary>
        /// A game flavored reason the stat was changed
        /// </summary>
        public readonly string FlavorJustification;

        /// <summary>
        /// The id of the effect, used for identification
        /// </summary>
        public readonly string Id;

        public GameInfoEffectBase(string affectedEntity, string justification, string flavorJustification, string id)
        {
            AffectedEntity = affectedEntity;
            Justification = justification;
            FlavorJustification = flavorJustification;
            Id = id;
        }

        public static GameInfo operator +(GameInfo gameInfo, GameInfoEffectBase effect)
        {
            gameInfo.ApplyGameInfoEffect(effect);
            return gameInfo;
        }
    }
    
    public class LevelEffect : GameInfoEffectBase
    {
        public readonly int ExpGain;

        public LevelEffect(int expGain, string justification, string flavorJustification, string id)
            : base(nameof(GameInfo.Level), justification, flavorJustification, id)
        {
            ExpGain = expGain;
        }
    }

    public class KingdomEffect : GameInfoEffectBase
    {
        public readonly int ExpGain;

        public KingdomEffect(int expGain, string justification, string flavorJustification, string id)
            : base(nameof(GameInfo.Kingdom), justification, flavorJustification, id)
        {
            ExpGain = expGain;
        }
    }

    public class ItemEffect : GameInfoEffectBase
    {
        public readonly GameItem Item;

        public ItemEffect(GameItem item, string justification, string flavorJustification, string id)
            : base(nameof(GameInfo.Inventory), justification, flavorJustification, id)
        {
            Item = item;
        }
    }

    public class CompoundEffect : GameInfoEffectBase
    {
        public readonly IEnumerable<GameInfoEffectBase> Effects;

        public CompoundEffect(IEnumerable<GameInfoEffectBase> effects, string justification, string flavorJustification, string id)
            : base(nameof(CompoundEffect), justification, flavorJustification, id)
        {
            Effects = effects;
        }
    }
}

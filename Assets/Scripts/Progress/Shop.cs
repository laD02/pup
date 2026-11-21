using System.Linq;
using SealCatapult.Core;

namespace SealCatapult.Progress
{
    /// <summary>Star wallet and upgrade purchases. Stars are earned, never consumed from levels.</summary>
    public static class Shop
    {
        public static int TotalStars(SaveData data) => data.stars.Sum();

        public static int AvailableStars(SaveData data) => TotalStars(data) - data.spentStars;

        public static int Level(SaveData data, UpgradeId id) => data.upgrades[(int)id];

        /// <summary>Cost of the next level, or -1 when maxed.</summary>
        public static int NextCost(SaveData data, UpgradeDef def)
        {
            int level = Level(data, def.id);
            return level >= UpgradeData.MaxLevel ? -1 : def.costs[level];
        }

        public static bool CanBuy(SaveData data, UpgradeDef def)
        {
            int cost = NextCost(data, def);
            return cost >= 0 && AvailableStars(data) >= cost;
        }

        /// <summary>Blaze at its top level melts through every ice cube.</summary>
        public const int BlazeAll = 99;

        /// <summary>Ice cubes a blazing pup melts through at an upgrade level.</summary>
        public static int BlazePierce(int level) => level >= UpgradeData.MaxLevel ? BlazeAll : level * 2;

        /// <summary>The number an upgrade's description names at <paramref name="level"/>, the value the game really
        /// uses (the shop showed the level number for every upgrade: Ice Spikes 1 smashes 2 blocks, Blaze 1 melts 2).</summary>
        public static int Effect(UpgradeId id, int level, int basePierce) => id switch
        {
            UpgradeId.Spiky => basePierce + level,
            UpgradeId.Blaze => BlazePierce(level),
            _ => level,   // bounces, pups and ninja stops: one per level
        };

        /// <summary>Spends stars and raises the level. Caller persists.</summary>
        public static bool TryBuy(SaveData data, UpgradeDef def)
        {
            if (!CanBuy(data, def)) return false;
            data.spentStars += NextCost(data, def);
            data.upgrades[(int)def.id]++;
            return true;
        }
    }
}

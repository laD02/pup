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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PufferLaunch.Progress
{
    /// <summary>Order matters: it is the index into SaveData.upgrades.</summary>
    public enum UpgradeId { Spiky = 0, PuffUp = 1, School = 2, Ninja = 3, Blaze = 4 }

    [Serializable]
    public class UpgradeDef
    {
        public UpgradeId id;
        public string icon;              // sprite name in Resources/Art
        public int[] costs = new int[UpgradeData.MaxLevel];
    }

    /// <summary>Upgrade catalogue. Each level adds +1 to the effect (pierce, bounce, pups, ninja turns, blaze).</summary>
    [CreateAssetMenu(menuName = "Puffer Launch/Upgrades", fileName = "Upgrades")]
    public class UpgradeData : ScriptableObject
    {
        public const int MaxLevel = 3;
        public List<UpgradeDef> upgrades = new List<UpgradeDef>();

        public UpgradeDef Get(UpgradeId id) => upgrades.Find(u => u.id == id);
    }
}

using UnityEngine;

namespace SealCatapult.Progress
{
    /// <summary>Root data asset at Resources/GameDatabase: levels, upgrades and tuning.</summary>
    [CreateAssetMenu(menuName = "Seal Catapult/Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public LevelData[] levels;
        public UpgradeData upgrades;
        public GameTuning tuning;

        static GameDatabase instance;

        public static GameDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameDatabase>("GameDatabase");
                    if (instance == null)
                        Debug.LogError("[SealCatapult] Resources/GameDatabase.asset missing. Run Seal Catapult > Build Project.");
                    else if (instance.levels == null || instance.levels.Length != Core.SaveData.LevelCount)
                        // The save keeps stars per level in arrays of LevelCount: a mismatch indexes out of range at Finish.
                        Debug.LogError($"[SealCatapult] GameDatabase has {instance.levels?.Length ?? 0} levels, the save expects {Core.SaveData.LevelCount}.");
                }
                return instance;
            }
        }
    }
}

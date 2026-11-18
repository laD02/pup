using System.Collections.Generic;

namespace SealCatapult.Progress
{
    /// <summary>
    /// Every level by its index, without end: the 18 hand-made assets first (GameDatabase.levels), then levels built by
    /// <see cref="LevelGenerator"/>. Castle k holds the indices k·PerCastle … k·PerCastle+PerCastle−1 (one CPC-style map).
    /// </summary>
    public static class LevelCatalog
    {
        public const int PerCastle = LevelGenerator.PerCastle;
        static readonly Dictionary<int, LevelData> built = new Dictionary<int, LevelData>();

        public static int Handmade => GameDatabase.Instance.levels.Length;

        /// <summary>The game has ten castles (user 17/11: "tối đa 10 thành"); the last level is the tenth castle's boss.</summary>
        public const int Castles = 10;
        public static int Total => Castles * PerCastle;
        public static int CastleOf(int index) => index / PerCastle;
        public static int SlotOf(int index) => index % PerCastle;
        public static int FirstOf(int castle) => castle * PerCastle;

        public static LevelData Get(int index)
        {
            if (index < 0) index = 0;
            var db = GameDatabase.Instance;
            if (index < db.levels.Length) return db.levels[index];
            if (!built.TryGetValue(index, out var level) || level == null)   // null: destroyed when Play mode ended
            {
                level = LevelGenerator.Build(CastleOf(index), SlotOf(index)).ToLevel(db.tuning);
                level.name = $"Castle {CastleOf(index) + 1} · {SlotOf(index) + 1}";
                built[index] = level;
            }
            return level;
        }

        /// <summary>The level's kind without building it (the map asks for sixty at a time).</summary>
        public static LevelKind KindOf(int index) =>
            index < GameDatabase.Instance.levels.Length ? GameDatabase.Instance.levels[index].kind : LevelGenerator.KindForSlot(SlotOf(index));

        /// <summary>The castle's own boss, last on its map.</summary>
        public static bool IsFinal(int index) => SlotOf(index) == LevelGenerator.FinalSlot;

        // The Editor keeps statics between plays (domain reload off): start each play with a fresh cache.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => built.Clear();
    }
}

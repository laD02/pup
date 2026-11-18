using SealCatapult.Core;
using UnityEngine;

namespace SealCatapult.UI
{
    /// <summary>
    /// The ten castles (user 17/11: "tối đa 10 thành"), each its own world: a map painting with the trail through it, a
    /// name and a colour that the catapult pit and the arena take on too (user: "các pha cx phải theo màu của thành").
    /// Index = castle. Paintings: Assets/Art/Reference/map_&lt;key&gt;.png → Resources/Art/map_bg_&lt;key&gt;.png; trails:
    /// MapTrails (Tools/map_trail.py).
    /// </summary>
    public static class WorldTheme
    {
        public const int Count = SealCatapult.Progress.LevelCatalog.Castles;

        readonly struct World
        {
            public readonly string key, nameKey;
            public readonly Color tint;   // multiplied into the pit painting, the arena sky and its rock

            public World(string key, string nameKey, Color tint)
            {
                this.key = key;
                this.nameKey = nameKey;
                this.tint = tint;
            }
        }

        static readonly World[] Worlds =
        {
            new World("world", "map_title", Color.white),                                    // Frost Bay: blue
            new World("canyon", "world_canyon", new Color(0.86f, 0.74f, 1f)),               // violet
            new World("forest", "world_forest", new Color(0.66f, 0.96f, 0.84f)),            // emerald
            new World("harbour", "world_harbour", new Color(1f, 0.8f, 0.62f)),              // sunset orange
            new World("caves", "world_caves", new Color(0.62f, 0.98f, 1f)),                 // turquoise
            new World("volcano", "world_volcano", new Color(1f, 0.66f, 0.58f)),             // red
            new World("blossom", "world_blossom", new Color(1f, 0.8f, 0.9f)),               // pink
            new World("palace", "world_palace", new Color(0.62f, 0.66f, 0.92f)),            // midnight navy
            new World("tundra", "world_tundra", new Color(1f, 0.86f, 0.62f)),               // amber
            new World("throne", "world_throne", new Color(0.78f, 0.72f, 0.8f)),             // the Bear King's grey storm
        };

        static World Of(int castle) => Worlds[Mathf.Clamp(castle, 0, Count - 1)];

        public static string Painting(int castle) => "map_bg_" + Of(castle).key;
        /// <summary>The world's short name: the key of its painting, trail and weather (MapWeather).</summary>
        public static string Key(int castle) => Of(castle).key;
        public static string Name(int castle) => Loc.T(Of(castle).nameKey);
        public static Color Tint(int castle) => Of(castle).tint;

        /// <summary>The castle's own version of a piece of scenery (user 17/11: "mỗi thành thì cx thay đổi phong cảnh trong
        /// cả pha 1 và pha 2 (đất đá, băng…)"): "bg_pit" → "bg_pit_volcano" when that painting exists, else the Frost Bay
        /// original. Paintings: Tools/prep_backgrounds.py SCENES.</summary>
        public static string Scene(string sprite, int castle)
        {
            string own = sprite + "_" + Of(castle).key;
            return Art.Has(own) ? own : sprite;
        }

        /// <summary>The colour to multiply into a piece of scenery: none when the castle has its own painting of it (the
        /// painting already carries the castle's colours), else the castle's tint over the Frost Bay art.</summary>
        public static Color SceneTint(string sprite, int castle) => Scene(sprite, castle) == sprite ? Tint(castle) : Color.white;

        /// <summary>The trail through this world's painting (painting px, y down), or null when not made yet.</summary>
        public static Vector2[] Trail(int castle)
        {
            switch (Of(castle).key)
            {
                case "canyon": return MapTrails.Canyon;
                case "forest": return MapTrails.Forest;
                case "harbour": return MapTrails.Harbour;
                case "caves": return MapTrails.Caves;
                case "volcano": return MapTrails.Volcano;
                case "blossom": return MapTrails.Blossom;
                case "palace": return MapTrails.Palace;
                case "tundra": return MapTrails.Tundra;
                case "throne": return MapTrails.Throne;
                default: return null;
            }
        }

        /// <summary>Open ground beside the trail where a polar bear stands and looks around (painting px), or null.</summary>
        public static Vector2[] Bears(int castle)
        {
            switch (Of(castle).key)
            {
                case "world": return MapTrails.WorldBears;
                case "canyon": return MapTrails.CanyonBears;
                case "forest": return MapTrails.ForestBears;
                case "harbour": return MapTrails.HarbourBears;
                case "caves": return MapTrails.CavesBears;
                case "volcano": return MapTrails.VolcanoBears;
                case "blossom": return MapTrails.BlossomBears;
                case "palace": return MapTrails.PalaceBears;
                case "tundra": return MapTrails.TundraBears;
                case "throne": return MapTrails.ThroneBears;
                default: return null;
            }
        }

        /// <summary>Open-water points of this world's painting (painting px, y down) where fish leap; null for worlds
        /// whose surroundings are lava or ice (MapTrails, Tools/map_trail.py SEA).</summary>
        public static Vector2[] Water(int castle)
        {
            switch (Of(castle).key)
            {
                case "world": return MapTrails.WorldWater;
                case "canyon": return MapTrails.CanyonWater;
                case "harbour": return MapTrails.HarbourWater;
                case "caves": return MapTrails.CavesWater;
                case "palace": return MapTrails.PalaceWater;
                case "throne": return MapTrails.ThroneWater;
                default: return null;
            }
        }
    }
}

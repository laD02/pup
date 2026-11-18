using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SealCatapult.Progress
{
    /// <summary>
    /// A level written as building pieces (rock, ice, bears, objects) in arena units: x = 0 at the open left edge,
    /// AttackPhase.ArenaWidth (13) at the rock wall, y = 0 on the cobble floor. The hand-made levels (Editor/LevelDesigns)
    /// and the generated castles (LevelGenerator) both use it; <see cref="ToLevel"/> turns one into a LevelData.
    /// </summary>
    public class LevelBlueprint
    {
        public const float Cube = 0.8f;
        const float ShelfThick = 1.4f;            // floating rock ledge thickness (0.7 was all snow cap)
        public const float EdgeX = 13.6f;         // the edge perch's centre: past ArenaWidth 13, so the view cuts it in half
        const float PillarWidth = 1.0f;
        const float ThreeStarShare = 0.6f;

        public int puffers;
        public bool tutorial;
        public int requiredBears;      // 0 = every bear
        public LevelKind kind = LevelKind.Normal;
        public LaunchStyle launcher = LaunchStyle.Swing;
        public int zone;               // arena palette (AttackPhase.Palettes) and map page
        public float caveFrom, caveTo, gapFrom, gapTo;
        public readonly List<TerrainSpec> terrain = new List<TerrainSpec>();
        public readonly List<BlockSpec> blocks = new List<BlockSpec>();
        public readonly List<BearSpec> bears = new List<BearSpec>();
        public readonly List<ObjectSpec> objects = new List<ObjectSpec>();

        public LevelBlueprint(int puffers, bool tutorial = false)
        {
            this.puffers = puffers;
            this.tutorial = tutorial;
        }

        /// <summary>Rock plateau: centre x, bottom y, width, height.</summary>
        public LevelBlueprint Rock(float x, float y, float w, float h)
        {
            terrain.Add(new TerrainSpec { shape = TerrainShape.Block, position = new Vector2(x, y + h * 0.5f), size = new Vector2(w, h) });
            return this;
        }

        /// <summary>Rock ramp from its left edge: rising toward +x when upRight, falling otherwise.</summary>
        public LevelBlueprint Ramp(float left, float y, float w, float h, bool upRight)
        {
            terrain.Add(new TerrainSpec
            {
                shape = upRight ? TerrainShape.RampUpRight : TerrainShape.RampUpLeft,
                position = new Vector2(left + w * 0.5f, y + h * 0.5f),
                size = new Vector2(w, h),
            });
            return this;
        }

        public LevelBlueprint Pillar(float x, float h) => Rock(x, 0f, PillarWidth, h);

        /// <summary>Floating ice shelf: centre x, top y, width (0.7 thick). The reference hangs one at the right edge, half cut off.</summary>
        public LevelBlueprint Shelf(float x, float top, float w) 
        {
            terrain.Add(new TerrainSpec { shape = TerrainShape.IceShelf, position = new Vector2(x, top - ShelfThick * 0.5f), size = new Vector2(w, ShelfThick) });
            return this;
        }

        /// <summary>The reference's right-edge perch: a shelf past the arena's end with a bear on it, half off screen.</summary>
        public LevelBlueprint EdgePerch(float top, bool helmet = false) => Shelf(EdgeX, top, 2.4f).Bear(EdgeX, top, helmet: helmet);

        public LevelBlueprint Box(float x, float y, float w, float h, BlockMaterial m)
        {
            blocks.Add(new BlockSpec { material = m, position = new Vector2(x, y + h * 0.5f), size = new Vector2(w, h) });
            return this;
        }

        /// <summary>A column of n ice cubes standing at centre x from bottom y.</summary>
        public LevelBlueprint Cubes(float x, float y, int n, BlockMaterial m = BlockMaterial.Ice)
        {
            for (int i = 0; i < n; i++) Box(x, y + i * Cube, Cube, Cube, m);
            return this;
        }

        /// <summary>An ice wedge the size of a cube, centre x, bottom y: rising to the right, or to the left.</summary>
        public LevelBlueprint Wedge(float x, float y, bool upRight)
        {
            blocks.Add(new BlockSpec { material = BlockMaterial.Ice, position = new Vector2(x, y + Cube * 0.5f), size = new Vector2(Cube, Cube), wedge = upRight ? 1 : -1 });
            return this;
        }

        /// <summary>A beam of n ice cubes side by side, its left cube centred at x, bottom at y.</summary>
        public LevelBlueprint Row(float x, float y, int n)
        {
            for (int i = 0; i < n; i++) Box(x + i * Cube, y, Cube, Cube, BlockMaterial.Ice);
            return this;
        }

        public LevelBlueprint Bear(float x, float y, bool helmet = false, BearMood mood = BearMood.Guard)
        {
            bears.Add(new BearSpec { position = new Vector2(x, y), helmet = helmet, mood = mood });
            return this;
        }

        LevelBlueprint Obj(ObjectKind kind, float x, float y, int group = 0, int value = 0)
        {
            objects.Add(new ObjectSpec { kind = kind, position = new Vector2(x, y), group = group, value = value });
            return this;
        }

        public LevelBlueprint Bumper(float x, float y) => Obj(ObjectKind.Bumper, x, y);
        public LevelBlueprint Tnt(float x, float y) => Obj(ObjectKind.Tnt, x, y + 0.4f);
        public LevelBlueprint Button(float x, float y, int group) => Obj(ObjectKind.Button, x, y, group);
        public LevelBlueprint Fish(float x, float y) => Obj(ObjectKind.Fish, x, y);
        public LevelBlueprint Cage(float x, float y) => Obj(ObjectKind.Cage, x, y);
        public LevelBlueprint Boss(float x, float y, int lives, int group) => Obj(ObjectKind.Boss, x, y, group, lives);

        /// <summary>Ice gate: a column of cubes that shatters when a button of the group is pressed.</summary>
        public LevelBlueprint Gate(float x, float y, int n, int group)
        {
            for (int i = 0; i < n; i++) blocks.Add(new BlockSpec { material = BlockMaterial.Ice, position = new Vector2(x, y + i * Cube + Cube * 0.5f), size = new Vector2(Cube, Cube), group = group });
            return this;
        }

        public LevelBlueprint Zone(int z)
        {
            zone = z;
            return this;
        }

        public LevelBlueprint Kind(LevelKind k)
        {
            kind = k;
            return this;
        }

        /// <summary>A hidden chamber under the floor from x a to b, opened by a gap from g0 to g1. Things inside sit at
        /// y = -AttackPhase.CaveDepth.</summary>
        public LevelBlueprint Cave(float a, float b, float g0, float g1)
        {
            caveFrom = a; caveTo = b; gapFrom = g0; gapTo = g1;
            return this;
        }

        /// <summary>The pup bobs on a bungee above the catapult instead of swinging on the rope.</summary>
        public LevelBlueprint Bungee()
        {
            launcher = LaunchStyle.Bungee;
            return this;
        }

        /// <summary>No rope: the pup jumps off the bridge and drops past the catapult.</summary>
        public LevelBlueprint Drop()
        {
            launcher = LaunchStyle.Drop;
            return this;
        }

        /// <summary>Bears that must go down to pass (the rest are for the Perfect badge).</summary>
        public LevelBlueprint Need(int n)
        {
            requiredBears = n;
            return this;
        }

        /// <summary>A runtime LevelData for this blueprint (the generated castles; hand-made levels are assets).</summary>
        public LevelData ToLevel(GameTuning tuning)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            Fill(level, tuning);
            return level;
        }

        public void Fill(LevelData level, GameTuning tuning)
        {
            level.pufferCount = puffers;
            level.requiredBears = requiredBears;
            level.launcher = launcher;
            level.caveFrom = caveFrom;
            level.caveTo = caveTo;
            level.gapFrom = gapFrom;
            level.gapTo = gapTo;
            level.kind = kind;
            level.zone = zone;
            level.showTutorial = tutorial;
            level.terrain = terrain.ToList();
            level.blocks = blocks.ToList();
            level.bears = bears.ToList();
            level.objects = objects.ToList();
            level.threeStarScore = ThreeStarScore(this, tuning);
        }

        /// <summary>3-star threshold: 60% of everything breakable in the level, rounded to 50.</summary>
        public static int ThreeStarScore(LevelBlueprint d, GameTuning t)
        {
            int total = d.bears.Count * t.bearScore + d.objects.Count(o => o.kind == ObjectKind.Fish) * t.fishScore + d.blocks.Sum(b => b.material switch
            {
                BlockMaterial.Snow => t.snowScore,
                BlockMaterial.Stone => t.stoneScore,
                _ => t.iceScore,
            });
            return Mathf.RoundToInt(total * ThreeStarShare / 50f) * 50;
        }
    }
}

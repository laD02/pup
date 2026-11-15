using System.Collections.Generic;
using System.Linq;
using PufferLaunch.Progress;
using UnityEngine;

namespace PufferLaunch.EditorTools
{
    /// <summary>
    /// Source of truth for the 10 Frost Bay levels. ProjectBuilder writes them into Assets/Data/Level_XX.asset;
    /// after that, tweak numbers in the Inspector (or here + "Rebuild Levels").
    /// The arena is AttackPhase.ArenaWidth (13) wide: x = 0 at the open left edge, 13 at the rock wall; y = 0 on the
    /// cobble floor. Terrain (ramps, plateaus, pillars) is solid rock spread across the width, ice cubes stack on it,
    /// and bears stand exposed on top, as in the genre's level screens. Pups glide in level from the left near the top.
    /// </summary>
    public static class LevelDesigns
    {
        const float Cube = 0.8f;
        const float ShelfThick = 1.4f;            // floating rock ledge thickness (0.7 was all snow cap)
        const float EdgeX = 13.6f;                // the edge perch's centre: past ArenaWidth 13, so the view cuts it in half
        const float PillarWidth = 1.0f;
        const float ThreeStarShare = 0.6f;

        public class Design
        {
            public int puffers;
            public bool tutorial;
            public int requiredBears;      // 0 = every bear
            public LevelKind kind = LevelKind.Normal;
            public LaunchStyle launcher = LaunchStyle.Swing;
            public float caveFrom, caveTo, gapFrom, gapTo;
            public readonly List<TerrainSpec> terrain = new List<TerrainSpec>();
            public readonly List<BlockSpec> blocks = new List<BlockSpec>();
            public readonly List<BearSpec> bears = new List<BearSpec>();
            public readonly List<ObjectSpec> objects = new List<ObjectSpec>();

            public Design(int puffers, bool tutorial = false)
            {
                this.puffers = puffers;
                this.tutorial = tutorial;
            }

            /// <summary>Rock plateau: centre x, bottom y, width, height.</summary>
            public Design Rock(float x, float y, float w, float h)
            {
                terrain.Add(new TerrainSpec { shape = TerrainShape.Block, position = new Vector2(x, y + h * 0.5f), size = new Vector2(w, h) });
                return this;
            }

            /// <summary>Rock ramp from its left edge: rising toward +x when upRight, falling otherwise.</summary>
            public Design Ramp(float left, float y, float w, float h, bool upRight)
            {
                terrain.Add(new TerrainSpec
                {
                    shape = upRight ? TerrainShape.RampUpRight : TerrainShape.RampUpLeft,
                    position = new Vector2(left + w * 0.5f, y + h * 0.5f),
                    size = new Vector2(w, h),
                });
                return this;
            }

            public Design Pillar(float x, float h) => Rock(x, 0f, PillarWidth, h);

            /// <summary>Floating ice shelf: centre x, top y, width (0.7 thick). The reference hangs one at the right edge, half cut off.</summary>
            public Design Shelf(float x, float top, float w) 
            {
                terrain.Add(new TerrainSpec { shape = TerrainShape.IceShelf, position = new Vector2(x, top - ShelfThick * 0.5f), size = new Vector2(w, ShelfThick) });
                return this;
            }

            /// <summary>The reference's right-edge perch: a shelf past the arena's end with a bear on it, half off screen.</summary>
            public Design EdgePerch(float top, bool helmet = false) => Shelf(EdgeX, top, 2.4f).Bear(EdgeX, top, helmet: helmet);

            public Design Box(float x, float y, float w, float h, BlockMaterial m)
            {
                blocks.Add(new BlockSpec { material = m, position = new Vector2(x, y + h * 0.5f), size = new Vector2(w, h) });
                return this;
            }

            /// <summary>A column of n ice cubes standing at centre x from bottom y.</summary>
            public Design Cubes(float x, float y, int n, BlockMaterial m = BlockMaterial.Ice)
            {
                for (int i = 0; i < n; i++) Box(x, y + i * Cube, Cube, Cube, m);
                return this;
            }

            /// <summary>An ice wedge the size of a cube, centre x, bottom y: rising to the right, or to the left.</summary>
            public Design Wedge(float x, float y, bool upRight)
            {
                blocks.Add(new BlockSpec { material = BlockMaterial.Ice, position = new Vector2(x, y + Cube * 0.5f), size = new Vector2(Cube, Cube), wedge = upRight ? 1 : -1 });
                return this;
            }

            /// <summary>A beam of n ice cubes side by side, its left cube centred at x, bottom at y.</summary>
            public Design Row(float x, float y, int n)
            {
                for (int i = 0; i < n; i++) Box(x + i * Cube, y, Cube, Cube, BlockMaterial.Ice);
                return this;
            }

            public Design Bear(float x, float y, bool helmet = false, BearMood mood = BearMood.Guard)
            {
                bears.Add(new BearSpec { position = new Vector2(x, y), helmet = helmet, mood = mood });
                return this;
            }

            Design Obj(ObjectKind kind, float x, float y, int group = 0, int value = 0)
            {
                objects.Add(new ObjectSpec { kind = kind, position = new Vector2(x, y), group = group, value = value });
                return this;
            }

            public Design Bumper(float x, float y) => Obj(ObjectKind.Bumper, x, y);
            public Design Tnt(float x, float y) => Obj(ObjectKind.Tnt, x, y + 0.4f);
            public Design Button(float x, float y, int group) => Obj(ObjectKind.Button, x, y, group);
            public Design Fish(float x, float y) => Obj(ObjectKind.Fish, x, y);
            public Design Cage(float x, float y) => Obj(ObjectKind.Cage, x, y);
            public Design Boss(float x, float y, int lives, int group) => Obj(ObjectKind.Boss, x, y, group, lives);

            /// <summary>Ice gate: a column of cubes that shatters when a button of the group is pressed.</summary>
            public Design Gate(float x, float y, int n, int group)
            {
                for (int i = 0; i < n; i++) blocks.Add(new BlockSpec { material = BlockMaterial.Ice, position = new Vector2(x, y + i * Cube + Cube * 0.5f), size = new Vector2(Cube, Cube), group = group });
                return this;
            }

            public Design Kind(LevelKind k)
            {
                kind = k;
                return this;
            }

            /// <summary>A hidden chamber under the floor from x a to b, opened by a gap from g0 to g1. Things inside sit at
            /// y = -AttackPhase.CaveDepth.</summary>
            public Design Cave(float a, float b, float g0, float g1)
            {
                caveFrom = a; caveTo = b; gapFrom = g0; gapTo = g1;
                return this;
            }

            /// <summary>The pup bobs on a bungee above the catapult instead of swinging on the rope.</summary>
            public Design Bungee()
            {
                launcher = LaunchStyle.Bungee;
                return this;
            }

            /// <summary>Bears that must go down to pass (the rest are for the Perfect badge).</summary>
            public Design Need(int n)
            {
                requiredBears = n;
                return this;
            }
        }

        const BlockMaterial S = BlockMaterial.Snow;
        const BearMood Dangle = BearMood.Dangle;
        const float Deep = PufferLaunch.Attack.AttackPhase.CaveDepth;
        const BearMood Eat = BearMood.Eat, Sleep = BearMood.Sleep;

        public static Design[] All()
        {
            const float C2 = Cube * 2, C3 = Cube * 3;
            return new[]
            {
                // 1 · tutorial: one hill in the middle (ramp up, plateau, ramp down), a bear on top
                new Design(5, tutorial: true)
                    .Ramp(2.5f, 0f, 2f, 1.4f, true).Rock(6.2f, 0f, 3.4f, 1.4f).Ramp(7.9f, 0f, 2f, 1.4f, false)
                    .Bear(6.2f, 1.4f, mood: Eat).EdgePerch(3.4f).Need(1),   // the perch bear is a bonus target on the tutorial
                // 2 · the arch (reference §17.1, mission 2): two 3-cube columns under a 5-cube beam; two bears on the
                // beam, two sheltered under it, one outside, one on the edge perch. Blocks stand still, so the beam
                // must be broken (or a bounce must come in from the side) to reach the bears below.
                new Design(6, tutorial: true)
                    .Cubes(5.2f, 0f, 3).Cubes(8.4f, 0f, 3).Row(5.2f, C3, 5)
                    .Wedge(4.4f, 0f, true).Wedge(9.2f, 0f, false).Wedge(4.4f, C3, true).Wedge(9.2f, C3, false)
                    .Bear(6f, C3 + Cube).Bear(7.6f, C3 + Cube)
                    .Bear(6.2f, 0f, mood: Eat).Bear(7.5f, 0f).Bear(3.4f, 0f).EdgePerch(3.6f).Need(3),
                // 3 · the tower and the T (mission 3): a 6-cube column, a beam off its middle with two bears, an ice step
                // down to the right with a lookout, three bears on the floor
                // Still a teaching level (GAME_DESIGN §R5: 1–3): hints and the easier launch stay on, 3 of 7 bears pass.
                // Without them it asked 4 bears of 6 pups in the same level the help switched off (the steepest step of the game).
                new Design(6, tutorial: true)
                    .Cubes(4.4f, 0f, 6).Row(5.2f, C3, 3).Row(7.6f, C2, 3)
                    .Wedge(3.6f, 0f, true).Wedge(5.2f, 0f, false).Wedge(7.6f, C3, false).Wedge(10f, C2, false)
                    .Bear(5.6f, C3 + Cube, mood: Sleep).Bear(6.8f, C3 + Cube).Bear(9f, C2 + Cube)
                    .Bear(2.8f, 0f).Bear(6.4f, 0f).Bear(10.8f, 0f, mood: Eat).EdgePerch(4.2f).Need(3),
                // 4 · shell-helmet bear on a pillar, a bear on cubes and one on the plateau
                new Design(6)
                    .Pillar(3f, 2.6f).Bear(3f, 2.6f, helmet: true)
                    .Rock(8.5f, 0f, 3.6f, 0.8f).Cubes(7.5f, 0.8f, 2).Bear(7.5f, 0.8f + C2).Bear(9.6f, 0.8f, mood: Eat)
                    .Row(8.3f, 0.8f + C2, 3).Wedge(10.7f, 0.8f + C2, false).Bear(9.1f, 0.8f + C3).Need(3),   // a beam roofs the eating bear
                // 5 · ramp to a plateau, a cube column in the middle, a tall perch on the right
                new Design(6)
                    .Ramp(0.5f, 0f, 2.5f, 1.6f, true).Rock(4.2f, 0f, 2.4f, 1.6f).Bear(4.2f, 1.6f, helmet: true)
                    // an ice arch in the middle: a bear on the beam, one sheltered under it
                    .Cubes(7f, 0f, 2).Cubes(9.4f, 0f, 2).Row(7f, C2, 4).Wedge(6.2f, 0f, true).Wedge(10.2f, C2, false)
                    .Bear(8.2f, C2 + Cube).Bear(8.2f, 0f, mood: Eat).Bumper(9.5f, 4.6f)
                    .Rock(11.5f, 0f, 2.2f, 2.6f).Bear(11.5f, 2.6f, mood: Sleep).Need(3).Bungee(),   // first bungee pit
                // 6 · three cube towers on a long ledge
                new Design(7)
                    .Ramp(1f, 0f, 1.8f, 0.8f, true).Rock(8.3f, 0f, 7.4f, 0.8f)
                    .Cubes(6.1f, 0.8f, 2).Bear(6.1f, 0.8f + C2)   // clear of the bigger dangling bear at the edge
                    .Cubes(8f, 0.8f, 3).Bear(8f, 0.8f + C3, helmet: true)
                    .Cubes(10.5f, 0.8f, 2).Bear(10.5f, 0.8f + C2)
                    // a beam from the tall tower shelters a bear between the towers
                    .Row(8.8f, 0.8f + C2, 2).Bear(9.3f, 0.8f, mood: Eat).Wedge(6.9f, 0.8f, false).Wedge(11.3f, 0.8f, false).Need(4)
                    // the first cave: a gap between the ramp and the ledge drops into a chamber with two bears
                    .Cave(0.8f, 7.2f, 2.9f, 4.6f).Bear(1.6f, -Deep, mood: Eat).Bear(6.3f, -Deep)
                    .Bear(4.85f, 0.8f, mood: Dangle),   // legs hanging over the cave opening
                // 7 · the big hill: bears on the crest and on a cube, guards on the floor
                new Design(7)
                    .Ramp(2.5f, 0f, 3f, 2f, true).Rock(7f, 0f, 3f, 2f).Ramp(8.5f, 0f, 3f, 2f, false)
                    .Bear(6.3f, 2f, helmet: true).Cubes(7.7f, 2f, 1).Bear(7.7f, 2f + Cube)
                    // an arch on the right floor roofs the sleeping bear
                    .Cubes(10.7f, 0f, 2).Cubes(12.5f, 0f, 2).Row(10.9f, C2, 3).Wedge(10.1f, C2, true)
                    // the arch's gap (11.1..12.1) clears the sleeper's 0.84 collider; 10.9 left only 0.8 and wedged it
                    .Cubes(1.2f, 0f, 1).Bear(1.2f, Cube).Bear(11.6f, 0f, mood: Sleep).Bear(11.7f, C2 + Cube).Tnt(2.05f, 0f).EdgePerch(4.6f, helmet: true).Need(4),   // TNT on the open floor beside the left cube (at 9.8 it was buried inside the right ramp)
                // 8 · three pillars of different heights, cube columns between
                new Design(7)
                    .Pillar(2.5f, 1.4f).Bear(2.5f, 1.4f)
                    .Pillar(6f, 3f).Bear(6f, 3f, helmet: true)
                    .Pillar(9.5f, 2.2f).Bear(9.5f, 2.2f)
                    .Cubes(4.2f, 0f, 2).Cubes(7.8f, 0f, 3).Bear(11.8f, 0f, mood: Eat)
                    .Wedge(3.4f, 0f, true).Wedge(5f, 0f, false).Wedge(7f, 0f, true).Wedge(8.6f, 0f, false).Need(3).Bungee(),
                // 9 · twin ice towers on plateaus, a guard on the floor by the wall
                new Design(8)
                    .Ramp(0.5f, 0f, 1.1f, 1f, true)
                    .Rock(4f, 0f, 2.4f, 1f).Cubes(4f, 1f, 3).Bear(4f, 1f + C3, helmet: true)
                    .Rock(9f, 0f, 2.4f, 1f).Cubes(9f, 1f, 3).Bear(9f, 1f + C3, helmet: true)
                    .Wedge(3.2f, 1f, true).Wedge(8.2f, 1f, true).Wedge(9.8f, 1f, false)
                    .Bear(11.8f, 0f, mood: Eat).Bear(2.2f, 0f, mood: Sleep).Gate(10.6f, 0f, 3, 1).Need(3)   // sleeper on the floor between ramp and plateau (at 1.1, 1 it hung on the ramp's corner)
                    // the gate button sits in a cave under the gap between the plateaus: drop a pup down onto it
                    .Cave(4f, 9f, 5.6f, 7.4f).Button(6.5f, -Deep, 1).Bear(8.3f, -Deep, mood: Sleep)
                    .Bear(5.0f, 1f, mood: Dangle),
                // 10 · the bear castle: a cube wall on a wide plateau, guards on pillars either side
                new Design(8)
                    .Rock(6.5f, 0f, 6f, 1f)
                    // the castle gate: two 3-cube columns under a 5-cube beam, two guards on the beam, one in the gateway
                    .Cubes(5f, 1f, 3).Cubes(8.2f, 1f, 3).Row(5f, 1f + C3, 5).Wedge(4.2f, 1f + C3, true).Wedge(9f, 1f + C3, false)
                    .Bear(5.8f, 1f + C3 + Cube, helmet: true).Bear(7.4f, 1f + C3 + Cube, helmet: true).Bear(6.6f, 1f)
                    .Pillar(1.5f, 2.6f).Bear(1.5f, 2.6f)
                    .Pillar(12f, 1.8f).Bear(12f, 1.8f, helmet: true)
                    .Cubes(2.6f, 0f, 1, S).Cubes(10.2f, 0f, 1, S).Bear(2.6f, Cube, mood: Eat).Bumper(10.5f, 4.8f).Tnt(9.1f, 1f).Need(4),
                // 11 · bonus: no bears, 24 fish floating in a wave across the arena; every pup that flies through collects
                Bonus(),
                // 12 · boss: the boss bear on a high perch takes two direct hits; a floor button throws it down to the floor; the cage hangs in the middle
                new Design(8).Kind(LevelKind.Boss)
                    .Rock(2.2f, 0f, 2.6f, 1.2f).Pillar(6.5f, 3.2f).Rock(10.8f, 0f, 3.4f, 2.0f)
                    .Button(2.2f, 1.2f, 7).Button(10.8f, 2.0f, 7)
                    .Boss(6.5f, 3.2f, 2, 7).Cage(4.3f, 5.6f)
                    .Bear(11.8f, 2.0f, mood: Eat).Bear(1.3f, 1.2f, mood: Sleep),   // 1.3: on the rock (edge 0.9), clear of the button at 2.2   // clear of the boss life bar at x 12.55
            };
        }

        static Design Bonus()
        {
            var d = new Design(6).Kind(LevelKind.Bonus).Rock(6.5f, 0f, 5f, 0.6f);
            for (int i = 0; i < 24; i++) d.Fish(0.8f + i * 0.52f, 3.2f + Mathf.Sin(i * 0.55f) * 1.8f);
            return d;
        }

        /// <summary>3-star threshold: 60% of everything breakable in the level, rounded to 50.</summary>
        public static int ThreeStarScore(Design d, GameTuning t)
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

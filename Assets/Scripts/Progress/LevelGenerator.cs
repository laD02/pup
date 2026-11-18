using System;
using System.Collections.Generic;
using UnityEngine;

namespace SealCatapult.Progress
{
    /// <summary>
    /// Castles without end (user 16/11: "vô hạn thành và nhiều màn như CPC"): every level after the hand-made ones is
    /// built here from the same pieces the hand-made levels use (towers, pillars, arches, plateaus, caves, gates, TNT,
    /// bumpers). A castle is one CPC-style map of <see cref="PerCastle"/> levels played in order (user 16/11: "thiết kế
    /// cho t nhiêu đó màn trong 1 thành như cpc"): every tenth level is a caged-pup rescue against a boss, two fish bonus
    /// levels, and the castle's big boss last. The seed is the level's place, so a level is the same every time.
    /// Difficulty grows with the castle and with the slot inside it: more bears, more of them required, fewer spare
    /// pups, more helmets and stone, more caves, gates and TNT.
    /// </summary>
    public static class LevelGenerator
    {
        public const int PerCastle = 41;   // user 17/11: "mỗi thành có 41 màn": a cage every 10 levels (10, 20, 30, 40) and the castle boss at 41
        public const int FinalSlot = PerCastle - 1;
        const int CageEvery = 10;

        /// <summary>What kind of level a slot holds: a cage boss every tenth (the last is the castle's own boss) and the
        /// fish bonus at 15 and 35.</summary>
        public static LevelKind KindForSlot(int slot) =>
            slot == FinalSlot || slot % CageEvery == CageEvery - 1 ? LevelKind.Boss
            : slot == 14 || slot == 34 ? LevelKind.Bonus : LevelKind.Normal;
        const float C = LevelBlueprint.Cube;
        const float Deep = Attack.AttackPhase.CaveDepth;
        const int MaxBears = 10;

        /// <summary>The level at <paramref name="slot"/> (0-based) of <paramref name="castle"/> (0-based; castle 1 is
        /// Glacier River, whose first six levels are hand-made).</summary>
        public static LevelBlueprint Build(int castle, int slot)
        {
            var rng = new System.Random(castle * 7919 + slot * 104729 + 17);
            // Grows by 1 every 10 levels (6 per castle). The generated levels start after the 18 hand-made ones (d ≈ 1.8),
            // whose last ask 4–5 bears of 7–8 pups.
            float d = castle * 4f + slot / 10f;
            int zone = (castle * 3 + slot / 14) % 3;   // the palette changes island by island along the map
            switch (KindForSlot(slot))
            {
                case LevelKind.Bonus: return Bonus(rng, zone);
                case LevelKind.Boss: return Boss(rng, zone, d, slot == FinalSlot);
                default: return Normal(rng, zone, d, slot);
            }
        }

        static LevelBlueprint Normal(System.Random rng, int zone, float d, int slot)
        {
            int target = Mathf.Clamp(4 + (int)(d * 0.7f) + rng.Next(0, 2), 4, MaxBears);
            float helmet = Mathf.Min(0.6f, 0.1f + 0.06f * d);
            float stone = Mathf.Min(0.4f, 0.05f * d);
            var b = new LevelBlueprint(0).Zone(zone);
            int bears = 0;

            // Three lots across the arena (the pups glide in from the left, the wall is at 13). A cave or a gate may
            // take a lot over; the rest get a structure each.
            var lots = new List<(float a, float c)> { (1.2f, 4.2f), (4.6f, 8.4f), (8.8f, 12.4f) };
            bool cave = rng.NextDouble() < Mathf.Min(0.35f, 0.1f + 0.04f * d);
            bool gate = !cave && d >= 1.5f && rng.NextDouble() < 0.25f;
            if (cave)
            {
                // A chamber under the first lot, opened by a gap in its middle; its bears count like any other.
                b.Cave(0.8f, 4.6f, 1.7f, 3.7f);   // a 2.0 gap, wider than the hand-made 1.8: a diving pup fits through
                b.Bear(1.2f, -Deep, mood: BearMood.Eat);
                bears++;
                if (rng.NextDouble() < 0.6f) { b.Bear(4.2f, -Deep, mood: BearMood.Sleep); bears++; }
                lots.RemoveAt(0);
            }
            else if (gate)
            {
                // A button on a plateau in the first lot opens an ice gate in the last; the bears wait behind it.
                b.Rock(2.7f, 0f, 2.6f, 1.2f).Button(2.7f, 1.2f, 1);
                b.Gate(9.2f, 0f, 3, 1).Bear(10.1f, 0f, helmet: Roll(rng, helmet)).Bear(12.0f, 0f, mood: BearMood.Eat);   // clear of the TNT at 11.05
                bears += 2;
                if (rng.NextDouble() < 0.5f) { b.Tnt(11.05f, 0f); }
                lots.RemoveAt(2);
                lots.RemoveAt(0);
            }

            foreach (var (a, e) in lots)
                bears += Structure(b, rng, a, e, helmet, stone, target - bears);

            // Short of the target: the edge perch, then a guard on the floor by the wall.
            if (bears < target) { b.EdgePerch(3.4f + (float)rng.NextDouble() * 1.2f, Roll(rng, helmet)); bears++; }
            if (bears < target && !gate) { b.Bear(12.7f, 0f, mood: BearMood.Sleep); bears++; }

            if (!gate && rng.NextDouble() < Mathf.Min(0.4f, 0.15f + 0.04f * d))
            {
                // Above whatever stands under it (a tall tower or a pillar lookout reaches 5.8), or not at all.
                float x = 5f + (float)rng.NextDouble() * 4f;
                float y = Mathf.Max(4.6f, TopNear(b, x) + BumperClear);
                if (y <= 5.6f) b.Bumper(x, y);
            }
            // The three launch styles take turns along the map (user 16/11): mostly the swing, a bungee and a drop now
            // and then.
            if (slot % 5 == 2) b.Bungee();
            else if (slot % 5 == 4) b.Drop();

            int spareBears = d < 3f ? 2 : d < 6f ? 1 : 0;
            int required = Mathf.Clamp(bears - spareBears, Mathf.Min(3, bears), bears);
            int sparePups = Mathf.Max(1, 3 - (int)(d / 2f));
            b.puffers = Mathf.Clamp(required + sparePups, 5, 10);
            return b.Need(required);
        }

        /// <summary>One structure in the lot from <paramref name="a"/> to <paramref name="e"/>; returns the bears it holds.</summary>
        static int Structure(LevelBlueprint b, System.Random rng, float a, float e, float helmet, float stone, int wanted)
        {
            float w = e - a, c = (a + e) * 0.5f;
            int pick = rng.Next(0, 6);
            if (wanted <= 1 && pick == 2) pick = 1;   // an arch holds two
            if (pick == 4 && w < 3.4f) pick = 1;      // a hill needs room for a crest a bear can stand on
            switch (pick)
            {
                case 0:   // a rock pillar with a lookout on top
                {
                    float h = 1.6f + (float)rng.NextDouble() * 1.8f;
                    b.Pillar(c, h).Bear(c, h, helmet: Roll(rng, helmet));
                    if (wanted > 1 && w >= 3f) { b.Bear(c + 1.2f, 0f, mood: BearMood.Eat); return 2; }
                    return 1;
                }
                case 1:   // an ice tower, maybe on a plateau, wedges at its foot
                {
                    float p = rng.Next(0, 3) * 0.6f;
                    int n = rng.Next(2, 5);
                    if (p > 0f) b.Rock(c, 0f, 2.4f, p);
                    for (int i = 0; i < n; i++) b.Box(c, p + i * C, C, C, Roll(rng, stone) ? BlockMaterial.Stone : BlockMaterial.Ice);
                    b.Wedge(c - C, p, true).Wedge(c + C, p, false);
                    b.Bear(c, p + n * C, helmet: Roll(rng, helmet));
                    return 1;
                }
                case 2:   // an arch: two columns under a beam, a bear on top and one sheltered under it
                {
                    float x0 = c - 1.2f;
                    int n = rng.Next(2, 4);
                    b.Cubes(x0, 0f, n).Cubes(x0 + 2.4f, 0f, n).Row(x0, n * C, 4);
                    b.Bear(x0 + 1.2f, (n + 1) * C, helmet: Roll(rng, helmet));
                    b.Bear(x0 + 1.2f, 0f, mood: rng.NextDouble() < 0.5f ? BearMood.Sleep : BearMood.Eat);
                    return 2;
                }
                case 3:   // a plateau with a bear and a short tower
                {
                    float h = 0.8f + rng.Next(0, 2) * 0.4f;
                    int n = rng.Next(1, 3);
                    b.Rock(c, 0f, Mathf.Min(w - 0.4f, 3.2f), h);
                    b.Bear(c - 0.8f, h, mood: BearMood.Eat).Cubes(c + 0.8f, h, n).Bear(c + 0.8f, h + n * C, helmet: Roll(rng, helmet));
                    return 2;
                }
                case 4:   // a hill: ramp up, crest, ramp down, a bear on the crest
                {
                    float h = 1.2f + (float)rng.NextDouble() * 0.8f;
                    b.Ramp(a, 0f, 1.1f, h, true).Rock(c, 0f, w - 2.2f, h).Ramp(e - 1.1f, 0f, 1.1f, h, false);
                    b.Bear(c, h, helmet: Roll(rng, helmet));
                    return 1;
                }
                default:  // guards on the floor, one behind a snow cube
                {
                    b.Cubes(c - 0.9f, 0f, 1, BlockMaterial.Snow).Bear(c - 0.9f, C, mood: BearMood.Eat);
                    if (wanted > 1) { b.Bear(c + 0.7f, 0f, helmet: Roll(rng, helmet)); return 2; }
                    return 1;
                }
            }
        }

        static LevelBlueprint Bonus(System.Random rng, int zone)
        {
            var b = new LevelBlueprint(6).Zone(zone).Kind(LevelKind.Bonus).Rock(6.5f, 0f, 5f, 0.6f);
            int fish = 24 + rng.Next(0, 7);
            float amp = 1.2f + (float)rng.NextDouble() * 1f, phase = (float)rng.NextDouble() * 6.28f, freq = 0.4f + (float)rng.NextDouble() * 0.3f;
            float step = 12.4f / (fish - 1);
            for (int i = 0; i < fish; i++) b.Fish(0.8f + i * step, 3.3f + Mathf.Sin(i * freq + phase) * amp);
            return b;
        }

        static LevelBlueprint Boss(System.Random rng, int zone, float d, bool final)
        {
            int castle = (int)(d / 3f);   // guard count grows like before (one step per half castle)
            int lives = Mathf.Clamp(2 + (int)(d / 4f) + (final ? 1 : 0), 2, 5);
            float jitter = (float)rng.NextDouble() * 0.6f - 0.3f;
            var b = new LevelBlueprint(Mathf.Min(8 + lives - 2, 10)).Zone(zone).Kind(LevelKind.Boss)
                .Rock(2.2f, 0f, 2.6f, 1.2f).Pillar(6.5f + jitter, 3.2f).Rock(10.8f, 0f, 3.4f, 2.0f)
                .Button(2.2f, 1.2f, 7).Button(10.8f, 2.0f, 7)
                .Boss(6.5f + jitter, 3.2f, lives, 7).Cage(4.3f, 5.6f)
                .Bear(11.8f, 2.0f, mood: BearMood.Eat).Bear(1.3f, 1.2f, mood: BearMood.Sleep);
            if (castle >= 3) b.Bear(9.7f, 2.0f, helmet: true);
            if (castle >= 5) b.EdgePerch(4.4f, helmet: true);
            return b;
        }

        static bool Roll(System.Random rng, float chance) => rng.NextDouble() < chance;

        const float BearHeight = 1.4f, BumperClear = 1.0f, NearX = 1.0f;

        /// <summary>The highest top (block, rock or bear's head) within <see cref="NearX"/> of x.</summary>
        static float TopNear(LevelBlueprint b, float x)
        {
            float top = 0f;
            foreach (var k in b.blocks) if (Mathf.Abs(k.position.x - x) < k.size.x * 0.5f + NearX) top = Mathf.Max(top, k.position.y + k.size.y * 0.5f);
            foreach (var t in b.terrain) if (Mathf.Abs(t.position.x - x) < t.size.x * 0.5f + NearX) top = Mathf.Max(top, t.position.y + t.size.y * 0.5f);
            foreach (var r in b.bears) if (Mathf.Abs(r.position.x - x) < NearX) top = Mathf.Max(top, r.position.y + BearHeight);
            return top;
        }
    }
}

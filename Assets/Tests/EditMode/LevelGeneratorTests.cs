using System.Linq;
using NUnit.Framework;
using SealCatapult.Progress;
using UnityEngine;

namespace SealCatapult.Tests
{
    /// <summary>The generated castles (LevelGenerator): stable, playable on paper, and harder castle after castle.</summary>
    public class LevelGeneratorTests
    {
        const int Castles = LevelCatalog.Castles;   // every castle the game has
        const float Eps = 0.02f;

        [Test]
        public void SameLevelEveryTime()
        {
            var a = LevelGenerator.Build(7, 4);
            var b = LevelGenerator.Build(7, 4);
            Assert.AreEqual(a.bears.Count, b.bears.Count);
            Assert.AreEqual(a.puffers, b.puffers);
            for (int i = 0; i < a.bears.Count; i++) Assert.AreEqual(a.bears[i].position, b.bears[i].position);
        }

        [Test]
        public void EveryCastleHasItsCagesBonusesAndFinalBoss()
        {
            for (int c = 0; c < Castles; c++)
            {
                for (int s = c == 0 ? 18 : 0; s < LevelGenerator.PerCastle; s++)   // castle 1's first 18 are hand-made
                {
                    var d = LevelGenerator.Build(c, s);
                    Assert.AreEqual(LevelGenerator.KindForSlot(s), d.kind, $"castle {c + 1} level {s + 1}");
                    if (d.kind == LevelKind.Boss)
                        Assert.IsTrue(d.objects.Any(o => o.kind == ObjectKind.Boss), $"castle {c + 1} level {s + 1}: boss level without a boss");
                }
                Assert.AreEqual((LevelGenerator.PerCastle - 1) / 10 + 1, Enumerable.Range(0, LevelGenerator.PerCastle).Count(s => LevelGenerator.KindForSlot(s) == LevelKind.Boss), "a boss every tenth level, the last one the castle's");
                Assert.AreEqual(LevelKind.Boss, LevelGenerator.KindForSlot(LevelGenerator.FinalSlot));
            }
        }

        [Test]
        public void EveryNormalLevelCanBePassed()
        {
            for (int c = 0; c < Castles; c++)
                for (int s = c == 0 ? 18 : 0; s < LevelGenerator.PerCastle; s++)
                {
                    if (LevelGenerator.KindForSlot(s) != LevelKind.Normal) continue;
                    var d = LevelGenerator.Build(c, s);
                    string where = $"castle {c + 1} level {s + 1}";
                    Assert.Greater(d.bears.Count, 0, where);
                    Assert.That(d.requiredBears, Is.InRange(1, d.bears.Count), where);
                    Assert.GreaterOrEqual(d.puffers, d.requiredBears, $"{where}: fewer pups than bears to knock out");
                    foreach (var bear in d.bears)
                    {
                        var p = bear.position;
                        Assert.That(p.x, Is.InRange(0.6f, 13.8f), $"{where}: bear outside the arena at {p}");
                        Assert.IsFalse(Inside(d, p), $"{where}: bear buried in a block or rock at {p}");
                        Assert.IsTrue(Supported(d, p), $"{where}: bear floating at {p}");
                    }
                    foreach (var o in d.objects.Where(o => o.kind == ObjectKind.Bumper || o.kind == ObjectKind.Tnt))
                    {
                        Assert.IsFalse(Inside(d, o.position), $"{where}: {o.kind} buried at {o.position}");
                        foreach (var bear in d.bears)
                            Assert.IsFalse(Mathf.Abs(o.position.x - bear.position.x) < 0.82f && o.position.y > bear.position.y - 0.5f && o.position.y < bear.position.y + 1.4f,
                                $"{where}: {o.kind} at {o.position} touches the bear at {bear.position}");
                    }
                    for (int i = 0; i < d.bears.Count; i++)
                        for (int j = i + 1; j < d.bears.Count; j++)
                        {
                            var a = d.bears[i].position; var b = d.bears[j].position;
                            Assert.IsFalse(Mathf.Abs(a.y - b.y) < 0.5f && Mathf.Abs(a.x - b.x) < 0.85f, $"{where}: bears overlap at {a} and {b}");
                        }
                }
        }

        [Test]
        public void LaterCastlesAreHarder()
        {
            Assert.Greater(Average(6, d => d.bears.Count), Average(1, d => d.bears.Count), "more bears later");
            Assert.Less(Average(6, d => d.puffers - d.requiredBears), Average(1, d => d.puffers - d.requiredBears), "fewer spare pups later");
        }

        static float Average(int castle, System.Func<LevelBlueprint, float> f) =>
            Enumerable.Range(0, LevelGenerator.PerCastle).Where(s => LevelGenerator.KindForSlot(s) == LevelKind.Normal).Average(s => f(LevelGenerator.Build(castle, s)));

        static bool Inside(LevelBlueprint d, Vector2 p)
        {
            foreach (var b in d.blocks)
                if (Mathf.Abs(p.x - b.position.x) < b.size.x * 0.5f - Eps && p.y > b.position.y - b.size.y * 0.5f + Eps && p.y < b.position.y + b.size.y * 0.5f - Eps) return true;
            foreach (var t in d.terrain)
                if (Mathf.Abs(p.x - t.position.x) < t.size.x * 0.5f - Eps && p.y > t.position.y - t.size.y * 0.5f + Eps && p.y < t.position.y + t.size.y * 0.5f - Eps) return true;
            return false;
        }

        /// <summary>Feet on the floor, in the cave, or on top of a block or rock under them.</summary>
        static bool Supported(LevelBlueprint d, Vector2 p)
        {
            if (Mathf.Abs(p.y) < Eps) return true;
            if (p.y < 0f) return d.caveTo > d.caveFrom && p.x > d.caveFrom && p.x < d.caveTo;
            foreach (var b in d.blocks)
                if (Mathf.Abs(p.x - b.position.x) < b.size.x * 0.5f + Eps && Mathf.Abs(p.y - (b.position.y + b.size.y * 0.5f)) < Eps) return true;
            foreach (var t in d.terrain)
                if (Mathf.Abs(p.x - t.position.x) < t.size.x * 0.5f + Eps && Mathf.Abs(p.y - (t.position.y + t.size.y * 0.5f)) < Eps) return true;
            return false;
        }
    }
}

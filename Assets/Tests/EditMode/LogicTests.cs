using System.IO;
using NUnit.Framework;
using SealCatapult.Core;
using SealCatapult.Progress;
using UnityEngine;

namespace SealCatapult.Tests
{
    public class StarRatingTests
    {
        [Test] public void LossGivesZeroStars() => Assert.AreEqual(0, StarRating.Compute(false, 3, 9999, 100));
        [Test] public void WinOnlyGivesOneStar() => Assert.AreEqual(1, StarRating.Compute(true, 0, 50, 100));
        [Test] public void LeftoverPufferAddsStar() => Assert.AreEqual(2, StarRating.Compute(true, 1, 50, 100));
        [Test] public void ScoreThresholdAddsStar() => Assert.AreEqual(2, StarRating.Compute(true, 0, 100, 100));
        [Test] public void BothBonusesGiveThreeStars() => Assert.AreEqual(3, StarRating.Compute(true, 2, 150, 100));

        [Test]
        public void EachStarBelongsToItsOwnRule()
        {
            // Score reached without a pup to spare: the third rule is met, the second is not (the result card shows this).
            var rules = StarRating.Rules(true, 0, 100, 100);
            Assert.IsTrue(rules.passed);
            Assert.IsFalse(rules.second);
            Assert.IsTrue(rules.third);
            Assert.AreEqual((false, false, false), StarRating.Rules(false, 3, 9999, 100), "a loss meets no rule");
        }

        // The bonus fish run always spends every pup, so it is rated on fish: it must be able to reach 3 stars.
        [Test] public void BonusRunAlwaysGivesOneStar() => Assert.AreEqual(1, StarRating.ComputeBonus(true, 0, 24));
        [Test] public void HalfTheFishGiveTwoStars() => Assert.AreEqual(2, StarRating.ComputeBonus(true, 12, 24));
        [Test] public void JustUnderHalfTheFishStayAtOneStar() => Assert.AreEqual(1, StarRating.ComputeBonus(true, 11, 24));
        [Test] public void EightyPercentOfTheFishGiveThreeStars() => Assert.AreEqual(3, StarRating.ComputeBonus(true, 20, 24));
        [Test] public void JustUnderEightyPercentStayAtTwoStars() => Assert.AreEqual(2, StarRating.ComputeBonus(true, 19, 24));
        // No pup left the pit: the run was never played (it used to be saved as cleared with one star).
        [Test] public void BonusRunWithNoPupOutEarnsNothing() => Assert.AreEqual(0, StarRating.ComputeBonus(false, 24, 24));
        [Test] public void BonusRunWithoutFishDoesNotDivideByZero() => Assert.AreEqual(1, StarRating.ComputeBonus(true, 0, 0));
    }

    public class EconomyTests
    {
        [Test]
        public void EveryUpgradeCanBeBoughtWithTheStarsTheLevelsGive()
        {
            var db = GameDatabase.Instance;
            int price = 0;
            foreach (var def in db.upgrades.upgrades)
            {
                Assert.AreEqual(UpgradeData.MaxLevel, def.costs.Length, $"{def.id}: one price per level");
                foreach (int cost in def.costs) price += cost;
            }
            Assert.LessOrEqual(price, db.levels.Length * 3, "the whole shop must be within reach of the stars in the game");
        }

        [Test]
        public void LevelCountMatchesTheSaveFormat() =>
            Assert.AreEqual(SaveData.LevelCount, GameDatabase.Instance.levels.Length);
    }

    public class ShopTests
    {
        static UpgradeDef Spiky => new UpgradeDef { id = UpgradeId.Spiky, costs = new[] { 3, 6, 10 } };

        static SaveData WithStars(params int[] stars)
        {
            var d = new SaveData();
            for (int i = 0; i < stars.Length; i++) d.stars[i] = stars[i];
            return d;
        }

        [Test]
        public void CannotBuyWithoutEnoughStars()
        {
            var d = WithStars(2);
            Assert.IsFalse(Shop.TryBuy(d, Spiky));
            Assert.AreEqual(0, d.upgrades[0]);
            Assert.AreEqual(0, d.spentStars);
        }

        [Test]
        public void BuyingSpendsStarsAndRaisesLevel()
        {
            var d = WithStars(3, 3, 3);
            Assert.IsTrue(Shop.TryBuy(d, Spiky));
            Assert.AreEqual(1, d.upgrades[0]);
            Assert.AreEqual(3, d.spentStars);
            Assert.AreEqual(6, Shop.AvailableStars(d));
            Assert.AreEqual(6, Shop.NextCost(d, Spiky));
        }

        [Test]
        public void CannotBuyPastMaxLevel()
        {
            var d = WithStars(3, 3, 3, 3, 3, 3, 3, 3, 3, 3);
            Assert.IsTrue(Shop.TryBuy(d, Spiky));
            Assert.IsTrue(Shop.TryBuy(d, Spiky));
            Assert.IsTrue(Shop.TryBuy(d, Spiky));
            Assert.AreEqual(-1, Shop.NextCost(d, Spiky));
            Assert.IsFalse(Shop.TryBuy(d, Spiky));
            Assert.AreEqual(19, d.spentStars);
        }
    }

    public class SaveSystemTests
    {
        string path;

        [SetUp] public void SetUp() => path = Path.Combine(Path.GetTempPath(), $"puffer_save_test_{System.Guid.NewGuid()}.json");

        [TearDown]
        public void TearDown()
        {
            foreach (var p in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + "*"))
                File.Delete(p);
        }

        [Test]
        public void MissingFileGivesFreshSave()
        {
            var d = SaveSystem.LoadFrom(path);
            Assert.AreEqual(SaveData.LevelCount, d.stars.Length);
            Assert.AreEqual(0, d.spentStars);
        }

        [Test]
        public void RoundTripKeepsProgress()
        {
            var d = new SaveData { spentStars = 4, language = "pl", reviewAsked = true };
            d.stars[2] = 3;
            d.stars[3] = 2;
            d.upgrades[(int)UpgradeId.Ninja] = 2;
            SaveSystem.SaveTo(path, d);
            var loaded = SaveSystem.LoadFrom(path);
            Assert.AreEqual(3, loaded.stars[2]);
            Assert.AreEqual(2, loaded.upgrades[(int)UpgradeId.Ninja]);
            Assert.AreEqual(4, loaded.spentStars);
            Assert.AreEqual("pl", loaded.language);
            Assert.IsTrue(loaded.reviewAsked);
        }

        [Test]
        public void CorruptFileIsBackedUpAndReplacedWithFreshSave()
        {
            File.WriteAllText(path, "{ not json");
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Save file unreadable"));
            var d = SaveSystem.LoadFrom(path);
            Assert.AreEqual(0, d.stars[0]);
            Assert.AreEqual(1, Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + ".*.corrupt").Length);
        }

        [Test]
        public void InterruptedSaveIsRecoveredFromTempFile()
        {
            // A kill between writing save.json.tmp and swapping it in leaves only the .tmp.
            var d = new SaveData();
            d.stars[0] = 3;
            SaveSystem.SaveTo(path, d);
            File.Move(path, path + ".tmp");
            Assert.AreEqual(3, SaveSystem.LoadFrom(path).stars[0]);
        }

        [Test]
        public void SaveOverExistingFileKeepsNewData()
        {
            var d = new SaveData();
            SaveSystem.SaveTo(path, d);
            d.stars[1] = 2;
            Assert.IsTrue(SaveSystem.SaveTo(path, d));
            Assert.AreEqual(2, SaveSystem.LoadFrom(path).stars[1]);
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test]
        public void NaNVolumesFallBackToDefaults()
        {
            var d = new SaveData { musicVolume = float.NaN, sfxVolume = float.NaN };
            d.Normalize();
            Assert.AreEqual(SaveData.DefaultMusic, d.musicVolume);
            Assert.AreEqual(SaveData.DefaultSfx, d.sfxVolume);
        }

        [Test]
        public void SpentStarsNeverExceedEarnedStars()
        {
            var d = new SaveData { spentStars = 50 };
            d.stars[0] = 3;
            d.stars[1] = 2;
            d.Normalize();
            Assert.AreEqual(5, d.spentStars);
        }

        [Test]
        public void NormalizeClampsHandEditedValues()
        {
            File.WriteAllText(path, "{\"stars\":[9,-1],\"upgrades\":[7],\"musicVolume\":3,\"language\":\"xx\"}");
            var d = SaveSystem.LoadFrom(path);
            Assert.AreEqual(SaveData.LevelCount, d.stars.Length);
            Assert.AreEqual(3, d.stars[0]);
            Assert.AreEqual(0, d.stars[1]);
            Assert.AreEqual(3, d.upgrades[0]);
            Assert.AreEqual(1f, d.musicVolume);
            Assert.AreEqual("", d.language);
        }
    }

    public class StatsTests
    {
        [Test]
        public void DivesAreCountedByBearsKnockedOut()
        {
            var d = new SaveData();
            foreach (int bears in new[] { 0, 1, 2, 3, 4, 6, 2 }) d.AddDive(bears);
            Assert.AreEqual(2, d.combo2);
            Assert.AreEqual(1, d.combo3);
            Assert.AreEqual(2, d.combo4);
        }

        [Test]
        public void NegativeStatsFromABadSaveAreReset()
        {
            var d = new SaveData { totalBears = -5, combo2 = -1, combo3 = -2, combo4 = -3 };
            d.Normalize();
            Assert.AreEqual(0, d.totalBears + d.combo2 + d.combo3 + d.combo4);
        }

        [Test]
        public void PerfectCountAddsPerfectLevels()
        {
            var d = new SaveData();
            d.perfect[0] = 1;
            d.perfect[5] = 1;
            Assert.AreEqual(2, d.PerfectCount());
        }
    }

    public class InAppReviewTests
    {
        [Test]
        public void AsksOnlyOnAWinFromLevelThreeUntilPlayAccepted()
        {
            var d = new SaveData();
            Assert.IsFalse(SealCatapult.Review.InAppReview.ShouldAsk(d, 1, true), "level 2 is too early");
            Assert.IsFalse(SealCatapult.Review.InAppReview.ShouldAsk(d, 2, false), "never on a loss");
            Assert.IsTrue(SealCatapult.Review.InAppReview.ShouldAsk(d, 2, true));
            Assert.IsTrue(SealCatapult.Review.InAppReview.ShouldAsk(d, 9, true), "a failed ask retries on a later win");
            d.reviewAsked = true;
            Assert.IsFalse(SealCatapult.Review.InAppReview.ShouldAsk(d, 9, true), "asked once");
        }
    }

    public class LocTests
    {
        /// <summary>The table uses index initializers, so a repeated key silently replaces the earlier text (it once
        /// turned the mission card's "Bears on this level" into "Total bears").</summary>
        [Test]
        public void NoKeyIsDefinedTwice()
        {
            var source = File.ReadAllText("Assets/Scripts/Core/Loc.cs");
            var keys = System.Text.RegularExpressions.Regex.Matches(source, @"\[""([a-z0-9_]+)""\]\s*=");
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (System.Text.RegularExpressions.Match m in keys)
                Assert.IsTrue(seen.Add(m.Groups[1].Value), $"key {m.Groups[1].Value} is defined twice");
            Assert.Greater(seen.Count, 50);
        }

        [Test]
        public void EveryKeyHasTextInEveryLanguage()
        {
            Assert.IsTrue(Loc.IsComplete(out string problem), problem);
        }

        [Test]
        public void EveryLanguageCodeIsAcceptedBySaves()
        {
            foreach (string code in Loc.Codes)
            {
                var d = new SaveData { language = code };
                d.Normalize();
                Assert.AreEqual(code, d.language);
            }
        }
    }

    public class AttackEntryTests
    {
        [Test]
        public void SteepExitEntersHighAndSlowFlatExitLowAndFast()
        {
            var (steepAt, steepV) = SealCatapult.Attack.AttackPhase.Entry(new Vector2(3f, 9f));
            var (flatAt, flatV) = SealCatapult.Attack.AttackPhase.Entry(new Vector2(10f, 3f));
            Assert.Greater(steepAt.y, flatAt.y);
            Assert.Greater(flatV.x, steepV.x);
            Assert.AreEqual(0f, steepV.y);
        }

        [Test]
        public void BackwardExitUsesTheDefaultEntryAndSpeedsAreClamped()
        {
            var (at, v) = SealCatapult.Attack.AttackPhase.Entry(new Vector2(-1f, 5f));
            Assert.AreEqual(SealCatapult.Attack.AttackPhase.SpawnPoint, at);
            Assert.AreEqual(SealCatapult.Attack.AttackPhase.SpawnVelocity, v);
            Assert.AreEqual(6.2f, SealCatapult.Attack.AttackPhase.Entry(new Vector2(40f, 1f)).velocity.x, 1e-4f);
            Assert.AreEqual(3.2f, SealCatapult.Attack.AttackPhase.Entry(new Vector2(0.5f, 9f)).velocity.x, 1e-4f);
        }

        [Test]
        public void NoExitUsesTheDefaultEntry()
        {
            var (at, v) = SealCatapult.Attack.AttackPhase.Entry(null);
            Assert.AreEqual(SealCatapult.Attack.AttackPhase.SpawnPoint, at);
            Assert.AreEqual(SealCatapult.Attack.AttackPhase.SpawnVelocity, v);
        }
    }

    public class TapTargetTests
    {
        [Test]
        public void SmallControlGetsAnInvisibleTouchAreaOfAtLeastMinHit()
        {
            var control = new GameObject("Button", typeof(RectTransform)).GetComponent<RectTransform>();
            try
            {
                control.sizeDelta = new Vector2(96, 60);
                SealCatapult.UI.UiKit.PadHitArea(control);
                var hit = (RectTransform)control.Find("Hit");
                Assert.IsNotNull(hit);
                Assert.GreaterOrEqual(hit.sizeDelta.x, SealCatapult.UI.UiKit.MinHit);
                Assert.GreaterOrEqual(hit.sizeDelta.y, SealCatapult.UI.UiKit.MinHit);
                Assert.AreEqual(new Vector2(96, 60), control.sizeDelta, "the visible control keeps its size");
            }
            finally
            {
                Object.DestroyImmediate(control.gameObject);
            }
        }
    }
}

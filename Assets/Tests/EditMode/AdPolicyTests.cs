using NUnit.Framework;
using PufferLaunch.Ads;

namespace PufferLaunch.Tests
{
    public class AdPolicyTests
    {
        const int Veteran = AdPolicy.NewPlayerLevels;

        static AdPolicy WithWins(int wins)
        {
            var p = new AdPolicy();
            for (int i = 0; i < wins; i++) p.RecordWin();
            return p;
        }

        [Test] public void NoInterstitialBeforeFourWins() => Assert.IsFalse(WithWins(3).ShouldShowInterstitial(1000f, Veteran, false));

        [Test] public void InterstitialOnTheFourthWin() => Assert.IsTrue(WithWins(4).ShouldShowInterstitial(1000f, Veteran, false));

        [Test] public void NewPlayerSeesNoInterstitial() => Assert.IsFalse(WithWins(4).ShouldShowInterstitial(1000f, Veteran - 1, false));

        [Test] public void NoInterstitialWhenTheChapterEnds() => Assert.IsFalse(WithWins(4).ShouldShowInterstitial(1000f, Veteran, true));

        [Test]
        public void AnAdRestartsTheWinCountAndTheCooldown()
        {
            var p = WithWins(4);
            p.RecordFullscreenAd(1000f);
            for (int i = 0; i < 4; i++) p.RecordWin();
            Assert.IsFalse(p.ShouldShowInterstitial(1000f + AdPolicy.FullscreenCooldown - 1f, Veteran, false), "inside 180 s");
            Assert.IsTrue(p.ShouldShowInterstitial(1000f + AdPolicy.FullscreenCooldown, Veteran, false), "after 180 s");
        }

        [Test]
        public void ARewardedAdAlsoDelaysTheNextInterstitial()
        {
            var p = WithWins(4);
            p.RecordFullscreenAd(500f);
            Assert.IsFalse(p.ShouldShowInterstitial(600f, Veteran, false));
        }

        [Test]
        public void WinsKeepCountingWhenNoAdWasReady()
        {
            var p = WithWins(4);   // 4th win: allowed, but no ad was loaded, so nothing recorded
            p.RecordWin();
            Assert.IsTrue(p.ShouldShowInterstitial(1000f, Veteran, false), "the 5th win still qualifies");
        }

        [Test]
        public void ResetStartsAFreshSession()
        {
            var p = WithWins(4);
            p.GrantBonusPup(2);
            p.RecordFullscreenAd(1000f);
            p.Reset();
            Assert.IsFalse(p.ShouldShowInterstitial(1000f, Veteran, false), "win count cleared");
            for (int i = 0; i < 4; i++) p.RecordWin();
            Assert.IsTrue(p.ShouldShowInterstitial(1000f, Veteran, false), "cooldown cleared");
            Assert.AreEqual(0, p.TakeBonusPups(2), "bonus cleared");
        }

        [Test]
        public void BonusPupIsSpentOnceOnItsOwnLevel()
        {
            var p = new AdPolicy();
            p.GrantBonusPup(4);
            Assert.AreEqual(1, p.TakeBonusPups(4));
            Assert.AreEqual(0, p.TakeBonusPups(4), "a second retry is back to normal");
        }

        [Test]
        public void BonusPupIsLostWhenAnotherLevelStarts()
        {
            var p = new AdPolicy();
            p.GrantBonusPup(4);
            Assert.AreEqual(0, p.TakeBonusPups(7));
            Assert.AreEqual(0, p.TakeBonusPups(4));
        }
    }
}

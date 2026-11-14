namespace PufferLaunch.Ads
{
    /// <summary>
    /// When an ad may show (GAME_DESIGN.md §13b). Pure C#, no Unity calls, so EditMode tests cover every rule.
    /// Counts are per session: reopening the game starts them again.
    /// </summary>
    public sealed class AdPolicy
    {
        /// <summary>One interstitial per this many level wins.</summary>
        public const int InterstitialEveryWins = 4;
        /// <summary>Minimum time between two full-screen ads of any kind, in seconds.</summary>
        public const float FullscreenCooldown = 180f;
        /// <summary>No interstitial until the player has cleared this many levels (counted from the save).</summary>
        public const int NewPlayerLevels = 3;

        int winsSinceAd;
        float lastAdTime = float.NegativeInfinity;
        int bonusLevel = -1;   // level the rewarded "+1 pup" was earned for; -1 = none

        public void RecordWin() => winsSinceAd++;

        /// <summary>Back to a fresh session (tests share the static policy across scenes).</summary>
        public void Reset()
        {
            winsSinceAd = 0;
            lastAdTime = float.NegativeInfinity;
            bonusLevel = -1;
        }

        /// <summary>chapterEnd: leaving the bonus or boss level, which closes the chapter (no ad there).</summary>
        public bool ShouldShowInterstitial(float now, int levelsCleared, bool chapterEnd) =>
            !chapterEnd && levelsCleared >= NewPlayerLevels && winsSinceAd >= InterstitialEveryWins &&
            now - lastAdTime >= FullscreenCooldown;

        /// <summary>Any full-screen ad that actually showed (interstitial or rewarded) restarts both counters.</summary>
        public void RecordFullscreenAd(float now)
        {
            lastAdTime = now;
            winsSinceAd = 0;
        }

        public void GrantBonusPup(int levelIndex) => bonusLevel = levelIndex;

        /// <summary>Extra pups for this level start; the reward is spent by the next level start, whichever it is.</summary>
        public int TakeBonusPups(int levelIndex)
        {
            int pups = bonusLevel == levelIndex ? 1 : 0;
            bonusLevel = -1;
            return pups;
        }
    }
}

namespace SealCatapult.Ads
{
    /// <summary>AdMob ids (GAME_DESIGN.md §13b). The app id lives in Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings
    /// (set by ProjectBuilder from <see cref="AppId"/>). Ids are not secrets: they ship inside the APK.</summary>
    public static class AdsConfig
    {
        // Real units, registered 2026-11-13 (same AdMob publisher as one-fill-line). Used only by release builds on a real device.
        public const string AppId = "ca-app-pub-5420799226073285~5657352369";
        public const string Interstitial = "ca-app-pub-5420799226073285/8091944013";
        public const string Rewarded = "ca-app-pub-5420799226073285/6778862341";

        // Google's public Android test units: debug/QA builds and emulators, so test clicks never hit the real account.
        public const string TestInterstitial = "ca-app-pub-3940256099942544/1033173712";
        public const string TestRewarded = "ca-app-pub-3940256099942544/5224354917";
    }
}

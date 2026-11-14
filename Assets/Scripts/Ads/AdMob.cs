using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using PufferLaunch.Core;
using UnityEngine;

namespace PufferLaunch.Ads
{
    /// <summary>
    /// AdMob (GAME_DESIGN.md §13b): UMP consent first, then a preloaded interstitial and rewarded ad. Without consent,
    /// in the Editor or when anything fails, every call here falls through and the game plays on without ads.
    /// SDK callbacks may arrive on a Java thread: each one is posted to <see cref="Post"/> and runs in Update.
    /// </summary>
    public class AdMob : MonoBehaviour
    {
        /// <summary>The rules; static so a Game scene started directly (tests, Editor) still counts wins.</summary>
        public static readonly AdPolicy Policy = new AdPolicy();

        static AdMob instance;

        const float RetryFirst = 30f, RetryMax = 300f;     // a failed load retries after 30 s, doubling up to 5 min
        const float MaxAge = 55f * 60f;                    // AdMob ads expire after about an hour
        const float RewardGrace = 0.3f;                    // the reward callback may land just after the close callback
        const float OpenTimeout = 5f;                      // an ad that never opens must not hold the game
        const float ConsentRetryGap = 60f;                 // offline at launch: ask UMP again, at most once a minute

        static readonly object gate = new object();
        static readonly Queue<Action> posted = new Queue<Action>();

        bool started, consentRunning, privacyOpen;
        float lastConsentTry = float.NegativeInfinity;
        InterstitialAd interstitial;
        RewardedAd rewarded;
        bool interstitialLoading, rewardedLoading;
        float interstitialLoaded, rewardedLoaded;
        float interstitialRetry = RetryFirst, rewardedRetry = RetryFirst;

        /// <summary>True when the player must be able to reopen the consent form (Settings shows a Privacy row).</summary>
        public static bool PrivacyOptionsRequired =>
            instance != null && ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        /// <summary>Called once from Boot. The Editor never starts the SDK (its placeholder ads would cover tests).</summary>
        public static void Create()
        {
            if (instance != null || Application.isEditor) return;
            var go = new GameObject("AdMob");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AdMob>();
        }

        static void Post(Action action)
        {
            lock (gate) posted.Enqueue(action);
        }

        void Update()
        {
            // Drains SDK callbacks on the main thread; costs one lock-free count check on a quiet frame.
            if (posted.Count == 0) return;
            while (true)
            {
                Action next;
                lock (gate)
                {
                    if (posted.Count == 0) return;
                    next = posted.Dequeue();
                }
                try { next(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        static bool UseTestUnits => Debug.isDebugBuild || IsEmulator;

        static bool IsEmulator
        {
            get
            {
                string model = SystemInfo.deviceModel.ToLowerInvariant();
                return model.Contains("sdk_gphone") || model.Contains("emulator") || model.Contains("android sdk built for");
            }
        }

        static string Unit(string real, string test)
        {
            if (UseTestUnits) return test;
            if (real.StartsWith("ca-app-pub-") && !real.Contains("XXXX")) return real;
            Debug.LogError("[PufferLaunch] AdMob unit id missing in AdsConfig; showing test ads");
            return test;
        }

        void Start()
        {
            AudioManager.Changed += ApplyMuted;
            TryConsent();
        }

        void OnDestroy() => AudioManager.Changed -= ApplyMuted;

        void OnApplicationPause(bool paused)
        {
            if (!paused) TryConsentAgain();
        }

        /// <summary>Cheap: asks UMP again when ads never started this session (offline at launch), at most once a minute.</summary>
        public static void TryConsentAgain()
        {
            if (instance == null || instance.started) return;
            instance.TryConsent();
        }

        void TryConsent()
        {
            if (started || consentRunning || Time.realtimeSinceStartup - lastConsentTry < ConsentRetryGap) return;
            consentRunning = true;
            lastConsentTry = Time.realtimeSinceStartup;
            ConsentInformation.Update(new ConsentRequestParameters(), updateError => Post(() =>
            {
                if (updateError != null) Debug.LogWarning($"[PufferLaunch] consent update: {updateError.Message}");
                // A player who consented last session can see ads while the form (if any) is still on its way.
                if (ConsentInformation.CanRequestAds()) StartSdk();
                if (updateError != null)
                {
                    consentRunning = false;
                    return;
                }
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => Post(() =>
                {
                    consentRunning = false;
                    if (formError != null) Debug.LogWarning($"[PufferLaunch] consent form: {formError.Message}");
                    if (ConsentInformation.CanRequestAds()) StartSdk();
                }));
            }));
        }

        /// <summary>Main thread only (every caller runs from Update or Start).</summary>
        void StartSdk()
        {
            if (started) return;
            started = true;
            MobileAds.Initialize(_ => Post(() =>
            {
                ApplyMuted();
                LoadInterstitial();
                LoadRewarded();
            }));
        }

        /// <summary>Ads follow the game's sound switch (set only after Initialize: the SDK ignores it before).</summary>
        void ApplyMuted()
        {
            if (started) MobileAds.SetApplicationMuted(SaveSystem.Data.muted);
        }

        static bool CanLoad => ConsentInformation.CanRequestAds();

        void LoadInterstitial()
        {
            if (interstitialLoading || !CanLoad) return;
            interstitialLoading = true;
            interstitial?.Destroy();
            interstitial = null;
            InterstitialAd.Load(Unit(AdsConfig.Interstitial, AdsConfig.TestInterstitial), new AdRequest(), (ad, error) => Post(() =>
            {
                interstitialLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[PufferLaunch] interstitial load failed: {error?.GetMessage()}");
                    StartCoroutine(After(interstitialRetry, LoadInterstitial));
                    interstitialRetry = Mathf.Min(RetryMax, interstitialRetry * 2f);
                    return;
                }
                interstitial = ad;
                interstitialLoaded = Time.realtimeSinceStartup;
                interstitialRetry = RetryFirst;
            }));
        }

        void LoadRewarded()
        {
            if (rewardedLoading || !CanLoad) return;
            rewardedLoading = true;
            rewarded?.Destroy();
            rewarded = null;
            RewardedAd.Load(Unit(AdsConfig.Rewarded, AdsConfig.TestRewarded), new AdRequest(), (ad, error) => Post(() =>
            {
                rewardedLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[PufferLaunch] rewarded load failed: {error?.GetMessage()}");
                    StartCoroutine(After(rewardedRetry, LoadRewarded));
                    rewardedRetry = Mathf.Min(RetryMax, rewardedRetry * 2f);
                    return;
                }
                rewarded = ad;
                rewardedLoaded = Time.realtimeSinceStartup;
                rewardedRetry = RetryFirst;
            }));
        }

        static bool Fresh(float loadedAt) => Time.realtimeSinceStartup - loadedAt < MaxAge;

        /// <summary>Shows an interstitial if the rules allow and one is ready, then runs <paramref name="next"/>
        /// exactly once (straight away when there is no ad).</summary>
        public static void InterstitialThen(int levelsCleared, bool chapterEnd, Action next)
        {
            var self = instance;
            if (self == null || !Policy.ShouldShowInterstitial(Time.realtimeSinceStartup, levelsCleared, chapterEnd))
            {
                next();
                return;
            }
            var ad = self.interstitial;
            if (ad == null || !ad.CanShowAd() || !Fresh(self.interstitialLoaded))
            {
                if (ad != null && !Fresh(self.interstitialLoaded)) self.LoadInterstitial();
                next();
                return;
            }
            self.interstitial = null;
            bool opened = false, done = false;
            void Finish()
            {
                if (done) return;
                done = true;
                ad.Destroy();
                self.LoadInterstitial();
                next();
            }
            ad.OnAdFullScreenContentOpened += () => Post(() =>
            {
                opened = true;
                Policy.RecordFullscreenAd(Time.realtimeSinceStartup);
            });
            ad.OnAdFullScreenContentClosed += () => Post(Finish);
            ad.OnAdFullScreenContentFailed += _ => Post(Finish);
            // Only a show that never opens times out: an opened ad may stay up while the player visits the store.
            self.StartCoroutine(After(OpenTimeout, () => { if (!opened) Finish(); }));
            ad.Show();
        }

        /// <summary>Opt-in rewarded ad. <paramref name="result"/> gets true only when the reward was earned; false when
        /// no ad is ready (offline, no fill, no consent), so the caller says "No ad available" and gives nothing.</summary>
        public static void ShowRewarded(Action<bool> result)
        {
            var self = instance;
            var ad = self != null ? self.rewarded : null;
            if (ad == null || !ad.CanShowAd() || !Fresh(self.rewardedLoaded))
            {
                if (ad != null && !Fresh(self.rewardedLoaded)) self.LoadRewarded();
                result(false);
                return;
            }
            self.rewarded = null;
            bool opened = false, earned = false, done = false;
            void Finish()
            {
                if (done) return;
                done = true;
                ad.Destroy();
                self.LoadRewarded();
                result(earned);
            }
            ad.OnAdFullScreenContentOpened += () => Post(() =>
            {
                opened = true;
                Policy.RecordFullscreenAd(Time.realtimeSinceStartup);
            });
            ad.OnAdFullScreenContentClosed += () => Post(() => self.StartCoroutine(After(RewardGrace, Finish)));
            ad.OnAdFullScreenContentFailed += _ => Post(Finish);
            self.StartCoroutine(After(OpenTimeout, () => { if (!opened) Finish(); }));
            ad.Show(_ => Post(() => earned = true));
        }

        /// <summary>Settings' Privacy row: reopens the consent form. A withdrawn consent drops the ads already loaded.</summary>
        public static void ShowPrivacyOptions()
        {
            var self = instance;
            if (self == null || self.privacyOpen) return;   // a double tap must not stack two forms
            self.privacyOpen = true;
            ConsentForm.ShowPrivacyOptionsForm(error => Post(() =>
            {
                self.privacyOpen = false;
                if (error != null) Debug.LogWarning($"[PufferLaunch] privacy form: {error.Message}");
                if (ConsentInformation.CanRequestAds())
                {
                    if (!self.started) self.StartSdk();
                    else
                    {
                        self.LoadInterstitial();   // consent given again after a withdrawal: refill
                        self.LoadRewarded();
                    }
                    return;
                }
                self.interstitial?.Destroy();
                self.interstitial = null;
                self.rewarded?.Destroy();
                self.rewarded = null;
            }));
        }

        static IEnumerator After(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action();
        }
    }
}

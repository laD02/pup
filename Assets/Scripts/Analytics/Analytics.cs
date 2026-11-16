using System;
using UnityEngine;

namespace SealCatapult.Analytics
{
    /// <summary>
    /// Firebase Analytics through JNI (no Firebase Unity SDK): the library comes from Editor/FirebaseDependencies.xml,
    /// the app ids from Plugins/Android/FirebaseConfig.androidlib. Collection starts off (manifest) and is switched on
    /// only once the ads consent allows (AdMob.StartSdk), off again when it is withdrawn. GAME_DESIGN.md §13d.
    /// Two events: level_start and level_end, so the level funnel shows where players stop.
    /// </summary>
    public static class GameAnalytics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject analytics;
        static bool broken;

        static AndroidJavaObject Instance
        {
            get
            {
                if (analytics != null || broken) return analytics;
                try
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var cls = new AndroidJavaClass("com.google.firebase.analytics.FirebaseAnalytics"))
                        analytics = cls.CallStatic<AndroidJavaObject>("getInstance", activity);
                }
                catch (Exception e)
                {
                    broken = true;   // no library or no Firebase app: the game plays on without analytics
                    Debug.LogWarning($"[SealCatapult] analytics off: {e.Message}");
                }
                return analytics;
            }
        }
#endif

        /// <summary>On once the player's consent allows ads (consent given, or not required where they live).</summary>
        public static void SetCollection(bool enabled)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { Instance?.Call("setAnalyticsCollectionEnabled", enabled); }
            catch (Exception e) { Debug.LogWarning($"[SealCatapult] analytics consent: {e.Message}"); }
#endif
        }

        /// <param name="level">1-based level number.</param>
        public static void LevelStart(int level, bool attackOnly) =>
            Log("level_start", ("level_name", level.ToString()), ("attack_only", attackOnly ? 1 : 0));

        public static void LevelEnd(int level, bool won, int stars, int pupsOut) =>
            Log("level_end", ("level_name", level.ToString()), ("success", won ? 1 : 0), ("stars", stars), ("pups_out", pupsOut));

        static void Log(string name, params (string key, object value)[] parameters)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var fa = Instance;
            if (fa == null) return;
            try
            {
                using (var bundle = new AndroidJavaObject("android.os.Bundle"))
                {
                    foreach (var (key, value) in parameters)
                    {
                        if (value is int i) bundle.Call("putLong", key, (long)i);
                        else bundle.Call("putString", key, value.ToString());
                    }
                    fa.Call("logEvent", name, bundle);
                }
            }
            catch (Exception e) { Debug.LogWarning($"[SealCatapult] analytics event {name}: {e.Message}"); }
#endif
        }
    }
}

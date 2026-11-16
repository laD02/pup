using System;
using UnityEngine;

namespace SealCatapult.Core
{
    /// <summary>
    /// A short tick through Android's own haptic feedback. It needs no permission and follows the phone's "touch
    /// feedback" setting, so the game has no switch of its own (Handheld.Vibrate is a long buzz and adds VIBRATE).
    /// shortcut: one fixed effect, no in-game toggle; add both if players ask for stronger or optional feedback.
    /// </summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        const int VirtualKey = 1;   // android.view.HapticFeedbackConstants.VIRTUAL_KEY
        static AndroidJavaObject activity, view;
        static AndroidJavaRunnable tick;
        static bool broken;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>Runs on Android's UI thread, outside Tap's try: it catches for itself.</summary>
        static void Tick()
        {
            try { view.Call<bool>("performHapticFeedback", VirtualKey); }
            catch (Exception e)
            {
                broken = true;
                Debug.LogWarning($"[SealCatapult] haptics off: {e.Message}");
            }
        }
#endif

        public static void Tap()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (broken) return;
            try
            {
                if (view == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                        view = window.Call<AndroidJavaObject>("getDecorView");
                    if (view == null) throw new InvalidOperationException("no decor view");
                    tick = Tick;
                }
                activity.Call("runOnUiThread", tick);   // views are touched on Android's UI thread only
            }
            catch (Exception e)
            {
                broken = true;   // a phone without the call must not log on every hit
                Debug.LogWarning($"[SealCatapult] haptics off: {e.Message}");
            }
#endif
        }
    }
}

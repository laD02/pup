using System;
using System.Collections;
using PufferLaunch.Core;
using UnityEngine;

namespace PufferLaunch.Review
{
    /// <summary>
    /// Google Play In-App Review, asked once (GAME_DESIGN §13c). Calls the Play review library straight through JNI;
    /// the library is declared for EDM in Review/Editor/PlayReviewDependencies.xml.
    /// </summary>
    public static class InAppReview
    {
        /// <summary>First level (1-based) whose win may ask.</summary>
        public const int AfterLevel = 5;

        const float Delay = 1.5f;      // let the win card land first
        const float Timeout = 60f;     // Play never answering must not keep the coroutine alive

        const int Idle = 0, Pending = 1, Accepted = 2, Failed = 3;
        static volatile int state = Idle;   // written by Play's callback thread, read by the coroutine
        static float pendingUntil;          // a coroutine killed by a scene change must not leave Pending forever

        public static bool ShouldAsk(SaveData data, int levelIndex, bool won) =>
            won && levelIndex >= AfterLevel - 1 && !data.reviewAsked;

        /// <summary>Waits, then asks Play. The flag is saved only when Play accepted the request, so a failure asks again
        /// on a later win.</summary>
        public static IEnumerator AskOnce()
        {
            yield return new WaitForSecondsRealtime(Delay);
            bool inFlight = state == Pending && Time.realtimeSinceStartup < pendingUntil;
            if (!Application.isFocused || ScreenFader.LoadPending || inFlight) yield break;
#if UNITY_ANDROID && !UNITY_EDITOR
            state = Pending;
            pendingUntil = Time.realtimeSinceStartup + Timeout;
            Request();
            while (state == Pending && Time.realtimeSinceStartup < pendingUntil) yield return null;
            if (state == Accepted)
            {
                SaveSystem.Data.reviewAsked = true;
                SaveSystem.Save();
            }
            state = Idle;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject activity, manager;   // kept alive until Play calls back

        static void Request()
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using (var factory = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory"))
                    manager = factory.CallStatic<AndroidJavaObject>("create", activity);
                using (var task = manager.Call<AndroidJavaObject>("requestReviewFlow"))
                    task.Call<AndroidJavaObject>("addOnCompleteListener", new OnComplete(OnRequested)).Dispose();
            }
            catch (Exception e)
            {
                Fail(e.Message);
            }
        }

        static void OnRequested(AndroidJavaObject task)
        {
            try
            {
                if (!task.Call<bool>("isSuccessful"))
                {
                    Fail("requestReviewFlow failed");
                    return;
                }
                using (var info = task.Call<AndroidJavaObject>("getResult"))
                    manager.Call<AndroidJavaObject>("launchReviewFlow", activity, info).Dispose();
                // Play took the request; it alone decides whether the sheet shows and never says if the player rated.
                state = Accepted;
            }
            catch (Exception e)
            {
                Fail(e.Message);
            }
        }

        static void Fail(string why)
        {
            Debug.LogWarning($"[PufferLaunch] In-app review not shown: {why}");
            state = Failed;
        }

        sealed class OnComplete : AndroidJavaProxy
        {
            readonly Action<AndroidJavaObject> done;

            public OnComplete(Action<AndroidJavaObject> done) : base("com.google.android.gms.tasks.OnCompleteListener") =>
                this.done = done;

            // Name and signature must match the Java interface method.
            public void onComplete(AndroidJavaObject task) => done(task);
        }
#endif
    }
}

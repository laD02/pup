using System.Collections;
using SealCatapult.Core;
using TMPro;
using UnityEngine;

namespace SealCatapult.UI
{
    /// <summary>Tells the player when the save could not be written or read (audit 20/11: a full disk lost stars without
    /// a word), and writes a failed save again when the app goes to the background. One hidden object for the whole
    /// session; it does no per-frame work.</summary>
    public class SaveNotice : MonoBehaviour
    {
        const float ShowSeconds = 6f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var go = new GameObject("SaveNotice");
            DontDestroyOnLoad(go);
            go.AddComponent<SaveNotice>();
        }

        void OnEnable() => SaveSystem.Problem += Show;
        void OnDisable() => SaveSystem.Problem -= Show;

        void OnApplicationPause(bool goingAway)
        {
            if (goingAway) SaveSystem.RetryIfFailed();
        }

        GameObject shown;

        void Show(string key)
        {
            // One at a time: a full disk raises "damaged" at boot and "could not save" a moment later, at the same spot.
            StopAllCoroutines();
            if (shown != null) Destroy(shown);
            StartCoroutine(Toast(Loc.T(key)));
        }

        /// <summary>A pill along the top, over every screen but the scene wipe; it takes no taps.</summary>
        IEnumerator Toast(string text)
        {
            var canvas = UiKit.CreateCanvas("Save notice", 900);
            shown = canvas.gameObject;
            DontDestroyOnLoad(canvas.gameObject);
            canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled = false;
            var pill = UiKit.Image(UiKit.SafeRoot(canvas), "ui_pill", UiKit.Top, new Vector2(0, -150), new Vector2(1200, 110));
            pill.type = UnityEngine.UI.Image.Type.Sliced;
            pill.preserveAspect = false;
            UiKit.Image(pill.transform, "glyph_lock", new Vector2(0f, 0.5f), new Vector2(24, 0), new Vector2(64, 64));   // a mark, not only words
            UiKit.Text(pill.transform, text, 38, UiKit.Center, new Vector2(40, 2), new Vector2(1040, 96), Color.white,
                TextAlignmentOptions.Center, true);
            pill.gameObject.AddComponent<SlideIn>().from = new Vector2(0, 80);
            yield return new WaitForSecondsRealtime(ShowSeconds);
            Destroy(canvas.gameObject);
        }
    }
}

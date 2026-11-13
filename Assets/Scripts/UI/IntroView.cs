using System.Collections;
using PufferLaunch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PufferLaunch.UI
{
    /// <summary>
    /// Opening story (GDD §R6): five ChatGPT paintings (Resources/Art/story_1..5) shown full screen, each drifting in a
    /// slow zoom and pan, cross-faded, with one caption line. Tap moves on; Skip ends it. Plays once on the first run
    /// (MainMenuView) and again from the menu's Story button.
    /// </summary>
    public class IntroView : MonoBehaviour
    {
        const int Scenes = 5;
        const float SceneSeconds = 4.2f;
        const float FadeSeconds = 0.6f;
        const float Zoom = 1.1f;   // each painting grows to this scale over its scene

        Image current, next;
        TMPro.TextMeshProUGUI caption;
        System.Action onDone;
        bool advance, finished;

        /// <summary>Shows the story; onDone runs when it ends (played through or skipped). Without art it just calls onDone.</summary>
        public static void Play(System.Action onDone)
        {
            if (!Art.Has("story_1"))
            {
                onDone?.Invoke();
                return;
            }
            var canvas = UiKit.CreateCanvas("Intro", 50);
            var view = canvas.gameObject.AddComponent<IntroView>();
            view.onDone = onDone;
            view.Build(canvas);
        }

        void Build(Canvas canvas)
        {
            var black = UiKit.Overlay(canvas.transform, "Black");
            black.GetComponent<Image>().color = Color.black;
            // Tapping anywhere moves to the next painting.
            var tap = black.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => advance = true);
            // Two full-screen layers that swap roles for the cross-fade; envelope-fitted to cover any aspect.
            current = Layer(black, "A");
            next = Layer(black, "B");
            var bar = UiKit.Place(UiKit.Node(black, "Caption bar"), UiKit.Bottom, new Vector2(0, 30), new Vector2(1500, 110));
            var barImg = bar.gameObject.AddComponent<Image>();
            barImg.color = new Color(0f, 0f, 0f, 0.45f);
            barImg.raycastTarget = false;
            caption = UiKit.Text(bar, "", 42, UiKit.Center, new Vector2(0, 2), new Vector2(1440, 100), Color.white,
                TMPro.TextAlignmentOptions.Center, true);
            var safe = UiKit.SafeRoot(canvas);
            UiKit.Button(safe, Loc.T("skip"), ButtonStyle.Teal, UiKit.TopRight, new Vector2(-24, -24), new Vector2(280, 100), Finish, "glyph_next");
            StartCoroutine(Run());
        }

        static Image Layer(Transform parent, string name)
        {
            var img = UiKit.Image(parent, "story_1", UiKit.Center, Vector2.zero, new Vector2(1920, 1080));
            img.name = name;
            img.preserveAspect = false;
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        IEnumerator Run()
        {
            for (int i = 1; i <= Scenes && !finished; i++)
            {
                if (!Art.Has($"story_{i}")) continue;
                next.sprite = Art.Get($"story_{i}");
                next.transform.SetSiblingIndex(current.transform.GetSiblingIndex() + 1);   // over the old one, under the caption
                caption.text = Loc.T($"story_{i}");
                // Alternate the drift so consecutive paintings do not slide the same way.
                Vector2 drift = new Vector2(i % 2 == 0 ? -40f : 40f, i % 3 == 0 ? 20f : -15f);
                advance = false;
                for (float t = 0f; t < SceneSeconds && !advance && !finished; t += Time.unscaledDeltaTime)
                {
                    float k = t / SceneSeconds;
                    float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / FadeSeconds));
                    next.color = new Color(1f, 1f, 1f, fade);
                    current.color = new Color(1f, 1f, 1f, 1f - fade);
                    next.transform.localScale = Vector3.one * Mathf.Lerp(1f, Zoom, k);
                    ((RectTransform)next.transform).anchoredPosition = drift * k;
                    yield return null;
                }
                (current, next) = (next, current);
            }
            Finish();
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            var group = gameObject.AddComponent<CanvasGroup>();
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / 0.4f;
                yield return null;
            }
            onDone?.Invoke();
            Destroy(gameObject);
        }
    }
}

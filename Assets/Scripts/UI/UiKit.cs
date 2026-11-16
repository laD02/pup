using System;
using SealCatapult.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SealCatapult.UI
{
    public enum ButtonStyle { Teal, Warm, Gold }

    /// <summary>Small helpers that build the uGUI + TextMeshPro screens in code.</summary>
    public static class UiKit
    {
        public static readonly Color Ink = new Color32(0x1b, 0x2a, 0x49, 0xff);
        public static readonly Color White = Color.white;
        public static readonly Color Dim = new Color(0.03f, 0.08f, 0.18f, 0.86f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 Top = new Vector2(0.5f, 1f);
        public static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        static TMP_FontAsset font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<TMP_FontAsset>("Fonts/Baloo2 SDF");
                    if (font == null) Debug.LogError("[SealCatapult] Missing Resources/Fonts/Baloo2 SDF");
                }
                return font;
            }
        }

        static Material outlineMaterial;

        public static Material OutlineMaterial
        {
            get
            {
                if (outlineMaterial == null)
                {
                    outlineMaterial = Resources.Load<Material>("Fonts/Baloo2 SDF Outline");
                    if (outlineMaterial == null) Debug.LogWarning("[SealCatapult] Missing Resources/Fonts/Baloo2 SDF Outline.mat");
                }
                return outlineMaterial;
            }
        }

        public static Canvas CreateCanvas(string name, int order = 0)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);  // landscape
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            UnityEngine.Object.DontDestroyOnLoad(es);
        }

        /// <summary>Child that fits Screen.safeArea, so HUD corners stay clear of phone notches and cutouts.</summary>
        public static RectTransform SafeRoot(Canvas canvas)
        {
            var rt = Stretch(Node(canvas.transform, "SafeArea"));
            rt.gameObject.AddComponent<SafeArea>();
            return rt;
        }

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Space kept free at each end of a button label: 12 px let long words (German) run onto the rounded caps.</summary>
        const float LabelPad = 20f;

        /// <summary>Smallest touch area in reference pixels: about 48dp on a 2400×1080 phone at 480 dpi (scale ~1.1).</summary>
        public const float MinHit = 130f;

        /// <summary>An invisible touch area around a small control, so the tap target is at least <see cref="MinHit"/>
        /// without changing how it looks. Clicks on it reach the control (it is a child).</summary>
        public static void PadHitArea(RectTransform control)
        {
            var size = control.sizeDelta;
            if (size.x >= MinHit && size.y >= MinHit) return;
            var hit = Place(Node(control, "Hit"), Center, Vector2.zero, Vector2.Max(size, new Vector2(MinHit, MinHit)));
            hit.gameObject.AddComponent<Image>().color = Color.clear;
            hit.SetAsFirstSibling();   // behind the control's own pictures
        }

        /// <summary>Anchor + pivot at the same point, then offset and size.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(Transform parent, string sprite, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rt = Place(Node(parent, sprite), anchor, position, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Art.Get(sprite);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Full-screen dimmer that also blocks clicks to whatever is behind it.</summary>
        public static RectTransform Overlay(Transform parent, string name)
        {
            var rt = Stretch(Node(parent, name));
            rt.gameObject.AddComponent<Image>().color = Dim;
            return rt;
        }

        public static Image Panel(Transform parent, Vector2 size, Vector2 position = default)
        {
            var rt = Place(Node(parent, "Panel"), Center, position, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Art.Get("ui_panel");
            img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size, Vector2 anchor, Vector2 position,
            Vector2 box, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Center, bool outline = false)
        {
            var rt = Place(Node(parent, "Text"), anchor, position, box);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = Font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color ?? Ink;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            // German/French run ~30% longer than English: shrink to fit the box instead of spilling out of it.
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = size;
            tmp.fontSizeMin = size * 0.65f;
            // Preset material asset (outline + soft drop shadow) so its shader variant ships in the build.
            if (outline && OutlineMaterial != null) tmp.fontSharedMaterial = OutlineMaterial;
            return tmp;
        }

        /// <summary>Big game-logo style text: gold-to-orange gradient, outline and drop shadow.</summary>
        public static TextMeshProUGUI Logo(Transform parent, string text, float size, Vector2 anchor, Vector2 position, Vector2 box)
        {
            var t = Text(parent, text, size, anchor, position, box, Color.white, TextAlignmentOptions.Center, true);
            t.enableVertexGradient = true;
            t.colorGradient = new VertexGradient(new Color32(0xff, 0xf3, 0xa8, 0xff), new Color32(0xff, 0xf3, 0xa8, 0xff),
                new Color32(0xff, 0xa8, 0x1c, 0xff), new Color32(0xff, 0x8a, 0x1c, 0xff));
            t.fontStyle = FontStyles.Bold;
            t.characterSpacing = 2f;
            return t;
        }

        public static Button Button(Transform parent, string label, ButtonStyle style, Vector2 anchor, Vector2 position,
            Vector2 size, Action onClick, string glyph = null)
        {
            var rt = Place(Node(parent, "Button " + (label ?? glyph)), anchor, position, size);
            var img = rt.gameObject.AddComponent<Image>();
            // A glyph-only button with a painted round icon (icon_<glyph name>, Tools/cut_props.py) is just that picture.
            string icon = string.IsNullOrEmpty(label) && glyph != null && glyph.StartsWith("glyph_") ? "icon_" + glyph.Substring(6) : null;
            bool painted = icon != null && Art.Has(icon);
            img.sprite = Art.Get(painted ? icon : style switch
            {
                ButtonStyle.Warm => "ui_button_warm",
                ButtonStyle.Gold => "ui_button_gold",
                _ => "ui_button",
            });
            img.type = painted ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
            img.preserveAspect = painted;
            if (painted) glyph = null;
            var button = rt.gameObject.AddComponent<Button>();
            PadHitArea(rt);
            rt.gameObject.AddComponent<PopIn>().delay = 0.05f * (rt.GetSiblingIndex() % 8);
            button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<ButtonJuice>();
            button.onClick.AddListener(() =>
            {
                AudioManager.Sfx("click");
                onClick?.Invoke();
            });

            float glyphSize = Mathf.Min(size.y * 0.7f, 90f);
            float textLeft = 0f;
            if (glyph != null)
            {
                bool glyphOnly = string.IsNullOrEmpty(label);
                var g = Image(rt, glyph, glyphOnly ? Center : new Vector2(0f, 0.5f),
                    glyphOnly ? new Vector2(0, 3) : new Vector2(24, 3), new Vector2(glyphSize, glyphSize));
                g.name = "Glyph";
                textLeft = glyphOnly ? 0f : glyphSize + 16f;
            }
            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(rt, label, size.y * 0.42f, Center, new Vector2(textLeft * 0.5f, 4), new Vector2(size.x - textLeft - LabelPad * 2f, size.y),
                    White, TextAlignmentOptions.Center, true);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true;
                t.fontSizeMin = 18;
                t.fontSizeMax = size.y * 0.42f;
            }
            return button;
        }

        /// <summary>A picture that is a button (map nodes): pop-in, squash on press, click sound.</summary>
        public static Button ImageButton(Transform parent, string sprite, Vector2 anchor, Vector2 position, Vector2 size, Action onClick, bool juice = true)
        {
            var img = Image(parent, sprite, anchor, position, size);
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            PadHitArea(img.rectTransform);
            img.gameObject.AddComponent<PopIn>().delay = 0.04f * (img.transform.GetSiblingIndex() % 10);
            if (juice) img.gameObject.AddComponent<ButtonJuice>();   // not on locked or pulsing buttons (it fights the pulse)
            button.onClick.AddListener(() =>
            {
                AudioManager.Sfx("click");
                onClick?.Invoke();
            });
            return button;
        }

        public static Slider Slider(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, float value, Action<float> onChange)
        {
            var root = Place(Node(parent, "Slider"), anchor, position, size);
            var track = Stretch(Node(root, "Track"));
            var trackImg = track.gameObject.AddComponent<Image>();
            // ui_bar (a plain rounded bar) for both: the snow-capped ui_panel and a square fill read as a glitch (QA 15/11).
            trackImg.sprite = Art.Get("ui_bar");
            trackImg.type = UnityEngine.UI.Image.Type.Sliced;
            trackImg.color = new Color32(0x1b, 0x2a, 0x49, 0x66);
            track.offsetMin = new Vector2(0, size.y * 0.25f);
            track.offsetMax = new Vector2(0, -size.y * 0.25f);

            var fillArea = Stretch(Node(root, "Fill Area"));
            fillArea.offsetMin = new Vector2(0, size.y * 0.25f);
            fillArea.offsetMax = new Vector2(0, -size.y * 0.25f);
            var fill = Stretch(Node(fillArea, "Fill"));
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.sprite = Art.Get("ui_bar");
            fillImg.type = UnityEngine.UI.Image.Type.Sliced;
            fillImg.color = new Color32(0x2e, 0xc4, 0xb6, 0xff);

            var handleArea = Stretch(Node(root, "Handle Area"));
            var handle = Node(handleArea, "Handle");
            handle.sizeDelta = new Vector2(size.y * 1.3f, size.y * 1.3f);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.sprite = Art.Get("gem");

            PadHitArea(root);   // a 60px track is too thin a finger target
            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }
    }

    /// <summary>Squash on press, pop on release. Uses unscaled time so it works while paused.</summary>
    public class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        float target = 1f;

        public void OnPointerDown(PointerEventData e) => target = 0.9f;
        public void OnPointerUp(PointerEventData e) => target = 1f;
        public void OnPointerExit(PointerEventData e) => target = 1f;
        void OnDisable() => target = 1f;   // hidden while held: no pointer-up follows, and it would come back squashed

        void Update()
        {
            float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
            if (Mathf.Abs(s - transform.localScale.x) < 0.0005f) return;   // at rest: no canvas dirtying
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>Flips a UI image through numbered sprites (prefix0..prefixN-1) at a fixed rate; unscaled time.</summary>
    public class FrameLoop : MonoBehaviour
    {
        Sprite[] frames;
        float fps;
        Image img;
        float t;

        public void Set(string prefix, int count, float rate) => Set(rate, (prefix, count));

        /// <summary>A loop stitched from several animations, e.g. a bow then a salute. Missing frames are skipped (the
        /// first frame of the Image stays if nothing is found) and the rate is kept positive.</summary>
        public void Set(float rate, params (string prefix, int count)[] parts)
        {
            var list = new System.Collections.Generic.List<Sprite>();
            foreach (var (prefix, count) in parts)
                for (int i = 0; i < count; i++)
                {
                    var s = Art.Get(prefix + i);
                    if (s != null) list.Add(s);
                }
            frames = list.Count > 0 ? list.ToArray() : null;
            fps = Mathf.Max(rate, 0.01f);
            img = GetComponent<Image>();
        }

        void Update()
        {
            if (frames == null || img == null) return;
            t += Time.unscaledDeltaTime * fps;
            var f = frames[(int)t % frames.Length];
            if (img.sprite != f) img.sprite = f;
        }
    }

    /// <summary>Entrance animation: grows from small with a springy overshoot (unscaled time, works while paused).</summary>
    public class PopIn : MonoBehaviour
    {
        public float delay;
        public float duration = 0.35f;

        System.Collections.IEnumerator Start()
        {
            var t = transform;
            // ButtonJuice also drives the scale: left on, it grew the button during the delay before the pop restarted it.
            var juice = GetComponent<ButtonJuice>();
            if (juice != null) juice.enabled = false;
            t.localScale = Vector3.zero;
            for (float w = 0f; w < delay; w += Time.unscaledDeltaTime) yield return null;
            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                float k = time / duration;
                // easeOutBack
                float c = 1.9f;
                float e = 1f + (c + 1f) * Mathf.Pow(k - 1f, 3) + c * Mathf.Pow(k - 1f, 2);
                t.localScale = Vector3.one * e;
                yield return null;
            }
            t.localScale = Vector3.one;
            if (juice != null) juice.enabled = true;
        }

        /// <summary>Hidden mid-pop (a pause menu closed at once): the coroutine dies and Start never runs again, so
        /// finish the pop here or the button stays tiny with its press animation off.</summary>
        void OnDisable()
        {
            transform.localScale = Vector3.one;
            var juice = GetComponent<ButtonJuice>();
            if (juice != null) juice.enabled = true;
        }
    }

    /// <summary>Slides in from an offset and fades up (banners, cards).</summary>
    public class SlideIn : MonoBehaviour
    {
        public Vector2 from = new Vector2(0, -60);
        public float delay;
        public float duration = 0.4f;

        System.Collections.IEnumerator Start()
        {
            var rt = (RectTransform)transform;
            var target = rt.anchoredPosition;
            var group = gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();  // Unity fake-null: no ?? here
            group.alpha = 0f;
            rt.anchoredPosition = target + from;
            for (float w = 0f; w < delay; w += Time.unscaledDeltaTime) yield return null;
            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - time / duration, 3);
                rt.anchoredPosition = Vector2.LerpUnclamped(target + from, target, k);
                group.alpha = k;
                yield return null;
            }
            rt.anchoredPosition = target;
            group.alpha = 1f;
        }
    }

    /// <summary>Keeps its RectTransform inside Screen.safeArea (re-checked when rotation changes).</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect applied;

        void Update()
        {
            var safe = Screen.safeArea;
            if (safe == applied || Screen.width == 0 || Screen.height == 0) return;
            applied = safe;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Turntable spin around the vertical axis (a character showing off on a pedestal).</summary>
    public class Spin : MonoBehaviour
    {
        public float period = 1.6f;
        void Update() => transform.localRotation = Quaternion.Euler(0f, Time.unscaledTime / period * 360f, 0f);
    }

    /// <summary>Gentle bob/pulse for hints and titles.</summary>
    public class Bob : MonoBehaviour
    {
        public float amplitude = 10f;
        public float speed = 2f;
        public bool pulse;
        Vector2 start;
        RectTransform rt;

        void Start()
        {
            rt = (RectTransform)transform;
            start = rt.anchoredPosition;
        }

        void Update()
        {
            float w = Mathf.Sin(Time.unscaledTime * speed);
            if (pulse) transform.localScale = Vector3.one * (1f + 0.08f * w);
            else rt.anchoredPosition = start + new Vector2(0, amplitude * w);
        }
    }
}

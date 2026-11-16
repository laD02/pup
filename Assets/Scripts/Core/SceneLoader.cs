using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SealCatapult.Core
{
    public static class SceneLoader
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string LevelMap = "LevelMap";
        public const string Game = "Game";

        /// <summary>Level index the Game scene should play (0-based).</summary>
        public static int CurrentLevel { get; private set; }

        /// <summary>Wipes to <paramref name="scene"/>. Time.timeScale is left alone: a paused game stays frozen behind the
        /// water (the wipe runs on unscaled time) and GameManager.Start/OnDestroy reset it.</summary>
        public static void Load(string scene) => ScreenFader.Instance.FadeTo(scene);

        /// <summary>Exit velocities of the pups kept for an attack-only retry of <see cref="CurrentLevel"/>; null means
        /// the level starts in the launch pit.</summary>
        public static Vector2[] KeptExits { get; private set; }

        public static void PlayLevel(int index)
        {
            CurrentLevel = index;
            KeptExits = null;
            Load(Game);
        }

        /// <summary>Replays only the attack with the pups that got out last time (the lose card's retry).</summary>
        public static void RetryAttack(int index, System.Collections.Generic.IReadOnlyList<Vector2> exits)
        {
            CurrentLevel = index;
            var kept = new Vector2[exits.Count];
            for (int i = 0; i < kept.Length; i++) kept[i] = exits[i];
            KeptExits = kept;
            Load(Game);
        }
    }

    /// <summary>
    /// Scene wipe like the genre's: water with a wavy edge rises over the screen, a bear runs past carrying the caged
    /// pup while the next scene loads, then the water drains away. Blocks input while covered.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        const float OutTime = 0.4f;
        const float InTime = 0.45f;
        const float MinCovered = 0.5f;
        RectTransform water;
        RectTransform runner;
        float height;

        static ScreenFader instance;
        CanvasGroup group;
        bool busy;
        bool loading;     // a scene was asked for and has not finished loading yet
        string pending;   // scene asked for while a wipe was running

        /// <summary>True from a load request until the new scene is in: the old scene must not record results meanwhile.</summary>
        public static bool LoadPending => instance != null && instance.loading;

        public static ScreenFader Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("ScreenFader");
                    DontDestroyOnLoad(go);
                    var canvas = go.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 1000;
                    go.AddComponent<GraphicRaycaster>();
                    instance = go.AddComponent<ScreenFader>();
                    instance.group = go.AddComponent<CanvasGroup>();
                    var cover = new GameObject("Cover", typeof(RectTransform)).GetComponent<RectTransform>();
                    cover.SetParent(go.transform, false);
                    cover.anchorMin = Vector2.zero;
                    cover.anchorMax = Vector2.one;
                    cover.offsetMin = cover.offsetMax = Vector2.zero;
                    cover.gameObject.AddComponent<Image>().color = new Color32(0x08, 0x16, 0x34, 0x00);   // the water panel covers; this only blocks taps
                    var scaler = go.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                    // Water panel: wider than the screen, tiled wave edge, parked below the bottom until a wipe.
                    var w = new GameObject("Water", typeof(RectTransform)).GetComponent<RectTransform>();
                    w.SetParent(go.transform, false);
                    w.anchorMin = new Vector2(-0.1f, 0f);
                    w.anchorMax = new Vector2(1.1f, 0f);
                    w.pivot = new Vector2(0.5f, 1f);
                    w.sizeDelta = new Vector2(0f, 1400f);
                    w.anchoredPosition = Vector2.zero;
                    var wi = w.gameObject.AddComponent<Image>();
                    wi.sprite = Art.Get("wave_cover");
                    wi.type = Image.Type.Tiled;
                    wi.pixelsPerUnitMultiplier = 0.55f;
                    instance.water = w;
                    var r = new GameObject("Runner", typeof(RectTransform)).GetComponent<RectTransform>();
                    r.SetParent(go.transform, false);
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                    r.sizeDelta = new Vector2(260f, 260f);
                    var cage = new GameObject("Cage", typeof(RectTransform)).GetComponent<RectTransform>();
                    cage.SetParent(r, false);
                    cage.anchoredPosition = new Vector2(-150f, -10f);
                    cage.sizeDelta = new Vector2(150f, 160f);
                    var ci = cage.gameObject.AddComponent<Image>();
                    ci.sprite = Art.Get("cage");
                    ci.preserveAspect = true;
                    var bear = new GameObject("Bear", typeof(RectTransform)).GetComponent<RectTransform>();
                    bear.SetParent(r, false);
                    bear.sizeDelta = new Vector2(220f, 260f);
                    var bi = bear.gameObject.AddComponent<Image>();
                    bi.sprite = Art.Get("bear_taunt_1");
                    bi.preserveAspect = true;
                    instance.runner = r;
                    r.gameObject.SetActive(false);
                    instance.SetAlpha(0f);
                }
                return instance;
            }
        }

        void SetAlpha(float a)
        {
            group.alpha = a;
            group.blocksRaycasts = a > 0.01f;
        }

        /// <summary>0 = water below the screen, 1 = screen covered.</summary>
        void SetWater(float k)
        {
            var canvas = (RectTransform)transform;
            float h = canvas.rect.height + 80f;
            water.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, h, k));
        }

        public void FadeTo(string scene)
        {
            loading = true;
            // A request during a running wipe is kept and played right after it (dropping it froze Retry/Map taps
            // made while the water was still draining; found by a PlayMode test 12/11).
            if (busy)
            {
                pending = scene;
                return;
            }
            StartCoroutine(Run(scene));
        }

        IEnumerator Run(string scene)
        {
            busy = true;
            group.alpha = 1f;   // visible from the first frame: at 0 the water rose unseen behind blocked input
            group.blocksRaycasts = true;
            AudioManager.Sfx("splash", 0f, 0.8f);
            for (float t = 0f; t < OutTime; t += Time.unscaledDeltaTime)
            {
                SetWater(1f - Mathf.Pow(1f - t / OutTime, 2));
                yield return null;
            }
            SetWater(1f);
            runner.gameObject.SetActive(true);
            float covered = 0f;
            var load = SceneManager.LoadSceneAsync(scene);
            if (load == null)
            {
                // Scene missing from the build: Unity has logged why. Drain the water so the old screen stays usable.
                Debug.LogError($"[SealCatapult] Could not load scene '{scene}'.");
                runner.gameObject.SetActive(false);
                SetWater(0f);
                SetAlpha(0f);
                busy = false;
                loading = false;
                if (pending != null)
                {
                    string next = pending;
                    pending = null;
                    FadeTo(next);
                }
                yield break;
            }
            while (!load.isDone || covered < MinCovered)
            {
                if (load.isDone) loading = pending != null;   // the new scene runs from here on
                covered += Time.unscaledDeltaTime;
                runner.anchoredPosition = new Vector2(Mathf.Lerp(-700f, 700f, (covered / 1.6f) % 1f), -40f + Mathf.Abs(Mathf.Sin(covered * 14f)) * 24f);
                yield return null;
            }
            runner.gameObject.SetActive(false);
            loading = pending != null;
            yield return null;
            for (float t = 0f; t < InTime; t += Time.unscaledDeltaTime)
            {
                SetWater(1f - Mathf.Pow(t / InTime, 2));
                yield return null;
            }
            SetWater(0f);
            SetAlpha(0f);
            busy = false;
            if (pending != null)
            {
                string next = pending;
                pending = null;
                StartCoroutine(Run(next));
            }
        }
    }
}

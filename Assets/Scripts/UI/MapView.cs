using SealCatapult.Core;
using SealCatapult.Progress;
using TMPro;
using UnityEngine;

namespace SealCatapult.UI
{
    /// <summary>
    /// Level map (landscape), laid out like the reference (report §15): the top-down island, 10 levels in an S — 1–6
    /// along the south shore left to right, 7–10 back along the north toward the bear fort — as "?" stones, a gold
    /// star stone once cleared, the hero standing on a green pad at the next level to play, cages for the boss and a
    /// fish for the bonus level. No numbers, no caption. This game adds the stars count top right and Upgrades.
    /// </summary>
    public class MapView : MonoBehaviour
    {
        // Node positions on the 1920×1080 reference canvas (centre origin); keep in step with map_bg in Tools/make_art.py.
        // One page per area: its painting and the node positions on it, in level order.
        static readonly (string painting, Vector2[] nodes)[] Pages =
        {
            // Frost Bay (map_bg), levels 1–12. Rows sit inside ±260 so the bottom caption/Upgrades and the top bars stay
            // clear on 20:9 phones (canvas height ≈ 1000).
            ("map_bg", new[]
            {
                new Vector2(-760, -250), new Vector2(-540, -290), new Vector2(-320, -250), new Vector2(-100, -290), new Vector2(120, -250),
                new Vector2(340, -280), new Vector2(560, -240), new Vector2(760, -120), new Vector2(700, 110), new Vector2(460, 170),
                new Vector2(200, 130), new Vector2(-120, 180),
            }),
            // Glacier River (map_bg_2), levels 13–18: along the open snow field below the river and the bear castle.
            ("map_bg_2", new[]
            {
                new Vector2(-700, -200), new Vector2(-420, -290), new Vector2(-140, -200), new Vector2(140, -290), new Vector2(420, -200),
                new Vector2(680, -110),
            }),
        };

        int page;
        Canvas canvas;


        TextMeshProUGUI starsLabel, spendLabel;
        Bob spendPulse;
        ShopView shop;

        void Start()
        {
            SaveSystem.EnsureLoaded();
            // map_bg covers the whole screen: no backdrop under it, and a still screen idles at half the frame rate.
            MenuScene.Setup(backdrop: false);
            Application.targetFrameRate = BootView.StillFps;
            page = PageOf(CurrentLevel(SaveSystem.Data));
            Build();
        }

        /// <summary>The next level to play: the first one open and not yet won (the last level when all are won).</summary>
        static int CurrentLevel(SaveData data)
        {
            int count = GameDatabase.Instance.levels.Length;
            for (int i = 0; i < count; i++)
                if (Unlocked(i, data) && data.stars[i] == 0) return i;
            return count - 1;
        }

        static int PageOf(int level)
        {
            int first = 0;
            for (int p = 0; p < Pages.Length; p++)
            {
                if (level < first + Pages[p].nodes.Length) return p;
                first += Pages[p].nodes.Length;
            }
            return Pages.Length - 1;
        }

        static int FirstLevelOf(int page)
        {
            int first = 0;
            for (int p = 0; p < page; p++) first += Pages[p].nodes.Length;
            return first;
        }

        /// <summary>Normal levels open one after another; the bonus and the boss both open once level 10 is done.</summary>
        static bool Unlocked(int index, SaveData data)
        {
            var kind = GameDatabase.Instance.levels[index].kind;
            int gate = kind == LevelKind.Normal ? index - 1 : 9;
            return index == 0 || data.stars[gate] > 0;
        }

        void ShowPage(int p)
        {
            page = p;
            Destroy(canvas.gameObject);   // the shop has its own canvas and is kept
            Build();
        }

        /// <summary>Android Back: the shop first, then the previous map page, then the main menu.</summary>
        void Update()
        {
            if (!GameInput.BackPressed() || ScreenFader.LoadPending) return;
            if (shop.HandleBack()) return;
            if (page > 0) ShowPage(page - 1);   // Back walks the pages back, then leaves for the menu
            else SceneLoader.Load(SceneLoader.MainMenu);
        }

        void Build()
        {
            canvas = UiKit.CreateCanvas("Map");
            // Top-down island fills the screen (cropped to cover any aspect).
            var map = UiKit.Image(canvas.transform, Pages[page].painting, UiKit.Center, Vector2.zero, new Vector2(1920, 1080));
            map.preserveAspect = false;
            var fitter = map.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1920f / 1080f;
            map.transform.SetAsFirstSibling();
            var root = UiKit.SafeRoot(canvas);

            // Top: back button left, sound/music centre (as on the reference's map).
            UiKit.Button(root, null, ButtonStyle.Teal, UiKit.TopLeft, new Vector2(24, -24), new Vector2(100, 100),
                () => SceneLoader.Load(SceneLoader.MainMenu), "glyph_back");
            float pitch = UiKit.MinHit;   // touch areas side by side, not overlapping
            var cluster = UiKit.Place(UiKit.Node(root, "Cluster"), UiKit.Top, new Vector2(0, -16), new Vector2(pitch * 2f, 100));
            HudView.AudioButton(cluster, new Vector2(-pitch * 0.5f, 0), false);
            HudView.AudioButton(cluster, new Vector2(pitch * 0.5f, 0), true);

            // This game's own: stars collected (top right) pay for upgrades (bottom right).
            var stars = UiKit.Place(UiKit.Node(root, "Stars"), UiKit.TopRight, new Vector2(-24, -24), new Vector2(260, 96));
            var pill = stars.gameObject.AddComponent<UnityEngine.UI.Image>();
            pill.sprite = Art.Get("ui_pill");
            pill.type = UnityEngine.UI.Image.Type.Sliced;
            UiKit.Image(stars, "star_full", new Vector2(0f, 0.5f), new Vector2(14, 2), new Vector2(70, 70));
            starsLabel = UiKit.Text(stars, "", 48, new Vector2(1f, 0.5f), new Vector2(-18, 2), new Vector2(160, 80), Color.white,
                TextAlignmentOptions.Right, true);
            var upgrades = UiKit.Button(root, Loc.T("shop"), ButtonStyle.Warm, UiKit.BottomRight, new Vector2(-24, 24), new Vector2(320, 110),
                () => shop.Open(), "glyph_shop");
            // Stars left to spend, on the button that spends them (the pill above counts stars earned, a different
            // number); the badge pulses while something is affordable.
            var badge = UiKit.Image(upgrades.transform, "star_full", UiKit.TopRight, new Vector2(4, 50), new Vector2(84, 84));   // on the corner, clear of the label
            spendLabel = UiKit.Text(badge.transform, "", 40, UiKit.Center, new Vector2(0, -4), new Vector2(70, 60));
            spendPulse = badge.gameObject.AddComponent<Bob>();
            spendPulse.pulse = true;
            spendPulse.speed = 5f;
            if (shop == null) shop = ShopView.Create(RefreshStars);
            RefreshStars();
            // Page arrows on the side edges, vertically centred: next area / previous area.
            if (page + 1 < Pages.Length)
                UiKit.Button(root, null, ButtonStyle.Gold, new Vector2(1f, 0.5f), new Vector2(-24, 0), new Vector2(110, 110),
                    () => ShowPage(page + 1), "glyph_next");
            if (page > 0)
            {
                var prev = UiKit.Button(root, null, ButtonStyle.Gold, new Vector2(0f, 0.5f), new Vector2(24, 0), new Vector2(110, 110),
                    () => ShowPage(page - 1), "glyph_next");
                prev.transform.Find("Glyph").localScale = new Vector3(-1f, 1f, 1f);   // the same arrow, pointing left
            }

            var data = SaveSystem.Data;
            var nodes = Pages[page].nodes;
            int first = FirstLevelOf(page);
            int count = Mathf.Clamp(GameDatabase.Instance.levels.Length - first, 0, nodes.Length);
            // Nodes sit on the painted trail, so they share the painting's frame (the full canvas), not the safe area.
            var trail = UiKit.Stretch(UiKit.Node(canvas.transform, "Trail"));
            for (int i = 0; i < count - 1; i++) DrawDots(trail, nodes[i], nodes[i + 1]);
            for (int i = 0; i < count; i++) BuildNode(trail, first + i, nodes[i], data);
        }

        void RefreshStars()
        {
            var data = SaveSystem.Data;
            starsLabel.text = $"{Shop.TotalStars(data)}/{GameDatabase.Instance.levels.Length * 3}";
            int spendable = Shop.AvailableStars(data);
            spendLabel.text = spendable.ToString();
            spendPulse.gameObject.SetActive(spendable > 0);   // nothing to spend: no badge
            bool affordable = false;
            foreach (var def in GameDatabase.Instance.upgrades.upgrades) affordable |= Shop.CanBuy(data, def);
            spendPulse.enabled = affordable;
            if (!affordable) spendPulse.transform.localScale = Vector3.one;
        }

        /// <summary>Footprint trail between levels.</summary>
        static void DrawDots(Transform root, Vector2 a, Vector2 b)
        {
            const int Steps = 6;
            for (int k = 1; k < Steps; k++)
            {
                var p = Vector2.Lerp(a, b, k / (float)Steps) + new Vector2(0, Mathf.Sin(k / (float)Steps * Mathf.PI) * 30f);
                var dot = UiKit.Image(root, "powder", UiKit.Center, p, new Vector2(26, 26));
                dot.color = new Color(0.36f, 0.6f, 0.85f, 0.9f);
            }
        }

        static void BuildNode(Transform root, int index, Vector2 pos, SaveData data)
        {
            var kind = GameDatabase.Instance.levels[index].kind;
            bool unlocked = Unlocked(index, data);
            int stars = data.stars[index];
            bool current = unlocked && stars == 0;
            // "You are here": the green pad under the stone, the hero standing on it.
            if (current) UiKit.Image(root, "node_glow", UiKit.Center, pos + new Vector2(0, -10), new Vector2(230, 150));
            var button = UiKit.ImageButton(root, stars > 0 ? "node_star" : "node_stone", UiKit.Center, pos, new Vector2(150, 150),
                () => SceneLoader.PlayLevel(index), juice: unlocked && !current);
            button.interactable = unlocked;
            if (!unlocked) button.GetComponent<UnityEngine.UI.Image>().color = new Color(0.7f, 0.78f, 0.9f, 0.85f);   // dim, and the lock below
            if (kind == LevelKind.Boss)
            {
                UiKit.Image(button.transform, "cage", UiKit.Center, new Vector2(0, 30), new Vector2(110, 110));
                if (stars == 0)
                {
                    // The captive calls for help until the boss level is won (reference §17.2).
                    var help = UiKit.Text(button.transform, Loc.T("help_cry"), 44, UiKit.Top, new Vector2(70, 70), new Vector2(260, 60), new Color(0.95f, 0.2f, 0.15f), TextAlignmentOptions.Center, true);
                    help.rectTransform.localRotation = Quaternion.Euler(0, 0, -8f);
                    help.gameObject.AddComponent<Bob>().pulse = true;
                }
            }
            else if (kind == LevelKind.Bonus) UiKit.Image(button.transform, "fish", UiKit.Center, new Vector2(0, 10), new Vector2(90, 90));
            if (!unlocked) UiKit.Image(button.transform, "glyph_lock", UiKit.Bottom, new Vector2(0, -8), new Vector2(54, 54));
            if (data.perfect[index] > 0) UiKit.Image(button.transform, "badge_perfect", UiKit.TopRight, new Vector2(18, 18), new Vector2(64, 64));
            // Cleared: the small star row below the stone (this game's 3-star rating, the shop's currency).
            if (stars > 0)
                for (int s = 0; s < 3; s++)
                    UiKit.Image(button.transform, s < stars ? "star_full" : "star_empty", UiKit.Bottom,
                        new Vector2((s - 1) * 38, -30 + (s == 1 ? -6 : 0)), new Vector2(42, 42));
            if (current)
            {
                var hero = UiKit.Image(root, Art.Has("pup_idle_0") ? "pup_idle_0" : "pup", UiKit.Center, pos + new Vector2(0, 70), new Vector2(130, 130));
                hero.raycastTarget = false;
                if (Art.Has("pup_idle_3")) hero.gameObject.AddComponent<FrameLoop>().Set("pup_idle_", 4, 4f);
                var bob = hero.gameObject.AddComponent<Bob>();
                bob.amplitude = 6f;
                bob.speed = 3f;
                button.gameObject.AddComponent<Bob>().pulse = true;
            }
        }
    }

    /// <summary>Shared camera + backdrop + music for menu scenes (same landscape framing as the game).</summary>
    public static class MenuScene
    {
        /// <summary>Camera + backdrop for menu scenes; skyOnly when a painting covers the ground (see Backdrop.Create),
        /// no backdrop at all when a full-screen UI picture hides the world (the map).</summary>
        public static Camera Setup(bool skyOnly = false, bool backdrop = true)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x11, 0x28, 0x5a, 0xff);
            camGo.transform.position = new Vector3(0f, 0.1f, -10f);
            if (backdrop) Backdrop.Create(cam, skyOnly);
            else cam.cullingMask = 0;
            AudioManager.Instance.PlayMusic("music_reef");
            return cam;
        }
    }
}

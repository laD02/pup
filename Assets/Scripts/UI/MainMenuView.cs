using SealCatapult.Core;
using SealCatapult.Launch;
using TMPro;
using UnityEngine;

namespace SealCatapult.UI
{
    /// <summary>
    /// Main menu (landscape), laid out like the reference's title (report §15): the logo across the top, a big Play in
    /// the middle, sound/music/help buttons top-centre above the logo; the cast on stage along the bottom — a pup
    /// sailing up and to the right with a star trail, the whacker with its operator, a smug snow bear on an ice block.
    /// Layout A (GAME_DESIGN.md §R10): rails of big buttons on both sides, one Play into the next level.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        const float FlightSeconds = 3.2f;
        const float MenuShelfHeight = 1.3f;   // snow line of bg_menu above the screen's bottom edge
        static readonly Vector2 FlyerFrom = new Vector2(-7.5f, -2.2f);
        static readonly Vector2 FlyerTo = new Vector2(-1.2f, 3.2f);

        Canvas canvas;
        SettingsView settings;
        InfoView info;
        PupRig flyer;
        float flyerT;
        float trailTimer;

        void Start()
        {
            SaveSystem.EnsureLoaded();
            // The ChatGPT menu painting (bg_menu) covers the screen; the cast stands on its snowy shelf.
            bool painted = Art.Has("bg_menu");
            var cam = MenuScene.Setup(skyOnly: painted);
            Application.targetFrameRate = BootView.GameFps;   // the flyer and its star trail animate all the time
            float ground = Backdrop.GroundY;
            if (painted)
            {
                var bg = Art.Spawn("bg_menu", null, cam.transform.position, 1f, -45);
                float h = cam.orthographicSize * 2f * 1.04f;
                float k = Mathf.Max(h / bg.sprite.bounds.size.y, h * cam.aspect / bg.sprite.bounds.size.x);   // cover any aspect
                bg.transform.localScale = new Vector3(k, k, 1f);
                ground = cam.transform.position.y - cam.orthographicSize + MenuShelfHeight;
            }
            flyer = PupRig.Create(null, FlyerFrom, 20);
            flyer.SetPose(PupRig.Pose.Launched);
            flyer.transform.localScale = Vector3.one * 1.4f;
            // The whacker and its operator stand ready on the left; the bear taunts from its ice block.
            WhackerMachine.Create(new Vector2(-5.6f, ground + 0.95f), null, 1f);
            var op = PupRig.Create(null, new Vector2(-5.6f, ground + 0.95f) + WhackerMachine.OperatorSeat + new Vector2(0f, PupRig.StandHeight), 20);   // on the machine, as in LaunchPhase
            op.SetPose(PupRig.Pose.OpWait);
            Art.Spawn("block_ice", null, new Vector2(5.2f, ground + 0.6f), 1.2f, 4);
            Art.Spawn("bear_smug_0", null, new Vector2(5.2f, ground + 1.2f + 0.95f), 1.5f, 5);
            Loc.Changed += Rebuild;
            SettingsView.ProgressErased += Rebuild;   // the Play label and the pills show the progress
            Rebuild();
            if (!SaveSystem.Data.seenIntro)
                IntroView.Play(() =>
                {
                    SaveSystem.Data.seenIntro = true;
                    SaveSystem.Save();
                });
        }

        void OnDestroy()
        {
            Loc.Changed -= Rebuild;
            SettingsView.ProgressErased -= Rebuild;
        }

        void Rebuild()
        {
            if (canvas != null) Destroy(canvas.gameObject);
            canvas = UiKit.CreateCanvas("MainMenu");
            var root = UiKit.SafeRoot(canvas);
            if (settings == null) settings = SettingsView.Create();   // survive language-change rebuilds
            if (info == null) info = InfoView.Create();
            settings.OnAbout = info.ShowAbout;
            if (shop == null) shop = ShopView.Create(Rebuild);
            var data = SaveSystem.Data;
            int next = MapView.CurrentLevel(data);
            string zone = MapView.CastleName(Progress.LevelCatalog.CastleOf(next));

            // Layout A (GAME_DESIGN.md §R10, like Angry Birds 2). Top corners: where you are (left), stars, help and
            // settings (right).
            var where = UiKit.Place(UiKit.Node(root, "Where"), UiKit.TopLeft, new Vector2(24, -24), new Vector2(480, 124));
            var wherePill = where.gameObject.AddComponent<UnityEngine.UI.Image>();
            wherePill.sprite = Art.Get("ui_pill");
            wherePill.type = UnityEngine.UI.Image.Type.Sliced;
            // Big enough to read at a glance (QA 16/11: 30/40 px read as small on a phone).
            UiKit.Image(where, Art.Has("hud_pup") ? "hud_pup" : "pup", new Vector2(0f, 0.5f), new Vector2(8, 0), new Vector2(108, 108));
            UiKit.Text(where, zone, 38, new Vector2(0f, 0.5f), new Vector2(128, 24), new Vector2(340, 50), Color.white, TextAlignmentOptions.Left, true);
            UiKit.Text(where, Loc.T("level_n", next + 1), 54, new Vector2(0f, 0.5f), new Vector2(128, -24), new Vector2(340, 64), Color.white, TextAlignmentOptions.Left, true);

            float pitch = UiKit.MinHit;
            UiKit.Button(root, null, ButtonStyle.Teal, UiKit.TopRight, new Vector2(-24, -24), new Vector2(104, 104), () => settings.Open(), "glyph_gear");
            UiKit.Button(root, null, ButtonStyle.Teal, UiKit.TopRight, new Vector2(-24 - pitch, -24), new Vector2(104, 104), () => info.ShowHowTo(), "glyph_help");
            var stars = UiKit.Place(UiKit.Node(root, "Stars"), UiKit.TopRight, new Vector2(-24 - pitch * 2f - 10, -24), new Vector2(220, 104));
            var starPill = stars.gameObject.AddComponent<UnityEngine.UI.Image>();
            starPill.sprite = Art.Get("ui_pill");
            starPill.type = UnityEngine.UI.Image.Type.Sliced;
            UiKit.Image(stars, "star_full", new Vector2(0f, 0.5f), new Vector2(12, 2), new Vector2(76, 76));
            UiKit.Text(stars, Progress.Shop.AvailableStars(data).ToString(), 48, new Vector2(1f, 0.5f), new Vector2(-20, 2), new Vector2(110, 80),
                Color.white, TextAlignmentOptions.Right, true);

            var title = UiKit.Logo(root, Loc.T("title"), 140, UiKit.Top, new Vector2(0, -160), new Vector2(1100, 190));   // under the corner pills, also on 4:3
            title.enableAutoSizing = true;
            title.fontSizeMin = 70;
            title.fontSizeMax = 140;
            title.gameObject.AddComponent<Bob>().amplitude = 6f;

            // Side rails: what you do between levels (left), where you go (right). Big, under the thumbs.
            var rail = new Vector2(300, 112);
            UiKit.Button(root, Loc.T("shop"), ButtonStyle.Warm, new Vector2(0f, 0.5f), new Vector2(24, 20), rail, () => shop.Open(), "glyph_shop");
            UiKit.Button(root, Loc.T("story"), ButtonStyle.Teal, new Vector2(0f, 0.5f), new Vector2(24, -112), rail, () => IntroView.Play(null), "glyph_play");
            UiKit.Button(root, Loc.T("map"), ButtonStyle.Teal, new Vector2(1f, 0.5f), new Vector2(-24, 20), rail,
                () => SceneLoader.Load(SceneLoader.LevelMap), "glyph_map");
            UiKit.Button(root, Loc.T("stats"), ButtonStyle.Teal, new Vector2(1f, 0.5f), new Vector2(-24, -112), rail,
                () => statsPopup = StatsPanel.Show(canvas.transform), "glyph_help");

            // The one big button: straight into the next level (a new player's is level 1, teaching by doing).
            UiKit.Button(root, Loc.T("play_level", next + 1), ButtonStyle.Gold, UiKit.Bottom, new Vector2(0, 40), new Vector2(640, 170),
                () => SceneLoader.PlayLevel(MapView.CurrentLevel(SaveSystem.Data)), "glyph_play");   // read now: progress may have been erased
            if (Application.isEditor)
                UiKit.Button(root, Loc.T("quit"), ButtonStyle.Warm, UiKit.BottomRight, new Vector2(-40, 40), new Vector2(300, 96), Application.Quit, "glyph_back");
        }

        GameObject statsPopup;
        ShopView shop;

        /// <summary>Android Back: the story, then an open page, then out of the game (the root screen).</summary>
        void HandleBack()
        {
            if (ScreenFader.LoadPending || IntroView.HandleBack() || settings.HandleBack() || info.HandleBack() || shop.HandleBack()) return;
            if (statsPopup != null)
            {
                Destroy(statsPopup);
                return;
            }
            Application.Quit();
        }

        void Update()
        {
            if (GameInput.BackPressed()) HandleBack();
            // The flyer sails up and to the right along a gentle arc, trailing stars, fades out and starts over.
            flyerT += Time.deltaTime / FlightSeconds;
            if (flyerT >= 1f) flyerT = 0f;
            float k = flyerT;
            Vector2 p = Vector2.Lerp(FlyerFrom, FlyerTo, k) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 0.8f);
            Vector2 dir = (FlyerTo - FlyerFrom).normalized + new Vector2(0f, Mathf.Cos(k * Mathf.PI) * 0.25f);
            flyer.transform.position = p;
            flyer.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - flyer.ArtAngle);
            flyer.SetAlpha(Mathf.Clamp01(Mathf.Min(k * 6f, (1f - k) * 6f)));
            trailTimer -= Time.deltaTime;
            if (trailTimer <= 0f && k > 0.05f && k < 0.9f)
            {
                trailTimer = 0.09f;
                Fx.Burst("star_full", p - dir * 0.5f, 1, 1.2f, 0.7f, 0.22f, 0.5f, 0.2f, new Color(1f, 0.92f, 0.5f));
            }
        }
    }
}

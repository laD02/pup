using PufferLaunch.Core;
using PufferLaunch.Launch;
using TMPro;
using UnityEngine;

namespace PufferLaunch.UI
{
    /// <summary>
    /// Main menu (landscape), laid out like the reference's title (report §15): the logo across the top, a big Play in
    /// the middle, sound/music/help buttons top-centre above the logo; the cast on stage along the bottom — a pup
    /// sailing up and to the right with a star trail, the whacker with its operator, a smug snow bear on an ice block.
    /// How to play / Settings / About sit in a row under Play; the first Play shows the instructions card first.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        const float FlightSeconds = 3.2f;
        const float MenuShelfHeight = 1.3f;   // snow line of bg_menu above the screen's bottom edge
        const float LaunchArtAngle = 35f;   // the Launched frames are drawn flying up-right at about 35°
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
            var op = PupRig.Create(null, new Vector2(-5.6f + 1.75f, ground + PupRig.StandHeight), 20);   // same lever offset as LaunchPhase
            op.Face(false);
            op.SetPose(PupRig.Pose.LeverReady);
            Art.Spawn("block_ice", null, new Vector2(5.2f, ground + 0.6f), 1.2f, 4);
            Art.Spawn("bear_smug_0", null, new Vector2(5.2f, ground + 1.2f + 0.95f), 1.5f, 5);
            Loc.Changed += Rebuild;
            Rebuild();
            if (!SaveSystem.Data.seenIntro)
                IntroView.Play(() =>
                {
                    SaveSystem.Data.seenIntro = true;
                    SaveSystem.Save();
                });
        }

        void OnDestroy() => Loc.Changed -= Rebuild;

        void Rebuild()
        {
            if (canvas != null) Destroy(canvas.gameObject);
            canvas = UiKit.CreateCanvas("MainMenu");
            var root = UiKit.SafeRoot(canvas);
            // Top: sound / music / help, then the logo and tagline across the middle of the sky.
            var cluster = UiKit.Place(UiKit.Node(root, "Cluster"), UiKit.Top, new Vector2(0, -16), new Vector2(330, 100));
            HudView.AudioButton(cluster, new Vector2(-110, 0), false);
            HudView.AudioButton(cluster, new Vector2(0, 0), true);
            if (settings == null) settings = SettingsView.Create();   // survive language-change rebuilds
            if (info == null) info = InfoView.Create();
            UiKit.Button(cluster, null, ButtonStyle.Teal, UiKit.Center, new Vector2(110, 0), new Vector2(96, 96), () => info.ShowHowTo(), "glyph_help");
            var title = UiKit.Logo(root, Loc.T("title"), 150, UiKit.Top, new Vector2(0, -110), new Vector2(1300, 200));
            title.enableAutoSizing = true;
            title.fontSizeMin = 70;
            title.fontSizeMax = 150;
            title.gameObject.AddComponent<Bob>().amplitude = 6f;
            UiKit.Text(root, Loc.T("tagline"), 38, UiKit.Top, new Vector2(0, -310), new Vector2(1100, 70), Color.white,
                TextAlignmentOptions.Center, true);

            // Middle: the big Play; a row of the secondary pages under it.
            UiKit.Button(root, Loc.T("play"), ButtonStyle.Gold, UiKit.Center, new Vector2(0, -40), new Vector2(560, 160), Play, "glyph_play");
            var size = new Vector2(380, 104);
            UiKit.Button(root, Loc.T("how_to"), ButtonStyle.Teal, UiKit.Center, new Vector2(-420, -200), size, () => info.ShowHowTo(), "glyph_tap");
            UiKit.Button(root, Loc.T("settings"), ButtonStyle.Teal, UiKit.Center, new Vector2(0, -200), size, () => settings.Open(), "glyph_gear");
            UiKit.Button(root, Loc.T("about"), ButtonStyle.Teal, UiKit.Center, new Vector2(420, -200), size, () => info.ShowAbout(), "glyph_map");
            // The story replays from a small button in the bottom-left corner.
            UiKit.Button(root, Loc.T("story"), ButtonStyle.Warm, Vector2.zero, new Vector2(40, 40), new Vector2(300, 96),
                () => IntroView.Play(null), "glyph_play");
            if (Application.isEditor)
                UiKit.Button(root, Loc.T("quit"), ButtonStyle.Warm, UiKit.BottomRight, new Vector2(-40, 40), new Vector2(300, 96), Application.Quit, "glyph_back");
        }

        /// <summary>The reference shows its instructions card once after the first Play; after that, straight to the map.</summary>
        void Play()
        {
            if (SaveSystem.Data.seenHowTo)
            {
                SceneLoader.Load(SceneLoader.LevelMap);
                return;
            }
            info.ShowHowTo(() =>
            {
                SaveSystem.Data.seenHowTo = true;
                SaveSystem.Save();
                SceneLoader.Load(SceneLoader.LevelMap);
            });
        }

        void Update()
        {
            // The flyer sails up and to the right along a gentle arc, trailing stars, fades out and starts over.
            flyerT += Time.deltaTime / FlightSeconds;
            if (flyerT >= 1f) flyerT = 0f;
            float k = flyerT;
            Vector2 p = Vector2.Lerp(FlyerFrom, FlyerTo, k) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 0.8f);
            Vector2 dir = (FlyerTo - FlyerFrom).normalized + new Vector2(0f, Mathf.Cos(k * Mathf.PI) * 0.25f);
            flyer.transform.position = p;
            flyer.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - LaunchArtAngle);
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

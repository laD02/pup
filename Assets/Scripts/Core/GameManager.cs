using System;
using System.Collections;
using PufferLaunch.Ads;
using PufferLaunch.Attack;
using PufferLaunch.Launch;
using PufferLaunch.Progress;
using PufferLaunch.Review;
using PufferLaunch.UI;
using Unity.Cinemachine;
using UnityEngine;

namespace PufferLaunch.Core
{
    /// <summary>
    /// Runs one level (landscape): Launch (fixed pit view — bat pups out through the top-right gap) → Transition →
    /// Attack (fortress view — escaped pups glide in from the left, tap to dive) → Won/Lost. Also camera, pause, saving stars.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum State { Launch, Transition, Attack, Won, Lost }

        /// <summary>Half the visible width that must always fit (narrowest landscape screens are 4:3 tablets).</summary>
        const float DesignHalfWidth = 7.1f;
        /// <summary>Half the visible height on 16:9 and wider; wider screens just see more rock and sky.</summary>
        const float MinViewSize = 5f;
        /// <summary>The fortress view is tighter than the pit so blocks and bears read bigger.</summary>
        const float AttackZoom = 1.3f;    // user 13/11: the arena view a bit further out (was 0.9)
        /// <summary>How far the arena camera follows the pup up and down from the arena's centre.</summary>
        const float FollowUp = 0.3f, FollowDown = 1.0f;   // 1.6 up pushed the floor and the bears under an arch off the screen (emulator 13/11)
        const float CaveFollow = 1.6f;

        public State Current { get; private set; }
        public int LevelIndex { get; private set; }
        public bool HasNextLevel => LevelIndex + 1 < GameDatabase.Instance.levels.Length;

        LevelData level;
        GameTuning tuning;
        LaunchPhase launch;
        AttackPhase attack;
        HudView hud;
        PauseMenu pause;
        ResultView result;
        Transform focus;
        CinemachineCamera vcam;
        CinemachineImpulseSource impulse;
        bool paused;

        void Start()
        {
            SaveSystem.EnsureLoaded();
            var db = GameDatabase.Instance;
            LevelIndex = Mathf.Clamp(SceneLoader.CurrentLevel, 0, db.levels.Length - 1);
            level = db.levels[LevelIndex];
            tuning = db.tuning;
            GameInput.GameplayEnabled = true;
            Time.timeScale = 1f;

            BuildCamera();
            Backdrop.Create(Camera.main, skyOnly: true);
            AudioManager.Instance.PlayMusic("music_reef");
            Fx.ShakeRequested += OnShake;

            hud = HudView.Create(this);
            pause = PauseMenu.Create(this);
            result = ResultView.Create(this);

            attack = new GameObject("AttackPhase").AddComponent<AttackPhase>();
            attack.BearsChanged += hud.SetBears;
            attack.ScoreChanged += hud.SetScore;
            attack.AmmoChanged += hud.SetPuffers;
            attack.Build(level, tuning);

            launch = new GameObject("Launcher").AddComponent<LaunchPhase>();
            launch.QueueChanged += hud.SetPuffers;
            int count = level.pufferCount + SaveSystem.Data.upgrades[(int)UpgradeId.School] + AdMob.Policy.TakeBonusPups(LevelIndex);
            Current = State.Launch;
            launch.Begin(tuning, count, OnLaunchDone, level.launcher, level.showTutorial);
            if (level.launcher == LaunchStyle.Bungee) StartCoroutine(After(2.4f, () => hud.Banner(Loc.T("bungee_tip"))));
        }

        void OnDestroy()
        {
            Fx.ShakeRequested -= OnShake;
            Time.timeScale = 1f;
            GameInput.GameplayEnabled = true;
        }

        void BuildCamera()
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            cam.orthographic = true;
            cam.orthographicSize = ViewSize(cam.aspect);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x11, 0x28, 0x5a, 0xff);
            Vector2 c = LaunchPhase.ViewCenter;
            camGo.transform.position = new Vector3(c.x, c.y, -10f);
            camGo.AddComponent<CinemachineBrain>();

            focus = new GameObject("Camera Focus").transform;
            focus.position = c;
            var vgo = new GameObject("CM Camera");
            vcam = vgo.AddComponent<CinemachineCamera>();
            var lens = LensSettings.Default;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = cam.orthographicSize;
            lens.NearClipPlane = 0.1f;
            lens.FarClipPlane = 50f;
            vcam.Lens = lens;
            vcam.Follow = focus;
            var composer = vgo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.9f, 0.9f, 0f);
            vgo.AddComponent<CinemachineImpulseListener>();
            impulse = vgo.AddComponent<CinemachineImpulseSource>();
            impulse.ImpulseDefinition.ImpulseDuration = 0.25f;
        }

        /// <summary>Landscape: design height, grown only when the screen is too narrow for the design width.</summary>
        static float ViewSize(float aspect) => Mathf.Max(MinViewSize, DesignHalfWidth / Mathf.Max(0.3f, aspect));

        void OnShake(float strength) => impulse.GenerateImpulseWithForce(strength);

        void Update()
        {
            if (GameInput.PausePressed() && Current != State.Won && Current != State.Lost) TogglePause();
            if (Current == State.Launch && GameInput.ActionPressed()) launch.TryFire();
            else if (Current == State.Transition && Time.time > missionShownAt + MissionTapGrace && GameInput.ActionPressed()) ContinueMission();
            // Tutorial levels always hint; later levels hint only once a pup has lapped the arena untapped.
            var p = attack.Current;
            bool aimHint = level.showTutorial && Current == State.Launch && launch.CanFire;
            bool diveHint = Current == State.Attack && p != null && !p.Dived && !p.Landed && (level.showTutorial || p.NeedsNudge);
            hud.ShowHint(aimHint || diveHint);
            hud.SetHintText(aimHint ? Loc.T(level.launcher == LaunchStyle.Bungee ? "hint_aim_bungee" : "hint_aim") : diveHint ? Loc.T("hint_dive") : null);
            var lens = vcam.Lens;
            bool fortressView = focus.position.x > LaunchPhase.ViewCenter.x + 1f;   // zoom starts with the pan, not before it
            float target = ViewSize(Camera.main != null ? Camera.main.aspect : 16f / 9f) * (fortressView ? AttackZoom : 1f);
            if (Mathf.Abs(lens.OrthographicSize - target) > 0.001f)   // settled: skip the lens write (it dirties Cinemachine)
            {
                lens.OrthographicSize = Mathf.Lerp(lens.OrthographicSize, target, 1f - Mathf.Exp(-3f * Time.deltaTime));
                vcam.Lens = lens;
            }
            // The arena camera follows the pup up and down (the composer damps it).
            if (Current == State.Attack)
            {
                var pup = attack.Current;
                Vector2 c = AttackPhase.ViewCenter;
                // A level with a cave lets the camera follow the pup further down, into the chamber.
                float down = FollowDown + (level.HasCave ? CaveFollow : 0f);
                float y = pup != null ? Mathf.Clamp(pup.transform.position.y, c.y - down, c.y + FollowUp) : c.y;
                // Once a pup has dived, the view stays as low as it got while that pup keeps bouncing (reference §16.2);
                // it rises again for the next pup.
                if (pup != heldFor) { heldFor = pup; heldLow = float.MaxValue; }
                if (pup != null && (pup.Dived || pup.Landed)) { heldLow = Mathf.Min(heldLow, y); y = heldLow; }
                focus.position = new Vector3(c.x, y, 0f);
            }
        }

        void OnLaunchDone(int passed)
        {
            if (passed == 0)
            {
                hud.Banner(Loc.T("none_through"));
                StartCoroutine(After(1.4f, () => Finish(false, 0, 0)));
                return;
            }
            StartCoroutine(GoAttack(passed));
        }

        IEnumerator GoAttack(int ammo)
        {
            Current = State.Transition;
            // The water curtain drops over the pit, the view moves to the arena behind it, and it lifts on the mission card
            // (reference §16.1).
            yield return hud.Curtain(true);
            focus.position = AttackPhase.ViewCenter;
            yield return new WaitForSeconds(0.7f);
            StartCoroutine(hud.Curtain(false));
            missionGo = false;
            missionShownAt = Time.time;
            hud.MissionCard(LevelIndex + 1, attack.BearsRequired, SaveSystem.Data.totalBears, ammo, ContinueMission);
            while (!missionGo) yield return null;   // the reference waits for Continue (a tap anywhere also counts)
            hud.DismissMission();
            hud.ShowBears(true);
            yield return new WaitForSeconds(0.4f);
            Current = State.Attack;
            AudioManager.Instance.PlayMusic("music_arena");
            hud.Banner(Loc.T("attack"));
            attack.Begin(ammo, SaveSystem.Data.upgrades, Finish, launch.Exits);   // each pup's exit sets its entry height and speed (user 13/11: keep the link)
        }

        PufferLaunch.Attack.PufferController heldFor;
        float heldLow;
        bool missionGo;
        float missionShownAt;
        const float MissionTapGrace = 0.6f;   // taps still landing from the last launch must not skip the card

        /// <summary>Leaves the mission card for the arena (the card's Continue button, a tap, or a test bot).</summary>
        public void ContinueMission() => missionGo = true;

        void Finish(bool won, int puffersLeft, int score)
        {
            if (ScreenFader.LoadPending) return;   // Retry/Map was tapped: this level is being left, record nothing
            Current = won ? State.Won : State.Lost;
            GameInput.GameplayEnabled = false;
            hud.ShowHint(false);
            if (won)
            {
                AdMob.Policy.RecordWin();
                AdMob.TryConsentAgain();   // a player offline at launch gets the consent check once back online
            }
            int stars = StarRating.Compute(won, puffersLeft, score, level.threeStarScore);
            bool perfect = won && attack.Perfect;
            var data = SaveSystem.Data;
            int previous = data.stars[LevelIndex];
            data.stars[LevelIndex] = Mathf.Max(previous, stars);
            if (perfect) data.perfect[LevelIndex] = 1;
            // Statistics count every attempt, won or lost.
            data.totalBears += attack.BearsDown;
            foreach (int d in attack.Dives) data.AddDive(d);
            SaveSystem.Save();
            AudioManager.Sfx(won ? "win" : "lose", 0f);
            result.Show(LevelIndex + 1, won, perfect, stars, attack.BearsDown, attack.BearsRequired, data.totalBears);
            if (InAppReview.ShouldAsk(data, LevelIndex, won)) StartCoroutine(InAppReview.AskOnce());
        }

        /// <summary>Going to the background mid-level opens the pause menu, so the player comes back to a still game
        /// instead of a pup already flying (QA 12/11 on the emulator: the game ran on after returning).</summary>
        void OnApplicationPause(bool goingAway)
        {
            if (goingAway && !paused && pause != null && (Current == State.Launch || Current == State.Attack || Current == State.Transition)) TogglePause();
        }

        public void TogglePause()
        {
            if (ScreenFader.LoadPending) return;   // Back during the wipe must not unpause the level being left
            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
            GameInput.GameplayEnabled = !paused;
            pause.SetVisible(paused);
        }

        public void Retry() => SceneLoader.PlayLevel(LevelIndex);

        bool leaving;

        /// <summary>Continue on the win card: the only place an interstitial may show (GAME_DESIGN.md §13b).</summary>
        public void NextLevel()
        {
            if (leaving) return;   // a second tap while the ad opens must not load twice
            leaving = true;
            int cleared = 0;
            foreach (int s in SaveSystem.Data.stars) if (s > 0) cleared++;
            bool chapterEnd = IsChapterEnd(level);
            AdMob.InterstitialThen(cleared, chapterEnd, () => SceneLoader.PlayLevel(LevelIndex + 1));
        }

        /// <summary>The bonus (fish) and boss levels close a chapter: no interstitial when leaving them.</summary>
        public static bool IsChapterEnd(LevelData level)
        {
            foreach (var o in level.objects)
                if (o.kind == ObjectKind.Fish || o.kind == ObjectKind.Boss) return true;
            return false;
        }

        /// <summary>Lose card ▶: a rewarded ad buys one retry of this level with an extra pup.</summary>
        public void RetryWithBonusPup(Action noAd)
        {
            if (leaving) return;
            leaving = true;
            AdMob.ShowRewarded(earned =>
            {
                leaving = false;
                if (!earned)
                {
                    noAd();
                    return;
                }
                AdMob.Policy.GrantBonusPup(LevelIndex);
                Retry();
            });
        }

        public void ToMap() => SceneLoader.Load(SceneLoader.LevelMap);

        static IEnumerator After(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }
    }
}

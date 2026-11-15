using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PufferLaunch.Attack;
using PufferLaunch.Core;
using PufferLaunch.Progress;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PufferLaunch.Tests
{
    /// <summary>Loads the real Game scene: fortress stability, dive damage, and screenshots for review.</summary>
    public class GameplayTests
    {
        const float SettleSeconds = 3f;
        const float MaxRestDrift = 0.15f;

        // The ad policy is static: wins counted by one test must not decide whether another shows an interstitial.
        [SetUp]
        public void ResetAdPolicy() => PufferLaunch.Ads.AdMob.Policy.Reset();

        [Test]
        public void OnlyBonusAndBossLevelsCloseTheChapter()
        {
            var levels = GameDatabase.Instance.levels;
            for (int i = 0; i < levels.Length; i++)
                Assert.AreEqual(i >= levels.Length - 2, GameManager.IsChapterEnd(levels[i]), $"level {i + 1}");
        }

        /// <summary>Loads a level and waits for the NEW scene (the fade keeps the old one alive for a moment).</summary>
        static IEnumerator LoadLevel(int index)
        {
            int before = SceneManager.GetActiveScene().handle;
            SceneLoader.PlayLevel(index);
            yield return new WaitUntil(() => SceneManager.GetActiveScene().handle != before
                                             && SceneManager.GetActiveScene().name == SceneLoader.Game);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryFortressStandsStillBeforeTheAttack()
        {
            var levels = GameDatabase.Instance.levels;
            for (int i = 0; i < levels.Length; i++)
            {
                yield return LoadLevel(i);
                var blocks = Object.FindObjectsByType<Block>(FindObjectsSortMode.None);
                var start = blocks.Select(b => (Vector2)b.transform.position).ToArray();
                var bears = Object.FindObjectsByType<Bear>(FindObjectsSortMode.None);
                var bearStart = bears.Select(b => (Vector2)b.transform.position).ToArray();
                // Bears start asleep; wake them as a first nudge would, so a wedged or overhanging bear shows itself.
                foreach (var bear in bears) bear.GetComponent<Rigidbody2D>().WakeUp();
                // TNT too: a crate placed inside terrain sleeps there unseen until physics throws it out.
                var crates = Object.FindObjectsByType<Tnt>(FindObjectsSortMode.None);
                var crateStart = crates.Select(c => (Vector2)c.transform.position).ToArray();
                foreach (var crate in crates) crate.GetComponent<Rigidbody2D>().WakeUp();
                yield return new WaitForSeconds(SettleSeconds);
                for (int c = 0; c < crates.Length; c++)
                {
                    Assert.IsTrue(crates[c] != null, $"level {i + 1}: TNT at {crateStart[c]} blew up at rest");
                    float drift = Vector2.Distance(crateStart[c], crates[c].transform.position);
                    Assert.Less(drift, MaxRestDrift, $"level {i + 1}: TNT at {crateStart[c]} moved {drift:0.00} at rest");
                }
                Assert.AreEqual(levels[i].blocks.Count, blocks.Length, $"level {i + 1}: block count");
                Assert.AreEqual(levels[i].bears.Count, bears.Length, $"level {i + 1}: bear count");
                for (int b = 0; b < blocks.Length; b++)
                {
                    float drift = Vector2.Distance(start[b], blocks[b].transform.position);
                    Assert.Less(drift, MaxRestDrift, $"level {i + 1}: {blocks[b].name} slid {drift:0.00} at rest");
                }
                for (int b = 0; b < bears.Length; b++)
                {
                    Assert.IsFalse(bears[b].IsDown, $"level {i + 1}: bear at {bearStart[b]} fell before the attack");
                    float drift = Vector2.Distance(bearStart[b], bears[b].transform.position);
                    Assert.Less(drift, MaxRestDrift, $"level {i + 1}: bear at {bearStart[b]} slid {drift:0.00} at rest");
                }
            }
        }

        /// <summary>The lose card's "Attack again": the level starts in the arena with the kept pups, no launch pit.</summary>
        [UnityTest]
        public IEnumerator AttackOnlyRetryStartsInTheArenaWithTheKeptPups()
        {
            int before = SceneManager.GetActiveScene().handle;
            SceneLoader.RetryAttack(0, new[] { new Vector2(6f, 4f), new Vector2(6f, 4f) });
            yield return new WaitUntil(() => SceneManager.GetActiveScene().handle != before
                                             && SceneManager.GetActiveScene().name == SceneLoader.Game);
            var game = Object.FindFirstObjectByType<GameManager>();
            float deadline = Time.realtimeSinceStartup + 10f;
            while (game.Current != GameManager.State.Attack && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(GameManager.State.Attack, game.Current, "the attack starts by itself");
            Assert.IsNull(Object.FindFirstObjectByType<PufferLaunch.Launch.LaunchPhase>(), "no launch pit on an attack-only retry");
            var attack = Object.FindFirstObjectByType<AttackPhase>();
            Assert.AreEqual(1, attack.Ammo, "two kept pups: one flying, one waiting");
            Assert.IsNotNull(attack.Current);
            // The kept exit decides how the pup flies in (not the default entry).
            var entry = AttackPhase.Entry(new Vector2(6f, 4f));
            Assert.AreNotEqual(AttackPhase.SpawnPoint.y, entry.at.y, "the test exit must differ from the default entry");
            Assert.AreEqual(entry.at.y, attack.Current.transform.position.y, 0.3f, "the pup enters at its kept exit's height");
            // The next ordinary start of the level is a full one again.
            yield return LoadLevel(0);
            Assert.IsNotNull(Object.FindFirstObjectByType<PufferLaunch.Launch.LaunchPhase>(), "PlayLevel forgets the kept pups");
        }

        [UnityTest]
        public IEnumerator DivingPufferSmashesACubeTower()
        {
            yield return LoadLevel(7);   // level 8: free-standing cube columns
            yield return new WaitForSeconds(1f);
            int bearsBefore = Object.FindObjectsByType<Bear>(FindObjectsSortMode.None).Length;
            int broken = 0;
            int bearsDown = 0;
            void CountBlock(Block b, int s) => broken++;
            void CountBear(Bear c, int s) => bearsDown++;
            Block.Broken += CountBlock;
            Bear.Defeated += CountBear;
            Block.Armed = true;
            var tuning = GameDatabase.Instance.tuning;
            // A cube column with no bear perched on it, so the dive meets ice first; the steep dive starts just up-left of
            // it (further out it would clip the helmeted bear on the pillar next door).
            var bears = Object.FindObjectsByType<Bear>(FindObjectsSortMode.None);
            var roof = Object.FindObjectsByType<Block>(FindObjectsSortMode.None)
                .Where(b => !bears.Any(c => Mathf.Abs(c.transform.position.x - b.transform.position.x) < 0.7f && c.transform.position.y > b.transform.position.y))
                .OrderByDescending(b => b.transform.position.y).First();
            var puffer = PufferController.Spawn((Vector2)roof.transform.position + new Vector2(-0.75f, 0.8f), tuning, new int[SaveData.UpgradeCount], Vector2.zero);
            puffer.Dive();
            yield return new WaitForSeconds(3f);
            Block.Broken -= CountBlock;
            Bear.Defeated -= CountBear;
            Debug.Log($"[PufferLaunch] dive test: {broken} blocks broken, {bearsDown}/{bearsBefore} bears down");
            Assert.Greater(broken, 0, "a dive should break at least one ice cube");
        }

        /// <summary>Balance sweep: a good bot (times the swing, dives just before the nearest standing bear) plays every
        /// level once; logs escapes, bears down and the result per level and captures each arena. Not an assertion on
        /// balance: it reports, so level tuning has numbers (user 13/11: "chơi các màn để tối ưu").</summary>
        [UnityTest, Timeout(1800000)]
        public IEnumerator BotPlaysEveryLevel()
        {
            var report = new System.Text.StringBuilder();
            int count = GameDatabase.Instance.levels.Length;
            for (int i = 0; i < count; i++)
            {
                yield return LoadLevel(i);
                var game = Object.FindFirstObjectByType<GameManager>();
                var launch = Object.FindFirstObjectByType<PufferLaunch.Launch.LaunchPhase>();
                var attack = Object.FindFirstObjectByType<AttackPhase>();
                float t = 0f, clock = 0f;
                int dives = 0;
                bool shot = false;
                while (t < 150f && game.Current != GameManager.State.Won && game.Current != GameManager.State.Lost)
                {
                    if (game.Current == GameManager.State.Transition) game.ContinueMission();
                    if (game.Current == GameManager.State.Launch)
                    {
                        clock += Time.deltaTime;
                        if (launch.CanFire && clock > 0.3f && launch.ApproachingBottom && launch.TryFire()) clock = 0f;
                    }
                    var p = attack.Current;
                    if (p != null && !p.Dived && !p.Landed)
                    {
                        if (!shot) { yield return new WaitForSeconds(0.2f); Capture($"sweep_level{i + 1:00}"); shot = true; }
                        var bear = Object.FindObjectsByType<Bear>(FindObjectsSortMode.None).Where(b => !b.IsDown)
                            .OrderBy(b => b.transform.position.x).FirstOrDefault();
                        if (bear != null && p.transform.position.x > bear.transform.position.x - 0.9f * (p.transform.position.y - bear.transform.position.y))
                        {
                            p.Dive();
                            dives++;
                        }
                    }
                    t += Time.deltaTime;
                    yield return null;
                }
                report.Append($" L{i + 1}:{game.Current} out{launch.Passed} down{attack.BearsDown}/{attack.BearsRequired} dives{dives} {t:0}s |");
            }
            Debug.Log("[PufferLaunch] sweep:" + report);
        }

        [UnityTest]
        public IEnumerator FullLevelReachesAResult()
        {
            yield return LoadLevel(0);
            var game = Object.FindFirstObjectByType<GameManager>();
            var launch = Object.FindFirstObjectByType<PufferLaunch.Launch.LaunchPhase>();
            var attack = Object.FindFirstObjectByType<AttackPhase>();
            const float Timeout = 150f;
            float t = 0f;
            int swings = 0;
            bool shotAim = false, shotFly = false, shotAttack = false;
            float swingClock = 0f;
            while (t < Timeout && game.Current != GameManager.State.Won && game.Current != GameManager.State.Lost)
            {
                if (game.Current == GameManager.State.Transition) game.ContinueMission();
                if (game.Current == GameManager.State.Launch)
                {
                    if (!shotAim && launch.CanFire && t > 3f) { CaptureWithUi("flow_aim"); shotAim = true; }
                    // Bot: tap as the pup swings back toward the bottom (the gap only takes ~44°–52° launches).
                    swingClock += Time.deltaTime;
                    if (launch.CanFire && swingClock > 0.3f && launch.ApproachingBottom)
                    {
                        if (launch.TryFire()) { swings++; swingClock = 0f; }
                    }
                    if (!shotFly && swings > 0 && !launch.CanFire && t > 4f) { yield return new WaitForSeconds(0.35f); CaptureWithUi("flow_fly"); shotFly = true; }
                }
                var p = attack.Current;
                if (p != null)
                {
                    var bear = Object.FindObjectsByType<Bear>(FindObjectsSortMode.None).OrderBy(c => c.transform.position.x).FirstOrDefault();
                    // The dive is diagonal (~45°, gravity bends it a little): lead the target by about 0.9× the height above it.
                    if (bear != null && !p.Dived && !p.Landed
                        && p.transform.position.x > bear.transform.position.x - 0.9f * (p.transform.position.y - bear.transform.position.y))
                    {
                        p.Dive();
                        if (!shotAttack) { yield return new WaitForSeconds(0.3f); CaptureWithUi("flow_attack"); shotAttack = true; }
                    }
                }
                t += Time.deltaTime;
                yield return null;
            }
            Debug.Log($"[PufferLaunch] flow test: {game.Current} after {t:0.0}s, swings {swings}, escaped {launch.Passed}, score {attack.Score}");
            Assert.That(game.Current == GameManager.State.Won || game.Current == GameManager.State.Lost, "level never finished");
            yield return new WaitForSeconds(1.5f);
            CaptureWithUi("flow_result");
        }

        /// <summary>Regression (QA 12/11 on the emulator): at a low frame rate one frame jumped the whole catapult
        /// sweep past its release angle, the snowball never left and the machine waited for it forever.</summary>
        [UnityTest]
        public IEnumerator CatapultStillFiresAtFourFps()
        {
            yield return LoadLevel(0);
            var launch = Object.FindFirstObjectByType<PufferLaunch.Launch.LaunchPhase>();
            float wait = 0f;
            while (!launch.CanFire && wait < 10f) { wait += Time.deltaTime; yield return null; }
            Time.captureFramerate = 4;   // every frame advances 0.25 s, longer than the 0.24 s sweep
            try
            {
                int before = launch.Waiting;
                Assert.IsTrue(launch.TryFire(), "ready to fire");
                bool readyAgain = false;
                for (int frame = 0; frame < 60 && !readyAgain; frame++)
                {
                    yield return null;
                    readyAgain = launch.CanFire || launch.Waiting < before;   // re-armed after a miss, or the pup was hit
                }
                Assert.IsTrue(readyAgain, "the catapult finishes its shot and can fire again");
            }
            finally
            {
                Time.captureFramerate = 0;
            }
        }

        /// <summary>Level 5 hangs the pup on a bungee: it bobs straight up and down over the catapult and the
        /// snowball can reach it.</summary>
        [UnityTest]
        public IEnumerator BungeePitBobsAndCanBeHit()
        {
            yield return LoadLevel(4);
            var launch = Object.FindFirstObjectByType<PufferLaunch.Launch.LaunchPhase>();
            int hits = 0, misses = 0;
            float minY = float.MaxValue, maxY = float.MinValue, minX = float.MaxValue, maxX = float.MinValue;
            for (int shot = 0; shot < 6 && launch.Waiting > 0; shot++)
            {
                float wait = 0f;
                while (!launch.CanFire && wait < 10f) { wait += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(0.5f);   // the rope settles straight down after the grab
                for (float t = 0f; t < 0.4f + shot * 0.23f; t += Time.deltaTime)
                {
                    Vector2 p = launch.HangingPosition;
                    minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                    minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                    yield return null;
                }
                int before = launch.Waiting;
                if (shot == 1) CaptureWithUi("bungee_aim");
                launch.TryFire();
                float w2 = 0f;
                while (!launch.CanFire && w2 < 8f && launch.Waiting > 0) { w2 += Time.deltaTime; yield return null; }
                if (launch.Waiting < before) hits++; else misses++;
            }
            Debug.Log($"[PufferLaunch] bungee: bob {minY:0.00}..{maxY:0.00}, x {minX:0.00}..{maxX:0.00}, hits {hits}, misses {misses}, escaped {launch.Passed}");
            Assert.Greater(maxY - minY, 0.5f, "the pup bobs up and down");
            Assert.Less(maxX - minX, 0.4f, "it does not swing sideways");
            Assert.Greater(hits, 0, "some shots connect");
            Assert.Greater(launch.Passed, 0, "a bungee pup can escape through the gap (regression: all stuck in the icicles)");
        }

        /// <summary>Level 6 has a cave (report §11): a pup dropped through the floor gap falls into the chamber.
        /// (The camera following the level's own pup down is checked on the emulator.)</summary>
        [UnityTest]
        public IEnumerator CaveLevelDropsThePupIntoTheChamber()
        {
            yield return LoadLevel(5);
            var level = GameDatabase.Instance.levels[5];
            Assert.IsTrue(level.HasCave);
            var cave = GameObject.Find("Cave").GetComponent<SpriteRenderer>();
            Assert.Greater(cave.bounds.size.x, level.caveTo - level.caveFrom - 0.1f, "the dark back wall fills the whole chamber");
            var game = Object.FindFirstObjectByType<GameManager>();
            float wait = 0f;
            while (game.Current != GameManager.State.Attack && wait < 40f)
            {
                var launch = Object.FindFirstObjectByType<PufferLaunch.Launch.LaunchPhase>();
                if (launch != null && launch.CanFire && launch.ApproachingBottom) launch.TryFire();
                if (game.Current == GameManager.State.Transition) game.ContinueMission();
                wait += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(GameManager.State.Attack, game.Current, "reached the arena");
            Block.Armed = true;
            // The dive is steep: start 3 up and 1.5 left of a point inside the gap (clear of the ramp on the left).
            Vector2 gap = AttackPhase.Origin + new Vector2(level.gapTo - 0.4f - 2.8f, 3f);
            var pup = PufferController.Spawn(gap, GameDatabase.Instance.tuning, new int[5], Vector2.zero);
            pup.Dive();
            float lowest = float.MaxValue;
            for (float t = 0f; t < 2.5f && pup != null; t += Time.deltaTime)
            {
                lowest = Mathf.Min(lowest, pup.transform.position.y);
                yield return null;
            }
            yield return new WaitForSeconds(0.3f);
            Capture("cave_level6");
            Debug.Log($"[PufferLaunch] cave: pup lowest {lowest - AttackPhase.Origin.y:0.00} from the floor, camera y {Camera.main.transform.position.y - AttackPhase.Origin.y:0.00}");
            Assert.Less(lowest, AttackPhase.Origin.y - 1f, "fell through the gap into the cave");
        }

        /// <summary>A knocked-out bear is flung up spinning before it lands (report §11).</summary>
        [UnityTest]
        public IEnumerator KnockedOutBearSpinsUp()
        {
            yield return LoadLevel(0);
            var bear = Object.FindFirstObjectByType<Bear>();
            Block.Armed = true;
            float y0 = bear.transform.position.y;
            bear.Hit();
            float top = y0, low = y0, maxAngle = 0f;
            var body = bear.GetComponentInChildren<SpriteRenderer>().transform;
            for (float t = 0f; t < 1.2f && bear != null; t += Time.deltaTime)
            {
                top = Mathf.Max(top, bear.transform.position.y);
                low = Mathf.Min(low, bear.transform.position.y);
                float a = Mathf.Abs(Mathf.DeltaAngle(0f, body.localEulerAngles.z));
                maxAngle = Mathf.Max(maxAngle, a);
                if (t > 0.2f && t < 0.25f) Capture("bear_spin");
                yield return null;
            }
            Assert.Greater(top - y0, 0.2f, "a small hop");
            Assert.Greater(maxAngle, 90f, "tumbles");
            Assert.Less(low, y0 - 1.5f, "then drops through the floor, out of the arena");
        }

        /// <summary>HUD music/sound buttons show an off slash, and music comes back at the level the player had set.</summary>
        [UnityTest]
        public IEnumerator AudioButtonsShowOffStateAndRestoreMusic()
        {
            SaveSystem.EnsureLoaded();
            var d = SaveSystem.Data;
            float keepMusic = d.musicVolume;
            bool keepMuted = d.muted;
            d.musicVolume = 0.35f;
            d.muted = false;
            yield return LoadLevel(0);
            var music = GameObject.Find("Button glyph_music").GetComponent<UnityEngine.UI.Button>();
            var sound = GameObject.Find("Button glyph_sound").GetComponent<UnityEngine.UI.Button>();
            var musicMark = music.transform.Find("ui_bar").GetComponent<UnityEngine.UI.Image>();
            var soundMark = sound.transform.Find("ui_bar").GetComponent<UnityEngine.UI.Image>();
            Assert.IsFalse(musicMark.enabled || soundMark.enabled, "both on at start");
            music.onClick.Invoke();
            sound.onClick.Invoke();
            Assert.IsTrue(musicMark.enabled && soundMark.enabled, "off slash shown");
            Assert.AreEqual(0f, d.musicVolume);
            yield return null;
            CaptureWithUi("hud_audio_off");
            music.onClick.Invoke();
            sound.onClick.Invoke();
            Assert.AreEqual(0.35f, d.musicVolume, 1e-4f, "music back at the player's level, not a fixed default");
            Assert.IsFalse(musicMark.enabled || soundMark.enabled);
            d.musicVolume = keepMusic;
            d.muted = keepMuted;
            SaveSystem.Save();
        }

        /// <summary>Going to the background mid-level pauses the game (and coming back leaves it paused).</summary>
        [UnityTest]
        public IEnumerator BackgroundingPausesTheLevel()
        {
            yield return LoadLevel(0);
            var game = Object.FindFirstObjectByType<GameManager>();
            game.SendMessage("OnApplicationPause", true);
            yield return null;
            Assert.AreEqual(0f, Time.timeScale, "paused while away");
            game.SendMessage("OnApplicationPause", false);
            yield return null;
            Assert.AreEqual(0f, Time.timeScale, "still paused on return: the player resumes");
            game.TogglePause();
            Assert.AreEqual(1f, Time.timeScale);
        }

        /// <summary>A dive that knocks a one-hit bear out still bounces the pup up, whichever collision callback runs first.</summary>
        [UnityTest]
        public IEnumerator DivingPupBouncesOffTheBearItKnocksOut()
        {
            yield return LoadLevel(0);
            yield return new WaitForSeconds(1f);
            var bear = Object.FindObjectsByType<Bear>(FindObjectsSortMode.None).First(b => b.name == "Bear");   // no helmet: one hit
            Block.Armed = true;
            var pup = PufferController.Spawn((Vector2)bear.transform.position + new Vector2(0f, 3f), GameDatabase.Instance.tuning, new int[5], Vector2.zero);
            pup.Dive();
            var rb = pup.GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.down * 8f;
            for (float t = 0f; t < 2f && !bear.IsDown; t += Time.deltaTime) yield return null;
            Assert.IsTrue(bear.IsDown, "the dive knocks the bear out");
            yield return new WaitForFixedUpdate();
            Assert.Greater(rb.linearVelocity.y, 0f, "the pup bounces up off the bear it knocked out");
        }

        /// <summary>Snow is softer than ice: a piercing dive goes through it instead of stopping.</summary>
        [UnityTest]
        public IEnumerator PiercingDiveGoesThroughSnow()
        {
            yield return LoadLevel(0);
            var tuning = GameDatabase.Instance.tuning;
            var spec = new BlockSpec { material = BlockMaterial.Snow, position = new Vector2(6.5f, 7f), size = new Vector2(0.8f, 0.8f) };
            var block = Block.Create(spec, AttackPhase.Origin, tuning, null);
            var upgrades = new int[5];
            upgrades[(int)UpgradeId.Spiky] = 1;
            var pup = PufferController.Spawn((Vector2)block.transform.position + Vector2.up * 1.5f, tuning, upgrades, Vector2.zero);
            pup.Dive();
            pup.GetComponent<Rigidbody2D>().linearVelocity = Vector2.down * 8f;
            for (float t = 0f; t < 1f && block != null; t += Time.deltaTime) yield return null;
            Assert.IsTrue(block == null, "the snow block breaks");
            Assert.IsFalse(pup.Landed, "the pup flies on through, like through ice");
            Object.Destroy(pup.gameObject);
        }

        /// <summary>Bonus level: no bears, so Perfect needs every fish (it used to be Perfect on any win).</summary>
        [UnityTest]
        public IEnumerator BonusLevelIsNotPerfectWithoutTheFish()
        {
            yield return LoadLevel(10);
            var attack = Object.FindFirstObjectByType<AttackPhase>();
            Assert.Greater(attack.FishTotal, 0, "the bonus level has fish");
            Assert.AreEqual(0, attack.BearsLeft);
            Assert.IsFalse(attack.Perfect, "no fish collected yet: not perfect");
        }

        /// <summary>Ninja: after the mid-air hang the pup kicks diagonally at the nearest standing bear below.</summary>
        [UnityTest]
        public IEnumerator NinjaKicksTowardTheNearestBear()
        {
            yield return LoadLevel(0);
            var bear = Object.FindFirstObjectByType<Bear>();
            Block.Armed = true;
            var upgrades = new int[5];
            upgrades[(int)UpgradeId.Ninja] = 1;
            Vector2 start = (Vector2)bear.transform.position + new Vector2(-3f, 4f);
            var pup = PufferController.Spawn(start, GameDatabase.Instance.tuning, upgrades, Vector2.zero);
            pup.Dive();
            yield return new WaitForSeconds(0.1f);
            pup.SecondTap();
            yield return new WaitForSeconds(GameDatabase.Instance.tuning.ninjaHang + 0.05f);
            var v = pup.GetComponent<Rigidbody2D>().linearVelocity;
            Debug.Log($"[PufferLaunch] ninja kick velocity {v}");
            Assert.Greater(v.x, 1f, "kicks sideways toward the bear (to the right)");
            Assert.Less(v.y, -1f, "and down");
            bool knocked = false;
            for (float t = 0f; t < 3f && !knocked; t += Time.deltaTime) { knocked = bear.IsDown; yield return null; }
            Assert.IsTrue(knocked, "the kick knocks the bear out");
        }

        /// <summary>The boss falls to direct hits (2 lives, no life bar); a button only throws it off its perch.</summary>
        [UnityTest]
        public IEnumerator BossIsBeatenByDirectHitsAndTheButtonOnlyTopplesIt()
        {
            yield return LoadLevel(11);
            var boss = Object.FindFirstObjectByType<BossBear>();
            var button = Object.FindFirstObjectByType<FloorButton>();
            Block.Armed = true;
            int start = boss.Lives;
            float perchY = boss.transform.position.y;
            button.Press();
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(start, boss.Lives, "the button costs no life");
            Assert.Less(boss.transform.position.y, perchY - 1f, "the button throws the boss off its perch");
            for (int i = 0; i < start; i++) { boss.Hit(); yield return new WaitForSeconds(0.6f); }   // past the per-dive hit cooldown
            Assert.AreEqual(0, boss.Lives, "out cold after its lives are gone");
        }

        /// <summary>Starts a level in the arena with <paramref name="pups"/> kept pups and waits for the attack.</summary>
        static IEnumerator StartAttackOnly(int level, int pups)
        {
            int before = SceneManager.GetActiveScene().handle;
            var exits = new Vector2[pups];
            for (int i = 0; i < pups; i++) exits[i] = new Vector2(6f, 4f);
            SceneLoader.RetryAttack(level, exits);
            yield return new WaitUntil(() => SceneManager.GetActiveScene().handle != before
                                             && SceneManager.GetActiveScene().name == SceneLoader.Game);
            var game = Object.FindFirstObjectByType<GameManager>();
            float deadline = Time.realtimeSinceStartup + 10f;
            while (game.Current != GameManager.State.Attack && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(GameManager.State.Attack, game.Current);
        }

        static IEnumerator WaitForState(GameManager game, GameManager.State state, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (game.Current != state && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(state, game.Current);
        }

        /// <summary>Beating the boss in the arena wins the last level and shows the closing card (captured for review).</summary>
        [UnityTest]
        public IEnumerator BeatingTheBossShowsTheEndingCard()
        {
            int last = GameDatabase.Instance.levels.Length - 1;
            yield return StartAttackOnly(last, 3);
            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsFalse(game.HasNextLevel);
            var boss = Object.FindFirstObjectByType<BossBear>();
            for (int n = 0; boss.Lives > 0 && n < 10; n++) { boss.Hit(); yield return new WaitForSeconds(0.6f); }   // past the per-dive hit cooldown
            Assert.AreEqual(0, boss.Lives);
            yield return WaitForState(game, GameManager.State.Won, 15f);
            Assert.AreEqual(1f, Time.timeScale, 0.001f, "the last blow's slow motion has ended");
            yield return new WaitForSecondsRealtime(1.6f);   // the card pops in and its stars land
            var texts = Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None).Select(t => t.text).ToArray();
            CollectionAssert.Contains(texts, Loc.T("ending"), "the last level's win card closes the story");
            CaptureWithUi("result_boss_win");
        }

        /// <summary>A lost attack offers both retries: "Attack again" keeps the pups, plain Retry replays the launch pit.</summary>
        [UnityTest]
        public IEnumerator LostAttackOffersAttackAgainAndAFullRetry()
        {
            yield return StartAttackOnly(2, 1);
            var game = Object.FindFirstObjectByType<GameManager>();
            Object.FindFirstObjectByType<AttackPhase>().Current.Dive();   // one pup, dived at once: it lands on the cliff
            yield return WaitForState(game, GameManager.State.Lost, 25f);
            Assert.IsTrue(game.CanRetryAttack);
            yield return new WaitForSecondsRealtime(1.2f);
            var texts = Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None).Select(t => t.text).ToArray();
            CollectionAssert.Contains(texts, Loc.T("retry_attack"));
            CollectionAssert.Contains(texts, Loc.T("retry"));
            CaptureWithUi("result_lose_retry");
            int before = SceneManager.GetActiveScene().handle;
            game.Retry();
            yield return new WaitUntil(() => SceneManager.GetActiveScene().handle != before
                                             && SceneManager.GetActiveScene().name == SceneLoader.Game);
            yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<PufferLaunch.Launch.LaunchPhase>(), "plain Retry goes back to the launch pit");
        }

        /// <summary>Boss level: the king's angry steam, then the cage bursting open (captured for review).</summary>
        [UnityTest]
        public IEnumerator BossCageBursts()
        {
            yield return LoadLevel(11);
            yield return new WaitForSeconds(1f);
            var boss = Object.FindFirstObjectByType<BossBear>();
            var cage = Object.FindFirstObjectByType<Cage>();
            Assert.IsNotNull(boss);
            Assert.IsNotNull(cage);
            var cam = Camera.main.transform;
            Vector3 keep = cam.position;
            cam.position = new Vector3(AttackPhase.ViewCenter.x, AttackPhase.ViewCenter.y, keep.z);
            var brain = Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            if (brain != null) brain.enabled = false;   // hold the camera on the arena for the shots
            yield return new WaitForSeconds(0.8f);
            Capture("boss_steam");
            cage.StartCoroutine(cage.Open());
            yield return new WaitForSeconds(0.55f);
            Capture("cage_burst");
            yield return new WaitForSeconds(1.2f);
            Capture("cage_freed");
            if (brain != null) brain.enabled = true;
        }

        /// <summary>Different moments of the swing must give different flights (some escape, some don't).</summary>
        [UnityTest]
        public IEnumerator SwingTimingChangesTheTrajectory()
        {
            SaveSystem.EnsureLoaded();
            int school = SaveSystem.Data.upgrades[(int)UpgradeId.School];
            SaveSystem.Data.upgrades[(int)UpgradeId.School] = 3;   // more pups to sample with (restored below)
            yield return LoadLevel(9);
            SaveSystem.Data.upgrades[(int)UpgradeId.School] = school;
            var launch = Object.FindFirstObjectByType<PufferLaunch.Launch.LaunchPhase>();
            var results = new System.Collections.Generic.List<string>();
            for (int shot = 0; shot < 16; shot++)
            {
                float wait = 0f;
                while (!launch.CanFire && wait < 10f) { wait += Time.deltaTime; yield return null; }
                // Let the swing build up, then fire at a shot-dependent moment.
                yield return new WaitForSeconds(1.2f + shot * 0.07f);
                int before = launch.Passed;
                Vector2 at = launch.HangingPosition;
                Vector2 v = launch.SwingVelocity;
                launch.TryFire();
                float t = 0f;
                while (!launch.CanFire && t < 9f && launch.Waiting > 0) { t += Time.deltaTime; yield return null; }
                var lv = launch.LastLaunch;
                results.Add($"x{at.x:0.0}/v{v.x:0.0}->{Mathf.Atan2(lv.y, lv.x) * Mathf.Rad2Deg:0}°@{lv.magnitude:0.0}:{(launch.Passed > before ? "OUT" : "in")}");
            }
            Debug.Log($"[PufferLaunch] swing shots: {string.Join("  ", results)}");
            Assert.That(results.Any(r => r.EndsWith("OUT")), "no swing timing escaped the pit");
            Assert.That(results.Any(r => r.EndsWith("in")), "every timing escaped: the gap is too easy");
        }

        /// <summary>Films every pup pose (8 frames each) into Logs/Rig for review.</summary>
        [UnityTest]
        public IEnumerator FilmPupPoses()
        {
            SaveSystem.EnsureLoaded();
            var camGo = new GameObject("RigCam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 1.0f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x2a, 0x55, 0x96, 0xff);
            var rig = PupRig.Create(null, Vector2.zero, 10);
            Directory.CreateDirectory("Logs/Rig");
            var poses = (PupRig.Pose[])System.Enum.GetValues(typeof(PupRig.Pose));
            foreach (var pose in poses)
            {
                rig.transform.position = Vector3.zero;
                rig.transform.rotation = Quaternion.identity;
                rig.SetPose(pose);
                for (int f = 0; f < 8; f++)
                {
                    for (float t = 0f; t < 0.09f; t += Time.deltaTime)
                    {
                        float time = Time.time;
                        if (pose == PupRig.Pose.Walk || pose == PupRig.Pose.Ball) rig.transform.position += Vector3.right * 1.3f * Time.deltaTime;
                        if (pose == PupRig.Pose.Hang)
                        {
                            float ang = Mathf.Sin(time * 2.6f) * 0.6f;
                            rig.SwingSpeed = Mathf.Cos(time * 2.6f) * 2.6f * 0.6f;
                            rig.transform.rotation = Quaternion.Euler(0, 0, ang * Mathf.Rad2Deg);
                        }
                        if (pose == PupRig.Pose.Flail) rig.transform.Rotate(0, 0, -200f * Time.deltaTime);
                        yield return null;
                    }
                    cam.transform.position = new Vector3(rig.transform.position.x, 0.05f, -10f);
                    var rt = new RenderTexture(320, 320, 24);
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(320, 320, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 320, 320), 0, 0);
                    tex.Apply();
                    File.WriteAllBytes($"Logs/Rig/{pose}_{f}.png", tex.EncodeToPNG());
                    cam.targetTexture = null;
                    RenderTexture.active = null;
                    Object.Destroy(rt);
                    Object.Destroy(tex);
                }
            }
            Object.Destroy(rig.gameObject);
            Object.Destroy(camGo);
        }

        /// <summary>Overlay canvases are not drawn by Camera.Render, so render them through the camera for the shot.</summary>
        static void CaptureWithUi(string name)
        {
            var cam = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.enabled).ToArray();
            foreach (var c in canvases)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1f + (100 - c.sortingOrder) * 0.01f;
                c.sortingOrder += 100;   // in camera mode the canvas sorts against sprites; overlays are always on top in the game
            }
            try
            {
                Capture(name);
            }
            finally
            {
                foreach (var c in canvases)
                {
                    c.sortingOrder -= 100;
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                }
            }
        }

        [UnityTest]
        public IEnumerator CaptureMenus()
        {
            // The opening story plays over the menu on a first run: capture its first painting, then the menu itself.
            SaveSystem.EnsureLoaded();
            bool seen = SaveSystem.Data.seenIntro;
            SaveSystem.Data.seenIntro = false;
            int h = SceneManager.GetActiveScene().handle;
            SceneLoader.Load(SceneLoader.MainMenu);
            yield return new WaitUntil(() => SceneManager.GetActiveScene().handle != h);
            yield return new WaitForSeconds(1.5f);
            CaptureWithUi("menu_story");
            var intro = Object.FindFirstObjectByType<PufferLaunch.UI.IntroView>();
            Assert.IsNotNull(intro, "the story plays on a first run");
            yield return new WaitForSeconds(25f);   // five scenes play through and the story closes itself
            Assert.IsTrue(intro == null, "the story ends on its own");
            Assert.IsTrue(SaveSystem.Data.seenIntro, "a finished story is not shown again");
            SaveSystem.Data.seenIntro = seen;
            CaptureWithUi("menu_main");
            var info = Object.FindFirstObjectByType<PufferLaunch.UI.InfoView>();
            info.ShowHowTo();
            yield return new WaitForSeconds(0.8f);
            CaptureWithUi("menu_howto");
            info.ShowAbout();
            yield return new WaitForSeconds(0.8f);
            CaptureWithUi("menu_about");
            h = SceneManager.GetActiveScene().handle;
            SceneLoader.Load(SceneLoader.LevelMap);
            yield return new WaitUntil(() => SceneManager.GetActiveScene().handle != h);
            yield return new WaitForSeconds(1.2f);
            CaptureWithUi("menu_map");
        }

        static void Capture(string name)
        {
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24);  // landscape
            cam.targetTexture = rt;
            cam.aspect = 16f / 9f;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Directory.CreateDirectory("Logs/Shots");
            File.WriteAllBytes($"Logs/Shots/{name}.png", tex.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(tex);
        }
    }
}

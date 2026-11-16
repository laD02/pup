using System;
using SealCatapult.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SealCatapult.UI
{
    /// <summary>
    /// In-game HUD laid out like the reference (report §15): hero portrait + "× pups" top-left, four round buttons
    /// (sound, music, help, pause) top-centre, bear portrait + "Total" + "down/required" top-right (arena only).
    /// No score on screen: the score is shown on the result panel. Also the mission card, phase banner and tap hint.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        const float Portrait = 120f;

        TextMeshProUGUI puffers;
        TextMeshProUGUI bears;
        TextMeshProUGUI totalBears;
        TextMeshProUGUI banner;
        RectTransform hintBar;
        TextMeshProUGUI hintText;
        RectTransform hint;
        RectTransform pupBadge;
        RectTransform bearBadge;
        RectTransform mission;
        float bannerTime = 99f;
        const float BannerEnd = 2.1f;   // the banner has faded out by 2.0 s
        int score;

        public int Score => score;

        public static HudView Create(GameManager game)
        {
            var canvas = UiKit.CreateCanvas("HUD", 10);
            var hud = canvas.gameObject.AddComponent<HudView>();
            var root = UiKit.SafeRoot(canvas);

            // Left: the hero's face and how many pups are left to launch / fly.
            hud.pupBadge = Badge(root, Art.Has("hud_pup") ? "hud_pup" : "pup", UiKit.TopLeft, new Vector2(24, -20));
            hud.puffers = UiKit.Text(root, "× 0", 68, UiKit.TopLeft, new Vector2(24 + Portrait + 14, -30), new Vector2(220, 100), Color.white,
                TextAlignmentOptions.Left, true);

            // Centre: sound, music, help, pause.
            // One MinHit apart, so the padded touch areas do not overlap (at 110 a miss next to Pause fired a pup).
            float pitch = UiKit.MinHit;
            var cluster = UiKit.Place(UiKit.Node(root, "Cluster"), UiKit.Top, new Vector2(0, -16), new Vector2(pitch * 4f, 100));
            AudioButton(cluster, new Vector2(-pitch * 1.5f, 0), false);
            AudioButton(cluster, new Vector2(-pitch * 0.5f, 0), true);
            UiKit.Button(cluster, null, ButtonStyle.Teal, UiKit.Center, new Vector2(pitch * 0.5f, 0), new Vector2(96, 96), hud.ToggleHelp, "glyph_help");
            UiKit.Button(cluster, null, ButtonStyle.Warm, UiKit.Center, new Vector2(pitch * 1.5f, 0), new Vector2(96, 96), game.TogglePause, "glyph_pause");
            hud.game = game;

            // Right (arena only): the bear's face, the all-time total above the mission counter.
            var right = UiKit.Place(UiKit.Node(root, "Bears"), UiKit.TopRight, new Vector2(-24, -20), new Vector2(360, Portrait));
            hud.bearBadge = Badge(right, "hud_bear", UiKit.TopRight, Vector2.zero);
            hud.totalBears = UiKit.Text(right, "", 36, UiKit.TopRight, new Vector2(-(Portrait + 14), 0), new Vector2(230, 46), Color.white,
                TextAlignmentOptions.Right, true);   // 30 px was about 11sp on a phone
            hud.bears = UiKit.Text(right, "0/0", 60, UiKit.TopRight, new Vector2(-(Portrait + 14), -42), new Vector2(230, 80), Color.white,
                TextAlignmentOptions.Right, true);
            right.gameObject.SetActive(false);

            // Caption bar along the bottom for short instructions.
            hud.hintBar = UiKit.Place(UiKit.Node(root, "Hint bar"), UiKit.Bottom, new Vector2(0, 26), new Vector2(900, 90));
            var hb = hud.hintBar.gameObject.AddComponent<Image>();
            hb.sprite = Art.Get("ui_pill");
            hb.type = Image.Type.Sliced;
            // The pill is translucent: a second one under the text keeps the hint readable over the catapult and the rocks.
            var backing = UiKit.Image(hud.hintBar, "ui_pill", UiKit.Center, Vector2.zero, new Vector2(900, 90));
            backing.preserveAspect = false;
            backing.type = Image.Type.Sliced;
            hud.hintText = UiKit.Text(hud.hintBar, "", 34, UiKit.Center, new Vector2(0, 2), new Vector2(860, 80), Color.white,
                TextAlignmentOptions.Center, true);
            hud.hintText.enableAutoSizing = true;   // long Vietnamese hints shrink instead of wrapping out of the bar
            hud.hintText.fontSizeMin = 20;
            hud.hintText.fontSizeMax = 34;
            hud.hintBar.gameObject.SetActive(false);

            hud.banner = UiKit.Text(root, "", 80, UiKit.Center, new Vector2(0, 150), new Vector2(1400, 200), Color.white,
                TextAlignmentOptions.Center, true);
            hud.banner.alpha = 0f;

            var hintImg = UiKit.Image(root, "glyph_tap", UiKit.Bottom, new Vector2(-320, 150), new Vector2(150, 150));   // clear of the whacker
            hud.hint = hintImg.rectTransform;
            hud.hint.gameObject.AddComponent<Bob>().pulse = true;
            hud.hint.gameObject.SetActive(false);
            return hud;
        }

        /// <summary>Round portrait badge (the painted ones have their own white ring).</summary>
        static RectTransform Badge(Transform root, string sprite, Vector2 anchor, Vector2 pos)
        {
            var img = UiKit.Image(root, sprite, anchor, pos, new Vector2(Portrait, Portrait));
            img.gameObject.AddComponent<SlideIn>().from = new Vector2(anchor.x > 0.5f ? 80 : -80, 0);
            return img.rectTransform;
        }

        /// <summary>Sound or music toggle with a red slash when off (shape, not only colour).</summary>
        public static Button AudioButton(Transform parent, Vector2 pos, bool music)
        {
            var button = UiKit.Button(parent, null, ButtonStyle.Teal, UiKit.Center, pos, new Vector2(96, 96), () =>
            {
                if (music) ToggleMusic(); else ToggleMute();
                AudioManager.NotifyChanged();
            }, music ? "glyph_music" : "glyph_sound");
            var sync = button.gameObject.AddComponent<OffMarkSync>();
            sync.mark = OffMark(button);
            sync.music = music;
            sync.Refresh();
            return button;
        }

        /// <summary>Keeps a sound/music slash in step with the save, whichever screen changed it (pause menu, settings).</summary>
        class OffMarkSync : MonoBehaviour
        {
            public Image mark;
            public bool music;

            void OnEnable()
            {
                AudioManager.Changed += Refresh;
                Refresh();
            }

            void OnDisable() => AudioManager.Changed -= Refresh;

            public void Refresh()
            {
                if (mark != null) SetOff(mark, music ? SaveSystem.Data.musicVolume <= 0.01f : SaveSystem.Data.muted);
            }
        }

        static void ToggleMute()
        {
            SaveSystem.Data.muted = !SaveSystem.Data.muted;
            SaveSystem.Save();
            AudioManager.Instance.ApplyVolumes();
        }

        /// <summary>Music volume before the music button turned it off, so turning it on restores the player's level.</summary>
        static float musicBeforeOff = SaveData.DefaultMusic;

        static void ToggleMusic()
        {
            var d = SaveSystem.Data;
            if (d.musicVolume > 0.01f)
            {
                musicBeforeOff = d.musicVolume;
                d.musicVolume = 0f;
            }
            else d.musicVolume = musicBeforeOff > 0.01f ? musicBeforeOff : SaveData.DefaultMusic;
            SaveSystem.Save();
            AudioManager.Instance.ApplyVolumes();
        }

        static Image OffMark(Button button)
        {
            var bar = UiKit.Image(button.transform, "ui_bar", UiKit.Center, Vector2.zero, new Vector2(96, 14));
            bar.preserveAspect = false;
            bar.type = Image.Type.Sliced;
            bar.color = new Color(0.9f, 0.15f, 0.15f);
            bar.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            return bar;
        }

        static void SetOff(Image mark, bool off)
        {
            if (mark == null) return;
            mark.enabled = off;
            var icon = mark.transform.parent.GetComponent<Image>();
            if (icon != null) icon.color = off ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
        }

        /// <summary>The ? button: shows the tip for the current phase in the caption bar for a few seconds (both tips
        /// at once shrank to about 9sp in German).</summary>
        public void ToggleHelp()
        {
            helpUntil = 0f;
            SetHintText(game.HelpText);
            helpUntil = Time.unscaledTime + 4f;
        }
        float helpUntil;
        GameManager game;

        public void SetPuffers(int n)
        {
            puffers.text = "× " + n;
            Bump(pupBadge);
        }

        /// <summary>Mission counter: bears knocked out over the number required; the all-time total above it.</summary>
        public void SetBears(int down, int required, int _)
        {
            bears.text = $"{down}/{required}";
            totalBears.text = Loc.T("total_label") + " " + (SaveSystem.Data.totalBears + down);
            Bump(bearBadge);
        }

        /// <summary>The bear counter belongs to the arena: hidden in the pit, shown when the attack starts.</summary>
        public void ShowBears(bool show) => bearBadge.parent.gameObject.SetActive(show);

        public void SetScore(int n) => score = n;

        void OnEnable() => SealCatapult.Attack.Bear.Defeated += OnBearDown;
        void OnDisable() => SealCatapult.Attack.Bear.Defeated -= OnBearDown;

        void OnBearDown(SealCatapult.Attack.Bear bear, int score)
        {
            // A bear that fell out of the arena has no on-screen spot to fly from: just bump the counter.
            if (bear == null || !isActiveAndEnabled) return;
            if (bear.transform.position.y < Backdrop.GroundY - 1f) { Bump(bearBadge); return; }
            StartCoroutine(StarToCounter(bear.transform.position + Vector3.up * 0.8f));
        }

        /// <summary>A gold star leaps from the knocked-out bear and arcs into the bear counter, which bumps on arrival
        /// (report §10.5: every bear knocked out sends a star to the HUD).</summary>
        System.Collections.IEnumerator StarToCounter(Vector3 world)
        {
            var cam = Camera.main;
            if (cam == null || bearBadge == null) yield break;
            var canvasRt = (RectTransform)GetComponent<Canvas>().transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, cam.WorldToScreenPoint(world), null, out Vector2 from);
            var star = UiKit.Image(canvasRt, "star_full", UiKit.Center, from, new Vector2(90, 90));
            var rt = star.rectTransform;
            const float Duration = 0.65f;
            for (float t = 0f; t < Duration; t += Time.unscaledDeltaTime)
            {
                if (bearBadge == null) break;
                Vector2 to = canvasRt.InverseTransformPoint(bearBadge.TransformPoint(bearBadge.rect.center));
                float k = t / Duration;
                float e = k * k * (3f - 2f * k);                       // ease in-out along the path
                rt.anchoredPosition = Vector2.Lerp(from, to, e) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 220f;
                rt.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.6f, k);
                rt.Rotate(0, 0, -540f * Time.unscaledDeltaTime);
                yield return null;
            }
            Destroy(star.gameObject);
            AudioManager.Sfx("star", 0.1f, 0.5f);
            Bump(bearBadge);
        }

        /// <summary>Between the phases, as in the reference: "Mission N", the hero, "Hit X bears to continue to the next
        /// level", "Total bears: Y" (all time), the pups that made it, and a Continue button. Stays until Continue (or a
        /// tap anywhere): <see cref="DismissMission"/>.</summary>
        public void MissionCard(int levelNumber, int required, int total, int pups, Action onContinue)
        {
            var card = UiKit.Place(UiKit.Node(transform.GetChild(0), "Mission"), UiKit.Center, new Vector2(0, 20), new Vector2(1000, 640));
            mission = card;
            var img = card.gameObject.AddComponent<Image>();
            img.sprite = Art.Get(Art.Has("ui_card") ? "ui_card" : "ui_panel");
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;   // a tap on the card itself counts as Continue (only the button is UI)
            UiKit.Text(card, Loc.T("mission_n", levelNumber), 56, UiKit.Top, new Vector2(0, -24), new Vector2(800, 70), Color.white,
                TextAlignmentOptions.Center, true);
            var pedestal = UiKit.Image(card, "powder", UiKit.Top, new Vector2(0, -290), new Vector2(320, 80));
            pedestal.color = new Color(0.7f, 0.9f, 1f, 0.9f);
            // The hero bows down and comes up saluting (report §14.3: the reference's hero stoops for its hat, then
            // salutes). Without the art, the old spin.
            var hero = UiKit.Image(card, Art.Has("pup_salute_0") ? "pup_salute_0" : "pup", UiKit.Top, new Vector2(0, -100), new Vector2(220, 220));
            if (Art.Has("pup_salute_3") && Art.Has("pup_leap_0")) hero.gameObject.AddComponent<FrameLoop>().Set(5f, ("pup_leap_", 1), ("pup_leap_", 1), ("pup_salute_", 4), ("pup_salute_", 4));   // a crouch, then the salute (the grab frames show the rope)
            else if (Art.Has("pup_salute_0")) hero.gameObject.AddComponent<FrameLoop>().Set("pup_salute_", 4, 5f);
            else hero.gameObject.AddComponent<Spin>();
            UiKit.Text(card, required > 0 ? Loc.T("hit_bears", required) : Loc.T("collect_fish"), 46, UiKit.Top, new Vector2(0, -340), new Vector2(900, 110), Color.white, TextAlignmentOptions.Center, true);
            UiKit.Text(card, Loc.T("bears_all_time", total), 40, UiKit.Top, new Vector2(0, -455), new Vector2(900, 60), new Color32(0x2a, 0x6f, 0xb0, 0xff), TextAlignmentOptions.Center, true);
            UiKit.Text(card, pups == 1 ? Loc.T("through_one") : Loc.T("through", pups), 32, UiKit.Top, new Vector2(-170, -530), new Vector2(560, 60), new Color32(0x56, 0x63, 0x7b, 0xff), TextAlignmentOptions.Left);
            UiKit.Button(card, Loc.T("continue"), ButtonStyle.Gold, UiKit.BottomRight, new Vector2(-30, 30), new Vector2(320, 104), onContinue, "glyph_next");
            card.gameObject.AddComponent<SlideIn>().from = new Vector2(0, 120);
        }

        RectTransform curtain;
        const float CurtainHeight = 1500f;   // taller than any canvas (1080 design height, up to ~1300 on 4:3)

        /// <summary>Water curtain between the phases: drops from the top to cover the screen, or lifts away again.</summary>
        public System.Collections.IEnumerator Curtain(bool down)
        {
            var canvasRt = (RectTransform)transform;
            if (curtain == null)
            {
                var img = UiKit.Image(canvasRt, "wave_cover", UiKit.Top, Vector2.zero, Vector2.zero);
                img.preserveAspect = false;
                img.type = Image.Type.Tiled;
                img.pixelsPerUnitMultiplier = 0.55f;
                img.raycastTarget = true;   // no taps reach the game under the water
                curtain = img.rectTransform;
                curtain.anchorMin = new Vector2(-0.1f, 1f);
                curtain.anchorMax = new Vector2(1.1f, 1f);
                curtain.pivot = new Vector2(0.5f, 0f);
                curtain.sizeDelta = new Vector2(0f, CurtainHeight);
                curtain.anchoredPosition = new Vector2(0f, CurtainHeight);
                curtain.localScale = new Vector3(1f, -1f, 1f);   // wavy edge leads the way down
                curtain.SetSiblingIndex(1);                       // over the game, under the HUD's own controls
                AudioManager.Sfx("splash", 0f, 0.8f);
            }
            // Flipped, the panel hangs down from its pivot: at 0 it covers the screen, at CurtainHeight it is all above it.
            const float Seconds = 0.45f;
            for (float t = 0f; t < Seconds; t += Time.deltaTime)
            {
                float k = t / Seconds;
                curtain.anchoredPosition = new Vector2(0f, CurtainHeight * (down ? 1f - k : k));
                yield return null;
            }
            if (down) curtain.anchoredPosition = Vector2.zero;
            else { Destroy(curtain.gameObject); curtain = null; }
        }

        public void DismissMission()
        {
            if (mission == null) return;
            StartCoroutine(FadeAway(mission, 0f));
            mission = null;
        }

        static System.Collections.IEnumerator FadeAway(RectTransform card, float after)
        {
            if (after > 0f) yield return new WaitForSeconds(after);
            var group = card.GetComponent<CanvasGroup>();
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                if (group != null) group.alpha = 1f - t / 0.35f;
                card.localScale = Vector3.one * (1f + t * 0.3f);
                yield return null;
            }
            Destroy(card.gameObject);
        }

        /// <summary>Bottom caption; empty hides it (a help tip keeps it open for a moment).</summary>
        public void SetHintText(string text)
        {
            if (Time.unscaledTime < helpUntil) return;
            bool show = !string.IsNullOrEmpty(text);
            if (hintBar.gameObject.activeSelf != show) hintBar.gameObject.SetActive(show);
            if (show && hintText.text != text) hintText.text = text;
        }

        public void ShowHint(bool show)
        {
            if (hint.gameObject.activeSelf != show) hint.gameObject.SetActive(show);
        }

        public void Banner(string text)
        {
            banner.text = text;
            bannerTime = 0f;
        }

        void Bump(RectTransform rt)
        {
            if (rt != null && isActiveAndEnabled && rt.gameObject.activeInHierarchy) StartCoroutine(BumpRoutine(rt));
        }

        static System.Collections.IEnumerator BumpRoutine(RectTransform rt)
        {
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                rt.localScale = Vector3.one * (1f + Mathf.Sin(t / 0.2f * Mathf.PI) * 0.15f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        void Update()
        {
            if (bannerTime > BannerEnd) return;   // faded out: stop dirtying the canvas every frame
            bannerTime += Time.unscaledDeltaTime;
            float a = bannerTime < 1.6f ? 1f : Mathf.Clamp01(1f - (bannerTime - 1.6f) / 0.4f);
            banner.alpha = a;
            float pop = 1f + Mathf.Max(0f, 0.25f - bannerTime) * 1.2f;
            banner.rectTransform.localScale = new Vector3(pop, pop, 1f);
        }
    }
}

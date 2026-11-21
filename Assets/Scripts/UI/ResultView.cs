using System.Collections;
using SealCatapult.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SealCatapult.UI
{
    /// <summary>End of mission: the win or loss card. No score, as in the reference; this game's three stars (the shop's
    /// currency) each come with the rule that earns it. The all-time bear total lives on the stats page.</summary>
    public class ResultView : MonoBehaviour
    {
        const float PanelWidth = 1040f;   // near-square like CPC's card; room for the loss row (2 round + ad + main)
        const float StarPitch = 310f;     // room under each star for its rule
        const float PageHeight = 2f * 500f + 40f;   // the ribbon's top is 500 above the centre; a little air round it

        GameManager game;
        Canvas canvas;

        public static ResultView Create(GameManager game)
        {
            var canvas = UiKit.CreateCanvas("Result", 30);
            var view = canvas.gameObject.AddComponent<ResultView>();
            view.game = game;
            view.canvas = canvas;
            canvas.enabled = false;
            return view;
        }

        /// <param name="down">Bears knocked out (fish caught on the bonus level) out of <paramref name="required"/>.</param>
        /// <param name="boss">The boss level: a win closes Frost Bay's story instead of counting bears, and the second
        /// star's rule names the boss.</param>
        /// <param name="rules">Which star rules were met; each star sits over its own rule, so the middle one can
        /// stay empty while the third is earned.</param>
        public void Show(int levelNumber, bool won, bool perfect, (bool passed, bool second, bool third) rules, int down, int required, bool bonus, bool boss)
        {
            canvas.enabled = true;
            var overlay = UiKit.Overlay(canvas.transform, "Overlay");
            // CPC's result card, studied frame by frame (video, 18/11; user: "xem lại thật kĩ các modal cpc để thiết kế
            // lại"): a near-square, airy panel; the headline on the ribbon, "Mission N" inside at the top, a small cast centred on a white snow oval (the pup on the knocked-out
            // bear's belly; Perfect: more pups round it), centred stat lines with big blue numbers, and ONE main button
            // at the bottom right. This game adds its three stars with their rules, a loss scene (the bear laughing at
            // the dazed pup) and small round Map/Retry buttons at the bottom left.
            // Fitted to the canvas height like the info pages: on 20:9 and wider the ribbon was cut off the top and the
            // buttons touched the bottom edge.
            var holder = UiKit.Place(UiKit.Node(overlay, "Fit"), UiKit.Center, Vector2.zero, Vector2.zero);
            float fit = Mathf.Min(1f, UiKit.CanvasSize().y / PageHeight);
            holder.localScale = new Vector3(fit, fit, 1f);
            var panel = UiKit.Panel(holder, new Vector2(PanelWidth, 900), new Vector2(0, -20)).transform;
            panel.localScale = Vector3.one * 0.6f;
            StartCoroutine(PopIn(panel));
            // The headline on the ribbon (user 18/11: "vẫn thêm ruy băng"), "Mission N" inside under it.
            var title = UiKit.Ribbon(panel, Loc.T(perfect ? "perfect_mission" : won ? "congrats" : "mission_failed"), 760, 70);
            title.enableAutoSizing = true;
            UiKit.Text(panel, Loc.T("mission_n", levelNumber), 60, UiKit.Top, new Vector2(0, -120), new Vector2(800, 80), Color.white,
                TMPro.TextAlignmentOptions.Center, true);

            // The stage: a pale snow oval, the small cast's feet on it.
            const float Feet = -455f;
            var patch = UiKit.Image(panel, "powder", UiKit.Top, new Vector2(0, Feet + 50), new Vector2(560, 130));
            patch.preserveAspect = false;
            patch.color = new Color(0.70f, 0.85f, 0.97f, 0.9f);   // CPC's oval is white on a flat panel; on this icy panel white vanishes
            if (won)
            {
                StartCoroutine(Confetti(overlay));
                UiKit.Cast(panel, "bear_modal_ko", UiKit.Top, new Vector2(0, Feet), 150f, 1.6f, true);
                // CPC: the winner stands on the bear's belly; on a Perfect more pups stand round the bear.
                UiKit.Cast(panel, "pup_modal_win", UiKit.Top, new Vector2(20, Feet + 70), 125f, 3.2f);
                if (perfect)
                {
                    UiKit.Cast(panel, "pup_modal_win", UiKit.Top, new Vector2(-190, Feet + 10), 120f, 2.9f);
                    UiKit.Cast(panel, "pup_modal_win", UiKit.Top, new Vector2(200, Feet + 10), 120f, 3.5f, true);
                    // Sparkles round the gold line, as on CPC's "Perfect!".
                    for (int i = 0; i < 4; i++)
                    {
                        var star = UiKit.Image(panel, "star_full", UiKit.Top, new Vector2(i < 2 ? -330 + i * 50 : 280 + (i - 2) * 50, -120 - (i % 2) * 50), new Vector2(40, 40));
                        star.gameObject.AddComponent<PopIn>().delay = 0.3f + i * 0.15f;
                        var twinkle = star.gameObject.AddComponent<Bob>();
                        twinkle.amplitude = 6f;
                        twinkle.speed = 5f + i;
                    }
                }
            }
            else
            {
                UiKit.Cast(panel, "pup_modal_lose", UiKit.Top, new Vector2(-120, Feet), 125f, 3.2f);
                UiKit.Cast(panel, "bear_modal_mock", UiKit.Top, new Vector2(90, Feet), 200f, 1.6f, true);   // points left, at the pup
            }

            // CPC's stat line: the label, then the number big and blue. A cage boss frees its captive pups; the castle's
            // own boss (last on the map) frees the castle.
            int index = levelNumber - 1;
            string bossLine = Progress.LevelCatalog.IsFinal(index)
                ? Loc.T("castle_freed", MapView.CastleName(Progress.LevelCatalog.CastleOf(index)))
                : Loc.T("cage_freed");
            string line = won && boss ? bossLine
                : Loc.T(bonus ? "fish_got" : "bears_hit", $"<size=140%><color=#{ColorUtility.ToHtmlStringRGB(UiKit.Accent)}>{down:00}", $"{required:00}</color></size>");
            UiKit.Text(panel, line, 46, UiKit.Top, new Vector2(0, -500), new Vector2(940, 80), Color.white, TMPro.TextAlignmentOptions.Center, true);

            // This game's own: the stars (the shop's currency), each over the rule that earns it, so a missing star
            // explains itself.
            bool[] earned = { rules.passed, rules.second, rules.third };
            int shown = 0;
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector2((i - 1) * StarPitch, -575);
                var star = UiKit.Image(panel, earned[i] ? "star_full" : "star_empty", UiKit.Top, at, new Vector2(84, 84));
                if (earned[i]) star.gameObject.AddComponent<PopIn>().delay = 0.4f + 0.25f * shown++;
                // On the boss level the second star is for beating the boss with a pup left, not for every bear.
                string key = bonus ? "star_fish_" + (i + 1) : boss && i == 1 ? "star_boss_2" : "star_rule_" + (i + 1);
                var rule = UiKit.Text(panel, Loc.T(key), 30, UiKit.Top, at + new Vector2(0, -86), new Vector2(StarPitch - 16, 80), UiKit.Ink);   // 26 px was under 10sp
                rule.fontSizeMin = 24;
                if (!earned[i]) rule.alpha = 0.6f;   // with the empty star above it: shape, not only colour
            }

            // CPC: one main button at the bottom right. Map and Retry are small round buttons at the bottom left.
            var round = new Vector2(110, 110);
            UiKit.Button(panel, null, ButtonStyle.Teal, UiKit.BottomLeft, new Vector2(50, 40), round, game.ToMap, "glyph_map");
            if (won)
            {
                if (perfect)
                {
                    var badge = UiKit.Image(panel, "badge_perfect", UiKit.TopRight, new Vector2(-50, -50), new Vector2(120, 120));
                    badge.gameObject.AddComponent<PopIn>().delay = 1.2f;
                }
                UiKit.Button(panel, null, ButtonStyle.Teal, UiKit.BottomLeft, new Vector2(180, 40), round, game.Retry, "glyph_retry");
                UiKit.Button(panel, Loc.T("continue"), ButtonStyle.Gold, UiKit.BottomRight, new Vector2(-50, 40), new Vector2(440, 110),
                    game.HasNextLevel ? game.NextLevel : (System.Action)game.ToMap, "glyph_next");
                return;
            }
            // The free retry is the main button. The opt-in rewarded ad (GAME_DESIGN.md §13b) is a smaller one and says
            // "Ad" in its label; no ad means the label turns into a note, never a free pup.
            TMPro.TextMeshProUGUI adLabel = null;
            System.Action showAd = () => game.RetryWithBonusPup(() => adLabel.text = Loc.T("ad_none"));
            Button ad;
            if (game.CanRetryAttack)
            {
                // Pups got out: "Attack again" keeps them and is the main button. The whole level stays one tap away
                // (the round Retry), or a player with too few pups could never redo the pit from here.
                UiKit.Button(panel, null, ButtonStyle.Teal, UiKit.BottomLeft, new Vector2(180, 40), round, game.Retry, "glyph_retry");
                ad = UiKit.Button(panel, Loc.T("ad_bonus_pup"), ButtonStyle.Warm, UiKit.BottomRight, new Vector2(-420, 40), new Vector2(320, 110), showAd);   // no glyph: the label ("Ad: +1 pup") needs the width
                UiKit.Button(panel, Loc.T("retry_attack"), ButtonStyle.Gold, UiKit.BottomRight, new Vector2(-50, 40), new Vector2(360, 110), game.RetryLost, "glyph_next");
            }
            else
            {
                ad = UiKit.Button(panel, Loc.T("ad_bonus_pup"), ButtonStyle.Warm, UiKit.BottomRight, new Vector2(-420, 40), new Vector2(360, 110), showAd);
                UiKit.Button(panel, Loc.T("retry"), ButtonStyle.Gold, UiKit.BottomRight, new Vector2(-50, 40), new Vector2(360, 110), game.RetryLost, "glyph_retry");
            }
            adLabel = ad.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        }

        /// <summary>UI confetti of stars and snowflakes bursting from the top of the screen.</summary>
        static IEnumerator Confetti(RectTransform parent)
        {
            const int Count = 40;
            var bits = new RectTransform[Count];
            var vel = new Vector2[Count];
            var spin = new float[Count];
            for (int i = 0; i < Count; i++)
            {
                var img = UiKit.Image(parent, i % 3 == 0 ? "star_full" : "snowflake", UiKit.Center, new Vector2(Random.Range(-200f, 200f), 380f),
                    Vector2.one * Random.Range(26f, 56f));
                bits[i] = img.rectTransform;
                vel[i] = new Vector2(Random.Range(-900f, 900f), Random.Range(300f, 1000f));
                spin[i] = Random.Range(-400f, 400f);
            }
            for (float t = 0f; t < 2.4f; t += Time.unscaledDeltaTime)
            {
                float dt = Time.unscaledDeltaTime;
                for (int i = 0; i < Count; i++)
                {
                    vel[i] += Vector2.down * 1500f * dt;
                    vel[i] *= 1f - 0.8f * dt;
                    bits[i].anchoredPosition += vel[i] * dt;
                    bits[i].Rotate(0, 0, spin[i] * dt);
                }
                yield return null;
            }
            foreach (var b in bits) if (b != null) Destroy(b.gameObject);
        }

        static IEnumerator PopIn(Transform t)
        {
            for (float time = 0f; time < 0.3f; time += Time.unscaledDeltaTime)
            {
                float k = time / 0.3f;
                float s = Mathf.LerpUnclamped(0.6f, 1f, 1f + 2.2f * Mathf.Pow(k - 1f, 3) + 1.2f * Mathf.Pow(k - 1f, 2));
                t.localScale = Vector3.one * s;
                yield return null;
            }
            t.localScale = Vector3.one;
        }
    }
}

using System.Collections;
using SealCatapult.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SealCatapult.UI
{
    /// <summary>End of mission, laid out like the reference (report §15): "Mission N" over "Congratulations!" (or
    /// "Mission failed!"), the hero standing on a knocked-out bear, "Bears hit: d/r", Continue.
    /// No score, as in the reference; this game adds its three stars (the shop's currency), each with a line saying
    /// what earns it, and the Retry/Map buttons. The all-time bear total lives on the mission card and the stats page.</summary>
    public class ResultView : MonoBehaviour
    {
        const float StarPitch = 380f;   // room under each star for its rule in the longest language

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
            var panel = UiKit.Panel(overlay, new Vector2(1240, 860)).transform;
            panel.localScale = Vector3.one * 0.6f;
            StartCoroutine(PopIn(panel));
            UiKit.Text(panel, Loc.T("mission_n", levelNumber), 50, UiKit.Top, new Vector2(0, -34), new Vector2(900, 64), Color.white,
                TMPro.TextAlignmentOptions.Center, true);
            var title = UiKit.Logo(panel, Loc.T(perfect ? "perfect_mission" : won ? "congrats" : "mission_failed"), 84, UiKit.Top, new Vector2(0, -92), new Vector2(1100, 120));
            title.enableAutoSizing = true;
            title.fontSizeMin = 40;
            title.fontSizeMax = 84;
            if (won)
            {
                StartCoroutine(Confetti(overlay));
                // The winners' pile: a knocked-out bear with pups hopping on it, like the reference's result card.
                var pile = UiKit.Place(UiKit.Node(panel, "Pile"), UiKit.Top, new Vector2(0, -215), new Vector2(360, 230));
                UiKit.Image(pile, "bear_ko_0", UiKit.Bottom, new Vector2(0, 30), new Vector2(300, 180));
                // CPC (live 16/11): one pup cheers on a plain win; on a Perfect four bare-headed pups dance, flippers up,
                // and stars twinkle round the title.
                int dancers = perfect ? 4 : 1;
                string loop = perfect && Art.Has("pup_cheer_bare_3") ? "pup_cheer_bare_" : "pup_cheer_";
                for (int i = 0; i < dancers; i++)
                {
                    float x = dancers == 1 ? 0f : -120 + i * 80;
                    var pup = UiKit.Image(pile, loop + "0", UiKit.Bottom, new Vector2(x, 130 + (i % 2) * 20), new Vector2(dancers == 1 ? 120 : 96, dancers == 1 ? 120 : 96));
                    if (Art.Has(loop + "3")) pup.gameObject.AddComponent<FrameLoop>().Set(loop, 4, 6f + i);
                    var bob = pup.gameObject.AddComponent<Bob>();
                    bob.amplitude = 14f;
                    bob.speed = 4f + i;
                }
                if (perfect)
                    for (int i = 0; i < 4; i++)
                    {
                        var star = UiKit.Image(panel, "star_full", UiKit.Top, new Vector2(i < 2 ? -560 + i * 60 : 500 + (i - 2) * 60, -70 - (i % 2) * 50), new Vector2(54, 54));
                        star.gameObject.AddComponent<PopIn>().delay = 0.3f + i * 0.15f;
                        var twinkle = star.gameObject.AddComponent<Bob>();
                        twinkle.amplitude = 8f;
                        twinkle.speed = 5f + i;
                    }
            }
            else
            {
                // The failed card (CPC): two bears laughing over a flattened pup.
                for (int i = 0; i < 2; i++)
                {
                    // Both point at the pup on the left: the laugh frames point right, so a flipped holder mirrors them
                    // (PopIn owns the image's own scale).
                    var holder = UiKit.Place(UiKit.Node(panel, "Laugher"), UiKit.Top, new Vector2(i == 0 ? 40 : 240, -220), new Vector2(220, 220));
                    holder.localScale = new Vector3(-1f, 1f, 1f);
                    var bear = UiKit.Image(holder, Art.Has("bear_laugh_0") ? "bear_laugh_0" : "bear_helmet", UiKit.Center, Vector2.zero, new Vector2(220, 220));
                    if (Art.Has("bear_laugh_3")) bear.gameObject.AddComponent<FrameLoop>().Set("bear_laugh_", 4, 6f + i);
                    bear.gameObject.AddComponent<PopIn>().delay = 0.2f + i * 0.15f;
                }
                // The beaten pup lies on its back, bare-headed and kicking (sheet T), between the laughing bears.
                bool kicking = Art.Has("pup_stuck_wall_7");
                var loser = UiKit.Image(panel, kicking ? "pup_stuck_wall_0" : "pup_stuck_0", UiKit.Top, new Vector2(-170, -300), new Vector2(170, 170));
                if (kicking) loser.gameObject.AddComponent<FrameLoop>().Set("pup_stuck_wall_", 8, 10f);
            }

            // A cage boss frees its captive pups; the castle's own boss (last on the map) frees the castle.
            int index = levelNumber - 1;
            string bossLine = Progress.LevelCatalog.IsFinal(index)
                ? Loc.T("castle_freed", MapView.CastleName(Progress.LevelCatalog.CastleOf(index)))
                : Loc.T("cage_freed");
            string line = won && boss ? bossLine : Loc.T(bonus ? "fish_got" : "bears_hit", down, required);
            UiKit.Text(panel, line, 44, UiKit.Top, new Vector2(0, -462), new Vector2(1100, 60), UiKit.Ink);

            // This game's own: the stars earned (they pay for upgrades), each over the rule that earns it, so a missing
            // star explains itself. No score, as in the reference.
            bool[] earned = { rules.passed, rules.second, rules.third };
            int shown = 0;
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector2((i - 1) * StarPitch, -528);
                var star = UiKit.Image(panel, earned[i] ? "star_full" : "star_empty", UiKit.Top, at, new Vector2(96, 96));
                if (earned[i]) star.gameObject.AddComponent<PopIn>().delay = 0.4f + 0.25f * shown++;
                // On the boss level the second star is for beating the boss with a pup left, not for every bear.
                string key = bonus ? "star_fish_" + (i + 1) : boss && i == 1 ? "star_boss_2" : "star_rule_" + (i + 1);
                var rule = UiKit.Text(panel, Loc.T(key), 30, UiKit.Top,
                    at + new Vector2(0, -98), new Vector2(StarPitch - 20, 72), UiKit.Ink);
                if (!earned[i]) rule.alpha = 0.7f;   // with the empty star above it: shape, not only colour
            }
            if (won)
            {
                if (perfect)
                {
                    var badge = UiKit.Image(panel, "badge_perfect", UiKit.Top, new Vector2(470, -200), new Vector2(150, 150));
                    badge.gameObject.AddComponent<PopIn>().delay = 1.2f;
                }
                // Layout A: Map and Retry are square icon buttons; the next level is the one big button.
                var square = new Vector2(120, 108);
                UiKit.Button(panel, null, ButtonStyle.Teal, UiKit.Bottom, new Vector2(-400, 36), square, game.ToMap, "glyph_map");
                UiKit.Button(panel, null, ButtonStyle.Teal, UiKit.Bottom, new Vector2(-250, 36), square, game.Retry, "glyph_retry");
                UiKit.Button(panel, Loc.T("continue"), ButtonStyle.Gold, UiKit.Bottom, new Vector2(150, 36), new Vector2(560, 108),
                    game.HasNextLevel ? game.NextLevel : (System.Action)game.ToMap, "glyph_next");
            }
            else
            {
                // The free retry is the main button, where Continue sits on the win card. The opt-in rewarded ad
                // (GAME_DESIGN.md §13b) is a smaller one and says "Ad" in its label; no ad means the label turns
                // into a note, never a free pup.
                TMPro.TextMeshProUGUI adLabel = null;
                System.Action showAd = () => game.RetryWithBonusPup(() => adLabel.text = Loc.T("ad_none"));
                Button ad;
                if (game.CanRetryAttack)
                {
                    // Pups got out: "Attack again" keeps them. The whole level stays one tap away (plain Retry), or a
                    // player with too few pups could never redo the pit from here; Map shrinks to its icon for room.
                    UiKit.Button(panel, null, ButtonStyle.Teal, UiKit.Bottom, new Vector2(-542, 36), new Vector2(120, 108), game.ToMap, "glyph_map");
                    UiKit.Button(panel, Loc.T("retry"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(-332, 36), new Vector2(270, 108), game.Retry, "glyph_retry");
                    ad = UiKit.Button(panel, Loc.T("ad_bonus_pup"), ButtonStyle.Warm, UiKit.Bottom, new Vector2(13, 36), new Vector2(390, 108), showAd, "glyph_play");   // the longest label of the row
                    UiKit.Button(panel, Loc.T("retry_attack"), ButtonStyle.Gold, UiKit.Bottom, new Vector2(413, 36), new Vector2(380, 108), game.RetryLost, "glyph_next");
                }
                else
                {
                    UiKit.Button(panel, Loc.T("map"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(-480, 36), new Vector2(240, 108), game.ToMap, "glyph_map");
                    ad = UiKit.Button(panel, Loc.T("ad_bonus_pup"), ButtonStyle.Warm, UiKit.Bottom, new Vector2(-135, 36), new Vector2(410, 108), showAd, "glyph_play");
                    UiKit.Button(panel, Loc.T("retry"), ButtonStyle.Gold, UiKit.Bottom, new Vector2(345, 36), new Vector2(490, 108), game.RetryLost, "glyph_retry");
                }
                adLabel = ad.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }
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

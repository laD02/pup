using SealCatapult.Core;
using SealCatapult.Progress;
using UnityEngine;

namespace SealCatapult.UI
{
    /// <summary>Pause (redesign 18/11, GAME_DESIGN.md "Bảng"): an ice panel with the "Paused" ribbon, the castle and level,
    /// Resume / Retry / Map stacked (Resume the one green button), the music and sound switches, and the pup sipping cocoa
    /// against the panel's left side.</summary>
    public class PauseMenu : MonoBehaviour
    {
        GameObject root;
        GameManager game;
        TMPro.TextMeshProUGUI where;

        public static PauseMenu Create(GameManager game)
        {
            var canvas = UiKit.CreateCanvas("Pause", 20);
            var menu = canvas.gameObject.AddComponent<PauseMenu>();
            menu.game = game;
            var overlay = UiKit.Overlay(canvas.transform, "Overlay");
            menu.root = overlay.gameObject;
            var safe = UiKit.SafeRoot(canvas);
            safe.SetParent(overlay, false);

            var panel = UiKit.Panel(safe, new Vector2(720, 700), new Vector2(0, -30)).transform;
            panel.gameObject.AddComponent<PopIn>();
            UiKit.Ribbon(panel, Loc.T("paused"), 620, 64);
            UiKit.CloseButton(panel, game.TogglePause);   // closing the pause card resumes, like Resume
            menu.where = UiKit.Text(panel, "", 40, UiKit.Top, new Vector2(0, -100), new Vector2(600, 56), UiKit.Ink);
            var size = new Vector2(480, 100);
            UiKit.Button(panel, Loc.T("resume"), ButtonStyle.Gold, UiKit.Top, new Vector2(0, -165), size, game.TogglePause, "glyph_play");   // 130 apart: each 100 px button has a 130 px touch area
            UiKit.Button(panel, Loc.T("retry"), ButtonStyle.Teal, UiKit.Top, new Vector2(0, -295), size, game.Retry, "glyph_retry");
            UiKit.Button(panel, Loc.T("map"), ButtonStyle.Teal, UiKit.Top, new Vector2(0, -425), size, game.ToMap, "glyph_map");
            // The HUD keeps only Pause, so the sound and music switches live here (round, with the off slash). Their
            // 130 px touch areas start where Map's ends (they covered its bottom 20 px: a tap there switched the music)
            // and stand 140 px apart, so they do not overlap each other either.
            HudView.AudioButton(panel, new Vector2(-70, -255), true);
            HudView.AudioButton(panel, new Vector2(70, -255), false);
            UiKit.Cast(panel, "pup_modal_wait", UiKit.BottomLeft, new Vector2(-20, 24), 340f, 3.2f);
            menu.root.SetActive(false);
            return menu;
        }

        public void SetVisible(bool visible)
        {
            if (visible && where != null)
            {
                int level = game.LevelIndex;
                where.text = MapView.CastleName(LevelCatalog.CastleOf(level)) + " · " + Loc.T("level_n", level + 1);
            }
            root.SetActive(visible);
        }
    }
}

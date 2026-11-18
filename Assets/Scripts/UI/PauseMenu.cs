using SealCatapult.Core;
using TMPro;
using UnityEngine;

namespace SealCatapult.UI
{
    /// <summary>Pause, as in the reference: the game dims under a big "PAUSE"; a row of buttons below it (Resume,
    /// Retry, Map, Sound) is this game's addition.</summary>
    public class PauseMenu : MonoBehaviour
    {
        GameObject root;

        public static PauseMenu Create(GameManager game)
        {
            var canvas = UiKit.CreateCanvas("Pause", 20);
            var menu = canvas.gameObject.AddComponent<PauseMenu>();
            var overlay = UiKit.Overlay(canvas.transform, "Overlay");
            menu.root = overlay.gameObject;
            var safe = UiKit.SafeRoot(canvas);
            safe.SetParent(overlay, false);

            var title = UiKit.Text(safe, Loc.T("paused"), 150, UiKit.Center, new Vector2(0, 110), new Vector2(1400, 200), Color.white,
                TextAlignmentOptions.Center, true);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 6f;
            var size = new Vector2(400, 120);
            UiKit.Button(safe, Loc.T("resume"), ButtonStyle.Gold, UiKit.Center, new Vector2(-440, -90), size, game.TogglePause, "glyph_play");
            UiKit.Button(safe, Loc.T("retry"), ButtonStyle.Teal, UiKit.Center, new Vector2(0, -90), size, game.Retry, "glyph_retry");
            UiKit.Button(safe, Loc.T("map"), ButtonStyle.Teal, UiKit.Center, new Vector2(440, -90), size, game.ToMap, "glyph_map");
            // Layout A: the HUD keeps only Pause, so the sound and music switches live here (round, with the off slash).
            HudView.AudioButton(safe, new Vector2(-UiKit.MinHit * 0.5f, -270), false);
            HudView.AudioButton(safe, new Vector2(UiKit.MinHit * 0.5f, -270), true);
            menu.root.SetActive(false);
            return menu;
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }


    }
}

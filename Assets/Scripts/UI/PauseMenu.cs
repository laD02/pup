using PufferLaunch.Core;
using TMPro;
using UnityEngine;

namespace PufferLaunch.UI
{
    /// <summary>Pause, as in the reference: the game dims under a big "PAUSE"; a row of buttons below it (Resume,
    /// Retry, Map, Sound) is this game's addition.</summary>
    public class PauseMenu : MonoBehaviour
    {
        GameObject root;
        TextMeshProUGUI soundLabel;

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
            var size = new Vector2(360, 110);
            UiKit.Button(safe, Loc.T("resume"), ButtonStyle.Gold, UiKit.Center, new Vector2(-600, -110), size, game.TogglePause, "glyph_play");
            UiKit.Button(safe, Loc.T("retry"), ButtonStyle.Teal, UiKit.Center, new Vector2(-200, -110), size, game.Retry, "glyph_retry");
            UiKit.Button(safe, Loc.T("map"), ButtonStyle.Teal, UiKit.Center, new Vector2(200, -110), size, game.ToMap, "glyph_map");
            var sound = UiKit.Button(safe, Loc.T("sound"), ButtonStyle.Warm, UiKit.Center, new Vector2(600, -110), size, menu.ToggleSound, "glyph_sound");
            menu.soundLabel = sound.GetComponentInChildren<TextMeshProUGUI>();
            menu.RefreshSound();
            menu.root.SetActive(false);
            return menu;
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
            if (visible) RefreshSound();   // the HUD sound button may have changed it meanwhile
        }

        void ToggleSound()
        {
            SaveSystem.Data.muted = !SaveSystem.Data.muted;
            SaveSystem.Save();
            AudioManager.Instance.ApplyVolumes();
            AudioManager.NotifyChanged();
            RefreshSound();
        }

        void RefreshSound() => soundLabel.text = Loc.T("sound") + ": " + Loc.T(SaveSystem.Data.muted ? "off" : "on");
    }
}

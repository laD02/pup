using PufferLaunch.Core;
using UnityEngine;

namespace PufferLaunch.UI
{
    /// <summary>Settings: music/SFX volume, language, erase save (with confirmation), credits.</summary>
    public class SettingsView : MonoBehaviour
    {
        Canvas canvas;
        Transform content;

        public static SettingsView Create()
        {
            var canvas = UiKit.CreateCanvas("Settings", 20);
            var view = canvas.gameObject.AddComponent<SettingsView>();
            view.canvas = canvas;
            canvas.enabled = false;
            return view;
        }

        public void Open()
        {
            canvas.enabled = true;
            Rebuild();
        }

        /// <summary>Volume drags are saved on Close; a kill while Settings is open would lose them otherwise.</summary>
        void OnApplicationPause(bool goingAway)
        {
            if (goingAway && canvas != null && canvas.enabled) SaveSystem.Save();
        }

        void Close()
        {
            SaveSystem.Save();
            canvas.enabled = false;
        }

        void Rebuild()
        {
            if (content != null) Destroy(content.gameObject);
            var overlay = UiKit.Overlay(canvas.transform, "Overlay");
            content = overlay;
            var panel = UiKit.Panel(overlay, new Vector2(1300, 820)).transform;

            UiKit.Text(panel, Loc.T("settings"), 80, UiKit.Top, new Vector2(0, -30), new Vector2(900, 110));
            Row(panel, Loc.T("music"), -140);
            UiKit.Slider(panel, UiKit.Top, new Vector2(200, -150), new Vector2(560, 60), SaveSystem.Data.musicVolume, v =>
            {
                SaveSystem.Data.musicVolume = v;   // read Data each time: an erase replaces the object
                AudioManager.Instance.ApplyVolumes();
                AudioManager.NotifyChanged();
            });
            Row(panel, Loc.T("sfx"), -250);
            UiKit.Slider(panel, UiKit.Top, new Vector2(200, -260), new Vector2(560, 60), SaveSystem.Data.sfxVolume, v =>
            {
                SaveSystem.Data.sfxVolume = v;
                AudioManager.Instance.ApplyVolumes();
                AudioManager.NotifyChanged();
            });
            Row(panel, Loc.T("language"), -360);
            UiKit.Button(panel, Loc.T("lang_name"), ButtonStyle.Gold, UiKit.Top, new Vector2(200, -345), new Vector2(560, 100), () =>
            {
                Loc.SetLanguage(Loc.NextLanguage);
                Rebuild();
            });
            UiKit.Button(panel, Loc.T("stats"), ButtonStyle.Teal, UiKit.Top, new Vector2(-260, -490), new Vector2(460, 100),
                () => StatsPanel.Show(canvas.transform), "glyph_help");
            UiKit.Button(panel, Loc.T("reset"), ButtonStyle.Warm, UiKit.Top, new Vector2(260, -490), new Vector2(460, 100), ConfirmReset);
            UiKit.Text(panel, Loc.T("credits"), 26, UiKit.Bottom, new Vector2(0, 140), new Vector2(1100, 60));
            UiKit.Button(panel, Loc.T("back"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(0, 30), new Vector2(340, 100), Close, "glyph_back");
            // Ad consent can be changed later where UMP requires it (EU/UK; privacy.html promises this row).
            if (Ads.AdMob.PrivacyOptionsRequired)
                UiKit.Button(panel, Loc.T("privacy"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(-420, 30), new Vector2(340, 100), Ads.AdMob.ShowPrivacyOptions);
        }

        static void Row(Transform panel, string label, float y) =>
            UiKit.Text(panel, label, 44, UiKit.Top, new Vector2(-400, y), new Vector2(420, 70), null, TMPro.TextAlignmentOptions.Left);

        void ConfirmReset()
        {
            var overlay = UiKit.Overlay(content, "Confirm");
            var panel = UiKit.Panel(overlay, new Vector2(860, 420)).transform;
            UiKit.Text(panel, Loc.T("reset_confirm"), 52, UiKit.Top, new Vector2(0, -50), new Vector2(780, 160));
            UiKit.Button(panel, Loc.T("yes"), ButtonStyle.Warm, UiKit.Bottom, new Vector2(-180, 50), new Vector2(300, 110), () =>
            {
                SaveSystem.ResetProgress();
                AudioManager.NotifyChanged();
                Rebuild();   // the sliders were bound to the old data; this also removes the confirm overlay
            });
            UiKit.Button(panel, Loc.T("no"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(180, 50), new Vector2(300, 110),
                () => Destroy(overlay.gameObject));
        }
    }
}

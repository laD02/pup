using SealCatapult.Core;
using UnityEngine;

namespace SealCatapult.UI
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
            // A disabled canvas still runs its children: the guitar pup's frames kept looping unseen behind the menu.
            if (content != null) Destroy(content.gameObject);
            if (popup != null) Destroy(popup);   // the stats card hangs on the canvas, not on the content
            content = null;
            popup = null;
        }

        float lastPreview;
        const float PreviewGap = 0.18f;   // seconds between the clicks that let a drag of the effects slider be heard

        /// <summary>Android Back: the card on top (stats, erase confirm, language picker) first, then Settings itself.</summary>
        public bool HandleBack()
        {
            if (!canvas.enabled) return false;
            if (popup != null) Destroy(popup);
            else Close();
            return true;
        }

        GameObject popup;   // the stats card, the erase confirmation or the language picker, while one is open
        const float LanguagePitch = 130f;
        /// <summary>Raised after "Erase progress": screens showing progress rebuild.</summary>
        public static event System.Action ProgressErased;
        /// <summary>Shows the About page; the screen that owns one sets it (the main menu). Null hides the button.</summary>
        public System.Action OnAbout;
        const float PageWidth = 1300f + 2f * 200f;   // the panel plus the cast standing against its side
        const float PageHeight = 820f + 140f;   // panel plus the ribbon above it

        void Rebuild()
        {
            if (popup != null) Destroy(popup);
            if (content != null) Destroy(content.gameObject);
            var overlay = UiKit.Overlay(canvas.transform, "Overlay");
            content = overlay;
            // Fitted to the canvas height like the info pages, so the ribbon overhanging the top stays on 21:9 phones.
            var holder = UiKit.Place(UiKit.Node(overlay, "Fit"), UiKit.Center, Vector2.zero, Vector2.zero);
            // Width too: the pup stands half outside the panel's left side and was cut off on 4:3.
            Vector2 view = UiKit.CanvasSize();
            float fit = Mathf.Min(1f, Mathf.Min(view.y / PageHeight, view.x / PageWidth));
            holder.localScale = new Vector3(fit, fit, 1f);
            var panel = UiKit.Panel(holder, new Vector2(1300, 820)).transform;
            panel.gameObject.AddComponent<PopIn>();
            UiKit.CloseButton(panel, Close);
            UiKit.Ribbon(panel, Loc.T("settings"), 760, 60);   // the same title ribbon as every popup (GAME_DESIGN.md §R10)
            Row(panel, "row_music", Loc.T("music"), -140);
            UiKit.Slider(panel, UiKit.Top, new Vector2(200, -150), new Vector2(560, 60), SaveSystem.Data.musicVolume, v =>
            {
                SaveSystem.Data.musicVolume = v;   // read Data each time: an erase replaces the object
                AudioManager.Instance.ApplyVolumes();
                AudioManager.NotifyChanged();
            });
            // Rows 130 px apart: every control's touch area is 130 tall, and at 110 they overlapped by 20.
            Row(panel, "row_sound", Loc.T("sfx"), -270);
            UiKit.Slider(panel, UiKit.Top, new Vector2(200, -280), new Vector2(560, 60), SaveSystem.Data.sfxVolume, v =>
            {
                SaveSystem.Data.sfxVolume = v;
                AudioManager.Instance.ApplyVolumes();
                AudioManager.NotifyChanged();
                if (Time.unscaledTime - lastPreview < PreviewGap) return;
                lastPreview = Time.unscaledTime;
                AudioManager.Sfx("click");   // the new level is heard while dragging
            });
            Row(panel, "row_globe", Loc.T("language"), -410);
            // Opens a picker (user 18/11: a tap that switched to the next language straight away was confusing).
            UiKit.Button(panel, Loc.T("lang_name"), ButtonStyle.Gold, UiKit.Top, new Vector2(200, -395), new Vector2(560, 100), PickLanguage);
            UiKit.Button(panel, Loc.T("stats"), ButtonStyle.Teal, UiKit.Top, new Vector2(-260, -525), new Vector2(460, 100),
                () => popup = StatsPanel.Show(canvas.transform), "row_chart");
            UiKit.Button(panel, Loc.T("reset"), ButtonStyle.Warm, UiKit.Top, new Vector2(260, -525), new Vector2(460, 100), ConfirmReset, "row_trash");
            // The pup on air guitar against the panel's left side (drawn for Settings, 18/11).
            UiKit.Cast(panel, "pup_modal_music", UiKit.BottomLeft, new Vector2(-40, 20), 360f, 2.4f);
            UiKit.Text(panel, Loc.T("credits"), 30, UiKit.Bottom, new Vector2(0, 134), new Vector2(1100, 54), UiKit.Muted).fontSizeMin = 24;   // 26 px was under 10sp
            UiKit.Button(panel, Loc.T("back"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(0, 30), new Vector2(340, 100), Close, "glyph_back");
            // About (version, credits) moved here from the old menu: Settings closes and the About page opens.
            if (OnAbout != null)
                UiKit.Button(panel, Loc.T("about"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(420, 30), new Vector2(340, 100), () => { Close(); OnAbout(); });
            // Ad consent can be changed later where UMP requires it (EU/UK; privacy.html promises this row).
            if (Ads.AdMob.PrivacyOptionsRequired)
                UiKit.Button(panel, Loc.T("privacy"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(-420, 30), new Vector2(340, 100), Ads.AdMob.ShowPrivacyOptions);
        }

        /// <summary>A setting's icon and label, clear of the ice panel's thick rim.</summary>
        static void Row(Transform panel, string icon, string label, float y)
        {
            UiKit.Image(panel, icon, UiKit.Top, new Vector2(-500, y), new Vector2(64, 64));
            UiKit.Text(panel, label, 44, UiKit.Top, new Vector2(-250, y), new Vector2(380, 70), null, TMPro.TextAlignmentOptions.Left);
        }

        /// <summary>Every language by its own name, two columns; the current one is the green button. A tap switches
        /// and closes; Back or the Back button closes without a change.</summary>
        void PickLanguage()
        {
            var overlay = UiKit.Overlay(content, "Languages");
            popup = overlay.gameObject;
            var panel = UiKit.Panel(overlay, new Vector2(1000, 800)).transform;
            panel.gameObject.AddComponent<PopIn>();
            UiKit.Ribbon(panel, Loc.T("language"), 640, 60);
            UiKit.CloseButton(panel, () => Destroy(overlay.gameObject));
            int rows = (Loc.Codes.Length + 1) / 2;
            for (int i = 0; i < Loc.Codes.Length; i++)
            {
                string code = Loc.Codes[i];
                bool current = code == Loc.Language;
                var pos = new Vector2(i % 2 == 0 ? -220 : 220, -130 - (i / 2) * LanguagePitch);   // rows a touch area apart
                if (i == Loc.Codes.Length - 1 && Loc.Codes.Length % 2 == 1) pos.x = 0f;   // the odd last one sits centred
                // The current one: the gold button AND a star on it (colour is never the only signal).
                UiKit.Button(panel, Loc.NameOf(code), current ? ButtonStyle.Gold : ButtonStyle.Teal, UiKit.Top, pos, new Vector2(400, 100), () =>
                {
                    if (code != Loc.Language) Loc.SetLanguage(code);
                    Rebuild();   // re-translates Settings and removes the picker
                }, current ? "star_full" : null);
            }
            UiKit.Button(panel, Loc.T("back"), ButtonStyle.Teal, UiKit.Top, new Vector2(0, -130 - rows * LanguagePitch - 15), new Vector2(340, 100),
                () => Destroy(overlay.gameObject), "glyph_back");
        }

        void ConfirmReset()
        {
            var overlay = UiKit.Overlay(content, "Confirm");
            popup = overlay.gameObject;
            var panel = UiKit.Panel(overlay, new Vector2(860, 480)).transform;
            panel.gameObject.AddComponent<PopIn>();
            UiKit.Ribbon(panel, Loc.T("reset"), 640, 60);   // every panel wears the title ribbon (GAME_DESIGN.md "Bảng")
            UiKit.CloseButton(panel, () => Destroy(overlay.gameObject));   // the same as "No"
            UiKit.Text(panel, Loc.T("reset_confirm"), 50, UiKit.Top, new Vector2(0, -110), new Vector2(780, 160));
            UiKit.Button(panel, Loc.T("yes"), ButtonStyle.Warm, UiKit.Bottom, new Vector2(-180, 50), new Vector2(300, 110), () =>
            {
                SaveSystem.ResetProgress();
                AudioManager.NotifyChanged();
                ProgressErased?.Invoke();
                Rebuild();   // the sliders were bound to the old data; this also removes the confirm overlay
            });
            UiKit.Button(panel, Loc.T("no"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(180, 50), new Vector2(300, 110),
                () => Destroy(overlay.gameObject));
        }
    }
}

using SealCatapult.Core;
using TMPro;
using UnityEngine;

namespace SealCatapult.UI
{
    /// <summary>"How to play" and "About" pages opened from the main menu: a full-screen card over the menu.</summary>
    public class InfoView : MonoBehaviour
    {
        Canvas canvas;
        Transform content;

        public static InfoView Create()
        {
            var canvas = UiKit.CreateCanvas("Info", 20);
            var view = canvas.gameObject.AddComponent<InfoView>();
            view.canvas = canvas;
            canvas.enabled = false;
            return view;
        }

        void Close() => canvas.enabled = false;

        /// <summary>Android Back: closes the page when one is open (the same as its Back button).</summary>
        public bool HandleBack()
        {
            if (!canvas.enabled) return false;
            Close();
            return true;
        }

        /// <summary>Panel plus the ribbon overhanging its top: on 20:9 and wider the canvas is shorter than this.</summary>
        const float PageHeight = 940f + 140f;

        Transform Open(string title, System.Action onContinue = null)
        {
            if (content != null) Destroy(content.gameObject);
            canvas.enabled = true;
            var overlay = UiKit.Overlay(canvas.transform, "Overlay");
            content = overlay;
            // A holder scaled to the canvas height keeps the ribbon on screen on wide phones (it was cut by a third on 21:9);
            // the panel inside keeps its own pop-in.
            var holder = UiKit.Place(UiKit.Node(overlay, "Fit"), UiKit.Center, Vector2.zero, Vector2.zero);
            float height = ((RectTransform)canvas.transform).rect.height;
            float fit = height > 1f ? Mathf.Min(1f, height / PageHeight) : 1f;   // no size yet: never scale the page to nothing
            holder.localScale = new Vector3(fit, fit, 1f);
            var panel = UiKit.Panel(holder, new Vector2(1600, 940)).transform;
            panel.gameObject.AddComponent<PopIn>();
            UiKit.Ribbon(panel, title, 860, 70);
            if (onContinue == null)
                UiKit.Button(panel, Loc.T("back"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(0, 30), new Vector2(360, 110), Close, "glyph_back");
            else
                UiKit.Button(panel, Loc.T("continue"), ButtonStyle.Gold, UiKit.BottomRight, new Vector2(-40, 30), new Vector2(380, 110),
                    () => { Close(); onContinue(); }, "glyph_next");
            return panel;
        }

        /// <summary>Four illustrated steps in a 2×2 grid: rope swing, escape, dive, stars. With `onContinue` it is the
        /// instructions card the reference shows after the first Play: Continue goes on to the map.</summary>
        public void ShowHowTo(System.Action onContinue = null)
        {
            var panel = Open(Loc.T("how_to"), onContinue);
            string[] icons = { "catapult_arm", "pup_ball", "bear_ko_0", "badge_perfect" };
            for (int i = 0; i < 4; i++)
            {
                var row = UiKit.Place(UiKit.Node(panel, "Step " + (i + 1)), UiKit.Top, new Vector2(i % 2 == 0 ? -390 : 390, i < 2 ? -230 : -520),
                    new Vector2(740, 260));
                row.gameObject.AddComponent<SlideIn>().delay = 0.08f * i;
                var badge = UiKit.Image(row, "ui_button_gold", new Vector2(0f, 0.5f), new Vector2(0, 0), new Vector2(84, 84));
                UiKit.Text(badge.transform, (i + 1).ToString(), 48, UiKit.Center, new Vector2(0, 4), new Vector2(84, 84), Color.white,
                    TextAlignmentOptions.Center, true);
                UiKit.Image(row, icons[i], new Vector2(0f, 0.5f), new Vector2(100, 0), new Vector2(170, 170));
                UiKit.Text(row, Loc.T("howto_" + (i + 1)), 32, new Vector2(1f, 0.5f), new Vector2(0, 0), new Vector2(450, 250), null,
                    TextAlignmentOptions.Left);
            }
        }

        public void ShowAbout()
        {
            var panel = Open(Loc.T("about"));
            UiKit.Logo(panel, Loc.T("title"), 110, UiKit.Top, new Vector2(0, -200), new Vector2(1000, 150));
            // Cast on the left (pup, whacker, bear), story on the right.
            var hero = UiKit.Image(panel, "pup", UiKit.Top, new Vector2(-560, -430), new Vector2(300, 300));
            hero.gameObject.AddComponent<Bob>().amplitude = 10f;
            UiKit.Image(panel, "catapult_base", UiKit.Top, new Vector2(-280, -430), new Vector2(360, 310));
            UiKit.Image(panel, "bear_helmet", UiKit.Top, new Vector2(-90, -480), new Vector2(160, 160));
            UiKit.Text(panel, Loc.T("about_text"), 38, UiKit.Top, new Vector2(390, -430), new Vector2(680, 300), null,
                TextAlignmentOptions.Left);
            UiKit.Text(panel, Loc.T("credits"), 30, UiKit.Top, new Vector2(0, -660), new Vector2(1300, 100), new Color32(0x56, 0x63, 0x7b, 0xff));
            UiKit.Text(panel, "v" + Application.version, 28, UiKit.Bottom, new Vector2(0, 150), new Vector2(400, 50), new Color32(0x56, 0x63, 0x7b, 0xff));
        }
    }
}

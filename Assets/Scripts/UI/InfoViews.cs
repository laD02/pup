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

        void Close()
        {
            canvas.enabled = false;
            // A disabled canvas still runs its children: the reading pup's frames kept looping unseen behind the menu.
            if (content != null) Destroy(content.gameObject);
            content = null;
        }

        /// <summary>Android Back: closes the page when one is open (the same as its Back button).</summary>
        public bool HandleBack()
        {
            if (!canvas.enabled) return false;
            Close();
            return true;
        }

        /// <summary>Panel plus the ribbon overhanging its top: on 20:9 and wider the canvas is shorter than this.</summary>
        const float PageHeight = 940f + 140f;
        const float PageWidth = 1600f + 2f * 200f;   // the panel plus the cast standing against its side

        Transform Open(string title, System.Action onContinue = null)
        {
            if (content != null) Destroy(content.gameObject);
            canvas.enabled = true;
            var overlay = UiKit.Overlay(canvas.transform, "Overlay");
            content = overlay;
            // A holder scaled to the canvas height keeps the ribbon on screen on wide phones (it was cut by a third on 21:9);
            // the panel inside keeps its own pop-in.
            var holder = UiKit.Place(UiKit.Node(overlay, "Fit"), UiKit.Center, Vector2.zero, Vector2.zero);
            // Width too: the reading pup stands half outside the panel's left side (it was cut off at 16:9, half gone on 4:3).
            Vector2 view = UiKit.CanvasSize();
            float fit = Mathf.Min(1f, Mathf.Min(view.y / PageHeight, view.x / PageWidth));
            holder.localScale = new Vector3(fit, fit, 1f);
            var panel = UiKit.Panel(holder, new Vector2(1600, 940)).transform;
            panel.gameObject.AddComponent<PopIn>();
            UiKit.Ribbon(panel, title, 860, 70);
            if (onContinue == null) UiKit.CloseButton(panel, Close);   // the first-play instructions card must go on with Continue
            if (onContinue == null)
                UiKit.Button(panel, Loc.T("back"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(0, 30), new Vector2(340, 110), Close, "glyph_back");
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
            // Redesign 18/11: four step cards in a row, each a drawn picture of the step over its text; the pup reading
            // the rules (upside down at first) against the panel's left side.
            for (int i = 0; i < 4; i++)
            {
                var card = UiKit.Place(UiKit.Node(panel, "Step " + (i + 1)), UiKit.Top, new Vector2(-555 + i * 370, -150), new Vector2(340, 560));
                card.gameObject.AddComponent<SlideIn>().delay = 0.08f * i;
                var bg = card.gameObject.AddComponent<UnityEngine.UI.Image>();
                bg.sprite = Art.Get("ui_row");
                bg.type = UnityEngine.UI.Image.Type.Sliced;
                bg.raycastTarget = false;
                UiKit.Image(card, "step_" + (i + 1), UiKit.Top, new Vector2(0, -20), new Vector2(280, 256));
                // The step number on an ice tile (the gold pill squeezed into a square read as a stub, QA 18/11).
                var badge = UiKit.Image(card, "mark_box", UiKit.TopLeft, new Vector2(-14, 18), new Vector2(84, 84));
                UiKit.Text(badge.transform, (i + 1).ToString(), 50, UiKit.Center, new Vector2(0, 4), new Vector2(84, 84), Color.white,
                    TextAlignmentOptions.Center, true);
                var text = UiKit.Text(card, Loc.T("howto_" + (i + 1)), 32, UiKit.Top, new Vector2(0, -284), new Vector2(316, 268), null,
                    TextAlignmentOptions.Top);
                text.enableAutoSizing = true;
                text.fontSizeMin = 25;   // 20 px was about 7sp on a phone: the longest step (German, step 3) still fits at 25
                text.fontSizeMax = 32;
            }
            UiKit.Cast(panel, "pup_modal_read", UiKit.BottomLeft, new Vector2(-50, 20), 360f, 4f);
        }

        public void ShowAbout()
        {
            var panel = Open(Loc.T("about"));
            UiKit.Brand(panel, UiKit.Top, new Vector2(0, -90), new Vector2(700, 280));
            // Cast on the left (pup, whacker, bear), story on the right.
            // The drawn modal cast, alive like on every other panel (they were three still sprites): the cheering pup,
            // the catapult, the bear pointing back at it. Feet on one line; the old sprites when the frames are missing.
            const float Feet = -645f;   // above the credits line (at -735 the bear stood on the text, emulator 20/11)
            if (UiKit.Cast(panel, "pup_modal_win", UiKit.Top, new Vector2(-590, Feet), 215f, 3.2f) == null)
                UiKit.Image(panel, "pup", UiKit.Top, new Vector2(-560, -430), new Vector2(300, 300));
            UiKit.Image(panel, "catapult_base", UiKit.Top, new Vector2(-345, -400), new Vector2(330, 290));
            if (UiKit.Cast(panel, "bear_modal_mock", UiKit.Top, new Vector2(-120, Feet), 215f, 1.6f, true) == null)
                UiKit.Image(panel, "bear_helmet", UiKit.Top, new Vector2(-90, -480), new Vector2(160, 160));
            UiKit.Text(panel, Loc.T("about_text"), 38, UiKit.Top, new Vector2(390, -430), new Vector2(680, 300), null,
                TextAlignmentOptions.Left);
            UiKit.Text(panel, Loc.T("credits"), 30, UiKit.Top, new Vector2(0, -660), new Vector2(1300, 100), UiKit.Muted);
            UiKit.Text(panel, "v" + Application.version, 30, UiKit.Bottom, new Vector2(0, 150), new Vector2(400, 50), UiKit.Muted);
        }
    }
}

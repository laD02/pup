using SealCatapult.Core;
using UnityEngine;

namespace SealCatapult.UI
{
    /// <summary>Statistics card (GDD §R5): totals kept in the save, shown over whatever screen opened it.</summary>
    public static class StatsPanel
    {
        public static GameObject Show(Transform canvas)
        {
            var data = SaveSystem.Data;
            var overlay = UiKit.Overlay(canvas, "Stats");
            var panel = UiKit.Panel(overlay, new Vector2(1000, 700)).transform;
            panel.gameObject.AddComponent<PopIn>();
            UiKit.Ribbon(panel, Loc.T("stats"), 720, 60);
            UiKit.CloseButton(panel, () => Object.Destroy(overlay.gameObject));
            (string icon, string key, int value)[] rows =
            {
                ("row_bear", "stats_total", data.totalBears),
                ("row_paw1", "stats_x2", data.combo2),
                ("row_paw2", "stats_x3", data.combo3),
                ("row_paw3", "stats_x4", data.combo4),
                ("row_star", "stats_perfect", data.PerfectCount()),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                var row = UiKit.ListRow(panel, rows[i].icon, Loc.T(rows[i].key), -140 - i * 82, 820f, 70f);
                row.gameObject.AddComponent<SlideIn>().delay = 0.06f * i;
                UiKit.Text(row, rows[i].value.ToString(), 44, new Vector2(1f, 0.5f), new Vector2(-30, 2), new Vector2(160, 64),
                    UiKit.Accent, TMPro.TextAlignmentOptions.Right, true);   // the same blue as the result card's numbers
            }
            // The pup polishing its trophy, against the panel's left side (drawn for Stats, 18/11).
            UiKit.Cast(panel, "pup_modal_trophy", UiKit.BottomLeft, new Vector2(-40, 20), 340f, 3.2f);
            UiKit.Button(panel, Loc.T("back"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(0, 40), new Vector2(340, 110),
                () => Object.Destroy(overlay.gameObject), "glyph_back");
            return overlay.gameObject;
        }
    }
}

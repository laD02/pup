using PufferLaunch.Core;
using UnityEngine;

namespace PufferLaunch.UI
{
    /// <summary>Statistics card (GDD §R5): totals kept in the save, shown over whatever screen opened it.</summary>
    public static class StatsPanel
    {
        public static GameObject Show(Transform canvas)
        {
            var data = SaveSystem.Data;
            var overlay = UiKit.Overlay(canvas, "Stats");
            var panel = UiKit.Panel(overlay, new Vector2(1000, 640)).transform;
            panel.gameObject.AddComponent<PopIn>();
            var ribbon = UiKit.Image(panel, "ribbon", UiKit.Top, new Vector2(0, 60), new Vector2(720, 180));
            UiKit.Text(ribbon.transform, Loc.T("stats"), 60, UiKit.Center, new Vector2(0, 8), new Vector2(560, 100),
                Color.white, TMPro.TextAlignmentOptions.Center, true);
            (string key, int value)[] rows =
            {
                ("stats_total", data.totalBears),
                ("stats_x2", data.combo2),
                ("stats_x3", data.combo3),
                ("stats_x4", data.combo4),
                ("stats_perfect", data.PerfectCount()),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                float y = -150 - i * 72;
                UiKit.Text(panel, Loc.T(rows[i].key), 38, UiKit.Top, new Vector2(-120, y), new Vector2(620, 60), UiKit.Ink,
                    TMPro.TextAlignmentOptions.Left);
                UiKit.Text(panel, rows[i].value.ToString(), 44, UiKit.Top, new Vector2(330, y), new Vector2(200, 60),
                    new Color32(0xd9, 0x53, 0x2a, 0xff), TMPro.TextAlignmentOptions.Right);
            }
            UiKit.Button(panel, Loc.T("back"), ButtonStyle.Teal, UiKit.Bottom, new Vector2(0, 40), new Vector2(280, 112),
                () => Object.Destroy(overlay.gameObject), "glyph_back");
            return overlay.gameObject;
        }
    }
}

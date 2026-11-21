using System.Collections;
using SealCatapult.UI;
using TMPro;
using UnityEngine;

namespace SealCatapult.Core
{
    /// <summary>Boot: the loading screen (painting, brand logo, ice progress bar), load the save, warm up audio, then the
    /// main menu (about 2 s).</summary>
    public class BootView : MonoBehaviour
    {
        const float LogoTime = 1.8f;
        const float BarWidth = 820f, BarMin = 130f;   // the fill is a sliced pill: narrower than its two end caps it would crumple
        /// <summary>Frame caps: 60 while playing; still screens (the map and its shop) idle at 30 (heat, battery).</summary>
        public const int GameFps = 60, StillFps = 30;

        IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;   // vSync would override the cap and render 90/120 fps on fast panels (heat, battery)
            Application.targetFrameRate = GameFps;
            SaveSystem.Load();
            Ads.AdMob.Create();   // consent form (if required) shows over the logo, then ads preload in the background
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.gameObject.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x11, 0x28, 0x5a, 0xff);
            var canvas = UiKit.CreateCanvas("Boot");
            // Loading screen (redesign 18/11: "sửa luôn cả trang loading khi vào game cho thật đẹp và chuyên nghiệp"): the
            // home painting behind a dark veil, the brand logo, and an ice progress bar filling under "Loading…".
            UiKit.PaintedBackdrop(canvas.transform, 0.45f);
            UiKit.Brand(canvas.transform, UiKit.Center, new Vector2(0, 110), new Vector2(1100, 600)).gameObject.AddComponent<Bob>().pulse = true;
            UiKit.Text(canvas.transform, Loc.T("loading"), 60, UiKit.Bottom, new Vector2(0, 150), new Vector2(900, 80), Color.white,
                TextAlignmentOptions.Center, true);
            var track = UiKit.Image(canvas.transform, "ui_row", UiKit.Bottom, new Vector2(0, 80), new Vector2(BarWidth, 56));
            track.type = UnityEngine.UI.Image.Type.Sliced;
            track.preserveAspect = false;
            var fill = UiKit.Image(track.transform, "ui_button_gold", new Vector2(0f, 0.5f), new Vector2(8, 0), new Vector2(BarMin, 40));
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            fill.preserveAspect = false;
            AudioManager.Instance.ApplyVolumes();
            for (float t = 0f; t < LogoTime; t += Time.unscaledDeltaTime)
            {
                // Eases out: quick at first, settling as it nears the end.
                float k = 1f - Mathf.Pow(1f - t / LogoTime, 2f);
                fill.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(BarMin, BarWidth - 16f, k), 40f);
                yield return null;
            }
            fill.rectTransform.sizeDelta = new Vector2(BarWidth - 16f, 40f);
            SceneLoader.Load(SceneLoader.MainMenu);
        }
    }
}

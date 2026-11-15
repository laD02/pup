using System.Collections;
using PufferLaunch.UI;
using TMPro;
using UnityEngine;

namespace PufferLaunch.Core
{
    /// <summary>Boot: logo, load the save, warm up audio, then the main menu (at most ~2 s).</summary>
    public class BootView : MonoBehaviour
    {
        const float LogoTime = 1.4f;
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
            var logo = UiKit.Image(canvas.transform, "pup_ball", UiKit.Center, new Vector2(0, 80), new Vector2(320, 320));
            logo.gameObject.AddComponent<Bob>().pulse = true;
            UiKit.Logo(canvas.transform, Loc.T("title"), 120, UiKit.Center, new Vector2(0, -170), new Vector2(1400, 170));
            AudioManager.Instance.ApplyVolumes();
            yield return new WaitForSeconds(LogoTime);
            SceneLoader.Load(SceneLoader.MainMenu);
        }
    }
}

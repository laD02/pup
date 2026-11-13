using System.Collections.Generic;
using UnityEngine;

namespace PufferLaunch.Core
{
    /// <summary>
    /// Polar backdrop: night-to-dawn sky and aurora locked to the camera, infinitely tiled parallax layers
    /// (stars + mountains, open sea with icebergs, snow-capped ice shelf) and gentle snowfall.
    /// </summary>
    public class Backdrop : MonoBehaviour
    {
        /// <summary>Top of the ice shelf: everything stands on this line.</summary>
        public const float GroundY = -4.5f;


        class Layer
        {
            public float factor;
            public float tileWidth;
            public readonly List<Transform> tiles = new List<Transform>();
        }

        readonly List<Layer> layers = new List<Layer>();
        readonly List<SpriteRenderer> auroras = new List<SpriteRenderer>();
        Camera cam;
        SpriteRenderer sky;
        SpriteRenderer vignette;
        Vector2 skySize = Vector2.one, vignetteSize = Vector2.one;   // sprite bounds, read once
        Vector3 lastCam = new Vector3(float.NaN, 0f, 0f);
        float lastHalfW, lastHalfH;

        /// <summary>skyOnly: just the night sky, aurora and far mountains (a level's two painted stages cover the rest;
        /// the ice shelf and drifts would draw over the arena painting).</summary>
        public static Backdrop Create(Camera cam, bool skyOnly = false)
        {
            var go = new GameObject("Backdrop");
            var bg = go.AddComponent<Backdrop>();
            bg.cam = cam;
            bg.Build(skyOnly);
            return bg;
        }

        void Build(bool skyOnly)
        {
            sky = Art.Spawn("bg_sky", transform, Vector2.zero, 1f, -100);
            for (int i = 0; i < 2; i++)
            {
                var a = Art.Spawn("aurora", transform, Vector2.zero, 18f, -95);
                a.color = new Color(1f, 1f, 1f, 0.6f);
                auroras.Add(a);
            }
            // y values put each picture's key line in place: mountain base, sea horizon, snow line (see Tools/make_art.py).
            AddLayer("bg_far", 0.6f, 30f, GroundY + 8.5f, -80);
            vignette = Art.Spawn("vignette", transform, Vector2.zero, 1f, 90);
            if (sky.sprite != null) skySize = sky.sprite.bounds.size;
            if (vignette.sprite != null) vignetteSize = vignette.sprite.bounds.size;
            if (skyOnly) return;
            AddLayer("bg_mid", 0.3f, 26f, GroundY + 2.24f, -60);
            AddLayer("ground", 0f, 12f, GroundY - 1.03f, -40);
            AddLayer("ground_fill", 0f, 12f, GroundY - 8.45f, -41);
            // Foreground drifts move faster than the world for depth; they sit below the playfield.
            AddLayer("fg_snow", 1.2f, 14f, GroundY - 1.15f, 50);
        }

        void AddLayer(string sprite, float factor, float width, float y, int order)
        {
            var layer = new Layer { factor = factor, tileWidth = width };
            for (int i = 0; i < 6; i++)
            {
                var sr = Art.Spawn(sprite, transform, new Vector2(0, y), width, order);
                layer.tiles.Add(sr.transform);
            }
            layers.Add(layer);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Vector2 c = cam.transform.position;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            // The aurora drifts on its own; everything else only moves with the camera.
            bool camMoved = (Vector3)c != lastCam || halfW != lastHalfW || halfH != lastHalfH;
            lastCam = c;
            lastHalfW = halfW;
            lastHalfH = halfH;

            if (camMoved)
            {
                sky.transform.position = new Vector3(c.x, c.y, 10f);
                sky.transform.localScale = new Vector3(halfW * 3f / skySize.x, halfH * 2.4f / skySize.y, 1f);
                vignette.transform.position = new Vector3(c.x, c.y, 0f);
                vignette.transform.localScale = new Vector3(halfW * 2.05f / vignetteSize.x, halfH * 2.05f / vignetteSize.y, 1f);
            }

            for (int i = 0; i < auroras.Count; i++)
            {
                var a = auroras[i];
                float x = c.x * 0.9f + (i == 0 ? -6f : 7f) + Mathf.Sin(Time.time * 0.07f + i * 2f) * 2f;
                a.transform.position = new Vector3(x, c.y + halfH - 3.2f - i * 0.8f, 8f);
                float w = a.transform.localScale.x;
                a.transform.localScale = new Vector3(w, w * (0.9f + 0.15f * Mathf.Sin(Time.time * 0.4f + i)), 1f);
                var col = a.color;
                col.a = 0.45f + 0.25f * Mathf.Sin(Time.time * 0.5f + i * 1.7f);
                a.color = col;
            }

            if (!camMoved) return;
            foreach (var layer in layers)
            {
                float baseX = c.x * layer.factor;
                float start = c.x - halfW - layer.tileWidth;
                int k0 = Mathf.FloorToInt((start - baseX) / layer.tileWidth);
                for (int i = 0; i < layer.tiles.Count; i++)
                {
                    var t = layer.tiles[i];
                    t.position = new Vector3(baseX + (k0 + i + 0.5f) * layer.tileWidth, t.position.y, 0f);
                }
            }

        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace PufferLaunch.Core
{
    /// <summary>Sprites by file name from Resources/Art. Swap a PNG, keep the name, and the game updates.</summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out var sprite)) return sprite;
            sprite = Resources.Load<Sprite>("Art/" + name);
            if (sprite == null) Debug.LogWarning($"[PufferLaunch] Missing sprite Resources/Art/{name}");
            cache[name] = sprite;
            return sprite;
        }

        /// <summary>True when the PNG exists (no warning): for optional frame sets.</summary>
        public static bool Has(string name)
        {
            if (cache.TryGetValue(name, out var s)) return s != null;
            var sprite = Resources.Load<Sprite>("Art/" + name);
            cache[name] = sprite;   // misses are cached too: UI rebuilds probe optional icons often
            return sprite != null;
        }

        /// <summary>Creates a sprite object sized in world units regardless of the PNG resolution.</summary>
        public static SpriteRenderer Spawn(string name, Transform parent, Vector2 position, float width, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Get(name);
            sr.sortingOrder = order;
            FitWidth(sr, width);
            return sr;
        }

        public static void FitWidth(SpriteRenderer sr, float width)
        {
            if (sr.sprite == null) return;
            float scale = width / sr.sprite.bounds.size.x;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}

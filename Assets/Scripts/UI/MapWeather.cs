using SealCatapult.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SealCatapult.UI
{
    /// <summary>
    /// What moves in the air over a castle's map, picked to fit its scenery (user 17/11: "ở các map hãy thêm các thứ sinh
    /// động phù hợp với phong cảnh thành … hiện tại hơi tĩnh"): snow over the bay, embers over the lava, petals over the
    /// blossom isles, leaves on the tundra wind, a blizzard and lightning at the throne, glints on crystals and water,
    /// the aurora over the forest. Everything is pooled (nothing allocated after Start) and lives on its own nested
    /// Canvas, so the moving flakes never make the map's two hundred stone graphics rebuild.
    /// </summary>
    public class MapWeather : MonoBehaviour
    {
        /// <summary>One world's air. velocity in canvas px/s (y up); a zero count turns that part off.</summary>
        readonly struct Air
        {
            public readonly string sprite;
            public readonly Color tint, glint;
            public readonly int count, glints;
            public readonly Vector2 velocity, size;   // size: smallest and largest, px
            public readonly float sway, flat;         // sideways swing in px; flat < 1: the sprite is a petal or leaf and tumbles
            public readonly bool lightning, aurora;

            public Air(string sprite, Color tint, int count, Vector2 velocity, Vector2 size, float sway, float flat,
                int glints, Color glint, bool lightning = false, bool aurora = false)
            {
                this.sprite = sprite; this.tint = tint; this.count = count; this.velocity = velocity; this.size = size;
                this.sway = sway; this.flat = flat; this.glints = glints; this.glint = glint;
                this.lightning = lightning; this.aurora = aurora;
            }
        }

        static readonly Color Snow = Color.white;

        static Air Of(string world)
        {
            switch (world)
            {
                case "canyon": return new Air("snowflake", new Color(0.95f, 0.88f, 1f, 1f), 20, new Vector2(-18, -60), new Vector2(26, 46), 22, 1f, 4, new Color(1f, 0.8f, 1f));
                case "forest": return new Air("snowflake", Snow, 26, new Vector2(12, -80), new Vector2(26, 48), 26, 1f, 0, Color.white, aurora: true);
                case "harbour": return new Air("snowflake", new Color(1f, 0.95f, 0.85f, 0.95f), 10, new Vector2(-10, -45), new Vector2(24, 40), 18, 1f, 6, new Color(1f, 0.9f, 0.55f));   // sun glitter
                case "caves": return new Air("powder", new Color(0.6f, 1f, 1f, 0.9f), 18, new Vector2(4, 22), new Vector2(18, 32), 30, 1f, 6, new Color(0.7f, 1f, 1f));   // glowing motes drift up
                case "volcano": return new Air("spark", new Color(1f, 0.75f, 0.25f, 1f), 26, new Vector2(18, 95), new Vector2(24, 44), 28, 1f, 0, Color.white);   // embers rise
                case "blossom": return new Air("petal", Color.white, 22, new Vector2(-48, -55), new Vector2(26, 40), 40, 0.99f, 0, Color.white);   // petals
                case "palace": return new Air("snowflake", new Color(0.85f, 0.9f, 1f, 0.95f), 14, new Vector2(-8, -40), new Vector2(24, 42), 20, 1f, 7, new Color(1f, 0.95f, 0.7f));   // stars and lantern light
                case "tundra": return new Air("leaf", Color.white, 18, new Vector2(-120, -28), new Vector2(28, 42), 34, 0.99f, 0, Color.white);   // leaves on the wind
                case "throne": return new Air("snowflake", new Color(0.9f, 0.93f, 1f, 1f), 36, new Vector2(-170, -150), new Vector2(22, 42), 14, 1f, 0, Color.white, lightning: true);   // blizzard
                default: return new Air("snowflake", Snow, 24, new Vector2(-20, -70), new Vector2(26, 48), 24, 1f, 4, new Color(0.85f, 0.97f, 1f));   // Frost Bay
            }
        }

        const float GlintMin = 0.8f, GlintMax = 1.5f, GlintSize = 46f;
        const float FlashMin = 5f, FlashMax = 9f;

        Air air;
        RectTransform area;
        RectTransform[] flakes, glints;
        Image[] glintImages;
        Vector2[] home;        // a flake's own drift position, before the sway
        float[] phase, glintBorn, glintLife;
        Image flash, aurora;
        float nextFlash;

        /// <summary>Adds the world's air over <paramref name="over"/> (the map painting), under the screen's buttons.</summary>
        public static void Add(Transform over, string world)
        {
            var node = UiKit.Stretch(UiKit.Node(over, "Weather"));
            node.gameObject.AddComponent<Canvas>();   // nested: its rebuilds stay its own
            node.gameObject.AddComponent<MapWeather>().air = Of(world);
        }

        void Start()
        {
            area = (RectTransform)transform;
            if (air.aurora && Art.Has("aurora"))
            {
                aurora = UiKit.Image(transform, "aurora", UiKit.Top, new Vector2(0, -40), new Vector2(1700, 300));
                aurora.raycastTarget = false;
            }
            int count = Art.Has(air.sprite) ? air.count : 0;
            flakes = new RectTransform[count];
            home = new Vector2[count];
            phase = new float[count];
            for (int i = 0; i < count; i++)
            {
                float s = Random.Range(air.size.x, air.size.y);
                var image = UiKit.Image(transform, air.sprite, UiKit.Center, Vector2.zero, new Vector2(s, s * air.flat));
                image.raycastTarget = false;
                image.color = air.tint;
                flakes[i] = image.rectTransform;
                phase[i] = Random.Range(0f, 6.28f);
            }
            int g = Art.Has("twinkle") ? air.glints : 0;
            glints = new RectTransform[g];
            glintImages = new Image[g];
            glintBorn = new float[g];
            glintLife = new float[g];
            for (int i = 0; i < g; i++)
            {
                glintImages[i] = UiKit.Image(transform, "twinkle", UiKit.Center, Vector2.zero, new Vector2(GlintSize, GlintSize));
                glintImages[i].raycastTarget = false;
                glints[i] = glintImages[i].rectTransform;
            }
            if (air.lightning)
            {
                flash = UiKit.Image(transform, "powder", UiKit.Center, Vector2.zero, Vector2.zero);
                UiKit.Stretch(flash.rectTransform);
                flash.sprite = null;   // a plain white sheet
                flash.raycastTarget = false;
                flash.color = Color.clear;
                nextFlash = Time.unscaledTime + Random.Range(2f, FlashMin);
            }
        }

        void Reglint(int i, float age)
        {
            var size = area.rect.size;
            glints[i].anchoredPosition = new Vector2(Random.Range(-0.46f, 0.46f) * size.x, Random.Range(-0.42f, 0.36f) * size.y);
            glintLife[i] = Random.Range(GlintMin, GlintMax);
            glintBorn[i] = Time.unscaledTime - age;
        }

        bool scattered;

        /// <summary>Spreads the flakes and glints over the map once layout has given it a size (in Start the rect is
        /// still empty: everything sat on one point and drifted off as a clump).</summary>
        bool Scatter()
        {
            var size = area.rect.size;
            if (size.x < 1f || size.y < 1f) return false;
            for (int i = 0; i < flakes.Length; i++)
                home[i] = new Vector2(Random.Range(-0.5f, 0.5f) * size.x, Random.Range(-0.5f, 0.5f) * size.y);
            for (int i = 0; i < glints.Length; i++) Reglint(i, Random.Range(0f, GlintMin));   // staggered, not all at once
            return true;
        }

        void Update()
        {
            if (!scattered && !(scattered = Scatter())) return;
            float now = Time.unscaledTime, dt = Time.unscaledDeltaTime;
            var half = area.rect.size * 0.5f + new Vector2(30, 30);
            for (int i = 0; i < flakes.Length; i++)
            {
                var p = home[i] + air.velocity * dt;
                // Off one edge: back in from the opposite one.
                if (p.x < -half.x) p.x += 2f * half.x; else if (p.x > half.x) p.x -= 2f * half.x;
                if (p.y < -half.y) p.y += 2f * half.y; else if (p.y > half.y) p.y -= 2f * half.y;
                home[i] = p;
                float swing = Mathf.Sin(now * 1.3f + phase[i]);
                flakes[i].anchoredPosition = p + new Vector2(swing * air.sway, 0f);
                if (air.flat < 1f) flakes[i].localRotation = Quaternion.Euler(0f, 0f, swing * 70f + phase[i] * 57f);   // a petal tumbles
            }
            for (int i = 0; i < glints.Length; i++)
            {
                float k = (now - glintBorn[i]) / glintLife[i];
                if (k >= 1f)
                {
                    Reglint(i, 0f);
                    k = 0f;
                }
                float beat = Mathf.Sin(k * Mathf.PI);   // in and out
                glints[i].localScale = Vector3.one * beat;
                glintImages[i].color = new Color(air.glint.r, air.glint.g, air.glint.b, beat);
            }
            if (aurora != null) aurora.color = new Color(0.6f, 1f, 0.8f, 0.3f + 0.2f * Mathf.Sin(now * 0.5f));
            if (flash == null) return;
            // Lightning: two quick flickers, then dark for a while.
            float since = now - nextFlash;
            float glow = since < 0f ? 0f : since < 0.08f ? 0.16f : since < 0.16f ? 0f : since < 0.3f ? 0.11f * (1f - (since - 0.16f) / 0.14f) : 0f;
            if (since > 0.3f) nextFlash = now + Random.Range(FlashMin, FlashMax);
            if (!Mathf.Approximately(flash.color.a, glow)) flash.color = new Color(0.9f, 0.92f, 1f, glow);
        }
    }
}

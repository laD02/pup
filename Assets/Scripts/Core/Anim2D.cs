using UnityEngine;

namespace PufferLaunch.Core
{
    /// <summary>
    /// Lightweight character animation for a SpriteRenderer: blink/taunt (swap to an alternate frame now and then),
    /// flap (alternate two frames continuously) and breathe (gentle squash). Put it on the sprite's own object, so
    /// physics and gameplay scripts on a parent can keep moving and scaling freely.
    /// </summary>
    public class Anim2D : MonoBehaviour
    {
        public enum Mode { Blink, Flap }

        public Mode mode = Mode.Blink;
        public float minInterval = 1.8f;
        public float maxInterval = 4.2f;
        public float altDuration = 0.13f;
        public float flapFps = 7f;
        public float breatheAmount = 0.03f;
        public float breatheSpeed = 2.2f;

        SpriteRenderer sr;
        Sprite main;
        Sprite alt;
        Vector3 baseScale;
        float timer;
        float phase;
        bool showingAlt;
        bool started;

        /// <summary>Adds the animator to a sprite. altSprite is the blink/taunt/flap frame name in Resources/Art.</summary>
        public static Anim2D Attach(SpriteRenderer target, string mainSprite, string altSprite, Mode mode = Mode.Blink)
        {
            var a = target.gameObject.AddComponent<Anim2D>();
            a.mode = mode;
            a.SetFrames(mainSprite, altSprite);
            return a;
        }

        public void SetFrames(string mainSprite, string altSprite)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            main = Art.Get(mainSprite);
            alt = string.IsNullOrEmpty(altSprite) ? null : Art.Get(altSprite);
            sr.sprite = main;
            showingAlt = false;
        }

        void Start()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            phase = Random.value * 10f;
            timer = Random.Range(minInterval, maxInterval);
            started = true;
        }

        void Update()
        {
            if (sr == null) return;
            if (mode == Mode.Flap && alt != null)
            {
                bool up = Mathf.FloorToInt((Time.time + phase) * flapFps) % 2 == 0;
                var want = up ? alt : main;
                if (sr.sprite != want) sr.sprite = want;
            }
            else if (alt != null)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    showingAlt = !showingAlt;
                    sr.sprite = showingAlt ? alt : main;
                    timer = showingAlt ? altDuration : Random.Range(minInterval, maxInterval);
                }
            }
            if (breatheAmount > 0f)
            {
                float b = Mathf.Sin((Time.time + phase) * breatheSpeed) * breatheAmount;
                transform.localScale = new Vector3(baseScale.x * (1f - b * 0.5f), baseScale.y * (1f + b), baseScale.z);
            }
        }

        /// <summary>Call after resizing the sprite (Art.FitWidth) so breathing scales around the new size.</summary>
        public void Rebase()
        {
            if (started) baseScale = transform.localScale;
        }

        /// <summary>Stops frame swaps and breathing (e.g. a stunned or defeated character).</summary>
        public void Freeze()
        {
            if (started) transform.localScale = baseScale;
            enabled = false;
        }
    }
}

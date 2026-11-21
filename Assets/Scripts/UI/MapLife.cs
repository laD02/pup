using SealCatapult.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SealCatapult.UI
{
    /// <summary>
    /// Life on the level map, as in CPC (video study 17/11, GAME_DESIGN "Bản đồ sống động"): every few seconds a fish
    /// leaps out of open water in an arc and leaves a ripple ring. One fish and two rings are reused, so nothing is
    /// allocated per leap; the map keeps its idle frame rate.
    /// </summary>
    public class MapLife : MonoBehaviour
    {
        const float MinWait = 2.5f, MaxWait = 5.5f;   // seconds between leaps
        const float Leap = 0.75f, Height = 54f, Reach = 46f, RingTime = 0.6f;
        static readonly Vector2 FishSize = new Vector2(44, 44), RingSize = new Vector2(70, 34);

        Vector2[] spots;   // anchors on the painting (0..1)
        RectTransform fish, ringOut, ringIn;
        Image ringOutImage, ringInImage;

        public static void Add(Transform trail, Vector2[] water, float paintingW, float paintingH)
        {
            if (water == null || water.Length == 0 || !Art.Has("fish")) return;
            var life = trail.gameObject.AddComponent<MapLife>();
            life.spots = new Vector2[water.Length];
            for (int i = 0; i < water.Length; i++) life.spots[i] = new Vector2(water[i].x / paintingW, 1f - water[i].y / paintingH);
            life.ringOutImage = Part(trail, Art.Has("impact_ring") ? "impact_ring" : "node_glow", RingSize);
            life.ringInImage = Part(trail, Art.Has("impact_ring") ? "impact_ring" : "node_glow", RingSize);
            life.ringOut = life.ringOutImage.rectTransform;
            life.ringIn = life.ringInImage.rectTransform;
            life.fish = Part(trail, "fish", FishSize).rectTransform;
        }

        static Image Part(Transform parent, string sprite, Vector2 size)
        {
            var image = UiKit.Image(parent, sprite, UiKit.Center, Vector2.zero, size);
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
            image.transform.SetAsFirstSibling();   // under the stones and the hero
            return image;
        }

        System.Collections.IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(MinWait, MaxWait));
                var spot = spots[Random.Range(0, spots.Length)];
                float dir = Random.value < 0.5f ? -1f : 1f;
                Place(fish, spot);
                Place(ringOut, spot);
                Place(ringIn, spot);
                var from = new Vector2(-dir * Reach * 0.5f, 0f);
                var to = new Vector2(dir * Reach * 0.5f, 0f);
                fish.localScale = new Vector3(dir, 1f, 1f);
                fish.gameObject.SetActive(true);
                ringOut.gameObject.SetActive(true);
                ringIn.gameObject.SetActive(false);
                for (float t = 0f; t < Leap; t += Time.unscaledDeltaTime)
                {
                    float k = t / Leap;
                    fish.anchoredPosition = Vector2.Lerp(from, to, k) + Vector2.up * Height * Mathf.Sin(k * Mathf.PI);
                    fish.localRotation = Quaternion.Euler(0f, 0f, dir * Mathf.Lerp(50f, -50f, k));   // nose up, then nose down
                    Ring(ringOut, ringOutImage, from, t / RingTime);
                    yield return null;
                }
                fish.gameObject.SetActive(false);
                ringOut.gameObject.SetActive(false);
                ringIn.gameObject.SetActive(true);
                for (float t = 0f; t < RingTime; t += Time.unscaledDeltaTime)
                {
                    Ring(ringIn, ringInImage, to, t / RingTime);
                    yield return null;
                }
                ringIn.gameObject.SetActive(false);
            }
        }

        static void Place(RectTransform part, Vector2 anchor)
        {
            part.anchorMin = part.anchorMax = anchor;
            part.anchoredPosition = Vector2.zero;
        }

        /// <summary>A ripple: grows and fades over k = 0..1.</summary>
        static void Ring(RectTransform ring, Image image, Vector2 at, float k)
        {
            k = Mathf.Clamp01(k);
            ring.anchoredPosition = at;
            ring.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, k);
            image.color = new Color(1f, 1f, 1f, 0.8f * (1f - k));
        }
    }

    /// <summary>
    /// A polar bear standing by the trail, as on CPC's map: it breathes, scratches, now and then turns to look the other
    /// way. Placed by <see cref="Add"/> on the painting points from WorldTheme.Bears (Tools/map_trail.py bear_spots).
    /// </summary>
    public class MapBear : MonoBehaviour
    {
        const float MinTurn = 2.5f, MaxTurn = 5f;   // seconds between looking left and right
        static readonly Vector2 Size = new Vector2(122, 122);   // as tall as the walking hero (MapView: Stone 64 × 1.9), user 19/11
        float nextTurn;

        public static void Add(Transform trail, Vector2[] spots, float paintingW, float paintingH)
        {
            if (spots == null || !Art.Has("bear_idle_3")) return;
            foreach (var spot in spots)
            {
                var node = UiKit.Node(trail, "Map Bear");
                node.anchorMin = node.anchorMax = new Vector2(spot.x / paintingW, 1f - spot.y / paintingH);
                node.sizeDelta = Vector2.zero;
                node.gameObject.AddComponent<Canvas>();   // its own canvas: its frames do not re-batch the whole map
                var bear = UiKit.Image(node, "bear_idle_0", UiKit.Bottom, Vector2.zero, Size);
                bear.rectTransform.pivot = new Vector2(0.5f, 0f);   // feet on the spot
                bear.raycastTarget = false;
                UiKit.StandOut(bear, Vector2.zero, Size.x * 0.7f);   // stands out from the painting (user 19/11)
                // Breathing most of the time, a scratch now and then (CPC's bear sniffs and looks about).
                bear.gameObject.AddComponent<FrameLoop>().Set(5f, ("bear_idle_", 4), ("bear_idle_", 4), ("bear_idle_", 4), ("bear_scratch_", 4));
                var turner = bear.gameObject.AddComponent<MapBear>();
                turner.nextTurn = Time.unscaledTime + Random.Range(0f, MaxTurn);
                node.SetAsFirstSibling();   // under the stones and the hero
            }
        }

        void Update()
        {
            if (Time.unscaledTime < nextTurn) return;
            nextTurn = Time.unscaledTime + Random.Range(MinTurn, MaxTurn);
            transform.localScale = new Vector3(-transform.localScale.x, 1f, 1f);
        }
    }

    /// <summary>A burst of twinkles flying out of a point and fading: the stone that has just become a star (CPC).</summary>
    public class Sparkle : MonoBehaviour
    {
        const int Count = 6;
        const float Life = 0.6f, Reach = 70f;
        public float delay;
        RectTransform[] parts;
        Image[] images;
        float t;

        void Start()
        {
            string sprite = Art.Has("twinkle") ? "twinkle" : "star_full";
            parts = new RectTransform[Count];
            images = new Image[Count];
            for (int i = 0; i < Count; i++)
            {
                images[i] = UiKit.Image(transform, sprite, UiKit.Center, Vector2.zero, new Vector2(30, 30));
                images[i].raycastTarget = false;
                images[i].enabled = false;
                parts[i] = images[i].rectTransform;
            }
            t = -delay;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (t < 0f) return;
            float k = t / Life;
            if (k >= 1f)
            {
                foreach (var part in parts) Destroy(part.gameObject);
                Destroy(this);
                return;
            }
            for (int i = 0; i < Count; i++)
            {
                float angle = (i + 0.5f) / Count * Mathf.PI * 2f;
                images[i].enabled = true;
                parts[i].anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Reach * (1f - (1f - k) * (1f - k));
                parts[i].localScale = Vector3.one * (1.2f - 0.7f * k);
                images[i].color = new Color(1f, 1f, 1f, 1f - k * k);
            }
        }
    }

    /// <summary>CPC's "HELP!" by the nearest cage: pops in as the map opens, wobbles for two seconds, shrinks away, and
    /// comes back every few seconds.</summary>
    public class Shout : MonoBehaviour
    {
        const float Every = 5f, Up = 2f, Pop = 0.25f;
        float start;

        void Start() => start = Time.unscaledTime - 0.3f;

        void Update()
        {
            float t = (Time.unscaledTime - start) % Every;
            float k = t < Pop ? t / Pop : t < Up ? 1f : t < Up + Pop ? 1f - (t - Up) / Pop : 0f;
            if (k <= 0f)
            {
                if (transform.localScale.x != 0f) transform.localScale = Vector3.zero;   // hidden: no work until it shows again
                return;
            }
            float s = k * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 7f));
            transform.localScale = new Vector3(s, s, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(Time.unscaledTime * 5f));   // CPC's text sways
        }
    }

    /// <summary>The captive's cage shakes in short bursts while it calls for help.</summary>
    public class Rattle : MonoBehaviour
    {
        const float Every = 2.2f, Shake = 0.5f, Angle = 9f, Speed = 38f;

        void Update()
        {
            float phase = Time.unscaledTime % Every;
            float z = phase < Shake ? Mathf.Sin(phase * Speed) * Angle * (1f - phase / Shake) : 0f;
            if (Mathf.Abs(z - Mathf.DeltaAngle(0f, transform.localEulerAngles.z)) < 0.01f) return;   // at rest: no canvas dirtying
            transform.localRotation = Quaternion.Euler(0f, 0f, z);
        }
    }
}

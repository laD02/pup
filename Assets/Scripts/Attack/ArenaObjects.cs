using System;
using System.Collections;
using SealCatapult.Core;
using UnityEngine;

namespace SealCatapult.Attack
{
    /// <summary>Orange gear-shaped bumper: anything that hits it is flung back hard (used to reach hidden bears).</summary>
    public class Bumper : MonoBehaviour
    {
        public const float Radius = 0.45f;
        public float Kick { get; private set; }
        SpriteRenderer sr;

        public static Bumper Create(Vector2 at, float kick, Transform parent)
        {
            var sr = Art.Spawn("bumper", parent, at, Radius * 2f, 11);
            sr.name = "Bumper";
            var col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.radius = Radius / sr.transform.localScale.x;
            var b = sr.gameObject.AddComponent<Bumper>();
            b.sr = sr;
            b.Kick = kick;
            return b;
        }

        void Update() => sr.transform.Rotate(0, 0, 40f * Time.deltaTime);

        public void Hit(Vector2 at)
        {
            AudioManager.Sfx("bumper", 0.1f);
            Fx.Burst("spark", at, 6, 3f, 0.3f, 0.2f, 0f, 0.1f, new Color(1f, 0.75f, 0.2f));
            StartCoroutine(Jolt());
        }

        IEnumerator Jolt()
        {
            var s0 = sr.transform.localScale;
            for (float t = 0f; t < 0.15f; t += Time.deltaTime)
            {
                sr.transform.localScale = s0 * (1f + Mathf.Sin(t / 0.15f * Mathf.PI) * 0.25f);
                yield return null;
            }
            sr.transform.localScale = s0;
        }
    }

    /// <summary>Red TNT crate: a diving pup or a hard knock sets it off; the blast breaks blocks and bowls bears over.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Tnt : MonoBehaviour
    {
        const float Size = 0.8f;
        const float BlastRadius = 2.2f;
        const float BlastForce = 9f;
        const float TriggerImpulse = 2.5f;

        static readonly Collider2D[] hits = new Collider2D[32];
        bool fired;

        public static Tnt Create(Vector2 at, Transform parent)
        {
            var sr = Art.Spawn("crate_tnt", parent, at, Size, 10);
            sr.name = "TNT";
            var col = sr.gameObject.AddComponent<BoxCollider2D>();
            col.size = sr.sprite.bounds.size;
            var rb = sr.gameObject.AddComponent<Rigidbody2D>();
            rb.mass = 1.2f;
            rb.sleepMode = RigidbodySleepMode2D.StartAsleep;
            return sr.gameObject.AddComponent<Tnt>();
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (!Block.Armed || fired) return;
            float impulse = 0f;
            for (int i = 0; i < c.contactCount; i++) impulse += c.GetContact(i).normalImpulse;
            bool pup = c.collider.TryGetComponent(out PufferController p) && p.Dived;
            if (pup || impulse >= TriggerImpulse) Explode();
        }

        public void Explode()
        {
            if (fired) return;
            fired = true;
            Vector2 at = transform.position;
            AudioManager.Sfx("explode", 0f);
            Fx.Shake(0.6f);
            Fx.Burst("spark", at, 18, 7f, 0.6f, 0.4f, 2f, 0.3f, new Color(1f, 0.6f, 0.15f));
            Fx.Puff(at, 14, 1.0f);
            int n = Physics2D.OverlapCircle(at, BlastRadius, new ContactFilter2D().NoFilter(), hits);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h.gameObject == gameObject) continue;
                Vector2 d = (Vector2)h.transform.position - at;
                float falloff = 1f - Mathf.Clamp01(d.magnitude / BlastRadius);
                if (h.TryGetComponent(out Block block)) block.TakeDamage(12f * falloff + 2f);
                if (h.TryGetComponent(out Bear bear)) bear.Hit();
                if (h.TryGetComponent(out Tnt other)) other.StartCoroutine(other.Fuse(0.12f));
                if (h.attachedRigidbody != null) h.attachedRigidbody.AddForce((d.normalized + Vector2.up * 0.6f) * BlastForce * falloff, ForceMode2D.Impulse);
            }
            Destroy(gameObject);
        }

        IEnumerator Fuse(float delay)
        {
            yield return new WaitForSeconds(delay);
            Explode();
        }
    }

    /// <summary>Floor button: pressed by a pup landing on it. Raises Pressed(group) once.</summary>
    public class FloorButton : MonoBehaviour
    {
        public static event Action<int> Pressed;
        /// <summary>Pops every pressed button of a group back up (the boss climbed back: it can be knocked again).</summary>
        public static event Action<int> RearmRequested;
        public static void Rearm(int group) => RearmRequested?.Invoke(group);

        public int Group { get; private set; }
        SpriteRenderer sr;
        bool down;

        public static FloorButton Create(Vector2 at, int group, Transform parent)
        {
            var root = new GameObject("Button").transform;
            root.SetParent(parent, false);
            root.position = at;
            var sr = Art.Spawn("button_up", root, Vector2.zero, 0.9f, 11);
            // The base sits on the floor; the collider is the raised cap.
            sr.transform.localPosition = new Vector3(0f, sr.bounds.size.y * 0.5f, 0f);
            var col = root.gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.7f, 0.3f);
            col.offset = new Vector2(0f, 0.15f);
            var b = root.gameObject.AddComponent<FloorButton>();
            b.sr = sr;
            b.Group = group;
            return b;
        }

        void OnEnable() => RearmRequested += OnRearm;
        void OnDisable() => RearmRequested -= OnRearm;

        void OnRearm(int group)
        {
            if (group != Group || !down) return;
            down = false;
            sr.sprite = Art.Get("button_up");
            Fx.Puff(transform.position, 3, 0.2f);
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (!Block.Armed || down) return;
            if (c.contactCount == 0 || c.GetContact(0).normal.y > -0.3f) return;   // pressed from above only
            if (!c.collider.TryGetComponent(out PufferController _)) return;       // a pup's dropped hat or debris must not press it
            Press();
        }

        /// <summary>Push the button down (a pup landing on it; tests call it directly).</summary>
        public void Press()
        {
            if (down) return;
            down = true;
            sr.sprite = Art.Get("button_down");
            AudioManager.Sfx("button", 0f);
            Fx.Burst("spark", transform.position, 5, 2f, 0.3f, 0.18f, 0f, 0.2f, new Color(0.5f, 1f, 0.6f));
            Pressed?.Invoke(Group);
        }
    }

    /// <summary>Fish to collect on bonus levels: a pup passing through picks it up.</summary>
    public class Fish : MonoBehaviour
    {
        public static event Action<Fish> Collected;
        float phase;

        public static Fish Create(Vector2 at, Transform parent)
        {
            var sr = Art.Spawn("fish", parent, at, 0.7f, 11);
            sr.name = "Fish";
            var col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.45f / sr.transform.localScale.x;
            col.isTrigger = true;
            var f = sr.gameObject.AddComponent<Fish>();
            f.phase = UnityEngine.Random.value * 6f;
            return f;
        }

        void Update() => transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 4f + phase) * 12f);

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out PufferController _)) return;
            AudioManager.Sfx("fish", 0.1f);
            Fx.Burst("star_full", transform.position, 5, 2.5f, 0.6f, 0.2f, 1f, 0.15f);
            Fx.FloatingText("+100", transform.position, new Color(1f, 0.85f, 0.3f), 4f, 0.8f);
            Collected?.Invoke(this);
            Destroy(gameObject);
        }
    }

    /// <summary>Hanging cage with a captive pup: swings gently, opens when the boss is beaten.</summary>
    public class Cage : MonoBehaviour
    {
        SpriteRenderer cage;
        PupRig captive;
        float phase;

        public static Cage Create(Vector2 at, Transform parent)
        {
            var root = new GameObject("Cage").transform;
            root.SetParent(parent, false);
            root.position = at;
            var c = root.gameObject.AddComponent<Cage>();
            // Chain up to the ceiling, the cage body, and the pup inside drawn behind the bars.
            var chain = Art.Spawn("rope", root, new Vector2(0f, 1.6f), 0.1f, 9);
            chain.transform.localScale = new Vector3(chain.transform.localScale.x, 2.2f / chain.sprite.bounds.size.y, 1f);
            c.captive = PupRig.Create(root, (Vector2)root.position + new Vector2(0f, -0.1f), 10);
            c.captive.transform.localScale = Vector3.one * 0.8f;
            c.captive.SetPose(PupRig.Pose.Idle);
            c.cage = Art.Spawn("cage", root, Vector2.zero, 1.5f, 12);
            c.phase = UnityEngine.Random.value * 3f;
            return c;
        }

        void Update() => transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 1.3f + phase) * 4f);

        /// <summary>The cage bursts (report §10.6): it swells and shakes, the bars fly apart, stars circle it, and the
        /// freed pup springs out, lands on the floor and waves.</summary>
        public IEnumerator Open()
        {
            var body = cage.transform;
            Vector3 size = body.localScale;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)   // strain: swell and shudder
            {
                float k = t / 0.35f;
                body.localScale = size * (1f + 0.15f * k);
                body.localPosition = new Vector3(Mathf.Sin(t * 70f) * 0.06f * k, 0f, 0f);
                yield return null;
            }
            body.localScale = size;
            body.localPosition = Vector3.zero;
            cage.sprite = Art.Get("cage_open");
            AudioManager.Sfx("cage", 0f);
            Fx.Shake(0.3f);
            Vector2 c = transform.position;
            for (int i = 0; i < 8; i++)   // bent bars flying out
            {
                var bar = Art.Spawn("ui_bar", null, c, 0.09f, 14);
                bar.color = new Color(0.55f, 0.6f, 0.7f);
                bar.transform.localScale = new Vector3(bar.transform.localScale.x, 0.9f / bar.sprite.bounds.size.y, 1f);
                var d = bar.gameObject.AddComponent<Debris>();
                float a = (i / 8f) * Mathf.PI * 2f;
                d.velocity = new Vector2(Mathf.Cos(a), Mathf.Abs(Mathf.Sin(a)) + 0.4f) * 5f;
                d.gravity = 12f;
                d.spin = UnityEngine.Random.Range(-720f, 720f);
                d.life = 1.2f;
            }
            Fx.Burst("star_full", c, 12, 4f, 1f, 0.3f, 2f, 0.3f);
            StartCoroutine(OrbitStars(c));
            // The pup pops out with a spin, lands on the floor below and waves.
            captive.transform.SetParent(null, true);
            captive.transform.rotation = Quaternion.identity;   // not the cage's sway tilt
            captive.SetPose(PupRig.Pose.Spin);
            Vector3 from = captive.transform.position;
            Vector3 to = new Vector3(from.x + 1.4f, AttackPhase.Origin.y + PupRig.StandHeight, 0f);
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                float k = t / 0.7f;
                captive.transform.position = Vector3.Lerp(from, to, k * k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.6f;
                yield return null;
            }
            captive.transform.position = to;
            captive.SetPose(PupRig.Pose.Cheer);
            Fx.Puff((Vector2)to - new Vector2(0f, PupRig.StandHeight), 6, 0.3f);
            Vector3 s0 = captive.transform.localScale;
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)   // landing squash
            {
                float k = Mathf.Sin(t / 0.2f * Mathf.PI) * 0.2f;
                captive.transform.localScale = new Vector3(s0.x * (1f + k), s0.y * (1f - k), 1f);
                yield return null;
            }
            captive.transform.localScale = s0;
        }

        /// <summary>Five stars circling where the cage burst, shrinking away.</summary>
        static IEnumerator OrbitStars(Vector2 c)
        {
            const int N = 5;
            var stars = new SpriteRenderer[N];
            for (int i = 0; i < N; i++) stars[i] = Art.Spawn("star_full", null, c, 0.35f, 42);
            for (float t = 0f; t < 1.4f; t += Time.deltaTime)
            {
                float r = 0.9f + t * 0.4f;
                for (int i = 0; i < N; i++)
                {
                    float a = t * 6f + i * Mathf.PI * 2f / N;
                    stars[i].transform.position = c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.5f);
                    stars[i].transform.localScale = Vector3.one * 0.35f / stars[i].sprite.bounds.size.x * (1f - t / 1.4f);
                }
                yield return null;
            }
            foreach (var s in stars) UnityEngine.Object.Destroy(s.gameObject);
        }
    }
}

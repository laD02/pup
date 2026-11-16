using System;
using System.Collections;
using System.Collections.Generic;
using SealCatapult.Core;
using SealCatapult.Progress;
using UnityEngine;

namespace SealCatapult.Attack
{
    /// <summary>
    /// Polar bear guard: stands, eats from a bowl or sleeps until a diving pup lands on it (ice-helmet bears take two
    /// hits) or it is knocked off its perch. A knocked-out bear tips over with stars and stays lying there.
    /// </summary>
    public class Bear : MonoBehaviour
    {
        const float Width = 1.2f;   // user 12/11: bears a little bigger (was 1.0)
        const float HitCooldown = 0.3f;

        public static event Action<Bear, int> Defeated;
        /// <summary>Bears in the scene, so pups never search the hierarchy (ninja target, hat collisions).</summary>
        public static readonly List<Bear> All = new List<Bear>();

        int hp;
        bool defeated;
        float startY;
        float lastHit = -10f;
        float wavePhase;
        float tauntTimer;
        BearMood mood;
        GameTuning tuning;
        BearRig rig;
        Transform body;
        Rigidbody2D rb;
        CapsuleCollider2D col;
        float height;

        public bool IsDown => defeated;

        public static Bear Create(BearSpec spec, Vector2 origin, GameTuning tuning, Transform parent)
        {
            var go = new GameObject(spec.helmet ? "Bear_Helmet" : "Bear");
            go.transform.SetParent(parent, false);
            var rig = BearRig.Create(go.transform, Width, spec.helmet, 12);
            float h = rig.Height;
            go.transform.position = origin + spec.position;   // the root is the feet point

            var col = go.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(Width * 0.7f, h * 0.92f);
            col.offset = new Vector2(0f, h * 0.5f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.mass = 0.8f;
            rb.linearDamping = 0.5f;
            rb.freezeRotation = true;   // stands up straight until it is knocked out
            rb.sleepMode = RigidbodySleepMode2D.StartAsleep;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var bear = go.AddComponent<Bear>();
            bear.hp = spec.helmet ? 2 : 1;
            bear.mood = spec.mood;
            bear.tuning = tuning;
            bear.rig = rig;
            rig.Face(false);   // bears face left, toward the pups flying in (user 13/11)
            bear.rb = rb;
            bear.col = col;
            bear.height = h;
            bear.body = rig.Body.transform;
            bear.tauntTimer = UnityEngine.Random.Range(1.2f, 3.5f);
            bear.wavePhase = UnityEngine.Random.value * 6f;
            bear.signature = spec.mood == BearMood.Eat ? BearRig.Pose.Eat
                : spec.mood == BearMood.Sleep ? BearRig.Pose.Sleep
                : spec.mood == BearMood.Dangle ? BearRig.Pose.Dangle
                : Signatures[Mathf.Abs(Mathf.RoundToInt(spec.position.x * 7f + spec.position.y * 13f)) % Signatures.Length];
            rig.SetPose(bear.signature);
            if (spec.mood == BearMood.Dangle)
            {
                // Its seat is on the ledge top (spec y); the hanging legs reach below it. Held in place while it sits,
                // since its body overlaps the ledge corner.
                go.transform.position += Vector3.down * h * DangleSeat;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            return bear;
        }

        void Start() => startY = transform.position.y;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Standing antics a guard bear cycles through at random, like the genre's bears: it never leaves its spot.</summary>
        static readonly BearRig.Pose[] Antics = { BearRig.Pose.Taunt, BearRig.Pose.Laugh, BearRig.Pose.Scratch, BearRig.Pose.Juggle, BearRig.Pose.Flex };
        /// <summary>Each guard bear keeps one looping antic for the whole level (picked from its spot, so a level always
        /// looks the same); Idle means "breathe and throw in random antics".</summary>
        static readonly BearRig.Pose[] Signatures = { BearRig.Pose.Idle, BearRig.Pose.Punch, BearRig.Pose.Smug, BearRig.Pose.Sit, BearRig.Pose.Bowl };
        BearRig.Pose signature;

        void Update()
        {
            if (defeated)
            {
                // Flung off the arena edge: stop falling forever (stays until the scene unloads, nothing references it).
                if (transform.position.y < Backdrop.GroundY - 8f) Destroy(gameObject);   // dropped out of the picture
                return;
            }
            if (mood == BearMood.Guard)
            {
                // An antic now and then (jeer, laugh, scratch, juggle a fish), plus a little sway.
                tauntTimer -= Time.deltaTime;
                if (signature == BearRig.Pose.Idle && tauntTimer < 0f && rig.Current == BearRig.Pose.Idle)
                {
                    rig.SetPose(Antics[UnityEngine.Random.Range(0, Antics.Length)]);
                    tauntTimer = UnityEngine.Random.Range(1.5f, 4f);
                }
                float w = Mathf.Sin(Time.time * 3f + wavePhase);
                body.localRotation = Quaternion.Euler(0, 0, w * 4f);
            }
            if (Block.Armed && startY - transform.position.y > tuning.bearFallDistance) Defeat();
            if (transform.position.y < Backdrop.GroundY - 3f) Defeat();
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!Block.Armed || defeated) return;
            float impulse = 0f;
            for (int i = 0; i < collision.contactCount; i++) impulse += collision.GetContact(i).normalImpulse;
            // Any diving pup counts as a hit; blocks and debris need real force.
            bool pup = collision.collider.TryGetComponent(out PufferController p) && p.Dived;
            if (pup || impulse >= tuning.bearHitImpulse) Hit();
        }

        public void Hit()
        {
            if (defeated || Time.time - lastHit < HitCooldown) return;
            lastHit = Time.time;
            hp--;
            if (hp <= 0)
            {
                Defeat();
                return;
            }
            // Helmet knocked off: shards, a flinch, then a dizzy spell.
            AudioManager.Sfx("boing");
            Fx.Burst("ice_shard", (Vector2)transform.position + Vector2.up * 0.6f, 6, 3f, 0.8f, 0.25f, 3f, 0.2f);
            rig.SetHelmet(false);
            mood = BearMood.Guard;   // woken up
            if (signature == BearRig.Pose.Sleep) signature = BearRig.Pose.Idle;   // and stays awake
            rig.SetPose(BearRig.Pose.Dizzy);
            StartCoroutine(Flinch());
        }

        void Defeat()
        {
            if (defeated) return;
            defeated = true;
            AudioManager.Sfx("bear_down");
            Fx.Shake(tuning.shakeOnBear);
            Fx.Puff(transform.position, 8);
            Fx.Burst("star_full", (Vector2)transform.position + Vector2.up * 1.2f, 5, 2.5f, 0.7f, 0.22f, 2f, 0.2f);
            Defeated?.Invoke(this, tuning.bearScore);
            // The knocked-out frames are drawn lying on the baseline, so the root stays upright: a little hop back,
            // a low wide collider, and it lies where it lands.
            body.localRotation = Quaternion.identity;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;   // a dangling bear was held in place until now
            // Reference §18 (frame by frame, 13/11): a hit bear tumbles with stars, a small hop back, and then drops
            // straight down through the floor and out of the picture. It does not stay lying in the arena.
            rb.linearDamping = 0f;
            rb.linearVelocity = new Vector2(UnityEngine.Random.Range(KnockBackMin, KnockBackMax), KnockUp);
            col.enabled = false;   // falls through everything
            StartCoroutine(SpinUp());
            rig.SetPose(BearRig.Pose.KnockedOut);
        }

        const float DangleSeat = 0.26f;   // seat line above the feet, share of the bear's height (bear_dangle frames)
        const float KnockUp = 3.5f;     // the small hop of a knocked-out bear before it drops away, units per second
        const float KnockBackMin = 0.6f, KnockBackMax = 1.4f;   // sideways drift while it falls
        const float SpinTime = 0.55f;   // one full turn in the air

        IEnumerator SpinUp()
        {
            float dir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            for (float t = 0f; t < SpinTime; t += Time.deltaTime)
            {
                float k = t / SpinTime;
                body.localRotation = Quaternion.Euler(0, 0, dir * 360f * (1f - (1f - k) * (1f - k)));   // fast, then settling
                yield return null;
            }
            body.localRotation = Quaternion.identity;
        }

        /// <summary>Squash and shake after losing the helmet.</summary>
        IEnumerator Flinch()
        {
            var t = transform;
            for (float time = 0f; time < 0.3f; time += Time.deltaTime)
            {
                float k = 1f - time / 0.3f;
                t.localScale = new Vector3(1f + 0.25f * k, 1f - 0.2f * k, 1f);
                yield return null;
            }
            t.localScale = Vector3.one;
            // Back to its own antic once the dizzy spell is over (the rig falls back to Idle).
            while (!defeated && rig.Current == BearRig.Pose.Dizzy) yield return null;
            if (!defeated && signature != BearRig.Pose.Idle) rig.SetPose(signature);
        }
    }
}

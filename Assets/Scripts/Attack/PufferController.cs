using System;
using System.Collections;
using SealCatapult.Core;
using SealCatapult.Progress;
using UnityEngine;

namespace SealCatapult.Attack
{
    /// <summary>
    /// Phase-2 pup, modelled on the genre's level screens: glides level across the arena and wraps from the right
    /// edge back to the left (a few loops), noses down on the tap and drops faster and faster, bounces up off bears
    /// it hits, smashes through a few ice cubes, lands with real physics, then gets up and walks off. Second tap
    /// mid-dive: Ninja (hang in a speech bubble, then drop straight down) or Blaze (catch fire, melt through ice).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class PufferController : MonoBehaviour
    {
        /// <summary>Arena pups are drawn smaller than phase-1 pups (user 12/11: "ở pha 2 hải cẩu to quá"); colliders scale with them.</summary>
        public const float ArenaScale = 0.75f;
        const float GlideRadius = 0.91f * PupRig.Scale;
        const float BallRadius = 1.09f * PupRig.Scale;
        const float RestSpeed = 0.35f;
        const float InputGrace = 0.2f;
        const float WalkOffDistance = 1.6f;
        const float WalkOffTime = 1.1f;
        const float WrapMargin = 1.5f;

        public event Action<PufferController> Finished;

        Rigidbody2D rb;
        CircleCollider2D col;
        PupRig rig;
        public PupRig.Pose Pose => rig.Current;
        GameTuning tuning;
        Vector2 lastVelocity;
        int pierceLeft;
        int bouncesLeft;
        int ninjaLeft;
        int blazeLevel;
        bool blazing;
        bool done;
        bool leaving;
        bool hanging;
        int loops;
        float age;
        float restTimer;
        float trailTimer;
        SpriteRenderer bubble;

        public bool Dived { get; private set; }
        public bool Landed { get; private set; }
        public bool Blazing => blazing;

        public static PufferController Spawn(Vector2 position, GameTuning tuning, int[] upgrades, Vector2 velocity)
        {
            var rig = PupRig.Create(null, position, 30);
            rig.name = "Puffer";
            rig.SetPose(PupRig.Pose.Fly);
            var go = rig.gameObject;
            go.transform.localScale = Vector3.one * ArenaScale;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.mass = tuning.pufferMass;
            rb.gravityScale = tuning.glideGravity;   // level flight until the dive
            rb.linearDamping = 0f;   // the glide holds its speed lap after lap; drag starts with the dive or the first bump
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = GlideRadius;
            col.sharedMaterial = Material;

            var p = go.AddComponent<PufferController>();
            p.rb = rb;
            p.col = col;
            p.rig = rig;
            p.tuning = tuning;
            p.pierceLeft = tuning.basePierce + upgrades[(int)UpgradeId.Spiky];
            p.bouncesLeft = BaseBounces + upgrades[(int)UpgradeId.PuffUp];
            p.ninjaLeft = upgrades[(int)UpgradeId.Ninja];
            p.blazeLevel = upgrades[(int)UpgradeId.Blaze];
            rb.linearVelocity = velocity;
            return p;
        }

        static PhysicsMaterial2D material;
        static PhysicsMaterial2D Material => material != null ? material : material = new PhysicsMaterial2D("Puffer") { bounciness = 0.15f, friction = 0.08f };   // little bounce, slides along snow and down slopes (reference §18)

        public int NinjaLeft => ninjaLeft;
        /// <summary>Still gliding after maxLoops laps: time to show the tap hint.</summary>
        public bool NeedsNudge => !Dived && !Landed && loops >= tuning.maxLoops;
        float divedAt;
        const int AutoDiveLaps = 4;      // × maxLoops laps without a tap
        const float NinjaReach = 5f;     // sideways reach of the ninja kick, world units
        const int BaseBounces = 2;       // every pup bounces on as a ball after its first hit (reference §16.2); the upgrade adds more

        void Update()
        {
            if (done || leaving) return;
            bool listening = age > InputGrace;   // the tap that launched the pup is not a dive
            if (listening && GameInput.ActionPressed())
            {
                if (!Dived && !Landed) Dive();
                else if (Dived && !Landed && !hanging) SecondTap();
            }
            Vector2 v = rb.linearVelocity;
            if (!Dived && !Landed)
            {
                rig.Face(true);
                transform.rotation = Quaternion.identity;
            }
            sliding -= Time.deltaTime;
            trailTimer -= Time.deltaTime;
            if (trailTimer <= 0f && (blazing || sliding > 0f || v.sqrMagnitude > 9f))
            {
                trailTimer = blazing ? 0.025f : 0.04f;
                if (blazing) Fx.Burst("spark", transform.position, 1, 1.5f, 0.35f, 0.3f, -2f, 0.15f, new Color(1f, 0.6f + 0.3f * UnityEngine.Random.value, 0.1f));
                else if (Dived && !Landed && v.sqrMagnitude > 25f) Fx.WindStreak(transform.position, v);   // diving: wind lines
                else if (sliding > 0f) Fx.Burst("spark", transform.position, 1, 1.2f, 0.25f, 0.18f, -1f, 0.1f, new Color(1f, 0.55f, 0.15f));   // friction fire on a slope
                else if (Landed) Fx.Smoke(transform.position);   // the balled-up pup trails grey smoke while it bounces
                else Fx.Trail(transform.position);
            }
        }

        void FixedUpdate()
        {
            if (done || leaving) return;
            lastVelocity = rb.linearVelocity;
            age += Time.fixedDeltaTime;
            restTimer = rb.linearVelocity.magnitude < RestSpeed ? restTimer + Time.fixedDeltaTime : 0f;
            Vector2 p = rb.position;
            // Level flight wraps from the right edge back to the left for as long as the player waits (GDD §R4);
            // after maxLoops laps the HUD nudges with the tap hint (NeedsNudge).
            // Wrap edges are just outside the camera view, so the pup never pops in or out in the middle of the wider view.
            if (!Dived && !Landed && p.x > AttackPhase.ViewRight + WrapMargin * 0.5f)
            {
                loops++;
                rb.position = new Vector2(AttackPhase.ViewLeft - WrapMargin * 0.5f, p.y);
                // An idle player: after many laps the pup dives by itself (from the left edge, inside the arena) so the level can end.
                if (loops >= tuning.maxLoops * AutoDiveLaps)
                {
                    rb.position = new Vector2(AttackPhase.Origin.x - WrapMargin, p.y);
                    Dive();
                }
            }
            bool outOfBounds = p.y < Backdrop.GroundY - 3f || p.x > AttackPhase.FortressX + 30f || p.x < AttackPhase.FortressX - 30f;
            if (Landed && !Dived && divedAt == 0f) divedAt = age;   // a glide that hit rock counts from its landing
            if (outOfBounds || ((Dived || Landed) && age - divedAt > tuning.maxLifetime)) Finish();
            else if (restTimer > tuning.restTime && Landed) StartCoroutine(WalkOff());
        }

        /// <summary>Nose down: a steep, quick drop along `diveAngle` at `diveSpeedScale` × the glide speed, bent steeper by
        /// `diveGravity` (reference, 12/11: the penguin tips nose-down and drops in about a third of a second, with only a
        /// little forward drift). Tap just before the target.</summary>
        public void Dive()
        {
            Dived = true;
            divedAt = age;
            StartCoroutine(NoseDown());
            rb.gravityScale = tuning.diveGravity;
            rb.linearDamping = tuning.airDrag;
            rb.linearVelocity = DiveVelocity();
        }

        Vector2 DiveVelocity()
        {
            float speed = Mathf.Max(rb.linearVelocity.magnitude, AttackPhase.SpawnVelocity.x) * tuning.diveSpeedScale;
            float a = tuning.diveAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), -Mathf.Sin(a)) * speed;
        }

        /// <summary>Where a dive started now would be after <paramref name="t"/> seconds (air drag left out): the
        /// tutorial's aim dots.</summary>
        public Vector2 DivePoint(float t) =>
            rb.position + DiveVelocity() * t + 0.5f * tuning.diveGravity * t * t * Physics2D.gravity;

        /// <summary>The nose-down turn (two frames) before the pup curls into a ball for the drop.</summary>
        IEnumerator NoseDown()
        {
            rig.SetPose(PupRig.Pose.Dive);
            Fx.Puff(transform.position, 6);
            AudioManager.Sfx("dive");
            int ninjaAtStart = ninjaLeft;   // a ninja tap during the turn owns the pup from then on
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                if (done || Landed || ninjaLeft != ninjaAtStart) yield break;
                transform.rotation = Quaternion.Euler(0, 0, -Mathf.Lerp(0f, 90f, t / 0.2f));
                yield return null;
            }
            if (done || Landed || ninjaLeft != ninjaAtStart) yield break;
            transform.rotation = Quaternion.identity;
            PuffUp();
            rb.freezeRotation = false;
            rb.angularVelocity = -540f;
        }

        /// <summary>Second tap mid-dive: Ninja hang-and-drop first, Blaze when there is no ninja turn left (public for tests).</summary>
        public void SecondTap()
        {
            if (ninjaLeft > 0) StartCoroutine(NinjaDrop());
            else if (blazeLevel > 0 && !blazing) Ignite();
        }

        IEnumerator NinjaDrop()
        {
            ninjaLeft--;
            hanging = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rig.SetPose(PupRig.Pose.NinjaHang);
            transform.rotation = Quaternion.identity;
            bubble = Art.Spawn("bubble_ninja", null, (Vector2)transform.position + new Vector2(0f, 0.9f), 1.3f, 35);
            AudioManager.Sfx("click", 0.1f, 0.8f);
            for (float t = 0f; t < tuning.ninjaHang; t += Time.deltaTime)
            {
                if (done) yield break;
                bubble.transform.position = (Vector2)transform.position + new Vector2(0f, 0.9f);
                yield return null;
            }
            Destroy(bubble.gameObject);
            bubble = null;
            hanging = false;
            if (done || Landed) yield break;
            rb.gravityScale = tuning.diveGravity;
            // Ninja kick (report §10.5): leap feet-first at the nearest standing bear below; straight down if none.
            var target = NearestBearBelow();
            Vector2 dir = target != null
                ? ((Vector2)target.position + Vector2.up * 0.5f - (Vector2)transform.position).normalized
                : Vector2.down;
            rb.linearVelocity = dir * AttackPhase.SpawnVelocity.x * tuning.diveSpeedScale * 1.2f;
            if (target != null) Fx.Burst("spark", transform.position, 6, 3f, 0.3f, 0.2f, 0f, 0.1f, new Color(1f, 0.9f, 0.5f));
            PuffUp();
            AudioManager.Sfx("dive", 0.2f);
            Fx.Puff(transform.position, 4, 0.2f);
        }

        /// <summary>Closest standing bear (or the boss) below the pup and within NinjaReach sideways.</summary>
        Transform NearestBearBelow()
        {
            Transform best = null;
            float bestD = float.MaxValue;
            foreach (var b in Bear.All)
                if (!b.IsDown) Closer(b.transform, ref best, ref bestD);
            var boss = BossBear.Current;
            if (boss != null && !boss.IsBeaten) Closer(boss.transform, ref best, ref bestD);
            return best;
        }

        void Closer(Transform candidate, ref Transform best, ref float bestD)
        {
            Vector2 d = (Vector2)candidate.position - (Vector2)transform.position;
            if (d.y > -0.8f || Mathf.Abs(d.x) > NinjaReach) return;   // clearly below: the kick always aims down
            if (d.sqrMagnitude < bestD) { bestD = d.sqrMagnitude; best = candidate; }
        }

        /// <summary>Blaze: a fiery pup melts through ice cubes until it hits something solid.</summary>
        void Ignite()
        {
            blazing = true;
            pierceLeft = blazeLevel >= 3 ? 99 : blazeLevel * 2;
            AudioManager.Sfx("launch", 0.3f);
            Fx.Burst("spark", transform.position, 10, 4f, 0.5f, 0.3f, 0f, 0.3f, new Color(1f, 0.7f, 0.2f));
        }

        void PuffUp()
        {
            if (rig.Current == PupRig.Pose.Ball) return;
            rig.SetBallSprite(pierceLeft > tuning.basePierce ? "pup_spiky" : "pup_ball");
            rig.SetPose(PupRig.Pose.Ball);
            StartCoroutine(Pop(transform.localScale));
            col.radius = BallRadius;
            rb.mass = tuning.pufferMass * 1.5f;
            AudioManager.Sfx("puff");
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (done || leaving) return;
            var other = collision.collider;
            // Bumpers fling the pup straight back out along the contact, hard.
            if (other.TryGetComponent(out Bumper bumper))
            {
                Vector2 n = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector2.up;
                rb.linearVelocity = n * bumper.Kick;
                if (Dived) rb.gravityScale = tuning.gravityScale;
                bumper.Hit(transform.position);
                return;
            }
            // Bears: bounce up off them and keep going; the bear itself decides whether the hit knocks it out.
            // No IsDown check: the bear's own callback may have knocked it out earlier in this same step (a down bear's
            // collider is off, so only that same-step contact reaches here).
            if (other.TryGetComponent(out Bear _))
            {
                Vector2 n = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector2.up;
                Vector2 bounced = Vector2.Reflect(lastVelocity, n) * tuning.bounceKeep;
                bounced.y = Mathf.Max(bounced.y, tuning.bearBounce);
                rb.linearVelocity = bounced;
                rb.gravityScale = tuning.gravityScale;
                DropHat();
                AudioManager.Sfx("boing");
                Fx.Puff(transform.position, 4, 0.2f);
                return;
            }
            // Ice (reference §20.1): a pup still gliding level breaks the block and flies straight on, unchanged.
            if (!Dived && !Landed && other.TryGetComponent(out Block glided) && glided.Material != BlockMaterial.Stone)
            {
                glided.Break();
                rb.linearVelocity = lastVelocity;
                return;
            }
            // Ice and snow: a diving pup smashes through a few cubes (Spiky upgrade) before it is slowed to real physics.
            if (Dived && !Landed && pierceLeft > 0 && other.TryGetComponent(out Block block) && block.Material != BlockMaterial.Stone)
            {
                pierceLeft--;
                block.Break();
                rb.linearVelocity = lastVelocity * tuning.pierceKeep;
                return;
            }
            // Diving into ice without pierce left: the block breaks and the pup rebounds back a short way, about half its
            // own size, then falls on (reference §20.2).
            if (Dived && !Landed && other.TryGetComponent(out Block hit) && hit.Material != BlockMaterial.Stone)
            {
                hit.Break();
                Landed = true;
                DropHat();
                rb.gravityScale = tuning.gravityScale;
                rb.linearVelocity = new Vector2(-Mathf.Sign(lastVelocity.x) * IceRebound, IceRebound);
                AudioManager.Sfx("thud");
                return;
            }
            bool firstHit = !Landed;
            Landed = true;
            if (blazing)
            {
                blazing = false;   // the fire goes out on the first solid hit
                Fx.Puff(transform.position, 5, 0.3f);
            }
            if (firstHit)
            {
                DropHat();
                // A dent where it struck rock (not on blocks, which break or stay clean).
                if (collision.contactCount > 0 && Mathf.Abs(collision.GetContact(0).normal.y) < 0.5f && !other.TryGetComponent(out Block _) && Art.Has("dent"))   // only bare rock faces: every top and slope wears snow
                {
                    // Under the snow cap (order 10), over the rock (order 9; a hair nearer the camera): a dent in the stone,
                    // never a brown blot on the snow.
                    var dent = Art.Spawn("dent", null, collision.GetContact(0).point, 0.5f, 9);
                    dent.transform.position += Vector3.back * 0.01f;
                }
                rb.gravityScale = tuning.gravityScale;   // from here on it falls with real weight
                rb.linearDamping = tuning.airDrag;
                if (!Dived) rig.SetPose(PupRig.Pose.Flail);   // bonked mid-glide
                rb.freezeRotation = false;
                if (collision.contactCount > 0) StartCoroutine(Squash(collision.GetContact(0).normal));
                AudioManager.Sfx("thud");
                Fx.Shake(tuning.shakeOnBreak * (Dived ? 2f : 1f));
            }
            // Rock slope (reference §20.2): no bounce; the pup slides down it on its belly, sparks flying.
            if (collision.contactCount > 0 && !other.TryGetComponent(out Block _))
            {
                Vector2 n = collision.GetContact(0).normal;
                if (n.y > 0.35f && n.y < 0.95f)
                {
                    Vector2 down = new Vector2(n.y, -n.x) * Mathf.Sign(n.x);   // along the slope, downhill
                    rb.linearVelocity = down * Mathf.Max(SlideSpeed, Vector2.Dot(lastVelocity, down));
                    sliding = SlideSparks;
                    return;
                }
            }
            if (bouncesLeft > 0 && lastVelocity.magnitude > 3f && collision.contactCount > 0)
            {
                bouncesLeft--;
                rb.linearVelocity = Vector2.Reflect(lastVelocity, collision.GetContact(0).normal) * tuning.bounceKeep;
                AudioManager.Sfx("boing");
                Fx.Puff(transform.position, 4);
            }
        }

        const float IceRebound = 1.4f;    // units/s back and up after a dive breaks an ice block
        const float SlideSpeed = 4f;      // the least speed of a belly slide down a slope
        const float SlideSparks = 0.6f;   // seconds of friction sparks after touching a slope
        float sliding;
        bool hatDropped;

        /// <summary>The first hit knocks the hat off: it flies away on its own (reference §16.2).</summary>
        void DropHat()
        {
            if (hatDropped || !Art.Has("pup_hat")) return;
            hatDropped = true;
            if (Art.Has("pup_ball_bare")) rig.SetBallSprite("pup_ball_bare");   // the ball goes on bare-headed
            rig.GoBare();
            // The hat is a small physics body: it flies off, lands on the rock and lies there a moment, then fades
            // (as a plain particle it used to fall through the floor).
            var hat = Art.Spawn("pup_hat", null, (Vector2)transform.position + Vector2.up * 0.3f, 0.45f, 16);
            var hrb = hat.gameObject.AddComponent<Rigidbody2D>();
            hrb.mass = 0.05f;
            hrb.linearVelocity = new Vector2(UnityEngine.Random.Range(-1.5f, 1.5f), 4f);
            hrb.angularVelocity = UnityEngine.Random.Range(-400f, 400f);
            var hcol = hat.gameObject.AddComponent<CircleCollider2D>();
            hcol.radius = hat.sprite.bounds.extents.x * 0.6f;
            Physics2D.IgnoreCollision(hcol, col);
            foreach (var bear in Bear.All)
                if (bear.TryGetComponent(out Collider2D bc)) Physics2D.IgnoreCollision(hcol, bc);   // a hat never knocks a bear out
            hat.gameObject.AddComponent<FadeAway>().after = 2.5f;
        }

        /// <summary>Curl-up pop: overshoot then settle.</summary>
        IEnumerator Pop(Vector3 target)
        {
            for (float t = 0f; t < 0.18f; t += Time.deltaTime)
            {
                if (done || leaving) yield break;
                float k = t / 0.18f;
                transform.localScale = target * (0.7f + 0.3f * k + Mathf.Sin(k * Mathf.PI) * 0.25f);
                yield return null;
            }
            if (!done && !leaving) transform.localScale = target;
        }

        /// <summary>Impact squash along the contact normal.</summary>
        IEnumerator Squash(Vector2 normal)
        {
            var target = transform.localScale;
            for (float t = 0f; t < 0.16f; t += Time.deltaTime)
            {
                if (done || leaving) yield break;
                float k = Mathf.Sin(t / 0.16f * Mathf.PI) * 0.22f;
                bool vertical = Mathf.Abs(normal.y) > Mathf.Abs(normal.x);
                transform.localScale = new Vector3(target.x * (1f + (vertical ? k : -k)), target.y * (1f - (vertical ? k : -k)), 1f);
                yield return null;
            }
            if (!done && !leaving) transform.localScale = target;
        }

        /// <summary>Came to rest: stand up, shake it off, walk a little way and fade out.</summary>
        IEnumerator WalkOff()
        {
            leaving = true;
            rb.simulated = false;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * ArenaScale;
            rig.SetPose(PupRig.Pose.GetUp);
            Fx.Puff(transform.position, 3, 0.3f);
            Fx.DizzyOrbit(transform, new Vector2(0f, 0.55f), 0.9f);
            yield return new WaitForSeconds(0.55f);
            if (done) yield break;
            bool right = transform.position.x < AttackPhase.Origin.x + AttackPhase.ArenaWidth * 0.5f;
            rig.Face(right);
            rig.SetPose(PupRig.Pose.Walk);
            Vector3 from = transform.position;
            Vector3 to = from + Vector3.right * (right ? WalkOffDistance : -WalkOffDistance);
            for (float t = 0f; t < WalkOffTime; t += Time.deltaTime)
            {
                if (done) yield break;
                float k = t / WalkOffTime;
                transform.position = Vector3.Lerp(from, to, k);
                rig.SetAlpha(1f - Mathf.Clamp01((k - 0.55f) / 0.45f));
                yield return null;
            }
            Finish();
        }

        void Finish()
        {
            if (done) return;
            done = true;
            if (bubble != null) Destroy(bubble.gameObject);
            if (!leaving) Fx.Puff(transform.position, 8);
            Finished?.Invoke(this);
            Destroy(gameObject);
        }
    }

    /// <summary>Waits, fades the sprite out over half a second, then removes the object (dropped hats).</summary>
    public class FadeAway : MonoBehaviour
    {
        public float after = 2f;
        float t;
        SpriteRenderer sr;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            t += Time.deltaTime;
            if (t < after) return;
            float k = (t - after) / 0.5f;
            if (k >= 1f) { Destroy(gameObject); return; }
            if (sr != null) { var c = sr.color; c.a = 1f - k; sr.color = c; }
        }
    }
}

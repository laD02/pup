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
        const float DazedTime = 1.1f;      // sitting dazed after the get-up, fading over its last 30 %
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

        /// <summary>The pup most recently sent into the arena while it is still in play (bears watch it).</summary>
        public static PufferController Latest { get; private set; }
        bool spikyUpgrade;

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
            p.spikyUpgrade = upgrades[(int)UpgradeId.Spiky] > 0;
            p.bouncesLeft = BaseBounces + upgrades[(int)UpgradeId.PuffUp];
            p.ninjaLeft = upgrades[(int)UpgradeId.Ninja];
            p.blazeLevel = upgrades[(int)UpgradeId.Blaze];
            rb.linearVelocity = velocity;
            Latest = p;
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
        const int BaseBounces = 3;       // every pup bounces on as a ball after its first hit (reference §16.2); the upgrade adds more

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
            if (rig.Current == PupRig.Pose.Roll && Landed && !leaving)
            {
                // Rolling without slipping: the turn follows the speed along the ground.
                Vector2 along = new Vector2(rollNormal.y, -rollNormal.x);
                rb.angularVelocity = -Vector2.Dot(v, along) / BallRadius * Mathf.Rad2Deg;
            }
            if (Landed && sliding <= 0f && rig.Current == PupRig.Pose.Slide)
                rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, 0f, LevelOutSpeed * Time.deltaTime);   // off the slope: lie flat again
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

        /// <summary>The dive: a quick nose-down turn, then a stiff head-first torpedo along its velocity until it hits
        /// something (CPC, live 16/11: it only curls into a ball on impact, see <see cref="BallUpOnImpact"/>).</summary>
        IEnumerator NoseDown()
        {
            rig.SetPose(PupRig.Pose.Dive);
            Fx.Puff(transform.position, 6);
            AudioManager.Sfx("dive");
            int ninjaAtStart = ninjaLeft;   // a ninja tap during the dive owns the pup from then on
            for (float t = 0f; ; t += Time.deltaTime)
            {
                if (done || Landed || ninjaLeft != ninjaAtStart || rig.Current != PupRig.Pose.Dive) yield break;
                // The ball's weight and size arrive on the old schedule (0.2 s after the tap), so the levels' block
                // knocks and star thresholds stay as tuned; only the picture waits for the impact.
                if (t >= BallPhysicsAfter) BallPhysics();
                Vector2 v = rb.linearVelocity;
                float aim = v.sqrMagnitude > 0.01f ? Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg : -90f;
                // Through the body, not the transform: an interpolated body rewrites the transform every frame.
                // The dive frames are already drawn nose-down (DiveArtAngle): turn only by what the flight adds to that,
                // or the head swings past the motion and the pup dives backwards (user 17/11: "lao chưa đúng chiều").
                rb.rotation = Mathf.LerpAngle(0f, aim - DiveArtAngle, Mathf.Clamp01(t / TurnTime));
                yield return null;
            }
        }

        const float TurnTime = 0.12f;   // nose-down turn at the start of a dive
        const float DiveArtAngle = -70f;   // heading of the held pup_dive_1 frame, nose toward bottom-right
        const float BallPhysicsAfter = 0.2f;   // when the old dive curled up: the ball's collider and mass from then on
        bool ballPhysics;

        void BallPhysics()
        {
            if (ballPhysics) return;
            ballPhysics = true;
            col.radius = BallRadius;
            rb.mass = tuning.pufferMass * 1.5f;
        }

        /// <summary>First contact of a torpedo dive: flung off as a rag doll (or the spiky ball) that bounces on.</summary>
        void BallUpOnImpact()
        {
            if (!Dived || rig.Current != PupRig.Pose.Dive) return;
            rb.rotation = 0f;
            transform.rotation = Quaternion.identity;
            PuffUp();
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
            rb.rotation = 0f;   // the torpedo turned the body, not just the transform
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

        /// <summary>The hit flings the pup off as a flailing rag doll in one of several drawn styles (user 17/11: "bỏ
        /// hoạt ảnh tròn khi văng đi … như cpc, nhiều kiểu"); the frames carry the spin, so the body does not turn.
        /// Only the Ice Spikes upgrade still curls into its spiky ball, which is that upgrade's look.</summary>
        void PuffUp()
        {
            if (rig.Current == PupRig.Pose.Ball || rig.Current == PupRig.Pose.Flung) return;
            BallPhysics();
            StartCoroutine(Pop(Vector3.one * ArenaScale));
            AudioManager.Sfx("puff");
            if (spikyUpgrade)   // not pierceLeft: Blaze raises that too
            {
                rig.SetBallSprite("pup_spiky");
                rig.SetPose(PupRig.Pose.Ball);
                rb.freezeRotation = false;
                rb.angularVelocity = -540f;
                return;
            }
            Tumble();
        }

        /// <summary>CPC (video at 20–30 fps, 17/11, user: "xem lại cách văng của cpc … quan sát thật kĩ"): a struck
        /// penguin keeps one flailing pose and the WHOLE body spins, about a turn every half second, the way it travels.
        /// It never curls up and never switches to a drawn somersault.</summary>
        void Tumble()
        {
            // CPC (60 fps, 17/11, checked twice): ONE flung drawing, hunched forward like sitting in mid-air, feet out in
            // front, flippers swept back; the whole sprite turns about once a second.
            rig.Face(true);
            rig.SetPose(PupRig.Pose.Flung);
            rb.freezeRotation = false;
            Spin();
        }

        /// <summary>CPC (55.6–57.9 s of the video, frame by frame): in the air the penguin tumbles every which way, head
        /// down, on its side, upside down, and each strike sends it turning a new way at a new speed.</summary>
        void Spin()
        {
            Vector2 v = rb.linearVelocity;
            float dir = UnityEngine.Random.value < 0.35f ? (rb.angularVelocity >= 0f ? -1f : 1f)   // sometimes flips the turn
                : Mathf.Abs(v.x) > 0.1f ? -Mathf.Sign(v.x) : 1f;
            rb.angularVelocity = dir * Mathf.Clamp(v.magnitude * SpinPerSpeed, SpinMin, SpinMax) * UnityEngine.Random.Range(0.6f, 1.4f);
            if (UnityEngine.Random.value < 0.5f) rig.Face(!rig.FacingRight);   // flips over side to side too
        }

        /// <summary>CPC: once it is down on the ground with speed left, the penguin curls round and rolls like a wheel
        /// (no slipping), then sits up dazed.</summary>
        void RollOn(Vector2 normal)
        {
            rollNormal = normal;
            if (rig.Current == PupRig.Pose.Roll) return;
            rig.SetPose(PupRig.Pose.Roll);
            rb.freezeRotation = false;
        }

        Vector2 rollNormal = Vector2.up;

        const float SpinPerSpeed = 70f, SpinMin = 250f, SpinMax = 650f;   // degrees/s
        const float RollMinSpeed = 1.2f;   // slower than this on the ground: it just comes to rest

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (done || leaving) return;
            BallUpOnImpact();
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
            if (other.TryGetComponent(out Bear hitBear))
            {
                Vector2 n = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector2.up;
                rb.linearVelocity = FlingVelocity(n, BearFlingSpeed);   // CPC: popped up and on past the bear, spinning
                rb.gravityScale = tuning.gravityScale;
                PuffUp();
                DropHat(hitBear);
                AudioManager.Sfx("boing");
                Fx.Puff(transform.position, 4, 0.2f);
                Fx.HitRings(collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position);
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
            // Already rolling or sliding after its first hit: CPC (video, 17/11) rolls straight on through the ice it
            // touches, breaking block after block along a row, instead of bouncing off each one.
            if (Landed && other.TryGetComponent(out Block rolled) && rolled.Material != BlockMaterial.Stone)
            {
                rb.linearVelocity = lastVelocity * RollThroughKeep;
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
                if (!other.TryGetComponent(out Block _))
                    Fx.Flash(collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position);
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
                if (!Dived && rig.Current != PupRig.Pose.Flung) rig.SetPose(PupRig.Pose.Flail);   // bonked mid-glide (a flung pup keeps tumbling)
                // The ball, the flung rag doll and the roll turn with the body; the other drawn frames do the turning.
                rb.freezeRotation = rig.Current != PupRig.Pose.Ball && rig.Current != PupRig.Pose.Flung && rig.Current != PupRig.Pose.Roll;
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
                    Skid(PupRig.Pose.Slide, down);   // CPC: a penguin on its belly, not a ball
                    return;
                }
            }
            if (firstHit && collision.contactCount > 0)
            {
                // CPC (video at 60 fps, 17/11): the first crash throws the pup in a long tumbling arc across a good part
                // of the arena, often back the way it came.
                rb.linearVelocity = FlingVelocity(collision.GetContact(0).normal, FlingSpeed);
                Fx.Splat(collision.GetContact(0).point);
                if (rig.Current == PupRig.Pose.Flung) Spin();
            }
            else if (bouncesLeft > 0 && lastVelocity.magnitude > 3f && collision.contactCount > 0)
            {
                // Every later wall or floor strike: a strong ricochet with a white puff and gold rings.
                bouncesLeft--;
                rb.linearVelocity = FlingVelocity(collision.GetContact(0).normal, lastVelocity.magnitude * FlingKeep, BounceLift);
                AudioManager.Sfx("boing");
                Fx.Splat(collision.GetContact(0).point);
                if (rig.Current == PupRig.Pose.Flung) Spin();
            }
            else if ((rig.Current == PupRig.Pose.Flung || rig.Current == PupRig.Pose.Roll) && collision.contactCount > 0
                     && collision.GetContact(0).normal.y > 0.3f && lastVelocity.magnitude > RollMinSpeed)
                RollOn(collision.GetContact(0).normal);   // out of bounces on the ground: curl up and roll
        }

        /// <summary>CPC (video, 17/11): the ball only lasts while it bounces; landing with speed it uncurls into a
        /// penguin that somersaults or belly-slides along, still breaking the ice it touches. The frames carry the
        /// rotation, so the body stops spinning.</summary>
        void Skid(PupRig.Pose pose, Vector2 along)
        {
            if (!Landed || hanging) return;
            transform.localScale = Vector3.one * ArenaScale;   // a Pop or Squash may be mid-way
            rb.freezeRotation = true;
            rb.angularVelocity = 0f;
            bool right = along.x >= 0f;
            rig.Face(right);
            // A belly slide lies along the slope; a somersault turns inside its own frames.
            float slope = pose == PupRig.Pose.Slide ? Mathf.Atan2(along.y, Mathf.Abs(along.x)) * Mathf.Rad2Deg * (right ? 1f : -1f) : 0f;
            rb.rotation = slope;
            rig.SetPose(pose);
        }

        /// <summary>Off a surface along the mirror of the incoming flight, at least <paramref name="minLift"/> degrees
        /// above the horizon, at <paramref name="speed"/>.</summary>
        Vector2 FlingVelocity(Vector2 normal, float speed, float minLift = FlingLift)
        {
            Vector2 dir = lastVelocity.sqrMagnitude > 0.01f ? Vector2.Reflect(lastVelocity.normalized, normal) : normal;
            float side = Mathf.Abs(dir.x) > 0.05f ? Mathf.Sign(dir.x) : -Mathf.Sign(lastVelocity.x == 0f ? 1f : lastVelocity.x);
            float lift = Mathf.Max(Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg, minLift);
            lift = Mathf.Min(lift, FlingMaxLift) * Mathf.Deg2Rad;
            return new Vector2(side * Mathf.Cos(lift), Mathf.Sin(lift)) * speed;
        }

        // CPC (video): off a wall the penguin rebounds in a modest arc, then bounces lively off the floor (about a third of
        // the screen high) and goes on; off a bear it pops up and on at a gentler speed.
        const float FlingSpeed = 6.5f;
        const float BearFlingSpeed = 5f;
        const float FlingKeep = 0.75f;    // speed kept on each later ricochet
        const float FlingLift = 35f;      // degrees: the throw always rises
        const float FlingMaxLift = 75f;   // never straight up: it always travels across
        const float BounceLift = 30f;
        const float RollThroughKeep = 0.85f;   // speed kept each time a rolling pup smashes through a block
        const float TumbleSpeed = 2.5f;   // units/s: slower than this the pup just comes to rest
        const float LevelOutSpeed = 240f; // degrees/s a belly slide turns back to flat at the foot of a slope

        const float IceRebound = 1.4f;    // units/s back and up after a dive breaks an ice block
        const float SlideSpeed = 4f;      // the least speed of a belly slide down a slope
        const float SlideSparks = 0.6f;   // seconds of friction sparks after touching a slope
        float sliding;
        bool hatDropped;

        /// <summary>The first hit knocks the hat off: it flies away on its own (reference §16.2). Knocked off on a bear
        /// that goes down, it lands on that bear's head and tumbles away with it (CPC video, 17/11).</summary>
        void DropHat(Bear onto = null)
        {
            if (hatDropped || !Art.Has("pup_hat")) return;
            hatDropped = true;
            if (Art.Has("pup_ball_bare")) rig.SetBallSprite("pup_ball_bare");   // the ball goes on bare-headed
            rig.GoBare();
            var hat = Art.Spawn("pup_hat", null, (Vector2)transform.position + Vector2.up * 0.3f, 0.45f, 16);
            if (onto != null)
            {
                flyingHat = hat;   // if this pup is gone before the hat lands, OnDestroy lets it fly loose
                StartCoroutine(HatOntoBear(hat, onto));
                return;
            }
            LooseHat(hat);
        }

        /// <summary>The bear decides in its own contact callback whether this hit knocks it out, which may run after
        /// this pup's: wait one physics step, then crown a downed bear or let the hat fly loose.</summary>
        SpriteRenderer flyingHat;

        /// <summary>The bear vanished mid-hop: the hat fades where it is.</summary>
        void DropFlyingHat()
        {
            if (flyingHat != null && flyingHat.GetComponent<FadeAway>() == null) flyingHat.gameObject.AddComponent<FadeAway>().after = 0.4f;
            flyingHat = null;
        }

        void OnDestroy()
        {
            if (!gameObject.scene.isLoaded) return;   // scene unloading: the hat goes with it
            // The hat's hop runs on this pup: destroyed mid-hop (it left the arena), the hat would hang in the air forever.
            // LooseHat needs this pup's collider, gone by now: the hat just fades where it is.
            if (flyingHat != null && flyingHat.transform.parent == null && flyingHat.GetComponent<FadeAway>() == null)
                flyingHat.gameObject.AddComponent<FadeAway>().after = 0.4f;
        }

        IEnumerator HatOntoBear(SpriteRenderer hat, Bear bear)
        {
            yield return new WaitForFixedUpdate();
            if (hat == null) yield break;
            if (bear == null || !bear.IsDown)
            {
                flyingHat = null;
                LooseHat(hat);
                yield break;
            }
            var body = bear.Body.GetComponent<SpriteRenderer>();
            Vector2 from = hat.transform.position;
            for (float t = 0f; t < HatHop; t += Time.deltaTime)
            {
                if (hat == null || bear == null) { DropFlyingHat(); yield break; }
                float k = t / HatHop;
                hat.transform.position = Vector2.Lerp(from, KoHead(body), k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 0.5f;
                hat.transform.rotation = Quaternion.Euler(0, 0, (1f - k) * 360f);
                yield return null;
            }
            if (hat == null || bear == null) { DropFlyingHat(); yield break; }
            // On the knocked-out bear's head, tipped with it, then it spins and drops out of the picture with the bear.
            hat.transform.position = KoHead(body);
            hat.transform.rotation = bear.Body.rotation * Quaternion.Euler(0, 0, body.flipX ? -HatTilt : HatTilt);
            hat.sortingOrder = body.sortingOrder + 1;
            hat.transform.SetParent(bear.Body, true);
            flyingHat = null;
        }

        /// <summary>Head of a knocked-out bear: the bear_ko frames lie on their back with the head at the drawn left end
        /// (the right end when the bear faces left, flipped). Shares measured on bear_ko_0.</summary>
        static Vector2 KoHead(SpriteRenderer body)
        {
            var size = body.sprite != null ? (Vector2)body.sprite.bounds.size : Vector2.one;
            var local = new Vector2(KoHeadX * size.x * (body.flipX ? -1f : 1f), KoHeadY * size.y);
            return body.transform.TransformPoint(local);   // sprite units: TransformPoint applies the scale and the spin
        }

        const float KoHeadX = -0.3f;   // head centre, share of the KO frame width from its centre (drawn facing right)
        const float KoHeadY = 0.12f;   // top of the head, share of the frame height above its centre (the sprite pivot)
        const float HatTilt = 30f;     // the head lies tipped back
        const float HatHop = 0.2f;      // seconds for the hat to arc onto the bear's head

        void LooseHat(SpriteRenderer hat)
        {
            // The hat is a small physics body: it flies off, lands on the rock and lies there a moment, then fades
            // (as a plain particle it used to fall through the floor).
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
            var target = Vector3.one * ArenaScale;   // not localScale: a Pop started on the same contact has shrunk it
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

        /// <summary>Came to rest: sit up dazed where it landed, stars and a smoke puff over its head, then fade out in
        /// place (CPC, live 16/11: the penguin stays on the field dizzy; it never walks away).</summary>
        IEnumerator WalkOff()
        {
            leaving = true;
            rb.simulated = false;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * ArenaScale;
            Fx.Puff(transform.position, 3, 0.3f);
            // Two ways to come to rest, picked by the pup's style (user 17/11: several styles per action): sit up dazed,
            // or stay knocked flat on its back, twitching.
            bool lieFlat = rig.Variant % 2 == 1;
            Fx.DizzyOrbit(transform, new Vector2(0f, lieFlat ? 0.35f : 0.55f), DazedTime + 0.55f);
            rig.SetPose(lieFlat ? PupRig.Pose.Flat : PupRig.Pose.GetUp);
            yield return new WaitForSeconds(0.55f);
            if (done) yield break;
            if (!lieFlat) rig.SetPose(PupRig.Pose.Dizzy);
            Fx.Puff(transform.position + Vector3.up * 0.6f, 4, 0.25f);
            for (float t = 0f; t < DazedTime; t += Time.deltaTime)
            {
                if (done) yield break;
                rig.SetAlpha(1f - Mathf.Clamp01((t / DazedTime - 0.7f) / 0.3f));
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
            if (Latest == this) Latest = null;
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

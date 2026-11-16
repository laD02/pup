using System;
using System.Collections;
using System.Collections.Generic;
using SealCatapult.Core;
using SealCatapult.Progress;
using UnityEngine;

namespace SealCatapult.Launch
{
    /// <summary>
    /// Phase 1 (landscape, fixed screen): the ice pit, one ChatGPT painting (bg_pit) with invisible colliders laid
    /// on its rock. Pups queue on the painted bridge, the front pup reaches down, grabs the rope at the bridge tip and
    /// swings. Under the swing stands the snowball catapult and an operator pup at its lever. Tapping fires a snowball
    /// almost straight up; if it meets the pup, the pup is knocked off along the hit and flies with real physics.
    /// Three outcomes, each with a big call-out and a picture (GDD §R3): a hit at the bottom of the swing is "GREAT!",
    /// any pup out through the top-right gap is "OK!", a pup that hits rock is "OOPS!" and stays stuck (on its back,
    /// kicking) in the icicles or on the floor until the phase ends. A missed snowball can startle a queued pup.
    /// </summary>
    public class LaunchPhase : MonoBehaviour
    {
        // Painting → world: bg_pit is one 1672x941 painting (Assets/Art/Reference/pit_bg_wide3.png) spanning
        // PaintingHeight units of height, centred on ViewCenter. Every layout number below was measured on it in pixels
        // and converted with W(px), H(px). The scale puts the hollow's floor one rope + catapult below the bridge; the
        // painting is then 22.5 units wide (covers 20:9).
        const float PaintingHeight = 12.64f;
        const float PxToWorld = PaintingHeight / 941f;
        static float W(float px) => (px - 836f) * PxToWorld + ViewCenter.x;
        static float H(float px) => (470.5f - px) * PxToWorld + ViewCenter.y;

        // The closed stone box of the reference (report §15.1): a ceiling across the whole top whose right end is cut on
        // a slope, and a right column whose snow-capped top rises to the right PARALLEL to that slope (user 13/11), so
        // the only way out is a diagonal channel between the two.
        public static readonly float LeftWallX = W(285f);
        public static readonly float RightWallX = W(1308f);
        public static readonly float RightWallTop = H(355f);   // the column's inner corner: the channel's lower lip
        static readonly Vector2 ColumnSlopeEnd = new Vector2(W(1672f), H(185f));   // its sloped top climbs to here
        public static readonly float CeilingY = H(150f);       // underside of the cobbles
        static readonly float IcicleTipY = H(220f);            // tips of the icicles under the flat ceiling
        public static readonly float CeilingEndX = W(1025f);   // the flat, icicled part ends here; the slope starts
        // The sloped underside of the ceiling's right end: from its hanging tip up past the top-right corner.
        static readonly Vector2 SlopeTip = new Vector2(W(1030f), H(270f)), SlopeEnd = new Vector2(W(1672f), H(-60f));
        // The floor: a rock block under the bridge with a steep face, a slope down into the hollow where the catapult
        // stands, a low bank rising to the foot of the right column.
        static readonly float BlockTop = H(505f), BlockEndX = W(625f), BlockFaceY = H(640f), HollowFromX = W(790f), HollowToX = W(1190f), BankTop = H(690f);
        static readonly float FloorY = H(772f);                // snow in the hollow
        static readonly float BridgeTop = H(402f);
        static readonly float BridgeEndX = W(722f);
        const float WallThickness = 6f;      // covers the screen edge on 21:9 phones
        public static readonly Vector2 RopeAnchor = new Vector2(W(706f), BridgeTop - 0.05f);
        // The catapult hub sits under the swing bottom so a cup released at ~22° meets the pup at the bottom
        // (see WhackerMachine: Reach * (cos 40°, sin 40°) from the hub is the swing bottom).
        static readonly float SwingBottomY = RopeAnchor.y - SwingRope.IdleLength - PupRig.HangReach;
        static readonly float HubY = SwingBottomY - WhackerMachine.Reach * 0.643f;
        static readonly Vector2 MachinePos = new Vector2(RopeAnchor.x + WhackerMachine.Reach * 0.766f, HubY - WhackerMachine.HubOffsetY);
        static readonly Vector2 OperatorPos = new Vector2(MachinePos.x + 1.75f, FloorY + PupRig.StandHeight);
        public const float PupRadius = 0.91f * PupRig.Scale;
        const float MaxFlightTime = 5f;
        const float FlyOffTime = 1.5f;      // an escaped pup flies on this long at most before it leaves the pit view
        const float BumpGrace = 0.06f;      // ignore contacts right at the launch
        const float LaunchArtAngle = 35f;   // the Launched frames are drawn flying up-right at about 35°
        const float BungeeAimDrop = 0f;      // bungee rail: 40° with the default 48° strikeAngle (escapes from the bob's lower half)
        const float BungeeAssist = 0.7f;     // 0.85 let blind taps through 13 of 14 (QA 12/11, levels 5 and 8); the bob's phase now matters

        public event Action<int> QueueChanged;     // pups not yet launched (queue + the one on the rope)
        public event Action<int> PassedChanged;    // pups out through the gap

        readonly List<PupRig> queue = new List<PupRig>();
        GameTuning tuning;
        SwingRope rope;
        WhackerMachine machine;
        PupRig operatorPup;
        PupRig hanging;
        bool busy;          // grabbing, swinging the mallet, or a pup in flight
        int passed;
        Action<int> onDone;
        bool finished;
        static PhysicsMaterial2D pupMaterial;   // shared: one per Begin leaked a Unity object on every retry

        public int Passed => passed;
        readonly List<Vector2> exits = new List<Vector2>();   // each escaped pup's velocity at the gap, in launch order
        /// <summary>Velocity of every pup as it left through the gap: phase 2 flies each pup on from its own exit.</summary>
        public IReadOnlyList<Vector2> Exits => exits;
        readonly List<PupRig> stuck = new List<PupRig>();   // pups that hit rock: they stay, kicking, as the miss count
        int launchedCount, total;   // pups sent onto the rope so far / in this level (each swings wider than the last)
        LaunchStyle style;
        /// <summary>Velocity given by the most recent hit (for tuning and tests).</summary>
        public Vector2 LastLaunch { get; private set; }
        public int Waiting => queue.Count + (hanging != null ? 1 : 0);
        public bool CanFire => hanging != null && machine.Ready && !busy && GameInput.GameplayEnabled;
        /// <summary>The hanging pup is right of the swing bottom and heading back toward it: the moment a player should tap (for bots and tests).</summary>
        public bool ApproachingBottom => hanging != null && rope.Velocity.x < 0f && HangingPosition.x > RopeAnchor.x + 0.85f && HangingPosition.x < RopeAnchor.x + 1.6f;   // measured on the box pit (13/11): shots from here leave at 34°–41° and get out
        public Vector2 HangingPosition => hanging != null ? (Vector2)hanging.transform.position : RopeAnchor;
        public Vector2 SwingVelocity => hanging != null ? rope.Velocity : Vector2.zero;

        /// <summary>Camera centre for the fixed phase-1 view.</summary>
        public static Vector2 ViewCenter => new Vector2(0f, 0.5f);   // the right wall top (4.0) and the gap above it fit under the top edge

        const float TutorialAssist = 0.6f;   // tutorial levels pull the shot harder to the gap (emulator 13/11: blind taps 0/5 on level 1)
        bool tutorial;

        public void Begin(GameTuning tuning, int pupCount, Action<int> onDone, LaunchStyle style = LaunchStyle.Swing, bool tutorial = false)
        {
            this.tutorial = tutorial;
            this.style = style;
            this.tuning = tuning;
            this.onDone = onDone;
            if (pupMaterial == null) pupMaterial = new PhysicsMaterial2D("Pup") { bounciness = 0.45f, friction = 0.3f };
            total = pupCount;
            BuildPit();
            for (int i = 0; i < pupCount; i++)
            {
                // Each pup gets its own band of sorting orders so limbs never interleave between pups.
                queue.Add(PupRig.Create(transform, QueueSlot(i), 60 - i * 8));
            }
            Report();
            StartCoroutine(NextPup(0.4f));
        }

        // ---------- stage ----------

        void BuildPit()
        {
            // The painting is the whole stage; its sides were widened for 20:9 by Tools/prep_backgrounds.py.
            var bg = Art.Spawn("bg_pit", transform, ViewCenter, 1f, -50);
            bg.name = "Pit Painting";
            float k = PaintingHeight / bg.sprite.bounds.size.y;
            bg.transform.localScale = new Vector3(k, k, 1f);

            Solid("Left Wall", Rect.MinMaxRect(LeftWallX - WallThickness, FloorY - 3f, LeftWallX, CeilingY + 6f));
            Solid("Right Wall", Rect.MinMaxRect(RightWallX, FloorY - 3f, RightWallX + WallThickness, RightWallTop));
            Solid("Ceiling", Rect.MinMaxRect(LeftWallX - WallThickness, IcicleTipY + 0.25f, CeilingEndX, CeilingY + 6f));
            Poly("Ceiling Slope", new Vector2(CeilingEndX, CeilingY + 6f), new Vector2(CeilingEndX, SlopeTip.y), SlopeTip, SlopeEnd,
                new Vector2(SlopeEnd.x + WallThickness, SlopeEnd.y + 3f), new Vector2(SlopeEnd.x + WallThickness, CeilingY + 6f));
            Solid("Floor", Rect.MinMaxRect(LeftWallX - WallThickness, FloorY - 3f, RightWallX + WallThickness, FloorY));
            Poly("Left Block", new Vector2(LeftWallX, FloorY), new Vector2(LeftWallX, BlockTop), new Vector2(BlockEndX, BlockTop), new Vector2(BlockEndX + 0.2f, BlockFaceY), new Vector2(HollowFromX, FloorY));
            Poly("Column Slope", new Vector2(RightWallX, RightWallTop), ColumnSlopeEnd + (ColumnSlopeEnd - new Vector2(RightWallX, RightWallTop)) * 0.5f, new Vector2(ColumnSlopeEnd.x + WallThickness, RightWallTop));
            Poly("Right Bank", new Vector2(HollowToX, FloorY), new Vector2(RightWallX, BankTop), new Vector2(RightWallX, FloorY));
            // Solid only where the pups queue: the tip the rope hangs from is scenery a launched pup flies past.
            Solid("Bridge", Rect.MinMaxRect(LeftWallX, BridgeTop - 1.0f, BridgeEndX - 0.35f, BridgeTop));   // the painted truss, short of the tip the rope hangs from

            rope = SwingRope.Create(RopeAnchor, transform);
            rope.Bungee = style == LaunchStyle.Bungee;
            machine = WhackerMachine.Create(MachinePos, transform, tuning.launcherReloadTime);
            operatorPup = PupRig.Create(transform, OperatorPos, 40);
            operatorPup.Face(false);
            operatorPup.SetPose(PupRig.Pose.LeverReady);
        }

        void Poly(string name, params Vector2[] points)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<PolygonCollider2D>().points = points;   // world points: the phase object sits at the origin
        }

        void Solid(string name, Rect r)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = r.center;
            go.AddComponent<BoxCollider2D>().size = r.size;
        }

        /// <summary>Queue on the bridge, front pup nearest the rope (the bridge is long enough for every pup).</summary>
        static Vector2 QueueSlot(int i) =>
            new Vector2(Mathf.Max(LeftWallX + 0.45f, BridgeEndX - 1.05f - i * 0.6f), BridgeTop + PupRig.StandHeight - 0.05f);   // shortcut: the bridge holds 8; later pups bunch at the wall end (each keeps its own sorting band)

        void Report() => QueueChanged?.Invoke(Waiting);

        void Update()
        {
            for (int i = 0; i < queue.Count; i++)
            {
                var t = queue[i].transform;
                var slot = QueueSlot(i);
                bool moving = ((Vector2)t.position - slot).sqrMagnitude > 0.0025f;
                queue[i].SetPose(moving ? PupRig.Pose.Walk : PupRig.Pose.Idle);
                t.position = Vector2.MoveTowards(t.position, slot, 1.6f * Time.deltaTime);
            }
            if (hanging != null) hanging.SwingSpeed = rope.Omega;
        }

        // ---------- flow ----------

        IEnumerator NextPup(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (queue.Count == 0)
            {
                if (!finished)
                {
                    finished = true;
                    yield return new WaitForSeconds(0.6f);
                    onDone?.Invoke(passed);
                }
                yield break;
            }
            busy = true;
            var pup = queue[0];
            queue.RemoveAt(0);
            var tr = pup.transform;
            // Walk to the end of the bridge, bend down, grab the rope with both flippers and slide onto it.
            pup.SetPose(PupRig.Pose.Walk);
            yield return MoveTo(tr, new Vector2(BridgeEndX - 0.75f, QueueSlot(0).y), 1.6f);
            pup.SetPose(PupRig.Pose.Grab);
            yield return new WaitForSeconds(0.42f);
            AudioManager.Sfx("splash", 0.15f, 0.5f);
            yield return rope.Grab(tr, rope.EndPoint);
            pup.SetPose(PupRig.Pose.Hang);
            rope.Carry(tr, total > 1 ? launchedCount / (float)(total - 1) : 0f);
            launchedCount++;
            hanging = pup;
            busy = false;
            Report();
        }

        /// <summary>Swing the whacker at the pup on the rope. Returns false if not ready.</summary>
        public bool TryFire()
        {
            if (!CanFire) return false;
            StartCoroutine(Whack());
            return true;
        }

        IEnumerator Whack()
        {
            busy = true;
            bool hit = false;
            LastLaunch = Vector2.zero;
            operatorPup.SetPose(PupRig.Pose.LeverPull);
            StartCoroutine(BackToLever());
            yield return machine.Swing(
                () => hanging != null ? (Vector2?)hanging.transform.position : null,
                PupRadius,
                (normal, ballVel) =>
                {
                    hit = true;
                    // Timing grade: a hit near the bottom of the swing is the golden shot, aimed straight at the gap.
                    bool great = rope.AtSweetSpot;
                    Vector2 swing = rope.Release();
                    var pup = hanging;
                    hanging = null;
                    Report();
                    // Knocked along the snowball's flight (plus the contact direction); the swing adds its share.
                    Vector2 raw = ballVel.normalized * tuning.strikePower * 0.7f + normal * tuning.strikePower * 0.3f + swing * tuning.swingInfluence;
                    // The machine's guide rail: pull the raw hit most of the way toward the top-right gap so every hit
                    // heads roughly at the gap, while the timing still bends it short, long, left or right.
                    float rawAngle = Mathf.Atan2(raw.y, raw.x) * Mathf.Rad2Deg;
                    // A bungee pup has no sideways swing to flatten the hit, so the snowball would knock it almost straight up
                    // into the icicles: its rail aims lower and pulls harder (QA 12/11 on the emulator: 6/6 shots stuck).
                    bool bungee = style == LaunchStyle.Bungee;
                    float aim = bungee ? tuning.strikeAngle - BungeeAimDrop : tuning.strikeAngle;
                    float assist = great ? 1f : bungee ? BungeeAssist : tutorial ? TutorialAssist : tuning.aimAssist;
                    float angle = Mathf.Clamp(Mathf.Lerp(rawAngle, aim, assist), 20f, 100f) * Mathf.Deg2Rad;
                    Vector2 v = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * tuning.strikePower;
                    LastLaunch = v;
                    StartCoroutine(Fly(pup, v, great));
                });
            if (!hit)
            {
                busy = false;   // whiff: the pup keeps swinging, try again
                Fx.FloatingText(Loc.T("grr_miss"), OperatorPos + new Vector2(0f, 0.9f), new Color(0.45f, 0.85f, 0.35f), 4.5f, 0.8f);
            }
        }

        /// <summary>The big call-out in the middle of the pit: GREAT / OK / OOPS (always with a picture to match).</summary>
        static void CallOut(string key, Color color) =>
            Fx.FloatingText(Loc.T(key), ViewCenter + new Vector2(0f, 1.2f), color, 13f, 1.1f);

        IEnumerator Fly(PupRig pup, Vector2 velocity, bool great)
        {
            var tr = pup.transform;
            pup.SetPose(PupRig.Pose.Launched);
            pup.Face(true);
            Fx.Burst("spark", tr.position, 8, 5f, 0.35f, 0.2f, 0f, 0.15f, new Color(1f, 0.95f, 0.6f));
            Fx.Puff(tr.position, 4, 0.2f);
            Fx.Puff(MachinePos + new Vector2(-0.4f, 1.3f), 3, 0.15f);   // the machine puffs smoke
            Fx.Shake(great ? 0.25f : 0.15f);
            AudioManager.Sfx("boing", 0.05f);
            var rb = pup.gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = tuning.gravityScale;
            rb.linearDamping = tuning.airDrag;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = pup.gameObject.AddComponent<CircleCollider2D>();
            col.radius = PupRadius / tr.lossyScale.x;
            col.sharedMaterial = pupMaterial;
            rb.linearVelocity = velocity;
            rb.freezeRotation = true;   // the art leans along the flight instead of spinning
            var bump = pup.gameObject.AddComponent<BumpFlag>();

            float age = 0f, trail = 0f;
            bool escaped = false;
            while (true)
            {
                age += Time.deltaTime;
                Vector2 p = tr.position;
                Vector2 v = rb.linearVelocity;
                // Knocked spinning by the snowball for the first moment, then it levels out along its flight.
                float lean = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - LaunchArtAngle;
                tr.rotation = Quaternion.Euler(0, 0, age < 0.35f ? lean - (0.35f - age) / 0.35f * 540f : lean);
                trail -= Time.deltaTime;
                if (trail <= 0f)
                {
                    trail = 0.04f;
                    Fx.Trail(p);
                }
                if (Escaped(p) && rb.linearVelocity.x > 1f)   // over the wall AND still heading out (not rolling on its top)
                {
                    escaped = true;
                    break;
                }
                // The flag catches a graze that starts and ends between two frames (two physics steps in one frame
                // under 50 fps), which IsTouchingLayers alone misses.
                if (age <= BumpGrace) bump.Hit = false;
                else if (rb.IsTouchingLayers() || bump.Hit) break;
                if (age > MaxFlightTime) break;
                yield return null;
            }

            if (escaped)
            {
                // Graded on the outcome: a golden-timed shot that still hit rock is an OOPS, not a GREAT.
                if (great)
                {
                    CallOut("great_hit", new Color(1f, 0.85f, 0.2f));
                    Haptics.Tap();
                    Fx.Burst("star_full", tr.position, 10, 4f, 0.9f, 0.3f, 2f, 0.25f);
                }
                else CallOut("ok_hit", new Color(1f, 0.55f, 0.15f));
                passed++;
                exits.Add(rb.linearVelocity);
                PassedChanged?.Invoke(passed);
                AudioManager.Sfx("star", 0f);
                Fx.Burst("star_full", tr.position, 6, 3f, 0.8f, 0.25f, 2f, 0.2f);
                // It does not stop: it keeps its flight out of the screen toward the fortress (phase 2 picks it up there).
                col.enabled = false;
                for (float t = 0f; t < FlyOffTime && OnScreen(tr.position); t += Time.deltaTime)
                {
                    Vector2 v = rb.linearVelocity;
                    tr.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - LaunchArtAngle);
                    trail -= Time.deltaTime;
                    if (trail <= 0f)
                    {
                        trail = 0.04f;
                        Fx.Trail(tr.position);
                    }
                    yield return null;
                }
                Destroy(pup.gameObject);
            }
            else
            {
                yield return KnockedOut(pup, rb, col);
            }
            if (machine.Ready) operatorPup.SetPose(PupRig.Pose.LeverReady);
            else StartCoroutine(BackToLever());   // the arm is still swinging back: let go once it is home
            busy = false;
            StartCoroutine(NextPup(0.3f));
        }

        /// <summary>Bumped into rock: "OOPS!", stars, and the pup sticks right where it hit until the phase ends, head
        /// into the rock and feet kicking outward along the surface normal: ceiling, slope, wall, ledge or floor alike
        /// (user 13/11: "bị đập vào đâu là dính luôn ở đó").</summary>
        IEnumerator KnockedOut(PupRig pup, Rigidbody2D rb, Collider2D col)
        {
            var tr = pup.transform;
            int n = rb.GetContacts(contactBuffer);
            if (n == 0)
            {
                // Timed out in the air (MaxFlightTime), or a graze that had ended by this frame: nothing to stick to, so it is a miss
                // that drops out of the picture instead of freezing mid-air.
                col.enabled = false;
                CallOut("oops_hit", new Color(0.95f, 0.25f, 0.2f));
                for (float t = 0f; t < FlyOffTime && OnScreen(tr.position); t += Time.deltaTime) yield return null;
                Destroy(pup.gameObject);
                yield break;
            }
            Vector2 normal = contactBuffer[0].normal;
            col.enabled = false;
            Destroy(rb);
            CallOut("oops_hit", new Color(0.95f, 0.25f, 0.2f));
            AudioManager.Sfx("zap");
            Fx.Burst("star_full", tr.position, 5, 2.5f, 0.6f, 0.2f, 2f, 0.15f);
            Fx.Puff(tr.position, 4, 0.2f);
            Fx.Shake(0.1f);
            pup.SetPose(PupRig.Pose.Stuck);
            // The stuck frames are drawn head down, feet up: turned so "up" is the surface normal.
            tr.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f);
            tr.position -= (Vector3)(normal * StuckSink);   // head a little way into the rock, not hovering off it
            yield return Squash(tr, 0.25f);
            Fx.DizzyOrbit(tr, (Vector2)(tr.up * 0.5f), 2.5f);
            stuck.Add(pup);
        }

        const float StuckSink = 0.22f;
        static readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[4];

        /// <summary>The operator lets go of the lever once the arm is back.</summary>
        IEnumerator BackToLever()
        {
            yield return null;
            while (!machine.Ready) yield return null;
            if (operatorPup.Current == PupRig.Pose.LeverPull) operatorPup.SetPose(PupRig.Pose.LeverReady);
        }

        /// <summary>Still inside the fixed phase-1 view (generous: the widest 21:9 screen).</summary>
        static bool OnScreen(Vector2 p) => Mathf.Abs(p.x - ViewCenter.x) < PaintingHeight * 1.2f && p.y < ViewCenter.y + PaintingHeight * 0.6f;

        /// <summary>Out through the gap: above the right wall and right of the ceiling's end.</summary>
        static bool Escaped(Vector2 p) =>
            p.x > RightWallX + PupRadius && p.y > RightWallTop;   // out over the right column, under the sloped ceiling

        // ---------- little animations ----------

        static IEnumerator MoveTo(Transform tr, Vector2 target, float speed)
        {
            while (((Vector2)tr.position - target).sqrMagnitude > 0.0004f)
            {
                float hop = Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.12f;
                Vector2 p = Vector2.MoveTowards((Vector2)tr.position - new Vector2(0, hop), target, speed * Time.deltaTime);
                tr.position = p + new Vector2(0, hop);
                yield return null;
            }
            tr.position = target;
        }

        static IEnumerator Squash(Transform tr, float duration)
        {
            Vector3 s0 = tr.localScale;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / duration * Mathf.PI) * 0.22f;
                tr.localScale = new Vector3(s0.x * (1f + k), s0.y * (1f - k), 1f);
                yield return null;
            }
            tr.localScale = s0;
        }
    }

    /// <summary>Remembers that its body touched something since the flag was last cleared.</summary>
    public class BumpFlag : MonoBehaviour
    {
        public bool Hit;
        void OnCollisionEnter2D(Collision2D _) => Hit = true;
    }
}

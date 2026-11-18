using System;
using System.Collections;
using System.Collections.Generic;
using SealCatapult.Core;
using SealCatapult.Progress;
using SealCatapult.UI;
using UnityEngine;
using Random = UnityEngine.Random;

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
        // Painting → world: bg_pit is one 1672x941 painting (Assets/Art/Reference/pit_bg_wide5.png: wide4 with the bridge one bay longer, user 16/11 "cho toàn màn") spanning
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
        // pit_bg_wide4 (user 16/11: "cắt bớt đất đá bên dưới"): the block now ends under the middle of the bridge, so the
        // swinging pup never passes over rock; measured on the new painting.
        static readonly float BlockTop = H(505f), BlockEndX = W(478f), BlockFaceY = H(680f), HollowFromX = W(620f), HollowToX = W(1190f), BankTop = H(690f);
        static readonly float FloorY = H(772f);                // snow in the hollow
        static readonly float BridgeTop = H(402f);
        static readonly float BridgeEndX = W(935f);   // pit_bg_wide5 (16/11): the bridge painted one bay longer, tip 727 → 940 px
        const float WallThickness = 6f;      // covers the screen edge on 21:9 phones
        public static readonly Vector2 RopeAnchor = new Vector2(W(919f), BridgeTop - 0.05f);
        // The catapult hub sits under the swing bottom so a cup released at ~22° meets the pup at the bottom
        // (see WhackerMachine: Reach * (cos 40°, sin 40°) from the hub is the swing bottom).
        static readonly float SwingBottomY = RopeAnchor.y - SwingRope.IdleLength - PupRig.HangReach;
        static readonly float HubY = SwingBottomY - WhackerMachine.Reach * 0.643f;
        static readonly Vector2 MachinePos = new Vector2(RopeAnchor.x + WhackerMachine.Reach * 0.766f, HubY - WhackerMachine.HubOffsetY);
        static readonly Vector2 OperatorPos = MachinePos + WhackerMachine.OperatorSeat + new Vector2(0f, PupRig.StandHeight);
        public const float PupRadius = 0.91f * PupRig.Scale;
        const float MaxFlightTime = 5f;
        const float FlyOffTime = 1.5f;      // an escaped pup flies on this long at most before it leaves the pit view
        const float BumpGrace = 0.06f;      // ignore contacts right at the launch
        const float LaunchWobble = 18f;     // degrees of tumble right after the strike
        const float BungeeAimDrop = 0f;      // bungee rail: 40° with the default 48° strikeAngle (escapes from the bob's lower half)
        const float BungeeAssist = 0.7f;     // 0.85 let blind taps through 13 of 14 (QA 12/11, levels 5 and 8); the bob's phase now matters

        public event Action<int> QueueChanged;     // pups not yet launched (queue + the one on the rope)
        public event Action<int> PassedChanged;    // pups out through the gap

        readonly List<PupRig> queue = new List<PupRig>();
        // Queue fidgets (CPC, live 16/11: each waiting penguin does its own idle): pup → (time the fidget ends, next one).
        readonly Dictionary<PupRig, Vector2> fidget = new Dictionary<PupRig, Vector2>();
        const float FidgetMin = 1.2f, FidgetMax = 3.5f, FidgetLength = 1.5f;   // two back-and-forth cycles at ~8 fps; several pups busy at once
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
        public bool CanFire => hanging != null && machine.Ready && !busy && GameInput.GameplayEnabled
            && (style != LaunchStyle.Drop || DropInReach());   // drop: a tap counts only when the swing will meet the pup

        /// <summary>Where the falling pup will be when a swing started now reaches it, close enough to the strike point.
        /// Early or late taps are ignored rather than wasting the swing.</summary>
        bool DropInReach()
        {
            float vy = dropVelocity.y, y = hanging.transform.position.y;
            float t = SwingLead;
            float fallT = Mathf.Max(0f, Mathf.Min(t, (vy + DropMaxSpeed) / DropGravity));   // still speeding up for this long
            float predicted = y + vy * fallT - 0.5f * DropGravity * fallT * fallT - DropMaxSpeed * (t - fallT);
            return Mathf.Abs(predicted - DropStrikeY) < DropHitBand;
        }
        /// <summary>The hanging pup is right of the swing bottom and heading back toward it: the moment a player should tap (for bots and tests).</summary>
        public bool ApproachingBottom => hanging != null && (style == LaunchStyle.Drop ? DropInReach() : rope.Velocity.x < 0f && HangingPosition.x > RopeAnchor.x + 0.85f && HangingPosition.x < RopeAnchor.x + 1.6f);   // measured on the box pit (13/11): shots from here leave at 34°–41° and get out
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
            queueGap = pupCount > 1 ? Mathf.Min(MaxQueueGap, QueueSpan / (pupCount - 1)) : MaxQueueGap;
            for (int i = 0; i < pupCount; i++)
            {
                // Each pup gets its own band of sorting orders so limbs never interleave between pups.
                queue.Add(PupRig.Create(transform, QueueSlot(i), 60 - i * QueueOrderStep));
            }
            Report();
            StartCoroutine(NextPup(0.4f));
        }

        // ---------- stage ----------

        void BuildPit()
        {
            // The painting is the whole stage; its sides were widened for 20:9 by Tools/prep_backgrounds.py.
            // Every castle has its own pit painting, same outlines, its own rock and scenery (WorldTheme.Scene).
            int castle = LevelCatalog.CastleOf(SceneLoader.CurrentLevel);
            var bg = Art.Spawn(WorldTheme.Scene("bg_pit", castle), transform, ViewCenter, 1f, -50);
            bg.name = "Pit Painting";
            bg.color = WorldTheme.SceneTint("bg_pit", castle);
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
            // No bridge collider (user 17/11: "ko có kiểu dính vào thanh, phải bay qua thanh"): a struck pup flies past the
            // truss, as in CPC; only the rock stops it.

            rope = SwingRope.Create(RopeAnchor, transform);
            rope.Bungee = style == LaunchStyle.Bungee;
            if (style == LaunchStyle.Drop)
            {
                rope.gameObject.SetActive(false);   // no rope: the pups jump off the bridge, from its lengthened tip

            }
            machine = WhackerMachine.Create(MachinePos, transform, tuning.launcherReloadTime);
            operatorPup = PupRig.Create(transform, OperatorPos, 40);
            operatorPup.SetPose(PupRig.Pose.OpWait);   // front view, its lever on the deck's outer side
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

        /// <summary>Queue on the bridge, front pup nearest the rope. Roomy spacing (user 16/11: "cho hàng đứng thoáng ra"):
        /// the front row holds <see cref="FrontRow"/> pups; the rest wait in a back row a little higher, between them.</summary>
        /// <summary>One even row, as roomy as the level's pup count allows (a back row overlapped the front pups: user
        /// 16/11 "đang lỗi khá nhiều"). The gap is fixed when the queue is built, so pups only step forward afterwards.</summary>
        Vector2 QueueSlot(int i) =>
            new Vector2(Mathf.Max(LeftWallX + 0.45f, BridgeEndX - 1.05f - i * queueGap), BridgeTop + PupRig.StandHeight - 0.05f);

        float queueGap = MaxQueueGap;
        const float MaxQueueGap = 0.85f;
        const float QueueSpan = 7.23f;   // from the first slot (BridgeEndX - 1.05) to the wall margin (LeftWallX + 0.45)
        const int QueueOrderStep = 4;    // 12 pups (8 + School 3 + ad 1) stay above the rope (6) and the machine (8-10)

        void Report() => QueueChanged?.Invoke(Waiting);

        static readonly PupRig.Pose[] Fidgets = { PupRig.Pose.Wave, PupRig.Pose.HatTip, PupRig.Pose.Look, PupRig.Pose.Hop, PupRig.Pose.Dance, PupRig.Pose.Yawn, PupRig.Pose.Sneeze };   // front view, like the idle (Cheer is side view)
        readonly Dictionary<PupRig, PupRig.Pose> fidgetPose = new Dictionary<PupRig, PupRig.Pose>();

        /// <summary>A waiting pup now and then waves, tips its helmet, looks around, hops, dances, yawns or sneezes, each on
        /// its own clock.</summary>
        PupRig.Pose IdleFidget(PupRig pup)
        {
            float now = Time.time;
            if (!fidget.TryGetValue(pup, out var f)) f = new Vector2(0f, now + Random.Range(FidgetMin, FidgetMax));
            if (now >= f.y)
            {
                f = new Vector2(now + FidgetLength, now + FidgetLength + Random.Range(FidgetMin, FidgetMax));
                fidgetPose[pup] = Fidgets[Random.Range(0, Fidgets.Length)];
            }
            fidget[pup] = f;
            return now < f.x && fidgetPose.TryGetValue(pup, out var pose) ? pose : PupRig.Pose.Idle;
        }

        void Update()
        {
            for (int i = 0; i < queue.Count; i++)
            {
                var t = queue[i].transform;
                var slot = QueueSlot(i);
                bool moving = ((Vector2)t.position - slot).sqrMagnitude > 0.0025f;
                if (moving) fidget.Remove(queue[i]);   // a fresh, random wait once it stops: no fidgets in unison after a shift
                queue[i].SetPose(moving ? PupRig.Pose.Walk : IdleFidget(queue[i]));
                t.position = Vector2.MoveTowards(t.position, slot, 1.6f * Time.deltaTime);
            }
            if (hanging != null)
            {
                hanging.SwingSpeed = rope.Omega;
                hanging.DrivenPhase = rope.Phase;
                hanging.SwingLean = style == LaunchStyle.Swing ? rope.Lean : (float?)null;
            }
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
            fidget.Remove(pup);
            fidgetPose.Remove(pup);
            pup.SetPose(PupRig.Pose.Walk);
            yield return MoveTo(tr, new Vector2((style == LaunchStyle.Drop ? DropX - 0.35f : BridgeEndX - 0.75f), QueueSlot(0).y), style == LaunchStyle.Drop ? 3f : 1.6f);
            // CPC (live 16/11): a short crouch at the bridge tip, flippers forward, before reaching for the rope.
            pup.SetPose(PupRig.Pose.Crouch);
            yield return new WaitForSeconds(style == LaunchStyle.Drop ? EdgeCrouch * 0.4f : EdgeCrouch);
            if (style == LaunchStyle.Drop)
            {
                yield return JumpOff(pup);
                yield break;
            }
            // Then it springs up into the hanging grip and rides the rope end down (the front-view reach frames, shown while
            // the body moved down to the rope, looked like the pup sinking through the bridge: user 16/11 queue check).
            pup.SetPose(style == LaunchStyle.Bungee ? PupRig.Pose.Bungee : PupRig.Pose.Hang);   // each launcher has its own drawn ride
            tr.position += Vector3.up * GrabHop;   // the hanging picture is taller than the standing one: lift it clear of the bridge
            AudioManager.Sfx("splash", 0.15f, 0.5f);
            yield return rope.Grab(tr, rope.EndPoint);
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
            operatorPup.SetPose(PupRig.Pose.OpPull);
            StartCoroutine(BackToLever());
            yield return machine.Swing(
                () => hanging != null ? (Vector2?)hanging.transform.position : null,
                PupRadius,
                (normal, ballVel) =>
                {
                    hit = true;
                    // Timing grade: a hit near the bottom of the swing is the golden shot, aimed straight at the gap.
                    bool great = rope.AtSweetSpot;
                    Vector2 swing = style == LaunchStyle.Drop ? dropVelocity : rope.Release();
                    // CPC (video 17/11): a white flash where the snowball meets the pup, and gold sparkles left at the
                    // empty rope end.
                    if (hanging != null) Fx.Flash(hanging.transform.position, 0.8f);
                    if (style != LaunchStyle.Drop)
                        Fx.Burst("spark", rope.EndPoint + Vector2.up * PupRig.HangReach, 8, 1.4f, 0.45f, 0.14f, 0f, 0.4f, new Color(1f, 0.85f, 0.25f));
                    dropVelocity = Vector2.zero;
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
                    bool bungee = style != LaunchStyle.Swing;   // bungee and drop: no sideways swing, the same lower rail
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
        // CPC (live 16/11): the grade sits tilted under the bridge on the left and holds still, not drifting up mid-screen.
        static void CallOut(string key, Color color) =>
            Fx.FloatingText(Loc.T(key), new Vector2((LeftWallX + BridgeEndX) * 0.5f + 0.4f, BridgeTop - 1.7f), color, 13f, 1.1f, 10f, 0f);

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
                // CPC (video at 60 fps, 17/11): no spin. Head first along the flight, flippers back, with a small wobble
                // that dies out as the pup leaves.
                float lean = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - pup.ArtAngle;
                tr.rotation = Quaternion.Euler(0, 0, lean + LaunchWobble * Mathf.Sin(age * 18f) * Mathf.Exp(-age * 3f));
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
                    tr.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - pup.ArtAngle);
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
            if (machine.Ready) operatorPup.SetPose(PupRig.Pose.OpWait);
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
            col.enabled = false;
            Vector2 normal, contact;
            if (n > 0)
            {
                normal = contactBuffer[0].normal;   // out of the rock, toward the pup
                contact = contactBuffer[0].point;
            }
            else if (!NearestRock(tr.position, out contact, out normal))
            {
                // Timed out in the air (MaxFlightTime) with no rock in reach: nothing to stick to, so it is a miss
                // that drops out of the picture instead of freezing mid-air.
                CallOut("oops_hit", new Color(0.95f, 0.25f, 0.2f));
                for (float t = 0f; t < FlyOffTime && OnScreen(tr.position); t += Time.deltaTime) yield return null;
                Destroy(pup.gameObject);
                yield break;
            }
            Destroy(rb);
            CallOut("oops_hit", new Color(0.95f, 0.25f, 0.2f));
            AudioManager.Sfx("zap");
            Fx.Burst("star_full", tr.position, 5, 2.5f, 0.6f, 0.2f, 2f, 0.15f);
            Fx.Puff(tr.position, 4, 0.2f);
            Fx.Shake(0.1f);
            pup.GoBare();   // CPC (live 16/11): a stuck penguin has lost its helmet
            KnockHatOff(tr.position, normal);
            Vector2 spot = contact;   // user 17/11: "dính đè nhau cx đc" — pups may pile on the same spot
            // One splat for every rock face (user 17/11: "kiểu nằm dính hình như bị sai, vẽ lại"; CPC: flat as a pancake,
            // limbs spread): the frames have the pup's back on their top line (HangReach above the root), laid on the rock.
            pup.SetPose(PupRig.Pose.Stuck);
            float scale = tr.lossyScale.y;
            tr.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(-normal.y, -normal.x) * Mathf.Rad2Deg - 90f);   // up = into the rock
            float reach = PupRig.HangReach * scale;
            tr.position = (Vector3)(spot + normal * (reach - StuckSink));
            yield return Squash(tr, 0.25f);
            Fx.DizzyOrbit(tr, (Vector2)normal * 0.1f, 2.5f);   // over the face
            stuck.Add(pup);
        }

        /// <summary>A graze whose contact had already ended by this frame (QA 17/11: the second pup at the same spot
        /// bounced off and fell, helmet on): stick it to the closest rock face within reach instead.</summary>
        static bool NearestRock(Vector2 at, out Vector2 point, out Vector2 normal)
        {
            point = normal = Vector2.zero;
            int n = Physics2D.OverlapCircle(at, PupRadius * GrazeReach, ContactFilter2D.noFilter, overlapBuffer);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = overlapBuffer[i];
                if (c.isTrigger || (c.attachedRigidbody != null && c.attachedRigidbody.bodyType != RigidbodyType2D.Static)) continue;   // rock only, not the snowball
                Vector2 p = c.ClosestPoint(at);
                float d = (p - at).sqrMagnitude;
                if (d < 1e-6f || d >= best) continue;   // inside a collider: no direction to read
                best = d;
                point = p;
                normal = (at - p).normalized;
            }
            return best < float.MaxValue;
        }

        const float GrazeReach = 1.8f;   // × pup radius
        static readonly Collider2D[] overlapBuffer = new Collider2D[8];

        /// <summary>The helmet flies off on its own and tumbles down out of the picture (CPC: it never stays on the
        /// stuck penguin).</summary>
        static void KnockHatOff(Vector2 at, Vector2 normal)
        {
            if (!Art.Has("pup_hat")) return;
            var hat = Fx.Particle("pup_hat", at + normal * 0.2f, HatSize, 16);
            hat.velocity = normal * 1.5f + new Vector2(Random.Range(-1f, 1f), 1.5f);
            hat.gravity = 9f;
            hat.drag = 0.2f;
            hat.spin = Random.Range(-500f, 500f);
            hat.life = 1.6f;
        }


        const float HatSize = 0.42f;

        // ---------- drop launch (no rope) ----------

        Vector2 dropVelocity;
        // Drop levels: the bridge reaches a little further (user 16/11: "cây cầu nối dài ra") and the pup jumps from its
        // new tip straight down a line the snowball crosses at 60° of its sweep, high and fast, aimed up at the gap.
        static readonly float DropX = RopeAnchor.x + DropReach;
        const float DropReach = 0.47f;                     // just past the tip: the pup jumps off its end
        static float DropStrikeY => HubY + WhackerMachine.Reach * Mathf.Sin(DropStrikeAngle * Mathf.Deg2Rad);
        const float DropStrikeAngle = 60f;
        const float SwingLead = 0.31f;                     // tap → snowball at 60° (0.08 s wind-up + most of the sweep)
        const float DropHitBand = 0.75f;                   // the predicted pup must be this close to the strike point
        // User 16/11 "cho hải cẩu nhảy nhanh vào": a quick hop and a brisk fall, still about 0.35 s in the snowball's reach.
        const float DropGravity = 14f, DropMaxSpeed = 7.5f;
        const float DropHopTime = 0.18f, DropHopHeight = 0.4f;


        /// <summary>The pup jumps off the bridge tip and falls straight past the catapult's swing: the player whacks it
        /// on the way down. Missed, it lands dazed on the snow and the next pup comes.</summary>
        IEnumerator JumpOff(PupRig pup)
        {
            var tr = pup.transform;
            Vector2 from = tr.position, to = new Vector2(DropX, from.y + DropHopHeight);
            pup.SetPose(PupRig.Pose.DropJump);
            AudioManager.Sfx("boing", 0.1f, 0.6f);
            for (float t = 0f; t < DropHopTime; t += Time.deltaTime)
            {
                float k = t / DropHopTime;
                tr.position = Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 0.3f;
                yield return null;
            }
            pup.SetPose(PupRig.Pose.DropFall);
            launchedCount++;
            hanging = pup;
            busy = false;
            Report();
            dropVelocity = Vector2.zero;
            float landY = FloorY + PupRig.StandHeight;
            while (hanging == pup)
            {
                dropVelocity.y = Mathf.Max(dropVelocity.y - DropGravity * Time.deltaTime, -DropMaxSpeed);
                tr.position += (Vector3)(dropVelocity * Time.deltaTime);
                if (tr.position.y <= landY && hanging == pup)
                {
                    // Missed: it plops on the snow, dazed, and fades; no pup left the pit.
                    hanging = null;
                    tr.position = new Vector3(tr.position.x, landY, tr.position.z);
                    Report();
                    CallOut("oops_hit", new Color(0.95f, 0.25f, 0.2f));
                    Fx.Puff(tr.position, 6, 0.3f);
                    AudioManager.Sfx("thud", 0.1f);
                    pup.SetPose(PupRig.Pose.Dizzy);
                    Fx.DizzyOrbit(tr, new Vector2(0f, 0.55f), 1.2f);
                    for (float t = 0f; t < 1.2f; t += Time.deltaTime)
                    {
                        pup.SetAlpha(1f - Mathf.Clamp01((t - 0.8f) / 0.4f));
                        yield return null;
                    }
                    Destroy(pup.gameObject);
                    StartCoroutine(NextPup(0.2f));
                    yield break;
                }
                yield return null;
            }
        }

        const float StuckSink = 0.05f;   // a hair into the rock
        const float EdgeCrouch = 0.25f;
        const float GrabHop = 0.22f;
        static readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[4];

        /// <summary>The operator lets go of the lever once the arm is back.</summary>
        IEnumerator BackToLever()
        {
            yield return null;
            while (!machine.Ready) yield return null;
            if (operatorPup.Current == PupRig.Pose.OpPull) operatorPup.SetPose(PupRig.Pose.OpWait);
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

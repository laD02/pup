using UnityEngine;

namespace SealCatapult.Core
{
    /// <summary>
    /// The hero: a harp seal pup drawn frame by frame (Resources/Art/pup_&lt;anim&gt;_&lt;i&gt;, cut from the ChatGPT
    /// sheets by Tools/slice_sheets.py). One sprite renderer flips frames per pose; nothing is assembled from parts.
    /// Root origin = body centre. Each animation's canvas has an anchor (see ANCHOR in slice_sheets.py):
    /// bottom = feet, rope = top of the drawn rope (the rope end in game), center = body centre.
    /// </summary>
    public class PupRig : MonoBehaviour
    {
        public enum Pose { Idle, Walk, Crouch, Leap, Hang, Flail, Launched, Fall, Cheer, Glide, Ball, Dizzy, LeverReady, LeverPull, Fly, Dive, GetUp, NinjaHang, Grab, Stuck, Flinch, Spin, Wave, HatTip, Look, Hop, Dance, Yawn, Sneeze, OpWait, OpPull, DropJump, DropFall, Slide, Tumble, Flat, Bungee, StuckSlope, Flung, Roll }

        enum Anchor { Bottom, Rope, Center }

        /// <summary>World units per 100 art pixels (standing pup = 300 px ≈ 1.38 world units).</summary>
        public const float Scale = 0.40f;   // user 12/11: pups a little smaller, bears a little bigger (was 0.46)
        /// <summary>Rope end above the body centre while hanging, world units.</summary>
        public const float HangReach = 1.278f * Scale;
        /// <summary>Body centre above the feet when standing, world units.</summary>
        public const float StandHeight = 1.5f * Scale;
        public const float BallWidth = 2.28f * Scale;

        const float WalkStep = 0.11f;      // world units walked per walk frame
        static readonly int[] Breath = { 0, 1, 2, 1 };
        const int IdleStill = 2;            // pup_idle_2: the frame used for the still idle
        const float BreathDepth = 0.018f;   // squash and stretch of the code breath
        const float BreathSpeed = 2.4f;     // radians per second
        float breathPhase;

        sealed class Anim
        {
            public Sprite[] Frames;
            public float Fps;
            public int LoopFrom;            // frames before this play once (e.g. the dizzy "oh!" frame)
            public bool PingPong;           // loop 0-1-2-3-2-1: smoother from four drawings (the queue fidgets)
            public Anchor Anchor;
            public float ArtAngle;          // heading the frames are drawn flying at, degrees (flight sets)
        }

        static Anim hangFast, opWait, opPull, dropJump, dropFall;
        const float FastSwing = 1.8f, FastSwingOff = 1.4f;   // rope rad/s: into the excited kicking set and back out (the carried swing peaks near 2.3)
        static Anim idle, walk, crouch, leap, hang, flail, launched, fall, cheer, glide, dizzy, leverReady, leverPull, fly, dive, getUp, ninjaHang, grab, stuck, flinch, spin, wave, hatTip, look, hop, dance, yawn, sneeze;
        static Anim walkBare, cheerBare, dizzyBare, getUpBare, idleBare, flailBare, stuckBare;
        static Anim slide, tumble, stuckWall, flat;   // arena, after the first hit (always bare-headed by then)
        // Several drawn styles per action (user 17/11: "mỗi loại hoạt ảnh sẽ có nhiều kiểu"): each pup picks one of each
        // at random (Variant), so no two pups fly, crash or tumble alike. Sets not drawn yet are left out.
        static Anim[] launchedSets, flungSets;
        // Sheet T2 (user 17/11: "học kiểu của cpc"; CPC live: a penguin under the ceiling lies on its back, face and belly
        // showing, flippers spread; one on a slope lies flat along it on its belly). Ceiling frames hang from their top
        // line (the back), slope frames stand on their bottom line (the belly).
        static Anim stuckCeiling, stuckSlope;
        // Sheets F2 + R2 (user 17/11: "phải vẽ lại văng … cpc có kiểu văng lộn lung tung các hướng và kiểu lộn tròn"):
        // hunched flung pose the code tumbles in the air, and a curled-round pup the code rolls along the ground.
        static Anim flung, roll;
        public static bool StuckIsDrawn => stuckCeiling != null;
        // Sheets Y/Z (user 17/11: swing, bungee and no-rope drop each get their own, gently funny animation): the drawn
        // swing and the upside-down bungee bounce. Their frame follows the rope (DrivenPhase), not a clock.
        static Anim swingDrawn, bungee;

        /// <summary>The swing frames lean with the swing themselves, so the rope should not tilt the body further.</summary>
        public static bool SwingIsDrawn => swingDrawn != null;

        /// <summary>0..1 position in a rope-driven loop (swing: left-to-right then back; bungee: sinking then rising), set
        /// each frame by the launcher. Null lets the frames run on their clock.</summary>
        public float? DrivenPhase { get; set; }

        /// <summary>Rope angle over the swing's reach, −1 (far left) .. 1 (far right), set by the launcher while swinging.
        /// The drawn swing is lopsided (QA 17/11, user: "lắc lư trên dây còn chưa đều 2 bên"), so only its left half is
        /// used (frames 0 = far left .. SwingCentre = hanging straight) and mirrored for the right half: both ends match.</summary>
        public float? SwingLean { get; set; }
        const int SwingCentre = 4;

        SpriteRenderer sr, ballSr;
        Anim current;
        float frameTime, blinkTimer, walkDistance;
        int frame;
        Vector3 lastPos;

        public Pose Current { get; private set; } = Pose.Idle;
        /// <summary>Rope angular speed (rad/s) while hanging; a faster swing flips the hang frames faster.</summary>
        public float SwingSpeed { get; set; }

        public static PupRig Create(Transform parent, Vector2 position, int order)
        {
            var root = new GameObject("Pup").transform;
            root.SetParent(parent, false);
            root.position = position;
            var rig = root.gameObject.AddComponent<PupRig>();
            rig.Build(order);
            return rig;
        }

        static void LoadAnims()
        {
            if (idle != null) return;
            // One still frame (sheet I's cleanest): ChatGPT drew the four breathing frames at slightly different sizes, and
            // cycling them made the queue flicker (user 16/11). The breath is a gentle squash in code instead.
            idle = Load("idle", 1, 1f, 0, Anchor.Bottom, IdleStill) ?? Load("idle", 4, 3f, 0, Anchor.Bottom);
            walk = LoadAll("walk", 1f);   // front-view waddle (sheet O: 8 frames); steps follow the distance walked
            if (walk != null) walk.PingPong = false;
            crouch = Load("ready", 1, 0f, 0, Anchor.Bottom) ?? Load("leap", 1, 0f, 0, Anchor.Bottom);   // sheet K front view, else leap_0
            leap = Load("leap", 3, 10f, 2, Anchor.Center, 1);       // leap_1..3 once, then hold the grab pose
            hang = Swing(Load("hang", 4, 5f, 0, Anchor.Rope));
            hangFast = Swing(Load("hang_fast", 4, 9f, 0, Anchor.Rope)) ?? hang;
            swingDrawn = Loop("swing", 1f, Anchor.Rope);
            if (swingDrawn != null) hang = hangFast = swingDrawn;   // one drawn swing replaces the plain and fast sets
            bungee = Loop("bungee", 1f, Anchor.Rope) ?? hang;
            // Sheets P and Q (16/11, drawn for these jobs, not borrowed): the operator at its own lever on the cart's deck,
            // and the no-rope launch.
            dropJump = Load("drop_jump", 8, 30f, 7, Anchor.Center);   // over in the 0.25 s hop
            dropFall = Swing(LoadAll("drop_fall", 0.7f)) ?? dropJump;
            if (dropFall != null) dropFall.Anchor = Anchor.Center;   // sheet K: legs kicking on a fast swing
            flail = Load("flail", 4, 10f, 0, Anchor.Center);
            launched = Load("launch", 4, 10f, 0, Anchor.Center);
            fall = Load("fall", 4, 8f, 0, Anchor.Center);
            leverReady = Load("lever", 4, 3f, 0, Anchor.Bottom);
            leverPull = Load("lever", 4, 14f, 3, Anchor.Bottom, 4);  // lever_4..7 once, then hold the release
            // Below their fallbacks, which must be loaded first.
            opWait = LoadAll("op_wait", 2.6f, pingPong: false) ?? leverReady;
            opPull = Load("op_pull", 8, 12f, 7, Anchor.Bottom) ?? leverPull;   // once, then hold the last frame
            cheer = Load("cheer", 4, 8f, 0, Anchor.Bottom);
            glide = Load("glide", 4, 8f, 0, Anchor.Center);
            dizzy = Load("dizzy", 4, 6f, 1, Anchor.Bottom);
            // Sheet E (docs/pup-art-brief-2.md): level flight with flapping flippers, the nose-down turn, getting up after
            // a landing, the ninja hang. Until it is drawn, the nearest older set stands in (see Fallback).
            fly = Load("fly", 4, 9f, 0, Anchor.Center) ?? glide;
            dive = Load("dive", 2, 10f, 1, Anchor.Center) ?? launched;
            getUp = Load("getup", 4, 7f, 3, Anchor.Bottom) ?? dizzy;
            ninjaHang = Load("ninja", 1, 1f, 0, Anchor.Center) ?? flail;
            // Sheet F (v10): bend down and grab the rope (frame 3, already on the rope, is left to the hang set),
            // stuck on its back kicking, startled by a stray snowball, curled ball spinning.
            grab = Load("reach", 3, 7f, 2, Anchor.Bottom) ?? Load("grab", 3, 7f, 2, Anchor.Bottom) ?? crouch;   // sheet K front view
            stuck = Load("stuck", 4, 8f, 0, Anchor.Center) ?? dizzy;
            flinch = Load("flinch", 4, 7f, 3, Anchor.Bottom) ?? dizzy;
            spin = Load("spin", 4, 12f, 0, Anchor.Center) ?? flail;
            idleBare = Load("idle_bare", 1, 1f, 0, Anchor.Bottom, IdleStill) ?? Load("idle_bare", 4, 3f, 0, Anchor.Bottom);   // still + code breath, like idle
            flailBare = Load("flail_bare", 4, 10f, 0, Anchor.Center);
            walkBare = Load("walk_bare", 6, 0f, 0, Anchor.Bottom);
            cheerBare = Load("cheer_bare", 4, 8f, 0, Anchor.Bottom);
            dizzyBare = Load("dizzy_bare", 4, 6f, 1, Anchor.Bottom);
            getUpBare = Load("getup_bare", 4, 7f, 3, Anchor.Bottom);
            // Sheet H (CPC live study 16/11): queue fidgets and the stuck pup that lost its helmet (drawn lying beside it).
            wave = LoadAll("wave", 0.9f) ?? cheer;
            hatTip = LoadAll("hattip", 1.3f) ?? idle;
            look = LoadAll("look", 1.5f) ?? idle;
            stuckBare = Load("stuck_bare", 4, 6f, 0, Anchor.Center);
            // Sheet T (17/11): flat on its back against the rock, kicking; the frame bottom is the rock surface.
            stuckWall = Loop("stuck_wall", 0.8f, Anchor.Bottom);
            // Sheets U-X (17/11, CPC-like variety): flung rag-doll styles in place of the curled ball, a knocked-flat rest,
            // two more launch flights, two more ways to be stuck in the rock (head at the frame top = the rock).
            flat = Loop("flat", 1f, Anchor.Bottom);
            flungSets = Sets(tumble, Loop("fling_cartwheel", 1f, Anchor.Center)   /* drawn as two turns */, Loop("fling_backflip", 0.57f, Anchor.Center),
                Loop("fling_corkscrew", 0.57f, Anchor.Center));
            launchedSets = Sets(Angled(launched, 35f), Angled(Loop("launch_panic", 0.57f, Anchor.Center), 25f),
                Angled(Loop("launch_hero", 0.8f, Anchor.Center), 15f));
            // Sheet X2: head in a snow splat at the frame top (anchored there like a rope frame), kicking or hanging limp.
            stuckCeiling = Loop("stuck_splat", 1f, Anchor.Rope) ?? Loop("stuck_ceiling", 0.8f, Anchor.Rope);
            stuckSlope = stuckCeiling;
            flung = Loop("flung", 0.8f, Anchor.Center);
            roll = Loop("roll", 0.8f, Anchor.Center) ?? flung;
            // Sheet J (user 16/11: "nhiều hoạt ảnh ngộ nghĩnh"): more queue fidgets, front view (all cut feet-down,
            // so the hop reads as a squash-and-stretch on the spot).
            hop = LoadAll("hop", 0.8f) ?? wave;
            dance = LoadAll("dance", 0.9f) ?? wave;
            yawn = LoadAll("yawn", 1.5f) ?? look;
            sneeze = LoadAll("sneeze", 1.5f, pingPong: false) ?? hatTip;
            // Sheet S (CPC video 17/11): after the bounces the pup skids on its belly, or somersaults like a rag doll.
            slide = Loop("slide", 0.67f, Anchor.Center) ?? flailBare ?? flail;
            tumble = Loop("tumble", 0.57f, Anchor.Center) ?? spin;
        }

        static Anim Angled(Anim a, float degrees)
        {
            if (a != null) a.ArtAngle = degrees;
            return a;
        }

        /// <summary>Heading the frames on show are drawn flying at (0 for sets that are not drawn in flight).</summary>
        public float ArtAngle => current != null ? current.ArtAngle : 0f;

        /// <summary>The drawn sets among <paramref name="sets"/> (missing files are null and dropped).</summary>
        static Anim[] Sets(params Anim[] sets) => System.Array.FindAll(sets, a => a != null);

        /// <summary>This pup's pick from a list of styles (null when none is drawn).</summary>
        Anim Pick(Anim[] sets) => sets == null || sets.Length == 0 ? null : sets[variant % sets.Length];

        /// <summary>Style seed of this pup: which of several drawn sets it uses for each action.</summary>
        public int Variant { get => variant; set => variant = Mathf.Abs(value); }
        int variant;   // rolled in Build: Unity forbids Random in a MonoBehaviour field initializer

        /// <summary>Marks a set to loop back and forth (null passes through).</summary>
        static Anim Swing(Anim a)
        {
            if (a != null) a.PingPong = true;
            return a;
        }

                /// <summary>A queue fidget with as many frames as were drawn (8 from the two-row sheets, else 4), timed so one
        /// back-and-forth cycle (or one pass when <paramref name="pingPong"/> is off) lasts <paramref name="cycle"/> s.</summary>
        static Anim LoadAll(string name, float cycle, bool pingPong = true)
        {
            int count = 0;
            while (count < 16 && Art.Has($"pup_{name}_{count}")) count++;
            if (count == 0) return null;
            var a = Load(name, count, (pingPong ? count * 2 - 2 : count) / cycle, 0, Anchor.Bottom);
            a.PingPong = pingPong;
            return a;
        }

        /// <summary>A looping set with as many frames as were drawn (16 for the smoothed sets, user 17/11: "tăng khung
        /// hình lên để hoạt ảnh mượt mà"), timed so one loop lasts <paramref name="cycle"/> s whatever the count.</summary>
        static Anim Loop(string name, float cycle, Anchor anchor)
        {
            int count = 0;
            while (count < 32 && Art.Has($"pup_{name}_{count}")) count++;
            return count == 0 ? null : Load(name, count, count / cycle, 0, anchor);
        }

        /// <summary>Loads a frame set; null when its first frame is missing (optional sets fall back to an older set).</summary>
        static Anim Load(string name, int count, float fps, int loopFrom, Anchor anchor, int first = 0)
        {
            if (!Art.Has($"pup_{name}_{first}")) return null;
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++) frames[i] = Art.Get($"pup_{name}_{first + i}");
            return new Anim { Frames = frames, Fps = fps, LoopFrom = loopFrom, Anchor = anchor };
        }

        void Build(int order)
        {
            LoadAnims();
            sr = new GameObject("Sprite").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.transform.localScale = Vector3.one * Scale;
            sr.sortingOrder = order;

            ballSr = new GameObject("Ball").AddComponent<SpriteRenderer>();
            ballSr.transform.SetParent(transform, false);
            ballSr.sortingOrder = order;
            SetBallSprite("pup_ball");
            ballSr.enabled = false;

            variant = Random.Range(0, 1 << 16);
            blinkTimer = Random.Range(1.5f, 4f);
            frameTime = Random.value;
            breathPhase = Random.value * Mathf.PI * 2f;   // pups in the queue breathe out of step
            lastPos = transform.position;
            Play(idle);
            Place();
        }

        public void SetPose(Pose pose)
        {
            if (Current == pose) return;
            Current = pose;
            bool ball = pose == Pose.Ball;
            ballSr.enabled = ball;
            sr.enabled = !ball;
            if (ball) return;
            Play(For(pose));
            Place();
        }

        bool bare;

        /// <summary>The hat is gone (knocked off in the arena): from now on the bare-headed frame sets are used where
        /// they exist (get up, walk, dizzy, cheer).</summary>
        public void GoBare() => bare = true;

        /// <summary>World height of the frame on show (rotation ignored).</summary>
        public float FrameHeight => sr != null && sr.sprite != null ? sr.sprite.bounds.size.y * sr.transform.lossyScale.y : StandHeight;

        Anim For(Pose pose)
        {
            if (bare)
                switch (pose)
                {
                    case Pose.Idle: return idleBare ?? idle;
                    case Pose.Flail: return flailBare ?? flail;
                    case Pose.Walk: return walkBare ?? walk;
                    case Pose.Cheer: return cheerBare ?? cheer;
                    case Pose.Dizzy: return dizzyBare ?? dizzy;
                    case Pose.GetUp: return getUpBare ?? getUp;
                    case Pose.Stuck: return stuckCeiling ?? stuckBare ?? stuck;
                    case Pose.StuckSlope: return stuckSlope ?? stuckBare ?? stuck;
                    case Pose.Flung: return flung ?? flailBare ?? flail;
                    case Pose.Roll: return roll ?? flailBare ?? flail;
                }
            switch (pose)
            {
                case Pose.Walk: return walk;
                case Pose.Crouch: return crouch;
                case Pose.Leap: return leap;
                case Pose.Hang: return hang;
                case Pose.Bungee: return bungee;
                case Pose.Flail: return flail;
                case Pose.Launched: return Pick(launchedSets) ?? launched;
                case Pose.Fall: return fall;
                case Pose.LeverReady: return leverReady;
                case Pose.LeverPull: return leverPull;
                case Pose.OpWait: return opWait;
                case Pose.OpPull: return opPull;
                case Pose.DropJump: return dropJump;
                case Pose.DropFall: return dropFall;
                case Pose.Cheer: return cheer;
                case Pose.Glide: return glide;
                case Pose.Dizzy: return dizzy;
                case Pose.Fly: return fly;
                case Pose.Dive: return dive;
                case Pose.GetUp: return getUp;
                case Pose.NinjaHang: return ninjaHang;
                case Pose.Grab: return grab;
                case Pose.Stuck: return stuck;
                case Pose.Flinch: return flinch;
                case Pose.Spin: return spin;
                case Pose.Wave: return wave;
                case Pose.HatTip: return hatTip;
                case Pose.Look: return look;
                case Pose.Hop: return hop;
                case Pose.Dance: return dance;
                case Pose.Yawn: return yawn;
                case Pose.Sneeze: return sneeze;
                case Pose.Slide: return slide;
                case Pose.Tumble: return Pick(flungSets) ?? tumble;
                case Pose.Flat: return flat ?? dizzyBare ?? dizzy;
                case Pose.Flung: return flung ?? flail;
                default: return idle;
            }
        }

        void Play(Anim anim)
        {
            var sc = sr.transform.localScale;
            sr.transform.localScale = new Vector3(Mathf.Sign(sc.x) * Scale, Scale, 1f);   // undo the idle breath
            current = anim;
            frame = 0;
            frameTime = 0f;
            Show(0);
        }

        void Show(int i)
        {
            var sprite = current.Frames[i];
            if (sr.sprite != sprite) sr.sprite = sprite;
        }

        /// <summary>Puts the canvas anchor on the right point of the root (feet, rope end or body centre).</summary>
        void Place()
        {
            if (sr.sprite == null) return;
            float half = sr.sprite.bounds.extents.y * sr.transform.localScale.y;
            float y;
            switch (current.Anchor)
            {
                case Anchor.Bottom: y = -StandHeight + half; break;
                case Anchor.Rope: y = HangReach - half; break;
                default: y = 0f; break;
            }
            sr.transform.localPosition = new Vector3(0f, y, 0f);
        }

        /// <summary>Curled-up look: plain ball or the Ice Spikes ball.</summary>
        public void SetBallSprite(string sprite)
        {
            ballSr.sprite = Art.Get(sprite);
            Art.FitWidth(ballSr, BallWidth);
        }

        /// <summary>True when the frames are shown as drawn (facing right).</summary>
        public bool FacingRight => sr.transform.localScale.x >= 0f;

        public void Face(bool right)
        {
            var s = sr.transform.localScale;
            s.x = Mathf.Abs(s.x) * (right ? 1f : -1f);
            sr.transform.localScale = s;
        }

        public void SetAlpha(float a)
        {
            var c = sr.color; c.a = a; sr.color = c;
            var b = ballSr.color; b.a = a; ballSr.color = b;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 moved = transform.position - lastPos;
            lastPos = transform.position;
            if (Current == Pose.Ball)
            {
                ballSr.transform.Rotate(0, 0, -moved.x / (BallWidth * 0.5f) * Mathf.Rad2Deg);
                return;
            }

            // A fast swing switches the hang to the excited, leg-kicking set (same rope anchor, same frame clock).
            if (Current == Pose.Hang && hangFast != hang)
            {
                float speed = Mathf.Abs(SwingSpeed);
                var want = current == hangFast ? (speed < FastSwingOff ? hang : hangFast) : (speed > FastSwing ? hangFast : hang);
                if (current != want)
                {
                    current = want;
                    Show(Mathf.Min(frame, current.Frames.Length - 1));
                    Place();   // the two sets differ in height: keep the hands on the rope end
                }
            }
            int count = current.Frames.Length;
            if (SwingLean.HasValue && current == swingDrawn && current != null && count > SwingCentre)
            {
                float lean = Mathf.Clamp(SwingLean.Value, -1f, 1f);
                frame = Mathf.Clamp(Mathf.RoundToInt((1f - Mathf.Abs(lean)) * SwingCentre), 0, SwingCentre);
                Face(lean <= 0f);   // the left-half frames, mirrored when the pup is on the right
            }
            else if (DrivenPhase.HasValue && (current == swingDrawn || current == bungee) && current != null)
                frame = Mathf.Clamp((int)(Mathf.Repeat(DrivenPhase.Value, 1f) * count), 0, count - 1);
            else if (current == walk || current == walkBare)
            {
                // Steps follow the ground covered, so a pup that stops also stops stepping.
                walkDistance += Mathf.Abs(moved.x) + Mathf.Abs(moved.y);
                frame = (int)(walkDistance / WalkStep) % count;
            }
            else if ((current == idle || current == idleBare) && count == 1)
            {
                frameTime += dt;
                float k = BreathDepth * Mathf.Sin(frameTime * BreathSpeed + breathPhase);
                var sc = sr.transform.localScale;
                sr.transform.localScale = new Vector3(Mathf.Sign(sc.x) * Scale * (1f - k), Scale * (1f + k), 1f);
                Place();   // the feet stay on the ground while the body breathes
                frame = 0;
            }
            else if (current == idle || current == leverReady)
            {
                // Breathe 0-1-2-1; frame 3 is the blink.
                frameTime += dt * current.Fps;
                frame = Breath[(int)frameTime % Breath.Length];
                blinkTimer -= dt;
                if (blinkTimer < 0f) frame = 3;
                if (blinkTimer < -0.14f) blinkTimer = Random.Range(1.8f, 4.2f);
            }
            else
            {
                float fps = current == hang || current == hangFast ? current.Fps * (1f + Mathf.Abs(SwingSpeed) * 0.5f) : current.Fps;
                frameTime += dt * fps;
                int i = (int)frameTime;
                if (current.PingPong && count > 2)
                {
                    int cycle = count * 2 - 2;
                    int k = i % cycle;
                    frame = k < count ? k : cycle - k;
                }
                else
                {
                    int loop = count - current.LoopFrom;
                    frame = i < count ? i : current.LoopFrom + (i - current.LoopFrom) % loop;
                }
            }
            Show(frame);
        }
    }
}

using UnityEngine;

namespace PufferLaunch.Core
{
    /// <summary>
    /// The hero: a harp seal pup drawn frame by frame (Resources/Art/pup_&lt;anim&gt;_&lt;i&gt;, cut from the ChatGPT
    /// sheets by Tools/slice_sheets.py). One sprite renderer flips frames per pose; nothing is assembled from parts.
    /// Root origin = body centre. Each animation's canvas has an anchor (see ANCHOR in slice_sheets.py):
    /// bottom = feet, rope = top of the drawn rope (the rope end in game), center = body centre.
    /// </summary>
    public class PupRig : MonoBehaviour
    {
        public enum Pose { Idle, Walk, Crouch, Leap, Hang, Flail, Launched, Fall, Cheer, Glide, Ball, Dizzy, LeverReady, LeverPull, Fly, Dive, GetUp, NinjaHang, Grab, Stuck, Flinch, Spin }

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

        sealed class Anim
        {
            public Sprite[] Frames;
            public float Fps;
            public int LoopFrom;            // frames before this play once (e.g. the dizzy "oh!" frame)
            public Anchor Anchor;
        }

        static Anim idle, walk, crouch, leap, hang, flail, launched, fall, cheer, glide, dizzy, leverReady, leverPull, fly, dive, getUp, ninjaHang, grab, stuck, flinch, spin;
        static Anim walkBare, cheerBare, dizzyBare, getUpBare, idleBare, flailBare;   // bare-headed sets (hat knocked off), null until drawn

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
            idle = Load("idle", 4, 3f, 0, Anchor.Bottom);
            walk = Load("walk", 6, 0f, 0, Anchor.Bottom);
            crouch = Load("leap", 1, 0f, 0, Anchor.Bottom);          // leap_0: knees bent before the jump
            leap = Load("leap", 3, 10f, 2, Anchor.Center, 1);       // leap_1..3 once, then hold the grab pose
            hang = Load("hang", 4, 5f, 0, Anchor.Rope);
            flail = Load("flail", 4, 10f, 0, Anchor.Center);
            launched = Load("launch", 4, 10f, 0, Anchor.Center);
            fall = Load("fall", 4, 8f, 0, Anchor.Center);
            leverReady = Load("lever", 4, 3f, 0, Anchor.Bottom);
            leverPull = Load("lever", 4, 14f, 3, Anchor.Bottom, 4);  // lever_4..7 once, then hold the release
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
            grab = Load("grab", 3, 7f, 2, Anchor.Bottom) ?? crouch;
            stuck = Load("stuck", 4, 8f, 0, Anchor.Center) ?? dizzy;
            flinch = Load("flinch", 4, 7f, 3, Anchor.Bottom) ?? dizzy;
            spin = Load("spin", 4, 12f, 0, Anchor.Center) ?? flail;
            idleBare = Load("idle_bare", 4, 3f, 0, Anchor.Bottom);
            flailBare = Load("flail_bare", 4, 10f, 0, Anchor.Center);
            walkBare = Load("walk_bare", 6, 0f, 0, Anchor.Bottom);
            cheerBare = Load("cheer_bare", 4, 8f, 0, Anchor.Bottom);
            dizzyBare = Load("dizzy_bare", 4, 6f, 1, Anchor.Bottom);
            getUpBare = Load("getup_bare", 4, 7f, 3, Anchor.Bottom);
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

            blinkTimer = Random.Range(1.5f, 4f);
            frameTime = Random.value;
            lastPos = transform.position;
            Play(idle);
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
                }
            switch (pose)
            {
                case Pose.Walk: return walk;
                case Pose.Crouch: return crouch;
                case Pose.Leap: return leap;
                case Pose.Hang: return hang;
                case Pose.Flail: return flail;
                case Pose.Launched: return launched;
                case Pose.Fall: return fall;
                case Pose.LeverReady: return leverReady;
                case Pose.LeverPull: return leverPull;
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
                default: return idle;
            }
        }

        void Play(Anim anim)
        {
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

            int count = current.Frames.Length;
            if (current == walk)
            {
                // Steps follow the ground covered, so a pup that stops also stops stepping.
                walkDistance += Mathf.Abs(moved.x) + Mathf.Abs(moved.y);
                frame = (int)(walkDistance / WalkStep) % count;
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
                float fps = current == hang ? current.Fps * (1f + Mathf.Abs(SwingSpeed) * 0.5f) : current.Fps;
                frameTime += dt * fps;
                int i = (int)frameTime;
                int loop = count - current.LoopFrom;
                frame = i < count ? i : current.LoopFrom + (i - current.LoopFrom) % loop;
            }
            Show(frame);
        }
    }
}

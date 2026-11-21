using SealCatapult.Core;
using UnityEngine;

namespace SealCatapult.Attack
{
    /// <summary>
    /// Frame-by-frame polar bear (Resources/Art/bear_&lt;anim&gt;_&lt;i&gt;: the ChatGPT sheet cut by Tools/slice_sheets.py,
    /// or Tools/make_art.py placeholders). Every frame's canvas has the feet on its bottom edge, so the root is the
    /// feet point and each frame is lifted by half its own height. Idle breathes and blinks; Taunt and Dizzy play
    /// once and fall back to Idle; KnockedOut loops. The ice helmet is a separate child sprite over the head.
    /// </summary>
    public class BearRig : MonoBehaviour
    {
        public enum Pose { Idle, Taunt, Dizzy, KnockedOut, Eat, Sleep, Flex, Laugh, Scratch, Juggle, Punch, Sit, Smug, Bowl, Dangle, Throw, Hit, FlexFail, Doze, Backfire }

        static readonly int[] Breath = { 0, 1, 2, 1 };
        const float HatHeightShare = 0.92f;   // helmet centre at this share of the idle frame's height above the feet

        sealed class Anim
        {
            public Sprite[] Frames;
            public float Fps;
            public bool Loop;
            public float Hold;   // extra time the last frame stays before falling back to Idle
        }

        static Anim idle, taunt, dizzy, ko, eat, sleep, flex, laugh, scratch, juggle, punch, sit, smug, bowl, dangle, throwBall, hitBall, flexFail, doze, backfire;

        SpriteRenderer sr, hat;
        Anim current;
        float frameTime, blinkTimer, scale;
        int frame;

        public Pose Current { get; private set; } = Pose.Idle;
        public SpriteRenderer Body => sr;
        /// <summary>Standing height in world units (idle frame).</summary>
        public float Height => idle.Frames[0].bounds.size.y * scale;

        /// <summary>Makes the bear under parent (its position = the feet); the idle frame is width world units wide.</summary>
        public static BearRig Create(Transform parent, float width, bool helmet, int order)
        {
            LoadAnims();
            var rig = parent.gameObject.AddComponent<BearRig>();
            rig.sr = new GameObject("Body").AddComponent<SpriteRenderer>();
            rig.sr.transform.SetParent(parent, false);
            rig.sr.sprite = idle.Frames[0];
            rig.sr.sortingOrder = order;
            Art.FitWidth(rig.sr, width);
            rig.scale = rig.sr.transform.localScale.x;
            rig.hat = Art.Spawn("bear_hat", parent, new Vector2(0f, rig.Height * HatHeightShare), width * 0.5f, order + 1);
            rig.hat.enabled = helmet;
            rig.blinkTimer = Random.Range(1.5f, 4f);
            rig.Play(idle);
            return rig;
        }

        static void LoadAnims()
        {
            if (idle != null) return;
            idle = Load("idle", 4, 3f, true);
            // Missing frames fall back to idle (make_art.py always writes idle placeholders) instead of a null anim.
            taunt = Load("taunt", 4, 7f, false, 0.2f) ?? idle;
            dizzy = Load("dizzy", 4, 6f, false, 0.6f) ?? idle;
            ko = Load("ko", 4, 5f, true) ?? idle;
            eat = Load("eat", 4, 4f, true) ?? idle;
            sleep = Load("sleep", 4, 2f, true) ?? idle;
            // Sheet C (docs/bear-art-brief.md): four standing antics. Missing sets fall back to older ones.
            flex = Load("flex", 4, 5f, false, 0.4f) ?? taunt;
            laugh = Load("laugh", 4, 7f, false, 0.3f) ?? taunt;
            scratch = Load("scratch", 4, 5f, false, 0.4f) ?? idle;
            juggle = Load("juggle", 4, 6f, false, 0.2f) ?? taunt;
            // Sheet D (v10): looping signature antics, one per guard bear (GDD §R4).
            punch = Load("punch", 4, 6f, true) ?? taunt;
            sit = Load("sit", 4, 3f, true) ?? idle;
            smug = Load("smug", 4, 3f, true) ?? idle;
            bowl = Load("bowl", 4, 4f, true) ?? eat;
            dangle = Load("dangle", 4, 3f, true) ?? sit;   // sheet E: legs hanging over a ledge
            // Sheet F (18/11): the home screen's snowball fight, played once each.
            throwBall = Load("throw", 8, 10f, false, 0.3f) ?? punch;
            hitBall = Load("hit", 8, 10f, false, 0.4f) ?? dizzy;
            // 18/11 ("thêm nhiều hoạt ảnh cho gấu"): three 16-frame gags for the guards, each drawn for the job
            // (bear_sheet_16_<name>.png): the flex that fails, dozing off on guard, the snowball that lands on its own head.
            flexFail = Load("flexfail", 16, 8f, false, 0.3f) ?? flex;
            doze = Load("doze", 16, 7f, false, 0.3f) ?? scratch;
            backfire = Load("backfire", 16, 8f, false, 0.3f) ?? juggle;
        }

        static Anim Load(string name, int count, float fps, bool loop, float hold = 0f)
        {
            if (!Art.Has($"bear_{name}_0")) return null;
            // A set redrawn with more frames (16-frame redraws, 18/11) keeps the same timing: all its frames, faster.
            int drawn = count;
            while (drawn < 32 && Art.Has($"bear_{name}_{drawn}")) drawn++;
            if (drawn >= 16)   // only a full redraw, never a longer set read in part
            {
                fps *= (float)drawn / count;
                count = drawn;
            }
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++) frames[i] = Art.Get($"bear_{name}_{i}");
            return new Anim { Frames = frames, Fps = fps, Loop = loop, Hold = hold };
        }

        public bool HasHelmet => hat.enabled;

        /// <summary>Faces left or right (the sheets are drawn facing the viewer, slightly right).</summary>
        public void Face(bool right)
        {
            sr.flipX = !right;
            hat.flipX = !right;
        }

        public void SetHelmet(bool on) => hat.enabled = on;

        public void SetPose(Pose pose)
        {
            if (Current == pose) return;
            var anim = pose switch
            {
                Pose.Taunt => taunt,
                Pose.Dizzy => dizzy,
                Pose.KnockedOut => ko,
                Pose.Eat => eat,
                Pose.Sleep => sleep,
                Pose.Flex => flex,
                Pose.Laugh => laugh,
                Pose.Scratch => scratch,
                Pose.Juggle => juggle,
                Pose.Punch => punch,
                Pose.Sit => sit,
                Pose.Smug => smug,
                Pose.Bowl => bowl,
                Pose.Dangle => dangle,
                Pose.Throw => throwBall,
                Pose.Hit => hitBall,
                Pose.FlexFail => flexFail,
                Pose.Doze => doze,
                Pose.Backfire => backfire,
                _ => idle,
            };
            // A one-shot antic whose frames are missing plays idle, which never ends: report Idle so callers waiting
            // for the antic to finish (Bear.Flinch, the antic timer) are not stuck.
            bool oneShot = pose is Pose.Taunt or Pose.Dizzy or Pose.Flex or Pose.Laugh or Pose.Scratch or Pose.Juggle or Pose.Throw or Pose.Hit or Pose.FlexFail or Pose.Doze or Pose.Backfire;
            Current = anim == idle && oneShot ? Pose.Idle : pose;
            Play(anim);
        }

        void Play(Anim anim)
        {
            current = anim;
            frame = 0;
            frameTime = 0f;
            Show(anim.Frames[0]);
        }

        /// <summary>Feet stay on the root: each frame is lifted by half its own height.</summary>
        void Show(Sprite sprite)
        {
            sr.sprite = sprite;
            sr.transform.localPosition = new Vector3(0f, sprite.bounds.extents.y * scale, 0f);
        }

        public void SetAlpha(float a)
        {
            var c = sr.color; c.a = a; sr.color = c;
            var h = hat.color; h.a = a; hat.color = h;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            int count = current.Frames.Length;
            if (current == idle && count == 4)
            {
                // The 4-frame set: breathe 0-1-2-1, frame 3 is the blink. A redrawn idle loops like any other set.
                frameTime += dt * current.Fps;
                frame = Breath[(int)frameTime % Breath.Length];
                blinkTimer -= dt;
                if (blinkTimer < 0f) frame = 3;
                if (blinkTimer < -0.14f) blinkTimer = Random.Range(1.8f, 4.2f);
            }
            else
            {
                frameTime += dt * current.Fps;
                int i = (int)frameTime;
                if (current.Loop) frame = i % count;
                else if (i < count) frame = i;
                else if (frameTime < count + current.Hold * current.Fps) frame = count - 1;
                else
                {
                    SetPose(Pose.Idle);
                    return;
                }
            }
            var sprite = current.Frames[frame];
            if (sr.sprite != sprite) Show(sprite);
        }
    }
}

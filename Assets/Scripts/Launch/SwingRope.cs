using System.Collections;
using SealCatapult.Core;
using UnityEngine;

namespace SealCatapult.Launch
{
    /// <summary>
    /// A rope hanging from the beam, simulated as a damped pendulum. Idle it sways on its own; a pup can grab
    /// its end at the ledge, swing down under gravity while the rope pays out, and let go above the see-saw.
    /// Angle is measured from straight down, positive toward +x.
    /// Bungee mode (LaunchStyle.Bungee): with a pup aboard the rope hangs straight down and stretches and shrinks
    /// like an elastic cord, so the pup bobs up and down over the catapult instead of swinging side to side.
    /// </summary>
    public class SwingRope : MonoBehaviour
    {
        const float Gravity = 9.81f;
        public const float IdleLength = 1.9f;   // fits the painted pit: the swing bottom sits right over the catapult cup
        const float IdleDamping = 0.8f;
        const float IdleDrive = 0.35f;     // gentle push at the natural frequency keeps a ~10° sway
        const float CarryDamping = 0.15f;
        // Sustained swing amplitude with a pup aboard, radians (46°–57°; the Flash game swings nearly horizontal, report §11).
        const float CarrySwingMin = 0.80f;
        const float CarrySwingMax = 1.00f;
        const float RopeWidth = 0.16f;
        const float HangOffset = PupRig.HangReach;  // from the hands (rope end) down to the pup's body centre

        Vector2 anchor;
        float angle;
        float omega;
        float length = IdleLength;
        bool carrying;
        Transform rider;
        float spriteHeight;
        float spriteWidth;
        float carrySwing = CarrySwingMin;
        bool bungee;
        float bobPhase, bobAmplitude, bobSpeed;
        float lengthSpeed;   // d(length)/dt while bobbing, for the release velocity

        // The bob's lowest point is the swing bottom (IdleLength), where the catapult is aimed; it rises 2·amplitude.
        const float BobAmplitudeMin = 0.4f, BobAmplitudeMax = 0.75f;   // world units
        const float BobSpeedMin = 2.6f, BobSpeedMax = 3.8f;            // radians per second
        const float SweetWindow = 4f;                  // degrees either side of the swing bottom (8 let 6 of 16 timings out, user wants the gap hard)
        const float SweetStretch = 0.12f;              // bungee: within this of full stretch

        /// <summary>Bobbing on a bungee instead of swinging (set once, before the first pup).</summary>
        public bool Bungee { get => bungee; set => bungee = value; }

        /// <summary>The pup is at the golden spot right now: the bottom of the swing, or the bungee fully stretched.</summary>
        public bool AtSweetSpot => bungee
            ? length >= IdleLength - SweetStretch
            : Mathf.Abs(angle) * Mathf.Rad2Deg <= SweetWindow;

        public static SwingRope Create(Vector2 anchor, Transform parent)
        {
            var sr = Art.Spawn("rope", parent, anchor, 1f, 6);
            sr.name = "Rope";
            var r = sr.gameObject.AddComponent<SwingRope>();
            r.anchor = anchor;
            r.spriteHeight = sr.sprite.bounds.size.y;
            r.spriteWidth = sr.sprite.bounds.size.x;
            r.angle = 0.15f;
            r.Draw();
            return r;
        }

        /// <summary>Swing angular speed, rad/s (positive = toward +x).</summary>
        public float Omega => omega;

        /// <summary>Velocity of the hanging point along the swing (or up/down along the bungee).</summary>
        public Vector2 Velocity => bungee && rider != null
            ? new Vector2(0f, -lengthSpeed)
            : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (omega * (length + HangOffset));

        /// <summary>Where a pup's centre sits when hanging from the rope right now.</summary>
        public Vector2 EndPoint => End + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * HangOffset;

        Vector2 End => anchor + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * length;

        void Update()
        {
            if (rider != null && bungee)
            {
                // Elastic bob: straight down, the length follows a sine (smooth turn-arounds at both ends).
                angle = Mathf.MoveTowards(angle, 0f, 2f * Time.deltaTime);
                omega = 0f;
                bobPhase += bobSpeed * Time.deltaTime;
                float target = IdleLength - bobAmplitude + bobAmplitude * Mathf.Sin(bobPhase);
                lengthSpeed = (target - length) / Mathf.Max(Time.deltaTime, 1e-4f);
                length = target;
                Hang(rider);
            }
            else if (rider != null)
            {
                // Pumping like a kid on a swing: push along the motion until the swing energy matches CarrySwing.
                float g = Gravity / length;
                float energy = 0.5f * omega * omega + g * (1f - Mathf.Cos(angle));
                float target = g * (1f - Mathf.Cos(carrySwing));
                float pump = Mathf.Clamp((target - energy) * 1.5f, -3f, 3f) * (omega >= 0f ? 1f : -1f);
                Step(Time.deltaTime, 0.05f, pump);
                length = Mathf.MoveTowards(length, IdleLength, 1.5f * Time.deltaTime);
                Hang(rider);
            }
            else if (!carrying)
            {
                float w0 = Mathf.Sqrt(Gravity / length);
                Step(Time.deltaTime, IdleDamping, IdleDrive * Mathf.Sin(Time.time * w0));
                length = Mathf.MoveTowards(length, IdleLength, 1.5f * Time.deltaTime);
            }
            Draw();
        }

        /// <summary>Semi-implicit Euler, sub-stepped for stability at large angles.</summary>
        void Step(float dt, float damping, float drive)
        {
            const int Steps = 4;
            float h = dt / Steps;
            for (int i = 0; i < Steps; i++)
            {
                float accel = -(Gravity / length) * Mathf.Sin(angle) - damping * omega + drive;
                omega += accel * h;
                angle += omega * h;
            }
        }

        void Draw()
        {
            Vector2 down = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
            transform.position = anchor + down * (length * 0.5f);
            transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            transform.localScale = new Vector3(RopeWidth / spriteWidth, length / spriteHeight, 1f);
        }

        /// <summary>The rope swings over to the ledge while the pup leaps up and catches the end.</summary>
        public IEnumerator Grab(Transform pup, Vector2 grabPoint)
        {
            carrying = true;
            Vector2 d = grabPoint - anchor;
            float targetAngle = Mathf.Atan2(d.x, -d.y);
            float targetLength = d.magnitude - HangOffset;
            float a0 = angle, l0 = length;
            Vector2 p0 = pup.position;
            const float Duration = 0.3f;
            for (float t = 0f; t < Duration; t += Time.deltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / Duration, 3);
                angle = Mathf.Lerp(a0, targetAngle, k);
                length = Mathf.Lerp(l0, targetLength, k);
                Draw();
                pup.position = Vector2.Lerp(p0, grabPoint, k) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * 0.35f);
                yield return null;
            }
            angle = targetAngle;
            length = targetLength;
            omega = 0f;
            Hang(pup);
        }

        /// <summary>Gravity swing down while paying out rope; returns when the pup is above the seat.</summary>
        public IEnumerator SwingDown(Transform pup, Vector2 seat)
        {
            float startAngle = angle;
            float startLength = length;
            float seatLength = Vector2.Distance(anchor, seat) - HangOffset - 0.2f;
            Vector2 toSeat = seat - anchor;
            float releaseAngle = Mathf.Atan2(toSeat.x, -toSeat.y);
            const float Timeout = 3f;
            for (float t = 0f; t < Timeout; t += Time.deltaTime)
            {
                Step(Time.deltaTime, CarryDamping, 0f);
                float progress = Mathf.InverseLerp(startAngle, releaseAngle, angle);
                length = Mathf.Lerp(startLength, seatLength, Mathf.SmoothStep(0f, 1f, progress));
                Draw();
                Hang(pup);
                if (omega > 0f && angle >= releaseAngle) break;
                yield return null;
            }
            carrying = false;   // let go: the rope keeps its momentum and settles back into an idle sway
        }

        /// <summary>Keep swinging with this pup hanging on (after Grab). `wildness` 0..1 grows with each pup in the queue:
        /// the first pup swings gently, the last one wide (plus a little random spread so no two shots match).</summary>
        public void Carry(Transform pup, float wildness)
        {
            carrying = true;
            rider = pup;
            float k = Mathf.Clamp01(wildness * 0.85f + Random.Range(0f, 0.15f));
            carrySwing = Mathf.Lerp(CarrySwingMin, CarrySwingMax, k);
            bobAmplitude = Mathf.Lerp(BobAmplitudeMin, BobAmplitudeMax, k);
            bobSpeed = Mathf.Lerp(BobSpeedMin, BobSpeedMax, k);
            // Start the bob from the length the pup grabbed at, so there is no jump.
            bobPhase = Mathf.Asin(Mathf.Clamp((length - (IdleLength - bobAmplitude)) / bobAmplitude, -1f, 1f));
            lengthSpeed = 0f;
        }

        /// <summary>Rope angle from straight down, radians (0 = the bottom of the swing).</summary>
        public float Angle => angle;

        /// <summary>Lets go of the rider and returns its velocity along the swing.</summary>
        public Vector2 Release()
        {
            Vector2 v = Velocity;
            rider = null;
            carrying = false;
            return v;
        }

        void Hang(Transform pup)
        {
            Vector2 down = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
            pup.position = End + down * HangOffset;
            // Tilted with the rope, trailing the swing a little.
            pup.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);  // hands on the rope: the body hangs straight down it
        }
    }
}

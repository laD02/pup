using UnityEngine;

namespace PufferLaunch.Progress
{
    /// <summary>Every "feel" number lives here so play-testing changes data, not code.</summary>
    [CreateAssetMenu(menuName = "Puffer Launch/Tuning", fileName = "Tuning")]
    public class GameTuning : ScriptableObject
    {
        [Header("Physics")]
        public float gravityScale = 0.55f;
        public float airDrag = 0.1f;
        public float blockDrag = 0.6f;

        [Header("Launch · rope swing + whacker")]
        [Tooltip("Speed the whacker gives a pup, world units per second")]
        public float strikePower = 13f;
        [Tooltip("Direction of the whack, degrees above horizontal")]
        public float strikeAngle = 41f;
        [Tooltip("How much of the pup's own swing speed carries into the flight")]
        public float swingInfluence = 0.6f;
        [Tooltip("0 = pure contact physics, 1 = always straight at strikeAngle")]
        [Range(0f, 1f)] public float aimAssist = 0.4f;
        public float launcherReloadTime = 0.45f;

        [Header("Phase 2 · flight & dive")]
        [Tooltip("Gravity while gliding across the arena, before the dive (0 = dead level flight)")]
        public float glideGravity = 0f;
        [Tooltip("Laps across the arena before the tap hint appears (the pup itself flies until tapped)")]
        public int maxLoops = 2;
        [Tooltip("Dive speed as a multiple of the glide speed (the reference drops about twice as fast as it flies)")]
        public float diveSpeedScale = 1.3f;   // user 13/11: a slower dive (was 2)
        [Tooltip("Dive direction, degrees below horizontal (the reference noses down steeply, keeping a little forward drift)")]
        [Range(10f, 90f)] public float diveAngle = 45f;   // user 13/11: more diagonal (was 65)
        [Tooltip("Gravity during the dive: bends the drop steeper as it goes")]
        public float diveGravity = 0.4f;
        [Tooltip("Ice cubes a diving pup smashes through before it is slowed to real physics (Ice Spikes adds to this)")]
        public int basePierce = 1;
        [Tooltip("Speed kept after smashing through a cube")]
        [Range(0f, 1f)] public float pierceKeep = 0.85f;
        [Tooltip("Upward kick after bouncing off a bear, world units per second")]
        public float bearBounce = 2.6f;   // a low hop off a bear (reference §18: the rebound stays about one bear high)
        [Tooltip("Ninja: hang time in the speech bubble before the straight drop")]
        public float ninjaHang = 0.25f;
        public float pufferMass = 2.2f;
        [Range(0f, 1f)] public float bounceKeep = 0.5f;
        [Tooltip("A puffer is done after resting this long, or this long after its dive")]
        public float restTime = 0.8f;
        public float maxLifetime = 7f;

        [Header("Blocks: HP in impulse units, density in mass per m²")]
        public float snowHp = 2f;
        public float iceHp = 7f;
        public float stoneHp = 22f;
        public float snowDensity = 0.6f;
        public float iceDensity = 1f;
        public float stoneDensity = 2.6f;
        [Tooltip("Impulses below this are ignored (resting contact)")]
        public float minDamageImpulse = 1.2f;

        [Header("Arena objects")]
        public float bumperKick = 11f;
        public int fishScore = 100;

        [Header("Bears")]
        public float bearHitImpulse = 3f;
        public float bearFallDistance = 1.1f;

        [Header("Score")]
        public int snowScore = 50;
        public int iceScore = 120;
        public int stoneScore = 200;
        public int bearScore = 500;

        [Header("Juice")]
        public float shakeOnBreak = 0.15f;
        public float shakeOnBear = 0.35f;
    }
}

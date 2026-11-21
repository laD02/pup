using SealCatapult.Attack;
using SealCatapult.Core;
using UnityEngine;

namespace SealCatapult.UI
{
    /// <summary>
    /// The home screen's snowball battle (user 18/11: "vẽ nền kiểu như 2 bên đang chiến đấu với nhau … vẽ các hoạt ảnh
    /// cho phù hợp với nền"): pups hop and wave by their camp on the left, bears taunt from their fortress on the right,
    /// and snowballs fly across the bay both ways. A bear winds up and throws; the pup it hits takes it in the face and
    /// shakes it off; then a pup winds up and throws one back. Every pose is drawn for this (pup_sheet_throw.png,
    /// bear_sheet_f.png: throw and hit; user 18/11: never borrowed frames). One pooled ball, nothing allocated per frame.
    /// </summary>
    public class MenuBattle : MonoBehaviour
    {
        const float Every = 2.6f, Flight = 1.15f, Arc = 3.2f, React = 1.2f;
        const float PupRelease = 5f / 11f, BearRelease = 5f / 10f;   // the throw sheets let go of the ball on frame 6

        PupRig[] pups;
        BearRig[] bears;
        Vector2[] pupSpots, bearSpots;
        SpriteRenderer ball;
        Vector2 from, to;
        float t = -1f, wait = 1.2f, reactLeft, release = -1f;
        bool bearsThrow = true;
        int thrower, target;

        public static MenuBattle Create(float ground)
        {
            var go = new GameObject("Menu Battle");
            var b = go.AddComponent<MenuBattle>();
            b.Build(go.transform, ground);
            return b;
        }

        void Build(Transform root, float ground)
        {
            // Pups by their camp (the whacker and its operator stand at x -5.6), bears by their fortress (the boss on
            // its ice block at x 5.2); everyone clear of the Play button in the lower centre.
            pupSpots = new[] { new Vector2(-3.6f, ground), new Vector2(-7.7f, ground) };
            pups = new PupRig[pupSpots.Length];
            for (int i = 0; i < pups.Length; i++)
            {
                pups[i] = PupRig.Create(root, pupSpots[i] + new Vector2(0f, PupRig.StandHeight), 18);
                pups[i].Face(true);
            }
            bearSpots = new[] { new Vector2(8.6f, ground), new Vector2(6.9f, ground) };   // right of the Play button
            bears = new BearRig[bearSpots.Length];
            for (int i = 0; i < bears.Length; i++)
            {
                var go = new GameObject("Menu Bear");
                go.transform.SetParent(root, false);
                go.transform.position = bearSpots[i];
                bears[i] = BearRig.Create(go.transform, 1.5f, false, 6);   // no ice helmet on the home screen (user 18/11)
                bears[i].Face(false);
            }
            Calm();
            ball = Art.Spawn("snowball", root, Vector2.zero, 0.42f, 30);
            ball.enabled = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (reactLeft > 0f && (reactLeft -= dt) <= 0f) Calm();
            if (release >= 0f)
            {
                if ((release -= dt) < 0f) Release();
                return;
            }
            if (t < 0f)
            {
                if ((wait -= dt) <= 0f) WindUp();
                return;
            }
            t += dt / Flight;
            float k = Mathf.Min(t, 1f);
            Vector2 p = Vector2.Lerp(from, to, k) + Vector2.up * Arc * 4f * k * (1f - k);
            ball.transform.position = p;
            ball.transform.Rotate(0f, 0f, (bearsThrow ? 540f : -540f) * dt);
            if (k > 0.04f && k < 0.96f && Random.value < 0.6f) Fx.Trail(p);
            if (k >= 1f) Land();
        }

        /// <summary>The thrower scoops, packs and winds up (drawn throw frames); the ball leaves its hand in Release.</summary>
        void WindUp()
        {
            if (bearsThrow)
            {
                thrower = Random.Range(0, bears.Length);
                target = Random.Range(0, pups.Length);
                bears[thrower].SetPose(BearRig.Pose.Throw);
                from = bearSpots[thrower] + new Vector2(-0.7f, 1.7f);
                to = pupSpots[target] + new Vector2(0f, 0.9f);   // the face: the hit frames show the splat there
                release = BearRelease;
            }
            else
            {
                thrower = Random.Range(0, pups.Length);
                target = Random.Range(0, bears.Length);
                pups[thrower].SetPose(PupRig.Pose.ThrowBall);
                from = pupSpots[thrower] + new Vector2(0.4f, 1.1f);
                to = bearSpots[target] + new Vector2(0f, 1.6f);
                release = PupRelease;
            }
        }

        void Release()
        {
            release = -1f;
            ball.transform.position = from;
            ball.enabled = true;
            AudioManager.Sfx("launch", 0.1f, 0.5f);
            t = 0f;
        }

        void Land()
        {
            ball.enabled = false;
            Fx.Puff(to, 5, 0.35f);
            AudioManager.Sfx("puff", 0.1f, 0.6f);
            if (bearsThrow) pups[target].SetPose(PupRig.Pose.HitBall);   // splat in the face, shakes it off
            else bears[target].SetPose(BearRig.Pose.Hit);
            reactLeft = React;
            bearsThrow = !bearsThrow;
            t = -1f;
            wait = Every - Flight;
        }

        /// <summary>Back to the waiting antics: the near pup hops, the far one waves; one bear taunts, one flexes.</summary>
        void Calm()
        {
            reactLeft = 0f;
            for (int i = 0; i < pups.Length; i++) pups[i].SetPose(i == 0 ? PupRig.Pose.Hop : PupRig.Pose.Wave);
            for (int i = 0; i < bears.Length; i++) bears[i].SetPose(i == 0 ? BearRig.Pose.Taunt : BearRig.Pose.Flex);
        }
    }
}

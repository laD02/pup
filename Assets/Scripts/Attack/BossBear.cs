using System;
using System.Collections;
using PufferLaunch.Core;
using PufferLaunch.Progress;
using UnityEngine;

namespace PufferLaunch.Attack
{
    /// <summary>
    /// The boss bear, as in the reference (report §10.6): the size of any other bear, steaming with anger on a perch,
    /// no life bar. Every hit by a diving pup costs it a life (stars, dizzy); at zero it is out cold and the cage opens.
    /// A floor button of its group throws it off the perch to the floor once, where it is easier to hit.
    /// </summary>
    public class BossBear : MonoBehaviour
    {
        const float Width = 1.2f;   // a guard bear's size
        const float HitCooldown = 0.5f;

        public static event Action<BossBear> Beaten;

        int lives;
        int group;
        Vector2 perch;
        bool airborne;
        bool toppled;
        float lastHit = -9f;
        bool beaten;
        BearRig rig;
        Rigidbody2D rb;
        Collider2D col;

        public int Lives => lives;
        public bool IsBeaten => beaten;
        /// <summary>The boss of the loaded level, if any (pups aim ninja kicks at it).</summary>
        public static BossBear Current { get; private set; }
        float steamTimer;
        bool steamFlip;

        /// <summary>Angry steam curls off the boss's head while it stands on its perch (report §10.6).</summary>
        void Update()
        {
            if (beaten) return;
            // Knocked out of the arena: never leave the boss level unwinnable.
            if (transform.position.y < Backdrop.GroundY - 3f) { KnockOut(); return; }
            if (airborne) return;
            steamTimer -= Time.deltaTime;
            if (steamTimer > 0f) return;
            steamTimer = 0.18f;
            // Two jets from either ear, white-grey so they read against the orange sky.
            float side = (steamFlip = !steamFlip) ? -0.19f : 0.19f;
            var d = Fx.Particle("snow_puff", (Vector2)transform.position + new Vector2(side, rig.Height * 0.98f), 0.3f, 14);
            d.Renderer.color = new Color(0.93f, 0.95f, 1f, 0.95f);
            d.velocity = new Vector2(side * 1.6f, 1.8f);
            d.life = 0.8f;
            d.grow = 1.4f;
            d.wobble = true;
        }

        public static BossBear Create(Vector2 feet, int lives, int group, GameTuning tuning, Transform parent)
        {
            var go = new GameObject("Boss");
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            var rig = BearRig.Create(go.transform, Width, false, 13);
            float h = rig.Height;
            var col = go.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(Width * 0.7f, h * 0.92f);
            col.offset = new Vector2(0f, h * 0.5f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.mass = 3f;
            rb.freezeRotation = true;
            rb.linearDamping = 0.3f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var boss = go.AddComponent<BossBear>();
            boss.rig = rig;
            rig.Face(false);   // faces the pups flying in from the left
            boss.rb = rb;
            boss.col = col;
            boss.lives = lives;
            boss.group = group;
            boss.perch = feet;
            Current = boss;
            FloorButton.Pressed += boss.OnButton;
            return boss;
        }

        void OnDestroy()
        {
            FloorButton.Pressed -= OnButton;
            if (Current == this) Current = null;
        }

        /// <summary>The button is a switch (reference §10.6: it takes away what the boss stands on): the boss is thrown off
        /// its perch to the floor, once, where it is easier to hit. It costs no life.</summary>
        void OnButton(int pressedGroup)
        {
            if (beaten || airborne || toppled || pressedGroup != group) return;
            toppled = true;
            StartCoroutine(Toppled());
        }

        IEnumerator Toppled()
        {
            airborne = true;
            rig.SetPose(BearRig.Pose.Dizzy);
            AudioManager.Sfx("boing", 0f, 0.7f);
            Fx.Shake(0.4f);
            rb.linearVelocity = new Vector2(-4f, 6f);
            yield return new WaitForSeconds(0.3f);
            for (float t = 0f; t < 3f && rb.linearVelocity.magnitude > 0.3f; t += Time.deltaTime) yield return null;
            Fx.Puff(transform.position, 10);
            if (!beaten) rig.SetPose(BearRig.Pose.Idle);
            airborne = false;
        }

        /// <summary>A diving pup that hits the boss costs it a life: stars and a dizzy spell, and the last life knocks it
        /// out (reference: "Hit the Boss bear several times to knock it out!").</summary>
        void OnCollisionEnter2D(Collision2D c)
        {
            if (!c.collider.TryGetComponent(out PufferController p) || !p.Dived) return;
            Hit();
        }

        public void Hit()
        {
            if (beaten || Time.time - lastHit < HitCooldown) return;   // one dive is one hit, however many contacts it makes
            lastHit = Time.time;
            lives--;
            Fx.Burst("star_full", (Vector2)transform.position + Vector2.up * rig.Height, 4, 3f, 0.8f, 0.25f, 2f, 0.2f);
            AudioManager.Sfx("bear_down");
            rig.SetPose(BearRig.Pose.Dizzy);
            if (lives > 0) return;
            KnockOut();
        }

        void KnockOut()
        {
            beaten = true;
            rig.SetPose(BearRig.Pose.KnockedOut);   // drawn lying down; the root stays upright
            col.enabled = false;
            rb.simulated = false;
            Beaten?.Invoke(this);
        }
    }
}

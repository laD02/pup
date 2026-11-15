using System;
using PufferLaunch.Core;
using PufferLaunch.Progress;
using UnityEngine;

namespace PufferLaunch.Attack
{
    /// <summary>Fortress block: HP by material, damaged by collision impulse, breaks with juice and score.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Block : MonoBehaviour
    {
        /// <summary>Damage only counts once the attack starts, so the fortress can settle without crumbling.</summary>
        public static bool Armed;
        public static event Action<Block, int> Broken;

        public BlockMaterial Material { get; private set; }
        public int Group { get; private set; }
        float hp;
        float maxHp;
        bool broken;
        GameTuning tuning;
        SpriteRenderer sr;

        public static Block Create(BlockSpec spec, Vector2 origin, GameTuning tuning, Transform parent)
        {
            string sprite = spec.material switch
            {
                BlockMaterial.Snow => "block_snow",
                BlockMaterial.Stone => "block_stone",
                _ => "block_ice",
            };
            var go = new GameObject($"Block_{spec.material}");
            go.transform.SetParent(parent, false);
            go.transform.position = origin + spec.position;
            go.transform.rotation = Quaternion.Euler(0, 0, spec.rotation);

            var sr = go.AddComponent<SpriteRenderer>();
            bool wedge = spec.wedge != 0 && Art.Has("block_wedge");   // without the art a wedge is a plain cube
            sr.sprite = Art.Get(wedge ? "block_wedge" : sprite);
            sr.sortingOrder = 10;
            if (wedge)
            {
                // The painted wedge rises to the right; flipped for the other side. Its collider is the triangle.
                sr.flipX = spec.wedge < 0;
                var b = sr.sprite.bounds.size;
                go.transform.localScale = new Vector3(spec.size.x / b.x, spec.size.y / b.y, 1f);
                float hx = b.x * 0.5f, hy = b.y * 0.5f, top = spec.wedge > 0 ? hx : -hx;
                go.AddComponent<PolygonCollider2D>().points = new[] { new Vector2(-hx, -hy), new Vector2(hx, -hy), new Vector2(top, hy) };
            }
            else
            {
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = spec.size;
                go.AddComponent<BoxCollider2D>().size = spec.size;
            }
            var rb = go.AddComponent<Rigidbody2D>();
            float density = spec.material switch
            {
                BlockMaterial.Snow => tuning.snowDensity,
                BlockMaterial.Stone => tuning.stoneDensity,
                _ => tuning.iceDensity,
            };
            rb.mass = Mathf.Max(0.1f, spec.size.x * spec.size.y * density);
            rb.gravityScale = 1f; // blocks feel heavy; only pups float
            rb.linearDamping = tuning.blockDrag;
            rb.angularDamping = tuning.blockDrag;
            rb.sleepMode = RigidbodySleepMode2D.StartAsleep;
            // Blocks stand still (reference §17.1): a hit block breaks, its neighbours stay where they are, nothing topples.
            rb.bodyType = RigidbodyType2D.Static;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var block = go.AddComponent<Block>();
            block.Material = spec.material;
            block.Group = spec.group;
            block.tuning = tuning;
            block.sr = sr;
            block.maxHp = block.hp = spec.material switch
            {
                BlockMaterial.Snow => tuning.snowHp,
                BlockMaterial.Stone => tuning.stoneHp,
                _ => tuning.iceHp,
            };
            return block;
        }

        /// <summary>The reference's rule (report §19, from a full playthrough video): there is ONE kind of ice and any pup
        /// that touches it breaks it at once; wooden crates never break from hits, only a floor button of their group
        /// removes them (the boss level's crate bridges). Snow is as soft as ice.</summary>
        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!Armed || broken) return;
            if (Material == BlockMaterial.Stone) return;   // crates: only a button opens them (AttackPhase.OnButton)
            if (collision.collider.GetComponent<PufferController>() != null) Break();
        }

        public void TakeDamage(float amount)
        {
            if (broken) return;
            hp -= amount;
            if (hp <= 0f)
            {
                Break();
                return;
            }
            // Cracks spread and the block darkens so the player can read remaining strength.
            float damage = 1f - hp / maxHp;
            float k = Mathf.Lerp(0.7f, 1f, hp / maxHp);
            sr.color = new Color(k, k, k, 1f);
            if (sr.drawMode == SpriteDrawMode.Sliced)
            {
                ShowCracks(damage);
                StartCoroutine(Jolt());
            }
            if (Material == BlockMaterial.Ice) Fx.Burst("ice_shard", transform.position, 2, 2f, 0.5f, 0.2f, 2f);
        }

        SpriteRenderer cracks;

        void ShowCracks(float damage)
        {
            if (cracks == null)
            {
                cracks = Art.Spawn("crack", transform, Vector2.zero, 1f, sr.sortingOrder + 1);
                var b = cracks.sprite.bounds.size;
                cracks.transform.localScale = new Vector3(sr.size.x / b.x, sr.size.y / b.y, 1f);
                cracks.transform.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.value < 0.5f ? 0f : 180f);
            }
            cracks.color = new Color(1f, 1f, 1f, Mathf.Clamp01(0.35f + damage));
        }

        /// <summary>A quick shiver when hit but not broken.</summary>
        System.Collections.IEnumerator Jolt()
        {
            for (float t = 0f; t < 0.12f; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / 0.12f * Mathf.PI) * 0.06f;
                sr.size = baseSize * (1f + k);
                yield return null;
            }
            sr.size = baseSize;
        }

        Vector2 baseSize;

        void Start() => baseSize = sr.size;

        public void Break()
        {
            if (broken) return;
            broken = true;
            Vector2 at = transform.position;
            int score;
            switch (Material)
            {
                case BlockMaterial.Snow:
                    score = tuning.snowScore;
                    AudioManager.Sfx("sand");
                    Fx.Burst("powder", at, 14, 2.5f, 0.9f, 0.35f, 0.5f, sr.bounds.size.x * 0.4f);
                    break;
                case BlockMaterial.Stone:
                    score = tuning.stoneScore;
                    AudioManager.Sfx("thud");
                    Fx.Burst("ice_shard", at, 8, 3f, 0.9f, 0.3f, 4f, 0.4f, new Color(0.65f, 0.7f, 0.8f));
                    break;
                default:
                    score = tuning.iceScore;
                    AudioManager.Sfx("pop");
                    Fx.Burst("ice_shard", at, 10, 3.5f, 0.9f, 0.28f, 3f, 0.4f);
                    Fx.Rubble("ice_shard", at, 4, 0.22f, sr.bounds.size.x * 0.5f);
                    break;
            }
            Fx.Puff(at, 6);
            Fx.Shake(tuning.shakeOnBreak);
            Broken?.Invoke(this, score);
            // Destroy waits for the end of the frame: without this a second physics step in the same frame (under 50 fps)
            // still collides with the broken block and stops a pierce or glide-through.
            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
            Destroy(gameObject);
        }
    }
}

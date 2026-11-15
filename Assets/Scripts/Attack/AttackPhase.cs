using System;
using System.Collections;
using System.Collections.Generic;
using PufferLaunch.Core;
using PufferLaunch.Progress;
using UnityEngine;

namespace PufferLaunch.Attack
{
    /// <summary>
    /// The arena (phase 2, landscape, fixed view, laid out like the genre's level screens): dusk sky and pack ice behind,
    /// a cobble floor, a rock wall on the right, cobble ramps, plateaus and pillars across the whole width, ice cubes
    /// stacked on them and bears standing exposed on top. Pups fly in level from the left edge near the top. Tracks the pup in flight, bears and score, and reports
    /// win (all bears down) or loss (no pups left once the rubble settles).
    /// </summary>
    public class AttackPhase : MonoBehaviour
    {
        public const float FortressX = 40f;
        /// <summary>Arena width: everything sits between x = 0 and ArenaWidth of the origin; the rock wall starts there.</summary>
        public const float ArenaWidth = 13f;
        const float FloorHeight = 0.8f;      // rock floor strip shown under the arena
        const float RimHeight = 0.7f;        // rock_top strip (512×44 px)
        /// <summary>Depth of a cave floor below the arena floor, and thickness of the cave's roof.</summary>
        public const float CaveDepth = 2.6f;
        const float CaveRoof = 0.8f;
        const float SkyWidth = 34f;          // bg_arena_sky is 3:2, so 16 units tall
        const float SkyHeight = SkyWidth / 1.5f;
        const float HorizonAboveFloor = 2.2f;
        const float NextPupDelay = 0.6f;
        const float WinDelay = 1.6f;
        const float SettleTimeout = 2.5f;

        /// <summary>Arena floor level; the rock floor strip sits just under it.</summary>
        public static Vector2 Origin => new Vector2(FortressX, Backdrop.GroundY + FloorHeight);
        /// <summary>Pups enter at the left edge near the top and glide right across the arena.</summary>
        public static Vector2 SpawnPoint => Origin + new Vector2(-1.5f, 9.6f);
        public static Vector2 SpawnVelocity => new Vector2(4.5f, 0f);
        // Each pup enters on the flight it left phase 1 with: a steep exit comes in high and slow, a flat one low and fast.
        const float EntryLow = 8.8f, EntryHigh = 10.4f;      // entry height above the floor (SpawnPoint is 9.6); user 13/11: fly higher (twice)
        const float ExitFlat = 25f, ExitSteep = 80f;         // exit angles mapped onto that height range, degrees
        const float EntrySpeedKeep = 0.55f;                  // share of the exit's sideways speed kept as glide speed
        const float EntrySpeedMin = 3.2f, EntrySpeedMax = 6.2f;
        /// <summary>Left and right edges of what the arena camera shows (the arena's own edges without a camera).</summary>
        public static float ViewLeft => Camera.main != null && Camera.main.orthographic
            ? Mathf.Min(Origin.x - 1.5f, ViewCenter.x - Camera.main.orthographicSize * Camera.main.aspect) : Origin.x - 1.5f;
        public static float ViewRight => Camera.main != null && Camera.main.orthographic
            ? Mathf.Max(Origin.x + ArenaWidth + 1.5f, ViewCenter.x + Camera.main.orthographicSize * Camera.main.aspect) : Origin.x + ArenaWidth + 1.5f;

        /// <summary>Camera centre for the fixed arena view: the whole arena plus a strip of rock floor fits one screen.</summary>
        public static Vector2 ViewCenter => Origin + new Vector2(ArenaWidth * 0.5f - ViewShift, 5.3f);   // the wider view shows the higher flight
        const float ViewShift = 0.5f;            // the view leans left so the cliff shows and the far-right perch is half cut, as in the reference
        const float CliffReach = 14f;   // how far left the cliff runs: past the arena view, short of the pit view (30 showed in the pit on 20:9)
        const float CliffHeight = 4.2f, CliffSlope = 1.8f, CliffFoot = 0.6f;   // the left cliff: block + steep slope ending on the floor at x = CliffFoot

        /// <summary>(knocked out, required, total)</summary>
        public event Action<int, int, int> BearsChanged;
        public event Action<int> ScoreChanged;

        Action<bool, int, int> onEnd;   // won, pups left, score
        bool ended;
        bool winPending;   // the last bear or the boss is down; End(true) follows after its delay
        int[] upgrades;
        GameTuning tuning;

        public int Ammo { get; private set; }
        public event Action<int> AmmoChanged;

        public int BearsLeft { get; private set; }
        public int BearsTotal { get; private set; }
        public int BearsRequired { get; private set; }
        public int BearsDown => BearsTotal - BearsLeft;
        /// <summary>Every bear knocked out and every fish collected (GDD "Hoàn hảo"; only the bonus level has fish).</summary>
        public bool Perfect => BearsLeft == 0 && fishGot == fishTotal;
        int combo;
        readonly List<int> dives = new List<int>();   // bears knocked out by each pup, for the statistics
        /// <summary>Bears knocked out by each pup that has finished so far (the game saves these as combo stats).</summary>
        public IReadOnlyList<int> Dives => dives;
        LevelKind kind;
        int fishTotal, fishGot;
        Cage cage;
        bool bossBeaten;
        BossBear boss;
        readonly List<Block> gated = new List<Block>();
        public int FishCollected => fishGot;
        public int FishTotal => fishTotal;
        public int Score { get; private set; }
        public bool Ended => ended;
        public PufferController Current { get; private set; }

        void OnEnable()
        {
            Block.Broken += OnBlockBroken;
            Bear.Defeated += OnBearDefeated;
            Fish.Collected += OnFish;
            FloorButton.Pressed += OnButton;
            BossBear.Beaten += OnBossBeaten;
        }

        void OnDisable()
        {
            Block.Broken -= OnBlockBroken;
            Bear.Defeated -= OnBearDefeated;
            Fish.Collected -= OnFish;
            FloorButton.Pressed -= OnButton;
            BossBear.Beaten -= OnBossBeaten;
        }

        /// <summary>Zone looks: sunset over pack ice, bright day over grey rock, orange ice cave (tints over the same art).</summary>
        static readonly (Color sky, Color floes, Color rock)[] Palettes =
        {
            (Color.white, Color.white, Color.white),
            (new Color(0.86f, 0.95f, 1f), new Color(0.95f, 0.98f, 1f), new Color(0.78f, 0.82f, 0.9f)),
            (new Color(0.95f, 0.6f, 0.35f), new Color(0.9f, 0.7f, 0.5f), new Color(0.72f, 0.5f, 0.38f)),
        };
        static Color rockTint = Color.white;

        /// <summary>Builds ground and fortress at level start; damage stays off until Arm().</summary>
        public void Build(LevelData level, GameTuning tuning)
        {
            this.tuning = tuning;
            Block.Armed = false;
            var arena = new GameObject("Arena").transform;
            // Backdrop of this area: one ChatGPT painting (sunset sky, icebergs, pack ice: bg_arena_sky), wide enough for
            // 20:9 at the arena zoom, its horizon (70% down the picture) just above the floor.
            var pal = Palettes[Mathf.Clamp(level.zone, 0, Palettes.Length - 1)];
            rockTint = pal.rock;
            var sky = Art.Spawn("bg_arena_sky", arena, Origin + new Vector2(ArenaWidth * 0.5f, SkyHeight * 0.2f + HorizonAboveFloor), SkyWidth, -70);
            sky.color = pal.sky;
            // Cobble floor across the whole view and a snow-capped cobble cliff on the LEFT, sloping down onto the floor
            // (reference, 12/11: the rock is on the left, the right side is open sky with floating ice ledges). Pups glide
            // in above the cliff.
            surface.Clear();
            BuildFloor(arena, level);
            RockBlock(arena, Rect.MinMaxRect(Origin.x - CliffReach, Origin.y, Origin.x + CliffFoot - CliffSlope, Origin.y + CliffHeight));
            Terrain(arena, new TerrainSpec { shape = TerrainShape.RampUpLeft, position = new Vector2(CliffFoot - CliffSlope * 0.5f, CliffHeight * 0.5f), size = new Vector2(CliffSlope, CliffHeight) });
            foreach (var spec in level.terrain) Terrain(arena, spec);
            BuildSnow(arena);

            var fortress = new GameObject("Fortress").transform;
            kind = level.kind;
            foreach (var spec in level.blocks)
            {
                var b = Block.Create(spec, Origin, tuning, fortress);
                if (spec.group > 0) gated.Add(b);
            }
            foreach (var spec in level.bears) Bear.Create(spec, Origin, tuning, fortress);
            foreach (var spec in level.objects)
            {
                Vector2 at = Origin + spec.position;
                switch (spec.kind)
                {
                    case ObjectKind.Bumper: Bumper.Create(at, tuning.bumperKick, fortress); break;
                    case ObjectKind.Tnt: Tnt.Create(at, fortress); break;
                    case ObjectKind.Button: FloorButton.Create(at, spec.group, fortress); break;
                    case ObjectKind.Fish: Fish.Create(at, fortress); fishTotal++; break;
                    case ObjectKind.Cage: cage = Cage.Create(at, fortress); break;
                    case ObjectKind.Boss: boss = BossBear.Create(at, Mathf.Max(1, spec.value), spec.group, tuning, fortress); break;
                }
            }
            // The king counts as one more bear on a boss level, so the counter reads e.g. 2/3 until it falls.
            BearsLeft = BearsTotal = level.bears.Count + (boss != null ? 1 : 0);
            BearsRequired = level.requiredBears <= 0 ? BearsTotal : Mathf.Min(level.requiredBears, BearsTotal);
            BearsChanged?.Invoke(BearsDown, BearsRequired, BearsTotal);
        }

        /// <summary>Cobble floor across the whole view; with a cave, the floor over the chamber is a thin ceiling with a gap
        /// in it, and the chamber below has its own floor and a dark back wall (report §11: the genre's hidden caves).</summary>
        static void BuildFloor(Transform parent, LevelData level)
        {
            float left = Origin.x - 30f, right = Origin.x + ArenaWidth + 16f, bottom = Origin.y - 4f;
            if (!level.HasCave)
            {
                RockBlock(parent, Rect.MinMaxRect(left, bottom, right, Origin.y));
                return;
            }
            float a = Origin.x + level.caveFrom, b = Origin.x + level.caveTo;
            float g0 = Origin.x + level.gapFrom, g1 = Origin.x + level.gapTo;
            float roof = Origin.y - CaveRoof, floorTop = Origin.y - CaveDepth;
            RockBlock(parent, Rect.MinMaxRect(left, bottom, a, Origin.y));
            RockBlock(parent, Rect.MinMaxRect(b, bottom, right, Origin.y));
            if (g0 > a) RockBlock(parent, Rect.MinMaxRect(a, roof, g0, Origin.y));
            if (b > g1) RockBlock(parent, Rect.MinMaxRect(g1, roof, b, Origin.y));
            RockBlock(parent, Rect.MinMaxRect(a, bottom, b, floorTop));
            var back = Art.Spawn("rock_cobble", parent, new Vector2((a + b) * 0.5f, (floorTop + roof) * 0.5f), 1f, 7);
            back.name = "Cave";
            back.transform.localScale = Vector3.one;   // Spawn fits the sprite to 1 unit wide; the tiled size is in world units
            back.drawMode = SpriteDrawMode.Tiled;
            back.size = new Vector2(b - a, roof - floorTop);
            back.color = rockTint * new Color(0.42f, 0.36f, 0.4f, 1f);
            // A few icicles under the cave roof on either side of the opening.
            for (float x = a + 0.4f; x < b - 0.3f; x += 0.9f)
            {
                if (x > g0 - 0.3f && x < g1 + 0.3f) continue;
                var ice = Art.Spawn("icicles", parent, new Vector2(x, roof - 0.18f), 0.8f, 8);
                ice.color = new Color(0.85f, 0.92f, 1f);
            }
        }

        static void Terrain(Transform parent, TerrainSpec spec, int order = 9)
        {
            if (spec.shape == TerrainShape.Block)
            {
                RockBlock(parent, new Rect(Origin + spec.position - spec.size * 0.5f, spec.size));
                return;
            }
            if (spec.shape == TerrainShape.IceShelf)
            {
                IceShelf(parent, new Rect(Origin + spec.position - spec.size * 0.5f, spec.size));
                return;
            }
            // Ramp: the same tiled cobble as the blocks, as a triangle mesh, with the same snow rim laid
            // along the slope (user 13/11: ramps were a separate stretched picture and did not match the blocks).
            bool upRight = spec.shape == TerrainShape.RampUpRight;
            float hw = spec.size.x * 0.5f, hh = spec.size.y * 0.5f;
            var root = new GameObject("Ramp").transform;
            root.SetParent(parent, false);
            root.position = Origin + spec.position;
            root.gameObject.AddComponent<PolygonCollider2D>().points = upRight
                ? new[] { new Vector2(-hw, -hh), new Vector2(hw, -hh), new Vector2(hw, hh) }
                : new[] { new Vector2(-hw, -hh), new Vector2(hw, -hh), new Vector2(-hw, hh) };
            Cobble(root, root.GetComponent<PolygonCollider2D>().points, order);
            Vector2 c = root.position;
            surface.Add(upRight ? (c + new Vector2(-hw, -hh), c + new Vector2(hw, hh)) : (c + new Vector2(-hw, hh), c + new Vector2(hw, -hh)));
        }

        /// <summary>Floating ledge (report §14.1, TerrainShape.IceShelf): a perch for a bear out over the open right side.
        /// It cannot be broken, so it is drawn as the same cobble rock with the same snow cap as the rest of the arena
        /// (user 13/11: "băng ko vỡ đc thay bằng đất đá"; ice means breakable).</summary>
        static void IceShelf(Transform parent, Rect r)
        {
            var root = new GameObject("Rock Ledge").transform;
            root.SetParent(parent, false);
            root.position = r.center;
            root.gameObject.AddComponent<BoxCollider2D>().size = r.size;
            float hw = r.width * 0.5f, hh = r.height * 0.5f;
            Cobble(root, new[] { new Vector2(-hw, -hh), new Vector2(-hw, hh), new Vector2(hw, hh), new Vector2(hw, -hh) }, 9);
            // Its own snow strip: it floats, so it is not part of the ground skyline.
            SnowStrip(parent, new List<Vector2> { new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax) });
        }

        /// <summary>Static cobble rock: cobble body, snow rim along the top, box collider, no rigidbody.</summary>
        static void RockBlock(Transform parent, Rect r)
        {
            var root = new GameObject("Rock").transform;
            root.SetParent(parent, false);
            root.position = r.center;
            root.gameObject.AddComponent<BoxCollider2D>().size = r.size;
            float hw = r.width * 0.5f, hh = r.height * 0.5f;
            Cobble(root, new[] { new Vector2(-hw, -hh), new Vector2(-hw, hh), new Vector2(hw, hh), new Vector2(hw, -hh) }, 9);
            surface.Add((new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax)));
        }

        /// <summary>Cobble body of a rock piece: a mesh over the given outline (3 or 4 points, local to `root`), textured
        /// with the cobble tile in WORLD coordinates, so the stones run on unbroken from one piece into its neighbour
        /// (tiled sprites each started their pattern at their own centre and showed a seam at every join).</summary>
        static void Cobble(Transform root, Vector2[] pts, int order)
        {
            // One unbroken piece of the pit's painted wall, mirrored sideways so it never shows a seam; its height
            // (11 units) spans from below the floor to above the tallest rock, so it never repeats vertically.
            var tile = Art.Get("rock_cobble");
            Vector2 tileSize = tile.bounds.size;
            tile.texture.wrapModeU = TextureWrapMode.Mirror;
            tile.texture.wrapModeV = TextureWrapMode.Clamp;
            float baseY = Origin.y - 5.5f;
            var vertices = new Vector3[pts.Length];
            var uv = new Vector2[pts.Length];
            var colors = new Color[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                vertices[i] = pts[i];
                Vector2 w = (Vector2)root.position + pts[i];
                uv[i] = new Vector2(w.x / tileSize.x, (w.y - baseY) / tileSize.y);
                colors[i] = rockTint;
            }
            // Both windings, so the outline's point order does not matter.
            var mesh = new Mesh
            {
                vertices = vertices, uv = uv, colors = colors,
                triangles = pts.Length == 3 ? new[] { 0, 1, 2, 0, 2, 1 } : new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 },
            };
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = root.gameObject.AddComponent<MeshRenderer>();
            if (cobbleMaterial == null) cobbleMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = tile.texture };
            mr.sharedMaterial = cobbleMaterial;
            mr.sortingOrder = order;
        }

        static Material cobbleMaterial;

        /// <summary>Top edges of every rock piece built this level (block tops, ramp slopes), for the snow rim.</summary>
        static readonly List<(Vector2 a, Vector2 b)> surface = new List<(Vector2 a, Vector2 b)>();
        static Material snowMaterial;
        const float SnowStep = 0.05f;      // skyline sample spacing, world units
        const float SnowCliff = 0.3f;      // a rise or drop steeper than this per sample is a rock face: the rim stops there

        /// <summary>ONE continuous snow rim over the whole rock skyline (user 13/11: each piece's own rim overlapped its
        /// neighbours at every corner and the floor's rim showed under the plateaus). The skyline is the highest rock top
        /// at each x; the rim follows it as a strip mesh with mitred corners, its texture running on unbroken, and stops
        /// only at vertical rock faces.</summary>
        static void BuildSnow(Transform parent)
        {
            if (surface.Count == 0) return;
            float x0 = float.MaxValue, x1 = float.MinValue;
            foreach (var (a, b) in surface) { x0 = Mathf.Min(x0, a.x); x1 = Mathf.Max(x1, b.x); }
            var run = new List<Vector2>();
            float? last = null;
            for (float x = x0; x <= x1 + SnowStep * 0.5f; x += SnowStep)
            {
                float? y = null;
                foreach (var (a, b) in surface)
                    if (x >= a.x - 0.001f && x <= b.x + 0.001f)
                    {
                        float k = b.x > a.x ? (x - a.x) / (b.x - a.x) : 0f;
                        float h = Mathf.Lerp(a.y, b.y, Mathf.Clamp01(k));
                        y = y.HasValue ? Mathf.Max(y.Value, h) : h;
                    }
                if (!y.HasValue || (last.HasValue && Mathf.Abs(y.Value - last.Value) > SnowCliff))
                {
                    SnowStrip(parent, run);
                    run.Clear();
                }
                if (y.HasValue) run.Add(new Vector2(x, y.Value));
                last = y;
            }
            SnowStrip(parent, run);
        }

        static void SnowStrip(Transform parent, List<Vector2> raw)
        {
            if (raw.Count < 2) return;
            // Keep only the corners: drop points that lie on the line through their neighbours.
            var pts = new List<Vector2> { raw[0] };
            for (int i = 1; i < raw.Count - 1; i++)
            {
                Vector2 d0 = (raw[i] - pts[pts.Count - 1]).normalized, d1 = (raw[i + 1] - raw[i]).normalized;
                if (Vector2.Dot(d0, d1) < 0.9999f) pts.Add(raw[i]);
            }
            pts.Add(raw[raw.Count - 1]);
            var tex = Art.Get("rock_top");
            float texWidth = tex.bounds.size.x * RimHeight / tex.bounds.size.y;   // world length of one picture, at the rim's height
            tex.texture.wrapModeU = TextureWrapMode.Repeat;
            tex.texture.wrapModeV = TextureWrapMode.Clamp;
            int n = pts.Count;
            var verts = new Vector3[n * 2];
            var uv = new Vector2[n * 2];
            var tris = new List<int>();
            float along = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 dPrev = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                Vector2 dNext = i < n - 1 ? (pts[i + 1] - pts[i]).normalized : dPrev;
                Vector2 nPrev = new Vector2(-dPrev.y, dPrev.x), nNext = new Vector2(-dNext.y, dNext.x);
                Vector2 miter = (nPrev + nNext).normalized;
                float len = Mathf.Min(RimHeight / Mathf.Max(0.3f, Vector2.Dot(miter, nNext)), RimHeight * 2f);
                if (i > 0) along += Vector2.Distance(pts[i - 1], pts[i]);
                verts[i * 2] = pts[i];                         // top edge on the rock's surface
                verts[i * 2 + 1] = pts[i] - miter * len;       // bottom edge, inside the rock
                uv[i * 2] = new Vector2(along / texWidth, 1f);
                uv[i * 2 + 1] = new Vector2(along / texWidth, 0f);
                if (i < n - 1)
                {
                    int a = i * 2;
                    tris.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3, a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }
            var go = new GameObject("Snow");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = new Mesh { vertices = verts, uv = uv, triangles = tris.ToArray() };
            var mr = go.AddComponent<MeshRenderer>();
            if (snowMaterial == null) snowMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex.texture };
            mr.sharedMaterial = snowMaterial;
            mr.sortingOrder = 10;
        }

        IReadOnlyList<Vector2> exits;   // phase-1 exit velocity per pup (null or short: the default entry)
        int spawned;

        /// <summary>Start the attack with the pups that escaped the pit.</summary>
        public void Begin(int ammo, int[] upgradeLevels, Action<bool, int, int> onEnd, IReadOnlyList<Vector2> exits = null)
        {
            this.exits = exits;
            Ammo = ammo;
            upgrades = upgradeLevels;
            this.onEnd = onEnd;
            Block.Armed = true;
            AmmoChanged?.Invoke(Ammo);
            SpawnNext();
        }

        void SpawnNext()
        {
            if (ended || winPending) return;   // a win already decided must not spend another pup (it cost the "pup left" star)
            if (Ammo <= 0)
            {
                StartCoroutine(WaitForSettleThenJudge());
                return;
            }
            Ammo--;
            AmmoChanged?.Invoke(Ammo);
            combo = 0;
            var (at, velocity) = Entry(exits != null && spawned < exits.Count ? exits[spawned] : (Vector2?)null);
            at.x = Mathf.Min(at.x, ViewLeft - 0.7f);   // flies in from beyond the left edge of the (wider) view
            spawned++;
            Current = PufferController.Spawn(at, tuning, upgrades, velocity);
            AudioManager.Sfx("launch");
            Current.Finished += OnPupFinished;
        }

        /// <summary>Where and how fast a pup glides in, from the velocity it left the pit with.</summary>
        public static (Vector2 at, Vector2 velocity) Entry(Vector2? exit)
        {
            if (exit == null || exit.Value.x <= 0f) return (SpawnPoint, SpawnVelocity);
            Vector2 e = exit.Value;
            float k = Mathf.InverseLerp(ExitFlat, ExitSteep, Mathf.Atan2(e.y, e.x) * Mathf.Rad2Deg);
            var at = new Vector2(SpawnPoint.x, Origin.y + Mathf.Lerp(EntryLow, EntryHigh, k));
            return (at, new Vector2(Mathf.Clamp(e.x * EntrySpeedKeep, EntrySpeedMin, EntrySpeedMax), 0f));
        }

        void OnPupFinished(PufferController p)
        {
            if (Current == p) Current = null;
            if (!ended) dives.Add(combo);   // End() records the pup still flying when the level ends
            combo = 0;
            StartCoroutine(After(NextPupDelay, SpawnNext));
        }

        IEnumerator WaitForSettleThenJudge()
        {
            for (float t = 0f; t < SettleTimeout && !ended; t += Time.deltaTime) yield return null;
            // Out of pups: the mission passes when enough bears are down (a perfect run ended earlier).
            // Bonus levels cannot be failed; the boss must be beaten.
            // A beaten king wins even if its finale (cage burst) is still playing when the last pup settles.
            while (!ended && bossBeaten) yield return null;
            if (!ended) End(kind == LevelKind.Bonus || (kind == LevelKind.Boss ? (boss == null || bossBeaten) : BearsDown >= BearsRequired));
        }

        void OnBlockBroken(Block block, int score)
        {
            if (ended) return;   // the result is already saved and shown
            Score += score;
            ScoreChanged?.Invoke(Score);
        }

        void OnFish(Fish fish)
        {
            if (ended) return;
            fishGot++;
            Score += tuning.fishScore;
            ScoreChanged?.Invoke(Score);
        }

        /// <summary>A pressed button shatters every gated block of its group (ice doors).</summary>
        void OnButton(int group)
        {
            foreach (var b in gated) if (b != null && b.Group == group) b.Break();
        }

        void OnBossBeaten(BossBear b)
        {
            if (b != boss || ended) return;
            bossBeaten = true;
            winPending = true;
            StartCoroutine(BossFinale());
        }

        IEnumerator BossFinale()
        {
            yield return new WaitForSeconds(0.8f);
            if (cage != null) yield return cage.Open();
            yield return new WaitForSeconds(0.9f);
            BearsLeft = Mathf.Max(0, BearsLeft - 1);   // the king itself
            BearsChanged?.Invoke(BearsDown, BearsRequired, BearsTotal);
            End(true);
        }

        void OnBearDefeated(Bear bear, int score)
        {
            if (ended) return;   // the result is already saved and shown
            // A bear toppling between pups belongs to the pup that just finished, not the next one.
            int hits = Current == null && dives.Count > 0 ? ++dives[dives.Count - 1] : ++combo;
            Score += score * hits;
            ScoreChanged?.Invoke(Score);
            BearsLeft = Mathf.Max(0, BearsLeft - 1);
            BearsChanged?.Invoke(BearsDown, BearsRequired, BearsTotal);
            if (hits >= 2)
            {
                // Several bears with one pup: the big combo call-out, like the genre's level screens.
                Fx.FloatingText($"×{hits}", ViewCenter + new Vector2(0f, 0.6f), new Color(0.55f, 0.32f, 0.1f), 16f, 1.3f);
                AudioManager.Sfx("combo", 0.1f);
            }
            if (BearsLeft == 0 && !ended && kind != LevelKind.Boss)
            {
                winPending = true;
                StartCoroutine(After(WinDelay, () => End(true)));
            }
        }

        void End(bool won)
        {
            if (ended) return;
            ended = true;
            if (Current != null) dives.Add(combo);   // the winning pup is often still bouncing
            onEnd?.Invoke(won, Ammo, Score);
        }

        static IEnumerator After(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }
    }
}

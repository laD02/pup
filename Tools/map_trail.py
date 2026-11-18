#!/usr/bin/env python3
"""Find the red trail dots ChatGPT painted on a map and write them, in trail order, into Assets/Scripts/UI/MapTrails.cs.

Each world map is painted twice: once with a line of small pure-red dots along the trail (Reference/map_<name>_dots.png),
then the same picture without them (Reference/map_<name>.png, copied to Resources/Art/map_bg_<name>.png). The dots give
the trail; MapView spreads the castle's level stones evenly along it.

Ordering: start at the dot nearest START (bottom-left village), then always step to the nearest unvisited dot.
Usage: python3 -I Tools/map_trail.py canyon forest harbour caves volcano blossom palace tundra throne [--preview DIR]
"""
from __future__ import annotations

import math
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
REF = ROOT / "Assets/Art/Reference"
OUT = ROOT / "Assets/Scripts/UI/MapTrails.cs"
W, H = 1672, 941                 # MapView's painting frame (every world painting is resized to this 16:9 size)
START = (0.1, 0.9)               # share of width/height: the village corner the trail starts from
END = (0.86, 0.12)               # and the fortress gate it ends at
MIN_AREA, MAX_AREA = 12, 2500    # px at W×H: a dot, not a speck or a red roof


def dots(path: Path) -> list[tuple[float, float]]:
    im = Image.open(path).convert("RGB").resize((W, H), Image.LANCZOS)
    a = np.asarray(im).astype(int)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mask = (r > 190) & (g < 75) & (b < 75) & (r - g > 140) & (np.abs(g - b) < 40)   # pure red, not orange lava
    h, w = mask.shape
    seen = np.zeros_like(mask)
    found = []
    for y0, x0 in zip(*np.nonzero(mask)):
        if seen[y0, x0]:
            continue
        stack, xs, ys = [(y0, x0)], [], []
        seen[y0, x0] = True
        while stack:
            y, x = stack.pop()
            xs.append(x)
            ys.append(y)
            for yy, xx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                if 0 <= yy < h and 0 <= xx < w and mask[yy, xx] and not seen[yy, xx]:
                    seen[yy, xx] = True
                    stack.append((yy, xx))
        if MIN_AREA <= len(xs) <= MAX_AREA:
            found.append((float(np.mean(xs)), float(np.mean(ys))))
    return found


def order(points: list[tuple[float, float]]) -> list[tuple[float, float]]:
    """Chains, then joins. Each dot links to its two nearest dots when they are about one dot-step away and the link is
    mutual: that gives the dotted runs, broken only where the painting has a gap (a bridge, a pier). Runs of fewer than
    MIN_RUN dots are stray red marks (flags, roofs). Starting from the run nearest START, the run whose end is closest to
    the current end is attached next, until none is left."""
    n = len(points)
    dist = [[math.dist(points[i], points[j]) for j in range(n)] for i in range(n)]
    near = [sorted((j for j in range(n) if j != i), key=lambda j: dist[i][j])[:2] for i in range(n)]
    step = sorted(dist[i][near[i][0]] for i in range(n))[n // 2]
    adj = [[j for j in near[i] if i in near[j] and dist[i][j] < step * LINK] for i in range(n)]
    seen, runs = set(), []
    for i in range(n):
        if i in seen:
            continue
        comp, stack = [], [i]
        seen.add(i)
        while stack:
            u = stack.pop()
            comp.append(u)
            for v in adj[u]:
                if v not in seen:
                    seen.add(v)
                    stack.append(v)
        ends = [u for u in comp if len(adj[u]) < 2] or [comp[0]]
        run, prev, cur = [ends[0]], -1, ends[0]
        while True:
            nxt = [v for v in adj[cur] if v != prev and v not in run]
            if not nxt:
                break
            prev, cur = cur, nxt[0]
            run.append(cur)
        if len(run) >= MIN_RUN:
            runs.append(run)
    return runs


def route(points, runs, plan):
    """The level route through the hand-picked runs of ROUTES: each run is entered at its dot nearest to where the last
    one left off (the first at the dot nearest START) and left at its dot nearest to the next run (the last at the dot
    nearest END), so a run may be walked only in part (a fork leaves it in the middle)."""
    sx, sy, ex, ey = START[0] * W, START[1] * H, END[0] * W, END[1] * H
    out = []
    tail = (sx, sy)
    for n, k in enumerate(plan):
        run = [points[i] for i in runs[k - 1]]
        enter = min(range(len(run)), key=lambda i: math.dist(run[i], tail))
        if n + 1 < len(plan):
            nxt = [points[i] for i in runs[plan[n + 1] - 1]]
            leave = min(range(len(run)), key=lambda i: min(math.dist(run[i], q) for q in nxt))
        else:
            leave = min(range(len(run)), key=lambda i: math.dist(run[i], (ex, ey)))
        part = run[enter:leave + 1] if leave >= enter else run[leave:enter + 1][::-1]
        out += part
        tail = part[-1]
    return out


def auto_route(points, runs, name):
    """For single trails (no forks): from the run end nearest the world's start village, keep attaching the run whose
    end is nearest the current tail, within JOIN px; stray red marks too far from the trail are never reached."""
    (sx, sy), (ex, ey) = [(x * W, y * H) for x, y in ENDS.get(name, (START, END))]
    left = [[points[i] for i in r] for r in runs]
    first = min(left, key=lambda r: min(math.dist(r[0], (sx, sy)), math.dist(r[-1], (sx, sy))))
    left.remove(first)
    if math.dist(first[-1], (sx, sy)) < math.dist(first[0], (sx, sy)):
        first.reverse()
    trail = list(first)
    while left and math.dist(trail[-1], (ex, ey)) > ARRIVED:
        tail = trail[-1]
        best = min(left, key=lambda r: min(math.dist(tail, r[0]), math.dist(tail, r[-1])))
        if min(math.dist(tail, best[0]), math.dist(tail, best[-1])) > JOIN:
            break
        left.remove(best)
        if math.dist(tail, best[-1]) < math.dist(tail, best[0]):
            best.reverse()
        trail += best
    return trail


JOIN = 220      # px: the widest gap in a dotted trail (a bridge)
ARRIVED = 130   # px from the fortress: the trail is complete, red marks beyond it (banners) are not joined

# Where each world's road starts (village) and ends (fortress gate), share of width/height. They are the S and F of the
# layout plan the painting was made from (README "World maps"): every world has its own road shape.
ENDS = {
    "canyon": ((.10, .85), (.85, .15)), "forest": ((.08, .45), (.82, .18)), "harbour": ((.50, .88), (.85, .10)),
    "caves": ((.10, .18), (.88, .82)), "volcano": ((.10, .85), (.42, .52)), "blossom": ((.90, .80), (.12, .15)),
    "palace": ((.10, .85), (.85, .72)), "tundra": ((.08, .15), (.90, .75)), "throne": ((.10, .82), (.52, .16)),
}


# Hand-picked run order per world (the paintings fork: dead-end side paths and loops are left out). Re-pick when a
# painting changes: run the tool with --preview to see the numbered runs.
# Every world is now painted as ONE road (user 17/11: "các bản đồ chỉ 1 đường thôi"), so all are joined automatically.
ROUTES: dict[str, list[int] | str] = {w: "auto" for w in ("canyon", "forest", "harbour", "caves", "volcano", "blossom", "palace", "tundra", "throne")}


LINK = 1.8      # a link is at most this many dot-steps long
GAP = 9         # a painting gap (bridge, stepping stones) may span this many dot-steps
MIN_RUN = 4     # dots in a run, or it is a stray mark


def clean(name: str) -> None:
    """Writes Reference/map_<name>.png: the dotted painting with its red dots painted over by the snow around them, so
    the clean picture lines up with the trail exactly (a second ChatGPT edit without the dots moves things)."""
    source, target = REF / f"map_{name}_dots.png", REF / f"map_{name}.png"
    if target.exists() and "--force" not in sys.argv and target.stat().st_mtime >= source.stat().st_mtime:
        # Already made from this dots painting, or fixed by hand afterwards: kept. (git does not keep file times: after a
        # fresh clone use --force to remake every clean painting from its dots copy.)
        print(f"{name}: kept map_{name}.png (newer than its dots copy; --force remakes it)")
        return
    im = Image.open(source).convert("RGB").resize((W, H), Image.LANCZOS)
    a = np.asarray(im).astype(int)
    m = (a[..., 0] > 170) & (a[..., 1] < 110) & (a[..., 2] < 110) & (a[..., 0] - a[..., 1] > 90) & (np.abs(a[..., 1] - a[..., 2]) < 45)
    mask = Image.fromarray((m * 255).astype("uint8")).filter(ImageFilter.MaxFilter(9))
    Image.composite(im.filter(ImageFilter.MedianFilter(21)), im, mask).save(REF / f"map_{name}.png")


SEA = ("world", "canyon", "harbour", "caves", "palace", "throne")   # worlds whose surroundings are open water (fish leap there)


def water(name: str, count: int = 10) -> list[tuple[int, int]]:
    """Calm open-water points of a painting, spread out: where the map's fish leap. Water = the colour of the picture's
    outer rim (every world is land inside a sea), in a patch that is all that colour, away from the edges and top bar."""
    im = Image.open(REF / f"map_{name}.png").convert("RGB").resize((W // 8, H // 8), Image.BOX)
    a = np.asarray(im).astype(int)
    rim = np.concatenate([a[-3:].reshape(-1, 3), a[:, :3].reshape(-1, 3), a[:, -3:].reshape(-1, 3)])
    sea = np.median(rim, axis=0)
    like = (np.abs(a - sea).sum(axis=2) < 120).astype(np.uint8) * 255
    calm = np.asarray(Image.fromarray(like).filter(ImageFilter.MinFilter(5))) > 0   # 5 cells = 40 px of water all round
    h, w = calm.shape
    cand = [(x * 8 + 4, y * 8 + 4) for y in range(int(h * 0.2), int(h * 0.92)) for x in range(int(w * 0.06), int(w * 0.94)) if calm[y, x]]
    if not cand:
        return []
    out = [cand[len(cand) // 2]]
    while len(out) < count:
        best = max(cand, key=lambda c: min(math.dist(c, o) for o in out))
        if min(math.dist(best, o) for o in out) < 150:
            break
        out.append(best)
    return out


def vectors(text: str) -> list[tuple[float, float]]:
    return [(float(x), float(y)) for x, y in re.findall(r"new Vector2\(([\d.]+)f?, ([\d.]+)f?\)", text)]


NO_BEAR: set[str] = set()


def stones_of(trail: list[tuple[float, float]], count: int = 41) -> list[tuple[float, float]]:
    """The level stones MapView spreads evenly along a trail."""
    seg = [math.dist(trail[k], trail[k + 1]) for k in range(len(trail) - 1)]
    total, out = sum(seg), []
    for k in range(count):
        t, j = total * k / (count - 1), 0
        while j < len(seg) - 1 and t > seg[j]:
            t -= seg[j]
            j += 1
        f = t / seg[j] if seg[j] else 0
        out.append((trail[j][0] + (trail[j + 1][0] - trail[j][0]) * f, trail[j][1] + (trail[j + 1][1] - trail[j][1]) * f))
    return out


def bear_spots(name: str, trail: list[tuple[float, float]], count: int = 2) -> list[tuple[int, int]]:
    """Open snow beside the trail where a polar bear stands and looks around (CPC's map has one by the path). Snow =
    the colour of the trail itself (every road is a snowy path), so tinted snow counts and sea, lava or lake never does;
    the bear's whole body (70x70 px above its feet) must be that plain snow, 90+ px from every stone, within 260 px of the
    trail, below the top bar; spread apart."""
    rgb = np.asarray(Image.open(REF / f"map_{name}.png").convert("RGB").resize((W, H), Image.LANCZOS)).astype(float)
    road = np.median(np.array([rgb[int(y), int(x)] for x, y in trail[::3]]), axis=0)
    snow = np.abs(rgb - road).sum(axis=2) < 110
    stones = stones_of(trail)
    cand = []
    for y in range(int(H * 0.2), int(H * 0.92), 8):
        for x in range(60, W - 60, 8):
            if min(math.dist((x, y - 35), st) for st in stones) < 80:
                continue
            if min(math.dist((x, y), t) for t in trail[::2]) > 260:
                continue
            body = snow[y - 70:y + 6, x - 35:x + 35]
            if body.size and body.mean() > 0.8 and rgb[y - 70:y + 6, x - 35:x + 35].std() < 38:
                cand.append((x, y))
    if not cand:
        return []
    mid = (W / 2, H * 0.55)
    out = [min(cand, key=lambda c: math.dist(c, mid))]
    while len(out) < count:
        best = max(cand, key=lambda c: min(math.dist(c, o) for o in out))
        if min(math.dist(best, o) for o in out) < 400:
            break
        out.append(best)
    return out


def depth_map(path: Path) -> np.ndarray:
    """How deep each pixel sits inside the light snow of the clean painting (erosions survived): the trail is a light band
    between dark outlines, so its centre line is where this peaks across the band."""
    im = Image.open(path).convert("L").resize((W, H), Image.LANCZOS)
    light = im.point(lambda v: 255 if v > LIGHT else 0)
    depth = np.zeros((H, W), np.int32)
    cur = light
    for _ in range(DEPTH_MAX):
        arr = np.asarray(cur) > 0
        if not arr.any():
            break
        depth += arr
        cur = cur.filter(ImageFilter.MinFilter(3))
    return depth


def centre(trail: list[tuple[float, float]], depth: np.ndarray) -> list[tuple[float, float]]:
    """Moves each trail point across the trail (along its normal, up to REACH px either way) to the deepest point of the
    snow band: ChatGPT puts the dots near one edge of the path, not on its middle. Then smooths the line a little."""
    out = []
    for i, (x, y) in enumerate(trail):
        a, b = trail[max(0, i - 2)], trail[min(len(trail) - 1, i + 2)]
        tx, ty = b[0] - a[0], b[1] - a[1]
        n = math.hypot(tx, ty) or 1.0
        nx, ny = -ty / n, tx / n
        # Start from the dot itself and move only to a strictly deeper point: on a flat band (dark ground, where nothing
        # passes LIGHT, or a wide snowfield) the dot stays put instead of jumping REACH px to one side.
        x0, y0 = int(round(x)), int(round(y))
        best, bx, by = (depth[y0, x0] if 0 <= x0 < W and 0 <= y0 < H else -1), x, y
        for t in sorted(range(-REACH, REACH + 1, 2), key=abs):   # nearest first, so ties keep the closer point
            px, py = int(round(x + nx * t)), int(round(y + ny * t))
            if 0 <= px < W and 0 <= py < H and depth[py, px] > best:
                best, bx, by = depth[py, px], px, py
        out.append((float(bx), float(by)))
    smooth = []
    for i in range(len(out)):
        pts = out[max(0, i - 2):i + 3]
        smooth.append((sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts)))
    return smooth


LIGHT = 200       # grey level: the snow trail and snowfields are lighter than this
DEPTH_MAX = 40    # erosion steps (px): deeper than any trail is wide
REACH = 40        # px either side of a dot searched for the trail's middle


def check(trail: list[tuple[float, float]], name: str) -> None:
    if len(trail) < 20:
        raise SystemExit(f"{name}: only {len(trail)} dots found")
    gaps = [math.dist(trail[i - 1], trail[i]) for i in range(1, len(trail))]
    median = sorted(gaps)[len(gaps) // 2]
    worst = max(gaps)
    if worst > median * 10:
        raise SystemExit(f"{name}: a jump of {worst:.0f} px (median {median:.0f}): the order skipped part of the trail")


def main() -> None:
    preview = sys.argv[sys.argv.index("--preview") + 1] if "--preview" in sys.argv else None
    names = [a for a in sys.argv[1:] if not a.startswith("--") and a != preview]
    trails = {}
    missing = [n for n in names if not (REF / f"map_{n}_dots.png").exists()]
    if missing:   # checked before any painting is touched
        raise SystemExit(f"missing in Assets/Art/Reference: {', '.join(f'map_{n}_dots.png' for n in missing)}; nothing was written")
    for name in names:
        clean(name)
        pts = dots(REF / f"map_{name}_dots.png")
        runs = order(pts)
        runs.sort(key=lambda r: -len(r))
        if preview:
            im = Image.open(REF / f"map_{name}_dots.png").convert("RGB").resize((W, H))
            d = ImageDraw.Draw(im)
            for k, r in enumerate(runs, 1):
                d.line([pts[i] for i in r], fill=(0, 255, 0), width=5)
                for end, tag in ((r[0], "a"), (r[-1], "b")):
                    x, y = pts[end]
                    d.rectangle([x - 2, y - 14, x + 34, y + 2], fill=(0, 0, 0))
                    d.text((x, y - 13), f"{k}{tag}", fill=(255, 255, 0))
            im.save(Path(preview) / f"runs_{name}.png")
        if name not in ROUTES:
            print(f"{name}: {len(runs)} runs, no route picked yet")
            continue
        plan = ROUTES[name]
        trail = auto_route(pts, runs, name) if plan == "auto" else route(pts, runs, plan)
        trail = centre(trail, depth_map(REF / f"map_{name}.png"))
        check(trail, name)
        trails[name] = trail
        print(f"{name}: {len(trail)} dots")
    old = OUT.read_text() if OUT.exists() else ""
    fields = dict(re.findall(r"public static readonly Vector2\[\] (\w+) =\s*(\{.*?\}|null);", old, re.S))
    for name, trail in trails.items():
        rows = ", ".join(f"new Vector2({x:.0f}, {y:.0f})" for x, y in trail)
        fields[name.capitalize()] = "{ " + rows + " }"
    for name in SEA:
        pts = water(name)
        fields[name.capitalize() + "Water"] = "{ " + ", ".join(f"new Vector2({x}, {y})" for x, y in pts) + " }"
        print(f"{name}: {len(pts)} water points")
    view = (ROOT / "Assets/Scripts/UI/MapView.cs").read_text()
    world_trail = vectors(view[view.index("static readonly Vector2[] Trail"):view.index("};", view.index("static readonly Vector2[] Trail"))])
    for name in ("world", "canyon", "forest", "harbour", "caves", "volcano", "blossom", "palace", "tundra", "throne"):
        trail = world_trail if name == "world" else vectors(fields.get(name.capitalize(), ""))
        if not trail:
            continue
        pts = [] if name in NO_BEAR else bear_spots(name, trail)
        fields[name.capitalize() + "Bears"] = "{ " + ", ".join(f"new Vector2({x}, {y})" for x, y in pts) + " }"
        print(f"{name}: {len(pts)} bear spots")
    for k in ("Canyon", "Forest", "Harbour", "Caves", "Volcano", "Blossom", "Palace", "Tundra", "Throne"):
        fields.setdefault(k, "null")
    body = "\n".join(f"        public static readonly Vector2[] {k} = {v};" for k, v in sorted(fields.items()))
    OUT.write_text(f"""using UnityEngine;

namespace SealCatapult.UI
{{
    /// <summary>Trails through the world map paintings (painting px at 1672×941, y down, village to fortress).
    /// Generated by Tools/map_trail.py from the red-dot paintings in Assets/Art/Reference; do not edit by hand.</summary>
    public static class MapTrails
    {{
{body}
    }}
}}
""")


if __name__ == "__main__":
    main()

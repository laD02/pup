#!/usr/bin/env python3
"""Find the level stones painted on a map (ChatGPT draws them; MapView anchors its buttons on them) and print their
centres, ordered along the trail, as the C# array for MapView.Stones.

Stones are the blue-grey discs (fill about RGB 165,172,197) on the white snow path. Ordering: nearest neighbour from the
stone closest to START; stones within TOP_ISLAND are ordered by hand rule (lower band right→left, the loop's left curve
bottom→top, upper band left→right), because the nearest-neighbour walk jumps between the two bands there.
Usage: python3 -I Tools/map_stones.py Assets/Art/Reference/map_world_stones40.png [preview.png]
"""
from __future__ import annotations
import math
import sys
import numpy as np
from PIL import Image, ImageDraw

START = (250, 290)                      # igloo village end of the trail (painting px)
TOP_ISLAND = (1150, 0, 1672, 320)       # x0, y0, x1, y1
MIN_AREA, MAX_W, MIN_W = 150, 75, 15


def find(path: str) -> list[tuple[float, float]]:
    a = np.asarray(Image.open(path).convert("RGB")).astype(int)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    lum = (r + g + b) / 3
    mask = (lum > 140) & (lum < 200) & (b - r < 48) & (b - r > 15) & (np.abs(g - r) < 18)
    h, w = mask.shape
    lab = np.zeros((h, w), np.int32)
    out, n = [], 0
    for y in range(h):
        for x in np.nonzero(mask[y] & (lab[y] == 0))[0]:
            if lab[y, x]:
                continue
            n += 1
            stack, xs, ys = [(y, x)], [], []
            lab[y, x] = n
            while stack:
                yy, xx = stack.pop()
                xs.append(int(xx)); ys.append(int(yy))
                for y2, x2 in ((yy + 1, xx), (yy - 1, xx), (yy, xx + 1), (yy, xx - 1)):
                    if 0 <= y2 < h and 0 <= x2 < w and mask[y2, x2] and not lab[y2, x2]:
                        lab[y2, x2] = n
                        stack.append((y2, x2))
            bw, bh = max(xs) - min(xs), max(ys) - min(ys)
            if len(xs) > MIN_AREA and MIN_W < bw < MAX_W and 8 < bh < 60:
                out.append((sum(xs) / len(xs), sum(ys) / len(ys)))
    return out


def order(pts: list[tuple[float, float]]) -> list[tuple[float, float]]:
    x0, y0, x1, y1 = TOP_ISLAND
    top = [p for p in pts if x0 < p[0] < x1 and y0 < p[1] < y1]
    rest = [p for p in pts if p not in top]
    seq = [min(rest, key=lambda p: math.dist(p, START))]
    pool = [p for p in rest if p != seq[0]]
    while pool:
        nxt = min(pool, key=lambda p: math.dist(p, seq[-1]))
        seq.append(nxt)
        pool.remove(nxt)
    if top:
        mid = sorted(p[1] for p in top)[len(top) // 2]
        lower = sorted([p for p in top if p[1] > mid + 5], key=lambda p: -p[0])
        upper = [p for p in top if p[1] <= mid + 5]
        left = min(p[0] for p in upper) + 45
        curve = sorted([p for p in upper if p[0] < left], key=lambda p: -p[1])
        upper = sorted([p for p in upper if p[0] >= left], key=lambda p: p[0])
        seq += lower + curve + upper
    return seq


def main() -> None:
    seq = order(find(sys.argv[1]))
    gaps = [math.dist(seq[i], seq[i + 1]) for i in range(len(seq) - 1)]
    print(f"// {len(seq)} stones, gaps {min(gaps):.0f}–{max(gaps):.0f} px")
    for i in range(0, len(seq), 6):
        print("            " + ", ".join(f"new Vector2({x:.0f}, {y:.0f})" for x, y in seq[i:i + 6]) + ",")
    if len(sys.argv) > 2:
        im = Image.open(sys.argv[1]).convert("RGB")
        d = ImageDraw.Draw(im)
        d.line(seq, fill=(255, 0, 0), width=3)
        for i, p in enumerate(seq):
            d.text((p[0] + 8, p[1] - 16), str(i + 1), fill=(0, 0, 0))
        im.save(sys.argv[2])


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Cut the hero sprite sheets (Assets/Art/Reference/*.png, made in ChatGPT) into game frames.

Each sheet is an even grid on a white background. Per frame: the white that touches the image border
becomes transparent (white fur inside the navy outline stays), the frame is cropped, scaled so every
sheet draws the pup at the same size (the red beanie measured on a standing row, BEANIE_PX across;
the standing pup is then about 300 px tall), and placed on a per-animation canvas around a fixed
anchor so frames never jump. Rows with the same name join into one animation.
  bottom - feet on the canvas bottom, body centroid at the canvas x centre (standing poses)
  center - body centroid at the canvas centre (in the air)
  rope   - top of the drawn rope at the canvas top centre (hang; the game's rope continues it)
  cell   - every frame keeps its place in the grid cell (lever: the lever base must not move)
PupRig reads the anchor per pose; keep ANCHOR in sync with PupRig.cs.

Usage: python3 -I Tools/slice_sheets.py   (then run ProjectBuilder.BuildAll to reimport)
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
REF = ROOT / "Assets/Art/Reference"
OUT = ROOT / "Assets/Resources/Art"

BEANIE_PX = 50.0        # sqrt of the red beanie area on a standing pup, output pixels (idle measured 50)
WHITE = 232             # all channels above this count as background when connected to the border
PAD = 4

# sheet file, output prefix, columns, rows, row names, scale rule: a row name (its red beanie must measure BEANIE_PX)
# or ("height", row name, px) (that row's frames are scaled to this height). A missing sheet is skipped with a note.
SHEETS = [
    ("pup_walk_sheet.png", "pup", 6, 1, ["walk"], "walk"),
    ("pup_sheet_a.png", "pup", 4, 4, ["idle", "hang", "flail", "cheer"], "idle"),
    ("pup_sheet_c.png", "pup", 4, 4, ["leap", "glide", "dizzy", "ball"], "dizzy"),
    ("pup_sheet_d.png", "pup", 4, 4, ["lever", "lever", "launch", "fall"], "lever"),
    # hero sheet E (docs/pup-art-brief-2.md): level flight (2 rows = 8 frames), nose-down turn + ninja hang, getting up
    ("pup_sheet_e.png", "pup", 4, 4, ["fly", "fly", "divening", "getup"], "getup"),
    # hero sheet F (v10 redesign, ChatGPT edit of sheet A): grab the rope, stuck upside down, flinch, spin ball
    ("pup_sheet_f.png", "pup", 4, 4, ["grab", "stuck", "flinch", "spin"], "flinch"),
    # hero sheet G (report §11): saluting on the mission card; four near-identical rows, the first one is the loop
    ("pup_sheet_g.png", "pup", 4, 4, ["salute"] * 4, "salute"),
    # bare-headed pup (hat knocked off in the arena): ChatGPT edits of sheets E, walk, C and A with the hat removed; only
    # the rows used after the hat is gone are cut. No beanie to measure: each uses ("scale", s), the scale this script
    # prints for its hatted sheet (the edits keep the same canvas and cell layout), so both sets are the same size.
    ("pup_sheet_e_bare.png", "pup", 4, 4, [None, None, None, "getup_bare"], ("scale", 1.231)),
    ("pup_walk_bare.png", "pup", 6, 1, ["walk_bare"], ("scale", 0.592)),
    ("pup_sheet_c_bare.png", "pup", 4, 4, [None, None, "dizzy_bare", "ball_bare"], ("scale", 1.058)),
    ("pup_sheet_a_bare.png", "pup", 4, 4, ["idle_bare", None, "flail_bare", "cheer_bare"], ("scale", 1.069)),
    # polar bear guard (docs/bear-art-brief.md): standing bear 260 px tall, feet on the canvas bottom
    ("bear_sheet.png", "bear", 4, 4, ["idle", "taunt", "dizzy", "ko"], ("height", "idle", 260)),
    ("bear_sheet_b.png", "bear", 4, 2, ["eat", "sleep"], ("height", "eat", 260)),
    ("bear_sheet_c.png", "bear", 4, 4, ["flex", "laugh", "scratch", "juggle"], ("height", "flex", 260)),
    # v10: more stand-in-place antics (GDD §R4): shadow-boxing, sitting hugging knees, smug nod, eating from a bowl
    ("bear_sheet_d.png", "bear", 4, 4, ["punch", "sit", "smug", "bowl"], ("height", "punch", 260)),
    # report §11: sitting on a ledge with legs dangling. ChatGPT drew four different sitting angles per row; only the
    # third column (legs hanging straight down) is the dangle, so that column read top to bottom is the loop.
    ("bear_sheet_e.png", "bear", 4, 4, ["dangle"] * 4, ("height", "dangle", 230), 2),
]
ANCHOR = {"getup_bare": "bottom", "walk_bare": "bottom", "dizzy_bare": "bottom", "cheer_bare": "bottom", "idle_bare": "bottom", "flail_bare": "center", "ball_bare": "center", "salute": "bottom", "dangle": "bottom", "punch": "bottom", "sit": "bottom", "smug": "bottom", "bowl": "bottom", "grab": "bottom", "stuck": "center", "flinch": "bottom", "spin": "center", "fly": "center", "divening": "center", "getup": "bottom", "eat": "bottom", "sleep": "bottom",
          "laugh": "bottom", "scratch": "bottom", "juggle": "bottom", "flex": "bottom",
          "leap": "bottom", "walk": "bottom", "idle": "bottom", "cheer": "bottom", "dizzy": "bottom", "taunt": "bottom", "ko": "bottom",
          "hang": "rope", "flail": "center", "glide": "center", "ball": "center",
          "lever": "cell", "launch": "center", "fall": "center"}
# single sprites used by menus and the dive (ball spins in code, so one ball frame is enough)
ALIASES = {"pup": "pup_idle_0", "pup_ball": "pup_ball_0", "pup_ball_bare": "pup_ball_bare_0", "pup_dizzy": "pup_dizzy_2", "bear": "bear_idle_0", "bear_helmet": "bear_idle_0"}


def remove_background(rgb: np.ndarray) -> np.ndarray:
    """Alpha mask: 0 where near-white pixels connect to the border, else 255."""
    light = (rgb > WHITE).all(axis=2)
    reach = np.zeros_like(light)
    reach[0, :], reach[-1, :], reach[:, 0], reach[:, -1] = light[0, :], light[-1, :], light[:, 0], light[:, -1]
    while True:   # grow the border region through light pixels until it stops
        grown = reach.copy()
        grown[1:, :] |= reach[:-1, :]
        grown[:-1, :] |= reach[1:, :]
        grown[:, 1:] |= reach[:, :-1]
        grown[:, :-1] |= reach[:, 1:]
        grown &= light
        if (grown == reach).all():
            break
        reach = grown
    return np.where(reach, 0, 255).astype(np.uint8)


def beanie(img: Image.Image) -> float:
    """sqrt of the red beanie area: a size measure that ignores poses, props and the scarf."""
    x = np.asarray(img.convert("RGBA")).astype(int)
    red = (x[..., 0] > 170) & (x[..., 1] < 80) & (x[..., 2] < 80) & (x[..., 3] > 200)
    return float(red.sum()) ** 0.5


def cells(path: Path, cols: int, rows: int) -> list[list[Image.Image | None]]:
    """Grid cells with the background removed, uncropped (frames keep their place in the cell). An empty cell stays in
    the grid as None so later frames keep their number (a spare or missing drawing never renumbers the rest)."""
    rgb = np.asarray(Image.open(path).convert("RGB"))
    rgba = Image.fromarray(np.dstack([rgb, remove_background(rgb)]), "RGBA")
    w, h = rgba.size
    grid = []
    for r in range(rows):
        row = []
        for c in range(cols):
            cell = rgba.crop((w * c // cols, h * r // rows, w * (c + 1) // cols, h * (r + 1) // rows))
            if cell.getchannel("A").getbbox() is None:
                print(f"{path.name}: empty cell row {r} col {c} (no frame written for it)")
                cell = None
            row.append(cell)
        grid.append(row)
    return grid


def anchor_point(img: Image.Image, mode: str) -> tuple[float, float]:
    a = np.asarray(img.getchannel("A")) > 128
    ys, xs = np.nonzero(a)
    if mode == "rope":   # drawn rope: the opaque run in the top rows
        top = np.nonzero(a[:6].any(axis=0))[0]
        if top.size == 0:
            raise ValueError("rope anchor: no rope drawn in the top 6 rows of the frame")
        return float(top.mean()), 0.0
    cx, cy = float(xs.mean()), float(ys.mean())
    return cx, (float(img.height) if mode == "bottom" else cy)


def place(frames: list[Image.Image], mode: str) -> list[Image.Image]:
    """Same canvas for the whole animation, each frame's anchor on the same canvas point."""
    anchors = [anchor_point(f, mode) for f in frames]
    half_w = max(max(ax, f.width - ax) for f, (ax, _) in zip(frames, anchors))
    if mode == "center":
        half_h = max(max(ay, f.height - ay) for f, (_, ay) in zip(frames, anchors))
        size, origin = (half_w * 2, half_h * 2), (half_w, half_h)
    else:
        size, origin = (half_w * 2, max(f.height for f in frames)), (half_w, 0.0)
    w, h = int(round(size[0])) + PAD * 2, int(round(size[1])) + PAD * 2
    out = []
    for f, (ax, ay) in zip(frames, anchors):
        canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        x = int(round(PAD + origin[0] - ax))
        y = h - PAD - f.height if mode == "bottom" else int(round(PAD + origin[1] - ay))
        canvas.alpha_composite(f, (x, y))
        out.append(canvas)
    return out


def crop(frames: list[Image.Image], mode: str) -> list[Image.Image]:
    """Each frame to its own bounding box, or all to their shared box in "cell" mode."""
    boxes = [f.getchannel("A").getbbox() for f in frames]
    if mode == "cell":
        union = (min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes))
        return [f.crop(union) for f in frames]
    return [f.crop(b) for f, b in zip(frames, boxes)]


# Rows that ChatGPT drew standing on a slab of ice ledge: the slab is erased so the pup stands on the game's own bridge.
STRIP_LEDGE = {"grab"}


def strip_ledge(img: Image.Image) -> Image.Image:
    """Erase a wide slab under the feet: the band of rows, counted up from the bottom, opaque across 55% of the width."""
    a = np.asarray(img).copy()
    opaque = a[..., 3] > 128
    h, w = opaque.shape
    y = h - 1
    while y > 0 and opaque[y].sum() >= 0.55 * w:
        y -= 1
    if y < h - 1:                     # a slab was there (frames on the rope have none)
        a[max(0, y - 3):, :, 3] = 0   # a few rows more take the slab's snow lip
    out = Image.fromarray(a, "RGBA")
    box = out.getchannel("A").getbbox()
    return out.crop(box) if box else out


def pad(img: Image.Image) -> Image.Image:
    canvas = Image.new("RGBA", (img.width + PAD * 2, img.height + PAD * 2), (0, 0, 0, 0))
    canvas.alpha_composite(img, (PAD, PAD))
    return canvas


def median_of(values: list[float], what: str) -> float:
    if not values:
        raise ValueError(f"no frames to measure {what} on")
    return float(np.median(values))


def remove_stale(prefix: str, anim: str, keep: set[str]) -> None:
    """Frames of this animation left from an earlier cut that has more frames (exact name match: walk is not walk_bare)."""
    pattern = re.compile(rf"{re.escape(prefix)}_{re.escape(anim)}_\d+")
    for png in OUT.glob(f"{prefix}_{anim}_*.png"):
        if pattern.fullmatch(png.stem) and png.stem not in keep:
            png.unlink()
            png.with_suffix(".png.meta").unlink(missing_ok=True)
            print(f"  removed stale {png.name}")


def out_name(prefix: str, anim: str, slot: int) -> str | None:
    """File name for a frame by its slot in the animation's cells; None for a cell that is not a frame."""
    if anim != "divening":
        return f"{prefix}_{anim}_{slot}"
    # the mixed row of sheet E: two nose-down frames, then the ninja hang; the 4th cell is a spare (PupRig loads 1)
    return f"{prefix}_dive_{slot}" if slot < 2 else (f"{prefix}_ninja_0" if slot == 2 else None)


def main() -> None:
    failed = []
    for entry in SHEETS:
        try:
            cut_sheet(entry)
        except (ValueError, OSError, KeyError, IndexError) as e:
            failed.append(f"{entry[0]}: {e}")
            print(f"{entry[0]}: FAILED, {e}")
    for alias, source in ALIASES.items():
        if (OUT / f"{source}.png").exists():
            Image.open(OUT / f"{source}.png").save(OUT / f"{alias}.png")
        else:
            print(f"alias {alias}: source {source}.png missing, old copy left as is")
    if failed:
        sys.exit("some sheets were not cut:\n  " + "\n  ".join(failed))


def cut_sheet(entry: tuple) -> None:
    """Cuts one sheet; ValueError when it cannot be measured or anchored (main collects these and exits 1)."""
    name, prefix, cols, rows, row_names, rule = entry[:6]
    only_col = entry[6] if len(entry) > 6 else None
    if not (REF / name).exists():
        print(f"{name}: not found, skipped")
        return
    OUT.mkdir(parents=True, exist_ok=True)
    grid = cells(REF / name, cols, rows)
    if only_col is not None:
        grid = [[row[only_col]] for row in grid]
    if isinstance(rule, tuple) and rule[0] == "scale":
        scale = rule[1]
    elif isinstance(rule, tuple):
        _, row, px = rule
        heights = [f.getchannel("A").getbbox()[3] - f.getchannel("A").getbbox()[1] for f in grid[row_names.index(row)] if f]
        scale = px / median_of(heights, f"the height of row '{row}'")
    else:
        scale = BEANIE_PX / median_of([beanie(f) for f in grid[row_names.index(rule)] if f], f"the beanie of row '{rule}'")
    anims: dict[str, list[Image.Image | None]] = {}
    for row, anim in zip(grid, row_names):
        if anim is not None:   # None = a row of the sheet that is not used
            anims.setdefault(anim, []).extend(row)
    # Measure and place every anim first, then write: a sheet that fails leaves its old frames untouched.
    results = []
    for anim, cells_ in anims.items():
        slots = [i for i, f in enumerate(cells_) if f is not None and out_name(prefix, anim, i)]
        gaps = [i for i, f in enumerate(cells_) if f is None and out_name(prefix, anim, i)]
        if gaps:   # the rigs load frames 0..n-1 in order; a hole would show an empty sprite
            raise ValueError(f"{anim}: empty cell(s) {gaps}")
        if not slots:
            raise ValueError(f"{anim}: every cell is empty")
        frames = [cells_[i] for i in slots]
        mode = ANCHOR[anim]
        scaled = [f.resize((max(1, round(f.width * scale)), max(1, round(f.height * scale))), Image.LANCZOS)
                  for f in crop(frames, mode)]
        if anim in STRIP_LEDGE:
            scaled = [strip_ledge(f) for f in scaled]
        try:
            placed = place(scaled, mode) if mode != "cell" else [pad(f) for f in scaled]
        except ValueError as e:
            raise ValueError(f"{anim}: {e}") from e
        results.append((anim, slots, placed))
    for anim, slots, placed in results:
        written = set()
        for slot, frame in zip(slots, placed):
            written.add(out_name(prefix, anim, slot))
            frame.save(OUT / f"{out_name(prefix, anim, slot)}.png")
        if anim == "divening":
            remove_stale(prefix, "dive", written)
            remove_stale(prefix, "ninja", written)
        else:
            remove_stale(prefix, anim, written)
        print(f"{name}: {anim} x{len(placed)} -> {placed[0].size} (scale {scale:.3f})")

if __name__ == "__main__":
    main()

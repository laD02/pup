#!/usr/bin/env python3
"""Cut single props (machine parts, arena objects, UI pieces) out of ChatGPT sheets on a white background.

Unlike the hero sheets (Tools/slice_sheets.py), a prop sheet has no fixed grid: every separate drawing is found as
one connected blob of non-background pixels, and the blobs are named in reading order (top row first, left to right;
two blobs are in the same row when their vertical centres are within ROW_TOLERANCE of the sheet height).
The white that touches the border becomes transparent, as in slice_sheets.py, so white snow inside an outline stays.

Usage: python3 -I Tools/cut_props.py   (then Puffer Launch -> 2. Build Project)
"""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from slice_sheets import remove_background  # noqa: E402  (same background rule as the hero sheets)

ROOT = Path(__file__).resolve().parent.parent
REF = ROOT / "Assets/Art/Reference"
OUT = ROOT / "Assets/Resources/Art"

MIN_AREA = 900          # px; smaller blobs are specks or stray marks
ROW_TOLERANCE = 0.12    # fraction of the sheet height
PAD = 4

# sheet -> names in reading order (None = drop that blob)
SHEETS: dict[str, list[str | None]] = {
    "catapult_parts.png": ["catapult_arm", "catapult_base", "snowball"],
    "arena_props.png": [
        # names the game already loads; the heavy "stone" block is drawn as a wooden crate, the wedge is unused
        "block_ice", None, "block_snow", "block_stone",
        "crate_tnt", "bumper", "button_up", "button_down",
        "fish", "cage", "cage_open", "hud_badge",
    ],
    # UI kit: panel, ribbon, 3 pill buttons (green = primary), 6 round icon buttons, stars, bear face, tap hint, medal
    "ui_kit.png": [
        "ui_panel", "ribbon",
        "ui_button_gold", "ui_button_warm", "ui_button",
        "icon_sound", "icon_music", "icon_help", "icon_pause", "icon_play", "icon_retry",
        "star_full", "star_empty", None, "hud_bear", "glyph_tap", "badge_perfect",
    ],
    # map + HUD kit (report §15): hero portrait, "?" stone node, cleared star node, "you are here" glow pad,
    # bear portrait (replaces the ui_kit one, same ring as the hero), plain card for the mission panel
    "map_ui_kit.png": ["hud_pup", "node_stone", "node_star", "node_glow", "hud_bear", "ui_card"],
    # hat knocked off a pup, ice wedge (rises to the right), dent left in rock (report §16.2, §17.1)
    "arena_extras.png": ["pup_hat", "block_wedge", "dent"],
}


# Enclosed background (holes, gaps between beams) is not connected to the border, so it is cleared from seed points
# given in the cut part's own pixels (measure on the output PNG).
HOLES: dict[str, list[tuple[int, int]]] = {
    "catapult_base": [(400, 200), (392, 75)],
    "catapult_arm": [(48, 100)],
}


def clear_holes(part: Image.Image, seeds: list[tuple[int, int]]) -> Image.Image:
    """Make the light region around each seed transparent (flood fill through near-white pixels)."""
    x = np.asarray(part).copy()
    light = (x[..., :3] > WHITE_HOLE).all(axis=2)
    for sx, sy in seeds:
        if not (0 <= sy < light.shape[0] and 0 <= sx < light.shape[1]):
            raise ValueError(f"hole seed {sx},{sy} is outside the {light.shape[1]}x{light.shape[0]} part (re-measure HOLES)")
        if not light[sy, sx]:
            print(f"  hole seed {sx},{sy} is not on a light pixel, skipped")
            continue
        reach = np.zeros_like(light)
        reach[sy, sx] = True
        while True:
            grown = reach.copy()
            grown[1:, :] |= reach[:-1, :]
            grown[:-1, :] |= reach[1:, :]
            grown[:, 1:] |= reach[:, :-1]
            grown[:, :-1] |= reach[:, 1:]
            grown &= light
            if (grown == reach).all():
                break
            reach = grown
        x[reach, 3] = 0
    return Image.fromarray(x, "RGBA")


WHITE_HOLE = 225
# Parts with no white of their own (bars, rings): every near-white pixel is background seen through them.
CLEAR_LIGHT = {"cage", "cage_open", "hud_badge"}


def blobs(alpha: np.ndarray) -> list[tuple[int, int, int, int]]:
    """Bounding boxes (x0, y0, x1, y1) of connected opaque regions, found on a 4x downsampled mask."""
    step = 4
    small = alpha[::step, ::step] > 0
    h, w = small.shape
    seen = np.zeros_like(small)
    boxes = []
    for y in range(h):
        for x in range(w):
            if not small[y, x] or seen[y, x]:
                continue
            stack, x0, y0, x1, y1, n = [(y, x)], x, y, x, y, 0
            seen[y, x] = True
            while stack:
                cy, cx = stack.pop()
                n += 1
                x0, y0, x1, y1 = min(x0, cx), min(y0, cy), max(x1, cx), max(y1, cy)
                for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                    if 0 <= ny < h and 0 <= nx < w and small[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        stack.append((ny, nx))
            if n * step * step >= MIN_AREA:
                # Samples are every `step` px: the true left/top edge can lie up to step-1 px before the first sample
                # (cut() trims the box back to the real alpha edge afterwards).
                boxes.append((max(0, x0 * step - step + 1), max(0, y0 * step - step + 1), min((x1 + 1) * step, alpha.shape[1]), min((y1 + 1) * step, alpha.shape[0])))
    return boxes


def reading_order(boxes: list[tuple[int, int, int, int]], height: int) -> list[tuple[int, int, int, int]]:
    rows: list[list[tuple[int, int, int, int]]] = []
    for b in sorted(boxes, key=lambda b: (b[1] + b[3]) / 2):
        cy = (b[1] + b[3]) / 2
        if rows and abs(cy - np.mean([(r[1] + r[3]) / 2 for r in rows[-1]])) < ROW_TOLERANCE * height:
            rows[-1].append(b)
        else:
            rows.append([b])
    return [b for row in rows for b in sorted(row, key=lambda b: b[0])]


def cut(sheet: str, names: list[str | None]) -> None:
    """Cuts one sheet; ValueError when its parts do not match SHEETS (nothing of that sheet is written)."""
    path = REF / sheet
    if not path.exists():
        print(f"{sheet}: not found, skipped")
        return
    rgb = np.asarray(Image.open(path).convert("RGB"))
    alpha = remove_background(rgb)
    rgba = Image.fromarray(np.dstack([rgb, alpha]), "RGBA")
    boxes = reading_order(blobs(alpha), rgb.shape[0])
    if len(boxes) != len(names):
        raise ValueError(f"found {len(boxes)} parts, expected {len(names)}: {boxes}")
    parts = []
    for name, box in zip(names, boxes):   # build every part first, so a bad seed never leaves a half-written sheet
        if name is None:
            continue
        part = rgba.crop(box)
        part = part.crop(part.getchannel("A").getbbox())
        canvas = Image.new("RGBA", (part.width + PAD * 2, part.height + PAD * 2), (0, 0, 0, 0))
        canvas.alpha_composite(part, (PAD, PAD))
        if name in HOLES:
            canvas = clear_holes(canvas, HOLES[name])
        if name in CLEAR_LIGHT:
            x = np.asarray(canvas).copy()
            x[(x[..., :3] > 238).all(axis=2), 3] = 0
            canvas = Image.fromarray(x, "RGBA")
        parts.append((name, canvas))
    OUT.mkdir(parents=True, exist_ok=True)
    for name, canvas in parts:
        canvas.save(OUT / f"{name}.png")
        print(f"{sheet}: {name} {canvas.size}")


def main() -> None:
    failed = []
    for sheet, names in SHEETS.items():
        try:
            cut(sheet, names)
        except ValueError as e:
            failed.append(f"{sheet}: {e}")
            print(f"{sheet}: FAILED, {e}")
    if failed:
        sys.exit("some sheets were not cut:\n  " + "\n  ".join(failed))


if __name__ == "__main__":
    main()

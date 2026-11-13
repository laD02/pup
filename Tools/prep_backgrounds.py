#!/usr/bin/env python3
"""Turn ChatGPT background paintings into game backgrounds.

Each painting in Assets/Art/Reference/ is 1536x1024. The pit is a fixed screen and phones are wider than 3:2 (up to
20:9), so the pit painting gets side pads from a second ChatGPT painting (pit_side_walls.png: left half plain wall,
right half sky + ledge + wall), faded into its edges. Repeating the painting's own edge strip was tried and looked
like woven cloth; mirroring turned cobbles into hearts.

Run: python3 -I Tools/prep_backgrounds.py   (then Puffer Launch -> 2. Build Project)
"""
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
REF = ROOT / "Assets" / "Art" / "Reference"
OUT = ROOT / "Assets" / "Resources" / "Art"

# output sprite -> painting it comes from (Tools/make_art.py leaves these alone once the painting exists)
PAINTINGS = {"bg_pit": "pit_bg_wide3.png", "bg_arena_sky": "arena_sky.png",
             "map_bg": "map_island.png", "bg_menu": "menu_bay.png",
             **{f"story_{i}": f"story_{i}.png" for i in range(1, 6)},
             "rock_cobble": "pit_side_walls_box.png", "rock_top": "pit_side_walls_box.png",
             "rock_ramp": "pit_side_walls_box.png"}


def build_pit() -> None:
    """bg_pit.png is ONE painting (pit_bg_wide3.png, 16:9): rock on every side is part of the picture, wide enough
    for 21:9 at the scale LaunchPhase uses. Nothing is pasted or padded any more (user 13/11: the composed pit
    "nhìn vẫn lộ là bị ghép")."""
    copy_plain("bg_pit", "pit_bg_wide3.png")


def copy_plain(name: str, source: str) -> None:
    """Paintings that are scaled to cover the screen (the arena sky) are used as they are."""
    path = REF / source
    if not path.exists():
        print(f"skip {name}: {source} not in Assets/Art/Reference yet")
        return
    Image.open(path).convert("RGB").save(OUT / f"{name}.png", optimize=True)
    print(f"{name}.png")


RIM_SIZE = (640, 96)  # px of the seamless snow rim strip


def draw_snow(out: Path) -> None:
    """rock_top.png: the painted snow cap of the launch pit (right half of pit_side_walls_box.png), cut out column by
    column between its navy top outline and its navy scalloped underside, made to tile sideways. The arena's rims are
    then the very same snow as the pit's (user 13/11: a drawn vector rim did not match the painted pit).
    snow_knob.png: a round joint in the cap's own colours for the ends of slope rims."""
    wall = np.asarray(Image.open(REF / "pit_side_walls_box.png").convert("RGB")).astype(int)
    x0, x1, y0, depth = 800, 1500, 330, 110
    band = wall[y0:y0 + depth, x0:x1]
    navy = (band[..., 0] < 75) & (band[..., 2] > 105)
    alpha = np.zeros(band.shape[:2], np.uint8)
    for x in range(band.shape[1]):
        rows = np.flatnonzero(navy[:, x])
        if rows.size == 0:
            continue
        top = rows[0]
        under = rows[rows > top + 30]                      # the scalloped underside's outline
        bottom = (under[0] + 7) if under.size else top + 60
        alpha[top:min(bottom, top + 78, depth), x] = 255   # never deeper than the deepest scallop (stray navy in the rock)
    rgba = np.dstack([band.astype(np.uint8), alpha])
    used = np.flatnonzero(alpha.any(axis=1))
    if used.size == 0:
        raise SystemExit(f"rock_top: no navy cap outline found in pit_side_walls_box.png at x {x0}-{x1}, y {y0}-{y0 + depth}")
    rgba = rgba[used[0]:used[-1] + 1].astype(float)
    overlap = 100                                           # wrap sideways: fade the far end into the start
    t = np.linspace(0, 1, overlap)[None, :, None]
    body = rgba[:, :-overlap].copy()
    body[:, :overlap] = rgba[:, -overlap:] * (1 - t) + body[:, :overlap] * t
    Image.fromarray(body.round().astype(np.uint8), "RGBA").resize(RIM_SIZE, Image.LANCZOS).save(out / "rock_top.png", optimize=True)
    print(f"rock_top.png from the pit cap: {body.shape[1]}x{body.shape[0]} px")
    n, line = 192, 12
    knob = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(knob)
    d.ellipse([0, 0, n - 1, n - 1], fill=(14, 62, 160, 255))                       # the cap's outline blue
    d.ellipse([line, line, n - 1 - line, n - 1 - line], fill=(196, 224, 252, 255))  # its shaded snow
    d.ellipse([line, line, n - 1 - line, n - 1 - line - 34], fill=(246, 250, 254, 255))
    knob.resize((96, 96), Image.LANCZOS).save(out / "snow_knob.png", optimize=True)


def build_terrain() -> None:
    """Arena rock: cobble texture and snow rim from the pit's own painting (pit_side_walls_box.png), plus a ramp mask."""
    path = REF / "pit_side_walls_box.png"
    if not path.exists():
        print("skip rock_*: pit_side_walls_box.png not in Assets/Art/Reference yet")
        return
    # Cobble texture: the pit's own stone (left half of pit_side_walls_box.png is a plain cobble wall), so the arena's
    # rock matches the launch pit (user 13/11: "pha 2 đất đá chưa nhất quán"). Square crop, wrap both ways.
    wall = Image.open(path).convert("RGB")
    # No blending at all: one unbroken piece of the painted wall (768x1024 px = 8.25 x 11 world units at the pit's
    # scale). AttackPhase.Cobble maps it in world space and MIRRORS it sideways, so it never shows a seam or the
    # doubled, smeared stones that cross-fading a small tile produced (user 13/11: "nhìn vẫn lộ là bị ghép").
    tex = wall.crop((0, 0, 760, 1024))
    tex.save(OUT / "rock_cobble.png", optimize=True)
    # Snow rim and its round joint, drawn here in the pit painting's snow colours (white, pale blue shade, navy
    # outline). The strip cut from arena_terrain.png carried a band of that sheet's brown rock along its top, which
    # showed as a brown edge outside the snow on every slope (user 13/11).
    draw_snow(OUT)
    # Ramp: only a white triangle (right angle bottom-right, rising to the right). AttackPhase uses it as a sprite mask
    # over the same tiled cobble as the blocks and lays the same snow rim along the slope, so ramps and blocks are one stone.
    n = 256
    tri = Image.new("RGBA", (n, n), (255, 255, 255, 0))
    ImageDraw.Draw(tri).polygon([(0, n), (n, n), (n, 0)], fill=(255, 255, 255, 255))
    tri.save(OUT / "rock_ramp.png", optimize=True)
    print("rock_cobble.png rock_top.png rock_ramp.png")


def crop_16_9(name: str, source: str) -> None:
    """Centre crop to 16:9 (the map is laid out on a 1920x1080 canvas and drawn envelope-fitted)."""
    path = REF / source
    if not path.exists():
        print(f"skip {name}: {source} not in Assets/Art/Reference yet")
        return
    img = Image.open(path).convert("RGB")
    h = min(img.height, round(img.width * 9 / 16))
    w = min(img.width, round(h * 16 / 9))
    x, y = (img.width - w) // 2, (img.height - h) // 2
    img.crop((x, y, x + w, y + h)).save(OUT / f"{name}.png", optimize=True)
    print(f"{name}.png  {w}x{h}")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    build_pit()
    crop_16_9("map_bg", PAINTINGS["map_bg"])
    crop_16_9("bg_menu", PAINTINGS["bg_menu"])
    for i in range(1, 6):
        crop_16_9(f"story_{i}", PAINTINGS[f"story_{i}"])
    build_terrain()
    copy_plain("bg_arena_sky", PAINTINGS["bg_arena_sky"])


if __name__ == "__main__":
    main()

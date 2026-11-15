#!/usr/bin/env python3
"""Generate the game's original vector art (SVG) and export PNG sprites.

Setting: polar ice sea. Style: soft shading (gradients, highlights, soft shadows) with a thin dark outline.
Run: python3 Tools/make_art.py   (needs rsvg-convert: brew install librsvg)
SVG sources -> Assets/Art/Source, PNGs -> Assets/Resources/Art.
Replacing a PNG with the same file name updates the game without code changes.
"""
import math
import pathlib
import random
import shutil
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
SVG_DIR = ROOT / "Assets/Art/Source"
PNG_DIR = ROOT / "Assets/Resources/Art"

INK = "#16304f"
OW = 5  # outline width

# palette
SNOW_HI, SNOW, SNOW_SH = "#ffffff", "#eef7fd", "#bcd9ee"
ICE_HI, ICE, ICE_SH, ICE_DEEP = "#e9fbff", "#9fe0fb", "#4fa8dd", "#2a6fb0"
CRAB_HI, CRAB, CRAB_SH = "#ffb07a", "#f26b3a", "#b8401c"
WALRUS_HI, WALRUS, WALRUS_SH = "#c99877", "#9c6a4f", "#6b4232"
GOLD_HI, GOLD, GOLD_SH = "#fff1a8", "#ffc93c", "#e08e0b"
TEAL_HI, TEAL, TEAL_SH = "#7ff3e3", "#22c3b8", "#127f86"
WARM_HI, WARM, WARM_SH = "#ffb38a", "#ff6f4f", "#c93e2c"
PINK = "#ff9fc0"


def svg(w, h, body, defs=""):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">'
            f'<defs><filter id="blur" x="-50%" y="-50%" width="200%" height="200%">'
            f'<feGaussianBlur stdDeviation="6"/></filter>'
            f'<filter id="soft" x="-50%" y="-50%" width="200%" height="200%">'
            f'<feGaussianBlur stdDeviation="2"/></filter>{defs}</defs>{body}</svg>')


def lg(gid, stops, x1=0, y1=0, x2=0, y2=1):
    s = "".join(f'<stop offset="{o}" stop-color="{c}"{"" if a is None else f" stop-opacity={chr(34)}{a}{chr(34)}"}/>'
                for o, c, a in [(st + (None,))[:3] for st in stops])
    return f'<linearGradient id="{gid}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{s}</linearGradient>'


def rg(gid, stops, cx=0.4, cy=0.35, r=0.75):
    s = "".join(f'<stop offset="{o}" stop-color="{c}"/>' for o, c in stops)
    return f'<radialGradient id="{gid}" cx="{cx}" cy="{cy}" r="{r}">{s}</radialGradient>'


def shadow(cx, cy, rx, ry, op=0.28):
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="#0a2240" opacity="{op}" filter="url(#blur)"/>'


def gloss(cx, cy, rx, ry, op=0.7, rot=-20):
    return (f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="#fff" opacity="{op}" '
            f'transform="rotate({rot} {cx} {cy})" filter="url(#soft)"/>')


def eye(cx, cy, r, look=(2, 1)):
    return (f'<ellipse cx="{cx}" cy="{cy}" rx="{r}" ry="{r*1.15}" fill="#0f1b2d"/>'
            f'<circle cx="{cx+look[0]-r*0.3}" cy="{cy+look[1]-r*0.45}" r="{r*0.38}" fill="#fff"/>'
            f'<circle cx="{cx+look[0]+r*0.35}" cy="{cy+look[1]+r*0.3}" r="{r*0.16}" fill="#fff" opacity="0.9"/>')


# ---------------------------------------------------------------- hero: harp seal pup

FUR = rg("fur", [(0, SNOW_HI), (0.55, SNOW), (1, "#b9d3e8")], 0.42, 0.28, 0.85)
GEAR_DEFS = (lg("knit", [(0, "#ff6b5b"), (1, "#c7352b")]) + lg("scarf", [(0, "#3fd6c8"), (1, "#16928c")])
             + rg("lens", [(0, "#e9fbff"), (0.6, "#7fc8f2"), (1, "#2f6fa8")], 0.35, 0.3, 0.8)
             + rg("pom", [(0, "#ffffff"), (1, "#dbe8f3")], 0.4, 0.35, 0.8))


def fur_strokes(cx, cy, rx, ry, n, seed):
    """Short soft strokes following the body curve: reads as fluffy fur."""
    rnd = random.Random(seed)
    out = ""
    for _ in range(n):
        a = rnd.uniform(math.pi * 0.95, math.pi * 2.05)
        r = rnd.uniform(0.55, 0.92)
        x, y = cx + math.cos(a) * rx * r, cy + math.sin(a) * ry * r
        dx = -math.sin(a) * 8
        dy = math.cos(a) * 8 * (ry / rx)
        out += (f'<path d="M{x:.1f},{y:.1f} q{dx*0.5:.1f},{dy*0.5-3:.1f} {dx:.1f},{dy:.1f}" fill="none" stroke="#c6dbec" '
                f'stroke-width="2.5" stroke-linecap="round" opacity="0.9"/>')
    return out


def big_eye(cx, cy, r, look=(1.5, 1)):
    return (f'<ellipse cx="{cx}" cy="{cy}" rx="{r}" ry="{r*1.18}" fill="#0d1726"/>'
            f'<ellipse cx="{cx+look[0]}" cy="{cy+look[1]+r*0.35}" rx="{r*0.62}" ry="{r*0.5}" fill="#27406b" opacity="0.9"/>'
            f'<circle cx="{cx+look[0]-r*0.32}" cy="{cy+look[1]-r*0.48}" r="{r*0.4}" fill="#fff"/>'
            f'<circle cx="{cx+look[0]+r*0.38}" cy="{cy+look[1]+r*0.28}" r="{r*0.17}" fill="#fff" opacity="0.95"/>')


def pup_face(cx, cy, s=1.0, dizzy=False, blink=False):
    if dizzy:
        eyes = "".join(
            f'<path d="M{x},{cy} m-{9*s},0 a{9*s},{9*s} 0 1 1 {18*s},0 a{6*s},{6*s} 0 1 1 -{12*s},0 '
            f'a{3*s},{3*s} 0 1 1 {6*s},0" fill="none" stroke="#0d1726" stroke-width="{4*s}" stroke-linecap="round"/>'
            for x in (cx - 21 * s, cx + 19 * s))
        mouth = (f'<path d="M{cx-9*s},{cy+31*s} q8,-7 16,0 q8,7 16,0" fill="none" stroke="#0d1726" '
                 f'stroke-width="{3.5*s}" stroke-linecap="round"/>')
    elif blink:
        eyes = "".join(f'<path d="M{x-12*s},{cy+2*s} q{12*s},{9*s} {24*s},0" fill="none" stroke="#0d1726" stroke-width="{4.5*s}" '
                       f'stroke-linecap="round"/>' for x in (cx - 21 * s, cx + 19 * s))
        mouth = (f'<path d="M{cx-8*s},{cy+30*s} q8,8 16,0" fill="none" stroke="#0d1726" stroke-width="{3.5*s}" stroke-linecap="round"/>')
    else:
        eyes = (big_eye(cx - 21 * s, cy, 13 * s) + big_eye(cx + 19 * s, cy + 1 * s, 13 * s)
                + f'<path d="M{cx-35*s},{cy-17*s} q{12*s},-{7*s} {24*s},-{2*s} M{cx+8*s},{cy-18*s} q{12*s},-{5*s} {24*s},{2*s}" '
                  f'fill="none" stroke="#7d93ab" stroke-width="{2.5*s}" stroke-linecap="round"/>')
        mouth = (f'<path d="M{cx-9*s},{cy+29*s} q9,11 18,0 Z" fill="#c24a5a" stroke="#0d1726" stroke-width="{3*s}" stroke-linejoin="round"/>'
                 f'<ellipse cx="{cx}" cy="{cy+34*s}" rx="{4.5*s}" ry="{2.5*s}" fill="#ff8fa0"/>')
    muzzle = (f'<ellipse cx="{cx-9*s}" cy="{cy+23*s}" rx="{11*s}" ry="{8*s}" fill="#ffffff" stroke="#d3e3f0" stroke-width="1.5"/>'
              f'<ellipse cx="{cx+9*s}" cy="{cy+23*s}" rx="{11*s}" ry="{8*s}" fill="#ffffff" stroke="#d3e3f0" stroke-width="1.5"/>')
    nose = (f'<path d="M{cx-8*s},{cy+14*s} q{8*s},-{5*s} {16*s},0 q-{2*s},{8*s} -{8*s},{9*s} q-{6*s},-{1*s} -{8*s},-{9*s} Z" fill="#2b3240"/>'
            f'<ellipse cx="{cx-2*s}" cy="{cy+15*s}" rx="{3.5*s}" ry="{1.6*s}" fill="#fff" opacity="0.7"/>')
    whiskers = "".join(f'<circle cx="{cx+dx*s}" cy="{cy+dy*s}" r="{1.6*s}" fill="#8aa0b8"/>'
                       for dx, dy in ((-16, 22), (-12, 27), (-19, 27), (16, 22), (12, 27), (19, 27)))
    whisk_lines = (f'<path d="M{cx-22*s},{cy+24*s} l-{18*s},-{4*s} M{cx-22*s},{cy+28*s} l-{18*s},{3*s} '
                   f'M{cx+22*s},{cy+24*s} l{18*s},-{4*s} M{cx+22*s},{cy+28*s} l{18*s},{3*s}" stroke="#a9bdd1" stroke-width="{1.8*s}" '
                   f'stroke-linecap="round"/>')
    cheeks = (f'<ellipse cx="{cx-36*s}" cy="{cy+17*s}" rx="{9*s}" ry="{6*s}" fill="{PINK}" opacity="0.6"/>'
              f'<ellipse cx="{cx+34*s}" cy="{cy+17*s}" rx="{9*s}" ry="{6*s}" fill="{PINK}" opacity="0.6"/>')
    return cheeks + whisk_lines + muzzle + eyes + nose + whiskers + mouth


def beanie(cx, cy, w, tilt=-8):
    """Red knit beanie with ribbed brim, white pom-pom, and goggles pushed up on it."""
    h = w * 0.62
    rib = "".join(f'<path d="M{cx - w/2 + 8 + i*(w-16)/9:.1f},{cy+2} v{h*0.26:.1f}" stroke="#a82a22" stroke-width="2.5" opacity="0.7"/>'
                  for i in range(10))
    knit = "".join(f'<path d="M{cx-w*0.38:.1f},{cy-h*k:.1f} q{w*0.38:.1f},-{h*0.12:.1f} {w*0.76:.1f},0" fill="none" stroke="#ff8f80" '
                   f'stroke-width="2.5" opacity="0.6"/>' for k in (0.25, 0.48, 0.7))
    dome = (f'<path d="M{cx-w/2},{cy} C{cx-w/2},{cy-h*1.25} {cx+w/2},{cy-h*1.25} {cx+w/2},{cy} Z" fill="url(#knit)" '
            f'stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>')
    brim = (f'<rect x="{cx-w/2-4}" y="{cy-4}" width="{w+8}" height="{h*0.32:.1f}" rx="{h*0.14:.1f}" fill="#e2463a" stroke="{INK}" stroke-width="4"/>'
            + rib)
    pom = (f'<circle cx="{cx+6}" cy="{cy-h*1.02:.1f}" r="{w*0.13:.1f}" fill="url(#pom)" stroke="#9cb4ca" stroke-width="2.5"/>'
           + "".join(f'<path d="M{cx+6+math.cos(a)*w*0.09:.1f},{cy-h*1.02+math.sin(a)*w*0.09:.1f} l{math.cos(a)*5:.1f},{math.sin(a)*5:.1f}" '
                     f'stroke="#c9d9e8" stroke-width="2" stroke-linecap="round"/>' for a in [i * math.pi / 4 for i in range(8)]))
    gy = cy - h * 0.42
    goggles = (f'<path d="M{cx-w/2+2},{gy+2} C{cx-w*0.2},{gy-6} {cx+w*0.2},{gy-6} {cx+w/2-2},{gy+2}" fill="none" stroke="#5a3a1e" stroke-width="7"/>'
               + "".join(f'<circle cx="{x:.1f}" cy="{gy:.1f}" r="{w*0.14:.1f}" fill="url(#lens)" stroke="#b07a3a" stroke-width="5"/>'
                         f'<path d="M{x-w*0.07:.1f},{gy-w*0.05:.1f} q{w*0.04:.1f},-{w*0.04:.1f} {w*0.08:.1f},-{w*0.02:.1f}" stroke="#fff" '
                         f'stroke-width="3" stroke-linecap="round" fill="none"/>' for x in (cx - w * 0.17, cx + w * 0.17))
               + f'<rect x="{cx-5}" y="{gy-4:.1f}" width="10" height="8" rx="3" fill="#b07a3a"/>')
    return f'<g transform="rotate({tilt} {cx} {cy})">{dome}{knit}{brim}{goggles}{pom}</g>'


def scarf(x, y, flap=0):
    tail = (f'<path d="M{x},{y} C{x-30},{y+6+flap} {x-58},{y-10+flap} {x-78},{y+6+flap} L{x-70},{y+26+flap} '
            f'C{x-52},{y+12+flap} {x-28},{y+26} {x+2},{y+20} Z" fill="url(#scarf)" stroke="{INK}" stroke-width="3.5" stroke-linejoin="round"/>'
            f'<path d="M{x-76},{y+8+flap} l-8,4 M{x-74},{y+16+flap} l-9,4 M{x-72},{y+24+flap} l-8,4" stroke="#16928c" stroke-width="3" stroke-linecap="round"/>')
    return tail


def pup(dizzy=False, blink=False):
    tail = (f'<path d="M58,166 C32,154 18,132 22,118 C38,126 50,138 64,150 Z" fill="url(#fur)" stroke="{INK}" '
            f'stroke-width="{OW}" stroke-linejoin="round"/>'
            f'<path d="M58,172 C34,182 22,202 26,214 C42,204 54,192 66,180 Z" fill="url(#fur)" stroke="{INK}" '
            f'stroke-width="{OW}" stroke-linejoin="round"/>'
            '<path d="M30,128 l14,10 M30,206 l14,-12" stroke="#a9c3da" stroke-width="2.5" stroke-linecap="round"/>')
    body = (f'<path d="M60,170 C58,120 104,96 152,96 C206,96 234,128 234,162 C234,198 198,214 152,214 '
            f'C100,214 62,206 60,170 Z" fill="url(#fur)" stroke="{INK}" stroke-width="{OW}"/>'
            '<path d="M80,196 C120,212 190,212 222,188" fill="none" stroke="#b9d3e8" stroke-width="10" stroke-linecap="round" opacity="0.7"/>'
            + fur_strokes(146, 154, 80, 52, 26, 4)
            + gloss(132, 114, 44, 12, 0.9))
    flipper = (f'<path d="M136,196 C130,222 146,236 170,234 C168,216 160,204 150,196 Z" fill="#dbe9f5" '
               f'stroke="{INK}" stroke-width="{OW-1}" stroke-linejoin="round"/>'
               '<path d="M150,224 l4,-10 M158,228 l3,-11 M164,231 l2,-10" stroke="#8aa0b8" stroke-width="2" stroke-linecap="round"/>')
    face = pup_face(186, 140, 1.0, dizzy, blink)
    hat = beanie(178, 108, 96, -10)
    return svg(256, 256, shadow(146, 226, 82, 10) + tail + body + scarf(146, 182, 4) + flipper + face + hat, FUR + GEAR_DEFS)


def pup_ball(spiky=False):
    c, r = 128, 80
    spikes = ""
    if spiky:
        for i in range(14):
            a = 2 * math.pi * i / 14 - math.pi / 2
            x1, y1 = c + math.cos(a - 0.16) * (r - 4), c + 8 + math.sin(a - 0.16) * (r - 4)
            x2, y2 = c + math.cos(a + 0.16) * (r - 4), c + 8 + math.sin(a + 0.16) * (r - 4)
            xt, yt = c + math.cos(a) * (r + 32), c + 8 + math.sin(a) * (r + 32)
            spikes += (f'<polygon points="{x1:.1f},{y1:.1f} {xt:.1f},{yt:.1f} {x2:.1f},{y2:.1f}" fill="url(#ice)" '
                       f'stroke="{INK}" stroke-width="3.5" stroke-linejoin="round"/>'
                       f'<line x1="{(x1+xt)/2:.1f}" y1="{(y1+yt)/2:.1f}" x2="{xt:.1f}" y2="{yt:.1f}" stroke="#fff" '
                       f'stroke-width="2.5" opacity="0.8"/>')
    cy = c + 8
    ball = (f'<circle cx="{c}" cy="{cy}" r="{r}" fill="url(#fur)" stroke="{INK}" stroke-width="{OW}"/>'
            + fur_strokes(c, cy, r * 0.95, r * 0.95, 22, 9)
            + f'<path d="M{c-r+10},{cy+30} C{c-r-20},{cy+30} {c-r-24},{cy+6} {c-r-14},{cy-4} C{c-r},{cy+6} {c-r+8},{cy+16} {c-r+14},{cy+20} Z" '
              f'fill="url(#fur)" stroke="{INK}" stroke-width="{OW-1}" stroke-linejoin="round"/>'
            + f'<path d="M{c-44},{cy+50} q18,22 46,10" fill="none" stroke="#b9d3e8" stroke-width="6" stroke-linecap="round"/>'
            + gloss(c - 28, cy - 44, 32, 12, 0.9))
    defs = FUR + GEAR_DEFS + lg("ice", [(0, ICE_HI), (1, ICE_SH)], 0, 0, 1, 1)
    body = ball + scarf(c - 30, cy + 44, 10) + pup_face(c + 16, cy - 4, 0.92) + beanie(c + 14, cy - 40, 86, 8)
    return svg(256, 256, shadow(c, cy + r + 8, r * 0.8, 9) + spikes + body, defs)


# ---------------------------------------------------------------- hero rig: two-legged pup (parts)
# Pivot pixels below are mirrored in Assets/Scripts/Core/PupRig.cs — keep them in sync.

def rig_head(mood="normal"):
    """Head 256×300: neck pivot at (128,276). Beanie + goggles on top."""
    cx, cy = 128, 180
    head = (f'<ellipse cx="{cx}" cy="{cy+4}" rx="96" ry="88" fill="url(#fur)" stroke="{INK}" stroke-width="{OW}"/>'
            + fur_strokes(cx, cy + 10, 80, 70, 14, 21)
            + gloss(cx - 40, cy - 40, 30, 11, 0.85))
    s_ = 1.15
    if mood == "blink":
        face = pup_face(cx + 6, cy + 6, s_, blink=True)
    elif mood == "dizzy":
        face = pup_face(cx + 6, cy + 6, s_, dizzy=True)
    else:
        face = pup_face(cx + 6, cy + 6, s_)
        if mood == "happy":
            face = face.replace('q9,11 18,0 Z', 'q9,18 18,0 Z')
        elif mood == "scared":
            ex = (f'<ellipse cx="{cx+6}" cy="{cy+44}" rx="9" ry="11" fill="#c24a5a" stroke="#0d1726" stroke-width="3"/>'
                  f'<path d="M{cx-34},{cy-26} l18,6 M{cx+46},{cy-26} l-18,6" stroke="#0d1726" stroke-width="4" stroke-linecap="round"/>')
            face = face + ex
    hat = beanie(cx + 4, cy - 52, 150, -6)
    return svg(256, 300, head + face + hat, FUR + GEAR_DEFS)


def rig_torso():
    """Torso 200×220 (centre = rig origin). Neck (100,28) · shoulders (42,78)/(158,78) · hips (72,196)/(128,196)."""
    body = (f'<path d="M100,24 C150,24 176,70 174,128 C172,186 144,214 100,214 C56,214 28,186 26,128 C24,70 50,24 100,24 Z" '
            f'fill="url(#fur)" stroke="{INK}" stroke-width="{OW}"/>'
            f'<ellipse cx="100" cy="146" rx="48" ry="54" fill="#ffffff" opacity="0.9"/>'
            + fur_strokes(100, 120, 60, 70, 12, 31)
            + gloss(72, 70, 20, 8, 0.7))
    scarf_wrap = ('<path d="M44,44 C70,62 130,62 156,44 L160,64 C130,84 70,84 40,64 Z" fill="url(#scarf)" stroke="#16304f" stroke-width="4" '
                  'stroke-linejoin="round"/>'
                  '<path d="M56,58 l8,14 M80,64 l4,14 M104,66 l2,14 M128,62 l-2,14 M148,56 l-6,12" stroke="#16928c" stroke-width="3"/>'
                  '<circle cx="64" cy="66" r="12" fill="url(#scarf)" stroke="#16304f" stroke-width="4"/>')
    return svg(200, 220, body + scarf_wrap, FUR + GEAR_DEFS)


def rig_arm():
    """Flipper arm 80×130, shoulder pivot at (40,16); round flipper hand at the bottom."""
    return svg(80, 130,
               f'<path d="M24,14 C20,44 18,74 20,96 C22,124 58,126 60,100 C62,76 60,44 56,14 Z" fill="url(#fur)" stroke="{INK}" '
               f'stroke-width="{OW-1}" stroke-linejoin="round"/>'
               '<path d="M28,104 l4,-9 M38,110 l2,-11 M48,108 l0,-11" stroke="#8aa0b8" stroke-width="2.5" stroke-linecap="round"/>'
               '<path d="M30,28 C28,50 28,70 30,86" stroke="#ffffff" stroke-width="6" fill="none" stroke-linecap="round" opacity="0.8"/>', FUR)


def rig_leg():
    """Leg 100×110, hip pivot at (40,14); the rear flipper is the foot, pointing forward (+x)."""
    return svg(100, 110,
               f'<path d="M24,12 C22,40 26,62 30,74 L56,74 C58,56 58,34 56,12 Z" fill="url(#fur)" stroke="{INK}" stroke-width="{OW-1}" '
               'stroke-linejoin="round"/>'
               f'<path d="M22,72 C30,64 64,64 86,74 C98,80 94,98 80,98 C60,100 34,100 22,94 C14,88 14,78 22,72 Z" fill="#dbe9f5" '
               f'stroke="{INK}" stroke-width="{OW-1}" stroke-linejoin="round"/>'
               '<path d="M54,80 l18,6 M50,88 l22,4" stroke="#8aa0b8" stroke-width="2.5" stroke-linecap="round"/>', FUR)


def rig_scarf_tail():
    """Scarf tail 150×70, pivot at (12,22) (the knot)."""
    return svg(150, 70,
               '<path d="M12,14 C50,4 90,20 130,10 L140,34 C100,46 60,32 14,40 Z" fill="url(#scarf)" stroke="#16304f" stroke-width="4" '
               'stroke-linejoin="round"/>'
               '<path d="M132,14 l12,-4 M136,24 l12,0 M138,34 l12,4" stroke="#16928c" stroke-width="4" stroke-linecap="round"/>', GEAR_DEFS)


def pup_standing():
    """Composed standing pup (icons, UI): same parts as the rig, assembled once."""
    def part(svg_text, x, y, w, h, rot=0, px=0, py=0):
        inner = svg_text.split("</defs>", 1)[1].rsplit("</svg>", 1)[0]
        return f'<g transform="translate({x} {y}) rotate({rot} {px} {py})">{inner}</g>'
    torso = rig_torso(); head = rig_head(); arm = rig_arm(); leg = rig_leg(); tail = rig_scarf_tail()
    # canvas 300×520; torso top-left at (50,230)
    tx, ty = 50, 230
    body = (part(tail, tx + 40 - 12, ty + 40 - 22, 150, 70, 160, 12, 22)
            + part(arm, tx + 42 - 40, ty + 78 - 16, 80, 130, 14, 40, 16)
            + part(leg, tx + 72 - 40, ty + 196 - 14, 100, 110, 0, 40, 14)
            + part(leg, tx + 128 - 40, ty + 196 - 14, 100, 110, 0, 40, 14)
            + part(torso, tx, ty, 200, 220)
            + part(arm, tx + 158 - 40, ty + 78 - 16, 80, 130, -16, 40, 16)
            + part(head, tx + 100 - 128, ty + 28 - 276, 256, 300))
    return svg(300, 520, shadow(150, 512, 90, 8) + body, FUR + GEAR_DEFS)


# ---------------------------------------------------------------- hero: snowy owlet (one-piece body + wings + feet)
# Head and body are a single blob, so nothing ever separates. Wing and foot pivots sit inside the body outline.
# Pivot pixels are mirrored in Assets/Scripts/Core/PupRig.cs — keep them in sync.

OWL_DEFS = (rg("owlfur", [(0, "#ffffff"), (0.6, "#f3f8fc"), (1, "#c9d9e8")], 0.42, 0.3, 0.85)
            + rg("owleye", [(0, "#fff7b0"), (0.7, "#ffd23f"), (1, "#e8a20a")], 0.4, 0.35, 0.7)
            + GEAR_DEFS)


def owl_eye(cx, cy, r, mood):
    if mood == "blink":
        return f'<path d="M{cx-r},{cy} q{r},{r*0.7} {2*r},0" fill="none" stroke="#1b2a49" stroke-width="6" stroke-linecap="round"/>'
    if mood == "dizzy":
        return (f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="#fff" stroke="#1b2a49" stroke-width="4"/>'
                f'<path d="M{cx},{cy} m-{r*0.6},0 a{r*0.6},{r*0.6} 0 1 1 {r*1.2},0 a{r*0.4},{r*0.4} 0 1 1 -{r*0.8},0" '
                f'fill="none" stroke="#1b2a49" stroke-width="4" stroke-linecap="round"/>')
    pupil = r * (0.62 if mood != "scared" else 0.32)
    return (f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="url(#owleye)" stroke="#1b2a49" stroke-width="4"/>'
            f'<circle cx="{cx+2}" cy="{cy+2}" r="{pupil}" fill="#0d1726"/>'
            f'<circle cx="{cx-pupil*0.35}" cy="{cy-pupil*0.4}" r="{pupil*0.38}" fill="#fff"/>'
            f'<circle cx="{cx+pupil*0.45}" cy="{cy+pupil*0.4}" r="{pupil*0.16}" fill="#fff"/>')


def owl_body(mood="normal"):
    """One-piece owlet 280×300; body centre (140,160)."""
    blob = ('M140,40 C210,40 254,92 254,160 C254,236 206,282 140,282 C74,282 26,236 26,160 C26,92 70,40 140,40 Z')
    tufts = (f'<path d="M78,66 L64,26 L104,52 Z M202,66 L216,26 L176,52 Z" fill="#e8f0f7" stroke="{INK}" stroke-width="5" stroke-linejoin="round"/>')
    body = (f'<path d="{blob}" fill="url(#owlfur)" stroke="{INK}" stroke-width="{OW}"/>'
            '<ellipse cx="140" cy="214" rx="72" ry="54" fill="#ffffff"/>'
            + "".join(f'<path d="M{x},{y} q5,5 10,0" fill="none" stroke="#8fa6bf" stroke-width="3" stroke-linecap="round"/>'
                      for x, y in ((110, 196), (134, 210), (160, 198), (122, 230), (150, 236), (100, 220), (172, 222), (70, 150), (204, 152), (60, 190), (214, 188)))
            + gloss(96, 84, 30, 11, 0.85))
    disc = ('<path d="M66,138 C66,98 102,88 140,108 C178,88 214,98 214,138 C214,168 182,178 140,166 C98,178 66,168 66,138 Z" '
            'fill="#fbfdff" stroke="#d3e1ee" stroke-width="3"/>')
    eyes = owl_eye(106, 136, 26, mood) + owl_eye(174, 136, 26, mood)
    beak = ('<path d="M128,160 L140,182 L152,160 Z" fill="#f2a23a" stroke="#1b2a49" stroke-width="3.5" stroke-linejoin="round"/>')
    if mood == "happy":
        beak = ('<path d="M126,158 L140,190 L154,158 Z" fill="#f2a23a" stroke="#1b2a49" stroke-width="3.5" stroke-linejoin="round"/>'
                '<path d="M132,170 L140,186 L148,170 Z" fill="#c24a5a"/>')
    elif mood == "scared":
        beak = ('<ellipse cx="140" cy="176" rx="12" ry="15" fill="#c24a5a" stroke="#1b2a49" stroke-width="3.5"/>'
                '<path d="M128,160 L140,168 L152,160 Z" fill="#f2a23a" stroke="#1b2a49" stroke-width="3"/>')
    cheeks = f'<ellipse cx="80" cy="170" rx="14" ry="8" fill="{PINK}" opacity="0.6"/><ellipse cx="200" cy="170" rx="14" ry="8" fill="{PINK}" opacity="0.6"/>'
    goggles = ('<path d="M54,92 C90,70 190,70 226,92" fill="none" stroke="#5a3a1e" stroke-width="8"/>'
               + "".join(f'<circle cx="{x}" cy="78" r="20" fill="url(#lens)" stroke="#b07a3a" stroke-width="6"/>'
                         f'<path d="M{x-9},72 q5,-5 11,-3" stroke="#fff" stroke-width="3" fill="none" stroke-linecap="round"/>' for x in (112, 168))
               + '<rect x="134" y="74" width="12" height="9" rx="3" fill="#b07a3a"/>')
    scarf_ = ('<path d="M50,196 C90,220 190,220 230,196 L236,218 C190,244 90,244 44,218 Z" fill="url(#scarf)" stroke="#16304f" stroke-width="4" stroke-linejoin="round"/>'
              '<path d="M70,214 l6,14 M100,222 l3,14 M140,226 v14 M180,222 l-3,14 M210,214 l-6,14" stroke="#16928c" stroke-width="3"/>')
    return svg(280, 300, tufts + body + disc + cheeks + eyes + beak + goggles + scarf_, OWL_DEFS)


def owl_wing():
    """Wing 120×150, pivot at (60,22) (tucked inside the body side). Hangs down at rest."""
    return svg(120, 150,
               f'<path d="M40,18 C24,60 26,108 50,138 C64,152 84,140 88,118 C94,84 88,48 80,18 Z" fill="url(#owlfur)" stroke="{INK}" '
               f'stroke-width="{OW-1}" stroke-linejoin="round"/>'
               '<path d="M52,128 q8,-12 4,-30 M66,132 q8,-16 6,-40 M78,124 q6,-16 4,-36" fill="none" stroke="#8fa6bf" stroke-width="3" stroke-linecap="round"/>'
               '<path d="M50,30 C42,60 42,86 48,108" stroke="#fff" stroke-width="6" fill="none" stroke-linecap="round" opacity="0.8"/>', OWL_DEFS)


def owl_foot():
    """Foot 90×70, pivot at (40,12) (inside the body bottom): short leg and three orange toes."""
    return svg(90, 70,
               '<path d="M30,4 L30,34 M50,4 L50,34" stroke="#f2a23a" stroke-width="0"/>'
               f'<path d="M28,6 C26,24 28,34 34,40 L50,40 C54,30 54,18 52,6 Z" fill="#e8f0f7" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>'
               f'<path d="M30,40 C20,52 18,60 24,62 C30,62 34,54 38,46 C40,58 42,64 48,64 C54,64 52,54 50,46 C56,54 64,60 70,58 '
               f'C76,56 70,46 56,40 Z" fill="#f2a23a" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>', OWL_DEFS)


def owl_ball():
    """Curled-up owlet: wings wrapped around, eyes squeezed shut in determination."""
    c = 128
    body = (f'<circle cx="{c}" cy="{c+6}" r="96" fill="url(#owlfur)" stroke="{INK}" stroke-width="{OW}"/>'
            f'<path d="M48,90 L34,52 L74,76 Z M208,90 L222,52 L182,76 Z" fill="#e8f0f7" stroke="{INK}" stroke-width="5" stroke-linejoin="round"/>'
            f'<path d="M40,150 C70,120 120,150 128,200 C90,214 52,196 40,150 Z" fill="url(#owlfur)" stroke="{INK}" stroke-width="4"/>'
            f'<path d="M216,150 C186,120 136,150 128,200 C166,214 204,196 216,150 Z" fill="url(#owlfur)" stroke="{INK}" stroke-width="4"/>'
            '<path d="M84,112 q14,10 28,0 M144,112 q14,10 28,0" fill="none" stroke="#1b2a49" stroke-width="6" stroke-linecap="round"/>'
            '<path d="M118,126 L128,144 L138,126 Z" fill="#f2a23a" stroke="#1b2a49" stroke-width="3.5" stroke-linejoin="round"/>'
            '<path d="M60,86 C96,66 160,66 196,86" fill="none" stroke="#5a3a1e" stroke-width="7"/>'
            + "".join(f'<circle cx="{x}" cy="74" r="16" fill="url(#lens)" stroke="#b07a3a" stroke-width="5"/>' for x in (106, 150))
            + gloss(92, 70, 24, 9, 0.8))
    return svg(256, 256, shadow(c, 236, 80, 8) + body, OWL_DEFS)


def owl_spiky():
    inner = owl_ball().split("</defs>", 1)[1].rsplit("</svg>", 1)[0]
    c, r = 128, 100
    spikes = ""
    for i in range(14):
        a = 2 * math.pi * i / 14 - math.pi / 2
        x1, y1 = c + math.cos(a - 0.16) * (r - 6), c + 6 + math.sin(a - 0.16) * (r - 6)
        x2, y2 = c + math.cos(a + 0.16) * (r - 6), c + 6 + math.sin(a + 0.16) * (r - 6)
        xt, yt = c + math.cos(a) * (r + 26), c + 6 + math.sin(a) * (r + 26)
        spikes += (f'<polygon points="{x1:.1f},{y1:.1f} {xt:.1f},{yt:.1f} {x2:.1f},{y2:.1f}" fill="url(#ice)" stroke="{INK}" '
                   f'stroke-width="3.5" stroke-linejoin="round"/>')
    return svg(256, 256, f'<g transform="translate(18 18) scale(0.86)">{spikes}{inner}</g>', OWL_DEFS + lg("ice", [(0, ICE_HI), (1, ICE_SH)], 0, 0, 1, 1))


def owl_standing():
    def part(svg_text, x, y, rot=0, px=0, py=0):
        inner = svg_text.split("</defs>", 1)[1].rsplit("</svg>", 1)[0]
        return f'<g transform="translate({x} {y}) rotate({rot} {px} {py})">{inner}</g>'
    bx, by = 20, 10   # body top-left in a 320×340 canvas
    wing, foot = owl_wing(), owl_foot()
    return svg(320, 340, shadow(160, 326, 90, 8)
               + part(foot, bx + 105 - 40, by + 262 - 12) + part(foot, bx + 175 - 40, by + 262 - 12)
               + part(wing, bx + 40 - 60, by + 170 - 22, 14, 60, 22)
               + part(owl_body(), bx, by)
               + part(wing, bx + 240 - 60, by + 170 - 22, -14, 60, 22), OWL_DEFS)


# ---------------------------------------------------------------- launcher: walrus on a snowy see-saw

def walrus(jump, blink=False):
    wood = lg("wood", [(0, "#c98d5b"), (1, "#8a5631")])
    body_g = rg("wal", [(0, WALRUS_HI), (0.65, WALRUS), (1, WALRUS_SH)], 0.4, 0.3, 0.85)
    defs = wood + body_g
    if jump:   # landed: left end down, right end up
        plank = (180, 226, 330, 196, 50, 248)
        wy, squash = 26, 0.86
    else:      # ready: left end up, right end down
        plank = (180, 226, 330, 250, 40, 204)
        wy, squash = -10, 1.0
    px, py, rx, ry, lx, ly = plank
    seesaw = (f'<path d="M{px-22},{py+22} L{px},{py-6} L{px+22},{py+22} Z" fill="#9aa8b8" stroke="{INK}" '
              f'stroke-width="{OW-1}" stroke-linejoin="round"/>'
              f'<path d="M{lx},{ly-10} L{rx},{ry-10} L{rx},{ry+6} L{lx},{ly+6} Z" fill="url(#wood)" stroke="{INK}" '
              f'stroke-width="{OW-1}" stroke-linejoin="round"/>'
              f'<path d="M{lx+4},{ly-12} Q{(lx+rx)/2},{(ly+ry)/2-24} {rx-4},{ry-12}" fill="none" stroke="#fff" '
              f'stroke-width="9" stroke-linecap="round" opacity="0.95"/>')
    cx, cy = 104, 132 + wy
    sx = f'translate({cx} {cy}) scale({1/squash:.3f} {squash:.3f}) translate({-cx} {-cy})'
    tusk = "".join(f'<path d="M{cx+dx},{cy-6} C{cx+dx+2},{cy+30} {cx+dx+6},{cy+44} {cx+dx+10},{cy+46} '
                   f'C{cx+dx+10},{cy+30} {cx+dx+10},{cy+10} {cx+dx+10},{cy-6} Z" fill="#fffaf0" stroke="{INK}" '
                   f'stroke-width="3.5" stroke-linejoin="round"/>' for dx in (-18, 6))
    arms = (f'<path d="M{cx-58},{cy+10} C{cx-90},{cy-20 if not jump else cy+20} {cx-84},{cy-40 if not jump else cy+36} '
            f'{cx-70},{cy-44 if not jump else cy+40} C{cx-60},{cy-24 if not jump else cy+30} {cx-50},{cy-6} {cx-44},{cy+4} Z" '
            f'fill="url(#wal)" stroke="{INK}" stroke-width="{OW-1}" stroke-linejoin="round"/>'
            f'<path d="M{cx+58},{cy+10} C{cx+90},{cy-20 if not jump else cy+20} {cx+84},{cy-40 if not jump else cy+36} '
            f'{cx+70},{cy-44 if not jump else cy+40} C{cx+60},{cy-24 if not jump else cy+30} {cx+50},{cy-6} {cx+44},{cy+4} Z" '
            f'fill="url(#wal)" stroke="{INK}" stroke-width="{OW-1}" stroke-linejoin="round"/>')
    body = (f'<path d="M{cx-62},{cy+60} C{cx-70},{cy-10} {cx-40},{cy-62} {cx},{cy-62} C{cx+40},{cy-62} {cx+70},{cy-10} '
            f'{cx+62},{cy+60} C{cx+40},{cy+76} {cx-40},{cy+76} {cx-62},{cy+60} Z" fill="url(#wal)" stroke="{INK}" '
            f'stroke-width="{OW}"/>'
            f'<ellipse cx="{cx}" cy="{cy+34}" rx="40" ry="30" fill="{WALRUS_HI}" opacity="0.55"/>'
            + gloss(cx - 22, cy - 44, 22, 9, 0.6))
    face = ((f'<path d="M{cx-30},{cy-30} q8,-6 16,0 M{cx+14},{cy-30} q8,-6 16,0" fill="none" stroke="#0f1b2d" '
             f'stroke-width="4" stroke-linecap="round"/>') if (jump or blink) else (eye(cx - 22, cy - 30, 7) + eye(cx + 22, cy - 30, 7)))
    muzzle = (f'<ellipse cx="{cx-14}" cy="{cy-8}" rx="20" ry="15" fill="#e6c3a6" stroke="{INK}" stroke-width="3"/>'
              f'<ellipse cx="{cx+14}" cy="{cy-8}" rx="20" ry="15" fill="#e6c3a6" stroke="{INK}" stroke-width="3"/>'
              f'<ellipse cx="{cx}" cy="{cy-22}" rx="9" ry="6" fill="#3a2a24"/>'
              + "".join(f'<circle cx="{cx+dx}" cy="{cy+dy}" r="2" fill="#7a5a48"/>'
                        for dx, dy in ((-22, -10), (-12, -4), (-26, -2), (22, -10), (12, -4), (26, -2))))
    walrus_g = f'<g transform="{sx}">{arms}{body}{tusk}{muzzle}{face}</g>'
    return svg(340, 280, shadow(170, 262, 150, 10) + seesaw + walrus_g, defs)


# ---------------------------------------------------------------- gate guard: snowy owl

def owl(up=False):
    defs = (rg("owl", [(0, "#ffffff"), (0.7, "#f1f6fb"), (1, "#c7d9ea")], 0.45, 0.3, 0.8)
            + rg("eye", [(0, "#fff5a0"), (1, "#f2b705")], 0.4, 0.35, 0.7))
    def wing(s):
        if up:  # wings raised mid-flap
            return (f'<path d="M{128+s*40},{120} C{128+s*96},{36} {128+s*126},{40} {128+s*124},{70} '
                    f'C{128+s*104},{96} {128+s*76},{128} {128+s*52},{150} Z" fill="url(#owl)" stroke="{INK}" '
                    f'stroke-width="{OW-1}" stroke-linejoin="round"/>'
                    + "".join(f'<path d="M{128+s*(80+k*12)},{72+k*14} q{s*10},4 {s*16},14" fill="none" stroke="#5c6f86" '
                              f'stroke-width="3" stroke-linecap="round"/>' for k in range(3)))
        return (f'<path d="M{128+s*40},{120} C{128+s*100},{80} {128+s*124},{120} {128+s*118},{170} '
                f'C{128+s*96},{160} {128+s*72},{170} {128+s*52},{176} Z" fill="url(#owl)" stroke="{INK}" '
                f'stroke-width="{OW-1}" stroke-linejoin="round"/>'
                + "".join(f'<path d="M{128+s*(70+k*14)},{132+k*4} q{s*8},10 {s*2},20" fill="none" stroke="#5c6f86" '
                          f'stroke-width="3" stroke-linecap="round"/>' for k in range(3)))
    wings = wing(-1) + wing(1)
    body = (f'<ellipse cx="128" cy="140" rx="62" ry="72" fill="url(#owl)" stroke="{INK}" stroke-width="{OW}"/>'
            + "".join(f'<path d="M{x},{y} l4,4 l-4,4" fill="none" stroke="#3d4f66" stroke-width="3" stroke-linecap="round"/>'
                      for x, y in ((108, 168), (128, 178), (148, 168), (118, 192), (140, 194)))
            + gloss(108, 90, 22, 9, 0.8))
    face = (f'<path d="M84,118 C84,86 108,78 128,92 C148,78 172,86 172,118 C172,140 150,150 128,140 '
            f'C106,150 84,140 84,118 Z" fill="#fbfdff" stroke="#c9d8e6" stroke-width="3"/>'
            f'<circle cx="108" cy="116" r="15" fill="url(#eye)" stroke="{INK}" stroke-width="3"/>'
            f'<circle cx="148" cy="116" r="15" fill="url(#eye)" stroke="{INK}" stroke-width="3"/>'
            f'<circle cx="110" cy="118" r="7" fill="#0f1b2d"/><circle cx="150" cy="118" r="7" fill="#0f1b2d"/>'
            f'<circle cx="107" cy="114" r="2.5" fill="#fff"/><circle cx="147" cy="114" r="2.5" fill="#fff"/>'
            f'<path d="M92,98 l26,10 M164,98 l-26,10" stroke="{INK}" stroke-width="5" stroke-linecap="round"/>'
            f'<path d="M122,128 L128,142 L134,128 Z" fill="#3b3b3b"/>')
    feet = (f'<path d="M110,206 l-6,12 M116,208 l0,12 M122,206 l6,12 M136,206 l-6,12 M142,208 l0,12 M148,206 l6,12" '
            f'stroke="#f2a23a" stroke-width="5" stroke-linecap="round"/>')
    return svg(256, 256, shadow(128, 236, 60, 8, 0.2) + wings + feet + body + face, defs)


def tern():
    defs = rg("tern", [(0, "#ffffff"), (1, "#d3e2ef")])
    return svg(160, 112,
               f'<path d="M40,58 C20,30 30,18 60,40 Z" fill="#dfe9f3" stroke="{INK}" stroke-width="3.5" stroke-linejoin="round"/>'
               f'<path d="M30,62 L4,50 L18,70 L2,84 L34,72 Z" fill="#dfe9f3" stroke="{INK}" stroke-width="3.5" stroke-linejoin="round"/>'
               f'<ellipse cx="74" cy="64" rx="42" ry="24" fill="url(#tern)" stroke="{INK}" stroke-width="4"/>'
               f'<path d="M86,48 C96,34 120,36 124,52 C112,52 100,54 90,58 Z" fill="#1d2633"/>'
               f'<path d="M120,56 L152,60 L120,66 Z" fill="#ff8a3d" stroke="{INK}" stroke-width="3" stroke-linejoin="round"/>'
               f'<path d="M64,62 C70,30 96,22 104,30 C92,44 84,56 76,70 Z" fill="#ffffff" stroke="{INK}" stroke-width="3.5" stroke-linejoin="round"/>'
               + eye(110, 56, 4.5), defs)


# ---------------------------------------------------------------- enemy: snow bear

# Placeholder frames until the ChatGPT sheet (docs/bear-art-brief.md) is sliced; same names, so the sheet replaces them.
BEAR_ANIMS = (("idle", 4), ("taunt", 4), ("dizzy", 4), ("ko", 4), ("eat", 4), ("sleep", 4))


def bear_eyes(kind, dx=0, dy=0):
    """Eye styles: open, blink, shut (hit), spiral (dizzy), x (knocked out)."""
    out = ""
    for cx in (70 + dx, 100 + dx):
        cy = 66 + dy
        if kind == "open":
            out += eye(cx, cy, 5)
        elif kind == "blink":
            out += f'<path d="M{cx-6},{cy} q6,4 12,0" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>'
        elif kind == "shut":
            out += f'<path d="M{cx-6},{cy-3} l6,4 l6,-4" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>'
        elif kind == "spiral":
            out += (f'<path d="M{cx},{cy} m0,-1 a1,1 0 0 1 1,1 a2,2 0 0 1 -2,2 a3,3 0 0 1 -3,-3 a4,4 0 0 1 4,-4 a5,5 0 0 1 5,5 a6,6 0 0 1 -6,6" '
                    f'fill="none" stroke="{INK}" stroke-width="2.2" stroke-linecap="round"/>')
        else:  # x
            out += f'<path d="M{cx-5},{cy-5} l10,10 M{cx+5},{cy-5} l-10,10" stroke="{INK}" stroke-width="3.5" stroke-linecap="round"/>'
    return out


def bear_frame(pose, i, hat=False):
    """One frame of the polar bear guard on a fixed 170×210 canvas (feet on the bottom line), all drawn in the same
    style: idle breathes and blinks, taunt waves both paws, hit flinches, dizzy wobbles with stars, ko lies out cold."""
    defs = (rg("fur", [(0, "#ffffff"), (0.6, "#f7f6ef"), (1, "#d6dde6")], 0.4, 0.3, 0.85)
            + lg("belly", [(0, "#f8efd6"), (1, "#e6d5a8")])
            + lg("helm", [(0, "#f4fdff", 0.95), (0.5, "#a8e6ff", 0.9), (1, "#4f9fd6", 0.9)], 0, 0, 1, 1))
    W, H = 170, 210
    breath = (0, 1, 2, 1)[i % 4]
    sx, sy, tilt, eyes, mouth, arms, stars, props = 1.0, 1.0, 0, "open", "smile", "down", "", ""
    if pose == "idle":
        sy = 1.0 + 0.015 * breath
        sx = 1.0 - 0.008 * breath
        if i == 3:
            eyes = "blink"
    elif pose == "taunt":
        arms = ("up", "wave", "up", "wave2")[i]
        mouth = "open"
        sy = 1.02 if i % 2 else 1.0
    elif pose == "hit":
        sx, sy = (1.16, 0.84) if i == 0 else (1.06, 0.94)
        eyes, mouth = "shut", "wavy"
    elif pose == "dizzy":
        tilt = (-7, 0, 7, 0)[i]
        eyes, mouth = "spiral", "wavy"
        stars = "".join(f'<path d="M{x},{y-8} l2.5,5.5 l6,0.5 l-4.5,4 l1.5,6 l-5.5,-3 l-5.5,3 l1.5,-6 l-4.5,-4 l6,-0.5 Z" fill="#ffd54a" stroke="#c98f00" stroke-width="1.5"/>'
                        for x, y in (((40, 30), (120, 22), (85, 12)), ((55, 20), (130, 36), (95, 10)),
                                     ((40, 36), (118, 18), (80, 8)), ((60, 14), (128, 40), (100, 14)))[i])
    elif pose == "eat":
        # scoops from a fish bowl held in front of the belly: spoon paw goes up to the mouth and back
        arms = ("bowl", "bowl_up", "bowl_mouth", "bowl_up")[i]
        mouth = ("smile", "smile", "open", "smile")[i]
        props = ('<ellipse cx="85" cy="150" rx="26" ry="10" fill="#7fc3e8" stroke="#2a6fb0" stroke-width="3"/>'
                 '<path d="M59,150 a26,16 0 0 0 52,0 Z" fill="#9fe0fb" stroke="#2a6fb0" stroke-width="3"/>'
                 '<path d="M70,146 q6,-7 12,0 M86,144 q6,-7 12,0" stroke="#ff9a2e" stroke-width="3" fill="none"/>')
    elif pose == "sleep":
        eyes, mouth = "blink", "smile"
        tilt = (6, 8, 6, 8)[i]
        sy = 1.0 - 0.015 * breath
        props = "".join(f'<text x="{122 + i * 6}" y="{48 - i * 10}" font-family="sans-serif" font-weight="bold" font-size="{18 + i * 2}" fill="#5a7cff" stroke="{INK}" stroke-width="1.5">z</text>'
                        for _ in range(1)) + f'<text x="136" y="30" font-family="sans-serif" font-weight="bold" font-size="14" fill="#5a7cff" stroke="{INK}" stroke-width="1.2" opacity="{0.4 + 0.15 * i}">z</text>'
    elif pose == "ko":
        tilt = (-12, -8, -12, -16)[i]
        eyes, mouth, arms = "x", "tongue", ("out", "out2", "out", "out2")[i]
        sy = 0.96
    feet = "".join(f'<ellipse cx="{x}" cy="194" rx="20" ry="11" fill="url(#fur)" stroke="{INK}" stroke-width="{OW}"/>' for x in (60, 110))
    body = (f'<ellipse cx="85" cy="138" rx="54" ry="60" fill="url(#fur)" stroke="{INK}" stroke-width="{OW}"/>'
            f'<ellipse cx="85" cy="150" rx="31" ry="40" fill="url(#belly)"/>')
    arm_pose = {"down": ((36, 146, -14), (134, 146, 14)), "up": ((34, 104, 28), (136, 104, -28)),
                "wave": ((30, 100, 48), (138, 102, -22)), "wave2": ((32, 102, 20), (140, 100, -48)),
                "out": ((26, 150, -70), (144, 150, 70)), "out2": ((28, 146, -62), (142, 146, 62)),
                "bowl": ((48, 150, -40), (122, 150, 40)), "bowl_up": ((48, 150, -40), (118, 126, 10)),
                "bowl_mouth": ((48, 150, -40), (108, 108, -20))}[arms]
    arm_svg = "".join(f'<ellipse cx="{x}" cy="{y}" rx="13" ry="30" fill="url(#fur)" stroke="{INK}" stroke-width="{OW}" transform="rotate({r} {x} {y})"/>'
                      for x, y, r in arm_pose)
    mouths = {"smile": f'<path d="M78,96 q7,5 14,0" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>',
              "open": f'<ellipse cx="85" cy="97" rx="7" ry="5" fill="#5a1f2b" stroke="{INK}" stroke-width="2.5"/>',
              "wavy": f'<path d="M76,97 q4,-4 8,0 t8,0" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>',
              "tongue": (f'<path d="M78,95 q7,4 14,0" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>'
                         f'<ellipse cx="88" cy="101" rx="5" ry="6" fill="#f08a8a" stroke="{INK}" stroke-width="2"/>')}[mouth]
    ears = "".join(f'<circle cx="{x}" cy="42" r="13" fill="url(#fur)" stroke="{INK}" stroke-width="{OW}"/><circle cx="{x}" cy="43" r="6" fill="#f3c6c6"/>'
                   for x in (54, 116))
    head = (f'<g transform="rotate({tilt} 85 100)">' + ears
            + f'<circle cx="85" cy="72" r="40" fill="url(#fur)" stroke="{INK}" stroke-width="{OW}"/>'
            f'<ellipse cx="85" cy="86" rx="19" ry="13" fill="#fdfcf7" stroke="{INK}" stroke-width="3"/>'
            f'<ellipse cx="85" cy="80" rx="8" ry="6" fill="#1c2430"/>' + mouths + bear_eyes(eyes)
            + '<circle cx="60" cy="82" r="6" fill="#f6b8b8" opacity="0.7"/><circle cx="110" cy="82" r="6" fill="#f6b8b8" opacity="0.7"/>'
            + (bear_hat_svg() if hat else "") + "</g>")
    figure = f'<g transform="translate(85 200) scale({sx} {sy}) translate(-85 -200)">' + feet + body + props + arm_svg + head + "</g>"
    return svg(W, H, shadow(85, 198, 56, 9) + figure + stars, defs)


def bear_hat_svg():
    return ('<rect x="52" y="6" width="66" height="44" rx="10" fill="url(#helm)" stroke="#2a6fb0" stroke-width="4"/>'
            '<path d="M62,16 l16,-4 M60,28 l30,-8" stroke="#fff" stroke-width="4" stroke-linecap="round" opacity="0.8"/>')


def bear_hat():
    """The ice helmet alone (worn as a child sprite above the head so every animation shares it)."""
    return svg(80, 56, bear_hat_svg().replace('x="52" y="6"', 'x="7" y="6"').replace("M62,16", "M17,16").replace("M60,28", "M15,28"),
               lg("helm", [(0, "#f4fdff", 0.95), (0.5, "#a8e6ff", 0.9), (1, "#4f9fd6", 0.9)], 0, 0, 1, 1))

def block(defs, base, extra):
    return svg(128, 128, base + extra, defs)


def block_snow():
    defs = lg("sn", [(0, SNOW_HI), (0.55, SNOW), (1, "#cfe5f4")])
    base = (f'<rect x="3" y="3" width="122" height="122" rx="22" fill="url(#sn)" stroke="#3b6e99" stroke-width="{OW}"/>'
            f'<rect x="12" y="10" width="104" height="22" rx="11" fill="#fff" opacity="0.9"/>'
            f'<path d="M10,104 q27,10 54,0 t54,0" fill="none" stroke="#a9cbe4" stroke-width="5" stroke-linecap="round"/>')
    texture = "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#d6e8f5"/>'
                      for x, y, r in ((46, 54, 6), (78, 62, 8), (58, 80, 5), (88, 84, 4), (40, 74, 3)))
    return block(defs, base, texture)


def block_ice():
    defs = lg("ic", [(0, "#dff8ff", 0.97), (0.5, ICE, 0.93), (1, ICE_SH, 0.95)], 0, 0, 1, 1)
    base = (f'<rect x="3" y="3" width="122" height="122" rx="16" fill="url(#ic)" stroke="{ICE_DEEP}" stroke-width="{OW}"/>'
            f'<rect x="11" y="11" width="106" height="106" rx="10" fill="none" stroke="#fff" stroke-width="3" opacity="0.55"/>')
    shine = (f'<path d="M34,36 L58,30 M30,52 L80,38" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity="0.85"/>'
             f'<path d="M70,88 l12,-14 l14,6 M60,96 l10,-6" fill="none" stroke="#e9fbff" stroke-width="3" stroke-linecap="round" opacity="0.8"/>')
    return block(defs, base, shine)


def block_stone():
    defs = lg("st", [(0, "#c3ccd8"), (1, "#7b8798")])
    base = f'<rect x="3" y="3" width="122" height="122" rx="18" fill="url(#st)" stroke="#3e4757" stroke-width="{OW}"/>'
    extra = (f'<rect x="12" y="10" width="104" height="16" rx="8" fill="#fff" opacity="0.6"/>'
             f'<path d="M36,44 l18,16 l-6,18 M82,36 l-10,24 l20,16" fill="none" stroke="#5b6475" stroke-width="5" stroke-linecap="round"/>')
    return block(defs, base, extra)


# ---------------------------------------------------------------- backdrop

def bg_sky():
    return svg(64, 1024, '<rect width="64" height="1024" fill="url(#sky)"/>',
               lg("sky", [(0, "#06122b"), (0.3, "#11285a"), (0.58, "#2f5e9e"), (0.8, "#7fc3e8"), (0.92, "#cdeefa"),
                          (1, "#f7dbe9")]))


def ridge_points(rnd, W, base_lo, base_hi, step_lo, step_hi):
    """Alternating valley/peak points across the tile; first and last share a height so tiles join."""
    pts, x, peak = [], 0, False
    y0 = rnd.randint(base_lo, base_hi)
    pts.append((0, y0))
    while x < W - step_hi:
        x += rnd.randint(step_lo, step_hi)
        peak = not peak
        pts.append((x, rnd.randint(base_lo - 170, base_lo - 60) if peak else rnd.randint(base_lo, base_hi)))
    pts.append((W, y0))
    return pts


def faceted_range(pts, H, lit, shade, cap=True):
    """Each peak split into a lit (left) and shadowed (right) face, with a jagged snow cap."""
    out = f'<path d="M0,{H} ' + " ".join(f"L{x},{y}" for x, y in pts) + f' L{pts[-1][0]},{H} Z" fill="{shade}"/>'
    for i in range(1, len(pts) - 1):
        (lx, ly), (px, py), (rx, ry) = pts[i - 1], pts[i], pts[i + 1]
        if py >= ly or py >= ry:
            continue  # valley
        foot = px + (rx - px) * 0.3
        out += f'<path d="M{lx},{ly} L{px},{py} L{foot:.0f},{H} L{lx},{H} Z" fill="{lit}"/>'
        if cap:
            dl, dr = (px - lx) * 0.28, (rx - px) * 0.28
            hl, hr = (ly - py) * 0.28, (ry - py) * 0.28
            out += (f'<path d="M{px},{py} L{px-dl:.0f},{py+hl:.0f} L{px-dl*0.5:.0f},{py+hl*0.8:.0f} L{px-dl*0.2:.0f},{py+hl*1.05:.0f} '
                    f'L{px+dr*0.3:.0f},{py+hr*0.85:.0f} L{px+dr*0.7:.0f},{py+hr*1.05:.0f} L{px+dr:.0f},{py+hr:.0f} Z" fill="#ffffff" opacity="0.95"/>'
                    f'<path d="M{px},{py} L{px+dr:.0f},{py+hr:.0f} L{px+dr*0.7:.0f},{py+hr*1.05:.0f} L{px+dr*0.3:.0f},{py+hr*0.85:.0f} Z" fill="#d6e6f5"/>')
    return out


def bg_far():
    """Stars, moon, two faceted mountain ridges and haze. Tiles horizontally."""
    rnd = random.Random(5)
    W, H = 2048, 1024
    stars = "".join(f'<circle cx="{rnd.uniform(0, W):.0f}" cy="{rnd.uniform(0, H*0.48):.0f}" r="{rnd.choice([1.2, 1.6, 2, 2.6, 3.4])}" '
                    f'fill="#fff" opacity="{rnd.uniform(0.35, 1):.2f}"/>' for _ in range(220))
    sparkles = "".join(f'<path d="M{x},{y-9} L{x+2},{y-2} L{x+9},{y} L{x+2},{y+2} L{x},{y+9} L{x-2},{y+2} L{x-9},{y} L{x-2},{y-2} Z" fill="#fff" opacity="0.9"/>'
                       for x, y in ((320, 120), (900, 60), (1500, 170), (1880, 90)))
    moon = ('<circle cx="1240" cy="150" r="90" fill="#fff6d8" opacity="0.12" filter="url(#blur)"/>'
            '<circle cx="1240" cy="150" r="46" fill="#fff6d8"/><circle cx="1262" cy="138" r="44" fill="#13305f" opacity="0.92"/>')
    back = faceted_range(ridge_points(rnd, W, 640, 700, 110, 190), H, "#e2f0fb", "#a9c8e4")
    front = faceted_range(ridge_points(rnd, W, 760, 820, 150, 260), H, "#c8e0f3", "#7aa3cb")
    glacier = "".join(f'<path d="M{x},{H} C{x+20},{860} {x+60},{820} {x+90},{800} C{x+110},{830} {x+130},{900} {x+150},{H} Z" fill="#eef8ff" opacity="0.8"/>'
                      for x in (240, 1100, 1700))
    haze = f'<rect x="0" y="{H-260}" width="{W}" height="260" fill="url(#haze)"/>'
    defs = lg("haze", [(0, "#cdeefa", 0), (1, "#cdeefa", 0.9)]) + lg("mist", [(0, "#cdeefa", 0), (1, "#cdeefa", 0.35)])
    return svg(W, H, stars + sparkles + moon + back + f'<rect x="0" y="560" width="{W}" height="464" fill="url(#mist)"/>'
               + front + glacier + haze, defs)


def iceberg(x, base, w, h, rnd):
    peak = [(x, base), (x + w * 0.12, base - h * 0.55), (x + w * 0.32, base - h), (x + w * 0.5, base - h * 0.72),
            (x + w * 0.72, base - h * 0.88), (x + w * 0.88, base - h * 0.4), (x + w, base)]
    d = "M" + " L".join(f"{px:.0f},{py:.0f}" for px, py in peak) + " Z"
    # lit left faces, shaded right faces, crisp rim highlights
    lit = (f'M{x:.0f},{base} L{x+w*0.12:.0f},{base-h*0.55:.0f} L{x+w*0.32:.0f},{base-h:.0f} L{x+w*0.4:.0f},{base} Z')
    lit2 = (f'M{x+w*0.5:.0f},{base-h*0.72:.0f} L{x+w*0.72:.0f},{base-h*0.88:.0f} L{x+w*0.7:.0f},{base} L{x+w*0.52:.0f},{base} Z')
    under = (f'<path d="M{x+w*0.05:.0f},{base} L{x+w*0.95:.0f},{base} L{x+w*0.8:.0f},{base+h*0.42:.0f} L{x+w*0.25:.0f},{base+h*0.38:.0f} Z" '
             f'fill="#5fb0dd" opacity="0.45"/>')
    refl = (f'<path d="M{x:.0f},{base+2} L{x+w:.0f},{base+2} L{x+w*0.75:.0f},{base+h*0.55:.0f} L{x+w*0.25:.0f},{base+h*0.55:.0f} Z" '
            f'fill="url(#refl)" opacity="0.5"/>')
    rims = (f'<path d="M{x+w*0.12:.0f},{base-h*0.55:.0f} L{x+w*0.32:.0f},{base-h:.0f} L{x+w*0.5:.0f},{base-h*0.72:.0f} L{x+w*0.72:.0f},{base-h*0.88:.0f}" '
            f'fill="none" stroke="#fff" stroke-width="5" stroke-linejoin="round" stroke-linecap="round"/>')
    cracks = "".join(f'<path d="M{x+w*a:.0f},{base-h*b:.0f} l{rnd.randint(-14,14)},{rnd.randint(16,34)}" stroke="#8cc4e8" stroke-width="2.5" opacity="0.8"/>'
                     for a, b in ((0.6, 0.6), (0.8, 0.5), (0.45, 0.4)))
    return (under + refl + f'<path d="{d}" fill="#a9d4f0" stroke="#3b6e99" stroke-width="3.5" stroke-linejoin="round"/>'
            f'<path d="{lit}" fill="#f2faff"/><path d="{lit2}" fill="#e3f3fd"/>' + cracks + rims)


def igloo_village(x, y):
    floe = (f'<ellipse cx="{x+150}" cy="{y+18}" rx="190" ry="26" fill="#a9d4f0" stroke="#3b6e99" stroke-width="3"/>'
            f'<ellipse cx="{x+150}" cy="{y+8}" rx="186" ry="20" fill="#f4fbff"/>')
    igloos = ""
    for ix, r in ((60, 46), (160, 62), (260, 40)):
        cx = x + ix
        igloos += (f'<path d="M{cx-r},{y+8} A{r},{r} 0 0 1 {cx+r},{y+8} Z" fill="url(#igloo)" stroke="#3b6e99" stroke-width="3"/>'
                   + "".join(f'<path d="M{cx-r*k:.0f},{y+8-r*(1-k*k)**0.5*0.5:.0f} H{cx+r*k:.0f}" stroke="#b9d6ec" stroke-width="2"/>'
                             for k in (0.95, 0.8))
                   + f'<path d="M{cx-r*0.32:.0f},{y+8} A{r*0.32:.0f},{r*0.32:.0f} 0 0 1 {cx+r*0.32:.0f},{y+8} Z" fill="#ffb347"/>'
                   + f'<circle cx="{cx}" cy="{y}" r="{r*0.5:.0f}" fill="#ffcf70" opacity="0.35" filter="url(#blur)"/>')
    flag = f'<path d="M{x+300},{y+8} V{y-50}" stroke="#6b4232" stroke-width="4"/><path d="M{x+300},{y-50} l34,10 l-34,10 Z" fill="#f26b3a"/>'
    return floe + igloos + flag


def bg_mid():
    """Open sea with faceted icebergs and the seal village. Tiles horizontally."""
    rnd = random.Random(9)
    W, H = 2048, 512
    sea_top = 330
    sea = (f'<rect x="0" y="{sea_top}" width="{W}" height="{H-sea_top}" fill="url(#sea)"/>'
           f'<rect x="0" y="{sea_top}" width="{W}" height="6" fill="#e6f7ff" opacity="0.7"/>'
           + "".join(f'<path d="M{x},{y} h{rnd.randint(30, 110)}" stroke="#d8f2ff" stroke-width="{rnd.choice([2, 3, 4])}" stroke-linecap="round" '
                     f'opacity="{rnd.uniform(0.2, 0.6):.2f}"/>'
                     for x, y in ((rnd.randint(0, W - 120), rnd.randint(sea_top + 14, H - 8)) for _ in range(70))))
    bergs = "".join(iceberg(x, sea_top + 4, w, h, rnd)
                    for x, w, h in ((40, 320, 200), (480, 170, 110), (760, 400, 240), (1780, 240, 160)))
    village = igloo_village(1300, sea_top - 14)
    defs = (lg("sea", [(0, "#4d8fcf"), (0.5, "#2a62a3"), (1, "#163a72")])
            + lg("refl", [(0, "#ffffff"), (1, "#ffffff", 0)])
            + rg("igloo", [(0, "#ffffff"), (1, "#cfe4f4")], 0.4, 0.3, 0.9))
    return svg(W, H, sea + bergs + village, defs)


def ground():
    """Snow-capped ice shelf with strata, icicles and trapped bubbles; edges meet at y=40 for seamless tiling."""
    W, H = 1024, 256
    rnd = random.Random(21)
    top = "M0,40 C120,22 220,52 340,38 C460,24 560,50 680,40 C800,30 900,52 1024,40"
    face = f'<path d="{top} L1024,{H} L0,{H} Z" fill="url(#iceface)"/>'
    strata = "".join(f'<path d="M0,{y} C256,{y-8} 512,{y+8} 768,{y-4} S1024,{y}" fill="none" stroke="{c}" stroke-width="{w}" opacity="0.5"/>'
                     for y, c, w in ((120, "#c8ecff", 6), (160, "#3f86c4", 4), (205, "#a8dcf7", 5), (236, "#2a6aa8", 3)))
    streaks = "".join(f'<path d="M{x},96 L{x+22},{H}" stroke="#e6f8ff" stroke-width="{w}" opacity="0.3"/>'
                      for x, w in ((60, 12), (210, 6), (380, 14), (540, 7), (700, 12), (880, 6)))
    bubbles = "".join(f'<circle cx="{rnd.randint(10, W-10)}" cy="{rnd.randint(120, H-10)}" r="{rnd.choice([3, 4, 6])}" fill="none" '
                      f'stroke="#e9fbff" stroke-width="2" opacity="0.55"/>' for _ in range(26))
    icicles = ""
    x = 8
    while x < W - 20:
        w, h = rnd.randint(10, 18), rnd.randint(16, 46)
        icicles += (f'<path d="M{x},92 L{x+w},92 L{x+w*0.55:.0f},{92+h} Z" fill="#e9fbff" stroke="#6fb3e0" stroke-width="1.5"/>'
                    f'<path d="M{x+2},94 L{x+w*0.45:.0f},{92+h*0.7:.0f}" stroke="#fff" stroke-width="2"/>')
        x += w + rnd.randint(14, 40)
    snow = (f'<path d="{top} L1024,96 C900,108 820,86 700,98 C580,110 470,88 340,100 C220,112 110,90 0,96 Z" fill="url(#snowcap)"/>'
            f'<path d="M0,96 C110,90 220,112 340,100 C470,88 580,110 700,98 C820,86 900,108 1024,96" fill="none" stroke="#9cc7e6" stroke-width="3"/>'
            f'<path d="{top}" fill="none" stroke="#3b6e99" stroke-width="{OW}"/>'
            f'<path d="M30,52 C160,40 260,60 380,50 M560,48 C640,40 720,52 800,46" fill="none" stroke="#fff" stroke-width="6" stroke-linecap="round"/>'
            + "".join(f'<circle cx="{rnd.randint(20, W-20)}" cy="{rnd.randint(52, 84)}" r="2" fill="#ffffff"/>' for _ in range(30)))
    defs = (lg("iceface", [(0, "#b3e4fa"), (0.45, "#5ba6d8"), (1, "#245f9e")])
            + lg("snowcap", [(0, "#ffffff"), (1, "#d7eaf7")]))
    return svg(W, H, face + strata + streaks + bubbles + icicles + snow, defs)


def fg_snow():
    """Foreground snow drifts for depth. Transparent above the drifts; edges meet for tiling."""
    W, H = 1024, 160
    top = "M0,60 C90,30 170,40 250,62 C330,84 400,44 500,36 C600,28 660,70 760,64 C860,58 930,30 1024,60"
    return svg(W, H, f'<path d="{top} L1024,{H} L0,{H} Z" fill="url(#fg)"/>'
               f'<path d="{top}" fill="none" stroke="#ffffff" stroke-width="6" stroke-linecap="round"/>'
               '<path d="M120,96 C200,84 260,100 330,92 M600,90 C680,80 760,96 840,88" fill="none" stroke="#c4def0" stroke-width="5" stroke-linecap="round"/>',
               lg("fg", [(0, "#f6fbff"), (1, "#b8d6ec")]))


def ice_ledge():
    """Raised ice cliff where pups queue, with a snow slide curving down to the see-saw (right side)."""
    W, H = 512, 420
    rnd = random.Random(33)
    body = (f'M0,70 L300,70 C360,70 400,150 430,250 C450,320 480,380 512,404 L512,{H} L0,{H} Z')
    face = f'<path d="{body}" fill="url(#cliff)" stroke="#2a6fb0" stroke-width="4" stroke-linejoin="round"/>'
    facets = (f'<path d="M0,110 L120,96 L80,{H} L0,{H} Z" fill="#cfeeff" opacity="0.45"/>'
              f'<path d="M180,100 L260,92 L300,{H} L200,{H} Z" fill="#2f74b5" opacity="0.25"/>')
    strata = "".join(f'<path d="M0,{y} C80,{y-6} 200,{y+6} {300+(y-70)*0.45:.0f},{y}" fill="none" stroke="#e3f6ff" stroke-width="3" opacity="0.5"/>'
                     for y in (160, 230, 300, 360))
    icicles = "".join(f'<path d="M{x},96 L{x+12},96 L{x+6},{96+h} Z" fill="#e9fbff" stroke="#6fb3e0" stroke-width="1.5"/>'
                      for x, h in ((20, 30), (70, 18), (120, 36), (170, 22), (230, 28)))
    snowtop = ('<path d="M0,70 L300,70 C330,70 350,84 362,100 C330,92 300,96 270,92 C200,100 120,88 60,96 C30,98 10,94 0,92 Z" fill="url(#snowtop)"/>'
               '<path d="M0,70 L300,70 C330,70 350,84 362,100" fill="none" stroke="#3b6e99" stroke-width="4"/>')
    # the slide: packed-snow chute with a raised lip
    chute = ('<path d="M300,70 C360,70 400,150 430,250 C450,320 480,380 512,404" fill="none" stroke="#ffffff" stroke-width="26" stroke-linecap="round"/>'
             '<path d="M300,70 C360,70 400,150 430,250 C450,320 480,380 512,404" fill="none" stroke="#bcdcf2" stroke-width="8" '
             'stroke-linecap="round" transform="translate(-6 8)"/>'
             '<path d="M306,64 C366,64 408,146 438,246" fill="none" stroke="#fff" stroke-width="5" stroke-linecap="round" opacity="0.9"/>')
    bubbles = "".join(f'<circle cx="{rnd.randint(20, 260)}" cy="{rnd.randint(150, 400)}" r="{rnd.choice([3, 5, 7])}" fill="none" '
                      f'stroke="#e9fbff" stroke-width="2" opacity="0.5"/>' for _ in range(14))
    defs = lg("cliff", [(0, "#a6dcf7"), (0.5, "#5ba6d8"), (1, "#2c6aa8")]) + lg("snowtop", [(0, "#ffffff"), (1, "#d7eaf7")])
    return svg(W, H, face + facets + strata + bubbles + icicles + chute + snowtop, defs)


def sign():
    """Wooden counter board on a post, capped with snow (the number is drawn in-game)."""
    return svg(200, 220,
               '<rect x="92" y="120" width="16" height="100" rx="4" fill="#7a4a2b" stroke="#3e2414" stroke-width="3"/>'
               '<rect x="14" y="40" width="172" height="100" rx="14" fill="url(#wood)" stroke="#3e2414" stroke-width="5"/>'
               '<rect x="30" y="56" width="140" height="68" rx="10" fill="#2a1a10" opacity="0.55"/>'
               '<path d="M18,48 C40,24 80,30 100,38 C130,24 170,28 184,48 C150,44 120,52 100,48 C70,54 40,44 18,48 Z" fill="#ffffff" stroke="#9cc7e6" stroke-width="2"/>'
               '<path d="M30,92 h8 M162,92 h8" stroke="#c99a6a" stroke-width="5" stroke-linecap="round"/>',
               lg("wood", [(0, "#c98d5b"), (1, "#8a5631")]))


def rope():
    """Braided rope segment, tiles vertically when stretched (anchor at the top)."""
    twists = "".join(f'<path d="M4,{y} L20,{y+10}" stroke="#7a5230" stroke-width="3"/>' for y in range(-4, 256, 12))
    return svg(24, 256, '<rect x="5" y="0" width="14" height="256" rx="7" fill="#c9965e" stroke="#5a3a1e" stroke-width="2"/>'
               + twists + '<rect x="8" y="0" width="3" height="256" fill="#fff" opacity="0.25"/>')


def beam():
    """Wooden crane arm bolted to the ice cliff; the rope hangs from its right end."""
    return svg(320, 90,
               '<rect x="4" y="30" width="300" height="30" rx="8" fill="url(#bw)" stroke="#3e2414" stroke-width="4"/>'
               '<path d="M10,30 C60,10 140,22 200,18 C250,14 290,22 304,30 Z" fill="#ffffff" stroke="#9cc7e6" stroke-width="2"/>'
               '<circle cx="40" cy="45" r="5" fill="#3e2414"/><circle cx="90" cy="45" r="5" fill="#3e2414"/>'
               '<rect x="282" y="56" width="20" height="18" rx="4" fill="#8a8f99" stroke="#3e4757" stroke-width="3"/>'
               '<path d="M12,44 h280" stroke="#e0b07a" stroke-width="3" opacity="0.6"/>',
               lg("bw", [(0, "#c98d5b"), (1, "#7a4a2b")]))


def whacker_base():
    """The whacker: a snow sled with a timber A-frame, gears and a spring; the arm pivots on the top hub."""
    W, H = 300, 260
    sled = ('<path d="M20,214 L262,214 C286,214 292,196 284,186" fill="none" stroke="#3e4757" stroke-width="10" stroke-linecap="round"/>'
            '<path d="M20,214 L262,214 C286,214 292,196 284,186" fill="none" stroke="#c0c8d4" stroke-width="5" stroke-linecap="round"/>'
            '<rect x="30" y="170" width="230" height="34" rx="8" fill="url(#wood)" stroke="#3e2414" stroke-width="4"/>'
            '<path d="M40,190 h210" stroke="#e0b07a" stroke-width="3" opacity="0.6"/>'
            '<path d="M60,204 v10 M230,204 v10" stroke="#3e4757" stroke-width="6"/>')
    frame = ('<path d="M70,172 L150,70 L230,172" fill="none" stroke="#3e2414" stroke-width="20" stroke-linejoin="round"/>'
             '<path d="M70,172 L150,70 L230,172" fill="none" stroke="url(#wood)" stroke-width="13" stroke-linejoin="round"/>'
             '<path d="M100,134 L200,134" stroke="#3e2414" stroke-width="10"/><path d="M100,134 L200,134" stroke="#b07a4a" stroke-width="5"/>')
    gear = "".join(f'<rect x="{88-5}" y="{150-26}" width="10" height="12" rx="2" fill="#8a8f99" stroke="#3e4757" stroke-width="2" '
                   f'transform="rotate({a} 88 150)"/>' for a in range(0, 360, 45))
    gear += ('<circle cx="88" cy="150" r="20" fill="#a3abb8" stroke="#3e4757" stroke-width="4"/><circle cx="88" cy="150" r="6" fill="#3e4757"/>')
    spring = '<path d="M196,170 ' + " ".join(f"l8,-6 l-16,-6" for _ in range(5)) + '" fill="none" stroke="#c0c8d4" stroke-width="5" stroke-linejoin="round"/>'
    hub = ('<circle cx="150" cy="70" r="24" fill="#a3abb8" stroke="#3e4757" stroke-width="5"/>'
           '<circle cx="150" cy="70" r="9" fill="#3e4757"/><circle cx="143" cy="63" r="5" fill="#fff" opacity="0.6"/>')
    snow = ('<path d="M30,172 C70,158 120,166 160,160 C200,154 240,164 260,172 Z" fill="#fff" stroke="#9cc7e6" stroke-width="2"/>'
            '<path d="M134,58 C140,44 160,44 166,58 Z" fill="#fff"/>')
    # No lever on the machine: the operator pup's frames draw the lever, and the pup stands at the sled's end.
    return svg(W, H, shadow(150, 226, 130, 10) + sled + frame + gear + spring + snow + hub,
               lg("wood", [(0, "#c98d5b"), (1, "#7a4a2b")]))


def whacker_arm():
    """Catapult arm: pole from the hub (left end) to an open cup (right end) that carries the snowball."""
    W, H = 360, 140
    pole = ('<rect x="10" y="60" width="250" height="20" rx="10" fill="url(#pole)" stroke="#3e2414" stroke-width="4"/>'
            '<path d="M20,66 h230" stroke="#e0b07a" stroke-width="3" opacity="0.6"/>'
            + "".join(f'<rect x="{x}" y="56" width="10" height="28" rx="3" fill="#8a8f99" stroke="#3e4757" stroke-width="2"/>' for x in (60, 170)))
    cup = ('<path d="M236,24 A64,64 0 1 0 236,116 L258,116 A42,42 0 1 1 258,24 Z" fill="url(#cupg)" stroke="#3e2414" stroke-width="5" stroke-linejoin="round"/>'
           '<path d="M244,34 A52,52 0 1 0 244,106" fill="none" stroke="#e0b07a" stroke-width="3" opacity="0.6"/>')
    return svg(W, H, pole + cup,
               lg("pole", [(0, "#c98d5b"), (1, "#7a4a2b")]) + lg("cupg", [(0, "#a86b3c"), (1, "#5e3a1c")]))


def snowball():
    return svg(128, 128, '<circle cx="64" cy="64" r="58" fill="url(#sb)" stroke="#2a6fb0" stroke-width="5"/>'
               '<ellipse cx="46" cy="42" rx="18" ry="9" fill="#fff" opacity="0.9" transform="rotate(-25 46 42)"/>'
               '<circle cx="80" cy="84" r="5" fill="#cfe4f4"/><circle cx="56" cy="92" r="3" fill="#cfe4f4"/>',
               rg("sb", [(0, "#ffffff"), (0.7, "#e6f3fc"), (1, "#a9cfe9")], 0.4, 0.3, 0.8))


def cave_wall():
    """9-slice icy rock for the launch pit walls and ceiling (border 40px): rock body, ice veins, snow cap."""
    W = 160
    rnd = random.Random(44)
    rocks = "".join(f'<path d="M{x},{y} l{rnd.randint(14,26)},{rnd.randint(-6,6)} l{rnd.randint(-4,6)},{rnd.randint(14,22)} '
                    f'l-{rnd.randint(16,26)},{rnd.randint(-4,4)} Z" fill="#6f87a8" opacity="0.55"/>'
                    for x, y in ((50, 56), (88, 70), (60, 96), (96, 104), (70, 60)))
    veins = ('<path d="M44,120 C64,100 70,84 96,74 M100,118 C110,100 108,88 120,70" fill="none" stroke="#bfe6ff" stroke-width="4" opacity="0.7"/>')
    return svg(W, W,
               f'<rect x="3" y="3" width="{W-6}" height="{W-6}" rx="26" fill="url(#rock)" stroke="#23365a" stroke-width="5"/>'
               f'<rect x="12" y="12" width="{W-24}" height="{W-24}" rx="18" fill="none" stroke="#8fa6c6" stroke-width="3" opacity="0.5"/>'
               + rocks + veins +
               f'<path d="M3,30 C30,14 60,22 80,16 C104,10 130,18 157,28 L157,38 C120,30 90,40 60,34 C40,30 20,40 3,40 Z" fill="#ffffff" opacity="0.95"/>',
               lg("rock", [(0, "#8ea4c4"), (0.5, "#5d7699"), (1, "#3c5274")]))


def icicles():
    """Icicle fringe hanging under the ceiling; tiles horizontally."""
    rnd = random.Random(12)
    out, x = "", 0
    while x < 512:
        w, h = rnd.randint(14, 26), rnd.randint(28, 90)
        out += (f'<path d="M{x},0 L{x+w},0 L{x+w*0.55:.0f},{h} Z" fill="url(#ic)" stroke="#5aa6d8" stroke-width="2"/>'
                f'<path d="M{x+3},2 L{x+w*0.45:.0f},{h*0.7:.0f}" stroke="#fff" stroke-width="2.5"/>')
        x += w + rnd.randint(2, 18)
    return svg(512, 100, out, lg("ic", [(0, "#ffffff"), (1, "#a9dcf7")]))


def map_bg():
    """Top-down Frost Bay island for the landscape level map (1920×1080 reference). Levels 1–6 run along the
    south shore left to right, 7–10 come back along the north toward the bear fort (see MapView.Nodes)."""
    W, H = 1920, 1080
    rnd = random.Random(77)
    sea = (f'<rect width="{W}" height="{H}" fill="url(#mapsea)"/>'
           + "".join(f'<path d="M{rnd.randint(0, W)},{rnd.randint(0, H)} h{rnd.randint(30, 90)}" stroke="#bfe8fb" stroke-width="4" '
                     f'stroke-linecap="round" opacity="{rnd.uniform(0.2, 0.5):.2f}"/>' for _ in range(90)))
    coast = ("M140,560 C160,330 420,180 760,170 C1100,160 1500,200 1760,360 C1880,440 1860,640 1720,780 "
             "C1600,920 1300,1000 980,960 C700,930 420,1010 260,900 C150,820 120,700 140,560 Z")
    shore = f'<path d="{coast}" fill="#9fd8f4" stroke="#2a6fb0" stroke-width="10" transform="translate(0 16)"/>'
    land = f'<path d="{coast}" fill="url(#mapsnow)" stroke="#3b6e99" stroke-width="8"/>'
    shading = "".join(f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="#d3e6f5" opacity="0.7"/>'
                      for x, y, rx, ry in ((420, 760, 170, 60), (900, 820, 190, 70), (1450, 700, 160, 60), (1200, 380, 150, 55)))
    lakes = "".join(f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="url(#lake)" stroke="#5aa6d8" stroke-width="5"/>'
                    f'<path d="M{x-rx*0.5},{y-ry*0.3} q{rx*0.3},-{ry*0.3} {rx*0.6},0" stroke="#fff" stroke-width="5" fill="none" stroke-linecap="round"/>'
                    for x, y, rx, ry in ((1500, 520, 90, 45), (640, 560, 80, 38), (1180, 640, 70, 34)))
    def peak(x, y, s):
        return (f'<path d="M{x-60*s},{y} L{x},{y-90*s} L{x+60*s},{y} Z" fill="#a9c8e4" stroke="#3b6e99" stroke-width="5" stroke-linejoin="round"/>'
                f'<path d="M{x-60*s},{y} L{x},{y-90*s} L{x+8*s},{y} Z" fill="#e2f0fb"/>'
                f'<path d="M{x-22*s},{y-56*s} L{x},{y-90*s} L{x+22*s},{y-56*s} L{x+8*s},{y-62*s} L{x-6*s},{y-52*s} Z" fill="#fff"/>')
    peaks = "".join(peak(x, y, s) for x, y, s in ((1000, 330, 1.3), (1120, 350, 1.0), (880, 360, 0.9), (1580, 330, 0.8), (300, 640, 0.8)))
    def igloo(x, y, r):
        return (f'<path d="M{x-r},{y} A{r},{r} 0 0 1 {x+r},{y} Z" fill="#fff" stroke="#3b6e99" stroke-width="4"/>'
                f'<path d="M{x-r*0.3},{y} A{r*0.3},{r*0.3} 0 0 1 {x+r*0.3},{y} Z" fill="#ffb347"/>')
    village = igloo(560, 940, 44) + igloo(650, 948, 32) + igloo(490, 950, 28)
    fort = ('<g transform="translate(470 330)"><rect x="-60" y="-40" width="120" height="70" fill="#9fe0fb" stroke="#2a6fb0" stroke-width="5"/>'
            '<path d="M-60,-40 h20 v-16 h20 v16 h20 v-16 h20 v16 h20 v-16 h20 v16" fill="#9fe0fb" stroke="#2a6fb0" stroke-width="5" stroke-linejoin="round"/>'
            '<path d="M0,-56 v-40" stroke="#6b4232" stroke-width="5"/><path d="M0,-96 l36,12 l-36,12 Z" fill="#f26b3a"/></g>')
    floes = "".join(f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{rx*0.45:.0f}" fill="#f4fbff" stroke="#5aa6d8" stroke-width="4"/>'
                    for x, y, rx in ((60, 200, 34), (1860, 160, 30), (1880, 980, 26), (80, 1020, 28), (960, 80, 36), (1500, 1040, 30)))
    defs = (lg("mapsea", [(0, "#2f6fb0"), (1, "#173e78")]) + rg("mapsnow", [(0, "#ffffff"), (0.75, "#eef7fd"), (1, "#cfe4f4")], 0.5, 0.45, 0.8)
            + rg("lake", [(0, "#e9fbff"), (1, "#7fc3e8")], 0.4, 0.35, 0.8))
    return svg(W, H, sea + floes + shore + land + shading + lakes + peaks + village + fort, defs)


# ---------------------------------------------------------------- phase-2 arena (dusk sky, pack ice, cobble rock)

ROCK_BASE, ROCK_EDGE = "#4a5d7c", "#2b3a55"
PEBBLES = ("#6f85a6", "#5b7194", "#8198b8", "#3f5272", "#7a90b0")


def pebbles(rnd, x0, y0, x1, y1, n):
    out = ""
    for _ in range(n):
        rx, ry = rnd.randint(9, 20), rnd.randint(7, 14)
        cx, cy = rnd.randint(x0 + rx, x1 - rx), rnd.randint(y0 + ry, y1 - ry)
        out += (f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="{rnd.choice(PEBBLES)}" stroke="{ROCK_EDGE}" stroke-width="2"/>'
                f'<ellipse cx="{cx-rx*0.3:.0f}" cy="{cy-ry*0.35:.0f}" rx="{rx*0.35:.0f}" ry="{ry*0.3:.0f}" fill="#fff" opacity="0.18"/>')
    return out


def rock_cobble():
    """Tiling cobble body for arena terrain (drawn Tiled in Unity, 1.6 world units per tile)."""
    rnd = random.Random(51)
    return svg(160, 160, f'<rect width="160" height="160" fill="{ROCK_BASE}"/>' + pebbles(rnd, 0, 0, 160, 160, 26))


def rock_top():
    """Snow rim laid along the top edge of rock blocks (tiles horizontally)."""
    return svg(512, 44,
               '<path d="M0,0 H512 V28 C440,40 380,22 320,32 C250,42 180,22 110,32 C70,38 30,24 0,30 Z" fill="#eef6fc"/>'
               '<path d="M0,30 C30,24 70,38 110,32 C180,22 250,42 320,32 C380,22 440,40 512,28" fill="none" stroke="#9cc7e6" stroke-width="3"/>'
               f'<path d="M0,2 H512" stroke="{ROCK_EDGE}" stroke-width="4"/>')


def rock_ramp():
    """Right triangle of cobble rising toward +x (right angle bottom-right), snow rim on the slope. flipX for the mirror."""
    rnd = random.Random(52)
    W = 512
    clip = f'<clipPath id="tri"><path d="M0,{W} L{W},{W} L{W},0 Z"/></clipPath>'
    body = (f'<g clip-path="url(#tri)"><rect width="{W}" height="{W}" fill="{ROCK_BASE}"/>' + pebbles(rnd, 0, 0, W, W, 160) + '</g>')
    rim = (f'<path d="M0,{W} L{W},0 L{W},44 L44,{W} Z" fill="#eef6fc" clip-path="url(#tri)"/>'
           f'<path d="M0,{W} L{W},0" stroke="{ROCK_EDGE}" stroke-width="6"/>'
           f'<path d="M44,{W} L{W},44" stroke="#9cc7e6" stroke-width="3"/>')
    return svg(W, W, body + rim, clip)


def bg_dusk():
    """Sunset sky for the arena, top to bottom: orange, gold, cream, pale ice blue."""
    return svg(64, 1024, '<rect width="64" height="1024" fill="url(#dusk)"/>',
               lg("dusk", [(0, "#ff8a3d"), (0.35, "#ffb347"), (0.62, "#ffd66e"), (0.82, "#fff1bd"), (1, "#dff3ff")]))


def bg_floes():
    """Arena horizon band (tiles horizontally): orange dune hills, a strip of distant icebergs, then pack ice floes."""
    rnd = random.Random(53)
    W, H = 1024, 480
    hills = "".join(f'<path d="M{x},120 C{x+20},{120-h} {x+60},{120-h} {x+90},120 Z" fill="{c}"/>'
                    for x, h, c in ((rnd.randint(-40, W), rnd.randint(30, 80), rnd.choice(("#f28c2b", "#ffa63a", "#ff9a2e")))
                                    for _ in range(40)))
    band = f'<rect x="0" y="100" width="{W}" height="24" fill="#ffd66e" opacity="0.6"/>'
    horizon = (f'<rect x="0" y="120" width="{W}" height="50" fill="#e8f6ff"/>'
               + "".join(f'<path d="M{x},170 L{x+w*0.3:.0f},{170-h} L{x+w*0.55:.0f},{170-h*0.6:.0f} L{x+w*0.8:.0f},{170-h*0.9:.0f} L{x+w},170 Z" '
                         f'fill="#ffffff" stroke="#bfe3f7" stroke-width="2"/>'
                         for x, w, h in ((rnd.randint(0, W - 90), rnd.randint(40, 90), rnd.randint(14, 36)) for _ in range(26))))
    sea = f'<rect x="0" y="170" width="{W}" height="{H-170}" fill="#a9d8f0"/>'
    floes = "".join(f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="#f4fbff" stroke="#7fc3e8" stroke-width="2"/>'
                    for x, y, rx, ry in ((rnd.randint(0, W), rnd.randint(185, H - 10), rnd.randint(26, 80), rnd.randint(8, 22))
                                         for _ in range(90)))
    shade = "".join(f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="#8cc6e6" opacity="0.5"/>'
                    for x, y, rx, ry in ((rnd.randint(0, W), rnd.randint(185, H - 10), rnd.randint(20, 60), rnd.randint(6, 14))
                                         for _ in range(30)))
    return svg(W, H, hills + band + horizon + sea + shade + floes)


# ---------------------------------------------------------------- arena objects

def bumper():
    """Orange gear bumper (spins in code)."""
    teeth = "".join(f'<rect x="54" y="4" width="20" height="24" rx="6" fill="#ff9a2e" stroke="{INK}" stroke-width="4" transform="rotate({a} 64 64)"/>'
                    for a in range(0, 360, 36))
    return svg(128, 128, teeth + f'<circle cx="64" cy="64" r="44" fill="url(#bmp)" stroke="{INK}" stroke-width="5"/>'
               f'<circle cx="64" cy="64" r="22" fill="#ffd66e" stroke="{INK}" stroke-width="4"/><circle cx="56" cy="54" r="7" fill="#fff" opacity="0.8"/>',
               rg("bmp", [(0, "#ffb347"), (1, "#f26b3a")], 0.4, 0.35, 0.8))


def crate_tnt():
    return svg(128, 128,
               f'<rect x="6" y="6" width="116" height="116" rx="10" fill="url(#tnt)" stroke="{INK}" stroke-width="5"/>'
               f'<path d="M6,40 H122 M6,88 H122" stroke="#8a2c10" stroke-width="6"/>'
               f'<rect x="26" y="50" width="76" height="28" rx="6" fill="#fff3d6" stroke="{INK}" stroke-width="3"/>'
               f'<text x="64" y="72" text-anchor="middle" font-family="sans-serif" font-weight="bold" font-size="22" fill="#c7352b">TNT</text>'
               f'<path d="M64,6 q10,-14 22,-4" fill="none" stroke="#3e2414" stroke-width="4"/><circle cx="88" cy="0" r="7" fill="#ffd54a"/>',
               lg("tnt", [(0, "#ff6b5b"), (1, "#c7352b")]))


def floor_button(down):
    cap_y = 44 if down else 28
    return svg(128, 72,
               f'<rect x="6" y="48" width="116" height="20" rx="8" fill="#8a8f99" stroke="{INK}" stroke-width="4"/>'
               f'<rect x="20" y="{cap_y}" width="88" height="{66 - cap_y}" rx="12" fill="url(#cap)" stroke="{INK}" stroke-width="4"/>'
               f'<ellipse cx="64" cy="{cap_y + 4}" rx="40" ry="10" fill="#ff8a80" opacity="0.7"/>',
               lg("cap", [(0, "#ff6b5b"), (1, "#c7352b")]))


def fish():
    return svg(128, 96,
               f'<path d="M14,48 C40,10 90,10 112,48 C90,86 40,86 14,48 Z" fill="url(#fsh)" stroke="{INK}" stroke-width="5" stroke-linejoin="round"/>'
               f'<path d="M14,48 L-2,26 L4,48 L-2,70 Z" fill="#ffb347" stroke="{INK}" stroke-width="4" stroke-linejoin="round" transform="translate(14 0)"/>'
               f'<circle cx="88" cy="42" r="6" fill="#1b2a49"/><circle cx="90" cy="40" r="2" fill="#fff"/>'
               f'<path d="M50,30 q12,18 0,36" fill="none" stroke="#ffd66e" stroke-width="3"/>',
               lg("fsh", [(0, "#ffd66e"), (1, "#ff9a2e")]))


def cage(open_door=False):
    bars = "".join(f'<path d="M{x},30 V150" stroke="#5b6475" stroke-width="6" stroke-linecap="round"/>' for x in range(34, 130, 16))
    door = ('<g transform="rotate(-70 110 30)">' + "".join(f'<path d="M{x},30 V150" stroke="#5b6475" stroke-width="6" stroke-linecap="round"/>' for x in (110, 126)) + '</g>') if open_door else ""
    return svg(160, 170,
               f'<path d="M20,30 a60,30 0 0 1 120,0" fill="none" stroke="{INK}" stroke-width="7"/>'
               f'<rect x="14" y="146" width="132" height="14" rx="6" fill="#8a8f99" stroke="{INK}" stroke-width="4"/>'
               + (bars if not open_door else "".join(f'<path d="M{x},30 V150" stroke="#5b6475" stroke-width="6" stroke-linecap="round"/>' for x in range(34, 100, 16)) + door)
               + f'<rect x="72" y="2" width="16" height="30" rx="4" fill="#8a8f99" stroke="{INK}" stroke-width="3"/>')


def vignette():
    return svg(256, 256, '<rect width="256" height="256" fill="url(#v)"/>',
               '<radialGradient id="v" cx="0.5" cy="0.5" r="0.72"><stop offset="0.55" stop-color="#06122b" stop-opacity="0"/>'
               '<stop offset="1" stop-color="#06122b" stop-opacity="0.6"/></radialGradient>')


def crack():
    return svg(128, 128, '<g fill="none" stroke-linecap="round" stroke-linejoin="round">'
               '<path d="M64,64 L40,30 L46,14 M64,64 L96,40 L118,46 M64,64 L70,100 L58,120 M64,64 L24,74 L8,70 M40,30 L26,36 M96,40 L100,22" '
               'stroke="#1f5d96" stroke-width="5" opacity="0.55"/>'
               '<path d="M64,64 L40,30 L46,14 M64,64 L96,40 L118,46 M64,64 L70,100 L58,120 M64,64 L24,74 L8,70" stroke="#fff" stroke-width="2"/></g>')


def map_node():
    return svg(256, 170, shadow(128, 150, 110, 12, 0.3)
               + '<path d="M18,70 C30,40 226,40 238,70 L222,120 C180,148 76,148 34,120 Z" fill="url(#side)" stroke="#2a6fb0" stroke-width="5" stroke-linejoin="round"/>'
               '<ellipse cx="128" cy="70" rx="110" ry="36" fill="url(#top)" stroke="#3b6e99" stroke-width="5"/>'
               '<path d="M50,64 C80,50 120,48 160,54" stroke="#fff" stroke-width="7" fill="none" stroke-linecap="round"/>'
               + "".join(f'<path d="M{x},96 L{x+8},{96+h} L{x+16},96" fill="#e9fbff" stroke="#6fb3e0" stroke-width="1.5"/>'
                         for x, h in ((54, 22), (100, 30), (150, 18), (190, 26))),
               lg("side", [(0, "#9fd8f4"), (1, "#3f86c4")]) + lg("top", [(0, "#ffffff"), (1, "#dcecf8")]))


def ui_pill():
    # 9-slice glass pill for HUD counters, border 30px
    return svg(128, 72, '<rect x="2" y="2" width="124" height="68" rx="34" fill="#0a1d3d" opacity="0.55"/>'
               '<rect x="2" y="2" width="124" height="68" rx="34" fill="none" stroke="#bfe6ff" stroke-width="3" opacity="0.55"/>'
               '<rect x="16" y="8" width="96" height="14" rx="7" fill="#fff" opacity="0.12"/>')


def ui_bar():
    # 9-slice white rounded bar (tinted in code), border 16px
    return svg(64, 32, '<rect x="1" y="1" width="62" height="30" rx="15" fill="#ffffff"/>')


def ribbon():
    return svg(640, 150,
               '<path d="M10,60 L90,60 L70,95 L90,130 L10,130 L30,95 Z" fill="#b8401c"/>'
               '<path d="M630,60 L550,60 L570,95 L550,130 L630,130 L610,95 Z" fill="#b8401c"/>'
               '<path d="M70,40 L570,40 L570,120 L70,120 Z" fill="url(#rb)" stroke="#8a2c10" stroke-width="5" stroke-linejoin="round"/>'
               '<path d="M70,120 L90,140 L90,120 Z M570,120 L550,140 L550,120 Z" fill="#7a250c"/>'
               '<rect x="90" y="50" width="460" height="16" rx="8" fill="#fff" opacity="0.3"/>',
               lg("rb", [(0, "#ffb07a"), (0.5, "#f26b3a"), (1, "#c94a20")]))


def ground_fill():
    return svg(512, 512, '<rect width="512" height="512" fill="url(#deep)"/>'
               + "".join(f'<path d="M{x},0 L{x+14},512" stroke="#7fb9e2" stroke-width="8" opacity="0.25"/>'
                         for x in (40, 170, 300, 430)),
               lg("deep", [(0, "#2c6aa8"), (1, "#1a4a82")]))


def aurora():
    defs = (lg("au", [(0, "#5effc0", 0), (0.25, "#5effc0", 0.55), (0.6, "#3fd6e0", 0.35), (1, "#9b7bff", 0)], 0, 0, 0, 1)
            + lg("auf", [(0, "#fff", 0), (0.2, "#fff", 1), (0.8, "#fff", 1), (1, "#fff", 0)], 0, 0, 1, 0))
    curtain = ('<path d="M0,180 C200,80 380,260 600,150 C800,60 920,200 1024,130 L1024,512 L0,512 Z" fill="url(#au)"/>')
    rays = "".join(f'<rect x="{x}" y="120" width="10" height="320" fill="#b8ffe6" opacity="0.18"/>'
                   for x in range(40, 1000, 46))
    return svg(1024, 512, f'<g mask="url(#m)" filter="url(#soft)">{curtain}{rays}</g>',
               defs + '<mask id="m"><rect width="1024" height="512" fill="url(#auf)"/></mask>')


# ---------------------------------------------------------------- particles

def snowflake():
    return svg(32, 32, '<circle cx="16" cy="16" r="9" fill="#fff" filter="url(#soft)"/><circle cx="16" cy="16" r="6" fill="#fff"/>')


def snow_puff():
    return svg(96, 80, '<g filter="url(#soft)">'
               '<circle cx="30" cy="48" r="22" fill="#fff"/><circle cx="52" cy="34" r="26" fill="#fff"/>'
               '<circle cx="70" cy="50" r="20" fill="#fff"/><ellipse cx="48" cy="58" rx="36" ry="14" fill="#e3f1fb"/></g>')


def powder():
    return svg(32, 32, '<circle cx="16" cy="16" r="12" fill="#f2f9ff" filter="url(#soft)"/>')


def ice_shard():
    return svg(48, 48, f'<polygon points="6,42 18,4 42,30" fill="url(#sh)" stroke="{ICE_DEEP}" stroke-width="3" stroke-linejoin="round"/>'
               '<path d="M16,30 L20,12" stroke="#fff" stroke-width="3" stroke-linecap="round"/>',
               lg("sh", [(0, ICE_HI), (1, ICE_SH)], 0, 0, 1, 1))


def spark():
    return svg(32, 32, '<path d="M16,2 L19,13 L30,16 L19,19 L16,30 L13,19 L2,16 L13,13 Z" fill="#fff6c4"/>')


# ---------------------------------------------------------------- UI

def star(filled):
    pts = "64,8 80,46 120,48 89,74 100,114 64,92 28,114 39,74 8,48 48,46"
    if not filled:
        return svg(128, 128, f'<polygon points="{pts}" fill="#1f3558" stroke="#0e1c33" stroke-width="6" stroke-linejoin="round" opacity="0.9"/>')
    return svg(128, 128,
               f'<polygon points="{pts}" fill="url(#st)" stroke="#a35a00" stroke-width="5" stroke-linejoin="round"/>'
               f'<polygon points="64,22 74,48 60,54" fill="#fff" opacity="0.8"/>',
               rg("st", [(0, GOLD_HI), (0.6, GOLD), (1, GOLD_SH)], 0.4, 0.3, 0.8))


def gem():
    return svg(64, 64, f'<polygon points="32,4 58,24 32,60 6,24" fill="url(#g)" stroke="{ICE_DEEP}" stroke-width="3.5" stroke-linejoin="round"/>'
               '<polygon points="32,4 44,24 32,24 20,24" fill="#fff" opacity="0.6"/>',
               lg("g", [(0, ICE_HI), (1, ICE_SH)], 0, 0, 1, 1))


def button(hi, mid, lo):
    # 9-slice: 128x128, border 44px
    return svg(128, 128,
               f'<rect x="4" y="12" width="120" height="112" rx="36" fill="{lo}"/>'
               f'<rect x="4" y="4" width="120" height="110" rx="36" fill="url(#b)" stroke="{lo}" stroke-width="3"/>'
               f'<rect x="18" y="12" width="92" height="30" rx="15" fill="#fff" opacity="0.38"/>',
               lg("b", [(0, hi), (0.55, mid), (1, lo)]))


def panel():
    return svg(128, 128,
               f'<rect x="4" y="8" width="120" height="116" rx="40" fill="#0a2240" opacity="0.25" filter="url(#soft)"/>'
               f'<rect x="4" y="4" width="120" height="116" rx="40" fill="url(#p)" stroke="#8fc1e3" stroke-width="4"/>'
               f'<rect x="14" y="10" width="100" height="26" rx="13" fill="#fff" opacity="0.7"/>',
               lg("p", [(0, "#ffffff"), (0.6, "#eef8ff"), (1, "#cfe8f8")]))


GLYPHS = {
    "music": '<path d="M50,96 V34 L96,24 V86" fill="none" stroke="{i}" stroke-width="10" stroke-linejoin="round"/><circle cx="38" cy="96" r="14" fill="{i}"/><circle cx="84" cy="86" r="14" fill="{i}"/>',
    "help": '<path d="M44,50 a20,20 0 1 1 30,18 c-8,5 -10,10 -10,18" fill="none" stroke="{i}" stroke-width="11" stroke-linecap="round"/><circle cx="64" cy="104" r="8" fill="{i}"/>',
    "play": '<polygon points="44,30 100,64 44,98" fill="#fff" stroke="{i}" stroke-width="5" stroke-linejoin="round"/>',
    "pause": '<rect x="38" y="30" width="18" height="68" rx="6" fill="#fff" stroke="{i}" stroke-width="4"/>'
             '<rect x="72" y="30" width="18" height="68" rx="6" fill="#fff" stroke="{i}" stroke-width="4"/>',
    "retry": '<path d="M92,52 A30,30 0 1 0 94,76" fill="none" stroke="{i}" stroke-width="15" stroke-linecap="round"/>'
             '<path d="M92,52 A30,30 0 1 0 94,76" fill="none" stroke="#fff" stroke-width="8" stroke-linecap="round"/>'
             '<polygon points="80,30 104,56 74,62" fill="#fff" stroke="{i}" stroke-width="4" stroke-linejoin="round"/>',
    "map": '<path d="M24,36 l26,-10 l28,10 l26,-10 v66 l-26,10 l-28,-10 l-26,10 Z" fill="#fff" stroke="{i}" '
           'stroke-width="5" stroke-linejoin="round"/><path d="M50,26 v66 M78,36 v66" stroke="{i}" stroke-width="4"/>',
    "shop": '<path d="M30,46 h68 l-6,52 h-56 Z" fill="#fff" stroke="{i}" stroke-width="5" stroke-linejoin="round"/>'
            '<path d="M46,46 v-8 a18,18 0 0 1 36,0 v8" fill="none" stroke="{i}" stroke-width="5"/>',
    "next": '<polygon points="34,30 74,64 34,98" fill="#fff" stroke="{i}" stroke-width="5" stroke-linejoin="round"/>'
            '<rect x="80" y="30" width="16" height="68" rx="5" fill="#fff" stroke="{i}" stroke-width="4"/>',
    "lock": '<rect x="34" y="56" width="60" height="46" rx="9" fill="#fff" stroke="{i}" stroke-width="5"/>'
            '<path d="M46,56 v-12 a18,18 0 0 1 36,0 v12" fill="none" stroke="{i}" stroke-width="6"/>',
    "sound": '<polygon points="28,50 46,50 70,28 70,100 46,78 28,78" fill="#fff" stroke="{i}" stroke-width="5" stroke-linejoin="round"/>'
             '<path d="M84,46 q14,18 0,36 M94,34 q26,30 0,60" fill="none" stroke="#fff" stroke-width="6" stroke-linecap="round"/>',
    "back": '<polygon points="94,30 54,64 94,98" fill="#fff" stroke="{i}" stroke-width="5" stroke-linejoin="round"/>'
            '<rect x="32" y="30" width="16" height="68" rx="5" fill="#fff" stroke="{i}" stroke-width="4"/>',
    "tap": '<circle cx="64" cy="50" r="30" fill="none" stroke="#fff" stroke-width="6" opacity="0.8"/>'
           '<path d="M58,50 v44 q0,10 12,10 h12 q12,0 14,-14 l4,-22 q0,-8 -8,-8 h-22 v-10 q0,-8 -6,-8 q-6,0 -6,8 Z" '
           'fill="#fff" stroke="{i}" stroke-width="4" stroke-linejoin="round"/>',
}


def gear():
    teeth = "".join(f'<rect x="57" y="20" width="14" height="18" rx="4" fill="#fff" stroke="{INK}" stroke-width="4" '
                    f'transform="rotate({a} 64 64)"/>' for a in range(0, 360, 45))
    return svg(128, 128, teeth + f'<circle cx="64" cy="64" r="28" fill="#fff" stroke="{INK}" stroke-width="5"/>'
               f'<circle cx="64" cy="64" r="10" fill="{INK}"/>')


def icon_ball():
    trail = "".join(f'<path d="M{30+k*6},{196-k*30} q20,-6 34,4" fill="none" stroke="#9fe0fb" stroke-width="8" '
                    f'stroke-linecap="round" opacity="{0.9-k*0.25}"/>' for k in range(3))
    inner = pup_ball().split("</defs>", 1)[1].rsplit("</svg>", 1)[0]
    return svg(256, 256, trail + f'<g transform="translate(40 -10) scale(0.82)">{inner}</g>', FUR + GEAR_DEFS)


def icon_pod():
    inner = pup().split("</defs>", 1)[1].rsplit("</svg>", 1)[0]
    return svg(256, 256, "".join(f'<g transform="translate({x} {y}) scale(0.55)">{inner}</g>'
                                 for x, y in ((-6, 20), (110, 50), (30, 110))), FUR + GEAR_DEFS)


def bubble_ninja():
    """Speech bubble with a tiny ninja pup inside: shown while a Ninja pup hangs mid-air before the straight drop."""
    return svg(160, 140,
               f'<path d="M80,8 a66,56 0 1 0 0,112 l-6,16 l22,-16 a66,56 0 0 0 -16,-112 Z" fill="#fff8e6" stroke="#f26b3a" stroke-width="6" stroke-linejoin="round"/>'
               f'<ellipse cx="80" cy="66" rx="24" ry="30" fill="#1b2a49"/><ellipse cx="80" cy="72" rx="14" ry="18" fill="#fff"/>'
               f'<rect x="56" y="50" width="48" height="14" rx="6" fill="#d9532a"/><path d="M104,54 l22,-10 l-14,16" fill="#d9532a"/>'
               + eye(72, 57, 4) + eye(88, 57, 4))


def icon_ninja():
    inner = pup().split("</defs>", 1)[1].rsplit("</svg>", 1)[0]
    return svg(256, 256, f'<g transform="translate(28 20) scale(0.8)">{inner}</g>'
               f'<rect x="92" y="72" width="104" height="26" rx="10" fill="#d9532a" stroke="{INK}" stroke-width="4"/>'
               f'<path d="M196,80 l40,-18 l-26,34" fill="#d9532a" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>', FUR + GEAR_DEFS)


def icon_blaze():
    inner = pup_ball().split("</defs>", 1)[1].rsplit("</svg>", 1)[0]
    flames = "".join(f'<path d="M{x},{y} c-18,-30 10,-44 6,-70 c22,20 30,44 8,74 Z" fill="{c}" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>'
                     for x, y, c in ((70, 150, "#ff8a3d"), (112, 130, "#ffb347"), (150, 160, "#ff6b5b")))
    return svg(256, 256, flames + f'<g transform="translate(70 60) scale(0.7)">{inner}</g>', FUR + GEAR_DEFS)


def badge_perfect():
    """Gold rosette with a star: the Perfect badge on result cards and map nodes."""
    import math
    pts = " ".join(f"{64 + (60 if k % 2 == 0 else 50) * math.cos(k * math.pi / 12):.1f},{64 + (60 if k % 2 == 0 else 50) * math.sin(k * math.pi / 12):.1f}"
                   for k in range(24))
    star_pts = "64,24 75,51 104,53 82,71 89,99 64,84 39,99 46,71 24,53 53,51"
    return svg(128, 128, f'<polygon points="{pts}" fill="#ffb347" stroke="#c98f00" stroke-width="4" stroke-linejoin="round"/>'
               f'<circle cx="64" cy="64" r="40" fill="#ffd66e" stroke="#c98f00" stroke-width="3"/>'
               f'<polygon points="{star_pts}" fill="url(#st)" stroke="#a35a00" stroke-width="4" stroke-linejoin="round"/>',
               rg("st", [(0, GOLD_HI), (0.6, GOLD), (1, GOLD_SH)], 0.4, 0.3, 0.8))


def wave_cover():
    """Screen-wipe water: a tall panel with a wavy top edge (tiles horizontally); rises to cover the screen between scenes."""
    top = "M0,60 C60,20 120,100 180,60 C240,20 300,100 360,60 C420,20 480,100 512,60"
    return svg(512, 1024, f'<path d="{top} L512,1024 L0,1024 Z" fill="url(#wv)"/>'
               f'<path d="{top}" fill="none" stroke="#e6f7ff" stroke-width="10" stroke-linecap="round"/>'
               '<path d="M40,140 h70 M300,200 h90 M160,320 h60 M400,420 h80" stroke="#bfe8fb" stroke-width="6" stroke-linecap="round" opacity="0.5"/>',
               lg("wv", [(0, "#5fb0dd"), (0.4, "#2f6fb0"), (1, "#173e78")]))


ASSETS = {
    # hero sprites (pup, pup_ball, pup_dizzy, pup_<anim>_<i>) come from Tools/slice_sheets.py and are never deleted here
    "fg_snow": fg_snow(),
    "vignette": vignette(),
    "sign": sign(),
    "rope": rope(),
    "map_bg": map_bg(),
    "cave_wall": cave_wall(),
    "rock_cobble": rock_cobble(),
    "rock_top": rock_top(),
    "rock_ramp": rock_ramp(),
    "bg_dusk": bg_dusk(),
    "bg_floes": bg_floes(),
    "icicles": icicles(),
    "whacker_base": whacker_base(),
    "whacker_arm": whacker_arm(),
    "snowball": snowball(),
    "beam": beam(),
    "crack": crack(),
    "ui_pill": ui_pill(),
    "ui_bar": ui_bar(),
    "ribbon": ribbon(),
    "pup_spiky": owl_spiky(),
    "block_snow": block_snow(),
    "block_ice": block_ice(),
    "block_stone": block_stone(),
    "bg_sky": bg_sky(),
    "bg_far": bg_far(),
    "bg_mid": bg_mid(),
    "ground": ground(),
    "ground_fill": ground_fill(),
    "aurora": aurora(),
    "snowflake": snowflake(),
    "snow_puff": snow_puff(),
    "powder": powder(),
    "ice_shard": ice_shard(),
    "spark": spark(),
    "star_full": star(True),
    "star_empty": star(False),
    "gem": gem(),
    "ui_button": button(TEAL_HI, TEAL, TEAL_SH),
    "ui_button_warm": button(WARM_HI, WARM, WARM_SH),
    "ui_button_gold": button(GOLD_HI, GOLD, GOLD_SH),
    "ui_panel": panel(),
    "icon_spiky": owl_spiky(),
    "icon_ball": icon_ball(),
    "icon_pod": icon_pod(),
    "icon_ninja": icon_ninja(),
    "badge_perfect": badge_perfect(),
    "bumper": bumper(),
    "wave_cover": wave_cover(),
    "crate_tnt": crate_tnt(),
    "button_up": floor_button(False),
    "button_down": floor_button(True),
    "fish": fish(),
    "cage": cage(),
    "cage_open": cage(True),
    "icon_blaze": icon_blaze(),
    "bubble_ninja": bubble_ninja(),
    "glyph_gear": gear(),
}
ASSETS.update({f"glyph_{k}": svg(128, 128, v.replace("{i}", INK)) for k, v in GLYPHS.items()})
# Bear frames come from the ChatGPT sheets once they are sliced (Assets/Art/Reference/bear_sheet*.png); only the rows
# whose sheet is still missing are generated here as placeholders, and the hat is always code-drawn.
_REF = ROOT / "Assets/Art/Reference"
_SHEET_ROWS = {"bear_sheet.png": ("idle", "taunt", "dizzy", "ko"), "bear_sheet_b.png": ("eat", "sleep"), "bear_sheet_c.png": ("flex", "laugh", "scratch", "juggle")}
_DRAWN = {pose for sheet, poses in _SHEET_ROWS.items() if (_REF / sheet).exists() for pose in poses}
ASSETS.update({f"bear_{pose}_{i}": bear_frame(pose, i) for pose, n in BEAR_ANIMS if pose not in _DRAWN for i in range(n)})
if "idle" not in _DRAWN:
    ASSETS.update({"bear": bear_frame("idle", 0), "bear_helmet": bear_frame("idle", 0, hat=True)})
ASSETS["bear_hat"] = bear_hat()


def painted_names():
    """Sprites cut from ChatGPT paintings (Tools/cut_props.py, Tools/prep_backgrounds.py). The painting wins: the SVG
    here is only a stand-in drawn while that painting does not exist yet, and its PNG is never deleted."""
    import sys
    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    from cut_props import REF, SHEETS
    from prep_backgrounds import PAINTINGS
    # A name counts as painted only once its PNG was actually produced (a failed cut leaves the stand-in in place).
    names = {n for sheet, ns in SHEETS.items() if (REF / sheet).exists() for n in ns if n}
    names |= {name for name, src in PAINTINGS.items() if (REF / src).exists()}
    # Existence only, never mtime: a clone or copy reorders mtimes and would let a stand-in overwrite the painting.
    return {n for n in names if (PNG_DIR / f"{n}.png").exists()}


def cut_names():
    """Every name the cut/prep tools can produce, whether or not its sheet is in Reference right now."""
    from cut_props import SHEETS
    from prep_backgrounds import PAINTINGS
    return {n for ns in SHEETS.values() for n in ns if n} | set(PAINTINGS)


def main():
    if shutil.which("rsvg-convert") is None:
        sys.exit("rsvg-convert not found (brew install librsvg); nothing was changed")
    painted = painted_names()
    keep = cut_names()
    redraw_frames = "--stand-ins" in sys.argv
    SVG_DIR.mkdir(parents=True, exist_ok=True)
    PNG_DIR.mkdir(parents=True, exist_ok=True)
    for name, content in ASSETS.items():
        if name in painted:
            for stale in (SVG_DIR / f"{name}.svg", SVG_DIR / f"{name}.svg.meta"):
                stale.unlink(missing_ok=True)   # its painting exists; the stand-in is retired
            continue
        if name in keep and (PNG_DIR / f"{name}.png").exists():
            continue   # a cut tool makes this name: its PNG may be the painting even when the sheet left Reference
            # (also under --stand-ins: delete the PNG to have the stand-in drawn again)
        if _sliced(name) and (PNG_DIR / f"{name}.png").exists() and not redraw_frames:
            continue   # may be a frame cut from a ChatGPT sheet; --stand-ins redraws the placeholders on purpose
        src = SVG_DIR / f"{name}.svg"
        src.write_text(content)
        subprocess.run(["rsvg-convert", str(src), "-o", str(PNG_DIR / f"{name}.png")], check=True)
    # Retire old stand-ins last, so a failed export never leaves deletions behind. Only names this script drew (their
    # SVG exists) are touched: PNGs from the cut/prep/slice tools have no SVG here.
    stale = [svg.stem for svg in SVG_DIR.glob("*.svg") if svg.stem not in ASSETS]
    for old in [SVG_DIR / f"{n}.svg" for n in stale] + [PNG_DIR / f"{n}.png" for n in stale]:
        if old.exists() and old.stem not in painted and old.stem not in keep and not _sliced(old.stem):
            old.unlink()
            meta = old.with_suffix(old.suffix + ".meta")
            if meta.exists():
                meta.unlink()
    print(f"exported {len(ASSETS)} sprites")


def _sliced(name):
    """Sheet frames and their aliases (slice_sheets.py) are never deleted here; `pupil*`/`bearing*` are not frames."""
    return name in ("pup", "bear") or name.startswith(("pup_", "bear_"))


if __name__ == "__main__":
    main()

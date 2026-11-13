#!/usr/bin/env python3
"""Frame-by-frame art for the hero (harp seal pup, side view, walks on two feet).

Every frame is one complete drawing: back limbs, then the head+body silhouette, then front limbs, then a
fur-coloured patch that hides the joint outlines, then face and gear. Fur uses one userSpaceOnUse gradient
so shapes blend into a single body.

Run: python3 Tools/make_pup_frames.py  -> Assets/Resources/Art/pup_<anim>_<n>.png (+ SVG in Assets/Art/Source)
"""
import math
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parent.parent
SVG_DIR = ROOT / "Assets/Art/Source"
PNG_DIR = ROOT / "Assets/Resources/Art"
W, H = 300, 340
INK = "#16304f"
OW = 5

DEFS = (
    '<defs>'
    '<radialGradient id="fur" gradientUnits="userSpaceOnUse" cx="140" cy="120" r="220">'
    '<stop offset="0" stop-color="#ffffff"/><stop offset="0.55" stop-color="#eef6fc"/><stop offset="1" stop-color="#b9d3e8"/></radialGradient>'
    '<linearGradient id="knit" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ff6b5b"/><stop offset="1" stop-color="#c7352b"/></linearGradient>'
    '<linearGradient id="scarf" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#3fd6c8"/><stop offset="1" stop-color="#16928c"/></linearGradient>'
    '<radialGradient id="lens" cx="0.35" cy="0.3" r="0.8"><stop offset="0" stop-color="#e9fbff"/><stop offset="0.6" stop-color="#7fc8f2"/>'
    '<stop offset="1" stop-color="#2f6fa8"/></radialGradient>'
    '<filter id="blur" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="6"/></filter>'
    '</defs>')


def rot(px, py, cx, cy, deg):
    a = math.radians(deg)
    dx, dy = px - cx, py - cy
    return cx + dx * math.cos(a) - dy * math.sin(a), cy + dx * math.sin(a) + dy * math.cos(a)


def arm(sx, sy, deg, length=74, back=False):
    """Front flipper used as an arm: tapered paddle from the shoulder; deg 0 = hanging down, + = forward/up."""
    fill = "#dbe8f4" if back else "url(#fur)"
    pts = [(sx - 13, sy), (sx - 15, sy + length * 0.55), (sx - 6, sy + length), (sx + 10, sy + length * 0.98),
           (sx + 16, sy + length * 0.5), (sx + 13, sy)]
    pts = [rot(x, y, sx, sy, -deg) for x, y in pts]
    d = (f"M{pts[0][0]:.1f},{pts[0][1]:.1f} C{pts[1][0]:.1f},{pts[1][1]:.1f} {pts[1][0]:.1f},{pts[1][1]:.1f} {pts[2][0]:.1f},{pts[2][1]:.1f} "
         f"Q{(pts[2][0]+pts[3][0])/2:.1f},{(pts[2][1]+pts[3][1])/2:.1f} {pts[3][0]:.1f},{pts[3][1]:.1f} "
         f"C{pts[4][0]:.1f},{pts[4][1]:.1f} {pts[4][0]:.1f},{pts[4][1]:.1f} {pts[5][0]:.1f},{pts[5][1]:.1f} Z")
    tip = rot(sx, sy + length * 0.92, sx, sy, -deg)
    claws = "".join(f'<circle cx="{tip[0] + k * 4:.1f}" cy="{tip[1]:.1f}" r="1.6" fill="#7d93ab"/>' for k in (-1, 0, 1))
    return f'<path d="{d}" fill="{fill}" stroke="{INK}" stroke-width="{OW-1}" stroke-linejoin="round"/>' + claws


def leg(hx, hy, deg, lift=0, back=False):
    """Short leg ending in a rear flipper used as a foot (points forward, +x)."""
    fill = "#dbe8f4" if back else "url(#fur)"
    foot = "#c9dbea" if back else "#dbe9f5"
    knee = rot(hx, hy + 30, hx, hy, -deg)
    ax, ay = knee[0], knee[1] - lift
    shin = (f'<path d="M{hx-12},{hy} L{hx+12},{hy} L{ax+11:.1f},{ay:.1f} L{ax-11:.1f},{ay:.1f} Z" fill="{fill}" '
            f'stroke="{INK}" stroke-width="{OW-1}" stroke-linejoin="round"/>')
    f = (f'<path d="M{ax-14:.1f},{ay-4:.1f} C{ax-4:.1f},{ay-12:.1f} {ax+30:.1f},{ay-10:.1f} {ax+44:.1f},{ay+2:.1f} '
         f'C{ax+52:.1f},{ay+10:.1f} {ax+44:.1f},{ay+20:.1f} {ax+30:.1f},{ay+18:.1f} C{ax+10:.1f},{ay+20:.1f} {ax-10:.1f},{ay+18:.1f} '
         f'{ax-16:.1f},{ay+10:.1f} C{ax-20:.1f},{ay+4:.1f} {ax-18:.1f},{ay-2:.1f} {ax-14:.1f},{ay-4:.1f} Z" fill="{foot}" stroke="{INK}" '
         f'stroke-width="{OW-1}" stroke-linejoin="round"/>'
         f'<path d="M{ax+20:.1f},{ay+4:.1f} l14,3 M{ax+18:.1f},{ay+11:.1f} l16,2" stroke="#8aa0b8" stroke-width="2.4" stroke-linecap="round"/>')
    return shin + f


def eye(cx, cy, r, mood):
    if mood == "blink":
        return f'<path d="M{cx-r},{cy+2} q{r},{r*0.8} {2*r},0" fill="none" stroke="#0d1726" stroke-width="4.5" stroke-linecap="round"/>'
    if mood == "dizzy":
        return (f'<path d="M{cx},{cy} m-{r*0.8},0 a{r*0.8},{r*0.8} 0 1 1 {r*1.6},0 a{r*0.5},{r*0.5} 0 1 1 -{r},0" '
                f'fill="none" stroke="#0d1726" stroke-width="3.5" stroke-linecap="round"/>')
    return (f'<ellipse cx="{cx}" cy="{cy}" rx="{r}" ry="{r*1.18}" fill="#0d1726"/>'
            f'<ellipse cx="{cx+1}" cy="{cy+r*0.45}" rx="{r*0.6}" ry="{r*0.45}" fill="#27406b"/>'
            f'<circle cx="{cx-r*0.32}" cy="{cy-r*0.45}" r="{r*0.38}" fill="#fff"/>'
            f'<circle cx="{cx+r*0.38}" cy="{cy+r*0.3}" r="{r*0.15}" fill="#fff"/>')


ROPE_X = 214   # rope line through the hang frames (hands and knot sit on it); must match PupRig/LaunchPhase


def rope_layer(oy):
    knot = (f'<ellipse cx="{ROPE_X}" cy="{318+oy}" rx="20" ry="13" fill="#c9965e" stroke="#5a3a1e" stroke-width="3"/>'
            f'<path d="M{ROPE_X-14},{314+oy} q14,8 28,0 M{ROPE_X-12},{322+oy} q12,7 24,0" stroke="#7a5230" stroke-width="2.5" fill="none"/>')
    rope = (f'<rect x="{ROPE_X-6}" y="0" width="12" height="{318+oy}" rx="6" fill="#c9965e" stroke="#5a3a1e" stroke-width="2"/>'
            + "".join(f'<path d="M{ROPE_X-6},{y} l12,7" stroke="#7a5230" stroke-width="2.5"/>' for y in range(4, 312, 12)))
    return rope, knot


def pup_frame(arm_f=0, arm_b=0, leg_f=0, leg_b=0, lift_f=0, lift_b=0, bob=0, tilt=0, mood="normal",
              scarf_flap=0, arm_len=74, shadow=True, rope=False):
    """One complete drawing of the pup, facing right."""
    oy = -bob
    hx, hy, hr = 178, 108 + oy, 70              # head
    bx, by, brx, bry = 150, 216 + oy, 76, 80     # body
    shoulder_f, shoulder_b = (186, 182 + oy), (140, 178 + oy)
    hip_f, hip_b = (170, 272 + oy), (134, 270 + oy)

    back = arm(*shoulder_b, arm_b, arm_len, back=True) + leg(*hip_b, leg_b, lift_b, back=True)
    tail = (f'<path d="M84,{232+oy} C60,{226+oy} 46,{240+oy} 44,{252+oy} C56,{254+oy} 70,{250+oy} 86,{246+oy} Z" fill="url(#fur)" '
            f'stroke="{INK}" stroke-width="{OW-1}" stroke-linejoin="round"/>')
    silhouette = (f'<ellipse cx="{bx}" cy="{by}" rx="{brx}" ry="{bry}" fill="url(#fur)" stroke="{INK}" stroke-width="{OW}"/>'
                  f'<circle cx="{hx}" cy="{hy}" r="{hr}" fill="url(#fur)" stroke="{INK}" stroke-width="{OW}"/>'
                  # seam patch: blends head into body (no neck line)
                  f'<ellipse cx="{(hx+bx)/2+6:.0f}" cy="{(hy+by)/2+6:.0f}" rx="58" ry="40" fill="url(#fur)"/>'
                  f'<path d="M{bx-46},{by+10} C{bx-30},{by+60} {bx+40},{by+70} {bx+66},{by+18}" fill="none" stroke="#cfe0ee" stroke-width="12" '
                  f'stroke-linecap="round" opacity="0.8"/>'
                  + "".join(f'<path d="M{x},{y+oy} q5,-7 11,0" fill="none" stroke="#c6dbec" stroke-width="2.5" stroke-linecap="round"/>'
                            for x, y in ((112, 190), (128, 236), (104, 252), (170, 226), (150, 260), (138, 208), (196, 70), (150, 92)))
                  + f'<ellipse cx="{hx-28}" cy="{hy-38}" rx="26" ry="10" fill="#fff" opacity="0.85" transform="rotate(-25 {hx-28} {hy-38})"/>')
    front = leg(*hip_f, leg_f, lift_f) + arm(*shoulder_f, arm_f, arm_len)
    # joint patches hide the outline where front limbs meet the body
    patches = (f'<circle cx="{shoulder_f[0]}" cy="{shoulder_f[1]+4}" r="11" fill="url(#fur)"/>'
               f'<rect x="{hip_f[0]-10}" y="{hip_f[1]-12}" width="20" height="14" fill="url(#fur)"/>')

    # face (3/4 view: near eye big, far eye smaller)
    fx, fy = hx + 30, hy + 4
    face = (f'<ellipse cx="{fx-34}" cy="{fy+18}" rx="10" ry="6" fill="#ff9fc0" opacity="0.6"/>'
            + eye(fx - 14, fy - 6, 11, mood) + eye(fx + 18, fy - 4, 13, mood)
            + f'<ellipse cx="{fx+26}" cy="{fy+22}" rx="14" ry="10" fill="#ffffff" stroke="#d3e3f0" stroke-width="1.5"/>'
            + f'<path d="M{fx+30},{fy+12} q9,-3 12,4 q-2,7 -8,8 q-6,-2 -4,-12 Z" fill="#2b3240"/>'
            + "".join(f'<circle cx="{fx+dx}" cy="{fy+dy}" r="1.6" fill="#8aa0b8"/>' for dx, dy in ((22, 22), (28, 27), (34, 24)))
            + f'<path d="M{fx+30},{fy+26} l22,-2 M{fx+30},{fy+30} l22,4" stroke="#a9bdd1" stroke-width="1.8" stroke-linecap="round"/>')
    if mood == "happy":
        face += f'<path d="M{fx+14},{fy+30} q10,14 22,0 Z" fill="#c24a5a" stroke="#0d1726" stroke-width="3" stroke-linejoin="round"/>'
    elif mood == "scared":
        face += f'<ellipse cx="{fx+22}" cy="{fy+34}" rx="7" ry="9" fill="#c24a5a" stroke="#0d1726" stroke-width="3"/>'
    elif mood != "dizzy":
        face += f'<path d="M{fx+16},{fy+32} q8,7 16,0" fill="none" stroke="#0d1726" stroke-width="3" stroke-linecap="round"/>'

    # beanie with goggles, pompom trailing back
    cx, cy = hx - 4, hy - 44
    hat = (f'<g transform="rotate(-14 {cx} {cy})">'
           f'<path d="M{cx-62},{cy+8} C{cx-62},{cy-66} {cx+62},{cy-66} {cx+62},{cy+8} Z" fill="url(#knit)" stroke="{INK}" stroke-width="4"/>'
           + "".join(f'<path d="M{cx-44},{cy-14-k*16} q44,-10 88,0" fill="none" stroke="#ff8f80" stroke-width="2.5" opacity="0.6"/>' for k in range(3))
           + f'<rect x="{cx-66}" y="{cy}" width="132" height="20" rx="9" fill="#e2463a" stroke="{INK}" stroke-width="4"/>'
           + "".join(f'<path d="M{cx-58+i*13},{cy+3} v14" stroke="#a82a22" stroke-width="2.5" opacity="0.7"/>' for i in range(10))
           + f'<circle cx="{cx-50}" cy="{cy-50}" r="15" fill="#ffffff" stroke="#9cb4ca" stroke-width="2.5"/>'
           + f'<path d="M{cx-40},{cy-14} C{cx-10},{cy-22} {cx+30},{cy-22} {cx+60},{cy-12}" fill="none" stroke="#5a3a1e" stroke-width="6"/>'
           + "".join(f'<circle cx="{x}" cy="{cy-18}" r="13" fill="url(#lens)" stroke="#b07a3a" stroke-width="5"/>'
                     f'<path d="M{x-6},{cy-22} q4,-4 8,-2" stroke="#fff" stroke-width="2.5" fill="none" stroke-linecap="round"/>'
                     for x in (cx + 18, cx + 46))
           + '</g>')

    # scarf around the neck, tail streaming back
    sx, sy = hx - 10, hy + 62
    scarf = (f'<path d="M{sx-50},{sy-6} C{sx-20},{sy+10} {sx+40},{sy+10} {sx+60},{sy-8} L{sx+62},{sy+12} C{sx+40},{sy+30} {sx-20},{sy+30} {sx-52},{sy+14} Z" '
             f'fill="url(#scarf)" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>'
             f'<path d="M{sx-44},{sy+4} C{sx-60},{sy+6+scarf_flap*0.5} {sx-66},{sy+20+scarf_flap} {sx-80},{sy+24+scarf_flap} '
             f'C{sx-84},{sy+36+scarf_flap} {sx-72},{sy+44+scarf_flap} {sx-62},{sy+40+scarf_flap} C{sx-58},{sy+30} {sx-50},{sy+22} {sx-40},{sy+20} Z" '
             f'fill="url(#scarf)" stroke="{INK}" stroke-width="3.5" stroke-linejoin="round"/>'
             f'<path d="M{sx-80},{sy+36+scarf_flap} l-8,4 M{sx-74},{sy+42+scarf_flap} l-6,7" stroke="#16928c" stroke-width="3" stroke-linecap="round"/>')

    if rope:
        r_line, r_knot = rope_layer(oy)
        body = back + tail + silhouette + scarf + r_line + r_knot + front + patches + face + hat  # rope in front of the belly, hugged by the front flipper
    else:
        body = back + tail + silhouette + scarf + front + patches + face + hat
    sh = f'<ellipse cx="160" cy="328" rx="70" ry="7" fill="#0a2240" opacity="0.25" filter="url(#blur)"/>' if shadow else ""
    g = f'<g transform="rotate({tilt} 160 220)">{body}</g>'
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">{DEFS}{sh}{g}</svg>'


def walk_frames(n=6):
    out = []
    for i in range(n):
        ph = 2 * math.pi * i / n
        s, c = math.sin(ph), math.cos(ph)
        out.append(pup_frame(arm_f=-30 * c, arm_b=30 * c, leg_f=34 * c, leg_b=-34 * c,
                             lift_f=max(0, s) * 18, lift_b=max(0, -s) * 18, bob=abs(s) * 8, tilt=4 * c,
                             mood="blink" if i == 3 else "normal", scarf_flap=6 * s))
    return out


def hang_frames(n=4):
    """Hugging the rope with both flippers, feet on the knot at the rope's end; leaning and kicking to pump."""
    out = []
    for i in range(n):
        ph = 2 * math.pi * i / n
        s = math.sin(ph)
        out.append(pup_frame(arm_f=58 + 6 * s, arm_b=74 + 6 * s, arm_len=70, leg_f=10 + 18 * s, leg_b=4 + 18 * s,
                             lift_f=max(0, s) * 8, bob=-4, tilt=-6 * s, mood="happy" if s > 0.2 else "normal",
                             scarf_flap=12 * s, shadow=False, rope=True))
    return out


ANIMS = {"walk": walk_frames, "hang": hang_frames}


def main():
    # slice_sheets.py cuts the same pup_walk_*/pup_hang_* names from the ChatGPT sheets; never clobber those.
    sheets = [s for s in ("pup_walk_sheet.png", "pup_sheet_a.png") if (ROOT / "Assets/Art/Reference" / s).exists()]
    if sheets:
        raise SystemExit(f"{', '.join(sheets)} exist: frames come from Tools/slice_sheets.py, nothing was written")
    SVG_DIR.mkdir(parents=True, exist_ok=True)
    PNG_DIR.mkdir(parents=True, exist_ok=True)
    for name, make in ANIMS.items():
        for i, frame in enumerate(make()):
            src = SVG_DIR / f"pup_{name}_{i}.svg"
            src.write_text(frame)
            subprocess.run(["rsvg-convert", str(src), "-o", str(PNG_DIR / f"pup_{name}_{i}.png")], check=True)
    print("frames done")


if __name__ == "__main__":
    main()

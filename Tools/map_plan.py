"""map_plan.py <out dir>: draws a layout plan per world: grey land, ONE black road (spline), brown bridges, S / F marks."""
import math, random, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter
W, H = 1672, 941
P = {
 'canyon':  [(.10,.85),(.30,.80),(.38,.55),(.15,.45),(.20,.20),(.45,.18),(.55,.45),(.62,.75),(.85,.72),(.90,.45),(.72,.35),(.85,.15)],
 'forest':  [(.08,.45),(.22,.18),(.48,.14),(.62,.38),(.40,.52),(.22,.80),(.50,.88),(.75,.72),(.92,.50),(.82,.18)],
 'harbour': [(.50,.88),(.25,.85),(.08,.65),(.22,.42),(.10,.20),(.35,.12),(.50,.35),(.65,.55),(.88,.70),(.92,.40),(.75,.28),(.85,.10)],
 'caves':   [(.10,.18),(.30,.25),(.20,.50),(.10,.78),(.32,.85),(.45,.60),(.50,.30),(.70,.15),(.90,.28),(.75,.50),(.62,.78),(.88,.82)],
 'volcano': [(.10,.85),(.35,.88),(.60,.80),(.85,.85),(.92,.55),(.80,.28),(.60,.13),(.35,.18),(.15,.35),(.26,.58),(.42,.52)],
 'blossom': [(.90,.80),(.72,.88),(.55,.70),(.75,.50),(.90,.30),(.68,.15),(.50,.30),(.38,.55),(.20,.80),(.08,.55),(.25,.38),(.12,.15)],
 'palace':  [(.10,.85),(.20,.55),(.10,.25),(.32,.15),(.42,.45),(.35,.78),(.58,.85),(.68,.55),(.58,.25),(.80,.15),(.92,.42),(.85,.72)],
 'tundra':  [(.08,.15),(.30,.12),(.55,.20),(.80,.12),(.92,.35),(.70,.45),(.45,.42),(.20,.40),(.08,.62),(.25,.85),(.50,.75),(.72,.85),(.90,.75)],
 'throne':  [(.10,.82),(.32,.88),(.52,.72),(.78,.84),(.92,.58),(.72,.40),(.45,.46),(.20,.52),(.09,.30),(.28,.13),(.52,.16)],
}
BRIDGES = {n: (0.36, 0.70) for n in P}

def spline(pts, n=40):
    q = [pts[0]] + pts + [pts[-1]]; out = []
    for i in range(1, len(q) - 2):
        p0, p1, p2, p3 = q[i - 1], q[i], q[i + 1], q[i + 2]
        for k in range(n):
            t = k / n
            out.append(tuple(0.5 * ((2 * p1[d]) + (-p0[d] + p2[d]) * t + (2 * p0[d] - 5 * p1[d] + 4 * p2[d] - p3[d]) * t * t + (-p0[d] + 3 * p1[d] - 3 * p2[d] + p3[d]) * t ** 3) for d in (0, 1)))
    return out + [pts[-1]]

def crosses(c):
    def ccw(a, b, d): return (d[1] - a[1]) * (b[0] - a[0]) > (b[1] - a[1]) * (d[0] - a[0])
    s = c[::8]
    for i in range(len(s) - 1):
        for j in range(i + 2, len(s) - 1):
            a, b, d, e = s[i], s[i + 1], s[j], s[j + 1]
            if ccw(a, d, e) != ccw(b, d, e) and ccw(a, b, d) != ccw(a, b, e): return True
    return False

def near(c):   # closest approach of two far-apart parts of the road, px
    s = c[::6]; best = 1e9
    for i in range(len(s)):
        for j in range(i + 25, len(s)):
            best = min(best, math.dist(s[i], s[j]))
    return best

font = ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc', 44)
for name, pts in P.items():
    random.seed(name)
    c = [(x * W, y * H) for x, y in spline(pts)]
    L = len(c); gaps = [(int(f * L) - 9, int(f * L) + 9) for f in BRIDGES[name]]
    land = Image.new('L', (W, H), 0); d = ImageDraw.Draw(land)
    for i in range(0, L, 5):
        if any(a - 6 <= i <= b + 6 for a, b in gaps): continue
        r = random.uniform(70, 100); ox, oy = random.uniform(-18, 18), random.uniform(-18, 18)
        d.ellipse([c[i][0] + ox - r, c[i][1] + oy - r, c[i][0] + ox + r, c[i][1] + oy + r], fill=255)
    land = land.filter(ImageFilter.GaussianBlur(10)).point(lambda v: 255 if v > 128 else 0)
    im = Image.new('RGB', (W, H), (255, 255, 255)); im.paste((200, 205, 210), mask=land)
    d = ImageDraw.Draw(im)
    for a, b in gaps: d.line(c[a:b + 1], fill=(170, 110, 50), width=58)
    d.line(c, fill=(20, 20, 20), width=16, joint='curve')
    for (x, y), t, col in ((c[0], 'S', (30, 150, 60)), (c[-1], 'F', (200, 30, 30))):
        d.ellipse([x - 34, y - 34, x + 34, y + 34], fill=col); d.text((x, y), t, font=font, fill='white', anchor='mm')
    im.save(f'{sys.argv[1]}/plan_{name}.png')
    print(name, 'crosses' if crosses(c) else 'ok', 'closest', round(near(c)))

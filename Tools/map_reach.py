"""map_reach.py: can every level stone of every castle map be seen and tapped on every screen shape?

The map painting covers the screen (MapView.Build), so 4:3 crops its sides and 20:9 its top and bottom; the painting
pans under a finger by that slack. A stone FAILS when no pan position shows it clear of the screen edge and of the
map's buttons (Back, Upgrades and its badge, the two page arrows), with and without a 100 px safe-area inset on screens
wider than 16:9; a cage or boss stone must also be clear where its cage or bear stands. Not modelled: the area's name
and star count beside Back (text, it takes no taps), and the hero standing on the current stone. Run after changing a trail (MapTrails.cs), the
stone count, or the position of a map button; keep the button boxes below in step with MapView.Build / UiKit.TopBar.

    python3 Tools/map_reach.py        exit code 1 when a stone fails
"""
import math
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
PAINT_W, PAINT_H = 1672, 941          # MapView.PaintingW / PaintingH
STONES = 41                            # LevelCatalog.PerCastle
REACH = 40                             # a stone's radius (32) plus a little, reference px
FINAL_REACH = 56                       # the castle's last stone is 1.5 times as big (MapView.BuildNode)
ART_UP = 75                            # the middle of what stands ON a cage or boss stone (cage 147 px, boss 122 px tall)
CAGES = (9, 19, 29, 39)                # LevelGenerator.KindForSlot: the cage stones (0-based slot in the castle)
FINAL = STONES - 1
INSETS = (0, 100)                      # safe-area inset at the left and right edge (a punch-hole phone), reference px
SCREENS = {"4:3": (1600, 1200), "16:9": (1920, 1080), "18.5:9": (2220, 1080), "20:9": (2400, 1080), "21:9": (2520, 1080)}
WORLDS = ["Trail", "Canyon", "Forest", "Harbour", "Caves", "Volcano", "Blossom", "Palace", "Tundra", "Throne"]


def trails():
    src = (ROOT / "Assets/Scripts/UI/MapTrails.cs").read_text() + (ROOT / "Assets/Scripts/UI/MapView.cs").read_text()
    found = {}
    for m in re.finditer(r"Vector2\[\]\s+(\w+)\s*=\s*\{(.*?)\};", src, re.S):
        found[m.group(1)] = [(float(a), float(b)) for a, b in re.findall(r"new Vector2\(([\d.]+)f?,\s*([\d.]+)f?\)", m.group(2))]
    return found


def resample(path, count):
    """MapView.Resample: `count` points evenly spaced along the path."""
    seg = [math.dist(path[i - 1], path[i]) for i in range(1, len(path))]
    total, out = sum(seg), []
    for n in range(count):
        target, walked, i = total * n / (count - 1), 0.0, 0
        while i < len(seg) - 1 and walked + seg[i] < target:
            walked += seg[i]
            i += 1
        k = min(1.0, max(0.0, (target - walked) / seg[i])) if seg[i] > 0 else 0.0
        a, b = path[i], path[i + 1]
        out.append((a[0] + (b[0] - a[0]) * k, a[1] + (b[1] - a[1]) * k))
    return out


def covered(x, y, cw, ch, r, inset):
    """What hides a thing of radius r at canvas point (x, y), origin top-left; None when it is clear.
    The buttons stand in the safe area, `inset` in from the left and right edges; the painting does not."""
    left, right = inset, cw - inset
    if x < r or x > cw - r or y < r or y > ch - r:
        return "off screen"
    if y < 128 + r and x < left + 128 + r:
        return "Back"
    if y < 134 + r and x > right - 134 - r:
        return "next arrow"                            # top-right corner, 110 px at (-24, -24)
    if y > ch - 134 - r and x > right - 344 - r:
        return "Upgrades"
    if y > ch - 184 - r and x > right - 104 - r:
        return "Upgrades badge"                        # the spendable-stars star on the button's top-right corner
    if ch - 224 - r < y < ch - 114 + r and x < left + 134 + r:
        return "previous arrow"                        # bottom-left, 110 px at (24, MapView.PrevArrowUp = 114)
    return None


def hidden(i, x, y, cw, ch, inset):
    """A stone must be clear itself; a cage or boss stone also where its cage or bear stands."""
    why = covered(x, y, cw, ch, FINAL_REACH if i == FINAL else REACH, inset)
    if why is None and (i in CAGES or i == FINAL):
        why = covered(x, y - ART_UP, cw, ch, REACH, inset)
        why = why and why + " (over its cage or boss)"
    return why


def main():
    all_trails, failed = trails(), 0
    for name, (w, h) in SCREENS.items():
        scale = math.sqrt(w / 1920 * h / 1080)          # CanvasScaler, match 0.5
        cw, ch = w / scale, h / scale
        k = max(cw / 1920, ch / 1080)
        pw, ph = 1920 * k, 1080 * k
        sx, sy = (pw - cw) / 2, (ph - ch) / 2            # pan slack each way
        for world in WORLDS:
            if world not in all_trails:
                print(f"FAIL {world}: no trail found")
                failed += 1
                continue
            for i, (px, py) in enumerate(resample(all_trails[world], STONES)):
                for inset in INSETS if w * 9 > h * 16 else INSETS[:1]:   # 16:9 and narrower screens have no cutout
                    why = None
                    for step in range(41):
                        f = step / 20 - 1   # only one axis has slack, so one factor covers the whole pan range
                        why = hidden(i, px / PAINT_W * pw - sx + f * sx, py / PAINT_H * ph - sy + f * sy, cw, ch, inset)
                        if why is None:
                            break
                    if why is not None:
                        print(f"FAIL {name} inset {inset} {world} stone {i + 1}: {why}")
                        failed += 1
    print("PASS: every stone can be reached" if not failed else f"{failed} stone(s) cannot be reached")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())

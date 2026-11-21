"""Tracking crop of the CPC penguin: track.py <start s> <dur s> <fps> <out.png> [tile px] [cols].
Follows the moving dark blob (the penguin is the only black thing that moves) and tiles a crop around it."""
import subprocess
import sys

import numpy as np
from PIL import Image, ImageDraw

VIDEO = sys.argv[7] if len(sys.argv) > 7 else "cpc.mp4"   # the CPC gameplay video (yt-dlp, 60 fps), not kept in the repo
W, H = 800, 720
start, dur, fps, out = float(sys.argv[1]), float(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
box = int(sys.argv[5]) if len(sys.argv) > 5 else 120
cols = int(sys.argv[6]) if len(sys.argv) > 6 else 16
raw = subprocess.run(["ffmpeg", "-loglevel", "error", "-ss", str(start), "-t", str(dur), "-i", VIDEO, "-vf",
                      f"crop={W}:{H}:240:0,fps={fps}", "-f", "rawvideo", "-pix_fmt", "rgb24", "-"],
                     capture_output=True, check=True).stdout
frames = np.frombuffer(raw, np.uint8).reshape(-1, H, W, 3)
tiles, pos, last = [], [], None
for i, f in enumerate(frames):
    dark = (f.astype(int).max(axis=2) < 70)
    dark[:70] = False   # HUD row
    prev = frames[i - 1] if i else frames[min(1, len(frames) - 1)]
    moved = np.abs(f.astype(int) - prev.astype(int)).sum(axis=2) > 60
    ys, xs = np.nonzero(dark & moved)
    if len(xs) > 25:
        if last is not None:   # keep to the blob near the last position when several things move
            near = (np.abs(xs - last[0]) < 150) & (np.abs(ys - last[1]) < 150)
            if near.sum() > 25:
                xs, ys = xs[near], ys[near]
        last = (int(np.median(xs)), int(np.median(ys)))
    if last is None:
        continue
    x0 = min(max(last[0] - box // 2, 0), W - box)
    y0 = min(max(last[1] - box // 2, 0), H - box)
    t = Image.fromarray(f[y0:y0 + box, x0:x0 + box]).resize((box * 2, box * 2), Image.NEAREST)
    ImageDraw.Draw(t).text((3, 2), f"{start + i / fps:.2f}", fill=(255, 255, 0))
    tiles.append(t)
    pos.append((round(start + i / fps, 3), *last))
rows = (len(tiles) + cols - 1) // cols
sheet = Image.new("RGB", (cols * box * 2, max(rows, 1) * box * 2))
for i, t in enumerate(tiles):
    sheet.paste(t, ((i % cols) * box * 2, (i // cols) * box * 2))
sheet.save(out)
with open(out + ".pos.txt", "w") as fh:
    fh.writelines(f"{t} {x} {y}\n" for t, x, y in pos)
print(len(tiles), "tiles", sheet.size)

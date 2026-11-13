#!/usr/bin/env python3
"""Synthesize the game's original sound effects and music loop (WAV, 44.1 kHz mono).

Run: python3 Tools/make_audio.py  -> Assets/Resources/Audio/*.wav
All sounds are generated here, so there is no third-party licence to track.
"""
import math
import pathlib
import random
import struct
import sys
import wave

RATE = 44100
OUT = pathlib.Path(__file__).resolve().parent.parent / "Assets/Resources/Audio"


def env(t, dur, attack=0.005, release=None):
    release = dur * 0.6 if release is None else release
    if t < attack:
        return t / attack
    if t > dur - release:
        return max(0.0, (dur - t) / release)
    return 1.0


def render(dur, fn):
    return [fn(i / RATE) for i in range(int(dur * RATE))]


def write(name, samples, gain=0.8):
    path = OUT / f"{name}.wav"
    if path.exists() and "--force" not in sys.argv:   # a hand-replaced WAV wins (README); --force regenerates all
        print(f"keep {path.name} (pass --force to regenerate)")
        return
    peak = max(1e-6, max(abs(s) for s in samples))
    data = b"".join(struct.pack("<h", int(max(-1, min(1, s / peak * gain)) * 32767)) for s in samples)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data)


def sweep(f0, f1, dur, shape=math.sin):
    phase = [0.0]

    def fn(t):
        f = f0 + (f1 - f0) * (t / dur)
        phase[0] += 2 * math.pi * f / RATE
        return shape(phase[0]) * env(t, dur)
    return render(dur, fn)


def noise(dur, lowpass=0.2):
    rnd = random.Random(f"{dur}:{lowpass}")   # seeded per sound, so adding or reordering sounds never changes the others
    prev = [0.0]

    def fn(t):
        prev[0] += (rnd.uniform(-1, 1) - prev[0]) * lowpass
        return prev[0] * env(t, dur, 0.002, dur * 0.9)
    return render(dur, fn)


def mix(*tracks):
    n = max(len(t) for t in tracks)
    return [sum(t[i] for t in tracks if i < len(t)) for i in range(n)]


def tone(freq, dur, harm=(1.0, 0.3, 0.1)):
    return render(dur, lambda t: sum(a * math.sin(2 * math.pi * freq * (k + 1) * t) for k, a in enumerate(harm))
                  * math.exp(-t * 6) * env(t, dur, 0.004, 0.05))


def seq(notes, step):
    out = []
    for f in notes:
        out += tone(f, step) if f else [0.0] * int(step * RATE)
    return out


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    write("launch", mix(sweep(180, 620, 0.25), noise(0.25, 0.5)))
    write("puff", sweep(300, 900, 0.18, lambda p: math.sin(p) + 0.4 * math.sin(3 * p)))
    write("dive", mix(sweep(900, 200, 0.35), noise(0.35, 0.3)))
    write("pop", mix(sweep(900, 300, 0.09), noise(0.12, 0.6)))
    write("sand", noise(0.35, 0.15))
    write("thud", mix(sweep(120, 50, 0.22), noise(0.15, 0.08)))
    write("zap", render(0.4, lambda t: (1 if math.sin(2 * math.pi * (220 + 80 * math.sin(t * 90)) * t) > 0 else -1)
                        * env(t, 0.4) * 0.6))
    write("boing", sweep(500, 160, 0.3, lambda p: math.sin(p + 2 * math.sin(p * 0.5))))
    write("bear_down", mix(sweep(700, 200, 0.3), tone(330, 0.3)))
    write("click", tone(1200, 0.05))
    write("star", seq([1047, 1319, 1568], 0.09))
    write("win", seq([523, 659, 784, 1047, 0, 784, 1047], 0.12))
    write("lose", seq([392, 370, 349, 262], 0.2))
    write("splash", noise(0.4, 0.35))
    write("combo", seq([784, 988, 1175, 1568], 0.07))
    write("fish", seq([1319, 1760], 0.06))
    write("explode", mix(sweep(90, 30, 0.5), noise(0.45, 0.9)))
    write("button", mix(tone(620, 0.08), tone(420, 0.1)))
    write("bumper", sweep(300, 1200, 0.12, lambda p: math.sin(p) + 0.5 * math.sin(2 * p)))
    write("cage", seq([659, 784, 1047, 1319, 1568], 0.1))

    # Calm reef loop: pentatonic marimba over a soft bass, 8 bars, loops seamlessly.
    beat = 0.25
    scale = [262, 294, 330, 392, 440, 523, 587, 659]
    melody, bass = [], []
    r = random.Random(11)
    for bar in range(8):
        root = [131, 110, 147, 98][bar % 4]
        bass += seq([root, 0, root * 1.5, 0], beat * 2)
        for _ in range(8):
            melody += seq([r.choice(scale) if r.random() < 0.7 else 0], beat)
    write("music_reef", mix([m * 0.5 for m in melody], bass), gain=0.5)

    # Arena loop: brighter, faster, major pentatonic with an off-beat bass, 8 bars.
    beat = 0.19
    scale = [392, 440, 494, 587, 659, 784, 880, 988]
    melody, bass = [], []
    r = random.Random(23)
    for bar in range(8):
        root = [196, 165, 220, 147][bar % 4]
        bass += seq([root, root, 0, root * 1.5], beat * 2)
        for _ in range(8):
            melody += seq([r.choice(scale) if r.random() < 0.8 else 0], beat)
    write("music_arena", mix([m * 0.5 for m in melody], bass), gain=0.5)
    print("audio done")


if __name__ == "__main__":
    main()

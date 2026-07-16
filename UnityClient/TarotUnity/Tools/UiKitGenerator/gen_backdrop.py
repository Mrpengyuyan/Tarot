#!/usr/bin/env python3
"""Midnight Parlor backdrop + candle-flame textures.

The menu's flat black quad read as "unfinished void" rather than "a dark room".
This composes a real parlor backdrop: a warm pool of candle-light haze low and
centered, falling off to near-black at the top and corners, plus faint vertical
wall banding so the darkness has structure instead of being a flat fill.
"""
import math
import random
from PIL import Image, ImageDraw, ImageFilter

W, H = 1024, 512

# Values sit deliberately low; this surface must never out-bright the cards.
NEAR_BLACK = (7, 4, 8)
WARM_LOW = (46, 20, 20)
HAZE = (150, 88, 46)


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def main():
    img = Image.new("RGB", (W, H), NEAR_BLACK)
    px = img.load()

    # Vertical grade: warm just above the table edge, black toward the ceiling.
    for y in range(H):
        t = y / (H - 1)
        # t=1 is the bottom of the quad (nearest the table).
        warmth = t ** 2.6
        row = lerp(NEAR_BLACK, WARM_LOW, warmth)
        for x in range(W):
            px[x, y] = row

    # Two candle haze pools, matching the left/right candle positions.
    haze = Image.new("L", (W, H), 0)
    hd = ImageDraw.Draw(haze)
    for cx in (int(W * 0.30), int(W * 0.70)):
        cy = int(H * 0.86)
        for i in range(60, 0, -1):
            r = i / 60
            rx, ry = 250 * r, 190 * r
            v = int(120 * (1 - r) ** 1.8)
            hd.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=v)
    haze = haze.filter(ImageFilter.GaussianBlur(34))
    img = Image.composite(Image.new("RGB", (W, H), HAZE), img, haze)

    # Heavy velvet drapery behind the table. The upper third of the menu was the
    # emptiest part of the frame; folds give that darkness structure to catch the
    # candlelight on, without adding a single lit polygon.
    fold = Image.new("L", (W, H), 0)
    fd = ImageDraw.Draw(fold)
    rng2 = random.Random(41)
    x = 0
    while x < W:
        width = rng2.randint(46, 96)
        # Each fold is a soft highlight on its lit flank and a dark gather beside.
        for k in range(width):
            t = k / width
            # Bright ridge just off-centre, falling to black in the gather.
            v = int(150 * math.sin(t * math.pi) ** 3)
            fd.line([(x + k, 0), (x + k, H)], fill=v)
        x += width
    fold = fold.filter(ImageFilter.GaussianBlur(7))

    # Drapery hangs from the ceiling and is swallowed by the table's shadow, so
    # the folds fade out before they reach the cloth.
    curtain_mask = Image.new("L", (W, H), 0)
    cm = ImageDraw.Draw(curtain_mask)
    for y in range(H):
        t = y / (H - 1)
        cm.line([(0, y), (W, y)], fill=int(255 * max(0.0, 1.0 - (t / 0.72) ** 1.4)))
    fold = Image.composite(fold, Image.new("L", (W, H), 0), curtain_mask)

    img = Image.composite(Image.new("RGB", (W, H), (58, 26, 34)), img, fold)

    # Corner falloff keeps the eye centered on the table.
    vig = Image.new("L", (W, H), 0)
    vd = ImageDraw.Draw(vig)
    for i in range(70):
        t = i / 70
        vd.rectangle([-W * t * 0.5, -H * t * 0.5, W + W * t * 0.5, H + H * t * 0.5],
                     outline=int(150 * t), width=8)
    vig = vig.filter(ImageFilter.GaussianBlur(50))
    img = Image.composite(Image.new("RGB", (W, H), NEAR_BLACK), img, vig)

    img.save("ParlorBackdrop.png")
    print("saved ParlorBackdrop.png", img.size)


def flame():
    """Soft additive teardrop for the candle flame billboard."""
    S = 256
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = S / 2
    for i in range(90, 0, -1):
        t = i / 90
        # Teardrop: narrow and tall at the top, round at the base.
        rx = 34 * t
        ry = 92 * t
        cy = S * 0.60 - 26 * (1 - t)
        a = int(255 * (1 - t) ** 1.5)
        core = (255, 236, 190) if t < 0.35 else (255, 150, 46)
        d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=core + (a,))
    img = img.filter(ImageFilter.GaussianBlur(3))
    img.save("CandleFlame.png")
    print("saved CandleFlame.png", img.size)


if __name__ == "__main__":
    main()
    flame()

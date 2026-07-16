#!/usr/bin/env python3
"""Midnight Parlor backdrop + candle-flame textures.

The menu's flat black quad read as "unfinished void" rather than "a dark room".
This composes a real parlor backdrop: a warm pool of candle-light haze low and
centered, falling off to near-black at the top and corners, plus faint vertical
wall banding so the darkness has structure instead of being a flat fill.
"""
import math
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

    # Faint wall banding so the dark has structure, not a flat fill.
    band = Image.new("L", (W, H), 0)
    bd = ImageDraw.Draw(band)
    for i in range(7):
        x = int(W * (i + 0.5) / 7)
        bd.line([(x, 0), (x, H)], fill=16, width=3)
    band = band.filter(ImageFilter.GaussianBlur(9))
    img = Image.composite(Image.new("RGB", (W, H), (26, 16, 24)), img, band)

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

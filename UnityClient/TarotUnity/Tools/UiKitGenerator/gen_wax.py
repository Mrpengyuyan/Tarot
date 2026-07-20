#!/usr/bin/env python3
"""Candle wax textures.

The candles read as plastic tubing: a perfect cylinder in one flat cream with
one flat emission value over the whole body. Real wax does three things none of
that did - it runs down the side in drips, it is translucent so it glows near
the flame and goes opaque toward the base, and it discolours where it has burnt.

Two maps, both mapped to Unity's cylinder side UV (V=0 at the base, V=1 at the
rim):
  WaxColor    - cream with vertical drips and soot at the rim
  WaxEmission - the translucency: bright at the rim, black by mid-body
"""
import math
import random
from PIL import Image, ImageDraw, ImageFilter

W, H = 512, 512
CREAM = (238, 228, 204)
CREAM_DEEP = (208, 192, 160)
SHADOW = (150, 136, 108)


def drip_column(d, x, top, length, width, fill):
    """One rivulet of wax: a rounded tail with a bead at the bottom."""
    d.rounded_rectangle([x - width / 2, top, x + width / 2, top + length],
                        radius=width / 2, fill=fill)
    r = width * 0.62
    d.ellipse([x - r, top + length - r, x + r, top + length + r], fill=fill)


def color_map():
    img = Image.new("RGB", (W, H), CREAM)
    d = ImageDraw.Draw(img)
    rng = random.Random(7)

    # Vertical grain: wax is poured, and it shows.
    for _ in range(90):
        x = rng.randint(0, W)
        v = rng.randint(-10, 8)
        shade = tuple(max(0, min(255, CREAM[i] + v)) for i in range(3))
        d.line([(x, 0), (x, H)], fill=shade, width=rng.randint(1, 4))

    # Drips running down from the rim (V=1 is the top => y=0 in texture space).
    for _ in range(9):
        x = rng.randint(0, W)
        drip_column(d, x, -6, rng.randint(50, 190), rng.randint(9, 20), CREAM)
        drip_column(d, x + rng.randint(-3, 3), -6, rng.randint(30, 150),
                    rng.randint(4, 9), (246, 238, 220))

    # Contact shadow where each drip meets the body, so they read as raised.
    # This pass must replay the *same* sequence the drips were drawn from. Re-seeding
    # alone is not enough: the drip loop runs after 90 grain iterations, so a fresh
    # Random(7) that skips them lands on entirely different columns, and every shadow
    # fell beside a drip instead of under one. Phase 57 lathes the drips as real
    # geometry at these exact angles, which made the mismatch impossible to miss.
    shade_layer = Image.new("L", (W, H), 0)
    sd = ImageDraw.Draw(shade_layer)
    rng2 = random.Random(7)
    for _ in range(90):
        rng2.randint(0, W)
        rng2.randint(-10, 8)
        rng2.randint(1, 4)
    for _ in range(9):
        x = rng2.randint(0, W)
        length = rng2.randint(50, 190)
        width = rng2.randint(9, 20)
        sd.rounded_rectangle([x - width / 2 - 3, -6, x + width / 2 + 3, length + 4],
                             radius=width / 2, fill=90)
        rng2.randint(-3, 3)
        rng2.randint(30, 150)
        rng2.randint(4, 9)
    shade_layer = shade_layer.filter(ImageFilter.GaussianBlur(4))
    img = Image.composite(Image.new("RGB", (W, H), CREAM_DEEP), img, shade_layer)

    # Soot and discolouration at the burnt rim.
    burn = Image.new("L", (W, H), 0)
    bd = ImageDraw.Draw(burn)
    for y in range(H):
        t = y / (H - 1)
        bd.line([(0, y), (W, y)], fill=int(150 * max(0.0, 1.0 - t / 0.10) ** 1.4))
    burn = burn.filter(ImageFilter.GaussianBlur(6))
    img = Image.composite(Image.new("RGB", (W, H), SHADOW), img, burn)

    # Grubby toward the base - candles stand in their own spill.
    base = Image.new("L", (W, H), 0)
    bd2 = ImageDraw.Draw(base)
    for y in range(H):
        t = y / (H - 1)
        bd2.line([(0, y), (W, y)], fill=int(70 * max(0.0, (t - 0.82) / 0.18)))
    img = Image.composite(Image.new("RGB", (W, H), CREAM_DEEP), img, base.filter(ImageFilter.GaussianBlur(8)))

    img.save("WaxColor.png")
    print("saved WaxColor.png", img.size)


def emission_map():
    """Translucency: the flame lights the wax it sits in, not the whole stick."""
    img = Image.new("RGB", (W, H), (0, 0, 0))
    d = ImageDraw.Draw(img)
    # Wax is translucent for its whole length, not just at the rim - it is only
    # *brightest* where the flame sits. Falling to pure black left the body with
    # no light at all (its own flame grazes it at N.L~0) and the candle read as a
    # charred stick, so the curve lands on a floor instead of zero.
    floor = 0.24
    for y in range(H):
        t = y / (H - 1)
        glow = floor + (1.0 - floor) * max(0.0, 1.0 - (t / 0.34) ** 1.5)
        v = int(255 * glow)
        d.line([(0, y), (W, y)], fill=(v, int(v * 0.66), int(v * 0.34)))

    img = img.filter(ImageFilter.GaussianBlur(3))
    img.save("WaxEmission.png")
    print("saved WaxEmission.png", img.size)


if __name__ == "__main__":
    color_map()
    emission_map()

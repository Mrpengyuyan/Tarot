#!/usr/bin/env python3
"""Arcane table decals for the Midnight Parlor menu.

The menu read as sparse because the velvet was empty: two candles and some
cards on a large dark field. This composes the ritual circle that belongs on a
diviner's table - concentric rings, a degree scale, alchemical marks and a
scattered star field - as a transparent gold decal laid onto the cloth. It is
flat, so it costs nothing to light and cannot clip anything.

Kept deliberately faint: the cards must stay the brightest thing on the table.
"""
import math
import random
from PIL import Image, ImageDraw, ImageFilter

S = 2
W = 1024 * S
GOLD = (198, 158, 74)


def ring(d, cx, cy, r, width, fill):
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=fill, width=width)


def polygon_star(d, cx, cy, r, points, rot, fill, width):
    pts = []
    for i in range(points * 2):
        rad = r if i % 2 == 0 else r * 0.45
        a = math.pi * 2 * i / (points * 2) + rot
        pts.append((cx + math.cos(a) * rad, cy + math.sin(a) * rad))
    d.polygon(pts, outline=fill, width=width)


def main():
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = W / 2
    g = GOLD + (255,)

    # Outer band: two rules with a degree scale between them.
    ring(d, c, c, W * 0.470, 3 * S, g)
    ring(d, c, c, W * 0.436, 2 * S, g)
    for i in range(72):
        a = math.pi * 2 * i / 72
        long_tick = i % 6 == 0
        r0 = W * 0.436
        r1 = W * (0.470 if long_tick else 0.458)
        d.line([c + math.cos(a) * r0, c + math.sin(a) * r0,
                c + math.cos(a) * r1, c + math.sin(a) * r1],
               fill=g, width=(2 if long_tick else 1) * S)

    # Twelve houses: dotted ring with a mark at each division.
    ring(d, c, c, W * 0.372, 2 * S, g)
    for i in range(12):
        a = math.pi * 2 * i / 12 - math.pi / 2
        px, py = c + math.cos(a) * W * 0.404, c + math.sin(a) * W * 0.404
        rr = 7 * S
        d.ellipse([px - rr, py - rr, px + rr, py + rr], outline=g, width=2 * S)
        # Spoke inward from each house.
        d.line([c + math.cos(a) * W * 0.372, c + math.sin(a) * W * 0.372,
                c + math.cos(a) * W * 0.300, c + math.sin(a) * W * 0.300],
               fill=g, width=1 * S)

    # Interlocked triangles - the classic scrying figure.
    ring(d, c, c, W * 0.300, 2 * S, g)
    for rot in (-math.pi / 2, math.pi / 2):
        pts = [(c + math.cos(rot + math.pi * 2 * k / 3) * W * 0.300,
                c + math.sin(rot + math.pi * 2 * k / 3) * W * 0.300) for k in range(3)]
        d.polygon(pts, outline=g, width=2 * S)

    # Inner sanctum.
    ring(d, c, c, W * 0.150, 2 * S, g)
    ring(d, c, c, W * 0.136, 1 * S, g)
    polygon_star(d, c, c, W * 0.118, 7, -math.pi / 2, g, 2 * S)

    # Alchemical corner marks around the ring, drawn geometrically so the decal
    # never depends on a font having occult glyphs.
    for i in range(4):
        a = math.pi / 4 + math.pi / 2 * i
        px, py = c + math.cos(a) * W * 0.240, c + math.sin(a) * W * 0.240
        rr = 20 * S
        if i % 2 == 0:
            d.polygon([(px, py - rr), (px + rr, py + rr * 0.6), (px - rr, py + rr * 0.6)],
                      outline=g, width=2 * S)
            d.line([px - rr * 0.7, py, px + rr * 0.7, py], fill=g, width=2 * S)
        else:
            d.polygon([(px, py + rr), (px + rr, py - rr * 0.6), (px - rr, py - rr * 0.6)],
                      outline=g, width=2 * S)

    # Star field between the sanctum and the houses.
    rng = random.Random(1909)
    for _ in range(60):
        a = rng.uniform(0, math.pi * 2)
        rad = rng.uniform(W * 0.160, W * 0.290)
        px, py = c + math.cos(a) * rad, c + math.sin(a) * rad
        rr = rng.choice([1, 1, 2]) * S
        d.ellipse([px - rr, py - rr, px + rr, py + rr], fill=g)

    img = img.resize((W // S, W // S), Image.LANCZOS)

    # Ink bleed: engraved into cloth, not printed on glass.
    img = img.filter(ImageFilter.GaussianBlur(0.6))
    img.save("ArcaneCircle.png")
    print("saved ArcaneCircle.png", img.size)


if __name__ == "__main__":
    main()

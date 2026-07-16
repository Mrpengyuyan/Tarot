#!/usr/bin/env python3
"""Scrying orb interior.

The orb read as a plastic marble because it was a matte sphere with nothing in
it. A crystal ball has two things this map supplies: something moving inside
(nebula, silt, a scatter of stars) and depth, so the eye reads through a surface
rather than at one.

Equirectangular, so it can be sampled by a parallax offset inside the shader and
appear to sit *behind* the glass rather than painted on it.
"""
import math
import random
from PIL import Image, ImageDraw, ImageFilter

W, H = 1024, 512
VOID = (10, 8, 26)
MIST = (78, 62, 150)
MIST_HOT = (150, 132, 226)


def main():
    img = Image.new("RGB", (W, H), VOID)
    rng = random.Random(1909)

    # Nebula: overlapping soft blobs at two scales, so the interior has a
    # foreground silt and a deeper haze behind it.
    for scale, count, colour, blur in (
        (1.0, 14, MIST, 54),
        (0.55, 22, MIST_HOT, 26),
    ):
        layer = Image.new("L", (W, H), 0)
        d = ImageDraw.Draw(layer)
        for _ in range(count):
            cx = rng.uniform(0, W)
            cy = rng.uniform(H * 0.18, H * 0.82)
            rx = rng.uniform(60, 190) * scale
            ry = rng.uniform(40, 130) * scale
            d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=rng.randint(60, 130))
        layer = layer.filter(ImageFilter.GaussianBlur(blur))
        img = Image.composite(Image.new("RGB", (W, H), colour), img, layer)

    d = ImageDraw.Draw(img)

    # A slow spiral drawn through the mist gives the eye something to follow and
    # keeps the blobs from reading as noise.
    for arm in range(2):
        for i in range(220):
            t = i / 220
            a = t * math.pi * 2.4 + arm * math.pi
            r = t * H * 0.42
            x = W / 2 + math.cos(a) * r * 1.9
            y = H / 2 + math.sin(a) * r
            rr = (1 - t) * 9 + 1
            v = int(150 * (1 - t) ** 0.8)
            d.ellipse([x - rr, y - rr, x + rr, y + rr],
                      fill=(v + 40, int(v * 0.85) + 30, 200))
    img = img.filter(ImageFilter.GaussianBlur(7))

    # Stars caught in the glass.
    d = ImageDraw.Draw(img)
    for _ in range(90):
        x, y = rng.uniform(0, W), rng.uniform(0, H)
        rr = rng.choice([0.6, 0.9, 1.4, 2.2])
        b = rng.randint(170, 255)
        d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(b, b, min(255, b + 20)))

    # Poles of an equirectangular map pinch; darken them so the pinch reads as
    # depth falling away rather than as a seam.
    pole = Image.new("L", (W, H), 0)
    pd = ImageDraw.Draw(pole)
    for y in range(H):
        t = abs((y / (H - 1)) - 0.5) * 2
        pd.line([(0, y), (W, y)], fill=int(235 * t ** 2.1))
    img = Image.composite(Image.new("RGB", (W, H), VOID), img, pole.filter(ImageFilter.GaussianBlur(14)))

    img.save("OrbInterior.png")
    print("saved OrbInterior.png", img.size)


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Moonlight cookie + drapery band, both driven by one measured fact.

Phase 48's diagnostic: with the camera pitched 27 degrees down, the highest
world Y visible at the backdrop's depth (z=11) is 0.34 - barely above the table.
Nothing can be hung on a wall in this shot, because the shot contains no wall.
Only the bottom ~29% of the backdrop texture is ever on screen.

So the window is never drawn. What is drawn is its light: a spot-light cookie
shaped like a mullioned window, thrown across the far cloth. You cannot see the
window; you know exactly where it is.
"""
import math
import random
from PIL import Image, ImageDraw, ImageFilter

S = 512


def cookie():
    """Mullioned window mask for a spot light. White passes light, black blocks."""
    img = Image.new("RGB", (S, S), (0, 0, 0))
    d = ImageDraw.Draw(img)

    # The opening. Kept well inside the frame so the cookie's own edge never
    # clips as a hard square - the falloff has to happen inside the texture.
    left, right = int(S * 0.20), int(S * 0.80)
    top, bottom = int(S * 0.12), int(S * 0.88)
    d.rectangle([left, top, right, bottom], fill=(255, 255, 255))

    # Mullions: one vertical, two horizontal. This cross is the entire signature
    # of window light - without it the pool is just a blob.
    bar = int(S * 0.028)
    cx = (left + right) // 2
    d.rectangle([cx - bar // 2, top, cx + bar // 2, bottom], fill=(0, 0, 0))
    for f in (0.36, 0.68):
        y = int(top + (bottom - top) * f)
        d.rectangle([left, y - bar // 2, right, y + bar // 2], fill=(0, 0, 0))

    # Old glass is uneven, so the panes are not evenly lit.
    rng = random.Random(5)
    mottle = Image.new("L", (S, S), 128)
    md = ImageDraw.Draw(mottle)
    for _ in range(40):
        x, y = rng.uniform(left, right), rng.uniform(top, bottom)
        r = rng.uniform(14, 52)
        md.ellipse([x - r, y - r, x + r, y + r], fill=rng.randint(96, 172))
    mottle = mottle.filter(ImageFilter.GaussianBlur(18))
    img = Image.composite(img, Image.new("RGB", (S, S), (0, 0, 0)),
                          mottle.point(lambda p: min(255, int(p * 1.5))))

    # Soft edge: moonlight through glass has no razor boundary.
    img = img.filter(ImageFilter.GaussianBlur(7))
    img.save("MoonCookie.png")
    print("saved MoonCookie.png", img.size)


def drapery_band():
    """
    Regenerates the backdrop with its folds in the band the camera can actually
    see. The Phase 45 version faded the folds out by 72% down from the texture's
    top, which lands at world Y 0.12 - above the 0.34 ceiling, so the drapery was
    never on screen at all.
    """
    W, H = 1024, 512
    NEAR_BLACK = (7, 4, 8)
    WARM_LOW = (46, 20, 20)
    HAZE = (150, 88, 46)

    img = Image.new("RGB", (W, H), NEAR_BLACK)
    px = img.load()
    for y in range(H):
        t = y / (H - 1)
        warmth = t ** 2.6
        row = tuple(int(NEAR_BLACK[i] + (WARM_LOW[i] - NEAR_BLACK[i]) * warmth) for i in range(3))
        for x in range(W):
            px[x, y] = row

    # Folds over the whole height now. Unity maps PIL row 0 to texV=1 (the quad's
    # top, off screen) and row H-1 to texV=0 (the quad's bottom, on screen), so
    # the fold pattern must reach the bottom rows to be seen at all.
    fold = Image.new("L", (W, H), 0)
    fd = ImageDraw.Draw(fold)
    rng = random.Random(41)
    x = 0
    while x < W:
        width = rng.randint(46, 96)
        for k in range(width):
            t = k / width
            fd.line([(x + k, 0), (x + k, H)], fill=int(170 * math.sin(t * math.pi) ** 3))
        x += width
    fold = fold.filter(ImageFilter.GaussianBlur(7))
    img = Image.composite(Image.new("RGB", (W, H), (64, 30, 38)), img, fold)

    haze = Image.new("L", (W, H), 0)
    hd = ImageDraw.Draw(haze)
    for cx in (int(W * 0.30), int(W * 0.70)):
        cy = int(H * 0.86)
        for i in range(60, 0, -1):
            r = i / 60
            rx, ry = 250 * r, 190 * r
            hd.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=int(120 * (1 - r) ** 1.8))
    haze = haze.filter(ImageFilter.GaussianBlur(34))
    img = Image.composite(Image.new("RGB", (W, H), HAZE), img, haze)

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


if __name__ == "__main__":
    cookie()
    drapery_band()

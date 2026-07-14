#!/usr/bin/env python3
"""Tarot card back generator - Midnight Parlor style.

180-degree rotationally symmetric ornate back: deep aubergine field,
fine double gold border, corner flourishes, central sun/moon medallion,
mirrored star field. Rendered at 2x and downsampled for crisp lines.
"""
import math
import random
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance

S = 2  # supersample factor
W, H = 1024 * S, 1728 * S

AUBERGINE_DARK = (26, 12, 22)
AUBERGINE_MID = (44, 20, 38)
GOLD = (201, 162, 39)
GOLD_HI = (232, 206, 121)
GOLD_DIM = (140, 110, 30)


def radial_field():
    """Aubergine field with a soft center glow and edge falloff."""
    img = Image.new("RGB", (W, H), AUBERGINE_DARK)
    glow = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(glow)
    cx, cy = W / 2, H / 2
    maxr = math.hypot(cx, cy)
    steps = 48
    for i in range(steps, 0, -1):
        r = maxr * i / steps
        v = int(90 * (1 - i / steps) ** 1.6)
        d.ellipse([cx - r, cy - r * 1.35, cx + r, cy + r * 1.35], fill=v)
    mid = Image.new("RGB", (W, H), AUBERGINE_MID)
    img = Image.composite(mid, img, glow)
    # mottle
    noise = Image.effect_noise((W // 4, H // 4), 28).resize((W, H))
    noise = noise.filter(ImageFilter.GaussianBlur(3 * S))
    img = Image.composite(
        ImageEnhance.Brightness(img).enhance(1.18), img, noise.point(lambda p: max(0, p - 118))
    )
    return img


def gold_layer():
    """All gold ornament drawn on a transparent layer."""
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    g = GOLD + (255,)

    # ---- borders ----
    m1 = 40 * S
    d.rectangle([m1, m1, W - m1, H - m1], outline=g, width=7 * S)
    m2 = 62 * S
    d.rectangle([m2, m2, W - m2, H - m2], outline=g, width=2 * S)

    # corner flourish: quarter fans + dot clusters, drawn once, rotated
    def corner(draw):
        ox, oy = m2 + 10 * S, m2 + 10 * S
        for k in range(4):
            r = (26 + k * 16) * S
            draw.arc([ox - r, oy - r, ox + r, oy + r], 0, 90, fill=g, width=2 * S)
        for k in range(3):
            rr = 5 * S
            a = math.radians(15 + k * 30)
            px = ox + math.cos(a) * 96 * S
            py = oy + math.sin(a) * 96 * S
            draw.ellipse([px - rr, py - rr, px + rr, py + rr], fill=g)

    quad = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    corner(ImageDraw.Draw(quad))
    layer.alpha_composite(quad)
    layer.alpha_composite(quad.transpose(Image.FLIP_LEFT_RIGHT))
    layer.alpha_composite(quad.transpose(Image.ROTATE_180))
    layer.alpha_composite(quad.transpose(Image.FLIP_TOP_BOTTOM))

    # ---- central medallion ----
    cx, cy = W // 2, H // 2
    R = 300 * S

    # radiating fine rays
    for i in range(24):
        a = math.pi * 2 * i / 24
        r0, r1 = R * 0.55, R * (0.98 if i % 2 == 0 else 0.82)
        d.line(
            [cx + math.cos(a) * r0, cy + math.sin(a) * r0,
             cx + math.cos(a) * r1, cy + math.sin(a) * r1],
            fill=g, width=(3 if i % 2 == 0 else 2) * S,
        )

    # concentric rings
    for rr, wd in [(R, 3), (R * 0.52, 2), (R * 0.47, 4)]:
        d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], outline=g, width=int(wd * S))

    # dotted ring
    for i in range(36):
        a = math.pi * 2 * i / 36
        px, py = cx + math.cos(a) * R * 1.12, cy + math.sin(a) * R * 1.12
        rr = 4 * S
        d.ellipse([px - rr, py - rr, px + rr, py + rr], fill=g)

    # eight-pointed star (two rotated squares), outline only
    def square_pts(rot, rad):
        return [
            (cx + math.cos(rot + math.pi / 2 * k) * rad,
             cy + math.sin(rot + math.pi / 2 * k) * rad)
            for k in range(4)
        ]

    for rot in (math.pi / 4, 0):
        d.polygon(square_pts(rot, R * 0.40), outline=g, width=3 * S)

    # crescent moon in the middle (circle minus offset circle)
    moon = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(moon)
    mr = R * 0.20
    md.ellipse([cx - mr, cy - mr, cx + mr, cy + mr], fill=255)
    md.ellipse([cx - mr + mr * 0.75, cy - mr - mr * 0.28,
                cx + mr + mr * 0.75, cy + mr - mr * 0.28], fill=0)
    solid = Image.new("RGBA", (W, H), g)
    layer.paste(solid, (0, 0), moon)

    # small star beside the moon
    def star(px, py, rad, n=5):
        pts = []
        for i in range(n * 2):
            r = rad if i % 2 == 0 else rad * 0.42
            a = math.pi * 2 * i / (n * 2) - math.pi / 2
            pts.append((px + math.cos(a) * r, py + math.sin(a) * r))
        d.polygon(pts, fill=g)

    star(cx + R * 0.26, cy - R * 0.24, 26 * S)

    # ---- mirrored star field between border and medallion ----
    rng = random.Random(1909)
    pts = []
    while len(pts) < 42:
        px = rng.uniform(m2 + 40 * S, W - m2 - 40 * S)
        py = rng.uniform(m2 + 40 * S, cy - 40 * S)
        if math.hypot(px - cx, py - cy) < R * 1.28:
            continue
        pts.append((px, py))
    for i, (px, py) in enumerate(pts):
        rr = (2 + (i % 3)) * S
        for q in ((px, py), (W - px, H - py)):
            if i % 4 == 0:
                star(q[0], q[1], rr * 3, 4)
            else:
                d.ellipse([q[0] - rr, q[1] - rr, q[0] + rr, q[1] + rr], fill=g)

    return layer


def shade_gold(layer):
    """Give the flat gold a vertical sheen so it reads as metal."""
    grad = Image.new("L", (W, H), 0)
    gd = ImageDraw.Draw(grad)
    for y in range(H):
        t = y / H
        v = int(120 + 100 * math.sin(t * math.pi * 2 + 1.2) * 0.5)
        gd.line([(0, y), (W, y)], fill=v)
    hi = Image.new("RGBA", (W, H), GOLD_HI + (0,))
    hi.putalpha(grad.point(lambda p: int(p * 0.9)))
    lo = Image.new("RGBA", (W, H), GOLD_DIM + (0,))
    lo.putalpha(grad.point(lambda p: int((255 - p) * 0.55)))
    shaded = layer.copy()
    shaded.alpha_composite(Image.composite(hi, Image.new("RGBA", (W, H), (0, 0, 0, 0)), layer.getchannel("A")))
    shaded.alpha_composite(Image.composite(lo, Image.new("RGBA", (W, H), (0, 0, 0, 0)), layer.getchannel("A")))
    # keep original alpha
    shaded.putalpha(layer.getchannel("A"))
    return shaded


def main():
    field = radial_field().convert("RGBA")
    gold = shade_gold(gold_layer())
    # soft glow under the gold
    glow = gold.getchannel("A").filter(ImageFilter.GaussianBlur(8 * S))
    glow_img = Image.new("RGBA", (W, H), (255, 214, 120, 0))
    glow_img.putalpha(glow.point(lambda p: int(p * 0.28)))
    field.alpha_composite(glow_img)
    field.alpha_composite(gold)
    out = field.convert("RGB").resize((W // S, H // S), Image.LANCZOS)
    out.save("TarotCardBack.png")
    print("saved TarotCardBack.png", out.size)


if __name__ == "__main__":
    main()

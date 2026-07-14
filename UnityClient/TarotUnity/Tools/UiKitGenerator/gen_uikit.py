#!/usr/bin/env python3
"""Midnight Parlor UI kit generator (nine-slice sprites + decals)."""
import math
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance

GOLD = (201, 162, 39)
GOLD_HI = (232, 206, 121)
GOLD_DIM = (140, 110, 30)
FIELD = (30, 15, 26)
FIELD_LIGHT = (48, 25, 42)
IVORY = (238, 226, 198)
IVORY_DARK = (196, 178, 142)


def sheen(layer, strength=0.85):
    """Vertical metal sheen applied to a gold RGBA layer."""
    w, h = layer.size
    grad = Image.new("L", (w, h), 0)
    gd = ImageDraw.Draw(grad)
    for y in range(h):
        t = y / max(1, h - 1)
        v = int(128 + 110 * math.sin(t * math.pi * 2 + 1.2) * 0.5)
        gd.line([(0, y), (w, y)], fill=v)
    a = layer.getchannel("A")
    hi = Image.new("RGBA", (w, h), GOLD_HI + (0,))
    hi.putalpha(grad.point(lambda p: int(p * strength)))
    lo = Image.new("RGBA", (w, h), GOLD_DIM + (0,))
    lo.putalpha(grad.point(lambda p: int((255 - p) * strength * 0.6)))
    out = layer.copy()
    out.alpha_composite(Image.composite(hi, Image.new("RGBA", (w, h), (0, 0, 0, 0)), a))
    out.alpha_composite(Image.composite(lo, Image.new("RGBA", (w, h), (0, 0, 0, 0)), a))
    out.putalpha(a)
    return out


def corner_ticks(d, x0, y0, x1, y1, s, g, arm):
    """L-shaped gold ticks in the four corners of a rect."""
    for cx, cy, dx, dy in [
        (x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1),
    ]:
        d.line([(cx, cy), (cx + dx * arm, cy)], fill=g, width=3 * s)
        d.line([(cx, cy), (cx, cy + dy * arm)], fill=g, width=3 * s)
        rr = 4 * s
        px, py = cx + dx * arm * 0.62, cy + dy * arm * 0.62
        d.ellipse([px - rr, py - rr, px + rr, py + rr], fill=g)


def make_panel(size=512, s=2, fill=FIELD, fill_alpha=235, name="TarotPanel.png",
               inner_line=True, corner_arm=44):
    W = size * s
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    m = 14 * s
    rad = 22 * s
    d.rounded_rectangle([m, m, W - m, W - m], radius=rad, fill=fill + (fill_alpha,))
    # soft inner shading: darker toward edges
    shade = Image.new("L", (W, W), 0)
    sd = ImageDraw.Draw(shade)
    for i in range(26):
        v = int(70 * (1 - i / 26))
        sd.rounded_rectangle([m + i * 2 * s, m + i * 2 * s, W - m - i * 2 * s, W - m - i * 2 * s],
                             radius=rad, outline=v, width=2 * s)
    dark = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    dark.paste(Image.new("RGBA", (W, W), (8, 4, 8, 130)), (0, 0), shade)
    img.alpha_composite(dark)

    gold = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    gd = ImageDraw.Draw(gold)
    g = GOLD + (255,)
    b1 = 20 * s
    gd.rounded_rectangle([b1, b1, W - b1, W - b1], radius=rad - 6 * s, outline=g, width=5 * s)
    if inner_line:
        b2 = 34 * s
        gd.rounded_rectangle([b2, b2, W - b2, W - b2], radius=rad - 12 * s, outline=g, width=2 * s)
        corner_ticks(gd, b2, b2, W - b2, W - b2, s, g, corner_arm * s)
    img.alpha_composite(sheen(gold))
    img.resize((size, size), Image.LANCZOS).save(name)


def make_button(size_w=512, size_h=192, s=2, name="TarotButton.png"):
    W, H = size_w * s, size_h * s
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    m = 8 * s
    rad = 26 * s
    # plaque fill with vertical gradient (lighter top)
    fill = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    fd = ImageDraw.Draw(fill)
    for y in range(m, H - m):
        t = (y - m) / (H - 2 * m)
        c = tuple(int(FIELD_LIGHT[i] + (FIELD[i] - FIELD_LIGHT[i]) * t) for i in range(3))
        fd.line([(m, y), (W - m, y)], fill=c + (255,))
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle([m, m, W - m, H - m], radius=rad, fill=255)
    img.paste(fill, (0, 0), mask)

    gold = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(gold)
    g = GOLD + (255,)
    b1 = 12 * s
    gd.rounded_rectangle([b1, b1, W - b1, H - b1], radius=rad - 4 * s, outline=g, width=5 * s)
    b2 = 24 * s
    gd.rounded_rectangle([b2, b2, W - b2, H - b2], radius=rad - 10 * s, outline=g, width=2 * s)
    # side diamonds
    for cx in (b2 + 18 * s, W - b2 - 18 * s):
        cy = H // 2
        r = 10 * s
        gd.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=g)
    img.alpha_composite(sheen(gold))
    img.resize((size_w, size_h), Image.LANCZOS).save(name)


def make_medallion(size=256, s=2, name="TarotMedallion.png"):
    W = size * s
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = W // 2
    # dark disc
    r0 = int(W * 0.46)
    d.ellipse([cx - r0, cx - r0, cx + r0, cx + r0], fill=FIELD + (255,))
    gold = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    gd = ImageDraw.Draw(gold)
    g = GOLD + (255,)
    gd.ellipse([cx - r0, cx - r0, cx + r0, cx + r0], outline=g, width=5 * s)
    r1 = int(W * 0.36)
    gd.ellipse([cx - r1, cx - r1, cx + r1, cx + r1], outline=g, width=2 * s)
    # dot ring
    for i in range(12):
        a = math.pi * 2 * i / 12
        px, py = cx + math.cos(a) * W * 0.41, cx + math.sin(a) * W * 0.41
        rr = 3 * s
        gd.ellipse([px - rr, py - rr, px + rr, py + rr], fill=g)
    # center star
    pts = []
    for i in range(8):
        r = W * (0.20 if i % 2 == 0 else 0.085)
        a = math.pi * 2 * i / 8 - math.pi / 2
        pts.append((cx + math.cos(a) * r, cx + math.sin(a) * r))
    gd.polygon(pts, fill=g)
    img.alpha_composite(sheen(gold))
    img.resize((size, size), Image.LANCZOS).save(name)


def make_parchment(size=1024, name="TarotParchment.png"):
    img = Image.new("RGB", (size, size), IVORY)
    noise = Image.effect_noise((size // 3, size // 3), 34).resize((size, size))
    noise = noise.filter(ImageFilter.GaussianBlur(2))
    dark = Image.new("RGB", (size, size), IVORY_DARK)
    img = Image.composite(dark, img, noise.point(lambda p: max(0, int((p - 100) * 0.9))))
    # blotches
    blot = Image.effect_noise((size // 16, size // 16), 60).resize((size, size))
    blot = blot.filter(ImageFilter.GaussianBlur(18))
    img = Image.composite(ImageEnhance.Brightness(img).enhance(0.93), img,
                          blot.point(lambda p: max(0, int((p - 128) * 1.4))))
    # edge vignette (for nine-slice border zone)
    vig = Image.new("L", (size, size), 0)
    vd = ImageDraw.Draw(vig)
    for i in range(40):
        v = int(120 * (1 - i / 40) ** 1.5)
        vd.rectangle([i * 3, i * 3, size - i * 3, size - i * 3], outline=v, width=3)
    img = Image.composite(ImageEnhance.Brightness(img).enhance(0.72), img, vig)
    img.convert("RGBA").save(name)


def make_divider(w=512, h=32, s=2, name="TarotDivider.png"):
    W, H = w * s, h * s
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gold = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(gold)
    g = GOLD + (255,)
    cy = H // 2
    gd.line([(6 * s, cy), (W - 6 * s, cy)], fill=g, width=2 * s)
    cx = W // 2
    r = 9 * s
    gd.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=g)
    for dx in (-24 * s, 24 * s):
        rr = 4 * s
        gd.ellipse([cx + dx - rr, cy - rr, cx + dx + rr, cy + rr], fill=g)
    img.alpha_composite(sheen(gold))
    img.resize((w, h), Image.LANCZOS).save(name)


def make_socket(w=512, h=863, s=2, name="TarotSocket.png"):
    """Gold inset marking a card slot on the cloth (decal, transparent bg)."""
    W, H = w * s, h * s
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    # inner shadow so the slot reads as pressed into the cloth
    sh = Image.new("L", (W, H), 0)
    sd = ImageDraw.Draw(sh)
    m = 26 * s
    sd.rounded_rectangle([m, m, W - m, H - m], radius=30 * s, fill=90)
    sh = sh.filter(ImageFilter.GaussianBlur(22 * s))
    shadow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shadow.paste(Image.new("RGBA", (W, H), (5, 2, 5, 170)), (0, 0), sh)
    img.alpha_composite(shadow)

    gold = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(gold)
    g = GOLD + (235,)
    b1 = 18 * s
    gd.rounded_rectangle([b1, b1, W - b1, H - b1], radius=26 * s, outline=g, width=4 * s)
    b2 = 34 * s
    gd.rounded_rectangle([b2, b2, W - b2, H - b2], radius=20 * s, outline=g, width=2 * s)
    corner_ticks(gd, b2, b2, W - b2, H - b2, s, g, 40 * s)
    img.alpha_composite(sheen(gold))
    img.resize((w, h), Image.LANCZOS).save(name)


def make_glow(size=512, name="TarotGlow.png"):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = size / 2
    steps = 60
    for i in range(steps, 0, -1):
        r = cx * i / steps
        a = int(200 * (1 - i / steps) ** 2.2)
        d.ellipse([cx - r, cx - r, cx + r, cx + r], fill=(255, 208, 120, a))
    img.save(name)


if __name__ == "__main__":
    make_panel()
    make_panel(name="TarotPanelSubtle.png", fill_alpha=200, inner_line=False)
    make_button()
    make_medallion()
    make_parchment()
    make_divider()
    make_socket()
    make_glow()
    print("ui kit done")

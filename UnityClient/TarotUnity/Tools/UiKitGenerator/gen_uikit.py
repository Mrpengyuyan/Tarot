#!/usr/bin/env python3
"""Midnight Parlor UI kit generator (nine-slice sprites + decals)."""
import math
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance

GOLD = (201, 162, 39)
GOLD_HI = (232, 206, 121)
GOLD_DIM = (140, 110, 30)
FIELD = (30, 15, 26)
FIELD_LIGHT = (62, 34, 54)
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

    # Inner shadow under the top edge: the plaque is recessed into its frame,
    # which is what a flat fill can never say.
    inner = Image.new("L", (W, H), 0)
    ind = ImageDraw.Draw(inner)
    for i in range(18):
        v = int(120 * (1 - i / 18) ** 1.5)
        ind.rounded_rectangle([m + i, m + i, W - m - i, H - m - i], radius=rad, outline=v, width=2)
    inner = inner.filter(ImageFilter.GaussianBlur(5 * s))
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shade.paste(Image.new("RGBA", (W, H), (6, 2, 8, 170)), (0, 0), inner)
    img.alpha_composite(shade)

    gold = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(gold)
    g = GOLD + (255,)
    b1 = 12 * s
    gd.rounded_rectangle([b1, b1, W - b1, H - b1], radius=rad - 4 * s, outline=g, width=8 * s)
    b2 = 24 * s
    gd.rounded_rectangle([b2, b2, W - b2, H - b2], radius=rad - 10 * s, outline=g, width=2 * s)
    # side diamonds
    for cx in (b2 + 18 * s, W - b2 - 18 * s):
        cy = H // 2
        r = 10 * s
        gd.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=g)
    img.alpha_composite(sheen(gold))

    # Bevel: the frame is a physical moulding, so it catches light on its upper
    # face and falls into shadow on its lower one. Drawn after the sheen so it
    # rides on top of the metal rather than being averaged into it.
    bevel_hi = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bh = ImageDraw.Draw(bevel_hi)
    bh.rounded_rectangle([b1, b1 - 1 * s, W - b1, H - b1], radius=rad - 4 * s,
                         outline=GOLD_HI + (150,), width=2 * s)
    img.alpha_composite(bevel_hi)

    bevel_lo = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bl = ImageDraw.Draw(bevel_lo)
    bl.rounded_rectangle([b1, b1 + 6 * s, W - b1, H - b1 + 3 * s], radius=rad - 4 * s,
                         outline=(70, 44, 8, 130), width=2 * s)
    img.alpha_composite(bevel_lo)
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
    """A card slot pressed into the velvet: a dark recess with a soft gold
    inner-edge glow and a single lit rim. Not a blueprint outline - an empty
    slot that quietly waits for its card, then vanishes once one lands on it."""
    W, H = w * s, h * s
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    m = 22 * s
    rad = 30 * s

    # 1) Deep recess: a soft dark pool so the slot reads as pressed into the
    #    cloth, darkest toward the center and fading before the rim.
    recess = Image.new("L", (W, H), 0)
    rd = ImageDraw.Draw(recess)
    rd.rounded_rectangle([m, m, W - m, H - m], radius=rad, fill=150)
    recess = recess.filter(ImageFilter.GaussianBlur(30 * s))
    dark = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    dark.paste(Image.new("RGBA", (W, H), (4, 2, 6, 205)), (0, 0), recess)
    img.alpha_composite(dark)

    # 2) Inner-edge shadow: a crisper dark ring just inside the rim = the lip
    #    of the recess catching shadow, which sells the pressed-in depth.
    lip = Image.new("L", (W, H), 0)
    ld = ImageDraw.Draw(lip)
    ld.rounded_rectangle([m, m, W - m, H - m], radius=rad, outline=255, width=10 * s)
    lip = lip.filter(ImageFilter.GaussianBlur(9 * s))
    lipimg = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    lipimg.paste(Image.new("RGBA", (W, H), (2, 1, 3, 150)), (0, 0), lip)
    img.alpha_composite(lipimg)

    # 3) Warm inner glow hugging the rim: the candle-lit edge of the socket,
    #    faint so the empty slot only breathes rather than shouts.
    glow = Image.new("L", (W, H), 0)
    gd = ImageDraw.Draw(glow)
    gd.rounded_rectangle([m + 3 * s, m + 3 * s, W - m - 3 * s, H - m - 3 * s],
                         radius=rad, outline=255, width=6 * s)
    glow = glow.filter(ImageFilter.GaussianBlur(11 * s))
    glowimg = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    glowimg.paste(Image.new("RGBA", (W, H), GOLD_HI + (110,)), (0, 0), glow)
    img.alpha_composite(glowimg)

    # 4) A single lit rim - soft, not a hard line. One gold pass, lightly
    #    blurred and given a metal sheen so it catches the light like a bevel.
    rim = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    rmd = ImageDraw.Draw(rim)
    rmd.rounded_rectangle([m, m, W - m, H - m], radius=rad,
                          outline=GOLD + (170,), width=3 * s)
    rim = rim.filter(ImageFilter.GaussianBlur(1 * s))
    img.alpha_composite(sheen(rim, strength=0.6))

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

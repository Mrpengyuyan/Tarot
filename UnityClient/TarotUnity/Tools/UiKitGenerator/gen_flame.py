import math
from PIL import Image, ImageFilter

S = 512

def smooth(t):
    t = max(0.0, min(1.0, t))
    return t*t*(3-2*t)

def flame_field(x, y):
    # normalized coords: u across (-1..1), v up (0 base .. 1 tip)
    u = (x/S - 0.5) * 2.0
    v = 1.0 - y/S
    # teardrop half-width profile: fat low, pinched tip, tucked base
    if v <= 0.0 or v >= 0.97:
        return 0.0
    peak = 0.22            # widest just above the wick
    wmax = 0.30            # half-width at the belly
    wbase = 0.16           # the flame still hugs the wick, not a point
    if v < peak:
        # round out from the wick to the belly
        width = wbase + (wmax - wbase) * smooth(v / peak)
    else:
        # taper the long way to a soft point at the tip
        t = (v - peak) / (0.97 - peak)
        width = wmax * (1.0 - smooth(t)) ** 0.85
    width = max(width, 0.008)
    d = abs(u) / width            # 0 at axis, 1 at edge
    if d >= 1.0:
        return 0.0
    # soft radial falloff across the flame body
    return smooth(1.0 - d)

def colour(intensity, v):
    # intensity 0..1 maps through a heat ramp; base gets a cool blue tuck
    i = intensity
    if i <= 0:
        return (0,0,0,0)
    # heat ramp: outer amber -> orange -> gold -> near-white core
    stops = [
        (0.00, (60, 20, 4)),
        (0.30, (196, 78, 16)),
        (0.55, (240, 140, 40)),
        (0.78, (255, 200, 96)),
        (0.92, (255, 240, 200)),
        (1.00, (255, 252, 244)),
    ]
    c = stops[-1][1]
    for k in range(len(stops)-1):
        a0,c0 = stops[k]; a1,c1 = stops[k+1]
        if a0 <= i <= a1:
            t = (i-a0)/(a1-a0)
            c = tuple(int(c0[j]+(c1[j]-c0[j])*t) for j in range(3))
            break
    r,g,b = c
    # cool blue root: just above the wick the flame burns bluish
    root = smooth((0.16 - v)/0.16) if v < 0.16 else 0.0
    if root > 0:
        br,bg,bb = (90,140,220)
        r = int(r*(1-root*0.7)+br*root*0.7)
        g = int(g*(1-root*0.4)+bg*root*0.4)
        b = int(b*(1-root*0.2)+bb*root*0.2)
    a = int(255 * smooth(i))
    return (r,g,b,a)

img = Image.new("RGBA",(S,S),(0,0,0,0))
px = img.load()
for y in range(S):
    for x in range(S):
        f = flame_field(x,y)
        if f>0:
            # bias intensity toward the core so the hot centre is compact
            px[x,y] = colour(math.pow(f, 0.85), 1.0-y/S)

# gentle blur unifies the ramp; a wide soft outer glow is added underneath
core = img.filter(ImageFilter.GaussianBlur(2.5))
glow = img.filter(ImageFilter.GaussianBlur(14))
glow_px = glow.load()
for y in range(S):
    for x in range(S):
        r,g,b,a = glow_px[x,y]
        glow_px[x,y] = (min(255,int(r*1.1)), int(g*0.85), int(b*0.6), int(a*0.5))
out = Image.alpha_composite(glow, core)
out.save("CandleFlame.png")
print("saved CandleFlame_new.png", out.size)

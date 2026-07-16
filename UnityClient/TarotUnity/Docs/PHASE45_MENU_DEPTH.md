# Phase 45 — Menu Depth, Mid-Ground Props, Type Block

Date: 2026-07-16

Driven by: "可以再选择多加一点元素，让整个页面的视觉效果饱满起来…文案的排版可以优化一下".

## Where the emptiness actually was

The frame had two candles, a deck and loose cards — all in the near band — and
nothing between the subtitle and the button. Everything added here goes into
that mid-ground band (z 1.5–2.6), and everything stays dimmer than the cards.

- **Drapery in the backdrop** (`gen_backdrop.py`): heavy velvet folds behind the
  table, masked to fade before they reach the cloth. The upper third was the
  emptiest part of the frame, and folds give that darkness something to catch
  candlelight on without adding a single lit polygon.
- **Scrying orb** on a brass stand, right of centre. Lit from within, in cool
  violet — the moon answering the candles, and the palette's only cold note. The
  cool light pool it throws on the oxblood cloth is what gives the composition a
  second colour.
- **Censer** with glowing coals and rising smoke, left of centre. The coals are
  the point: brass in the dark is only a shape.
- **Two more candles**, set further back and burnt lower. Repeating a light at a
  second depth is what turns a lit strip into a room.

## The type block

The three lines sat isolated with voids between them. Now: a gold rule
(`TarotDivider`) joins the title to its subtitle, the display cut is tracked
open (characterSpacing 14) so it reads as inscribed rather than typed, the
subtitle is smaller and tracked, and the status line recedes to a footnote at
15pt.

## The same bug, twice

```csharp
var light = root.GetComponent<Light>();
light.transform.localPosition = Vector3.zero;   // drags the whole prop
```

A Light added to a prop root **shares that root's Transform**, so writing
`light.transform.localPosition` moves the prop. Phase 42 hit this and stacked
both candles at the origin; this phase hit it again and stacked the orb and the
censer in the centre of the frame, behind the button, blown out. Documenting it
was not enough.

Now: `EnsureChildLight` puts prop lights on children only, strips any stray root
light left by an earlier run, and `Phase45MenuDepthTests.PropLightsLiveOnChildrenNotOnPropRoots`
asserts the rule so it cannot come back a third time.

Also fixed in-flight: `ParticleSystem.MinMaxCurve` cannot take a Gradient
(colour-over-lifetime needs `MinMaxGradient`), and the first orb halo ran at
scale 1.15 / alpha 0.5 and washed the centre of the frame out.

## Verification

Five capture iterations at 2560x1440, each read before the next change; a scene
audit was used to prove the props' positions rather than guessing from the
image. EditMode 228/228 (+5 new), PlayMode 30/30.

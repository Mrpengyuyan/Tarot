# Phase 58 — The Candles Were Never Red

Date: 2026-07-20

This phase was opened to fix the menu's front candles, which rendered as
saturated red plastic tubes instead of cream wax. It ends without touching a
light, a colour, or a material — because the candles were never red. The
capture pipeline used to review them was.

## What the measurement said

The candles are shared between scenes, so the same material rendering cream in
the ReadingRoom and red in the MainMenu pointed at the scenes. Sampled off the
HD archive:

```
MENU front-left candle    RGB(246,  74, 25)   saturation 0.90   B/R 0.10
MENU back-left candle     RGB(238, 168, 51)   saturation 0.79   B/R 0.21
ROOM front-left candle    RGB(213, 149, 59)   saturation 0.72   B/R 0.28
```

A read-only probe then rendered the menu camera under nine candidate fixes
(candle light colour, fill intensity, candle intensity, emission level, and
combinations). Seven of the nine landed on almost exactly the same number, which
is not a result — it is the signature of a broken instrument. Two control rows
were added: *turn the candle lights off*, and *turn the fill off*. Killing the
candle lights produced the red look, which inverts the hypothesis entirely; and
the plain baseline row now disagreed with the baseline from the previous run.

The same scene, measured twice, gave two answers. So the variants were never
the variable.

## The actual cause

The wax is marked `RealtimeEmissive`, and realtime GI does not resolve on the
frame a scene is loaded. Rendering one loaded scene repeatedly and measuring
after each render:

```
render 1    RGB(246,  77, 23)   saturation 0.91
render 2    RGB(234, 178, 77)   saturation 0.67
render 3    RGB(234, 178, 77)   saturation 0.67
...
render 24   RGB(232, 177, 79)   saturation 0.66
```

It converges on the second render and holds flat through twenty-four. Every
capture builder in this project — fifteen of them — called `camera.Render()`
exactly once after opening a scene, then read pixels. **Every visual review
this project has run was looking at lighting that had not settled.** The player
never saw the red candles; only the review images did.

## The fix

`CaptureRig.RenderConverged` renders a short warm-up (4 by default, twice the
measured convergence point) before the caller reads pixels. All fifteen capture
builders now route through it. `Phase58CaptureConvergenceTests` scans
`Assets/Editor` for a direct `Camera.Render()` call and fails on the next one
written, because the weakest point in this fix is the next builder someone adds.

The HD archive was regenerated on the corrected pipeline. The menu's front
candle now reads RGB(233,179,76) at saturation 0.67 — warm amber wax, and
slightly *less* saturated than the room's, which was the reference all along.

## Why this is written down at length

The project's standing rule is to diagnose before changing anything visual, and
to measure the real thing rather than a proxy. That rule held here and is the
only reason a lighting change was not shipped to fix a defect that did not
exist. The lesson worth keeping is narrower and sharper: **when several
different changes produce the same result, stop and doubt the instrument.**
Adding two control rows cost one run and overturned the conclusion.

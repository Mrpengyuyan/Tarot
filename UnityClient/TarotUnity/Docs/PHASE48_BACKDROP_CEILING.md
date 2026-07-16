# Phase 48 — There Is No Wall: The Backdrop's Geometric Ceiling

Date: 2026-07-16

Asked to add a hanging lamp, a window or a picture to the menu's backdrop. This
phase's real output is the measurement that says none of them can exist here,
plus one fix it exposed. **The moonlight it attempted is not shipped.**

## The ceiling

`Phase48BackdropDiagnostic` (read-only, kept) projects world points against the
live camera. With the camera at y=2.7 pitched 27 degrees down on a 36-degree
FOV, the top of the frame is only 9 degrees below horizontal, so the highest
visible world Y falls off fast with depth:

```
z= 2.0  yMax= 1.78      z= 8.0  yMax= 0.82
z= 4.0  yMax= 1.46      z=10.0  yMax= 0.50
z= 6.0  yMax= 1.14      z=11.0  yMax= 0.34   <- the backdrop's depth
```

At the backdrop, **the ceiling is world Y 0.34 — 34cm above the tabletop.**
Anything hung at wall height is out of shot. What reads as the frame's "upper
third" is not a wall: it is the far tabletop plus the bottom ~29% of the
backdrop quad (texV 0.00–0.29; everything above texV 0.30 has viewportY > 1).

Any future staging in that band must be measured against this diagnostic first.

## What this exposed

**Phase 45's drapery was never on screen.** Its folds faded out by 72% down from
the texture's top, which lands at world Y 0.12 — above the 0.34 ceiling. The
claim that it "filled the upper third" was wrong. The backdrop is regenerated
with the folds running the full height so they reach the band the camera
actually sees.

## What was attempted and dropped

Moonlight through an unseen window: a spot light wearing a mullioned cookie,
raked across the far cloth. The idea stands — you never see the window, you know
where it is; it would have been the cold source that explains the orb's violet
and answers the retired 月光牌桌 crest.

It is not shipped because it could not be verified:

- An A/B capture (light enabled vs disabled, same frame) proved the light **was**
  rendering — a diff bbox of (864,245)-(2560,959) — but it contributed roughly
  **3% brightness** at its own focus: (10,3,3) lit versus (3,0,0) unlit.
- Its response to intensity then stopped being explicable: 26 produced that
  measurable lift, 170 produced none.

Shipping an effect whose behaviour I cannot account for is worse than not
shipping it, so `RemoveMoonlight` strips the rig.

## Three wrong readings on the way (all mine)

Worth recording, because each one nearly closed the investigation early:

1. **`b > r` as a probe for cool light.** On oxblood velvet the red albedo
   dominates, so cold light still renders r > b. The probe can never fire. It
   said "no light" for a light that was working.
2. **Sampling the wrong band.** The pool projects to screen y≈403; I measured
   560–760 for several iterations, so every "no change" reading was meaningless.
3. **Blaming URP's `m_AdditionalLightsPerObjectLimit: 4`.** The PC renderer is
   already Forward+ (`m_RenderingMode: 2`), which has no per-object limit, so
   that setting was never in play.

The lesson is the one this project already had: measure the thing itself (A/B
the actual pixels), not a proxy that can lie.

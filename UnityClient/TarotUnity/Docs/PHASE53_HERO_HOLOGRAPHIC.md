# Phase 53 — A Holographic Foil on the Result Hero Card

Date: 2026-07-18

Phase 28 gave the flipped 3D card face a holographic sheen driven by its real
view angle - as the card tilts or the camera breathes, a glare band sweeps
across it. The Result hero card, the one the player sits and admires, never got
it: it is a **UI Image on a Screen-Space Overlay canvas**, which has no view
angle to read, so the Phase 28 sprite shader would just sit inert on it.

This phase gives it a foil that fits its medium.

## A UI shader, driven not by a view angle but by intent

`TarotUnity/HolographicCardUI` is the UI counterpart of the Phase 28 shader -
same diagonal glare band and iridescent tint, same additive sheen masked by the
card's own alpha - but built on the standard UI shader template (so it honours
the canvas clip rect and stencil) and positioned by a fed-in `_Sheen` uniform
instead of the object-space view direction. `_Sheen.x` sweeps the band across the
face; `_Sheen.y` shifts the iridescent hue. Both cards now shimmer in the same
material language.

`HolographicHeroCard` drives that uniform:

- **Idle** — a slow, uneven figure-of-eight drift, so a foil band drifts gently
  across the card and it is never perfectly still, the way a real foil catches
  ambient light.
- **On hover** — the band snaps to follow the pointer and the card tilts a few
  degrees toward it (an X/Y RectTransform rotation, which foreshortens the quad
  on an Overlay canvas so it reads as leaning in hand). It eases back to the idle
  drift on exit.

The material is instanced at runtime so the per-frame sheen never dirties the
shared asset, and the hero Image's `raycastTarget` is turned on so the pointer
can be heard. The glare colour is warm and gold-leaning to sit inside the
parlor's palette, tuned a touch calmer than the 3D face because the hero card is
large and viewed flat.

## Verification

`Phase53HeroHolographicTests` guards the wiring (shader + material exist, the
hero Image renders through the foil shader and can receive hovers, the driver is
attached and pointed at both). Because the hero card is empty at rest (its sprite
arrives at runtime), a read-only capture loads a card into it and renders the
foil at two sheen positions - `Docs/VisualReview/Phase53/` - confirming the band
renders on the card face and travels across it. The idle animation and the
pointer feel are, as ever, the player's to judge in hand; the values are a
tasteful starting point, every one a serialized knob.

# Phase 28 Holographic Card Face

Date: 2026-06-15

## Purpose

Make the flipped card face read as a glossy, dimensional object rather than a
flat picture. The cards were already a 3D mesh (Phase 15) that tilts toward the
pointer (Phase 23); this adds a holographic foil sheen that sweeps across the
face as it tilts, the way a premium foil trading card catches the light.

## What Changed

- New shader `TarotUnity/HolographicCard` (URP sprite, unlit) samples the card
  art and adds a diagonal glare band plus a subtle iridescent tint. The band is
  positioned by the object-space view direction, so it sweeps automatically as
  the card tilts (Phase 23) or the camera breathes (Phase 21) - no per-frame
  script. The sheen is masked by the card's own alpha, so it only lights the card.
- `MAT_HolographicCardFace` is built from that shader with a tuned, tasteful
  glare (intensity 0.5, narrow band) that adds shine without washing out the art.
- `Phase28HolographicCardBootstrapper` assigns the material to the card prefab's
  face SpriteRenderer (the one CardView drives with the real RWS art). It is a
  single, reversible material swap.

Safety: the shader declares `Fallback "Sprites/Default"`, so if the holographic
pass ever fails to compile on a target the card art still renders normally. The
sheen only shows on a face-up card (CardView enables the face renderer on flip).

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 28 Holographic Card Bootstrap`, then
`Tools/Tarot Unity/Run Phase 28 Holographic Capture` for a seated-view preview.

## Exit Criteria

- The holographic shader compiles and is supported.
- `MAT_HolographicCardFace` uses the shader with a tuned glare.
- The card prefab's face renderer uses the holographic material.
- EditMode tests pass.

## Remaining Limitation

The sheen sweep is a view-angle effect; its motion is best judged live (hover a
card so it tilts). Intensity, band width, and iridescence are single material
properties, easy to tune from feedback. The Result hero card is a separate UI
image and is not yet holographic; it could get a matching effect later.

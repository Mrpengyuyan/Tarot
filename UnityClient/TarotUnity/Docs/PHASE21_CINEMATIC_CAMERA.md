# Phase 21 Cinematic Camera

Date: 2026-06-11

## Purpose

Phase 21 replaces the prototype "surveillance" camera with seated, cinematic
framing and gives the camera a living response to the reading ritual.

The design is research-driven, not guessed:

- Hearthstone (GDC 2015, Derek Sakamoto) shows that premium card-game feel comes
  from a stable board view plus physical reactions, not from constant camera
  flight. So Phase 21 keeps five fixed framings and adds reactions only at
  meaningful moments.
- Inscryption demonstrates that a solo, atmospheric card table reads best from a
  first-person seated perspective: low angle, close to the cloth, darkness
  around the table.
- Cinematography practice: narrow FOV (tele) reads intimate and premium, wide
  FOV at close range reads distorted and cheap. The previous vertical FOV of 60
  (about 92 degrees horizontal) was the single biggest source of the flat
  prototype look.

## Measured Scene Geometry

All framing values are computed against the measured ReadingRoom layout:

- spread center: `(0, 0.12, 0.15)`; three-card slots at x = -1.45 / 0 / 1.45
- deck stack: `(-2.9, 0.12, 0.1)`
- card size: about 0.7 x 0.98 world units

## What Changed

### Seated pose framing (ReadingRoom)

| Pose | Position | Pitch/Yaw | FOV | Intent |
| --- | --- | --- | --- | --- |
| DefaultPose | (0, 2.45, -3.55) | 32.2 / 0 | 36 | seated at the table, cloth fills the frame |
| DeckPose | (-2.0, 1.7, -1.9) | 35.8 / -24.2 | 30 | leaning toward the deck during shuffle |
| OneCardPose | (0, 1.85, -2.6) | 32.2 / 0 | 32 | single card close-up, card is about half the frame height |
| ThreeCardPose | (0, 2.2, -3.2) | 31.8 / 0 | 36 | spread spans about 80 percent of frame width |
| ResultPose | (0, 2.9, -3.9) | 34.5 / 0 | 38 | ceremonial pull-back before the reveal |

The scene camera spawns one step behind DefaultPose (FOV 40), so the existing
`PlayOpening` call now eases the player down into the seat on scene start.

Each pose's aim is verified by an EditMode test that measures the angle between
pose forward and the direction to its subject.

### Living camera

`CameraChoreographyController` now owns a single composed camera frame:

- base pose blending with per-pose FOV interpolation
- idle breathing: Perlin micro-drift (position 0.03, rotation 0.3 degrees)
- flip punch-in: `PunchToward(card)` leans 0.55 units toward the flipped card,
  tightens FOV by 4 degrees, micro-shakes at the reveal apex via `Kick`, then
  settles back
- `CardFlipController` triggers the punch on every flip (toggle:
  `cameraPunchEnabled`)

### Restraint where it matters

- Result scene: framing and FOV are untouched because the screen-space panels
  and the 3D card stage are aligned against the existing camera. Only subtle
  breathing (0.015) is added; text readability wins.
- MainMenu: breathing only, via a new `MainMenuCameraChoreography` object.

### Latent bug fixed: fontless card labels

The seated framing exposed a graybox-era defect on `PF_TarotCard`: the
`TitleLabel` and `PositionLabel` TextMesh objects never had a font assigned
(`m_Font: {fileID: 0}`), so their glyphs render as giant solid ribbons
stretching across the table. The isolation captures in
`Docs/VisualReview/Phase21/Diagnostics/` document the hunt. Real RWS artwork
replaced these labels' purpose in Phase 13, so Phase 21 deactivates them in the
prefab (deactivated, not deleted; covered by a regression test). A future phase
can add a proper TextMeshPro card-name label if needed.

## Why Not Cinemachine

Cinemachine 3 was evaluated (Unity 6 ships 3.1.x with a fully changed API). The
needs here are five static framings, eased blends, idle noise, and one punch
reaction; the in-house controller covers these in about 200 lines with full
test coverage, while batch-mode wiring of Cinemachine components would add
integration risk without changing what the player sees. The decision can be
revisited if a later phase needs procedural follow or complex blend chains.

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 21 Cinematic Camera Bootstrap`.

Screenshot capture (places temporary framing-proxy cards on the spread slots,
never saves the scene):
`Tools/Tarot Unity/Run Phase 21 Visual Capture` -> `Docs/VisualReview/Phase21/`.

## Exit Criteria

- ReadingRoom poses use seated cinematic framing and aim at their subjects.
- Scene camera starts at the opening pose with cinematic FOV.
- Breathing is enabled in ReadingRoom, Result (subtle), and MainMenu.
- Flip triggers a camera punch that returns cleanly.
- Result framing/FOV is preserved.
- Phase 16 aura, Phase 20 volume, and prior wiring remain intact.
- EditMode and PlayMode tests pass.

## Remaining Limitation

Phase 21 is the camera-feel pass, not final high-end VFX. Later phases can add
Shader Graph card materials (foil, rim light), card motion trails,
depth-of-field staging, and screenshot-driven final polish.

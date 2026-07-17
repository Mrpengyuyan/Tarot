# Phase 49 — The ReadingRoom Gets Its Own Light

Date: 2026-07-16

First phase on the screen the player actually sits in. It carried the exact
defect Phase 42 diagnosed on the menu, and one worse.

## Two defects, both measured before touching anything

```
              ambient mode      legacy Text
MainMenu      3 (Flat, fixed)   0    <- TMP SDF
ReadingRoom   0 (Skybox!)       15
Result        0 (Skybox!)       13
```

1. **A daylight sky was lighting a midnight room.** `m_AmbientMode: 0` with
   Unity's stock skybox at full strength. That is why this screen read flat -
   no pools, no falloff, the far wooden rim barred grey across the top - no
   matter how its seven point lights were tuned. Same fix as Phase 42: ambient
   drops to a near-black flat floor, reflections to 0.06, the directional to
   0.035.

2. **Nothing in the room emitted light.** Seven point lights lit the table from
   nowhere. The menu spent six phases establishing that candles light this
   world; the room the player spends all their time in has to honour that, or
   the product reads as two different games. Four candles now stand in it -
   same rig as the menu's, same wax, same seeded flicker - plus the one unseen
   warm fill that lets the gold read.

The Phase 12-15 point lights are **dimmed, not deleted** (0.22-0.55): they were
tuned to fight the skybox and now stack into a wash, but the reveal and flip
phases reference them by name.

## The frame edge is a measurement, not a guess

The first pass put the front candles at x=±3.35/3.45. At this camera
(pos 0,2.85,-4.15 / pitch 32.4 / FOV 40) the frame edge at the card row's depth
falls at **x=±3.34** - they stood exactly on the cut and were sliced in half.
The back pair sat at z≈2.9 and rose into the step tracker, growing out of the
UI. Front candles moved to ±2.9/3.0 (clear of the card row at ±1.45), back pair
pushed out to the frame's corners at ±4.15/4.25.

`Phase49ReadingRoomLightTests` projects every candle against the live camera so
this cannot regress.

## Verification

Two capture iterations at 2560x1440. EditMode 244/244, PlayMode 30/30.

## Next on this screen

The 15 legacy Text components, including the question InputField - which needs
the TMP_InputField swap the menu never had to do.

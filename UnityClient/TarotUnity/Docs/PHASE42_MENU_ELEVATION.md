# Phase 42 — Text Sharpness + Main Menu Elevation

Date: 2026-07-16

Driven by the first real playthrough: "字体比较模糊，经过构建打包的字体非常模糊" and
"这个视角还有点不对…整个界面看上去还是太低端，根本没有高端游戏的设计感".

## 1. Why the built text was blurry

Diagnosed by elimination, and two early hypotheses were wrong — both recorded
here because the wrong ones cost time:

- **Wrong #1: URP render scale.** A `-h` grep over `Assets/Settings/*.asset`
  printed values without filenames, and the shell expands the glob
  alphabetically, so `Mobile_RPAsset`'s `m_RenderScale: 0.8` was misread as the
  PC asset's. The asset actually used by Standalone (`PC_RPAsset`, resolved via
  GraphicsSettings since neither quality level assigns a pipeline) is
  `m_RenderScale: 1`, `m_MSAA: 4`. Always grep with filenames.
- **Wrong #2: post-processing on the UI.** All three canvases are Screen Space
  **Overlay** (`m_RenderMode: 0`), which composites after the URP stack, so
  bloom/SMAA/ACES never touch the glyphs.
- **Actual cause: presentation resolution.** The player shipped as
  `fullscreenMode: 3` (Windowed) at a fixed `1280x720` with
  `resizableWindow: 0`. Going fullscreen stretched 720p across a 1440p display,
  softening everything including text. Font import settings were fine
  (`fontRenderingMode: 1` Hinted Smooth, `includeFontData: 1`).

Fix: the player now opens as `FullScreenWindow` (borderless, at the display's
native resolution — no upscale), defaults to 1920x1080 windowed, and is
resizable. Retina support stays on.

## 2. Why the menu read as low-end

The table concept was sound; the staging was not.

- **The room was lit by a daylight sky.** `m_AmbientMode: 0` (Skybox) with
  Unity's stock blue skybox at `m_AmbientIntensity: 1` and
  `m_ReflectionIntensity: 1` — a midnight candlelit parlor flat-lit from every
  direction. This is what made the far walnut rim read as a fluorescent bar
  across the frame, the candles read as grey PVC, and the whole image read flat
  no matter how the practical lights were tuned. Ambient is now a near-black
  flat tint (a shadow floor, not a light source) with reflections at 0.06.
- **The camera was wrong twice.** 10.8° pitch compressed the velvet into a thin
  band under a black void, and FOV was still Unity's 60° wide-angle default
  while the ReadingRoom had been on a 30–38° cinematic lens since Phase 21. Now
  seated and leaning in: pitch 27°, FOV 36°.
- **The void is gone.** The flat black backdrop quad is replaced by a composed
  parlor haze (`Tools/UiKitGenerator/gen_backdrop.py`): warm candle-light pools
  low, falling to near-black up and at the corners, with faint wall banding so
  the darkness has structure. The cloth now ends exactly at the far rim, ~9
  units out and beyond candle range, so the table falls off into that haze on
  its own.
- **The candles are candles.** Tapered wax with a pooled base, a lip, a wick,
  and an additive flame billboard, at asymmetric heights. Wax carries emission
  because a point light sitting on the wick grazes the candle's own vertical
  sides at N·L≈0 and leaves the body black — the glow has to be in the material.
  They own the room at intensity 4.2 / range 7.
- **One unseen fill** (`MP_TableFill`) reads as the candles' collective glow and
  is what lets the gold on the card backs and the wax hold their colour.
- **Palette corrected.** The velvet tint dropped from a mid-red that blew out to
  pool-table scarlet under the candle key to deep oxblood, and the walnut from a
  near-white multiplier to dark wood. Both are shared with the ReadingRoom, and
  the ReadingRoom captured **better** for it (the gold now pops off the deeper
  cloth) — verified, not assumed.
- **One accessory removed.** The `月光牌桌` crest restated the title and subtitle
  and floated between them and the button; deactivated (kept in scene).

## Bugs found and fixed in my own staging code

- `light.transform.localPosition = Vector3.zero` — the Light lives *on* the
  candle root, so this silently wiped the position set moments earlier and
  stacked both candles at the origin (one candle impaling the start button, and
  a doubled light pool blowing the velvet to lava). The root is now seated at
  flame height with the wax hanging below on negative offsets.
- `GetComponent<Light>() ?? AddComponent<Light>()` — `??` uses real null and
  bypasses Unity's overloaded `==`, so it handed back a fake-null Component and
  threw `MissingComponentException`. Explicit `== null` checks only.

## Verification

Six capture iterations at 2560x1440 (`Phase38CaptureBuilder.RunMainMenu`), each
one read before the next change. ReadingRoom and Result re-captured to prove the
shared-material change didn't regress them. `Phase42MenuElevationTests` guard the
display settings, the ambient/candle lighting contract, the camera, the fill, the
retired washes, and the palette floors.

## Not done (feedback-gated)

The deck and the loose cards still run off the bottom corners of the frame, and
the centre foreground is quiet. Reframing those trades against the UI column and
is a taste call — next pass, with the user's eyes on this one first.

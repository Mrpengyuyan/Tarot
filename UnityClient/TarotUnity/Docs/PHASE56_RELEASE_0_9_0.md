# Phase 56 — Release 0.9.0: The Presentation-Complete Build

Date: 2026-07-20

The version number had not moved since Phase 6 set it. Every phase from 7 to 55
- the entire visual identity, the redesign, and the card's motion chain -
shipped under `0.6.0`, which by now said nothing true about the build. This
phase bumps it to `0.9.0` and cuts a fresh desktop build on it.

## Why 0.9.0, and why not 1.0.0

0.9.0 marks the presentation as complete: all three screens are redesigned and
TMP SDF, and all four beats of the card's motion chain are in
(shuffle → deal → flip → result). What is deliberately *not* claimed is 1.0:
the backend reading path has not been verified end-to-end in a real
playthrough, and the feel of Phases 52–55 is still unjudged by a player. 1.0.0
belongs after a session at the table, not before one.

## What is in 0.9.0 that was not in 0.6.0

- **Visual identity and the table** — the flat prototype became the Midnight
  Parlor: velvet-and-walnut table stage with gold card sockets, candlelit
  lighting contract, arcane circle, drapery, censer, scrying orb (Phases 8,
  15, 37–49).
- **Real cards** — all 78 public-domain Rider-Waite-Smith 1909 faces at HD
  (~740x1280), correctly fitted to the card footprint, with a holographic foil
  on the flipped face (Phases 13, 27, 28, 32).
- **Cinematic presentation** — HDR colour grading, ACES tonemapping, bloom,
  vignette, SMAA, and a seated cinematic camera with idle breathing and
  reaction moves (Phases 20, 21).
- **Typography** — bundled LXGW WenKai, then the full migration to TextMeshPro
  SDF across all three screens, including the question field. No legacy
  `UI.Text` or `InputField` ships anywhere (Phases 24, 43, 50, 51).
- **Sound** — seven synthesized SFX plus an ambient music bed, on the
  persistent Boot AudioManager (Phase 33).
- **The motion chain** — shuffle riffle choreography (55), deal landing weight
  (54), flip weight with a reveal-synced camera (52), and the Result hero
  card's holographic foil (53), all in one motion language.
- **Presentation fixes that mattered** — the built-text blur (a fixed 720p
  window stretched across a 1440p display, Phase 42/43), the scrollable
  reading panel for arbitrary-length AI copy (29), and the card-face sizing
  bug (32).

## Where the version lives

`Phase6DesktopBuildBuilder.BundleVersion` is the single source of truth;
`ApplyDesktopPlayerSettings` writes it into `PlayerSettings.bundleVersion` on
every build, so building is what makes it real (`ProjectSettings.asset` follows
the const, never the other way round). `Phase6DesktopBuildTests` asserts the
shipped value, so a drifting version fails the suite.

The same doc correction went in here: `PHASE6_DESKTOP_BUILD.md` still described
the window as `1280 x 720` windowed, which Phase 42/43 changed to `1920 x 1080`
`FullScreenWindow` - and that stale line described the exact setting that
caused the blurry-text regression.

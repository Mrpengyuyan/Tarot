# Phase 36 Performance Pass

Date: 2026-07-10

## Purpose

The framework review found two objective performance items: the card-flip path
scanned the whole scene on every click, and the project had never been measured.
Phase 36 fixes the first and establishes the measurement baseline for the second.

## What Changed

- `CardFlipController` no longer calls `FindFirstObjectByType` inside the flip
  routine. The camera choreography and ritual feedback controllers are cached in
  lazy accessors: cached on first use (so scene-load order never matters) and
  self-healing across scene reloads via Unity's destroyed-object null semantics.
- `Phase36PerformanceProbeTests` (PlayMode) drives the real three-card slice -
  deal, reading-room idle, all three flips, result reveal - and logs main-thread
  frame times plus per-frame managed allocations. Assertions are generous sanity
  bounds only (a probe, not a flaky gate).

## Static Audit

All three per-frame loops were reviewed for allocations:

- `CardHoverTiltController.Update` - pure math, no allocations.
- `CameraChoreographyController.LateUpdate` (breathing/punch/shake compose) -
  struct math only, no allocations.
- `RitualAuraMotionController.Update` (rune rotation, glow pulse via transform
  scale, anchor drift) - no material instantiation, no allocations.

`new WaitForSeconds` appears only in per-action coroutines (deal, flip, reveal) -
a few dozen bytes per click, deliberately left as-is.

## Measured Baseline (editor batch, 2026-07-10)

- deal: mean 0.10 ms, p95 0.11 ms, max 8.22 ms
- reading-room idle: mean 0.10 ms, p95 0.11 ms, max 0.20 ms
- flips: mean 0.11 ms, p95 0.20 ms, max 1.21 ms
- result reveal+idle: mean 1.11 ms, p95 0.11 ms, max 120.25 ms (the max is the
  Result scene-load first frame - the hitch lands on the scene transition itself)
- reading-room idle managed alloc/frame: median 0 B, mean 0 B, max 0 B

Conclusion: the steady state is allocation-free and main-thread cost is trivial.
These numbers are CPU/GC only; batch mode says nothing about real GPU cost.

## Measuring the Real Build (GPU)

Editor numbers cannot capture GPU cost (bloom/ACES/SMAA at native resolution).
On macOS, run the player with Apple's Metal Performance HUD overlay:

```bash
MTL_HUD_ENABLED=1 "./Builds/Desktop/macOS/Tarot Unity.app/Contents/MacOS/Tarot Unity"
```

The HUD shows live FPS, GPU frame time, and memory while playing a round. On
Windows, any FPS overlay (e.g. the Xbox Game Bar performance widget) serves the
same purpose. Record the numbers during a full round for the release baseline.

## Verification

- `Phase36PerformancePassTests` (EditMode): the flip controller carries the cached
  references, and this document stays present.
- PlayMode 30/30 including the probe; EditMode suite green; macOS player build
  succeeds with the change.

## Remaining Limitation

GPU frame time on the actual target hardware (especially Windows D3D12) is still
unmeasured until a real round is played with the HUD - the probe covers CPU and
allocations only. The Result scene-load hitch (~120 ms once, behind the
transition) is acceptable; shader prewarming could shave it if it ever reads as
a visible stutter.

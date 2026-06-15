# Phase 26 Result Text Robustness

Date: 2026-06-15

## Purpose

Phase 25 composed the Result reading in fixed-height boxes sized for the short
local-mode placeholder copy. Real backend AI interpretation is variable and can
be much longer; with fixed boxes, long copy would overflow downward and overlap
the next section. Local mode never shows this (its text is short), so it is an
invisible production risk on the payoff screen.

## What Changed

The four reading body texts (`SummaryText`, `OverallText`, `CardAnalysisText`,
`AdviceText`) now use Unity Text best-fit: each caps to its own box. Short copy
still renders at the design size (19), and long copy shrinks toward a readable
floor (13) so a section can never overflow into the one below it, whatever the
backend returns. `fontSize` stays at the design size, so the OverallText size
invariant earlier phases assert still holds.

This is a deliberately small, no-churn safety net: it changes only Text
properties, touches no layout or hierarchy, and breaks no existing tests.

## Note On The Glow Experiment

This phase originally attempted a materials-only "moonlit glow" on the card-back
and deck. Screenshot verification showed it failed - the card-back emission had
no visible effect, and the deck blew out into a garish red under ACES tonemapping
plus the warm key light. Those material changes were reverted, and the phase was
redirected to this objective robustness fix. The lesson: emissive on flat back
planes either does nothing or blooms badly; a real arcane back needs actual
pattern geometry/texture, which is a feedback-gated, hero-element change.

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 26 Result Text Robustness Bootstrap`.

## Exit Criteria

- The four reading body texts use best-fit with a max of 19 and a readable min.
- OverallText keeps its size floor.
- EditMode tests pass.

## Remaining Limitation

Phase 26 is a robustness floor; it is not final high-end VFX. The eventual ideal
for very long readings is a scrollable reading panel, and the card/back visual
"wow" (foil sheen, view-dependent shimmer, animated reveal) remains for a pass
tuned with live feedback.

# Phase 29 Result Reading Scroll Panel

Date: 2026-06-15

## Purpose

Phase 26 hardened the Result reading against variable-length backend AI text by
shrinking each section with best-fit so long copy could not overflow into the
section below. That was a safety floor, not the ideal: very long readings shrank
to a small, hard-to-read size, and the design size (19) only held for short copy.

Phase 29 delivers the "right" fix Phase 26 itself flagged as the eventual ideal:
a real scrollable reading panel. Every section now renders at full size, and any
length that exceeds the viewport scrolls instead of shrinking or overflowing.

## What Changed

The four header/body pairs (`Phase7_ResultSectionSummary` + `SummaryText`,
`Phase7_ResultSectionOverall` + `OverallText`, `Phase7_ResultSectionCards` +
`CardAnalysisText`, `Phase7_ResultSectionAdvice` + `AdviceText`) are reparented,
in reading order, into a new `ResultReadingScroll`:

- `ResultReadingScroll` — `ScrollRect` (vertical only, `Clamped`), 736x448,
  centered on the Phase 25 reading column (x = 178). Its `Image` carries the
  translucent panel colour, so the old flat backing panel is now redundant.
- `Viewport` — stretches to the scroll, with a `RectMask2D` that clips anything
  past its bounds (this is what makes the reading scroll instead of overflow).
- `Content` — top-anchored, with a `VerticalLayoutGroup`
  (padding 14/14/12/14, spacing 6, child-control width+height, force-expand
  width) plus a `ContentSizeFitter` (vertical = PreferredSize) so its height
  tracks the text. 736 wide minus 28 padding leaves a 708px body column, which
  keeps the Phase 8 (>=700) and Phase 12 (>=660) reading-width invariants.

Best-fit is turned **off** on the four bodies (Phase 26's mechanism is
superseded): they render at the design size and the viewport handles overflow.
The redundant `Phase8_ResultScrollPanel` is deactivated, not deleted. The card
hero, question header, dividers, and footer are untouched.

## Test Impact (Intent Preserved, Not Gamed)

Reparenting the sections into a nested, layout-driven Content changed enough that
a few older Result tests needed updating to match the new structure while keeping
their original intent:

- Phase 8 / Phase 12 looked up `OverallText` as a direct child of the canvas and
  asserted `sizeDelta.x`. It is now nested in Content and its width is
  layout-driven, so the tests find it recursively and assert the resolved
  `rect.width` (still >= 700 / >= 660).
- Phase 25 geometry tests compared `anchoredPosition`-derived edges, which assume
  a shared coordinate space. They now use world-space corners
  (`GetWorldCorners`), valid no matter how deeply a rect is nested.
- Phase 26's best-fit assertion is superseded: its test now asserts the bodies
  render full size and live under the viewport `RectMask2D`, so the original
  guarantee (long copy never overflows into the next section) still holds, the
  better way.

## How To Run

1. Editor menu: `Tools/Tarot Unity/Run Phase 29 Result Scroll Bootstrap`.
2. Verification capture:
   `Tools/Tarot Unity/Run Phase 29 Result Scroll Capture` writes
   `Docs/VisualReview/Phase29/Result_default.png` (short copy, no scroll) and
   `Result_long.png` (long injected copy filling the panel at full size).

## Exit Criteria

- `ResultReadingScroll` has a vertical `ScrollRect` wired to a `RectMask2D`
  viewport and a `VerticalLayoutGroup` + `ContentSizeFitter` Content.
- All eight reading sections are children of Content, in reading order.
- Bodies render at full size (best-fit off); long copy makes Content taller than
  the viewport (it scrolls).
- The Result reading width still clears the >= 700 / >= 660 column invariants.
- EditMode tests pass.

## Remaining Limitation

Phase 29 is the layout/robustness completion for long readings; it is
not final high-end VFX. A styled scrollbar handle, scroll-edge fades, and the
card-back "wow" pass (animated reveal, richer foil motion) remain for a
feedback-tuned visual pass.

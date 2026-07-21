# Phase 60 — The Result Screen Shows the Whole Spread

Date: 2026-07-21

## The defect

`ResultPanelPresenter` received the full `card_draws` array but rendered only
`draws[0]` into a single hero slot. So a three-card reading — past / present /
advice — showed **one** card, under a header that already promised
"三牌阵·过去·现在·建议". The ReadingRoom deals and flips all three correctly; the
loss was only on the Result screen, the payoff.

## What changed

A one-card reading is untouched (the original left-third hero + right reading
column). A multi-card reading switches to the layout the user chose: a **top band
of all N cards**, past → present → advice left to right, with the reading reflowed
full-width below them.

- **`MP_ResultSpreadBand`** (built by `Phase60ResultSpreadBandBootstrapper`): up to
  three framed card cells, each a gold `TarotPanel` frame over the card art with a
  warm candle glow behind it, and a gold position label (过去 / 现在 / 建议) beneath.
- **Reversed cards are turned.** Each cell's artwork sits under a `ReversePivot`
  that rotates 180° when `is_reversed` is set, and the label gains "（逆位）". The
  pivot is separate from the foil driver's own rect, which the Phase 53 hover tilt
  animates, so the two never fight.
- **The Phase 53 holographic foil** is carried onto every card (one
  `HolographicHeroCard` per cell), so the whole band shimmers in the same material
  language as the single hero.
- **Adaptive layout in the presenter.** `ResultPanelPresenter.PresentCards` counts
  the draws: one card → show the hero, hide the band, reading in the right column;
  two or more → hide the hero (all three sibling objects: showcase, artwork slot,
  placeholder), show the band, reading full-width below, and the near-footer
  warning line is hidden because the wide reading reaches into it. Every position
  label comes from `position_name`, so a backend spread of any shape is labelled
  from its own data.

## The one honest note

The **local** placeholder reading is entirely English (position names, summary,
card names), so the demo captures show English labels under the Chinese header.
This is pre-existing (the whole offline placeholder is English) and not this
phase's scope: the presenter is data-driven, so a real backend reading — which is
Chinese — renders Chinese labels and text throughout. Localising the offline
placeholder is a separate, optional follow-up.

## Verification

- `Docs/VisualReview/Phase60/` — `Result_threeCard.png` (three cards, the third
  reversed and upside-down, full-width reading) and `Result_oneCard.png` (the
  original single hero, now populated). Reviewed and iterated: the first pass left
  the single-hero slot showing behind the band and the resized reading scroll
  clipping at a stale mask size; both fixed.
- `Phase60ResultSpreadTests` drives the real presenter over the Result scene: a
  three-card session fills all three cells with art and labels and turns the
  reversed card 180°, while a one-card session keeps the single hero and hides the
  band.

## Not touched

The ReadingRoom (already deals all N), the reading scroll's Phase 29 internals,
and the one-card composition. The band's cell size, spacing, and the reading's
two layouts are all serialized constants in the bootstrapper / presenter, easy to
retune.

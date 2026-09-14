# Phase 67 — Result reading experience

Spec: `docs/superpowers/specs/2026-09-13-result-reading-experience-design.md` (sub-project C).

## What changed

- **Multi-card layout.** `ResultSpreadLayout` hangs the card band under the header (one row up to five
  cards, two rows of five for ten) and gives the reading panel everything down to the button row. At
  16:9 the reading panel is 332 tall for three to five cards (it was 232) and 293 for ten; a 16:10
  screen adds 80.
- **Frame.** The viewport is inset 24 inside the `TarotPanel` frame, whose inner gold line ends 18 in,
  so text no longer touches the border or shows below it. The single-card panel widened to 772 (right
  edge unchanged) to keep its 700-wide reading column.
- **Scroll cues.** A thin gold scrollbar (auto-hide) and a bottom fade that disappears at the end of
  the reading (`ResultReadingNavigator`, `ReadingFadeGradient`).
- **Per-card blocks.** `CardAnalysisParser` splits `card_analysis` by position name, or by line order
  when the counts match, and `CardAnalysisFormatter` writes a gold heading per card from the client's
  own card data. Clicking a card scrolls to its block and glows the heading (`ResultSpreadCellTarget`).
  Text that cannot be split shows as one block, as before. The backend prompt now asks for one line per
  card.
- **Safe display.** Server and player text goes through `ReadingTextSanitizer`: text containing `<` is
  wrapped in `noparse`, and each field is capped at 4000 characters.
- **Notices.** An offline reading shows the offline notice as its first line on every layout; an
  online AI warning is a 提醒 section after 建议 on every layout.
- **Generating.** After 20 seconds, 查看离线解读 appears together with the slow notice.
- **Aspect.** `ResultCanvasAspectFit` matches width on screens narrower than 16:9 and height on wider
  ones, and pins the header and the button row to the canvas edges at runtime.
- **Reveal.** Section headings, the mode label and the card band fade in with their sections
  (`ResultRevealDirector` companions).

## Review shots

`Docs/VisualReview/Phase67/`: one, three, five and ten cards at 16:9; three and ten cards at 16:10;
three cards at 4:3; and the generating (20 s), failed and offline states. The capture builder checks
the canvas took the expected height and fails if body text appears at the reading frame's bottom edge.

## Known limits

- Card hover and click-to-scroll are pointer-only; there is no keyboard or gamepad navigation.
- An AI answer that ignores the one-line-per-card format shows as a single block.
- Only the Result screen adapts to 16:10 and 4:3; the reading room and the main menu keep the shared
  0.5 match factor.
- The Phase 60 and Phase 62 bootstrappers must not be re-run: they rebuild the band and would drop the
  Phase 67 wiring.

# Phase 30 Cross-Screen Secondary Spacing/Alignment

Date: 2026-06-15

## Purpose

Unify the secondary spacing and alignment shared by the three core screens
(MainMenu, ReadingRoom, Result). These screens were composed independently across
many phases, so before changing anything this phase ran a read-only layout audit
to find genuine cross-screen inconsistencies with real numbers instead of
guessing.

## Audit Finding: Already Largely Consistent

`Tools/Tarot Unity/Run Phase 30 Layout Audit` opens each scene and logs the
canvas reference size plus the screen-edge margins of the comparable secondary
elements (heading, sub-label, primary button / footer). The audit showed the
screens are already strongly unified:

- **Identical CanvasScaler** on all three: `ScaleWithScreenSize`, reference
  resolution `1280x720`, match factor `0.5`. Layout therefore scales the same way
  on every screen and off-aspect window.
- **Symmetric centering everywhere**: every centered heading and footer has equal
  left/right margins (TitleText, SubtitleText, StatusText, StartReadingButton on
  MainMenu; QuestionText, SpreadNameText, BackToMenuButton on Result).
- Heading / sub-label pairs abut cleanly (Title->Subtitle and
  Question->SpreadName rects touch within ~2px; text leading keeps a visible gap).

This is the honest, evidence-based conclusion: the "unify" work was mostly
already done by the earlier composition phases. The risk of speculative
repositioning (the exact "build blind -> visual collapse" failure mode this
project guards against) outweighs any benefit, so this phase **codifies and
guards** the existing consistency rather than moving visually-fine layouts.

## What Changed

- `TarotUiSpacing` (new, `Assets/Scripts/UI/`): the single source of truth for the
  shared rhythm - reference resolution, match factor, centering tolerance, and the
  max allowed heading/sub-label overlap. Future screens read these so they stay in
  step; no runtime behaviour or visual changes.
- `Phase30LayoutAuditBuilder` (new editor tool): the read-only audit above. It
  never modifies or saves a scene.
- `Phase30CrossScreenConsistencyTests` (new EditMode tests): regression guards that
  assert the shared CanvasScaler, symmetric centering, and non-overlapping
  heading/sub-label pairs across the screens. If a later phase nudges one screen
  out of step, a test fails.

No scene assets were modified in this phase.

## Deliberately Left Alone (Flagged For Visual Review)

The one cross-screen difference worth noting is the **primary action button**
height: StartReadingButton, RevealResultButton, and BackToMenuButton render at
slightly different heights (roughly 32 / 31 / 28 canvas units). Unifying them
cannot be done cleanly: the MainMenu button carries gold-trim children and the
Result footer sits just below the reading scroll, so resizing risks detaching trim
or colliding with the scroll. Because that is a taste-and-cascade call rather than
an objective fix, it is left for a feedback-tuned visual pass.

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 30 Layout Audit` (read-only; prints the
audit table to the console).

## Exit Criteria

- All three core canvases share the `1280x720`, match-`0.5` CanvasScaler.
- Centered headings/footers are horizontally symmetric on every screen.
- Heading/sub-label pairs do not overlap.
- EditMode tests pass.

## Remaining Limitation

Phase 30 is an objective consistency guard; it is not final high-end VFX. The
primary-button height unification and any deliberate spacing refresh remain for a
feedback-tuned visual pass.

# Phase 51 — The Result Screen Joins the World

Date: 2026-07-18

The Result screen was the last holdout on legacy `UI.Text`: 13 components, the
same bitmap-atlas text that softens at the built resolution. With this phase all
three screens are TMP SDF, and all three share one ambient contract.

```
              legacy Text   ambient
MainMenu      0             Flat
ReadingRoom   0 (Phase 50)  Flat
Result        13 -> 0       Skybox -> Flat
```

## The text (the visible win)

All 13 convert in place with the shared Text -> TMP routine (Phases 43/50). The
authored colour carries over verbatim, so the four gold section headers stay
gold and the four ivory bodies stay ivory - the `TarotUiAccentText` markers keep
the headers gold at runtime too. `ResultPanelPresenter`'s seven readout fields
change type `Text -> TMP_Text` (the four that carry arbitrary-length backend AI
copy included); the dynamic SDF atlas resolves whatever Chinese the backend
returns, and TMP_Text exposes the same `.text` the presenter already set, so
only the declarations move. `SetText` retypes with them.

The reading still lives in the Phase 29 scroll (RectMask2D viewport +
VerticalLayoutGroup/ContentSizeFitter); TMP_Text implements the layout element
interface, so long copy still grows the content and scrolls.

## The ambient (correctness, not cosmetics — stated plainly)

The scene shipped with `m_AmbientMode: 0` (a daylight skybox) and
`reflectionIntensity: 1`, the same defect the menu and ReadingRoom carried. It is
now the Flat near-black contract like theirs.

Unlike those two screens, **this change is not visible here**, and the phase does
not pretend otherwise. The Result canvas is Screen-Space **Overlay** and its
camera clears to a solid near-black colour, so the daylight ambient was lighting
only 3D dressing (`MP_ParlorBackdrop`, `MP_ResultCloth`) that never reaches the
frame. Before/after captures are identical. The value is that a midnight product
no longer carries daylight-sky ambient in its data and all three scenes' render
settings finally match. The four tuned Result lights are left untouched - they
touch only off-frame dressing.

## Verification

`Phase51ResultTmpTests`: no legacy Text survives, the presenter points at TMP,
the Flat ambient contract holds. Plus the legacy Result tests flipped to the TMP
path (alignment reads move from `TextAnchor` to `TextAlignmentOptions`). EditMode
and PlayMode green; before/after capture confirms the text renders and the scene
look is unchanged.

## The migration is complete

MainMenu, ReadingRoom, and Result are all TMP SDF now. No legacy `UI.Text` or
`InputField` remains in any shipping screen.

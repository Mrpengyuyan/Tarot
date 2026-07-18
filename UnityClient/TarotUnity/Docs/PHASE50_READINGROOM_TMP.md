# Phase 50 — The ReadingRoom Gets Sharp Text

Date: 2026-07-18

Phase 43 moved the menu to TextMeshPro SDF and stopped there, because the menu
holds no input field. The ReadingRoom — the screen the player actually sits in —
still ran on legacy `UI.Text`: 15 components that rasterise each glyph into a
bitmap atlas at one size and soften the instant the canvas scales up to the
built resolution. Their outlines were four offset copies of the mesh, the muddy
edge you see on every legacy title. This phase finishes the migration.

## What changed

```
              legacy Text   TMP    input field
MainMenu      0             7      (none)
ReadingRoom   15 -> 0       15     InputField -> TMP_InputField
Result        13            0      (next)
```

All 15 Text components convert in place — the RectTransform, sibling order,
content, size, alignment and colour carry over; the legacy `Outline`/`Shadow`
mesh tricks are dropped for the SDF shader's real, resolution-independent edge.

## The piece the menu never had: the question field

The menu converted cleanly because it has no `InputField`. The ReadingRoom's
question field is the one element that can't be swapped by replacing a component
on the same object: a legacy `InputField` and a `TMP_InputField` have different
child hierarchies (TMP wants a masked *Text Area* holding a TMP text view and a
TMP placeholder). So the field is **rebuilt from `TMP_DefaultControls`** — TMP's
own factory, which produces the correct structure — then reseated into the old
field's RectTransform, background sprite and sibling slot, and the old
GameObject deleted. Preserved across the swap: single-line type, content type,
character limit, background, placeholder copy.

Two things improved while it was open:

- **The caret is now visible.** The legacy caret defaulted to near-black on the
  near-black input ground — invisible. It now uses the ivory the typed text uses.
- **The placeholder reads as a prompt**, italic and muted, not as an answer.

## Source changes (not just the scene)

- `ReadingRoomController`: the four serialized fields change type —
  `InputField -> TMP_InputField`, three `Text -> TMP_Text`. Both expose the same
  `.text` the controller already read, so only the declarations move.
- `TarotUiTheme`: gains `ApplyTmpInputStyle(TMP_InputField)`, the TMP counterpart
  of the legacy input styler the menu never exercised. The legacy paths stay —
  the Result screen still has legacy Text until its own phase.

## Verification

`Phase50ReadingRoomTmpTests`: no legacy Text or InputField survives the canvas,
the field is a real `TMP_InputField` with a non-empty placeholder, and the
controller re-points to the TMP components. EditMode green, plus a scene capture
to confirm the text renders (dynamic SDF atlas resolves the CJK glyphs at
runtime).

## Next on this screen / product

The Result screen is the last legacy holdout: 13 legacy Text **and** the same
skybox-ambient defect Phase 42/49 already fixed twice. It is the natural next
cut — same two problems, one more screen.

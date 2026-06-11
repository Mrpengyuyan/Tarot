# Phase 12 Card-First Reveal Design

Date: 2026-05-28

## Decision

The approved Phase 12 direction is **B. Card-First Reveal**.

Phase 12 should make the tarot cards, especially the moment after flip, feel like the center of the game. The table remains important, but it supports card reveal drama instead of competing with it. This phase is still a Unity implementation pass, not a final imported real-deck-art pass.

## Product Goal

The active vertical slice remains:

```text
Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result
```

The player should feel that the game is moving toward a premium 3D card game:

- larger and more readable cards
- stronger flip focus and reveal staging
- visible face-art space on every revealed card
- result screen that preserves card presence instead of becoming a plain report
- a clean path to later replace placeholders with licensed real tarot artwork

## Non-Goals

Phase 12 will not add history, settings, profile, admin, or backend dashboard features.

Phase 12 will not import the complete real tarot deck yet. Real tarot artwork requires source discovery, license review, image naming, texture import decisions, and a reliable card-name-to-asset mapping.

Phase 12 will not rebuild the whole UI framework. It should work with the current scenes, prefabs, `CardView`, `CardFlipController`, `DeckController`, `ReadingRoomController`, `ResultPanelPresenter`, and existing tests.

## Visual Design

### Reading Room

The Reading Room becomes the card reveal stage:

- The dealt cards are visually larger and sit closer to the center of the composition.
- One active card receives a stronger reveal glow and becomes the player's focus during flip.
- Side cards can remain visible, but should support the focused card rather than forming a flat row.
- The question input and action controls stay readable, but they should not dominate the table.
- The table area should frame the cards with a stage-like surface, not a generic panel stack.

### Card Face

The card front needs a dedicated artwork region:

- A central `SpriteRenderer` or equivalent face-art holder for placeholder artwork now.
- The existing title and position labels remain as fallbacks.
- The placeholder art must make it obvious where real tarot art will go.
- The face-art holder must be stable across one-card and three-card readings.

### Result Scene

The Result scene should continue showing the reading text, but the cards must remain part of the scene:

- A card showcase area appears near the interpretation text.
- It should suggest "these are the cards that produced this answer."
- Text stays readable at 1280x720.
- The result layout should look like a revealed card reading, not a document page.

### Main Menu

Main Menu changes should be minimal in Phase 12:

- Keep the current ritual-table mood.
- Avoid spending Phase 12 effort on menu decoration beyond what is needed to keep the visual language consistent.

## Architecture

### Editor Bootstrapper

Add a Phase 12 editor bootstrapper following the established project pattern:

```text
Assets/Editor/Phase12CardRevealBootstrapper.cs
```

Responsibilities:

- Call the previous visual setup as needed, without undoing Phase 11 adjustments.
- Upgrade `PF_TarotCard` with Phase 12 face-art placeholders and reveal-stage anchors.
- Upgrade `ReadingRoom.unity` with card-first reveal stage anchors.
- Upgrade `Result.unity` with result card-showcase anchors.
- Create any small deterministic placeholder materials or textures needed for the card face-art slot.
- Save assets and scenes.

### Runtime Boundaries

Prefer small runtime additions only where they remove real coupling:

- `CardView` can receive optional serialized fields for face-art display.
- A small `CardArtworkCatalog` or equivalent can be introduced only if Phase 12 needs a stable mapping interface for later real art.
- If a card has no artwork, it must continue using existing text labels and placeholder visuals.

Avoid tying the card-art placeholder to backend calls. Backend/local data already reaches cards through `CardDrawData`; artwork mapping should be a presentation concern.

## Data Flow

Existing flow:

```text
Backend/local draw -> CardDrawData -> DeckController.DealCards -> CardView.Bind -> CardFlipController.Flip
```

Phase 12 should extend this flow at `CardView.Bind` or a nearby presentation layer:

```text
CardDrawData.tarot_card id/name -> optional artwork lookup -> face-art SpriteRenderer
```

Fallback path:

```text
No artwork found -> show placeholder face-art panel + title label + position label
```

This keeps the vertical slice playable before real artwork is imported.

## Error Handling

- Missing card-art catalog: fall back to placeholder face-art slot.
- Missing card sprite: fall back to placeholder face-art slot.
- Missing optional Phase 12 anchors: tests should fail in EditMode.
- Missing required existing references such as `CardView`, `DeckController`, or `ReadingRoomController`: existing tests should continue to catch this.
- Screenshot capture for visual review must run with graphics enabled; `-nographics` is only for automated tests, not screenshot generation.

## Tests

Use test-first implementation for Phase 12.

Add EditMode tests that initially fail before implementation:

- `Phase12CardRevealTypesExist`
- `CardPrefabHasFaceArtworkPlaceholder`
- `ReadingRoomHasCardFirstRevealStage`
- `ResultSceneHasCardShowcase`
- `Phase12DoesNotBreakPhase11ScreenshotReviewArtifacts`

Run after implementation:

- Phase 12 targeted EditMode tests
- full Unity EditMode tests
- full Unity PlayMode tests
- backend pytest if runtime/backend-facing code changes
- scene/prefab missing-reference scan
- Unity log scan for compile errors, exceptions, and Console errors

The PlayMode vertical slice must still verify:

```text
Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result
```

## Acceptance Criteria

Phase 12 is acceptable only when:

- `PF_TarotCard` has a visible, test-covered face-art placeholder region.
- ReadingRoom has a card-first reveal stage with stronger focus on flipped cards.
- Result has a card-showcase area tied visually to the reading.
- Existing Phase 2-11 tests still pass.
- PlayMode vertical slice still passes.
- No missing script references are introduced in scenes or prefabs.
- The documentation clearly states that real tarot artwork import remains a follow-up licensing/source task.

## Image Generation Use

Generated images may be used for:

- target-direction reference art
- non-final background mood boards
- placeholder card-face frames
- animation-frame exploration

Generated images should not be treated as final tarot deck art unless their usage rights and source constraints are reviewed. If any generated bitmap becomes a project asset, it must be copied into the Unity workspace and referenced from there.

## Open Implementation Notes

The first implementation should stay modest:

- improve card-face presentation and reveal staging
- add durable placeholder slots for later real tarot artwork
- avoid broad scene rewrites
- avoid introducing a large asset pipeline before the deck source is chosen

The next dedicated asset phase can then handle real tarot artwork discovery, license review, import settings, and card mapping.

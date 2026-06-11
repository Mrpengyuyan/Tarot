# Phase 2 Graybox Vertical Slice

## Scope

This phase makes the first local playable Unity flow. It still avoids backend integration, final art, history, settings, profile, and packaging.

Playable path:

`Boot -> MainMenu -> ReadingRoom -> Result`

## Implemented Flow

1. Start from `Boot` or `MainMenu`.
2. Press `Start Reading`.
3. Choose `One Card` or `Three Cards`.
4. Enter a question or leave the default placeholder question.
5. Press `Draw`.
6. Cards deal from the deck stack into spread slots.
7. Click each card to flip it.
8. Press `Reveal Result`.
9. Result scene reads the local session snapshot and shows placeholder interpretation text.

## Runtime Scripts Added

- `MainMenuController`
- `ReadingRoomController`
- `ResultSceneController`
- `CardClickHandler`
- `LocalReadingSimulator`
- `ReadingSessionSnapshot`

## Test Coverage

EditMode test:

- `ReadingFlowControllerTests.RegisterCardFlippedMovesToResultReadyAfterSelectedSpreadCardCount`

This locks the core Phase 2 transition: selected card count -> all cards flipped -> `ResultReady`.

## Manual Editor Notes

The graybox assets can be regenerated from:

`Tools -> Tarot Unity -> Run Phase 2 Graybox Bootstrap`

The generated assets are intentionally simple and replaceable:

- flat block cards
- block deck stack
- simple spread slots
- UGUI text/buttons
- local placeholder interpretation data


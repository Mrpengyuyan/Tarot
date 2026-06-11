# Phase 11 Visual Review And Final Adjustment

Date: 2026-05-27

## Purpose

Phase 11 creates a repeatable screenshot baseline before the next beauty pass. The goal is to make visual problems concrete instead of relying on memory from the Unity Editor.

The long-term target remains a Hearthstone-like 3D card-table presentation, but this phase does not import the final deck art yet.

## Captured Screens

- Main Menu: `Docs/VisualReview/Phase11/MainMenu.png`
- Reading Room: `Docs/VisualReview/Phase11/ReadingRoom.png`
- Result: `Docs/VisualReview/Phase11/Result.png`

Capture note: run `TarotUnity.Editor.Phase11VisualReviewBuilder.Run` in Unity batchmode with graphics enabled. Do not add `-nographics`, because that can produce uniform placeholder screenshots instead of actual scene renders.

## Review Notes

- Main Menu must sell the tarot-table fantasy within the first viewport.
- Reading Room must keep spread select, question input, shuffle/draw, flip cards, and result reveal readable at 1280x720.
- Result must feel like a resolved reading, not a plain text report.
- Flipped card faces should later use real tarot card artwork instead of placeholder text or generic symbols.
- The real tarot card artwork task needs source and license review before assets are imported into the release package.

## Screenshot Review Findings

- Main Menu has the clearest mood, but the central seal and button overlap into a flat block; later passes should separate title, emblem, table, and primary action with more depth.
- Reading Room is functional, but the top workflow buttons and question panel feel like stacked UI bars above the table; later passes should make the table and deck/card area the visual center.
- Result is readable but still report-like. It needs a stronger result panel composition, card presence, and staged hierarchy before it will feel like a finished game screen.

## Adjustments Applied

- Main Menu: separated the title, crest, action rail, and table shadow so the first screen has more depth.
- Reading Room: added a table focus frame and action dock, then lowered result/status copy away from the card table.
- Result: added a left-side card-presence panel and shifted reading text into a clearer right-side column.

## Deferred Asset Task

Before importing card art, choose a deck source, verify usage license, define image naming, map backend/local card names to assets, and decide texture import settings for desktop builds.

## Current Limitations

- The interface is still a prototype visual pass and is not yet the final 3D card-game look.
- Windows executable smoke testing still needs a Windows machine.

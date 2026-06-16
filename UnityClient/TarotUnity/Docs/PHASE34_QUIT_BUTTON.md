# Phase 34 Main-Menu Quit Button

Date: 2026-06-16

## Purpose

The desktop builds had no in-game way to exit - players had to use the OS (Cmd+Q /
Alt+F4). This adds a quit affordance to the main menu. It is a small, objective UX
gap fix, not a visual/taste change.

## What Changed

- `MainMenuController` gained a `quitButton` field, wired the same way as the start
  button (runtime `AddListener` in `Awake`, removed in `OnDestroy`), and a
  `QuitGame` handler that exits the player (`Application.Quit`) or stops play mode
  in the editor.
- `Phase34QuitButtonBootstrapper` clones the existing `StartReadingButton` so the
  styling stays identical to the menu's primary button (Phase 30 consistency),
  renames it `QuitButton`, relabels it `退出占卜`, clears the inherited click
  wiring, places it as a stacked secondary action centered below the start plate
  and its status line (computed from the live rects, ~30px gap), and assigns it to
  `MainMenuController.quitButton`. The bootstrap is idempotent.

## Verification

- `Phase34QuitButtonTests`: the QuitButton exists, is horizontally centered like the
  start button, sits below it without overlapping the start plate or the status
  line, carries a 退出 label, is assigned on the controller, and the `QuitGame`
  handler exists.
- Visual: re-rendered `Docs/VisualReview/Phase31_HDArchive/01_MainMenu.png` shows
  开始占卜 and 退出占卜 stacked and consistently styled.
- EditMode and PlayMode suites green.

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 34 Quit Button Bootstrap`.

## Remaining Limitation

Phase 34 is a functional UX fix, not final high-end VFX. The quit button currently
mirrors the primary button's prominence for consistency; de-emphasizing it
(smaller, quieter styling) or adding a confirm step are easy taste-driven tweaks.

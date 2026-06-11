# Phase 7 Immersive UI Design

Date: 2026-05-26

## Direction

The selected direction is `A. Immersive Ritual Desktop`.

Phase 7 focuses on making the current Unity vertical slice feel like a real downloadable game prototype instead of a bootstrap demo. It does not add history, settings, account management, admin, or new backend scope.

## Target Player Experience

The Windows release goal is:

```text
Download zip -> unzip -> double-click TarotUnity.exe -> immediately see a polished tarot game flow
```

The active game flow remains:

```text
Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result
```

## Visual Principles

- First screen must look like an intentional game scene, not a URP sample or UI test.
- The tarot table, cards, warm light, and ritual pacing should be the first impression.
- Menus should be minimal and atmospheric.
- Controls should be obvious, but not styled like a desktop admin panel.
- UI text should be Chinese-first for this prototype.
- The result screen should read comfortably and feel like a reveal, not a raw API response dump.

## Scene Design

### MainMenu

- Full-screen ritual backdrop with a table/card visual motif.
- Large Chinese title and short subtitle.
- One clear primary action: start reading.
- Remove demo wording such as `Graybox` and `Local graybox mode`.

### ReadingRoom

- The card table remains the center of the screen.
- Spread selection, question input, and draw controls become a compact ritual control band.
- Add progress markers for the main loop:
  - choose spread
  - ask question
  - draw cards
  - flip cards
  - reveal result
- Status copy should guide the user in Chinese.

### Result

- Interpretation appears inside an oracle-style reading frame.
- Sections should be visually separated:
  - question
  - spread
  - summary
  - overall interpretation
  - card analysis
  - advice
- Back to menu remains available but secondary.

## Implementation Strategy

Add a Phase 7 editor bootstrapper that upgrades existing scenes and reusable UI styling without replacing the gameplay controllers. This keeps the current tested flow intact while allowing repeated regeneration of the immersive UI pass.

The first implementation pass should:

- add scene-level immersive UI roots and labels
- restyle existing buttons, panels, and input fields
- localize visible prototype text to Chinese
- add stable tests that prove the demo wording is gone and immersive UI anchors exist
- rebuild the desktop prototype after tests pass

## Acceptance Criteria

- MainMenu no longer looks like a default/demo screen.
- ReadingRoom clearly presents spread, question, draw, flip, and reveal steps.
- Result screen has a more polished reading layout.
- The existing vertical slice still passes automated PlayMode verification.
- EditMode and PlayMode tests pass.
- Windows build still produces `Builds/Desktop/Windows/TarotUnity.exe`.
- Windows release zip exists at `Builds/Desktop/Release/TarotUnity-Windows-x64.zip`.

## Current Implementation

Implemented in:

- `Assets/Editor/Phase7ImmersiveUiBootstrapper.cs`
- `Assets/Tests/EditMode/Phase7ImmersiveUiTests.cs`

Scene updates are regenerated with:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -enableUnityConnectPrefs false \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -executeMethod TarotUnity.Editor.Phase7ImmersiveUiBootstrapper.Run \
  -quit
```

Latest local verification:

- Phase 7 RED check: 4 failed before implementation.
- Phase 7 GREEN check: 4 passed after implementation.
- Full EditMode: passed.
- Full PlayMode: passed.
- Windows desktop build: passed.
- macOS desktop build: passed.

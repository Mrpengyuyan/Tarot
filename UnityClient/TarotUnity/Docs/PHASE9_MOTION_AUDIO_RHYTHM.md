# Phase 9 Motion And Audio Rhythm

Date: 2026-05-26

## Direction

Phase 9 starts moving the prototype toward a 3D card-game feel.

It does not attempt final Hearthstone-level art. Instead, it establishes the rhythm layer that a polished 3D interface needs: paced shuffle, heavier card travel, more deliberate flip timing, safe sound cue routing, and staged result reveal.

## Scope

Included:

- timing profile for shuffle, draw, flip, and result reveal
- reading-room rhythm director anchor
- stronger card deal arc and settling
- flip pacing suitable for a card-game reveal
- scene wiring for cue order and audio-safe feedback
- automated tests proving the rhythm layer exists

Excluded:

- final 3D card models
- final VFX/sound assets
- history/settings/admin/profile pages
- backend API expansion

## Acceptance Criteria

- ReadingRoom has a Phase 9 rhythm director object.
- ReadingRoomController is wired to the rhythm director.
- DeckController uses slower, weightier card motion values.
- CardFlipController uses a more deliberate reveal cadence.
- ResultRevealDirector uses staged reveal timing with breathing room.
- Full EditMode and PlayMode checks still pass.

## Implementation

Implemented in:

- `Assets/Scripts/Presentation/RitualRhythmDirector.cs`
- `Assets/Editor/Phase9MotionAudioBootstrapper.cs`
- `Assets/Tests/EditMode/Phase9MotionAudioRhythmTests.cs`
- `Assets/Scripts/Gameplay/DeckController.cs`
- `Assets/Scripts/Gameplay/CardFlipController.cs`
- `Assets/Scripts/UI/ReadingRoomController.cs`

Regenerate the Phase 9 scene and prefab pass with:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -enableUnityConnectPrefs false \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -executeMethod TarotUnity.Editor.Phase9MotionAudioBootstrapper.Run \
  -quit
```

## Latest Verification

- Phase 9 RED check: 4 failed before implementation.
- Phase 9 GREEN check: 4 passed after implementation.
- Full EditMode: 38 passed, 0 failed.
- Full PlayMode: 2 passed, 0 failed.
- Backend tests: 161 passed, 1 third-party warning.
- Windows desktop build: passed.
- macOS desktop build: passed.
- Windows release zip integrity: passed.

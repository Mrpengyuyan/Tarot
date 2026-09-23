# Phase 68 — Reading Room Table

Design: `docs/superpowers/specs/2026-09-23-reading-room-parlor-design.md` (repo root).

## Why earlier reading-room shots were misleading

Every earlier reading-room capture rendered the Main Camera where the scene saves it,
`(0, 2.85, -4.15)` at FOV 40. But `ReadingRoomController.Start()` calls
`CameraChoreographyController.PlayOpening()`, which moves the camera to `defaultPose`
(`(0, 2.45, -3.55)`, pitch 32.2°, FOV 36) within 0.75 s. The player sits at the controller's
poses, not at the saved transform.

`Editor/Phase68ParlorCaptureBuilder.cs` renders what the player sees: every pose the
controller can move to (default, deck, one card, three cards, result, and each registered
spread pose such as the Celtic Cross), each at its own field of view, plus the saved camera
for comparison and the default and three-card poses without UI. Each pose shows the sockets
of the spread it is seen with in play. Set `PHASE68_CAPTURE_DIR` to render somewhere other
than `Docs/VisualReview/Phase68/`.

From the real poses the frame is almost all table. They showed two problems the saved-camera
shots hid.

## Only the selected spread's sockets show

Phase 63 added ten Celtic sockets beside the four original ones, and nothing hid either set,
so all fourteen outlines were drawn on top of each other in every spread.

- `Scripts/Presentation/SpreadSocketVisibility.cs` groups the sockets by card count
  (1 → `MP_Socket_OneCardSlot`; 3 → Past / Present / Advice; 10 → `MP_Socket_Celtic_00..09`)
  and shows only the selected group.
- It listens to `ReadingFlowController.SpreadSelected`, a new event raised on every spread
  selection. `StateChanged` is not enough: `SelectSpread` moves the flow to `QuestionInput`,
  and `SetState` returns early when the state is unchanged, so switching spreads while writing
  the question raised nothing.
- Only each `MP_Socket_*` object's own active flag is switched. `MP_CardSockets` and
  `MP_CelticSockets` stay active and keep their children:
  `Phase38TableRebuildTests` counts four children under `MP_CardSockets`, and
  `Phase63SpreadDefinitionTests` finds the Celtic group with `GameObject.Find`, which skips
  inactive objects.
- The scene is saved with one card selected, the state the room opens in.

## The play area is lit

The centre of the table, where the cards land, was the darkest part of the frame: the only
table light, `MP_RoomFill`, is a weak point light kept for letting gold read as gold, and the
cloth is deliberately dark.

`MP_TableStage/MP_TablePool` is a spot straight above the card row:

| Setting | Value |
|---|---|
| Position | `(0, 4.0, 0.3)`, pointing straight down |
| Inner / outer angle | 40° / 75° |
| Range | 8 |
| Colour | `(1, 0.8, 0.58)` |
| Intensity | 60 |
| Shadows | none (the front candles cast them) |

Measured on the default pose without UI (mean luminance of 255): the centre cloth went from
4.2 to 39.0, while the cloth by the front-right candle stays near 18.8 and the far band near
1.0. The deck face went from 19.5 to 21.9. The sockets are unlit materials, so they read as
recesses darker than the lit cloth, and their gold outlines do not bloom.

The pool reads as a lamp hanging above the table, out of frame. It is a deliberate, single
exception to Phase 49's candles-only room, which removed seven lights that washed the table
flat. `MP_RoomFill` is unchanged.

## Order and re-running

`Editor/Phase68TableBootstrapper.cs` (menu: Tools/Tarot Unity/Run Phase 68 Table Bootstrap)
runs after the Phase 38, 49 and 63 bootstrappers and can be run again with the same result.
Running Phase 49 again does not touch `MP_TablePool` or the socket visibility.

## Not done

- No new tests: the user checks this phase in Unity.
- The socket material is unchanged.
- Camera, poses and UI are unchanged.

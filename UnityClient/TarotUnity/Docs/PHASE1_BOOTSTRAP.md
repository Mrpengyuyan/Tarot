# Phase 1 Bootstrap

## Scope

Phase 1 turns the generated URP project into a stable Unity workspace for the first vertical slice. It does not implement history, settings, profile, admin, asset packaging, or final art.

V1 flow:

`MainMenu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result`

## Asset Folders

Keep first-pass assets under these folders:

```text
Assets/
  Art/
  Audio/
  Materials/
  Prefabs/
    Cards/
    Gameplay/
    UI/
  Scenes/
  Scripts/
    Core/
    Data/
    Gameplay/
    Network/
    UI/
  ScriptableObjects/
    Spreads/
  UI/
  VFX/
```

## Scenes

- `Boot`: creates persistent bootstrap services, then loads `MainMenu`.
- `MainMenu`: entry scene with start/login hooks only.
- `ReadingRoom`: spread select, question input, shuffle, draw, card flip, and reveal staging.
- `Result`: interpretation reveal and summary presentation.

Build Settings order should be:

1. `Boot`
2. `MainMenu`
3. `ReadingRoom`
4. `Result`

## Prefabs

- `PF_TarotCard`: card visual root with `CardView` and `CardFlipController`.
- `PF_DeckStack`: deck table object with `DeckController`.
- `PF_SpreadSlot`: anchor object for card target positions.
- `PF_ResultPanel`: result UI root with `ResultPanelPresenter`.

## First Script Inventory

- `Assets/Scripts/Core/GameBootstrap.cs`
- `Assets/Scripts/Core/SceneFlowManager.cs`
- `Assets/Scripts/Core/AudioManager.cs`
- `Assets/Scripts/Gameplay/DeckController.cs`
- `Assets/Scripts/Gameplay/CardView.cs`
- `Assets/Scripts/Gameplay/CardFlipController.cs`
- `Assets/Scripts/Gameplay/SpreadLayoutController.cs`
- `Assets/Scripts/Gameplay/ReadingFlowController.cs`
- `Assets/Scripts/UI/ResultPanelPresenter.cs`
- `Assets/Scripts/Network/ApiClient.cs`

Supporting contracts:

- `Assets/Scripts/Data/ApiContracts.cs`
- `Assets/Scripts/Network/ApiRoutes.cs`

Editor bootstrap helper:

- `Assets/Editor/Phase1AssetBootstrapper.cs`

## Naming Rules

- Scenes: PascalCase, no prefix, for example `ReadingRoom`.
- Prefabs: `PF_` prefix, for example `PF_TarotCard`.
- ScriptableObjects: `SO_` prefix, for example `SO_ThreeCardSpread`.
- Materials: `MAT_` prefix.
- VFX prefabs: `FX_` prefix.
- Audio clips: `BGM_` for loops and `SFX_` for one-shots.
- Runtime scripts: PascalCase class name matching file name.

## Unity V1 Backend API Scope

Base path is `/api/v1`.

| Endpoint | Unity V1 use | Request notes |
| --- | --- | --- |
| `POST /login` | Login and receive bearer token | OAuth2 form fields: `username`, `password`; response includes `access_token`. |
| `GET /users/me` | Check current user/session | Use `Authorization: Bearer <token>` for Unity V1. |
| `POST /refresh` | Refresh session when using cookies | Requires refresh cookie plus `X-CSRF-Token`; lower priority than bearer flow. |
| `POST /logout` | Clear backend cookies/session | Optional in first graybox; useful once login UI exists. |
| `GET /spreads/` | Populate spread selection | Optional query filters: `difficulty`, `card_count`, `question_type`, `beginner_friendly`, `active_only`. |
| `GET /spreads/{spread_id}` | Load positions and card count | Needed before building spread slot anchors. |
| `POST /records/` | Create a reading record | JSON: `question`, `question_type`, `spread_type_id`. |
| `POST /records/{prediction_id}/draw` | Persist backend card draw | Optional `seed` query for deterministic tests. |
| `GET /records/{prediction_id}/cards` | Fetch drawn cards with meanings | Used to bind cards after draw. |
| `POST /records/{prediction_id}/interpret` | Trigger AI interpretation | Send no body for backend-generated AI interpretation. |
| `GET /records/{prediction_id}` | Fetch final detail | Includes spread, draws, and interpretation. |

Unity V1 should prefer bearer auth from `/login` first. Cookie + CSRF support is kept in `ApiClient` as a later hardening path because `/refresh` depends on backend cookies.

## Editor Steps Still Needed

If the batch bootstrap command succeeds, Unity will already have the initial scenes, prefabs, and Build Settings entries.

If it does not run on this machine, open Unity and run:

`Tools -> Tarot Unity -> Run Phase 1 Bootstrap`

Then verify:

- `Assets/Scenes/Boot.unity`
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/ReadingRoom.unity`
- `Assets/Scenes/Result.unity`
- `Assets/Prefabs/Cards/PF_TarotCard.prefab`
- `Assets/Prefabs/Gameplay/PF_DeckStack.prefab`
- `Assets/Prefabs/Gameplay/PF_SpreadSlot.prefab`
- `Assets/Prefabs/UI/PF_ResultPanel.prefab`


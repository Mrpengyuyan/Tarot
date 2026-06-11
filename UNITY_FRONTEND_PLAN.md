# Unity Frontend Plan

## Goal

Use Unity to rebuild the game's presentation layer while keeping the existing FastAPI backend.

The target is not "port the whole web app immediately".
The target is "build a polished Unity vertical slice that feels like a real tarot game".

## Product Direction

What Unity should improve:

- atmosphere
- card dealing rhythm
- flip reveal performance
- camera movement
- particles and transitions
- music and sound effects
- sense of ritual before the interpretation appears

What stays in the backend:

- account auth
- record creation
- card draw persistence
- AI interpretation
- history records
- API key security

## First Deliverable

Build one playable vertical slice:

`Main Menu -> Spread Select -> Question Input -> Shuffle / Draw -> Flip Cards -> Interpretation Reveal`

Do not rebuild history, profile, admin, or settings first.

## Required Tools

Install:

- Unity Hub
- Unity 6.3 LTS
- a new project using URP

Recommended Unity packages for this project:

- TextMeshPro
- Input System
- Cinemachine
- Addressables (later, when assets start growing)

## Recommended Repo Layout

When the Unity project is created, place it inside this repo:

```text
UnityTarot/
  UnityClient/
    TarotUnity/
```

Recommended Unity folders:

```text
Assets/
  Art/
  Audio/
  Materials/
  Prefabs/
  Scenes/
  Scripts/
    Core/
    Data/
    Gameplay/
    UI/
    Network/
  ScriptableObjects/
  UI/
  VFX/
```

## Milestones

### Phase 0: Setup

Definition of done:

- Unity Hub installed
- Unity 6.3 LTS installed
- new URP project created
- project opens normally
- TextMeshPro imported
- Input System enabled
- Cinemachine installed

Tasks:

1. Create the Unity project.
2. Confirm target platforms: macOS/Windows first.
3. Set default resolution and quality presets.
4. Create base folders.
5. Add a README inside `UnityClient/` describing editor version and package choices.

### Phase 1: Graybox Vertical Slice

Definition of done:

- there is a simple playable flow with placeholder art
- cards can be dealt and flipped
- camera moves during reveal
- a placeholder interpretation panel appears at the end

Scenes:

- `Boot`
- `MainMenu`
- `ReadingRoom`
- `Result`

Tasks:

1. Build a graybox tarot table scene.
2. Create a `TarotCard` prefab.
3. Create a `Deck` object with shuffle animation timing.
4. Create spread slot anchors for 1-card and 3-card layouts first.
5. Add one reveal camera shot and one result camera shot.
6. Add placeholder particles for card flip and final reveal.
7. Add temporary background loop and basic SFX.

### Phase 2: Playable Presentation Pass

Definition of done:

- the flow feels ceremonial
- the user can clearly feel "draw", "reveal", and "result"
- timing is pleasant even without final art

Tasks:

1. Improve table lighting and color mood.
2. Add transitions between menu, reading room, and result.
3. Add card hover, select, and flip feedback.
4. Add reveal pacing:
   - shuffle
   - pause
   - deal
   - pause
   - flip
   - final interpretation reveal
5. Add sound layering:
   - ambient loop
   - card movement
   - flip impact
   - interpretation reveal cue

### Phase 3: Backend Integration

Definition of done:

- Unity authenticates against FastAPI
- Unity can create a reading, draw cards, request interpretation, and fetch the result

Tasks:

1. Build a Unity API client with cookie support.
2. Add login flow.
3. Load spreads from backend.
4. Create a reading record from Unity.
5. Trigger backend draw.
6. Trigger backend interpretation.
7. Load interpretation detail and show it in the result scene.

### Phase 4: Content and Packaging

Definition of done:

- first downloadable build exists
- core flow is stable
- visuals are coherent

Tasks:

1. Replace placeholder card art and VFX.
2. Improve typography and Chinese text readability.
3. Add build profile for desktop.
4. Add config for backend URL.
5. Add error handling and reconnect flows.

## Scene Checklist

### Boot

Purpose:

- initialize settings
- load configuration
- optionally warm up auth/session state

Needed objects:

- `Bootstrapper`
- `ConfigLoader`
- `SceneFlowManager`

### MainMenu

Purpose:

- enter the experience cleanly

Needed UI:

- title
- start reading button
- login button or auto-login state
- optional ambience-only idle presentation

### ReadingRoom

Purpose:

- the core game scene

Needed systems:

- tarot table
- spread layout controller
- deck controller
- card spawn/deal controller
- flip interaction controller
- camera shot controller
- reading state controller

### Result

Purpose:

- interpretation reveal and emotional payoff

Needed UI:

- question recap
- spread name
- drawn cards summary
- overall interpretation
- card analysis
- advice
- warning
- continue / back button

## Prefab Checklist

Create these first:

- `PF_TarotCard`
- `PF_DeckStack`
- `PF_SpreadSlot`
- `PF_ResultPanel`
- `PF_FlipVFX`
- `PF_RevealVFX`

Card prefab should support:

- front art
- back art
- upright/reversed state
- highlight state
- flip animation state
- selected state

## Script Checklist

Create these scripts first:

### Core

- `GameBootstrap`
- `SceneFlowManager`
- `AudioManager`
- `ConfigService`

### Data

- `SpreadDefinition`
- `TarotCardData`
- `ReadingSessionData`

### Gameplay

- `DeckController`
- `CardView`
- `CardFlipController`
- `SpreadLayoutController`
- `ReadingFlowController`
- `RevealSequenceController`
- `CameraShotController`

### UI

- `MainMenuPresenter`
- `QuestionInputPanel`
- `SpreadSelectPanel`
- `ResultPanelPresenter`
- `LoadingOverlay`

### Network

- `ApiClient`
- `AuthApi`
- `SpreadApi`
- `ReadingApi`

## Backend Endpoints Needed for Unity V1

Use only this minimal set first.

### Auth

- `POST /api/v1/login`
- `GET /api/v1/users/me`
- `POST /api/v1/refresh`
- `POST /api/v1/logout`

Notes:

- backend currently uses cookie auth plus CSRF protection
- Unity client will need to store cookies and send CSRF header for unsafe methods

### Spread Loading

- `GET /api/v1/spreads/`
- `GET /api/v1/spreads/{spread_id}`

Use this to render:

- spread name
- description
- card count
- positions
- difficulty

### Reading Flow

- `POST /api/v1/records/`
- `POST /api/v1/records/{prediction_id}/draw`
- `GET /api/v1/records/{prediction_id}/cards`
- `POST /api/v1/records/{prediction_id}/interpret`
- `GET /api/v1/records/{prediction_id}`

Create reading payload:

```json
{
  "spread_type_id": 1,
  "question": "我最近的感情会怎么发展？",
  "question_type": "love"
}
```

Draw response gives:

- `prediction_id`
- `card_draws[]`

Each card draw includes:

- `position`
- `is_reversed`
- `tarot_card`
- `position_name`
- `position_meaning`
- `card_meaning`

Interpretation response includes:

- `overall_interpretation`
- `card_analysis`
- `relationship_analysis`
- `advice`
- `warning`
- `summary`
- `key_themes`
- `model_used`

## Suggested Unity Integration Order

Do not connect everything at once.

Order:

1. hardcoded local mock data
2. local JSON mock data
3. backend spread list
4. backend reading creation
5. backend draw
6. backend interpretation
7. backend session refresh and logout

## Visual Design Direction

Choose one strong visual direction early and stay consistent.

Recommended direction for this project:

- deep blue / black / gold palette
- candlelight or moonlight mood
- subtle dust, glow, and rune particles
- elegant serif typography for titles
- clean readable body text for interpretation
- slow, intentional timing instead of flashy speed

Avoid:

- mobile hyper-casual look
- overly bright fantasy colors
- UI panels that still feel like a web dashboard

## Immediate Next Tasks for Codex

Once Unity is installed, the next work should happen in this order:

1. create the Unity project folder in this repo
2. write a Unity-side project README
3. generate the Unity folder structure
4. create the first scene plan
5. inventory the exact backend auth and reading flow for Unity cookie handling
6. scaffold the Unity API client design

## First Week Plan

### Day 1

- install Unity
- create URP project
- create folders
- create `Boot`, `MainMenu`, `ReadingRoom`, `Result`

### Day 2

- create tarot table graybox
- create card prefab
- create spread slot anchors

### Day 3

- implement deck deal and flip sequence
- implement one Cinemachine reveal shot

### Day 4

- implement mock result panel
- add placeholder audio and particles

### Day 5

- connect spread loading and reading creation to backend

### Day 6

- connect draw and interpretation endpoints

### Day 7

- polish pacing
- make first downloadable prototype build

## Notes About Unity Download

Yes, Unity needs to be installed locally.

Recommended install path:

1. install Unity Hub
2. sign in with a Unity account
3. install `Unity 6.3 LTS`
4. create a new `URP` project

Official references:

- Unity Hub: https://unity.com/en/unity-hub
- Install Hub: https://docs.unity.com/en-us/hub/install-hub
- Install Editor with Hub: https://docs.unity.com/en-us/hub/add-editor
- Unity release overview: https://unity.com/releases/release-overview
- UnityWebRequest: https://docs.unity3d.com/ja/2021.3/Manual/UnityWebRequest.html
- TextMeshPro: https://docs.unity3d.com/ja/2023.1/Manual/com.unity.textmeshpro.html
- Input System: https://docs.unity3d.com/ja/2023.2/Manual/com.unity.inputsystem.html
- Cinemachine: https://docs.unity3d.com/ja/Packages/com.unity.cinemachine%402.6/manual/index.html
- Addressables: https://docs.unity3d.com/ja/Packages/com.unity.addressables%401.20/manual/index.html

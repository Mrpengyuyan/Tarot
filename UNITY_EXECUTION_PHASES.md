# Unity Execution Phases

## Overview

This document is the practical execution plan for the Unity version.

It answers three questions:

1. what you need to do in Unity Hub
2. what I can do immediately after each setup step is complete
3. what the acceptance standard is for each phase

## Big Picture

We are not starting from "rewrite everything".

We are starting from:

`make one impressive Unity vertical slice that proves the tarot game should move to Unity`

The working directory for the Unity side is:

```text
/Users/maochuandou/BUPT/Game/UnityTarot
```

The recommended Unity project location is:

```text
/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
```

## What You Need To Do Right Now

In Unity Hub, do these steps:

1. Install `Unity 6.3 LTS`
2. Create a new project with the `URP` template
3. Save the project to:
   `/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity`

That is the minimum required step before I can begin real Unity-side implementation work.

## Do You Only Need To Download 6.3 LTS?

Not quite.

You need to complete these Unity-side actions:

1. Download and install `Unity 6.3 LTS`
2. Create the Unity project itself
3. Open the project once successfully
4. Confirm the project path is correct

After the project opens, I can start working against the Unity project structure.

You do not need to do the package setup manually yet unless Unity asks during project creation.
I can guide the next package steps after the project exists.

## Phase 0: Unity Installation And Project Creation

### Your Tasks

1. Install `Unity 6.3 LTS` in Unity Hub
2. Create `URP` project
3. Save it under `UnityTarot/UnityClient/TarotUnity`
4. Open the project once

### My Tasks After That

1. inspect the generated Unity project structure
2. create a Unity-side bootstrap plan
3. create the initial folder/file conventions if still missing
4. prepare first batch of C# scripts and scene responsibilities

### Exit Criteria

- Unity project exists on disk
- it opens successfully
- `Assets/`, `Packages/`, and `ProjectSettings/` are present

## Phase 1: Project Bootstrap

### Goal

Turn the empty Unity project into a structured project ready for gameplay work.

### My Tasks

1. create and organize Unity folders
2. create a project README
3. define first-pass scene structure
4. define core scripts
5. define naming rules for prefabs and ScriptableObjects

### Deliverables

- folder structure under `Assets/`
- project conventions document
- first script scaffold list

### Exit Criteria

- project structure is clean
- scenes and script folders are stable
- we are ready to build gameplay, not just setup

## Phase 2: Graybox Vertical Slice

### Goal

Make a playable placeholder version with no final art requirement.

### My Tasks

1. define and scaffold these scenes:
   - `Boot`
   - `MainMenu`
   - `ReadingRoom`
   - `Result`
2. define the first prefabs:
   - tarot card
   - deck stack
   - spread slot
   - result panel
3. define gameplay flow state transitions
4. implement graybox interaction scripts

### Target Experience

- enter main menu
- choose a spread
- enter a question
- trigger draw
- see cards dealt
- flip cards
- move to result reveal

### Exit Criteria

- the whole flow runs with placeholder visuals
- no backend dependency is required yet

## Phase 3: Presentation Pass

### Goal

Upgrade from "functional prototype" to "this already feels like a game".

### My Tasks

1. improve reveal pacing
2. add camera choreography
3. add particles
4. add sound trigger map
5. improve lighting and mood
6. improve result reveal staging

### Focus

- ritual feeling
- anticipation
- visual payoff
- emotional rhythm

### Exit Criteria

- the flow is already enjoyable even with placeholder art
- the game feel is noticeably stronger than the current web version

## Phase 4: Backend Integration

### Goal

Use the existing FastAPI backend as the data and AI layer.

### My Tasks

1. define Unity network layer
2. handle auth cookie and CSRF flow
3. connect spread loading
4. connect reading creation
5. connect draw endpoint
6. connect interpretation endpoint
7. connect reading detail fetch

### Backend Scope For V1

Only connect these first:

- `POST /api/v1/login`
- `GET /api/v1/users/me`
- `POST /api/v1/refresh`
- `POST /api/v1/logout`
- `GET /api/v1/spreads/`
- `GET /api/v1/spreads/{spread_id}`
- `POST /api/v1/records/`
- `POST /api/v1/records/{prediction_id}/draw`
- `GET /api/v1/records/{prediction_id}/cards`
- `POST /api/v1/records/{prediction_id}/interpret`
- `GET /api/v1/records/{prediction_id}`

### Exit Criteria

- Unity can complete one real reading end-to-end using backend data

## Phase 5: Art And UI Polish

### Goal

Replace placeholder presentation with a shippable visual style.

### My Tasks

1. refine fonts and text hierarchy
2. replace placeholder VFX/SFX
3. improve card presentation
4. improve menu and result UI
5. improve Chinese reading comfort and spacing

### Exit Criteria

- the Unity version feels cohesive and intentional

## Phase 6: Desktop Build

### Goal

Create the first downloadable build.

### My Tasks

1. define config strategy for backend URL
2. define desktop build settings
3. improve loading and error recovery
4. support first downloadable Windows/macOS build path

### Exit Criteria

- first desktop prototype build exists

## What I Can Start Now, Before Unity Is Fully Installed

I can already do these:

1. keep refining the execution documents
2. prepare Unity-side architecture notes
3. prepare backend integration mapping
4. prepare the first C# script inventory

I should not start writing real Unity project files yet unless the Unity project has been created, because otherwise we would be guessing against a folder structure that does not exist yet.

## What Happens Immediately After You Finish Installation

Once you tell me:

`Unity 6.3 LTS is installed and the URP project has been created`

I will move to the next concrete implementation step:

1. inspect the new Unity project
2. scaffold the project structure
3. create the first implementation checklist for scenes, prefabs, and scripts
4. begin the Unity V1 bootstrap work

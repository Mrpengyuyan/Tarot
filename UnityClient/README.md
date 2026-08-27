# Unity Client Workspace

This folder contains the Unity frontend for the Tarot project.

## Layout

```text
UnityClient/
  README.md
  TarotUnity/
```

The Unity project lives at:

```text
/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
```

Open the project with Unity `6000.3.16f1` and start from
`TarotUnity/Assets/Scenes/Boot.unity`.

## Project Boundary

Unity owns the desktop presentation layer, 3D table, card interaction, camera,
VFX, audio, and result UI. The existing FastAPI service owns authentication,
reading records, card draws, AI interpretation, rate limits, and API key
security.

## Main References

- [`PROJECT_COMPLETION_PLAN.md`](../PROJECT_COMPLETION_PLAN.md) — current
  project completion and release plan.
- [`UNITY_FRONTEND_PLAN.md`](../UNITY_FRONTEND_PLAN.md) — frontend direction and
  backend boundary.
- [`TarotUnity/README.md`](TarotUnity/README.md) — Unity project setup and play
  instructions.

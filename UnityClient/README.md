# Unity Client Workspace

Place the Unity frontend project here.

Recommended project name:

- `TarotUnity`

Recommended final structure:

```text
UnityTarot/
  UNITY_FRONTEND_PLAN.md
  UNITY_EXECUTION_PHASES.md
  UnityClient/
    README.md
    TarotUnity/
```

Recommended Unity project path:

```text
/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
```

Current status:

- `TarotUnity/` exists and has been bootstrapped for Phase 1.
- Unity editor version is recorded in `TarotUnity/README.md`.
- Phase 1 scene, prefab, script, and API scope are recorded in `TarotUnity/Docs/PHASE1_BOOTSTRAP.md`.
- Phase 2 graybox flow is recorded in `TarotUnity/Docs/PHASE2_GRAYBOX.md`.

Alternative shortened view:

```text
UnityClient/
  README.md
  TarotUnity/
```

This Unity project is planned to replace the current web presentation layer while keeping the existing FastAPI backend for:

- auth
- reading records
- card draws
- AI interpretation
- API key security

Before creating the project, read:

- [UNITY_FRONTEND_PLAN.md](../UNITY_FRONTEND_PLAN.md)

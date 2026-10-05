# Tarot Unity Client

Unity frontend for the Tarot desktop game. The current baseline is Unity
`6000.3.16f1`, URP, and release version `0.9.0`.

## Project Structure

- `Assets/Scenes/Boot.unity` is the editor entry scene.
- `Assets/Scenes/MainMenu.unity` is the menu and spread selection screen.
- `Assets/Scenes/ReadingRoom.unity` is the question, draw, and flip screen.
- `Assets/Scenes/Result.unity` is the multi-card result and interpretation screen.
- `Assets/Scripts/` contains runtime code grouped by core, data, gameplay,
  network, presentation, and UI responsibilities.
- `Assets/Tests/` contains EditMode and PlayMode coverage for the vertical slice.

## Playable Flow

```text
Boot -> Main Menu -> Spread Select -> Question Input -> Shuffle/Draw
     -> Flip Cards -> Result
```

The editor flow is always available through the local simulator. The online
flow uses the FastAPI backend through the runtime configuration in
`Assets/StreamingAssets/tarot_desktop_config.json`.

## Runtime Modes

- Offline mode: runs without a backend and uses local reading data.
- Online mode: sends the reading request to the configured backend. API keys
  must remain on the backend and must never be placed in this Unity project.

The default address is intended for local development only. A public release
must point to a deployed HTTPS backend rather than a developer machine's
`localhost` address.

At startup, the client checks `/api/v1/health/` and then requests
`/api/v1/guest-session`. If the backend is unavailable, the game keeps the
offline local-simulation path available. The current local FastAPI smoke flow
has been verified for 1-card, 3-card, and 10-card readings with mock AI; a
deployed backend and an activated Unity Editor license are still required for
final client acceptance.

## Documentation

- [`Docs/THIRD_PARTY_ASSETS.md`](Docs/THIRD_PARTY_ASSETS.md) — asset provenance
  and license record.

## Local Development

1. Open this folder with Unity `6000.3.16f1`.
2. Open `Assets/Scenes/Boot.unity`.
3. Press Play and complete the local flow.
4. Start the FastAPI backend separately when validating online mode.
5. Confirm that Unity Hub has an active Editor license before running EditMode
   or PlayMode tests in batch mode.

The Unity package manifest already contains URP, Input System, UGUI, and the
Unity Test Framework. Do not commit `Library`, `Temp`, `Logs`, `TestResults`,
IDE project files, Python caches, or local secrets.

## Release

The intended player experience is: download a platform archive, extract it,
and launch the game without installing Python, Conda, Docker, or Unity. Release
archives belong in GitHub Releases; build outputs do not belong in source
history.

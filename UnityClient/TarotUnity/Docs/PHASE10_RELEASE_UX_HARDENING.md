# Phase 10 Release UX Hardening

Date: 2026-05-27

## Direction

Phase 10 makes the downloadable prototype easier to run and diagnose.

It does not attempt the final Hearthstone-like visual redesign. It hardens the first public desktop experience so a player can unzip, start the game, understand local/offline mode, and edit backend config without reading source code.

## Scope

Included:

- release package readme
- backend config example
- player-readable in-game backend/offline status copy
- Phase 10 editor package helper
- automated tests for release UX assets and copy

Excluded:

- final card art
- final 3D table redesign
- settings/history/profile/admin pages
- new backend API scope

## Acceptance Criteria

- Windows release folder contains `README_FIRST.txt`.
- Windows release folder contains `tarot_desktop_config.example.json`.
- Release readme explains `TarotUnity.exe`, backend URL config, and local/offline fallback.
- ReadingRoom contains Phase 10 release status copy.
- Backend-only failure copy is readable and points to `tarot_desktop_config.json`.
- Full EditMode and PlayMode checks still pass.

## Implementation

- Added release package copy builders for `README_FIRST.txt` and `tarot_desktop_config.example.json`.
- Added player-readable release/offline status copy for local mode, backend fallback, and backend-only failures.
- Wired `ReadingRoomController` to display release status text without blocking the playable local flow.
- Added `Phase10ReleasePackageBuilder` to refresh release support files and upgrade scene wiring from the editor.
- Added EditMode coverage for release copy, release package helper availability, and scene wiring.

## Verification Evidence

Latest check: 2026-05-27.

- Phase 10 red check: `4 failed / 0 passed` before implementation, proving the new tests covered missing behavior.
- Phase 10 green check: `4 passed / 0 failed / 0 skipped`.
- Full Unity EditMode: `42 passed / 0 failed / 0 skipped`.
- Full Unity PlayMode: `2 passed / 0 failed / 0 skipped`.
- Backend pytest: `161 passed / 0 failed`, with one existing passlib crypt deprecation warning.
- Windows desktop build: passed, generated `Builds/Desktop/Windows/TarotUnity.exe`.
- macOS desktop build: passed, generated `Builds/Desktop/macOS/Tarot Unity.app`.
- Windows release zip integrity: passed for `Builds/Desktop/Release/TarotUnity-Windows-x64.zip`.
- Scene/prefab missing-reference scan: no missing script or zero GUID matches in `Assets/Scenes` or `Assets/Prefabs`.

Known non-blocking external noise:

- Unity Licensing handshake/access-token messages can still appear in batchmode logs.
- UnityConnect/public CDN request timeouts can appear when Unity online services are unavailable.
- URP ray tracing shader warnings can appear on unsupported platforms and do not block the desktop build.

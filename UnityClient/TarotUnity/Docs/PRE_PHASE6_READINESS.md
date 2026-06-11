# Pre-Phase-6 Readiness

Date: 2026-05-24

Scope: finish all checks before Phase 6 Desktop Build.

Update: on 2026-05-24, the user explicitly approved bypassing the known external Unity Licensing token-update log noise below so Phase 6 could begin. All other errors remain blocking unless classified and fixed.

## Phase Status

- Phase 1 Project Bootstrap: passed by project structure, scene list, prefab/script scaffold, and API scope.
- Phase 2 Graybox Vertical Slice: passed by scene wiring and playable vertical slice tests.
- Phase 3 Presentation Pass: passed by presentation-related EditMode tests.
- Phase 4 Backend Integration: passed by API model/client EditMode tests and backend PlayMode flow test.
- Phase 5 Art And UI Polish: passed by polish tests and existing scene/prefab validation.

## Verified Flow

The current PlayMode vertical slice test covers:

```text
Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result
```

## Current Verification Commands

Run Unity EditMode tests:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -runTests \
  -testPlatform editmode \
  -testResults /tmp/tarot-unity-prephase6/EditModeResults.xml \
  -logFile /tmp/tarot-unity-prephase6/EditMode.log
```

Run Unity PlayMode tests:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -runTests \
  -testPlatform playmode \
  -testResults /tmp/tarot-unity-prephase6/PlayModeResults.xml \
  -logFile /tmp/tarot-unity-prephase6/PlayMode.log
```

Run backend tests:

```bash
cd /Users/maochuandou/BUPT/Game/Tarot
/tmp/tarot-prephase6-py311-venv/bin/python -m pytest -q
```

Scan Unity logs for blocking errors:

```bash
rg -n -i "(^|[^a-z])(error|exception|curl error)([^a-z]|$)" \
  /tmp/tarot-unity-prephase6/EditMode.log \
  /tmp/tarot-unity-prephase6/PlayMode.log \
  /Users/maochuandou/Library/Logs/Unity/Editor.log
```

Scan scene and prefab references:

```bash
rg -n "m_Script: \{fileID: 0\}|guid: 00000000000000000000000000000000|Missing" \
  /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Assets/Scenes \
  /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Assets/Prefabs
```

## Known External Blocker

Unity Hub shows a valid Unity Personal license, and the local entitlement license exists at:

```text
/Users/maochuandou/Library/Unity/licenses/UnityEntitlementLicense.xml
```

However, Unity still writes this non-project licensing error to Editor logs:

```text
[Licensing::Module] Error: Access token is unavailable; failed to update
```

This is outside the Unity project code. It should be cleared by refreshing Unity Hub account/auth state, for example by re-authenticating in Unity Hub. For Phase 6 only, the user approved treating this exact line as non-blocking external noise.

## Latest Check

2026-05-24 16:17 local:

- Unity Hub account re-authenticated.
- Unity Personal Version entitlement restored.
- `/Users/maochuandou/Library/Unity/licenses/UnityEntitlementLicense.xml` exists again.
- Unity EditMode tests passed: 18 passed, 0 failed, 0 skipped.
- Unity PlayMode tests passed: 2 passed, 0 failed, 0 skipped.
- Backend pytest passed: 161 passed, 0 failed.
- Main vertical slice test passed.
- The only remaining strict gate failure is still:

```text
[Licensing::Module] Error: Access token is unavailable; failed to update
```

This line appears in fresh Unity EditMode, PlayMode, and public `Editor.log` logs even after Unity Hub re-authentication and license refresh.

## Phase 6 Entry Gate

Before continuing beyond Phase 6, the final preflight report should have:

- Unity EditMode: 0 failed tests.
- Unity PlayMode: 0 failed tests.
- Backend pytest: 0 failed tests.
- Main vertical slice verified by PlayMode.
- No project compile errors.
- No missing scene/prefab script references.
- No unclassified `error`, `exception`, or `curl error` lines in the checked Unity logs.

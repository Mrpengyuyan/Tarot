# Phase 6 Desktop Build

Date: 2026-05-25

Scope: desktop prototype build support for the current Unity vertical slice:

```text
Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result
```

Phase 6 does not add history, settings, admin, profile, or final release packaging.

## Build Outputs

- macOS prototype: `Builds/Desktop/macOS/Tarot Unity.app`
- Windows prototype path: `Builds/Desktop/Windows/TarotUnity.exe`

Both macOS and Windows desktop prototype builds have been generated on this machine.

## Runtime Backend Config

The desktop build loads runtime backend settings from:

```text
Assets/StreamingAssets/tarot_desktop_config.json
```

Default content:

```json
{
  "backendBaseUrl": "http://localhost:8000/api/v1",
  "requestTimeoutSeconds": 15
}
```

The `TAROT_BACKEND_URL` environment variable overrides `backendBaseUrl`. If the value omits `/api/v1`, Unity normalizes it by appending `/api/v1`.

## Editor Entry Points

Run setup only:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -enableUnityConnectPrefs false \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -executeMethod TarotUnity.Editor.Phase6DesktopBuildBuilder.RunSetup \
  -quit \
  -logFile /tmp/tarot-unity-phase6/Phase6Setup.log
```

Build macOS:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -enableUnityConnectPrefs false \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -executeMethod TarotUnity.Editor.Phase6DesktopBuildBuilder.BuildMacOS \
  -quit \
  -logFile /tmp/tarot-unity-phase6/BuildMacOS.log
```

Build Windows after installing Windows Standalone Build Support:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -enableUnityConnectPrefs false \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -executeMethod TarotUnity.Editor.Phase6DesktopBuildBuilder.BuildWindows \
  -quit \
  -logFile /tmp/tarot-unity-phase6/BuildWindows.log
```

## Local Prototype Settings

`Phase6DesktopBuildBuilder.RunSetup` enforces:

- Build scenes: `Boot`, `MainMenu`, `ReadingRoom`, `Result`
- Product name: `Tarot Unity`
- Bundle version: `0.6.0`
- Bundle identifier: `com.maochuandou.tarotunity`
- Standalone scripting backend: `Mono`
- Default window size: `1280 x 720`
- Fullscreen mode: windowed
- Local prototype Cloud Diagnostics and Engine Diagnostics: disabled
- Boot scene: `GameBootstrap`, `ApiClient`, and `DesktopConfigLoader` on `Bootstrapper`

Unity still writes this external licensing line in batchmode logs on this machine:

```text
[Licensing::Module] Error: Access token is unavailable; failed to update
```

The user explicitly approved bypassing this known external licensing noise for Phase 6.

Use `-enableUnityConnectPrefs false` for command-line checks and builds. Without it, Unity may try to fetch UnityConnect service config from `public-cdn.cloud.unity3d.com` after tests have already completed, which creates external network timeout noise unrelated to this project.

## Verification Snapshot

Latest Phase 6 checks:

- EditMode: 25 passed, 0 failed, 0 skipped
- PlayMode: 2 passed, 0 failed, 0 skipped
- Backend pytest: 161 passed, 0 failed, 0 skipped
- macOS build: success
- macOS app binary: universal `x86_64` and `arm64`
- Windows build: success
- Windows app binary: `PE32+ executable (GUI) x86-64`
- Vertical slice: covered by PlayMode flow test through `Result.unity`

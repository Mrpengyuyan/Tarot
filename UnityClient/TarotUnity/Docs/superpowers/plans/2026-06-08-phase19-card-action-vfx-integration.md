# Phase 19 Card Action VFX Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect existing shuffle, deal, flip, and result presentation cues to the Phase18 true particle-system layer without changing gameplay flow, backend behavior, release packaging, or broad UI layout.

**Architecture:** Add a null-safe `RitualActionVfxController` that maps `PresentationCueId` values to Phase18 particle intensity/play/burst calls. Extend `RitualFeedbackController` with optional cue forwarding so existing UI/gameplay code remains unchanged. Use an idempotent editor bootstrapper to wire ReadingRoom and Result feedback objects to their scene-local Phase18 particle controllers.

**Tech Stack:** Unity 6.3 LTS, C#, URP, UnityEditor, built-in `ParticleSystem`, NUnit EditMode tests, Unity Test Framework PlayMode tests.

---

## File Structure

- Create: `Assets/Tests/EditMode/Phase19CardActionVfxIntegrationTests.cs`
  - Verifies runtime API, scene wiring, previous phase retention, and documentation.
- Create: `Assets/Scripts/Presentation/RitualActionVfxController.cs`
  - Owns optional action cue to Phase18 particle mapping.
- Modify: `Assets/Scripts/Presentation/RitualFeedbackController.cs`
  - Adds optional cue forwarding into `RitualActionVfxController`.
- Create: `Assets/Tests/PlayMode/Phase19CardActionVfxPlayModeTests.cs`
  - Verifies null safety, cue mapping, reveal bursts, and feedback forwarding.
- Create: `Assets/Editor/Phase19CardActionVfxBootstrapper.cs`
  - Wires ReadingRoom and Result feedback objects to scene-local Phase18 particle controllers.
- Modify through bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify through bootstrapper: `Assets/Scenes/Result.unity`
- Create: `Docs/PHASE19_CARD_ACTION_VFX_INTEGRATION.md`
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify: `README.md`
- Modify after final checks: `Docs/superpowers/plans/2026-06-08-phase19-card-action-vfx-integration.md`

No backend files, release build files, tarot artwork assets, history/settings/profile/admin/dashboard code, or desktop release zip files are in scope.

---

## Task 1: Write Phase19 Failing EditMode Tests

**Files:**
- Create: `Assets/Tests/EditMode/Phase19CardActionVfxIntegrationTests.cs`

- [x] **Step 1: Add the EditMode failing test file**

Create `Assets/Tests/EditMode/Phase19CardActionVfxIntegrationTests.cs` with tests for:

```text
RitualActionVfxControllerRuntimeTypeExists
RitualFeedbackControllerExposesActionVfxForwardingFields
ReadingRoomHasPhase19ActionVfxWiring
ResultHasPhase19ActionVfxWiring
Phase19KeepsPhase18ParticlesAndPriorAuraWiring
Phase19KeepsCardArtAndPriorVisualWiring
Phase19KeepsPhase13ArtworkCatalogAvailable
Phase19DocumentationExists
```

Concrete assertions:

```text
Type.GetType("TarotUnity.Presentation.RitualActionVfxController, TarotUnity.Runtime") != null
public methods: PlayCue(PresentationCueId), PlayCue(PresentationCueId, Transform)
public properties: LastCue, LastAnchor, CueCount
RitualFeedbackController serialized fields: actionVfxController, forwardCuesToActionVfx
ReadingRoomRitualFeedback has actionVfxController and forwardCuesToActionVfx = true
ReadingRoom action VFX references Phase16_RitualAuraRoot RitualParticleSystemController
ResultRitualFeedback has actionVfxController and forwardCuesToActionVfx = true
Result action VFX references Phase16_ResultAuraRoot RitualParticleSystemController
Phase18 particle objects remain in ReadingRoom and Result
Catalog entries remain 78
Doc path: Docs/PHASE19_CARD_ACTION_VFX_INTEGRATION.md
```

- [x] **Step 2: Run targeted EditMode tests and verify RED**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase19CardActionVfxIntegrationTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase19-red-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-red-editmode.log
```

Expected:

```text
result=Failed
failed > 0
```

The failures should mention missing `RitualActionVfxController`, missing feedback serialized fields, missing scene wiring, and missing Phase19 documentation. If there is a C# compile error, fix the test file before proceeding.

---

## Task 2: Implement Runtime Controller And Feedback Forwarding

**Files:**
- Create: `Assets/Scripts/Presentation/RitualActionVfxController.cs`
- Modify: `Assets/Scripts/Presentation/RitualFeedbackController.cs`
- Create: `Assets/Tests/PlayMode/Phase19CardActionVfxPlayModeTests.cs`

- [x] **Step 1: Add `RitualActionVfxController`**

Create a sealed `MonoBehaviour` in namespace `TarotUnity.Presentation` with these serialized fields:

```csharp
[SerializeField] private RitualParticleSystemController particleSystemController;
[SerializeField] private bool playAmbientOnShuffle = true;
[SerializeField] private bool playAmbientOnDeal = true;
[SerializeField] private bool burstOnFlip = true;
[SerializeField] private bool burstOnResult = true;
[SerializeField] private float shuffleIntensity = 0.76f;
[SerializeField] private float dealIntensity = 0.66f;
[SerializeField] private float flipIntensity = 0.95f;
[SerializeField] private float resultIntensity = 0.72f;
```

Expose:

```csharp
public PresentationCueId LastCue { get; private set; }
public Transform LastAnchor { get; private set; }
public int CueCount { get; private set; }
public void PlayCue(PresentationCueId cue)
public void PlayCue(PresentationCueId cue, Transform anchor)
```

Map cues:

```text
ShuffleStarted -> SetIntensity(shuffleIntensity), PlayAmbient if playAmbientOnShuffle
CardDealt -> SetIntensity(dealIntensity), PlayAmbient if playAmbientOnDeal
CardFlipped -> SetIntensity(flipIntensity), TriggerRevealBurst if burstOnFlip
ResultReady/ResultReveal -> SetIntensity(resultIntensity), PlayAmbient, TriggerRevealBurst if burstOnResult
Other cues -> record state only
```

- [x] **Step 2: Extend `RitualFeedbackController`**

Add serialized fields:

```csharp
[SerializeField] private RitualActionVfxController actionVfxController;
[SerializeField] private bool forwardCuesToActionVfx = true;
```

At the end of `PlayCue(PresentationCueId cue, Transform anchor)`, call:

```csharp
if (forwardCuesToActionVfx)
{
    actionVfxController?.PlayCue(cue, anchor);
}
```

Existing `LastCue`, `CueCount`, audio cue, and legacy particles must continue to work.

- [x] **Step 3: Add PlayMode tests**

Create `Assets/Tests/PlayMode/Phase19CardActionVfxPlayModeTests.cs` with tests:

```text
RitualActionVfxControllerIsSafeWithMissingParticleController
RitualActionVfxControllerMapsShuffleAndDealToAmbientPlayback
RitualActionVfxControllerMapsFlipToRevealBurst
RitualActionVfxControllerMapsResultRevealToSafeResultVfx
RitualFeedbackControllerForwardsCueToActionVfxWhenEnabled
RitualFeedbackControllerDoesNotForwardCueWhenDisabled
```

Use manually created `GameObject` instances with `ParticleSystem` components and reflection to assign private fields.

- [x] **Step 4: Run targeted PlayMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testFilter TarotUnity.Tests.PlayMode.Phase19CardActionVfxPlayModeTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase19-controller-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-controller-playmode.log
```

Expected:

```text
result=Passed
failed=0
```

- [x] **Step 5: Run targeted EditMode tests after runtime code**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase19CardActionVfxIntegrationTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase19-controller-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-controller-editmode.log
```

Expected:

```text
result=Failed
failed > 0
```

Remaining failures should be scene wiring and documentation only.

---

## Task 3: Implement Scene Bootstrapper And Wiring

**Files:**
- Create: `Assets/Editor/Phase19CardActionVfxBootstrapper.cs`
- Modify through bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify through bootstrapper: `Assets/Scenes/Result.unity`

- [x] **Step 1: Add the editor bootstrapper**

Create `Phase19CardActionVfxBootstrapper` in namespace `TarotUnity.Editor` with menu:

```text
Tools/Tarot Unity/Run Phase 19 Card Action VFX Bootstrap
```

The bootstrapper must:

```text
Exit Play Mode if needed.
Open Assets/Scenes/ReadingRoom.unity and Assets/Scenes/Result.unity.
Find ReadingRoomRitualFeedback and ResultRitualFeedback.
Find Phase16_RitualAuraRoot / Phase16_ResultAuraRoot.
Require each root to have RitualParticleSystemController.
Add or reuse RitualActionVfxController on the feedback GameObject.
Assign feedback.actionVfxController and feedback.forwardCuesToActionVfx = true.
Assign actionVfxController.particleSystemController and cue settings.
Save scenes and refresh AssetDatabase.
```

- [x] **Step 2: Run the bootstrapper in batchmode**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase19CardActionVfxBootstrapper.Run -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-bootstrap.log -quit
```

Expected:

```text
Exit code 0
Tarot Unity Phase 19 card action VFX bootstrap complete.
```

- [x] **Step 3: Run targeted EditMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase19CardActionVfxIntegrationTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase19-wiring-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-wiring-editmode.log
```

Expected:

```text
result=Failed
failed=1
```

The only remaining failure should be `Phase19DocumentationExists`.

---

## Task 4: Add Documentation And Mainline Updates

**Files:**
- Create: `Docs/PHASE19_CARD_ACTION_VFX_INTEGRATION.md`
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify: `README.md`

- [x] **Step 1: Add Phase19 doc**

Create `Docs/PHASE19_CARD_ACTION_VFX_INTEGRATION.md` describing:

```text
Phase19 scope
RitualActionVfxController runtime behavior
RitualFeedbackController forwarding behavior
ReadingRoom wiring
Result wiring
Out of scope
Verification commands
Known limitation that this is action VFX integration, not final high-end VFX
```

- [x] **Step 2: Update mainline and README**

Update:

```text
Docs/UI_COMPLETION_MAINLINE.md
README.md
```

Add Phase19 as the action-triggered VFX integration pass after Phase18.

- [x] **Step 3: Run targeted EditMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase19CardActionVfxIntegrationTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase19-green-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-green-editmode.log
```

Expected:

```text
result=Passed
failed=0
```

---

## Task 5: Final End-Check Flow

**Files:**
- Modify after checks: `Docs/superpowers/plans/2026-06-08-phase19-card-action-vfx-integration.md`
- Modify after checks: `Docs/UI_COMPLETION_MAINLINE.md`

- [x] **Step 1: Confirm Unity is not in Play Mode or close batchmode leftovers**

Run:

```bash
pgrep -fl "Unity.*TarotUnity" || true
```

If a normal Unity Editor window is in Play Mode, exit Play Mode before generation, tests, or saves. Batchmode test processes should not remain running.

- [x] **Step 2: Run Unity compile/import check**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-final-unity-check.log -quit
```

Expected:

```text
Exit code 0
No project-level compile errors, exceptions, missing scripts, missing references, or LocalKeyword errors.
```

- [x] **Step 3: Run full EditMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase19-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-final-editmode.log
```

Expected:

```text
result=Passed
failed=0
```

- [x] **Step 4: Run full PlayMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase19-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-final-playmode.log
```

Expected:

```text
result=Passed
failed=0
```

- [x] **Step 5: Scan logs and assets**

Run:

```bash
rg -n "error CS|CompilerError|Compilation failed|Exception:|NullReferenceException|MissingReferenceException|Missing Script|m_Script: \\{fileID: 0\\}|Fatal|Aborting batchmode|Local keyword" Logs/phase19-final-unity-check.log Logs/phase19-final-editmode.log Logs/phase19-final-playmode.log Assets/Scenes Assets/Prefabs || true
```

Expected:

```text
No project-level errors.
Known Unity licensing/CDN/batchmode shutdown noise may be documented separately.
```

- [x] **Step 6: Verify vertical slice test evidence**

Confirm `VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns` passed in `TestResults/phase19-final-playmode.xml`.

- [x] **Step 7: Record actual results**

Append a short `Task 5 Actual Results` section to this plan with:

```text
Unity error/log scan result
EditMode passed/failed/skipped counts
PlayMode passed/failed/skipped counts
Vertical slice verification result
Scene/prefab reference scan result
Known external noise
Remaining limitations
```

### Task 5 Actual Results

- Unity process / Play Mode check: `pgrep -fl "Unity.*TarotUnity"` returned no project Unity process before final checks, so no Play Mode exit was required.
- Unity compile/import check: `Logs/phase19-final-unity-check.log`, exit code `0`.
- Project error scan: no matches for compiler errors, exceptions, missing scripts, missing references, fatal errors, aborting batchmode, or LocalKeyword errors across final logs, `Assets/Scenes`, and `Assets/Prefabs`.
- Editor.log error scan: no matches for compiler errors, exceptions, missing scripts, missing references, fatal errors, aborting batchmode, or LocalKeyword errors.
- EditMode tests: `TestResults/phase19-final-editmode.xml` reported `112` passed, `0` failed, `0` skipped.
- PlayMode tests: `TestResults/phase19-final-playmode.xml` reported `20` passed, `0` failed, `0` skipped.
- Vertical slice verification: `VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns` passed in `phase19-final-playmode.xml`, covering `Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result`.
- Scene/prefab reference scan: no `m_Script: {fileID: 0}` or missing-reference signatures found; ReadingRoom and Result scene YAML contain Phase19 `actionVfxController` and `forwardCuesToActionVfx` wiring.
- Known external noise: Unity Licensing access-token update noise, UnityConnect/CDN timeout noise, Curl error 42 during batchmode shutdown, prematurely finalized thread messages, and debugger-agent port-listen noise. These did not fail Unity or tests and are not project-level errors.
- Remaining limitation: Phase19 is action-triggered VFX integration, not final high-end VFX, Shader Graph rune animation, camera choreography, post-processing, or broad UI redesign.

---

## Plan Self-Review

- Spec coverage: every Phase19 design requirement maps to Task 1, 2, 3, 4, or 5.
- Placeholder scan: no `TBD`, `TODO`, or deferred implementation placeholders are intended in this plan.
- Type consistency: controller, method, field, scene, and child names match the Phase19 design.
- Scope check: backend, history, settings, release packaging, Shader Graph, post-processing, camera choreography, and broad UI redesign remain out of scope.

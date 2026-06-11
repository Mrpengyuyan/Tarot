# Phase 18 Ritual Particle Systems Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the first true Unity `ParticleSystem` layer to ReadingRoom and Result ritual aura scenes without changing gameplay, backend behavior, release packaging, or broad UI layout.

**Architecture:** Add a null-safe `RitualParticleSystemController` that owns optional particle arrays and exposes deterministic play, stop, intensity, reveal-burst, and simulate methods. Use an idempotent Unity Editor bootstrapper to create and wire built-in `ParticleSystem` objects under existing Phase16 particle/focus anchors so Phase17 motion naturally carries them. Keep Phase18 additive so Phase13 artwork, Phase14 dimensional reveal, Phase15 table foundation, Phase16 aura anchors, and Phase17 motion remain intact.

**Tech Stack:** Unity 6.3 LTS, C#, URP, UnityEditor, built-in `ParticleSystem`, NUnit EditMode tests, Unity Test Framework PlayMode tests.

---

## File Structure

- Create: `Assets/Tests/EditMode/Phase18RitualParticleSystemTests.cs`
  - Verifies runtime API, scene particle wiring, particle settings, previous phase retention, and docs.
- Create: `Assets/Scripts/Presentation/RitualParticleSystemController.cs`
  - Owns optional particle references and null-safe runtime controls.
- Create: `Assets/Tests/PlayMode/Phase18RitualParticleSystemPlayModeTests.cs`
  - Verifies null safety, intensity clamps, play/stop behavior, reveal bursts, and manual simulation.
- Create: `Assets/Editor/Phase18RitualParticleSystemBootstrapper.cs`
  - Creates and wires Phase18 particle systems under existing Phase16 particle/focus anchors.
- Modify through bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify through bootstrapper: `Assets/Scenes/Result.unity`
- Create: `Docs/PHASE18_RITUAL_PARTICLE_SYSTEMS.md`
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify: `README.md`
- Modify after final checks: `Docs/superpowers/plans/2026-06-05-phase18-ritual-particle-systems.md`

No backend files, release build files, tarot artwork assets, history/settings/profile/admin/dashboard code, or desktop release zip files are in scope.

---

## Task 1: Write Phase18 Failing EditMode Tests

**Files:**
- Create: `Assets/Tests/EditMode/Phase18RitualParticleSystemTests.cs`

- [x] **Step 1: Add the EditMode failing test file**

Create `Assets/Tests/EditMode/Phase18RitualParticleSystemTests.cs` with tests for:

```text
RitualParticleSystemControllerRuntimeTypeExists
ReadingRoomHasPhase18ParticleWiring
ResultHasPhase18ParticleWiring
Phase18ParticleSystemsUseSafeVisualSettings
Phase18KeepsPhase16AndPhase17AuraWiring
Phase18KeepsCardArtAndPriorVisualWiring
Phase18KeepsPhase13ArtworkCatalogAvailable
Phase18DocumentationExists
```

The tests must assert these concrete requirements:

```csharp
Type.GetType("TarotUnity.Presentation.RitualParticleSystemController, TarotUnity.Runtime") != null
public methods: SetParticlesVisible(bool), SetIntensity(float), PlayAmbient(), StopAll(bool), TriggerRevealBurst(), SimulateTick(float)
public properties: IsPlaying, CurrentIntensity
ReadingRoom root: Phase16_RitualAuraRoot
ReadingRoom children: Phase18_AmbientDustParticles under Phase16_ParticleAnchorNorth, Phase18_DeckFocusParticles under Phase16_ParticleAnchorEast, Phase18_FlipSparkParticles under Phase16_AuraFocusAnchor
Result root: Phase16_ResultAuraRoot
Result children: Phase18_ResultCardMotes under Phase16_ResultParticleAnchorLeft, Phase18_ResultInterpretationGlow under Phase16_ResultAuraFocusAnchor
Serialized arrays: ambientParticles, focusParticles, revealParticles
Prior anchors: Phase16_RitualAuraRoot, Phase16_ResultAuraRoot
Prior controllers: RitualAuraController, RitualAuraMotionController
Catalog entries: 78
Doc path: Docs/PHASE18_RITUAL_PARTICLE_SYSTEMS.md
```

- [x] **Step 2: Run the targeted EditMode tests and verify RED**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase18RitualParticleSystemTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase18-red-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase18-red-editmode.log
```

Expected:

```text
result=Failed
failed > 0
```

The failures should mention missing `RitualParticleSystemController`, missing Phase18 scene wiring, and missing Phase18 documentation. If the command fails because of a C# compile error, fix the test file before proceeding.

---

## Task 2: Implement Runtime Controller And PlayMode Tests

**Files:**
- Create: `Assets/Scripts/Presentation/RitualParticleSystemController.cs`
- Create: `Assets/Tests/PlayMode/Phase18RitualParticleSystemPlayModeTests.cs`

- [x] **Step 1: Add `RitualParticleSystemController`**

Create a sealed `MonoBehaviour` in namespace `TarotUnity.Presentation` with these serialized fields:

```csharp
[SerializeField] private RitualAuraController auraController;
[SerializeField] private ParticleSystem[] ambientParticles;
[SerializeField] private ParticleSystem[] focusParticles;
[SerializeField] private ParticleSystem[] revealParticles;
[SerializeField] private bool playOnEnable = true;
[SerializeField] private float baseEmissionMultiplier = 1f;
[SerializeField] private float focusEmissionMultiplier = 0.85f;
[SerializeField] private float revealEmissionMultiplier = 1.35f;
[SerializeField] private float intensity = 0.65f;
```

Expose:

```csharp
public bool IsPlaying { get; private set; }
public float CurrentIntensity { get; private set; } = 0.65f;
public void SetParticlesVisible(bool visible)
public void SetIntensity(float value)
public void PlayAmbient()
public void StopAll(bool clear)
public void TriggerRevealBurst()
public void SimulateTick(float deltaSeconds)
```

Implement null-safe helpers that iterate all arrays, skip null entries, and call Unity `ParticleSystem` APIs only on valid systems.

- [x] **Step 2: Add PlayMode tests**

Create `Assets/Tests/PlayMode/Phase18RitualParticleSystemPlayModeTests.cs` with tests for:

```text
RitualParticleSystemControllerIsSafeWithMissingReferences
RitualParticleSystemControllerClampsIntensityAndAppliesEmission
RitualParticleSystemControllerPlaysAndStopsAssignedParticles
RitualParticleSystemControllerTriggersRevealBurst
RitualParticleSystemControllerSimulatesAssignedParticles
```

Use manually created `GameObject` instances and `AddComponent<ParticleSystem>()`. Use reflection to assign private arrays and scalar fields.

- [x] **Step 3: Run targeted PlayMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testFilter TarotUnity.Tests.PlayMode.Phase18RitualParticleSystemPlayModeTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase18-controller-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase18-controller-playmode.log
```

Expected:

```text
result=Passed
failed=0
```

- [x] **Step 4: Run targeted EditMode tests after runtime controller**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase18RitualParticleSystemTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase18-controller-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase18-controller-editmode.log
```

Expected:

```text
result=Failed
failed > 0
```

Remaining failures should be scene wiring and docs only.

---

## Task 3: Implement Scene Bootstrapper And Wiring

**Files:**
- Create: `Assets/Editor/Phase18RitualParticleSystemBootstrapper.cs`
- Modify through bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify through bootstrapper: `Assets/Scenes/Result.unity`

- [x] **Step 1: Add the editor bootstrapper**

Create `Phase18RitualParticleSystemBootstrapper` in namespace `TarotUnity.Editor` with menu:

```text
Tools/Tarot Unity/Run Phase 18 Ritual Particle Systems Bootstrap
```

The bootstrapper must:

```text
Exit Play Mode if needed.
Open Assets/Scenes/ReadingRoom.unity and Assets/Scenes/Result.unity.
Find Phase16_RitualAuraRoot and Phase16_ResultAuraRoot.
Add or reuse RitualParticleSystemController on each root.
Create or reuse child GameObjects with ParticleSystem components under existing Phase16 particle/focus anchors.
Configure ParticleSystem modules with safe low-emission values.
Assign serialized fields ambientParticles, focusParticles, revealParticles, playOnEnable, baseEmissionMultiplier, focusEmissionMultiplier, revealEmissionMultiplier, intensity.
Save scenes and refresh AssetDatabase.
```

ReadingRoom children:

```text
Phase18_AmbientDustParticles
Phase18_DeckFocusParticles
Phase18_FlipSparkParticles
```

ReadingRoom parents:

```text
Phase18_AmbientDustParticles -> Phase16_ParticleAnchorNorth
Phase18_DeckFocusParticles -> Phase16_ParticleAnchorEast
Phase18_FlipSparkParticles -> Phase16_AuraFocusAnchor
```

Result children:

```text
Phase18_ResultCardMotes
Phase18_ResultInterpretationGlow
```

Result parents:

```text
Phase18_ResultCardMotes -> Phase16_ResultParticleAnchorLeft
Phase18_ResultInterpretationGlow -> Phase16_ResultAuraFocusAnchor
```

- [x] **Step 2: Run the bootstrapper in batchmode**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase18RitualParticleSystemBootstrapper.Run -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase18-bootstrap.log -quit
```

Expected:

```text
Exit code 0
Tarot Unity Phase 18 ritual particle systems bootstrap complete.
```

- [x] **Step 3: Run targeted EditMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase18RitualParticleSystemTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase18-wiring-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase18-wiring-editmode.log
```

Expected:

```text
result=Failed
failed=1
```

The only remaining failure should be `Phase18DocumentationExists`.

---

## Task 4: Add Documentation And Mainline Updates

**Files:**
- Create: `Docs/PHASE18_RITUAL_PARTICLE_SYSTEMS.md`
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify: `README.md`

- [x] **Step 1: Add Phase18 doc**

Create `Docs/PHASE18_RITUAL_PARTICLE_SYSTEMS.md` describing:

```text
Phase18 scope
Runtime controller
ReadingRoom particle children
Result particle children
Out of scope
Verification commands
Known limitation that this is not final high-end VFX
```

- [x] **Step 2: Update mainline and README**

Update:

```text
Docs/UI_COMPLETION_MAINLINE.md
README.md
```

Add Phase18 as the first true particle-system layer after Phase17.

- [x] **Step 3: Run targeted EditMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase18RitualParticleSystemTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase18-green-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase18-green-editmode.log
```

Expected:

```text
result=Passed
failed=0
```

---

## Task 5: Final End-Check Flow

**Files:**
- Modify after checks: `Docs/superpowers/plans/2026-06-05-phase18-ritual-particle-systems.md`
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
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase18-final-unity-check.log -quit
```

Expected:

```text
Exit code 0
No project-level compile errors, exceptions, missing scripts, missing references, or LocalKeyword errors.
```

- [x] **Step 3: Run full EditMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase18-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase18-final-editmode.log
```

Expected:

```text
result=Passed
failed=0
```

- [x] **Step 4: Run full PlayMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase18-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase18-final-playmode.log
```

Expected:

```text
result=Passed
failed=0
```

- [x] **Step 5: Scan logs and assets**

Run:

```bash
rg -n "error CS|CompilerError|Compilation failed|Exception:|NullReferenceException|MissingReferenceException|Missing Script|m_Script: \\{fileID: 0\\}|Fatal|Aborting batchmode|Local keyword" Logs/phase18-final-unity-check.log Logs/phase18-final-editmode.log Logs/phase18-final-playmode.log Assets/Scenes Assets/Prefabs || true
```

Expected:

```text
No project-level errors.
Known Unity licensing/CDN/batchmode shutdown noise may be documented separately.
```

- [x] **Step 6: Verify vertical slice test evidence**

Confirm `VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns` passed in `TestResults/phase18-final-playmode.xml`.

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

---

## Plan Self-Review

- Spec coverage: every Phase18 design requirement maps to Task 1, 2, 3, 4, or 5.
- Placeholder scan: no `TBD`, `TODO`, or deferred implementation placeholders are intended in this plan.
- Type consistency: controller, method, field, scene, and child names match the Phase18 design.
- Scope check: backend, history, settings, release packaging, Shader Graph, post-processing, and camera choreography remain out of scope.

## Task 5 Actual Results

Date: 2026-06-05

- Unity process / Play Mode check: `pgrep -fl "Unity.*TarotUnity"` returned no running TarotUnity Unity process before final checks.
- Unity compile/import check: `Logs/phase18-final-unity-check.log`, exit code 0.
- Full EditMode tests: `TestResults/phase18-final-editmode.xml`, Passed 104, Failed 0, Skipped 0.
- Full PlayMode tests: `TestResults/phase18-final-playmode.xml`, Passed 14, Failed 0, Skipped 0.
- Vertical slice: `TarotUnity.Tests.PlayMode.VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns` passed in `phase18-final-playmode.xml`.
- Project error scan: no project-level `error CS`, `CompilerError`, `Compilation failed`, `Exception:`, `NullReferenceException`, missing script/reference, `Fatal`, `Aborting batchmode`, or `Local keyword` matches in final logs, `Assets/Scenes`, or `Assets/Prefabs`.
- Scene/prefab reference scan: no missing script or missing reference matches in `Assets/Scenes` or `Assets/Prefabs`.
- Scope review: no files under `/Users/maochuandou/BUPT/Game/Tarot` were modified during Phase18 checks; no backend, history, settings, admin, profile, dashboard, release zip, or package output work was added.
- Review feedback resolved: `Phase18RitualParticleSystemBootstrapper` now requires existing Phase16 particle/focus anchors instead of silently falling back to the aura root.
- Known external Unity noise: licensing access-token update messages, UnityConnect config fallback, Curl error 42 during batchmode shutdown, prematurely finalized thread messages, and debugger-agent listener messages appeared in logs; these are outside project code and did not fail tests or compile/import checks.
- Remaining limitation: Phase18 is the first true ParticleSystem layer, not final high-end VFX, card trails, Shader Graph rune animation, camera choreography, post-processing, or screenshot-driven final polish.

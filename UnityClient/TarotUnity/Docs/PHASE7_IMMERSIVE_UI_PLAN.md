# Phase 7 Immersive UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade the current Unity vertical slice UI from a demo-like prototype into an immersive tarot desktop prototype.

**Architecture:** Add a Phase 7 editor bootstrapper that upgrades existing scenes and prefabs in place. Preserve current runtime controllers and backend integration so the existing PlayMode vertical slice remains the main safety net.

**Tech Stack:** Unity 6.3 LTS, URP, UGUI, Unity Test Framework, existing TarotUnity runtime scripts.

---

## Files

- Create: `Assets/Editor/Phase7ImmersiveUiBootstrapper.cs`
- Create: `Assets/Tests/EditMode/Phase7ImmersiveUiTests.cs`
- Modify by bootstrapper: `Assets/Scenes/MainMenu.unity`
- Modify by bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify by bootstrapper: `Assets/Scenes/Result.unity`
- Modify by bootstrapper: `Assets/Prefabs/Cards/PF_TarotCard.prefab`
- Modify docs: `Docs/PHASE7_IMMERSIVE_UI_DESIGN.md`
- Modify docs: `README.md`

## Task 1: Lock Phase 7 With Failing Tests

- [ ] Add EditMode tests asserting MainMenu, ReadingRoom, Result contain Phase 7 immersive anchors and no visible graybox copy.
- [ ] Run the Phase 7 test fixture only and verify it fails because the bootstrapper has not upgraded scenes yet.

## Task 2: Add Phase 7 Bootstrapper

- [ ] Create `Phase7ImmersiveUiBootstrapper` with menu item `Tools/Tarot Unity/Run Phase 7 Immersive UI Bootstrap`.
- [ ] Add idempotent helpers for folders, materials, panels, text, buttons, and scene roots.
- [ ] Upgrade MainMenu with immersive title, subtitle, backdrop, card halo, CTA styling, and Chinese status text.
- [ ] Upgrade ReadingRoom with Chinese status copy, ritual HUD root, progress markers, compact control grouping, and clearer draw/reveal hierarchy.
- [ ] Upgrade Result with oracle frame, section headers, Chinese labels, and improved reading text layout.
- [ ] Upgrade card prefab with stronger frame/sigil detail and readable presentation labels.

## Task 3: Generate Assets And Verify Tests

- [ ] Run the Phase 7 bootstrapper through Unity batchmode.
- [ ] Run the Phase 7 EditMode test fixture and verify it passes.
- [ ] Run the full EditMode suite.
- [ ] Run the full PlayMode suite.

## Task 4: Rebuild Desktop Prototype

- [ ] Run Windows desktop build.
- [ ] Run macOS desktop build if Phase 7 scene changes affect build output.
- [ ] Verify build logs contain no project compile errors.
- [ ] Verify `Builds/Desktop/Windows/TarotUnity.exe` exists.

## Task 5: End Check

- [ ] Scan Unity logs for unclassified error / exception / failed lines.
- [ ] Scan scenes and prefabs for missing script references.
- [ ] Confirm vertical slice test still covers `MainMenu -> ReadingRoom -> Result`.
- [ ] Report Passed / Failed / Skipped counts.


# Phase 43 — TextMeshPro SDF Text + the Real Sharpness Fix

Date: 2026-07-16

Driven by playthrough feedback: "字体还是有点低级…让字体渲染换一个方法".

## The sharpness fix Phase 42 claimed but never shipped

Phase 42 set `fullScreenMode = FullScreenWindow` / 1920x1080 through
`PlayerSettings` and committed the resulting `ProjectSettings.asset`. The
setting was then **silently reverted on every build**:
`Phase6DesktopBuildBuilder.BuildMacOS` calls `RunSetup()` first, and
`ApplyDesktopPlayerSettings()` hardcoded `1280x720` + `FullScreenMode.Windowed`
from Phase 6. So the build handed to the user still stretched 720p across a
1440p display — the user was right that the built text was still blurry, and the
"fix" never reached them.

The values now live in `Phase6DesktopBuildBuilder` (the code that actually runs
before every build), and `Phase42MenuElevationTests.PlayerPresentsAtNativeResolution`
catches the revert — it is what surfaced this.

Separately: the editor Game view was at `Scale 2.8x`, which renders at
viewport÷2.8 (~400x200) and magnifies. That accounts for the pixelation in the
editor screenshot and is not a game defect.

## Why the text rendering method changed

Legacy `UI.Text` rasterises glyphs into a bitmap atlas at one pixel size, so it
softens whenever it is scaled, and its `Outline` component fakes a stroke by
drawing four offset copies of the mesh — which is exactly the muddy, cheap look
that was being complained about. TextMeshPro stores glyphs as signed distance
fields: sharp at any resolution, with outline, glow and gradient as real shader
features.

**Phase 24's reason for rejecting TMP is out of date.** It argued that the
Result screen renders arbitrary runtime Chinese from the backend and a static
glyph atlas cannot hold it. TMP dynamic atlas population rasterises glyphs on
demand at runtime, so arbitrary CJK is fine. Both font assets are created
explicitly as `AtlasPopulationMode.Dynamic` with multi-atlas support.

## What shipped

- **Font assets**: `LXGWWenKai-Regular SDF` / `LXGWWenKai-Medium SDF`, dynamic,
  90pt sampling, 9px padding (≈10% of an em of outline headroom), SDFAA.
- **Fallback chain**: LiberationSans SDF is attached as a fallback on both.
  LXGW WenKai has full CJK coverage but no dingbats, so the title's "✦ ✧ ✦"
  row rendered as tofu the moment legacy Text's OS font fallback went away.
  This matters beyond decoration — the Result screen's backend Chinese would
  tofu on any glyph outside the face with no fallback in the chain.
- **MainMenu migrated** (7 components). The menu is the bridgehead because it
  has no InputField; ReadingRoom's question field needs the separate
  TMP_InputField swap, so ReadingRoom and Result stay on legacy Text for now.
- **`TarotUiTheme` drives both systems** during the migration, and leaves an
  authored vertex gradient alone rather than flattening it to one colour.
- **The title is gilded**: a vertical gold gradient down the glyphs plus a crisp
  SDF stroke, both resolution-independent — the thing the legacy Outline could
  never do, and the point of the exercise.
- **The tofu star row retires** rather than being propped up by a fallback face:
  it was generic decoration restating the title's own mood. Kept in scene.

## Bugs found in my own migration code

- `Unity.TextMeshPro` was not referenced by `TarotUnity.Runtime.asmdef`, so the
  runtime scripts failed with CS0246 and the project dropped into Safe Mode.
  `autoReferenced: true` on a package assembly applies to predefined assemblies
  (Assembly-CSharp), **not** to custom asmdefs, which must list references.
- The migrator was not idempotent: gilding lived inside the Text→TMP conversion,
  so a re-run over an already-migrated scene styled nothing. Typography is now a
  separate pass over whatever TMP is present.
- TMP essentials import is asynchronous; a batch run with `-quit` exits before a
  single file lands. Step 1 drives off `importPackageCompleted` and exits from
  the callback (caller must not pass `-quit`).

## Verification

EditMode 228/228, PlayMode 30/30, HD capture reviewed. Migration-era tests
(Phase 7/8/24/34/35) updated to read either text system so the remaining screens
can migrate independently.

## Next

ReadingRoom and Result still run legacy Text, including the TMP_InputField swap
for the question field.

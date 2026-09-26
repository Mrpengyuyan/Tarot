# 文字 UI 重设计：玻璃为底，卡纸点睛（Phase 69）实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把占卜房、结果页和主菜单「入席问牌」按钮上的金色粗框换成烟色玻璃（容器、未选中项）和象牙卡纸（当前步骤、选中牌阵、下一步动作），字号放大 15%，任何文字都不超出它的框。

**Architecture:**
- 运行时新增三个小单元：
  - `UiFitLayout`：纯静态的尺寸与排布计算；
  - `UiSkinState`：挂在按钮或步骤小牌上，在玻璃和卡纸之间切换；
  - `UiTypeScale`：记录已经应用过的字号倍数，保证重复运行不会重复放大。
- `TarotUiTheme` 改为跳过带皮肤的元素，`RitualStepIndicator` 和 `ReadingRoomController` 通过 `UiSkinState` 切换强调状态。
- 编辑器侧：`Phase69UiKitGenerator` 用 C# 画三张贴图；`Phase69UiRestyleBootstrapper` 按 TMP 首选尺寸算出矩形并写进三个场景。场景层级不变。

**Tech Stack:** Unity 6000.3.16f1、uGUI、TextMeshPro、C#；批处理命令行运行生成器、bootstrapper、截图和测试。

**Spec:** `docs/superpowers/specs/2026-09-26-ui-glass-cardstock-design.md`（含第 9 节补记）

## Global Constraints

- 场景层级不变：不移动、不改名、不删除任何现有对象；只新增 `Phase69_*` 子对象。
- 不修改 Phase 37 / 39 / 40 / 60 / 63 / 67 / 68 的 bootstrapper 源码。例外只有 `Phase67ResultReadingCaptureBuilder.FrameInnerLineUnits` 这一个常量。
- 旧贴图 `TarotPanel.png`、`TarotPanelSubtle.png`、`TarotButton.png` 保留不删。
- 贴图画布坐标：Image 的 `pixelsPerUnitMultiplier = 1`；画布 `referencePixelsPerUnit = 100`，贴图 `spritePixelsPerUnit = 100`，1 像素 = 1 画布单位。
- 两张九宫格贴图外圈都有 8 px 透明 / 投影边（`UiFitLayout.SkinMargin = 8`），所以同一个矩形换成玻璃或卡纸时，看得见的大小相同。
- 颜色（spec 4.3）：
  - 玻璃上文字 `#f5e8cc` α0.85；
  - 卡纸上文字 `#2a1d17`；
  - 步骤「未到」`#f5e8cc` α0.55，「已完成」`#e8c47a`；
  - 容器角星 `#e8b85a`；步骤分隔星 `#e8b85a` α0.6；主按钮侧星 `#9a6b1e`。
- 字号 ×1.15，四舍五入到 0.5，0.5 一律进位（15 → 17.5，16 → 18.5）。例外：
  - 结果页 `MP_ResultSpreadBand/SpreadCell_*/Label` 不放大；
  - 主菜单只放大 `StartReadingButton/Label`。
- `TarotUiTheme.mutedSizeThreshold`：占卜房、结果页设为 18.5，主菜单保持 16。
- 字体图集 `Assets/Fonts/LXGWWenKai-Regular SDF.asset` 和 `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` 的工作区改动不提交、不还原。每次 `git add` 都只加列出的路径，不用 `git add -A` 或 `git commit -a`。
- 同一时间只运行一个 Unity 进程。截图不能用 `-nographics`；bootstrapper 也不用 `-nographics`，以保证 TMP 能生成字形。
- 当前分支 `feat/ui-glass-cardstock`，不推送。
- 提交信息结尾：`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`。

## Review Focus

1. **运行时主题盖掉皮肤**：进入占卜房后，`TarotUiTheme.Awake` 不能把卡纸染成深紫，也不能把卡纸上的深棕文字改成象牙色。由 Task 5 的 PlayMode 测试覆盖。
2. **写问题阶段切换牌阵**：流程状态不变，卡纸也必须跟着移到新选的按钮上。由 Task 5 的 PlayMode 测试覆盖（进入 QuestionInput 后再点另一个牌阵）。
3. **步骤小牌的 Label 排在 Plate 前面**：抽牌、翻牌两个小牌里 Label 在 Plate 之前，不透明的卡纸会把字盖住。Task 5 要求 Plate 放到第一个，并用测试钉住。
4. **操作栏变高后挤到下方状态文字**：由 Task 5 的测试覆盖。要求操作栏、FlowStatusText、Phase10_ReleaseStatusText 三者互不重叠，并且最下面的一行不超出画布（y ≥ -360）。
5. **重复运行 bootstrapper 不能叠加放大**：由 Task 6 的「连续运行两次，场景文件哈希不变」检查覆盖。

## 文件结构

| 文件 | 职责 |
|---|---|
| `Assets/Scripts/UI/UiFitLayout.cs`（新增） | 纯静态：字号缩放、内边距、按文字求矩形、行内排布；本期全部排布常量 |
| `Assets/Scripts/UI/UiSkinState.cs`（新增） | 一个元素的玻璃 / 卡纸切换与按钮 ColorBlock |
| `Assets/Scripts/UI/UiTypeScale.cs`（新增） | 记录已应用的字号倍数 |
| `Assets/Scripts/UI/TarotUiTheme.cs`（修改） | 跳过带皮肤的按钮和文字；`mutedSizeThreshold` 可序列化；保色输入框 |
| `Assets/Scripts/UI/RitualStepIndicator.cs`（修改） | 当前步骤走 `UiSkinState` |
| `Assets/Scripts/UI/ReadingRoomController.cs`（修改） | `ApplySpreadEmphasis(int)` |
| `Assets/Editor/Phase69UiKitGenerator.cs`（新增） | 生成 `GlassPanel.png`、`CardStock.png`、`Sparkle.png` 并设置导入参数 |
| `Assets/Editor/Phase69UiRestyleBootstrapper.cs`（新增） | 三个场景的换皮肤、字号、尺寸、点缀 |
| `Assets/Editor/Phase69MenuCaptureBuilder.cs`（新增） | 主菜单评审截图 |
| `Assets/Editor/Phase67ResultReadingCaptureBuilder.cs`（修改一个常量） | `FrameInnerLineUnits` 18 → 14 |
| `Assets/Tests/EditMode/Phase69UiPrimitivesTests.cs`（新增） | Task 1 |
| `Assets/Tests/EditMode/Phase69ThemeSkinTests.cs`（新增） | Task 2 |
| `Assets/Tests/EditMode/Phase69EmphasisTests.cs`（新增） | Task 3 |
| `Assets/Tests/EditMode/Phase69UiKitTests.cs`（新增） | Task 4 |
| `Assets/Tests/EditMode/Phase69ReadingRoomRestyleTests.cs`（新增） | Task 5 |
| `Assets/Tests/PlayMode/Phase69RuntimeSkinTests.cs`（新增） | Task 5 |
| `Assets/Tests/EditMode/Phase69ResultMenuRestyleTests.cs`（新增） | Task 6 |
| `Assets/Tests/EditMode/Phase39UiReskinTests.cs`、`Phase40MenuResultTests.cs`、`Phase67ResultSceneStructureTests.cs`（修改） | 旧断言改为新规格 |
| `Assets/Art/MidnightParlor/Sprites/{GlassPanel,CardStock,Sparkle}.png`（新增，含 .meta） | 生成的贴图 |
| `Assets/Scenes/{ReadingRoom,Result,MainMenu}.unity`（修改） | bootstrapper 输出 |
| `Docs/PHASE69_UI_GLASS_CARDSTOCK.md`（新增） | 本期说明 |

以下路径以 `UnityClient/TarotUnity/` 为根。命令中的变量：

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
PROJECT=/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
W=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-26-ui-glass-cardstock
R=$W/run/ut.sh      # 测试运行器：bash $R <EditMode|PlayMode> <唯一tag> [-testFilter X]
PT=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-12-online-interpretation-unity/scratch/online-a-run/pt.sh
```

`ut.sh` 每个 tag 只能用一次；tag 重复时它会拒绝运行，需要换一个新 tag。

---

### Task 1: 运行时基础单元（UiFitLayout / UiSkinState / UiTypeScale）

**Files:**
- Create: `Assets/Scripts/UI/UiFitLayout.cs`、`Assets/Scripts/UI/UiSkinState.cs`、`Assets/Scripts/UI/UiTypeScale.cs`
- Test: `Assets/Tests/EditMode/Phase69UiPrimitivesTests.cs`

**Interfaces:**
- Produces:
  - `UiFitLayout`：
    - 常量 `SkinMargin`、`TypeScale`、`StepGap`、`StepDotSize`、`HudPad`、`DockPad`、`DockRowGap`、`DockTop`、`BelowDockGap`、`RowGap`、`InputWidthRatio`、`CornerStarSize`、`FlankStarPerFont`、`FlankGapPerFont`、`CanvasHalfHeight`、`MutedSizeThresholdScaled`；
    - `float ScaledFontSize(float size, float factor)`；
    - `Vector2 Padding(float fontSize)`；
    - `Vector2 FitSize(Vector2 preferred, float fontSize)`；
    - `float[] RowCenters(float[] widths, float gap)`。
  - `UiSkinState`：
    - `void Configure(Image plate, Graphic label, Sprite glass, Sprite cardStock, bool driveLabelColor)`、`void SetEmphasis(bool)`；
    - 属性 `IsEmphasized`、`Plate`、`Label`、`Glass`、`CardStock`；
    - 静态 `GlassLabel`、`CardStockLabel`、`ColorBlock SkinColors`。
  - `UiTypeScale.AppliedScale`（可读写 float，默认 1）。

- [ ] **Step 0: 准备本期工作区（不入库）**

```bash
mkdir -p "$W/run" && cp /Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-23-reading-room-table/run/{ut.sh,summarize.py} "$W/run/"
```

- [ ] **Step 1: 写失败测试** `Assets/Tests/EditMode/Phase69UiPrimitivesTests.cs`

```csharp
using NUnit.Framework;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 69: the fit maths and the glass / card-stock switch.</summary>
    public sealed class Phase69UiPrimitivesTests
    {
        [TestCase(13f, 15f)]
        [TestCase(15f, 17.5f)]
        [TestCase(16f, 18.5f)]
        [TestCase(17f, 19.5f)]
        [TestCase(18f, 20.5f)]
        [TestCase(20f, 23f)]
        [TestCase(22f, 25.5f)]
        [TestCase(23f, 26.5f)]
        [TestCase(24f, 27.5f)]
        public void TypeScaleRoundsHalfUpToHalfPoints(float size, float expected)
        {
            Assert.That(UiFitLayout.ScaledFontSize(size, UiFitLayout.TypeScale), Is.EqualTo(expected));
        }

        [Test]
        public void ScalingByOneLeavesTheSizeAlone()
        {
            Assert.That(UiFitLayout.ScaledFontSize(17.5f, 1f), Is.EqualTo(17.5f));
            Assert.That(UiFitLayout.ScaledFontSize(19.5f, UiFitLayout.TypeScale / UiFitLayout.TypeScale), Is.EqualTo(19.5f));
        }

        [Test]
        public void MutedThresholdTracksTheScaledSixteen()
        {
            Assert.That(UiFitLayout.MutedSizeThresholdScaled, Is.EqualTo(UiFitLayout.ScaledFontSize(16f, UiFitLayout.TypeScale)));
        }

        [Test]
        public void FitSizeWrapsTextInPaddingAndTheSkinMargin()
        {
            var size = UiFitLayout.FitSize(new Vector2(52.3f, 22.1f), 17.5f);
            Assert.That(UiFitLayout.Padding(17.5f), Is.EqualTo(new Vector2(24f, 9f)), "Mathf.Round: 24.5 -> 24, 8.75 -> 9");
            Assert.That(size.x, Is.EqualTo(53f + 2f * 24f + 2f * UiFitLayout.SkinMargin));
            Assert.That(size.y, Is.EqualTo(23f + 2f * 9f + 2f * UiFitLayout.SkinMargin));
        }

        [Test]
        public void RowCentersAreSymmetricWithTheGivenGap()
        {
            var centers = UiFitLayout.RowCenters(new[] { 100f, 60f, 100f }, 20f);
            Assert.That(centers, Is.EqualTo(new[] { -100f, 0f, 100f }));
            var single = UiFitLayout.RowCenters(new[] { 80f }, 20f);
            Assert.That(single, Is.EqualTo(new[] { 0f }));
        }

        [Test]
        public void EmphasisSwapsSpriteAndLabelColour()
        {
            var (skin, plate, label, glass, stock) = MakeSkin(withGlass: true);
            skin.SetEmphasis(false);
            Assert.That(skin.IsEmphasized, Is.False);
            Assert.That(plate.sprite, Is.SameAs(glass));
            Assert.That(plate.enabled, Is.True);
            Assert.That(label.color, Is.EqualTo(UiSkinState.GlassLabel));

            skin.SetEmphasis(true);
            Assert.That(skin.IsEmphasized, Is.True);
            Assert.That(plate.sprite, Is.SameAs(stock));
            Assert.That(plate.color, Is.EqualTo(Color.white));
            Assert.That(label.color, Is.EqualTo(UiSkinState.CardStockLabel));
            Object.DestroyImmediate(skin.gameObject);
        }

        [Test]
        public void NoGlassMeansNoPlateWhenNotEmphasized()
        {
            var (skin, plate, _, _, stock) = MakeSkin(withGlass: false);
            skin.SetEmphasis(false);
            Assert.That(plate.enabled, Is.False);
            Assert.That(plate.color, Is.EqualTo(Color.clear), "Phase 61 compares plate brightness by colour");
            skin.SetEmphasis(true);
            Assert.That(plate.enabled, Is.True);
            Assert.That(plate.sprite, Is.SameAs(stock));
            Object.DestroyImmediate(skin.gameObject);
        }

        [Test]
        public void ButtonsGetTheSkinColorBlock()
        {
            var (skin, _, _, _, _) = MakeSkin(withGlass: true);
            var button = skin.gameObject.AddComponent<Button>();
            skin.SetEmphasis(true);
            Assert.That(button.colors.normalColor, Is.EqualTo(Color.white));
            Assert.That(button.colors.disabledColor.a, Is.EqualTo(0.45f).Within(0.001f));
            Object.DestroyImmediate(skin.gameObject);
        }

        [Test]
        public void LabelColourCanBeLeftToAnotherDriver()
        {
            var go = new GameObject("chip", typeof(RectTransform));
            var plate = go.AddComponent<Image>();
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform);
            var label = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            label.color = Color.red;
            var skin = go.AddComponent<UiSkinState>();
            skin.Configure(plate, label, null, Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero), false);
            skin.SetEmphasis(true);
            Assert.That(label.color, Is.EqualTo(Color.red));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TypeScaleMarkerDefaultsToOne()
        {
            var go = new GameObject("marker");
            var marker = go.AddComponent<UiTypeScale>();
            Assert.That(marker.AppliedScale, Is.EqualTo(1f));
            marker.AppliedScale = UiFitLayout.TypeScale;
            Assert.That(marker.AppliedScale, Is.EqualTo(UiFitLayout.TypeScale));
            Object.DestroyImmediate(go);
        }

        private static (UiSkinState, Image, TMPro.TMP_Text, Sprite, Sprite) MakeSkin(bool withGlass)
        {
            var go = new GameObject("skinned", typeof(RectTransform));
            var plate = go.AddComponent<Image>();
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform);
            var label = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            var glass = withGlass ? Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero) : null;
            var stock = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var skin = go.AddComponent<UiSkinState>();
            skin.Configure(plate, label, glass, stock, true);
            return (skin, plate, label, glass, stock);
        }
    }
}
```

- [ ] **Step 2: 运行，确认失败（编译错误：类型不存在）**

Run: `bash $R EditMode p69-t1-red -testFilter Phase69UiPrimitivesTests`
Expected: 编译失败或 0 通过，日志里有 `UiFitLayout` 不存在。

- [ ] **Step 3: 实现** `Assets/Scripts/UI/UiFitLayout.cs`

```csharp
using System;
using UnityEngine;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 69: the sizing rules of the glass-and-card-stock UI. Panels are sized by their
    /// text: a TMP preferred size plus padding that scales with the font, plus the transparent
    /// or shadow ring baked into both nine-slice sprites. Pure maths, so the editor bootstrapper
    /// and the tests share one source.
    /// </summary>
    public static class UiFitLayout
    {
        /// <summary>Transparent (glass) or shadow (card stock) ring inside both sprites, in canvas units.</summary>
        public const float SkinMargin = 8f;
        public const float TypeScale = 1.15f;
        public const float PadXPerFont = 1.4f;
        public const float PadYPerFont = 0.5f;

        /// <summary>16pt, the theme's muted-text limit, after the type scale.</summary>
        public const float MutedSizeThresholdScaled = 18.5f;

        // Step bar.
        public const float StepGap = 20f;
        public const float StepDotSize = 12f;
        public static readonly Vector2 HudPad = new Vector2(20f, 4f);

        // Action dock.
        public const float RowGap = 20f;
        public static readonly Vector2 DockPad = new Vector2(24f, 10f);
        public const float DockRowGap = 2f;
        public const float DockTop = -122f;
        public const float BelowDockGap = 4f;
        public const float InputWidthRatio = 0.62f;

        // Stars.
        public const float CornerStarSize = 18f;
        public const float FlankStarPerFont = 0.6f;
        public const float FlankGapPerFont = 0.35f;

        public const float CanvasHalfHeight = 360f;

        /// <summary>Scales a font size and rounds it to the nearest half point, halves rounding up.</summary>
        public static float ScaledFontSize(float size, float factor)
        {
            if (Mathf.Abs(factor - 1f) < 1e-4f)
            {
                return size;
            }

            return (float)(Math.Floor((double)size * factor * 2.0 + 0.5 + 1e-4) / 2.0);
        }

        public static Vector2 Padding(float fontSize)
        {
            return new Vector2(Mathf.Round(PadXPerFont * fontSize), Mathf.Round(PadYPerFont * fontSize));
        }

        /// <summary>The rect that holds text of this preferred size without touching the frame.</summary>
        public static Vector2 FitSize(Vector2 preferred, float fontSize)
        {
            var pad = Padding(fontSize);
            return new Vector2(
                Mathf.Ceil(preferred.x) + 2f * pad.x + 2f * SkinMargin,
                Mathf.Ceil(preferred.y) + 2f * pad.y + 2f * SkinMargin);
        }

        /// <summary>Centres of items laid edge to edge with a fixed gap, the row centred on zero.</summary>
        public static float[] RowCenters(float[] widths, float gap)
        {
            var total = 0f;
            foreach (var w in widths)
            {
                total += w;
            }

            total += gap * Mathf.Max(0, widths.Length - 1);
            var centers = new float[widths.Length];
            var x = -total / 2f;
            for (var i = 0; i < widths.Length; i++)
            {
                centers[i] = x + widths[i] / 2f;
                x += widths[i] + gap;
            }

            return centers;
        }
    }
}
```

`Assets/Scripts/UI/UiTypeScale.cs`：

```csharp
using UnityEngine;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 69: records the type scale already applied under this object, so the restyle
    /// bootstrapper scales by (target / applied) and running it again changes nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiTypeScale : MonoBehaviour
    {
        [SerializeField] private float appliedScale = 1f;

        public float AppliedScale
        {
            get => appliedScale;
            set => appliedScale = value;
        }
    }
}
```

`Assets/Scripts/UI/UiSkinState.cs`：

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 69: one element of the glass-and-card-stock UI. Emphasised (the current step,
    /// the chosen spread, the next action) it wears ivory card stock with dark ink; otherwise
    /// smoked glass with ivory ink, or no plate at all when it has no glass sprite. Buttons
    /// also get a white-based ColorBlock so the sprite colours survive, and TarotUiTheme leaves
    /// anything carrying this component to it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiSkinState : MonoBehaviour
    {
        public static readonly Color GlassLabel = new Color(0.961f, 0.910f, 0.800f, 0.85f);   // #f5e8cc
        public static readonly Color CardStockLabel = new Color(0.165f, 0.114f, 0.090f, 1f);  // #2a1d17

        public static ColorBlock SkinColors => new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1f, 0.96f, 0.86f, 1f),
            pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f),
            selectedColor = Color.white,
            disabledColor = new Color(1f, 1f, 1f, 0.45f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f,
        };

        [SerializeField] private Image plate;
        [SerializeField] private Graphic label;
        [SerializeField] private Sprite glass;
        [SerializeField] private Sprite cardStock;
        [SerializeField] private bool driveLabelColor = true;
        [SerializeField] private bool emphasized;

        public bool IsEmphasized => emphasized;
        public Image Plate => plate;
        public Graphic Label => label;
        public Sprite Glass => glass;
        public Sprite CardStock => cardStock;

        public void Configure(Image plate, Graphic label, Sprite glass, Sprite cardStock, bool driveLabelColor)
        {
            this.plate = plate;
            this.label = label;
            this.glass = glass;
            this.cardStock = cardStock;
            this.driveLabelColor = driveLabelColor;
            Apply();
        }

        public void SetEmphasis(bool on)
        {
            emphasized = on;
            Apply();
        }

        // Start runs after every Awake, so this also settles anything set before the scene woke.
        private void Start()
        {
            Apply();
        }

        private void Apply()
        {
            if (plate != null)
            {
                var sprite = emphasized ? cardStock : glass;
                plate.sprite = sprite;
                plate.type = Image.Type.Sliced;
                plate.pixelsPerUnitMultiplier = 1f;
                plate.enabled = sprite != null;
                plate.color = sprite != null ? Color.white : Color.clear;
            }

            if (driveLabelColor && label != null)
            {
                label.color = emphasized ? CardStockLabel : GlassLabel;
            }

            var button = GetComponent<Button>();
            if (button != null)
            {
                button.colors = SkinColors;
            }
        }
    }
}
```

- [ ] **Step 4: 运行，确认通过**

Run: `bash $R EditMode p69-t1-green -testFilter Phase69UiPrimitivesTests`
Expected: `total=18 passed=18`（9 个 TestCase + 9 个 Test）。

- [ ] **Step 5: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
git add Assets/Scripts/UI/UiFitLayout.cs Assets/Scripts/UI/UiFitLayout.cs.meta Assets/Scripts/UI/UiSkinState.cs Assets/Scripts/UI/UiSkinState.cs.meta Assets/Scripts/UI/UiTypeScale.cs Assets/Scripts/UI/UiTypeScale.cs.meta Assets/Tests/EditMode/Phase69UiPrimitivesTests.cs Assets/Tests/EditMode/Phase69UiPrimitivesTests.cs.meta
git commit -m "feat(ui): fit maths and glass/card-stock skin state (Phase 69)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

（.meta 由 Step 4 运行 Unity 时生成；如果没有生成，就先运行一次测试再提交。）

---

### Task 2: 运行时主题让位给皮肤

**Files:**
- Modify: `Assets/Scripts/UI/TarotUiTheme.cs`（字段区、`ApplyTmpTextStyle`、`ApplyTextStyle`、`ApplyButtonStyle`、`ApplyTmpInputStyle`）
- Test: `Assets/Tests/EditMode/Phase69ThemeSkinTests.cs`

**Interfaces:**
- Consumes: `UiSkinState`（Task 1），现有 `TarotUiPreserveColor`。
- Produces: 序列化字段 `mutedSizeThreshold`（float，默认 16）和只读属性 `MutedSizeThreshold`。

- [ ] **Step 1: 写失败测试** `Assets/Tests/EditMode/Phase69ThemeSkinTests.cs`

```csharp
using NUnit.Framework;
using TMPro;
using TarotUnity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 69: TarotUiTheme recolours every button and text on Awake. Skinned elements must
    /// keep their card stock and ink; the muted-text limit follows the type scale.
    /// </summary>
    public sealed class Phase69ThemeSkinTests
    {
        private static readonly Color ThemeText = new Color(0.96f, 0.91f, 0.80f, 1f);
        private static readonly Color ThemeMuted = new Color(0.74f, 0.72f, 0.76f, 1f);
        private GameObject root;

        [TearDown]
        public void Clean()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SkinnedButtonKeepsItsColoursAndInk()
        {
            var theme = MakeTheme();
            var (button, label) = MakeButton("Skinned");
            var skin = button.gameObject.AddComponent<UiSkinState>();
            skin.Configure(button.GetComponent<Image>(), label, null, Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero), true);
            skin.SetEmphasis(true);

            theme.Apply();

            Assert.That(button.colors.normalColor, Is.EqualTo(Color.white));
            Assert.That(button.targetGraphic.color, Is.EqualTo(Color.white));
            Assert.That(label.color, Is.EqualTo(UiSkinState.CardStockLabel));
        }

        [Test]
        public void UnskinnedButtonStillGetsTheThemeColours()
        {
            var theme = MakeTheme();
            var (button, _) = MakeButton("Plain");

            theme.Apply();

            Assert.That(button.colors.normalColor, Is.Not.EqualTo(Color.white), "control: the theme still styles plain buttons");
        }

        [Test]
        public void MutedLimitIsSerializedAndDrivesTheColour()
        {
            var theme = MakeTheme();
            var text = MakeText("Small", 17.5f);

            theme.Apply();
            Assert.That(text.color, Is.EqualTo(ThemeText), "17.5 is above the default 16 limit");

            var so = new SerializedObject(theme);
            so.FindProperty("mutedSizeThreshold").floatValue = UiFitLayout.MutedSizeThresholdScaled;
            so.ApplyModifiedPropertiesWithoutUndo();
            theme.Apply();
            Assert.That(theme.MutedSizeThreshold, Is.EqualTo(18.5f));
            Assert.That(text.color, Is.EqualTo(ThemeMuted));
        }

        [Test]
        public void PreservedInputKeepsItsTransparentGround()
        {
            var theme = MakeTheme();
            var go = new GameObject("Input", typeof(RectTransform));
            go.transform.SetParent(root.transform);
            var ground = go.AddComponent<Image>();
            ground.color = new Color(1f, 1f, 1f, 0f);
            var input = go.AddComponent<TMP_InputField>();
            input.targetGraphic = ground;
            go.AddComponent<TarotUiPreserveColor>();

            theme.Apply();

            Assert.That(ground.color.a, Is.EqualTo(0f));
        }

        private TarotUiTheme MakeTheme()
        {
            root = new GameObject("ThemeRoot", typeof(RectTransform));
            return root.AddComponent<TarotUiTheme>();
        }

        private (Button, TMP_Text) MakeButton(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root.transform);
            var image = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = UiSkinState.SkinColors;
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = 19.5f;
            return (button, label);
        }

        private TMP_Text MakeText(string name, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root.transform);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            return text;
        }
    }
}
```

- [ ] **Step 2: 运行，确认失败**

Run: `bash $R EditMode p69-t2-red -testFilter Phase69ThemeSkinTests`
Expected: 编译失败（`MutedSizeThreshold` 不存在）。

- [ ] **Step 3: 修改 `TarotUiTheme.cs`**

在 `[SerializeField] private int displaySizeThreshold = 30;` 下一行加：

```csharp
        // Phase 69: text at or below this size reads as muted. Scenes whose type was scaled
        // by 1.15 raise it with the type (16 -> 18.5) so no text changes role.
        [SerializeField] private float mutedSizeThreshold = 16f;
```

在 `public int DisplaySizeThreshold => displaySizeThreshold;` 下一行加：

```csharp
        public float MutedSizeThreshold => mutedSizeThreshold;
```

`ApplyTmpTextStyle` 里，把

```csharp
            if (!text.enableVertexGradient && text.GetComponent<TarotUiPreserveColor>() == null)
```

改成

```csharp
            // Phase 69: a skinned element (UiSkinState) owns its ink.
            if (!text.enableVertexGradient && text.GetComponent<TarotUiPreserveColor>() == null
                && text.GetComponentInParent<UiSkinState>(true) == null)
```

同一方法里把 `text.color = text.fontSize <= 16f ? mutedTextColor : textColor;` 改成：

```csharp
                    text.color = text.fontSize <= mutedSizeThreshold ? mutedTextColor : textColor;
```

`ApplyTextStyle` 里把 `text.color = text.fontSize <= 16 ? mutedTextColor : textColor;` 改成：

```csharp
                    text.color = text.fontSize <= mutedSizeThreshold ? mutedTextColor : textColor;
```

`ApplyButtonStyle` 开头的空检查之后加：

```csharp
            // Phase 69: skinned buttons keep the white-based ColorBlock their sprites need.
            if (button.GetComponent<UiSkinState>() != null)
            {
                return;
            }
```

`ApplyTmpInputStyle` 里把

```csharp
            if (input.targetGraphic != null)
            {
                input.targetGraphic.color = inputColor;
            }
```

改成

```csharp
            // Phase 69: the question field is an underline over a transparent, still clickable ground.
            if (input.targetGraphic != null && input.GetComponent<TarotUiPreserveColor>() == null)
            {
                input.targetGraphic.color = inputColor;
            }
```

- [ ] **Step 4: 运行本任务测试和已有的主题测试**

Run: `bash $R EditMode p69-t2-green -testFilter "Phase69ThemeSkinTests|Phase24TypographyTests"`
Expected: 全部通过。

- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/UI/TarotUiTheme.cs Assets/Tests/EditMode/Phase69ThemeSkinTests.cs Assets/Tests/EditMode/Phase69ThemeSkinTests.cs.meta
git commit -m "feat(ui): theme leaves skinned elements alone, muted limit is serialized (Phase 69)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: 步骤条和牌阵按钮走 UiSkinState

**Files:**
- Modify: `Assets/Scripts/UI/RitualStepIndicator.cs`（`Refresh`）、`Assets/Scripts/UI/ReadingRoomController.cs`（`SelectSpread` 与新增方法）
- Test: `Assets/Tests/EditMode/Phase69EmphasisTests.cs`

**Interfaces:**
- Consumes: `UiSkinState.SetEmphasis/IsEmphasized`。
- Produces: `ReadingRoomController.ApplySpreadEmphasis(int cardCount)`（public）；`RitualStepIndicator` 在 chip 根对象带 `UiSkinState` 时，当前步 `SetEmphasis(true)`、其余 `false`，并且不再给 plate 着色。

- [ ] **Step 1: 写失败测试** `Assets/Tests/EditMode/Phase69EmphasisTests.cs`

```csharp
using NUnit.Framework;
using TarotUnity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 69: only the current step and the chosen spread wear card stock.</summary>
    public sealed class Phase69EmphasisTests
    {
        private GameObject root;

        [TearDown]
        public void Clean()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OnlyTheCurrentStepIsEmphasized()
        {
            root = new GameObject("Hud");
            var indicator = root.AddComponent<RitualStepIndicator>();
            var skins = new UiSkinState[5];
            var so = new SerializedObject(indicator);
            var chips = so.FindProperty("chips");
            chips.arraySize = skins.Length;
            for (var i = 0; i < skins.Length; i++)
            {
                var chip = new GameObject($"Chip{i}", typeof(RectTransform));
                chip.transform.SetParent(root.transform);
                var plate = new GameObject("Plate", typeof(RectTransform)).AddComponent<Image>();
                plate.transform.SetParent(chip.transform);
                var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
                label.transform.SetParent(chip.transform);
                skins[i] = chip.AddComponent<UiSkinState>();
                skins[i].Configure(plate, label, null, Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero), false);
                var element = chips.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = chip.transform;
                element.FindPropertyRelative("plate").objectReferenceValue = plate;
                element.FindPropertyRelative("label").objectReferenceValue = label;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            for (var step = 0; step < skins.Length; step++)
            {
                indicator.SetStep(step);
                for (var i = 0; i < skins.Length; i++)
                {
                    Assert.That(skins[i].IsEmphasized, Is.EqualTo(i == step), $"step {step}, chip {i}");
                }
            }
        }

        [Test]
        public void OnlyTheChosenSpreadButtonIsEmphasized()
        {
            root = new GameObject("Room");
            var controller = root.AddComponent<ReadingRoomController>();
            var so = new SerializedObject(controller);
            var skins = new System.Collections.Generic.Dictionary<int, UiSkinState>();
            foreach (var (field, count) in new[] { ("oneCardButton", 1), ("threeCardButton", 3), ("celticCrossButton", 10) })
            {
                var go = new GameObject(field, typeof(RectTransform));
                go.transform.SetParent(root.transform);
                var image = go.AddComponent<Image>();
                var button = go.AddComponent<Button>();
                var skin = go.AddComponent<UiSkinState>();
                skin.Configure(image, null, Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero),
                    Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero), true);
                so.FindProperty(field).objectReferenceValue = button;
                skins[count] = skin;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (var chosen in new[] { 1, 3, 10, 3 })
            {
                controller.ApplySpreadEmphasis(chosen);
                foreach (var pair in skins)
                {
                    Assert.That(pair.Value.IsEmphasized, Is.EqualTo(pair.Key == chosen), $"chosen {chosen}, button {pair.Key}");
                }
            }
        }
    }
}
```

- [ ] **Step 2: 运行，确认失败**

Run: `bash $R EditMode p69-t3-red -testFilter Phase69EmphasisTests`
Expected: 编译失败（`ApplySpreadEmphasis` 不存在）。

- [ ] **Step 3: 修改 `RitualStepIndicator.Refresh`**

把

```csharp
                if (chip.plate != null)
                {
                    chip.plate.color = plateColor;
                }
```

改成

```csharp
                // Phase 69: a skinned chip shows card stock when current and no plate otherwise.
                var skin = chip.root != null ? chip.root.GetComponent<UiSkinState>() : null;
                if (skin != null)
                {
                    skin.SetEmphasis(isCurrent);
                }
                else if (chip.plate != null)
                {
                    chip.plate.color = plateColor;
                }
```

- [ ] **Step 4: 修改 `ReadingRoomController`**

在 `SelectSpread` 里 `selectedSpreadName = spreadName;` 之后加一行：

```csharp
            ApplySpreadEmphasis(cardCount);
```

在 `SetDrawControls` 之前加：

```csharp
        /// <summary>
        /// Phase 69: the chosen spread's button wears card stock, the others glass. Called on
        /// every selection, including a switch while the question is being written.
        /// </summary>
        public void ApplySpreadEmphasis(int cardCount)
        {
            SetEmphasis(oneCardButton, cardCount == 1);
            SetEmphasis(threeCardButton, cardCount == 3);
            SetEmphasis(celticCrossButton, cardCount == 10);
        }

        private static void SetEmphasis(Button button, bool on)
        {
            var skin = button != null ? button.GetComponent<UiSkinState>() : null;
            if (skin != null)
            {
                skin.SetEmphasis(on);
            }
        }
```

（`Start()` 已经调用 `SelectOneCard()`，所以开场时会自动选中「一张牌」，不需要另外调用。）

- [ ] **Step 5: 运行本任务测试和 Phase 61 步骤条测试**

Run: `bash $R EditMode p69-t3-green -testFilter "Phase69EmphasisTests|Phase61ReadingRoomSlotStepTests"`
Expected: 全部通过。场景还没有挂 `UiSkinState`，Phase 61 仍走旧的着色分支。

- [ ] **Step 6: 提交**

```bash
git add Assets/Scripts/UI/RitualStepIndicator.cs Assets/Scripts/UI/ReadingRoomController.cs Assets/Tests/EditMode/Phase69EmphasisTests.cs Assets/Tests/EditMode/Phase69EmphasisTests.cs.meta
git commit -m "feat(ui): current step and chosen spread switch to card stock (Phase 69)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: 生成三张贴图

**Files:**
- Create: `Assets/Editor/Phase69UiKitGenerator.cs`
- Create（生成）: `Assets/Art/MidnightParlor/Sprites/GlassPanel.png`、`CardStock.png`、`Sparkle.png`（含 .meta）
- Test: `Assets/Tests/EditMode/Phase69UiKitTests.cs`

**Interfaces:**
- Produces:
  - `Phase69UiKitGenerator.Run()`（菜单 `Tools/Tarot Unity/Generate Phase 69 UI Kit`）；
  - 常量 `GlassPath`、`CardStockPath`、`SparklePath`；
  - 三张贴图：Single Sprite，九宫格边界：玻璃、卡纸 `(32,32,32,32)`，Sparkle 为 0。
  - 几何：96×96，外圈 8 px 透明 / 投影；玻璃内线的中心在距贴图边 13.5 px 处，所以 `FrameInnerLineUnits = 14`。

- [ ] **Step 1: 写失败测试** `Assets/Tests/EditMode/Phase69UiKitTests.cs`

```csharp
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 69: the generated glass, card-stock and sparkle sprites.</summary>
    public sealed class Phase69UiKitTests
    {
        private const string Folder = "Assets/Art/MidnightParlor/Sprites";

        [TestCase("GlassPanel", 32f)]
        [TestCase("CardStock", 32f)]
        [TestCase("Sparkle", 0f)]
        public void SpriteImportsAsSingleWithItsBorder(string name, float border)
        {
            var path = $"{Folder}/{name}.png";
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path), Is.Not.Null, path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.spriteBorder, Is.EqualTo(new Vector4(border, border, border, border)));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f));
        }

        [Test]
        public void GlassIsSmokyInsideAndClearInTheMargin()
        {
            var tex = Load("GlassPanel");
            Assert.That(tex.width, Is.EqualTo(96));
            var centre = tex.GetPixel(48, 48);
            Assert.That(centre.a, Is.EqualTo(0.52f).Within(0.02f));
            Assert.That(centre.r, Is.EqualTo(14f / 255f).Within(0.02f));
            Assert.That(tex.GetPixel(3, 48).a, Is.EqualTo(0f), "the 8 px margin is clear");
            var line = tex.GetPixel(8, 48);
            Assert.That(line.r, Is.GreaterThan(0.4f), "the outer gold hairline sits on the margin edge");
            var inner = tex.GetPixel(13, 48);
            Assert.That(inner.r, Is.GreaterThan(centre.r + 0.05f), "the faint inner line");
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void CardStockIsIvoryWithADarkKeylineAndAShadow()
        {
            var tex = Load("CardStock");
            var centre = tex.GetPixel(48, 48);
            Assert.That(centre.a, Is.EqualTo(1f).Within(0.01f));
            Assert.That(centre.r, Is.GreaterThan(0.88f));
            Assert.That(tex.GetPixel(48, 84).r, Is.GreaterThan(tex.GetPixel(48, 12).r), "lighter at the top");
            Assert.That(tex.GetPixel(11, 48).r, Is.LessThan(0.5f), "keyline 3 px inside the stock edge");
            var shadow = tex.GetPixel(4, 40);
            Assert.That(shadow.a, Is.GreaterThan(0.05f).And.LessThan(0.6f));
            Assert.That(shadow.r, Is.LessThan(0.1f));
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void SparkleIsAFourPointStar()
        {
            var tex = Load("Sparkle");
            Assert.That(tex.width, Is.EqualTo(64));
            Assert.That(tex.GetPixel(32, 32).a, Is.GreaterThan(0.95f));
            Assert.That(tex.GetPixel(32, 50).a, Is.GreaterThan(0.5f), "an arm");
            Assert.That(tex.GetPixel(44, 44).a, Is.LessThan(0.05f), "the diagonal between arms");
            Assert.That(tex.GetPixel(1, 1).a, Is.LessThan(0.02f));
            Object.DestroyImmediate(tex);
        }

        private static Texture2D Load(string name)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes($"{Folder}/{name}.png"));
            return tex;
        }
    }
}
```

逐字节一致性由 Step 5 在命令行检查：EditMode 测试程序集引用不到编辑器代码，不能在测试里调用生成器。

- [ ] **Step 2: 运行，确认失败**

Run: `bash $R EditMode p69-t4-red -testFilter Phase69UiKitTests`
Expected: 失败（贴图不存在）。

- [ ] **Step 3: 实现** `Assets/Editor/Phase69UiKitGenerator.cs`

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 69: draws the glass-and-card-stock kit in C#, so it rebuilds without Python/PIL.
    /// Both panels are 96 px with an 8 px outer ring - clear on the glass, a soft shadow on the
    /// card stock - so a rect shows the same visible size in either state. Output is
    /// deterministic; files are only rewritten when their bytes change.
    /// </summary>
    public static class Phase69UiKitGenerator
    {
        public const string GlassPath = "Assets/Art/MidnightParlor/Sprites/GlassPanel.png";
        public const string CardStockPath = "Assets/Art/MidnightParlor/Sprites/CardStock.png";
        public const string SparklePath = "Assets/Art/MidnightParlor/Sprites/Sparkle.png";

        private const int PanelSize = 96;
        private const float Margin = 8f;
        private const float Radius = 4f;
        private const int PanelBorder = 32;

        [MenuItem("Tools/Tarot Unity/Generate Phase 69 UI Kit")]
        public static void Run()
        {
            Write(GlassPath, DrawGlass());
            Write(CardStockPath, DrawCardStock());
            Write(SparklePath, DrawSparkle());
            AssetDatabase.Refresh();
            Configure(GlassPath, PanelBorder);
            Configure(CardStockPath, PanelBorder);
            Configure(SparklePath, 0);
            Debug.Log("Phase 69 UI kit generated.");
        }

        private static Color[] DrawGlass()
        {
            var fill = new Color(14f / 255f, 6f / 255f, 14f / 255f, 0.52f);
            var gold = new Color(219f / 255f, 161f / 255f, 61f / 255f, 1f);
            var px = new Color[PanelSize * PanelSize];
            for (var y = 0; y < PanelSize; y++)
            {
                for (var x = 0; x < PanelSize; x++)
                {
                    var d = PanelSdf(x + 0.5f, y + 0.5f);
                    var c = fill;
                    c.a *= Coverage(d);
                    c = Over(WithAlpha(gold, 0.55f * Band(d, 0.5f)), c);
                    c = Over(WithAlpha(gold, 0.22f * Band(d, 5.5f)), c);
                    px[y * PanelSize + x] = c;
                }
            }

            return px;
        }

        private static Color[] DrawCardStock()
        {
            var top = new Color(242f / 255f, 231f / 255f, 203f / 255f, 1f);
            var bottom = new Color(230f / 255f, 214f / 255f, 176f / 255f, 1f);
            var keyline = new Color(58f / 255f, 42f / 255f, 32f / 255f, 1f);
            var px = new Color[PanelSize * PanelSize];
            for (var y = 0; y < PanelSize; y++)
            {
                for (var x = 0; x < PanelSize; x++)
                {
                    var d = PanelSdf(x + 0.5f, y + 0.5f);
                    // Shadow: the same shape nudged 2 px down, fading over the 8 px ring.
                    var ds = PanelSdf(x + 0.5f, y + 0.5f + 2f);
                    var fade = 1f - Mathf.Clamp01(ds / Margin);
                    var c = new Color(0f, 0f, 0f, 0.55f * fade * fade);
                    var t = Mathf.InverseLerp(Margin, PanelSize - Margin, y + 0.5f);
                    var stock = Color.Lerp(bottom, top, t);
                    stock.a = Coverage(d);
                    c = Over(stock, c);
                    c = Over(WithAlpha(keyline, 0.85f * Band(d, 3.5f)), c);
                    px[y * PanelSize + x] = c;
                }
            }

            return px;
        }

        private static Color[] DrawSparkle()
        {
            const int size = 64;
            var px = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs((x + 0.5f - size / 2f) / (size / 2f));
                    var dy = Mathf.Abs((y + 0.5f - size / 2f) / (size / 2f));
                    var s = Mathf.Sqrt(dx) + Mathf.Sqrt(dy);          // |x|^0.5 + |y|^0.5 <= 1 is a four-point star
                    var star = Mathf.Clamp01((1f - s) * 6f);
                    var r2 = dx * dx + dy * dy;
                    var glow = 0.35f * Mathf.Exp(-r2 * 12f);
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Max(star, glow));
                }
            }

            return px;
        }

        /// <summary>Signed distance to the panel's rounded rect (negative inside).</summary>
        private static float PanelSdf(float x, float y)
        {
            var c = PanelSize / 2f;
            var half = c - Margin - Radius;
            var qx = Mathf.Abs(x - c) - half;
            var qy = Mathf.Abs(y - c) - half;
            var ox = Mathf.Max(qx, 0f);
            var oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - Radius;
        }

        private static float Coverage(float d) => Mathf.Clamp01(0.5f - d);

        /// <summary>A 1 px line whose centre is <paramref name="inset"/> px inside the edge.</summary>
        private static float Band(float d, float inset) => Mathf.Clamp01(1f - Mathf.Abs(-d - inset));

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static Color Over(Color top, Color under)
        {
            var a = top.a + under.a * (1f - top.a);
            if (a <= 0f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            var r = (top.r * top.a + under.r * under.a * (1f - top.a)) / a;
            var g = (top.g * top.a + under.g * under.a * (1f - top.a)) / a;
            var b = (top.b * top.a + under.b * under.a * (1f - top.a)) / a;
            return new Color(r, g, b, a);
        }

        private static void Write(string path, Color[] pixels)
        {
            var size = (int)Mathf.Sqrt(pixels.Length);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            var bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            if (File.Exists(path) && ByteEqual(File.ReadAllBytes(path), bytes))
            {
                return;
            }

            File.WriteAllBytes(path, bytes);
        }

        private static bool ByteEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void Configure(string path, int border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;   // the project default is Multiple
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
```

- [ ] **Step 4: 批处理运行生成器**

```bash
"$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase69UiKitGenerator.Run -quit -logFile "$W/run/p69-kit-1.log"; echo exit=$?
grep -n "Phase 69 UI kit generated\|error" "$W/run/p69-kit-1.log" | head
```

Expected: `exit=0`，日志里有 `Phase 69 UI kit generated.`。

- [ ] **Step 5: 确认输出确定（重复生成逐字节相同）**

```bash
cd "$PROJECT/Assets/Art/MidnightParlor/Sprites"
shasum GlassPanel.png CardStock.png Sparkle.png > "$W/run/kit-sha-1.txt"
"$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase69UiKitGenerator.Run -quit -logFile "$W/run/p69-kit-2.log"
shasum GlassPanel.png CardStock.png Sparkle.png | diff - "$W/run/kit-sha-1.txt" && echo SAME
```

Expected: `SAME`。

- [ ] **Step 6: 运行测试**

Run: `bash $R EditMode p69-t4-green -testFilter Phase69UiKitTests`
Expected: `total=6 passed=6`（3 个 TestCase + 3 个 Test）。若像素断言失败，先用 Read 工具打开 PNG 看一眼，再查采样坐标；不要为了通过测试去放宽断言。

- [ ] **Step 7: 提交**

```bash
cd "$PROJECT"
git add Assets/Editor/Phase69UiKitGenerator.cs Assets/Editor/Phase69UiKitGenerator.cs.meta \
  Assets/Art/MidnightParlor/Sprites/GlassPanel.png Assets/Art/MidnightParlor/Sprites/GlassPanel.png.meta \
  Assets/Art/MidnightParlor/Sprites/CardStock.png Assets/Art/MidnightParlor/Sprites/CardStock.png.meta \
  Assets/Art/MidnightParlor/Sprites/Sparkle.png Assets/Art/MidnightParlor/Sprites/Sparkle.png.meta \
  Assets/Tests/EditMode/Phase69UiKitTests.cs Assets/Tests/EditMode/Phase69UiKitTests.cs.meta
git commit -m "feat(ui): generate glass, card-stock and sparkle sprites in C# (Phase 69)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: 占卜房换皮肤

**Files:**
- Create: `Assets/Editor/Phase69UiRestyleBootstrapper.cs`（本任务写通用工具和占卜房部分，Task 6 再补结果页和主菜单）
- Modify: `Assets/Scenes/ReadingRoom.unity`（运行 bootstrapper 生成）
- Modify: `Assets/Tests/EditMode/Phase39UiReskinTests.cs`
- Test: `Assets/Tests/EditMode/Phase69ReadingRoomRestyleTests.cs`、`Assets/Tests/PlayMode/Phase69RuntimeSkinTests.cs`

**Interfaces:**
- Consumes: Task 1–4 的全部接口；`Phase69UiKitGenerator.GlassPath/CardStockPath/SparklePath`。
- Produces:
  - `Phase69UiRestyleBootstrapper.Run()`（菜单 `Tools/Tarot Unity/Run Phase 69 UI Restyle Bootstrap`）、`RestyleReadingRoom()`；
  - 私有工具 `ScaleType`、`FitButton`、`Skin`、`EnsureImage`、`AddCornerStars`、`StretchInset`；
  - 场景中新增的子对象：
    - `Phase7_RitualHudRoot/Phase7_HudPlate/Phase69_Star_TL|BR`
    - `Phase7_RitualHudRoot/Phase69_StepDot_0..3`
    - `Phase11_ActionDock/Phase69_Star_TL|BR`
    - `QuestionInput/Phase69_InputUnderline`
    - `DrawButton|RevealResultButton/Phase69_Flank_L|R`
  - `ReadingRoomCanvas` 上挂 `UiTypeScale`。

- [ ] **Step 1: 写失败测试** `Assets/Tests/EditMode/Phase69ReadingRoomRestyleTests.cs`

```csharp
using System.Linq;
using NUnit.Framework;
using TMPro;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 69: the reading room in glass and card stock, sized by its text.</summary>
    public sealed class Phase69ReadingRoomRestyleTests
    {
        private static readonly string[] Chips =
        {
            "Phase7_Progress_ChooseSpread", "Phase7_Progress_AskQuestion", "Phase7_Progress_DrawCards",
            "Phase7_Progress_FlipCards", "Phase7_Progress_RevealResult",
        };

        private static readonly string[] Buttons =
        {
            "OneCardButton", "ThreeCardButton", "CelticCrossButton", "DrawButton", "RevealResultButton",
        };

        private Transform root;

        [SetUp]
        public void Open()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");
            root = GameObject.Find("ReadingRoomCanvas").transform;
        }

        [Test]
        public void ContainersAreGlassWithCornerStars()
        {
            foreach (var path in new[] { "Phase7_RitualHudRoot/Phase7_HudPlate", "Phase11_ActionDock" })
            {
                var image = root.Find(path).GetComponent<Image>();
                Assert.That(image.sprite?.name, Is.EqualTo("GlassPanel"), path);
                Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), path);
                Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(1f), path);
                foreach (var star in new[] { "Phase69_Star_TL", "Phase69_Star_BR" })
                {
                    var s = root.Find($"{path}/{star}")?.GetComponent<Image>();
                    Assert.That(s, Is.Not.Null, $"{path}/{star}");
                    Assert.That(s.sprite?.name, Is.EqualTo("Sparkle"));
                    Assert.That(s.raycastTarget, Is.False);
                }
            }
        }

        [Test]
        public void EveryButtonAndChipIsSkinnedAndItsTextFits()
        {
            foreach (var name in Buttons)
            {
                AssertFits(root.Find(name) as RectTransform, name);
            }

            foreach (var name in Chips)
            {
                AssertFits(root.Find($"Phase7_RitualHudRoot/{name}") as RectTransform, name);
            }
        }

        [Test]
        public void ChipPlatesSitBehindTheirLabels()
        {
            foreach (var name in Chips)
            {
                var chip = root.Find($"Phase7_RitualHudRoot/{name}");
                Assert.That(chip.Find("Plate").GetSiblingIndex(), Is.EqualTo(0), name);
            }
        }

        [Test]
        public void OnlyTheCurrentStepWearsCardStock()
        {
            var indicator = Object.FindFirstObjectByType<RitualStepIndicator>();
            foreach (var state in new[]
            {
                ReadingFlowState.SpreadSelect, ReadingFlowState.QuestionInput, ReadingFlowState.Drawing,
                ReadingFlowState.WaitingForFlip, ReadingFlowState.ResultReady,
            })
            {
                indicator.ApplyFlowState(state);
                var current = RitualStepIndicator.StepForState(state);
                for (var i = 0; i < Chips.Length; i++)
                {
                    var skin = root.Find($"Phase7_RitualHudRoot/{Chips[i]}").GetComponent<UiSkinState>();
                    Assert.That(skin.IsEmphasized, Is.EqualTo(i == current), $"{state}: {Chips[i]}");
                }
            }
        }

        [Test]
        public void SceneOpensWithOneCardChosenAndTheActionsInCardStock()
        {
            Assert.That(Skin("OneCardButton").IsEmphasized, Is.True);
            Assert.That(Skin("ThreeCardButton").IsEmphasized, Is.False);
            Assert.That(Skin("CelticCrossButton").IsEmphasized, Is.False);
            Assert.That(Skin("DrawButton").IsEmphasized, Is.True);
            Assert.That(Skin("RevealResultButton").IsEmphasized, Is.True);
            Assert.That(Skin("ThreeCardButton").Glass?.name, Is.EqualTo("GlassPanel"));
            Assert.That(Skin("OneCardButton").CardStock?.name, Is.EqualTo("CardStock"));

            var controller = Object.FindFirstObjectByType<ReadingRoomController>();
            controller.ApplySpreadEmphasis(10);
            Assert.That(Skin("CelticCrossButton").IsEmphasized, Is.True);
            Assert.That(Skin("OneCardButton").IsEmphasized, Is.False);
        }

        [Test]
        public void PrimaryActionsAreFlankedBySparkles()
        {
            foreach (var name in new[] { "DrawButton", "RevealResultButton" })
            {
                var left = root.Find($"{name}/Phase69_Flank_L") as RectTransform;
                var right = root.Find($"{name}/Phase69_Flank_R") as RectTransform;
                Assert.That(left, Is.Not.Null, name);
                Assert.That(right, Is.Not.Null, name);
                Assert.That(left.anchoredPosition.x, Is.EqualTo(-right.anchoredPosition.x).Within(0.01f));
                var button = root.Find(name) as RectTransform;
                Assert.That(right.anchoredPosition.x + right.rect.width / 2f,
                    Is.LessThanOrEqualTo(button.rect.width / 2f - UiFitLayout.SkinMargin), $"{name}: star inside the stock");
            }
        }

        [Test]
        public void QuestionIsAnUnderlineOverAClickableClearGround()
        {
            var input = root.Find("QuestionInput");
            var ground = input.GetComponent<Image>();
            Assert.That(ground.color.a, Is.EqualTo(0f));
            Assert.That(ground.raycastTarget, Is.True, "the field must stay clickable");
            Assert.That(input.GetComponent<TarotUiPreserveColor>(), Is.Not.Null);
            var line = input.Find("Phase69_InputUnderline") as RectTransform;
            Assert.That(line, Is.Not.Null);
            Assert.That(line.rect.height, Is.EqualTo(1f));
        }

        [Test]
        public void TheDockAndTheLinesBelowItDoNotOverlap()
        {
            var dock = Bounds("Phase11_ActionDock");
            var flow = Bounds("FlowStatusText");
            var release = Bounds("Phase10_ReleaseStatusText");
            Assert.That(dock.yMax, Is.EqualTo(UiFitLayout.DockTop).Within(0.01f));
            Assert.That(flow.yMax, Is.LessThanOrEqualTo(dock.yMin + 0.01f));
            Assert.That(release.yMax, Is.LessThanOrEqualTo(flow.yMin + 0.01f));
            Assert.That(release.yMin, Is.GreaterThanOrEqualTo(-UiFitLayout.CanvasHalfHeight));

            foreach (var name in Buttons.Concat(new[] { "QuestionInput" }))
            {
                var b = Bounds(name);
                Assert.That(b.yMin, Is.GreaterThanOrEqualTo(dock.yMin - 0.01f), name);
                Assert.That(b.yMax, Is.LessThanOrEqualTo(dock.yMax + 0.01f), name);
                Assert.That(b.xMin, Is.GreaterThanOrEqualTo(dock.xMin - 0.01f), name);
                Assert.That(b.xMax, Is.LessThanOrEqualTo(dock.xMax + 0.01f), name);
            }
        }

        [Test]
        public void TypeIsScaledOnceAndTheMutedLimitFollows()
        {
            Assert.That(root.GetComponent<UiTypeScale>().AppliedScale, Is.EqualTo(UiFitLayout.TypeScale));
            Assert.That(root.GetComponent<TarotUiTheme>().MutedSizeThreshold, Is.EqualTo(UiFitLayout.MutedSizeThresholdScaled));
            Assert.That(root.Find("OneCardButton/Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(19.5f));
            Assert.That(root.Find("DrawButton/Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(23f));
            Assert.That(root.Find("Phase7_RitualHudRoot/Phase7_Progress_DrawCards/Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(17.5f));
        }

        [Test]
        public void StepDotsSitBetweenTheChips()
        {
            for (var i = 0; i < 4; i++)
            {
                var dot = root.Find($"Phase7_RitualHudRoot/Phase69_StepDot_{i}") as RectTransform;
                Assert.That(dot, Is.Not.Null, $"dot {i}");
                var left = root.Find($"Phase7_RitualHudRoot/{Chips[i]}") as RectTransform;
                var right = root.Find($"Phase7_RitualHudRoot/{Chips[i + 1]}") as RectTransform;
                Assert.That(dot.anchoredPosition.x, Is.GreaterThan(left.anchoredPosition.x));
                Assert.That(dot.anchoredPosition.x, Is.LessThan(right.anchoredPosition.x));
                Assert.That(dot.GetComponent<Image>().raycastTarget, Is.False);
            }
        }

        private UiSkinState Skin(string name) => root.Find(name).GetComponent<UiSkinState>();

        private Rect Bounds(string name)
        {
            var rt = root.Find(name) as RectTransform;
            var p = rt.anchoredPosition;
            var s = rt.sizeDelta;
            return new Rect(p.x - s.x * rt.pivot.x, p.y - s.y * rt.pivot.y, s.x, s.y);
        }

        private static void AssertFits(RectTransform rect, string name)
        {
            Assert.That(rect, Is.Not.Null, name);
            Assert.That(rect.GetComponent<UiSkinState>(), Is.Not.Null, $"{name}: skinned");
            var label = rect.Find("Label").GetComponent<TMP_Text>();
            label.ForceMeshUpdate();
            var preferred = label.GetPreferredValues(label.text);
            var needed = UiFitLayout.FitSize(preferred, label.fontSize);
            Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(needed.x - 0.01f), $"{name}: width");
            Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(needed.y - 0.01f), $"{name}: height");
            Assert.That(label.rectTransform.rect.width, Is.GreaterThanOrEqualTo(preferred.x), $"{name}: label rect");
        }
    }
}
```

同时写 PlayMode 测试 `Assets/Tests/PlayMode/Phase69RuntimeSkinTests.cs`：

```csharp
using System.Collections;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 69: in play, TarotUiTheme.Awake must not tint the card stock, and the card stock
    /// follows the chosen spread even while the question is being written.
    /// </summary>
    public sealed class Phase69RuntimeSkinTests
    {
        [UnityTest]
        public IEnumerator CardStockSurvivesTheThemeAndFollowsTheChoice()
        {
            SceneManager.LoadScene("ReadingRoom");
            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }

            var canvas = GameObject.Find("ReadingRoomCanvas").transform;
            var one = canvas.Find("OneCardButton").GetComponent<Button>();
            var three = canvas.Find("ThreeCardButton").GetComponent<Button>();
            var oneSkin = one.GetComponent<UiSkinState>();
            var threeSkin = three.GetComponent<UiSkinState>();

            Assert.That(oneSkin.IsEmphasized, Is.True, "the room opens on one card");
            Assert.That(one.colors.normalColor, Is.EqualTo(Color.white), "the theme left the ColorBlock alone");
            Assert.That(((Image)one.targetGraphic).color, Is.EqualTo(Color.white));
            Assert.That(oneSkin.Label.color, Is.EqualTo(UiSkinState.CardStockLabel), "dark ink on card stock");
            Assert.That(threeSkin.Label.color, Is.EqualTo(UiSkinState.GlassLabel));

            three.onClick.Invoke();
            yield return null;
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.QuestionInput));
            Assert.That(threeSkin.IsEmphasized, Is.True);
            Assert.That(oneSkin.IsEmphasized, Is.False);

            one.onClick.Invoke();   // same state, another spread
            yield return null;
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.QuestionInput));
            Assert.That(oneSkin.IsEmphasized, Is.True);
            Assert.That(threeSkin.IsEmphasized, Is.False);

            var askChip = canvas.Find("Phase7_RitualHudRoot/Phase7_Progress_AskQuestion").GetComponent<UiSkinState>();
            Assert.That(askChip.IsEmphasized, Is.True, "the step bar is on 写问题");
        }
    }
}
```

- [ ] **Step 2: 运行，确认失败**

Run: `bash $R EditMode p69-t5-red -testFilter Phase69ReadingRoomRestyleTests`
Expected: 失败（场景里没有 GlassPanel 等）。

- [ ] **Step 3: 实现 bootstrapper 的通用部分和占卜房部分** `Assets/Editor/Phase69UiRestyleBootstrapper.cs`

```csharp
using System;
using System.Linq;
using TMPro;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 69 restyles the text UI of the reading room, the result page and the menu's
    /// start button: smoked glass for containers and unchosen options, ivory card stock for the
    /// current step, the chosen spread and the next action. Type grows 15%; every framed element
    /// is sized from its TMP preferred size, so no text touches its frame. The hierarchy is
    /// unchanged (tests and older bootstrappers find objects by path); only Phase69_* children
    /// are added. Runs after the Phase 39/40/60/63/67 bootstrappers - re-running any of those
    /// puts the old gold plaques back, so run this one again afterwards. Idempotent.
    /// </summary>
    public static class Phase69UiRestyleBootstrapper
    {
        public const string ReadingRoomPath = "Assets/Scenes/ReadingRoom.unity";
        public const string ResultPath = "Assets/Scenes/Result.unity";
        public const string MenuPath = "Assets/Scenes/MainMenu.unity";

        private static readonly Color CornerStar = new Color(232f / 255f, 184f / 255f, 90f / 255f, 1f);   // #e8b85a
        private static readonly Color DotStar = new Color(232f / 255f, 184f / 255f, 90f / 255f, 0.6f);
        private static readonly Color FlankStar = new Color(154f / 255f, 107f / 255f, 30f / 255f, 1f);    // #9a6b1e
        private static readonly Color Underline = new Color(219f / 255f, 161f / 255f, 61f / 255f, 0.6f);  // #dba13d
        private static readonly Color StepUpcoming = new Color(0.961f, 0.910f, 0.800f, 0.55f);
        private static readonly Color StepCompleted = new Color(232f / 255f, 196f / 255f, 122f / 255f, 1f); // #e8c47a

        private static Sprite glass;
        private static Sprite stock;
        private static Sprite sparkle;

        [MenuItem("Tools/Tarot Unity/Run Phase 69 UI Restyle Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            glass = AssetDatabase.LoadAssetAtPath<Sprite>(Phase69UiKitGenerator.GlassPath);
            stock = AssetDatabase.LoadAssetAtPath<Sprite>(Phase69UiKitGenerator.CardStockPath);
            sparkle = AssetDatabase.LoadAssetAtPath<Sprite>(Phase69UiKitGenerator.SparklePath);
            if (glass == null || stock == null || sparkle == null)
            {
                Debug.LogError("Phase 69: UI kit missing; run Tools/Tarot Unity/Generate Phase 69 UI Kit first.");
                return;
            }

            RestyleReadingRoom();
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 69 UI restyle complete.");
        }

        // ---------------------------------------------------------------- reading room

        private static readonly string[] ChipNames =
        {
            "Phase7_Progress_ChooseSpread", "Phase7_Progress_AskQuestion", "Phase7_Progress_DrawCards",
            "Phase7_Progress_FlipCards", "Phase7_Progress_RevealResult",
        };

        public static void RestyleReadingRoom()
        {
            var scene = EditorSceneManager.OpenScene(ReadingRoomPath, OpenSceneMode.Single);
            var root = GameObject.Find("ReadingRoomCanvas").transform;

            SetMutedThreshold(root, UiFitLayout.MutedSizeThresholdScaled);
            ScaleType(root, root.gameObject, _ => true);

            BuildStepBar(root);
            BuildDock(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildStepBar(Transform root)
        {
            var hud = (RectTransform)root.Find("Phase7_RitualHudRoot");
            var widths = new float[ChipNames.Length];
            var heights = new float[ChipNames.Length];
            var chips = new RectTransform[ChipNames.Length];
            for (var i = 0; i < ChipNames.Length; i++)
            {
                var chip = (RectTransform)hud.Find(ChipNames[i]);
                var plate = chip.Find("Plate").GetComponent<Image>();
                var label = chip.Find("Label").GetComponent<TMP_Text>();
                plate.transform.SetAsFirstSibling();   // two chips held their Label first; card stock would hide it
                Stretch((RectTransform)plate.transform, 0f);

                var size = FitLabel(chip, label);
                chips[i] = chip;
                widths[i] = size.x;
                heights[i] = size.y;

                var skin = GetOrAdd<UiSkinState>(chip.gameObject);
                skin.Configure(plate, label, null, stock, false);
                EditorUtility.SetDirty(skin);
            }

            var centers = UiFitLayout.RowCenters(widths, UiFitLayout.StepGap);
            for (var i = 0; i < chips.Length; i++)
            {
                chips[i].anchoredPosition = new Vector2(centers[i], 0f);
                EditorUtility.SetDirty(chips[i]);
            }

            for (var i = 0; i < chips.Length - 1; i++)
            {
                var x = (centers[i] + widths[i] / 2f + centers[i + 1] - widths[i + 1] / 2f) / 2f;
                var dot = EnsureImage(hud, $"Phase69_StepDot_{i}", sparkle, DotStar, Vector2.one * UiFitLayout.StepDotSize);
                dot.anchoredPosition = new Vector2(x, 0f);
            }

            var rowWidth = widths.Sum() + UiFitLayout.StepGap * (widths.Length - 1);
            var plateSize = new Vector2(
                rowWidth + 2f * UiFitLayout.HudPad.x + 2f * UiFitLayout.SkinMargin,
                heights.Max() + 2f * UiFitLayout.HudPad.y + 2f * UiFitLayout.SkinMargin);
            hud.sizeDelta = plateSize;
            var hudPlate = (RectTransform)hud.Find("Phase7_HudPlate");
            hudPlate.anchoredPosition = Vector2.zero;
            hudPlate.sizeDelta = plateSize;
            Skin(hudPlate.GetComponent<Image>(), glass);
            AddCornerStars(hudPlate);
            EditorUtility.SetDirty(hud);

            var indicator = UnityEngine.Object.FindFirstObjectByType<RitualStepIndicator>();
            var so = new SerializedObject(indicator);
            so.FindProperty("upcomingLabel").colorValue = StepUpcoming;
            so.FindProperty("completedLabel").colorValue = StepCompleted;
            so.FindProperty("currentLabel").colorValue = UiSkinState.CardStockLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
            indicator.SetStep(0);   // SpreadSelect: 选牌阵 is current in the saved scene
        }

        private static void BuildDock(Transform root)
        {
            var spread = new[] { "OneCardButton", "ThreeCardButton", "CelticCrossButton" };
            var actions = new[] { "DrawButton", "RevealResultButton" };
            var names = spread.Concat(actions).ToArray();
            var rects = new RectTransform[names.Length];
            var widths = new float[names.Length];
            var rowHeight = 0f;
            for (var i = 0; i < names.Length; i++)
            {
                rects[i] = (RectTransform)root.Find(names[i]);
                var label = rects[i].Find("Label").GetComponent<TMP_Text>();
                var size = FitLabel(rects[i], label);
                widths[i] = size.x;
                rowHeight = Mathf.Max(rowHeight, size.y);

                var skin = GetOrAdd<UiSkinState>(rects[i].gameObject);
                skin.Configure(rects[i].GetComponent<Image>(), label, glass, stock, true);
                skin.SetEmphasis(names[i] == "OneCardButton" || Array.IndexOf(actions, names[i]) >= 0);
                EditorUtility.SetDirty(skin);
                EditorUtility.SetDirty(rects[i].GetComponent<Button>());
            }

            foreach (var name in actions)
            {
                AddFlanks((RectTransform)root.Find(name));
            }

            // The question: an underline over a clear ground that still takes clicks.
            var input = (RectTransform)root.Find("QuestionInput");
            var field = input.GetComponent<TMP_InputField>();
            var placeholder = (TMP_Text)field.placeholder;
            placeholder.ForceMeshUpdate();
            var inputPreferred = placeholder.GetPreferredValues(placeholder.text);
            var inputHeight = Mathf.Ceil(inputPreferred.y) + 2f * UiFitLayout.Padding(placeholder.fontSize).y;
            var ground = input.GetComponent<Image>();
            ground.sprite = null;
            ground.color = new Color(1f, 1f, 1f, 0f);
            ground.raycastTarget = true;
            GetOrAdd<TarotUiPreserveColor>(input.gameObject);
            var line = EnsureImage(input, "Phase69_InputUnderline", null, Underline, new Vector2(0f, 1f));
            line.anchorMin = new Vector2(0f, 0f);
            line.anchorMax = new Vector2(1f, 0f);
            line.pivot = new Vector2(0.5f, 0f);
            line.anchoredPosition = Vector2.zero;
            line.sizeDelta = new Vector2(0f, 1f);

            var rowWidth = widths.Sum() + UiFitLayout.RowGap * (widths.Length - 1);
            var dockWidth = rowWidth + 2f * UiFitLayout.DockPad.x + 2f * UiFitLayout.SkinMargin;
            var dockHeight = 2f * UiFitLayout.SkinMargin + 2f * UiFitLayout.DockPad.y
                             + inputHeight + UiFitLayout.DockRowGap + rowHeight;
            var top = UiFitLayout.DockTop;
            var dock = (RectTransform)root.Find("Phase11_ActionDock");
            dock.sizeDelta = new Vector2(dockWidth, dockHeight);
            dock.anchoredPosition = new Vector2(0f, top - dockHeight / 2f);
            Skin(dock.GetComponent<Image>(), glass);
            AddCornerStars(dock);

            var inputY = top - UiFitLayout.SkinMargin - UiFitLayout.DockPad.y - inputHeight / 2f;
            input.sizeDelta = new Vector2(Mathf.Round(UiFitLayout.InputWidthRatio * dockWidth), inputHeight);
            input.anchoredPosition = new Vector2(0f, inputY);
            EditorUtility.SetDirty(input);

            var rowY = top - UiFitLayout.SkinMargin - UiFitLayout.DockPad.y - inputHeight
                       - UiFitLayout.DockRowGap - rowHeight / 2f;
            var centers = UiFitLayout.RowCenters(widths, UiFitLayout.RowGap);
            for (var i = 0; i < rects.Length; i++)
            {
                rects[i].anchoredPosition = new Vector2(centers[i], rowY);
                EditorUtility.SetDirty(rects[i]);
            }

            // The two status lines stack under the dock.
            var below = top - dockHeight - UiFitLayout.BelowDockGap;
            foreach (var name in new[] { "FlowStatusText", "Phase10_ReleaseStatusText" })
            {
                var rt = (RectTransform)root.Find(name);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, below - rt.sizeDelta.y / 2f);
                below -= rt.sizeDelta.y + UiFitLayout.BelowDockGap;
                EditorUtility.SetDirty(rt);
            }
        }

        // ---------------------------------------------------------------- shared tools

        /// <summary>Sizes a framed element to its label and stretches the label inside the skin margin.</summary>
        private static Vector2 FitLabel(RectTransform frame, TMP_Text label)
        {
            label.alignment = TextAlignmentOptions.Center;
            label.ForceMeshUpdate();
            var size = UiFitLayout.FitSize(label.GetPreferredValues(label.text), label.fontSize);
            frame.sizeDelta = size;
            Stretch(label.rectTransform, UiFitLayout.SkinMargin);
            EditorUtility.SetDirty(label);
            EditorUtility.SetDirty(frame);
            return size;
        }

        /// <summary>Like FitLabel, but never shrinks a standalone button below its authored size.</summary>
        private static void FitLabelAtLeast(RectTransform frame, TMP_Text label)
        {
            var authored = frame.sizeDelta;
            var size = FitLabel(frame, label);
            frame.sizeDelta = Vector2.Max(authored, size);
        }

        /// <summary>Scales every included TMP text under <paramref name="scope"/> once, recorded on the marker.</summary>
        private static void ScaleType(Transform scope, GameObject markerHost, Func<TMP_Text, bool> include)
        {
            var marker = GetOrAdd<UiTypeScale>(markerHost);
            var factor = UiFitLayout.TypeScale / marker.AppliedScale;
            foreach (var text in scope.GetComponentsInChildren<TMP_Text>(true).Where(include))
            {
                text.fontSize = UiFitLayout.ScaledFontSize(text.fontSize, factor);
                if (text.enableAutoSizing)
                {
                    text.fontSizeMin = UiFitLayout.ScaledFontSize(text.fontSizeMin, factor);
                    text.fontSizeMax = UiFitLayout.ScaledFontSize(text.fontSizeMax, factor);
                }

                EditorUtility.SetDirty(text);
            }

            marker.AppliedScale = UiFitLayout.TypeScale;
            EditorUtility.SetDirty(marker);
        }

        private static void SetMutedThreshold(Transform canvas, float value)
        {
            var theme = canvas.GetComponent<TarotUiTheme>();
            var so = new SerializedObject(theme);
            so.FindProperty("mutedSizeThreshold").floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Skin(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
            EditorUtility.SetDirty(image);
        }

        private static void AddCornerStars(RectTransform panel)
        {
            var size = Vector2.one * UiFitLayout.CornerStarSize;
            var m = UiFitLayout.SkinMargin;
            var tl = EnsureImage(panel, "Phase69_Star_TL", sparkle, CornerStar, size);
            tl.anchorMin = tl.anchorMax = new Vector2(0f, 1f);
            tl.anchoredPosition = new Vector2(m, -m);
            var br = EnsureImage(panel, "Phase69_Star_BR", sparkle, CornerStar, size);
            br.anchorMin = br.anchorMax = new Vector2(1f, 0f);
            br.anchoredPosition = new Vector2(-m, m);
        }

        private static void AddFlanks(RectTransform button)
        {
            var label = button.Find("Label").GetComponent<TMP_Text>();
            label.ForceMeshUpdate();
            var textWidth = label.GetPreferredValues(label.text).x;
            var star = Mathf.Round(UiFitLayout.FlankStarPerFont * label.fontSize);
            var x = textWidth / 2f + UiFitLayout.FlankGapPerFont * label.fontSize + star / 2f;
            EnsureImage(button, "Phase69_Flank_L", sparkle, FlankStar, Vector2.one * star).anchoredPosition = new Vector2(-x, 0f);
            EnsureImage(button, "Phase69_Flank_R", sparkle, FlankStar, Vector2.one * star).anchoredPosition = new Vector2(x, 0f);
        }

        private static RectTransform EnsureImage(Transform parent, string name, Sprite sprite, Color color, Vector2 size)
        {
            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            if (existing == null)
            {
                rt.SetParent(parent, false);
            }

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            EditorUtility.SetDirty(go);
            return rt;
        }

        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            EditorUtility.SetDirty(rt);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
```

（`FitLabelAtLeast` 留给 Task 6 使用。）

- [ ] **Step 4: 批处理运行 bootstrapper**

```bash
"$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase69UiRestyleBootstrapper.Run -quit -logFile "$W/run/p69-rr-1.log"; echo exit=$?
grep -n "Phase 69\|error\|Exception" "$W/run/p69-rr-1.log" | head
```

Expected: `exit=0`，日志里有 `Tarot Unity Phase 69 UI restyle complete.`，没有异常。

- [ ] **Step 5: 更新 Phase 39 旧断言**

`Assets/Tests/EditMode/Phase39UiReskinTests.cs`：

`ChromeWearsTheNineSlicePlaques` 的数组改成两项，并在方法开头加注释：

```csharp
            // Phase 69: the gold plaques became smoked glass; the question field is an underline (see Phase69ReadingRoomRestyleTests).
            foreach (var (path, sprite) in new[]
            {
                ("Phase7_RitualHudRoot/Phase7_HudPlate", "GlassPanel"),
                ("Phase11_ActionDock", "GlassPanel"),
            })
```

`ButtonsAreGoldPlaquesWithCleanColorBlocks` 里，把 `Assert.That(image.sprite?.name, Is.EqualTo("TarotButton"), name);` 换成下面两行（Unity 自带的 NUnit 版本较旧，不用 `Is.AnyOf`）：

```csharp
                // Phase 69: glass or card stock, by selection.
                Assert.That(new[] { "GlassPanel", "CardStock" }, Does.Contain(image.sprite?.name), name);
```

`ProgressPlatesShareTheSubtlePanel` 改名为 `ProgressPlatesAreCardStockOnlyWhenCurrent`，循环体改成：

```csharp
                var chip = root.Find($"Phase7_RitualHudRoot/{step}");
                var skin = chip.GetComponent<UiSkinState>();
                Assert.That(skin, Is.Not.Null, step);
                Assert.That(skin.CardStock?.name, Is.EqualTo("CardStock"), step);
                Assert.That(skin.Glass, Is.Null, $"{step}: no plate unless current");
```

在文件头加 `using TarotUnity.UI;`。

- [ ] **Step 6: 运行占卜房相关测试**

```bash
bash $R EditMode p69-t5-edit -testFilter "Phase69ReadingRoomRestyleTests|Phase39UiReskinTests|Phase61ReadingRoomSlotStepTests|Phase63SpreadDefinitionTests|Phase38TableRebuildTests"
bash $R PlayMode p69-t5-play -testFilter Phase69RuntimeSkinTests
```

Expected: 全部通过。
- 如果防穿模断言失败：先看日志里差多少，再查 `FitLabel` 是否在字号放大之后调用（`ScaleType` 必须先于 `BuildStepBar`/`BuildDock`）。不要去改断言。
- 如果下方状态文字越界：把 `UiFitLayout.DockTop` 往上调（例如 -116），并在台账里记下实际数值。

- [ ] **Step 7: 提交**

```bash
git add Assets/Editor/Phase69UiRestyleBootstrapper.cs Assets/Editor/Phase69UiRestyleBootstrapper.cs.meta \
  Assets/Scenes/ReadingRoom.unity Assets/Tests/EditMode/Phase39UiReskinTests.cs \
  Assets/Tests/EditMode/Phase69ReadingRoomRestyleTests.cs Assets/Tests/EditMode/Phase69ReadingRoomRestyleTests.cs.meta \
  Assets/Tests/PlayMode/Phase69RuntimeSkinTests.cs Assets/Tests/PlayMode/Phase69RuntimeSkinTests.cs.meta
git commit -m "feat(ui): reading room in glass and card stock, sized by its text (Phase 69)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: 结果页与主菜单

**Files:**
- Modify: `Assets/Editor/Phase69UiRestyleBootstrapper.cs`（加 `RestyleResult`、`RestyleMenu`，`Run` 里依次调用）
- Modify: `Assets/Editor/Phase67ResultReadingCaptureBuilder.cs:27`（`FrameInnerLineUnits`）
- Modify: `Assets/Scenes/Result.unity`、`Assets/Scenes/MainMenu.unity`（bootstrapper 生成）
- Modify: `Assets/Tests/EditMode/Phase40MenuResultTests.cs`、`Assets/Tests/EditMode/Phase67ResultSceneStructureTests.cs`
- Test: `Assets/Tests/EditMode/Phase69ResultMenuRestyleTests.cs`

**Interfaces:**
- Consumes: Task 5 的工具方法（`ScaleType`、`FitLabel`、`FitLabelAtLeast`、`Skin`、`GetOrAdd`、`SetMutedThreshold`）。
- Produces:
  - `RestyleResult()`、`RestyleMenu()`；
  - `ResultCanvas` 和 `StartReadingButton` 上挂 `UiTypeScale`；
  - 结果页四个按钮带 `UiSkinState`：`BackToMenuButton`、`Phase66_RetryInterpretationButton` 为卡纸；`Phase66_OfflineInterpretationButton` 为玻璃。

- [ ] **Step 1: 写失败测试** `Assets/Tests/EditMode/Phase69ResultMenuRestyleTests.cs`

```csharp
using NUnit.Framework;
using TMPro;
using TarotUnity.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 69: the result page and the menu's start button.</summary>
    public sealed class Phase69ResultMenuRestyleTests
    {
        [Test]
        public void ResultPanelsAreGlass()
        {
            var root = OpenResult();
            foreach (var path in new[] { "ResultReadingScroll", "Phase12_ResultCardShowcase" })
            {
                var image = root.Find(path).GetComponent<Image>();
                Assert.That(image.sprite?.name, Is.EqualTo("GlassPanel"), path);
                Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(1f), path);
            }

            var band = root.Find("MP_ResultSpreadBand");
            for (var i = 0; i < band.childCount; i++)
            {
                var frame = band.GetChild(i).Find("ReversePivot/Frame")?.GetComponent<Image>();
                Assert.That(frame, Is.Not.Null, $"cell {i}");
                Assert.That(frame.sprite?.name, Is.EqualTo("GlassPanel"), $"cell {i}");
            }
        }

        [TestCase("BackToMenuButton", true)]
        [TestCase("Phase66_RetryInterpretationButton", true)]
        [TestCase("Phase66_OfflineInterpretationButton", false)]
        public void ResultButtonsAreSkinnedAndFit(string name, bool cardStock)
        {
            var root = OpenResult();
            var rect = root.Find(name) as RectTransform;
            var skin = rect.GetComponent<UiSkinState>();
            Assert.That(skin, Is.Not.Null, name);
            Assert.That(skin.IsEmphasized, Is.EqualTo(cardStock), name);
            AssertFits(rect, name);
        }

        [Test]
        public void ResultTypeIsScaledButTheCardCaptionsAreNot()
        {
            var root = OpenResult();
            Assert.That(root.GetComponent<UiTypeScale>().AppliedScale, Is.EqualTo(UiFitLayout.TypeScale));
            Assert.That(root.GetComponent<TarotUiTheme>().MutedSizeThreshold, Is.EqualTo(UiFitLayout.MutedSizeThresholdScaled));
            Assert.That(root.Find("BackToMenuButton/Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(19.5f));
            var band = root.Find("MP_ResultSpreadBand");
            for (var i = 0; i < band.childCount; i++)
            {
                var label = band.GetChild(i).Find("Label").GetComponent<TMP_Text>();
                Assert.That(label.fontSize, Is.EqualTo(22f), "the presenter sizes card captions at runtime (Phase 60 default 22)");
            }
        }

        [Test]
        public void MenuStartButtonIsCardStockAndOnlyItsTextGrew()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var root = GameObject.Find("MainMenuCanvas").transform;
            var start = root.Find("StartReadingButton") as RectTransform;
            var skin = start.GetComponent<UiSkinState>();
            Assert.That(skin, Is.Not.Null);
            Assert.That(skin.IsEmphasized, Is.True);
            Assert.That(start.GetComponent<Image>().sprite?.name, Is.EqualTo("CardStock"));
            Assert.That(start.GetComponent<UiTypeScale>().AppliedScale, Is.EqualTo(UiFitLayout.TypeScale));
            Assert.That(start.Find("Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(23f));
            Assert.That(root.Find("QuitButton/Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(18f), "离席 unchanged");
            Assert.That(root.GetComponent<TarotUiTheme>().MutedSizeThreshold, Is.EqualTo(16f));
            Assert.That(start.sizeDelta.x, Is.GreaterThanOrEqualTo(308f), "the invitation keeps its authored width");
            AssertFits(start, "StartReadingButton");
        }

        private static Transform OpenResult()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");
            return GameObject.Find("ResultCanvas").transform;
        }

        private static void AssertFits(RectTransform rect, string name)
        {
            var label = rect.Find("Label").GetComponent<TMP_Text>();
            label.ForceMeshUpdate();
            var preferred = label.GetPreferredValues(label.text);
            var needed = UiFitLayout.FitSize(preferred, label.fontSize);
            Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(needed.x - 0.01f), $"{name}: width");
            Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(needed.y - 0.01f), $"{name}: height");
        }
    }
}
```

- [ ] **Step 2: 运行，确认失败**

Run: `bash $R EditMode p69-t6-red -testFilter Phase69ResultMenuRestyleTests`
Expected: 失败。

- [ ] **Step 3: 在 bootstrapper 里加结果页和主菜单**

`Run()` 里把 `RestyleReadingRoom();` 改成：

```csharp
            RestyleReadingRoom();
            RestyleResult();
            RestyleMenu();
```

在「shared tools」注释之前加：

```csharp
        // ---------------------------------------------------------------- result

        public static void RestyleResult()
        {
            var scene = EditorSceneManager.OpenScene(ResultPath, OpenSceneMode.Single);
            var root = GameObject.Find("ResultCanvas").transform;

            SetMutedThreshold(root, UiFitLayout.MutedSizeThresholdScaled);
            // Card captions under the spread band are sized by ResultPanelPresenter at runtime.
            ScaleType(root, root.gameObject, t => !t.transform.GetComponentsInParent<Transform>(true)
                .Any(p => p.name.StartsWith("SpreadCell_", StringComparison.Ordinal)));

            Skin(root.Find("ResultReadingScroll").GetComponent<Image>(), glass);
            Skin(root.Find("Phase12_ResultCardShowcase").GetComponent<Image>(), glass);
            var band = root.Find("MP_ResultSpreadBand");
            for (var i = 0; i < band.childCount; i++)
            {
                var frame = band.GetChild(i).Find("ReversePivot/Frame")?.GetComponent<Image>();
                if (frame != null)
                {
                    Skin(frame, glass);
                }
            }

            foreach (var (name, emphasized) in new[]
            {
                ("BackToMenuButton", true),
                ("Phase66_RetryInterpretationButton", true),
                ("Phase66_OfflineInterpretationButton", false),
            })
            {
                SkinStandaloneButton((RectTransform)root.Find(name), emphasized);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ---------------------------------------------------------------- menu

        public static void RestyleMenu()
        {
            var scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            var root = GameObject.Find("MainMenuCanvas").transform;
            var start = (RectTransform)root.Find("StartReadingButton");
            ScaleType(start, start.gameObject, _ => true);   // only the invitation's text grows
            SkinStandaloneButton(start, true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SkinStandaloneButton(RectTransform button, bool emphasized)
        {
            var label = button.Find("Label").GetComponent<TMP_Text>();
            FitLabelAtLeast(button, label);
            var skin = GetOrAdd<UiSkinState>(button.gameObject);
            skin.Configure(button.GetComponent<Image>(), label, glass, stock, true);
            skin.SetEmphasis(emphasized);
            EditorUtility.SetDirty(skin);
            EditorUtility.SetDirty(button.GetComponent<Button>());
        }
```

- [ ] **Step 4: 改截图脚本常量**

`Assets/Editor/Phase67ResultReadingCaptureBuilder.cs` 第 27 行改成：

```csharp
        private const float FrameInnerLineUnits = 14f; // Phase 69 GlassPanel: 8 px margin + inner line centred 5.5 px in, at multiplier 1
```

- [ ] **Step 5: 更新旧断言**

`Phase40MenuResultTests.MenuButtonsWearTheKit`：把 `Is.EqualTo("TarotButton")` 改成 `Is.EqualTo("CardStock")`，上方加注释 `// Phase 69: the invitation is ivory card stock.`。

`Phase40MenuResultTests.ResultHasParlorStageAndGoldChrome`：
- 两处 `Is.EqualTo("TarotPanel")` 改成 `Is.EqualTo("GlassPanel")`；
- `BackToMenuButton` 的 `Is.EqualTo("TarotButton")` 改成 `Is.EqualTo("CardStock")`；
- 在第一处上方加注释 `// Phase 69: gold plaques became glass; the way back is card stock.`。

`Phase67ResultSceneStructureTests`：
- `FrameInnerGoldLine` 改成 `14f`，注释改为 `// Phase 69 GlassPanel inner line at pixelsPerUnitMultiplier 1`；
- `ViewportClearsTheFrameBorder` 里，control 断言改成 `Is.EqualTo("GlassPanel")` 和 `pixelsPerUnitMultiplier` 等于 `1f`。

- [ ] **Step 6: 重新生成三个场景，并检查两次运行结果完全一致（幂等）**

```bash
cd "$PROJECT"
"$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase69UiRestyleBootstrapper.Run -quit -logFile "$W/run/p69-all-1.log"; echo exit=$?
shasum Assets/Scenes/ReadingRoom.unity Assets/Scenes/Result.unity Assets/Scenes/MainMenu.unity > "$W/run/scene-sha-1.txt"
"$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase69UiRestyleBootstrapper.Run -quit -logFile "$W/run/p69-all-2.log"; echo exit=$?
shasum Assets/Scenes/ReadingRoom.unity Assets/Scenes/Result.unity Assets/Scenes/MainMenu.unity | diff - "$W/run/scene-sha-1.txt" && echo IDEMPOTENT
```

Expected: 两次都是 `exit=0`，最后输出 `IDEMPOTENT`。如果不一致，用 `git diff --word-diff` 找出变化的字段（常见原因是浮点尾数，或者对象 ID 在重建）；修好 bootstrapper 后，从这一步重新开始。

- [ ] **Step 7: 运行相关测试**

Run: `bash $R EditMode p69-t6-edit -testFilter "Phase69|Phase40MenuResultTests|Phase67|Phase62|Phase60|Phase66|Phase35UiPolishTests|Phase45MenuDepthTests"`
Expected: 全部通过。
- 如果出现本计划没有列出的旧断言失败：先判断它钉住的是不是本期有意改变的东西（贴图名、字号、按钮或面板矩形）。
- 是：改成新规格，并在台账 `$W/progress.md` 里记下测试名、旧值、新值和原因。
- 否：说明是真 bug，修代码，不改断言。

- [ ] **Step 8: 提交**

```bash
git add Assets/Editor/Phase69UiRestyleBootstrapper.cs Assets/Editor/Phase67ResultReadingCaptureBuilder.cs \
  Assets/Scenes/ReadingRoom.unity Assets/Scenes/Result.unity Assets/Scenes/MainMenu.unity \
  Assets/Tests/EditMode/Phase40MenuResultTests.cs Assets/Tests/EditMode/Phase67ResultSceneStructureTests.cs \
  Assets/Tests/EditMode/Phase69ResultMenuRestyleTests.cs Assets/Tests/EditMode/Phase69ResultMenuRestyleTests.cs.meta
git commit -m "feat(ui): result page and menu invitation in glass and card stock (Phase 69)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

（Step 5 若在台账之外还改了别的旧测试文件，把它们一并加进这次提交。）

---

### Task 7: 截图、文档、全量回归

**Files:**
- Create: `Assets/Editor/Phase69MenuCaptureBuilder.cs`
- Create: `Docs/PHASE69_UI_GLASS_CARDSTOCK.md`

**Interfaces:**
- Consumes: `CaptureRig.RenderConverged(Camera)`；`PHASE68_CAPTURE_DIR`、`PHASE67_CAPTURE_DIR` 两个环境变量。
- Produces: `Phase69MenuCaptureBuilder.Run()`，读取环境变量 `PHASE69_CAPTURE_DIR`（必填）。

- [ ] **Step 1: 写主菜单截图脚本** `Assets/Editor/Phase69MenuCaptureBuilder.cs`

```csharp
using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 69: renders the main menu (its start button is now card stock) to
    /// $PHASE69_CAPTURE_DIR/MainMenu.png for review. Same canvas-to-camera treatment as the
    /// Phase 64 capture; nothing is written into the repo.
    /// </summary>
    public static class Phase69MenuCaptureBuilder
    {
        private const int W = 2560, H = 1440;

        public static void Run()
        {
            var dir = Environment.GetEnvironmentVariable("PHASE69_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir))
            {
                throw new InvalidOperationException("Set PHASE69_CAPTURE_DIR.");
            }

            Directory.CreateDirectory(dir);
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = camera;
                c.planeDistance = 1f;
                c.pixelPerfect = false;
            }

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            camera.aspect = (float)W / H;
            camera.targetTexture = rt;
            RenderTexture.active = rt;
            Canvas.ForceUpdateCanvases();
            CaptureRig.RenderConverged(camera);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, "MainMenu.png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log($"Phase 69 menu capture -> {dir}");
        }
    }
}
```

（场景没有保存，改过的 renderMode 会随进程退出而丢弃。）

- [ ] **Step 2: 出三套截图，写到会话临时目录（不入库）**

```bash
OUT=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/790b0dfd-6ecb-40b1-a435-0828f97da096/scratchpad/p69-captures
mkdir -p "$OUT"/{room,result,menu}
PHASE68_CAPTURE_DIR="$OUT/room" "$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase68ParlorCaptureBuilder.Run -quit -logFile "$OUT/room.log"; echo room=$?
PHASE67_CAPTURE_DIR="$OUT/result" "$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase67ResultReadingCaptureBuilder.Run -quit -logFile "$OUT/result.log"; echo result=$?
PHASE69_CAPTURE_DIR="$OUT/menu" "$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase69MenuCaptureBuilder.Run -quit -logFile "$OUT/menu.log"; echo menu=$?
ls "$OUT"/*/
```

Expected: 三个 `=0`，并且各目录下都有 PNG。

- [ ] **Step 3: 自己先用 Read 工具看截图**

逐张打开这几张截图：`room/ReadingRoom_default.png`、`room/ReadingRoom_threeCard.png`、`result/Result_3card_16x9.png`、`result/Result_10card_16x9.png`、`result/Result_failed.png`、`menu/MainMenu.png`。对照 spec 第 6 节的五条逐条检查：

- 是否还有粗金框；
- 卡纸是否只出现在规定的位置；
- 有没有文字碰到框；
- 小星是否显示正常（不是方块）；
- 下方两行状态文字是否完整。

发现问题先修 bootstrapper 或常量，再从 Task 6 Step 6 重跑。截图交给用户看，是否入库由用户决定。

- [ ] **Step 4: 写本期说明** `Docs/PHASE69_UI_GLASS_CARDSTOCK.md`

内容（中文，参照 `Docs/PHASE68_READING_ROOM_TABLE.md` 的写法）：
- 为什么改：金框土、框中套框、选中牌阵没有反馈；
- 两种材质的规格和用途表（照 spec 第 3、4 节）；
- 运行时主题的问题：编辑模式截图和真实游戏不一致，以及现在的处理方式；
- 字体里没有 ✦ ✧，改用 Sparkle 贴图；
- 排布规则和 `UiFitLayout` 的常量（写最终值；如果 Task 5 调过 `DockTop`，写调整后的值）；
- 运行顺序：先 Generate Phase 69 UI Kit，再 Run Phase 69 UI Restyle Bootstrap；重跑 Phase 39、40、60、63、67 任一 bootstrapper 后，都必须再跑一遍 Phase 69；
- 截图方法（三个环境变量）；
- 没做的事：
  - 结果页牌下的位置标签字号没有放大；
  - 「离席」按钮没动；
  - 旧贴图文件保留。

- [ ] **Step 5: 全量回归**

```bash
bash $R EditMode p69-final-edit && bash $R PlayMode p69-final-play
bash $PT tests | tail -1
```

Expected:
- EditMode 全部通过：总数 = 456 + 本期新增，失败 0；
- PlayMode `passed = 73`（原 72 + 本期 1），`skipped = 3`；
- 后端 `190 passed`。

- [ ] **Step 6: 提交**

```bash
git add Assets/Editor/Phase69MenuCaptureBuilder.cs Assets/Editor/Phase69MenuCaptureBuilder.cs.meta Docs/PHASE69_UI_GLASS_CARDSTOCK.md
git commit -m "docs(ui): Phase 69 glass and card-stock notes, menu capture

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git status --short   # 应只剩两个字体图集文件
```

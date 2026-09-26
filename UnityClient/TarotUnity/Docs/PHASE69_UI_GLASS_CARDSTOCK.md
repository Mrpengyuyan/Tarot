# Phase 69 — 文字 UI：玻璃为底，卡纸点睛

设计：`docs/superpowers/specs/2026-09-26-ui-glass-cardstock-design.md`（仓库根目录）。

## 为什么改

- Phase 37 的金色粗框（`TarotPanel` / `TarotPanelSubtle` / `TarotButton`）显得土，和塔罗牌背景割裂。
- 步骤条和底部操作栏都是框中套框。
- 选中的牌阵按钮没有任何视觉反馈。
- 文字偏小。

## 两种材质

| 材质 | 贴图 | 用在哪里 | 文字颜色 |
|---|---|---|---|
| 烟色玻璃 | `Sprites/GlassPanel.png`：底 `rgb(14,6,14)` α0.52、1 px 金线 `#dba13d` α0.55、往里 5 px 一圈 α0.22 的淡内线、圆角 4 | 步骤条底板、操作栏、结果页解读区和牌框、未选中的牌阵按钮、「查看离线解读」 | `#f5e8cc` α0.85 |
| 象牙卡纸 | `Sprites/CardStock.png`：`#f2e7cb` → `#e6d6b0` 竖向渐变、距边 3 px 的 `#3a2a20` 细边、外圈柔和投影 | 当前步骤、选中的牌阵、洗牌抽取、揭示结果、重新解读、回到牌桌、入席问牌 | `#2a1d17` |

- 两张贴图都是 96 px，外圈留 8 px（玻璃透明、卡纸是投影），九宫格边界 32。所以同一个矩形换成另一种材质时，看得见的大小不变。
- 小星统一用 `Sprites/Sparkle.png`（64 px 四角星），因为霞鹜文楷两个字重都没有 ✦ / ✧（U+2726 / U+2727）。三种用法：
  - 容器对角：18 单位，`#e8b85a`；
  - 步骤之间：12 单位，同色 α0.6；
  - 主按钮文字两侧：0.6 倍字号，`#9a6b1e`。
- 三张图都由 `Editor/Phase69UiKitGenerator.cs` 用 C# 画出，不依赖 Python / PIL。输出逐字节确定，内容没变时不会重写文件。

## 运行时主题

三个场景的画布上都挂着 `TarotUiTheme`。它在 `Awake` 时会把所有按钮染成深紫色、按字号给文字上色，还会给输入框底图上深色。编辑模式下渲染的评审截图不会运行 `Awake`，所以以前的截图和真实游戏里看到的不一样。

现在的处理：

- 带 `UiSkinState` 的按钮，主题不再改它的 ColorBlock；这些按钮里的文字只换字体、不改颜色。
- 带 `TarotUiPreserveColor` 的输入框，主题不再改底图颜色。
- 「字号 ≤ 阈值时显示为灰色」的阈值改成序列化字段 `mutedSizeThreshold`：占卜房、结果页设为 18.5，主菜单保持 16。

`Tests/PlayMode/Phase69RuntimeSkinTests` 在真实运行中验证卡纸没有被主题染色。

## 状态切换

`Scripts/UI/UiSkinState.cs` 挂在每个换过皮肤的按钮和步骤小牌上，`SetEmphasis(bool)` 在玻璃和卡纸之间切换。

- `RitualStepIndicator`：当前步骤显示卡纸，其余步骤不画底板，只靠文字颜色区分：未到 `#f5e8cc` α0.55，已完成 `#e8c47a`。
- `ReadingRoomController.ApplySpreadEmphasis(cardCount)`：每次选择牌阵都会调用，包括写问题阶段中途换牌阵。

## 字号与排布

- 占卜房、结果页的 TMP 字号 ×1.15，四舍五入到 0.5，0.5 进位（15 → 17.5，17 → 19.5，20 → 23）。
- 例外：
  - 结果页牌下的位置标签由 `ResultPanelPresenter` 在运行时决定字号，不放大；
  - 主菜单只放大「入席问牌」。
- 画布或按钮上的 `UiTypeScale` 记录已应用的倍数，所以重跑不会叠加放大。

所有带框元素的矩形都按公式计算，常量在 `Scripts/UI/UiFitLayout.cs`：

- 矩形 = TMP 首选尺寸 + 内边距（横向 1.4 倍字号，纵向 0.5 倍字号，四舍五入）+ 两侧各 8 的贴图外圈。
- 步骤条：小牌间距 `StepGap` 20，底板内边距 `HudPad` (20, 4)。
- 操作栏：按钮间距 `RowGap` 20，内边距 `DockPad` (24, 10)，上沿固定在 `DockTop` = -122。输入框宽度为操作栏的 62%，高度 = 占位文字首选高度 + 纵向内边距。
- 操作栏下方的 FlowStatusText、Phase10_ReleaseStatusText 依次排在它下面，间隔 `BelowDockGap` 4，不超出画布（y ≥ -360）。
- 结果页和主菜单上单独放置的按钮，只会按文字放大，不会缩小到原设计尺寸以下。
  - 主菜单按钮的中心位置不变。
  - 结果页按钮保持上沿不动、向下长高，因为 `ResultSpreadLayout` 把 `ButtonTopFromCanvasBottom`（84）以上的空间留给了解读区。
  - `ResultCanvasAspectFit` 运行时会按自己表里记录的「到底边的距离」重新摆放按钮，所以这张表里的对应值要同步调整，否则按钮会被放回原来的位置。

场景层级没有改变。新增的对象都以 `Phase69_*` 开头：角星、步骤分隔星、输入框下划线、主按钮两侧的星。

## 运行顺序

1. `Tools/Tarot Unity/Generate Phase 69 UI Kit`
2. `Tools/Tarot Unity/Run Phase 69 UI Restyle Bootstrap`

- 第 2 步可以重复运行，连续两次后场景文件哈希不变。
- 保存前会对每个 TMP 调用 `ForceMeshUpdate`。原因是 TMP 的缓存色 `m_fontColor32` 只在重建网格时才更新，不这样做的话，第二次运行还会改动场景。
- 重跑 Phase 39、40、60、63、67 中任何一个 bootstrapper 之后，都会把旧金框写回场景，必须再跑一遍 Phase 69。

## 截图

每个截图脚本都通过环境变量指定输出目录：

- 占卜房：`PHASE68_CAPTURE_DIR`，脚本 `Phase68ParlorCaptureBuilder`；
- 结果页：`PHASE67_CAPTURE_DIR`，脚本 `Phase67ResultReadingCaptureBuilder`；
- 主菜单：`PHASE69_CAPTURE_DIR`，脚本 `Phase69MenuCaptureBuilder`。

`Phase67ResultReadingCaptureBuilder` 有两处相应调整：

- `FrameInnerLineUnits` 改为 14（8 px 外圈加内线）。
- 它的自检（关掉遮罩后必须能检测到框外文字）原来只看一个位置。字号放大后，被检查的那条 20 单位高的横带恰好落在两段之间的空白里，所以一个像素也检测不到。现在让正文在 0 / 12 / 24 / 36 四个偏移上各测一次，取最大值。

## 没做的

- 结果页牌下的位置标签字号没有放大。
- 「离席」按钮没有改。
- 旧贴图 `TarotPanel`、`TarotPanelSubtle`、`TarotButton` 保留。
- `Phase7_TableVignette`（操作栏背后的平涂暗色矩形）没有动。它比新的操作栏宽，两侧会露出来，是否处理交给用户决定。

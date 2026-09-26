# 文字 UI 重设计：玻璃为底，卡纸点睛（Phase 69）

- **日期：** 2026-09-26
- **状态：** 用户已批准（2026-09-26）。写计划时核对代码又发现四处，已在第 9 节补记，待用户确认。
- **前置：** `main` @ `63975c3`（Phase 68 已合并并推送）。本期分支 `feat/ui-glass-cardstock`。
- **验收：** 实现完成后由用户在 Unity 中亲自检查。第 6 节只列出本设计承诺的效果，供对照。

下文代码路径以 `UnityClient/TarotUnity/Assets/` 为根。

## 1. 问题

- 占卜房、结果页和主菜单「开始占卜」上的金色粗框（Phase 37 的 `TarotPanel` / `TarotPanelSubtle` / `TarotButton` 九宫格）用户认为「土」，与塔罗牌背景割裂。
- 步骤条是一圈大金框里套 5 个小金框，底部操作栏是大金框里套 5 个金按钮，框中套框。
- 选中的牌阵按钮没有任何视觉反馈：`ReadingRoomController.SelectSpread` 只改了状态文字。
- 用户希望文字整体更大。

## 2. 已定方向（用户在草图中选定）

草图：`.superpowers/brainstorm/19473-1790154561/content/ui-direction-bc1-size-v2.html`，档位「稍大 +15%」。

- **玻璃（B）** 是底子：容器和未选中的按钮用烟色半透明玻璃、细金线、更淡的内线，容器对角各一颗小星 ✦。
- **卡纸（C）** 只用在「你选了什么、下一步做什么」上：象牙白卡纸、黑色细边。
- 用户否决了「卡纸为框」方案（②）。

## 3. 范围

| 屏 | 元素 | 新样式 |
|---|---|---|
| 占卜房 | 步骤条底板 `Phase7_RitualHudRoot/Phase7_HudPlate` | 玻璃 + 对角 ✦ |
| 占卜房 | 5 个步骤小牌 `Phase7_Progress_*/Plate` | 当前步 = 卡纸；其余无底板（只有文字） |
| 占卜房 | 底部操作栏 `Phase11_ActionDock` | 玻璃 + 对角 ✦ |
| 占卜房 | 问题输入框 `QuestionInput` | 去掉底板，只留一根金色下划线 |
| 占卜房 | `OneCardButton` / `ThreeCardButton` / `CelticCrossButton` | 选中 = 卡纸；未选中 = 玻璃 |
| 占卜房 | `DrawButton`（洗牌抽取）、`RevealResultButton`（揭示结果） | 卡纸，文字两侧加 ✦ |
| 结果页 | `ResultReadingScroll`、`Phase12_ResultCardShowcase`、横排每格 `SpreadCell_i/ReversePivot/Frame` | 玻璃 |
| 结果页 | `BackToMenuButton`（回到牌桌） | 卡纸 |
| 结果页 | `Phase66_RetryInterpretationButton`（重新解读） | 卡纸（第 9 节补记） |
| 结果页 | `Phase66_OfflineInterpretationButton`（查看离线解读） | 玻璃（第 9 节补记） |
| 主菜单 | `StartReadingButton`（开始占卜） | 卡纸 |

两处细化（对照已批准的草图得出，设计对话中未单列）：

1. 未到、已完成的步骤小牌**不画底板**，只靠文字颜色区分。草图里就是这样。若给每一步都加玻璃底板，又会变回「框中套框」。
2. 问题输入框按草图只留下划线，不再有底板。

不改的：
- 「离席」按钮（Phase 64 已改成克制的象牙色）；
- 结果页的金色分隔线和解读正文里的金色小标题（这些是排版，不是框）；
- 主菜单其余部分、3D 场景和相机；
- 旧的 `TarotPanel`、`TarotPanelSubtle`、`TarotButton` 贴图文件（保留不删，别处可能还在用）。

## 4. 视觉规格

### 4.1 玻璃 `Sprites/GlassPanel.png`

- 底色 `rgb(14,6,14)`，不透明度 0.52。
- 外金线 1 px，`#dba13d`，不透明度 0.55。
- 内线在外线往里 4 px 处，同一种金色，不透明度 0.22。
- 圆角约 4 px。
- 九宫格边界各 32 px（原写 24；加上第 9 节补记的 8 px 外圈后为 32），拉伸时线宽不变。

### 4.2 卡纸 `Sprites/CardStock.png`

- 象牙白纵向渐变，上 `#f2e7cb`，下 `#e6d6b0`。
- 黑棕细边 `#3a2a20`，在边缘往里 3 px，1 px 宽。
- 外围画一圈约 8 px 的柔和黑色投影，透明区域留在贴图内，九宫格边界把它包进去。

### 4.3 文字与点缀

| 场合 | 颜色 |
|---|---|
| 玻璃上的文字 | 象牙 `#f5e8cc`，不透明度 0.85 |
| 卡纸上的文字 | 深棕 `#2a1d17` |
| 步骤「未到」 | 象牙，不透明度 0.55 |
| 步骤「已完成」 | 淡金 `#e8c47a` |
| 容器对角 ✦ | `#e8b85a` |
| 主按钮文字两侧的 ✦ | `#9a6b1e` |

### 4.4 交互

- Button 的 ColorBlock：常态白，悬停偏暖 `(1, 0.96, 0.86)`（ColorBlock 只能压暗、不能提亮到 1 以上，所以「亮 6%」改成偏暖提示），按下 `(0.8, 0.8, 0.8)`，禁用时不透明度 0.45。
- 当前步骤保留 `RitualStepIndicator.currentScale = 1.07`。

### 4.5 字号与尺寸

- 三屏所有 TMP 文字字号 ×1.15，四舍五入到 0.5。只有主菜单例外：只放大「开始占卜」的文字，「离席」、标题等其余文字不动。
- 内边距：横向约为字号的 1.4 倍，纵向约 0.5 倍。
- 按钮和步骤小牌的矩形 = TMP 首选尺寸 + 内边距。
- 步骤条底板、操作栏的矩形 = 包住其中元素后，四周再留一圈边距。操作栏里的按钮在栏内等距排开。输入框宽度约为操作栏的 62%。
- 各元素的锚点和中心位置保持不变，只改大小；同一行元素的位置重新等距排布。

## 5. 结构

**层级不动。** 操作栏背后的框和按钮、输入框是平级的兄弟对象，几十个测试和旧 bootstrapper 都按 `ReadingRoomCanvas/OneCardButton` 这样的路径查找它们。所以不用运行时 LayoutGroup，而是由编辑器脚本量出文字尺寸、算好矩形后写进场景。这些文字都是固定的，搭场景时量一次就够。

新增文件：

1. `Editor/Phase69UiKitGenerator.cs`
   - 用 C# 在 `Texture2D` 上画出 4.1、4.2 两张图，用 `EncodeToPNG` 写入 `Art/MidnightParlor/Sprites/`。
   - 导入设置：Single Sprite，九宫格边界，无 mipmap，Clamp。
   - 可以重复运行，每次输出逐字节相同。
   - 菜单：`Tools/Tarot Unity/Generate Phase 69 UI Kit`。
2. `Scripts/UI/UiSkinState.cs`
   - 挂在需要在玻璃和卡纸之间切换的按钮和步骤小牌上。
   - 序列化字段：`Image plate`、`Graphic label`、`Sprite glass`、`Sprite cardStock`（`glass` 为空表示「非强调时不画底板」）、两种文字颜色。
   - 公开接口：`SetEmphasis(bool)` 和只读属性 `IsEmphasized`，不含其他逻辑。
3. `Editor/Phase69UiRestyleBootstrapper.cs`
   - 可以重复运行，依次打开三个场景，按第 3 节换皮肤、按 4.5 改字号和矩形。
   - 为步骤条底板和操作栏各加两个子对象 `Phase69_Star_TL` / `Phase69_Star_BR`（TMP ✦，不接收射线）。
   - 为输入框加子对象 `Phase69_InputUnderline`。
   - 挂上 `UiSkinState` 并连好引用；为主按钮文字加上两侧 ✦（用 TMP 富文本颜色标签）。
   - 必须在 Phase 39/40/60/63/67 的 bootstrapper 之后运行。重复运行时，字号以记录下的原字号为基准，不会重复放大 1.15 倍（见第 7 节）。

改动的现有代码：

- `RitualStepIndicator.Refresh`：当前步调用 `UiSkinState.SetEmphasis(true)`，其余调用 `false`。标签颜色沿用现有字段，默认值改为 4.3 的颜色。没挂 `UiSkinState` 的旧场景仍走原来的着色逻辑，保证兼容。
- `ReadingRoomController.SelectSpread`：选中牌阵的按钮 `SetEmphasis(true)`，另外两个 `false`。打开场景时默认选中「一张牌」，保存场景时也处于这个状态。
- `Editor/Phase67ResultReadingCaptureBuilder.FrameInnerLineUnits`：改成新玻璃内线的实际值。

## 6. 承诺的效果（供人工检查）

1. 三屏不再有粗金框；容器是能透出桌面的烟色玻璃。
2. 画面上只有这几处是象牙卡纸：当前步骤、选中的牌阵、洗牌抽取或揭示结果、回到牌桌、开始占卜。
3. 切换牌阵时，卡纸跟着移动到新选中的按钮上；在写问题阶段切换也一样。
4. 文字比现在大 15%，任何文字都不超出它的框。
5. 1 张、3 张、10 张牌阵的结果页都能正常阅读，解读区可以滚动。

## 7. 测试

新增守护测试（EditMode）：

1. `GlassPanel`、`CardStock` 存在，是 Single Sprite，九宫格边界非零。
2. 第 3 节里每个换过皮肤的按钮和步骤小牌：TMP 首选宽高加内边距不超过矩形大小（防穿模）。
3. `RitualStepIndicator` 在每个流程状态下都只有当前一步 `IsEmphasized`。
4. `SelectSpread(1/3/10)` 之后，只有对应的牌阵按钮 `IsEmphasized`。
5. 连续运行两次 bootstrapper，字号和矩形与运行一次时完全相同。
6. 第 3 节的对象路径全部仍然存在。

旧测试：凡是写死旧贴图名、旧字号或旧矩形的断言，逐条改为新规格，并在台账里说明改了什么、为什么。检查相对大小的断言（Phase 24 的字号层级、Phase 35 的「离席比开始占卜小」）预计不用改：「离席」字号不变，「开始占卜」变大，两者的大小关系保持不变。

完成标准：
- EditMode、PlayMode、后端测试全绿。
- 用 `Phase68ParlorCaptureBuilder`（真实机位）和 `Phase67ResultReadingCaptureBuilder` 各出一套截图，再补一张主菜单截图，交给用户看。
- 截图的 PNG 写到临时目录；是否刷新仓库里已入库的评审截图，由用户决定。

## 8. 风险

- **TMP 首选尺寸依赖字体图集。** LXGWWenKai 是动态字体，运行时才补字形。量尺寸前要先调用 `ForceMeshUpdate`，保证字形已生成。量完后字体图集文件会出现改动噪声，照旧不提交。
- **旧 bootstrapper 重跑会覆盖新皮肤。** Phase 39/40 的 `Skin` 会把贴图写回旧的金框。按惯例由最新一期的 bootstrapper 负责最终状态，记录在 Phase 69 文档里：重跑旧 bootstrapper 之后，必须再跑一遍 Phase 69。

## 9. 写计划时的补记（2026-09-26）

1. **运行时主题会盖掉新皮肤。**
   - 三个场景的画布上都挂着 `Scripts/UI/TarotUiTheme`，它在 `Awake` 时把所有按钮的 ColorBlock 和底图颜色设成深紫 `(0.12, 0.09, 0.16)`，还会按字号给文字上色（≤16 用灰色，其余用象牙色），给输入框底图上深色。
   - 以前的评审截图在编辑模式下渲染，`Awake` 不会运行，所以截图里的金按钮和真实游戏里看到的不一样。若不处理，卡纸在真实游戏里会被染成深紫色。
   - 处理办法：
     - `TarotUiTheme` 跳过挂了 `UiSkinState` 的按钮；这些按钮里的文字只换字体、不改颜色；
     - 挂了 `TarotUiPreserveColor` 的 TMP 输入框不改底图颜色，问题输入框就挂这个标记，底图设成全透明，这样仍能点中；
     - 「≤16 用灰色」的阈值 16 改为可序列化字段 `mutedSizeThreshold`，占卜房和结果页设为 18.5（16 × 1.15 四舍五入到 0.5），使字号放大后文字颜色角色不变。主菜单保持 16。
   - 标题字体阈值 30 不受影响：两屏最大字号 24，放大后 27.5。
2. **结果页另有两个按钮在用旧金框**：「重新解读」和「查看离线解读」（Phase 66 的失败 / 等待超过 20 秒时出现）。「重新解读」是下一步动作，用卡纸；「查看离线解读」是退路，用玻璃。第 3 节已补进表格。
3. **实现细节的落点：**
   - 结果页横排每张牌下的位置标签（过去 / 现在…）字号由 `ResultPanelPresenter` 在运行时按牌数计算，不在场景里，本期不放大，以免牵动 Phase 62 / 67 的排布。
   - 步骤之间按草图加 ✧ 分隔（新子对象 `Phase69_StepDot_0..3`，不接收射线）。
   - 排布计算放在运行时程序集的纯静态类 `UiFitLayout` 里，因为 EditMode 测试程序集引用不到编辑器代码。
   - 字号放大的幂等性由画布上的标记组件 `UiTypeScale`（记录已应用的倍数）保证：再次运行时按「1.15 ÷ 已应用倍数」缩放，结果是 1，不会重复放大。
   - 玻璃和卡纸贴图外圈各留 8 px 透明 / 投影边，这样同一个按钮在两种状态下看起来一样大。玻璃内线中心距贴图边缘 13.5 px（贴图倍率 1），所以 `FrameInnerLineUnits` 与 Phase 67 测试里的 `FrameInnerGoldLine` 都改为 14。
4. **字体里没有 ✦ / ✧。** 已直接读取 `LXGWWenKai-Regular.ttf` / `-Medium.ttf` 的 cmap：U+2726、U+2727 都不在其中（只有 ★☆，太重）。因此所有小星都改用贴图：
   - 生成器再画第三张图 `Sprites/Sparkle.png`：64×64，四角星，白色，中心实、尖端渐隐；
   - 用 `Image` 显示，按用途着色：容器对角 `#e8b85a`，步骤分隔同色、不透明度 0.6，主按钮文字两侧 `#9a6b1e`；
   - 主按钮两侧的星是按钮的子对象 `Phase69_Flank_L/R`，位置按文字宽度计算，不再写进文字里。

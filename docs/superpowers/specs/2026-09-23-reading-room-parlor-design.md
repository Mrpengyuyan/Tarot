# 占卜房桌面设计（Phase 68）

- **日期：** 2026-09-23
- **状态：** 已获用户批准（2026-09-23，修订版）。
  - 修订原因见第 1.1 节：初版（`01c4fb8`，「真墙 + 帷幔」）依据的截图不是玩家实际看到的机位。按真实机位渲染后，用户把本期主攻从「空间感」改为「桌面」，并批准了第 3、4 节。
  - 验收方式不在本文定义：实现完成后由用户在 Unity 中亲自检查，代码整理也由用户进行（D5）。第 5 节只列出本设计承诺的效果，供人工检查对照。
- **对应计划：** `PROJECT_COMPLETION_PLAN.md` Phase 3 第 4 条（继续使用烛光、桌面方向，并控制实时灯光的性能预算）；1.0 目标中的「进入游戏后能看到具有空间层次的牌桌、烛光、背景和氛围效果」。
- **前置工作：** 分支 `feat/online-interpretation-loop` @ `cd31577`（子项目 A 与 C 已合并）。本期分支 `feat/reading-room-parlor` 从该提交拉出。
- **Phase 编号：** 68

## 1. 背景：已经核实的问题

下文代码路径以 `UnityClient/TarotUnity/Assets/` 为根，坐标为世界单位。

### 1.1 以前的占卜房截图不是玩家看到的画面

- 以往的占卜房评审截图（例如 `Docs/VisualReview/Phase61/ReadingRoom_idle.png`）都用主相机在场景中的保存位置渲染：`(0, 2.85, -4.15)`，FOV 40。
- 但 `Scripts/UI/ReadingRoomController.cs` 的 `Start()` 会调用 `CameraChoreographyController.PlayOpening()`，在 0.75 秒内把相机移到 `defaultPose`：`(0, 2.45, -3.55)`，俯仰 32.2°，FOV 36。玩家静置和游玩时看到的都是 `CameraChoreographyController` 各机位的画面，保存位置只在开场不到一秒里出现。
- 新增的 `Editor/Phase68ParlorCaptureBuilder.cs` 按控制器里的真实机位和 FOV 渲染全部机位：默认、牌堆、单牌、三牌、结果、凯尔特十字。

在真实机位下：

- 画面几乎全是桌面。真正发黑的只有顶部一条约 6–8% 的窄带，那是远边框后面烛光照不到的桌布，不是虚空。
- 初版设计中放在 z=6.2 的墙，在默认、牌堆、单牌、三牌四个机位下都在画面之外。

### 1.2 桌上同时画着 14 个牌位

- 场景里原有的 4 个牌位（`MP_TableStage/MP_CardSockets` 下的 `MP_Socket_OneCardSlot`、`_PastSlot`、`_PresentSlot`、`_AdviceSlot`），加上 Phase 63 的 10 个凯尔特牌位（`MP_CelticSockets/MP_Socket_Celtic_00..09`），全部保存为激活状态。
- `Scripts/` 中没有任何代码按所选牌阵显示或隐藏牌位。所以无论选哪个牌阵，14 个描边始终叠在一起。凯尔特十字靠近镜头的几个在透视下很大，一直延伸出画面底边。
- 这是 Phase 63 引入的回归：当时只在凯尔特机位下检查过效果。

### 1.3 放牌的中央是画面里最暗的地方

- 去掉 UI 的默认机位截图里，只有画外的前排蜡烛把画面左右边缘照出暖红，牌堆是唯一被照亮的物体；放牌的中央区域几乎是纯黑。
- 桌面的主要光源 `MP_TableStage/MP_RoomFill` 是一盏点光源，位于 `(0, 2.3, 0.4)`，强度 1.5，半径 9.5（`Editor/Phase49ReadingRoomLightBootstrapper.cs`）。按平方反比，它在桌面中央的照度只有 1.5/2.3² ≈ 0.28；桌布底色又是有意压暗的深牛血红 `(0.27, 0.095, 0.125)`（`Editor/Phase37AssetFoundationBootstrapper.cs`，为的是让牌和金色保持最亮）。
- 这盏补光的职责是「让金色读成金色」（Phase 42 的教训），画面边缘的牌堆也靠它。
- 牌位材质 `MP_CardSocket.mat` 是 URP Unlit 透明，不受任何光照影响；在暗桌布上，它的暗色内里和桌面糊成一片。

### 1.4 约束

- **相机、机位和 UI 都不动。**
- **不新增贴图。** `Tools/UiKitGenerator/gen_uikit.py` 依赖 PIL，而本机所有 Python 环境都没有 PIL，且不在 tarot 环境中装包。
- **既有守护测试：**
  - `Phase38TableRebuildTests` 断言 `MP_CardSockets` 下正好 4 个子物体。
  - `Phase63SpreadDefinitionTests` 用 `GameObject.Find("MP_CelticSockets")` 找牌位组，而 `GameObject.Find` 只能找到激活的对象；它还断言该组下正好 10 个子物体。
- `ReadingFlowController.SelectSpread()` 通过 `SetState(QuestionInput)` 发出 `StateChanged`，但 `SetState` 在状态不变时直接返回。玩家在「写问题」阶段更换牌阵时，不会有任何事件。

## 2. 用户已定的方向

| # | 决定 |
|---|---|
| D1 | 先修牌位重叠；本期主攻改为**桌面**（初版的「空间感：真墙 + 帷幔」取消）。 |
| D2 | 相机、机位和 UI 布局都不动。 |
| D3 | 不新增贴图。 |
| D4 | 保留补光 `MP_RoomFill`；另加一盏聚光灯做桌面中央的光池。 |
| D5 | 验收由用户实现完成后在 Unity 中亲自检查；代码整理由用户进行。本期不定义自动化验收，也不新增守护测试。 |

## 3. 牌位只显示当前选中的牌阵

- 新增运行时组件 `Scripts/Presentation/SpreadSocketVisibility.cs`，按牌数存放牌位组：
  - 1 张 → `MP_Socket_OneCardSlot`；
  - 3 张 → `MP_Socket_PastSlot`、`MP_Socket_PresentSlot`、`MP_Socket_AdviceSlot`；
  - 10 张 → `MP_Socket_Celtic_00` 至 `_09`。
- 选中哪个牌数，就只激活对应组的牌位，其余全部隐藏；尚未选择任何牌阵（牌数为 0）时全部隐藏。
- 写法沿用 Phase 61 `RitualStepIndicator.socketGlowSets` 的「按牌数分组」模式。
- 在 `ReadingFlowController.SelectSpread()` 中新增事件 `SpreadSelected`（参数为牌数），**每次选牌阵都触发**，不受状态是否变化的影响。组件订阅它，并在启用时按当前牌数应用一次。
- **只切换每个 `MP_Socket_*` 自身的显隐。** `MP_CardSockets` 和 `MP_CelticSockets` 两个组始终保持激活，层级不变，以满足第 1.4 节的两条测试约束。
- 开场 `ReadingRoomController.Start()` 会自动选中「一张牌」，所以玩家一进入就只看到一个牌位。场景保存时也处于这个状态，编辑器里看到的与开场一致。
- 牌位的辉光（Phase 61）照旧由 `RitualStepIndicator` 管理，不改。

## 4. 桌面中央的光池

- **保留**补光 `MP_RoomFill` 原样不动（D4）。
- **新增一盏聚光灯** `MP_TableStage/MP_TablePool`：
  - 位置约 `(0, 4.0, 0.3)`，垂直向下，对准三牌阵那一排牌的中心。
  - 内圈约 40°、外圈约 75°。落到桌面，全亮区半径约 1.5，正好罩住三牌阵的三张牌（x = ±1.45）；再柔和过渡到半径约 3。
  - 颜色取烛光 `(1, 0.66, 0.30)` 与补光 `(1, 0.84, 0.66)` 之间的暖白，起始 `(1, 0.8, 0.58)`；起始强度 12，按渲染结果调整。
  - 不投影：前排两烛已经负责投影，本期不增加阴影开销。
- **效果：** 空桌时，放牌的中央是整张桌布最亮的地方，向四周和远端逐渐沉入黑暗。顶部那条黑带从「虚空」变成「光池外的自然暗部」。
- **凯尔特十字：** 十字主体（x 约 -1.75 至 1.7）落在光池里；右侧权杖那一列（x = 3.2）紧挨着右前烛（x = 3.0），本来就被它照着。
- **三条规则，防止提亮过头：**
  1. 发牌之后，牌仍然是画面中最亮的东西；桌布再亮也只是被照亮的深红。
  2. 牌位描边和牌堆的金色不能被泛光糊掉（项目使用 ACES 色调映射和 Bloom）。
  3. 牌堆不能比现在暗。
- **牌位材质不改。** 桌面亮起来之后，暗色的牌位嵌在亮的桌布里，应当读成凹槽；如果到时仍不对，再单独提出。
- **执行顺序：** 光池由新的 Phase 68 bootstrapper 写入，不修改 Phase 49 的 bootstrapper。重跑 Phase 49 不会影响光池（它只改 `MP_RoomFill`、蜡烛和旧灯）。

## 5. 本设计承诺的效果（供人工检查对照）

本期不定义自动化验收。以下是本设计承诺的效果，供用户在 Unity 中检查时对照：

1. 进入占卜房时，桌上只有「一张牌」的一个牌位。
2. 点「三张牌」或「凯尔特十字」时，桌上只剩该牌阵的牌位；在「写问题」阶段来回切换牌阵也一样。
3. 空桌时，放牌的中央是桌布最亮处，向四周自然变暗。
4. 发牌、翻牌之后，牌仍是画面中最亮的东西。
5. 牌位描边、牌堆上的金色没有被泛光糊成一片。
6. 牌堆不比现在暗。
7. 主菜单与结果页没有任何变化。

## 6. 实现边界

- **新增：**
  - `Scripts/Presentation/SpreadSocketVisibility.cs`（第 3 节）。
  - `Editor/Phase68TableBootstrapper.cs`：给 `MP_TableStage` 挂上并连好 `SpreadSocketVisibility`，把场景存为「一张牌」状态，创建并设置 `MP_TablePool`。可重复运行，结果一致。
  - `Editor/Phase68ParlorCaptureBuilder.cs`：按真实机位渲染评审截图（第 1.1 节），每个机位先应用对应牌阵的牌位；截图经过 Phase 58 的 `CaptureRig.RenderConverged`。
  - `Docs/VisualReview/Phase68/` 下的评审截图。
  - `Docs/PHASE68_READING_ROOM_TABLE.md`：本期说明。
- **修改：**
  - `Scripts/Gameplay/ReadingFlowController.cs`：新增 `SpreadSelected` 事件。
  - `Scenes/ReadingRoom.unity`（由 bootstrapper 写入）。
- **不修改：** Phase 37 / 38 / 49 / 63 的 bootstrapper 源码、共享材质、主菜单与结果页场景、相机与 UI。
- **测试：** 不新增测试（D5）。现有 EditMode、PlayMode 与后端测试须全部保持通过。
- **资源：** 不引入任何新的外部资源。

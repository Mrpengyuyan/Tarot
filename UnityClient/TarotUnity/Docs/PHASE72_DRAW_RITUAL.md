# Phase 72 — 抽牌仪式

用户试玩后反馈：「点击洗牌抽取之后的动画有点快，而且只是简单的展现抽出来的牌，没有什么设计性的效果」。本期让玩家亲手抽牌。

- 设计：`docs/superpowers/specs/2026-09-28-draw-ritual-design.md`
- 计划：`docs/superpowers/plans/2026-09-28-draw-ritual.md`

## 新流程

1. **三拍洗牌**，约 1.7 秒：切牌 → 两次交错搓洗（第二次更快）→ 方齐。
2. **摊牌**：牌堆里滑出 22 张牌背朝上的牌，在牌位和玩家之间摊成 70° 的弧形扇面。同时镜头转到选牌视角，底部动作坞淡出，扇面上方的一盏暖光淡入。
3. **选牌**：鼠标划过扇面，牌随指针起伏成波浪；下一个牌位单独亮着呼吸光。
4. 每点一张牌，分四拍，约 1.1 秒：
   - **抽出**：离开扇面，抬起；
   - **悬空**：转向牌位，光晕涨起，目标牌位同时闪一下；
   - **飞行**：带暖金光轨飞向牌位；
   - **落位**：Phase 54 的压扁回弹和镜头轻震。
5. 选满后，剩下的牌从两端向中间收回牌堆，扇面的灯淡出。
6. 这时才等后台开局的结果（开局在洗牌时就已经在后台开始），然后按牌位顺序把牌面绑定到桌上的牌，进入原有的翻牌流程。

牌的身份始终由后端或本地模拟决定。玩家点哪个位置，只决定落位的先后顺序。后端接口不变。

## 组件

- **`DeckShuffleChoreographer`**（改写）：三拍洗牌。
  - 从下往上数，偶数张归左叠、奇数张归右叠；搓洗时按从下到上的顺序落牌，自然左右交替。这样每张牌都正好落回自己原来的位置，不需要重新排序，Phase 55 的亚毫米回位测试原样通过。
  - `PlannedSeconds` 给出按当前参数计算的总时长。
- **`SpreadFanController`**（新增，挂在 `MP_SpreadFan` 上）：
  - `GetFanPose`：扇面几何（半径 3.4，张角 70°，每张牌比左边一张高 0.004，所以从左往右依次压在上面）。
  - `Spread`：摊开扇面，并淡入扇面灯。
  - 悬停波浪：指针下的牌抬高 0.05、朝玩家滑出 0.3，相邻牌按余弦衰减跟着抬起。
  - `hoverReleaseDelay`（0.12 秒）：指针离开后先等一会再放下牌，避免牌滑出指针范围时来回抽动。
  - `PickCards`：飞行中的点击只排队一次，其余丢弃。
  - `Gather`：收拢剩余的牌，并淡出扇面灯。
  - 扇面的牌悬停时光晕保持原大小（`fanHoverHaloScale` = 1），因为扇面牌挨得太密。
- **`DeckController.DealPickedCard / BindDealtCards / ReturnDealtCards`**：
  - `DealPickedCard`：四拍飞牌。悬空那一拍开始时触发 `CardHovering` 事件；飞行时打开光轨；落位后恢复牌的悬停倾斜。
  - `BindDealtCards`：按牌位顺序绑定牌面。
  - `ReturnDealtCards`：只走后端的模式下开局失败时，把桌上的牌飞回牌堆。
  - 原来的 `DealCards` 保留，但抽牌流程不再调用它。
- **`CardView`**：
  - 新增 `HoverHaloScale`（可以临时覆盖悬停光晕倍数）、`ClearHoverHaloScale`、`SetHaloBoost`（悬空时的光晕增益；增益大于 1 时，即使没在等待翻开也会亮）。
  - `CardClickHandler` 新增 `HoverChanged` 事件。
  - `CardHoverTiltController` 新增 `Resume()`，恢复后会重新记录静止位置。
- **`CameraChoreographyController.drawPoses` / `FocusDraw`**：选牌镜头按牌阵张数取。
  - 1 张和 3 张共用近景 `DrawPoseNear`：(0, 5, -6.3)，俯角 50°，FOV 45。
  - 凯尔特十字用远景 `DrawPoseCeltic`：(0.7, 6.8, -7)，俯角 45°，FOV 50。
  - 为什么分开：如果都用凯尔特的远景，扇面上每张牌露出的边在 1920 宽的屏幕上只有约 24 像素，很难点中。
- **`RitualStepIndicator.FocusSocket / FlashFocusedSocket`**：选牌时只点亮下一格牌位；牌悬空时这一格闪一下。
- **`ReadingFlowController.AbortDraw`**：修复只走后端的模式下开局失败后无法重抽的问题（以前流程停在 `Shuffling`，再按「洗牌抽取」没有反应）。
- **`ReadingRoomController.DrawRoutine`**（重写）：
  - 不再固定等 `ShuffleBreathSeconds`，而是等洗牌动画实际播完。`RitualRhythmDirector` 的这个字段保留，因为 Phase 9 的测试引用它。
  - 选牌期间把 `pickHiddenUi`（动作坞、问题输入框、四个按钮）的透明度降到 0，并关闭点击。

## 引导脚本

`Editor/Phase72DrawRitualBootstrapper.cs`，菜单 `Tools/Tarot Unity/Run Phase 72 Draw Ritual Bootstrap`。可以重复运行，连续运行两次文件哈希不变。它负责：

- 创建 `MP_SpreadFan`，包括 `FanCenter`（0, 0.13, -2.45）和 `FanLight`（聚光灯，颜色与桌面光池相同，默认关闭，强度由扇面控制）；
- 在 `ReadingRoomCameraChoreography` 下创建 `DrawPoseNear` 和 `DrawPoseCeltic`；
- 给 6 个 UI 对象加 `CanvasGroup`，并接到 `ReadingRoomController` 的 `spreadFan`、`stepIndicator`、`pickHiddenUi`；
- 在 `PF_TarotCard` 下加 `FlightTrail`：TrailRenderer，默认不发射，持续 0.3 秒，宽度 0.12 渐细到 0，材质 MP_WarmGlow。

## 测试

- EditMode `Phase72DrawRitualTests`：
  - 洗牌参数范围；
  - 光晕倍数和增益、悬停事件、倾斜恢复；
  - 扇面几何；
  - 选牌镜头按牌数取、牌位聚焦；
  - 光轨；
  - 场景接线和扇面灯；
  - 1、3、10 张牌阵在 16:9 和 4:3 下，扇面与所有牌位都在画面内（6 个用例）；
  - 本文档存在。
- PlayMode：
  - `Phase72ShuffleTests`：洗牌时长，以及确实分成了左右两叠。
  - `Phase72FanTests`：摊开、波浪、短暂离开不抽动、点选顺序与空位、飞行中连点只排队一次、收拢、扇面灯。
  - `Phase72DealPickedTests`：四拍飞牌、光晕涨起、光轨、落位、倾斜恢复、绑定、退回。
  - `Phase72DrawFlowTests`：三张牌全流程（点中的牌对象就是翻开的那张）、绑定前点牌不会翻、凯尔特 10 张。
  - `Phase66ReadingRoomOnlineFlowTests.BackendOnlyFailureReturnsCardsAndAllowsARetry`：只走后端的模式下开局失败，牌飞回牌堆，并且可以重抽。

**改写的旧测试：** 以前点「洗牌抽取」后会自动发牌，现在要玩家点选，所以旧的完整流程测试都要替玩家选牌。新增的测试工具 `DrawRitualTestDriver.PickAll()` 每次点扇面正中间剩下的那张，直到选满。涉及：

- `Phase36PerformanceProbeTests`：帧时间采样从选牌之后开始；
- `Phase66ReadingRoomOnlineFlowTests`（7 处）；
- `Phase66LiveBackendTests`（3 处，这组测试需要真后端，平时跳过）；
- `Phase70RevealReplacesDrawTests`、`Phase71CardHoverTests`、`VerticalSliceFlowTests`。

另外，牌现在先落位、后绑定，所以原来只看「桌上有 N 张牌」的等待条件，改为同时要求进入「等待翻牌」状态。否则会在会话保存之前就去读会话。实时联调测试里记录的「首张牌耗时」，现在的含义是「牌桌就绪耗时」，其中包含选牌时间。

## 可调参数

- **洗牌**：`cutSpread`、`cutSeconds`、`recutSeconds`、`cutYawDegrees`、`riffleBendDegrees`、`secondRiffleSpeedup`、`squareHoldSeconds`，以及 Phase 55 原有的各项参数。
- **扇面**：`radius`、`arcDegrees`、`layerStep`、摊开节奏、`hoverLift`、`hoverSlide`、`waveRadius`、`hoverReleaseDelay`、`fanHoverHaloScale`、`gatherSeconds`、`fanLightIntensity`。
- **飞牌**：`pickPullSeconds`、`pickPullDistance`、`pickRise`、`pickHoverSeconds`、`pickHoverBob`、`pickGlowBoost`、`returnSeconds`。飞行段沿用 `dealDuration`（0.56，满足 Phase 9 下限）和弧高。

## 截图与验收

`Editor/Phase72CaptureBuilder.cs`，输出目录由环境变量 `PHASE72_CAPTURE_DIR` 指定，不写入仓库。生成：

- `Fan_{1,3,10}_{169,43}.png`：三种牌阵、两种画面比例下的扇面；
- `Fan_hover.png`：悬停波浪；
- `Pick_hover.png`：选中的牌悬空、光晕涨起；
- `Pick_flight.png`：飞行中的牌。

编辑模式下 TrailRenderer 手动加入的点不会渲染，所以飞行截图里看不到光轨。光轨确实在飞行时发射，这一点由 PlayMode 测试保证。

需要在 Unity 里实际体验的手感：

- 洗牌够不够有分量；
- 扇面波浪顺不顺手；
- 悬空停顿和光轨的时长、亮度；
- 整体会不会拖沓。

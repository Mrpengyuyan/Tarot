# 抽牌仪式（Phase 72）设计

日期：2026-09-28　状态：待用户审阅　分支：`feat/draw-ritual`

## 背景与目标

用户试玩反馈：「点击洗牌抽取之后的动画有点快，而且只是简单的展现抽出来的牌，没有什么设计性的效果」。

现状：
- 洗牌 = `DeckShuffleChoreographer` 一拍，约 0.66 秒，压下、涟漪、方齐。
- `DrawRoutine` 固定等待 `RitualRhythmDirector.ShuffleBreathSeconds`（0.66 秒）后再等后端开局。
- `DeckController.DealCards` 自己生成 N 张牌，逐张沿弧线飞到牌位。
- 玩家全程不参与「抽」。

目标：让玩家**亲手选牌**。已由用户在对话中逐项确认：
1. 亲手选牌，不做纯自动演出。
2. 洗完牌后，牌在桌上摊成**弧形扇面**。
3. **选一张飞一张**，不做「选满再一起飞」。
4. 实现走**方案 A**：扇面里被点中的牌对象，就是之后落位、翻开的那张牌。

成功标准：玩家从点「洗牌抽取」到可以翻牌，经历「有分量的洗牌 → 扇面展开 → 亲手点选 → 每张牌悬空、带光轨落位」。三张牌阵的非选牌时间约 6–8 秒，不拖沓。翻牌及之后的流程不变。

## 1. 流程与后台时序

- 不新增 `ReadingFlowState`。`Drawing` 的语义扩为「选牌 + 飞牌」。`RitualStepIndicator`、音效提示等状态消费者不改。
- 新的 `DrawRoutine` 顺序：
  1. `BeginShuffle`，播洗牌，并照旧在后台开局（`StartOnlineReadingRoutine`）。
  2. 等洗牌动画**实际播完**（`DeckShuffleChoreographer.IsPlaying == false`），不再固定等 0.66 秒。
  3. `BeginDeal` 进入 `Drawing`，摊开扇面。**不等后端**，玩家立即可选。
  4. 玩家选满 N 张，每张飞到下一个牌位。飞过去的牌背朝上，**尚未绑定数据**。
  5. 扇面收拢回牌堆，然后等后台开局结果，沿用现有的超时逻辑：
     - 成功：按牌位顺序把 `session.cardDraws[i]` 绑定（`Bind` + `SetFaceArtwork`）到第 i 张已落位的牌。
     - 失败且允许离线：用 `LocalReadingSimulator` 生成的牌绑定，与现状一致。
     - `BackendOnly` 模式下失败：已落位的牌飞回牌堆并销毁，显示现有错误文案，恢复按钮，结束本次抽牌。
  6. 绑定后给每张牌设 `SetHighlighted(true)`，调用 `WireActiveCards`，然后 `WaitForCardFlips` 和 `FocusSpread`。翻牌流程不变。
- 牌的身份永远由后端或本地模拟决定。玩家点哪个位置，只决定落位的先后顺序。不改后端接口。
- 防误操作：
  - 选牌期间牌阵按钮和输入栏隐藏。
  - 桌上已落位的牌在绑定前不可翻：不响应 `CardClickHandler` 的翻牌。
  - 飞行中的牌不可点。

## 2. 扇面与悬停

- 扇面位于牌位与玩家之间的桌面上，即原底部输入栏所挡的区域。弧心在玩家一侧，弧线向牌位方向拱起。
- 固定 22 张道具牌，使用 `PF_TarotCard` 实例、牌背朝上、未绑定数据。
- 总张角约 70°，相邻牌重叠约 2/3，每张都露出一条可点的边。
- 新增「选牌镜头」位姿 `drawPose`：高于且俯于默认视角。1、3、10 张牌阵下，扇面与所有牌位都必须在画面内。选满后镜头转到 `FocusSpread`。
- 摊开约 1 秒：
  - 牌堆（洗完的道具叠）位置不动；22 张扇面牌从牌堆位置逐张（间隔约 0.035 秒）沿弧线滑到扇面位。
  - 最后一张到位时，整排做一次轻微落定（微压扁）。
- 悬停：
  - 指针下的牌抬起一点，并朝玩家方向滑出约半个牌身。
  - 左右相邻两到三张按距离衰减跟着抬起，形成随指针移动的波浪。所有位移用平滑插值跟随。
  - 悬停牌底亮 Phase 71 的暖光晕，但幅度比「等待翻开」更收敛，扇面专用缩放。
  - **下一个**目标牌位呼吸发光：复用 Phase 61 的 socket glow，只点亮下一格。
- 选牌期间，底部输入栏所在的 UI（动作坞）淡出，状态文案保留，提示「选出 N 张牌」。

## 3. 点选后的飞牌与光轨

每张被点中的牌分四拍，约 1.1 秒：
1. **抽出**（约 0.15 秒）：沿悬停滑出方向继续抽离扇面并抬高，缓入。
2. **悬空**（约 0.25 秒）：
   - 在扇面上方停住，缓转到目标牌位朝向，轻微上下浮动。
   - 牌底暖光先升到峰值再回落。
   - 目标牌位的光同时闪一下。
3. **飞行**（约 0.55 秒）：
   - 沿弧线飞到牌位，弧高与侧倾沿用 `DeckController` 现有字段，并满足 Phase 9 的节奏下限（`dealDuration ≥ 0.52`）。
   - 拖尾光轨用 `TrailRenderer`，材质为 MP_WarmGlow，宽度与透明度逐渐衰减，约 0.3 秒消失。只在飞行时发射。
4. **落位**（约 0.15 秒）：
   - 沿用 Phase 54 的触地压扁、回弹和镜头 `Kick`，触发 `CardDealt` 事件（音效和粒子照旧）。
   - 当前牌位的光熄灭，下一个牌位的光亮起。
   - 牌保持背面朝上，并亮起「等待翻开」的淡光。
- 飞行中可以继续悬停。飞行中的点击**排队一次**，落位后立即起飞，其余点击忽略。
- 被抽走的位置在扇面上留空，两侧牌不合拢。
- 声音：不新增素材。落位用现有 `CardDealt` 提示音。点中时是否加轻提示音，实现时视现有音效而定，没有合适的就不加。
- 选满后，剩余扇面牌在约 0.6 秒内从两端向中间收拢、滑回牌堆，然后销毁。

## 4. 洗牌加长

`DeckShuffleChoreographer` 改为三拍，共约 1.8 秒，每拍参数都做成序列化旋钮：
1. **切牌**（约 0.45 秒）：
   - 保留预压。
   - 然后上半叠抬起平移到左侧，下半叠向右微移，两叠带相反偏转。
2. **搓洗两次**（每次约 0.5 秒，第二次略快）：
   - 两叠内侧边缘翘起，左右交替一张张落到中间，交错成一叠。
   - 第一次洗完再分叠洗第二次。
3. **方齐**（约 0.35 秒）：收拢、压扁，镜头 `Kick`，然后精确回到原位（保留 Phase 55 的亚毫米回位断言）。

另外：
- 用现有的十来张可见叠牌做交错，视觉复用。
- `DrawRoutine` 改为等 `IsPlaying` 变为 false，不再等 `ShuffleBreathSeconds`。该字段保留（Phase 9 测试引用），但不再用于驱动流程。
- 洗牌的粒子和音效保持在开始时触发。

## 5. 组件划分

- `DeckShuffleChoreographer`（改）：三拍洗牌。对外接口不变：`Play()` / `IsPlaying`。
- `SpreadFanController`（新，`Scripts/Gameplay/`）：
  - 负责扇面牌的生成、摊开、悬停波浪、点击排队与收拢。
  - 公开 `IEnumerator Spread()`、`IEnumerator Gather()`、`event Action<CardView> CardPicked`、`bool AcceptingPicks`。
  - 扇面几何（中心、半径、张角、牌数）是序列化字段。
- `DeckController`（改）：
  - 新增 `IEnumerator DealPickedCard(CardView card, Transform slot)`：抽出、悬空、飞行、落位四拍，并把牌加入 `ActiveCards`。
  - 新增 `void BindDealtCards(IList<CardDrawData> draws)`。
  - 新增 `IEnumerator ReturnDealtCards()`：`BackendOnly` 失败时牌飞回牌堆。
  - 原有 `DealCards` 保留（现有测试与离线路径可用），但抽牌主流程不再调用。
- `CardView`（改）：新增「可翻」开关。未绑定前 `CardClickHandler` 不触发翻牌；绑定时打开。
- `CameraChoreographyController`（改）：新增 `drawPose`、`drawFov` 与 `FocusDraw()`。
- `ReadingRoomController`（改）：`DrawRoutine` 按第 1 段重写；选牌期间隐藏动作坞，并更新状态文案。
- 下一格牌位发光：由 `RitualStepIndicator` 的 socket glow 集合扩展出「只点亮第 k 格」的接口。
- `Phase72DrawRitualBootstrapper`（新，Editor）：
  - 在 ReadingRoom 场景挂 `SpreadFanController`，建 `drawPose`，给牌预制体加 `TrailRenderer`（默认关闭），写入各项参数。
  - 可重复运行，结果不变。
- `Phase72CaptureBuilder`（新，Editor）：输出验收截图到 `$PHASE72_CAPTURE_DIR`。
- 文案新增到 `ReleaseUxCopy`：选牌提示，例如「凭直觉选出 N 张牌」。

## 6. 出错与边界

- **后端慢：** 选牌期间后台继续开局。选满后若还没完成，沿用现有的开局超时（`OnlineStartTimeoutSeconds`），状态文案显示等待。
- **BackendOnly 失败：** 见第 1 段，牌飞回牌堆，显示错误，恢复控件。
- **扇面牌数少于牌阵张数：** 不会发生（22 > 10），但加守护：`SpreadFanController` 的牌数必须 ≥ 目录里最大的牌阵张数。
- **场景切换或中途退出：** 协程随对象销毁，扇面牌挂在牌堆父节点下，一并清理。
- **窗口比例变化：** 选牌镜头按现有 `CameraChoreography` 窄屏规则取景。截图验收要覆盖 16:9 与 4:3。

## 7. 测试与验收

- **EditMode：**
  - 扇面控制器存在、22 张、张角约 70°。
  - `drawPose` 在 1、3、10 张牌阵下，扇面与所有牌位都在视锥内（16:9 与 4:3）。
  - 洗牌总时长约 1.8 秒，且各拍参数就位。
  - Phase 9 下限依旧满足。
  - 牌预制体的 `TrailRenderer` 使用 MP_WarmGlow，默认不发射。
- **PlayMode：**
  - 三张牌局全流程：洗牌 → 摊牌 → 模拟点选 3 次 → 依次落位 → 收拢 → 绑定 → 翻牌 → 结果。
  - 被点中的牌对象就是最终翻开的对象（同一实例）。
  - 飞行中点击会排队，不会两张抢一个牌位。
  - 绑定前点桌上的牌不翻。
  - BackendOnly 失败时，牌返回牌堆、显示错误、控件恢复。
  - 洗牌后牌堆亚毫米回位。
  - 凯尔特十字 10 张可全部选完并落位。
- **旧测试：** 写死旧流程的测试按新行为改写，每处在 `Docs/PHASE72_DRAW_RITUAL.md` 记录原因。
- **截图：** 扇面（1、3、10 张阵）、悬停波浪、悬空停顿、光轨飞行中、收拢；16:9 与 4:3。先自检，再交用户。
- **用户验收（在 Unity 中）：** 洗牌分量、扇面波浪手感、悬空与光轨的时长和亮度、总时长是否拖沓。

## 不做的事

- 不改翻牌动画、结果页和后端接口。
- 不新增音频素材。
- 不做「选满再一起飞」和「快速抽牌」开关。
- 不做 78 张全牌扇面：22 张是道具数量。

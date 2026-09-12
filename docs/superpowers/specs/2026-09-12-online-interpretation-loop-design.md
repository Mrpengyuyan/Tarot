# 在线解读闭环设计（子项目 A）

- **日期：** 2026-09-12
- **状态：** 第 1–5 节已逐节获用户批准，待审阅 spec
- **对应计划：** `PROJECT_COMPLETION_PLAN.md` Phase 1（真实后端与会话闭环）、Phase 4（结果页与 AI 失败降级）
- **前置工作：** 仓库归一（本地 `main` 为 `2452f83`，后端代码在 `Server/`）
- **Phase 编号：** 66（`Phase65GuestSessionTests` 已占用 65）

## 1. 背景：已经核实的问题

以下结论都来自对当前代码的逐行阅读，文件路径以 `UnityClient/TarotUnity/Assets/` 和 `Server/` 为根目录。

### 1.1 占卜房从来没有真正发起过在线解读

- `Scenes/ReadingRoom.unity` 里，`BackendReadingService` 的 `apiClient` 字段序列化为 `fileID: 1615814164`。这是**同一场景内**的另一个 `ApiClient` 组件（脚本 guid `277e7933…`），`baseUrl` 仍是默认的 `http://localhost:8000/api/v1`。
- `Scripts/Network/BackendReadingService.cs` 优先使用这个序列化引用，并且 `CanCreateAuthenticatedReading => Client != null && Client.HasSession`。
- 访客令牌由常驻的 `BackendSessionBootstrap` 申请。`Scripts/Core/GameBootstrap.cs` 在 Boot 对象上创建它和常驻 `ApiClient`，令牌写入主菜单阶段 `FindFirstObjectByType<ApiClient>()` 找到的实例，也就是常驻实例。
- `ApiClient` 不是单例。`ApplySession` 和 `ExportSession` 在运行时代码中没有任何调用方（只有一个 EditMode 测试用到）；`DesktopConfigLoader` 也只配置 Boot 对象上的客户端。
- **结论（静态分析）**：占卜房里 `ShouldTryBackend()` 恒为 false，每一局都走本地模拟。许可证激活后，第一件事就是用失败测试复现这一点。

### 1.2 令牌问题修好后，在线流程仍然走不通

- `BackendReadingService.CompleteReading` 在**发牌之前**串行执行建记录、抽牌、取牌、生成 AI 解读、取记录共 5 个请求。
- `ApiClient.PrepareRequest` 给所有请求设置 `timeout = 15` 秒。而后端 `DEEPSEEK_TIMEOUT=65` 秒，`AI_MAX_RETRIES=2`，另有退避等待和 reasoner 回退，单次生成最坏大约 3.5 分钟。
- 请求超时后，Unity 会退回本地模拟，发出的牌和后端抽到的牌不同。但后端记录已经建好，访客额度也已经扣掉（`Server/app/api/v1/endpoints/records.py` 在建记录时调用 `_enforce_guest_daily_reading_limit`），服务器还在继续调用 AI。
- 玩家看到的是 `ApiClient.BuildError` 拼出的原始报文，例如 `504: {"detail":"AI interpretation request timed out"}`。

### 1.3 其他相关问题

- 结果页（`Scripts/UI/ResultSceneController.cs`）只在 `Start` 时读取一次会话，没有"生成中"、失败或重试的处理。
- 后端解读接口没有并发保护：两个同时到达的请求会各自调用一次 AI。状态枚举里有 `PROCESSING`，但从来没有被设置过；记录表也没有"开始生成时间"字段。
- 占卜房的默认问题是英文 `"What should I notice now?"`，会原样进入 AI 提示词；抽牌、翻牌、结果阶段的流程状态文字也都是英文。
- `LocalReadingSimulator` 的提醒文案"这是本地占位文本，后端 AI 解读将在后续阶段接入。"会显示给玩家。
- `/guest-session` 每次调用都新建一个访客用户（`auth.py` 的 `_create_guest_user`）。所以令牌过期后**不能**重新申请访客会话，否则会拿不到自己的记录，只能调用 `/refresh` 续期。

## 2. 目标与非目标

### 2.1 目标

1. Unity 从 Boot 运行且后端在线时，1、3、10 张牌阵都能拿到**真实的 AI 解读**。
2. 发牌不再等待 AI；等待发生在结果页，并且有明确的状态提示。
3. 失败可以恢复：重试不新建记录、不多扣访客额度，也可以随时查看离线解读。
4. 在线流程中玩家能看到的文字全部是中文，不出现原始报文。
5. 同一条记录不会被并发地重复调用 AI，每条记录最多生成 3 次。

### 2.2 非目标

- 子项目 B（占卜房交互清晰度）、C（结果页阅读体验）、D（设置菜单）。
- 后端部署与 HTTPS。
- 访客"重启游戏就重置额度"的问题（留给部署级限流处理）。
- 正式账号的登录界面。
- 10 张牌阵的整体解读文案（`LocalReadingSimulator.BuildOverall` 对多张牌只有一段通用文字）。
- `BackendOnly` 模式的行为变化。
- 牌阵选择阶段的英文文案（牌阵名、`N card(s)`、开场提示），这些归 B 处理。

## 3. 已做决策

| 编号 | 决策 | 理由 |
| --- | --- | --- |
| D1 | 采用"先发牌 + 后端异步生成 + 结果页轮询"。不采用同步长请求，也不采用只调长超时。 | 每个请求都能在 15 秒内完成；部署到 PaaS 或经过代理时，常见请求超时在几十秒量级（例如 Heroku 30 秒、Cloudflare 代理 100 秒），长请求会被切断。只调超时会保留洗牌阶段的长时间干等。 |
| D2 | 验收包含使用用户 DeepSeek Key 的真实联调。 | 慢响应是真实环境下的问题，只用 mock 证明不了已经解决。Key 只放在 `Server/.env`，不进对话、不进仓库、不进客户端。 |
| D3 | 轮询复用现有的 `GET /records/{id}`，不新增查询接口。 | 返回内容已经包含 `status` 和 `interpretation`。 |
| D4 | 同步接口 `POST /records/{id}/interpret` 的对外行为保持不变。 | 现有测试依赖它的 200、幂等、400、504/502 行为。 |
| D5 | 用代码修复令牌问题（`ApiClient.Shared`），不手改场景文件。 | 改动小、可以测试，并且在编辑器里直接运行 ReadingRoom 的开发方式仍然可用。 |
| D6 | 结果页新增的 UI 沿用 Bootstrapper 惯例（`Phase66…Bootstrapper` 修改场景并加守护测试）。 | 与项目现有做法一致。代价是必须有 Unity 许可证才能运行。 |
| D7 | 建记录阶段失败时，本局自动改为离线解读，不停下来让玩家选择。 | 保持仪式节奏；文案会明确告诉玩家这是离线解读及原因。 |

## 4. 架构与数据流

```text
主菜单   常驻 ApiClient ← 访客会话（已有逻辑）
占卜房   洗牌动画 ─┬─ POST /records                  建记录（扣 1 次访客额度）
                  └─ POST /records/{id}/draw        抽牌 → GET /records/{id}/cards
         用后端真实抽到的牌发牌
         InterpretationPoller.Begin(快照)
           └─ POST /records/{id}/interpret/async → 202，后端开始生成
         玩家翻牌（与后端生成同时进行）
         点「揭示结果」直接进入结果页，不等待解读
结果页   牌面立即亮相，解读区显示「牌意正在汇聚……」
         InterpretationPoller（常驻）轮询 GET /records/{id}
           completed 且带解读 → 显示解读（mock_ai 时标注「模拟解读」）
           failed / 超时 / 连接中断 → 中文原因 +「重新解读」+「查看离线解读」
```

### 4.1 组件与职责

| 组件 | 位置 | 职责 | 依赖 |
| --- | --- | --- | --- |
| 异步生成接口 | `Server/app/api/v1/endpoints/records.py` | 校验记录，抢占生成权，登记后台任务，返回 200/202/400/404/429 | 抢占函数、生成函数、会话工厂 |
| 抢占函数 | `Server/app/crud/prediction.py` | 用一条带条件的 UPDATE 原子地进入 `PROCESSING` | 数据库 |
| 生成函数 | `Server/app/api/v1/endpoints/records.py` | 取牌、调用 AI、存库、设置 `COMPLETED` 或 `FAILED`，同步与异步路径共用 | `tarot_interpretation_service` |
| 会话工厂依赖 | `Server/app/db/session.py` | 给后台任务提供可被测试替换的会话工厂 | `SessionLocal` |
| `ApiClient.Shared` | `Scripts/Network/ApiClient.cs` | 指向常驻客户端，唯一持有令牌的实例 | `GameBootstrap` 负责赋值 |
| `BackendReadingService` | `Scripts/Network/BackendReadingService.cs` | `StartReading`（建记录、抽牌、取牌）与 `RequestInterpretationAsync` | `ApiClient` |
| `InterpretationPoller` | `Scripts/Network/InterpretationPoller.cs`（新增，常驻） | 触发异步生成、轮询、退避、总时限、401 续期，对外发布状态事件 | `BackendReadingService`、`ApiClient` |
| `ReadingSessionSnapshot` | `Scripts/Data/ReadingSessionSnapshot.cs` | 增加在线/离线来源、记录 ID 和解读状态 | 无 |
| 占卜房控制器 | `Scripts/UI/ReadingRoomController.cs` | 新流程编排与失败时改为离线 | 以上各项 |
| 结果页控制器与面板 | `Scripts/UI/ResultSceneController.cs`、`ResultPanelPresenter.cs` | 订阅轮询器状态，渲染四种显示 | `InterpretationPoller` |
| 文案 | `Scripts/UI/ReleaseUxCopy.cs` | 所有面向玩家的在线解读中文文案 | `ApiError` |

## 5. 后端设计

### 5.1 数据库字段与迁移

在 `predictions` 表（`Server/app/models/record.py` 的 `Prediction` 模型）新增两列：

| 列 | 类型 | 说明 |
| --- | --- | --- |
| `interpretation_started_at` | `DateTime(timezone=True)`，可空 | 最近一次开始生成的时间 |
| `interpretation_attempts` | `Integer`，非空，`server_default="0"` | 已开始生成的次数 |

新增迁移 `Server/alembic/versions/0002_interpretation_generation_tracking.py`：`down_revision = "0001_initial_schema"`。

`0001` 是直接用**当前模型**执行 `Base.metadata.create_all` 建表的，所以全新数据库跑完 `0001` 就已经有这两列。因此 `0002` 在 `upgrade()` 中必须先用 inspector 检查列是否存在，不存在才 `add_column`；`downgrade()` 同样只删除存在的列。

### 5.2 新增配置项

在 `Server/app/core/config.py` 与 `Server/.env.example` 中新增：

| 配置 | 默认值 | 含义 |
| --- | --- | --- |
| `AI_INTERPRETATION_STALE_SECONDS` | `300` | `PROCESSING` 超过这个时间视为卡住，可以重新抢占 |
| `AI_INTERPRETATION_MAX_ATTEMPTS` | `3` | 每条记录最多开始生成的次数 |

Unity 端轮询总时限为 330 秒（300 + 30 秒余量）。两边的数值需要保持一致，在代码注释里互相指明。

### 5.3 抢占式开始生成

在 `Server/app/crud/prediction.py` 中新增 `claim_interpretation_generation(db, prediction_id, now, stale_before, max_attempts) -> bool`，执行一条原子的条件更新：

```sql
UPDATE predictions
SET status = 'processing',
    interpretation_started_at = :now,
    interpretation_attempts = interpretation_attempts + 1
WHERE id = :prediction_id
  AND interpretation_attempts < :max_attempts
  AND NOT EXISTS (SELECT 1 FROM interpretations WHERE prediction_id = :prediction_id)
  AND (status <> 'processing'
       OR interpretation_started_at IS NULL
       OR interpretation_started_at < :stale_before)
```

受影响行数为 1 表示抢占成功。多个请求、多个进程同时调用时，最多只有一个成功。

### 5.4 提取生成逻辑

把现在写在 `create_ai_interpretation` 中的"取牌 → 组装牌数据 → 调用 `tarot_interpretation_service.create_interpretation` → 构造 `InterpretationCreate` → 存库 → 设置状态"提取为 `generate_and_store_interpretation(db, prediction, user_context)`。异常分类保持现状：超时时设为 `FAILED` 并抛出 504，上游 HTTP 错误和其他错误时设为 `FAILED` 并抛出 502，存库遇到唯一约束冲突时返回已有的解读。同步接口改为调用这个函数，对外行为不变。

### 5.5 异步接口

`POST /records/{prediction_id}/interpret/async`，依赖 `get_current_active_user`、`get_db` 和新的 `get_session_factory`。

按以下顺序判断：

1. 记录不存在或不属于当前用户（非超级用户）→ **404**
2. 已经有解读 → **200**，返回解读内容（与同步接口相同的结构）
3. 还没有抽牌 → **400**
4. 抢占成功 → 登记后台任务 `run_interpretation_job(session_factory, prediction_id, user_context)`，返回 **202**：`{"prediction_id": id, "status": "processing"}`
5. 抢占失败时：
   - 重新检查，如果此时已经有解读 → **200**
   - 状态为 `PROCESSING` 且未超时 → **202**（不重复登记任务）
   - 尝试次数已达上限 → **429**，`detail` 为 `"Interpretation attempts exhausted"`

### 5.6 后台任务与数据库会话

- `Server/app/db/session.py` 新增 `get_session_factory()`，返回 `SessionLocal`。
- 后台任务通过这个工厂自己打开、关闭会话，**不复用请求自带的 `db`**，因为它在响应发出后会被关闭。
- 后台任务捕获所有异常：记录日志，并确保状态为 `FAILED`。
- `Server/tests/conftest.py` 的 `client` fixture 增加一行替换：`app.dependency_overrides[get_session_factory] = lambda: db_session_factory`。
- 进程中途退出时，状态会停在 `PROCESSING`，超过 `AI_INTERPRETATION_STALE_SECONDS` 后可以被重新抢占。

### 5.7 mock 解读中文化

`Server/app/services/tarot_service.py` 的 `_create_mock_interpretation` 只在 `ALLOW_MOCK_AI_FALLBACK=true` 时使用。把概要、整体解读、建议和提醒改成中文，并在整体解读开头写明"这是模拟解读，未连接 AI 服务"。`model_used` 保持 `mock_ai`。

## 6. Unity 设计

### 6.1 修复令牌问题：`ApiClient.Shared`

- `ApiClient` 新增 `public static ApiClient Shared { get; private set; }` 以及用于赋值和清空的方法（供 `GameBootstrap` 和测试使用）。
- `GameBootstrap.Awake` 在 `EnsureService<ApiClient>()` 之后，把 Boot 对象上的实例设为 `Shared`。
- `BackendReadingService.Client` 与 `ReadingRoomController.EnsureBackendReferences` 按以下顺序取客户端：`ApiClient.Shared` → 序列化引用 → `FindFirstObjectByType<ApiClient>()`。
- 不修改 `ReadingRoom.unity`。不经过 Boot、在编辑器里直接运行占卜房时 `Shared` 为空，行为与现在相同。

### 6.2 结构化错误 `ApiError`

新增 `Scripts/Network/ApiError.cs`：

| 字段 | 说明 |
| --- | --- |
| `long StatusCode` | HTTP 状态码，连接失败时为 0 |
| `ApiErrorKind Kind` | 按状态码归类：401 → `Unauthorized`，404 → `NotFound`，400 或 422 → `BadRequest`，429 → `RateLimited`，≥500 → `Server`；状态码为 0 时，`request.error` 包含 "timeout"（不区分大小写）→ `Timeout`，否则 → `Network` |
| `int RetryAfterSeconds` | 读取 `Retry-After` 响应头，没有该头时为 -1 |
| `string RawMessage` | 原始报文，只写日志，不显示给玩家 |

新增的在线解读方法使用 `Action<ApiError>` 回调；已有方法的签名保持 `Action<string>` 不变。

### 6.3 会话快照字段

`ReadingSessionSnapshot` 新增：

| 字段 | 类型 | 默认值 |
| --- | --- | --- |
| `predictionId` | `int` | 0（离线局） |
| `source` | `ReadingSource { Offline, Online }` | `Offline` |
| `interpretationState` | `InterpretationState { Pending, Ready, Failed }` | `Ready`（离线局一开始就有文本） |
| `modelUsed` | `string` | 空 |
| `failureMessage` | `string` | 空（面向玩家的中文） |
| `canRetry` | `bool` | false |

`ReadingSessionMapper` 在把"建记录 + 抽牌"的结果映射为快照时，设置 `source = Online`、`interpretationState = Pending`；在映射带解读的记录详情时，设置为 `Ready` 并填入 `modelUsed`。

### 6.4 `BackendReadingService`

- 删除 `CompleteReading`。
- 新增 `StartReading(PredictionCreateRequest, Action<ReadingSessionSnapshot>, Action<ApiError>)`：建记录 → 抽牌 → 取牌，每个请求 15 秒超时。
- 新增 `RequestInterpretationAsync(int predictionId, Action<AsyncInterpretationResult>, Action<ApiError>)`：结果为 `Accepted`（202）或 `AlreadyReady`（200，附带解读）。
- 新增 `GetRecord(int predictionId, Action<PredictionDetailResponse>, Action<ApiError>)`，供轮询使用。
- `ApiRoutes` 新增 `RecordInterpretAsync(int predictionId)`，返回 `/records/{id}/interpret/async`。

### 6.5 `InterpretationPoller`（常驻）

- 由 `GameBootstrap.EnsureService<InterpretationPoller>()` 创建。
- `Begin(ReadingSessionSnapshot)`：停止之前的轮询，记录开始时间，调用 `RequestInterpretationAsync`；返回 `AlreadyReady` 时直接完成，返回 `Accepted` 时开始轮询。
- **轮询间隔**：第 1、2 次各 2 秒，第 3、4 次各 3 秒，之后每次 5 秒。写成纯函数 `PollDelaySeconds(int attemptIndex)`。
- **总时限**：从 `Begin` 算起 330 秒，超时后进入 `Failed`（超时，可重试）。
- 每次 `GET /records/{id}`：
  - `completed` 且带解读 → 映射进快照，进入 `Ready`
  - `failed` → 进入 `Failed`（可重试）
  - `processing` 或 `pending` → 继续
- **网络错误**：连续 3 次才进入 `Failed`（连接中断，可重试），任意一次成功就把计数清零。
- **401**：调用 `ApiClient.Refresh` 一次，成功后重试本次请求；续期失败则进入 `Failed`（连接过期，不可重试）。
- **异步接口返回 429** → 进入 `Failed`（尝试次数用完，不可重试）；**返回 400 或 404** → 进入 `Failed`（无法生成，不可重试）。
- 对外暴露 `Current`（快照）、`StateChanged` 事件、`Retry()`（对 `Current` 再次调用 `Begin`）、`Stop()`、`UseOffline()`（停止轮询，并用 `LocalReadingSimulator` 基于 `Current.cardDraws` 生成离线文本，`source` 改为 `Offline`）。
- 为了测试，轮询间隔倍率和总时限都可以注入。

### 6.6 占卜房流程

`ReadingRoomController.DrawRoutine` 改为：

1. 播放洗牌动画，同时调用 `StartReading`。状态文字为"正在洗牌……"。
2. `StartReading` 失败时：
   - 401：先调用 `/refresh`；失败则重新申请访客会话；然后重试一次 `StartReading`（此时还没有产生记录，可以换用户）。
   - 其余失败，或重试后仍然失败：按 7.3 表显示原因，用 `LocalReadingSimulator` 生成离线局，`source = Offline`。
3. `StartReading` 成功：`ReadingSessionStore.Save(快照)`，用后端的牌发牌，并调用 `InterpretationPoller.Begin(快照)`。
4. 翻牌阶段的状态文字为"点击每张牌，把它翻开。"。底部的 `releaseStatusText` 跟随轮询器状态，显示"解读正在生成……"或"解读已就绪"。
5. 全部翻开后，状态文字依次为"牌已全部揭开。"和"可以查看结果了。"；点击"揭示结果"直接进入结果页。
6. 默认问题改为"此刻我最需要留意什么？"。

以上状态文字和默认问题都定义为 `ReleaseUxCopy` 的常量。`BackendOnly` 模式下，`StartReading` 失败时保持现有行为（停止并提示）。

## 7. 结果页与文案

### 7.1 结果页的四种显示

| 状态 | 解读区 | 按钮 |
| --- | --- | --- |
| 生成中（在线，`Pending`） | 状态文字「牌意正在汇聚……」，带呼吸式透明度动画；进入该状态 20 秒后追加「这次解读比平时慢一些，请再稍候。」 | 回到牌桌 |
| 完成（在线，`Ready`） | 淡入显示概要、整体解读、牌面分析、建议和提醒；`modelUsed == "mock_ai"` 时模式标签显示「模拟解读」 | 回到牌桌 |
| 失败（在线，`Failed`） | 状态文字为 `failureMessage` | `canRetry` 为真时显示「重新解读」；「查看离线解读」；回到牌桌 |
| 离线（`Offline`） | 本地解读文本，模式标签显示「离线解读」 | 回到牌桌 |

- 所有状态下牌面都立即亮相，沿用现有揭示动画。
- 状态切换：生成中 → 完成或失败；失败时点「重新解读」回到生成中；点「查看离线解读」切换到离线显示，并且不再切回在线结果。
- 点「回到牌桌」时调用 `InterpretationPoller.Stop()`。
- 离线局的提醒文案改为「这是离线解读，由本地牌义生成，未经过 AI。」。

### 7.2 结果页新增 UI

新增 `Editor/Phase66ResultInterpretationStateBootstrapper.cs`（`public static void Run()`，打开 `Result.unity`，添加对象，标记为已修改并保存）。在解读面板中新增：

- 状态文字（TMP，正文字体，象牙色）
- 模式标签（TMP，小号，金色）
- 「重新解读」和「查看离线解读」两个按钮（复制「回到牌桌」按钮的现有样式）

`ResultPanelPresenter` 新增 `ShowPending`、`ShowReady`、`ShowFailed`、`ShowOffline`，并接收上述对象的序列化引用；`ResultSceneController` 负责订阅轮询器并切换显示。

### 7.3 中文文案对照表

所有文案集中放在 `ReleaseUxCopy`。

| 触发条件 | 阶段 | 文案 | 后续 |
| --- | --- | --- | --- |
| 网络错误 / 超时 / 5xx | 建记录 | 暂时连不上占卜服务，这一局使用离线解读。 | 离线局 |
| 429（带 `Retry-After`） | 建记录 | 今天的访客占卜次数已用完，约 {N} 小时后恢复。这一局使用离线解读。 | 离线局 |
| 401，续期和重新申请会话都失败 | 建记录 | 访客会话连接失败，这一局使用离线解读。 | 离线局 |
| 其他错误（400、404 等） | 建记录 | 在线占卜暂时不可用，这一局使用离线解读。 | 离线局 |
| 后端返回 `failed` | 解读 | 这次解读没有顺利生成，可以再试一次。 | 可重试 |
| 超过 330 秒 | 解读 | 解读花的时间比预期长，可以再试一次。 | 可重试 |
| 连续 3 次网络错误 | 解读 | 与占卜服务的连接中断了，可以再试一次。 | 可重试 |
| 异步接口返回 429 | 解读 | 这一局已经尝试多次仍未成功，先看看离线解读吧。 | 不可重试 |
| 401 且续期失败 | 解读 | 连接已过期，这次解读无法取回。 | 不可重试 |
| 异步接口返回 400 或 404 | 解读 | 这次解读无法生成，先看看离线解读吧。 | 不可重试 |

`{N}` 为 `Retry-After` 秒数除以 3600 后向上取整；不足 3600 秒时显示"不到 1 小时"，缺少该响应头时省略整句"约 {N} 小时后恢复。"。

## 8. 测试与验收

### 8.1 开工前提

- **Unity 编辑器许可证已激活。**2026-09-12 的探测结果为 `No valid Unity Editor license found`（退出码 198）。未激活时后端部分可以先做，Unity 部分暂停。
- **在 `Server/.env` 中填好 `DEEPSEEK_API_KEY` 和 `SECRET_KEY`**（由用户自己完成），只在 8.5 节的真实联调时需要。
- Unity 测试数量的基线，以许可证激活后第一次全量运行 EditMode 和 PlayMode 的结果为准。

### 8.2 后端 pytest

运行环境为 conda `tarot`，命令与 CI 相同：`python -m pytest -q tests` 和 `python -m pytest -q`。

新增 `Server/tests/test_records_async_interpretation.py`：

- 首次调用返回 202；在 TestClient 中后台任务执行完毕后，`GET /records/{id}` 的 `status` 为 `completed` 且带有解读，AI 调用计数为 1
- 已有解读时返回 200 和解读内容，AI 调用计数不增加
- 状态为 `PROCESSING` 且未超时时返回 202，AI 调用计数不增加
- 未抽牌返回 400；访问他人记录返回 404
- AI 抛出超时后状态为 `failed`；再次调用可以重新抢占并成功
- 尝试次数达到上限后返回 429
- `PROCESSING` 超时后可以重新抢占
- 同步接口的现有行为不变（由现有测试保证）

新增迁移测试 `Server/tests/test_migration_0002.py`：全新 SQLite 数据库执行 `alembic upgrade head` 不报错；缺少这两列的 `predictions` 表执行 `0002` 后两列都存在。

在 `test_guest_session.py` 中补充：访客会话通过 cookie + CSRF 调用 `/refresh` 能拿到新令牌，并且新令牌仍然能读取原来的记录。

现有 166 个测试保持通过。

### 8.3 Unity EditMode（`Tests/EditMode/Phase66OnlineInterpretationTests.cs`）

- `ApiRoutes.RecordInterpretAsync(42)` 等于 `/records/42/interpret/async`
- 7.3 表中每一行的文案映射，以及小时数取整规则（3599 秒、3600 秒、3601 秒、缺少响应头）
- 快照新字段的默认值
- `PollDelaySeconds` 的序列（2, 2, 3, 3, 5, 5, …）以及 330 秒总时限常量
- **令牌问题的回归测试**：`ApiClient.Shared` 有会话、序列化引用没有会话时，`BackendReadingService.CanCreateAuthenticatedReading` 为 true
- 守护测试：`Result.unity` 中存在状态文字、模式标签、重试按钮和离线按钮，并已接到 `ResultPanelPresenter` 和 `ResultSceneController`
- 守护测试：6.6 节的流程状态文字和默认问题集中定义为 `ReleaseUxCopy` 的常量，断言这些常量不含 ASCII 字母；并扫描 `ReadingRoomController.cs` 源码，断言以下旧字面量已不存在：`"Shuffling..."`、`"Creating backend reading..."`、`"Dealing cards..."`、`"Click each card to flip it."`、`"The reading is almost ready."`、`"The reading is ready."`、`"What should I notice now?"`（牌阵选择阶段的英文属于子项目 B，不在检查范围内）

### 8.4 Unity PlayMode

把 `Tests/PlayMode/BackendReadingServiceFlowTests.cs` 中的 `MockTarotBackend` 扩展为可以按路由排队返回预设的状态码、响应体、响应头和延迟；把现有测试改为测 `StartReading`；新增以下场景：

1. 正常流程：202 → `processing` ×2 → `completed`，快照为 `Ready`，`predictionId` 和 `modelUsed` 正确
2. 慢响应：`processing` 次数足以超过 15 秒（通过注入的间隔倍率缩短实际耗时），不退回离线，最终 `Ready`
3. `failed` → `Retry()` → 202 → `completed`
4. 建记录返回 429 并带 `Retry-After`，得到离线快照，文案与 7.3 一致
5. 轮询返回 401 → `/refresh` 返回 200 → 继续轮询直到 `Ready`；`/refresh` 返回 401 → `Failed` 且 `canRetry` 为 false
6. 连续 3 次连接错误 → `Failed`；连续 2 次后恢复 → 继续轮询
7. 注入较短的总时限后超时 → `Failed`（超时文案）
8. 调用 `Begin` 的对象（模拟占卜房控制器所在的物体）被销毁后，挂在常驻对象上的轮询器仍继续轮询并发出 `StateChanged`

EditMode 和 PlayMode 全量测试必须保持全绿。

### 8.5 真实联调验收（用户的 DeepSeek Key）

1. 在 conda `tarot` 环境中按 `Server/README.md` 启动后端；Unity 从 `Boot.unity` 运行。
2. 1、3、10 张牌阵各跑一局，确认：
   - 从点击"洗牌抽取"到第一张牌发出，不再等待 AI 生成；
   - 结果页从「生成中」切换为真实的 AI 解读，`modelUsed` 不是 `mock_ai`；
   - 记录每局从进入结果页到解读出现的耗时。
3. 故障演练：
   - 关闭后端后开局，出现 7.3 表中"暂时连不上"的文案，并生成离线局；
   - 解读生成中途关闭后端，出现连接中断的文案；重启后端后点「重新解读」，能够成功。
4. 在 `Docs/VisualReview/Phase66/` 保存生成中、完成、失败、离线四张截图。
5. 成本核对：3 局真实对局中，后端日志显示的 AI 调用次数正好为 3。

### 8.6 完成标准

- 后端全量 pytest 通过。
- Unity EditMode 与 PlayMode 全绿。
- 8.5 全部通过，截图齐全。
- 在线流程中玩家能看到的文字不含原始报文或英文状态。

## 9. 风险与处理

| 风险 | 处理 |
| --- | --- |
| 1.1 节的令牌问题是静态分析结论 | 许可证激活后，先写回归测试和 PlayMode 场景来复现，再动手修复 |
| Unity 许可证无法激活 | 后端部分（第 5 节和 8.2 节）独立完成并提交；Unity 部分等许可证可用后再做 |
| 后台任务与请求会话的关闭时机 | 后台任务只通过会话工厂打开自己的会话（5.6 节） |
| 进程重启导致后台任务丢失 | 状态在 `AI_INTERPRETATION_STALE_SECONDS` 后可以被重新抢占；客户端超时后提示重试 |
| 同步接口与异步接口同时请求同一条记录，可能重复调用 AI | Unity 只使用异步接口；同步接口保持原样，并在代码注释中说明 |
| 真实 DeepSeek 返回格式与本地假设不同 | 8.5 节观察验证；解析失败时沿用现有的纯文本回退逻辑 |
| 轮询带来的请求量 | 单局最多约 70 次轻量 GET（330 秒，间隔 2–5 秒），可以忽略 |

## 10. 本次会新增或修改的文件

### 10.1 后端（`Server/`）

- 修改：`app/models/record.py`、`app/crud/prediction.py`、`app/api/v1/endpoints/records.py`、`app/db/session.py`、`app/core/config.py`、`.env.example`、`app/services/tarot_service.py`、`tests/conftest.py`、`tests/test_guest_session.py`
- 新增：`alembic/versions/0002_interpretation_generation_tracking.py`、`tests/test_records_async_interpretation.py`、`tests/test_migration_0002.py`

### 10.2 Unity（`UnityClient/TarotUnity/Assets/`）

- 修改：`Scripts/Network/ApiClient.cs`、`Scripts/Network/ApiRoutes.cs`、`Scripts/Network/BackendReadingService.cs`、`Scripts/Core/GameBootstrap.cs`、`Scripts/Data/ReadingSessionSnapshot.cs`、`Scripts/Data/ReadingSessionMapper.cs`、`Scripts/UI/ReadingRoomController.cs`、`Scripts/UI/ResultSceneController.cs`、`Scripts/UI/ResultPanelPresenter.cs`、`Scripts/UI/ReleaseUxCopy.cs`、`Scripts/Gameplay/LocalReadingSimulator.cs`、`Scenes/Result.unity`（由 Bootstrapper 修改）、`Tests/PlayMode/BackendReadingServiceFlowTests.cs`
- 新增：`Scripts/Network/ApiError.cs`、`Scripts/Network/InterpretationPoller.cs`、`Editor/Phase66ResultInterpretationStateBootstrapper.cs`、`Tests/EditMode/Phase66OnlineInterpretationTests.cs`，以及对应的 `.meta` 文件

### 10.3 文档

- 新增：`Docs/VisualReview/Phase66/` 下的四张截图
- 修改：`PROJECT_COMPLETION_PLAN.md`（执行记录）

## 11. 建议的实施顺序

1. 后端（第 5 节 + 8.2 节）：不需要 Unity 许可证，可以立即开始。
2. Unity 逻辑（6.1–6.5 节 + 8.3 节中的纯逻辑部分 + 8.4 节）：需要许可证。
3. 占卜房与结果页（6.6 节、第 7 节 + 守护测试）：需要许可证。
4. 真实联调（8.5 节）：需要许可证和用户的 Key。

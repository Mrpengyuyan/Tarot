# 在线解读闭环 · 计划 2：Unity 客户端与真实联调 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 Unity 真正发起在线占卜：洗牌时建记录并抽牌，拿到牌就发牌；AI 解读在后端后台生成，由常驻轮询器取回；结果页按「生成中 / 完成 / 失败 / 离线」四种状态显示，玩家看到的文字全部是中文；最后用用户自己的 DeepSeek Key 做真实联调。

**Architecture:** 先修令牌问题（`ApiClient.Shared`），再加结构化错误 `ApiError` 和五个结构化请求。`BackendReadingService.StartReading` 只做「建记录 → 抽牌 → 取牌」，不等 AI。`InterpretationPoller` 挂在 Boot 常驻对象上：它调用异步解读接口、轮询记录详情，原地更新 `ReadingSessionStore.Current`，并发出 `StateChanged`。占卜房和结果页订阅这个事件。结果页新增的 UI 由 Phase 66 Bootstrapper 写入 `Result.unity`。

**Tech Stack:** Unity 6000.3.16f1（URP、TextMeshPro、Unity Test Framework 的 NUnit EditMode/PlayMode）；C#（`UnityWebRequest`、`JsonUtility`，测试桩用 `HttpListener`）。后端沿用计划 1：FastAPI，运行在 conda `tarot` 环境（Python 3.11.15）。

**Spec:** `docs/superpowers/specs/2026-09-12-online-interpretation-loop-design.md`，本计划实现第 6、7 节和 8.3–8.6 节。后端接口约定见 `docs/superpowers/plans/2026-09-12-online-interpretation-backend.md` 末尾的「计划 2 须知」。

## Global Constraints

- 仓库：`/Users/maochuandou/BUPT/Game/UnityTarot`。分支：`feat/online-interpretation-unity`，从 `feat/online-interpretation-loop`@`0fdf8e4`（PR #1，尚未合并）拉出。Unity 工程在 `UnityClient/TarotUnity`，下文路径 `Assets/…`、`Docs/…` 都相对这个目录。
- `$S` = `/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad`。每个 bash 代码块开头都要重新设置 `S=...`，因为 shell 状态不会保留。
- Unity 可执行文件：`/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity`。
  - Unity 测试一律通过 `bash "$S/online-b-run/ut.sh" <EditMode|PlayMode> <新标签> [-testFilter …]` 运行（Task 0 创建）。
  - 执行 Bootstrapper 用 `-batchmode -nographics -executeMethod … -quit`。
  - 截图 Builder 必须去掉 `-nographics`。
- **许可证是硬前提。**Task 0 的探测退出码不是 0 → STOP。
- 已有公开签名保持不变：`ApiClient` 里所有 `Action<string>` 回调的方法，以及 `ReleaseUxCopy.LocalModeReady`、`BackendFallback`、`BackendOnlyFailure`。**不修改 `ReadingRoom.unity`**（Phase4 测试断言了它的引用）。
- 后端接口约定（计划 1 已实现并测试）：
  - `POST /api/v1/records/{id}/interpret/async`：
    - 202：`{"prediction_id": id, "status": "processing"}`
    - 200：响应体与 `InterpretationResponse` 结构相同
    - 400：尚未抽牌
    - 404：`detail="Record not found"`
    - 429：`detail="Interpretation attempts exhausted"`
  - 建记录 `POST /api/v1/records/` 的 429：`detail="Guest daily reading limit reached. Please try again tomorrow."`，并带 `Retry-After` 响应头。
  - 续期：`POST /api/v1/refresh`，需要带 cookie 和 `X-CSRF-Token`。
- 轮询规则：
  - 间隔依次为 2、2、3、3 秒，之后每次 5 秒。
  - 总时限 330 秒（后端 `AI_INTERPRETATION_STALE_SECONDS` 300 秒 + 30 秒）。
  - 连续 3 次网络错误进入失败。
  - 遇到 401 续期一次。
- **记录详情里有解读就算完成，不管 `status` 是什么。**"有解读"指 `interpretation.id > 0` 或 `overall_interpretation` 非空（见偏差 U2）。
- 玩家看到的文案全部来自 `ReleaseUxCopy`，逐字使用 spec 7.1、7.3 和 6.6 的原文。原始报文只写进 `Debug.Log`。
- 新 TMP 文字的字体（Phase24 规则）：
  - 字号小于 30 且不在 Button 下：`Assets/Fonts/LXGWWenKai-Regular SDF.asset`
  - Button 内的文字：`Assets/Fonts/LXGWWenKai-Medium SDF.asset`
- Editor 代码里判断 `GetComponent<T>()` 的结果时，一律写 `== null`，不要用 `??` 或 `?.`，因为编辑器可能返回"假 null"对象。
- DeepSeek Key 只存在于用户自己填写的 `Server/.env`。不得读取、打印或复制 Key；检查配置时只打印布尔值。
- 不得向 conda `tarot` 环境安装、升级或卸载任何包。后端 pytest 仍然用 `bash "$S/online-a-run/pt.sh"` 运行。
- 禁止删除任何文件。
  - 新建文件前先确认它不存在；heredoc 之前执行 `set -C`。
  - 截图、日志、测试结果都写到新的文件名，不覆盖已有文件（`ut.sh` 遇到同名文件会 STOP）。
  - Unity 生成的 `.meta` 文件与对应文件一起提交。
- 跑测试或截图后，TMP 动态字体图集（`Assets/Fonts/*SDF*.asset`）可能出现增量改动：不提交、不还原，在任务汇报中列出。
- 每个任务结束时提交一次，只 `git add` 本任务列出的文件。提交信息必须以下面两行结尾：
  ```
  Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
  ```
  **禁止 `git push`**（推送前需要用户同意）。
- zsh 注意事项：变量后面紧跟冒号时写成 `${VAR}:`；路径列表用数组；复杂脚本写成文件后用 `bash` 运行。
- 遇到 **STOP**：立即停止，把输出原样报告给用户。

## 与 spec 的偏差

| 编号 | spec 的写法 | 本计划的做法 | 原因 |
| --- | --- | --- | --- |
| U1 | 6.5：轮询器调用 `RequestInterpretationAsync` | 轮询器直接调用 `ApiClient` 的结构化方法。`BackendReadingService.RequestInterpretationAsync` 和 `GetRecord` 仍按 6.4 提供，只是对这两个方法的薄封装 | `BackendReadingService` 挂在 ReadingRoom 场景里，进入结果页时就会被销毁；轮询器只能依赖常驻对象 |
| U2 | "`interpretation` 不为空即完成" | 判定函数 `ReadingSessionMapper.HasInterpretation`：`id > 0` 或 `overall_interpretation` 非空。EditMode 测试用包含 `"interpretation": null` 的真实 JSON 固定这个行为 | `JsonUtility` 可能把 JSON 里的 `null` 反序列化成一个空对象而不是 null；如果只判断是否为 null，可能在还没有解读时就当成已完成 |
| U3 | 6.6 没有校验牌阵 | 发起在线占卜前，先确保后端牌阵已加载且存在张数相同的牌阵，否则转离线（新文案 `OfflineBecauseSpread`）。`StartReading` 返回的牌数与所选张数不一致时，也转离线（使用"在线占卜暂时不可用"那一行文案） | 后端牌阵未加载时，凯尔特十字会发送本地 id 3，而后端的 3 号是 5 张的爱情牌阵。流程不会卡住，但会发错牌阵 |
| U4 | `InterpretationState { Pending, Ready, Failed }` | 成员顺序改为 `{ Ready, Pending, Failed }`，字段初始值仍为 `Ready` | 让 `default(InterpretationState)` 也是 `Ready` |
| U5 | 8.3 列出的旧英文字面量 | 额外替换并守护 `"Backend spreads loaded."`：改为在底部状态栏显示 `OnlineReady`。`"Dealing cards..."` 改为 `FlowDealing`（6.6 没有给发牌阶段的文案） | 8.6 要求在线流程中没有英文状态 |
| U6 | 6.4 没有给结构化请求命名 | 新增 `PostRecord`、`PostDraw`、`FetchRecordCards`、`PostInterpretAsync`、`FetchRecordDetail`，不与已有方法重载 | lambda 回调在 `Action<string>` 和 `Action<ApiError>` 两个重载之间会产生二义性；已有签名保持不变（6.2） |
| U7 | 6.6 第 2 步把 401 处理写在控制器流程里 | 放进 `BackendReadingService.RecoverSession(Action<bool>)`（先续期，失败则清空会话并申请新访客），控制器只调用它 | 可以在 PlayMode 中单独测试 |
| U8 | 8.4 只修改 `BackendReadingServiceFlowTests.cs` | 把 `MockTarotBackend` 抽到 `Tests/PlayMode/MockTarotBackend.cs`；新增 `Phase66InterpretationPollerTests.cs`、`Phase66ReadingRoomOnlineFlowTests.cs`、`Phase66ResultInterpretationStateTests.cs` | 多个测试文件要共用同一个测试桩；场景级流程需要单独覆盖 |
| U9 | 8.5：人工从 `Boot.unity` 运行 | 新增 PlayMode 测试 `Phase66LiveBackendTests`，只在 `TAROT_LIVE_BACKEND=1` 时运行，由它驱动 Boot → 菜单 → 占卜房 → 结果页；`live.sh` 负责启停后端。另外建议用户亲手玩一局，但不作为验收的阻塞项 | 执行者无法在编辑器界面里点击；自动化还能精确计时，并且可以重复运行 |
| U10 | 8.5.4：四张截图 | 由 `Editor/Phase66InterpretationStateCaptureBuilder.cs` 在真实的 `Result.unity` 布局上渲染四种状态（使用示例文本） | 以 `-nographics` 批处理运行的联调测试不渲染画面 |
| U11 | 8.5.3："解读生成中途关闭后端，重启后点「重新解读」能成功" | 验收改为：先出现连接中断的文案；重启后端后点「重新解读」，**最多重试两次**就到达完成（第一次重试可能以超时结束），并记录耗时 | 后端进程被杀掉时，记录会停在 `processing`，要过 300 秒才能被重新抢占，在此之前客户端只会看到 `processing`。spec 第 9 节风险表已经接受这一点。"后端启动时回收这类孤儿记录"留给用户决定 |
| U12 | 8.5.5：从后端日志统计 AI 调用次数 | 以数据库中 3 局的 `predictions.interpretation_attempts` 之和为准（应为 3） | 每次抢占成功才会调用一次 AI，计数列比日志文本可靠 |
| U13 | 10.2 的文件清单 | 另外新增 `Editor/Phase66InterpretationStateCaptureBuilder.cs`、`Docs/PHASE66_ONLINE_INTERPRETATION.md`、`Tests/PlayMode/Phase66LiveBackendTests.cs` 以及 U8 中的测试文件；另外修改 `Scripts/Data/ApiContracts.cs` 和 `Docs/PROJECT_CHRONICLE.md` | 沿用 Phase 6x 的截图和文档惯例（文档存在性测试与 `Phase64ResultBackdropTests` 相同） |

**不在本计划范围内**（其他子项目或后续决策）：
- 选牌阵阶段的英文：`"Choose a spread, ask a question, then draw."`、`"One Card Focus"`、`"Past / Present / Advice"`、`spreadStatusText` 里的 `card(s)`，属于子项目 B。
- 访客额度在重启后重置。
- 后端启动时回收孤儿记录。
- 部署和 HTTPS。

## 文件结构

| 文件 | 操作 | 职责 | 任务 |
| --- | --- | --- | --- |
| `Assets/Scripts/Network/ApiClient.cs` | 修改 | 新增 `Shared`；新增结构化请求 | 1、4 |
| `Assets/Scripts/Network/BackendReadingService.cs` | 修改 | 先取 `Shared`；新增 `StartReading`、`RecoverSession`、`RequestInterpretationAsync`、`GetRecord`；删除 `CompleteReading` | 1、4 |
| `Assets/Scripts/Core/GameBootstrap.cs` | 修改 | 设置 `Shared`；创建 `InterpretationPoller` | 1、5 |
| `Assets/Scripts/UI/ReadingRoomController.cs` | 修改 | 先取 `Shared`；改为新流程 | 1、6 |
| `Assets/Scripts/Network/ApiError.cs` | 新增 | 结构化错误与归类 | 2 |
| `Assets/Scripts/Network/ApiRoutes.cs` | 修改 | 新增 `RecordInterpretAsync` | 2 |
| `Assets/Scripts/Data/ReadingSessionSnapshot.cs` | 修改 | 三个枚举和六个新字段 | 2 |
| `Assets/Scripts/UI/ReleaseUxCopy.cs` | 修改 | 中文文案表与映射函数 | 2 |
| `Assets/Scripts/Gameplay/LocalReadingSimulator.cs` | 修改 | 离线提醒文案 | 2 |
| `Assets/Scripts/Data/ReadingSessionMapper.cs` | 修改 | `FromBackendStart`、`HasInterpretation`、`ApplyInterpretation` | 3 |
| `Assets/Scripts/Data/ApiContracts.cs` | 修改 | `AsyncInterpretationOutcome` 和 `AsyncInterpretationResult` | 4 |
| `Assets/Scripts/Network/InterpretationPoller.cs` | 新增 | 常驻轮询器 | 5 |
| `Assets/Scripts/UI/ResultPanelPresenter.cs` | 修改 | `ShowPending`、`ShowReady`、`ShowFailed`、`ShowOffline` | 7 |
| `Assets/Scripts/UI/ResultSceneController.cs` | 修改 | 订阅轮询器；重试和离线按钮 | 7 |
| `Assets/Editor/Phase66ResultInterpretationStateBootstrapper.cs` | 新增 | 在结果页添加状态文字、模式标签和两个按钮，并连好引用 | 7 |
| `Assets/Scenes/Result.unity` | 由 Bootstrapper 修改 | 场景数据 | 7 |
| `Assets/Editor/Phase66InterpretationStateCaptureBuilder.cs` | 新增 | 四种状态的截图 | 8 |
| `Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs` | 新增，之后各任务追加测试 | EditMode 测试 | 1–8 |
| `Assets/Tests/PlayMode/MockTarotBackend.cs` | 新增 | 可编排的 HTTP 测试桩 | 4 |
| `Assets/Tests/PlayMode/BackendReadingServiceFlowTests.cs` | 重写 | `StartReading`、错误归类的对照测试、会话恢复 | 4 |
| `Assets/Tests/PlayMode/Phase66InterpretationPollerTests.cs` | 新增 | 轮询器场景 | 5 |
| `Assets/Tests/PlayMode/Phase66ReadingRoomOnlineFlowTests.cs` | 新增 | 占卜房在线流程 | 6 |
| `Assets/Tests/PlayMode/Phase66ResultInterpretationStateTests.cs` | 新增 | 结果页状态切换 | 7 |
| `Assets/Tests/PlayMode/Phase66LiveBackendTests.cs` | 新增 | 真实后端联调（默认忽略） | 9 |
| `Docs/PHASE66_ONLINE_INTERPRETATION.md` | 新增 | 阶段文档 | 8 |
| `Docs/PROJECT_CHRONICLE.md` | 修改 | 追加 Phase 66 段落 | 8 |
| `Docs/VisualReview/Phase66/*.png` | 新增 4 张 | 视觉审查截图 | 8 |
| `PROJECT_COMPLETION_PLAN.md`（仓库根目录） | 修改 | 执行记录 | 9 |

scratch 目录（不入库）：
- `$S/online-b-run/`：`ut.sh`、`summarize.py`、`results/`
- `$S/online-b-live/`：`live.sh`、`live.db`、uvicorn 日志、`markers/`、`report.txt`

测试数量预期见文末「测试数量总表」。

---

### Task 0: 许可证、基线与测试脚本

**Files:**
- Create（scratch，不入库）：`$S/online-b-run/ut.sh`、`$S/online-b-run/summarize.py`

**Interfaces:**
- Produces：`bash "$S/online-b-run/ut.sh" <EditMode|PlayMode> <tag> [Unity 参数…]`，输出一行 `total=… passed=… failed=… skipped=…`，并逐条列出失败用例；基线数量 `E0`（EditMode）和 `P0`（PlayMode）。

- [ ] **Step 1: 确认分支和工作区**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git branch --show-current
git status --short
git merge-base --is-ancestor 0fdf8e4 HEAD && echo stacked-ok
```

预期：`feat/online-interpretation-unity`，工作区为空，并输出 `stacked-ok`。否则 STOP。

- [ ] **Step 2: 确认没有 Unity 进程占用工程**

```bash
pgrep -fl 'Unity.app/Contents/MacOS/Unity' || echo no-unity-running
```

预期：`no-unity-running`。否则 STOP，请用户先关闭 Unity 编辑器。残留的 `Temp/UnityLockfile` 不影响批处理，不要删除它。

- [ ] **Step 3: 许可证探测**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
UNITY=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
mkdir -p "$S/online-b-run/results"
PROBE="$S/online-b-run/license-probe.log"
if [ -e "$PROBE" ]; then echo "STOP: $PROBE exists, use license-probe-2.log"; else
  "$UNITY" -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
    -batchmode -nographics -enableUnityConnectPrefs false -quit -logFile "$PROBE"
  echo "exit=$?"
  grep -n 'No valid Unity Editor license' "$PROBE" || echo license-ok
fi
```

预期：`exit=0` 和 `license-ok`。如果退出码是 198，或者出现 `No valid Unity Editor license`，STOP：请用户在 Unity Hub 里激活许可证。重新探测时换用新的日志文件名。

- [ ] **Step 4: 创建测试脚本**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
set -C
cat > "$S/online-b-run/ut.sh" <<'EOF'
#!/bin/bash
# Phase 66 Unity test runner: ut.sh <EditMode|PlayMode> <tag> [extra Unity args]
set -u
if [ $# -lt 2 ]; then echo "usage: ut.sh <EditMode|PlayMode> <tag> [args]"; exit 64; fi
PLATFORM="$1"; TAG="$2"; shift 2
UNITY=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
PROJECT=/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
HERE="$(cd "$(dirname "$0")" && pwd)"
OUT="$HERE/results"
mkdir -p "$OUT"
XML="$OUT/$TAG-$PLATFORM.xml"
LOG="$OUT/$TAG-$PLATFORM.log"
if [ -e "$XML" ] || [ -e "$LOG" ]; then
  echo "STOP: $XML or $LOG already exists - use a new tag"
  exit 65
fi
if pgrep -f 'Unity.app/Contents/MacOS/Unity' >/dev/null; then
  echo "STOP: another Unity process is running"
  exit 66
fi
"$UNITY" -projectPath "$PROJECT" -batchmode -nographics -enableUnityConnectPrefs false \
  -runTests -testPlatform "$PLATFORM" -testResults "$XML" -logFile "$LOG" "$@"
CODE=$?
echo "unity exit=$CODE"
/opt/miniconda3/envs/tarot/bin/python "$HERE/summarize.py" "$XML" "$LOG"
exit $CODE
EOF
cat > "$S/online-b-run/summarize.py" <<'EOF'
"""Summarize a Unity Test Framework NUnit3 result file (Phase 66 runner)."""
import os
import re
import sys
import xml.etree.ElementTree as ET

xml_path, log_path = sys.argv[1], sys.argv[2]
if not os.path.exists(xml_path):
    print("NO RESULTS XML - Unity did not run the tests")
    if os.path.exists(log_path):
        with open(log_path, encoding="utf-8", errors="replace") as handle:
            lines = handle.read().splitlines()
        pattern = re.compile(
            r"error CS\d+|No valid Unity Editor license|Aborting batchmode|Scripts have compiler errors"
        )
        for line in [item for item in lines if pattern.search(item)][-40:]:
            print("  " + line)
    sys.exit(2)

root = ET.parse(xml_path).getroot()
keys = ("total", "passed", "failed", "skipped", "inconclusive", "result")
print(" ".join(f"{key}={root.attrib.get(key, '?')}" for key in keys))
for case in root.iter("test-case"):
    if case.attrib.get("result") == "Failed":
        print("FAILED: " + case.attrib.get("fullname", "?"))
        message = case.find("failure/message")
        if message is not None and message.text:
            print("    " + message.text.strip().splitlines()[0][:400])
EOF
set +C
ls -l "$S/online-b-run/"
```

预期：列出 `ut.sh`、`summarize.py` 和 `results/`。如果 `set -C` 报 `cannot overwrite existing file`，STOP。

- [ ] **Step 5: 跑两组基线**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t0
bash "$S/online-b-run/ut.sh" PlayMode t0
cd /Users/maochuandou/BUPT/Game/UnityTarot && git status --short
```

预期：两组都是 `failed=0`，把两组的 `total` 分别记为 `E0` 和 `P0`（上一次记录是 308 和 33）。有任何失败就 STOP，基线不绿时不开工。`git status` 如果出现字体图集改动，记下来，不要处理。

本任务不提交（只创建了 scratch 文件）。

### Task 1: 令牌问题回归测试与 `ApiClient.Shared`

**Files:**
- Create: `Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs`
- Modify: `Assets/Scripts/Network/ApiClient.cs`（私有字段区；`SetAccessToken` 之前）
- Modify: `Assets/Scripts/Network/BackendReadingService.cs:12-23`（`Client` 属性）
- Modify: `Assets/Scripts/Core/GameBootstrap.cs:17`
- Modify: `Assets/Scripts/UI/ReadingRoomController.cs:379-385`（`EnsureBackendReferences` 开头）

**Interfaces:**
- Produces：
  - `public static ApiClient ApiClient.Shared { get; }`、`public static void ApiClient.SetShared(ApiClient client)`、`public static void ApiClient.ClearShared()`
  - `BackendReadingService.Client` 按 `Shared` → 序列化引用 → `FindFirstObjectByType` 的顺序取客户端
  - 测试类 `TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests`；后续任务都在锚点注释 `// Phase 66: later tasks append tests above this line.` 之前追加测试

- [ ] **Step 1: 写失败的测试**

确认文件不存在（`ls Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs` 应报 No such file），然后创建：

```csharp
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 66: the online interpretation loop - the shared guest session, structured
    /// API errors, Chinese copy, snapshot state, the poller contract, and the Result
    /// screen's interpretation-state UI.
    /// </summary>
    public sealed class Phase66OnlineInterpretationTests
    {
        [TearDown]
        public void ClearSharedClient()
        {
            ApiClient.ClearShared();
        }

        [Test]
        public void ReadingServicePrefersTheSharedSessionOverTheSceneClient()
        {
            var boot = new GameObject("Phase66_BootClient");
            var room = new GameObject("Phase66_RoomServices");
            try
            {
                var shared = boot.AddComponent<ApiClient>();
                shared.SetAccessToken("guest-token");
                ApiClient.SetShared(shared);

                var sceneClient = room.AddComponent<ApiClient>();
                var service = room.AddComponent<BackendReadingService>();
                var serialized = new SerializedObject(service);
                serialized.FindProperty("apiClient").objectReferenceValue = sceneClient;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(sceneClient.HasSession, Is.False, "control: the scene client has no token");
                Assert.That(service.Client, Is.SameAs(shared));
                Assert.That(service.CanCreateAuthenticatedReading, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(room);
                Object.DestroyImmediate(boot);
            }
        }

        [Test]
        public void ReadingServiceFallsBackToItsSerializedClientWithoutShared()
        {
            var room = new GameObject("Phase66_RoomServices");
            try
            {
                var sceneClient = room.AddComponent<ApiClient>();
                var service = room.AddComponent<BackendReadingService>();
                var serialized = new SerializedObject(service);
                serialized.FindProperty("apiClient").objectReferenceValue = sceneClient;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(ApiClient.Shared, Is.Null, "control: no shared client in this test");
                Assert.That(service.Client, Is.SameAs(sceneClient));
                Assert.That(service.CanCreateAuthenticatedReading, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(room);
            }
        }

        // Phase 66: later tasks append tests above this line.
    }
}
```

- [ ] **Step 2: 运行，确认失败**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t1-red -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
```

预期：`NO RESULTS XML`，并列出 `error CS0117`，内容为 `'ApiClient' does not contain a definition for 'SetShared'`（或 `ClearShared`）。

- [ ] **Step 3: 实现**

`Assets/Scripts/Network/ApiClient.cs`，用 Edit 把

```csharp
        private string accessToken;
        private string cookieHeader;
        private string csrfToken;
```

替换为

```csharp
        private string accessToken;
        private string cookieHeader;
        private string csrfToken;

        // Phase 66: the guest session is opened on the persistent Boot ApiClient,
        // but ReadingRoom carries its own scene ApiClient that never gets a token.
        // Scene code reads Shared first so a reading uses the session the menu
        // actually opened; without Boot (a scene run directly) Shared stays null.
        public static ApiClient Shared { get; private set; }

        public static void SetShared(ApiClient client)
        {
            Shared = client;
        }

        public static void ClearShared()
        {
            Shared = null;
        }
```

再把

```csharp
        public void SetAccessToken(string token)
```

替换为

```csharp
        private void OnDestroy()
        {
            if (Shared == this)
            {
                Shared = null;
            }
        }

        public void SetAccessToken(string token)
```

`Assets/Scripts/Network/BackendReadingService.cs`，把

```csharp
        public ApiClient Client
        {
            get
            {
                if (apiClient == null)
```

替换为

```csharp
        // Phase 66: prefer the Boot client that holds the guest session (see
        // ApiClient.Shared); the scene reference is only a fallback.
        public ApiClient Client
        {
            get
            {
                if (ApiClient.Shared != null)
                {
                    return ApiClient.Shared;
                }

                if (apiClient == null)
```

`Assets/Scripts/Core/GameBootstrap.cs`，把

```csharp
            EnsureService<ApiClient>();
```

替换为

```csharp
            EnsureService<ApiClient>();
            ApiClient.SetShared(GetComponent<ApiClient>());
```

`Assets/Scripts/UI/ReadingRoomController.cs`，把

```csharp
        private void EnsureBackendReferences()
        {
            if (apiClient == null)
            {
```

替换为

```csharp
        private void EnsureBackendReferences()
        {
            if (ApiClient.Shared != null)
            {
                apiClient = ApiClient.Shared;
            }
            else if (apiClient == null)
            {
```

- [ ] **Step 4: 运行，确认通过**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t1-green -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
```

预期：`total=2 passed=2 failed=0`。

- [ ] **Step 5: 全量回归**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t1
bash "$S/online-b-run/ut.sh" PlayMode t1
```

预期：EditMode `total=E0+2 failed=0`；PlayMode `total=P0 failed=0`。

- [ ] **Step 6: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
ls Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs.meta
git add Assets/Scripts/Network/ApiClient.cs Assets/Scripts/Network/BackendReadingService.cs \
  Assets/Scripts/Core/GameBootstrap.cs Assets/Scripts/UI/ReadingRoomController.cs \
  Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs.meta
git commit -F - <<'EOF'
fix(unity): read the shared guest session in the reading room

The guest token is issued to the persistent Boot ApiClient, but the
reading room used its own scene ApiClient, so online readings never
started. Scene code now reads ApiClient.Shared first.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
```

`.meta` 不存在时 STOP（说明 Unity 没有导入这个文件）。

---

### Task 2: `ApiError`、路由、快照状态与中文文案表

**Files:**
- Create: `Assets/Scripts/Network/ApiError.cs`
- Modify: `Assets/Scripts/Network/ApiRoutes.cs:34-37`
- Modify: `Assets/Scripts/Data/ReadingSessionSnapshot.cs`
- Modify: `Assets/Scripts/UI/ReleaseUxCopy.cs`
- Modify: `Assets/Scripts/Gameplay/LocalReadingSimulator.cs:4`、`:106`
- Test: `Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs`（追加）

**Interfaces:**
- Consumes：Task 1 的测试类和锚点注释。
- Produces：
  - `TarotUnity.Network.ApiErrorKind { Network, Timeout, Unauthorized, NotFound, BadRequest, RateLimited, Server, Unexpected }`
  - `TarotUnity.Network.ApiError`：
    - 构造函数 `ApiError(long statusCode, ApiErrorKind kind, int retryAfterSeconds, string rawMessage)`
    - 属性 `long StatusCode`、`ApiErrorKind Kind`、`int RetryAfterSeconds`、`string RawMessage`、`bool IsTransient`（Network、Timeout、Server 三种为 true）
    - 静态方法 `FromResponse(long statusCode, string requestError, string body, string retryAfterHeader)`、`Local(string message)`、`Classify(long statusCode, string requestError)`、`ParseRetryAfter(string header)`
  - `ApiRoutes.RecordInterpretAsync(int predictionId)` → `/records/{id}/interpret/async`
  - `TarotUnity.Data` 命名空间：
    - `ReadingSource { Offline, Online }`
    - `InterpretationState { Ready, Pending, Failed }`
    - `InterpretationFailure { BackendFailed, TimedOut, ConnectionLost, AttemptsExhausted, SessionExpired, Unavailable }`
  - `ReadingSessionSnapshot` 新字段：`int predictionId`、`ReadingSource source = Offline`、`InterpretationState interpretationState = Ready`、`string modelUsed = ""`、`string failureMessage = ""`、`bool canRetry`
  - `ReleaseUxCopy` 常量：`DefaultQuestion`、`FlowShuffling`、`FlowDealing`、`FlowFlipPrompt`、`FlowAllRevealed`、`FlowResultReady`、`InterpretationGenerating`、`InterpretationReadyHint`、`OnlineReady`、`OfflineBecauseNetwork`、`OfflineBecauseSession`、`OfflineBecauseUnavailable`、`OfflineBecauseSpread`、`InterpretationBackendFailed`、`InterpretationTimedOut`、`InterpretationConnectionLost`、`InterpretationAttemptsExhausted`、`InterpretationSessionExpired`、`InterpretationUnavailable`、`ResultPending`、`ResultPendingSlow`、`ModeOffline`、`ModeMock`、`RetryButtonLabel`、`OfflineButtonLabel`、`OfflineWarning`
  - `ReleaseUxCopy` 方法：`string GuestQuotaExhausted(int retryAfterSeconds)`、`string ForStartReadingFailure(ApiError error)`、`string ForInterpretationFailure(InterpretationFailure failure)`、`bool CanRetry(InterpretationFailure failure)`

- [ ] **Step 1: 追加失败的测试**

在测试文件中，用 Edit 把 `        // Phase 66: later tasks append tests above this line.` 替换为：

```csharp
        [Test]
        public void AsyncInterpretRouteMatchesBackendContract()
        {
            Assert.That(ApiRoutes.RecordInterpretAsync(42), Is.EqualTo("/records/42/interpret/async"));
        }

        [TestCase(401L, "", ApiErrorKind.Unauthorized)]
        [TestCase(404L, "", ApiErrorKind.NotFound)]
        [TestCase(400L, "", ApiErrorKind.BadRequest)]
        [TestCase(422L, "", ApiErrorKind.BadRequest)]
        [TestCase(429L, "", ApiErrorKind.RateLimited)]
        [TestCase(500L, "", ApiErrorKind.Server)]
        [TestCase(503L, "", ApiErrorKind.Server)]
        [TestCase(0L, "Request timeout", ApiErrorKind.Timeout)]
        [TestCase(0L, "Cannot connect to destination host", ApiErrorKind.Network)]
        [TestCase(403L, "", ApiErrorKind.Unexpected)]
        public void ApiErrorKindFollowsStatusCode(long statusCode, string requestError, ApiErrorKind expected)
        {
            Assert.That(ApiError.Classify(statusCode, requestError), Is.EqualTo(expected));
        }

        [TestCase("5400", 5400)]
        [TestCase(" 60 ", 60)]
        [TestCase(null, -1)]
        [TestCase("", -1)]
        [TestCase("Wed, 21 Oct 2026 07:28:00 GMT", -1)]
        public void RetryAfterParsesSecondsOrMinusOne(string header, int expected)
        {
            Assert.That(ApiError.ParseRetryAfter(header), Is.EqualTo(expected));
        }

        [Test]
        public void ApiErrorKeepsTheRawBodyOutOfPlayerCopy()
        {
            var error = ApiError.FromResponse(
                429,
                "HTTP/1.1 429 Too Many Requests",
                "{\"detail\":\"Guest daily reading limit reached. Please try again tomorrow.\"}",
                "3601");

            Assert.That(error.Kind, Is.EqualTo(ApiErrorKind.RateLimited));
            Assert.That(error.RetryAfterSeconds, Is.EqualTo(3601));
            Assert.That(error.RawMessage, Does.Contain("Guest daily reading limit"), "control: the raw body is kept for logs");

            var copy = ReleaseUxCopy.ForStartReadingFailure(error);
            Assert.That(copy, Is.EqualTo("今天的访客占卜次数已用完，约 2 小时后恢复。这一局使用离线解读。"));
            Assert.That(copy, Does.Not.Contain("Guest"));
        }

        [TestCase(3599, "今天的访客占卜次数已用完，不到 1 小时后恢复。这一局使用离线解读。")]
        [TestCase(3600, "今天的访客占卜次数已用完，约 1 小时后恢复。这一局使用离线解读。")]
        [TestCase(3601, "今天的访客占卜次数已用完，约 2 小时后恢复。这一局使用离线解读。")]
        [TestCase(-1, "今天的访客占卜次数已用完，这一局使用离线解读。")]
        public void GuestQuotaCopyRoundsHoursUp(int retryAfterSeconds, string expected)
        {
            Assert.That(ReleaseUxCopy.GuestQuotaExhausted(retryAfterSeconds), Is.EqualTo(expected));
        }

        [TestCase(0L, "Cannot connect to destination host", "暂时连不上占卜服务，这一局使用离线解读。")]
        [TestCase(0L, "Request timeout", "暂时连不上占卜服务，这一局使用离线解读。")]
        [TestCase(502L, "", "暂时连不上占卜服务，这一局使用离线解读。")]
        [TestCase(401L, "", "访客会话连接失败，这一局使用离线解读。")]
        [TestCase(400L, "", "在线占卜暂时不可用，这一局使用离线解读。")]
        [TestCase(404L, "", "在线占卜暂时不可用，这一局使用离线解读。")]
        public void StartReadingFailuresMapToOfflineCopy(long statusCode, string requestError, string expected)
        {
            var error = ApiError.FromResponse(statusCode, requestError, string.Empty, null);
            Assert.That(ReleaseUxCopy.ForStartReadingFailure(error), Is.EqualTo(expected));
        }

        [TestCase(InterpretationFailure.BackendFailed, "这次解读没有顺利生成，可以再试一次。", true)]
        [TestCase(InterpretationFailure.TimedOut, "解读花的时间比预期长，可以再试一次。", true)]
        [TestCase(InterpretationFailure.ConnectionLost, "与占卜服务的连接中断了，可以再试一次。", true)]
        [TestCase(InterpretationFailure.AttemptsExhausted, "这一局已经尝试多次仍未成功，先看看离线解读吧。", false)]
        [TestCase(InterpretationFailure.SessionExpired, "连接已过期，这次解读无法取回。", false)]
        [TestCase(InterpretationFailure.Unavailable, "这次解读无法生成，先看看离线解读吧。", false)]
        public void InterpretationFailuresMapToCopyAndRetryability(InterpretationFailure failure, string expected, bool canRetry)
        {
            Assert.That(ReleaseUxCopy.ForInterpretationFailure(failure), Is.EqualTo(expected));
            Assert.That(ReleaseUxCopy.CanRetry(failure), Is.EqualTo(canRetry));
        }

        [Test]
        public void SnapshotDefaultsDescribeAnOfflineReadyReading()
        {
            var snapshot = new ReadingSessionSnapshot();

            Assert.That(snapshot.predictionId, Is.EqualTo(0));
            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(snapshot.modelUsed, Is.Empty);
            Assert.That(snapshot.failureMessage, Is.Empty);
            Assert.That(snapshot.canRetry, Is.False);
            Assert.That(default(InterpretationState), Is.EqualTo(InterpretationState.Ready), "the enum default must be Ready too");
        }

        [Test]
        public void OfflineSessionsCarryTheOfflineWarning()
        {
            var session = LocalReadingSimulator.CreateSession(
                1, "单张牌", "问题？", "general", LocalReadingSimulator.CreatePlaceholderDraws(1));

            Assert.That(session.warning, Is.EqualTo("这是离线解读，由本地牌义生成，未经过 AI。"));
            Assert.That(session.source, Is.EqualTo(ReadingSource.Offline));
        }

        // Phase 66: later tasks append tests above this line.
```

- [ ] **Step 2: 运行，确认失败**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t2-red -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
```

预期：`NO RESULTS XML`，列出 `error CS0246`（找不到 `ApiErrorKind`、`ApiError`、`InterpretationFailure`）或 `error CS0117`（`RecordInterpretAsync`）。

- [ ] **Step 3: 新建 `ApiError.cs`**

确认 `Assets/Scripts/Network/ApiError.cs` 不存在，然后创建：

```csharp
using System;
using System.Globalization;

namespace TarotUnity.Network
{
    public enum ApiErrorKind
    {
        Network,
        Timeout,
        Unauthorized,
        NotFound,
        BadRequest,
        RateLimited,
        Server,
        Unexpected,
    }

    // Phase 66: a structured request failure for the online interpretation flow.
    // Kind drives the player-facing ReleaseUxCopy line; RawMessage is only ever
    // written to the log, never shown to the player.
    public sealed class ApiError
    {
        public ApiError(long statusCode, ApiErrorKind kind, int retryAfterSeconds, string rawMessage)
        {
            StatusCode = statusCode;
            Kind = kind;
            RetryAfterSeconds = retryAfterSeconds;
            RawMessage = rawMessage ?? string.Empty;
        }

        public long StatusCode { get; }
        public ApiErrorKind Kind { get; }
        public int RetryAfterSeconds { get; }
        public string RawMessage { get; }

        public bool IsTransient =>
            Kind == ApiErrorKind.Network || Kind == ApiErrorKind.Timeout || Kind == ApiErrorKind.Server;

        public static ApiError FromResponse(long statusCode, string requestError, string body, string retryAfterHeader)
        {
            var detail = string.IsNullOrWhiteSpace(body) ? requestError ?? string.Empty : body;
            return new ApiError(
                statusCode,
                Classify(statusCode, requestError),
                ParseRetryAfter(retryAfterHeader),
                $"{statusCode}: {detail}");
        }

        public static ApiError Local(string message)
        {
            return new ApiError(0, ApiErrorKind.Network, -1, message);
        }

        public static ApiErrorKind Classify(long statusCode, string requestError)
        {
            if (statusCode == 0)
            {
                return !string.IsNullOrEmpty(requestError)
                    && requestError.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                        ? ApiErrorKind.Timeout
                        : ApiErrorKind.Network;
            }

            if (statusCode == 401)
            {
                return ApiErrorKind.Unauthorized;
            }

            if (statusCode == 404)
            {
                return ApiErrorKind.NotFound;
            }

            if (statusCode == 400 || statusCode == 422)
            {
                return ApiErrorKind.BadRequest;
            }

            if (statusCode == 429)
            {
                return ApiErrorKind.RateLimited;
            }

            return statusCode >= 500 ? ApiErrorKind.Server : ApiErrorKind.Unexpected;
        }

        public static int ParseRetryAfter(string header)
        {
            return int.TryParse(header?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
                && seconds >= 0
                    ? seconds
                    : -1;
        }
    }
}
```

- [ ] **Step 4: 路由、快照、文案、离线提醒**

`Assets/Scripts/Network/ApiRoutes.cs`，把

```csharp
        public static string RecordDetail(int predictionId)
        {
            return $"/records/{predictionId}";
        }
```

替换为

```csharp
        public static string RecordDetail(int predictionId)
        {
            return $"/records/{predictionId}";
        }

        public static string RecordInterpretAsync(int predictionId)
        {
            return $"/records/{predictionId}/interpret/async";
        }
```

`Assets/Scripts/Data/ReadingSessionSnapshot.cs`，把

```csharp
    [Serializable]
    public sealed class ReadingSessionSnapshot
```

替换为

```csharp
    // Phase 66: where a reading's interpretation text came from.
    public enum ReadingSource
    {
        Offline,
        Online,
    }

    // Phase 66: Ready is the first member so default(InterpretationState) and a
    // fresh snapshot both describe an offline reading that already has its text.
    public enum InterpretationState
    {
        Ready,
        Pending,
        Failed,
    }

    // Phase 66: why an online interpretation stopped (spec 7.3, interpretation rows).
    public enum InterpretationFailure
    {
        BackendFailed,
        TimedOut,
        ConnectionLost,
        AttemptsExhausted,
        SessionExpired,
        Unavailable,
    }

    [Serializable]
    public sealed class ReadingSessionSnapshot
```

再把

```csharp
        public string warning;
```

替换为

```csharp
        public string warning;

        // Phase 66: online interpretation state. An offline reading keeps the
        // defaults: no record, offline source, text already Ready.
        public int predictionId;
        public ReadingSource source = ReadingSource.Offline;
        public InterpretationState interpretationState = InterpretationState.Ready;
        public string modelUsed = string.Empty;
        public string failureMessage = string.Empty;
        public bool canRetry;
```

`Assets/Scripts/UI/ReleaseUxCopy.cs`，把

```csharp
namespace TarotUnity.UI
{
    public static class ReleaseUxCopy
    {
```

替换为

```csharp
using TarotUnity.Data;
using TarotUnity.Network;

namespace TarotUnity.UI
{
    public static class ReleaseUxCopy
    {
        // Phase 66: reading-room flow copy (spec 6.6). Guarded to contain no ASCII letters.
        public const string DefaultQuestion = "此刻我最需要留意什么？";
        public const string FlowShuffling = "正在洗牌……";
        public const string FlowDealing = "正在发牌……";
        public const string FlowFlipPrompt = "点击每张牌，把它翻开。";
        public const string FlowAllRevealed = "牌已全部揭开。";
        public const string FlowResultReady = "可以查看结果了。";
        public const string InterpretationGenerating = "解读正在生成……";
        public const string InterpretationReadyHint = "解读已就绪";
        public const string OnlineReady = "已连上占卜服务：抽牌后会在线生成解读。";

        // Phase 66: a failed start (spec 7.3, create-record rows) - the reading goes offline.
        public const string OfflineBecauseNetwork = "暂时连不上占卜服务，这一局使用离线解读。";
        public const string OfflineBecauseSession = "访客会话连接失败，这一局使用离线解读。";
        public const string OfflineBecauseUnavailable = "在线占卜暂时不可用，这一局使用离线解读。";
        public const string OfflineBecauseSpread = "这个牌阵暂时无法在线占卜，这一局使用离线解读。";

        // Phase 66: interpretation failures (spec 7.3, interpretation rows).
        public const string InterpretationBackendFailed = "这次解读没有顺利生成，可以再试一次。";
        public const string InterpretationTimedOut = "解读花的时间比预期长，可以再试一次。";
        public const string InterpretationConnectionLost = "与占卜服务的连接中断了，可以再试一次。";
        public const string InterpretationAttemptsExhausted = "这一局已经尝试多次仍未成功，先看看离线解读吧。";
        public const string InterpretationSessionExpired = "连接已过期，这次解读无法取回。";
        public const string InterpretationUnavailable = "这次解读无法生成，先看看离线解读吧。";

        // Phase 66: Result screen (spec 7.1).
        public const string ResultPending = "牌意正在汇聚……";
        public const string ResultPendingSlow = "这次解读比平时慢一些，请再稍候。";
        public const string ModeOffline = "离线解读";
        public const string ModeMock = "模拟解读";
        public const string RetryButtonLabel = "重新解读";
        public const string OfflineButtonLabel = "查看离线解读";
        public const string OfflineWarning = "这是离线解读，由本地牌义生成，未经过 AI。";

        public static string GuestQuotaExhausted(int retryAfterSeconds)
        {
            const string head = "今天的访客占卜次数已用完，";
            const string tail = "这一局使用离线解读。";
            if (retryAfterSeconds < 0)
            {
                return head + tail;
            }

            if (retryAfterSeconds < 3600)
            {
                return head + "不到 1 小时后恢复。" + tail;
            }

            var hours = (retryAfterSeconds + 3599) / 3600;
            return head + $"约 {hours} 小时后恢复。" + tail;
        }

        // A 401 reaching this point means BackendReadingService.RecoverSession
        // already failed (spec 6.6 step 2).
        public static string ForStartReadingFailure(ApiError error)
        {
            if (error == null)
            {
                return OfflineBecauseUnavailable;
            }

            switch (error.Kind)
            {
                case ApiErrorKind.Network:
                case ApiErrorKind.Timeout:
                case ApiErrorKind.Server:
                    return OfflineBecauseNetwork;
                case ApiErrorKind.RateLimited:
                    return GuestQuotaExhausted(error.RetryAfterSeconds);
                case ApiErrorKind.Unauthorized:
                    return OfflineBecauseSession;
                default:
                    return OfflineBecauseUnavailable;
            }
        }

        public static string ForInterpretationFailure(InterpretationFailure failure)
        {
            switch (failure)
            {
                case InterpretationFailure.BackendFailed:
                    return InterpretationBackendFailed;
                case InterpretationFailure.TimedOut:
                    return InterpretationTimedOut;
                case InterpretationFailure.ConnectionLost:
                    return InterpretationConnectionLost;
                case InterpretationFailure.AttemptsExhausted:
                    return InterpretationAttemptsExhausted;
                case InterpretationFailure.SessionExpired:
                    return InterpretationSessionExpired;
                default:
                    return InterpretationUnavailable;
            }
        }

        public static bool CanRetry(InterpretationFailure failure)
        {
            return failure == InterpretationFailure.BackendFailed
                || failure == InterpretationFailure.TimedOut
                || failure == InterpretationFailure.ConnectionLost;
        }

```

（替换文本以一个空行结尾，原有的 `LocalModeReady` 等三个成员紧随其后，保持不变。）

`Assets/Scripts/Gameplay/LocalReadingSimulator.cs`，把

```csharp
using TarotUnity.Data;
```

替换为

```csharp
using TarotUnity.Data;
using TarotUnity.UI;
```

再把

```csharp
                warning = "这是本地占位文本，后端 AI 解读将在后续阶段接入。",
```

替换为

```csharp
                warning = ReleaseUxCopy.OfflineWarning,
```

- [ ] **Step 5: 运行，确认通过**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t2-green -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
```

预期：`total=37 passed=37 failed=0`（Task 1 的 2 个加上本任务的 35 个；`TestCase` 每组参数算一个测试）。

- [ ] **Step 6: 全量回归**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t2
bash "$S/online-b-run/ut.sh" PlayMode t2
```

预期：EditMode `total=E0+37 failed=0`；PlayMode `total=P0 failed=0`。如果有旧测试断言旧的提醒文案 `这是本地占位文本`，STOP 并报告（写计划时已用 grep 确认，全仓只有 `LocalReadingSimulator.cs` 含这句，没有测试断言它）。

- [ ] **Step 7: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
ls Assets/Scripts/Network/ApiError.cs.meta
git add Assets/Scripts/Network/ApiError.cs Assets/Scripts/Network/ApiError.cs.meta \
  Assets/Scripts/Network/ApiRoutes.cs Assets/Scripts/Data/ReadingSessionSnapshot.cs \
  Assets/Scripts/UI/ReleaseUxCopy.cs Assets/Scripts/Gameplay/LocalReadingSimulator.cs \
  Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs
git commit -F - <<'EOF'
feat(unity): add structured API errors, interpretation state and Chinese copy

ApiError classifies failures by status code and Retry-After; the reading
snapshot records its source and interpretation state; ReleaseUxCopy holds
the spec 7.3 copy table so the player never sees a raw server message.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
```

---

### Task 3: 映射器：在线开局快照与"有解读"的判定

**Files:**
- Modify: `Assets/Scripts/Data/ReadingSessionMapper.cs:28-34`（`FromBackendDetail` 末尾），并新增四个静态方法
- Test: `Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs`（追加）

**Interfaces:**
- Consumes：Task 2 的 `ReadingSource`、`InterpretationState` 和快照新字段。
- Produces：
  - `ReadingSessionMapper.FromBackendStart(PredictionResponse prediction, CardDrawData[] cards)`：返回 `source = Online`、`interpretationState = Pending`、`predictionId = prediction.id` 的快照，文本字段为空，`spreadName` 为空（由调用方填写）。`prediction` 为 null 时返回 null。
  - `bool ReadingSessionMapper.HasInterpretation(PredictionDetailResponse detail)`
  - `bool ReadingSessionMapper.IsRealInterpretation(InterpretationResponse interpretation)`：`id > 0` 或 `overall_interpretation` 非空
  - `void ReadingSessionMapper.ApplyInterpretation(ReadingSessionSnapshot target, InterpretationResponse interpretation)`：原地写入文本和 `modelUsed`，并设置 `Online`、`Ready`，清空 `failureMessage`，`canRetry = false`
  - `FromBackendDetail` 额外设置 `predictionId`、`source = Online`、`interpretationState`（有解读为 `Ready`，否则为 `Pending`）和 `modelUsed`

- [ ] **Step 1: 追加失败的测试**

用 Edit 把锚点注释 `        // Phase 66: later tasks append tests above this line.` 替换为：

```csharp
        private const string DetailWithoutInterpretationJson =
            "{\"id\":601,\"user_id\":7,\"spread_type_id\":1,\"question\":\"问题\",\"question_type\":\"general\","
            + "\"status\":\"processing\",\"card_draws\":[],\"interpretation\":null}";

        private const string DetailWithInterpretationJson =
            "{\"id\":601,\"user_id\":7,\"spread_type_id\":1,\"question\":\"问题\",\"question_type\":\"general\","
            + "\"status\":\"processing\",\"card_draws\":[],\"interpretation\":{\"id\":3001,\"prediction_id\":601,"
            + "\"overall_interpretation\":\"整体\",\"card_analysis\":\"牌面\",\"advice\":\"建议\",\"warning\":\"提醒\","
            + "\"summary\":\"概要\",\"model_used\":\"deepseek-chat\"}}";

        [Test]
        public void JsonNullInterpretationIsNotTreatedAsReady()
        {
            var detail = JsonUtility.FromJson<PredictionDetailResponse>(DetailWithoutInterpretationJson);

            Assert.That(detail, Is.Not.Null, "control: the JSON parsed");
            Assert.That(detail.id, Is.EqualTo(601), "control: fields were read");
            Assert.That(ReadingSessionMapper.HasInterpretation(detail), Is.False);
        }

        [Test]
        public void InterpretationPresentCountsAsReadyWhateverTheStatus()
        {
            var detail = JsonUtility.FromJson<PredictionDetailResponse>(DetailWithInterpretationJson);

            Assert.That(detail.status, Is.EqualTo("processing"), "control: status is not completed");
            Assert.That(ReadingSessionMapper.HasInterpretation(detail), Is.True);
        }

        [Test]
        public void StartMappingProducesAnOnlinePendingSnapshot()
        {
            var prediction = new PredictionResponse { id = 601, spread_type_id = 2, question = "问题", question_type = "general" };
            var cards = LocalReadingSimulator.CreatePlaceholderDraws(3);

            var snapshot = ReadingSessionMapper.FromBackendStart(prediction, cards);

            Assert.That(snapshot.predictionId, Is.EqualTo(601));
            Assert.That(snapshot.spreadId, Is.EqualTo(2));
            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Pending));
            Assert.That(snapshot.cardDraws, Is.SameAs(cards));
            Assert.That(snapshot.cardCount, Is.EqualTo(3));
            Assert.That(snapshot.question, Is.EqualTo("问题"));
            Assert.That(snapshot.summary, Is.Empty);
            Assert.That(snapshot.warning, Is.Empty);
            Assert.That(ReadingSessionMapper.FromBackendStart(null, cards), Is.Null);
        }

        [Test]
        public void ApplyingAnInterpretationMakesTheSnapshotReady()
        {
            var snapshot = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 601 }, LocalReadingSimulator.CreatePlaceholderDraws(1));
            snapshot.interpretationState = InterpretationState.Failed;
            snapshot.failureMessage = "旧的失败原因";
            snapshot.canRetry = true;

            ReadingSessionMapper.ApplyInterpretation(snapshot, new InterpretationResponse
            {
                id = 3001,
                summary = "概要",
                overall_interpretation = "整体",
                card_analysis = "牌面",
                advice = "建议",
                warning = "提醒",
                model_used = "deepseek-chat",
            });

            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(snapshot.summary, Is.EqualTo("概要"));
            Assert.That(snapshot.overallInterpretation, Is.EqualTo("整体"));
            Assert.That(snapshot.cardAnalysis, Is.EqualTo("牌面"));
            Assert.That(snapshot.advice, Is.EqualTo("建议"));
            Assert.That(snapshot.warning, Is.EqualTo("提醒"));
            Assert.That(snapshot.modelUsed, Is.EqualTo("deepseek-chat"));
            Assert.That(snapshot.failureMessage, Is.Empty);
            Assert.That(snapshot.canRetry, Is.False);
        }

        [Test]
        public void DetailMappingRecordsTheOnlineStateAndModel()
        {
            var ready = ReadingSessionMapper.FromBackendDetail(
                JsonUtility.FromJson<PredictionDetailResponse>(DetailWithInterpretationJson));
            Assert.That(ready.predictionId, Is.EqualTo(601));
            Assert.That(ready.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(ready.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(ready.modelUsed, Is.EqualTo("deepseek-chat"));

            var pending = ReadingSessionMapper.FromBackendDetail(
                JsonUtility.FromJson<PredictionDetailResponse>(DetailWithoutInterpretationJson));
            Assert.That(pending.interpretationState, Is.EqualTo(InterpretationState.Pending));
            Assert.That(pending.modelUsed, Is.Empty);
        }

        // Phase 66: later tasks append tests above this line.
```

- [ ] **Step 2: 运行，确认失败**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t3-red -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
```

预期：`NO RESULTS XML`，列出 `error CS0117`，内容为 `'ReadingSessionMapper' does not contain a definition for 'HasInterpretation'`（或 `FromBackendStart`、`ApplyInterpretation`）。

- [ ] **Step 3: 实现**

`Assets/Scripts/Data/ReadingSessionMapper.cs`，用 Edit 把

```csharp
                warning = interpretation?.warning ?? string.Empty,
            };
        }
```

替换为

```csharp
                warning = interpretation?.warning ?? string.Empty,
                predictionId = detail.id,
                source = ReadingSource.Online,
                interpretationState = IsRealInterpretation(interpretation)
                    ? InterpretationState.Ready
                    : InterpretationState.Pending,
                modelUsed = IsRealInterpretation(interpretation)
                    ? interpretation.model_used ?? string.Empty
                    : string.Empty,
            };
        }

        // Phase 66: the start of an online reading - the record and its drawn cards,
        // with no interpretation yet. The caller fills in spreadName; the poller
        // fills in the text once the background generation finishes.
        public static ReadingSessionSnapshot FromBackendStart(PredictionResponse prediction, CardDrawData[] cards)
        {
            if (prediction == null)
            {
                return null;
            }

            var draws = cards ?? Array.Empty<CardDrawData>();
            return new ReadingSessionSnapshot
            {
                spreadId = prediction.spread_type_id,
                spreadName = string.Empty,
                cardCount = draws.Length,
                question = prediction.question,
                questionType = prediction.question_type,
                cardDraws = draws,
                summary = string.Empty,
                overallInterpretation = string.Empty,
                cardAnalysis = string.Empty,
                advice = string.Empty,
                warning = string.Empty,
                predictionId = prediction.id,
                source = ReadingSource.Online,
                interpretationState = InterpretationState.Pending,
            };
        }

        // Phase 66: JsonUtility may turn a JSON "interpretation": null into an empty
        // instance rather than null, so "has an interpretation" means a stored row
        // (id > 0) or body text - never merely a non-null reference.
        public static bool HasInterpretation(PredictionDetailResponse detail)
        {
            return IsRealInterpretation(detail?.interpretation);
        }

        public static bool IsRealInterpretation(InterpretationResponse interpretation)
        {
            return interpretation != null
                && (interpretation.id > 0 || !string.IsNullOrWhiteSpace(interpretation.overall_interpretation));
        }

        public static void ApplyInterpretation(ReadingSessionSnapshot target, InterpretationResponse interpretation)
        {
            if (target == null || interpretation == null)
            {
                return;
            }

            target.summary = interpretation.summary ?? string.Empty;
            target.overallInterpretation = interpretation.overall_interpretation ?? string.Empty;
            target.cardAnalysis = interpretation.card_analysis ?? string.Empty;
            target.advice = interpretation.advice ?? string.Empty;
            target.warning = interpretation.warning ?? string.Empty;
            target.modelUsed = interpretation.model_used ?? string.Empty;
            target.source = ReadingSource.Online;
            target.interpretationState = InterpretationState.Ready;
            target.failureMessage = string.Empty;
            target.canRetry = false;
        }
```

- [ ] **Step 4: 运行，确认通过**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t3-green -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
```

预期：`total=42 passed=42 failed=0`。

如果 `JsonNullInterpretationIsNotTreatedAsReady` 失败，先看失败的是哪一条断言：
- 两条 `control` 断言失败：说明 JSON 本身没有被解析（仪器问题），STOP 并报告。
- 只有最后一条失败：说明 `IsRealInterpretation` 的判定写错了，对照 Step 3 修正。

- [ ] **Step 5: 全量回归**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t3
bash "$S/online-b-run/ut.sh" PlayMode t3
```

预期：EditMode `total=E0+42 failed=0`；PlayMode `total=P0 failed=0`（旧的 `CompletesReadingAgainstHttpBackend` 仍然调用 `FromBackendDetail`，新增字段不影响它的断言）。

- [ ] **Step 6: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
git add Assets/Scripts/Data/ReadingSessionMapper.cs Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs
git commit -F - <<'EOF'
feat(unity): map online reading starts and detect stored interpretations

An online reading now starts as a Pending snapshot built from the record
and its cards. An interpretation counts as present only when it is a stored
row or has body text, because JsonUtility may not keep a JSON null as null.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
```

---

### Task 4: 结构化请求、`StartReading` 与可编排的测试桩

**Files:**
- Modify: `Assets/Scripts/Data/ApiContracts.cs:156-162`（`PredictionDetailResponse` 之后）
- Modify: `Assets/Scripts/Network/ApiClient.cs`（`GetRecord` 之后；`BuildUrl` 之前）
- Modify: `Assets/Scripts/Network/BackendReadingService.cs`（在 `HasError` 之前插入新方法；`CompleteReading` 留到 Task 6 删除，因为 `ReadingRoomController` 在那之前还在调用它）
- Create: `Assets/Tests/PlayMode/MockTarotBackend.cs`
- Modify（改写整个测试类）: `Assets/Tests/PlayMode/BackendReadingServiceFlowTests.cs`

**Interfaces:**
- Consumes：Task 2 的 `ApiError`、`ApiErrorKind`、`ApiRoutes.RecordInterpretAsync`、`ReleaseUxCopy.ForStartReadingFailure`；Task 3 的 `ReadingSessionMapper.FromBackendStart`、`IsRealInterpretation`。
- Produces：
  - `TarotUnity.Data.AsyncInterpretationOutcome { Accepted, AlreadyReady }`
  - `TarotUnity.Data.AsyncInterpretationResult { AsyncInterpretationOutcome outcome; InterpretationResponse interpretation; }`
  - `ApiClient` 新方法。每个方法都恰好调用两个回调中的一个；返回的 JSON 无法解析时报 `Unexpected`：
    - `IEnumerator PostRecord(PredictionCreateRequest payload, Action<PredictionResponse> onSuccess, Action<ApiError> onError)`
    - `IEnumerator PostDraw(int predictionId, Action<DrawCardsResponse> onSuccess, Action<ApiError> onError)`
    - `IEnumerator FetchRecordCards(int predictionId, Action<CardDrawData[]> onSuccess, Action<ApiError> onError)`
    - `IEnumerator PostInterpretAsync(int predictionId, Action<AsyncInterpretationResult> onSuccess, Action<ApiError> onError)`：202 → `Accepted`；200 且带真实解读 → `AlreadyReady`；200 但没有解读 → `Unexpected`
    - `IEnumerator FetchRecordDetail(int predictionId, Action<PredictionDetailResponse> onSuccess, Action<ApiError> onError)`
    - `static TItem[] ParseJsonArray<TItem>(string text)`
  - `BackendReadingService` 新方法：
    - `IEnumerator StartReading(PredictionCreateRequest payload, Action<ReadingSessionSnapshot> onSuccess, Action<ApiError> onError)`
    - `IEnumerator RecoverSession(Action<bool> onDone)`
    - `IEnumerator RequestInterpretationAsync(int predictionId, Action<AsyncInterpretationResult> onSuccess, Action<ApiError> onError)`
    - `IEnumerator GetRecord(int predictionId, Action<PredictionDetailResponse> onSuccess, Action<ApiError> onError)`
  - PlayMode 测试桩：
    - `internal sealed class MockTarotBackend : IDisposable`：
      - 静态：`Start()`、`Json(int statusCode, string body)`、`Aborted()`
      - 属性：`string ApiBaseUrl`、`string[] RequestLog`（每项形如 `"GET /api/v1/records/601"`）
      - 方法：`Script(string method, string path, params Reply[] replies)`（每条路由一个队列，最后一条回复会重复返回，未编排的路由返回 404）、`int Count(string method, string path)`、`string LastBody(string method, string path)`
      - `Reply.WithHeader(string name, string value)`、`Reply.WithDelay(int milliseconds)`
    - `internal static class MockTarotJson`：
      - `Record(int id, int spreadId)`、`Cards(int predictionId, int count)`、`Draw(int predictionId, int count)`、`Accepted(int predictionId)`、`Interpretation(int predictionId, string model)`、`Detail(int predictionId, string status, int cardCount, string interpretationJson)`、`Token(string accessToken)`、`Spreads(params (int id, string name, int cardCount)[] spreads)`
      - 常量：`GuestLimitDetail`、`AttemptsExhaustedDetail`、`RecordNotFoundDetail`

- [ ] **Step 1: 新建测试桩**

确认 `Assets/Tests/PlayMode/MockTarotBackend.cs` 不存在，然后创建：

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 66: a scripted stand-in for the FastAPI backend. Each route (method +
    /// path) holds a queue of replies and the last reply keeps repeating, so a test
    /// scripts only the transitions it cares about. Unscripted routes answer 404.
    /// </summary>
    internal sealed class MockTarotBackend : IDisposable
    {
        internal sealed class Reply
        {
            public int StatusCode = 200;
            public string Body = "{}";
            public int DelayMilliseconds;
            public bool AbortConnection;
            public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();

            public Reply WithHeader(string name, string value)
            {
                Headers[name] = value;
                return this;
            }

            public Reply WithDelay(int milliseconds)
            {
                DelayMilliseconds = milliseconds;
                return this;
            }
        }

        private readonly HttpListener listener;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly Task serverTask;
        private readonly Dictionary<string, Queue<Reply>> routes = new Dictionary<string, Queue<Reply>>();
        private readonly Dictionary<string, string> lastBodies = new Dictionary<string, string>();
        private readonly List<string> requestLog = new List<string>();

        private MockTarotBackend(HttpListener listener, int port)
        {
            this.listener = listener;
            ApiBaseUrl = $"http://127.0.0.1:{port}/api/v1";
            serverTask = Task.Run(ServerLoop);
        }

        public string ApiBaseUrl { get; }

        public string[] RequestLog
        {
            get
            {
                lock (requestLog)
                {
                    return requestLog.ToArray();
                }
            }
        }

        public static MockTarotBackend Start()
        {
            var port = GetFreePort();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
            return new MockTarotBackend(listener, port);
        }

        public static Reply Json(int statusCode, string body)
        {
            return new Reply { StatusCode = statusCode, Body = body };
        }

        public static Reply Aborted()
        {
            return new Reply { AbortConnection = true };
        }

        public void Script(string method, string path, params Reply[] replies)
        {
            lock (routes)
            {
                var key = Key(method, path);
                if (!routes.TryGetValue(key, out var queue))
                {
                    queue = new Queue<Reply>();
                    routes[key] = queue;
                }

                foreach (var reply in replies)
                {
                    queue.Enqueue(reply);
                }
            }
        }

        public int Count(string method, string path)
        {
            var key = Key(method, path);
            var count = 0;
            foreach (var entry in RequestLog)
            {
                if (entry == key)
                {
                    count++;
                }
            }

            return count;
        }

        public string LastBody(string method, string path)
        {
            lock (lastBodies)
            {
                return lastBodies.TryGetValue(Key(method, path), out var body) ? body : null;
            }
        }

        public void Dispose()
        {
            cancellation.Cancel();
            listener.Stop();
            listener.Close();

            try
            {
                serverTask.Wait(500);
            }
            catch (AggregateException)
            {
                // The listener is intentionally stopped during teardown.
            }
        }

        private static string Key(string method, string path)
        {
            return $"{method.ToUpperInvariant()} {path}";
        }

        private async Task ServerLoop()
        {
            while (!cancellation.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (HttpListenerException)
                {
                    return;
                }

                _ = Task.Run(() => Handle(context));
            }
        }

        private async Task Handle(HttpListenerContext context)
        {
            var key = Key(context.Request.HttpMethod, context.Request.Url?.AbsolutePath ?? string.Empty);
            string body;
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                body = reader.ReadToEnd();
            }

            lock (requestLog)
            {
                requestLog.Add(key);
            }

            lock (lastBodies)
            {
                lastBodies[key] = body;
            }

            Reply reply;
            lock (routes)
            {
                if (routes.TryGetValue(key, out var queue) && queue.Count > 0)
                {
                    reply = queue.Count > 1 ? queue.Dequeue() : queue.Peek();
                }
                else
                {
                    reply = Json(404, "{\"detail\":\"not scripted\"}");
                }
            }

            try
            {
                if (reply.DelayMilliseconds > 0)
                {
                    await Task.Delay(reply.DelayMilliseconds);
                }

                if (reply.AbortConnection)
                {
                    context.Response.Abort();
                    return;
                }

                var bytes = Encoding.UTF8.GetBytes(reply.Body ?? string.Empty);
                context.Response.StatusCode = reply.StatusCode;
                context.Response.ContentType = "application/json";
                foreach (var header in reply.Headers)
                {
                    context.Response.AddHeader(header.Key, header.Value);
                }

                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.Close();
            }
            catch (Exception)
            {
                // The client gave up (timeout tests) or the listener was stopped.
            }
        }

        private static int GetFreePort()
        {
            var tcp = new TcpListener(IPAddress.Loopback, 0);
            tcp.Start();
            var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
            tcp.Stop();
            return port;
        }
    }

    /// <summary>Phase 66: response bodies shaped like the FastAPI schemas.</summary>
    internal static class MockTarotJson
    {
        public const string GuestLimitDetail =
            "{\"detail\":\"Guest daily reading limit reached. Please try again tomorrow.\"}";
        public const string AttemptsExhaustedDetail = "{\"detail\":\"Interpretation attempts exhausted\"}";
        public const string RecordNotFoundDetail = "{\"detail\":\"Record not found\"}";

        private static readonly string[] CardNames =
        {
            "愚者", "魔术师", "女祭司", "皇后", "皇帝", "教皇", "恋人", "战车", "力量", "隐者",
        };

        public static string Record(int id, int spreadId)
        {
            return "{\"id\":" + id + ",\"user_id\":7,\"spread_type_id\":" + spreadId
                + ",\"question\":\"此刻我最需要留意什么？\",\"question_type\":\"general\",\"status\":\"pending\","
                + "\"created_at\":\"2026-09-12T00:00:00Z\",\"completed_at\":null,\"is_favorite\":false,"
                + "\"user_rating\":0,\"user_notes\":\"\"}";
        }

        public static string Cards(int predictionId, int count)
        {
            var items = new string[count];
            for (var i = 0; i < count; i++)
            {
                var name = CardNames[i % CardNames.Length];
                var position = i + 1;
                items[i] = "{\"id\":" + (9000 + i) + ",\"prediction_id\":" + predictionId + ",\"tarot_card_id\":" + i
                    + ",\"position\":" + position + ",\"is_reversed\":" + (i == 2 ? "true" : "false")
                    + ",\"drawn_at\":\"2026-09-12T00:00:01Z\",\"tarot_card\":{\"id\":" + i + ",\"name_zh\":\"" + name
                    + "\",\"name_en\":\"Card " + i + "\",\"arcana\":\"major\",\"suit\":\"\",\"number\":" + i
                    + ",\"image_url\":\"\"},\"card_meaning\":{\"id\":" + i + ",\"name_zh\":\"" + name
                    + "\",\"name_en\":\"Card " + i + "\",\"is_reversed\":false,\"meaning\":\"测试牌义\","
                    + "\"keywords\":[\"测试\"],\"position\":" + position + ",\"position_name\":\"第" + position
                    + "位\",\"position_meaning\":\"测试牌位\"},\"position_name\":\"第" + position
                    + "位\",\"position_meaning\":\"测试牌位\"}";
            }

            return "[" + string.Join(",", items) + "]";
        }

        public static string Draw(int predictionId, int count)
        {
            return "{\"prediction_id\":" + predictionId + ",\"status\":\"success\",\"card_draws\":"
                + Cards(predictionId, count) + "}";
        }

        public static string Accepted(int predictionId)
        {
            return "{\"prediction_id\":" + predictionId + ",\"status\":\"processing\"}";
        }

        public static string Interpretation(int predictionId, string model)
        {
            return "{\"id\":" + (3000 + predictionId) + ",\"prediction_id\":" + predictionId
                + ",\"overall_interpretation\":\"整体解读来自测试桩。\",\"card_analysis\":\"牌面分析来自测试桩。\","
                + "\"relationship_analysis\":\"\",\"advice\":\"建议来自测试桩。\",\"warning\":\"提醒来自测试桩。\","
                + "\"summary\":\"概要来自测试桩。\",\"key_themes\":\"测试\",\"model_used\":\"" + model
                + "\",\"model_version\":\"test\",\"confidence_score\":0.9,\"generated_at\":\"2026-09-12T00:00:02Z\"}";
        }

        public static string Detail(int predictionId, string status, int cardCount, string interpretationJson)
        {
            return "{\"id\":" + predictionId + ",\"user_id\":7,\"spread_type_id\":2,"
                + "\"question\":\"此刻我最需要留意什么？\",\"question_type\":\"general\",\"status\":\"" + status
                + "\",\"created_at\":\"2026-09-12T00:00:00Z\",\"completed_at\":null,\"is_favorite\":false,"
                + "\"user_rating\":0,\"user_notes\":\"\",\"spread_type\":null,\"card_draws\":"
                + Cards(predictionId, cardCount) + ",\"interpretation\":" + (interpretationJson ?? "null") + "}";
        }

        public static string Token(string accessToken)
        {
            return "{\"access_token\":\"" + accessToken + "\",\"token_type\":\"bearer\"}";
        }

        public static string Spreads(params (int id, string name, int cardCount)[] spreads)
        {
            var items = new string[spreads.Length];
            for (var i = 0; i < spreads.Length; i++)
            {
                items[i] = "{\"id\":" + spreads[i].id + ",\"name\":\"" + spreads[i].name + "\",\"name_en\":\"Spread "
                    + spreads[i].id + "\",\"description\":\"\",\"card_count\":" + spreads[i].cardCount
                    + ",\"difficulty_level\":1,\"positions\":[],\"is_beginner_friendly\":true,\"usage_count\":0,"
                    + "\"suitable_for_love\":true,\"suitable_for_career\":true,\"suitable_for_finance\":true,"
                    + "\"suitable_for_health\":true,\"suitable_for_general\":true}";
            }

            return "[" + string.Join(",", items) + "]";
        }
    }
}
```

- [ ] **Step 2: 改写 `BackendReadingServiceFlowTests.cs`**

先 Read 整个文件，再做两次 Edit。

**Edit A**：把第 1 行 `using System;` 到第 62 行（`CompletesReadingAgainstHttpBackend` 方法结束处的 `        }`）整段替换为：

```csharp
using System.Collections;
using NUnit.Framework;
using TarotUnity.Core;
using TarotUnity.Data;
using TarotUnity.Network;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 66: BackendReadingService starts an online reading (record + draw +
    /// cards, no interpretation) and reports structured errors. The aborted and slow
    /// tests are instrument controls: they prove the mock really produces a network
    /// error and a timeout before the poller tests rely on either.
    /// </summary>
    public sealed class BackendReadingServiceFlowTests
    {
        private MockTarotBackend server;
        private GameObject owner;
        private ApiClient client;
        private BackendReadingService service;

        [SetUp]
        public void SetUp()
        {
            server = MockTarotBackend.Start();
            owner = new GameObject("BackendReadingServiceFlowTest");
            client = owner.AddComponent<ApiClient>();
            client.BaseUrl = server.ApiBaseUrl;
            client.SetAccessToken("test-access-token");
            ApiClient.SetShared(client);
            service = owner.AddComponent<BackendReadingService>();
        }

        [TearDown]
        public void TearDown()
        {
            ApiClient.ClearShared();
            Object.Destroy(owner);
            server.Dispose();
        }

        [UnityTest]
        public IEnumerator StartReadingCreatesAnOnlinePendingSnapshotWithoutInterpreting()
        {
            server.Script("POST", "/api/v1/records/", MockTarotBackend.Json(200, MockTarotJson.Record(501, 2)));
            server.Script("POST", "/api/v1/records/501/draw", MockTarotBackend.Json(200, MockTarotJson.Draw(501, 3)));
            server.Script("GET", "/api/v1/records/501/cards", MockTarotBackend.Json(200, MockTarotJson.Cards(501, 3)));

            ReadingSessionSnapshot session = null;
            ApiError error = null;
            yield return service.StartReading(Payload(2), value => session = value, value => error = value);

            Assert.That(error, Is.Null, error?.RawMessage);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.predictionId, Is.EqualTo(501));
            Assert.That(session.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending));
            Assert.That(session.cardDraws, Has.Length.EqualTo(3));
            Assert.That(session.cardDraws[0].tarot_card.name_zh, Is.EqualTo("愚者"));
            Assert.That(server.LastBody("POST", "/api/v1/records/"), Does.Contain("\"spread_type_id\":2"));
            Assert.That(server.Count("POST", "/api/v1/records/501/interpret"), Is.EqualTo(0));
            Assert.That(server.Count("POST", "/api/v1/records/501/interpret/async"), Is.EqualTo(0),
                "starting a reading must not ask for the interpretation");
        }

        [UnityTest]
        public IEnumerator GuestQuotaOnCreateReportsRateLimitWithRetryAfter()
        {
            server.Script("POST", "/api/v1/records/",
                MockTarotBackend.Json(429, MockTarotJson.GuestLimitDetail).WithHeader("Retry-After", "5400"));

            ReadingSessionSnapshot session = null;
            ApiError error = null;
            yield return service.StartReading(Payload(1), value => session = value, value => error = value);

            Assert.That(session, Is.Null);
            Assert.That(error, Is.Not.Null);
            Assert.That(error.Kind, Is.EqualTo(ApiErrorKind.RateLimited));
            Assert.That(error.RetryAfterSeconds, Is.EqualTo(5400));
            Assert.That(ReleaseUxCopy.ForStartReadingFailure(error),
                Is.EqualTo("今天的访客占卜次数已用完，约 2 小时后恢复。这一局使用离线解读。"));
            Assert.That(server.RequestLog, Is.EqualTo(new[] { "POST /api/v1/records/" }), "no draw after a refused record");
        }

        [UnityTest]
        public IEnumerator AbortedConnectionSurfacesAsNetworkError()
        {
            server.Script("GET", "/api/v1/records/777", MockTarotBackend.Aborted());

            PredictionDetailResponse detail = null;
            ApiError error = null;
            yield return client.FetchRecordDetail(777, value => detail = value, value => error = value);

            Assert.That(server.Count("GET", "/api/v1/records/777"), Is.EqualTo(1), "control: the request reached the mock");
            Assert.That(detail, Is.Null);
            Assert.That(error, Is.Not.Null);
            Assert.That(error.StatusCode, Is.EqualTo(0), error.RawMessage);
            Assert.That(error.Kind, Is.EqualTo(ApiErrorKind.Network), error.RawMessage);
        }

        [UnityTest]
        public IEnumerator SlowResponseSurfacesAsTimeoutError()
        {
            var config = DesktopRuntimeConfig.CreateDefault();
            config.requestTimeoutSeconds = 1;
            client.ApplyRuntimeConfig(config);
            client.BaseUrl = server.ApiBaseUrl;
            server.Script("GET", "/api/v1/records/778", MockTarotBackend.Json(200, "{}").WithDelay(3000));

            ApiError error = null;
            yield return client.FetchRecordDetail(778, _ => { }, value => error = value);

            Assert.That(client.RequestTimeoutSeconds, Is.EqualTo(1), "control: the short timeout was applied");
            Assert.That(error, Is.Not.Null);
            Assert.That(error.Kind, Is.EqualTo(ApiErrorKind.Timeout), error.RawMessage);
        }

        [UnityTest]
        public IEnumerator AsyncInterpretReportsAcceptedThenAlreadyReady()
        {
            server.Script("POST", "/api/v1/records/501/interpret/async",
                MockTarotBackend.Json(202, MockTarotJson.Accepted(501)),
                MockTarotBackend.Json(200, MockTarotJson.Interpretation(501, "deepseek-chat")));

            AsyncInterpretationResult first = null;
            AsyncInterpretationResult second = null;
            ApiError error = null;
            yield return service.RequestInterpretationAsync(501, value => first = value, value => error = value);
            yield return service.RequestInterpretationAsync(501, value => second = value, value => error = value);

            Assert.That(error, Is.Null, error?.RawMessage);
            Assert.That(first.outcome, Is.EqualTo(AsyncInterpretationOutcome.Accepted));
            Assert.That(second.outcome, Is.EqualTo(AsyncInterpretationOutcome.AlreadyReady));
            Assert.That(second.interpretation.model_used, Is.EqualTo("deepseek-chat"));
        }

        [UnityTest]
        public IEnumerator RecoverSessionUsesRefreshWhenItWorks()
        {
            server.Script("POST", "/api/v1/refresh", MockTarotBackend.Json(200, MockTarotJson.Token("refreshed-token")));

            var recovered = false;
            yield return service.RecoverSession(value => recovered = value);

            Assert.That(recovered, Is.True);
            Assert.That(client.AccessToken, Is.EqualTo("refreshed-token"));
            Assert.That(server.Count("POST", "/api/v1/guest-session"), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator RecoverSessionStartsANewGuestWhenRefreshFails()
        {
            server.Script("POST", "/api/v1/refresh", MockTarotBackend.Json(401, "{\"detail\":\"Invalid refresh token\"}"));
            server.Script("POST", "/api/v1/guest-session", MockTarotBackend.Json(200, MockTarotJson.Token("fresh-guest")));

            var recovered = false;
            yield return service.RecoverSession(value => recovered = value);

            Assert.That(recovered, Is.True);
            Assert.That(client.AccessToken, Is.EqualTo("fresh-guest"));
            Assert.That(server.RequestLog, Is.EqualTo(new[] { "POST /api/v1/refresh", "POST /api/v1/guest-session" }));
        }
```

**Edit B**：把原来的嵌套类整段替换掉，范围从 `        private sealed class MockTarotBackend : IDisposable` 到它结束处的 `        }`（紧挨在文件末尾 `    }` 和 `}` 之前），替换为：

```csharp
        private static PredictionCreateRequest Payload(int spreadId)
        {
            return new PredictionCreateRequest
            {
                question = "此刻我最需要留意什么？",
                question_type = "general",
                spread_type_id = spreadId,
            };
        }
```

改完后，文件末尾应该是 `Payload` 方法，接着是 `    }` 和 `}`；文件里不能再出现 `CompleteReading` 或 `ResolveResponse`：

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
grep -c 'CompleteReading\|ResolveResponse' Assets/Tests/PlayMode/BackendReadingServiceFlowTests.cs
```

预期：`0`。

- [ ] **Step 3: 运行，确认失败**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" PlayMode t4-red -testFilter TarotUnity.Tests.PlayMode.BackendReadingServiceFlowTests
```

预期：`NO RESULTS XML`，并列出 `error CS1061`/`CS0246`，涉及 `StartReading`、`FetchRecordDetail`、`AsyncInterpretationResult` 等。

- [ ] **Step 4: 实现契约类型与 `ApiClient` 结构化请求**

`Assets/Scripts/Data/ApiContracts.cs`，把

```csharp
    [Serializable]
    public sealed class PredictionDetailResponse : PredictionResponse
    {
        public SpreadSummary spread_type;
        public CardDrawData[] card_draws;
        public InterpretationResponse interpretation;
    }
```

替换为

```csharp
    [Serializable]
    public sealed class PredictionDetailResponse : PredictionResponse
    {
        public SpreadSummary spread_type;
        public CardDrawData[] card_draws;
        public InterpretationResponse interpretation;
    }

    // Phase 66: POST /records/{id}/interpret/async answers 202 while the
    // interpretation is generating and 200 with it once one is stored.
    public enum AsyncInterpretationOutcome
    {
        Accepted,
        AlreadyReady,
    }

    public sealed class AsyncInterpretationResult
    {
        public AsyncInterpretationOutcome outcome;
        public InterpretationResponse interpretation;
    }
```

`Assets/Scripts/Network/ApiClient.cs`，把

```csharp
        public IEnumerator GetRecord(int predictionId, Action<PredictionDetailResponse> onSuccess, Action<string> onError)
        {
            yield return Get(ApiRoutes.RecordDetail(predictionId), onSuccess, onError);
        }
```

替换为

```csharp
        public IEnumerator GetRecord(int predictionId, Action<PredictionDetailResponse> onSuccess, Action<string> onError)
        {
            yield return Get(ApiRoutes.RecordDetail(predictionId), onSuccess, onError);
        }

        // Phase 66: structured variants for the online interpretation flow. Each one
        // calls exactly one of its callbacks and reports failures as ApiError
        // (status, kind, Retry-After). The Action<string> methods above are unchanged.
        public IEnumerator PostRecord(
            PredictionCreateRequest payload,
            Action<PredictionResponse> onSuccess,
            Action<ApiError> onError)
        {
            using var request = CreatePost(ApiRoutes.Records, JsonUtility.ToJson(payload));
            yield return request.SendWebRequest();
            HandleStructured(request, onSuccess, onError);
        }

        public IEnumerator PostDraw(int predictionId, Action<DrawCardsResponse> onSuccess, Action<ApiError> onError)
        {
            using var request = CreatePost(ApiRoutes.RecordDraw(predictionId), null);
            yield return request.SendWebRequest();
            HandleStructured(request, onSuccess, onError);
        }

        public IEnumerator FetchRecordCards(int predictionId, Action<CardDrawData[]> onSuccess, Action<ApiError> onError)
        {
            using var request = UnityWebRequest.Get(BuildUrl(ApiRoutes.RecordCards(predictionId)));
            PrepareRequest(request);
            yield return request.SendWebRequest();
            HandleStructuredArray(request, onSuccess, onError);
        }

        public IEnumerator PostInterpretAsync(
            int predictionId,
            Action<AsyncInterpretationResult> onSuccess,
            Action<ApiError> onError)
        {
            using var request = CreatePost(ApiRoutes.RecordInterpretAsync(predictionId), null);
            yield return request.SendWebRequest();
            HandleInterpretAsync(request, onSuccess, onError);
        }

        public IEnumerator FetchRecordDetail(
            int predictionId,
            Action<PredictionDetailResponse> onSuccess,
            Action<ApiError> onError)
        {
            using var request = UnityWebRequest.Get(BuildUrl(ApiRoutes.RecordDetail(predictionId)));
            PrepareRequest(request);
            yield return request.SendWebRequest();
            HandleStructured(request, onSuccess, onError);
        }

        public static TItem[] ParseJsonArray<TItem>(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<TItem>();
            }

            var wrapper = JsonUtility.FromJson<ArrayWrapper<TItem>>($"{{\"items\":{text}}}");
            return wrapper?.items ?? Array.Empty<TItem>();
        }

        private UnityWebRequest CreatePost(string path, string json)
        {
            var request = new UnityWebRequest(BuildUrl(path), UnityWebRequest.kHttpVerbPOST);
            if (json != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            PrepareRequest(request);
            return request;
        }

        private void HandleStructured<TResponse>(
            UnityWebRequest request,
            Action<TResponse> onSuccess,
            Action<ApiError> onError)
        {
            CaptureCookies(request);
            if (IsError(request))
            {
                onError?.Invoke(BuildApiError(request));
                return;
            }

            if (TryParseJson(request, out TResponse response, onError))
            {
                onSuccess?.Invoke(response);
            }
        }

        private void HandleStructuredArray<TItem>(
            UnityWebRequest request,
            Action<TItem[]> onSuccess,
            Action<ApiError> onError)
        {
            CaptureCookies(request);
            if (IsError(request))
            {
                onError?.Invoke(BuildApiError(request));
                return;
            }

            TItem[] items;
            try
            {
                items = ParseJsonArray<TItem>(request.downloadHandler?.text);
            }
            catch (ArgumentException exception)
            {
                onError?.Invoke(UnreadableBody(request, exception));
                return;
            }

            onSuccess?.Invoke(items);
        }

        private void HandleInterpretAsync(
            UnityWebRequest request,
            Action<AsyncInterpretationResult> onSuccess,
            Action<ApiError> onError)
        {
            CaptureCookies(request);
            if (IsError(request))
            {
                onError?.Invoke(BuildApiError(request));
                return;
            }

            // 202: generation started or is already running; 200: the stored interpretation.
            if (request.responseCode == 202)
            {
                onSuccess?.Invoke(new AsyncInterpretationResult { outcome = AsyncInterpretationOutcome.Accepted });
                return;
            }

            if (!TryParseJson(request, out InterpretationResponse interpretation, onError))
            {
                return;
            }

            if (!ReadingSessionMapper.IsRealInterpretation(interpretation))
            {
                onError?.Invoke(new ApiError(
                    request.responseCode,
                    ApiErrorKind.Unexpected,
                    -1,
                    $"{request.responseCode}: the interpretation body was empty"));
                return;
            }

            onSuccess?.Invoke(new AsyncInterpretationResult
            {
                outcome = AsyncInterpretationOutcome.AlreadyReady,
                interpretation = interpretation,
            });
        }

        private static bool TryParseJson<TResponse>(
            UnityWebRequest request,
            out TResponse response,
            Action<ApiError> onError)
        {
            var text = request.downloadHandler?.text;
            try
            {
                response = string.IsNullOrWhiteSpace(text) ? default : JsonUtility.FromJson<TResponse>(text);
                return true;
            }
            catch (ArgumentException exception)
            {
                response = default;
                onError?.Invoke(UnreadableBody(request, exception));
                return false;
            }
        }

        private static ApiError BuildApiError(UnityWebRequest request)
        {
            return ApiError.FromResponse(
                request.responseCode,
                request.error,
                request.downloadHandler?.text,
                request.GetResponseHeader("Retry-After"));
        }

        private static ApiError UnreadableBody(UnityWebRequest request, Exception exception)
        {
            return new ApiError(
                request.responseCode,
                ApiErrorKind.Unexpected,
                -1,
                $"{request.responseCode}: unreadable JSON ({exception.Message})");
        }
```

- [ ] **Step 5: 实现 `BackendReadingService` 的新方法**

`Assets/Scripts/Network/BackendReadingService.cs`，把

```csharp
        private static bool HasError(string error, Action<string> onError)
```

替换为

```csharp
        // Phase 66: an online reading starts with record + draw + cards only. The
        // interpretation is generated in the background and fetched by the
        // persistent InterpretationPoller, so the table deals as soon as the cards
        // exist instead of waiting on the AI the way CompleteReading did.
        public IEnumerator StartReading(
            PredictionCreateRequest payload,
            Action<ReadingSessionSnapshot> onSuccess,
            Action<ApiError> onError)
        {
            var client = Client;
            if (client == null)
            {
                onError?.Invoke(ApiError.Local("ApiClient is not available."));
                yield break;
            }

            PredictionResponse prediction = null;
            DrawCardsResponse drawn = null;
            CardDrawData[] cards = null;
            ApiError error = null;

            yield return client.PostRecord(payload, value => prediction = value, value => error = value);
            if (error == null && (prediction == null || prediction.id <= 0))
            {
                error = new ApiError(200, ApiErrorKind.Unexpected, -1, "200: the record response had no id");
            }

            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            yield return client.PostDraw(prediction.id, value => drawn = value, value => error = value);
            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            yield return client.FetchRecordCards(prediction.id, value => cards = value, value => error = value);
            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            if (cards == null || cards.Length == 0)
            {
                cards = drawn?.card_draws;
            }

            var snapshot = ReadingSessionMapper.FromBackendStart(prediction, cards);
            if (snapshot.cardDraws.Length == 0)
            {
                onError?.Invoke(new ApiError(200, ApiErrorKind.Unexpected, -1, "200: the backend reading returned no cards"));
                yield break;
            }

            onSuccess?.Invoke(snapshot);
        }

        // Phase 66: recover a guest whose access token was rejected before any record
        // exists (spec 6.6 step 2): refresh first; if that fails, start a new guest.
        // A new guest is safe here only because nothing has been created yet.
        public IEnumerator RecoverSession(Action<bool> onDone)
        {
            var client = Client;
            if (client == null)
            {
                onDone?.Invoke(false);
                yield break;
            }

            var refreshed = false;
            yield return client.Refresh(
                token => refreshed = token != null && !string.IsNullOrWhiteSpace(token.access_token),
                _ => refreshed = false);
            if (refreshed)
            {
                onDone?.Invoke(true);
                yield break;
            }

            client.ClearSession();
            var created = false;
            yield return client.CreateGuestSession(
                token => created = token != null && !string.IsNullOrWhiteSpace(token.access_token),
                _ => created = false);
            onDone?.Invoke(created && client.HasAccessToken);
        }

        public IEnumerator RequestInterpretationAsync(
            int predictionId,
            Action<AsyncInterpretationResult> onSuccess,
            Action<ApiError> onError)
        {
            var client = Client;
            if (client == null)
            {
                onError?.Invoke(ApiError.Local("ApiClient is not available."));
                yield break;
            }

            yield return client.PostInterpretAsync(predictionId, onSuccess, onError);
        }

        public IEnumerator GetRecord(
            int predictionId,
            Action<PredictionDetailResponse> onSuccess,
            Action<ApiError> onError)
        {
            var client = Client;
            if (client == null)
            {
                onError?.Invoke(ApiError.Local("ApiClient is not available."));
                yield break;
            }

            yield return client.FetchRecordDetail(predictionId, onSuccess, onError);
        }

        private static bool HasError(string error, Action<string> onError)
```

- [ ] **Step 6: 运行，确认通过**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" PlayMode t4-green -testFilter TarotUnity.Tests.PlayMode.BackendReadingServiceFlowTests
```

预期：`total=7 passed=7 failed=0`。

两个对照测试的判读：
- `AbortedConnectionSurfacesAsNetworkError` 失败：说明测试桩没能制造出连接错误。STOP，把失败信息里的 `RawMessage` 原样报告。**不要**为了让测试通过去改 `ApiError.Classify`。
- `SlowResponseSurfacesAsTimeoutError` 失败，并且 `RawMessage` 不含 `timeout`：STOP 并报告。

- [ ] **Step 7: 全量回归**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t4
bash "$S/online-b-run/ut.sh" PlayMode t4
```

预期：EditMode `total=E0+42 failed=0`；PlayMode `total=P0+6 failed=0`（原来的 1 个测试换成了 7 个）。

- [ ] **Step 8: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
ls Assets/Tests/PlayMode/MockTarotBackend.cs.meta
git add Assets/Scripts/Data/ApiContracts.cs Assets/Scripts/Network/ApiClient.cs \
  Assets/Scripts/Network/BackendReadingService.cs \
  Assets/Tests/PlayMode/MockTarotBackend.cs Assets/Tests/PlayMode/MockTarotBackend.cs.meta \
  Assets/Tests/PlayMode/BackendReadingServiceFlowTests.cs
git commit -F - <<'EOF'
feat(unity): start online readings without waiting for the interpretation

StartReading creates the record, draws and fetches the cards, and returns a
Pending snapshot. Structured ApiClient requests report ApiError, and the
PlayMode mock backend can now script status codes, headers, delays and
dropped connections per route.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
```

---

### Task 5: 常驻轮询器 `InterpretationPoller`

**Files:**
- Create: `Assets/Scripts/Network/InterpretationPoller.cs`
- Modify: `Assets/Scripts/Core/GameBootstrap.cs:19`
- Create: `Assets/Tests/PlayMode/Phase66InterpretationPollerTests.cs`
- Test: `Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs`（追加）

**Interfaces:**
- Consumes：
  - Task 1：`ApiClient.Shared`
  - Task 2：`ReleaseUxCopy.ForInterpretationFailure`、`ReleaseUxCopy.CanRetry`、`InterpretationFailure`
  - Task 3：`ReadingSessionMapper.HasInterpretation`、`ApplyInterpretation`、`FromBackendStart`
  - Task 4：`ApiClient.PostInterpretAsync`、`FetchRecordDetail`，已有的 `ApiClient.Refresh`；测试桩 `MockTarotBackend`、`MockTarotJson`
- Produces（命名空间 `TarotUnity.Network`，类 `InterpretationPoller : MonoBehaviour`）：
  - 常量：`const float TotalDeadlineSeconds = 330f`、`const int MaxConsecutiveNetworkErrors = 3`
  - `static InterpretationPoller Instance { get; }`：最后一个 Awake 的实例，销毁时清空
  - `event Action<ReadingSessionSnapshot> StateChanged`：在 `Begin`（Pending）、完成（Ready）、失败（Failed）、`UseOffline`（Offline/Ready）时触发
  - `ReadingSessionSnapshot Current { get; }`、`bool IsPolling { get; }`
  - `void Configure(ApiClient client, float pollDelayScale, float totalDeadlineSeconds)`：供测试注入
  - `void Begin(ReadingSessionSnapshot snapshot)`、`void Retry()`、`void Stop()`、`void UseOffline()`
  - `static float PollDelaySeconds(int attemptIndex)`
  - `static void ApplyFailure(ReadingSessionSnapshot snapshot, InterpretationFailure failure)`
  - `static void ApplyOffline(ReadingSessionSnapshot snapshot)`
  - **原地修改**传给 `Begin` 的快照对象（`ReadingSessionStore.Current` 始终指向同一个对象）
- 错误处理规则：

  | 请求 | 结果 | 处理 |
  | --- | --- | --- |
  | 异步开始 | 429 | `AttemptsExhausted`（不可重试） |
  | 异步开始 | 其他非瞬时错误，且不是 401 | `Unavailable`（不可重试） |
  | 轮询 GET | 404、400 | `Unavailable`（不可重试） |
  | 任意请求 | 401 | 续期一次后立刻重发这次请求；续期失败，或续期后再次 401 → `SessionExpired`（不可重试） |
  | 任意请求 | 其他错误 | 计入"连续网络错误"；达到 3 次 → `ConnectionLost`（可重试）；任意一次成功就清零 |

- [ ] **Step 1: 追加 EditMode 测试**

用 Edit 把锚点注释 `        // Phase 66: later tasks append tests above this line.` 替换为：

```csharp
        [Test]
        public void PollDelaysFollowTwoTwoThreeThreeThenFive()
        {
            var expected = new[] { 2f, 2f, 3f, 3f, 5f, 5f, 5f };
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(InterpretationPoller.PollDelaySeconds(i), Is.EqualTo(expected[i]), $"attempt {i}");
            }

            Assert.That(InterpretationPoller.PollDelaySeconds(40), Is.EqualTo(5f));
        }

        [Test]
        public void PollerLimitsMatchTheBackendContract()
        {
            Assert.That(InterpretationPoller.TotalDeadlineSeconds, Is.EqualTo(330f), "backend stale window 300 s + 30 s");
            Assert.That(InterpretationPoller.MaxConsecutiveNetworkErrors, Is.EqualTo(3));
        }

        [Test]
        public void ApplyingAFailureRecordsCopyAndRetryability()
        {
            var snapshot = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 601 }, LocalReadingSimulator.CreatePlaceholderDraws(1));

            InterpretationPoller.ApplyFailure(snapshot, InterpretationFailure.ConnectionLost);
            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Failed));
            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationConnectionLost));
            Assert.That(snapshot.canRetry, Is.True);

            InterpretationPoller.ApplyFailure(snapshot, InterpretationFailure.SessionExpired);
            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationSessionExpired));
            Assert.That(snapshot.canRetry, Is.False);
        }

        [Test]
        public void UsingOfflineTextKeepsTheDrawnCardsAndSwitchesSource()
        {
            var cards = LocalReadingSimulator.CreatePlaceholderDraws(3);
            var snapshot = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 601, question = "问题" }, cards);
            snapshot.spreadName = "过去现在未来";
            InterpretationPoller.ApplyFailure(snapshot, InterpretationFailure.Unavailable);

            InterpretationPoller.ApplyOffline(snapshot);

            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(snapshot.cardDraws, Is.SameAs(cards));
            Assert.That(snapshot.predictionId, Is.EqualTo(601));
            Assert.That(snapshot.warning, Is.EqualTo(ReleaseUxCopy.OfflineWarning));
            Assert.That(snapshot.cardAnalysis, Does.Contain(cards[0].tarot_card.name_zh));
            Assert.That(snapshot.failureMessage, Is.Empty);
            Assert.That(snapshot.canRetry, Is.False);
        }

        [Test]
        public void GameBootstrapSharesTheClientAndHostsThePoller()
        {
            var source = File.ReadAllText("Assets/Scripts/Core/GameBootstrap.cs");

            Assert.That(source, Does.Contain("EnsureService<ApiClient>()"), "control: the scan reads the real bootstrap");
            Assert.That(source, Does.Contain("ApiClient.SetShared(GetComponent<ApiClient>())"));
            Assert.That(source, Does.Contain("EnsureService<InterpretationPoller>()"));
        }

        // Phase 66: later tasks append tests above this line.
```

- [ ] **Step 2: 新建 PlayMode 轮询器测试**

确认 `Assets/Tests/PlayMode/Phase66InterpretationPollerTests.cs` 不存在，然后创建：

```csharp
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 66: the persistent InterpretationPoller against a scripted backend (spec
    /// 8.4). Poll delays are scaled by 0.05, so the 2/2/3/3/5 s schedule runs in
    /// tenths of a second.
    /// </summary>
    public sealed class Phase66InterpretationPollerTests
    {
        private const int PredictionId = 601;
        private const string AsyncPath = "/api/v1/records/601/interpret/async";
        private const string DetailPath = "/api/v1/records/601";
        private const string RefreshPath = "/api/v1/refresh";

        private MockTarotBackend server;
        private GameObject boot;
        private ApiClient client;
        private InterpretationPoller poller;
        private List<InterpretationState> observed;

        [SetUp]
        public void SetUp()
        {
            ApiClient.ClearShared();
            server = MockTarotBackend.Start();
            boot = new GameObject("Phase66_PollerBoot");
            client = boot.AddComponent<ApiClient>();
            client.BaseUrl = server.ApiBaseUrl;
            client.SetAccessToken("test-access-token");
            poller = boot.AddComponent<InterpretationPoller>();
            poller.Configure(client, 0.05f, 30f);
            observed = new List<InterpretationState>();
            poller.StateChanged += changed => observed.Add(changed.interpretationState);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(boot);
            server.Dispose();
        }

        [UnityTest]
        public IEnumerator ProcessingTwiceThenCompletedBecomesReady()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath, Processing(), Processing(), Completed("deepseek-chat"));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Ready, 10f, "expected Ready");

            Assert.That(snapshot.predictionId, Is.EqualTo(PredictionId));
            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(snapshot.modelUsed, Is.EqualTo("deepseek-chat"));
            Assert.That(snapshot.summary, Is.EqualTo("概要来自测试桩。"));
            Assert.That(server.Count("POST", AsyncPath), Is.EqualTo(1));
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(3));
            Assert.That(observed, Is.EqualTo(new[] { InterpretationState.Pending, InterpretationState.Ready }));
            Assert.That(poller.IsPolling, Is.False);
        }

        [UnityTest]
        public IEnumerator GenerationSlowerThanARequestTimeoutStaysOnline()
        {
            var unscaled = 0f;
            for (var i = 0; i < 6; i++)
            {
                unscaled += InterpretationPoller.PollDelaySeconds(i);
            }

            Assert.That(unscaled, Is.GreaterThan(15f), "control: at real speed these waits exceed the 15 s request timeout");

            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath,
                Processing(), Processing(), Processing(), Processing(), Processing(), Processing(), Completed("deepseek-chat"));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState != InterpretationState.Pending, 15f, "expected the poll to finish");

            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(7));
        }

        [UnityTest]
        public IEnumerator FailedThenRetrySucceeds()
        {
            server.Script("POST", AsyncPath, Accepted(), Accepted());
            server.Script("GET", DetailPath, Failed(), Completed("deepseek-chat"));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 10f, "expected Failed");
            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationBackendFailed));
            Assert.That(snapshot.canRetry, Is.True);

            poller.Retry();
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Ready, 10f, "expected Ready after Retry");

            Assert.That(server.Count("POST", AsyncPath), Is.EqualTo(2));
            Assert.That(observed, Is.EqualTo(new[]
            {
                InterpretationState.Pending, InterpretationState.Failed, InterpretationState.Pending, InterpretationState.Ready,
            }));
        }

        [UnityTest]
        public IEnumerator ExhaustedAttemptsCannotBeRetried()
        {
            server.Script("POST", AsyncPath, MockTarotBackend.Json(429, MockTarotJson.AttemptsExhaustedDetail));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 10f, "expected Failed");

            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationAttemptsExhausted));
            Assert.That(snapshot.canRetry, Is.False);
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator MissingRecordCannotBeInterpreted()
        {
            server.Script("POST", AsyncPath, MockTarotBackend.Json(404, MockTarotJson.RecordNotFoundDetail));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 10f, "expected Failed");

            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationUnavailable));
            Assert.That(snapshot.canRetry, Is.False);
        }

        [UnityTest]
        public IEnumerator UnauthorizedPollRefreshesAndKeepsPolling()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath,
                MockTarotBackend.Json(401, "{\"detail\":\"Could not validate credentials\"}"), Processing(), Completed("deepseek-chat"));
            server.Script("POST", RefreshPath, MockTarotBackend.Json(200, MockTarotJson.Token("refreshed-token")));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Ready, 10f, "expected Ready");

            Assert.That(server.Count("POST", RefreshPath), Is.EqualTo(1));
            Assert.That(client.AccessToken, Is.EqualTo("refreshed-token"));
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator UnauthorizedWithFailedRefreshExpiresTheSession()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath, MockTarotBackend.Json(401, "{\"detail\":\"Could not validate credentials\"}"));
            server.Script("POST", RefreshPath, MockTarotBackend.Json(401, "{\"detail\":\"Invalid refresh token\"}"));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 10f, "expected Failed");

            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationSessionExpired));
            Assert.That(snapshot.canRetry, Is.False);
            Assert.That(server.Count("POST", RefreshPath), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ThreeConnectionErrorsInARowLoseTheConnection()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath, MockTarotBackend.Aborted());
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 15f, "expected Failed");

            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationConnectionLost));
            Assert.That(snapshot.canRetry, Is.True);
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator TwoConnectionErrorsThenRecoveryKeepsPolling()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath,
                MockTarotBackend.Aborted(), MockTarotBackend.Aborted(), Processing(), Completed("deepseek-chat"));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState != InterpretationState.Pending, 15f, "expected the poll to finish");

            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(4));
            Assert.That(observed, Is.EqualTo(new[] { InterpretationState.Pending, InterpretationState.Ready }));
        }

        [UnityTest]
        public IEnumerator PastTheDeadlineTheReadingTimesOut()
        {
            poller.Configure(client, 0.05f, 0.8f);
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath, Processing());
            var snapshot = OnlineSnapshot(1);
            var startedAt = Time.realtimeSinceStartup;

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 10f, "expected Failed");

            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationTimedOut));
            Assert.That(snapshot.canRetry, Is.True);
            Assert.That(Time.realtimeSinceStartup - startedAt, Is.LessThan(5f));
            Assert.That(server.Count("GET", DetailPath), Is.GreaterThan(0), "control: it polled before timing out");
        }

        [UnityTest]
        public IEnumerator PollingSurvivesTheCallerBeingDestroyed()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath, Processing(), Processing(), Processing(), Completed("deepseek-chat"));
            var snapshot = OnlineSnapshot(1);
            var room = new GameObject("Phase66_ReadingRoomStandIn");
            var roomService = room.AddComponent<BackendReadingService>();
            roomService.StartCoroutine(BeginFromRoom(poller, snapshot));

            yield return WaitUntil(
                () => snapshot.interpretationState == InterpretationState.Pending && server.Count("GET", DetailPath) >= 1,
                10f,
                "expected polling to start");
            Object.Destroy(room);
            yield return null;
            Assert.That(room == null, Is.True, "control: the caller is gone");

            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Ready, 10f,
                "expected Ready after the caller was destroyed");
            Assert.That(observed, Is.EqualTo(new[] { InterpretationState.Pending, InterpretationState.Ready }));
        }

        [UnityTest]
        public IEnumerator UseOfflineStopsPollingAndKeepsTheCards()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath, Processing());
            var snapshot = OnlineSnapshot(3);
            var cards = snapshot.cardDraws;

            poller.Begin(snapshot);
            yield return WaitUntil(() => server.Count("GET", DetailPath) >= 1, 10f, "expected polling to start");

            poller.UseOffline();
            var pollsAtSwitch = server.Count("GET", DetailPath);
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(snapshot.cardDraws, Is.SameAs(cards));
            Assert.That(snapshot.warning, Is.EqualTo(ReleaseUxCopy.OfflineWarning));
            Assert.That(server.Count("GET", DetailPath), Is.LessThanOrEqualTo(pollsAtSwitch + 1),
                "at most the request already in flight may still arrive");
            Assert.That(observed, Is.EqualTo(new[] { InterpretationState.Pending, InterpretationState.Ready }));
            Assert.That(poller.IsPolling, Is.False);
        }

        [UnityTest]
        public IEnumerator AlreadyStoredInterpretationCompletesWithoutPolling()
        {
            server.Script("POST", AsyncPath, MockTarotBackend.Json(200, MockTarotJson.Interpretation(PredictionId, "mock_ai")));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Ready, 10f, "expected Ready");

            Assert.That(snapshot.modelUsed, Is.EqualTo("mock_ai"));
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(0));
        }

        private static IEnumerator BeginFromRoom(InterpretationPoller target, ReadingSessionSnapshot snapshot)
        {
            yield return null;
            target.Begin(snapshot);
        }

        private static ReadingSessionSnapshot OnlineSnapshot(int cardCount)
        {
            var snapshot = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse
                {
                    id = PredictionId,
                    spread_type_id = 1,
                    question = "此刻我最需要留意什么？",
                    question_type = "general",
                },
                LocalReadingSimulator.CreatePlaceholderDraws(cardCount));
            snapshot.spreadName = "单牌抽取";
            return snapshot;
        }

        private static MockTarotBackend.Reply Accepted()
        {
            return MockTarotBackend.Json(202, MockTarotJson.Accepted(PredictionId));
        }

        private static MockTarotBackend.Reply Processing()
        {
            return MockTarotBackend.Json(200, MockTarotJson.Detail(PredictionId, "processing", 1, null));
        }

        private static MockTarotBackend.Reply Failed()
        {
            return MockTarotBackend.Json(200, MockTarotJson.Detail(PredictionId, "failed", 1, null));
        }

        private static MockTarotBackend.Reply Completed(string model)
        {
            return MockTarotBackend.Json(200, MockTarotJson.Detail(
                PredictionId, "completed", 1, MockTarotJson.Interpretation(PredictionId, model)));
        }

        private static IEnumerator WaitUntil(System.Func<bool> predicate, float seconds, string message)
        {
            var timeoutAt = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(predicate(), Is.True, message);
        }
    }
}
```

- [ ] **Step 3: 运行，确认失败**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t5-red -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
```

预期：`NO RESULTS XML`，列出 `error CS0246` 或 `CS0103`，提示找不到 `InterpretationPoller`。

- [ ] **Step 4: 实现轮询器，并在 Boot 上创建它**

确认 `Assets/Scripts/Network/InterpretationPoller.cs` 不存在，然后创建：

```csharp
using System;
using System.Collections;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEngine;

namespace TarotUnity.Network
{
    /// <summary>
    /// Phase 66: fetches the background AI interpretation for the current online
    /// reading. It lives on the persistent Boot object (GameBootstrap), so leaving the
    /// reading room for the Result screen never interrupts it. It mutates the snapshot
    /// it was given in place - ReadingSessionStore.Current keeps pointing at the same
    /// object - and raises StateChanged on every state change.
    /// </summary>
    public sealed class InterpretationPoller : MonoBehaviour
    {
        // Backend AI_INTERPRETATION_STALE_SECONDS (300) plus 30 s of slack.
        public const float TotalDeadlineSeconds = 330f;
        public const int MaxConsecutiveNetworkErrors = 3;

        [SerializeField] private ApiClient apiClient;
        [SerializeField] private float delayScale = 1f;
        [SerializeField] private float deadlineSeconds = TotalDeadlineSeconds;

        // Bumped by Begin, Stop, UseOffline and OnDestroy. A running poll re-checks its
        // own id after every yield and exits once it is stale, so an in-flight request
        // finishes and disposes normally instead of being cut off mid-coroutine.
        private int runId;

        public static InterpretationPoller Instance { get; private set; }

        public event Action<ReadingSessionSnapshot> StateChanged;

        public ReadingSessionSnapshot Current { get; private set; }

        public bool IsPolling { get; private set; }

        private ApiClient Client
        {
            get
            {
                if (apiClient != null)
                {
                    return apiClient;
                }

                return ApiClient.Shared != null ? ApiClient.Shared : GetComponent<ApiClient>();
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            runId++;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Configure(ApiClient client, float pollDelayScale, float totalDeadlineSeconds)
        {
            apiClient = client;
            delayScale = Mathf.Max(0f, pollDelayScale);
            deadlineSeconds = totalDeadlineSeconds;
        }

        public static float PollDelaySeconds(int attemptIndex)
        {
            if (attemptIndex < 2)
            {
                return 2f;
            }

            return attemptIndex < 4 ? 3f : 5f;
        }

        public void Begin(ReadingSessionSnapshot snapshot)
        {
            runId++;
            IsPolling = false;
            Current = snapshot;
            if (snapshot == null || snapshot.source != ReadingSource.Online || snapshot.predictionId <= 0)
            {
                return;
            }

            snapshot.interpretationState = InterpretationState.Pending;
            snapshot.failureMessage = string.Empty;
            snapshot.canRetry = false;
            IsPolling = true;
            var run = runId;
            StateChanged?.Invoke(snapshot);
            StartCoroutine(PollRoutine(run, snapshot));
        }

        public void Retry()
        {
            if (Current != null)
            {
                Begin(Current);
            }
        }

        public void Stop()
        {
            runId++;
            IsPolling = false;
        }

        public void UseOffline()
        {
            runId++;
            IsPolling = false;
            if (Current == null)
            {
                return;
            }

            ApplyOffline(Current);
            StateChanged?.Invoke(Current);
        }

        public static void ApplyFailure(ReadingSessionSnapshot snapshot, InterpretationFailure failure)
        {
            if (snapshot == null)
            {
                return;
            }

            snapshot.interpretationState = InterpretationState.Failed;
            snapshot.failureMessage = ReleaseUxCopy.ForInterpretationFailure(failure);
            snapshot.canRetry = ReleaseUxCopy.CanRetry(failure);
        }

        // Keeps the dealt cards and the record id; only the text becomes the local
        // reading, and the snapshot never switches back to online afterwards.
        public static void ApplyOffline(ReadingSessionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            var offline = LocalReadingSimulator.CreateSession(
                snapshot.spreadId,
                snapshot.spreadName,
                snapshot.question,
                snapshot.questionType,
                snapshot.cardDraws);
            snapshot.summary = offline.summary;
            snapshot.overallInterpretation = offline.overallInterpretation;
            snapshot.cardAnalysis = offline.cardAnalysis;
            snapshot.advice = offline.advice;
            snapshot.warning = offline.warning;
            snapshot.source = ReadingSource.Offline;
            snapshot.interpretationState = InterpretationState.Ready;
            snapshot.modelUsed = string.Empty;
            snapshot.failureMessage = string.Empty;
            snapshot.canRetry = false;
        }

        private IEnumerator PollRoutine(int run, ReadingSessionSnapshot snapshot)
        {
            var client = Client;
            if (client == null)
            {
                Fail(run, snapshot, InterpretationFailure.ConnectionLost, "no ApiClient");
                yield break;
            }

            var startedAt = Time.realtimeSinceStartup;
            var accepted = false;
            var attempt = 0;
            var networkErrors = 0;
            var refreshedThisRequest = false;

            while (run == runId)
            {
                if (Time.realtimeSinceStartup - startedAt >= deadlineSeconds)
                {
                    Fail(run, snapshot, InterpretationFailure.TimedOut, "deadline reached");
                    yield break;
                }

                ApiError error = null;
                if (!accepted)
                {
                    AsyncInterpretationResult result = null;
                    yield return client.PostInterpretAsync(snapshot.predictionId, value => result = value, value => error = value);
                    if (run != runId)
                    {
                        yield break;
                    }

                    if (error == null)
                    {
                        if (result.outcome == AsyncInterpretationOutcome.AlreadyReady)
                        {
                            Complete(run, snapshot, result.interpretation);
                            yield break;
                        }

                        accepted = true;
                    }
                    else if (error.Kind == ApiErrorKind.RateLimited)
                    {
                        Fail(run, snapshot, InterpretationFailure.AttemptsExhausted, error.RawMessage);
                        yield break;
                    }
                    else if (error.Kind != ApiErrorKind.Unauthorized && !error.IsTransient)
                    {
                        Fail(run, snapshot, InterpretationFailure.Unavailable, error.RawMessage);
                        yield break;
                    }
                }
                else
                {
                    PredictionDetailResponse detail = null;
                    yield return client.FetchRecordDetail(snapshot.predictionId, value => detail = value, value => error = value);
                    if (run != runId)
                    {
                        yield break;
                    }

                    if (error == null)
                    {
                        // Plan 1 contract: a stored interpretation means done, whatever the status says.
                        if (ReadingSessionMapper.HasInterpretation(detail))
                        {
                            Complete(run, snapshot, detail.interpretation);
                            yield break;
                        }

                        if (detail != null && string.Equals(detail.status, "failed", StringComparison.OrdinalIgnoreCase))
                        {
                            Fail(run, snapshot, InterpretationFailure.BackendFailed, "status failed");
                            yield break;
                        }
                    }
                    else if (error.Kind == ApiErrorKind.NotFound || error.Kind == ApiErrorKind.BadRequest)
                    {
                        Fail(run, snapshot, InterpretationFailure.Unavailable, error.RawMessage);
                        yield break;
                    }
                }

                if (error == null)
                {
                    networkErrors = 0;
                    refreshedThisRequest = false;
                }
                else if (error.Kind == ApiErrorKind.Unauthorized)
                {
                    if (refreshedThisRequest)
                    {
                        Fail(run, snapshot, InterpretationFailure.SessionExpired, error.RawMessage);
                        yield break;
                    }

                    refreshedThisRequest = true;
                    var refreshed = false;
                    yield return client.Refresh(_ => refreshed = true, _ => refreshed = false);
                    if (run != runId)
                    {
                        yield break;
                    }

                    if (!refreshed)
                    {
                        Fail(run, snapshot, InterpretationFailure.SessionExpired, error.RawMessage);
                        yield break;
                    }

                    // Repeat the same request straight away with the refreshed token.
                    continue;
                }
                else
                {
                    networkErrors++;
                    Debug.Log($"InterpretationPoller: prediction {snapshot.predictionId} request failed " +
                        $"({networkErrors}/{MaxConsecutiveNetworkErrors}): {error.RawMessage}");
                    if (networkErrors >= MaxConsecutiveNetworkErrors)
                    {
                        Fail(run, snapshot, InterpretationFailure.ConnectionLost, error.RawMessage);
                        yield break;
                    }
                }

                var remaining = deadlineSeconds - (Time.realtimeSinceStartup - startedAt);
                var delay = Mathf.Min(PollDelaySeconds(attempt) * delayScale, Mathf.Max(0f, remaining));
                attempt++;
                if (delay > 0f)
                {
                    yield return new WaitForSecondsRealtime(delay);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private void Complete(int run, ReadingSessionSnapshot snapshot, InterpretationResponse interpretation)
        {
            if (run != runId)
            {
                return;
            }

            ReadingSessionMapper.ApplyInterpretation(snapshot, interpretation);
            IsPolling = false;
            StateChanged?.Invoke(snapshot);
        }

        private void Fail(int run, ReadingSessionSnapshot snapshot, InterpretationFailure failure, string detail)
        {
            if (run != runId)
            {
                return;
            }

            ApplyFailure(snapshot, failure);
            IsPolling = false;
            Debug.Log($"InterpretationPoller: prediction {snapshot.predictionId} stopped with {failure} ({detail}).");
            StateChanged?.Invoke(snapshot);
        }
    }
}
```

`Assets/Scripts/Core/GameBootstrap.cs`，把

```csharp
            EnsureService<BackendSessionBootstrap>();
```

替换为

```csharp
            EnsureService<BackendSessionBootstrap>();
            EnsureService<InterpretationPoller>();
```

- [ ] **Step 5: 运行，确认通过**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t5-green -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
bash "$S/online-b-run/ut.sh" PlayMode t5-green -testFilter TarotUnity.Tests.PlayMode.Phase66InterpretationPollerTests
```

预期：EditMode `total=47 passed=47 failed=0`；PlayMode `total=13 passed=13 failed=0`。

某个场景失败时：
- 先看失败信息里的请求次数（`Count`）是否与脚本编排一致。次数不一致，说明是测试桩或队列语义的问题（仪器问题）：STOP 并报告。
- 次数一致但状态不对，才去修改轮询器，并且要对照上面的错误处理规则表。

- [ ] **Step 6: 全量回归**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t5
bash "$S/online-b-run/ut.sh" PlayMode t5
```

预期：EditMode `total=E0+47 failed=0`；PlayMode `total=P0+19 failed=0`。

- [ ] **Step 7: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
ls Assets/Scripts/Network/InterpretationPoller.cs.meta Assets/Tests/PlayMode/Phase66InterpretationPollerTests.cs.meta
git add Assets/Scripts/Network/InterpretationPoller.cs Assets/Scripts/Network/InterpretationPoller.cs.meta \
  Assets/Scripts/Core/GameBootstrap.cs \
  Assets/Tests/PlayMode/Phase66InterpretationPollerTests.cs Assets/Tests/PlayMode/Phase66InterpretationPollerTests.cs.meta \
  Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs
git commit -F - <<'EOF'
feat(unity): poll background interpretations from a persistent poller

InterpretationPoller lives on the Boot object, starts the async
interpretation, polls the record on a 2/2/3/3/5 s schedule within 330 s,
refreshes once on 401, gives up after three network errors in a row, and
can retry or switch the reading to offline text.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
```

---

### Task 6: 占卜房新流程（先发牌，解读在后台生成）

**Files:**
- Modify: `Assets/Scripts/UI/ReadingRoomController.cs`：
  - 字段区（`backendSpreads` 之后）
  - `OnDestroy`
  - `DrawRoutine` 整个方法（原第 158–247 行）
  - `ResultReadyRoutine`
  - `LoadBackendSpreadsRoutine`
- Modify: `Assets/Scripts/Network/BackendReadingService.cs`：删除 `CompleteReading` 和 `HasError` 两个方法
- Create: `Assets/Tests/PlayMode/Phase66ReadingRoomOnlineFlowTests.cs`
- Test: `Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs`（追加 2 个测试）

**Interfaces:**
- Consumes：
  - Task 2：`ReleaseUxCopy` 的流程文案常量、`ForStartReadingFailure`、`OfflineBecauseSpread`、`OfflineWarning`
  - Task 4：`BackendReadingService.StartReading`、`RecoverSession`；测试桩 `MockTarotJson.Spreads`
  - Task 5：`InterpretationPoller.Instance`、`Begin`、`StateChanged`、`ApplyOffline`
- Produces（行为）：
  - 洗牌动画和 `StartReading` 同时进行。
  - 成功时：先 `ReadingSessionStore.Save`，再 `InterpretationPoller.Begin`，然后用后端的牌发牌。
  - 失败时：发离线牌，底部状态栏显示 7.3 表中对应的文案。
  - 翻牌提示为 `FlowFlipPrompt`；全部翻开后依次显示 `FlowAllRevealed`、`FlowResultReady`。
  - 底部状态栏跟随轮询器状态。
  - 没有 Boot 常驻轮询器时（直接运行场景），在线局就地转为离线文本。

- [ ] **Step 1: 追加 EditMode 守护测试**

用 Edit 把锚点注释 `        // Phase 66: later tasks append tests above this line.` 替换为：

```csharp
        private static readonly string[] RetiredReadingRoomLiterals =
        {
            "\"Shuffling...\"",
            "\"Creating backend reading...\"",
            "\"Dealing cards...\"",
            "\"Click each card to flip it.\"",
            "\"The reading is almost ready.\"",
            "\"The reading is ready.\"",
            "\"What should I notice now?\"",
            "\"Backend spreads loaded.\"",
        };

        [Test]
        public void ReadingRoomFlowCopyIsChineseAndCentralised()
        {
            var flowCopy = new[]
            {
                ReleaseUxCopy.DefaultQuestion,
                ReleaseUxCopy.FlowShuffling,
                ReleaseUxCopy.FlowDealing,
                ReleaseUxCopy.FlowFlipPrompt,
                ReleaseUxCopy.FlowAllRevealed,
                ReleaseUxCopy.FlowResultReady,
                ReleaseUxCopy.InterpretationGenerating,
                ReleaseUxCopy.InterpretationReadyHint,
                ReleaseUxCopy.OnlineReady,
            };
            foreach (var line in flowCopy)
            {
                Assert.That(line, Is.Not.Empty);
                Assert.That(Regex.IsMatch(line, "[A-Za-z]"), Is.False, $"'{line}' should not contain ASCII letters");
            }

            var source = File.ReadAllText("Assets/Scripts/UI/ReadingRoomController.cs");
            Assert.That(source, Does.Contain("ReleaseUxCopy.FlowShuffling"), "control: the scan reads the real controller");
            foreach (var literal in RetiredReadingRoomLiterals)
            {
                Assert.That(source, Does.Not.Contain(literal), $"{literal} should come from ReleaseUxCopy");
            }
        }

        [Test]
        public void CompleteReadingIsRetired()
        {
            Assert.That(typeof(BackendReadingService).GetMethod("StartReading"), Is.Not.Null,
                "control: reflection sees the service");
            Assert.That(typeof(BackendReadingService).GetMethod("CompleteReading"), Is.Null,
                "the old path waited on the AI before dealing; online readings use StartReading");
        }

        // Phase 66: later tasks append tests above this line.
```

- [ ] **Step 2: 新建 PlayMode 占卜房流程测试**

确认 `Assets/Tests/PlayMode/Phase66ReadingRoomOnlineFlowTests.cs` 不存在，然后创建：

```csharp
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 66: the reading room against a scripted backend. The table deals the
    /// backend's cards while the interpretation is still generating, and every failed
    /// start falls back to an offline reading that shows the spec 7.3 copy.
    /// </summary>
    public sealed class Phase66ReadingRoomOnlineFlowTests
    {
        private MockTarotBackend server;
        private GameObject boot;
        private ApiClient client;
        private InterpretationPoller poller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ReadingSessionStore.Clear();
            server = MockTarotBackend.Start();
            boot = new GameObject("Phase66_RoomTestBoot");
            Object.DontDestroyOnLoad(boot);
            client = boot.AddComponent<ApiClient>();
            client.BaseUrl = server.ApiBaseUrl;
            client.SetAccessToken("test-access-token");
            ApiClient.SetShared(client);
            poller = boot.AddComponent<InterpretationPoller>();
            poller.Configure(client, 0.05f, 30f);
            server.Script("GET", "/api/v1/spreads/", MockTarotBackend.Json(200,
                MockTarotJson.Spreads((11, "单牌抽取", 1), (12, "过去现在未来", 3), (13, "爱情牌阵", 5))));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ApiClient.ClearShared();
            Object.Destroy(boot);
            ReadingSessionStore.Clear();
            yield return null;
            server.Dispose();
        }

        [UnityTest]
        public IEnumerator OnlineReadingDealsBackendCardsBeforeTheInterpretationIsReady()
        {
            server.Script("POST", "/api/v1/records/", MockTarotBackend.Json(200, MockTarotJson.Record(701, 12)));
            server.Script("POST", "/api/v1/records/701/draw", MockTarotBackend.Json(200, MockTarotJson.Draw(701, 3)));
            server.Script("GET", "/api/v1/records/701/cards", MockTarotBackend.Json(200, MockTarotJson.Cards(701, 3)));
            server.Script("POST", "/api/v1/records/701/interpret/async", MockTarotBackend.Json(202, MockTarotJson.Accepted(701)));
            server.Script("GET", "/api/v1/records/701",
                MockTarotBackend.Json(200, MockTarotJson.Detail(701, "processing", 3, null)));

            yield return LoadReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "threeCardButton").onClick.Invoke();
            GetField<TMP_InputField>(room, "questionInput").text = string.Empty;
            GetField<Button>(room, "drawButton").onClick.Invoke();

            yield return WaitUntil(
                () => deck.ActiveCards.Count == 3 && flow.State == ReadingFlowState.WaitingForFlip,
                20f,
                "expected three dealt cards waiting for flips");

            var session = ReadingSessionStore.Current;
            Assert.That(session.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(session.predictionId, Is.EqualTo(701));
            Assert.That(session.spreadId, Is.EqualTo(12));
            Assert.That(session.spreadName, Is.EqualTo("过去现在未来"));
            Assert.That(session.question, Is.EqualTo(ReleaseUxCopy.DefaultQuestion));
            Assert.That(server.LastBody("POST", "/api/v1/records/"), Does.Contain("\"spread_type_id\":12"));
            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending),
                "the table dealt before the interpretation existed");
            Assert.That(GetField<TMP_Text>(room, "flowStatusText").text, Is.EqualTo(ReleaseUxCopy.FlowFlipPrompt));
            Assert.That(GetField<TMP_Text>(room, "releaseStatusText").text, Is.EqualTo(ReleaseUxCopy.InterpretationGenerating));

            server.Script("GET", "/api/v1/records/701", MockTarotBackend.Json(200,
                MockTarotJson.Detail(701, "completed", 3, MockTarotJson.Interpretation(701, "deepseek-chat"))));
            yield return WaitUntil(() => session.interpretationState == InterpretationState.Ready, 10f,
                "expected the interpretation to arrive");

            Assert.That(session.modelUsed, Is.EqualTo("deepseek-chat"));
            Assert.That(GetField<TMP_Text>(room, "releaseStatusText").text, Is.EqualTo(ReleaseUxCopy.InterpretationReadyHint));
        }

        [UnityTest]
        public IEnumerator GuestQuotaOnStartFallsBackToAnOfflineReading()
        {
            server.Script("POST", "/api/v1/records/",
                MockTarotBackend.Json(429, MockTarotJson.GuestLimitDetail).WithHeader("Retry-After", "5400"));

            yield return LoadReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "oneCardButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            yield return WaitUntil(() => deck.ActiveCards.Count == 1, 20f, "expected one dealt card");

            var session = ReadingSessionStore.Current;
            Assert.That(server.Count("POST", "/api/v1/records/"), Is.EqualTo(1), "control: the online start was attempted");
            Assert.That(session.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(session.warning, Is.EqualTo(ReleaseUxCopy.OfflineWarning));
            Assert.That(GetField<TMP_Text>(room, "releaseStatusText").text,
                Is.EqualTo("今天的访客占卜次数已用完，约 2 小时后恢复。这一局使用离线解读。"));
            foreach (var entry in server.RequestLog)
            {
                Assert.That(entry, Does.Not.Contain("/draw"));
                Assert.That(entry, Does.Not.Contain("interpret"));
            }
        }

        [UnityTest]
        public IEnumerator SpreadMissingOnTheBackendStaysOfflineWithoutCreatingARecord()
        {
            yield return LoadReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "celticCrossButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            yield return WaitUntil(() => deck.ActiveCards.Count == 10, 30f, "expected ten dealt cards");

            Assert.That(ReadingSessionStore.Current.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(ReadingSessionStore.Current.cardDraws, Has.Length.EqualTo(10));
            Assert.That(GetField<TMP_Text>(room, "releaseStatusText").text, Is.EqualTo(ReleaseUxCopy.OfflineBecauseSpread));
            Assert.That(server.Count("POST", "/api/v1/records/"), Is.EqualTo(0),
                "a ten-card reading must not be sent under another spread's id");
        }

        [UnityTest]
        public IEnumerator CardCountMismatchFromTheBackendGoesOffline()
        {
            server.Script("POST", "/api/v1/records/", MockTarotBackend.Json(200, MockTarotJson.Record(702, 12)));
            server.Script("POST", "/api/v1/records/702/draw", MockTarotBackend.Json(200, MockTarotJson.Draw(702, 5)));
            server.Script("GET", "/api/v1/records/702/cards", MockTarotBackend.Json(200, MockTarotJson.Cards(702, 5)));

            yield return LoadReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "threeCardButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            yield return WaitUntil(() => deck.ActiveCards.Count == 3, 20f, "expected three dealt cards");

            Assert.That(server.Count("GET", "/api/v1/records/702/cards"), Is.EqualTo(1), "control: the backend dealt five");
            Assert.That(ReadingSessionStore.Current.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(ReadingSessionStore.Current.cardDraws, Has.Length.EqualTo(3));
            Assert.That(GetField<TMP_Text>(room, "releaseStatusText").text, Is.EqualTo(ReleaseUxCopy.OfflineBecauseUnavailable));
            Assert.That(server.Count("POST", "/api/v1/records/702/interpret/async"), Is.EqualTo(0));
        }

        private static IEnumerator LoadReadingRoom()
        {
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            yield return null;
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "ReadingRoom"
                    && Object.FindFirstObjectByType<ReadingRoomController>() != null,
                10f,
                "expected the ReadingRoom scene");
        }

        private static IEnumerator WaitForBackendSpreads(ReadingRoomController room)
        {
            var field = typeof(ReadingRoomController).GetField("backendSpreads", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "control: backendSpreads field exists");
            yield return WaitUntil(() => field.GetValue(room) != null, 10f, "expected backend spreads to load");
        }

        private static IEnumerator WaitUntil(System.Func<bool> predicate, float seconds, string message)
        {
            var timeoutAt = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(predicate(), Is.True, message);
        }

        private static T GetField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}");

            var value = field.GetValue(target) as T;
            Assert.That(value, Is.Not.Null, $"Field {fieldName} on {target.GetType().Name} is null");
            return value;
        }
    }
}
```

- [ ] **Step 3: 运行，确认失败**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t6-red -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
bash "$S/online-b-run/ut.sh" PlayMode t6-red -testFilter TarotUnity.Tests.PlayMode.Phase66ReadingRoomOnlineFlowTests
```

预期：
- EditMode：`total=49 failed=2`，失败的是 `ReadingRoomFlowCopyIsChineseAndCentralised`（`control` 断言）和 `CompleteReadingIsRetired`。
- PlayMode：`total=4 failed=4`。旧流程会调用同步 `/interpret`（测试桩没有编排，返回 404），然后退回英文原因的离线局；凯尔特十字会以本地 id 3 发出建记录请求。

如果 PlayMode 的某个测试**通过了**，STOP 并报告：说明这个测试没有测到旧流程的问题。

- [ ] **Step 4: 改写占卜房控制器**

`Assets/Scripts/UI/ReadingRoomController.cs`，依次做下面几处 Edit。

(a) 把

```csharp
        private SpreadSummary[] backendSpreads;
```

替换为

```csharp
        private SpreadSummary[] backendSpreads;
        private InterpretationPoller subscribedPoller;

        // Phase 66: the longest the draw waits for the online start (three requests,
        // a session recovery and one retry, each bounded by the request timeout)
        // before dealing an offline reading instead.
        private const float OnlineStartTimeoutSeconds = 150f;
```

(b) 把

```csharp
            if (deckController != null)
            {
                deckController.CardDealt -= HandleCardDealt;
            }
        }
```

替换为

```csharp
            if (deckController != null)
            {
                deckController.CardDealt -= HandleCardDealt;
            }

            UnsubscribePoller();
        }
```

(c) 把 `DrawRoutine` 整个方法替换掉：范围从 `        private IEnumerator DrawRoutine()` 开始，到方法末尾的

```csharp
            SetStatus("Click each card to flip it.");
            drawInProgress = false;
        }
```

为止（原第 158–247 行），替换为：

```csharp
        private IEnumerator DrawRoutine()
        {
            drawInProgress = true;
            SetResultButtonVisible(false);
            SetDrawControls(false);

            var question = string.IsNullOrWhiteSpace(questionInput?.text)
                ? ReleaseUxCopy.DefaultQuestion
                : questionInput.text.Trim();

            flowController?.SetQuestion(question, "general");
            flowController?.BeginShuffle();
            cameraChoreography?.FocusDeck();
            ritualFeedback?.PlayCue(PresentationCueId.ShuffleStarted, deckController != null ? deckController.transform : null);
            deckShuffle?.Play();
            SetStatus(ReleaseUxCopy.FlowShuffling);

            // Phase 66: the online start (record + draw + cards, no AI) runs while the
            // shuffle plays. The interpretation is generated in the background and
            // fetched by the persistent InterpretationPoller once the deal begins.
            var attempt = new OnlineStartAttempt();
            if (ShouldTryBackend())
            {
                StartCoroutine(StartOnlineReadingRoutine(question, attempt));
            }
            else
            {
                attempt.Done = true;
            }

            yield return new WaitForSeconds(rhythmDirector != null
                ? rhythmDirector.ResolvePause(PresentationCueId.ShuffleStarted)
                : 0.8f);

            var giveUpAt = Time.realtimeSinceStartup + OnlineStartTimeoutSeconds;
            yield return new WaitUntil(() => attempt.Done || Time.realtimeSinceStartup > giveUpAt);
            if (!attempt.Done)
            {
                attempt.Session = null;
                attempt.OfflineMessage = ReleaseUxCopy.OfflineBecauseNetwork;
                attempt.RawError = $"the online start did not finish within {OnlineStartTimeoutSeconds} s";
                Debug.Log($"ReadingRoom: {attempt.RawError}");
            }

            var session = attempt.Session;
            if (session == null && attempt.OfflineMessage != null && backendMode == BackendIntegrationMode.BackendOnly)
            {
                var message = ReleaseUxCopy.BackendOnlyFailure(attempt.RawError);
                SetStatus(message);
                SetReleaseStatus(message);
                SetDrawControls(true);
                drawInProgress = false;
                yield break;
            }

            if (session == null)
            {
                if (attempt.OfflineMessage != null)
                {
                    SetReleaseStatus(attempt.OfflineMessage);
                }

                session = LocalReadingSimulator.CreateSession(
                    selectedSpreadId,
                    selectedSpreadName,
                    question,
                    "general",
                    CreateLocalDraws());
            }

            ReadingSessionStore.Save(session);
            if (session.source == ReadingSource.Online)
            {
                BeginInterpretation(session);
            }

            flowController?.BeginDeal();
            SetStatus(ReleaseUxCopy.FlowDealing);

            var draws = session.cardDraws ?? CreateLocalDraws();
            var slots = flowController != null ? flowController.GetSelectedSpreadSlots() : new List<Transform>();
            if (deckController != null)
            {
                yield return deckController.DealCards(draws, slots);
                if (rhythmDirector != null && rhythmDirector.DealSettleSeconds > 0f)
                {
                    yield return new WaitForSeconds(rhythmDirector.DealSettleSeconds);
                }

                WireActiveCards();
            }

            flowController?.WaitForCardFlips();
            cameraChoreography?.FocusSpread(selectedCardCount);
            SetStatus(ReleaseUxCopy.FlowFlipPrompt);
            drawInProgress = false;
        }

        private sealed class OnlineStartAttempt
        {
            public bool Done;
            public ReadingSessionSnapshot Session;
            public string OfflineMessage;
            public string RawError;
        }

        private IEnumerator StartOnlineReadingRoutine(string question, OnlineStartAttempt attempt)
        {
            if (backendSpreads == null)
            {
                yield return LoadBackendSpreadsRoutine();
            }

            // Phase 66: only ask for a spread the backend really has with this card
            // count - a local spread id would name a different backend spread.
            var backendSpread = FindBackendSpread(selectedCardCount);
            if (backendSpread == null)
            {
                attempt.OfflineMessage = ReleaseUxCopy.OfflineBecauseSpread;
                attempt.RawError = $"no backend spread with {selectedCardCount} cards";
                Debug.Log($"ReadingRoom: online reading skipped - {attempt.RawError}");
                attempt.Done = true;
                yield break;
            }

            var payload = new PredictionCreateRequest
            {
                question = question,
                question_type = "general",
                spread_type_id = backendSpread.id,
            };

            ReadingSessionSnapshot session = null;
            ApiError error = null;
            yield return backendReadingService.StartReading(payload, value => session = value, value => error = value);

            // Spec 6.6 step 2: nothing exists yet, so a rejected token may refresh or
            // even become a new guest before one retry.
            if (error != null && error.Kind == ApiErrorKind.Unauthorized)
            {
                var recovered = false;
                yield return backendReadingService.RecoverSession(value => recovered = value);
                if (recovered)
                {
                    error = null;
                    session = null;
                    yield return backendReadingService.StartReading(payload, value => session = value, value => error = value);
                }
            }

            if (error == null && session != null && session.cardDraws.Length != selectedCardCount)
            {
                error = new ApiError(
                    200,
                    ApiErrorKind.Unexpected,
                    -1,
                    $"200: the backend dealt {session.cardDraws.Length} cards for a {selectedCardCount}-card spread");
            }

            if (error != null || session == null)
            {
                Debug.Log($"ReadingRoom: online reading unavailable - {error?.RawMessage}");
                attempt.OfflineMessage = ReleaseUxCopy.ForStartReadingFailure(error);
                attempt.RawError = error?.RawMessage;
                attempt.Session = null;
            }
            else
            {
                session.spreadId = backendSpread.id;
                session.spreadName = !string.IsNullOrWhiteSpace(backendSpread.name) ? backendSpread.name : selectedSpreadName;
                attempt.Session = session;
            }

            attempt.Done = true;
        }

        private void BeginInterpretation(ReadingSessionSnapshot session)
        {
            var poller = InterpretationPoller.Instance;
            if (poller == null)
            {
                // Without Boot (this scene run on its own) nothing survives into the
                // Result screen to fetch the interpretation, so use the offline text.
                InterpretationPoller.ApplyOffline(session);
                SetReleaseStatus(ReleaseUxCopy.OfflineBecauseUnavailable);
                return;
            }

            if (subscribedPoller != poller)
            {
                UnsubscribePoller();
                subscribedPoller = poller;
                poller.StateChanged += HandleInterpretationStateChanged;
            }

            poller.Begin(session);
        }

        private void HandleInterpretationStateChanged(ReadingSessionSnapshot session)
        {
            if (session == null || session != ReadingSessionStore.Current)
            {
                return;
            }

            if (session.source == ReadingSource.Offline)
            {
                SetReleaseStatus(ReleaseUxCopy.OfflineWarning);
                return;
            }

            switch (session.interpretationState)
            {
                case InterpretationState.Pending:
                    SetReleaseStatus(ReleaseUxCopy.InterpretationGenerating);
                    break;
                case InterpretationState.Ready:
                    SetReleaseStatus(ReleaseUxCopy.InterpretationReadyHint);
                    break;
                default:
                    SetReleaseStatus(session.failureMessage);
                    break;
            }
        }

        private void UnsubscribePoller()
        {
            if (subscribedPoller != null)
            {
                subscribedPoller.StateChanged -= HandleInterpretationStateChanged;
            }

            subscribedPoller = null;
        }
```

(d) 把

```csharp
            SetStatus("The reading is almost ready.");
```

替换为

```csharp
            SetStatus(ReleaseUxCopy.FlowAllRevealed);
```

把

```csharp
            SetStatus("The reading is ready.");
```

替换为

```csharp
            SetStatus(ReleaseUxCopy.FlowResultReady);
```

(e) 把

```csharp
                SetStatus("Backend spreads loaded.");
```

替换为

```csharp
                SetReleaseStatus(ReleaseUxCopy.OnlineReady);
```

`Assets/Scripts/Network/BackendReadingService.cs` 做两处删除（删的是方法代码，不是文件）：
- 删除 `CompleteReading` 整个方法：从 `        public IEnumerator CompleteReading(` 到它结束处的 `        }`，连同其后的一个空行。
- 删除 `HasError` 整个方法：从 `        private static bool HasError(string error, Action<string> onError)` 到它结束处的 `        }`，连同其前面的一个空行。

然后确认两个名字都已不存在：

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
grep -rn 'CompleteReading\|HasError(' Assets/Scripts Assets/Tests Assets/Editor
```

预期：只剩两处结果，一处是 EditMode 测试 `CompleteReadingIsRetired` 里的 `GetMethod("CompleteReading")`，另一处是 `BackendReadingService.StartReading` 上方注释里提到的旧方法名。不应再有任何代码调用 `CompleteReading` 或 `HasError(`。

- [ ] **Step 5: 运行，确认通过**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t6-green -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
bash "$S/online-b-run/ut.sh" PlayMode t6-green -testFilter TarotUnity.Tests.PlayMode.Phase66ReadingRoomOnlineFlowTests
```

预期：EditMode `total=49 passed=49 failed=0`；PlayMode `total=4 passed=4 failed=0`。

- [ ] **Step 6: 全量回归**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t6
bash "$S/online-b-run/ut.sh" PlayMode t6
```

预期：EditMode `total=E0+49 failed=0`；PlayMode `total=P0+23 failed=0`。`VerticalSliceFlowTests` 直接加载 MainMenu，没有 `Shared`，走的是离线分支，必须仍然通过。

- [ ] **Step 7: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
ls Assets/Tests/PlayMode/Phase66ReadingRoomOnlineFlowTests.cs.meta
git add Assets/Scripts/UI/ReadingRoomController.cs Assets/Scripts/Network/BackendReadingService.cs \
  Assets/Tests/PlayMode/Phase66ReadingRoomOnlineFlowTests.cs Assets/Tests/PlayMode/Phase66ReadingRoomOnlineFlowTests.cs.meta \
  Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs
git commit -F - <<'EOF'
feat(unity): deal online readings first and generate the interpretation in the background

The reading room starts the backend record while the shuffle plays, deals
the backend's cards, and hands the interpretation to the persistent poller.
Failed starts fall back to offline readings with the Chinese copy table,
and a spread the backend lacks or a card-count mismatch never goes online.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
```

---

### Task 7: 结果页四种显示与 Phase 66 Bootstrapper

**Files:**
- Modify: `Assets/Scripts/UI/ResultPanelPresenter.cs`
- Modify: `Assets/Scripts/UI/ResultSceneController.cs`
- Create: `Assets/Editor/Phase66ResultInterpretationStateBootstrapper.cs`
- Modify（由 Bootstrapper 写入）: `Assets/Scenes/Result.unity`
- Create: `Assets/Tests/PlayMode/Phase66ResultInterpretationStateTests.cs`
- Test: `Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs`（追加 4 个测试和 1 组 3 个 `TestCase`）

**Interfaces:**
- Consumes：
  - Task 2：结果页文案常量 `ResultPending`、`ResultPendingSlow`、`ModeOffline`、`ModeMock`、`RetryButtonLabel`、`OfflineButtonLabel`
  - Task 3：`ReadingSessionMapper.FromBackendStart`、`ApplyInterpretation`
  - Task 5：`InterpretationPoller.Instance`、`StateChanged`、`Current`、`Retry`、`Stop`、`UseOffline`、`ApplyOffline`、`ApplyFailure`
- Produces：
  - `ResultPanelPresenter` 新增序列化字段：`TMP_Text interpretationStatusText`、`TMP_Text modeLabelText`、`Button retryInterpretationButton`、`Button offlineInterpretationButton`、`CanvasGroup readingContentGroup`、`float readyFadeSeconds = 0.6f`
  - `const float ResultPanelPresenter.PendingSlowNoticeSeconds = 20f`
  - `void ShowPending(ReadingSessionSnapshot)`、`void ShowReady(ReadingSessionSnapshot, bool fadeIn)`、`void ShowFailed(ReadingSessionSnapshot)`、`void ShowOffline(ReadingSessionSnapshot)`
  - `PresentSession` 按快照分流：`Offline` → `ShowOffline`；`Online` 且 `Pending`/`Failed`/`Ready` → 对应方法（`Ready` 不淡入）
  - `static string BuildPendingStatus(float elapsedSeconds)`、`static string ModeLabelFor(ReadingSessionSnapshot)`
  - `ResultSceneController` 新增序列化字段：`Button retryInterpretationButton`、`Button offlineInterpretationButton`
  - 场景对象：
    - `ResultCanvas/ResultReadingScroll/Phase66_InterpretationStatus`：TMP，22 号，象牙色，正文字体，初始不激活
    - `ResultCanvas/Phase66_ModeLabel`：TMP，15 号，金色，右对齐，与 `SpreadNameText` 同一个矩形
    - `ResultCanvas/Phase66_RetryInterpretationButton`：位置 (-280, -300)，初始不激活
    - `ResultCanvas/Phase66_OfflineInterpretationButton`：位置 (280, -300)，初始不激活
    - `ResultReadingScroll/Viewport` 上新增 `CanvasGroup`

- [ ] **Step 1: 追加 EditMode 测试，并新建 PlayMode 结果页测试**

EditMode：用 Edit 把锚点注释 `        // Phase 66: later tasks append tests above this line.` 替换为：

```csharp
        private const string ResultScenePath = "Assets/Scenes/Result.unity";

        [Test]
        public void ResultSceneHasInterpretationStateUiWired()
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var canvas = GameObject.Find("ResultCanvas");
            Assert.That(canvas, Is.Not.Null, "control: ResultCanvas exists");
            var presenter = canvas.GetComponent<ResultPanelPresenter>();
            var controller = canvas.GetComponent<ResultSceneController>();
            Assert.That(presenter, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);

            var presenterSo = new SerializedObject(presenter);
            var status = presenterSo.FindProperty("interpretationStatusText").objectReferenceValue as TMP_Text;
            var mode = presenterSo.FindProperty("modeLabelText").objectReferenceValue as TMP_Text;
            var retry = presenterSo.FindProperty("retryInterpretationButton").objectReferenceValue as Button;
            var offline = presenterSo.FindProperty("offlineInterpretationButton").objectReferenceValue as Button;
            var group = presenterSo.FindProperty("readingContentGroup").objectReferenceValue as CanvasGroup;

            Assert.That(status, Is.Not.Null, "status line");
            Assert.That(mode, Is.Not.Null, "mode label");
            Assert.That(retry, Is.Not.Null, "retry button");
            Assert.That(offline, Is.Not.Null, "offline button");
            Assert.That(group, Is.Not.Null, "reading content group");

            Assert.That(status.transform.parent.name, Is.EqualTo("ResultReadingScroll"));
            Assert.That(group.gameObject.name, Is.EqualTo("Viewport"));
            Assert.That(retry.transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo(ReleaseUxCopy.RetryButtonLabel));
            Assert.That(offline.transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo(ReleaseUxCopy.OfflineButtonLabel));
            Assert.That(retry.gameObject.activeSelf, Is.False, "retry stays hidden until a reading fails");
            Assert.That(offline.gameObject.activeSelf, Is.False, "offline stays hidden until a reading fails");

            var controllerSo = new SerializedObject(controller);
            Assert.That(controllerSo.FindProperty("retryInterpretationButton").objectReferenceValue, Is.SameAs(retry));
            Assert.That(controllerSo.FindProperty("offlineInterpretationButton").objectReferenceValue, Is.SameAs(offline));
        }

        [Test]
        public void InterpretationStateUiUsesTheBundledFontsByRole()
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var presenterSo = new SerializedObject(GameObject.Find("ResultCanvas").GetComponent<ResultPanelPresenter>());
            var body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/LXGWWenKai-Regular SDF.asset");
            var display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/LXGWWenKai-Medium SDF.asset");
            Assert.That(body, Is.Not.Null, "control: body SDF font asset");
            Assert.That(display, Is.Not.Null, "control: display SDF font asset");

            var status = (TMP_Text)presenterSo.FindProperty("interpretationStatusText").objectReferenceValue;
            var mode = (TMP_Text)presenterSo.FindProperty("modeLabelText").objectReferenceValue;
            var retry = (Button)presenterSo.FindProperty("retryInterpretationButton").objectReferenceValue;
            var offline = (Button)presenterSo.FindProperty("offlineInterpretationButton").objectReferenceValue;

            Assert.That(status.font, Is.EqualTo(body));
            Assert.That(status.fontSize, Is.LessThan(30f));
            Assert.That(mode.font, Is.EqualTo(body));
            Assert.That(mode.fontSize, Is.LessThan(30f));
            Assert.That(retry.transform.Find("Label").GetComponent<TMP_Text>().font, Is.EqualTo(display));
            Assert.That(offline.transform.Find("Label").GetComponent<TMP_Text>().font, Is.EqualTo(display));
        }

        [Test]
        public void InterpretationButtonsShareTheBackButtonRowWithoutOverlapping()
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var canvas = GameObject.Find("ResultCanvas").transform;
            var back = canvas.Find("BackToMenuButton") as RectTransform;
            var retry = canvas.Find("Phase66_RetryInterpretationButton") as RectTransform;
            var offline = canvas.Find("Phase66_OfflineInterpretationButton") as RectTransform;
            Assert.That(back, Is.Not.Null, "control: the back button exists");
            Assert.That(retry, Is.Not.Null);
            Assert.That(offline, Is.Not.Null);

            foreach (var button in new[] { retry, offline })
            {
                Assert.That(button.anchorMin, Is.EqualTo(back.anchorMin));
                Assert.That(button.anchoredPosition.y, Is.EqualTo(back.anchoredPosition.y).Within(0.01f));
            }

            Assert.That(RightEdge(retry), Is.LessThan(LeftEdge(back)), "重新解读 sits left of 回到牌桌");
            Assert.That(RightEdge(back), Is.LessThan(LeftEdge(offline)), "查看离线解读 sits right of 回到牌桌");
        }

        [Test]
        public void PresenterShowsEachInterpretationState()
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var presenter = GameObject.Find("ResultCanvas").GetComponent<ResultPanelPresenter>();
            var presenterSo = new SerializedObject(presenter);
            var status = (TMP_Text)presenterSo.FindProperty("interpretationStatusText").objectReferenceValue;
            var mode = (TMP_Text)presenterSo.FindProperty("modeLabelText").objectReferenceValue;
            var retry = (Button)presenterSo.FindProperty("retryInterpretationButton").objectReferenceValue;
            var offline = (Button)presenterSo.FindProperty("offlineInterpretationButton").objectReferenceValue;
            var group = (CanvasGroup)presenterSo.FindProperty("readingContentGroup").objectReferenceValue;
            var summary = (TMP_Text)presenterSo.FindProperty("summaryText").objectReferenceValue;

            var offlineSession = LocalReadingSimulator.CreateSession(
                1, "单张牌", "问题？", "general", LocalReadingSimulator.CreatePlaceholderDraws(1));
            presenter.PresentSession(offlineSession);
            Assert.That(status.gameObject.activeSelf, Is.False, "offline: no status line");
            Assert.That(mode.text, Is.EqualTo(ReleaseUxCopy.ModeOffline));
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(summary.text, Is.EqualTo(offlineSession.summary));
            Assert.That(retry.gameObject.activeSelf || offline.gameObject.activeSelf, Is.False);

            var online = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 9, question = "问题？" }, LocalReadingSimulator.CreatePlaceholderDraws(1));
            online.spreadName = "单牌抽取";
            presenter.PresentSession(online);
            Assert.That(status.gameObject.activeSelf, Is.True, "pending: the status line shows");
            Assert.That(status.text, Is.EqualTo(ReleaseUxCopy.ResultPending));
            Assert.That(group.alpha, Is.EqualTo(0f), "pending: the empty sections stay hidden");
            Assert.That(summary.text, Is.Empty);
            Assert.That(mode.text, Is.Empty);

            InterpretationPoller.ApplyFailure(online, InterpretationFailure.ConnectionLost);
            presenter.PresentSession(online);
            Assert.That(status.text, Is.EqualTo(ReleaseUxCopy.InterpretationConnectionLost));
            Assert.That(retry.gameObject.activeSelf, Is.True);
            Assert.That(offline.gameObject.activeSelf, Is.True);

            InterpretationPoller.ApplyFailure(online, InterpretationFailure.SessionExpired);
            presenter.PresentSession(online);
            Assert.That(retry.gameObject.activeSelf, Is.False, "not retryable");
            Assert.That(offline.gameObject.activeSelf, Is.True);

            ReadingSessionMapper.ApplyInterpretation(online, new InterpretationResponse
            {
                id = 1,
                summary = "概要",
                overall_interpretation = "整体",
                model_used = "mock_ai",
            });
            presenter.PresentSession(online);
            Assert.That(status.gameObject.activeSelf, Is.False, "ready: no status line");
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(summary.text, Is.EqualTo("概要"));
            Assert.That(mode.text, Is.EqualTo(ReleaseUxCopy.ModeMock));
            Assert.That(retry.gameObject.activeSelf || offline.gameObject.activeSelf, Is.False);

            online.modelUsed = "deepseek-chat";
            presenter.PresentSession(online);
            Assert.That(mode.text, Is.Empty, "a real model needs no label");
        }

        [TestCase(0f, "牌意正在汇聚……")]
        [TestCase(19.9f, "牌意正在汇聚……")]
        [TestCase(20f, "牌意正在汇聚……\n这次解读比平时慢一些，请再稍候。")]
        public void PendingStatusAddsTheSlowNoticeAfterTwentySeconds(float elapsedSeconds, string expected)
        {
            Assert.That(ResultPanelPresenter.BuildPendingStatus(elapsedSeconds), Is.EqualTo(expected));
        }

        private static float LeftEdge(RectTransform rect)
        {
            return rect.anchoredPosition.x - rect.sizeDelta.x * rect.pivot.x;
        }

        private static float RightEdge(RectTransform rect)
        {
            return rect.anchoredPosition.x + rect.sizeDelta.x * (1f - rect.pivot.x);
        }

        // Phase 66: later tasks append tests above this line.
```

PlayMode：确认 `Assets/Tests/PlayMode/Phase66ResultInterpretationStateTests.cs` 不存在，然后创建：

```csharp
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 66: the Result screen follows the persistent poller. A Pending reading
    /// turns into its AI text, and a failed one offers 重新解读 and 查看离线解读.
    /// </summary>
    public sealed class Phase66ResultInterpretationStateTests
    {
        private const int PredictionId = 801;
        private const string AsyncPath = "/api/v1/records/801/interpret/async";
        private const string DetailPath = "/api/v1/records/801";

        private MockTarotBackend server;
        private GameObject boot;
        private InterpretationPoller poller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ReadingSessionStore.Clear();
            server = MockTarotBackend.Start();
            boot = new GameObject("Phase66_ResultTestBoot");
            Object.DontDestroyOnLoad(boot);
            var client = boot.AddComponent<ApiClient>();
            client.BaseUrl = server.ApiBaseUrl;
            client.SetAccessToken("test-access-token");
            ApiClient.SetShared(client);
            poller = boot.AddComponent<InterpretationPoller>();
            poller.Configure(client, 0.05f, 30f);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ApiClient.ClearShared();
            Object.Destroy(boot);
            ReadingSessionStore.Clear();
            yield return null;
            server.Dispose();
        }

        [UnityTest]
        public IEnumerator PendingReadingSwitchesToTheInterpretationWhenItArrives()
        {
            server.Script("POST", AsyncPath, MockTarotBackend.Json(202, MockTarotJson.Accepted(PredictionId)));
            server.Script("GET", DetailPath,
                MockTarotBackend.Json(200, MockTarotJson.Detail(PredictionId, "processing", 3, null)));
            var session = StartOnlineSession(3);

            yield return LoadResult();
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            var status = GetField<TMP_Text>(presenter, "interpretationStatusText");
            var summary = GetField<TMP_Text>(presenter, "summaryText");
            var group = GetField<CanvasGroup>(presenter, "readingContentGroup");

            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending), "control: still generating");
            Assert.That(status.gameObject.activeInHierarchy, Is.True, "pending: the status line shows");
            Assert.That(status.text, Does.StartWith(ReleaseUxCopy.ResultPending));
            Assert.That(group.alpha, Is.EqualTo(0f));

            server.Script("GET", DetailPath, MockTarotBackend.Json(200,
                MockTarotJson.Detail(PredictionId, "completed", 3, MockTarotJson.Interpretation(PredictionId, "deepseek-chat"))));
            yield return WaitUntil(() => session.interpretationState == InterpretationState.Ready, 10f, "expected Ready");
            yield return WaitUntil(() => group.alpha >= 1f, 5f, "expected the reading to fade in");

            Assert.That(status.gameObject.activeSelf, Is.False);
            Assert.That(summary.text, Is.EqualTo("概要来自测试桩。"));
            Assert.That(GetField<TMP_Text>(presenter, "modeLabelText").text, Is.Empty);
        }

        [UnityTest]
        public IEnumerator FailedReadingOffersRetryAndOfflineText()
        {
            server.Script("POST", AsyncPath,
                MockTarotBackend.Json(202, MockTarotJson.Accepted(PredictionId)),
                MockTarotBackend.Json(202, MockTarotJson.Accepted(PredictionId)));
            server.Script("GET", DetailPath,
                MockTarotBackend.Json(200, MockTarotJson.Detail(PredictionId, "failed", 3, null)));
            var session = StartOnlineSession(3);

            yield return LoadResult();
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            var controller = Object.FindFirstObjectByType<ResultSceneController>();
            var status = GetField<TMP_Text>(presenter, "interpretationStatusText");
            yield return WaitUntil(() => session.interpretationState == InterpretationState.Failed, 10f, "expected Failed");

            var retry = GetField<Button>(controller, "retryInterpretationButton");
            var offline = GetField<Button>(controller, "offlineInterpretationButton");
            Assert.That(status.text, Is.EqualTo(ReleaseUxCopy.InterpretationBackendFailed));
            Assert.That(retry.gameObject.activeInHierarchy, Is.True);
            Assert.That(offline.gameObject.activeInHierarchy, Is.True);

            retry.onClick.Invoke();
            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending), "retry restarts the poll");
            yield return WaitUntil(() => session.interpretationState == InterpretationState.Failed, 10f,
                "expected the retry to fail again");
            Assert.That(server.Count("POST", AsyncPath), Is.EqualTo(2));

            offline.onClick.Invoke();
            Assert.That(session.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(GetField<TMP_Text>(presenter, "modeLabelText").text, Is.EqualTo(ReleaseUxCopy.ModeOffline));
            Assert.That(GetField<TMP_Text>(presenter, "warningText").text, Is.EqualTo(ReleaseUxCopy.OfflineWarning));
            Assert.That(status.gameObject.activeSelf, Is.False);
            Assert.That(retry.gameObject.activeSelf || offline.gameObject.activeSelf, Is.False);
        }

        private ReadingSessionSnapshot StartOnlineSession(int cardCount)
        {
            var session = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse
                {
                    id = PredictionId,
                    spread_type_id = 12,
                    question = "此刻我最需要留意什么？",
                    question_type = "general",
                },
                LocalReadingSimulator.CreatePlaceholderDraws(cardCount));
            session.spreadName = "过去现在未来";
            ReadingSessionStore.Save(session);
            poller.Begin(session);
            return session;
        }

        private static IEnumerator LoadResult()
        {
            SceneManager.LoadScene("Result");
            yield return null;
            yield return null;
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "Result"
                    && Object.FindFirstObjectByType<ResultSceneController>() != null,
                10f,
                "expected the Result scene");
            yield return null;
        }

        private static IEnumerator WaitUntil(System.Func<bool> predicate, float seconds, string message)
        {
            var timeoutAt = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(predicate(), Is.True, message);
        }

        private static T GetField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}");

            var value = field.GetValue(target) as T;
            Assert.That(value, Is.Not.Null, $"Field {fieldName} on {target.GetType().Name} is null");
            return value;
        }
    }
}
```

- [ ] **Step 2: 运行，确认失败**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t7-red -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
```

预期：`NO RESULTS XML`，列出 `error CS0117`，内容为 `'ResultPanelPresenter' does not contain a definition for 'BuildPendingStatus'`。

- [ ] **Step 3: 实现 `ResultPanelPresenter`**

`Assets/Scripts/UI/ResultPanelPresenter.cs`，依次做下面几处 Edit。

(a) 把

```csharp
using System;
using TarotUnity.Data;
```

替换为

```csharp
using System;
using System.Collections;
using TarotUnity.Data;
```

(b) 把

```csharp
        [SerializeField] private float spreadCellY = 88f;
```

替换为

```csharp
        [SerializeField] private float spreadCellY = 88f;

        // Phase 66: an online reading can reach this screen before its AI text exists.
        // The status line, the mode label and the retry / offline buttons are wired by
        // the Phase 66 bootstrapper; readingContentGroup (on the scroll's Viewport)
        // hides the empty sections while the text is pending or has failed.
        [Header("Phase 66: online interpretation states")]
        [SerializeField] private TMP_Text interpretationStatusText;
        [SerializeField] private TMP_Text modeLabelText;
        [SerializeField] private Button retryInterpretationButton;
        [SerializeField] private Button offlineInterpretationButton;
        [SerializeField] private CanvasGroup readingContentGroup;
        [SerializeField] private float readyFadeSeconds = 0.6f;

        public const float PendingSlowNoticeSeconds = 20f;

        private float pendingSince = -1f;
        private Coroutine readyFade;
```

(c) 把 `PresentSession` 整个方法

```csharp
        public void PresentSession(ReadingSessionSnapshot session)
        {
            if (session == null)
            {
                Clear();
                return;
            }

            SetText(questionText, session.question);
            SetText(spreadNameText, session.spreadName);
            SetText(summaryText, session.summary);
            SetText(overallText, session.overallInterpretation);
            SetText(cardAnalysisText, session.cardAnalysis);
            SetText(adviceText, session.advice);
            SetText(warningText, session.warning);
            PresentCards(session.cardDraws);
        }
```

替换为

```csharp
        public void PresentSession(ReadingSessionSnapshot session)
        {
            if (session == null)
            {
                Clear();
                return;
            }

            if (session.source == ReadingSource.Offline)
            {
                ShowOffline(session);
                return;
            }

            switch (session.interpretationState)
            {
                case InterpretationState.Pending:
                    ShowPending(session);
                    break;
                case InterpretationState.Failed:
                    ShowFailed(session);
                    break;
                default:
                    ShowReady(session, false);
                    break;
            }
        }

        public void ShowPending(ReadingSessionSnapshot session)
        {
            PresentFrame(session);
            SetReadingTexts(null);
            SetReadingVisible(false);
            SetStatus(ReleaseUxCopy.ResultPending);
            SetText(modeLabelText, string.Empty);
            SetInterpretationButtons(false, false);
            pendingSince = Time.unscaledTime;
        }

        public void ShowReady(ReadingSessionSnapshot session, bool fadeIn)
        {
            PresentFrame(session);
            SetReadingTexts(session);
            SetStatus(null);
            SetText(modeLabelText, ModeLabelFor(session));
            SetInterpretationButtons(false, false);
            pendingSince = -1f;
            SetReadingVisible(true);

            if (fadeIn && readingContentGroup != null && isActiveAndEnabled && readyFadeSeconds > 0f)
            {
                if (readyFade != null)
                {
                    StopCoroutine(readyFade);
                }

                readyFade = StartCoroutine(FadeInReading());
            }
        }

        public void ShowFailed(ReadingSessionSnapshot session)
        {
            PresentFrame(session);
            SetReadingTexts(null);
            SetReadingVisible(false);
            SetStatus(session.failureMessage);
            SetText(modeLabelText, string.Empty);
            SetInterpretationButtons(session.canRetry, true);
            pendingSince = -1f;
        }

        public void ShowOffline(ReadingSessionSnapshot session)
        {
            PresentFrame(session);
            SetReadingTexts(session);
            SetStatus(null);
            SetText(modeLabelText, ReleaseUxCopy.ModeOffline);
            SetInterpretationButtons(false, false);
            pendingSince = -1f;
            SetReadingVisible(true);
        }

        public static string BuildPendingStatus(float elapsedSeconds)
        {
            return elapsedSeconds >= PendingSlowNoticeSeconds
                ? ReleaseUxCopy.ResultPending + "\n" + ReleaseUxCopy.ResultPendingSlow
                : ReleaseUxCopy.ResultPending;
        }

        public static string ModeLabelFor(ReadingSessionSnapshot session)
        {
            if (session == null)
            {
                return string.Empty;
            }

            if (session.source == ReadingSource.Offline)
            {
                return ReleaseUxCopy.ModeOffline;
            }

            return string.Equals(session.modelUsed, "mock_ai", StringComparison.OrdinalIgnoreCase)
                ? ReleaseUxCopy.ModeMock
                : string.Empty;
        }

        private void Update()
        {
            if (pendingSince < 0f || interpretationStatusText == null)
            {
                return;
            }

            // A breathing status line while the interpretation is generating; after
            // 20 s the slow-generation notice joins it (spec 7.1).
            var elapsed = Time.unscaledTime - pendingSince;
            var status = BuildPendingStatus(elapsed);
            if (interpretationStatusText.text != status)
            {
                interpretationStatusText.text = status;
            }

            interpretationStatusText.alpha = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI * 2f / 2.4f));
        }

        private IEnumerator FadeInReading()
        {
            readingContentGroup.alpha = 0f;
            var elapsed = 0f;
            while (elapsed < readyFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                readingContentGroup.alpha = Mathf.Clamp01(elapsed / readyFadeSeconds);
                yield return null;
            }

            readingContentGroup.alpha = 1f;
            readyFade = null;
        }

        private void PresentFrame(ReadingSessionSnapshot session)
        {
            SetText(questionText, session.question);
            SetText(spreadNameText, session.spreadName);
            PresentCards(session.cardDraws);
        }

        private void SetReadingTexts(ReadingSessionSnapshot session)
        {
            SetText(summaryText, session?.summary);
            SetText(overallText, session?.overallInterpretation);
            SetText(cardAnalysisText, session?.cardAnalysis);
            SetText(adviceText, session?.advice);
            SetText(warningText, session?.warning);
        }

        private void SetReadingVisible(bool visible)
        {
            if (readingContentGroup == null)
            {
                return;
            }

            if (!visible && readyFade != null)
            {
                StopCoroutine(readyFade);
                readyFade = null;
            }

            readingContentGroup.alpha = visible ? 1f : 0f;
            readingContentGroup.blocksRaycasts = visible;
        }

        private void SetStatus(string message)
        {
            if (interpretationStatusText == null)
            {
                return;
            }

            interpretationStatusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            interpretationStatusText.text = message ?? string.Empty;
            interpretationStatusText.alpha = 1f;
        }

        private void SetInterpretationButtons(bool retryVisible, bool offlineVisible)
        {
            if (retryInterpretationButton != null)
            {
                retryInterpretationButton.gameObject.SetActive(retryVisible);
            }

            if (offlineInterpretationButton != null)
            {
                offlineInterpretationButton.gameObject.SetActive(offlineVisible);
            }
        }
```

(d) 在 `Clear` 方法里，把

```csharp
            SetText(warningText, string.Empty);
            PresentCards(null);
        }
```

替换为

```csharp
            SetText(warningText, string.Empty);
            PresentCards(null);
            SetStatus(null);
            SetText(modeLabelText, string.Empty);
            SetInterpretationButtons(false, false);
            SetReadingVisible(true);
            pendingSince = -1f;
        }
```

- [ ] **Step 4: 实现 `ResultSceneController`**

`Assets/Scripts/UI/ResultSceneController.cs`，依次做下面几处 Edit。

(a) 把

```csharp
using TarotUnity.Data;
```

替换为

```csharp
using TarotUnity.Data;
using TarotUnity.Network;
```

(b) 把

```csharp
        [SerializeField] private CameraChoreographyController cameraChoreography;

        private void Awake()
        {
            backToMenuButton?.onClick.AddListener(BackToMenu);
        }
```

替换为

```csharp
        [SerializeField] private CameraChoreographyController cameraChoreography;
        [SerializeField] private Button retryInterpretationButton;
        [SerializeField] private Button offlineInterpretationButton;

        private InterpretationPoller poller;

        private void Awake()
        {
            backToMenuButton?.onClick.AddListener(BackToMenu);
            retryInterpretationButton?.onClick.AddListener(RetryInterpretation);
            offlineInterpretationButton?.onClick.AddListener(UseOfflineInterpretation);
        }
```

(c) 把

```csharp
                resultPanel?.PresentSession(ReadingSessionStore.Current);
            }
```

替换为

```csharp
                resultPanel?.PresentSession(ReadingSessionStore.Current);
                SubscribeToPoller();
            }
```

(d) 把

```csharp
        private void OnDestroy()
        {
            backToMenuButton?.onClick.RemoveListener(BackToMenu);
        }

        private void BackToMenu()
        {
            ReadingSessionStore.Clear();
```

替换为

```csharp
        private void OnDestroy()
        {
            backToMenuButton?.onClick.RemoveListener(BackToMenu);
            retryInterpretationButton?.onClick.RemoveListener(RetryInterpretation);
            offlineInterpretationButton?.onClick.RemoveListener(UseOfflineInterpretation);

            if (poller != null)
            {
                poller.StateChanged -= HandleInterpretationStateChanged;
            }
        }

        // Phase 66: follow the persistent poller so a Pending reading switches to its
        // AI text, or to the failure copy, while the player is on this screen.
        private void SubscribeToPoller()
        {
            var instance = InterpretationPoller.Instance;
            if (instance == null)
            {
                return;
            }

            poller = instance;
            poller.StateChanged += HandleInterpretationStateChanged;
        }

        private void HandleInterpretationStateChanged(ReadingSessionSnapshot session)
        {
            if (session == null || session != ReadingSessionStore.Current || resultPanel == null)
            {
                return;
            }

            if (session.source == ReadingSource.Offline)
            {
                resultPanel.ShowOffline(session);
                return;
            }

            switch (session.interpretationState)
            {
                case InterpretationState.Pending:
                    resultPanel.ShowPending(session);
                    break;
                case InterpretationState.Ready:
                    resultPanel.ShowReady(session, true);
                    break;
                default:
                    resultPanel.ShowFailed(session);
                    break;
            }
        }

        private void RetryInterpretation()
        {
            var session = ReadingSessionStore.Current;
            if (poller != null && session != null && session.canRetry && poller.Current == session)
            {
                poller.Retry();
            }
        }

        // Spec 7.1: switch to the offline text for good - polling stops and the
        // screen never flips back to an online result.
        private void UseOfflineInterpretation()
        {
            var session = ReadingSessionStore.Current;
            if (session == null)
            {
                return;
            }

            if (poller != null && poller.Current == session)
            {
                poller.UseOffline();
                return;
            }

            InterpretationPoller.ApplyOffline(session);
            resultPanel?.ShowOffline(session);
        }

        private void BackToMenu()
        {
            var activePoller = poller != null ? poller : InterpretationPoller.Instance;
            if (activePoller != null)
            {
                activePoller.Stop();
            }

            ReadingSessionStore.Clear();
```

- [ ] **Step 5: 新建 Bootstrapper**

确认 `Assets/Editor/Phase66ResultInterpretationStateBootstrapper.cs` 不存在，然后创建：

```csharp
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 66: an online reading can reach the Result screen while its AI text is
    /// still generating, after it failed, or after the player switched to offline
    /// text. This lays the status line (inside the reading scroll), the mode label (on
    /// the spread-name line) and the retry / offline buttons (copied from the back
    /// button), then wires them to the presenter and the scene controller. Re-running
    /// reuses the objects it created, so the layout stays reproducible.
    /// </summary>
    public static class Phase66ResultInterpretationStateBootstrapper
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        public const string StatusName = "Phase66_InterpretationStatus";
        public const string ModeLabelName = "Phase66_ModeLabel";
        public const string RetryButtonName = "Phase66_RetryInterpretationButton";
        public const string OfflineButtonName = "Phase66_OfflineInterpretationButton";

        private static readonly Color StatusInk = new Color(0.96f, 0.91f, 0.80f, 1f);
        private static readonly Color ModeInk = new Color(0.86f, 0.71f, 0.42f, 1f);

        [MenuItem("Tools/Tarot Unity/Run Phase 66 Result Interpretation States")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("ResultCanvas");
            if (canvas == null)
            {
                Debug.LogError("Phase 66: ResultCanvas not found.");
                return;
            }

            var scroll = canvas.transform.Find("ResultReadingScroll");
            var viewport = scroll != null ? scroll.Find("Viewport") : null;
            var spreadNameTransform = canvas.transform.Find("SpreadNameText");
            var backButton = canvas.transform.Find("BackToMenuButton");
            var presenter = canvas.GetComponent<ResultPanelPresenter>();
            var controller = canvas.GetComponent<ResultSceneController>();
            if (scroll == null || viewport == null || spreadNameTransform == null || backButton == null
                || presenter == null || controller == null)
            {
                Debug.LogError("Phase 66: ResultReadingScroll/Viewport, SpreadNameText, BackToMenuButton, " +
                    "ResultPanelPresenter or ResultSceneController not found.");
                return;
            }

            var spreadName = spreadNameTransform.GetComponent<TMP_Text>();
            if (spreadName == null)
            {
                Debug.LogError("Phase 66: SpreadNameText has no TMP_Text to borrow the body font from.");
                return;
            }

            var status = EnsureText(scroll, StatusName, spreadName);
            var statusRect = status.rectTransform;
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.offsetMin = new Vector2(40f, 40f);
            statusRect.offsetMax = new Vector2(-40f, -40f);
            status.fontSize = 22f;
            status.color = StatusInk;
            status.alignment = TextAlignmentOptions.Center;
            status.enableWordWrapping = true;
            status.text = string.Empty;
            status.transform.SetAsLastSibling();
            status.gameObject.SetActive(false);

            var mode = EnsureText(canvas.transform, ModeLabelName, spreadName);
            var modeRect = mode.rectTransform;
            var spreadRect = spreadName.rectTransform;
            modeRect.anchorMin = spreadRect.anchorMin;
            modeRect.anchorMax = spreadRect.anchorMax;
            modeRect.pivot = spreadRect.pivot;
            modeRect.anchoredPosition = spreadRect.anchoredPosition;
            modeRect.sizeDelta = spreadRect.sizeDelta;
            mode.fontSize = 15f;
            mode.color = ModeInk;
            mode.alignment = TextAlignmentOptions.MidlineRight;
            mode.enableWordWrapping = false;
            mode.text = string.Empty;
            mode.transform.SetSiblingIndex(spreadNameTransform.GetSiblingIndex() + 1);

            var retry = EnsureButton(canvas.transform, backButton.gameObject, RetryButtonName,
                ReleaseUxCopy.RetryButtonLabel, new Vector2(-280f, -300f));
            var offline = EnsureButton(canvas.transform, backButton.gameObject, OfflineButtonName,
                ReleaseUxCopy.OfflineButtonLabel, new Vector2(280f, -300f));

            var group = viewport.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = viewport.gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            var presenterSo = new SerializedObject(presenter);
            presenterSo.FindProperty("interpretationStatusText").objectReferenceValue = status;
            presenterSo.FindProperty("modeLabelText").objectReferenceValue = mode;
            presenterSo.FindProperty("retryInterpretationButton").objectReferenceValue = retry;
            presenterSo.FindProperty("offlineInterpretationButton").objectReferenceValue = offline;
            presenterSo.FindProperty("readingContentGroup").objectReferenceValue = group;
            presenterSo.ApplyModifiedPropertiesWithoutUndo();

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("retryInterpretationButton").objectReferenceValue = retry;
            controllerSo.FindProperty("offlineInterpretationButton").objectReferenceValue = offline;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Phase 66: result interpretation-state UI laid and wired.");
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name, TMP_Text fontSource)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }

            var text = go.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = go.AddComponent<TextMeshProUGUI>();
            }

            // Borrow the body SDF font and material so the Phase 24 role check holds.
            text.font = fontSource.font;
            text.fontSharedMaterial = fontSource.fontSharedMaterial;
            text.raycastTarget = false;
            return text;
        }

        private static Button EnsureButton(Transform canvas, GameObject template, string name, string label, Vector2 position)
        {
            var existing = canvas.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                // A copy of 回到牌桌 keeps the kit sprite, label font and size. Its click
                // handler is added in code; the template has no persistent calls.
                go = Object.Instantiate(template, canvas);
                go.name = name;
            }

            go.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;

            var labelTransform = go.transform.Find("Label");
            var labelText = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
            if (labelText != null)
            {
                labelText.text = label;
            }

            go.SetActive(false);
            return go.GetComponent<Button>();
        }
    }
}
```

- [ ] **Step 6: 执行 Bootstrapper，检查场景改动**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
UNITY=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
LOG="$S/online-b-run/results/t7-bootstrap.log"
if [ -e "$LOG" ]; then echo "STOP: $LOG exists"; else
  "$UNITY" -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
    -batchmode -nographics -enableUnityConnectPrefs false \
    -executeMethod TarotUnity.Editor.Phase66ResultInterpretationStateBootstrapper.Run -quit -logFile "$LOG"
  echo "exit=$?"
  grep -n 'Phase 66:\|error CS' "$LOG"
fi
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
git status --short Assets/Scenes
grep -c 'Phase66_' Assets/Scenes/Result.unity
```

预期：
- `exit=0`，日志里只有一行 `Phase 66: result interpretation-state UI laid and wired.`，没有 `error CS`，也没有其他 `Phase 66:` 错误行。
- `git status` 只显示 ` M Assets/Scenes/Result.unity`。
- `grep -c` 至少为 4。

出现 `Phase 66: … not found` → STOP。有其他场景被改动 → STOP。

- [ ] **Step 7: 运行，确认通过**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t7-green -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
bash "$S/online-b-run/ut.sh" PlayMode t7-green -testFilter TarotUnity.Tests.PlayMode.Phase66ResultInterpretationStateTests
```

预期：EditMode `total=56 passed=56 failed=0`；PlayMode `total=2 passed=2 failed=0`。

- [ ] **Step 8: 全量回归**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t7
bash "$S/online-b-run/ut.sh" PlayMode t7
```

预期：EditMode `total=E0+56 failed=0`；PlayMode `total=P0+25 failed=0`。

重点关注几组已有的结果页测试：Phase 24（字体角色）、29（滚动区与 Content 的子物体顺序）、30（居中与不重叠）、35/40（结果页组件与贴图）、51（不能有 Legacy Text）、60/62（牌阵横排）、64（背景必须是第一个子物体）。其中任何一个失败都 STOP，并报告失败断言的原文。

- [ ] **Step 9: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
ls Assets/Editor/Phase66ResultInterpretationStateBootstrapper.cs.meta Assets/Tests/PlayMode/Phase66ResultInterpretationStateTests.cs.meta
git add Assets/Scripts/UI/ResultPanelPresenter.cs Assets/Scripts/UI/ResultSceneController.cs \
  Assets/Editor/Phase66ResultInterpretationStateBootstrapper.cs Assets/Editor/Phase66ResultInterpretationStateBootstrapper.cs.meta \
  Assets/Scenes/Result.unity \
  Assets/Tests/PlayMode/Phase66ResultInterpretationStateTests.cs Assets/Tests/PlayMode/Phase66ResultInterpretationStateTests.cs.meta \
  Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs
git commit -F - <<'EOF'
feat(unity): show generating, ready, failed and offline interpretations on the Result screen

The Result screen follows the persistent poller: a breathing status line
while the AI text is generating, a fade-in when it arrives, retry and
offline buttons when it fails, and a mode label for offline or mock text.
The Phase 66 bootstrapper lays and wires the new UI in Result.unity.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
```

---

### Task 8: 阶段文档、编年史与四种状态截图

**Files:**
- Create: `Docs/PHASE66_ONLINE_INTERPRETATION.md`
- Modify: `Docs/PROJECT_CHRONICLE.md`（在文件末尾 Phase 64 段落之后追加）
- Create: `Assets/Editor/Phase66InterpretationStateCaptureBuilder.cs`
- Create: `Docs/VisualReview/Phase66/Result_pending.png`、`Result_ready.png`、`Result_failed.png`、`Result_offline.png`
- Test: `Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs`（追加 1 个测试）

**Interfaces:**
- Consumes：Task 3 的 `ReadingSessionMapper.FromBackendStart`、`ApplyInterpretation`；Task 5 的 `InterpretationPoller.ApplyFailure`、`ApplyOffline`；Task 7 的 `ResultPanelPresenter.PresentSession` 和结果页场景对象；`CaptureRig.RenderConverged(Camera)`（已有，命名空间 `TarotUnity.Editor`）。
- Produces：`TarotUnity.Editor.Phase66InterpretationStateCaptureBuilder.Run()`。输出目录默认为 `Docs/VisualReview/Phase66`，可以用环境变量 `PHASE66_CAPTURE_DIR` 指定；目标文件已存在时抛异常，不覆盖。

- [ ] **Step 1: 追加失败的文档测试**

用 Edit 把锚点注释 `        // Phase 66: later tasks append tests above this line.` 替换为：

```csharp
        [Test]
        public void Phase66DocumentationExists()
        {
            const string docPath = "Docs/PHASE66_ONLINE_INTERPRETATION.md";
            Assert.That(File.Exists(docPath), Is.True, $"Missing Phase 66 doc at {docPath}");
            var text = File.ReadAllText(docPath);
            Assert.That(text, Does.Contain("InterpretationPoller"));
            Assert.That(text, Does.Contain("ApiClient.Shared"));
            Assert.That(text, Does.Contain("Result"));

            var chronicle = File.ReadAllText("Docs/PROJECT_CHRONICLE.md");
            Assert.That(chronicle, Does.Contain("### Phase 66"));
        }

        // Phase 66: later tasks append tests above this line.
```

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t8-red -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
```

预期：`total=57 failed=1`，失败的测试是 `Phase66DocumentationExists`，失败信息为 `Missing Phase 66 doc`。

- [ ] **Step 2: 写阶段文档和编年史**

确认 `Docs/PHASE66_ONLINE_INTERPRETATION.md` 不存在，然后创建：

````markdown
# Phase 66 — Online Interpretation Loop

The reading room now starts a real online reading, and the Result screen waits for
the AI interpretation instead of the reading room blocking on it.

## Why

- The guest token was issued to the persistent Boot `ApiClient`, but the reading room
  used its own scene `ApiClient`, so `CanCreateAuthenticatedReading` was always false
  and every reading silently went offline. Scene code now reads `ApiClient.Shared` first.
- Even with a token, the old `CompleteReading` waited for the AI (often longer than the
  15 s request timeout) before a single card was dealt.

## Flow

1. The shuffle starts while `BackendReadingService.StartReading` creates the record,
   draws and fetches the cards (no AI). A rejected token refreshes the guest, or starts
   a new one, and the start is retried once.
2. The table deals the backend's cards. The snapshot is saved as `Online` / `Pending`
   and handed to `InterpretationPoller`, which lives on the persistent Boot object.
3. `InterpretationPoller` calls `POST /records/{id}/interpret/async`, then polls
   `GET /records/{id}` after 2, 2, 3, 3 and then every 5 s, for up to 330 s. A stored
   interpretation counts as done whatever the record status says.
4. The Result screen shows one of four states: generating (a breathing status line,
   plus a slow notice after 20 s), ready (the reading fades in), failed (重新解读 when
   retryable, and 查看离线解读), or offline.

A failed start deals an offline reading and shows the matching line from
`ReleaseUxCopy`; raw server messages only reach the log. A spread the backend does
not have, or a card-count mismatch, never goes online.

## Known limits

- If the backend process dies mid-generation, the record stays `processing` until the
  300 s stale window passes, so the first 重新解读 may time out before a second one
  succeeds.
- The guest daily quota starts over for a new guest session; real rate limiting belongs
  to deployment.
- The spread-selection copy ("Choose a spread…", "One Card Focus") is still English; it
  belongs to the reading-room clarity sub-project.

## Files

- `Assets/Scripts/Network/ApiClient.cs` — `Shared` and the structured requests.
- `Assets/Scripts/Network/ApiError.cs` — status code, kind and Retry-After.
- `Assets/Scripts/Network/BackendReadingService.cs` — `StartReading`, `RecoverSession`.
- `Assets/Scripts/Network/InterpretationPoller.cs` — the persistent poller.
- `Assets/Scripts/Data/ReadingSessionSnapshot.cs`, `ReadingSessionMapper.cs` — reading
  source and interpretation state.
- `Assets/Scripts/UI/ReleaseUxCopy.cs` — the Chinese copy table.
- `Assets/Scripts/UI/ReadingRoomController.cs` — deal first, interpret in the background.
- `Assets/Scripts/UI/ResultPanelPresenter.cs`, `ResultSceneController.cs` — the four states.
- `Assets/Editor/Phase66ResultInterpretationStateBootstrapper.cs` — lays and wires the
  Result UI.
- `Assets/Editor/Phase66InterpretationStateCaptureBuilder.cs` — review shots.
- `Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs`,
  `Assets/Tests/PlayMode/Phase66*Tests.cs`, `Assets/Tests/PlayMode/MockTarotBackend.cs` —
  guards and scripted-backend scenarios.
- `Docs/VisualReview/Phase66/` — generating, ready, failed and offline captures.
````

`Docs/PROJECT_CHRONICLE.md`，用 Edit 把

```markdown
The Result screen received the current parlor backdrop and a readable quiet exit
link, completing the Phase 64 visual baseline for the next product-closure work.
```

替换为

```markdown
The Result screen received the current parlor backdrop and a readable quiet exit
link, completing the Phase 64 visual baseline for the next product-closure work.

### Phase 66 — Online interpretation loop

The reading room now deals the backend's cards as soon as they exist, and a
persistent `InterpretationPoller` fetches the AI interpretation in the background.
The Result screen shows generating, ready, failed or offline states with Chinese copy
instead of the table waiting on the AI. See `Docs/PHASE66_ONLINE_INTERPRETATION.md`.
```

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t8-green -testFilter TarotUnity.Tests.EditMode.Phase66OnlineInterpretationTests
```

预期：`total=57 passed=57 failed=0`。

- [ ] **Step 3: 新建截图 Builder**

确认 `Assets/Editor/Phase66InterpretationStateCaptureBuilder.cs` 不存在，然后创建：

```csharp
using System;
using System.IO;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 66 review shots: the Result screen for a three-card online reading that is
    /// generating, ready (real-model text), failed (retryable) and switched to offline
    /// text. READ-ONLY: it presents sample snapshots and renders; the scene is not saved.
    /// Set PHASE66_CAPTURE_DIR to render a review round somewhere other than Docs.
    /// </summary>
    public static class Phase66InterpretationStateCaptureBuilder
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string DefaultOutFolder = "Docs/VisualReview/Phase66";
        private const int W = 2560, H = 1440;

        private static readonly string[] Files =
        {
            "Result_pending.png", "Result_ready.png", "Result_failed.png", "Result_offline.png",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 66 Interpretation State Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            var outFolder = Environment.GetEnvironmentVariable("PHASE66_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(outFolder))
            {
                outFolder = DefaultOutFolder;
            }

            Directory.CreateDirectory(outFolder);
            foreach (var file in Files)
            {
                if (File.Exists(Path.Combine(outFolder, file)))
                {
                    throw new InvalidOperationException($"{outFolder}/{file} already exists; captures never overwrite.");
                }
            }

            Capture(outFolder, Files[0], BuildOnline());

            var ready = BuildOnline();
            ReadingSessionMapper.ApplyInterpretation(ready, SampleInterpretation());
            Capture(outFolder, Files[1], ready);

            var failed = BuildOnline();
            InterpretationPoller.ApplyFailure(failed, InterpretationFailure.ConnectionLost);
            Capture(outFolder, Files[2], failed);

            var offline = BuildOnline();
            InterpretationPoller.ApplyOffline(offline);
            Capture(outFolder, Files[3], offline);

            Debug.Log($"Phase 66 interpretation state capture complete -> {outFolder}");
        }

        private static ReadingSessionSnapshot BuildOnline()
        {
            var session = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse
                {
                    id = 66,
                    spread_type_id = 2,
                    question = "我接下来最该把力气放在哪里？",
                    question_type = "general",
                },
                LocalReadingSimulator.CreatePlaceholderDraws(3));
            session.spreadName = "过去现在未来";
            return session;
        }

        private static InterpretationResponse SampleInterpretation()
        {
            return new InterpretationResponse
            {
                id = 1,
                summary = "旧的节奏正在松动，新的方向需要你亲手确认。",
                overall_interpretation = "过去的积累给了你底气，眼下的犹豫来自选择太多。把注意力收回到一件真正重要的事上，局面会比想象中更快清晰。",
                card_analysis = "过去：愚者 — 敢于开始的勇气仍在。\n现在：魔术师 — 资源齐备，关键在于专注。\n建议：女祭司（逆位） — 别只听外界的声音。",
                advice = "这一周只定一个目标，每天为它做一件小事。",
                warning = "解读仅供参考，重要决定请结合现实情况。",
                model_used = "deepseek-chat",
            };
        }

        private static void Capture(string outFolder, string file, ReadingSessionSnapshot session)
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var presenter = UnityEngine.Object.FindFirstObjectByType<ResultPanelPresenter>();
            if (presenter == null)
            {
                throw new InvalidOperationException("ResultPanelPresenter not found.");
            }

            presenter.PresentSession(session);

            Canvas.ForceUpdateCanvases();
            var contentObject = GameObject.Find("Content");
            if (contentObject != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentObject.GetComponent<RectTransform>());
            }

            // RectMask2D caches its clip rect; toggle it so the capture clips to the
            // layout the presenter just applied (Phase 60 lesson).
            foreach (var mask in UnityEngine.Object.FindObjectsByType<RectMask2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                mask.enabled = false;
                mask.enabled = true;
            }

            Canvas.ForceUpdateCanvases();
            RenderActiveCamera(Path.Combine(outFolder, file));
        }

        private static void RenderActiveCamera(string path)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            }

            if (camera == null)
            {
                throw new InvalidOperationException("No camera in scene.");
            }

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;
            var prevAspect = camera.aspect;
            var states = PrepareCanvases(camera);

            try
            {
                camera.aspect = (float)W / H;
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                Canvas.ForceUpdateCanvases();
                CaptureRig.RenderConverged(camera);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                RestoreCanvases(states);
                camera.targetTexture = prevTarget;
                camera.aspect = prevAspect;
                RenderTexture.active = prevActive;
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        private static (Canvas c, RenderMode m, Camera cam, float d, bool p)[] PrepareCanvases(Camera camera)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var states = new (Canvas, RenderMode, Camera, float, bool)[canvases.Length];
            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                states[i] = (c, c.renderMode, c.worldCamera, c.planeDistance, c.pixelPerfect);
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = camera;
                c.planeDistance = 1f;
                c.pixelPerfect = false;
            }

            return states;
        }

        private static void RestoreCanvases((Canvas c, RenderMode m, Camera cam, float d, bool p)[] states)
        {
            foreach (var s in states)
            {
                if (s.c != null)
                {
                    s.c.renderMode = s.m;
                    s.c.worldCamera = s.cam;
                    s.c.planeDistance = s.d;
                    s.c.pixelPerfect = s.p;
                }
            }
        }
    }
}
```

（`DestroyImmediate` 销毁的是本次渲染在内存中临时创建的 `RenderTexture` 和 `Texture2D`，不涉及任何文件；这与 Phase 64 Builder 的做法相同。）

- [ ] **Step 4: 先把一轮审查截图渲染到 scratch**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
UNITY=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
ROUND="$S/online-b-run/captures/round1"
LOG="$S/online-b-run/results/t8-capture-round1.log"
if [ -e "$ROUND" ] || [ -e "$LOG" ]; then echo "STOP: $ROUND or $LOG exists, use round2"; else
  PHASE66_CAPTURE_DIR="$ROUND" "$UNITY" -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
    -batchmode -enableUnityConnectPrefs false \
    -executeMethod TarotUnity.Editor.Phase66InterpretationStateCaptureBuilder.Run -quit -logFile "$LOG"
  echo "exit=$?"
  grep -n 'Phase 66 interpretation state capture complete\|Exception\|error CS' "$LOG"
  ls -l "$ROUND"
fi
```

预期：`exit=0`，日志里有 `capture complete`，`round1` 下有 4 个 PNG，每个都大于 100 KB。注意这条命令**不带** `-nographics`。

- [ ] **Step 5: 看图验收**

用 Read 工具逐张查看 `round1` 的 4 张 PNG，按下表逐项核对：

| 截图 | 必须满足 |
| --- | --- |
| `Result_pending.png` | 解读区中央是「牌意正在汇聚……」；看不到四个小节标题；顶部三张牌完整；没有模式标签；没有「重新解读」「查看离线解读」 |
| `Result_ready.png` | 四个小节的中文正文清晰可读；没有状态文字；没有模式标签（模型为 deepseek-chat） |
| `Result_failed.png` | 中央是「与占卜服务的连接中断了，可以再试一次。」；底部从左到右依次是「重新解读」「回到牌桌」「查看离线解读」，三个按钮互不重叠，文字完整 |
| `Result_offline.png` | 显示离线正文；「离线解读」金色小字在牌阵名同一行的右端，不与牌阵名、问题或牌面重叠 |

**先怀疑仪器**：如果 4 张图看起来完全一样，先检查截图流程本身（是否每张都重新 `OpenScene` 并调用了 `PresentSession`），不要先去改布局。

发现重叠或文字不可读时：
- 只允许修改 Bootstrapper 里的位置和字号常量。
- 然后重跑 Task 7 的 Step 6（Bootstrapper）和 Step 8（全量测试），再渲染到 `round2`。
- 两轮之后仍然不满足 → STOP，把截图发给用户判断。

- [ ] **Step 6: 渲染最终截图到 `Docs/VisualReview/Phase66`**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
UNITY=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
LOG="$S/online-b-run/results/t8-capture-final.log"
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
if [ -e Docs/VisualReview/Phase66 ] || [ -e "$LOG" ]; then echo "STOP: Docs/VisualReview/Phase66 or $LOG exists"; else
  "$UNITY" -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
    -batchmode -enableUnityConnectPrefs false \
    -executeMethod TarotUnity.Editor.Phase66InterpretationStateCaptureBuilder.Run -quit -logFile "$LOG"
  echo "exit=$?"
  ls -l Docs/VisualReview/Phase66
fi
```

预期：`exit=0`，`Docs/VisualReview/Phase66` 下有 4 个 PNG。用 Read 工具抽查 `Result_failed.png`，确认它与审查通过的那一轮一致。

- [ ] **Step 7: 全量回归**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t8
bash "$S/online-b-run/ut.sh" PlayMode t8
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity && git status --short
```

预期：EditMode `total=E0+57 failed=0`；PlayMode `total=P0+25 failed=0`。`git status` 只应列出本任务的文件；字体图集改动记下来，不要处理。

- [ ] **Step 8: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
ls Assets/Editor/Phase66InterpretationStateCaptureBuilder.cs.meta
git add Docs/PHASE66_ONLINE_INTERPRETATION.md Docs/PROJECT_CHRONICLE.md \
  Assets/Editor/Phase66InterpretationStateCaptureBuilder.cs Assets/Editor/Phase66InterpretationStateCaptureBuilder.cs.meta \
  Docs/VisualReview/Phase66/Result_pending.png Docs/VisualReview/Phase66/Result_ready.png \
  Docs/VisualReview/Phase66/Result_failed.png Docs/VisualReview/Phase66/Result_offline.png \
  Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs
git commit -F - <<'EOF'
docs(unity): document the online interpretation loop and capture its four states

Adds the Phase 66 doc, a chronicle entry, and a capture builder that renders
the Result screen while generating, ready, failed and offline.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
```

---

### Task 9: 全量回归、真实 DeepSeek 联调与收尾

**Files:**
- Create: `Assets/Tests/PlayMode/Phase66LiveBackendTests.cs`
- Create（scratch，不入库）：`$S/online-b-live/live.sh`、`$S/online-b-live/count_attempts.py`
- Modify: `PROJECT_COMPLETION_PLAN.md`（仓库根目录，在 2.4 节之后追加 2.5 节）

**Interfaces:**
- Consumes：前面所有任务；Boot 场景（`GameBootstrap` 会创建 `ApiClient.Shared`、`BackendSessionBootstrap`、`InterpretationPoller`）；`DesktopConfigLoader.BackendUrlEnvironmentVariable` = `TAROT_BACKEND_URL`。
- Produces：
  - `Phase66LiveBackendTests`：3 个测试，只在 `TAROT_LIVE_BACKEND=1` 时运行，否则 `Assert.Ignore`（计为 skipped）。
    - `LiveDrillBackendDownBeforeTheDraw`
    - `LiveDrillBackendLostWhileGenerating`
    - `LiveReadingsForOneThreeAndTenCards`
  - 与 `live.sh` 的握手协议：测试在 `$TAROT_LIVE_MARKERS` 目录里创建 `<名字>-stop-backend` 或 `<名字>-start-backend`；`live.sh` 完成操作后创建 `<名字>-backend-stopped` 或 `<名字>-backend-started`。
  - 联调报告按行追加到 `$TAROT_LIVE_REPORT`。

**AI 调用预算**：3 局正式联调，加上演练 2 的 1–2 次生成，一共约 5 次 DeepSeek 生成；某一次主模型失败时，还会多一次备用模型请求。演练 1 不调用 AI。

- [ ] **Step 1: 全量回归（Unity 两组 + 后端两组）**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" EditMode t9
bash "$S/online-b-run/ut.sh" PlayMode t9
bash "$S/online-a-run/pt.sh" tests
bash "$S/online-a-run/pt.sh" .
```

预期：EditMode `total=E0+57 failed=0`；PlayMode `total=P0+25 failed=0`；后端两次都是 `188 passed`。

- [ ] **Step 2: 新建联调测试**

确认 `Assets/Tests/PlayMode/Phase66LiveBackendTests.cs` 不存在，然后创建：

```csharp
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TarotUnity.Core;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 66 live acceptance (spec 8.5) against the real FastAPI backend and the
    /// user's DeepSeek key. Ignored unless TAROT_LIVE_BACKEND=1. It is driven by
    /// live.sh, which stops or restarts the backend when a test writes a marker file.
    /// </summary>
    public sealed class Phase66LiveBackendTests
    {
        private const float InterpretationWaitSeconds = 360f;

        private static string MarkerDir => Environment.GetEnvironmentVariable("TAROT_LIVE_MARKERS");
        private static string ReportPath => Environment.GetEnvironmentVariable("TAROT_LIVE_REPORT");

        [SetUp]
        public void RequireLiveBackend()
        {
            if (Environment.GetEnvironmentVariable("TAROT_LIVE_BACKEND") != "1")
            {
                Assert.Ignore("Live backend run only: set TAROT_LIVE_BACKEND=1 (plan 2, Task 9).");
            }
        }

        [UnityTearDown]
        public IEnumerator DestroyBootObjects()
        {
            foreach (var bootstrap in Object.FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None))
            {
                Object.Destroy(bootstrap.gameObject);
            }

            ApiClient.ClearShared();
            ReadingSessionStore.Clear();
            yield return null;
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator LiveDrillBackendDownBeforeTheDraw()
        {
            yield return BootToOnlineMenu();
            yield return EnterReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            yield return WaitForBackendSpreads(room);

            yield return Handshake("drill1-stop-backend", "drill1-backend-stopped", 120f);

            GetField<Button>(room, "oneCardButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitUntil(() => deck.ActiveCards.Count == 1, 90f, "expected an offline card");

            var release = GetField<TMP_Text>(room, "releaseStatusText").text;
            Report($"drill1 source={ReadingSessionStore.Current.source} release={release}");
            Assert.That(ReadingSessionStore.Current.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(release, Is.EqualTo(ReleaseUxCopy.OfflineBecauseNetwork));

            yield return Handshake("drill1-start-backend", "drill1-backend-started", 180f);
        }

        [UnityTest]
        [Timeout(1500000)]
        public IEnumerator LiveDrillBackendLostWhileGenerating()
        {
            yield return BootToOnlineMenu();
            yield return EnterReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "threeCardButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitUntil(() => deck.ActiveCards.Count >= 1, 60f, "expected the first card");

            var session = ReadingSessionStore.Current;
            Assert.That(session.source, Is.EqualTo(ReadingSource.Online), "control: the drill needs an online reading");
            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending), "control: the AI is still generating");
            yield return Handshake("drill2-stop-backend", "drill2-backend-stopped", 120f);

            yield return FlipAllCardsAndReveal(room, 3);
            yield return WaitUntil(() => session.interpretationState == InterpretationState.Failed, 120f,
                "expected the lost connection to surface (Ready here means the AI beat the drill - rerun it)");
            Report($"drill2 failure={session.failureMessage} canRetry={session.canRetry}");
            Assert.That(session.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationConnectionLost));
            Assert.That(session.canRetry, Is.True);

            yield return Handshake("drill2-start-backend", "drill2-backend-started", 180f);

            var controller = Object.FindFirstObjectByType<ResultSceneController>();
            var retry = GetField<Button>(controller, "retryInterpretationButton");
            var startedAt = Time.realtimeSinceStartup;
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                Assert.That(retry.gameObject.activeInHierarchy, Is.True, $"retry button hidden before attempt {attempt}");
                retry.onClick.Invoke();
                yield return WaitUntil(() => session.interpretationState != InterpretationState.Pending,
                    InterpretationWaitSeconds, $"retry {attempt} never finished");
                Report($"drill2 retry={attempt} state={session.interpretationState} failure={session.failureMessage} " +
                    $"elapsed={Time.realtimeSinceStartup - startedAt:F0}s");
                if (session.interpretationState == InterpretationState.Ready || !session.canRetry)
                {
                    break;
                }
            }

            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Ready), session.failureMessage);
            Assert.That(session.modelUsed, Is.Not.EqualTo("mock_ai"));
        }

        [UnityTest]
        [Timeout(1500000)]
        public IEnumerator LiveReadingsForOneThreeAndTenCards()
        {
            yield return BootToOnlineMenu();

            foreach (var (buttonField, cardCount) in new[] { ("oneCardButton", 1), ("threeCardButton", 3), ("celticCrossButton", 10) })
            {
                yield return EnterReadingRoom();
                var room = Object.FindFirstObjectByType<ReadingRoomController>();
                yield return WaitForBackendSpreads(room);

                GetField<Button>(room, buttonField).onClick.Invoke();
                GetField<TMP_InputField>(room, "questionInput").text = $"联调 {cardCount} 张：此刻我最需要留意什么？";
                var clickedAt = Time.realtimeSinceStartup;
                GetField<Button>(room, "drawButton").onClick.Invoke();

                var deck = Object.FindFirstObjectByType<DeckController>();
                yield return WaitUntil(() => deck.ActiveCards.Count >= 1, 60f, "expected the first card to be dealt");
                var firstCardSeconds = Time.realtimeSinceStartup - clickedAt;

                var session = ReadingSessionStore.Current;
                var release = GetField<TMP_Text>(room, "releaseStatusText").text;
                Assert.That(session, Is.Not.Null);
                Assert.That(session.source, Is.EqualTo(ReadingSource.Online), $"{cardCount}-card reading went offline: {release}");
                Assert.That(session.cardDraws, Has.Length.EqualTo(cardCount));
                Assert.That(Regex.IsMatch(release, "[A-Za-z]"), Is.False, $"English status in the online flow: {release}");

                yield return FlipAllCardsAndReveal(room, cardCount);
                var resultAt = Time.realtimeSinceStartup;
                yield return WaitUntil(() => session.interpretationState != InterpretationState.Pending,
                    InterpretationWaitSeconds, "expected the interpretation to finish");
                var textSeconds = Time.realtimeSinceStartup - resultAt;

                Report($"cards={cardCount} prediction={session.predictionId} state={session.interpretationState} " +
                    $"model={session.modelUsed} firstCard={firstCardSeconds:F1}s resultToText={textSeconds:F1}s " +
                    $"failure={session.failureMessage}");
                Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Ready), session.failureMessage);
                Assert.That(session.modelUsed, Is.Not.EqualTo("mock_ai"), "the backend answered with mock text - check DEEPSEEK_API_KEY");
                Assert.That(firstCardSeconds, Is.LessThan(15f), "dealing must not wait for the AI");

                yield return BackToMenu();
            }
        }

        private static IEnumerator BootToOnlineMenu()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu", 30f, "expected Boot to open the menu");

            var sessionBootstrap = Object.FindFirstObjectByType<BackendSessionBootstrap>();
            Assert.That(sessionBootstrap, Is.Not.Null, "control: Boot created the session bootstrap");
            yield return WaitUntil(
                () => sessionBootstrap.Status == BackendSessionStatus.Online || sessionBootstrap.Status == BackendSessionStatus.Offline,
                30f,
                "expected the guest session to settle");
            Assert.That(sessionBootstrap.Status, Is.EqualTo(BackendSessionStatus.Online), $"guest session failed: {sessionBootstrap.LastError}");
            Assert.That(ApiClient.Shared, Is.Not.Null, "control: Boot shared its ApiClient");
            Assert.That(ApiClient.Shared.HasAccessToken, Is.True);
            Assert.That(InterpretationPoller.Instance, Is.Not.Null, "control: Boot hosts the poller");
        }

        private static IEnumerator EnterReadingRoom()
        {
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.That(menu, Is.Not.Null, "expected the main menu");
            GetField<Button>(menu, "startReadingButton").onClick.Invoke();
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "ReadingRoom" && Object.FindFirstObjectByType<ReadingRoomController>() != null,
                30f,
                "expected the reading room");
            yield return null;
        }

        private static IEnumerator WaitForBackendSpreads(ReadingRoomController room)
        {
            var field = typeof(ReadingRoomController).GetField("backendSpreads", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "control: backendSpreads field exists");
            yield return WaitUntil(() => field.GetValue(room) != null, 30f, "expected backend spreads to load");
        }

        private static IEnumerator FlipAllCardsAndReveal(ReadingRoomController room, int cardCount)
        {
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitUntil(
                () => deck.ActiveCards.Count == cardCount && flow.State == ReadingFlowState.WaitingForFlip,
                60f,
                "expected every card dealt");

            for (var i = 0; i < cardCount; i++)
            {
                var card = deck.ActiveCards[i];
                card.GetComponent<CardClickHandler>().OnPointerClick(new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left,
                });
                yield return WaitUntil(() => card.IsFaceUp, 10f, $"expected card {i + 1} to flip");
            }

            yield return WaitUntil(() => flow.State == ReadingFlowState.ResultReady, 20f, "expected ResultReady");
            var reveal = GetField<Button>(room, "revealResultButton");
            yield return WaitUntil(() => reveal.gameObject.activeInHierarchy, 10f, "expected the reveal button");
            reveal.onClick.Invoke();
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "Result" && Object.FindFirstObjectByType<ResultSceneController>() != null,
                30f,
                "expected the Result screen");
            yield return null;
        }

        private static IEnumerator BackToMenu()
        {
            var controller = Object.FindFirstObjectByType<ResultSceneController>();
            GetField<Button>(controller, "backToMenuButton").onClick.Invoke();
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "MainMenu" && Object.FindFirstObjectByType<MainMenuController>() != null,
                30f,
                "expected the main menu");
            yield return null;
        }

        private static IEnumerator Handshake(string request, string response, float seconds)
        {
            Assert.That(string.IsNullOrEmpty(MarkerDir), Is.False, "TAROT_LIVE_MARKERS is not set");
            File.WriteAllText(Path.Combine(MarkerDir, request), DateTime.UtcNow.ToString("O"));
            var responsePath = Path.Combine(MarkerDir, response);
            yield return WaitUntil(() => File.Exists(responsePath), seconds, $"live.sh did not answer {request}");
        }

        private static void Report(string line)
        {
            Debug.Log("Phase66Live: " + line);
            if (!string.IsNullOrEmpty(ReportPath))
            {
                File.AppendAllText(ReportPath, line + Environment.NewLine);
            }
        }

        private static IEnumerator WaitUntil(Func<bool> predicate, float seconds, string message)
        {
            var timeoutAt = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(predicate(), Is.True, message);
        }

        private static T GetField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}");

            var value = field.GetValue(target) as T;
            Assert.That(value, Is.Not.Null, $"Field {fieldName} on {target.GetType().Name} is null");
            return value;
        }
    }
}
```

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-run/ut.sh" PlayMode t9-live-ignored -testFilter TarotUnity.Tests.PlayMode.Phase66LiveBackendTests
```

预期：`total=3 passed=0 failed=0 skipped=3`（没有设置 `TAROT_LIVE_BACKEND` 时 3 个测试都被忽略）。

- [ ] **Step 3: 确认用户已经配好 `Server/.env`（只打印布尔值）**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
test -f .env && echo env-present || echo "STOP: Server/.env missing"
PYTHONPATH=. /opt/miniconda3/envs/tarot/bin/python - <<'EOF'
from app.core.config import settings
print("deepseek_key_configured =", bool((settings.DEEPSEEK_API_KEY or "").strip()))
print("secret_key_length_ok =", len(settings.SECRET_KEY or "") >= 32)
print("guest_daily_limit_at_least_3 =", settings.GUEST_DAILY_READING_LIMIT >= 3)
print("stale_seconds =", settings.AI_INTERPRETATION_STALE_SECONDS)
EOF
```

预期：`env-present`，三个布尔值都是 `True`，`stale_seconds = 300`。

出现以下任何一种情况都 STOP，请用户自己编辑 `Server/.env`（不要在对话里粘贴 Key）：`.env` 不存在、导入报错、任何一项为 `False`。

- [ ] **Step 4: 创建联调脚本**

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
mkdir -p "$S/online-b-live"
set -C
cat > "$S/online-b-live/live.sh" <<'EOF'
#!/bin/bash
# Phase 66 live acceptance driver. Starts the real backend (the user's Server/.env,
# a throwaway SQLite DB in this run's folder), runs Phase66LiveBackendTests, and stops
# or restarts uvicorn whenever a test writes a *-stop-backend / *-start-backend marker.
set -u
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
SERVER=/Users/maochuandou/BUPT/Game/UnityTarot/Server
PY=/opt/miniconda3/envs/tarot/bin/python
UNITY=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
PROJECT=/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
FILTER="${1:-TarotUnity.Tests.PlayMode.Phase66LiveBackendTests}"
RUN="$S/online-b-live/run-$(date +%Y%m%d-%H%M%S)"

if curl -fsS http://127.0.0.1:8000/api/v1/health/ >/dev/null 2>&1; then
  echo "STOP: something already serves port 8000 - stop that backend first"; exit 64
fi
if pgrep -f 'Unity.app/Contents/MacOS/Unity' >/dev/null; then
  echo "STOP: another Unity process is running"; exit 66
fi
mkdir "$RUN" || { echo "STOP: $RUN exists"; exit 65; }
mkdir "$RUN/markers"

export DATABASE_URL="sqlite:///$RUN/live.db"
export TAROT_LIVE_BACKEND=1
export TAROT_LIVE_MARKERS="$RUN/markers"
export TAROT_LIVE_REPORT="$RUN/report.txt"
export TAROT_BACKEND_URL="http://127.0.0.1:8000/api/v1"
UVICORN_PID=""

start_backend() {
  (cd "$SERVER" && exec "$PY" -m uvicorn app.main:app --host 127.0.0.1 --port 8000 >>"$RUN/uvicorn.log" 2>&1) &
  UVICORN_PID=$!
  for _ in $(seq 1 60); do
    if curl -fsS http://127.0.0.1:8000/api/v1/health/ >/dev/null 2>&1; then
      echo "backend up (pid $UVICORN_PID)"; return 0
    fi
    sleep 1
  done
  echo "backend did not become healthy - see $RUN/uvicorn.log"; return 1
}

stop_backend() {
  if [ -n "$UVICORN_PID" ] && kill -0 "$UVICORN_PID" 2>/dev/null; then
    # SIGKILL simulates a crash: in-flight background generations die with the process.
    kill -9 "$UVICORN_PID"
    wait "$UVICORN_PID" 2>/dev/null
    echo "backend stopped (pid $UVICORN_PID)"
  fi
  UVICORN_PID=""
}

(cd "$SERVER" && "$PY" -m alembic upgrade head && "$PY" -m app.scripts.init_tarot_data) >"$RUN/db-init.log" 2>&1 \
  || { echo "STOP: database init failed - see $RUN/db-init.log"; exit 1; }
start_backend || exit 1

"$UNITY" -projectPath "$PROJECT" -batchmode -nographics -enableUnityConnectPrefs false \
  -runTests -testPlatform PlayMode -testFilter "$FILTER" \
  -testResults "$RUN/live-PlayMode.xml" -logFile "$RUN/live-PlayMode.log" &
UNITY_PID=$!

while kill -0 "$UNITY_PID" 2>/dev/null; do
  for request in "$RUN"/markers/*-stop-backend; do
    [ -e "$request" ] || continue
    answer="${request%-stop-backend}-backend-stopped"
    if [ ! -e "$answer" ]; then stop_backend; touch "$answer"; fi
  done
  for request in "$RUN"/markers/*-start-backend; do
    [ -e "$request" ] || continue
    answer="${request%-start-backend}-backend-started"
    if [ ! -e "$answer" ]; then start_backend && touch "$answer"; fi
  done
  sleep 1
done

wait "$UNITY_PID"
CODE=$?
stop_backend
echo "unity exit=$CODE"
"$PY" "$S/online-b-run/summarize.py" "$RUN/live-PlayMode.xml" "$RUN/live-PlayMode.log"
echo "--- report ---"
cat "$RUN/report.txt" 2>/dev/null
echo "--- generation attempts ---"
"$PY" "$S/online-b-live/count_attempts.py" "$RUN/live.db"
echo "--- AI fallback lines in uvicorn.log ---"
grep -c 'Falling back to' "$RUN/uvicorn.log"
echo "run dir: $RUN"
exit $CODE
EOF
cat > "$S/online-b-live/count_attempts.py" <<'EOF'
"""Phase 66 live acceptance: AI generation attempts per reading in the live SQLite DB."""
import sqlite3
import sys

connection = sqlite3.connect(sys.argv[1])
rows = connection.execute(
    "SELECT id, status, interpretation_attempts, question FROM predictions ORDER BY id"
).fetchall()
print("id | status | attempts | question")
live_attempts = 0
for prediction_id, status, attempts, question in rows:
    print(f"{prediction_id} | {status} | {attempts} | {question}")
    if question and question.startswith("联调"):
        live_attempts += attempts or 0
print(f"attempts for the three 联调 readings: {live_attempts} (expected 3)")
EOF
set +C
ls -l "$S/online-b-live/"
```

预期：列出 `live.sh` 和 `count_attempts.py`。

- [ ] **Step 5: 运行联调**

`live.sh` 大约需要 15–25 分钟，而 Bash 工具单次最长 10 分钟，所以用 `run_in_background: true` 运行，完成通知到达后再读取输出：

```bash
S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$S/online-b-live/live.sh"
```

预期输出：
- `unity exit=0`，`total=3 passed=3 failed=0`
- report 中有：
  - 三行 `cards=1|3|10 … state=Ready model=<不是 mock_ai> firstCard=<小于 15>s`
  - `drill1 source=Offline release=暂时连不上占卜服务，这一局使用离线解读。`
  - `drill2 failure=与占卜服务的连接中断了，可以再试一次。 canRetry=True`
  - 1–2 行 `drill2 retry=…`，最后一行 `state=Ready`
- `attempts for the three 联调 readings: 3 (expected 3)`

失败时的判读：
- `guest session failed` 或 `backend did not become healthy`：查看 `$RUN/uvicorn.log` 的最后 50 行，报告后 STOP（不要打印 `.env`）。
- `the backend answered with mock text`：说明 Key 没有生效。STOP，请用户检查 `Server/.env`。
- `drill2 … the AI beat the drill`：AI 在后端被停掉之前就生成完了。只重跑演练 2：`bash "$S/online-b-live/live.sh" TarotUnity.Tests.PlayMode.Phase66LiveBackendTests.LiveDrillBackendLostWhileGenerating`。
- 其他断言失败：原样报告失败信息和 report 后 STOP；不要为了通过去改断言。

- [ ] **Step 6:（可选，不阻塞）请用户亲手玩一局**

在汇报中建议用户：在 `Server/` 下按 `Server/README.md` 启动后端，在 Unity Hub 打开工程，从 `Assets/Scenes/Boot.unity` 进入 Play，玩一局三张牌，主观感受"发牌不再等待 AI"以及结果页从「牌意正在汇聚……」切换到正文的过程。

- [ ] **Step 7: 写执行记录**

`PROJECT_COMPLETION_PLAN.md`（仓库根目录），用 Edit 把

```markdown
- 后端测试在 `Server/` 下与源提交干净导出的结果逐条一致：`pytest -q tests` 为 `166 passed, 1 warning`，`pytest -q` 为 `166 passed, 1 warning`。
```

替换为下面的内容。提交前把所有尖括号换成本次的实测值；实测结果与预期不符时如实填写：

```markdown
- 后端测试在 `Server/` 下与源提交干净导出的结果逐条一致：`pytest -q tests` 为 `166 passed, 1 warning`，`pytest -q` 为 `166 passed, 1 warning`。

### 2.5 本轮执行记录（<执行日期 YYYY-MM-DD>）

- 在线解读闭环（子项目 A）完成：
  - 后端新增异步解读接口（PR #1）。
  - Unity 修复了占卜房没有使用访客令牌的问题。
  - 洗牌时只建记录和抽牌，拿到牌就发牌；AI 解读由常驻轮询器在后台取回。
- 结果页新增「生成中 / 完成 / 失败 / 离线」四种显示，失败时可以重新解读或查看离线解读；在线流程中玩家看到的状态全部是中文。
- 测试：
  - Unity EditMode `<E0+57> passed`，PlayMode `<P0+25> passed, 3 skipped`（真实联调测试默认跳过）。
  - 后端 `pytest -q tests` 与 `pytest -q` 均为 `188 passed`。
- 真实 DeepSeek 联调：
  - 1、3、10 张牌阵各一局都得到了 AI 解读。
  - 从点击到发出第一张牌分别耗时 `<a>` / `<b>` / `<c>` 秒；从进入结果页到解读出现分别耗时 `<x>` / `<y>` / `<z>` 秒。
  - 3 局的 `interpretation_attempts` 之和为 `<n>`。
- 故障演练：
  - 开局前关闭后端：进入离线局，显示「暂时连不上占卜服务，这一局使用离线解读。」
  - 生成中关闭后端：显示连接中断；重启后第 `<k>` 次「重新解读」成功，耗时 `<t>` 秒。
- 待决事项：
  - 后端进程在生成中途退出后，记录要过 300 秒才能被重新抢占；是否在后端启动时回收，待定。
  - 选牌阵阶段的英文文案属于子项目 B。
```

- [ ] **Step 8: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
ls UnityClient/TarotUnity/Assets/Tests/PlayMode/Phase66LiveBackendTests.cs.meta
grep -n '<' PROJECT_COMPLETION_PLAN.md | grep -n '2\.5\|执行日期\|E0+57\|P0+25' || echo placeholders-filled
git add UnityClient/TarotUnity/Assets/Tests/PlayMode/Phase66LiveBackendTests.cs \
  UnityClient/TarotUnity/Assets/Tests/PlayMode/Phase66LiveBackendTests.cs.meta \
  PROJECT_COMPLETION_PLAN.md
git commit -F - <<'EOF'
test(unity): add the opt-in live backend acceptance run and record Phase 66

Phase66LiveBackendTests drives Boot -> menu -> reading room -> Result against
the real backend and DeepSeek when TAROT_LIVE_BACKEND=1, including the
backend-down and backend-lost-while-generating drills. The completion plan
records the measured results.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
git status --short
git log --oneline 0fdf8e4..HEAD
```

预期：`placeholders-filled`；`git status` 只剩字体图集这类不入库的改动（逐条列在汇报里）；`git log` 列出计划提交和 Task 1–9 的 9 个提交。

- [ ] **Step 9: 完成标准核对（spec 8.6）并收尾**

逐条核对，并附上证据：
1. 后端全量 pytest 通过（Step 1）。
2. Unity EditMode 和 PlayMode 全绿。Step 2 在 Step 1 之后新增了联调测试文件，所以 Step 8 提交之后要再跑一次全量：

   ```bash
   S=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
   bash "$S/online-b-run/ut.sh" EditMode t9-final
   bash "$S/online-b-run/ut.sh" PlayMode t9-final
   ```

   预期：EditMode `total=E0+57 failed=0`；PlayMode `total=P0+28 failed=0 skipped=3`。
3. 8.5 全部通过，截图齐全（Step 5；Task 8 生成的 `Docs/VisualReview/Phase66/` 四张截图）。
4. 在线流程中玩家看到的文字没有原始报文或英文状态：`ReadingRoomFlowCopyIsChineseAndCentralised` 测试，以及联调测试对底部状态栏的断言。

然后使用 superpowers:finishing-a-development-branch：
- 本分支叠在 PR #1 的分支上，基准分支是 `feat/online-interpretation-loop`；如果 PR #1 已经合并，则是 `main`。
- `push` 之前必须征得用户同意。

---

## 测试数量总表

`E0` 和 `P0` 是 Task 0 实测的基线（上一次记录是 308 和 33）。

| 任务结束时 | EditMode | PlayMode |
| --- | --- | --- |
| Task 0 | `E0` | `P0` |
| Task 1 | `E0+2` | `P0` |
| Task 2 | `E0+37` | `P0` |
| Task 3 | `E0+42` | `P0` |
| Task 4 | `E0+42` | `P0+6`（原有的 1 个测试换成 7 个） |
| Task 5 | `E0+47` | `P0+19` |
| Task 6 | `E0+49` | `P0+23` |
| Task 7 | `E0+56` | `P0+25` |
| Task 8 | `E0+57` | `P0+25` |
| Task 9 | `E0+57` | `P0+28`（其中 3 个 skipped） |

## 自检记录（写计划时完成）

**spec 覆盖情况：**

| spec 条目 | 实现位置 |
| --- | --- |
| 6.1 `ApiClient.Shared` | Task 1 |
| 6.2 `ApiError` | Task 2（类型与归类）、Task 4（结构化请求） |
| 6.3 快照字段 | Task 2（字段与枚举）、Task 3（映射） |
| 6.4 `BackendReadingService` | Task 4（新方法）、Task 6（删除 `CompleteReading`）、Task 2（`RecordInterpretAsync`） |
| 6.5 `InterpretationPoller` | Task 5 |
| 6.6 占卜房流程 | Task 6 |
| 7.1 四种显示 | Task 7：20 秒慢提示、淡入、`mock_ai` 标签、「回到牌桌」时调用 `Stop` |
| 7.1 离线提醒文案 | Task 2 |
| 7.2 结果页新增 UI | Task 7 |
| 7.3 文案表 | Task 2（常量与映射），Task 5、6、7 使用 |
| 8.3 路由 | Task 2 |
| 8.3 文案映射和小时取整 | Task 2 |
| 8.3 快照默认值 | Task 2 |
| 8.3 轮询间隔和 330 秒 | Task 5 |
| 8.3 令牌回归测试 | Task 1 |
| 8.3 `Result.unity` 守护测试 | Task 7 |
| 8.3 流程文案守护与源码扫描 | Task 6 |
| 8.4 场景 1、2、3、5、6、7、8 | Task 5 |
| 8.4 场景 4 | Task 4（服务层）、Task 6（场景层） |
| 8.5 真实联调 | Task 9（见偏差 U9、U11、U12） |
| 8.5 截图 | Task 8（见偏差 U10） |
| 8.6 完成标准 | Task 9 Step 9 |

**占位词扫描**：计划中没有 TBD 或 TODO。唯一的尖括号出现在 Task 9 Step 7 的执行记录模板里，那是需要填写实测值的位置，Step 8 用 `grep` 确认已经填完。

**名称一致性**（逐项核对过跨任务使用的名称）：
- `ApiClient`：`SetShared`、`ClearShared`、`PostRecord`、`PostDraw`、`FetchRecordCards`、`PostInterpretAsync`、`FetchRecordDetail`
- `ReadingSessionMapper`：`FromBackendStart`、`HasInterpretation`、`IsRealInterpretation`、`ApplyInterpretation`
- `InterpretationPoller`：`Configure`、`Begin`、`Retry`、`Stop`、`UseOffline`、`ApplyFailure`、`ApplyOffline`、`PollDelaySeconds`、`TotalDeadlineSeconds`、`MaxConsecutiveNetworkErrors`
- `ReleaseUxCopy` 的全部常量
- 结果页字段：`interpretationStatusText`、`modeLabelText`、`retryInterpretationButton`、`offlineInterpretationButton`、`readingContentGroup`
- 场景对象名：`Phase66_InterpretationStatus`、`Phase66_ModeLabel`、`Phase66_RetryInterpretationButton`、`Phase66_OfflineInterpretationButton`


# 结果页阅读体验实现计划（子项目 C，Phase 67）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 结果页改完后要做到五件事：
- 1/3/5/10 张牌在各种窗口比例下，正文都不压框、框外不露字；
- 滚动时有明确提示；
- 牌面分析按牌分块，点牌可以跳到对应的一块；
- AI 文字按纯文本显示；
- 生成超过 20 秒可以改看离线解读。

**Architecture:** 核心计算都做成纯函数：`ReadingTextSanitizer`、`CardAnalysisParser`、`CardAnalysisFormatter`、`ResultSpreadLayout`。纯函数之外有两类改动：
- 新增三个小组件：`ResultReadingNavigator`、`ResultSpreadCellTarget`、`ResultCanvasAspectFit`。
- `ResultRevealDirector` 增加"伴随组"（companion）支持。

场景结构统一由 `Phase67ResultReadingBootstrapper` 搭好，最后由 `ResultPanelPresenter` 把以上部分接起来。

**Tech Stack:** Unity 6000.3.16f1（URP、uGUI、TextMeshPro），NUnit EditMode/PlayMode，FastAPI + pytest（只改一行提示词）。

**Spec:** `docs/superpowers/specs/2026-09-13-result-reading-experience-design.md`（已批准，提交 `e56d38d`）。执行者要把 spec 和本计划一起读。

## Global Constraints

- **仓库与分支：** 仓库 `/Users/maochuandou/BUPT/Game/UnityTarot`，分支 `feat/result-reading-experience`（从 `dd2dd75` 拉出，起点 `e56d38d`）。Unity 工程在 `UnityClient/TarotUnity`，下文的 `Assets/…`、`Docs/…` 都相对这个目录。
- **脚本目录 `$S`：** `/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch`（git 已忽略）。每个 bash 代码块开头都要重新设置 `S=...`。
- **测试命令：**
  - Unity 测试一律用 `bash "$S/run/ut.sh" <EditMode|PlayMode> <新标签> [-testFilter …]`（Task 0 创建），一次只跑一个 Unity 进程，Bash 超时设为 600000 ms。
  - 后端测试用 `bash "$S/run/pt.sh" <参数>`：它在没有 `.env` 的目录里运行，并设置 `PYTHONPATH=Server`。
- **Bootstrapper 的执行方式**（先设置 `ulimit -n 10240`）：`"$UNITY" -projectPath "$PROJECT" -batchmode -nographics -enableUnityConnectPrefs false -executeMethod <类>.Run -quit -logFile <绝对路径>`。截图 Builder 必须去掉 `-nographics`。
  - `UNITY=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity`
  - `PROJECT=/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity`
- **提交信息：** 每条都以下面两行结尾，一字不改。提交后用 `git log -1 --format=%B | tail -3` 核对：
  ```
  Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
  ```
  `Co-Authored-By` 这一行写的是会话署名，不是你运行的模型名，不要改成自己的模型。
- **禁止事项：**
  - 不读、不创建、不打印 `Server/.env`；
  - 不删除任何文件；不对文件用 `git checkout/restore/reset/stash`；
  - 不 push；
  - 不提交 `Assets/Fonts/LXGWWenKai-Regular SDF.asset`（字体图集噪声），也不还原它；
  - 不 `git add` 未跟踪的 `Assets/Tests/PlayMode/Phase66LiveBackendTests.cs` 及其 `.meta`；
  - 不改 `/opt/miniconda3/envs/tarot` 里的包。
- **基线：** EditMode `total=370`；PlayMode `total=68 passed=65 skipped=3`（skipped 是未跟踪的真实联调测试）；后端 `188 passed`（`pt.sh tests` 和 `pt.sh .` 各跑一次）。
- **画布：** 参考分辨率 1280×720（`TarotUiSpacing.ReferenceWidth/ReferenceHeight`），场景里保存的匹配系数仍是 0.5（`Phase30CrossScreenConsistencyTests` 守护）；运行时的匹配方式由 `ResultCanvasAspectFit` 切换。
- **金框几何（实测）：**
  - `ResultReadingScroll` 的 Image 使用 `TarotPanel.png`（512×512，九宫格边距 96 px），`pixelsPerUnitMultiplier = 2`，画布 PPU 100，所以边框按 1/2 渲染。
  - 换算成画布单位：0–7 是透明边，10–12.5 是外金线，17–18 是内金线，18 以内是深色底。
  - 视口四边内缩 24（内金线再往里 6）。
- **玩家可见文案：** 放在 `ReleaseUxCopy` 里，新增常量不含 ASCII 字母。
- **spec 定下的数值（验收时不能放宽）：**
  - 16:9 下阅读区高度：3–5 张 ≥ 326，10 张 ≥ 282；16:10 下两者再各多 80。
  - 10 张牌排两行、每行 5 张，位置标签字号 ≥ 14。
  - 生成中满 20 秒出现离线按钮；单个字段上限 4000 字。
  - 点牌滚动 0.35 秒，目标停在视口顶部往下 12%；高亮 1.2 秒；悬停放大 1.04 倍。
  - 截图中框外文字像素数为 0。

## 计划相对 spec 的偏差（写计划时核实）

| 编号 | 偏差 | 理由 |
| --- | --- | --- |
| P1 | 单张牌的阅读框从 736×448、x=178 改为 772×448、x=160（右边缘仍在 546）。 | 视口要内缩 24 才能让开金框。仍用 736 宽的话，正文只剩 632，`Phase8VisualIdentityTests` 要求 ≥ 700。772 宽时正文 = 772 − 48 − 8 − 14 = 702。 |
| P2 | 标题区和按钮区不改锚点，改由 `ResultCanvasAspectFit` 在运行时按画布上下沿重新定位（在 720 高时位置与场景里保存的完全一致）。 | `Phase25`、`Phase30` 的守护测试在 EditMode 里用世界坐标比较标题和阅读区，而 EditMode 下 Overlay 画布没有尺寸，改锚点会让它们失真；运行时效果与 spec 4.7 相同。 |
| P3 | 牌的缩放：一行时 0.52（牌图高 111），两行时 0.31（牌图高 66），不采用 spec 写的 130 和 76。 | 实测牌格：牌框 234 高、中心上移 18，标签在 −118；按钮 48 高，上沿在 −276。要保住阅读区高度 ≥ 326 和 ≥ 282 这两条硬指标，只能用这两个缩放值。 |
| P4 | `ReadingTextSanitizer.Plain` 只在文字含 `<` 时才包 `<noparse>`。 | 不含 `<` 的文字不可能构成标签，安全性相同；普通文字保持逐字不变，现有断言 `.text` 的测试（Phase 66 等）继续通过。 |
| P5 | 点击目标挂在牌格根节点上，位置标签打开 `raycastTarget`。 | `HolographicHeroCard` 只处理进入、离开、移动三种指针事件，点击会从牌图冒泡到牌格根节点；进入和离开事件本来就会沿父链下发。 |
| P6 | 视口内缩 24 之后，内容区内边距只用 左 8、右 14、上 8、下 28；分段间距 8，标题上边距 10，正文行距 +10。 | 受 P1 的宽度限制。文字离内金线仍有 14 的距离；右侧 14 给滚动条留位置，下侧 28 给渐隐留位置。 |
| P7 | `ResultRevealDirector` 保留原来的 7 个主分组，其余元素作为"伴随组"随对应主分组一起淡入。 | 揭示总时长不变（约 2.5 秒），不会因为多加元素而变长。 |
| P8 | 删除 `ResultPanelPresenter` 里已无用的 `spreadReadingPos/Size`、`spreadRowWidth/BasePitch/MinCellScale/CellY` 字段，布局改由 `ResultSpreadLayout` 负责。 | 旧的 Phase 60/62 Bootstrapper 执行 Phase 67 之后本就不能再跑（会重建牌列，冲掉 Phase 67 的接线），不为它们保留死字段。 |

## 文件结构

**新建（运行时代码，`Assets/Scripts/UI/`，程序集 `TarotUnity.Runtime`）：**
- `ReadingTextSanitizer.cs`：截断外部文字、中和其中的标签。
- `CardAnalysisParser.cs`：把 `card_analysis` 拆成每张牌一段，失败时返回失败。
- `CardAnalysisFormatter.cs`：拼出分块的富文本，并记录每个小标题的字符下标（`CardBlockRange`）。
- `ResultSpreadLayout.cs`：输入牌数和画布高度，算出牌格位置/缩放、标签字号、阅读框矩形。
- `ResultCanvasAspectFit.cs`：按宽高比切换匹配方式，并把上下沿元素钉在画布边缘。
- `ReadingFadeGradient.cs`：阅读框底部渐隐的顶点透明度渐变（`BaseMeshEffect`）。
- `ResultReadingNavigator.cs`：控制渐隐的显隐；点牌后平滑滚动并高亮小标题。
- `ResultSpreadCellTarget.cs`：牌格的悬停反馈和点击。

**修改：**
- `Assets/Scripts/UI/ReleaseUxCopy.cs`：新增 4 个常量。
- `Assets/Scripts/Presentation/ResultRevealDirector.cs`：伴随组。
- `Assets/Scripts/UI/ResultPanelPresenter.cs`：整合以上部分。
- `Assets/Scenes/Result.unity`：由 Bootstrapper 修改。
- `Assets/Tests/EditMode/Phase29ResultScrollTests.cs`：顺序断言改为"相对顺序且相邻"。
- `Server/app/services/tarot_service.py`：`card_analysis` 的格式说明。
- 文档：
  - `docs/superpowers/specs/2026-09-12-online-interpretation-loop-design.md`：7.1 表"生成中"一行。
  - `Docs/PHASE66_ONLINE_INTERPRETATION.md`：已知限制。
  - `Docs/PROJECT_CHRONICLE.md`。

**新建（编辑器、测试、文档）：**
- `Assets/Editor/Phase67ResultReadingBootstrapper.cs`
- `Assets/Editor/Phase67ResultReadingCaptureBuilder.cs`
- `Assets/Tests/EditMode/Phase67ReadingTextTests.cs`
- `Assets/Tests/EditMode/Phase67ResultLayoutTests.cs`
- `Assets/Tests/EditMode/Phase67ResultReadingComponentsTests.cs`
- `Assets/Tests/EditMode/Phase67ResultSceneStructureTests.cs`
- `Assets/Tests/EditMode/Phase67ResultPresenterTests.cs`
- `Assets/Tests/PlayMode/Phase67ResultReadingPlayTests.cs`
- `Assets/Tests/PlayMode/Phase67SlowGenerationTests.cs`
- `Server/tests/test_prompt_card_analysis_format.py`
- `Docs/PHASE67_RESULT_READING.md`
- `Docs/VisualReview/Phase67/*.png`（10 张截图）

Unity 会给每个新建的 `.cs` 生成 `.meta`，这些 `.meta` 必须和 `.cs` 一起提交。

---

### Task 0: 脚本目录与基线

**Files:**
- Create（scratch，不入库）：`$S/run/ut.sh`、`$S/run/summarize.py`、`$S/run/pt.sh`

**Interfaces:**
- Produces：
  - `bash "$S/run/ut.sh" <EditMode|PlayMode> <tag> [Unity 参数…]`：打印一行 `total=… passed=… failed=… skipped=…`，并逐条列出失败的测试。
  - `bash "$S/run/pt.sh" <参数…>`：`tests`、`tests/…`、`app/…` 会自动加上 `Server/` 前缀，`.` 表示整个 `Server` 目录。

- [ ] **Step 1: 确认起点**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git rev-parse --abbrev-ref HEAD; git rev-parse --short HEAD; git status --short
pgrep -fl 'Unity.app/Contents/MacOS/Unity' || echo no-unity
```

预期：
- 分支为 `feat/result-reading-experience`，HEAD 为 `e56d38d`（如果本计划已提交，则是计划提交的那个 SHA）；
- `git status` 只列出字体图集和 `Phase66LiveBackendTests.cs`（含 `.meta`）；
- 输出 `no-unity`。

任何一项不符合就 STOP。

- [ ] **Step 2: 创建脚本**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
test ! -e "$S/run" || { echo "STOP: $S/run already exists"; exit 1; }
mkdir -p "$S/run"
set -C
cat > "$S/run/ut.sh" <<'EOF'
#!/bin/bash
# Phase 67 Unity test runner: ut.sh <EditMode|PlayMode> <tag> [extra Unity args]
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
# bee_backend aborts (exit 134) when the open-file limit is too low.
ulimit -n 10240 2>/dev/null || ulimit -n 4096 2>/dev/null || true
"$UNITY" -projectPath "$PROJECT" -batchmode -nographics -enableUnityConnectPrefs false \
  -runTests -testPlatform "$PLATFORM" -testResults "$XML" -logFile "$LOG" "$@"
CODE=$?
echo "unity exit=$CODE"
/opt/miniconda3/envs/tarot/bin/python "$HERE/summarize.py" "$XML" "$LOG"
exit $CODE
EOF
cat > "$S/run/summarize.py" <<'EOF'
"""Summarize a Unity Test Framework NUnit3 result file (Phase 67 runner)."""
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
cat > "$S/run/pt.sh" <<'SH'
#!/bin/bash
# Runs backend pytest from a directory without .env, so Server/.env never leaks into tests.
# Arguments "tests", "tests/...", "app", "app/..." resolve under Server/; "." means the whole Server tree.
cd "$(dirname "$0")" || exit 2
SERVER=/Users/maochuandou/BUPT/Game/UnityTarot/Server
args=()
for a in "$@"; do
  case "$a" in
    .) args+=("$SERVER") ;;
    tests|tests/*|app|app/*) args+=("$SERVER/$a") ;;
    *) args+=("$a") ;;
  esac
done
PYTHONPATH="$SERVER" PYTHONDONTWRITEBYTECODE=1 exec /opt/miniconda3/envs/tarot/bin/python -m pytest -q -p no:cacheprovider "${args[@]}"
SH
set +C
ls -l "$S/run"
```

预期：列出 `ut.sh`、`summarize.py`、`pt.sh` 三个文件。

- [ ] **Step 3: 跑基线**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c0
bash "$S/run/ut.sh" PlayMode c0
bash "$S/run/pt.sh" tests 2>&1 | tail -n 1
bash "$S/run/pt.sh" . 2>&1 | tail -n 1
```

预期：
- EditMode `total=370 passed=370 failed=0`
- PlayMode `total=68 passed=65 failed=0 skipped=3`
- 后端两次都是 `188 passed`

任何一项不符合就 STOP，并原样报告输出。本任务不提交。

### Task 1: 纯文本显示与牌面分析分块（纯函数）

**Files:**
- Create: `Assets/Scripts/UI/ReadingTextSanitizer.cs`
- Create: `Assets/Scripts/UI/CardAnalysisParser.cs`
- Create: `Assets/Scripts/UI/CardAnalysisFormatter.cs`
- Modify: `Assets/Scripts/UI/ReleaseUxCopy.cs`（紧接在 `OfflineWarning` 常量之后）
- Test: `Assets/Tests/EditMode/Phase67ReadingTextTests.cs`

**Interfaces:**
- Consumes：
  - `CardDrawData`（`position_name`、`is_reversed`、`tarot_card.name_zh`）
  - `LocalReadingSimulator.CreatePlaceholderDraws(int)`：3 张时依次是过去/愚者、现在/魔术师、建议/女祭司（逆位）
- Produces：
  - `ReadingTextSanitizer`：
    - `const int MaxFieldLength = 4000`
    - `const string TruncationMark = "……"`
    - `string Visible(string)`：玩家实际看到的字符，已截断、已中和，不带标签
    - `string Wrap(string visible)`：含 `<` 时包 `<noparse>`
    - `string Plain(string)`：等于 `Wrap(Visible(x))`
  - `CardAnalysisParseResult`：`bool Success`、`string[] Bodies`、`string[] Leftovers`、`static Failed`
  - `CardAnalysisParser.Parse(string cardAnalysis, CardDrawData[] draws)`：返回 `CardAnalysisParseResult`
  - `CardBlockRange`：`int CardIndex, HeadingStart, HeadingLength`，其中 `HeadingStart` 按 TMP 的 `characterInfo` 下标计
  - `FormattedCardAnalysis`：`string RichText`、`CardBlockRange[] Ranges`
  - `CardAnalysisFormatter`：
    - `const string HeadingColorHex = "#DBA13D"`
    - `string BuildHeading(CardDrawData)`
    - `FormattedCardAnalysis Build(string cardAnalysis, CardDrawData[] draws)`
  - `ReleaseUxCopy`：`ResultSectionWarning = "提醒"`、`CardHeadingSeparator = " · "`、`CardUprightMark = "（正位）"`、`CardReversedMark = "（逆位）"`

- [ ] **Step 1: 写失败的测试**

创建 `Assets/Tests/EditMode/Phase67ReadingTextTests.cs`：

```csharp
using System.Text.RegularExpressions;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 67: external reading text is shown literally, and the per-card analysis is
    /// split into blocks whose headings come from the client's own card data.
    /// </summary>
    public sealed class Phase67ReadingTextTests
    {
        // 过去 / 现在 / 建议: 愚者, 魔术师, 女祭司 (the third card is reversed).
        private static CardDrawData[] ThreeCards()
        {
            return LocalReadingSimulator.CreatePlaceholderDraws(3);
        }

        private static string OfflineAnalysis(CardDrawData[] draws)
        {
            return LocalReadingSimulator.CreateSession(2, "三牌阵", "问题？", "general", draws).cardAnalysis;
        }

        private static string StripTags(string richText)
        {
            return Regex.Replace(richText, "<[^>]+>", string.Empty);
        }

        [Test]
        public void PlainLeavesTagFreeTextUntouched()
        {
            const string text = "过去的积累给了你底气。";
            Assert.That(ReadingTextSanitizer.Plain(text), Is.EqualTo(text));
        }

        [Test]
        public void PlainWrapsTextThatContainsAngleBrackets()
        {
            Assert.That(ReadingTextSanitizer.Plain("<b>粗</b>"), Is.EqualTo("<noparse><b>粗</b></noparse>"));
        }

        [Test]
        public void PlainNeutralisesAnEmbeddedNoparseClose()
        {
            Assert.That(ReadingTextSanitizer.Plain("a</NOPARSE><size=200>b"),
                Is.EqualTo("<noparse>a＜/NOPARSE><size=200>b</noparse>"));
        }

        [Test]
        public void PlainTruncatesPastTheFieldCapAndLogs()
        {
            var text = new string('字', ReadingTextSanitizer.MaxFieldLength + 1);
            LogAssert.Expect(LogType.Warning, new Regex("truncated a 4001-character field"));

            var plain = ReadingTextSanitizer.Plain(text);

            Assert.That(plain.Length,
                Is.EqualTo(ReadingTextSanitizer.MaxFieldLength + ReadingTextSanitizer.TruncationMark.Length));
            Assert.That(plain, Does.EndWith(ReadingTextSanitizer.TruncationMark));
            Assert.That(ReadingTextSanitizer.Plain(new string('字', ReadingTextSanitizer.MaxFieldLength)).Length,
                Is.EqualTo(ReadingTextSanitizer.MaxFieldLength), "control: text at the cap is kept whole");
        }

        [Test]
        public void PlainTurnsNullIntoEmpty()
        {
            Assert.That(ReadingTextSanitizer.Plain(null), Is.Empty);
            Assert.That(ReadingTextSanitizer.Wrap(null), Is.Empty);
        }

        [Test]
        public void ParsesTheOfflineFormat()
        {
            var draws = ThreeCards();
            var result = CardAnalysisParser.Parse(OfflineAnalysis(draws), draws);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Bodies, Is.EqualTo(new[] { "新的开始、信任、迈出第一步", "专注、意志、能力", "直觉、沉默、隐藏的知识" }));
            Assert.That(result.Leftovers, Is.Empty);
        }

        [Test]
        public void ParsesTheBackendMockFormatWithEmptyBodies()
        {
            var draws = ThreeCards();
            var result = CardAnalysisParser.Parse("1. 过去：愚者（正位）\n2. 现在：魔术师（正位）\n3. 建议：女祭司（逆位）", draws);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Bodies, Is.EqualTo(new[] { string.Empty, string.Empty, string.Empty }));
        }

        [Test]
        public void ParsesOrdinalsSeparatorsAndPositionOnlyPrefixes()
        {
            var draws = ThreeCards();
            const string analysis = "（1）过去——旧的节奏正在松动。\r\n② 现在：魔术师 资源齐备。\n三、建议 · 女祭司（逆位）：别只听外界的声音。";

            var result = CardAnalysisParser.Parse(analysis, draws);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Bodies, Is.EqualTo(new[] { "旧的节奏正在松动。", "资源齐备。", "别只听外界的声音。" }));
        }

        [Test]
        public void FallsBackToLineOrderWhenCountsMatch()
        {
            var draws = ThreeCards();
            var result = CardAnalysisParser.Parse("1. 旧的节奏正在松动。\n2. 资源齐备。\n3. 别只听外界的声音。", draws);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Bodies, Is.EqualTo(new[] { "旧的节奏正在松动。", "资源齐备。", "别只听外界的声音。" }));
        }

        [Test]
        public void FailsWhenLinesCannotBeMatched()
        {
            var result = CardAnalysisParser.Parse("整组牌讲的是节奏。\n也讲专注。", ThreeCards());
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void KeepsUnmatchedLinesAsLeftovers()
        {
            var draws = ThreeCards();
            var result = CardAnalysisParser.Parse(OfflineAnalysis(draws) + "\n三张牌合起来看，节奏在变。", draws);

            Assert.That(result.Success, Is.True, "control: every card still found its line");
            Assert.That(result.Leftovers, Is.EqualTo(new[] { "三张牌合起来看，节奏在变。" }));
        }

        [Test]
        public void FailsOnEmptyInput()
        {
            Assert.That(CardAnalysisParser.Parse(null, ThreeCards()).Success, Is.False);
            Assert.That(CardAnalysisParser.Parse("  \n ", ThreeCards()).Success, Is.False);
            Assert.That(CardAnalysisParser.Parse("过去：愚者 — 勇气", null).Success, Is.False);
        }

        [Test]
        public void FormatsBlocksWithClientHeadingsAndRecordsTheirPositions()
        {
            var draws = ThreeCards();
            var formatted = CardAnalysisFormatter.Build(OfflineAnalysis(draws), draws);
            var visible = StripTags(formatted.RichText);
            var expected = new[] { "过去 · 愚者（正位）", "现在 · 魔术师（正位）", "建议 · 女祭司（逆位）" };

            Assert.That(formatted.Ranges.Length, Is.EqualTo(3));
            Assert.That(formatted.RichText, Does.Contain("<color=" + CardAnalysisFormatter.HeadingColorHex + ">"));
            for (var i = 0; i < 3; i++)
            {
                var range = formatted.Ranges[i];
                Assert.That(range.CardIndex, Is.EqualTo(i));
                Assert.That(CardAnalysisFormatter.BuildHeading(draws[i]), Is.EqualTo(expected[i]));
                Assert.That(visible.Substring(range.HeadingStart, range.HeadingLength), Is.EqualTo(expected[i]));
            }

            Assert.That(visible, Does.Contain("新的开始、信任、迈出第一步"));
            Assert.That(visible, Does.Not.Contain("过去：愚者"), "the line prefix is replaced by the client heading");
        }

        [Test]
        public void FormatsTheRawTextWhenParsingFailed()
        {
            const string analysis = "整组牌讲的是节奏。\n也讲专注。";
            var formatted = CardAnalysisFormatter.Build(analysis, ThreeCards());

            Assert.That(formatted.RichText, Is.EqualTo(analysis));
            Assert.That(formatted.Ranges, Is.Empty);
        }

        [Test]
        public void ResultReadingCopyHasNoAsciiLetters()
        {
            var copies = new[]
            {
                ReleaseUxCopy.ResultSectionWarning, ReleaseUxCopy.CardHeadingSeparator,
                ReleaseUxCopy.CardUprightMark, ReleaseUxCopy.CardReversedMark,
            };

            foreach (var copy in copies)
            {
                Assert.That(string.IsNullOrWhiteSpace(copy), Is.False, "control: the constant has content");
                Assert.That(Regex.IsMatch(copy, "[A-Za-z]"), Is.False, $"'{copy}' should not contain ASCII letters");
            }
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c1-red -testFilter TarotUnity.Tests.EditMode.Phase67ReadingTextTests
```

预期：`NO RESULTS XML`，下面列出 `error CS0103` 或 `error CS0246`，指出 `ReadingTextSanitizer`、`CardAnalysisParser`、`CardAnalysisFormatter` 不存在，或 `ReleaseUxCopy` 缺少新常量。

- [ ] **Step 3: 新增文案常量**

在 `Assets/Scripts/UI/ReleaseUxCopy.cs` 中，把

```csharp
        public const string OfflineWarning = "这是离线解读，由本地牌义生成，未经过 AI。";
```

替换为

```csharp
        public const string OfflineWarning = "这是离线解读，由本地牌义生成，未经过 AI。";

        // Phase 67: Result reading (spec C 4.3, 4.6). No ASCII letters.
        public const string ResultSectionWarning = "提醒";
        public const string CardHeadingSeparator = " · ";
        public const string CardUprightMark = "（正位）";
        public const string CardReversedMark = "（逆位）";
```

- [ ] **Step 4: 创建 `Assets/Scripts/UI/ReadingTextSanitizer.cs`**

```csharp
using System.Text.RegularExpressions;
using UnityEngine;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 67 (spec C 4.5): text from the server or the player - the AI interpretation, the
    /// question, spread and card names - is shown literally. TMP rich text stays on for the
    /// client's own styling, so external text that contains '&lt;' is wrapped in noparse; text
    /// without '&lt;' cannot form a tag and is returned unchanged.
    /// </summary>
    public static class ReadingTextSanitizer
    {
        public const int MaxFieldLength = 4000;
        public const string TruncationMark = "……";

        private const string NoparseOpen = "<noparse>";
        private const string NoparseClose = "</noparse>";

        private static readonly Regex EmbeddedNoparseClose =
            new Regex("</noparse>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>The characters the player will see: truncated, neutralised, no tags.</summary>
        public static string Visible(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var text = value;
            if (text.Length > MaxFieldLength)
            {
                var cut = MaxFieldLength;
                if (char.IsHighSurrogate(text[cut - 1]))
                {
                    cut--;
                }

                Debug.LogWarning($"ReadingTextSanitizer: truncated a {value.Length}-character field to {cut} characters.");
                text = text.Substring(0, cut) + TruncationMark;
            }

            // A literal "</noparse>" would close the wrapper early; a full-width bracket keeps it visible and inert.
            return text.IndexOf('<') < 0
                ? text
                : EmbeddedNoparseClose.Replace(text, match => "＜" + match.Value.Substring(1));
        }

        /// <summary>Wraps already-visible text so TMP shows it literally.</summary>
        public static string Wrap(string visible)
        {
            if (string.IsNullOrEmpty(visible))
            {
                return string.Empty;
            }

            return visible.IndexOf('<') < 0 ? visible : NoparseOpen + visible + NoparseClose;
        }

        public static string Plain(string value)
        {
            return Wrap(Visible(value));
        }
    }
}
```

- [ ] **Step 5: 创建 `Assets/Scripts/UI/CardAnalysisParser.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TarotUnity.Data;

namespace TarotUnity.UI
{
    public sealed class CardAnalysisParseResult
    {
        public static readonly CardAnalysisParseResult Failed =
            new CardAnalysisParseResult(false, Array.Empty<string>(), Array.Empty<string>());

        public CardAnalysisParseResult(bool success, string[] bodies, string[] leftovers)
        {
            Success = success;
            Bodies = bodies;
            Leftovers = leftovers;
        }

        public bool Success { get; }

        /// <summary>Bodies[i] belongs to draws[i]; a line that only named the card gives an empty body.</summary>
        public string[] Bodies { get; }

        /// <summary>Lines no card claimed, in their original order.</summary>
        public string[] Leftovers { get; }
    }

    /// <summary>
    /// Phase 67 (spec C 4.3): split card_analysis into one body per drawn card.
    /// 1) Each card takes the first unused line that starts, after an ordinal, with its position name.
    /// 2) If not every card matched but the line count equals the card count, match by order.
    /// 3) Otherwise fail; the caller then shows the whole text as before.
    /// </summary>
    public static class CardAnalysisParser
    {
        private static readonly Regex Ordinal = new Regex(
            @"^\s*(?:[\(（]\s*\d{1,2}\s*[\)）]|\d{1,2}\s*[\.．、:：\)）]|[①②③④⑤⑥⑦⑧⑨⑩]|[一二三四五六七八九十]{1,3}\s*[、\.．])\s*",
            RegexOptions.CultureInvariant);

        private static readonly Regex LeadingSeparators =
            new Regex(@"^[\s：:、,，·\-—–]+", RegexOptions.CultureInvariant);

        private static readonly Regex LeadingOrientation =
            new Regex(@"^[\(（]?\s*(?:正位|逆位)\s*[\)）]?", RegexOptions.CultureInvariant);

        public static CardAnalysisParseResult Parse(string cardAnalysis, CardDrawData[] draws)
        {
            if (string.IsNullOrWhiteSpace(cardAnalysis) || draws == null || draws.Length == 0)
            {
                return CardAnalysisParseResult.Failed;
            }

            var lines = SplitLines(cardAnalysis);
            if (lines.Count == 0)
            {
                return CardAnalysisParseResult.Failed;
            }

            var bodies = new string[draws.Length];
            var used = new bool[lines.Count];
            var matched = 0;
            for (var d = 0; d < draws.Length; d++)
            {
                var position = draws[d]?.position_name?.Trim();
                if (string.IsNullOrEmpty(position))
                {
                    continue;
                }

                for (var l = 0; l < lines.Count; l++)
                {
                    var line = StripOrdinal(lines[l]);
                    if (used[l] || !line.StartsWith(position, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    used[l] = true;
                    bodies[d] = StripHeading(line.Substring(position.Length), draws[d]);
                    matched++;
                    break;
                }
            }

            if (matched == draws.Length)
            {
                var leftovers = new List<string>();
                for (var l = 0; l < lines.Count; l++)
                {
                    if (!used[l])
                    {
                        leftovers.Add(lines[l]);
                    }
                }

                return new CardAnalysisParseResult(true, bodies, leftovers.ToArray());
            }

            if (lines.Count == draws.Length)
            {
                var ordered = new string[lines.Count];
                for (var l = 0; l < lines.Count; l++)
                {
                    ordered[l] = StripOrdinal(lines[l]);
                }

                return new CardAnalysisParseResult(true, ordered, Array.Empty<string>());
            }

            return CardAnalysisParseResult.Failed;
        }

        private static List<string> SplitLines(string text)
        {
            var lines = new List<string>();
            foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length > 0)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        private static string StripOrdinal(string line)
        {
            return Ordinal.Replace(line, string.Empty, 1).TrimStart();
        }

        private static string StripHeading(string rest, CardDrawData draw)
        {
            var text = LeadingSeparators.Replace(rest, string.Empty, 1);
            var name = draw?.tarot_card?.name_zh?.Trim();
            if (!string.IsNullOrEmpty(name) && text.StartsWith(name, StringComparison.Ordinal))
            {
                text = text.Substring(name.Length).TrimStart();
            }

            text = LeadingOrientation.Replace(text, string.Empty, 1);
            text = LeadingSeparators.Replace(text, string.Empty, 1);
            return text.Trim();
        }
    }
}
```

- [ ] **Step 6: 创建 `Assets/Scripts/UI/CardAnalysisFormatter.cs`**

```csharp
using System;
using System.Text;
using TarotUnity.Data;

namespace TarotUnity.UI
{
    /// <summary>Where one card's heading sits in the formatted card analysis.</summary>
    [Serializable]
    public struct CardBlockRange
    {
        public int CardIndex;
        public int HeadingStart;
        public int HeadingLength;

        public CardBlockRange(int cardIndex, int headingStart, int headingLength)
        {
            CardIndex = cardIndex;
            HeadingStart = headingStart;
            HeadingLength = headingLength;
        }
    }

    public sealed class FormattedCardAnalysis
    {
        public FormattedCardAnalysis(string richText, CardBlockRange[] ranges)
        {
            RichText = richText;
            Ranges = ranges;
        }

        public string RichText { get; }
        public CardBlockRange[] Ranges { get; }
    }

    /// <summary>
    /// Phase 67 (spec C 4.3): one block per card - a gold heading built from the client's own
    /// card data, then the body the parser found. HeadingStart counts every character TMP lays
    /// out (line feeds included, rich-text tags excluded), so it indexes
    /// TMP_TextInfo.characterInfo directly.
    /// </summary>
    public static class CardAnalysisFormatter
    {
        public const string HeadingColorHex = "#DBA13D";

        private const string HeadingOpen = "<color=" + HeadingColorHex + "><b><size=95%>";
        private const string HeadingClose = "</size></b></color>";

        public static string BuildHeading(CardDrawData draw)
        {
            var position = draw?.position_name?.Trim() ?? string.Empty;
            var name = draw?.tarot_card?.name_zh?.Trim() ?? string.Empty;
            var mark = draw != null && draw.is_reversed ? ReleaseUxCopy.CardReversedMark : ReleaseUxCopy.CardUprightMark;
            return position.Length == 0 ? name + mark : position + ReleaseUxCopy.CardHeadingSeparator + name + mark;
        }

        public static FormattedCardAnalysis Build(string cardAnalysis, CardDrawData[] draws)
        {
            var parsed = CardAnalysisParser.Parse(cardAnalysis, draws);
            if (!parsed.Success)
            {
                return new FormattedCardAnalysis(ReadingTextSanitizer.Plain(cardAnalysis), Array.Empty<CardBlockRange>());
            }

            var builder = new StringBuilder();
            var ranges = new CardBlockRange[draws.Length];
            var visibleCount = 0;
            for (var i = 0; i < draws.Length; i++)
            {
                if (i > 0)
                {
                    visibleCount += AppendVisible(builder, "\n\n");
                }

                var heading = ReadingTextSanitizer.Visible(BuildHeading(draws[i]));
                builder.Append(HeadingOpen);
                ranges[i] = new CardBlockRange(i, visibleCount, heading.Length);
                visibleCount += AppendVisible(builder, heading);
                builder.Append(HeadingClose);

                var body = ReadingTextSanitizer.Visible(parsed.Bodies[i]);
                if (body.Length > 0)
                {
                    visibleCount += AppendVisible(builder, "\n");
                    visibleCount += AppendVisible(builder, body);
                }
            }

            foreach (var line in parsed.Leftovers)
            {
                visibleCount += AppendVisible(builder, "\n\n");
                visibleCount += AppendVisible(builder, ReadingTextSanitizer.Visible(line));
            }

            return new FormattedCardAnalysis(builder.ToString(), ranges);
        }

        // Appends text the player sees (wrapped in noparse when needed) and returns how many
        // characters TMP will lay out for it.
        private static int AppendVisible(StringBuilder builder, string visible)
        {
            builder.Append(ReadingTextSanitizer.Wrap(visible));
            return visible.Length;
        }
    }
}
```

- [ ] **Step 7: 运行测试，确认通过**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c1-green -testFilter TarotUnity.Tests.EditMode.Phase67ReadingTextTests
bash "$S/run/ut.sh" EditMode c1-full
```

预期：过滤运行 `total=15 passed=15 failed=0`；全量 EditMode `total=385 failed=0`。

- [ ] **Step 8: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
git add Assets/Scripts/UI/ReadingTextSanitizer.cs Assets/Scripts/UI/ReadingTextSanitizer.cs.meta \
  Assets/Scripts/UI/CardAnalysisParser.cs Assets/Scripts/UI/CardAnalysisParser.cs.meta \
  Assets/Scripts/UI/CardAnalysisFormatter.cs Assets/Scripts/UI/CardAnalysisFormatter.cs.meta \
  Assets/Scripts/UI/ReleaseUxCopy.cs \
  Assets/Tests/EditMode/Phase67ReadingTextTests.cs Assets/Tests/EditMode/Phase67ReadingTextTests.cs.meta
git commit -F - <<'EOF'
feat(unity): show external reading text literally and split card analysis into per-card blocks

ReadingTextSanitizer caps a field at 4000 characters and wraps text that contains
'<' in noparse. CardAnalysisParser matches each card's line by position name (or by
order when the counts match) and CardAnalysisFormatter builds gold client-side
headings, recording where each heading lands in TMP's character info.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
git log -1 --format=%B | tail -3
```

### Task 2: 多牌布局计算（纯函数）

**Files:**
- Create: `Assets/Scripts/UI/ResultSpreadLayout.cs`
- Test: `Assets/Tests/EditMode/Phase67ResultLayoutTests.cs`

**Interfaces:**
- Consumes：`TarotUiSpacing.ReferenceWidth`（1280）、`TarotUiSpacing.ReferenceHeight`（720）。
- Produces：
  - `readonly struct SpreadCellPlacement`：`Vector2 Position`、`float Scale`。
  - `sealed class SpreadLayoutResult`：
    - 字段：`SpreadCellPlacement[] Cells`、`int Rows`、`float LabelFontSize`、`float LabelHeight`、`float BandBottom`、`Vector2 ReadingPosition`、`Vector2 ReadingSize`；
    - 只读属性：`float ReadingTop`、`float ReadingBottom`。
  - `static class ResultSpreadLayout`：
    - 方法：`SpreadLayoutResult Compute(int cardCount, float canvasHeight)`；
    - 常量：`RowWidth`、`BasePitch`、`ReadingWidth`、`BandTopFromCanvasTop`、`ReadingBottomFromCanvasBottom`、`ButtonTopFromCanvasBottom`、`BandReadingGap`、`RowGap`、`CellFrameTop`、`CellFrameHalfWidth`、`CellLabelCentre`、`TwoRowThreshold`、`OneRowScale`、`TwoRowScale`、`OneRowLabelSize`、`TwoRowLabelSize`、`OneRowLabelHeight`、`TwoRowLabelHeight`。

计算依据（全部取自场景实测）：
- 牌格：牌框 164×234，中心上移 18，所以牌框顶在本地 +135；标签中心在本地 −118。
- 上分隔线中心在 +238、高 14，下沿在 +231。
- 「回到牌桌」按钮中心在 −300、高 48，上沿在 −276。

- [ ] **Step 1: 写失败的测试**

创建 `Assets/Tests/EditMode/Phase67ResultLayoutTests.cs`：

```csharp
using NUnit.Framework;
using TarotUnity.UI;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 67 (spec C 4.1): the top card band is sized by card count - one row up to five,
    /// two rows from six - and the reading panel takes everything between the band and the
    /// button row, including any extra height a taller canvas brings.
    /// </summary>
    public sealed class Phase67ResultLayoutTests
    {
        private const float ReferenceHeight = 720f;
        private const float DividerBottom = 231f;   // Phase8_ResultGoldDividerTop: y 238, 14 tall
        private const float ButtonTop = -276f;      // BackToMenuButton: y -300, 48 tall

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        public void OneRowReadingIsAtLeast326AtSixteenByNine(int cardCount)
        {
            var layout = ResultSpreadLayout.Compute(cardCount, ReferenceHeight);

            Assert.That(layout.Rows, Is.EqualTo(1));
            Assert.That(layout.Cells.Length, Is.EqualTo(cardCount), "control: every card is placed");
            Assert.That(layout.ReadingSize.y, Is.GreaterThanOrEqualTo(326f));
            Assert.That(layout.ReadingSize.x, Is.EqualTo(1180f));
        }

        [Test]
        public void TenCardsUseTwoRowsOfFiveWithReadableLabels()
        {
            var layout = ResultSpreadLayout.Compute(10, ReferenceHeight);

            Assert.That(layout.Rows, Is.EqualTo(2));
            var firstRowY = layout.Cells[0].Position.y;
            for (var i = 0; i < 5; i++)
            {
                Assert.That(layout.Cells[i].Position.y, Is.EqualTo(firstRowY).Within(0.01f), $"card {i} is in the first row");
            }

            for (var i = 5; i < 10; i++)
            {
                Assert.That(layout.Cells[i].Position.y, Is.LessThan(firstRowY - 1f), $"card {i} is in the second row");
            }

            Assert.That(layout.LabelFontSize, Is.GreaterThanOrEqualTo(14f));
        }

        [Test]
        public void TenCardReadingIsAtLeast282AtSixteenByNine()
        {
            Assert.That(ResultSpreadLayout.Compute(10, ReferenceHeight).ReadingSize.y, Is.GreaterThanOrEqualTo(282f));
        }

        [TestCase(3, 800f)]
        [TestCase(3, 960f)]
        [TestCase(10, 800f)]
        [TestCase(10, 960f)]
        public void TallerCanvasesGiveEveryExtraUnitToTheReading(int cardCount, float canvasHeight)
        {
            var reference = ResultSpreadLayout.Compute(cardCount, ReferenceHeight);
            var taller = ResultSpreadLayout.Compute(cardCount, canvasHeight);

            Assert.That(taller.ReadingSize.y - reference.ReadingSize.y,
                Is.EqualTo(canvasHeight - ReferenceHeight).Within(0.01f));
            Assert.That(taller.Cells[0].Position.y - reference.Cells[0].Position.y,
                Is.EqualTo((canvasHeight - ReferenceHeight) * 0.5f).Within(0.01f), "the band stays pinned under the header");
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(10)]
        public void BandClearsTheReadingAndTheReadingClearsTheButtons(int cardCount)
        {
            var layout = ResultSpreadLayout.Compute(cardCount, ReferenceHeight);
            var firstFrameTop = layout.Cells[0].Position.y + ResultSpreadLayout.CellFrameTop * layout.Cells[0].Scale;

            Assert.That(firstFrameTop, Is.LessThanOrEqualTo(DividerBottom), "the first row stays under the header divider");
            Assert.That(layout.BandBottom - layout.ReadingTop, Is.GreaterThanOrEqualTo(ResultSpreadLayout.BandReadingGap - 0.01f));
            Assert.That(layout.ReadingBottom - ButtonTop, Is.GreaterThanOrEqualTo(10f - 0.01f));
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        public void CellsStayInsideTheReferenceWidth(int cardCount)
        {
            var layout = ResultSpreadLayout.Compute(cardCount, ReferenceHeight);
            foreach (var cell in layout.Cells)
            {
                var halfWidth = ResultSpreadLayout.CellFrameHalfWidth * cell.Scale;
                Assert.That(Mathf.Abs(cell.Position.x) + halfWidth, Is.LessThanOrEqualTo(640f));
            }
        }

        [Test]
        public void FiveCardRowIsCentredAndDistinct()
        {
            var cells = ResultSpreadLayout.Compute(5, ReferenceHeight).Cells;

            Assert.That(cells[2].Position.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(cells[0].Position.x, Is.EqualTo(-cells[4].Position.x).Within(0.01f));
            for (var i = 1; i < 5; i++)
            {
                Assert.That(cells[i].Position.x - cells[i - 1].Position.x,
                    Is.GreaterThan(2f * ResultSpreadLayout.CellFrameHalfWidth * cells[i].Scale), "cards must not overlap");
            }
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c2-red -testFilter TarotUnity.Tests.EditMode.Phase67ResultLayoutTests
```

预期：`NO RESULTS XML`，并出现 `error CS0103`，提示 `ResultSpreadLayout` 不存在。

- [ ] **Step 3: 创建 `Assets/Scripts/UI/ResultSpreadLayout.cs`**

```csharp
using UnityEngine;

namespace TarotUnity.UI
{
    public readonly struct SpreadCellPlacement
    {
        public SpreadCellPlacement(Vector2 position, float scale)
        {
            Position = position;
            Scale = scale;
        }

        public Vector2 Position { get; }
        public float Scale { get; }
    }

    public sealed class SpreadLayoutResult
    {
        public SpreadCellPlacement[] Cells;
        public int Rows;
        public float LabelFontSize;
        public float LabelHeight;
        public float BandBottom;
        public Vector2 ReadingPosition;
        public Vector2 ReadingSize;

        public float ReadingTop => ReadingPosition.y + ReadingSize.y * 0.5f;
        public float ReadingBottom => ReadingPosition.y - ReadingSize.y * 0.5f;
    }

    /// <summary>
    /// Phase 67 (spec C 4.1): geometry of the multi-card Result screen, in centre-anchored
    /// canvas units. The band hangs from the header (136 below the canvas top), rows are
    /// centred at a pitch of min(348, 1180 / cards in the row), and the reading panel fills the
    /// rest down to 10 above the button row - so every extra unit of canvas height goes to the
    /// reading. Cell constants come from the Phase 60 cell (frame 164x234 centred 18 above the
    /// cell centre, label centred at -118).
    /// </summary>
    public static class ResultSpreadLayout
    {
        public const float RowWidth = 1180f;
        public const float BasePitch = 348f;
        public const float ReadingWidth = 1180f;
        public const float BandTopFromCanvasTop = 136f;
        public const float ButtonTopFromCanvasBottom = 84f;
        public const float ReadingBottomFromCanvasBottom = 94f;
        public const float BandReadingGap = 12f;
        public const float RowGap = 6f;
        public const float CellFrameTop = 135f;
        public const float CellFrameHalfWidth = 82f;
        public const float CellLabelCentre = -118f;
        public const int TwoRowThreshold = 6;
        public const float OneRowScale = 0.52f;
        public const float TwoRowScale = 0.31f;
        public const float OneRowLabelSize = 18f;
        public const float TwoRowLabelSize = 14f;
        public const float OneRowLabelHeight = 28f;
        public const float TwoRowLabelHeight = 22f;

        public static SpreadLayoutResult Compute(int cardCount, float canvasHeight)
        {
            var count = Mathf.Max(0, cardCount);
            var height = canvasHeight >= 1f ? canvasHeight : TarotUiSpacing.ReferenceHeight;
            var twoRows = count >= TwoRowThreshold;
            var rows = count == 0 ? 0 : (twoRows ? 2 : 1);
            var scale = twoRows ? TwoRowScale : OneRowScale;
            var labelHeight = twoRows ? TwoRowLabelHeight : OneRowLabelHeight;
            var firstRowCount = twoRows ? (count + 1) / 2 : count;

            var cells = new SpreadCellPlacement[count];
            var rowTop = height * 0.5f - BandTopFromCanvasTop;
            var bandBottom = rowTop;
            var placed = 0;
            for (var row = 0; row < rows; row++)
            {
                var inRow = row == 0 ? firstRowCount : count - firstRowCount;
                var centreY = rowTop - CellFrameTop * scale;
                var labelBottom = centreY + CellLabelCentre * scale - labelHeight * 0.5f;
                var pitch = Mathf.Min(BasePitch, inRow > 0 ? RowWidth / inRow : BasePitch);
                for (var i = 0; i < inRow; i++)
                {
                    var x = (i - (inRow - 1) * 0.5f) * pitch;
                    cells[placed++] = new SpreadCellPlacement(new Vector2(x, centreY), scale);
                }

                bandBottom = labelBottom;
                rowTop = labelBottom - RowGap;
            }

            var readingTop = bandBottom - BandReadingGap;
            var readingBottom = -height * 0.5f + ReadingBottomFromCanvasBottom;
            return new SpreadLayoutResult
            {
                Cells = cells,
                Rows = rows,
                LabelFontSize = twoRows ? TwoRowLabelSize : OneRowLabelSize,
                LabelHeight = labelHeight,
                BandBottom = bandBottom,
                ReadingPosition = new Vector2(0f, (readingTop + readingBottom) * 0.5f),
                ReadingSize = new Vector2(ReadingWidth, Mathf.Max(0f, readingTop - readingBottom)),
            };
        }
    }
}
```

参考值（16:9）：
- 一行：牌中心 y ≈ 153.8，阅读框 332.4 高。
- 两行：两行中心分别 ≈ 182.2 和 86.7，阅读框 293.1 高。

- [ ] **Step 4: 运行测试，确认通过**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c2-green -testFilter TarotUnity.Tests.EditMode.Phase67ResultLayoutTests
bash "$S/run/ut.sh" EditMode c2-full
```

预期：过滤运行 `total=18 passed=18 failed=0`；全量 EditMode `total=403 failed=0`。

- [ ] **Step 5: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
git add Assets/Scripts/UI/ResultSpreadLayout.cs Assets/Scripts/UI/ResultSpreadLayout.cs.meta \
  Assets/Tests/EditMode/Phase67ResultLayoutTests.cs Assets/Tests/EditMode/Phase67ResultLayoutTests.cs.meta
git commit -F - <<'EOF'
feat(unity): compute the Result spread band and reading panel from card count and canvas height

One row up to five cards, two rows from six; the reading panel spans from the band to
the button row, so a taller canvas lengthens the reading instead of adding empty space.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
git log -1 --format=%B | tail -3
```

### Task 3: 后端提示词约定牌面分析格式

**Files:**
- Modify: `Server/app/services/tarot_service.py`（`TarotPromptTemplate._output_schema` 里的 `card_analysis` 一项）
- Test: `Server/tests/test_prompt_card_analysis_format.py`

**Interfaces:**
- Consumes：
  - `TarotPromptTemplate._output_schema()`（staticmethod）
  - `TarotPromptTemplate.create_interpretation_messages(*, question, question_type, spread_name, spread_description, cards, user_context=None)`（classmethod，返回 `[system, user]` 两条消息）
- Produces：AI 的输出格式说明要求 `card_analysis` 每张牌一行，客户端的 `CardAnalysisParser`（Task 1）按这个格式解析。只改这一行说明，接口、数据库和模拟解读都不变。

- [ ] **Step 1: 写失败的测试**

创建 `Server/tests/test_prompt_card_analysis_format.py`：

```python
from app.services.tarot_service import TarotPromptTemplate


def test_card_analysis_schema_asks_for_one_line_per_card():
    description = TarotPromptTemplate._output_schema()["card_analysis"]

    assert "one line per card" in description
    assert "（正位|逆位）" in description


def test_card_analysis_format_reaches_the_prompt():
    messages = TarotPromptTemplate.create_interpretation_messages(
        question="我接下来该专注什么？",
        question_type="general",
        spread_name="过去现在未来",
        spread_description="",
        cards=[],
    )

    assert messages[1]["role"] == "user"
    assert "one line per card" in messages[1]["content"]
```

- [ ] **Step 2: 运行测试，确认失败**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/pt.sh" tests/test_prompt_card_analysis_format.py 2>&1 | tail -n 3
```

预期：`2 failed`，两条都是 `assert "one line per card" in ...` 失败。

- [ ] **Step 3: 修改说明文字**

在 `Server/app/services/tarot_service.py` 中，把

```python
            "card_analysis": "string, optional, per-card analysis",
```

替换为

```python
            "card_analysis": "string, optional, one line per card in input order: '<position>：<card name_zh>（正位|逆位）— <analysis>'",
```

- [ ] **Step 4: 运行测试，确认通过**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/pt.sh" tests/test_prompt_card_analysis_format.py 2>&1 | tail -n 1
bash "$S/run/pt.sh" tests 2>&1 | tail -n 1
bash "$S/run/pt.sh" . 2>&1 | tail -n 1
```

预期：
- 第一条：`2 passed`
- 后两条：都是 `190 passed`（其中 `test_mock_interpretation_zh.py` 锁定的模拟格式不受影响）

- [ ] **Step 5: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git add Server/app/services/tarot_service.py Server/tests/test_prompt_card_analysis_format.py
git commit -F - <<'EOF'
feat(server): ask the AI for one card_analysis line per card

The Result screen splits the per-card analysis into blocks by position name, so the
prompt schema now asks for "<position>：<card>（正位|逆位）— <analysis>" per line.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
git log -1 --format=%B | tail -3
```

### Task 4: 运行时组件（宽高比适配、渐隐、阅读导航、牌格点击、揭示伴随组）

**Files:**
- Create: `Assets/Scripts/UI/ResultCanvasAspectFit.cs`
- Create: `Assets/Scripts/UI/ReadingFadeGradient.cs`
- Create: `Assets/Scripts/UI/ResultReadingNavigator.cs`
- Create: `Assets/Scripts/UI/ResultSpreadCellTarget.cs`
- Modify: `Assets/Scripts/Presentation/ResultRevealDirector.cs`（整文件替换）
- Test: `Assets/Tests/EditMode/Phase67ResultReadingComponentsTests.cs`

**Interfaces:**
- Consumes：Task 1 的 `CardBlockRange`；`TarotUiSpacing.ReferenceWidth/ReferenceHeight`。
- Produces：
  - `ResultCanvasAspectFit`：
    - 类型：`enum Edge { Top, Bottom }`；`[Serializable] class PinnedElement { RectTransform target; Edge edge; float offsetFromEdge; }`。
    - 序列化字段：`scaler`（CanvasScaler）、`canvasRect`（RectTransform）、`pinned`（`PinnedElement[]`）。
    - 方法：`static float MatchFor(int width, int height)`、`static float PinnedY(float canvasHeight, Edge edge, float offsetFromEdge)`、`void Apply(int screenWidth, int screenHeight)`、`void Repin(float canvasHeight)`。
  - `ReadingFadeGradient : BaseMeshEffect`：
    - 序列化字段：`topAlpha = 0`、`bottomAlpha = 1`。
    - 方法：`override void ModifyMesh(VertexHelper)`。
  - `ResultReadingNavigator`：
    - 序列化字段：`scroll`、`bottomFade`（Graphic）、`cardAnalysisText`（TMP_Text）、`cardSectionHeading`（RectTransform）、`focusSeconds = 0.35`、`focusViewportFraction = 0.12`、`highlightSeconds = 1.2`、`highlightColor`。
    - 属性：`bool IsInteractive`、`bool IsFadeVisible`、`int HighlightedCard`（没有高亮时为 −1）、`IReadOnlyList<CardBlockRange> Blocks`。
    - 方法：`void SetBlocks(IReadOnlyList<CardBlockRange>)`、`void SetInteractive(bool)`、`void FocusCard(int cardIndex)`、`static float NormalizedPositionFor(float targetOffsetFromTop, float contentHeight, float viewportHeight, float viewportFraction)`、`static bool ShouldShowFade(bool interactive, float contentHeight, float viewportHeight, float normalizedPosition)`。
  - `ResultSpreadCellTarget : IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler`：
    - 序列化字段：`cardIndex`、`navigator`、`glow`（Image）、`hoverScale = 1.04`、`hoverGlowAlpha = 0.42`。
    - 属性和方法：`int CardIndex`、`bool IsHovered`、`void SetBaseScale(float)`、`void Activate()`。
  - `ResultRevealDirector`：新增 `[Serializable] struct RevealCompanion { int groupIndex; CanvasGroup group; }` 和序列化字段 `companions`。伴随组和 `revealGroups[groupIndex]` 同时淡入，并参与"一次性全部显示"。

- [ ] **Step 1: 写失败的测试**

创建 `Assets/Tests/EditMode/Phase67ResultReadingComponentsTests.cs`：

```csharp
using NUnit.Framework;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 67: the small runtime pieces of the Result reading - aspect-aware scaling with
    /// edge-pinned header and buttons, the bottom fade, scroll targeting, card hover, and
    /// reveal companions.
    /// </summary>
    public sealed class Phase67ResultReadingComponentsTests
    {
        private GameObject root;

        [TearDown]
        public void DestroyRoot()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(1920, 1080, 1f)]
        [TestCase(2560, 1080, 1f)]
        [TestCase(1440, 900, 0f)]
        [TestCase(1024, 768, 0f)]
        public void AspectFitMatchesHeightOnWideScreensAndWidthOnNarrowOnes(int width, int height, float expected)
        {
            Assert.That(ResultCanvasAspectFit.MatchFor(width, height), Is.EqualTo(expected));
        }

        [Test]
        public void PinnedElementsKeepTheirReferenceOffsetsFromTheEdges()
        {
            Assert.That(ResultCanvasAspectFit.PinnedY(720f, ResultCanvasAspectFit.Edge.Top, 60f), Is.EqualTo(300f),
                "QuestionText keeps its saved position at 16:9");
            Assert.That(ResultCanvasAspectFit.PinnedY(720f, ResultCanvasAspectFit.Edge.Bottom, 60f), Is.EqualTo(-300f),
                "BackToMenuButton keeps its saved position at 16:9");
            Assert.That(ResultCanvasAspectFit.PinnedY(960f, ResultCanvasAspectFit.Edge.Top, 60f), Is.EqualTo(420f));
            Assert.That(ResultCanvasAspectFit.PinnedY(960f, ResultCanvasAspectFit.Edge.Bottom, 60f), Is.EqualTo(-420f));
            Assert.That(ResultCanvasAspectFit.PinnedY(0f, ResultCanvasAspectFit.Edge.Top, 60f), Is.EqualTo(300f),
                "an unsized canvas falls back to the reference height");
        }

        [Test]
        public void FadeGradientRunsFromTransparentTopToOpaqueBottom()
        {
            root = new GameObject("Phase67_FadeTest", typeof(RectTransform));
            root.AddComponent<Image>();
            var gradient = root.AddComponent<ReadingFadeGradient>();

            var mesh = new VertexHelper();
            try
            {
                AddVertex(mesh, 0f, 0f);
                AddVertex(mesh, 0f, 36f);
                AddVertex(mesh, 100f, 36f);
                AddVertex(mesh, 100f, 0f);

                gradient.ModifyMesh(mesh);

                var vertex = new UIVertex();
                mesh.PopulateUIVertex(ref vertex, 0);
                Assert.That(vertex.color.a, Is.EqualTo(255), "the bottom edge stays opaque");
                mesh.PopulateUIVertex(ref vertex, 1);
                Assert.That(vertex.color.a, Is.EqualTo(0), "the top edge is transparent");
                mesh.PopulateUIVertex(ref vertex, 2);
                Assert.That(vertex.color.r, Is.EqualTo(200), "control: the colour itself is untouched");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void NavigatorScrollTargetKeepsTheHeadingNearTheTopAndClamps()
        {
            // Content 1000 tall, viewport 250 -> 750 scrollable; a heading 400 down aims 30 (12%) above it.
            Assert.That(ResultReadingNavigator.NormalizedPositionFor(400f, 1000f, 250f, 0.12f),
                Is.EqualTo(1f - (400f - 30f) / 750f).Within(0.0001f));
            Assert.That(ResultReadingNavigator.NormalizedPositionFor(10f, 1000f, 250f, 0.12f), Is.EqualTo(1f),
                "a heading near the top clamps to the top");
            Assert.That(ResultReadingNavigator.NormalizedPositionFor(990f, 1000f, 250f, 0.12f), Is.EqualTo(0f),
                "a heading near the end clamps to the bottom");
            Assert.That(ResultReadingNavigator.NormalizedPositionFor(400f, 200f, 250f, 0.12f), Is.EqualTo(1f),
                "text that fits does not scroll");
        }

        [Test]
        public void FadeShowsOnlyWhileMoreTextIsBelow()
        {
            Assert.That(ResultReadingNavigator.ShouldShowFade(true, 1000f, 250f, 1f), Is.True, "at the top of long text");
            Assert.That(ResultReadingNavigator.ShouldShowFade(true, 1000f, 250f, 0f), Is.False, "at the bottom");
            Assert.That(ResultReadingNavigator.ShouldShowFade(true, 1000f, 250f, 7f / 750f), Is.False, "within 8 of the bottom");
            Assert.That(ResultReadingNavigator.ShouldShowFade(true, 200f, 250f, 1f), Is.False, "text that fits");
            Assert.That(ResultReadingNavigator.ShouldShowFade(false, 1000f, 250f, 1f), Is.False, "pending or failed readings");
        }

        [Test]
        public void HoverLiftsTheCellOnlyWhileTheReadingIsInteractive()
        {
            root = new GameObject("Phase67_CellTest", typeof(RectTransform));
            var navigator = root.AddComponent<ResultReadingNavigator>();
            var cell = new GameObject("Cell", typeof(RectTransform));
            cell.transform.SetParent(root.transform, false);
            var target = cell.AddComponent<ResultSpreadCellTarget>();
            var so = new SerializedObject(target);
            so.FindProperty("navigator").objectReferenceValue = navigator;
            so.ApplyModifiedPropertiesWithoutUndo();
            target.SetBaseScale(0.52f);

            target.OnPointerEnter(null);
            Assert.That(cell.transform.localScale.x, Is.EqualTo(0.52f).Within(0.0001f), "no lift while the reading is hidden");

            navigator.SetInteractive(true);
            target.OnPointerEnter(null);
            Assert.That(target.IsHovered, Is.True);
            Assert.That(cell.transform.localScale.x, Is.EqualTo(0.52f * 1.04f).Within(0.0001f));

            target.OnPointerExit(null);
            Assert.That(cell.transform.localScale.x, Is.EqualTo(0.52f).Within(0.0001f));
        }

        [Test]
        public void RevealDirectorShowsCompanionsWithTheirGroup()
        {
            root = new GameObject("Phase67_RevealTest");
            root.SetActive(false);
            var director = root.AddComponent<ResultRevealDirector>();
            var primary = new GameObject("Primary").AddComponent<CanvasGroup>();
            var companion = new GameObject("Companion").AddComponent<CanvasGroup>();
            primary.transform.SetParent(root.transform);
            companion.transform.SetParent(root.transform);
            primary.alpha = 0f;
            companion.alpha = 0f;

            var so = new SerializedObject(director);
            var groups = so.FindProperty("revealGroups");
            groups.arraySize = 1;
            groups.GetArrayElementAtIndex(0).objectReferenceValue = primary;
            var companions = so.FindProperty("companions");
            Assert.That(companions, Is.Not.Null, "ResultRevealDirector needs a serialized companions array");
            companions.arraySize = 1;
            companions.GetArrayElementAtIndex(0).FindPropertyRelative("groupIndex").intValue = 0;
            companions.GetArrayElementAtIndex(0).FindPropertyRelative("group").objectReferenceValue = companion;
            so.ApplyModifiedPropertiesWithoutUndo();

            director.PlayReveal(); // an inactive director shows everything at once

            Assert.That(primary.alpha, Is.EqualTo(1f), "control: the primary group is shown");
            Assert.That(companion.alpha, Is.EqualTo(1f), "a companion is shown with its group");
            Assert.That(companion.blocksRaycasts, Is.True);
        }

        private static void AddVertex(VertexHelper mesh, float x, float y)
        {
            mesh.AddVert(new UIVertex { position = new Vector3(x, y, 0f), color = new Color32(200, 100, 50, 255) });
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c4-red -testFilter TarotUnity.Tests.EditMode.Phase67ResultReadingComponentsTests
```

预期：`NO RESULTS XML`，`error CS0246`/`CS0103` 指出 `ResultCanvasAspectFit`、`ReadingFadeGradient`、`ResultReadingNavigator`、`ResultSpreadCellTarget` 不存在。

- [ ] **Step 3: 创建 `Assets/Scripts/UI/ResultCanvasAspectFit.cs`**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 67 (spec C 4.7): the Result canvas keeps the whole 1280-wide design on narrow
    /// screens (match width) and the whole 720-tall design on wide ones (match height). The
    /// header and the button row stay centre-anchored in the scene (EditMode guards measure
    /// them against an unsized overlay canvas) and are pinned to the canvas top and bottom at
    /// runtime instead, so a taller canvas opens space in the middle for the reading.
    /// </summary>
    public sealed class ResultCanvasAspectFit : MonoBehaviour
    {
        public enum Edge
        {
            Top,
            Bottom,
        }

        [Serializable]
        public sealed class PinnedElement
        {
            public RectTransform target;
            public Edge edge;
            public float offsetFromEdge;
        }

        [SerializeField] private CanvasScaler scaler;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private PinnedElement[] pinned = Array.Empty<PinnedElement>();

        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private float lastCanvasHeight = -1f;

        public static float MatchFor(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return 1f;
            }

            var referenceAspect = TarotUiSpacing.ReferenceWidth / TarotUiSpacing.ReferenceHeight;
            return (float)width / height >= referenceAspect - 0.0001f ? 1f : 0f;
        }

        public static float PinnedY(float canvasHeight, Edge edge, float offsetFromEdge)
        {
            var half = (canvasHeight >= 1f ? canvasHeight : TarotUiSpacing.ReferenceHeight) * 0.5f;
            return edge == Edge.Top ? half - offsetFromEdge : -half + offsetFromEdge;
        }

        private void Awake()
        {
            Apply(Screen.width, Screen.height);
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                Apply(Screen.width, Screen.height);
                return;
            }

            // The scaler resizes the canvas a frame after the match factor changes.
            var canvasHeight = canvasRect != null ? canvasRect.rect.height : 0f;
            if (!Mathf.Approximately(canvasHeight, lastCanvasHeight))
            {
                Repin(canvasHeight);
            }
        }

        public void Apply(int screenWidth, int screenHeight)
        {
            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;
            if (scaler != null)
            {
                scaler.matchWidthOrHeight = MatchFor(screenWidth, screenHeight);
            }

            Repin(canvasRect != null ? canvasRect.rect.height : 0f);
        }

        public void Repin(float canvasHeight)
        {
            lastCanvasHeight = canvasHeight;
            foreach (var element in pinned)
            {
                if (element == null || element.target == null)
                {
                    continue;
                }

                var position = element.target.anchoredPosition;
                element.target.anchoredPosition =
                    new Vector2(position.x, PinnedY(canvasHeight, element.edge, element.offsetFromEdge));
            }
        }
    }
}
```

- [ ] **Step 4: 创建 `Assets/Scripts/UI/ReadingFadeGradient.cs`**

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 67 (spec C 4.2): the bottom fade of the reading panel - a plain quad whose vertex
    /// alpha runs from topAlpha at its top edge to bottomAlpha at its bottom edge, so no
    /// gradient texture is needed.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class ReadingFadeGradient : BaseMeshEffect
    {
        [SerializeField, Range(0f, 1f)] private float topAlpha = 0f;
        [SerializeField, Range(0f, 1f)] private float bottomAlpha = 1f;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh == null || vh.currentVertCount == 0)
            {
                return;
            }

            var vertex = new UIVertex();
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            for (var i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                minY = Mathf.Min(minY, vertex.position.y);
                maxY = Mathf.Max(maxY, vertex.position.y);
            }

            var height = Mathf.Max(0.0001f, maxY - minY);
            for (var i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                var t = (vertex.position.y - minY) / height;
                var color = vertex.color;
                color.a = (byte)Mathf.RoundToInt(color.a * Mathf.Lerp(bottomAlpha, topAlpha, t));
                vertex.color = color;
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
```

- [ ] **Step 5: 创建 `Assets/Scripts/UI/ResultReadingNavigator.cs`**

```csharp
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 67 (spec C 4.2, 4.4): the reading panel's affordances. The bottom fade shows while
    /// more text is below. Clicking a card scrolls its analysis block so the heading sits 12%
    /// down the viewport, then glows the heading back to its gold over 1.2 s. When the per-card
    /// blocks could not be parsed, a click scrolls to the 牌面分析 heading instead. Nothing
    /// happens while the reading is pending or failed.
    /// </summary>
    public sealed class ResultReadingNavigator : MonoBehaviour
    {
        public const float FadeHideDistance = 8f;

        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Graphic bottomFade;
        [SerializeField] private TMP_Text cardAnalysisText;
        [SerializeField] private RectTransform cardSectionHeading;
        [SerializeField] private float focusSeconds = 0.35f;
        [SerializeField] private float focusViewportFraction = 0.12f;
        [SerializeField] private float highlightSeconds = 1.2f;
        [SerializeField] private Color highlightColor = new Color(1f, 0.86f, 0.55f, 1f);

        private readonly List<CardBlockRange> blocks = new List<CardBlockRange>();
        private Coroutine scrollRoutine;
        private Coroutine highlightRoutine;

        public bool IsInteractive { get; private set; }
        public bool IsFadeVisible { get; private set; }
        public int HighlightedCard { get; private set; } = -1;
        public IReadOnlyList<CardBlockRange> Blocks => blocks;

        public static float NormalizedPositionFor(
            float targetOffsetFromTop, float contentHeight, float viewportHeight, float viewportFraction)
        {
            var scrollable = contentHeight - viewportHeight;
            if (scrollable <= 0.5f)
            {
                return 1f;
            }

            var desired = targetOffsetFromTop - viewportFraction * viewportHeight;
            return 1f - Mathf.Clamp(desired, 0f, scrollable) / scrollable;
        }

        public static bool ShouldShowFade(bool interactive, float contentHeight, float viewportHeight, float normalizedPosition)
        {
            if (!interactive)
            {
                return false;
            }

            var scrollable = contentHeight - viewportHeight;
            return scrollable > 1f && normalizedPosition * scrollable > FadeHideDistance;
        }

        public void SetBlocks(IReadOnlyList<CardBlockRange> ranges)
        {
            StopHighlight();
            blocks.Clear();
            if (ranges == null)
            {
                return;
            }

            for (var i = 0; i < ranges.Count; i++)
            {
                blocks.Add(ranges[i]);
            }
        }

        public void SetInteractive(bool interactive)
        {
            IsInteractive = interactive;
            if (!interactive)
            {
                StopScroll();
                StopHighlight();
            }
        }

        public void FocusCard(int cardIndex)
        {
            if (!IsInteractive || scroll == null || scroll.content == null || scroll.viewport == null)
            {
                return;
            }

            StopHighlight();
            Canvas.ForceUpdateCanvases();
            var block = FindBlock(cardIndex);
            var offset = block.HasValue ? HeadingLineOffset(block.Value) : SectionHeadingOffset();
            if (!offset.HasValue)
            {
                return;
            }

            var target = NormalizedPositionFor(
                offset.Value, scroll.content.rect.height, scroll.viewport.rect.height, focusViewportFraction);
            StopScroll();
            scrollRoutine = StartCoroutine(ScrollTo(target));
            if (block.HasValue)
            {
                highlightRoutine = StartCoroutine(Highlight(block.Value));
            }
        }

        private void LateUpdate()
        {
            var visible = scroll != null && scroll.content != null && scroll.viewport != null
                && ShouldShowFade(IsInteractive, scroll.content.rect.height, scroll.viewport.rect.height,
                    scroll.verticalNormalizedPosition);
            IsFadeVisible = visible;
            if (bottomFade == null)
            {
                return;
            }

            var current = bottomFade.canvasRenderer.GetAlpha();
            var next = Mathf.MoveTowards(current, visible ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            if (!Mathf.Approximately(current, next))
            {
                bottomFade.canvasRenderer.SetAlpha(next);
            }
        }

        private void OnDisable()
        {
            StopScroll();
            StopHighlight();
        }

        private CardBlockRange? FindBlock(int cardIndex)
        {
            foreach (var block in blocks)
            {
                if (block.CardIndex == cardIndex)
                {
                    return block;
                }
            }

            return null;
        }

        private float? HeadingLineOffset(CardBlockRange block)
        {
            if (cardAnalysisText == null)
            {
                return null;
            }

            cardAnalysisText.ForceMeshUpdate();
            var info = cardAnalysisText.textInfo;
            if (block.HeadingStart < 0 || block.HeadingStart >= info.characterCount)
            {
                return null;
            }

            var line = info.characterInfo[block.HeadingStart].lineNumber;
            if (line < 0 || line >= info.lineCount)
            {
                return null;
            }

            var world = cardAnalysisText.rectTransform.TransformPoint(new Vector3(0f, info.lineInfo[line].ascender, 0f));
            return OffsetFromContentTop(world);
        }

        private float? SectionHeadingOffset()
        {
            if (cardSectionHeading == null)
            {
                return null;
            }

            var world = cardSectionHeading.TransformPoint(new Vector3(0f, cardSectionHeading.rect.yMax, 0f));
            return OffsetFromContentTop(world);
        }

        private float OffsetFromContentTop(Vector3 worldPoint)
        {
            var content = scroll.content;
            return content.rect.yMax - content.InverseTransformPoint(worldPoint).y;
        }

        private IEnumerator ScrollTo(float normalizedTarget)
        {
            scroll.StopMovement();
            var start = scroll.verticalNormalizedPosition;
            var duration = Mathf.Max(0.01f, focusSeconds);
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - (1f - t) * (1f - t) * (1f - t);
                scroll.verticalNormalizedPosition = Mathf.Lerp(start, normalizedTarget, eased);
                yield return null;
            }

            scroll.verticalNormalizedPosition = normalizedTarget;
            scrollRoutine = null;
        }

        private IEnumerator Highlight(CardBlockRange block)
        {
            HighlightedCard = block.CardIndex;
            var text = cardAnalysisText;
            if (text == null)
            {
                HighlightedCard = -1;
                yield break;
            }

            text.ForceMeshUpdate();
            var info = text.textInfo;
            var characterCount = info.characterCount;
            var end = Mathf.Min(block.HeadingStart + block.HeadingLength, characterCount);
            var vertices = new List<(int material, int index, Color32 original)>();
            for (var i = Mathf.Max(0, block.HeadingStart); i < end; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                var colors = info.meshInfo[character.materialReferenceIndex].colors32;
                for (var v = 0; v < 4 && character.vertexIndex + v < colors.Length; v++)
                {
                    vertices.Add((character.materialReferenceIndex, character.vertexIndex + v, colors[character.vertexIndex + v]));
                }
            }

            Color32 glow = highlightColor;
            var duration = Mathf.Max(0.01f, highlightSeconds);
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                if (info.characterCount != characterCount)
                {
                    break; // the text changed under us
                }

                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                foreach (var vertex in vertices)
                {
                    var colors = info.meshInfo[vertex.material].colors32;
                    if (vertex.index < colors.Length)
                    {
                        colors[vertex.index] = Color32.Lerp(glow, vertex.original, t);
                    }
                }

                text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                yield return null;
            }

            text.ForceMeshUpdate();
            HighlightedCard = -1;
            highlightRoutine = null;
        }

        private void StopScroll()
        {
            if (scrollRoutine == null)
            {
                return;
            }

            StopCoroutine(scrollRoutine);
            scrollRoutine = null;
        }

        private void StopHighlight()
        {
            if (highlightRoutine == null)
            {
                return;
            }

            StopCoroutine(highlightRoutine);
            highlightRoutine = null;
            HighlightedCard = -1;
            if (cardAnalysisText != null)
            {
                cardAnalysisText.ForceMeshUpdate();
            }
        }
    }
}
```

- [ ] **Step 6: 创建 `Assets/Scripts/UI/ResultSpreadCellTarget.cs`**

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 67 (spec C 4.4): one spread cell on the Result screen. While the reading is shown,
    /// hovering lifts the card 4% and brightens its glow, and clicking scrolls the reading to the
    /// card's block. It sits on the cell root: clicks bubble up from the artwork (whose
    /// holographic driver only handles enter/exit/move) and from the label.
    /// </summary>
    public sealed class ResultSpreadCellTarget : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private int cardIndex;
        [SerializeField] private ResultReadingNavigator navigator;
        [SerializeField] private Image glow;
        [SerializeField] private float hoverScale = 1.04f;
        [SerializeField] private float hoverGlowAlpha = 0.42f;

        private float baseScale = 1f;
        private float baseGlowAlpha = -1f;

        public int CardIndex => cardIndex;
        public bool IsHovered { get; private set; }

        /// <summary>The layout scale for this card; the hover lift multiplies it.</summary>
        public void SetBaseScale(float scale)
        {
            baseScale = scale;
            ApplyVisual();
        }

        public void Activate()
        {
            if (navigator != null)
            {
                navigator.FocusCard(cardIndex);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Activate();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsHovered = navigator != null && navigator.IsInteractive;
            ApplyVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
            ApplyVisual();
        }

        private void OnDisable()
        {
            IsHovered = false;
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            transform.localScale = Vector3.one * (baseScale * (IsHovered ? hoverScale : 1f));
            if (glow == null)
            {
                return;
            }

            if (baseGlowAlpha < 0f)
            {
                baseGlowAlpha = glow.color.a;
            }

            var color = glow.color;
            color.a = IsHovered ? hoverGlowAlpha : baseGlowAlpha;
            glow.color = color;
        }
    }
}
```

- [ ] **Step 7: 整文件替换 `Assets/Scripts/Presentation/ResultRevealDirector.cs`**

```csharp
using System;
using System.Collections;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class ResultRevealDirector : MonoBehaviour
    {
        // Phase 67: an element that fades in together with revealGroups[groupIndex], so the
        // section headings, the mode label and the spread band join the reveal without making
        // it longer.
        [Serializable]
        public struct RevealCompanion
        {
            public int groupIndex;
            public CanvasGroup group;
        }

        [SerializeField] private CanvasGroup[] revealGroups;
        [SerializeField] private RevealCompanion[] companions = Array.Empty<RevealCompanion>();
        [SerializeField] private float firstDelay = 0.18f;
        [SerializeField] private float groupInterval = 0.16f;
        [SerializeField] private float fadeDuration = 0.28f;

        private Coroutine activeReveal;

        public bool IsRevealComplete { get; private set; }

        private void Awake()
        {
            SetAllVisibleInstant(false);
        }

        public void PlayReveal()
        {
            if (!gameObject.activeInHierarchy)
            {
                SetAllVisibleInstant(true);
                return;
            }

            if (activeReveal != null)
            {
                StopCoroutine(activeReveal);
            }

            activeReveal = StartCoroutine(RevealRoutine());
        }

        public IEnumerator RevealRoutine()
        {
            IsRevealComplete = false;
            SetAllVisibleInstant(false);

            if (firstDelay > 0f)
            {
                yield return new WaitForSeconds(firstDelay);
            }

            if (revealGroups != null)
            {
                for (var i = 0; i < revealGroups.Length; i++)
                {
                    if (revealGroups[i] == null)
                    {
                        continue;
                    }

                    yield return FadeGroup(i, 0f, 1f);

                    if (groupInterval > 0f)
                    {
                        yield return new WaitForSeconds(groupInterval);
                    }
                }
            }

            IsRevealComplete = true;
            activeReveal = null;
        }

        private IEnumerator FadeGroup(int index, float from, float to)
        {
            var duration = Mathf.Max(0.01f, fadeDuration);
            SetGroupState(index, from, false);

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                SetGroupAlpha(index, Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }

            SetGroupState(index, to, true);
        }

        private void SetGroupAlpha(int index, float alpha)
        {
            revealGroups[index].alpha = alpha;
            if (companions == null)
            {
                return;
            }

            foreach (var companion in companions)
            {
                if (companion.groupIndex == index && companion.group != null)
                {
                    companion.group.alpha = alpha;
                }
            }
        }

        private void SetGroupState(int index, float alpha, bool interactive)
        {
            Apply(revealGroups[index], alpha, interactive);
            if (companions == null)
            {
                return;
            }

            foreach (var companion in companions)
            {
                if (companion.groupIndex == index)
                {
                    Apply(companion.group, alpha, interactive);
                }
            }
        }

        private void SetAllVisibleInstant(bool visible)
        {
            IsRevealComplete = visible;
            var alpha = visible ? 1f : 0f;
            if (revealGroups != null)
            {
                foreach (var group in revealGroups)
                {
                    Apply(group, alpha, visible);
                }
            }

            if (companions != null)
            {
                foreach (var companion in companions)
                {
                    Apply(companion.group, alpha, visible);
                }
            }
        }

        private static void Apply(CanvasGroup group, float alpha, bool interactive)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }
    }
}
```

- [ ] **Step 8: 运行测试，确认通过**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c4-green -testFilter TarotUnity.Tests.EditMode.Phase67ResultReadingComponentsTests
bash "$S/run/ut.sh" EditMode c4-full
bash "$S/run/ut.sh" PlayMode c4-full
```

预期：
- 过滤运行 `total=10 passed=10 failed=0`
- 全量 EditMode `total=413 failed=0`
- 全量 PlayMode `total=68 passed=65 failed=0 skipped=3`（揭示分组改写后，原有流程不受影响）

- [ ] **Step 9: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
git add Assets/Scripts/UI/ResultCanvasAspectFit.cs Assets/Scripts/UI/ResultCanvasAspectFit.cs.meta \
  Assets/Scripts/UI/ReadingFadeGradient.cs Assets/Scripts/UI/ReadingFadeGradient.cs.meta \
  Assets/Scripts/UI/ResultReadingNavigator.cs Assets/Scripts/UI/ResultReadingNavigator.cs.meta \
  Assets/Scripts/UI/ResultSpreadCellTarget.cs Assets/Scripts/UI/ResultSpreadCellTarget.cs.meta \
  Assets/Scripts/Presentation/ResultRevealDirector.cs \
  Assets/Tests/EditMode/Phase67ResultReadingComponentsTests.cs Assets/Tests/EditMode/Phase67ResultReadingComponentsTests.cs.meta
git commit -F - <<'EOF'
feat(unity): add Result reading components for aspect fit, fade, card focus and reveal companions

ResultCanvasAspectFit switches the scaler between width and height matching and pins
the header and button row to the canvas edges. ResultReadingNavigator shows the bottom
fade while text remains and scrolls a clicked card's block into view with a heading
glow. ResultSpreadCellTarget adds hover and click to each spread cell, and
ResultRevealDirector fades companion groups with their primary group.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
git log -1 --format=%B | tail -3
```

### Task 5: 场景结构（Phase 67 Bootstrapper）

**Files:**
- Create: `Assets/Editor/Phase67ResultReadingBootstrapper.cs`
- Modify: `Assets/Scripts/UI/ResultPanelPresenter.cs`（只新增 5 个序列化字段）
- Modify: `Assets/Tests/EditMode/Phase29ResultScrollTests.cs`（`AllSectionsLiveInContentInReadingOrder`）
- Modify: `Assets/Scenes/Result.unity`（由 Bootstrapper 修改）
- Test: `Assets/Tests/EditMode/Phase67ResultSceneStructureTests.cs`

**Interfaces:**
- Consumes：Task 4 的全部组件；场景中现有的对象：`ResultCanvas`（上面挂有 `ResultPanelPresenter`、`ResultRevealDirector`、`CanvasScaler`）、`ResultReadingScroll/Viewport/Content`、`MP_ResultSpreadBand/SpreadCell_0..9/{Glow,Label}`、`WarningText`、`Phase8_ResultGoldDividerBottom`、`Phase66_ModeLabel`。
- Produces（场景对象名，Task 6 起依赖）：
  - `Content` 的子对象依次为：`Phase67_OfflineNotice`、Phase 29 的 8 个分段、`Phase67_ResultSectionWarning`、`WarningText`。
  - `ResultReadingScroll` 下新增 `Phase67_ReadingBottomFade`（挂 `ReadingFadeGradient`）和 `Phase67_ReadingScrollbar`（挂 `Scrollbar`，含子对象 `Handle`）。
  - `ResultReadingScroll` 上新增 `ResultReadingNavigator`；每个 `SpreadCell_i` 上新增 `ResultSpreadCellTarget`；`ResultCanvas` 上新增 `ResultCanvasAspectFit`。
  - 揭示伴随组：
    - `SpreadNameText` → `Phase66_ModeLabel`、`MP_ResultSpreadBand`
    - `SummaryText` → `Phase67_OfflineNotice`、`Phase7_ResultSectionSummary`
    - `OverallText` → `Phase7_ResultSectionOverall`
    - `CardAnalysisText` → `Phase7_ResultSectionCards`
    - `AdviceText` → `Phase7_ResultSectionAdvice`
    - `WarningText` → `Phase67_ResultSectionWarning`
  - `ResultPanelPresenter` 新字段：`readingNavigator`、`offlineNoticeText`、`warningHeading`、`bottomDivider`、`cellTargets`；`singleReadingPos = (160, 4)`、`singleReadingSize = (772, 448)`。

- [ ] **Step 1: 写失败的结构测试**

创建 `Assets/Tests/EditMode/Phase67ResultSceneStructureTests.cs`：

```csharp
using System.Linq;
using NUnit.Framework;
using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 67: the Result scene built by Phase67ResultReadingBootstrapper - a viewport that
    /// clears the gold frame, the offline notice and warning section around the Phase 29
    /// sections, scroll affordances, clickable cells, edge-pinned header and buttons, reveal
    /// companions, and a single-card panel wide enough for the Phase 8 reading column.
    /// </summary>
    public sealed class Phase67ResultSceneStructureTests
    {
        private const string ScenePath = "Assets/Scenes/Result.unity";
        private const float FrameInnerGoldLine = 18f; // TarotPanel border rendered at pixelsPerUnitMultiplier 2

        private Transform canvas;

        private RectTransform Scroll => canvas.Find("ResultReadingScroll") as RectTransform;
        private RectTransform Viewport => Scroll.Find("Viewport") as RectTransform;
        private RectTransform Content => Viewport.Find("Content") as RectTransform;

        [SetUp]
        public void OpenScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            canvas = GameObject.Find("ResultCanvas")?.transform;
            Assert.That(canvas, Is.Not.Null, "control: ResultCanvas exists");
        }

        [Test]
        public void ViewportClearsTheFrameBorder()
        {
            var frame = Scroll.GetComponent<Image>();
            Assert.That(frame.sprite?.name, Is.EqualTo("TarotPanel"), "control: the frame art the inset was measured on");
            Assert.That(frame.pixelsPerUnitMultiplier, Is.EqualTo(2f), "control: the border renders at half size");
            Assert.That(Viewport.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(Viewport.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(Viewport.offsetMin.x, Is.GreaterThanOrEqualTo(FrameInnerGoldLine + 6f));
            Assert.That(Viewport.offsetMin.y, Is.GreaterThanOrEqualTo(FrameInnerGoldLine + 6f));
            Assert.That(-Viewport.offsetMax.x, Is.GreaterThanOrEqualTo(FrameInnerGoldLine + 6f));
            Assert.That(-Viewport.offsetMax.y, Is.GreaterThanOrEqualTo(FrameInnerGoldLine + 6f));
        }

        [Test]
        public void ReadingSectionsKeepTheirOrderWithTheNoticeFirstAndWarningLast()
        {
            var expected = new[]
            {
                "Phase67_OfflineNotice",
                "Phase7_ResultSectionSummary", "SummaryText",
                "Phase7_ResultSectionOverall", "OverallText",
                "Phase7_ResultSectionCards", "CardAnalysisText",
                "Phase7_ResultSectionAdvice", "AdviceText",
                "Phase67_ResultSectionWarning", "WarningText",
            };
            var actual = Enumerable.Range(0, Content.childCount).Select(i => Content.GetChild(i).name).ToArray();

            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(Content.Find("Phase67_OfflineNotice").gameObject.activeSelf, Is.False,
                "the notice is shown only for offline readings");
            Assert.That(Content.Find("Phase67_ResultSectionWarning").gameObject.activeSelf, Is.False);
            Assert.That(Content.Find("WarningText").gameObject.activeSelf, Is.False);
            Assert.That(canvas.Find("WarningText"), Is.Null, "WarningText has left the footer");
        }

        [Test]
        public void ScrollbarAndFadeAreWired()
        {
            var scrollRect = Scroll.GetComponent<ScrollRect>();
            Assert.That(scrollRect.verticalScrollbar, Is.Not.Null);
            Assert.That(scrollRect.verticalScrollbar.name, Is.EqualTo("Phase67_ReadingScrollbar"));
            Assert.That(scrollRect.verticalScrollbarVisibility, Is.EqualTo(ScrollRect.ScrollbarVisibility.AutoHide));
            Assert.That(scrollRect.verticalScrollbar.direction, Is.EqualTo(Scrollbar.Direction.BottomToTop));

            var fade = Scroll.Find("Phase67_ReadingBottomFade");
            Assert.That(fade, Is.Not.Null);
            Assert.That(fade.GetComponent<ReadingFadeGradient>(), Is.Not.Null);
            Assert.That(fade.GetComponent<Image>().raycastTarget, Is.False, "the fade must not swallow scroll input");
            Assert.That(fade.GetSiblingIndex(), Is.GreaterThan(Viewport.GetSiblingIndex()), "the fade draws over the text");

            var navigator = Scroll.GetComponent<ResultReadingNavigator>();
            Assert.That(navigator, Is.Not.Null);
            var so = new SerializedObject(navigator);
            Assert.That(so.FindProperty("scroll").objectReferenceValue, Is.SameAs(scrollRect));
            Assert.That(so.FindProperty("bottomFade").objectReferenceValue, Is.SameAs(fade.GetComponent<Image>()));
            Assert.That(so.FindProperty("cardAnalysisText").objectReferenceValue,
                Is.SameAs(Content.Find("CardAnalysisText").GetComponent<TMP_Text>()));
            Assert.That(so.FindProperty("cardSectionHeading").objectReferenceValue,
                Is.SameAs(Content.Find("Phase7_ResultSectionCards")));
        }

        [Test]
        public void EverySpreadCellIsAClickTarget()
        {
            var band = canvas.Find("MP_ResultSpreadBand");
            Assert.That(band, Is.Not.Null, "control: the Phase 60 band exists");
            for (var i = 0; i < 10; i++)
            {
                var cell = band.Find($"SpreadCell_{i}");
                var target = cell.GetComponent<ResultSpreadCellTarget>();
                Assert.That(target, Is.Not.Null, $"cell {i} needs a ResultSpreadCellTarget");
                Assert.That(target.CardIndex, Is.EqualTo(i));

                var so = new SerializedObject(target);
                Assert.That(so.FindProperty("navigator").objectReferenceValue, Is.Not.Null);
                Assert.That(so.FindProperty("glow").objectReferenceValue, Is.SameAs(cell.Find("Glow").GetComponent<Image>()));
                Assert.That(cell.Find("Label").GetComponent<TMP_Text>().raycastTarget, Is.True, "the label is clickable too");
            }
        }

        [Test]
        public void HeaderAndButtonsArePinnedToTheCanvasEdges()
        {
            var fit = canvas.GetComponent<ResultCanvasAspectFit>();
            Assert.That(fit, Is.Not.Null);
            Assert.That(canvas.GetComponent<CanvasScaler>().matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.001f),
                "the saved scene keeps the shared match factor; the fit changes it only at runtime");

            var so = new SerializedObject(fit);
            Assert.That(so.FindProperty("scaler").objectReferenceValue, Is.SameAs(canvas.GetComponent<CanvasScaler>()));
            var pinned = so.FindProperty("pinned");
            var expected = new (string name, int edge)[]
            {
                ("QuestionText", 0), ("SpreadNameText", 0), ("Phase66_ModeLabel", 0), ("Phase8_ResultGoldDividerTop", 0),
                ("BackToMenuButton", 1), ("Phase66_RetryInterpretationButton", 1), ("Phase66_OfflineInterpretationButton", 1),
            };

            Assert.That(pinned.arraySize, Is.EqualTo(expected.Length));
            for (var i = 0; i < expected.Length; i++)
            {
                var element = pinned.GetArrayElementAtIndex(i);
                var target = (RectTransform)element.FindPropertyRelative("target").objectReferenceValue;
                Assert.That(target.name, Is.EqualTo(expected[i].name));
                Assert.That(element.FindPropertyRelative("edge").enumValueIndex, Is.EqualTo(expected[i].edge));
                var y = ResultCanvasAspectFit.PinnedY(720f, (ResultCanvasAspectFit.Edge)expected[i].edge,
                    element.FindPropertyRelative("offsetFromEdge").floatValue);
                Assert.That(y, Is.EqualTo(target.anchoredPosition.y).Within(0.01f), $"{target.name} keeps its saved 16:9 position");
            }
        }

        [Test]
        public void RevealIncludesHeadingsModeLabelAndBand()
        {
            var so = new SerializedObject(canvas.GetComponent<ResultRevealDirector>());
            var groups = so.FindProperty("revealGroups");
            var companions = so.FindProperty("companions");
            var pairs = Enumerable.Range(0, companions.arraySize).Select(i =>
            {
                var element = companions.GetArrayElementAtIndex(i);
                var primary = (CanvasGroup)groups.GetArrayElementAtIndex(element.FindPropertyRelative("groupIndex").intValue)
                    .objectReferenceValue;
                var companion = (CanvasGroup)element.FindPropertyRelative("group").objectReferenceValue;
                return primary.name + ">" + companion.name;
            }).ToArray();

            Assert.That(groups.arraySize, Is.EqualTo(7), "control: the seven Phase 3 reveal groups are unchanged");
            Assert.That(pairs, Is.EquivalentTo(new[]
            {
                "SpreadNameText>Phase66_ModeLabel", "SpreadNameText>MP_ResultSpreadBand",
                "SummaryText>Phase67_OfflineNotice", "SummaryText>Phase7_ResultSectionSummary",
                "OverallText>Phase7_ResultSectionOverall", "CardAnalysisText>Phase7_ResultSectionCards",
                "AdviceText>Phase7_ResultSectionAdvice", "WarningText>Phase67_ResultSectionWarning",
            }));
        }

        [Test]
        public void SingleCardReadingPanelKeepsItsRightEdgeAndPresenterIsWired()
        {
            var so = new SerializedObject(canvas.GetComponent<ResultPanelPresenter>());
            var position = so.FindProperty("singleReadingPos").vector2Value;
            var size = so.FindProperty("singleReadingSize").vector2Value;

            Assert.That(position, Is.EqualTo(new Vector2(160f, 4f)));
            Assert.That(size, Is.EqualTo(new Vector2(772f, 448f)));
            Assert.That(position.x + size.x * 0.5f, Is.EqualTo(546f).Within(0.01f), "the right edge stays where Phase 60 put it");
            Assert.That(Scroll.anchoredPosition, Is.EqualTo(position), "the saved scene shows the single-card layout");
            Assert.That(Scroll.sizeDelta, Is.EqualTo(size));

            Assert.That(so.FindProperty("readingNavigator").objectReferenceValue,
                Is.SameAs(Scroll.GetComponent<ResultReadingNavigator>()));
            Assert.That(so.FindProperty("offlineNoticeText").objectReferenceValue,
                Is.SameAs(Content.Find("Phase67_OfflineNotice").GetComponent<TMP_Text>()));
            Assert.That(so.FindProperty("warningHeading").objectReferenceValue,
                Is.SameAs(Content.Find("Phase67_ResultSectionWarning").gameObject));
            Assert.That(so.FindProperty("bottomDivider").objectReferenceValue,
                Is.SameAs(canvas.Find("Phase8_ResultGoldDividerBottom").gameObject));
            Assert.That(so.FindProperty("cellTargets").arraySize, Is.EqualTo(10));
        }
    }
}
```

- [ ] **Step 2: 放宽 Phase 29 的顺序守护**

在 `Assets/Tests/EditMode/Phase29ResultScrollTests.cs` 中，把

```csharp
            var content = FindScroll().content;
            for (var i = 0; i < SectionOrder.Length; i++)
            {
                var child = content.Find(SectionOrder[i]);
                Assert.That(child, Is.Not.Null, $"{SectionOrder[i]} should be parented under the scroll Content");
                Assert.That(child.GetSiblingIndex(), Is.EqualTo(i),
                    $"{SectionOrder[i]} should keep reading order (header then body)");
            }
```

替换为

```csharp
            var content = FindScroll().content;
            var first = content.Find(SectionOrder[0]);
            Assert.That(first, Is.Not.Null, $"{SectionOrder[0]} should be parented under the scroll Content");

            // Phase 67 puts the offline notice before the sections and the warning section after
            // them, so the eight keep reading order as one adjacent run rather than at fixed indices.
            var start = first.GetSiblingIndex();
            for (var i = 0; i < SectionOrder.Length; i++)
            {
                var child = content.Find(SectionOrder[i]);
                Assert.That(child, Is.Not.Null, $"{SectionOrder[i]} should be parented under the scroll Content");
                Assert.That(child.GetSiblingIndex(), Is.EqualTo(start + i),
                    $"{SectionOrder[i]} should keep reading order (header then body)");
            }
```

- [ ] **Step 3: 给 presenter 加字段**

在 `Assets/Scripts/UI/ResultPanelPresenter.cs` 中，把

```csharp
        [SerializeField] private float readyFadeSeconds = 0.6f;
```

替换为

```csharp
        [SerializeField] private float readyFadeSeconds = 0.6f;

        // Phase 67: reading experience (spec C). Wired by Phase67ResultReadingBootstrapper.
        [Header("Phase 67: reading experience")]
        [SerializeField] private ResultReadingNavigator readingNavigator;
        [SerializeField] private TMP_Text offlineNoticeText;
        [SerializeField] private GameObject warningHeading;
        [SerializeField] private GameObject bottomDivider;
        [SerializeField] private ResultSpreadCellTarget[] cellTargets;
```

- [ ] **Step 4: 运行测试，确认失败**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c5-red -testFilter "TarotUnity.Tests.EditMode.Phase67ResultSceneStructureTests;TarotUnity.Tests.EditMode.Phase29ResultScrollTests"
```

预期：
- `Phase67ResultSceneStructureTests` 的 7 个测试全部失败（场景还没搭）；
- `Phase29ResultScrollTests` 全部通过（放宽后的断言在旧场景上也成立）；
- 汇总一行为 `failed=7`。

- [ ] **Step 5: 创建 `Assets/Editor/Phase67ResultReadingBootstrapper.cs`**

```csharp
using System.Collections.Generic;
using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 67 (spec C): builds the Result reading experience on the existing scene.
    /// - Viewport inset 24 inside the gold frame, new content padding and spacing.
    /// - Offline notice first in the reading; a 提醒 section (WarningText moved in from the footer) last.
    /// - Bottom fade, scrollbar, reading navigator, click targets on every spread cell.
    /// - Edge pinning for the header and button row, and reveal companions.
    /// - Presenter wiring, plus a single-card panel of 772x448 at x 160.
    /// Re-running reuses what it created.
    /// </summary>
    public static class Phase67ResultReadingBootstrapper
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        public const string OfflineNoticeName = "Phase67_OfflineNotice";
        public const string WarningHeadingName = "Phase67_ResultSectionWarning";
        public const string ScrollbarName = "Phase67_ReadingScrollbar";
        public const string BottomFadeName = "Phase67_ReadingBottomFade";

        private const float ViewportInset = 24f;
        private const float ContentSpacing = 8f;
        private const float HeadingTopMargin = 10f;
        private const float BodyLineSpacing = 10f;
        private const float ScrollbarWidth = 4f;
        private const float ScrollbarRightInset = 26f;
        private const float ScrollbarVerticalInset = 30f;
        private const float FadeHeight = 36f;
        private static readonly Vector2 SingleReadingPos = new Vector2(160f, 4f);
        private static readonly Vector2 SingleReadingSize = new Vector2(772f, 448f);

        private static readonly Color NoticeInk = new Color(0.78f, 0.66f, 0.44f, 1f);
        private static readonly Color BodyInk = new Color(0.92f, 0.88f, 0.78f, 1f);
        private static readonly Color FrameFill = new Color(0.118f, 0.059f, 0.102f, 0.92f);
        private static readonly Color ScrollTrack = new Color(0.86f, 0.71f, 0.42f, 0.12f);
        private static readonly Color ScrollHandle = new Color(0.86f, 0.71f, 0.42f, 0.8f);

        private static readonly string[] SectionHeadings =
        {
            "Phase7_ResultSectionSummary", "Phase7_ResultSectionOverall", "Phase7_ResultSectionCards", "Phase7_ResultSectionAdvice",
        };

        private static readonly string[] SectionBodies = { "SummaryText", "OverallText", "CardAnalysisText", "AdviceText" };

        private static readonly (string name, ResultCanvasAspectFit.Edge edge)[] PinnedElements =
        {
            ("QuestionText", ResultCanvasAspectFit.Edge.Top),
            ("SpreadNameText", ResultCanvasAspectFit.Edge.Top),
            ("Phase66_ModeLabel", ResultCanvasAspectFit.Edge.Top),
            ("Phase8_ResultGoldDividerTop", ResultCanvasAspectFit.Edge.Top),
            ("BackToMenuButton", ResultCanvasAspectFit.Edge.Bottom),
            ("Phase66_RetryInterpretationButton", ResultCanvasAspectFit.Edge.Bottom),
            ("Phase66_OfflineInterpretationButton", ResultCanvasAspectFit.Edge.Bottom),
        };

        private static readonly (string group, string companion)[] RevealCompanions =
        {
            ("SpreadNameText", "Phase66_ModeLabel"),
            ("SpreadNameText", "MP_ResultSpreadBand"),
            ("SummaryText", OfflineNoticeName),
            ("SummaryText", "Phase7_ResultSectionSummary"),
            ("OverallText", "Phase7_ResultSectionOverall"),
            ("CardAnalysisText", "Phase7_ResultSectionCards"),
            ("AdviceText", "Phase7_ResultSectionAdvice"),
            ("WarningText", WarningHeadingName),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 67 Result Reading Bootstrap")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);
            var canvasObject = GameObject.Find("ResultCanvas");
            if (canvasObject == null)
            {
                Debug.LogError("Phase 67: ResultCanvas not found.");
                return;
            }

            var canvas = canvasObject.transform;
            var presenter = canvasObject.GetComponent<ResultPanelPresenter>();
            var reveal = canvasObject.GetComponent<ResultRevealDirector>();
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            var scroll = canvas.Find("ResultReadingScroll") as RectTransform;
            var scrollRect = scroll != null ? scroll.GetComponent<ScrollRect>() : null;
            var viewport = scroll != null ? scroll.Find("Viewport") as RectTransform : null;
            var content = viewport != null ? viewport.Find("Content") as RectTransform : null;
            var band = canvas.Find("MP_ResultSpreadBand");
            var warning = FindDeep(canvas, "WarningText");
            var adviceHeading = content != null ? content.Find("Phase7_ResultSectionAdvice") : null;
            var summaryText = content != null ? content.Find("SummaryText")?.GetComponent<TMP_Text>() : null;
            var bottomDivider = canvas.Find("Phase8_ResultGoldDividerBottom");
            if (presenter == null || reveal == null || scaler == null || scrollRect == null || viewport == null
                || content == null || band == null || warning == null || adviceHeading == null || summaryText == null
                || bottomDivider == null)
            {
                Debug.LogError("Phase 67: expected Result objects are missing (presenter, reveal director, scaler, " +
                    "reading scroll, spread band, WarningText, section headings or the bottom divider).");
                return;
            }

            LayOutViewport(viewport, content);
            var notice = EnsureNotice(content, summaryText);
            var warningHeading = EnsureWarningSection(content, adviceHeading, warning);
            var fade = EnsureBottomFade(scroll, viewport);
            EnsureScrollbar(scroll, scrollRect, fade.transform.GetSiblingIndex() + 1);
            var navigator = EnsureNavigator(scroll, scrollRect, fade, content);
            var targets = EnsureCellTargets(band, navigator);
            EnsureAspectFit(canvasObject, scaler);
            WireRevealCompanions(reveal, canvas);
            WirePresenter(presenter, navigator, notice, warningHeading, bottomDivider.gameObject, targets);

            scroll.anchoredPosition = SingleReadingPos;
            scroll.sizeDelta = SingleReadingSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Phase 67: Result reading layout built and wired.");
        }

        private static void LayOutViewport(RectTransform viewport, RectTransform content)
        {
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(ViewportInset, ViewportInset);
            viewport.offsetMax = new Vector2(-ViewportInset, -ViewportInset);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 14, 8, 28);
            layout.spacing = ContentSpacing;

            foreach (var name in SectionHeadings)
            {
                var heading = content.Find(name)?.GetComponent<TMP_Text>();
                if (heading != null)
                {
                    heading.margin = new Vector4(0f, HeadingTopMargin, 0f, 0f);
                }
            }

            foreach (var name in SectionBodies)
            {
                var body = content.Find(name)?.GetComponent<TMP_Text>();
                if (body != null)
                {
                    body.lineSpacing = BodyLineSpacing;
                }
            }
        }

        private static TextMeshProUGUI EnsureNotice(RectTransform content, TMP_Text fontSource)
        {
            var notice = EnsureContentText(content, OfflineNoticeName, fontSource);
            notice.fontSize = 16f;
            notice.color = NoticeInk;
            notice.alignment = TextAlignmentOptions.TopLeft;
            notice.enableWordWrapping = true;
            notice.text = ReleaseUxCopy.OfflineWarning;
            notice.transform.SetSiblingIndex(0);
            notice.gameObject.SetActive(false);
            return notice;
        }

        private static GameObject EnsureWarningSection(RectTransform content, Transform adviceHeading, Transform warning)
        {
            var heading = content.Find(WarningHeadingName);
            if (heading == null)
            {
                // A copy of the 建议 heading keeps its font, gold accent marker and size.
                heading = Object.Instantiate(adviceHeading.gameObject, content).transform;
                heading.name = WarningHeadingName;
            }

            heading.GetComponent<TMP_Text>().text = ReleaseUxCopy.ResultSectionWarning;
            heading.SetAsLastSibling();
            heading.gameObject.SetActive(false);

            warning.SetParent(content, false);
            warning.SetAsLastSibling();
            var warningRect = (RectTransform)warning;
            warningRect.anchorMin = new Vector2(0f, 1f);
            warningRect.anchorMax = new Vector2(1f, 1f);
            warningRect.pivot = new Vector2(0.5f, 1f);
            warningRect.anchoredPosition = Vector2.zero;
            var warningText = warning.GetComponent<TMP_Text>();
            warningText.fontSize = 19f;
            warningText.color = BodyInk;
            warningText.alignment = TextAlignmentOptions.TopLeft;
            warningText.enableWordWrapping = true;
            warningText.lineSpacing = BodyLineSpacing;
            warningText.margin = Vector4.zero;
            warning.gameObject.SetActive(false);
            return heading.gameObject;
        }

        private static Image EnsureBottomFade(RectTransform scroll, RectTransform viewport)
        {
            var fade = EnsureChild(scroll, BottomFadeName);
            fade.anchorMin = new Vector2(0f, 0f);
            fade.anchorMax = new Vector2(1f, 0f);
            fade.pivot = new Vector2(0.5f, 0f);
            fade.anchoredPosition = new Vector2(0f, ViewportInset);
            fade.sizeDelta = new Vector2(-2f * ViewportInset, FadeHeight);

            var image = EnsureComponent<Image>(fade.gameObject);
            image.sprite = null;
            image.color = FrameFill;
            image.raycastTarget = false;
            EnsureComponent<ReadingFadeGradient>(fade.gameObject);
            fade.SetSiblingIndex(viewport.GetSiblingIndex() + 1);
            return image;
        }

        private static void EnsureScrollbar(RectTransform scroll, ScrollRect scrollRect, int siblingIndex)
        {
            var bar = EnsureChild(scroll, ScrollbarName);
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 0.5f);
            bar.anchoredPosition = new Vector2(-ScrollbarRightInset, 0f);
            bar.sizeDelta = new Vector2(ScrollbarWidth, -2f * ScrollbarVerticalInset);
            var track = EnsureComponent<Image>(bar.gameObject);
            track.sprite = null;
            track.color = ScrollTrack;

            var handle = EnsureChild(bar, "Handle");
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.offsetMin = Vector2.zero;
            handle.offsetMax = Vector2.zero;
            var handleImage = EnsureComponent<Image>(handle.gameObject);
            handleImage.sprite = null;
            handleImage.color = ScrollHandle;

            var scrollbar = EnsureComponent<Scrollbar>(bar.gameObject);
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.verticalScrollbarSpacing = 0f;
            bar.SetSiblingIndex(siblingIndex);
        }

        private static ResultReadingNavigator EnsureNavigator(
            RectTransform scroll, ScrollRect scrollRect, Image fade, RectTransform content)
        {
            var navigator = EnsureComponent<ResultReadingNavigator>(scroll.gameObject);
            var so = new SerializedObject(navigator);
            so.FindProperty("scroll").objectReferenceValue = scrollRect;
            so.FindProperty("bottomFade").objectReferenceValue = fade;
            so.FindProperty("cardAnalysisText").objectReferenceValue = content.Find("CardAnalysisText").GetComponent<TMP_Text>();
            so.FindProperty("cardSectionHeading").objectReferenceValue = content.Find("Phase7_ResultSectionCards");
            so.ApplyModifiedPropertiesWithoutUndo();
            return navigator;
        }

        private static ResultSpreadCellTarget[] EnsureCellTargets(Transform band, ResultReadingNavigator navigator)
        {
            var targets = new List<ResultSpreadCellTarget>();
            for (var i = 0; ; i++)
            {
                var cell = band.Find($"SpreadCell_{i}");
                if (cell == null)
                {
                    break;
                }

                var target = EnsureComponent<ResultSpreadCellTarget>(cell.gameObject);
                var so = new SerializedObject(target);
                so.FindProperty("cardIndex").intValue = i;
                so.FindProperty("navigator").objectReferenceValue = navigator;
                so.FindProperty("glow").objectReferenceValue = cell.Find("Glow")?.GetComponent<Image>();
                so.ApplyModifiedPropertiesWithoutUndo();

                var label = cell.Find("Label")?.GetComponent<TMP_Text>();
                if (label != null)
                {
                    label.raycastTarget = true;
                }

                targets.Add(target);
            }

            return targets.ToArray();
        }

        private static void EnsureAspectFit(GameObject canvasObject, CanvasScaler scaler)
        {
            var canvas = canvasObject.transform;
            var fit = EnsureComponent<ResultCanvasAspectFit>(canvasObject);
            var so = new SerializedObject(fit);
            so.FindProperty("scaler").objectReferenceValue = scaler;
            so.FindProperty("canvasRect").objectReferenceValue = canvasObject.GetComponent<RectTransform>();
            var pinned = so.FindProperty("pinned");
            pinned.arraySize = PinnedElements.Length;
            var half = TarotUiSpacing.ReferenceHeight * 0.5f;
            for (var i = 0; i < PinnedElements.Length; i++)
            {
                var (name, edge) = PinnedElements[i];
                var target = canvas.Find(name) as RectTransform;
                if (target == null)
                {
                    Debug.LogError($"Phase 67: {name} not found for edge pinning.");
                    continue;
                }

                // The offset is taken from the saved 16:9 position, so pinning at 720 changes nothing.
                var element = pinned.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("target").objectReferenceValue = target;
                element.FindPropertyRelative("edge").enumValueIndex = (int)edge;
                element.FindPropertyRelative("offsetFromEdge").floatValue = edge == ResultCanvasAspectFit.Edge.Top
                    ? half - target.anchoredPosition.y
                    : target.anchoredPosition.y + half;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireRevealCompanions(ResultRevealDirector reveal, Transform canvas)
        {
            var so = new SerializedObject(reveal);
            var groups = so.FindProperty("revealGroups");
            var companions = so.FindProperty("companions");
            companions.arraySize = RevealCompanions.Length;
            for (var i = 0; i < RevealCompanions.Length; i++)
            {
                var (groupName, companionName) = RevealCompanions[i];
                var index = -1;
                for (var g = 0; g < groups.arraySize; g++)
                {
                    var group = groups.GetArrayElementAtIndex(g).objectReferenceValue as CanvasGroup;
                    if (group != null && group.name == groupName)
                    {
                        index = g;
                        break;
                    }
                }

                var companion = FindDeep(canvas, companionName);
                if (index < 0 || companion == null)
                {
                    Debug.LogError($"Phase 67: reveal companion {groupName} > {companionName} could not be wired.");
                    continue;
                }

                var element = companions.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("groupIndex").intValue = index;
                element.FindPropertyRelative("group").objectReferenceValue = EnsureComponent<CanvasGroup>(companion.gameObject);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePresenter(
            ResultPanelPresenter presenter, ResultReadingNavigator navigator, TMP_Text notice, GameObject warningHeading,
            GameObject bottomDivider, ResultSpreadCellTarget[] targets)
        {
            var so = new SerializedObject(presenter);
            so.FindProperty("readingNavigator").objectReferenceValue = navigator;
            so.FindProperty("offlineNoticeText").objectReferenceValue = notice;
            so.FindProperty("warningHeading").objectReferenceValue = warningHeading;
            so.FindProperty("bottomDivider").objectReferenceValue = bottomDivider;
            var cells = so.FindProperty("cellTargets");
            cells.arraySize = targets.Length;
            for (var i = 0; i < targets.Length; i++)
            {
                cells.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            }

            so.FindProperty("singleReadingPos").vector2Value = SingleReadingPos;
            so.FindProperty("singleReadingSize").vector2Value = SingleReadingSize;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TextMeshProUGUI EnsureContentText(RectTransform content, string name, TMP_Text fontSource)
        {
            var existing = content.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(content, false);
            }

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            // Borrow the body SDF font and material so the font-role guards hold.
            var text = EnsureComponent<TextMeshProUGUI>(go);
            text.font = fontSource.font;
            text.fontSharedMaterial = fontSource.fontSharedMaterial;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return (RectTransform)existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
```

- [ ] **Step 6: 执行 Bootstrapper**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
UNITY=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
PROJECT=/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
LOG="$S/run/results/c5-bootstrap.log"
if pgrep -f 'Unity.app/Contents/MacOS/Unity' >/dev/null; then echo "STOP: Unity is running"; exit 1; fi
test ! -e "$LOG" || { echo "STOP: $LOG exists - use a new name"; exit 1; }
ulimit -n 10240 2>/dev/null || ulimit -n 4096 2>/dev/null || true
"$UNITY" -projectPath "$PROJECT" -batchmode -nographics -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase67ResultReadingBootstrapper.Run -quit -logFile "$LOG"
echo "unity exit=$?"
grep -n "Phase 67" "$LOG" | tail -n 12
```

预期：
- `unity exit=0`；
- 日志里有 `Phase 67: Result reading layout built and wired.`；
- 没有任何 `Phase 67: … not found`、`could not be wired` 或 `missing` 的错误行。

出现错误行就 STOP，并原样报告。

- [ ] **Step 7: 运行测试，确认通过**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c5-green -testFilter "TarotUnity.Tests.EditMode.Phase67ResultSceneStructureTests;TarotUnity.Tests.EditMode.Phase29ResultScrollTests"
bash "$S/run/ut.sh" EditMode c5-full
bash "$S/run/ut.sh" PlayMode c5-full
```

预期：
- 过滤运行 `failed=0`；
- 全量 EditMode `total=420 failed=0`；
- 全量 PlayMode `total=68 passed=65 failed=0 skipped=3`。

旧的守护测试应当原样通过，包括 `Phase8VisualIdentityTests`（正文宽 702 ≥ 700）、`Phase11VisualReviewTests`、`Phase12CardFirstRevealTests`、`Phase25ResultCompositionTests`、`Phase30CrossScreenConsistencyTests`、`Phase35UiPolishTests`、`Phase60/62/64/66` 系列。如果其中有失败，不要修改这些测试，STOP 并报告。

- [ ] **Step 8: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
git status --short
git add Assets/Editor/Phase67ResultReadingBootstrapper.cs Assets/Editor/Phase67ResultReadingBootstrapper.cs.meta \
  Assets/Scripts/UI/ResultPanelPresenter.cs Assets/Scenes/Result.unity \
  Assets/Tests/EditMode/Phase29ResultScrollTests.cs \
  Assets/Tests/EditMode/Phase67ResultSceneStructureTests.cs Assets/Tests/EditMode/Phase67ResultSceneStructureTests.cs.meta
git commit -F - <<'EOF'
feat(unity): build the Phase 67 Result reading structure

The viewport now clears the gold frame, the reading gains an offline notice and a
提醒 section, and the panel gets a bottom fade, a scrollbar and a reading navigator.
Every spread cell becomes a click target, the header and buttons are pinned to the
canvas edges at runtime, and headings, the mode label and the band join the reveal.
The single-card panel widens to 772 so the body keeps its 700-wide column. The Phase 29
order guard now checks relative order instead of fixed indices.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
git log -1 --format=%B | tail -3
git status --short
```

预期：提交后 `git status --short` 只剩字体图集，以及未跟踪的 `Phase66LiveBackendTests.cs`（含 `.meta`）。

### Task 6: presenter 整合（布局、纯文本、分块、离线提示、提醒段、点牌与渐隐）

**Files:**
- Modify: `Assets/Scripts/UI/ResultPanelPresenter.cs`（整文件替换）
- Test: `Assets/Tests/EditMode/Phase67ResultPresenterTests.cs`
- Test: `Assets/Tests/PlayMode/Phase67ResultReadingPlayTests.cs`

**Interfaces:**
- Consumes：Task 1、2、4 的所有 API，以及 Task 5 搭好的场景对象和 presenter 字段。
- Produces：
  - `ResultPanelPresenter.ApplyLayout(Vector2 canvasSize)`（public，供截图 Builder 和测试调用）。
  - presenter 在运行时检测到画布高度变化时，自动重新布局。
  - 状态与导航的对应关系：Ready/Offline 时导航可交互，Pending/Failed 时不可交互。
  - 离线会话显示离线提示；在线会话且 `warning` 非空时显示「提醒」段。
  - 多牌布局隐藏页脚装饰线，单牌布局显示。
  - 模式标签随正文一起淡入。

- [ ] **Step 1: 写 PlayMode 测试**

创建 `Assets/Tests/PlayMode/Phase67ResultReadingPlayTests.cs`：

```csharp
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 67 on the real Result scene: clicking a card scrolls to its analysis block, the
    /// bottom fade hides at the end of the reading, and headings, the mode label and the band
    /// fade in with the reveal.
    /// </summary>
    public sealed class Phase67ResultReadingPlayTests
    {
        private static readonly string[] CelticNames =
        {
            "现状", "挑战", "根基", "过去", "顶冠", "未来", "自我", "环境", "希望与恐惧", "结果",
        };

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ReadingSessionStore.Clear();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ReadingSessionStore.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClickingACardScrollsItsBlockToTheTopAndHighlightsIt()
        {
            yield return LoadResultWith(Offline(10));
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            var navigator = Object.FindFirstObjectByType<ResultReadingNavigator>();
            var scroll = navigator.GetComponent<ScrollRect>();
            yield return WaitUntil(() => Object.FindFirstObjectByType<ResultRevealDirector>().IsRevealComplete, 10f,
                "expected the reveal to finish");
            Canvas.ForceUpdateCanvases();

            Assert.That(navigator.Blocks.Count, Is.EqualTo(10), "control: the analysis was split into ten blocks");
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height), "control: the reading scrolls");
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f), "control: it starts at the top");

            var cell = GameObject.Find("MP_ResultSpreadBand").transform.Find("SpreadCell_4").GetComponent<ResultSpreadCellTarget>();
            cell.Activate();
            Assert.That(navigator.HighlightedCard, Is.EqualTo(4), "the heading glow starts at once");
            yield return new WaitForSecondsRealtime(0.6f);

            var text = GetField<TMP_Text>(presenter, "cardAnalysisText");
            text.ForceMeshUpdate();
            var block = navigator.Blocks[4];
            var info = text.textInfo;
            var line = info.characterInfo[block.HeadingStart].lineNumber;
            var world = text.rectTransform.TransformPoint(new Vector3(0f, info.lineInfo[line].ascender, 0f));
            var viewport = scroll.viewport;
            var fromTop = viewport.rect.yMax - viewport.InverseTransformPoint(world).y;

            Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(0.999f), "control: the reading moved");
            Assert.That(fromTop, Is.InRange(-1f, viewport.rect.height * 0.2f),
                "the fifth card's heading sits near the top of the viewport");
        }

        [UnityTest]
        public IEnumerator FadeHidesAtTheBottomOfTheReading()
        {
            yield return LoadResultWith(Offline(10));
            var navigator = Object.FindFirstObjectByType<ResultReadingNavigator>();
            var scroll = navigator.GetComponent<ScrollRect>();
            yield return WaitUntil(() => Object.FindFirstObjectByType<ResultRevealDirector>().IsRevealComplete, 10f,
                "expected the reveal to finish");
            yield return null;

            Assert.That(navigator.IsFadeVisible, Is.True, "long text at the top shows the fade");
            Assert.That(scroll.verticalScrollbar.gameObject.activeInHierarchy, Is.True, "the scrollbar shows for long text");

            scroll.verticalNormalizedPosition = 0f;
            yield return null;
            yield return null;

            Assert.That(navigator.IsFadeVisible, Is.False, "the fade hides at the bottom");
        }

        [UnityTest]
        public IEnumerator RevealBringsInTheHeadingsModeLabelAndBand()
        {
            yield return LoadResultWith(Offline(3));
            var director = Object.FindFirstObjectByType<ResultRevealDirector>();
            var summaryHeading = GameObject.Find("Phase7_ResultSectionSummary").GetComponent<CanvasGroup>();

            Assert.That(director.IsRevealComplete, Is.False, "control: the reveal is still running");
            Assert.That(summaryHeading.alpha, Is.LessThan(1f), "a heading waits for its section");

            yield return WaitUntil(() => director.IsRevealComplete, 10f, "expected the reveal to finish");

            var names = new[]
            {
                "Phase66_ModeLabel", "MP_ResultSpreadBand", "Phase67_OfflineNotice", "Phase7_ResultSectionSummary",
                "Phase7_ResultSectionOverall", "Phase7_ResultSectionCards", "Phase7_ResultSectionAdvice",
            };
            foreach (var name in names)
            {
                var go = GameObject.Find(name);
                Assert.That(go, Is.Not.Null, $"{name} should be active for an offline three-card reading");
                Assert.That(go.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f), $"{name} is fully shown after the reveal");
            }
        }

        private static ReadingSessionSnapshot Offline(int cardCount)
        {
            var draws = cardCount == 10
                ? LocalReadingSimulator.CreatePlaceholderDraws(10, CelticNames, null)
                : LocalReadingSimulator.CreatePlaceholderDraws(cardCount);
            return LocalReadingSimulator.CreateSession(2, "牌阵", "此刻我最需要留意什么？", "general", draws);
        }

        private static IEnumerator LoadResultWith(ReadingSessionSnapshot session)
        {
            ReadingSessionStore.Save(session);
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

- [ ] **Step 2: 运行 PlayMode 测试，确认失败**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" PlayMode c6-red -testFilter TarotUnity.Tests.PlayMode.Phase67ResultReadingPlayTests
```

预期 `total=3 failed=3`：
- `ClickingACard…` 失败在 `control: the analysis was split into ten blocks`（presenter 还没把分块交给导航组件）；
- `FadeHides…` 失败在 `long text at the top shows the fade`（导航组件还没被设为可交互）；
- `RevealBringsIn…` 失败在 `Phase67_OfflineNotice should be active for an offline three-card reading`（离线提示要等本任务的 presenter 才会显示；伴随组本身在 Task 4、5 已接好）。

任何一条的失败原因与上面不同，就 STOP 并报告。

- [ ] **Step 3: 写 EditMode 测试**

创建 `Assets/Tests/EditMode/Phase67ResultPresenterTests.cs`：

```csharp
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 67: the presenter applies ResultSpreadLayout to the scene, shows external text
    /// literally, splits the card analysis into blocks whose positions match TMP, and routes the
    /// offline notice, the 提醒 section and the bottom divider by source and layout.
    /// </summary>
    public sealed class Phase67ResultPresenterTests
    {
        private const string ScenePath = "Assets/Scenes/Result.unity";

        private static readonly string[] CelticNames =
        {
            "现状", "挑战", "根基", "过去", "顶冠", "未来", "自我", "环境", "希望与恐惧", "结果",
        };

        private ResultPanelPresenter presenter;
        private SerializedObject presenterSo;

        [SetUp]
        public void OpenScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            Assert.That(presenter, Is.Not.Null, "control: the Result scene has its presenter");
            presenterSo = new SerializedObject(presenter);
        }

        private T Field<T>(string name) where T : Object
        {
            return (T)presenterSo.FindProperty(name).objectReferenceValue;
        }

        private static ReadingSessionSnapshot Offline(int cardCount, string question = "问题？")
        {
            var draws = cardCount == 10
                ? LocalReadingSimulator.CreatePlaceholderDraws(10, CelticNames, null)
                : LocalReadingSimulator.CreatePlaceholderDraws(cardCount);
            return LocalReadingSimulator.CreateSession(2, "牌阵", question, "general", draws);
        }

        [Test]
        public void SpreadLayoutIsAppliedToTheSceneForTenCards()
        {
            presenter.PresentSession(Offline(10));
            presenter.ApplyLayout(new Vector2(1280f, 720f));
            var layout = ResultSpreadLayout.Compute(10, 720f);
            var band = GameObject.Find("MP_ResultSpreadBand").transform;

            for (var i = 0; i < 10; i++)
            {
                var cell = (RectTransform)band.Find($"SpreadCell_{i}");
                Assert.That(cell.gameObject.activeSelf, Is.True, $"control: cell {i} is used");
                Assert.That(Vector2.Distance(cell.anchoredPosition, layout.Cells[i].Position), Is.LessThan(0.01f), $"cell {i} position");
                Assert.That(cell.localScale.x, Is.EqualTo(layout.Cells[i].Scale).Within(0.0001f), $"cell {i} scale");
                var label = cell.Find("Label").GetComponent<TMP_Text>();
                Assert.That(label.fontSize * cell.localScale.x, Is.EqualTo(14f).Within(0.01f), $"cell {i} label renders at 14");
            }

            var scroll = Field<RectTransform>("readingScrollRect");
            Assert.That(Vector2.Distance(scroll.anchoredPosition, layout.ReadingPosition), Is.LessThan(0.01f));
            Assert.That(Vector2.Distance(scroll.sizeDelta, layout.ReadingSize), Is.LessThan(0.01f));
        }

        [Test]
        public void OfflineReadingShowsTheNoticeAndHidesTheWarningSection()
        {
            var notice = Field<TMP_Text>("offlineNoticeText");
            var warningHeading = Field<GameObject>("warningHeading");
            var warning = Field<TMP_Text>("warningText");

            foreach (var cardCount in new[] { 1, 3 })
            {
                var session = Offline(cardCount);
                presenter.PresentSession(session);

                Assert.That(notice.gameObject.activeSelf, Is.True, $"{cardCount} card(s): the offline notice shows");
                Assert.That(notice.text, Is.EqualTo(ReleaseUxCopy.OfflineWarning));
                Assert.That(warningHeading.activeSelf, Is.False, $"{cardCount} card(s): no 提醒 section for offline text");
                Assert.That(warning.gameObject.activeSelf, Is.False);
                Assert.That(warning.text, Is.EqualTo(session.warning), "control: the warning copy is still set");
            }
        }

        [Test]
        public void OnlineWarningShowsAsASectionOnSpreads()
        {
            var online = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 67, question = "问题？" }, LocalReadingSimulator.CreatePlaceholderDraws(3));
            ReadingSessionMapper.ApplyInterpretation(online, new InterpretationResponse
            {
                id = 1,
                summary = "概要",
                overall_interpretation = "整体",
                warning = "解读仅供参考。",
                model_used = "deepseek-chat",
            });

            presenter.PresentSession(online);

            Assert.That(GameObject.Find("MP_ResultSpreadBand"), Is.Not.Null, "control: the spread layout is showing");
            Assert.That(Field<TMP_Text>("offlineNoticeText").gameObject.activeSelf, Is.False);
            Assert.That(Field<GameObject>("warningHeading").activeSelf, Is.True);
            var warning = Field<TMP_Text>("warningText");
            Assert.That(warning.gameObject.activeSelf, Is.True);
            Assert.That(warning.text, Is.EqualTo("解读仅供参考。"));
        }

        [Test]
        public void ExternalTextIsShownLiterally()
        {
            presenter.PresentSession(Offline(1, "<size=200>大"));
            var question = Field<TMP_Text>("questionText");

            Assert.That(question.text, Is.EqualTo("<noparse><size=200>大</noparse>"));
            question.ForceMeshUpdate();
            Assert.That(question.textInfo.characterCount, Is.EqualTo(11), "every character of the question is laid out");
            Assert.That(question.textInfo.characterInfo[0].character, Is.EqualTo('<'));
            Assert.That(question.textInfo.characterInfo[10].pointSize, Is.EqualTo(question.fontSize).Within(0.01f),
                "the size tag is not applied");

            presenter.PresentSession(Offline(1, "我接下来该专注什么？"));
            Assert.That(question.text, Is.EqualTo("我接下来该专注什么？"), "control: plain text stays byte-identical");
        }

        [Test]
        public void CardBlockHeadingsLineUpWithTmpCharacters()
        {
            var session = Offline(3);
            presenter.PresentSession(session);
            var navigator = Field<ResultReadingNavigator>("readingNavigator");
            var text = Field<TMP_Text>("cardAnalysisText");

            Assert.That(navigator.Blocks.Count, Is.EqualTo(3), "control: the offline analysis was split into blocks");
            text.ForceMeshUpdate();
            var info = text.textInfo;
            for (var i = 0; i < 3; i++)
            {
                var block = navigator.Blocks[i];
                var heading = CardAnalysisFormatter.BuildHeading(session.cardDraws[i]);
                Assert.That(block.HeadingLength, Is.EqualTo(heading.Length));
                Assert.That(info.characterInfo[block.HeadingStart].character, Is.EqualTo(heading[0]),
                    $"block {i}: first heading character");
                Assert.That(info.characterInfo[block.HeadingStart + block.HeadingLength - 1].character,
                    Is.EqualTo(heading[heading.Length - 1]), $"block {i}: last heading character");
            }
        }

        [Test]
        public void SpreadHidesTheBottomDividerAndSingleShowsIt()
        {
            var divider = Field<GameObject>("bottomDivider");

            presenter.PresentSession(Offline(3));
            Assert.That(divider.activeSelf, Is.False, "the divider would cross the taller spread reading");

            presenter.PresentSession(Offline(1));
            Assert.That(divider.activeSelf, Is.True, "the single-card composition keeps its divider");
            var scroll = Field<RectTransform>("readingScrollRect");
            Assert.That(scroll.anchoredPosition, Is.EqualTo(new Vector2(160f, 4f)));
            Assert.That(scroll.sizeDelta, Is.EqualTo(new Vector2(772f, 448f)));
        }
    }
}
```

- [ ] **Step 4: 运行 EditMode 测试，确认失败**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c6-red -testFilter TarotUnity.Tests.EditMode.Phase67ResultPresenterTests
```

预期：`NO RESULTS XML`，报 `error CS1061`，指出 `ResultPanelPresenter` 没有 `ApplyLayout`。

- [ ] **Step 5: 整文件替换 `Assets/Scripts/UI/ResultPanelPresenter.cs`**

```csharp
using System;
using System.Collections;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    public sealed class ResultPanelPresenter : MonoBehaviour
    {
        // Phase 51: the Result screen migrates to TMP SDF like the other two.
        // These seven readouts - including the four that carry arbitrary-length
        // backend AI copy - become TMP_Text. The dynamic SDF atlas resolves any
        // Chinese the backend returns, and TMP_Text exposes the same .text the
        // presenter already set, so only the field types change.
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text spreadNameText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text overallText;
        [SerializeField] private TMP_Text cardAnalysisText;
        [SerializeField] private TMP_Text adviceText;
        [SerializeField] private TMP_Text warningText;
        [SerializeField] private Image resultCardArtworkSlot;
        [SerializeField] private CardArtworkCatalog cardArtworkCatalog;

        // Phase 60: a multi-card spread shows every drawn card, not just the first.
        // A one-card reading keeps the original single hero (left third + right
        // reading); a multi-card reading switches to a top band of all N cards with
        // the reading reflowed full-width below it. Phase 67 moves the band and
        // reading geometry into ResultSpreadLayout.
        [Serializable]
        public sealed class SpreadCardCell
        {
            public GameObject root;            // whole cell (toggled per used/unused)
            public RectTransform reversePivot; // 180deg Z for a reversed card (not the foil-driven rect)
            public Image artwork;              // the card face
            public TMP_Text label;             // position name, e.g. 过去 / 现在 / 建议
        }

        [Header("Phase 60: multi-card spread band")]
        // Every object that makes up the one-card hero (the showcase frame, the
        // artwork slot, and the placeholder are separate siblings) - all hidden
        // together for a multi-card spread.
        [SerializeField] private GameObject[] singleModeRoots = Array.Empty<GameObject>();
        [SerializeField] private GameObject spreadBandRoot;
        [SerializeField] private SpreadCardCell[] spreadCards = Array.Empty<SpreadCardCell>();
        [SerializeField] private RectTransform readingScrollRect;
        [SerializeField] private Vector2 singleReadingPos = new Vector2(160f, 4f);
        [SerializeField] private Vector2 singleReadingSize = new Vector2(772f, 448f);

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

        // Phase 67: reading experience (spec C). Wired by Phase67ResultReadingBootstrapper.
        [Header("Phase 67: reading experience")]
        [SerializeField] private ResultReadingNavigator readingNavigator;
        [SerializeField] private TMP_Text offlineNoticeText;
        [SerializeField] private GameObject warningHeading;
        [SerializeField] private GameObject bottomDivider;
        [SerializeField] private ResultSpreadCellTarget[] cellTargets;

        public const float PendingSlowNoticeSeconds = 20f;

        private float pendingSince = -1f;
        private Coroutine readyFade;
        private CardDrawData[] presentedDraws;
        private float laidOutCanvasHeight = -1f;

        private CardArtworkCatalog defaultArtworkCatalog;

        public void Present(PredictionDetailResponse detail)
        {
            if (detail == null)
            {
                Clear();
                return;
            }

            SetText(questionText, ReadingTextSanitizer.Plain(detail.question));
            SetText(spreadNameText, ReadingTextSanitizer.Plain(detail.spread_type?.name));
            SetText(summaryText, ReadingTextSanitizer.Plain(detail.interpretation?.summary));
            SetText(overallText, ReadingTextSanitizer.Plain(detail.interpretation?.overall_interpretation));
            ApplyCardAnalysis(detail.interpretation?.card_analysis, detail.card_draws);
            SetText(adviceText, ReadingTextSanitizer.Plain(detail.interpretation?.advice));
            SetText(warningText, ReadingTextSanitizer.Plain(detail.interpretation?.warning));
            SetOfflineNotice(false);
            SetWarningSection(!string.IsNullOrWhiteSpace(detail.interpretation?.warning));
            PresentCards(detail.card_draws);
            SetNavigatorInteractive(true);
        }

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
            SetNavigatorInteractive(false);
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
            SetNavigatorInteractive(true);

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
            SetNavigatorInteractive(false);
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
            SetNavigatorInteractive(true);
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

        /// <summary>
        /// Phase 67 (spec C 4.1): lays the spread band and the reading panel out for a canvas
        /// size. The presenter calls it when it presents a spread and whenever the canvas
        /// height changes; the capture builder calls it after resizing the canvas.
        /// </summary>
        public void ApplyLayout(Vector2 canvasSize)
        {
            laidOutCanvasHeight = canvasSize.y;
            var count = presentedDraws?.Length ?? 0;
            if (spreadBandRoot == null || !spreadBandRoot.activeSelf || spreadCards == null || count < 2)
            {
                return;
            }

            var used = Mathf.Min(count, spreadCards.Length);
            var layout = ResultSpreadLayout.Compute(used, canvasSize.y);
            for (var i = 0; i < used; i++)
            {
                PositionSpreadCell(i, layout.Cells[i], layout);
            }

            ApplyReadingLayout(layout.ReadingPosition, layout.ReadingSize);
        }

        private void Update()
        {
            RelayoutIfCanvasChanged();

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
            SetReadingAlpha(0f);
            var elapsed = 0f;
            while (elapsed < readyFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetReadingAlpha(Mathf.Clamp01(elapsed / readyFadeSeconds));
                yield return null;
            }

            SetReadingAlpha(1f);
            readyFade = null;
        }

        // Phase 67: the mode label fades in with the reading it describes.
        private void SetReadingAlpha(float alpha)
        {
            readingContentGroup.alpha = alpha;
            if (modeLabelText != null)
            {
                modeLabelText.alpha = alpha;
            }
        }

        private void PresentFrame(ReadingSessionSnapshot session)
        {
            SetText(questionText, ReadingTextSanitizer.Plain(session.question));
            SetText(spreadNameText, ReadingTextSanitizer.Plain(session.spreadName));
            PresentCards(session.cardDraws);
        }

        private void SetReadingTexts(ReadingSessionSnapshot session)
        {
            SetText(summaryText, ReadingTextSanitizer.Plain(session?.summary));
            SetText(overallText, ReadingTextSanitizer.Plain(session?.overallInterpretation));
            ApplyCardAnalysis(session?.cardAnalysis, session?.cardDraws);
            SetText(adviceText, ReadingTextSanitizer.Plain(session?.advice));
            SetText(warningText, ReadingTextSanitizer.Plain(session?.warning));

            // Phase 67 (spec C 4.6): an offline reading says so in its first line. Its warning
            // field holds that same sentence, so the 提醒 section is only for an online warning.
            var offline = session != null && session.source == ReadingSource.Offline;
            SetOfflineNotice(offline);
            SetWarningSection(session != null && !offline && !string.IsNullOrWhiteSpace(session.warning));
        }

        private void ApplyCardAnalysis(string cardAnalysis, CardDrawData[] draws)
        {
            var formatted = CardAnalysisFormatter.Build(cardAnalysis, draws);
            SetText(cardAnalysisText, formatted.RichText);
            if (readingNavigator != null)
            {
                readingNavigator.SetBlocks(formatted.Ranges);
            }
        }

        private void SetOfflineNotice(bool visible)
        {
            if (offlineNoticeText == null)
            {
                return;
            }

            offlineNoticeText.text = ReleaseUxCopy.OfflineWarning;
            offlineNoticeText.gameObject.SetActive(visible);
        }

        private void SetWarningSection(bool visible)
        {
            if (warningHeading != null)
            {
                warningHeading.SetActive(visible);
            }

            if (warningText != null)
            {
                warningText.gameObject.SetActive(visible);
            }
        }

        private void SetNavigatorInteractive(bool interactive)
        {
            if (readingNavigator != null)
            {
                readingNavigator.SetInteractive(interactive);
            }
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
            if (visible && modeLabelText != null)
            {
                modeLabelText.alpha = 1f;
            }
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

        public void Clear()
        {
            SetText(questionText, string.Empty);
            SetText(spreadNameText, string.Empty);
            SetText(summaryText, string.Empty);
            SetText(adviceText, string.Empty);
            SetText(overallText, string.Empty);
            SetText(cardAnalysisText, string.Empty);
            SetText(warningText, string.Empty);
            if (readingNavigator != null)
            {
                readingNavigator.SetBlocks(null);
            }

            SetOfflineNotice(false);
            SetWarningSection(false);
            PresentCards(null);
            SetStatus(null);
            SetText(modeLabelText, string.Empty);
            SetInterpretationButtons(false, false);
            SetNavigatorInteractive(false);
            SetReadingVisible(true);
            pendingSince = -1f;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private CardArtworkCatalog ResolveCatalog()
        {
            return cardArtworkCatalog != null
                ? cardArtworkCatalog
                : defaultArtworkCatalog ??= Resources.Load<CardArtworkCatalog>("TarotArt/RWS1909_CardArtworkCatalog");
        }

        private void PresentCards(CardDrawData[] draws)
        {
            presentedDraws = draws;
            var catalog = ResolveCatalog();
            var count = draws?.Length ?? 0;

            var hasBand = spreadBandRoot != null && spreadCards != null && spreadCards.Length > 0;
            var useSpread = hasBand && count >= 2;

            if (useSpread)
            {
                SetSingleModeActive(false);
                if (bottomDivider != null)
                {
                    // The single-card footer divider would cross the taller spread reading.
                    bottomDivider.SetActive(false);
                }

                spreadBandRoot.SetActive(true);

                // If a spread ever has more cards than the pool, the extras have nowhere to
                // go - warn rather than drop silently, so it can never regress unnoticed.
                if (count > spreadCards.Length)
                {
                    Debug.LogWarning($"ResultPanelPresenter: {count}-card spread exceeds the " +
                        $"{spreadCards.Length}-cell band; rebuild the band with more cells.");
                }

                for (var i = 0; i < spreadCards.Length; i++)
                {
                    FillSpreadCell(spreadCards[i], i < count ? draws[i] : null, catalog);
                }

                ApplyLayout(CurrentCanvasSize());
                return;
            }

            // Single-card (or empty) layout - the original hero showcase.
            if (spreadBandRoot != null)
            {
                spreadBandRoot.SetActive(false);
            }

            SetSingleModeActive(true);
            if (bottomDivider != null)
            {
                bottomDivider.SetActive(true);
            }

            ApplyReadingLayout(singleReadingPos, singleReadingSize);
            laidOutCanvasHeight = CurrentCanvasSize().y;

            var primary = count > 0 && catalog != null ? catalog.FindSprite(draws[0]) : null;
            SetArtwork(primary);
        }

        // EditMode tests and captures lay out at the reference size; at runtime the canvas
        // rect carries the real size once the scaler has run.
        private Vector2 CurrentCanvasSize()
        {
            var reference = new Vector2(TarotUiSpacing.ReferenceWidth, TarotUiSpacing.ReferenceHeight);
            if (!Application.isPlaying)
            {
                return reference;
            }

            var rect = transform as RectTransform;
            return rect != null && rect.rect.height >= 1f ? rect.rect.size : reference;
        }

        private void RelayoutIfCanvasChanged()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var size = CurrentCanvasSize();
            if (Mathf.Approximately(size.y, laidOutCanvasHeight))
            {
                return;
            }

            if (spreadBandRoot != null && spreadBandRoot.activeSelf)
            {
                ApplyLayout(size);
            }
            else
            {
                laidOutCanvasHeight = size.y;
            }
        }

        private void SetSingleModeActive(bool active)
        {
            if (singleModeRoots == null)
            {
                return;
            }

            foreach (var go in singleModeRoots)
            {
                if (go != null)
                {
                    go.SetActive(active);
                }
            }
        }

        private void FillSpreadCell(SpreadCardCell cell, CardDrawData draw, CardArtworkCatalog catalog)
        {
            if (cell == null || cell.root == null)
            {
                return;
            }

            var used = draw != null;
            cell.root.SetActive(used);
            if (!used)
            {
                return;
            }

            if (cell.artwork != null)
            {
                var sprite = catalog != null ? catalog.FindSprite(draw) : null;
                cell.artwork.sprite = sprite;
                cell.artwork.preserveAspect = true;
                cell.artwork.enabled = sprite != null;
            }

            if (cell.reversePivot != null)
            {
                cell.reversePivot.localRotation = Quaternion.Euler(0f, 0f, draw.is_reversed ? 180f : 0f);
            }

            if (cell.label != null)
            {
                cell.label.text = ReadingTextSanitizer.Plain(BuildCellLabel(draw));
            }
        }

        private void PositionSpreadCell(int index, SpreadCellPlacement placement, SpreadLayoutResult layout)
        {
            var cell = spreadCards[index];
            if (cell == null || cell.root == null)
            {
                return;
            }

            var rt = cell.root.transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            rt.anchoredPosition = placement.Position;
            var target = cellTargets != null && index < cellTargets.Length ? cellTargets[index] : null;
            if (target != null)
            {
                target.SetBaseScale(placement.Scale);
            }
            else
            {
                rt.localScale = Vector3.one * placement.Scale;
            }

            // The label is a child of the scaled cell; compensate so it keeps a fixed on-screen size.
            if (cell.label != null && placement.Scale > 0f)
            {
                cell.label.fontSize = layout.LabelFontSize / placement.Scale;
                cell.label.rectTransform.sizeDelta = new Vector2(
                    ResultSpreadLayout.BasePitch / placement.Scale, layout.LabelHeight / placement.Scale);
            }
        }

        private static string BuildCellLabel(CardDrawData draw)
        {
            var position = !string.IsNullOrWhiteSpace(draw.position_name)
                ? draw.position_name
                : (draw.tarot_card != null ? draw.tarot_card.name_zh : string.Empty);
            position ??= string.Empty;
            return draw.is_reversed ? position + ReleaseUxCopy.CardReversedMark : position;
        }

        private void ApplyReadingLayout(Vector2 pos, Vector2 size)
        {
            if (readingScrollRect == null)
            {
                return;
            }

            readingScrollRect.anchoredPosition = pos;
            readingScrollRect.sizeDelta = size;
        }

        private void SetArtwork(Sprite sprite)
        {
            if (resultCardArtworkSlot == null)
            {
                return;
            }

            resultCardArtworkSlot.sprite = sprite;
            resultCardArtworkSlot.preserveAspect = true;
            resultCardArtworkSlot.enabled = sprite != null;
        }
    }
}
```

说明：
- 已经删除的字段：`spreadReadingPos`、`spreadReadingSize`、`spreadRowWidth`、`spreadBasePitch`、`spreadMinCellScale`、`spreadCellY`（见偏差 P8）。
- 场景里残留的这些序列化数据会被 Unity 忽略，不需要清理。

- [ ] **Step 6: 运行测试，确认通过**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c6-green -testFilter TarotUnity.Tests.EditMode.Phase67ResultPresenterTests
bash "$S/run/ut.sh" PlayMode c6-green -testFilter TarotUnity.Tests.PlayMode.Phase67ResultReadingPlayTests
bash "$S/run/ut.sh" EditMode c6-full
bash "$S/run/ut.sh" PlayMode c6-full
```

预期：
- EditMode 过滤运行 `total=6 passed=6 failed=0`；
- PlayMode 过滤运行 `total=3 passed=3 failed=0`；
- 全量 EditMode `total=426 failed=0`；
- 全量 PlayMode `total=71 passed=68 failed=0 skipped=3`。

Phase 60/62/66 已有的测试必须原样通过（Phase 66 PlayMode 断言 `warningText.text == OfflineWarning`，`Plain` 对不含 `<` 的文字保持原样，所以不受影响）。

- [ ] **Step 7: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
git add Assets/Scripts/UI/ResultPanelPresenter.cs \
  Assets/Tests/EditMode/Phase67ResultPresenterTests.cs Assets/Tests/EditMode/Phase67ResultPresenterTests.cs.meta \
  Assets/Tests/PlayMode/Phase67ResultReadingPlayTests.cs Assets/Tests/PlayMode/Phase67ResultReadingPlayTests.cs.meta
git commit -F - <<'EOF'
feat(unity): lay out, sanitise and navigate the Result reading

The presenter applies ResultSpreadLayout (and re-applies it when the canvas height
changes), shows server and player text literally, formats the card analysis into
blocks the navigator can scroll to, shows the offline notice or the 提醒 section by
source, hides the footer divider on spreads, and fades the mode label with the reading.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
git log -1 --format=%B | tail -3
```

### Task 7: 生成中满 20 秒提供「查看离线解读」，并同步文档

**Files:**
- Modify: `Assets/Scripts/UI/ResultPanelPresenter.cs`（`Update`）
- Modify: `Assets/Tests/EditMode/Phase67ResultPresenterTests.cs`（新增 1 个测试和 1 条 using）
- Test: `Assets/Tests/PlayMode/Phase67SlowGenerationTests.cs`
- Modify: `docs/superpowers/specs/2026-09-12-online-interpretation-loop-design.md`（仓库根目录，7.1 表"生成中"那一行）
- Modify: `Docs/PHASE66_ONLINE_INTERPRETATION.md`（Known limits）

**Interfaces:**
- Consumes：
  - `ResultPanelPresenter.PendingSlowNoticeSeconds`（20）、`ResultPanelPresenter.ShowPending`
  - `ResultSceneController.UseOfflineInterpretation`（现有，点击离线按钮时调用 `InterpretationPoller.UseOffline()`）
  - `MockTarotBackend`、`MockTarotJson`（PlayMode 测试桩）
- Produces：
  - `public static bool ShouldOfferOfflineWhilePending(float elapsedSeconds)`
  - `public void RefreshPendingState(float elapsedSeconds)`：由 `Update` 每帧调用，截图 Builder 也调用它来渲染"满 20 秒"的状态。

- [ ] **Step 1: 写 PlayMode 测试**

创建 `Assets/Tests/PlayMode/Phase67SlowGenerationTests.cs`：

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
    /// Phase 67 (spec C, decision C3): after 20 seconds of generating, the Result screen offers
    /// 查看离线解读 together with the slow notice; picking it shows the offline reading and stops
    /// polling.
    /// </summary>
    public sealed class Phase67SlowGenerationTests
    {
        private const int PredictionId = 867;
        private const string AsyncPath = "/api/v1/records/867/interpret/async";
        private const string DetailPath = "/api/v1/records/867";

        private MockTarotBackend server;
        private GameObject boot;
        private InterpretationPoller poller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ReadingSessionStore.Clear();
            server = MockTarotBackend.Start();
            boot = new GameObject("Phase67_SlowGenerationTestBoot");
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
        public IEnumerator SlowGenerationOffersOfflineTextAndStopsPolling()
        {
            server.Script("POST", AsyncPath, MockTarotBackend.Json(202, MockTarotJson.Accepted(PredictionId)));
            server.Script("GET", DetailPath,
                MockTarotBackend.Json(200, MockTarotJson.Detail(PredictionId, "processing", 3, null)));
            var session = StartOnlineSession(3);

            yield return LoadResult();
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            var controller = Object.FindFirstObjectByType<ResultSceneController>();
            var offline = GetField<Button>(controller, "offlineInterpretationButton");
            var status = GetField<TMP_Text>(presenter, "interpretationStatusText");
            yield return WaitUntil(() => server.Count("GET", DetailPath) >= 1, 10f, "control: the reading is being polled");

            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending));
            Assert.That(offline.gameObject.activeSelf, Is.False, "control: no offline button in the first seconds");

            // Pretend generation started 21 seconds ago instead of waiting for it.
            SetPendingSince(presenter, Time.unscaledTime - (ResultPanelPresenter.PendingSlowNoticeSeconds + 1f));
            yield return null;
            yield return null;

            Assert.That(offline.gameObject.activeInHierarchy, Is.True, "查看离线解读 appears after 20 seconds");
            Assert.That(status.text, Does.Contain(ReleaseUxCopy.ResultPendingSlow));

            offline.onClick.Invoke();
            Assert.That(session.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(GetField<TMP_Text>(presenter, "offlineNoticeText").gameObject.activeInHierarchy, Is.True,
                "the offline reading says so in its first line");

            // A request already in flight may still land; after that, no more polling.
            yield return new WaitForSecondsRealtime(0.3f);
            var polled = server.Count("GET", DetailPath);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(polled), "polling stops once the player picks offline text");
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

        private static void SetPendingSince(ResultPanelPresenter presenter, float value)
        {
            var field = typeof(ResultPanelPresenter).GetField("pendingSince", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "ResultPanelPresenter.pendingSince");
            field.SetValue(presenter, value);
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

- [ ] **Step 2: 运行 PlayMode 测试，确认失败**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" PlayMode c7-red -testFilter TarotUnity.Tests.PlayMode.Phase67SlowGenerationTests
```

预期：`total=1 failed=1`，失败信息是 `查看离线解读 appears after 20 seconds`。

- [ ] **Step 3: 写 EditMode 测试**

在 `Assets/Tests/EditMode/Phase67ResultPresenterTests.cs` 中：

(a) 把

```csharp
using UnityEngine;

namespace TarotUnity.Tests.EditMode
```

替换为

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
```

(b) 把文件末尾的

```csharp
            Assert.That(scroll.sizeDelta, Is.EqualTo(new Vector2(772f, 448f)));
        }
    }
}
```

替换为

```csharp
            Assert.That(scroll.sizeDelta, Is.EqualTo(new Vector2(772f, 448f)));
        }

        [Test]
        public void PendingReadingOffersOfflineTextFromTwentySeconds()
        {
            var online = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 68, question = "问题？" }, LocalReadingSimulator.CreatePlaceholderDraws(3));
            var offline = Field<Button>("offlineInterpretationButton");
            var retry = Field<Button>("retryInterpretationButton");
            var status = Field<TMP_Text>("interpretationStatusText");

            presenter.PresentSession(online);
            Assert.That(online.interpretationState, Is.EqualTo(InterpretationState.Pending), "control: generating");

            presenter.RefreshPendingState(19.9f);
            Assert.That(offline.gameObject.activeSelf, Is.False, "no offline button before 20 seconds");
            Assert.That(status.text, Is.EqualTo(ReleaseUxCopy.ResultPending));

            presenter.RefreshPendingState(ResultPanelPresenter.PendingSlowNoticeSeconds);
            Assert.That(offline.gameObject.activeSelf, Is.True, "查看离线解读 appears with the slow notice");
            Assert.That(retry.gameObject.activeSelf, Is.False, "retry stays reserved for failures");
            Assert.That(status.text, Does.Contain(ReleaseUxCopy.ResultPendingSlow));
        }
    }
}
```

- [ ] **Step 4: 运行 EditMode 测试，确认失败**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c7-red -testFilter TarotUnity.Tests.EditMode.Phase67ResultPresenterTests
```

预期：`NO RESULTS XML`，报 `error CS1061`，指出 `ResultPanelPresenter` 没有 `RefreshPendingState`。

- [ ] **Step 5: 实现**

在 `Assets/Scripts/UI/ResultPanelPresenter.cs` 中，把

```csharp
        private void Update()
        {
            RelayoutIfCanvasChanged();

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
```

替换为

```csharp
        private void Update()
        {
            RelayoutIfCanvasChanged();

            if (pendingSince >= 0f)
            {
                RefreshPendingState(Time.unscaledTime - pendingSince);
            }
        }

        public static bool ShouldOfferOfflineWhilePending(float elapsedSeconds)
        {
            return elapsedSeconds >= PendingSlowNoticeSeconds;
        }

        /// <summary>
        /// The generating state after <paramref name="elapsedSeconds"/>: a breathing status line
        /// (spec 7.1), and from 20 s the slow notice together with 查看离线解读 (spec C, decision C3).
        /// Update drives it every frame; the capture builder calls it to render the 20-second state.
        /// </summary>
        public void RefreshPendingState(float elapsedSeconds)
        {
            if (interpretationStatusText != null)
            {
                var status = BuildPendingStatus(elapsedSeconds);
                if (interpretationStatusText.text != status)
                {
                    interpretationStatusText.text = status;
                }

                interpretationStatusText.alpha =
                    0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(elapsedSeconds * Mathf.PI * 2f / 2.4f));
            }

            if (ShouldOfferOfflineWhilePending(elapsedSeconds) && offlineInterpretationButton != null
                && !offlineInterpretationButton.gameObject.activeSelf)
            {
                SetInterpretationButtons(false, true);
            }
        }
```

- [ ] **Step 6: 同步文档**

(a) 在 `docs/superpowers/specs/2026-09-12-online-interpretation-loop-design.md`（仓库根目录）中，把

```markdown
| 生成中（在线，`Pending`） | 状态文字「牌意正在汇聚……」，带呼吸式透明度动画；进入该状态 20 秒后追加「这次解读比平时慢一些，请再稍候。」 | 回到牌桌 |
```

替换为

```markdown
| 生成中（在线，`Pending`） | 状态文字「牌意正在汇聚……」，带呼吸式透明度动画；进入该状态 20 秒后追加「这次解读比平时慢一些，请再稍候。」 | 回到牌桌；满 20 秒后与慢提示一起出现「查看离线解读」（子项目 C 决策 C3，Phase 67） |
```

(b) 在 `Docs/PHASE66_ONLINE_INTERPRETATION.md` 中，把

```markdown
- While the interpretation is generating, the Result screen offers only 回到牌桌; the
  offline text becomes reachable only once generation fails (spec 7.1 table). Whether to
  offer 查看离线解读 during generation is an open product decision.
```

替换为

```markdown
- While the interpretation is generating, the Result screen offers 回到牌桌 at first;
  after 20 seconds 查看离线解读 appears together with the slow notice (Phase 67,
  sub-project C decision C3).
```

- [ ] **Step 7: 运行测试，确认通过**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c7-green -testFilter TarotUnity.Tests.EditMode.Phase67ResultPresenterTests
bash "$S/run/ut.sh" PlayMode c7-green -testFilter "TarotUnity.Tests.PlayMode.Phase67SlowGenerationTests;TarotUnity.Tests.PlayMode.Phase66ResultInterpretationStateTests"
bash "$S/run/ut.sh" EditMode c7-full
bash "$S/run/ut.sh" PlayMode c7-full
```

预期：
- EditMode 过滤运行 `total=7 passed=7 failed=0`；
- PlayMode 过滤运行 `total=3 passed=3 failed=0`（Phase 66 的生成中/失败流程不受影响）；
- 全量 EditMode `total=427 failed=0`；
- 全量 PlayMode `total=72 passed=69 failed=0 skipped=3`。

- [ ] **Step 8: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git add UnityClient/TarotUnity/Assets/Scripts/UI/ResultPanelPresenter.cs \
  UnityClient/TarotUnity/Assets/Tests/EditMode/Phase67ResultPresenterTests.cs \
  UnityClient/TarotUnity/Assets/Tests/PlayMode/Phase67SlowGenerationTests.cs \
  UnityClient/TarotUnity/Assets/Tests/PlayMode/Phase67SlowGenerationTests.cs.meta \
  UnityClient/TarotUnity/Docs/PHASE66_ONLINE_INTERPRETATION.md \
  docs/superpowers/specs/2026-09-12-online-interpretation-loop-design.md
git commit -F - <<'EOF'
feat(unity): offer the offline reading after 20 seconds of generating

Decision C3 of the result reading design: 查看离线解读 now appears together with the
slow-generation notice, so a long wait no longer holds the player on the table
button. Picking it shows the offline reading and stops polling. The online loop
spec and the Phase 66 known limits are updated to match.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
git log -1 --format=%B | tail -3
```

### Task 8: 截图、框外像素检查、文档与全量回归

**Files:**
- Create: `Assets/Editor/Phase67ResultReadingCaptureBuilder.cs`
- Create: `Docs/PHASE67_RESULT_READING.md`
- Create: `Docs/VisualReview/Phase67/*.png`（10 张）
- Modify: `Docs/PROJECT_CHRONICLE.md`（文件末尾追加 Phase 67 条目）
- Modify: `Assets/Tests/EditMode/Phase67ResultSceneStructureTests.cs`（新增 1 个测试和 1 条 using）

**Interfaces:**
- Consumes：
  - `ResultPanelPresenter.PresentSession`、`ApplyLayout`、`RefreshPendingState`
  - `ResultCanvasAspectFit.Apply`、`Repin`
  - `InterpretationPoller.ApplyFailure`、`ApplyOffline`
  - `CaptureRig.RenderConverged(Camera, int warmupRenders = default)`（命名空间 `TarotUnity.Editor`）
- Produces：
  - 10 张审查截图；
  - 每张截图输出一行日志：`Phase 67 capture <file>: canvas=WxH readingHeight=… belowFrameIvoryPixels=N`；
  - 画布高度不符合预期，或框外出现文字像素时，Builder 直接抛异常。

- [ ] **Step 1: 写失败的文档/截图测试**

在 `Assets/Tests/EditMode/Phase67ResultSceneStructureTests.cs` 中：

(a) 把 `using System.Linq;` 替换为

```csharp
using System.IO;
using System.Linq;
```

(b) 把文件末尾的

```csharp
            Assert.That(so.FindProperty("cellTargets").arraySize, Is.EqualTo(10));
        }
    }
}
```

替换为

```csharp
            Assert.That(so.FindProperty("cellTargets").arraySize, Is.EqualTo(10));
        }

        [Test]
        public void Phase67DocumentationAndScreenshotsExist()
        {
            const string docPath = "Docs/PHASE67_RESULT_READING.md";
            Assert.That(File.Exists(docPath), Is.True, $"Missing Phase 67 doc at {docPath}");
            var doc = File.ReadAllText(docPath);
            Assert.That(doc, Does.Contain("ResultSpreadLayout"));
            Assert.That(doc, Does.Contain("CardAnalysisParser"));
            Assert.That(doc, Does.Contain("noparse"));
            Assert.That(File.ReadAllText("Docs/PROJECT_CHRONICLE.md"), Does.Contain("### Phase 67"));

            var shots = new[]
            {
                "Result_1card_16x9.png", "Result_3card_16x9.png", "Result_5card_16x9.png", "Result_10card_16x9.png",
                "Result_3card_16x10.png", "Result_10card_16x10.png", "Result_3card_4x3.png",
                "Result_pending20s.png", "Result_failed.png", "Result_offline.png",
            };
            foreach (var file in shots)
            {
                var path = Path.Combine("Docs/VisualReview/Phase67", file);
                Assert.That(File.Exists(path), Is.True, $"Missing review shot {path}");
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(4096), $"{path} is unexpectedly small");
            }
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c8-red -testFilter TarotUnity.Tests.EditMode.Phase67ResultSceneStructureTests.Phase67DocumentationAndScreenshotsExist
```

预期：`total=1 failed=1`，失败信息为 `Missing Phase 67 doc at Docs/PHASE67_RESULT_READING.md`。

- [ ] **Step 3: 创建 `Assets/Editor/Phase67ResultReadingCaptureBuilder.cs`**

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
    /// Phase 67 review shots of the Result reading: 1/3/5/10 cards at 16:9, 3 and 10 cards at
    /// 16:10, 3 cards at 4:3, and the generating (20 s), failed and offline states.
    /// READ-ONLY: it presents sample snapshots and renders; the scene is not saved. Each shot
    /// checks the canvas really took the expected height and fails if any ivory body-text pixel
    /// appears in the rows around the reading frame's bottom edge. Set PHASE67_CAPTURE_DIR to
    /// render a review round somewhere other than Docs.
    /// </summary>
    public static class Phase67ResultReadingCaptureBuilder
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string DefaultOutFolder = "Docs/VisualReview/Phase67";
        private const int BelowFrameRows = 12;    // pixels either side of the frame's bottom edge (2 px per canvas unit)
        private const int CornerMargin = 40;      // skip the rounded frame corners

        private static readonly string[] CelticNames =
        {
            "现状", "挑战", "根基", "过去", "顶冠", "未来", "自我", "环境", "希望与恐惧", "结果",
        };

        private sealed class Shot
        {
            public Shot(string file, int width, int height, float expectedCanvasHeight, Action<ResultPanelPresenter> present)
            {
                File = file;
                Width = width;
                Height = height;
                ExpectedCanvasHeight = expectedCanvasHeight;
                Present = present;
            }

            public string File { get; }
            public int Width { get; }
            public int Height { get; }
            public float ExpectedCanvasHeight { get; }
            public Action<ResultPanelPresenter> Present { get; }
        }

        [MenuItem("Tools/Tarot Unity/Run Phase 67 Result Reading Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            var outFolder = Environment.GetEnvironmentVariable("PHASE67_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(outFolder))
            {
                outFolder = DefaultOutFolder;
            }

            var shots = BuildShots();
            Directory.CreateDirectory(outFolder);
            foreach (var shot in shots)
            {
                if (File.Exists(Path.Combine(outFolder, shot.File)))
                {
                    throw new InvalidOperationException($"{outFolder}/{shot.File} already exists; captures never overwrite.");
                }
            }

            foreach (var shot in shots)
            {
                Capture(outFolder, shot);
            }

            Debug.Log($"Phase 67 result reading capture complete -> {outFolder}");
        }

        private static Shot[] BuildShots()
        {
            return new[]
            {
                new Shot("Result_1card_16x9.png", 2560, 1440, 720f, p => p.PresentSession(Offline(1))),
                new Shot("Result_3card_16x9.png", 2560, 1440, 720f, p => p.PresentSession(Ready())),
                new Shot("Result_5card_16x9.png", 2560, 1440, 720f, p => p.PresentSession(Offline(5))),
                new Shot("Result_10card_16x9.png", 2560, 1440, 720f, p => p.PresentSession(Offline(10))),
                new Shot("Result_3card_16x10.png", 2560, 1600, 800f, p => p.PresentSession(Ready())),
                new Shot("Result_10card_16x10.png", 2560, 1600, 800f, p => p.PresentSession(Offline(10))),
                new Shot("Result_3card_4x3.png", 2560, 1920, 960f, p => p.PresentSession(Ready())),
                new Shot("Result_pending20s.png", 2560, 1440, 720f, p =>
                {
                    p.PresentSession(Online());
                    p.RefreshPendingState(ResultPanelPresenter.PendingSlowNoticeSeconds + 1f);
                }),
                new Shot("Result_failed.png", 2560, 1440, 720f, p =>
                {
                    var session = Online();
                    InterpretationPoller.ApplyFailure(session, InterpretationFailure.ConnectionLost);
                    p.PresentSession(session);
                }),
                new Shot("Result_offline.png", 2560, 1440, 720f, p =>
                {
                    var session = Online();
                    InterpretationPoller.ApplyOffline(session);
                    p.PresentSession(session);
                }),
            };
        }

        private static ReadingSessionSnapshot Offline(int cardCount)
        {
            var draws = cardCount == 10
                ? LocalReadingSimulator.CreatePlaceholderDraws(10, CelticNames, null)
                : LocalReadingSimulator.CreatePlaceholderDraws(cardCount);
            var spreadName = cardCount == 10 ? "凯尔特十字" : cardCount == 1 ? "单张牌" : "牌阵";
            return LocalReadingSimulator.CreateSession(2, spreadName, "我这段关系的整体走向？", "general", draws);
        }

        private static ReadingSessionSnapshot Online()
        {
            var session = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse
                {
                    id = 67,
                    spread_type_id = 2,
                    question = "我接下来最该把力气放在哪里？",
                    question_type = "general",
                },
                LocalReadingSimulator.CreatePlaceholderDraws(3));
            session.spreadName = "过去现在未来";
            return session;
        }

        private static ReadingSessionSnapshot Ready()
        {
            var session = Online();
            ReadingSessionMapper.ApplyInterpretation(session, new InterpretationResponse
            {
                id = 1,
                summary = "旧的节奏正在松动，新的方向需要你亲手确认。",
                overall_interpretation = "过去的积累给了你底气，眼下的犹豫来自选择太多。把注意力收回到一件真正重要的事上，" +
                    "局面会比想象中更快清晰。接下来的几周适合收拢精力，先完成手头最关键的一步，再决定要不要扩展。",
                card_analysis = "过去：愚者（正位）— 敢于开始的勇气仍在，旧的节奏正在松动。\n" +
                    "现在：魔术师（正位）— 资源齐备，关键在于把注意力收回到一件事上。\n" +
                    "建议：女祭司（逆位）— 别只听外界的声音，给直觉留一点安静。",
                advice = "这一周只定一个目标，每天为它做一件小事。",
                warning = "解读仅供参考，重要决定请结合现实情况。",
                model_used = "deepseek-chat",
            });
            return session;
        }

        private static void Capture(string outFolder, Shot shot)
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var canvasObject = GameObject.Find("ResultCanvas");
            var presenter = canvasObject != null ? canvasObject.GetComponent<ResultPanelPresenter>() : null;
            var fit = canvasObject != null ? canvasObject.GetComponent<ResultCanvasAspectFit>() : null;
            var scaler = canvasObject != null ? canvasObject.GetComponent<CanvasScaler>() : null;
            var camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (presenter == null || fit == null || scaler == null || camera == null)
            {
                throw new InvalidOperationException("Result presenter, aspect fit, canvas scaler or camera not found.");
            }

            var canvasRect = (RectTransform)canvasObject.transform;
            var scroll = (RectTransform)canvasObject.transform.Find("ResultReadingScroll");
            var rt = new RenderTexture(shot.Width, shot.Height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(shot.Width, shot.Height, TextureFormat.RGBA32, false);
            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;
            var prevAspect = camera.aspect;
            var states = PrepareCanvases(camera);

            try
            {
                camera.aspect = (float)shot.Width / shot.Height;
                camera.targetTexture = rt;
                RenderTexture.active = rt;

                // Size the canvas for this shot, then lay the reading out for that size.
                fit.Apply(shot.Width, shot.Height);
                scaler.enabled = false;
                scaler.enabled = true;
                Canvas.ForceUpdateCanvases();

                shot.Present(presenter);

                var canvasSize = canvasRect.rect.size;
                if (Mathf.Abs(canvasSize.y - shot.ExpectedCanvasHeight) > 1f)
                {
                    throw new InvalidOperationException(
                        $"{shot.File}: canvas is {canvasSize.x}x{canvasSize.y}, expected height {shot.ExpectedCanvasHeight}.");
                }

                fit.Repin(canvasSize.y);
                presenter.ApplyLayout(canvasSize);
                RebuildReading(canvasObject);
                Canvas.ForceUpdateCanvases();

                CaptureRig.RenderConverged(camera);
                tex.ReadPixels(new Rect(0, 0, shot.Width, shot.Height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(outFolder, shot.File), tex.EncodeToPNG());

                var ivory = CountIvoryAroundFrameBottom(tex, camera, scroll);
                Debug.Log($"Phase 67 capture {shot.File}: canvas={canvasSize.x:0}x{canvasSize.y:0} " +
                    $"readingHeight={scroll.rect.height:0.0} belowFrameIvoryPixels={ivory}");
                if (ivory > 0)
                {
                    throw new InvalidOperationException($"{shot.File}: {ivory} body-text pixels at the reading frame's bottom edge.");
                }
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

        private static void RebuildReading(GameObject canvasObject)
        {
            var content = canvasObject.transform.Find("ResultReadingScroll/Viewport/Content") as RectTransform;
            if (content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }

            // RectMask2D caches its clip rect; toggle it so the capture clips to the layout just
            // applied (Phase 60 lesson).
            foreach (var mask in UnityEngine.Object.FindObjectsByType<RectMask2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                mask.enabled = false;
                mask.enabled = true;
            }
        }

        // Body text is ivory (0.92, 0.88, 0.78); the gold frame lines, gold headings and the dark
        // backdrop all fail the blue threshold. The rows checked run from 6 canvas units inside the
        // frame's bottom edge (its transparent margin, below the outer gold line) to 6 units below it.
        private static int CountIvoryAroundFrameBottom(Texture2D tex, Camera camera, RectTransform frame)
        {
            var corners = new Vector3[4];
            frame.GetWorldCorners(corners);
            var bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var bottomRight = RectTransformUtility.WorldToScreenPoint(camera, corners[3]);
            var bottom = Mathf.RoundToInt(bottomLeft.y);
            var left = Mathf.RoundToInt(bottomLeft.x) + CornerMargin;
            var right = Mathf.RoundToInt(bottomRight.x) - CornerMargin;

            var count = 0;
            for (var y = Mathf.Max(0, bottom - BelowFrameRows); y <= Mathf.Min(tex.height - 1, bottom + BelowFrameRows); y++)
            {
                for (var x = Mathf.Max(0, left); x <= Mathf.Min(tex.width - 1, right); x++)
                {
                    var pixel = tex.GetPixel(x, y);
                    if (pixel.r > 0.8f && pixel.g > 0.75f && pixel.b > 0.62f)
                    {
                        count++;
                    }
                }
            }

            return count;
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

- [ ] **Step 4: 第一轮截图（输出到 scratch 目录，仅供审查）**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
UNITY=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
PROJECT=/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
LOG="$S/run/results/c8-capture-round1.log"
if pgrep -f 'Unity.app/Contents/MacOS/Unity' >/dev/null; then echo "STOP: Unity is running"; exit 1; fi
test ! -e "$LOG" || { echo "STOP: $LOG exists - use a new name"; exit 1; }
test ! -e "$S/captures/round1" || { echo "STOP: $S/captures/round1 exists - use round2"; exit 1; }
ulimit -n 10240 2>/dev/null || ulimit -n 4096 2>/dev/null || true
PHASE67_CAPTURE_DIR="$S/captures/round1" "$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase67ResultReadingCaptureBuilder.Run -quit -logFile "$LOG"
echo "unity exit=$?"
grep -n "Phase 67 capture\|Exception" "$LOG" | tail -n 24
ls -l "$S/captures/round1"
```

预期：`unity exit=0`；10 行 `Phase 67 capture …`，每行都是 `belowFrameIvoryPixels=0`。画布和阅读区高度应为：

| 截图 | 画布 | 阅读区高度（约） |
| --- | --- | --- |
| `Result_1card_16x9` | 1280x720 | 448.0（单张牌布局） |
| `Result_3card_16x9`、`Result_5card_16x9`、`Result_offline` | 1280x720 | 332.4 |
| `Result_10card_16x9` | 1280x720 | 293.1 |
| `Result_3card_16x10` | 1280x800 | 412.4 |
| `Result_10card_16x10` | 1280x800 | 373.1 |
| `Result_3card_4x3` | 1280x960 | 572.4 |

- 如果日志出现 `canvas is …, expected height …`：说明截图时画布没有改变尺寸，这是测量工具本身的问题，不是布局问题。STOP 并报告，不要改掉这条检查。
- 如果出现 `body-text pixels at the reading frame's bottom edge`：说明框外有字。STOP 并报告是哪张图、多少像素。

- [ ] **Step 5: 逐张审查第一轮截图**

用 Read 工具打开 `$S/captures/round1/` 下的 10 张图，逐项核对：
- `Result_1card_16x9`：
  - 正文离左侧和上方金线都有明显距离，框下方没有半行字；
  - 第一行是离线提示；
  - 页脚分隔线在阅读框下方。
- `Result_3card_16x9`、`Result_5card_16x9`：
  - 牌比 `Docs/VisualReview/Phase66/Result_ready.png` 小，阅读区明显更高；
  - 位置标签清晰；
  - 文字较长时右侧有细滚动条，底部有渐隐。
- `Result_3card_16x9`（在线已完成）：牌面分析显示为三块，每块有金色小标题，例如「过去 · 愚者（正位）」。
- `Result_10card_16x9`、`Result_10card_16x10`：两行各 5 张牌，位置标签能读清。
- `Result_3card_16x10`、`Result_3card_4x3`：标题贴顶部，按钮贴底部，多出的高度都在阅读区。
- `Result_pending20s`：状态行包含"这次解读比平时慢一些"，右侧有「查看离线解读」按钮。
- `Result_failed`：有「重新解读」和「查看离线解读」两个按钮。
- `Result_offline`：第一行是离线提示。

发现视觉问题时，只允许调整下面两类常量：
- `Phase67ResultReadingBootstrapper` 的内缩、内边距、间距、颜色；
- `ResultSpreadLayout` 的缩放和间隙。

调整的前提是 Task 2 的测试不改一个字仍然通过。调整后依次：
1. 重新执行 Bootstrapper（换新的日志名）；
2. 运行 `Phase67ResultLayoutTests`、`Phase67ResultSceneStructureTests`、`Phase67ResultPresenterTests`；
3. 单独提交：`fix(unity): tune the Phase 67 reading layout after capture review`，只 add 改动过的文件和 `Assets/Scenes/Result.unity`；
4. 用 `round2` 重新截图，再审查一遍。

超出这个范围的问题，STOP 并报告有问题的图片名。

- [ ] **Step 6: 正式截图到 Docs**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
UNITY=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
PROJECT=/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
LOG="$S/run/results/c8-capture-final.log"
if pgrep -f 'Unity.app/Contents/MacOS/Unity' >/dev/null; then echo "STOP: Unity is running"; exit 1; fi
test ! -e "$LOG" || { echo "STOP: $LOG exists - use a new name"; exit 1; }
ulimit -n 10240 2>/dev/null || ulimit -n 4096 2>/dev/null || true
"$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase67ResultReadingCaptureBuilder.Run -quit -logFile "$LOG"
echo "unity exit=$?"
grep -n "Phase 67 capture\|Exception" "$LOG" | tail -n 24
ls -l "$PROJECT/Docs/VisualReview/Phase67"
```

预期：与 Step 4 相同的 10 行日志（`belowFrameIvoryPixels=0`），`Docs/VisualReview/Phase67/` 下有 10 张 PNG。

- [ ] **Step 7: 写文档和编年史**

创建 `Docs/PHASE67_RESULT_READING.md`：

```markdown
# Phase 67 — Result reading experience

Spec: `docs/superpowers/specs/2026-09-13-result-reading-experience-design.md` (sub-project C).

## What changed

- **Multi-card layout.** `ResultSpreadLayout` hangs the card band under the header (one row up to five
  cards, two rows of five for ten) and gives the reading panel everything down to the button row. At
  16:9 the reading panel is 332 tall for three to five cards (it was 232) and 293 for ten; a 16:10
  screen adds 80.
- **Frame.** The viewport is inset 24 inside the `TarotPanel` frame, whose inner gold line ends 18 in,
  so text no longer touches the border or shows below it. The single-card panel widened to 772 (right
  edge unchanged) to keep its 700-wide reading column.
- **Scroll cues.** A thin gold scrollbar (auto-hide) and a bottom fade that disappears at the end of
  the reading (`ResultReadingNavigator`, `ReadingFadeGradient`).
- **Per-card blocks.** `CardAnalysisParser` splits `card_analysis` by position name, or by line order
  when the counts match, and `CardAnalysisFormatter` writes a gold heading per card from the client's
  own card data. Clicking a card scrolls to its block and glows the heading (`ResultSpreadCellTarget`).
  Text that cannot be split shows as one block, as before. The backend prompt now asks for one line per
  card.
- **Safe display.** Server and player text goes through `ReadingTextSanitizer`: text containing `<` is
  wrapped in `noparse`, and each field is capped at 4000 characters.
- **Notices.** An offline reading shows the offline notice as its first line on every layout; an
  online AI warning is a 提醒 section after 建议 on every layout.
- **Generating.** After 20 seconds, 查看离线解读 appears together with the slow notice.
- **Aspect.** `ResultCanvasAspectFit` matches width on screens narrower than 16:9 and height on wider
  ones, and pins the header and the button row to the canvas edges at runtime.
- **Reveal.** Section headings, the mode label and the card band fade in with their sections
  (`ResultRevealDirector` companions).

## Review shots

`Docs/VisualReview/Phase67/`: one, three, five and ten cards at 16:9; three and ten cards at 16:10;
three cards at 4:3; and the generating (20 s), failed and offline states. The capture builder checks
the canvas took the expected height and fails if body text appears at the reading frame's bottom edge.

## Known limits

- Card hover and click-to-scroll are pointer-only; there is no keyboard or gamepad navigation.
- An AI answer that ignores the one-line-per-card format shows as a single block.
- Only the Result screen adapts to 16:10 and 4:3; the reading room and the main menu keep the shared
  0.5 match factor.
- The Phase 60 and Phase 62 bootstrappers must not be re-run: they rebuild the band and would drop the
  Phase 67 wiring.
```

在 `Docs/PROJECT_CHRONICLE.md` 中，把

```markdown
instead of the table waiting on the AI. See `Docs/PHASE66_ONLINE_INTERPRETATION.md`.
```

替换为

```markdown
instead of the table waiting on the AI. See `Docs/PHASE66_ONLINE_INTERPRETATION.md`.

### Phase 67 — Result reading experience

The Result screen sizes its card band by card count (two rows for ten cards) and gives the
reading panel the rest of the screen. Text clears the gold frame, scroll cues show when more
is below, the card analysis splits into per-card blocks the cards jump to, and AI text is
shown literally. See `Docs/PHASE67_RESULT_READING.md`.
```

- [ ] **Step 8: 运行测试与全量回归**

```bash
S=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-13-result-reading-experience/scratch
bash "$S/run/ut.sh" EditMode c8-green -testFilter TarotUnity.Tests.EditMode.Phase67ResultSceneStructureTests.Phase67DocumentationAndScreenshotsExist
bash "$S/run/ut.sh" EditMode c8-full
bash "$S/run/ut.sh" PlayMode c8-full
bash "$S/run/pt.sh" tests 2>&1 | tail -n 1
bash "$S/run/pt.sh" . 2>&1 | tail -n 1
```

预期：
- 过滤运行 `total=1 passed=1 failed=0`；
- 全量 EditMode `total=428 failed=0`；
- 全量 PlayMode `total=72 passed=69 failed=0 skipped=3`；
- 后端两次都是 `190 passed`。

- [ ] **Step 9: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
git add Assets/Editor/Phase67ResultReadingCaptureBuilder.cs Assets/Editor/Phase67ResultReadingCaptureBuilder.cs.meta \
  Assets/Tests/EditMode/Phase67ResultSceneStructureTests.cs \
  Docs/PHASE67_RESULT_READING.md Docs/PROJECT_CHRONICLE.md Docs/VisualReview/Phase67
git commit -F - <<'EOF'
docs(unity): capture and document the Phase 67 result reading

Ten review shots cover one to ten cards, 16:9, 16:10 and 4:3, and the generating,
failed and offline states. The capture builder asserts the canvas height and fails
on body text at the reading frame's bottom edge. The phase doc and the chronicle
record the change.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
EOF
git log -1 --format=%B | tail -3
git status --short
git log --oneline e56d38d..HEAD
```

预期：
- `git status --short` 只剩字体图集，以及未跟踪的 `Phase66LiveBackendTests.cs`（含 `.meta`）；
- `git log` 列出 Task 1–8 的提交（如果 Step 5 做过调整，还有那次调整的提交）。

---

## 测试数量总表

| 任务结束时 | EditMode | PlayMode | 后端 |
| --- | --- | --- | --- |
| Task 0（基线） | 370 | 68（65 + 3 skipped） | 188 |
| Task 1 | 385（+15） | 68 | 188 |
| Task 2 | 403（+18） | 68 | 188 |
| Task 3 | 403 | 68 | 190（+2） |
| Task 4 | 413（+10） | 68 | 190 |
| Task 5 | 420（+7） | 68 | 190 |
| Task 6 | 426（+6） | 71（68 + 3 skipped） | 190 |
| Task 7 | 427（+1） | 72（69 + 3 skipped） | 190 |
| Task 8 | 428（+1） | 72（69 + 3 skipped） | 190 |

"3 skipped" 是未跟踪的 `Phase66LiveBackendTests`。如果执行前子项目 A 的 Task 9 已经把它提交到 A 的分支，本分支上的数字仍然一样。

## 自检记录（写计划时完成）

**spec 覆盖：**

| spec 条目 | 实现位置 |
| --- | --- |
| 4.1 多牌布局、按牌数分配、10 张两行、标签固定字号 | Task 2（`ResultSpreadLayout`）、Task 6（presenter 应用布局并补偿标签字号）；偏差 P3 |
| 4.1 单张牌构图不变 | Task 5（宽 772，右边缘不变）；偏差 P1 |
| 4.2 视口内缩、内边距、段落层次 | Task 5；偏差 P6 |
| 4.2 框外不露字 | Task 5（内缩 24）、Task 8（截图像素检查） |
| 4.2 滚动条、底部渐隐 | Task 4（`ReadingFadeGradient`、`ResultReadingNavigator`）、Task 5（接线）、Task 6（PlayMode 验证） |
| 4.3 按牌分块、解析三层规则、客户端小标题 | Task 1（Parser/Formatter）、Task 6（presenter 与 TMP 下标对齐测试） |
| 4.3 后端提示词格式 | Task 3 |
| 4.4 点牌定位、高亮、悬停、解析失败时退回标题 | Task 4（Navigator、CellTarget）、Task 5（每个牌格挂点击目标）、Task 6（PlayMode） |
| 4.5 纯文本显示、4000 字上限 | Task 1、Task 6（问题、牌阵名、各段、牌格标签）；偏差 P4 |
| 4.6 离线提示首行、「提醒」段 | Task 5（对象）、Task 6（按来源显示或隐藏） |
| 4.6 模式标签随正文淡入，标题和牌列随揭示淡入 | Task 4（伴随组）、Task 5（接线）、Task 6（淡入与 PlayMode 验证）；偏差 P7 |
| 4.6 生成中满 20 秒出现离线按钮，并同步文档 | Task 7 |
| 4.7 宽高比适配、上下沿钉住 | Task 4、Task 5、Task 6（画布高度变化时重新布局）、Task 8（16:10 和 4:3 截图）；偏差 P2 |
| 6.1–6.3 测试与截图 | Task 1、2、4、5、6、7、8 |
| 6.4 旧守护测试 | Task 5（Phase 29 改为相对顺序）；其余旧守护测试在 Task 5、6、7 的全量回归中原样通过 |
| 7 完成标准 1–3 | Task 8 Step 8 全量回归、Step 5 截图审查，Task 6/7 的 PlayMode 测试 |
| 7 完成标准 4（真实 AI 牌面分析能分块） | 不在本计划内：需要子项目 A 的 Task 9 真实联调结果，收尾时向用户说明 |

**与 spec 核对时发现的问题：**
- spec 6.4 预计要修改 Phase 66 里断言"生成中两个按钮都隐藏"的测试。实际核对：
  - `PresenterShowsEachInterpretationState` 在生成中那一段没有断言按钮；
  - `ResultSceneHasInterpretationStateUiWired` 只断言场景保存时按钮是隐藏的，Bootstrapper 不改变这一点；
  - 所以没有需要修改的 Phase 66 测试，Task 7 只新增测试。
- spec 4.2 写的内边距（28/28/20/36）和 4.1 写的牌高（130/76）与实测几何冲突，按 spec 自己的规定"坐标可以调整，验收条件不能放宽"处理，见偏差 P1、P3、P6。

**占位符检查：** 全文没有 TBD、TODO、"类似 Task N" 或只有描述没有代码的步骤。每个代码步骤都给出完整文件，或精确的替换前后文本。

**类型一致性：**
- `CardBlockRange` 的 `CardIndex`、`HeadingStart`、`HeadingLength`：Task 1 定义，Task 4 Navigator、Task 6 测试使用。
- `SpreadLayoutResult`：`Cells`、`Rows`、`LabelFontSize`、`LabelHeight`、`BandBottom`、`ReadingPosition`、`ReadingSize`、`ReadingTop`、`ReadingBottom`。
- `ResultSpreadLayout.CellFrameTop`、`CellFrameHalfWidth`、`BasePitch`、`BandReadingGap`。
- `ResultReadingNavigator` 的 `SetBlocks`、`SetInteractive`、`FocusCard`、`Blocks`、`IsInteractive`、`IsFadeVisible`、`HighlightedCard`。
- `ResultSpreadCellTarget` 的 `SetBaseScale`、`Activate`、`CardIndex`、`IsHovered`。
- `ResultCanvasAspectFit` 的 `MatchFor`、`PinnedY`、`Apply`、`Repin`、`Edge`、`PinnedElement`。
- `ResultRevealDirector.RevealCompanion` 的 `groupIndex`、`group`，以及字段 `companions`。
- presenter 的 `ApplyLayout`、`RefreshPendingState`、`ShouldOfferOfflineWhilePending`，以及字段 `readingNavigator`、`offlineNoticeText`、`warningHeading`、`bottomDivider`、`cellTargets`。

以上名称在各任务之间一致。

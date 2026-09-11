# Tarot 仓库归一 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把旧后端仓库 `Tarot@f19c7cd` 中的 80 个后端文件，以一次纯搬运提交放进 `UnityTarot/Server/`；消除两个无关仓库共用同一个远端带来的误推风险；用可以重复执行的证据证明搬运没有改动任何内容。

**Architecture:** 在 `UnityTarot` 的 `chore/unify-repo` 分支上按"忽略规则 → 纯搬运 → CI → 文档"的顺序做 5 个提交。所有验证都在会话 scratchpad 里完成，包括一份完整的对照组导出和几个验证脚本，这些都不进任何仓库；Python 环境复用本机现有的 conda 环境 `tarot`（只读使用）。旧仓库 `Tarot/` 只改 `.git/config` 里的两项。

**Tech Stack:** git 2.50.1（需要 `ls-tree --format` 和 `ls-files --format`）、bash 3.2（macOS `/bin/bash`，用来执行验证脚本）、zsh（交互 shell）、Python 3.11.15（conda 环境 `tarot`）、pytest 8.4.1、FastAPI 0.111、Alembic 1.13、GitHub Actions。

**Spec:** `docs/superpowers/specs/2026-09-11-repo-unification-design.md`

## Global Constraints

- 源提交：`f19c7cdaff954d71d602d99bc3a0e8f54c5340dc`（`Tarot/` 仓库，与远端 `backend-main` 相同）
- 搬运集合：`app tests alembic data alembic.ini requirements.txt setup.py Dockerfile .dockerignore .env.example check_config.py start_with_utf8.py`，恰好 80 个已跟踪文件，权限全部是 `100644`，没有 CRLF
- 产品仓库：`/Users/maochuandou/BUPT/Game/UnityTarot`，工作分支 `chore/unify-repo`，从 `main`@`4ec3816` 切出
- 禁止 force push，禁止改写任何已有提交，禁止 `git push`（推送前必须征得用户同意）
- 禁止删除任何文件（包括 scratchpad 里的文件）。不要覆盖任何未跟踪文件或 scratch 文件：创建前先确认目标不存在，用 heredoc 写文件前先执行 `set -C`（noclobber）
- 允许修改的已跟踪文件只有三个，都能从 `main` 恢复：`.gitignore`（追加）、`README.md`（重写）、`PROJECT_COMPLETION_PLAN.md`（三处替换）
- 不改动 `UnityClient/` 下的任何文件
- `Tarot/` 目录只允许修改 `.git/config` 中的 upstream 和 push URL 两项
- 远端 `backend-main` 必须保持为 `f19c7cd`
- 验证环境：复用 conda 环境 `/opt/miniconda3/envs/tarot`（即 `$VENV`），两次测试共用；**不得向该环境安装、升级或卸载任何包**；运行时设置 `PYTHONDONTWRITEBYTECODE=1` 并加 `-p no:cacheprovider`
- 在 zsh 中传递路径列表必须写成数组 `"${MOVE[@]}"`，因为 zsh 不会对 `$VAR` 分词。验证脚本一律用 `bash <script>` 执行
- 在 zsh 中，变量后面紧跟冒号时必须加花括号，写成 `${SRC_COMMIT}:path`。zsh 会把 `$VAR:r`、`:h`、`:t`、`:e` 等当成修饰符：`"$SRC_COMMIT:requirements.txt"` 会变成 `f19c7cd…equirements.txt`（执行 Task 2 时实际踩到过）
- 每个提交信息都以下面两行结尾：
  ```
  Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
  ```
- 遇到 **STOP** 就停止执行，并把输出原样报告给用户，不要自行绕过

## 与 spec 的偏差（写计划时已核实，执行以本计划为准）

| 编号 | spec 原文 | 本计划 | 原因 |
| --- | --- | --- | --- |
| P1 | 提交顺序：导入 → CI → 忽略规则 | 忽略规则 → 导入 → CI → 文档 | 忽略规则先生效，再执行 `git add Server/`，`.env` 和数据库文件就不会被暂存 |
| P2 | Phase 0 整体标记为完成 | Phase 0 按任务逐项写状态 | 任务 6（版本 tag）在远端还没有任何 tag，整体标完成与事实不符 |
| P3 | 执行记录追加到 2.3 | 新增小节 `2.4 本轮执行记录（2026-09-11）` | 2.3 的标题日期是 2026-08-27，09-11 的记录不应写在它下面 |
| P4 | 提交 1 只包含 spec | 提交 1 同时包含 spec 和本计划 | 两份文档一起入库 |
| P5 | pytest 只跑一次 | 分别跑 CI 的两条命令：`pytest -q tests` 和 `pytest -q` | `app/scripts/` 下有 `test_ai_integration.py`、`test_spread_interpretation.py`，只有不带路径的第二条命令会收集到它们 |
| P6 | 文档没有运行验证 | 文档任务增加 README 命令冒烟（在 scratchpad 副本中运行，端口 8765） | 证明 `Server/README.md` 里的命令确实能跑通 |
| P7 | 在 scratchpad 新建 venv；依赖装不上时改用 conda 3.10 | 复用现有 conda 环境 `/opt/miniconda3/envs/tarot`（Python 3.11.15），只读使用 | 用户在执行 Task 2 前决定（2026-09-11）。该环境中 16 个固定依赖的版本与 `requirements.txt` 完全一致，`pip check` 无冲突；不需要联网安装，scratchpad 也不新增环境目录。代价：它不是全新环境，未声明的依赖要到 GitHub CI 首次运行时才会暴露，但不影响搬运前后一致性的比对 |
| P8 | 用 `.outcomes` 比对测试结果 | 另用 `extract_ids.sh` 生成 `.ids`，比对 `.ids` | 执行 Task 2 时发现：`-rA` 会打印测试捕获的日志，`^ERROR ` 这个正则把一行 `ERROR    app.api.v1.endpoints.records:records.py:512 …simulated ai failure` 也计入了结果（166 个通过却统计出 167 行）。真正的结果行在关键字后面只有一个空格。`run_pytest.sh` 按规定不能覆盖，所以新增脚本 |

## 文件结构

**UnityTarot 仓库（入库）**

| 文件 | 操作 | 职责 | 任务 |
| --- | --- | --- | --- |
| `docs/superpowers/specs/2026-09-11-repo-unification-design.md` | 提交（已存在，目前未跟踪） | 设计 | 3 |
| `docs/superpowers/plans/2026-09-11-repo-unification.md` | 提交（本文件） | 实施计划 | 3 |
| `.gitignore` | 末尾追加 16 行 | 忽略秘密文件和后端运行产物 | 4 |
| `Server/**`（80 个文件） | 创建（纯搬运） | 后端源码 | 5 |
| `.github/workflows/backend-tests.yml` | 创建 | 只针对 `Server/` 的后端 CI | 6 |
| `README.md` | 重写 | 仓库的权威入口 | 7 |
| `Server/README.md` | 创建 | 后端的启动、配置和测试说明 | 7 |
| `PROJECT_COMPLETION_PLAN.md` | 三处替换 | 2.2 删除缺口、新增 2.4、写入 Phase 0 状态 | 7 |

**Tarot 仓库：** 只改 `.git/config`（任务 1）。

**Scratchpad（不入库，会话结束即失效）：** `$SCRATCH` = `/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify`

| 路径 | 职责 | 创建于任务 |
| --- | --- | --- |
| `env.sh` | 共享变量和 `MOVE` 数组 | 2 |
| `run_pytest.sh` | 执行 CI 的两条 pytest 命令并记录结果 | 2 |
| `extract_ids.sh` | 从日志中提取真正的测试结果行，排除捕获的日志行（P8） | 2 |
| `baseline-src/` | `f19c7cd` 的完整导出（对照组） | 2 |
| `results/baseline/`、`results/server/` | 每次测试的日志、逐条结果、摘要和退出码 | 2、5 |
| `verify_blobs.sh` | 逐一比对 80 个文件的路径、权限和 blob SHA | 5 |
| `scan_secrets.sh` | 带对照命中的秘密扫描 | 5 |
| `check_workflow.py` | 检查 CI 文件结构 | 6 |
| `check_docs.py` | 检查文档内容和链接 | 7 |
| `update_plan_doc.py` | 修改完结计划 | 7 |
| `smoke_server.sh`、`smoke/` | 按 README 命令做运行冒烟 | 7 |

---

### Task 1: 拆雷——修正 `Tarot/` 仓库的追踪状态并禁止推送

**Files:**
- Modify: `/Users/maochuandou/BUPT/Game/Tarot/.git/config`（只改 `branch.main.merge` 和 `remote.origin.pushurl`）

**Interfaces:**
- Consumes: 无
- Produces: `Tarot/` 的 `main` 追踪 `origin/backend-main`；`remote.origin.pushurl` 为 `no_push`

- [ ] **Step 1: 记录修改前的状态（失败检查）**

```bash
git -C /Users/maochuandou/BUPT/Game/Tarot status -sb | head -1
git -C /Users/maochuandou/BUPT/Game/Tarot status --porcelain | wc -l
git -C /Users/maochuandou/BUPT/Game/Tarot rev-parse origin/backend-main
git -C /Users/maochuandou/BUPT/Game/Tarot config --get remote.origin.pushurl
```

Expected:
- `## main...origin/main [ahead 4]`（当前是错误状态）
- `0`（没有已跟踪文件被修改）
- `f19c7cdaff954d71d602d99bc3a0e8f54c5340dc`
- `ssh://git@ssh.github.com:443/Mrpengyuyan/Tarot.git`

如果第 3 行不是 `f19c7cd…`：**STOP**，远端归档可能已经变了。

- [ ] **Step 2: 修改 upstream 并禁止推送**

```bash
git -C /Users/maochuandou/BUPT/Game/Tarot branch --set-upstream-to=origin/backend-main main
git -C /Users/maochuandou/BUPT/Game/Tarot remote set-url --push origin no_push
```

Expected: 输出 `branch 'main' set up to track 'origin/backend-main'.`；第二条命令没有输出。

- [ ] **Step 3: 验证**

```bash
git -C /Users/maochuandou/BUPT/Game/Tarot status -sb | head -1
git -C /Users/maochuandou/BUPT/Game/Tarot remote -v
git -C /Users/maochuandou/BUPT/Game/Tarot push --dry-run origin HEAD:main 2>&1 | head -1
git -C /Users/maochuandou/BUPT/Game/Tarot push --dry-run origin HEAD:main >/dev/null 2>&1; echo "push exit=$?"
```

Expected:
- `## main...origin/backend-main`（后面没有 ahead/behind）
- `origin	https://github.com/Mrpengyuyan/Tarot.git (fetch)` 和 `origin	no_push (push)`
- 一行包含 `no_push` 的 `fatal:` 报错（本地直接失败，不会联网）
- `push exit=128`

- [ ] **Step 4: 不需要提交**（只改了本地配置）。撤销方法见 spec §6.5。

---

### Task 2: 建立对照组基线

**Files:**
- Create (scratch): `$SCRATCH/env.sh`、`$SCRATCH/run_pytest.sh`、`$SCRATCH/extract_ids.sh`、`$SCRATCH/baseline-src/`、`$SCRATCH/results/baseline/`

**Interfaces:**
- Consumes: 无
- Produces:
  - `source $SCRATCH/env.sh` 会定义 `GAME UNITY BACKEND SRC_COMMIT SCRATCH VENV`，导出 `PYTHONDONTWRITEBYTECODE=1`，并定义数组 `MOVE`
  - `bash $SCRATCH/run_pytest.sh <project_dir> <label>` 写出 `$SCRATCH/results/<label>/{tests,all}.{log,exit,outcomes,summary}`，每组打印一行摘要；如果 `<label>` 目录已存在则退出码为 2
  - `bash $SCRATCH/extract_ids.sh <label>` 写出 `$SCRATCH/results/<label>/{tests,all}.ids`，每行对应一条真正的测试结果（P8）；如果 `.ids` 已存在则退出码为 2
  - 基线结果保存在 `$SCRATCH/results/baseline/`

- [ ] **Step 1: 创建 scratch 目录和 env.sh**

```bash
SCRATCH=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify
test ! -e "$SCRATCH" || { echo "STOP: $SCRATCH already exists"; exit 1; }
mkdir -p "$SCRATCH"
set -C
cat > "$SCRATCH/env.sh" <<'SH'
# Shared variables for the repo-unification plan. Source it; do not execute it.
GAME=/Users/maochuandou/BUPT/Game
UNITY=$GAME/UnityTarot
BACKEND=$GAME/Tarot
SRC_COMMIT=f19c7cdaff954d71d602d99bc3a0e8f54c5340dc
SCRATCH=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify
VENV=/opt/miniconda3/envs/tarot
export PYTHONDONTWRITEBYTECODE=1
MOVE=(app tests alembic data alembic.ini requirements.txt setup.py Dockerfile .dockerignore .env.example check_config.py start_with_utf8.py)
SH
bash -c 'source "$1/env.sh" && echo "MOVE has ${#MOVE[@]} entries" && git -C "$BACKEND" ls-tree -r --name-only "$SRC_COMMIT" -- "${MOVE[@]}" | wc -l | tr -d " "' _ "$SCRATCH"
```

Expected: 先输出 `MOVE has 12 entries`，再输出 `80`。
如果不是 `80`：**STOP**，说明测量方法本身有问题（先检查 `MOVE` 是否被当成了一个整体字符串）。

- [ ] **Step 2: 只读核对 conda 环境 `tarot`**（不安装、不升级任何包）

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
test -x "$VENV/bin/python" || { echo "STOP: $VENV/bin/python not found"; exit 1; }
"$VENV/bin/python" --version
git -C "$BACKEND" show "${SRC_COMMIT}:requirements.txt" | grep -E '^[A-Za-z]' | while read -r line; do
  pkg=$(printf '%s' "$line" | sed -E 's/\[.*\]//; s/[<>=].*$//')
  want=$(printf '%s' "$line" | sed -E 's/^[^<>=]*//')
  inst=$("$VENV/bin/python" -m pip show "$pkg" 2>/dev/null | awk '/^Version:/{print $2}')
  printf '%-20s want %-10s installed %s\n' "$pkg" "$want" "${inst:-MISSING}"
done
"$VENV/bin/python" -m pip check
"$VENV/bin/python" -c "import fastapi, sqlalchemy, pytest, yaml; print('ok', fastapi.__version__, sqlalchemy.__version__, pytest.__version__, yaml.__version__)"
ls "$VENV/bin/alembic" "$VENV/bin/uvicorn"
```

Expected:
- `Python 3.11.15`
- 16 行依赖。除 `bcrypt`（要求 `<4.0.0`，实际 `3.2.2`，满足要求）外，每行 installed 都等于 want 中 `==` 后面的版本，没有 `MISSING`
- `No broken requirements found.`
- `ok 0.111.0 2.0.30 8.4.1 6.0.3`（`yaml` 供任务 6 使用）
- `alembic` 和 `uvicorn` 两个路径都存在

如果有依赖 MISSING 或版本不符：**STOP**（P7），不要往环境里安装或升级任何包，由用户决定。

- [ ] **Step 3: 创建 run_pytest.sh**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
set -C
cat > "$SCRATCH/run_pytest.sh" <<'SH'
#!/bin/bash
# Usage: bash run_pytest.sh <project_dir> <label>
# Runs the two pytest commands from backend-tests.yml and records the outcomes.
set -o pipefail
source "$(dirname "$0")/env.sh"
dir="$1"; label="$2"; out="$SCRATCH/results/$label"
if [ -e "$out" ]; then echo "STOP: $out already exists"; exit 2; fi
mkdir -p "$out"
cd "$dir" || exit 2
run_suite() {
  local suite="$1"; shift
  env -u DEEPSEEK_API_KEY "$VENV/bin/python" -m pytest -q -rA -p no:cacheprovider "$@" > "$out/$suite.log" 2>&1
  echo $? > "$out/$suite.exit"
  grep -E '^(PASSED|FAILED|ERROR|SKIPPED|XFAIL|XPASS) ' "$out/$suite.log" | LC_ALL=C sort > "$out/$suite.outcomes"
  tail -n 1 "$out/$suite.log" | sed -E 's/ in [0-9.]+s.*$//' > "$out/$suite.summary"
  echo "[$label/$suite] exit=$(cat "$out/$suite.exit") summary=\"$(cat "$out/$suite.summary")\" outcome-lines=$(wc -l < "$out/$suite.outcomes" | tr -d ' ')"
}
run_suite tests tests
run_suite all
SH
ls -l "$SCRATCH/run_pytest.sh"
```

Expected: 列出这个文件。

- [ ] **Step 4: 导出完整对照组**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
test ! -e "$SCRATCH/baseline-src" || { echo "STOP: baseline-src already exists"; exit 1; }
mkdir -p "$SCRATCH/baseline-src"
git -C "$BACKEND" archive "$SRC_COMMIT" | tar -x -C "$SCRATCH/baseline-src"
ls "$SCRATCH/baseline-src" | tr '\n' ' '; echo
test ! -e "$SCRATCH/baseline-src/.env" && echo "no .env in baseline (correct)"
```

Expected: 输出中包含 `alembic app data public scripts src tests`，并输出 `no .env in baseline (correct)`。
这里特意导出**完整的树**（包括 `public/` 和 `src/`）。如果测试暗中依赖了没有搬运的文件，基线会通过而搬运后会失败，任务 5 的比对就能发现。

- [ ] **Step 5: 跑基线**（Bash timeout 设为 600000 ms）

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
bash "$SCRATCH/run_pytest.sh" "$SCRATCH/baseline-src" baseline
```

Expected: 输出两行，格式类似 `[baseline/tests] exit=0 summary="166 passed, N warnings" outcome-lines=…`。完结计划记录的是 166 passed（2026-08-27）。`outcome-lines` 可能比通过数多，因为其中混有捕获的日志行（P8），不要用它判断结果。
如果退出码不是 0，或者数字不是 166：这**不算本任务失败**。把两份 `.summary` 和 `grep -E '^(FAILED|ERROR) [^ ]' "$SCRATCH"/results/baseline/*.log` 的输出原样写进任务报告，然后继续。后续只要求搬运前后结果一致。

- [ ] **Step 5b: 提取精确的测试结果行（P8）**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
set -C
cat > "$SCRATCH/extract_ids.sh" <<'SH'
#!/bin/bash
# Usage: bash extract_ids.sh <label>
# Writes results/<label>/<suite>.ids with one line per real pytest outcome.
# Real summary lines have exactly one space after the keyword; captured log records
# ("ERROR    logger:file.py:12 message", level padded to 8 chars) are excluded.
source "$(dirname "$0")/env.sh"
label="$1"
for suite in tests all; do
  log="$SCRATCH/results/$label/$suite.log"; ids="$SCRATCH/results/$label/$suite.ids"
  if [ -e "$ids" ]; then echo "STOP: $ids already exists"; exit 2; fi
  grep -E '^(PASSED|FAILED|ERROR|SKIPPED|XFAIL|XPASS) [^ ]' "$log" | LC_ALL=C sort > "$ids"
  echo "[$label/$suite] ids=$(wc -l < "$ids" | tr -d ' ') by-kind: $(awk '{print $1}' "$ids" | sort | uniq -c | awk '{printf "%s=%s ", $2, $1}')"
done
SH
bash "$SCRATCH/extract_ids.sh" baseline
diff "$SCRATCH/results/baseline/tests.ids" "$SCRATCH/results/baseline/all.ids" && echo "identical (app/scripts/test_*.py contribute 0 tests, 0 collection errors)"
```

Expected: 两组都输出 `ids=` 加上与摘要一致的数量（2026-09-11 实测为 `ids=166 by-kind: PASSED=166`），并输出 `identical (…)`。

- [ ] **Step 6: 确认没有往 `Tarot/` 写入任何文件**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
find "$BACKEND" -newer "$SCRATCH/env.sh" -not -path '*/.git/*' -print | head -5; echo "(end)"
```

Expected: 只输出 `(end)`。

- [ ] **Step 7: 不需要提交**。

---

### Task 3: 开分支，提交设计文档和计划

**Files:**
- Commit: `docs/superpowers/specs/2026-09-11-repo-unification-design.md`、`docs/superpowers/plans/2026-09-11-repo-unification.md`

**Interfaces:**
- Consumes: 无
- Produces: 分支 `chore/unify-repo`（从 `main`@`4ec3816` 切出）和提交 1

- [ ] **Step 1: 确认起点**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git rev-parse --abbrev-ref HEAD; git rev-parse --short HEAD
git status --porcelain
git ls-remote --heads origin main backend-main
```

Expected:
- `main`，`4ec3816`
- 只有一行 `?? docs/`
- `4ec3816827a9d0fe2d8c176e0ca031af3ab4a22b	refs/heads/main` 和 `f19c7cdaff954d71d602d99bc3a0e8f54c5340dc	refs/heads/backend-main`（两行顺序可能不同）

如果 status 里有别的改动，或者远端引用和上面不同：**STOP**。

- [ ] **Step 2: 开分支**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot && git switch -c chore/unify-repo
```

Expected: `Switched to a new branch 'chore/unify-repo'`

- [ ] **Step 3: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git add docs/superpowers/specs/2026-09-11-repo-unification-design.md docs/superpowers/plans/2026-09-11-repo-unification.md
git commit -q -F - <<'MSG'
docs: add repo unification design spec and implementation plan

Design and step-by-step plan for merging the backend service from the
old Tarot repository (f19c7cd, archived on origin/backend-main) into
Server/ of this repository.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
MSG
git show --stat --format='%h %s' HEAD
```

Expected: 显示 `docs: add repo unification design spec and implementation plan`，`2 files changed`，只包含这两份文档。

---

### Task 4: `.gitignore`——忽略秘密文件和后端运行产物

**Files:**
- Modify: `.gitignore`（在末尾追加 16 行；文件原本以换行结尾，共 32 行）

**Interfaces:**
- Consumes: 分支 `chore/unify-repo`
- Produces: 提交 2。之后 `Server/.env`、`Server/**/*.db` 等会被忽略，`Server/.env.example` 不会被忽略

- [ ] **Step 1: 失败检查**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
for p in .env Server/.env Server/.env.local Server/tarot_game.db Server/app/x.log Server/.pytest_cache/v Server/.venv/bin/python Server/app/scripts/reports/r.json Server/.env.example; do
  if git check-ignore -q --no-index "$p"; then echo "ignored     $p"; else echo "NOT ignored $p"; fi
done
```

Expected: 9 行都是 `NOT ignored`。

- [ ] **Step 2: 追加规则**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
cat >> .gitignore <<'GI'

# Secrets (repository-wide safety net)
.env
.env.*
!.env.example

# Backend runtime artifacts (Server/)
Server/**/*.db
Server/**/*.sqlite
Server/**/*.sqlite3
Server/**/*.log
Server/**/.pytest_cache/
Server/**/*.egg-info/
Server/.venv/
Server/venv/
Server/app/scripts/reports/
GI
```

- [ ] **Step 3: 通过检查**

再执行一遍 Step 1 的循环，然后执行：

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git ls-files -ci --exclude-standard; echo "(end tracked-but-ignored)"
git diff --stat
```

Expected:
- 前 8 行是 `ignored`，最后一行是 `NOT ignored Server/.env.example`
- 只输出 `(end tracked-but-ignored)`（没有已跟踪文件被新规则匹配到）
- `.gitignore | 16 ++++++++++++++++`

- [ ] **Step 4: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git add .gitignore
git commit -q -F - <<'MSG'
chore: add secret and backend runtime ignore rules

Added before importing Server/ so that .env files and local databases
cannot be staged by the import.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
MSG
git show --stat --format='%h %s' HEAD
```

Expected: `1 file changed, 16 insertions(+)`

---

### Task 5: 纯搬运——把 80 个后端文件导入 `Server/`

**Files:**
- Create: `Server/` 下 80 个文件（内容与 `f19c7cd` 逐字节一致）
- Create (scratch): `$SCRATCH/verify_blobs.sh`、`$SCRATCH/scan_secrets.sh`、`$SCRATCH/results/server/`

**Interfaces:**
- Consumes: `env.sh`（`MOVE`、`SRC_COMMIT`、`UNITY`、`BACKEND`、`VENV`）、`run_pytest.sh`、`extract_ids.sh`、`results/baseline/`、提交 2 的忽略规则
- Produces:
  - 提交 3，subject 固定为 `chore: import backend service into Server/ from Tarot@f19c7cd`（任务 8 用 `--grep` 定位）
  - `bash $SCRATCH/verify_blobs.sh [index|<commit-ish>]`：通过时退出码 0 并输出 `PASS: 80/80 paths, modes and blob SHAs identical`
  - `bash $SCRATCH/scan_secrets.sh <dir>`：通过时退出码 0
  - `$SCRATCH/results/server/{tests,all}.summary`（任务 7 读取）

- [ ] **Step 1: 创建 verify_blobs.sh**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
set -C
cat > "$SCRATCH/verify_blobs.sh" <<'SH'
#!/bin/bash
# Usage: bash verify_blobs.sh [index|<commit-ish>]
# Compares mode + blob SHA + path of the 80 moved files between Tarot@SRC_COMMIT
# and UnityTarot Server/ (staged index or a commit). Server/README.md is excluded.
set -o pipefail
source "$(dirname "$0")/env.sh"
target="${1:-index}"
fmt='%(objectmode) %(objectname) %(path)'
expected=$(git -C "$BACKEND" ls-tree -r --format="$fmt" "$SRC_COMMIT" -- "${MOVE[@]}" | LC_ALL=C sort)
if [ "$target" = "index" ]; then
  actual_raw=$(git -C "$UNITY" ls-files --format="$fmt" -- Server/)
else
  actual_raw=$(git -C "$UNITY" ls-tree -r --format="$fmt" "$target" -- Server/)
fi
actual=$(printf '%s\n' "$actual_raw" | grep -v ' Server/README\.md$' | sed 's# Server/# #' | grep . | LC_ALL=C sort)
exp_n=$(printf '%s\n' "$expected" | grep -c .)
act_n=$(printf '%s\n' "$actual" | grep -c .)
echo "expected files: $exp_n"
echo "actual files:   $act_n"
if [ "$exp_n" -ne 80 ]; then echo "FAIL: expected set is $exp_n files, not 80 (instrument broken)"; exit 2; fi
if diff <(printf '%s\n' "$expected") <(printf '%s\n' "$actual"); then
  echo "PASS: 80/80 paths, modes and blob SHAs identical"
else
  echo "FAIL: differences above (< Tarot@f19c7cd, > UnityTarot Server/)"; exit 1
fi
SH
ls -l "$SCRATCH/verify_blobs.sh"
```

- [ ] **Step 2: 失败检查**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
bash "$SCRATCH/verify_blobs.sh" index | tail -3; echo "exit=${pipestatus[1]}"
```

Expected: 输出中有 `expected files: 80`、`actual files:   0` 和一行 `FAIL: ...`，最后是 `exit=1`。

- [ ] **Step 3: 导出到 `Server/`**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
cd "$UNITY"
test ! -e Server || { echo "STOP: Server/ already exists"; exit 1; }
mkdir Server
git -C "$BACKEND" archive "$SRC_COMMIT" -- "${MOVE[@]}" | tar -x -C Server
find Server -type f | wc -l | tr -d ' '
test ! -e Server/.env && echo "no Server/.env (correct)"
```

Expected: `80`，然后是 `no Server/.env (correct)`。

- [ ] **Step 4: 暂存，并与 index 比对**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
cd "$UNITY" && git add Server
git status --porcelain | grep -c '^A  Server/'
git status --porcelain | grep -v '^A  Server/'; echo "(end other changes)"
bash "$SCRATCH/verify_blobs.sh" index; echo "exit=$?"
```

Expected: `80`，然后只有 `(end other changes)`，接着输出 `PASS: 80/80 paths, modes and blob SHAs identical` 和 `exit=0`。
如果 FAIL：**STOP**，不要提交，也不要删除或覆盖 `Server/` 下的任何文件。报告 diff 后由用户决定怎么处理。

- [ ] **Step 5: 搬运后测试，与基线逐条比对**（Bash timeout 设为 600000 ms）

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
bash "$SCRATCH/run_pytest.sh" "$UNITY/Server" server
bash "$SCRATCH/extract_ids.sh" server
for s in tests all; do
  echo "== $s =="
  diff "$SCRATCH/results/baseline/$s.summary" "$SCRATCH/results/server/$s.summary" && echo "summary identical"
  diff "$SCRATCH/results/baseline/$s.ids"     "$SCRATCH/results/server/$s.ids"     && echo "ids identical"
  diff "$SCRATCH/results/baseline/$s.exit"    "$SCRATCH/results/server/$s.exit"    && echo "exit identical"
done
```

Expected: `tests` 和 `all` 两组都输出 `summary identical`、`ids identical`、`exit identical`。
如果有任何 diff：**STOP**，不要提交。出现 diff 说明被测代码依赖了没有搬运的文件，需要回到 spec §5 重新判断文件归属。

- [ ] **Step 6: 检查测试有没有在 `Server/` 下留下文件**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git status --porcelain --ignored -- Server | grep -v '^A  '; echo "(end)"
```

Expected: 只输出 `(end)`。
- 如果出现 `!! Server/...`：这是被忽略的副产物，不会入库。在任务报告里列出来，**不要删除**。
- 如果出现 `?? Server/...`：这是没被忽略的新文件，**STOP**。

- [ ] **Step 7: 秘密扫描**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
set -C
cat > "$SCRATCH/scan_secrets.sh" <<'SH'
#!/bin/bash
# Usage: bash scan_secrets.sh <dir>
set -o pipefail
dir="$1"
control=$(grep -rIl --exclude-dir=__pycache__ 'DEEPSEEK_API_KEY' "$dir" | wc -l | tr -d ' ')
echo "control hits (files naming DEEPSEEK_API_KEY): $control"
if [ "$control" -lt 1 ]; then echo "FAIL: control found nothing, so the scanner is not reading files"; exit 2; fi
hits=$(grep -rInE --exclude-dir=__pycache__ 'sk-[A-Za-z0-9]{16,}|ghp_[A-Za-z0-9]{20,}|AKIA[0-9A-Z]{16}|-----BEGIN [A-Z ]*PRIVATE KEY' "$dir" | cut -d: -f1-2)
if [ -n "$hits" ]; then echo "FAIL: secret-pattern hits (file:line):"; echo "$hits"; exit 1; fi
echo "PASS: 0 secret-pattern hits"
envfiles=$(find "$dir" \( -name '.env' -o -name '.env.*' \) ! -name '.env.example')
if [ -n "$envfiles" ]; then echo "FAIL: env files present:"; echo "$envfiles"; exit 1; fi
echo "PASS: no .env files besides .env.example"
SH
bash "$SCRATCH/scan_secrets.sh" "$UNITY/Server"; echo "exit=$?"
```

Expected: `control hits (files naming DEEPSEEK_API_KEY): 3`（即 `.env.example`、`app/core/config.py`、`app/services/coze_service.py`），然后是 `PASS: 0 secret-pattern hits`、`PASS: no .env files besides .env.example`，最后 `exit=0`。

- [ ] **Step 8: 提交，并对提交再比对一次**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
cd "$UNITY"
git commit -q -F - <<'MSG'
chore: import backend service into Server/ from Tarot@f19c7cd

Pure copy of the 80 tracked backend files (app, tests, alembic, data,
requirements, Dockerfile, config helpers) exported with git archive from
the old Tarot repository at f19c7cd. No file content was changed.

The previous 16 backend commits remain on origin/backend-main. The
retired React web client, its assets and docker-compose.yml stay there
and are not part of main.

Verified: paths, modes and blob SHAs identical to the source commit;
pytest outcomes identical to a clean export of the source commit.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
MSG
git show --stat --format='%h %s' HEAD | tail -1
bash "$SCRATCH/verify_blobs.sh" HEAD | tail -1
```

Expected: 输出 `80 files changed, …` 和 `PASS: 80/80 paths, modes and blob SHAs identical`。

---

### Task 6: CI——后端测试 workflow 只在 `Server/` 下运行

**Files:**
- Create: `.github/workflows/backend-tests.yml`
- Create (scratch): `$SCRATCH/check_workflow.py`

**Interfaces:**
- Consumes: 提交 3（`Server/requirements.txt` 已存在）、`tarot` 环境中的 PyYAML
- Produces: 提交 4；执行 `$VENV/bin/python $SCRATCH/check_workflow.py <repo_root>`，通过时退出码为 0

- [ ] **Step 1: 创建 check_workflow.py**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
set -C
cat > "$SCRATCH/check_workflow.py" <<'PY'
#!/usr/bin/env python3
"""Checks .github/workflows/backend-tests.yml. Usage: check_workflow.py <repo_root>"""
import sys
from pathlib import Path

import yaml

root = Path(sys.argv[1])
wf = root / ".github/workflows/backend-tests.yml"
if not wf.exists():
    print(f"FAIL: {wf.relative_to(root)} does not exist")
    sys.exit(1)

doc = yaml.safe_load(wf.read_text(encoding="utf-8"))
# PyYAML follows YAML 1.1, where a bare `on` key is parsed as boolean True.
triggers = doc.get("on", doc.get(True)) or {}
want_paths = ["Server/**", ".github/workflows/backend-tests.yml"]
problems = []

push = triggers.get("push") or {}
pull_request = triggers.get("pull_request") or {}
if push.get("branches") != ["**"]:
    problems.append(f"on.push.branches is {push.get('branches')!r}")
if push.get("paths") != want_paths:
    problems.append(f"on.push.paths is {push.get('paths')!r}")
if pull_request.get("paths") != want_paths:
    problems.append(f"on.pull_request.paths is {pull_request.get('paths')!r}")

working_dir = ((doc.get("defaults") or {}).get("run") or {}).get("working-directory")
if working_dir != "Server":
    problems.append(f"defaults.run.working-directory is {working_dir!r}")
elif not (root / working_dir / "requirements.txt").exists():
    problems.append(f"{working_dir}/requirements.txt does not exist")

runs = "\n".join(step.get("run", "") for step in doc["jobs"]["pytest"]["steps"])
for command in ("pip install -r requirements.txt", "python -m pytest -q tests", "python -m pytest -q"):
    if command not in runs:
        problems.append(f"missing step command {command!r}")

if problems:
    print("FAIL:")
    for problem in problems:
        print(f"  - {problem}")
    sys.exit(1)
print("PASS: backend-tests.yml is scoped to Server/")
PY
ls -l "$SCRATCH/check_workflow.py"
```

- [ ] **Step 2: 失败检查**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
"$VENV/bin/python" "$SCRATCH/check_workflow.py" "$UNITY"; echo "exit=$?"
```

Expected: 输出 `FAIL: .github/workflows/backend-tests.yml does not exist` 和 `exit=1`。

- [ ] **Step 3: 写入 workflow**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
test ! -e .github || { echo "STOP: .github already exists"; exit 1; }
mkdir -p .github/workflows
set -C
cat > .github/workflows/backend-tests.yml <<'YML'
name: backend-tests

on:
  push:
    branches:
      - "**"
    paths:
      - "Server/**"
      - ".github/workflows/backend-tests.yml"
  pull_request:
    paths:
      - "Server/**"
      - ".github/workflows/backend-tests.yml"

defaults:
  run:
    working-directory: Server

jobs:
  pytest:
    runs-on: ubuntu-latest
    timeout-minutes: 15

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup Python
        uses: actions/setup-python@v5
        with:
          python-version: "3.10"

      - name: Install dependencies
        run: |
          python -m pip install --upgrade pip
          pip install -r requirements.txt

      - name: Run focused tests
        run: python -m pytest -q tests

      - name: Run full tests
        run: python -m pytest -q
YML
```

- [ ] **Step 4: 通过检查，并与原文件比对**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
"$VENV/bin/python" "$SCRATCH/check_workflow.py" "$UNITY"; echo "exit=$?"
diff <(git -C "$BACKEND" show "${SRC_COMMIT}:.github/workflows/backend-tests.yml") "$UNITY/.github/workflows/backend-tests.yml" > "$SCRATCH/workflow.diff"
echo "removed lines: $(grep -c '^<' "$SCRATCH/workflow.diff")  added lines: $(grep -c '^>' "$SCRATCH/workflow.diff")"
```

Expected: 输出 `PASS: backend-tests.yml is scoped to Server/`、`exit=0`，以及 `removed lines: 0  added lines: 10`。

- [ ] **Step 5: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git add .github/workflows/backend-tests.yml
git commit -q -F - <<'MSG'
ci: move backend tests workflow and scope it to Server/

Same jobs as Tarot@f19c7cd:.github/workflows/backend-tests.yml, plus
path filters and a Server/ working directory so Unity-only commits do
not trigger a backend run that cannot find requirements.txt.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
MSG
git show --stat --format='%h %s' HEAD | tail -1
```

Expected: `1 file changed, 42 insertions(+)`

> GitHub Actions 要等推送之后才会真正运行。推送需要用户同意，所以本计划只能验证文件结构，在任务 8 的报告中要说明这一点。

---

### Task 7: 文档——根 README、Server README、完结计划

**Files:**
- Modify (rewrite): `README.md`（已跟踪；旧版可以用 `git show main:README.md` 找回，spec §9 已批准重写）
- Create: `Server/README.md`
- Modify: `PROJECT_COMPLETION_PLAN.md`（三处替换）
- Create (scratch): `$SCRATCH/check_docs.py`、`$SCRATCH/update_plan_doc.py`、`$SCRATCH/smoke_server.sh`、`$SCRATCH/smoke/`

**Interfaces:**
- Consumes: `$SCRATCH/results/server/{tests,all}.summary`（任务 5）、提交 3 至 4、`tarot` 环境
- Produces: 提交 5；执行 `$VENV/bin/python $SCRATCH/check_docs.py <repo_root>`，通过时退出码为 0

- [ ] **Step 1: 创建 check_docs.py**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
set -C
cat > "$SCRATCH/check_docs.py" <<'PY'
#!/usr/bin/env python3
"""Checks the Task 7 documentation deliverables. Usage: check_docs.py <repo_root>"""
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
problems = []


def read(rel):
    path = root / rel
    if not path.exists():
        problems.append(f"missing {rel}")
        return None
    return path.read_text(encoding="utf-8")


def check_links(rel, text):
    base = (root / rel).parent
    for target in re.findall(r"\]\(([^)\s]+)\)", text):
        if target.startswith(("http://", "https://", "#", "mailto:")):
            continue
        if not (base / target.split("#", 1)[0]).exists():
            problems.append(f"{rel}: broken link -> {target}")


def require(rel, text, needles):
    for needle in needles:
        if needle not in text:
            problems.append(f"{rel}: missing {needle!r}")


readme = read("README.md")
if readme is not None:
    check_links("README.md", readme)
    require("README.md", readme, [
        "Server/", "Server/README.md", "backend-main", "f19c7cd",
        "UnityClient/TarotUnity/", "/api/v1/health/", "/api/v1/guest-session",
    ])
    if "尚未合并到这个 Unity" in readme:
        problems.append("README.md: still says the backend is not merged")

server = read("Server/README.md")
if server is not None:
    check_links("Server/README.md", server)
    require("Server/README.md", server, [
        "pip install -r requirements.txt", "cp .env.example .env", "Copy-Item .env.example .env",
        "REQUIRE_STRONG_SECRET", "alembic upgrade head", "python -m app.scripts.init_tarot_data",
        "uvicorn app.main:app", "GUEST_DAILY_READING_LIMIT", "/api/v1/health/",
        "python -m pytest -q tests", "docker build",
    ])
    for rel in ["requirements.txt", ".env.example", "alembic.ini", "app/main.py",
                "app/scripts/init_tarot_data.py", "check_config.py", "start_with_utf8.py",
                "Dockerfile", "data/tarotCards.json", "data/spreads.json"]:
        if not (root / "Server" / rel).exists():
            problems.append(f"Server/README.md references Server/{rel}, which does not exist")

plan = read("PROJECT_COMPLETION_PLAN.md")
if plan is not None:
    if "旧的 FastAPI/React 代码位于另一个本地仓库" in plan:
        problems.append("PROJECT_COMPLETION_PLAN.md: 2.2 still lists the repository boundary gap")
    require("PROJECT_COMPLETION_PLAN.md", plan, [
        "### 2.4 本轮执行记录（2026-09-11）", "> **状态（2026-09-11）：**",
    ])
    match = re.search(r"### 2\.4 本轮执行记录（2026-09-11）\n(.*?)\n## ", plan, re.S)
    if match and ("{" in match.group(1) or "passed" not in match.group(1)):
        problems.append("PROJECT_COMPLETION_PLAN.md: 2.4 has unfilled values or no pytest summary")

if problems:
    print("FAIL:")
    for problem in problems:
        print(f"  - {problem}")
    sys.exit(1)
print("PASS: README.md, Server/README.md and PROJECT_COMPLETION_PLAN.md checks")
PY
ls -l "$SCRATCH/check_docs.py"
```

- [ ] **Step 2: 失败检查**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
"$VENV/bin/python" "$SCRATCH/check_docs.py" "$UNITY"; echo "exit=$?"
```

Expected: 输出 `FAIL:`，下面至少包括 `missing Server/README.md`、`README.md: missing 'backend-main'`、`README.md: still says the backend is not merged`、`PROJECT_COMPLETION_PLAN.md: 2.2 still lists the repository boundary gap`，最后是 `exit=1`。

- [ ] **Step 3: 重写根目录 `README.md`**（有意覆盖已跟踪文件，所以用 `>|`）

````bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
cat >| README.md <<'MD'
# Tarot

Tarot 3D 塔罗牌仪式游戏的产品仓库，包含 Unity 桌面客户端和 FastAPI 后端。

- 客户端：Unity `6000.3.16f1`、URP、版本 `0.9.0`，目标平台为 macOS 和 Windows 桌面端。
- 后端：FastAPI、SQLAlchemy、Alembic，负责认证与访客会话、抽牌、阅读记录、AI 解读、限流和预算。

## 目录

```text
Tarot/
├── README.md                     本文件
├── PROJECT_COMPLETION_PLAN.md    1.0 完结路线
├── UNITY_FRONTEND_PLAN.md        Unity 前端方向和后端边界
├── UnityClient/
│   ├── README.md
│   └── TarotUnity/               Unity 工程
├── Server/
│   ├── README.md                 后端启动、配置和测试
│   ├── app/                      FastAPI 应用
│   ├── tests/                    后端测试
│   ├── alembic/                  数据库迁移
│   └── data/                     78 张牌和 6 个牌阵的参考数据
├── .github/workflows/
│   └── backend-tests.yml         只在 Server/ 变动时运行
└── docs/superpowers/             设计与实施文档
```

Unity 工程位于 `UnityClient/TarotUnity/`。构建场景包括：

- `Boot`：启动和持久化服务初始化。
- `MainMenu`：牌阵选择和进入仪式。
- `ReadingRoom`：问题输入、洗牌、发牌和翻牌。
- `Result`：多张牌展示和 AI 解读阅读。

## 运行方式

### Unity 本地开发

使用 Unity `6000.3.16f1` 打开 `UnityClient/TarotUnity/`，然后从
`Assets/Scenes/Boot.unity` 运行。默认可以使用本地模拟数据完成完整流程。

### 本地后端联调

1. 按 [`Server/README.md`](Server/README.md) 启动后端，默认地址为 `http://localhost:8000`。
2. Unity 的在线模式通过
   `UnityClient/TarotUnity/Assets/StreamingAssets/tarot_desktop_config.json`
   读取后端地址，默认为 `http://localhost:8000/api/v1`。该配置面向本地开发，不代表公开发行地址。
3. 客户端启动时先访问 `/api/v1/health/`，服务可达后再访问 `/api/v1/guest-session` 申请访客会话。

后端负责认证、阅读记录、抽牌、AI 解读、限流、预算和 API Key；Unity 只负责客户端表现和交互。

### 玩家发行包

玩家不需要安装 Python、Conda、Docker 或 Unity 编辑器。正式发行包应从
GitHub Releases 下载并解压运行；在线模式必须指向部署后的 HTTPS 后端，
不能指向开发者电脑的 `localhost`。

## 安全边界

- API Key、数据库密码和管理员令牌只允许存在于后端安全配置中（`Server/.env` 或部署平台的密钥配置）。
- Unity 配置文件只能包含公开的服务地址、超时时间和客户端选项。
- `.env`、本地数据库、日志、构建目录和 Unity 缓存不得提交；根目录 `.gitignore` 已忽略它们。
- 公开发行前必须完成客户端、构建产物和 Git 历史的秘密扫描。

## 仓库历史

- 从 2026-09-11 起，本仓库是唯一的提交入口。后端以一次提交并入 `Server/`：除 `Server/README.md` 外的 80 个文件与旧后端仓库提交 `f19c7cd` 逐字节一致。
- 旧后端仓库的 16 个提交保留在远端分支 `backend-main`（`f19c7cd`）。查看方法：先执行 `git fetch origin backend-main`，再执行 `git log origin/backend-main`。
- 已废弃的 React Web 前端（`src/`、`public/`）和旧的 `docker-compose.yml` 只保留在 `backend-main`，不在 `main` 中维护。
- 设计依据：[`docs/superpowers/specs/2026-09-11-repo-unification-design.md`](docs/superpowers/specs/2026-09-11-repo-unification-design.md)。

## 文档入口

- [`PROJECT_COMPLETION_PLAN.md`](PROJECT_COMPLETION_PLAN.md)：1.0 完结路线、阶段任务和验收标准。
- [`UNITY_FRONTEND_PLAN.md`](UNITY_FRONTEND_PLAN.md)：Unity 前端方向和后端边界。
- [`UnityClient/README.md`](UnityClient/README.md)：Unity 工程目录、运行和发行说明。
- [`Server/README.md`](Server/README.md)：后端启动、配置和测试。
- [`UnityClient/TarotUnity/Docs/PROJECT_CHRONICLE.md`](UnityClient/TarotUnity/Docs/PROJECT_CHRONICLE.md)：Phase 1-64 的整理记录。
- [`UnityClient/TarotUnity/Docs/THIRD_PARTY_ASSETS.md`](UnityClient/TarotUnity/Docs/THIRD_PARTY_ASSETS.md)：资源来源和授权记录。

## 当前下一步

1. 激活 Unity Editor 许可证，重新执行 EditMode、PlayMode 和真实客户端联调。
2. 将后端部署到 HTTPS 地址，并把发行配置从 `localhost` 切换到正式地址。
3. 完善部署级访客频率限制和 AI 预算保护，再验证 macOS/Windows 干净环境。
4. 通过 1.0 验收后，将发行包放入 GitHub Release，而不是提交到源码历史。
MD
````

- [ ] **Step 4: 创建 `Server/README.md`**

````bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
set -C
cat > Server/README.md <<'MD'
# Tarot Server

Tarot 的 FastAPI 后端，负责认证与访客会话、抽牌、阅读记录、AI 解读（DeepSeek）、限流和预算保护。Unity 客户端通过 `/api/v1` 访问它。

下面的命令都在 `Server/` 目录下执行。

## 环境要求

- Python 3.10 或 3.11（CI 使用 3.10，`Dockerfile` 使用 3.11）
- 默认数据库是 SQLite 本地文件，不需要额外安装数据库

## 首次准备

macOS / Linux：

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env
```

Windows PowerShell：

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
```

然后编辑 `.env`：

- `SECRET_KEY`：`.env.example` 开启了 `REQUIRE_STRONG_SECRET=true`，示例值不能直接使用。可以用下面的命令生成：
  `python -c "import secrets; print(secrets.token_urlsafe(48))"`
- `DEEPSEEK_API_KEY`：需要真实 AI 解读时，替换成真实 Key。
- `DEBUG`：示例值为 `true`，此时 `/docs` 可以访问；部署时改为 `false`。

`.env` 已被仓库的 `.gitignore` 忽略，不要提交。

## 初始化数据库

```bash
alembic upgrade head
python -m app.scripts.init_tarot_data
```

第一条命令建表。第二条命令从 `data/tarotCards.json` 和 `data/spreads.json` 导入 78 张牌和 6 个牌阵。

## 启动

```bash
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

- Windows 控制台出现中文乱码时，改用 `python start_with_utf8.py`。
- `python check_config.py` 会打印当前生效的配置，其中数据库地址会脱敏。

启动后可以访问：

- `GET http://localhost:8000/api/v1/health/`：基础健康检查，Unity 启动时首先访问它。
- `POST http://localhost:8000/api/v1/guest-session`：申请访客会话。
- `http://localhost:8000/docs`：OpenAPI 文档，仅在 `DEBUG=true` 时可用。

## 关键配置

完整列表见 `.env.example`。

| 变量 | `.env.example` 中的值 | 说明 |
| --- | --- | --- |
| `DATABASE_URL` | `sqlite:///./tarot_game.db` | 生产环境请换成 PostgreSQL 地址（依赖中已包含 `psycopg2-binary`） |
| `GUEST_DAILY_READING_LIMIT` | `3` | 访客每日阅读上限，超出后返回 `429` 和 `Retry-After` |
| `AI_BUDGET_GUARD_ENABLED` | `true` | AI 预算保护。计数器保存在进程内存中，多实例部署时需要集中存储 |
| `AUTO_CREATE_TABLES_ON_STARTUP` | `false` | 设为 `true` 后，启动时自动建表 |
| `AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP` | `false` | 设为 `true` 后，启动时自动导入牌和牌阵数据 |

## 测试

```bash
python -m pytest -q tests
python -m pytest -q
```

第二条命令不带路径，所以还会收集 `app/scripts/` 下的 `test_*.py` 模块。GitHub Actions（仓库根目录的 `.github/workflows/backend-tests.yml`）只在 `Server/` 有改动时运行这两条命令。

## Docker

镜像的构建上下文是 `Server/`：

```bash
docker build -t tarot-server .
docker run --rm -p 8000:8000 --env-file .env tarot-server
```

容器启动时不会执行 `alembic upgrade head`。需要建表和导入数据时，在 `.env` 中把 `AUTO_CREATE_TABLES_ON_STARTUP` 和 `AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP` 设为 `true`。

目前没有 `docker-compose.yml`：旧版编排包含已废弃的 React 前端，等部署方式确定后再重写，见 [`PROJECT_COMPLETION_PLAN.md`](../PROJECT_COMPLETION_PLAN.md) Phase 5。
MD
ls -l Server/README.md
````

- [ ] **Step 5: 修改完结计划**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
set -C
cat > "$SCRATCH/update_plan_doc.py" <<'PY'
#!/usr/bin/env python3
"""Applies the three approved edits to PROJECT_COMPLETION_PLAN.md.
Usage: update_plan_doc.py <scratch_dir> <plan_doc>"""
import sys
from pathlib import Path

scratch = Path(sys.argv[1])
doc = Path(sys.argv[2])
text = doc.read_text(encoding="utf-8")

tests_summary = (scratch / "results/server/tests.summary").read_text(encoding="utf-8").strip()
all_summary = (scratch / "results/server/all.summary").read_text(encoding="utf-8").strip()
if not tests_summary or not all_summary:
    sys.exit("FAIL: empty pytest summary files")


def replace_once(source, old, new, label):
    count = source.count(old)
    if count != 1:
        sys.exit(f"FAIL: {label}: expected exactly 1 match, found {count}")
    return source.replace(old, new)


# Edit A: 2.2 no longer lists the repository boundary gap.
text = replace_once(
    text,
    "- GitHub 当前 `main` 主要是 Unity 客户端树，旧的 FastAPI/React 代码位于另一个本地仓库，代码仓库边界和发布方式需要统一。\n",
    "",
    "2.2 repository boundary gap",
)

# Edit B: new 2.4 record after the last bullet of 2.3.
anchor_b = "- Unity 测试执行的唯一环境阻塞是本机没有有效的 Unity Editor 许可证，需在 Unity Hub 激活后重新运行。\n"
record = (
    anchor_b
    + "\n### 2.4 本轮执行记录（2026-09-11）\n\n"
    + "- 仓库归一完成：后端 80 个文件从旧仓库提交 `f19c7cd` 以一次纯搬运提交并入 `Server/`，路径、权限和 blob SHA 与源提交完全一致。\n"
    + "- 旧后端的 16 个提交保留在远端分支 `backend-main`；React Web 前端废弃，不进入 `main`；`docker-compose.yml` 等部署方式确定后重写。\n"
    + "- 后端 CI 移到 `.github/workflows/backend-tests.yml`，只在 `Server/` 或该文件变动时运行。\n"
    + "- 本地旧仓库 `Tarot/` 改为追踪 `origin/backend-main` 并禁用推送，不再作为提交入口。\n"
    + f"- 后端测试在 `Server/` 下与源提交干净导出的结果逐条一致：`pytest -q tests` 为 `{tests_summary}`，`pytest -q` 为 `{all_summary}`。\n"
)
text = replace_once(text, anchor_b, record, "2.3 last bullet")

# Edit C: Phase 0 per-task status.
anchor_c = "**目标：** 先消除两个本地仓库、一个 GitHub 地址和多个运行方式造成的发布歧义。\n"
status = (
    anchor_c
    + "\n> **状态（2026-09-11）：** 任务 2、3 已由仓库归一完成（`docs/superpowers/specs/2026-09-11-repo-unification-design.md`）；"
    + "任务 4 已由提交 `c636595` 和本次 README 重写完成；任务 1、5 已在清理基线提交 `9307007` 中完成；"
    + "任务 6 的版本策略已写在本节，但尚未创建 `v0.9.0` tag。\n"
)
text = replace_once(text, anchor_c, status, "Phase 0 goal line")

doc.write_text(text, encoding="utf-8")
print("OK: 3 edits applied")
PY
"$VENV/bin/python" "$SCRATCH/update_plan_doc.py" "$SCRATCH" "$UNITY/PROJECT_COMPLETION_PLAN.md"
git -C "$UNITY" diff --stat -- PROJECT_COMPLETION_PLAN.md
```

Expected: 输出 `OK: 3 edits applied` 和 `PROJECT_COMPLETION_PLAN.md | 11 ++++++++++-`。
如果某处替换的匹配数不是 1：脚本会在写文件之前退出，文件保持原样，此时 **STOP**。

- [ ] **Step 6: 文档检查通过**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
"$VENV/bin/python" "$SCRATCH/check_docs.py" "$UNITY"; echo "exit=$?"
bash "$SCRATCH/verify_blobs.sh" index | tail -1
```

Expected: 输出 `PASS: README.md, Server/README.md and PROJECT_COMPLETION_PLAN.md checks`、`exit=0`，以及 `PASS: 80/80 paths, modes and blob SHAs identical`（新增的 `Server/README.md` 已被排除在比对之外）。

- [ ] **Step 7: 按 README 命令做运行冒烟**（Bash timeout 设为 600000 ms）

这一步在 scratchpad 副本中按 `Server/README.md` 的步骤执行。依赖直接复用 `tarot` 环境，不新建 `.venv`。

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
set -C
cat > "$SCRATCH/smoke_server.sh" <<'SH'
#!/bin/bash
# Follows Server/README.md inside a throwaway copy of Server/ from the working tree index.
set -o pipefail
source "$(dirname "$0")/env.sh"
SMOKE="$SCRATCH/smoke"
if [ -e "$SMOKE" ]; then echo "STOP: $SMOKE already exists"; exit 2; fi
if lsof -nP -iTCP:8765 -sTCP:LISTEN >/dev/null 2>&1; then echo "STOP: port 8765 is in use"; exit 2; fi
mkdir -p "$SMOKE"
git -C "$UNITY" archive "$(git -C "$UNITY" write-tree)" Server | tar -x -C "$SMOKE"
cd "$SMOKE/Server" || exit 2
cp .env.example .env
export SECRET_KEY="$("$VENV/bin/python" -c 'import secrets; print(secrets.token_urlsafe(48))')"

"$VENV/bin/alembic" upgrade head > "$SMOKE/alembic.log" 2>&1 \
  || { echo "FAIL: alembic upgrade head"; tail -20 "$SMOKE/alembic.log"; exit 1; }
echo "PASS: alembic upgrade head"

"$VENV/bin/python" -m app.scripts.init_tarot_data > "$SMOKE/init.log" 2>&1 \
  || { echo "FAIL: init_tarot_data exited non-zero"; tail -20 "$SMOKE/init.log"; exit 1; }
grep -q '塔罗牌: 78 条记录' "$SMOKE/init.log" && grep -q '牌阵: 6 条记录' "$SMOKE/init.log" \
  || { echo "FAIL: unexpected init counts"; grep -E '塔罗牌|牌阵' "$SMOKE/init.log"; exit 1; }
echo "PASS: init_tarot_data imported 78 cards and 6 spreads"

"$VENV/bin/python" check_config.py > "$SMOKE/check_config.log" 2>&1 \
  || { echo "FAIL: check_config.py"; tail -20 "$SMOKE/check_config.log"; exit 1; }
echo "PASS: check_config.py"

"$VENV/bin/python" -m uvicorn app.main:app --host 127.0.0.1 --port 8765 > "$SMOKE/uvicorn.log" 2>&1 &
pid=$!
health=$(curl -sS --retry 30 --retry-delay 1 --retry-connrefused -o "$SMOKE/health.json" -w '%{http_code}' http://127.0.0.1:8765/api/v1/health/)
guest=$(curl -sS -X POST -o "$SMOKE/guest.json" -w '%{http_code}' http://127.0.0.1:8765/api/v1/guest-session)
kill "$pid" 2>/dev/null; wait "$pid" 2>/dev/null

if [ "$health" != "200" ]; then echo "FAIL: GET /api/v1/health/ -> $health"; tail -30 "$SMOKE/uvicorn.log"; exit 1; fi
echo "PASS: GET /api/v1/health/ -> 200"
body=$(head -c 300 "$SMOKE/guest.json" | tr -d '\n' | sed -E 's/"(access_token|refresh_token)":"[^"]*"/"\1":"<redacted>"/g')
echo "INFO: POST /api/v1/guest-session -> $guest body=$body"
SH
cd "$UNITY" && git add README.md Server/README.md PROJECT_COMPLETION_PLAN.md
bash "$SCRATCH/smoke_server.sh"; echo "exit=$?"
```

Expected:
```
PASS: alembic upgrade head
PASS: init_tarot_data imported 78 cards and 6 spreads
PASS: check_config.py
PASS: GET /api/v1/health/ -> 200
INFO: POST /api/v1/guest-session -> <status> body=<redacted json>
exit=0
```

- 任何一行 `FAIL`：**STOP**，不要提交。说明 README 里的命令本身有问题，把日志报告给用户。
- `guest-session` 只作为记录项：搬运前后逐字节一致，也有测试覆盖，所以即使它不是 200 也不是本次搬运造成的。把状态码写进任务 8 的报告，作为 Phase 1 联调的线索，**不要**修改后端代码。
- 这次冒烟没有覆盖 Docker 那一节，因为本机没有验证过 Docker 环境，在报告中如实说明。

- [ ] **Step 8: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git status --porcelain | grep -v -E '^(M  README\.md|A  Server/README\.md|M  PROJECT_COMPLETION_PLAN\.md)$'; echo "(end unexpected changes)"
git commit -q -F - <<'MSG'
docs: rewrite root README, add Server README, record Phase 0 status

- README.md is now the single entry point: layout with Server/, the
  three run modes, security boundary, and where the old backend history
  lives (origin/backend-main at f19c7cd).
- Server/README.md documents setup, database init, startup, key config,
  tests and Docker for macOS/Linux and Windows. Its commands were run
  in a throwaway copy (alembic, reference data import, check_config,
  uvicorn health check).
- PROJECT_COMPLETION_PLAN.md: drop the resolved repository-boundary gap,
  add the 2026-09-11 execution record, and give Phase 0 a per-task
  status (the v0.9.0 tag is still missing).

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
MSG
git show --stat --format='%h %s' HEAD | tail -4
```

Expected: 第一条命令只输出 `(end unexpected changes)`；提交后显示 `3 files changed`，涉及 `PROJECT_COMPLETION_PLAN.md`、`README.md`、`Server/README.md`。

---

### Task 8: 最终验收与交接

**Files:** 无改动

**Interfaces:**
- Consumes: 提交 1 至 5、`verify_blobs.sh`、任务 1 的配置、任务 5 和任务 7 的输出
- Produces: 交给用户的验收报告；随后调用 superpowers:finishing-a-development-branch

- [ ] **Step 1: 检查提交序列**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git log --reverse --format='%s' main..chore/unify-repo
git diff --shortstat main..chore/unify-repo
```

Expected:
```
docs: add repo unification design spec and implementation plan
chore: add secret and backend runtime ignore rules
chore: import backend service into Server/ from Tarot@f19c7cd
ci: move backend tests workflow and scope it to Server/
docs: rewrite root README, add Server README, record Phase 0 status
```
另外输出 `87 files changed, …`（80 个后端文件，加上 spec、计划、`.gitignore`、workflow、`README.md`、`Server/README.md`、`PROJECT_COMPLETION_PLAN.md`）。

- [ ] **Step 2: 确认 80 个文件在导入提交和分支末端都保持不变**

```bash
source /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/repo-unify/env.sh
cd "$UNITY"
import_commit=$(git log --format=%H -1 --grep='^chore: import backend service into Server/' chore/unify-repo)
echo "import commit: $import_commit"
bash "$SCRATCH/verify_blobs.sh" "$import_commit" | tail -1
bash "$SCRATCH/verify_blobs.sh" chore/unify-repo | tail -1
```

Expected: 输出一个 40 位的提交号，后面两行都是 `PASS: 80/80 paths, modes and blob SHAs identical`。

- [ ] **Step 3: 检查边界**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git diff --stat main..chore/unify-repo -- UnityClient/; echo "(end UnityClient diff)"
git status --porcelain; echo "(end status)"
git status --porcelain --ignored | grep '^!! Server/'; echo "(end ignored Server artifacts)"
git ls-remote --heads origin main backend-main
git -C /Users/maochuandou/BUPT/Game/Tarot status -sb | head -1
git -C /Users/maochuandou/BUPT/Game/Tarot status --porcelain | wc -l | tr -d ' '
git -C /Users/maochuandou/BUPT/Game/Tarot config --get remote.origin.pushurl
```

Expected:
- `(end UnityClient diff)` 前面没有任何输出
- `(end status)` 前面没有任何输出
- 列出被忽略的 `Server/` 副产物（可能一个都没有），然后是 `(end ignored Server artifacts)`
- 远端 `main` 仍然是 `4ec3816…`，`backend-main` 仍然是 `f19c7cd…`（说明本计划没有推送任何东西）
- `## main...origin/backend-main`、`0`、`no_push`

- [ ] **Step 4: 向用户报告**（中文，写清楚以下内容）

1. 5 个提交的 hash 和 subject。
2. 证据：80/80 blob 比对结果；基线与 `Server/` 下两条 pytest 命令的摘要（从 `results/*/*.summary` 读取），并注明两者逐条一致；秘密扫描的对照命中数和结果；README 冒烟中每一行 PASS 的内容，以及 `guest-session` 的状态码。
3. 基线本身如果有失败或错误，原样列出。
4. 没有验证到的部分：GitHub Actions 要推送后才会运行；Docker 那一节没有做运行验证。
5. 本机新增的文件：
   - 仓库内（入库）：`docs/superpowers/specs/…`、`docs/superpowers/plans/…`、`.github/workflows/backend-tests.yml`、`Server/` 下 80 个文件、`Server/README.md`
   - 仓库内（被忽略、不入库）：Step 3 列出的 `Server/` 副产物
   - scratchpad（会话临时目录）：`repo-unify/` 下的 `env.sh`、`baseline-src/`、`results/`、`smoke/`，以及各个验证脚本
6. `Tarot/` 只改了 `.git/config` 的两项，附上撤销命令（spec §6.5）。

- [ ] **Step 5: 交接分支**

调用 superpowers:finishing-a-development-branch，让用户选择合并方式。**没有得到用户的明确同意之前，不执行 `git push`。** 如果用户决定合并到 `main` 并推送，推送前再确认一次：远端 `backend-main` 仍然是 `f19c7cd`。

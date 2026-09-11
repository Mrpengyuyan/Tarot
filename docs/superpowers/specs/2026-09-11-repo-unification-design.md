# Tarot 仓库归一设计（方案 A：单仓 + 归档分支）

- **日期：** 2026-09-11
- **状态：** 设计与 spec 均已批准（2026-09-11）；实施计划见 `docs/superpowers/plans/2026-09-11-repo-unification.md`
- **对应计划：** `PROJECT_COMPLETION_PLAN.md` Phase 0（范围冻结与仓库归一化）
- **涉及仓库：** `UnityTarot/`（产品仓库）、`Tarot/`（旧后端仓库，只读归档）

## 1. 背景与问题

本机有两个 Git 仓库，历史完全无关，却配置了同一个远端 `github.com/Mrpengyuyan/Tarot.git`：

| 本地目录 | 根提交 | 当前 HEAD | 远端对应分支 |
| --- | --- | --- | --- |
| `UnityTarot/` | `708d741` | `4ec3816` | `main` |
| `Tarot/` | `380665c` | `f19c7cd` | `backend-main` |

问题：

- `Tarot/` 的本地 `main` 追踪的是 `origin/main`，而这个引用最后一次抓取是 2026-04-15。所以 `git status` 显示 "ahead 4"，但这 4 个提交其实已经在远端 `backend-main` 上了。
- 远端 `main` 现在是 Unity 树。在 `Tarot/` 里 `git pull` 会去合并一段无关历史；`git push` 会试图把后端推到 Unity 的 `main`。普通推送会被拒绝，但一次 `--force` 就能抹掉 Unity 的全部历史。
- 计划书要求"一条主分支和一个明确的发行入口"，现在做不到。

## 2. 目标与非目标

**目标**

1. 只保留一个产品仓库（`UnityTarot/` → 远端 `main`），后端源码放进 `Server/`。
2. 后端旧历史完整保留，可以查到，不改写。
3. 让 `Tarot/` 本地仓库再也推不出去，`git status` 显示真实状态。
4. 用可复现的证据证明搬运前后文件一致、测试结果一致。

**非目标（本次不做）**

- 部署到 HTTPS（Phase 5）。
- 重写 `docker-compose.yml`。
- 删除 `Tarot/` 目录中的任何文件，或清理 React 代码。
- 修改后端业务逻辑。
- 恢复 Unity EditMode/PlayMode 测试基线（单独任务）。
- 推送到 GitHub（完成后先征求同意再推）。

## 3. 已做决策

| 编号 | 决策 | 理由 |
| --- | --- | --- |
| D1 | 采用方案 A：单仓，后端作为一次提交进入 `Server/`。不采用 B（subtree 嫁接历史）和 C（拆成两个仓库）。 | B 会带进 327M 的历史对象（主要是 78 张 UHD 图片），或者要先用 `filter-repo` 改写历史。C 发行时要手动对齐两个仓库的版本。远端已有的 `backend-main` 分支已经把历史保存好了，A 不需要付出这些代价。 |
| D2 | React Web 前端废弃，不进入新的代码树。 | 用户确认 1.0 只发 Unity 桌面客户端。 |
| D3 | 布局要同时适合 PaaS（root 设为 `Server/`）和 Docker（构建上下文设为 `Server/`）。 | 部署方式还没定。 |
| D4 | `docker-compose.yml` 暂不搬，等部署方式定了再重写。 | 它的 `frontend` 服务挂载的是已废弃的 React 源码，搬过去也跑不起来。 |
| D5 | 后端历史不嫁接进主线，保留在远端 `backend-main` @ `f19c7cd`。 | 只有 16 个提交、一个作者，追溯需求很低。需要时切到该分支查看。 |

## 4. 目标布局

```text
UnityTarot/                          → 远端 main
├── README.md                        重写：唯一权威入口
├── PROJECT_COMPLETION_PLAN.md       更新 Phase 0 状态
├── UNITY_FRONTEND_PLAN.md           不动
├── .gitignore                       合并后端规则
├── .github/workflows/
│   └── backend-tests.yml            从后端搬来，限定在 Server/
├── docs/superpowers/specs/
│   └── 2026-09-11-repo-unification-design.md
├── UnityClient/TarotUnity/          不改动任何文件
└── Server/
    ├── README.md                    新写
    ├── app/  tests/  alembic/  data/
    ├── alembic.ini  requirements.txt  setup.py
    ├── Dockerfile  .dockerignore  .env.example
    └── check_config.py  start_with_utf8.py
```

## 5. 文件归属

### 5.1 搬入 `Server/`（80 个已跟踪文件，来源 `f19c7cd`）

| 路径 | 文件数 | 说明 |
| --- | --- | --- |
| `app/` | 48 | FastAPI 应用 |
| `tests/` | 19 | 后端测试 |
| `alembic/` + `alembic.ini` | 3 + 1 | 数据库迁移 |
| `data/` | 2 | `spreads.json`、`tarotCards.json`，由 `app/scripts/init_tarot_data.py` 读取 |
| `requirements.txt`、`setup.py` | 2 | 依赖 |
| `Dockerfile`、`.dockerignore` | 2 | 后端镜像 |
| `.env.example` | 1 | 配置模板（已确认没有真实密钥，见 8.4） |
| `check_config.py`、`start_with_utf8.py` | 2 | 启动/配置辅助脚本 |

### 5.2 留在 `backend-main` 归档，不搬

| 路径 | 原因 |
| --- | --- |
| `src/`（101 个文件）、`public/`（321 个文件） | React 前端和它的卡牌图片 |
| `package.json`、`package-lock.json`、`tsconfig.json`、`fix_card.js` | 前端工具链 |
| `scripts/*.js`（5 个） | 前端图片处理脚本 |
| `scripts/smoke_ui_interpretation.py`、`scripts/smoke_ui_pages.py` | 浏览器冒烟脚本，测的是已废弃的 React UI |
| `.env.production` | 只有 CRA 构建变量 `GENERATE_SOURCEMAP` |
| `docker-compose.yml`、`start_demo.sh`、`start_demo.bat` | 编排包含已废弃的前端服务（D4） |
| `README.md` | 旧版扁平目录说明，有用内容并入新 README |
| `CODEX_REVIEW_AND_TEST_PROMPT.md` | 一次性审查提示词 |

### 5.3 搬入并改写

- `.github/workflows/backend-tests.yml` → 根目录 `.github/workflows/`（见 6.2）
- `.gitignore` 规则 → 合并进根目录 `.gitignore`（见 6.3）

## 6. 实现要点

### 6.1 导出方式

从 `Tarot/` 仓库按提交导出，不从工作区复制：

```bash
git -C Tarot archive f19c7cd -- <5.1 列出的路径> | tar -x -C UnityTarot/Server
```

`git archive` 只输出已提交的内容，所以未跟踪的 `.env`、`tarot_game.db`、`node_modules/`、`build/`、`__pycache__/` 都不会被带进来。

> **注意：** 在 zsh 里，路径列表必须用数组 `"${MOVE[@]}"` 传递。zsh 默认不对 `$VAR` 分词，整串会被当成一个路径，结果是 0 个文件匹配，而且不会报错。

### 6.2 CI workflow 改写

在原文件基础上只改三处：

```yaml
on:
  push:
    branches: ["**"]
    paths: ["Server/**", ".github/workflows/backend-tests.yml"]
  pull_request:
    paths: ["Server/**", ".github/workflows/backend-tests.yml"]

defaults:
  run:
    working-directory: Server
```

不加 `paths` 的话，每次提交 Unity 改动都会触发后端 CI，而且根目录没有 `requirements.txt`，这次 CI 一定会失败。

### 6.3 `.gitignore` 合并

在现有 Unity 规则后面追加两组：

```gitignore
# Secrets（全仓生效，安全防线）
.env
.env.*
!.env.example

# Backend runtime artifacts（限定 Server/）
Server/**/*.db
Server/**/*.sqlite
Server/**/*.sqlite3
Server/**/*.log
Server/**/.pytest_cache/
Server/**/*.egg-info/
Server/.venv/
Server/venv/
Server/app/scripts/reports/
```

已确认 Unity 仓库里没有任何已跟踪文件会被这些规则匹配到（`__pycache__`、`*.pyc`、`Logs/`、`Build/`、`.DS_Store` 已有全局规则覆盖）。

### 6.4 路径依赖核查（已完成）

| 检查项 | 结论 |
| --- | --- |
| 待搬代码是否引用归档文件（`public/`、`src/`、`smoke_ui`、`package.json`、`docker-compose` 等） | 没有引用 |
| `init_tarot_data.py` 定位 `data/` | `Path(__file__).parent.parent.parent / "data"`，`app/` 和 `data/` 一起搬，相对关系不变 |
| `Dockerfile` | 只 `COPY ./requirements.txt ./app ./data`，构建上下文设为 `Server/` 即可原样使用 |
| `alembic.ini` | `alembic/` 与之同级一起搬 |
| 测试数据库 | `tests/conftest.py` 使用 `tmp_path`，不在仓库里写数据库文件 |
| 配置加载 | `app/core/config.py` 从**当前工作目录**读 `.env`（见 8.1，会影响验证方法） |

### 6.5 拆雷（只改 `Tarot/.git/config`，可撤销）

```bash
git -C Tarot branch --set-upstream-to=origin/backend-main main
git -C Tarot remote set-url --push origin no_push
```

撤销方法：`git -C Tarot remote set-url --push origin ssh://git@ssh.github.com:443/Mrpengyuyan/Tarot.git`，然后把 upstream 设回原值。

### 6.6 分支与提交粒度

在 `UnityTarot/` 开分支 `chore/unify-repo`。**纯搬运**和**改写**分开提交，这样第 2 个提交可以逐字节核对：

1. `docs: add repo unification design spec`
2. `chore: import backend service into Server/ from Tarot@f19c7cd`（80 个文件，内容不做任何修改）
3. `ci: move backend tests workflow and scope it to Server/`
4. `chore: add secret and backend ignore rules`
5. `docs: rewrite root README, add Server README, mark Phase 0 done`

## 7. 安全护栏

- 不 force push，不改写任何已有提交。
- 不删除 `Tarot/` 目录中的任何文件；只修改 `Tarot/.git/config` 的两项配置。
- `UnityClient/` 下不改动任何文件。
- 分支合并回 `main` 后，要先告知用户、得到同意再 `git push`。
- 远端 `backend-main` 必须保持为 `f19c7cd`，不做删除或移动。

## 8. 验证方案

### 8.1 对照组基线（搬运前）

用 `git archive f19c7cd` 把后端完整导出到会话 scratchpad，在那里运行 `pytest -p no:cacheprovider`，记下通过数和失败/错误集合。

**为什么不在 `Tarot/` 原地跑：** `Tarot/` 下有一个未跟踪的真实 `.env`，而配置从当前目录读取 `.env`。原地跑时会加载这个文件，搬运后的 `Server/` 却没有，两边条件不同，比较就没有意义。scratchpad 里的干净导出和 `Server/`、CI 的条件完全一样，而且不会往 `Tarot/` 写任何文件。

### 8.2 搬运后

在 `UnityTarot/Server/` 里用**同一个 venv**运行同一条命令。

**通过条件：** 通过数与基线相同，失败/错误集合也相同。计划书记录的数字是 166 passed（2026-08-27），以实测基线为准。如果基线本身就不是全绿，先如实报告，不在本次任务里修。

### 8.3 venv 位置

venv 建在会话 scratchpad，不放进任何仓库。*（对已批准设计的调整：原设计是 `Server/.venv/`，改到 scratchpad 可以少在本机新增一个目录。）* 使用 Python 3.11（与 `Dockerfile` 一致；CI 用 3.10）。

### 8.4 逐字节一致性

Git 的 blob SHA 由内容决定，所以可以跨仓库直接比较：

- 在 `Tarot/` 中执行 `git ls-tree -r f19c7cd -- <5.1 路径>`
- 在 `UnityTarot/` 中对提交 2 执行 `git ls-tree -r HEAD -- Server/`，去掉 `Server/` 前缀

**通过条件：** 两边都是 80 行，路径和 blob SHA 完全一致。

### 8.5 秘密扫描

对 `Server/` 做模式扫描（`sk-…`、`ghp_…`、`AKIA…`、私钥头）。**必须带一个对照命中**（例如扫描 `DEEPSEEK_API_KEY` 这个变量名应该有结果），用来证明扫描器确实扫到了文件。

spec 编写期间已经在 `f19c7cd` 的 80 个文件上做过一次：对照命中 3 个文件，真实密钥模式 0 命中。`.env.example` 里的 `SECRET_KEY` 是 10 个小写单词用连字符连起来的短语，看形态是占位符。

### 8.6 其他检查

- `git diff --stat main..chore/unify-repo -- UnityClient/` 输出为空。
- `git status` 干净。
- 在 `Tarot/` 里 `git status` 显示追踪 `origin/backend-main` 且状态为 up to date；`git push --dry-run` 失败。

## 9. 文档改动

- **根 `README.md`：** 目录地图（`UnityClient/`、`Server/`、`docs/`）；三种运行方式（Unity 本地开发、本地后端联调、玩家发行包）；API Key 只在后端；旧后端历史位于 `backend-main` @ `f19c7cd`。
- **`Server/README.md`：** macOS 和 Windows 的 venv 创建与启动命令；从 `.env.example` 生成 `.env`；运行测试；初始化参考数据；说明 compose 暂缺（D4）。
- **`PROJECT_COMPLETION_PLAN.md`：** Phase 0 标记完成；2.2 删除"仓库边界需要统一"这一缺口；2.3 追加本次执行记录。

## 10. 风险与回滚

| 风险 | 处理 |
| --- | --- |
| Python 3.11 装不上依赖（例如 `bcrypt<4.0.0`） | 改用 conda 建 3.10 环境，与 CI 保持一致；两次运行必须使用同一个环境 |
| 基线本身有失败 | 如实记录，只要求搬运前后一致，不在本次修复 |
| 搬错或漏搬 | 8.4 的 blob SHA 比对会发现；分支还没合并，直接重做提交 2 |
| 整体回滚 | 删除本地分支 `chore/unify-repo`（需要用户授权），`main` 保持不变；`Tarot/` 按 6.5 撤销配置 |

## 11. 本次新增文件

| 文件 | 用途 | 是否入库 |
| --- | --- | --- |
| `docs/superpowers/specs/2026-09-11-repo-unification-design.md` | 本设计文档 | 是 |
| `Server/`（80 个文件） | 从 `Tarot@f19c7cd` 导出的后端源码 | 是 |
| `Server/README.md` | 后端启动说明 | 是 |
| `.github/workflows/backend-tests.yml` | 后端 CI | 是 |
| scratchpad 中的 venv 和基线导出 | 验证用 | 否（会话临时目录，不在仓库内） |

## 12. 后续（不在本次范围）

- Phase 5 确定部署方式后，在 `Server/` 下重写 compose 或 PaaS 配置。
- 恢复 Unity EditMode/PlayMode 测试基线。
- `Tarot/` 目录何时移出工作区，由用户自己决定。

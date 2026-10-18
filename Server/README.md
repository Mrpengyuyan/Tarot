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

- `SECRET_KEY`：`.env.example` 开启了 `REQUIRE_STRONG_SECRET=true`，服务会拒绝公开的示例值。可以用下面的命令生成：
  `python -c "import secrets; print(secrets.token_urlsafe(48))"`
- `DEEPSEEK_API_KEY`：需要真实 AI 解读时，替换成真实 Key。
- 正式部署时设置 `ENVIRONMENT=production`、`AUTH_COOKIE_SECURE=true` 和 `DEBUG=false`；开发环境即使关闭 `DEBUG`，仍会开放 `/docs`。

`.env` 已被仓库的 `.gitignore` 忽略，不要提交。

## 初始化数据库

```bash
alembic upgrade head
python -m app.scripts.init_tarot_data
```

第一条命令建表。第二条命令从 `data/tarotCards.json` 和 `data/spreads.json` 导入 78 张牌和 6 个牌阵。
已有数据库升级到此版本时也必须先停止旧后端进程，再运行 `alembic upgrade head`，最后启动新后端。迁移会把历史 AI 生成尝试保守地视为可能已经发送，避免部署后自动再次计费。

本次会话安全更新将登录凭证绑定到固定用户 ID，改用户名不会改变凭证所属账户。升级时必须停止全部旧进程，再统一启动新版本，不要新旧版本混跑。旧格式的访问和刷新凭证会统一失效：注册用户需重新登录；旧访客需开启新会话。账户和历史阅读不会被删除，但旧访客无法再用旧凭证访问其记录。此更新本身不需要新增数据库迁移。

## 启动

```bash
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

- Windows 控制台出现中文乱码时，改用 `python start_with_utf8.py`。
- `python check_config.py` 会打印当前生效的配置，其中数据库地址会脱敏。

启动后可以访问：

- `GET http://localhost:8000/api/v1/health/`：基础健康检查，Unity 启动时首先访问它。
- `POST http://localhost:8000/api/v1/guest-session`：申请访客会话。
- `POST http://localhost:8000/api/v1/records/{id}/interpret/async`：仅启动首次 AI 解读；重复调用不会自动重新发送付费请求。
- `GET http://localhost:8000/api/v1/records/{id}`：读取 `interpretation_run` 中的状态、已发送次数和是否可手动重试。
- `POST http://localhost:8000/api/v1/records/{id}/interpret/retry`：仅由玩家主动触发一次失败后的重试；同一局最多两次可能计费的请求。
- `http://localhost:8000/docs`：OpenAPI 文档，开发环境或 `DEBUG=true` 时可用，正式环境默认关闭。

更新接口中，省略字段表示保留原值；只有备注、评分等允许为空的字段才能通过 `null` 清空。牌阵支持 1 至 78 张牌、1 至 5 级难度，位置编号必须完整且不重复地覆盖 `1..card_count`；修改牌数时需要同时提供匹配的位置配置。

被阅读记录引用的卡牌和牌阵不能硬删除，接口会返回 `409`，保留原数据。牌阵默认删除操作仅将其停用：新阅读不能再使用它，已有阅读仍能继续抽牌、解读和查看历史。删除自己的阅读记录会一并清理对应的抽牌和解读，但不退还阅读次数或已预留、已消耗的 AI 预算。

创建阅读时牌阵恰好被删除，或删除阅读时恰好有抽牌、解读写入，接口会返回 `409` 并回滚本次操作，避免部分写入或删除。前者不会扣除阅读额度；后者保留完整记录，稍后可重新发起删除。

SQLite 的应用连接会自动启用外键约束，防止写入孤立关联；已有数据库无需为此新增迁移，重启后端即可生效。此保护不会自动修复此前已存在的孤立记录；使用其他工具直接操作 SQLite 时，需要在该工具的连接上启用外键约束。

停用账户不能登录、刷新会话或使用管理接口，已有凭证也不能绕过停用检查。管理员权限每次从数据库读取，撤销管理员身份后立即失去相应权限。

登录时，超出密码校验器限制、包含无效字符的密码，以及无法识别或损坏的密码哈希，会统一返回 `401`，不签发凭证、不回显密码或哈希、不修改账户数据。正常登录和现有密码算法保持不变；此处理不会自动修复损坏的密码记录。

注册和修改用户名时，先去掉首尾空白，再校验 3 至 50 字符及是否重名；保留大小写和内部空格。注册和修改邮箱时，规范化后的邮箱不能超过数据库现有的 100 字符上限。密码不做去空白处理，历史账户不会被自动清洗；已有值的读取和省略字段的更新行为保持不变，此修复不需要新增数据库迁移。

## 关键配置

完整列表见 `.env.example`。

| 变量 | `.env.example` 中的值 | 说明 |
| --- | --- | --- |
| `DATABASE_URL` | `sqlite:///./tarot_game.db` | 生产环境请换成 PostgreSQL 地址（依赖中已包含 `psycopg2-binary`） |
| `GUEST_DAILY_READING_LIMIT` | `3` | 访客每日阅读上限，删除阅读记录不退还次数 |
| `GUEST_SESSIONS_PER_HOUR` | `10` | 同一访问来源每 UTC 小时最多申请的访客会话数；超限返回 `429` 和 `Retry-After` |
| `USER_DAILY_READING_LIMIT` | `3` | 普通注册账号每日阅读上限；管理员不受玩家上限约束 |
| `AI_BUDGET_GUARD_ENABLED` | `true` | AI 预算保护；每次向模型服务请求前在数据库中预留额度 |
| `AUTO_CREATE_TABLES_ON_STARTUP` | `false` | 设为 `true` 后，启动时自动建表 |
| `AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP` | `false` | 设为 `true` 后，启动时自动导入牌和牌阵数据 |
| `AI_INTERPRETATION_STALE_SECONDS` | `300` | 旧领取参数；已发送请求不会因超时被自动重新发送 |
| `AI_INTERPRETATION_MAX_ATTEMPTS` | `3` | 首次任务领取保护；可能计费的 HTTP 请求另有固定的每局两次上限 |

在线解读每次只调用一个模型地址，不自动重试、跟随重定向或切换备用模型。首次失败后，初始接口和同步 `force_ai=true` 接口都不会再次联网；只有玩家明确调用 `/interpret/retry` 才能尝试第二次。旧客户端尚需接入这个新接口及状态提示。普通账号和访客的次数按 UTC 日计算，删除记录不会退还次数。问题和额外解读上下文各最多 2000 字符。

访客会话按服务端看到的连接来源计数，数据库只保存来源地址的密钥散列，旧计数会清理。它不是“每个真人”限额：共用网络的玩家可能共用额度，切换网络也可能绕过。应用不直接信任客户端提交的 `X-Forwarded-For`；经 HTTPS 代理部署时必须只信任代理本身转发的地址，并在入口另设限流。全局 AI 预算仍是成本保护的最后一道边界。

预算会在每次上游请求前按输入字节数、输出 token 上限及配置价格预留。网络超时或上游不提供用量时，预留额会继续占用额度，避免不确定费用被重复使用。在线解读如果没有启用持久预算保护，会直接拒绝付费请求。此机制不能保证与模型服务商的实际账单完全一致；请同时在服务商后台设置消费上限。

## 测试

```bash
python -m pytest -q tests
python -m pytest -q
```

第二条命令不带路径，所以还会收集 `app/scripts/` 下的 `test_*.py` 模块。GitHub Actions（仓库根目录的 `.github/workflows/backend-tests.yml`）只在 `Server/` 有改动时运行这两条命令。

测试包含 50 并发请求下的访客会话额度、单账号阅读额度，以及多个服务实例共享日/月预算的检查。模型服务在测试中使用模拟响应，不产生付费调用；同时检查上游错误回显不会把密钥或问题内容写入日志。正式部署后仍需在实际数据库和 HTTPS 入口重复验收。

写入完整性测试覆盖卡牌枚举的创建、批量创建与更新，以及空值拒绝、可空字段清空和牌阵数量/位置配置校验。无效请求会在写入前被拒绝。

删除完整性测试覆盖数据库外键保护、引用冲突、权限限制、牌阵停用后的旧流程、阅读关联数据的清理，以及删除后额度不退还、延迟 AI 任务不再执行的行为。

会话身份测试覆盖用户名重用后访问和刷新凭证不串号、旧格式凭证拒绝、异常身份字段和过期时间拒绝，以及停用账户、管理员权限撤销和账户不存在时的权限边界。

## Docker

镜像的构建上下文是 `Server/`：

```bash
docker build -t tarot-server .
docker run --rm --env-file .env tarot-server alembic upgrade head
docker run --rm --env-file .env tarot-server python -m app.scripts.init_tarot_data
docker run --rm -p 8000:8000 --env-file .env tarot-server
```

以上三次运行必须连接同一个持久数据库。可以使用容器可访问的 PostgreSQL；单实例、低流量部署也可使用 SQLite，但三次运行都要挂载同一个持久卷，并将 `DATABASE_URL` 指向卷内的绝对路径。示例中的相对 SQLite 路径会随容器删除，不适合这套命令。镜像包含迁移文件，但服务启动时不会自动执行迁移，以免多个实例同时修改数据库。`AUTO_CREATE_TABLES_ON_STARTUP` 和 `AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP` 仅适合本地临时体验，不替代正式迁移。

目前没有 `docker-compose.yml`：旧版编排包含已废弃的 React 前端，等部署方式确定后再重写。

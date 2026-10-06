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
- `http://localhost:8000/docs`：OpenAPI 文档，仅在 `DEBUG=true` 时可用。

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

## Docker

镜像的构建上下文是 `Server/`：

```bash
docker build -t tarot-server .
docker run --rm -p 8000:8000 --env-file .env tarot-server
```

容器启动时不会执行 `alembic upgrade head`。需要建表和导入数据时，在 `.env` 中把 `AUTO_CREATE_TABLES_ON_STARTUP` 和 `AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP` 设为 `true`。

目前没有 `docker-compose.yml`：旧版编排包含已废弃的 React 前端，等部署方式确定后再重写。

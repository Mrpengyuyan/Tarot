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
| `AI_INTERPRETATION_STALE_SECONDS` | `300` | 解读处于生成中超过这个秒数，视为卡住，可以重新开始生成 |
| `AI_INTERPRETATION_MAX_ATTEMPTS` | `3` | 每条记录最多开始生成解读的次数，超出后异步接口返回 `429` |

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

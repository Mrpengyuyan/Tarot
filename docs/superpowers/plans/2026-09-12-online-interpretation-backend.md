# 在线解读闭环 · 计划 1：后端 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 Unity 在线解读闭环提供后端能力：新增 `POST /records/{id}/interpret/async`，在后台生成 AI 解读；通过原子抢占防止重复调用 AI，并限制每条记录的生成次数；现有同步接口的行为保持不变。

**Architecture:** `predictions` 表新增两列（最近开始生成时间、已生成次数），用一条带条件的 UPDATE 抢占生成权。把同步接口里的生成逻辑提取成共用函数。异步接口抢占成功后，交给 FastAPI `BackgroundTasks` 执行；后台任务通过可替换的会话工厂自己打开数据库会话。客户端轮询现有的 `GET /records/{id}` 获取结果。

**Tech Stack:** Python 3.11.15（conda `tarot` 环境）、FastAPI 0.111.0、Starlette 0.37.2、SQLAlchemy 2.0.30、Alembic 1.13.1、pydantic-settings 2.2.1、pytest 8.4.1、SQLite 3.51.2。

**Spec:** `docs/superpowers/specs/2026-09-12-online-interpretation-loop-design.md`。本计划实现其中第 5 节和 8.2 节；Unity 部分与真实联调由计划 2 实现。

## Global Constraints

- 仓库：`/Users/maochuandou/BUPT/Game/UnityTarot`；分支：`feat/online-interpretation-loop`（起点 `78caed8`）；后端代码在 `Server/`。
- Python 解释器：`/opt/miniconda3/envs/tarot/bin/python`。**不得**向这个环境安装、升级或卸载任何包。
- 所有 pytest 都必须通过 `bash $SCRATCH/online-a-run/pt.sh` 运行。它在一个没有 `.env` 的目录里执行，并设置 `PYTHONPATH=Server`，这样用户以后创建的 `Server/.env`（里面有真实的 DeepSeek Key）既不会影响测试，也不会触发真实的 AI 调用。
- `$SCRATCH` = `/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad`。
- 同步接口 `POST /records/{id}/interpret` 的对外行为（200、幂等、400、504/502）保持不变。
- 新增配置的默认值：`AI_INTERPRETATION_STALE_SECONDS=300`，`AI_INTERPRETATION_MAX_ATTEMPTS=3`。
- 异步接口的返回约定：
  - 已有解读：200，结构与同步接口相同
  - 已开始生成或正在生成：202，`{"prediction_id": <id>, "status": "processing"}`
  - 尚未抽牌：400
  - 记录不存在或无权访问：404，`detail="Record not found"`
  - 生成次数用完：429，`detail="Interpretation attempts exhausted"`
- 迁移的 revision ID 不能超过 32 个字符，因为 PostgreSQL 的 `alembic_version.version_num` 是 `VARCHAR(32)`。
- zsh 注意事项：变量后面紧跟冒号时要写成 `${VAR}:`；路径列表要用数组。
- 禁止删除任何文件。创建文件前先确认它不存在（heredoc 之前执行 `set -C`）。修改已跟踪的文件时，使用"恰好匹配一次"的替换脚本；只要匹配次数不是 1，就不写入。
- 禁止 `git push`。每个任务结束时提交一次，提交信息必须以下面两行结尾：
  ```
  Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
  ```
- 遇到 **STOP**：立即停止，把输出原样报告给用户。

## 与 spec 的偏差

| 编号 | spec 的写法 | 本计划的做法 | 原因 |
| --- | --- | --- | --- |
| P1 | 一份计划同时覆盖前后端 | 拆成两份：本计划只做后端；Unity 与真实联调放在计划 2 | 后端现在就能开发并验证；Unity 需要许可证，而许可证目前无效。计划 2 会在本计划完成后，基于实际实现出来的接口编写 |
| P2 | 迁移文件名 `0002_interpretation_generation_tracking.py` | `0002_interpretation_tracking.py`，revision 为 `0002_interpretation_tracking` | 原名有 39 个字符，超过 PostgreSQL 的 `VARCHAR(32)` 限制 |
| P3 | 测试命令与 CI 相同 | 用 `pt.sh` 在没有 `.env` 的目录里运行同样的两组测试 | 避免用户的 `.env` 干扰测试或调用真实 AI。已实测，结果与 CI 写法一致（166 passed） |
| P4 | 8.2 节没有列出 | 额外新增"访客令牌调用异步接口"的覆盖测试 | Unity 是以访客身份调用接口的 |
| P5 | 10.1 节没有列出 `Server/README.md` | 在 README 中补充异步接口和两个配置项 | 让后端设置文档与实现保持一致 |

## 文件结构

| 文件 | 操作 | 职责 | 任务 |
| --- | --- | --- | --- |
| `Server/app/models/record.py` | 修改 | `Prediction` 新增两列 | 1 |
| `Server/alembic/versions/0002_interpretation_tracking.py` | 新增 | 以幂等方式为旧数据库补上两列 | 1 |
| `Server/tests/test_migration_0002.py` | 新增 | 迁移测试 | 1 |
| `Server/app/core/config.py` | 修改 | 新增两个配置项 | 2 |
| `Server/.env.example` | 修改 | 新增两个配置项 | 2 |
| `Server/README.md` | 修改 | 配置表加两行；接口列表加一行 | 2、4 |
| `Server/app/crud/prediction.py` | 修改 | 新增 `claim_interpretation_generation` | 2 |
| `Server/tests/test_interpretation_claim.py` | 新增 | 抢占函数与配置默认值的测试 | 2 |
| `Server/app/api/v1/endpoints/records.py` | 修改 | 提取生成函数；后台任务；异步接口 | 3、4 |
| `Server/app/db/session.py` | 修改 | 新增 `get_session_factory` | 4 |
| `Server/tests/conftest.py` | 修改 | 替换会话工厂 | 4 |
| `Server/tests/test_records_async_interpretation.py` | 新增 | 异步接口测试 | 4 |
| `Server/tests/test_guest_session.py` | 修改 | 访客续期、访客调用异步接口 | 5 |
| `Server/app/services/tarot_service.py` | 修改 | mock 解读改为中文 | 6 |
| `Server/tests/test_mock_interpretation_zh.py` | 新增 | mock 中文化的测试 | 6 |

scratch 目录（不入库）：`$SCRATCH/online-a-run/`（存放 `pt.sh`）、`$SCRATCH/online-a-smoke/`（任务 7 使用）。

**测试数量预期**（`pt.sh tests` 与 `pt.sh .` 的结果相同）：基线 166 → 任务 1 后 168 → 任务 2 后 176 → 任务 3 后 176 → 任务 4 后 184 → 任务 5 后 186 → 任务 6 后 188。

---

### Task 1: 数据库字段与迁移 0002

**Files:**
- Modify: `Server/app/models/record.py`（在 `completed_at` 定义之后）
- Create: `Server/alembic/versions/0002_interpretation_tracking.py`
- Create: `Server/tests/test_migration_0002.py`
- Create (scratch): `$SCRATCH/online-a-run/pt.sh`

**Interfaces:**
- Consumes: 无
- Produces:
  - `Prediction.interpretation_started_at: datetime | None`
  - `Prediction.interpretation_attempts: int`（默认 0）
  - Alembic revision `0002_interpretation_tracking`
  - `bash $SCRATCH/online-a-run/pt.sh <参数…>`：参数中的 `tests`、`tests/…`、`app/…` 会自动加上 `Server/` 前缀，`.` 表示整个 `Server` 目录

- [ ] **Step 1: 检查起点，创建测试运行器，跑出基线**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git rev-parse --abbrev-ref HEAD; git rev-parse --short HEAD; git status --porcelain; echo "(end status)"
SCRATCH=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
test ! -e "$SCRATCH/online-a-run" || { echo "STOP: $SCRATCH/online-a-run already exists"; exit 1; }
mkdir -p "$SCRATCH/online-a-run"
set -C
cat > "$SCRATCH/online-a-run/pt.sh" <<'SH'
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
bash "$SCRATCH/online-a-run/pt.sh" tests 2>&1 | tail -n 1
bash "$SCRATCH/online-a-run/pt.sh" . 2>&1 | tail -n 1
```

Expected：`feat/online-interpretation-loop`、`78caed8`、只输出 `(end status)`，然后两行都是 `166 passed, 1 warning in …`。任何一项不符：**STOP**。

- [ ] **Step 2: 写失败的迁移测试**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
test ! -e tests/test_migration_0002.py || { echo "STOP: file exists"; exit 1; }
set -C
cat > tests/test_migration_0002.py <<'PY'
from __future__ import annotations

import sqlite3
from pathlib import Path

import pytest
from alembic import command
from alembic.config import Config
from sqlalchemy import create_engine, inspect

from app.core.config import settings

SERVER_ROOT = Path(__file__).resolve().parents[1]
NEW_COLUMNS = {"interpretation_started_at", "interpretation_attempts"}
HEAD_REVISION = "0002_interpretation_tracking"


def _alembic_config() -> Config:
    config = Config()
    config.set_main_option("script_location", str(SERVER_ROOT / "alembic"))
    return config


def _prediction_columns(database_url: str) -> set[str]:
    engine = create_engine(database_url)
    try:
        return {column["name"] for column in inspect(engine).get_columns("predictions")}
    finally:
        engine.dispose()


def _current_revision(database_url: str) -> str:
    engine = create_engine(database_url)
    try:
        with engine.connect() as connection:
            return connection.exec_driver_sql("SELECT version_num FROM alembic_version").scalar_one()
    finally:
        engine.dispose()


@pytest.fixture()
def migration_db(tmp_path, monkeypatch):
    db_path = tmp_path / "migration.db"
    database_url = f"sqlite:///{db_path}"
    # alembic/env.py always replaces sqlalchemy.url with settings.DATABASE_URL.
    monkeypatch.setattr(settings, "DATABASE_URL", database_url)
    return db_path, database_url


def test_upgrade_head_on_fresh_database_has_generation_tracking(migration_db):
    _, database_url = migration_db

    command.upgrade(_alembic_config(), "head")

    assert NEW_COLUMNS <= _prediction_columns(database_url)
    assert _current_revision(database_url) == HEAD_REVISION
    assert len(HEAD_REVISION) <= 32


def test_upgrade_adds_missing_columns_to_legacy_predictions_table(migration_db):
    db_path, database_url = migration_db
    config = _alembic_config()
    command.upgrade(config, "0001_initial_schema")

    # 0001 builds tables from the current models; drop the new columns to recreate a pre-0002 schema.
    connection = sqlite3.connect(db_path)
    try:
        connection.execute("ALTER TABLE predictions DROP COLUMN interpretation_attempts")
        connection.execute("ALTER TABLE predictions DROP COLUMN interpretation_started_at")
        connection.commit()
    finally:
        connection.close()
    assert not NEW_COLUMNS & _prediction_columns(database_url)

    command.upgrade(config, "head")

    assert NEW_COLUMNS <= _prediction_columns(database_url)
    assert _current_revision(database_url) == HEAD_REVISION
PY
```

- [ ] **Step 3: 运行测试，确认失败**

```bash
bash /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh tests/test_migration_0002.py 2>&1 | tail -n 4
```

Expected：`2 failed`。第一个测试因为缺列或 revision 仍为 `0001_initial_schema` 而断言失败；第二个测试报 `sqlite3.OperationalError: no such column`。

- [ ] **Step 4: 给模型加两列**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
/opt/miniconda3/envs/tarot/bin/python - <<'PY'
from pathlib import Path
p = Path("app/models/record.py")
t = p.read_text(encoding="utf-8")
old = '    completed_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True, comment="完成时间")\n'
new = old + r'''    interpretation_started_at: Mapped[datetime | None] = mapped_column(
        DateTime(timezone=True),
        nullable=True,
        comment="最近一次开始生成解读的时间",
    )
    interpretation_attempts: Mapped[int] = mapped_column(
        Integer,
        default=0,
        server_default="0",
        nullable=False,
        comment="已开始生成解读的次数",
    )
'''
if t.count(old) != 1:
    raise SystemExit(f"FAIL: anchor count {t.count(old)} — nothing written")
p.write_text(t.replace(old, new), encoding="utf-8")
print("OK: Prediction columns added")
PY
```

- [ ] **Step 5: 创建迁移文件**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
test ! -e alembic/versions/0002_interpretation_tracking.py || { echo "STOP: file exists"; exit 1; }
set -C
cat > alembic/versions/0002_interpretation_tracking.py <<'PY'
"""Track interpretation generation attempts

Revision ID: 0002_interpretation_tracking
Revises: 0001_initial_schema
Create Date: 2026-09-12 00:00:00
"""

from alembic import op
import sqlalchemy as sa

# revision identifiers, used by Alembic.
revision = "0002_interpretation_tracking"
down_revision = "0001_initial_schema"
branch_labels = None
depends_on = None


def _prediction_columns() -> set[str]:
    return {column["name"] for column in sa.inspect(op.get_bind()).get_columns("predictions")}


def upgrade() -> None:
    # 0001 creates tables from the current models, so fresh databases already have these columns.
    existing = _prediction_columns()
    if "interpretation_started_at" not in existing:
        op.add_column(
            "predictions",
            sa.Column("interpretation_started_at", sa.DateTime(timezone=True), nullable=True),
        )
    if "interpretation_attempts" not in existing:
        op.add_column(
            "predictions",
            sa.Column("interpretation_attempts", sa.Integer(), nullable=False, server_default="0"),
        )


def downgrade() -> None:
    existing = _prediction_columns()
    with op.batch_alter_table("predictions") as batch_op:
        if "interpretation_attempts" in existing:
            batch_op.drop_column("interpretation_attempts")
        if "interpretation_started_at" in existing:
            batch_op.drop_column("interpretation_started_at")
PY
```

- [ ] **Step 6: 运行迁移测试和全量测试**

```bash
PT=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh
bash "$PT" tests/test_migration_0002.py 2>&1 | tail -n 1
bash "$PT" tests 2>&1 | tail -n 1
bash "$PT" . 2>&1 | tail -n 1
```

Expected：`2 passed`；随后两行都是 `168 passed, 1 warning`。

- [ ] **Step 7: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git status --porcelain
git add Server/app/models/record.py Server/alembic/versions/0002_interpretation_tracking.py Server/tests/test_migration_0002.py
git commit -q -F - <<'MSG'
feat(server): track interpretation generation on predictions

Add interpretation_started_at and interpretation_attempts to predictions,
with an idempotent 0002 migration (0001 builds tables from the current
models, so fresh databases already have the columns).

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
MSG
git show --stat --format='%h %s' HEAD
```

Expected：提交前 `git status` 只列出上面 3 个文件；提交后显示 `3 files changed`。

---

### Task 2: 配置项与抢占函数

**Files:**
- Modify: `Server/app/core/config.py`（在 `GUEST_DAILY_READING_LIMIT` 之后）
- Modify: `Server/app/crud/prediction.py`（import 行；在 `update_prediction_status` 之后新增函数）
- Modify: `Server/.env.example`、`Server/README.md`
- Create: `Server/tests/test_interpretation_claim.py`

**Interfaces:**
- Consumes: Task 1 新增的两列
- Produces:
  - `settings.AI_INTERPRETATION_STALE_SECONDS: int`（300）
  - `settings.AI_INTERPRETATION_MAX_ATTEMPTS: int`（3）
  - `prediction_crud.claim_interpretation_generation(db: Session, prediction_id: int, now: datetime, stale_before: datetime, max_attempts: int) -> bool`

- [ ] **Step 1: 写失败的测试**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
test ! -e tests/test_interpretation_claim.py || { echo "STOP: file exists"; exit 1; }
set -C
cat > tests/test_interpretation_claim.py <<'PY'
from __future__ import annotations

from datetime import datetime, timedelta, timezone

from app.core.config import Settings
from app.crud import prediction as prediction_crud
from app.models.record import Interpretation, Prediction, PredictionStatus, QuestionType
from app.models.user import User

MAX_ATTEMPTS = 3


def _now() -> datetime:
    return datetime.now(timezone.utc)


def _make_prediction(db_session, spread_id: int, username: str = "claim_user") -> Prediction:
    user = User(
        username=username,
        email=f"{username}@example.com",
        hashed_password="not-used-in-this-test",
        nickname="claim",
        is_active=True,
        is_superuser=False,
    )
    db_session.add(user)
    db_session.commit()
    prediction = Prediction(
        user_id=user.id,
        spread_type_id=spread_id,
        question="我接下来该专注什么？",
        question_type=QuestionType.GENERAL,
    )
    db_session.add(prediction)
    db_session.commit()
    db_session.refresh(prediction)
    return prediction


def _claim(db_session, prediction_id: int, now: datetime, stale_seconds: int = 300) -> bool:
    return prediction_crud.claim_interpretation_generation(
        db_session,
        prediction_id=prediction_id,
        now=now,
        stale_before=now - timedelta(seconds=stale_seconds),
        max_attempts=MAX_ATTEMPTS,
    )


def test_generation_tracking_settings_defaults():
    defaults = Settings(_env_file=None)

    assert defaults.AI_INTERPRETATION_STALE_SECONDS == 300
    assert defaults.AI_INTERPRETATION_MAX_ATTEMPTS == 3


def test_first_claim_marks_prediction_processing(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])

    assert _claim(db_session, prediction.id, _now()) is True

    db_session.refresh(prediction)
    assert prediction.status == PredictionStatus.PROCESSING
    assert prediction.interpretation_attempts == 1
    assert prediction.interpretation_started_at is not None


def test_second_claim_while_fresh_processing_is_rejected(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])
    started = _now()
    assert _claim(db_session, prediction.id, started) is True

    assert _claim(db_session, prediction.id, started + timedelta(seconds=10)) is False

    db_session.refresh(prediction)
    assert prediction.interpretation_attempts == 1


def test_stale_processing_can_be_reclaimed(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])
    started = _now()
    assert _claim(db_session, prediction.id, started) is True

    assert _claim(db_session, prediction.id, started + timedelta(seconds=301)) is True

    db_session.refresh(prediction)
    assert prediction.interpretation_attempts == 2


def test_failed_prediction_can_be_reclaimed(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])
    started = _now()
    assert _claim(db_session, prediction.id, started) is True
    prediction_crud.update_prediction_status(db_session, prediction_id=prediction.id, status=PredictionStatus.FAILED)

    assert _claim(db_session, prediction.id, started + timedelta(seconds=5)) is True


def test_claim_rejected_after_max_attempts(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])
    started = _now()
    for attempt in range(MAX_ATTEMPTS):
        assert _claim(db_session, prediction.id, started + timedelta(seconds=attempt)) is True
        prediction_crud.update_prediction_status(
            db_session,
            prediction_id=prediction.id,
            status=PredictionStatus.FAILED,
        )

    assert _claim(db_session, prediction.id, started + timedelta(seconds=60)) is False

    db_session.refresh(prediction)
    assert prediction.interpretation_attempts == MAX_ATTEMPTS


def test_claim_rejected_when_interpretation_exists(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])
    db_session.add(Interpretation(prediction_id=prediction.id, overall_interpretation="已经生成"))
    db_session.commit()

    assert _claim(db_session, prediction.id, _now()) is False


def test_claim_unknown_prediction_returns_false(db_session):
    assert _claim(db_session, 999999, _now()) is False
PY
bash /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh tests/test_interpretation_claim.py 2>&1 | tail -n 3
```

Expected：`8 failed`。设置默认值的测试报 `AttributeError`（`Settings` 没有这个属性）；其余测试报 `AttributeError: module 'app.crud.prediction' has no attribute 'claim_interpretation_generation'`。

- [ ] **Step 2: 添加配置项**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
/opt/miniconda3/envs/tarot/bin/python - <<'PY'
from pathlib import Path
p = Path("app/core/config.py")
t = p.read_text(encoding="utf-8")
old = "    GUEST_DAILY_READING_LIMIT: int = 3\n"
new = old + r'''    # Background interpretation generation (POST /records/{id}/interpret/async).
    # Unity's InterpretationPoller gives up after STALE_SECONDS + 30s; keep the two in sync.
    AI_INTERPRETATION_STALE_SECONDS: int = 300
    AI_INTERPRETATION_MAX_ATTEMPTS: int = 3
'''
if t.count(old) != 1:
    raise SystemExit(f"FAIL: anchor count {t.count(old)} — nothing written")
p.write_text(t.replace(old, new), encoding="utf-8")
print("OK: settings added")
PY
```

- [ ] **Step 3: 实现抢占函数**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
/opt/miniconda3/envs/tarot/bin/python - <<'PY'
from pathlib import Path
p = Path("app/crud/prediction.py")
t = p.read_text(encoding="utf-8")
edits = [
    (
        "from sqlalchemy import and_, or_, desc, asc, func, update\n",
        "from sqlalchemy import and_, or_, desc, asc, func, select, update\n",
    ),
    (
        "def delete_prediction(db: Session, prediction_id: int) -> bool:\n",
        r'''def claim_interpretation_generation(
    db: Session,
    prediction_id: int,
    now: datetime,
    stale_before: datetime,
    max_attempts: int,
) -> bool:
    """原子地抢占解读生成权。

    只有在记录还没有解读、生成次数未达上限，并且不处于有效的生成中
    （状态不是 PROCESSING，或者 PROCESSING 已经早于 stale_before）时才会成功。
    并发调用时最多只有一个返回 True。
    """
    has_interpretation = select(Interpretation.id).where(Interpretation.prediction_id == prediction_id).exists()
    statement = (
        update(Prediction)
        .where(Prediction.id == prediction_id)
        .where(Prediction.interpretation_attempts < max_attempts)
        .where(~has_interpretation)
        .where(
            or_(
                Prediction.status != PredictionStatus.PROCESSING,
                Prediction.interpretation_started_at.is_(None),
                Prediction.interpretation_started_at < stale_before,
            )
        )
        .values(
            status=PredictionStatus.PROCESSING,
            interpretation_started_at=now,
            interpretation_attempts=Prediction.interpretation_attempts + 1,
        )
        .execution_options(synchronize_session=False)
    )
    result = db.execute(statement)
    db.commit()
    return result.rowcount == 1

def delete_prediction(db: Session, prediction_id: int) -> bool:
''',
    ),
]
for old, new in edits:
    if t.count(old) != 1:
        raise SystemExit(f"FAIL: anchor {old[:40]!r} count {t.count(old)} — nothing written")
for old, new in edits:
    t = t.replace(old, new)
p.write_text(t, encoding="utf-8")
print("OK: claim_interpretation_generation added")
PY
bash /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh tests/test_interpretation_claim.py 2>&1 | tail -n 1
```

Expected：`8 passed`。

- [ ] **Step 4: 更新 `.env.example` 和 README**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
/opt/miniconda3/envs/tarot/bin/python - <<'PY'
from pathlib import Path
edits = {
    ".env.example": [(
        "GUEST_DAILY_READING_LIMIT=3\n",
        "GUEST_DAILY_READING_LIMIT=3\nAI_INTERPRETATION_STALE_SECONDS=300\nAI_INTERPRETATION_MAX_ATTEMPTS=3\n",
    )],
    "README.md": [(
        "| `AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP` | `false` | 设为 `true` 后，启动时自动导入牌和牌阵数据 |\n",
        "| `AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP` | `false` | 设为 `true` 后，启动时自动导入牌和牌阵数据 |\n"
        "| `AI_INTERPRETATION_STALE_SECONDS` | `300` | 解读处于生成中超过这个秒数，视为卡住，可以重新开始生成 |\n"
        "| `AI_INTERPRETATION_MAX_ATTEMPTS` | `3` | 每条记录最多开始生成解读的次数，超出后异步接口返回 `429` |\n",
    )],
}
for name, pairs in edits.items():
    p = Path(name)
    t = p.read_text(encoding="utf-8")
    for old, new in pairs:
        if t.count(old) != 1:
            raise SystemExit(f"FAIL: {name} anchor count {t.count(old)} — nothing written")
    for old, new in pairs:
        t = t.replace(old, new)
    p.write_text(t, encoding="utf-8")
    print(f"OK: {name} updated")
PY
```

- [ ] **Step 5: 全量测试**

```bash
PT=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh
bash "$PT" tests 2>&1 | tail -n 1
bash "$PT" . 2>&1 | tail -n 1
```

Expected：两行都是 `176 passed, 1 warning`（其中包含 `test_repo_hygiene`，证明 `.env.example` 里的占位值仍符合规则）。

- [ ] **Step 6: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git status --porcelain
git add Server/app/core/config.py Server/app/crud/prediction.py Server/.env.example Server/README.md Server/tests/test_interpretation_claim.py
git commit -q -F - <<'MSG'
feat(server): atomic claim for interpretation generation

claim_interpretation_generation moves a prediction to PROCESSING with a
single conditional UPDATE: no interpretation yet, attempts below the cap,
and not already processing unless that run is stale. Adds
AI_INTERPRETATION_STALE_SECONDS (300) and AI_INTERPRETATION_MAX_ATTEMPTS (3).

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
MSG
git show --stat --format='%h %s' HEAD
```

Expected：提交前 `git status` 只列出上面 5 个文件；提交后显示 `5 files changed`。

---

### Task 3: 提取生成逻辑（纯重构，不改变行为）

**Files:**
- Modify: `Server/app/api/v1/endpoints/records.py`（替换同步解读接口所在的整个代码段）

**Interfaces:**
- Consumes: 无
- Produces（模块内函数，供 Task 4 使用）：
  - `async def _build_ai_interpretation_create(db: Session, prediction: PredictionModel, user_context: Optional[str]) -> InterpretationCreate`：AI 失败时把记录标为 FAILED，并抛出 504 或 502
  - `def _store_interpretation(db: Session, prediction_id: int, interpretation_create: InterpretationCreate)`：存库并把记录标为 COMPLETED；遇到唯一约束冲突时返回已有的解读

- [ ] **Step 1: 记录现有解读相关测试的通过数**

```bash
PT=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh
bash "$PT" tests/test_records_interpretation_flow.py tests/test_guest_session.py tests/test_smoke_e2e_flow.py tests/test_records_edge_cases.py tests/test_records_list_queries.py 2>&1 | tail -n 1
```

Expected：`N passed`，没有 failed 或 error。记下 N，Step 3 必须得到相同的 N。

- [ ] **Step 2: 替换同步接口代码段**

把从 `@router.post("/{prediction_id:int}/interpret", …)` 开始、到 `@router.get("/{prediction_id:int}/interpretation", …)` 之前的整段代码，替换为两个辅助函数加上变薄后的接口：

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
/opt/miniconda3/envs/tarot/bin/python - <<'PY'
from pathlib import Path
p = Path("app/api/v1/endpoints/records.py")
t = p.read_text(encoding="utf-8")
start_anchor = '@router.post("/{prediction_id:int}/interpret", response_model=Interpretation, summary="Create AI interpretation")\n'
end_anchor = '@router.get("/{prediction_id:int}/interpretation", response_model=Interpretation, summary="Get interpretation")\n'
for anchor in (start_anchor, end_anchor):
    if t.count(anchor) != 1:
        raise SystemExit(f"FAIL: anchor count {t.count(anchor)} for {anchor[:60]!r} — nothing written")
start = t.index(start_anchor)
end = t.index(end_anchor)
if start >= end:
    raise SystemExit("FAIL: anchors out of order — nothing written")
new_block = r'''async def _build_ai_interpretation_create(
    db: Session,
    prediction: PredictionModel,
    user_context: Optional[str],
) -> InterpretationCreate:
    """Generate interpretation content with the AI service.

    On AI failure the prediction is marked FAILED and 504/502 is raised,
    which is the behavior the synchronous endpoint has always had.
    """
    prediction_id = prediction.id
    try:
        card_draws = prediction_crud.get_prediction_card_draws(db, prediction_id=prediction_id)
        if not card_draws:
            raise HTTPException(status_code=400, detail="Cards must be drawn before interpretation")

        spread = spread_crud.get_spread_by_id(db, spread_id=prediction.spread_type_id)
        cards_data = []
        for draw in card_draws:
            card = card_crud.get_card_by_id(db, card_id=draw.tarot_card_id)
            if not card:
                continue
            position_name = spread.get_position_name(draw.position) if spread else f"Position {draw.position}"
            cards_data.append(
                {
                    "card": card,
                    "position": position_name,
                    "is_reversed": draw.is_reversed,
                }
            )

        if len(cards_data) != len(card_draws):
            raise HTTPException(
                status_code=500,
                detail="Card data is incomplete; please redraw cards",
            )

        ai_payload = await tarot_interpretation_service.create_interpretation(
            db=db,
            prediction=prediction,
            cards_data=cards_data,
            user_context=user_context,
        )

        return InterpretationCreate(
            overall_interpretation=ai_payload.get("overall_interpretation", ""),
            card_analysis=ai_payload.get("card_analysis"),
            relationship_analysis=ai_payload.get("relationship_analysis"),
            advice=ai_payload.get("advice"),
            warning=ai_payload.get("warning"),
            summary=ai_payload.get("summary"),
            key_themes=_normalize_key_themes(ai_payload.get("key_themes")),
            model_used=ai_payload.get(
                "model_used",
                tarot_interpretation_service.default_model_name()
                if tarot_interpretation_service.ai_service.is_configured()
                else "mock_ai",
            ),
            model_version=ai_payload.get("model_version"),
            confidence_score=(
                ai_payload.get("confidence_score")
                if ai_payload.get("confidence_score") is not None
                else 0.85
            ),
        )
    except HTTPException:
        raise
    except CozeTimeoutError as exc:
        logger.error("AI interpretation timed out: %s", exc)
        prediction_crud.update_prediction_status(
            db,
            prediction_id=prediction_id,
            status=PredictionStatus.FAILED,
        )
        raise HTTPException(status_code=504, detail="AI interpretation request timed out") from exc
    except CozeHttpStatusError as exc:
        logger.error("AI interpretation provider HTTP error: %s", exc)
        prediction_crud.update_prediction_status(
            db,
            prediction_id=prediction_id,
            status=PredictionStatus.FAILED,
        )
        raise HTTPException(status_code=502, detail="AI interpretation upstream service error") from exc
    except (CozeRequestError, CozeError) as exc:
        logger.error("AI interpretation upstream request failed: %s", exc)
        prediction_crud.update_prediction_status(
            db,
            prediction_id=prediction_id,
            status=PredictionStatus.FAILED,
        )
        raise HTTPException(status_code=502, detail="AI interpretation request failed") from exc
    except Exception as exc:
        logger.error("AI interpretation generation failed: %s", exc)
        prediction_crud.update_prediction_status(
            db,
            prediction_id=prediction_id,
            status=PredictionStatus.FAILED,
        )
        raise HTTPException(status_code=502, detail="AI interpretation service unavailable") from exc


def _store_interpretation(
    db: Session,
    prediction_id: int,
    interpretation_create: InterpretationCreate,
):
    """Persist an interpretation and mark the prediction COMPLETED; return the existing one on a race."""
    try:
        interpretation = prediction_crud.create_interpretation(
            db,
            prediction_id=prediction_id,
            interpretation_create=interpretation_create,
        )
    except IntegrityError:
        db.rollback()
        interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
        if interpretation:
            return interpretation
        raise

    prediction_crud.update_prediction_status(
        db,
        prediction_id=prediction_id,
        status=PredictionStatus.COMPLETED,
    )
    return interpretation


@router.post("/{prediction_id:int}/interpret", response_model=Interpretation, summary="Create AI interpretation")
async def create_ai_interpretation(
    prediction_id: int,
    interpretation_create: Optional[InterpretationCreate] = None,
    user_context: Optional[str] = Query(None, description="Additional user context"),
    force_ai: bool = Query(False, description="Force AI generation even when manual payload is provided"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser and not prediction_crud.validate_prediction_ownership(
        db,
        prediction_id=prediction_id,
        user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")

    prediction = prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
    if not prediction:
        raise HTTPException(status_code=404, detail="Record not found")

    existing_interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
    if existing_interpretation:
        if prediction.status != PredictionStatus.COMPLETED or prediction.completed_at is None:
            prediction_crud.update_prediction_status(
                db,
                prediction_id=prediction_id,
                status=PredictionStatus.COMPLETED,
            )
        return existing_interpretation

    if not interpretation_create or force_ai:
        interpretation_create = await _build_ai_interpretation_create(db, prediction, user_context)

    return _store_interpretation(db, prediction_id, interpretation_create)


'''
p.write_text(t[:start] + new_block + t[end:], encoding="utf-8")
print("OK: synchronous interpret endpoint refactored")
PY
```

- [ ] **Step 3: 重跑 Step 1 的测试，再跑全量**

```bash
PT=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh
bash "$PT" tests/test_records_interpretation_flow.py tests/test_guest_session.py tests/test_smoke_e2e_flow.py tests/test_records_edge_cases.py tests/test_records_list_queries.py 2>&1 | tail -n 1
bash "$PT" tests 2>&1 | tail -n 1
bash "$PT" . 2>&1 | tail -n 1
```

Expected：第一行是与 Step 1 相同的 `N passed`；随后两行都是 `176 passed, 1 warning`。只要有任何失败：**STOP**，说明重构改变了行为。

- [ ] **Step 4: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git status --porcelain
git add Server/app/api/v1/endpoints/records.py
git commit -q -F - <<'MSG'
refactor(server): extract interpretation generation helpers

Move AI generation and persistence out of the synchronous interpret
endpoint into _build_ai_interpretation_create and _store_interpretation so
background generation can reuse them. Endpoint behavior is unchanged.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
MSG
git show --stat --format='%h %s' HEAD
```

Expected：提交前只有 `M Server/app/api/v1/endpoints/records.py`；提交后显示 `1 file changed`。

---

### Task 4: 会话工厂、后台任务与异步接口

**Files:**
- Modify: `Server/app/db/session.py`
- Modify: `Server/tests/conftest.py`
- Modify: `Server/app/api/v1/endpoints/records.py`（import；在 `@router.get("/{prediction_id:int}/interpretation"` 之前插入新代码段）
- Modify: `Server/README.md`
- Create: `Server/tests/test_records_async_interpretation.py`

**Interfaces:**
- Consumes: Task 2 的 `claim_interpretation_generation` 与两个配置项；Task 3 的 `_build_ai_interpretation_create`、`_store_interpretation`
- Produces:
  - `app.db.session.get_session_factory() -> Callable[[], Session]`
  - `records.generate_and_store_interpretation(db, prediction, user_context)`（async）
  - `records.run_interpretation_job(session_factory, prediction_id: int, user_context: Optional[str]) -> None`（async）
  - HTTP：`POST /api/v1/records/{prediction_id}/interpret/async`，返回约定见 Global Constraints

- [ ] **Step 1: 写失败的测试**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
test ! -e tests/test_records_async_interpretation.py || { echo "STOP: file exists"; exit 1; }
set -C
cat > tests/test_records_async_interpretation.py <<'PY'
from __future__ import annotations

from datetime import datetime, timedelta, timezone

import pytest

from app.api.v1.endpoints import records as records_endpoint
from app.core.config import settings
from app.crud import prediction as prediction_crud
from app.services.coze_service import CozeTimeoutError


def _register_and_login(client, username: str) -> None:
    register_resp = client.post(
        "/api/v1/register",
        json={"username": username, "email": f"{username}@example.com", "password": "password123"},
    )
    assert register_resp.status_code == 200
    login_resp = client.post("/api/v1/login", data={"username": username, "password": "password123"})
    assert login_resp.status_code == 200
    csrf_token = login_resp.cookies.get(settings.CSRF_COOKIE_NAME) or client.cookies.get(settings.CSRF_COOKIE_NAME)
    if csrf_token:
        client.headers.update({settings.CSRF_HEADER_NAME: csrf_token})


def _create_prediction(client, spread_id: int) -> int:
    create_resp = client.post(
        "/api/v1/records/",
        json={"spread_type_id": spread_id, "question": "我接下来该专注什么？", "question_type": "general"},
    )
    assert create_resp.status_code == 200
    return create_resp.json()["id"]


def _create_drawn_prediction(client, spread_id: int) -> int:
    prediction_id = _create_prediction(client, spread_id)
    draw_resp = client.post(f"/api/v1/records/{prediction_id}/draw")
    assert draw_resp.status_code == 200
    return prediction_id


def _start_async(client, prediction_id: int):
    return client.post(f"/api/v1/records/{prediction_id}/interpret/async")


def _record(client, prediction_id: int) -> dict:
    detail_resp = client.get(f"/api/v1/records/{prediction_id}")
    assert detail_resp.status_code == 200
    return detail_resp.json()


@pytest.fixture()
def fake_ai(monkeypatch):
    state = {"count": 0, "error": None}

    async def fake_create_interpretation(db, prediction, cards_data, user_context=None):  # noqa: ANN001
        state["count"] += 1
        if state["error"] is not None:
            raise state["error"]
        return {
            "overall_interpretation": "牌面显示稳步前进。",
            "summary": "保持节奏。",
            "advice": "先完成一件最重要的事。",
            "key_themes": ["节奏", "专注"],
            "model_used": "unit_test_ai",
            "confidence_score": 0.9,
        }

    monkeypatch.setattr(
        records_endpoint.tarot_interpretation_service,
        "create_interpretation",
        fake_create_interpretation,
    )
    return state


def test_async_interpretation_returns_202_and_completes_in_background(client, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_happy")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 202
    assert resp.json() == {"prediction_id": prediction_id, "status": "processing"}
    detail = _record(client, prediction_id)
    assert detail["status"] == "completed"
    assert detail["interpretation"]["model_used"] == "unit_test_ai"
    assert fake_ai["count"] == 1


def test_async_interpretation_returns_existing_interpretation_with_200(client, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_existing")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    assert _start_async(client, prediction_id).status_code == 202

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 200
    body = resp.json()
    assert body["prediction_id"] == prediction_id
    assert body["model_used"] == "unit_test_ai"
    assert body["overall_interpretation"] == "牌面显示稳步前进。"
    assert fake_ai["count"] == 1


def test_async_interpretation_does_not_start_twice_while_processing(
    client, db_session, seeded_spread_and_cards, fake_ai
):
    _register_and_login(client, "async_processing")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    now = datetime.now(timezone.utc)
    assert prediction_crud.claim_interpretation_generation(
        db_session,
        prediction_id=prediction_id,
        now=now,
        stale_before=now - timedelta(seconds=300),
        max_attempts=3,
    )

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 202
    assert resp.json() == {"prediction_id": prediction_id, "status": "processing"}
    assert fake_ai["count"] == 0
    assert _record(client, prediction_id)["status"] == "processing"


def test_async_interpretation_requires_drawn_cards(client, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_no_draw")
    prediction_id = _create_prediction(client, seeded_spread_and_cards["spread_id"])

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 400
    assert resp.json()["detail"] == "Cards must be drawn before interpretation"
    assert fake_ai["count"] == 0


def test_async_interpretation_hides_other_users_records(client, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_owner")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    _register_and_login(client, "async_intruder")

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 404
    assert resp.json()["detail"] == "Record not found"
    assert fake_ai["count"] == 0


def test_async_interpretation_failure_marks_failed_and_retry_succeeds(client, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_retry")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    fake_ai["error"] = CozeTimeoutError("simulated upstream timeout")

    assert _start_async(client, prediction_id).status_code == 202
    assert _record(client, prediction_id)["status"] == "failed"

    fake_ai["error"] = None
    assert _start_async(client, prediction_id).status_code == 202

    detail = _record(client, prediction_id)
    assert detail["status"] == "completed"
    assert detail["interpretation"]["model_used"] == "unit_test_ai"
    assert fake_ai["count"] == 2


def test_async_interpretation_returns_429_after_max_attempts(
    client, seeded_spread_and_cards, fake_ai, monkeypatch
):
    monkeypatch.setattr(settings, "AI_INTERPRETATION_MAX_ATTEMPTS", 2)
    _register_and_login(client, "async_exhausted")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    fake_ai["error"] = RuntimeError("simulated failure")

    assert _start_async(client, prediction_id).status_code == 202
    assert _start_async(client, prediction_id).status_code == 202
    resp = _start_async(client, prediction_id)

    assert resp.status_code == 429
    assert resp.json()["detail"] == "Interpretation attempts exhausted"
    assert fake_ai["count"] == 2


def test_async_interpretation_reclaims_stale_processing(client, db_session, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_stale")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    stale_start = datetime.now(timezone.utc) - timedelta(seconds=settings.AI_INTERPRETATION_STALE_SECONDS + 60)
    assert prediction_crud.claim_interpretation_generation(
        db_session,
        prediction_id=prediction_id,
        now=stale_start,
        stale_before=stale_start - timedelta(seconds=300),
        max_attempts=3,
    )

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 202
    assert _record(client, prediction_id)["status"] == "completed"
    assert fake_ai["count"] == 1
PY
bash /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh tests/test_records_async_interpretation.py 2>&1 | tail -n 3
```

Expected：`8 failed`。路由还不存在，所以接口返回 404 且 `detail` 为 `"Not Found"`，与各个断言都不符。

- [ ] **Step 2: 新增会话工厂，并在测试中替换**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
/opt/miniconda3/envs/tarot/bin/python - <<'PY'
from pathlib import Path
edits = {
    "app/db/session.py": [
        ("from typing import Generator\n", "from typing import Callable, Generator\n"),
        (
            "def get_db_session() -> Session:\n",
            r'''def get_session_factory() -> Callable[[], Session]:
    """Session factory for work that outlives the request, such as FastAPI background tasks."""
    return SessionLocal


def get_db_session() -> Session:
''',
        ),
    ],
    "tests/conftest.py": [
        ("from app.db.session import get_db\n", "from app.db.session import get_db, get_session_factory\n"),
        (
            "    app.dependency_overrides[get_db] = override_get_db\n",
            "    app.dependency_overrides[get_db] = override_get_db\n"
            "    app.dependency_overrides[get_session_factory] = lambda: db_session_factory\n",
        ),
    ],
}
for name, pairs in edits.items():
    p = Path(name)
    t = p.read_text(encoding="utf-8")
    for old, new in pairs:
        if t.count(old) != 1:
            raise SystemExit(f"FAIL: {name} anchor {old[:40]!r} count {t.count(old)} — nothing written")
    for old, new in pairs:
        t = t.replace(old, new)
    p.write_text(t, encoding="utf-8")
    print(f"OK: {name} updated")
PY
```

- [ ] **Step 3: 实现后台任务和异步接口**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
/opt/miniconda3/envs/tarot/bin/python - <<'PY'
from pathlib import Path
p = Path("app/api/v1/endpoints/records.py")
t = p.read_text(encoding="utf-8")
anchor = '@router.get("/{prediction_id:int}/interpretation", response_model=Interpretation, summary="Get interpretation")\n'
async_block = r'''async def generate_and_store_interpretation(
    db: Session,
    prediction: PredictionModel,
    user_context: Optional[str],
):
    """Generate with AI and persist; the entry point used by background generation."""
    interpretation_create = await _build_ai_interpretation_create(db, prediction, user_context)
    return _store_interpretation(db, prediction.id, interpretation_create)


async def run_interpretation_job(
    session_factory: Callable[[], Session],
    prediction_id: int,
    user_context: Optional[str],
) -> None:
    """Background task. Opens its own session because the request session is already closed."""
    db = session_factory()
    try:
        prediction = prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
        if prediction is None:
            logger.warning("Background interpretation skipped: record %s not found", prediction_id)
            return
        await generate_and_store_interpretation(db, prediction, user_context)
    except Exception as exc:  # noqa: BLE001 - background work must never raise into the event loop
        logger.error("Background interpretation for record %s failed: %s", prediction_id, exc)
        db.rollback()
        prediction_crud.update_prediction_status(
            db,
            prediction_id=prediction_id,
            status=PredictionStatus.FAILED,
        )
    finally:
        db.close()


def _interpretation_json(interpretation) -> JSONResponse:  # noqa: ANN001
    return JSONResponse(
        status_code=200,
        content=jsonable_encoder(Interpretation.model_validate(interpretation)),
    )


def _processing_json(prediction_id: int) -> JSONResponse:
    return JSONResponse(
        status_code=202,
        content={"prediction_id": prediction_id, "status": "processing"},
    )


def _as_utc(value: Optional[datetime]) -> Optional[datetime]:
    if value is None or value.tzinfo is not None:
        return value
    return value.replace(tzinfo=timezone.utc)


@router.post(
    "/{prediction_id:int}/interpret/async",
    summary="Start AI interpretation in the background",
    responses={
        200: {"model": Interpretation, "description": "Interpretation already exists"},
        202: {"description": "Generation started or already running"},
        429: {"description": "Interpretation attempts exhausted"},
    },
)
async def start_ai_interpretation_async(
    prediction_id: int,
    background_tasks: BackgroundTasks,
    user_context: Optional[str] = Query(None, description="Additional user context"),
    db: Session = Depends(get_db),
    session_factory: Callable[[], Session] = Depends(get_session_factory),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser and not prediction_crud.validate_prediction_ownership(
        db,
        prediction_id=prediction_id,
        user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")

    prediction = prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
    if not prediction:
        raise HTTPException(status_code=404, detail="Record not found")

    existing_interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
    if existing_interpretation:
        return _interpretation_json(existing_interpretation)

    if not prediction_crud.get_prediction_card_draws(db, prediction_id=prediction_id):
        raise HTTPException(status_code=400, detail="Cards must be drawn before interpretation")

    now = datetime.now(timezone.utc)
    stale_before = now - timedelta(seconds=max(1, int(settings.AI_INTERPRETATION_STALE_SECONDS)))
    max_attempts = max(1, int(settings.AI_INTERPRETATION_MAX_ATTEMPTS))
    if prediction_crud.claim_interpretation_generation(
        db,
        prediction_id=prediction_id,
        now=now,
        stale_before=stale_before,
        max_attempts=max_attempts,
    ):
        background_tasks.add_task(run_interpretation_job, session_factory, prediction_id, user_context)
        return _processing_json(prediction_id)

    db.refresh(prediction)
    existing_interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
    if existing_interpretation:
        return _interpretation_json(existing_interpretation)

    started_at = _as_utc(prediction.interpretation_started_at)
    if prediction.status == PredictionStatus.PROCESSING and started_at is not None and started_at >= stale_before:
        return _processing_json(prediction_id)

    if prediction.interpretation_attempts >= max_attempts:
        raise HTTPException(status_code=429, detail="Interpretation attempts exhausted")

    return _processing_json(prediction_id)


'''
edits = [
    ("from typing import List, Optional, cast\n", "from typing import Callable, List, Optional, cast\n"),
    (
        "from fastapi import APIRouter, Depends, HTTPException, Query\n",
        "from fastapi import APIRouter, BackgroundTasks, Depends, HTTPException, Query\n"
        "from fastapi.encoders import jsonable_encoder\n"
        "from fastapi.responses import JSONResponse\n",
    ),
    ("from app.db.session import get_db\n", "from app.db.session import get_db, get_session_factory\n"),
    (anchor, async_block + anchor),
]
for old, new in edits:
    if t.count(old) != 1:
        raise SystemExit(f"FAIL: anchor {old[:50]!r} count {t.count(old)} — nothing written")
for old, new in edits:
    t = t.replace(old, new)
p.write_text(t, encoding="utf-8")
print("OK: async interpretation endpoint added")
PY
bash /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh tests/test_records_async_interpretation.py 2>&1 | tail -n 1
```

Expected：`8 passed`。

- [ ] **Step 4: README 补充异步接口**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
/opt/miniconda3/envs/tarot/bin/python - <<'PY'
from pathlib import Path
p = Path("README.md")
t = p.read_text(encoding="utf-8")
old = "- `POST http://localhost:8000/api/v1/guest-session`：申请访客会话。\n"
new = old + "- `POST http://localhost:8000/api/v1/records/{id}/interpret/async`：在后台生成 AI 解读，返回 202 后轮询 `GET /api/v1/records/{id}` 获取结果（Unity 使用这条路径）。\n"
if t.count(old) != 1:
    raise SystemExit(f"FAIL: anchor count {t.count(old)} — nothing written")
p.write_text(t.replace(old, new), encoding="utf-8")
print("OK: README endpoint line added")
PY
```

- [ ] **Step 5: 全量测试**

```bash
PT=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh
bash "$PT" tests 2>&1 | tail -n 1
bash "$PT" . 2>&1 | tail -n 1
```

Expected：两行都是 `184 passed, 1 warning`。

- [ ] **Step 6: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git status --porcelain
git add Server/app/db/session.py Server/tests/conftest.py Server/app/api/v1/endpoints/records.py Server/README.md Server/tests/test_records_async_interpretation.py
git commit -q -F - <<'MSG'
feat(server): add background interpretation endpoint

POST /records/{id}/interpret/async claims generation atomically, runs the
AI call as a FastAPI background task with its own DB session, and returns
202 while generating, 200 with the interpretation once it exists, 400
before cards are drawn, 404 for other users' records and 429 once the
attempt cap is reached. Clients poll GET /records/{id}.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
MSG
git show --stat --format='%h %s' HEAD
```

Expected：提交前 `git status` 只列出上面 5 个文件；提交后显示 `5 files changed`。

---

### Task 5: 访客续期与访客调用异步接口的覆盖测试

**Files:**
- Modify: `Server/tests/test_guest_session.py`（在文件末尾追加两个测试）

**Interfaces:**
- Consumes: 现有的 `/guest-session`、`/refresh`；Task 4 的异步接口
- Produces: 无（锁定计划 2 所依赖的两条后端行为）

- [ ] **Step 1: 追加测试**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
/opt/miniconda3/envs/tarot/bin/python - <<'PY'
from pathlib import Path
p = Path("tests/test_guest_session.py")
t = p.read_text(encoding="utf-8")
old = '    assert int(second_record.headers["retry-after"]) > 0\n'
new = old + r'''

def test_guest_session_refresh_keeps_access_to_own_record(client, seeded_spread_and_cards):
    guest_response = client.post("/api/v1/guest-session")
    assert guest_response.status_code == 200
    first_token = guest_response.json()["access_token"]

    record_response = client.post(
        "/api/v1/records/",
        headers={"Authorization": f"Bearer {first_token}"},
        json={
            "question": "续期之后还能看到这条记录吗？",
            "question_type": "general",
            "spread_type_id": seeded_spread_and_cards["spread_id"],
        },
    )
    assert record_response.status_code == 200
    prediction_id = record_response.json()["id"]

    csrf_token = client.cookies.get(settings.CSRF_COOKIE_NAME)
    assert csrf_token
    refresh_response = client.post("/api/v1/refresh", headers={settings.CSRF_HEADER_NAME: csrf_token})

    assert refresh_response.status_code == 200
    refreshed_token = refresh_response.json()["access_token"]
    assert refreshed_token

    detail_response = client.get(
        f"/api/v1/records/{prediction_id}",
        headers={"Authorization": f"Bearer {refreshed_token}"},
    )
    assert detail_response.status_code == 200
    assert detail_response.json()["id"] == prediction_id


def test_guest_session_can_run_async_interpretation(client, seeded_spread_and_cards, monkeypatch):
    guest_response = client.post("/api/v1/guest-session")
    assert guest_response.status_code == 200
    headers = {"Authorization": f"Bearer {guest_response.json()['access_token']}"}

    record_response = client.post(
        "/api/v1/records/",
        headers=headers,
        json={
            "question": "访客也能在后台生成解读吗？",
            "question_type": "general",
            "spread_type_id": seeded_spread_and_cards["spread_id"],
        },
    )
    assert record_response.status_code == 200
    prediction_id = record_response.json()["id"]
    assert client.post(f"/api/v1/records/{prediction_id}/draw", headers=headers).status_code == 200

    async def fake_create_interpretation(db, prediction, cards_data, user_context=None):  # noqa: ANN001
        return {
            "overall_interpretation": "访客的后台解读已经生成。",
            "summary": "可以继续。",
            "model_used": "guest_async_mock_ai",
        }

    monkeypatch.setattr(
        records_endpoint.tarot_interpretation_service,
        "create_interpretation",
        fake_create_interpretation,
    )

    start_response = client.post(f"/api/v1/records/{prediction_id}/interpret/async", headers=headers)
    assert start_response.status_code == 202

    detail_response = client.get(f"/api/v1/records/{prediction_id}", headers=headers)
    assert detail_response.status_code == 200
    detail = detail_response.json()
    assert detail["status"] == "completed"
    assert detail["interpretation"]["model_used"] == "guest_async_mock_ai"
'''
if t.count(old) != 1:
    raise SystemExit(f"FAIL: anchor count {t.count(old)} — nothing written")
p.write_text(t.replace(old, new), encoding="utf-8")
print("OK: guest tests appended")
PY
```

- [ ] **Step 2: 运行测试**

```bash
bash /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh tests/test_guest_session.py 2>&1 | tail -n 3
```

Expected：`7 passed`。这两个测试验证的是已经实现的行为，所以应该直接通过。**如果续期测试失败，STOP**：这说明后端存在缺陷，而计划 2 中 Unity 的 401 恢复正依赖这条路径，需要先和用户确认修复方案。

- [ ] **Step 3: 全量测试**

```bash
PT=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh
bash "$PT" tests 2>&1 | tail -n 1
bash "$PT" . 2>&1 | tail -n 1
```

Expected：两行都是 `186 passed, 1 warning`。

- [ ] **Step 4: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git status --porcelain
git add Server/tests/test_guest_session.py
git commit -q -F - <<'MSG'
test(server): cover guest refresh and guest async interpretation

A refreshed guest token must still read the guest's own record, because a
new guest session creates a different user. Guests must also be able to
use the background interpretation endpoint with a bearer token.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
MSG
git show --stat --format='%h %s' HEAD
```

Expected：`1 file changed`。

---

### Task 6: mock 解读改为中文

**Files:**
- Modify: `Server/app/services/tarot_service.py`（替换 `_create_mock_interpretation` 整个方法）
- Create: `Server/tests/test_mock_interpretation_zh.py`

**Interfaces:**
- Consumes: `TarotPromptTemplate.format_card` 输出的 `name_zh`、`position`、`orientation`（取值为 `upright` 或 `reversed`）
- Produces: `_create_mock_interpretation` 返回的中文内容；`model_used` 仍为 `mock_ai`

- [ ] **Step 1: 写失败的测试**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
test ! -e tests/test_mock_interpretation_zh.py || { echo "STOP: file exists"; exit 1; }
set -C
cat > tests/test_mock_interpretation_zh.py <<'PY'
from __future__ import annotations

import re
from types import SimpleNamespace

from app.services.tarot_service import tarot_interpretation_service

TEXT_FIELDS = ("overall_interpretation", "advice", "warning", "summary")


def test_mock_interpretation_is_chinese_and_marked_as_mock():
    payload = tarot_interpretation_service._create_mock_interpretation(
        SimpleNamespace(question="我接下来该专注什么？"),
        [
            {"name_zh": "愚者", "position": "过去", "orientation": "upright"},
            {"name_zh": "女祭司", "position": "现在", "orientation": "reversed"},
        ],
        reason="",
    )

    assert payload["model_used"] == "mock_ai"
    assert "模拟解读" in payload["overall_interpretation"]
    assert "我接下来该专注什么？" in payload["overall_interpretation"]
    assert payload["card_analysis"] == "1. 过去：愚者（正位）\n2. 现在：女祭司（逆位）"
    assert payload["key_themes"] == "愚者,节奏,专注"
    for field in TEXT_FIELDS:
        assert not re.search(r"[A-Za-z]{3,}", payload[field]), f"{field} still has English: {payload[field]!r}"


def test_mock_interpretation_without_cards_uses_chinese_defaults():
    payload = tarot_interpretation_service._create_mock_interpretation(
        SimpleNamespace(question="今天怎样？"),
        [],
        reason="",
    )

    assert payload["card_analysis"] is None
    assert payload["key_themes"] == "节奏,专注"
    assert "模拟解读" in payload["overall_interpretation"]
    for field in TEXT_FIELDS:
        assert not re.search(r"[A-Za-z]{3,}", payload[field]), f"{field} still has English: {payload[field]!r}"
PY
bash /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh tests/test_mock_interpretation_zh.py 2>&1 | tail -n 3
```

Expected：`2 failed`（找不到"模拟解读"，文本中含有英文）。

- [ ] **Step 2: 替换 mock 方法**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/Server
/opt/miniconda3/envs/tarot/bin/python - <<'PY'
from pathlib import Path
p = Path("app/services/tarot_service.py")
t = p.read_text(encoding="utf-8")
start_anchor = "    def _create_mock_interpretation(\n"
end_anchor = "    async def health_check(self, *, deep: bool = False) -> Dict[str, Any]:\n"
for anchor in (start_anchor, end_anchor):
    if t.count(anchor) != 1:
        raise SystemExit(f"FAIL: anchor count {t.count(anchor)} for {anchor.strip()!r} — nothing written")
start = t.index(start_anchor)
end = t.index(end_anchor)
if start >= end:
    raise SystemExit("FAIL: anchors out of order — nothing written")
new_method = r'''    def _create_mock_interpretation(
        self,
        prediction: Prediction,
        cards: List[Dict[str, Any]],
        reason: str = "",
    ) -> Dict[str, Any]:
        orientation_names = {"upright": "正位", "reversed": "逆位"}
        card_names: List[str] = []
        card_lines: List[str] = []
        for idx, card in enumerate(cards, start=1):
            name = card.get("name_zh") or "未知牌"
            orientation = orientation_names.get(card.get("orientation", "upright"), "正位")
            position = card.get("position") or f"位置 {idx}"
            card_names.append(name)
            card_lines.append(f"{idx}. {position}：{name}（{orientation}）")

        overall = (
            f"这是模拟解读，未连接 AI 服务（生成于 {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}）。\n"
            f"问题：{prediction.question}\n"
            "牌面提示你在直觉与现实行动之间找到平衡，一次专注推进一个最关键的步骤，并定期回顾进展。"
        )

        card_analysis = "\n".join(card_lines) if card_lines else None
        advice = "把下一步拆成几个可以马上执行的小动作，保持稳定的节奏去完成。"
        warning = "避免同时追逐太多目标，分散精力会拖慢进展。"
        summary = "保持节奏，先求清晰，再逐步推进。"
        key_themes = ",".join([card_names[0], "节奏", "专注"]) if card_names else "节奏,专注"

        if reason:
            logger.warning("Using mock interpretation fallback. reason=%s", reason)

        return {
            "overall_interpretation": overall,
            "card_analysis": card_analysis,
            "relationship_analysis": None,
            "advice": advice,
            "warning": warning,
            "summary": summary,
            "key_themes": key_themes,
            "model_used": "mock_ai",
            "model_version": None,
            "confidence_score": 0.45,
        }

'''
p.write_text(t[:start] + new_method + t[end:], encoding="utf-8")
print("OK: mock interpretation localized")
PY
bash /private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh tests/test_mock_interpretation_zh.py 2>&1 | tail -n 1
```

Expected：`2 passed`。

- [ ] **Step 3: 全量测试**

```bash
PT=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh
bash "$PT" tests 2>&1 | tail -n 1
bash "$PT" . 2>&1 | tail -n 1
```

Expected：两行都是 `188 passed, 1 warning`。

- [ ] **Step 4: 提交**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git status --porcelain
git add Server/app/services/tarot_service.py Server/tests/test_mock_interpretation_zh.py
git commit -q -F - <<'MSG'
feat(server): localize the mock interpretation to Chinese

The mock fallback (only used when ALLOW_MOCK_AI_FALLBACK=true) now reads
as a Chinese reading that says it is simulated. model_used stays mock_ai
so clients can label it.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01SzXyQ4Efyzs2UuRKp9SrAp
MSG
git show --stat --format='%h %s' HEAD
```

Expected：`2 files changed`。

---

### Task 7: 真实服务冒烟与收尾

**Files:**
- Create (scratch): `$SCRATCH/online-a-smoke/smoke_client.py`、`$SCRATCH/online-a-smoke/smoke_async.sh`、`$SCRATCH/online-a-smoke/Server/`（已提交代码的副本）

**Interfaces:**
- Consumes: Task 1–6 的提交
- Produces: 冒烟证据；交给用户的报告

TestClient 会在请求返回前就把后台任务执行完，与真实 uvicorn 的行为不同。这个任务在真实 uvicorn 进程上验证：迁移能升级到 0002，异步接口返回 202 后后台任务确实会完成。使用 mock AI（`ALLOW_MOCK_AI_FALLBACK=true`，Key 为空），不花钱。

- [ ] **Step 1: 创建冒烟客户端和脚本**

```bash
SCRATCH=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
test ! -e "$SCRATCH/online-a-smoke" || { echo "STOP: $SCRATCH/online-a-smoke already exists"; exit 1; }
mkdir -p "$SCRATCH/online-a-smoke"
set -C
cat > "$SCRATCH/online-a-smoke/smoke_client.py" <<'PY'
"""Async interpretation smoke test against a running server. Usage: smoke_client.py <api_base_url>"""
import json
import sys
import time
import urllib.error
import urllib.request

BASE = sys.argv[1].rstrip("/")


def call(method, path, token=None, body=None):
    data = json.dumps(body).encode("utf-8") if body is not None else None
    request = urllib.request.Request(BASE + path, data=data, method=method)
    request.add_header("Accept", "application/json")
    if body is not None:
        request.add_header("Content-Type", "application/json")
    if token:
        request.add_header("Authorization", f"Bearer {token}")
    try:
        with urllib.request.urlopen(request, timeout=15) as response:
            raw = response.read()
            return response.status, json.loads(raw) if raw else None
    except urllib.error.HTTPError as error:
        raw = error.read()
        return error.code, json.loads(raw) if raw else None


def expect(condition, message):
    if not condition:
        print(f"FAIL: {message}")
        sys.exit(1)
    print(f"PASS: {message}")


status, _ = call("GET", "/health/")
expect(status == 200, f"GET /health/ -> {status}")

status, session = call("POST", "/guest-session")
expect(status == 200 and bool(session and session.get("access_token")), f"POST /guest-session -> {status}")
token = session["access_token"]

status, spreads = call("GET", "/spreads/", token)
expect(status == 200 and bool(spreads), f"GET /spreads/ -> {status}, {len(spreads or [])} spreads")
spread = next((item for item in spreads if item.get("card_count") == 3), spreads[0])

status, record = call(
    "POST",
    "/records/",
    token,
    {"spread_type_id": spread["id"], "question": "我接下来该专注什么？", "question_type": "general"},
)
expect(status == 200, f"POST /records/ -> {status}")
prediction_id = record["id"]

status, _ = call("POST", f"/records/{prediction_id}/draw", token)
expect(status == 200, f"POST /records/{prediction_id}/draw -> {status}")

status, started = call("POST", f"/records/{prediction_id}/interpret/async", token)
expect(status == 202 and started == {"prediction_id": prediction_id, "status": "processing"},
       f"POST /interpret/async -> {status} {started}")

detail = None
deadline = time.monotonic() + 30
while time.monotonic() < deadline:
    status, detail = call("GET", f"/records/{prediction_id}", token)
    if status == 200 and detail and detail.get("status") in ("completed", "failed"):
        break
    time.sleep(0.5)
expect(bool(detail) and detail.get("status") == "completed", f"record status after polling -> {detail and detail.get('status')}")
interpretation = detail.get("interpretation") or {}
expect(interpretation.get("model_used") == "mock_ai", f"interpretation.model_used -> {interpretation.get('model_used')}")
expect("模拟解读" in (interpretation.get("overall_interpretation") or ""), "mock interpretation text is Chinese")

status, again = call("POST", f"/records/{prediction_id}/interpret/async", token)
expect(status == 200 and bool(again) and again.get("prediction_id") == prediction_id,
       f"second POST /interpret/async -> {status}")
PY
cat > "$SCRATCH/online-a-smoke/smoke_async.sh" <<'SH'
#!/bin/bash
# Boots a copy of the committed Server/ with mock AI and runs smoke_client.py against it.
set -o pipefail
SMOKE="$(cd "$(dirname "$0")" && pwd)"
REPO=/Users/maochuandou/BUPT/Game/UnityTarot
PY=/opt/miniconda3/envs/tarot/bin/python
PORT=8766
if [ -e "$SMOKE/Server" ]; then echo "STOP: $SMOKE/Server already exists"; exit 2; fi
if lsof -nP -iTCP:$PORT -sTCP:LISTEN >/dev/null 2>&1; then echo "STOP: port $PORT is in use"; exit 2; fi
git -C "$REPO" archive HEAD Server | tar -x -C "$SMOKE"
cd "$SMOKE/Server" || exit 2
cp .env.example .env
export SECRET_KEY="$("$PY" -c 'import secrets; print(secrets.token_urlsafe(48))')"
export DEEPSEEK_API_KEY=""
export ALLOW_MOCK_AI_FALLBACK=true
export PYTHONDONTWRITEBYTECODE=1

/opt/miniconda3/envs/tarot/bin/alembic upgrade head > "$SMOKE/alembic.log" 2>&1 \
  || { echo "FAIL: alembic upgrade head"; tail -20 "$SMOKE/alembic.log"; exit 1; }
version=$("$PY" -c "import sqlite3; print(sqlite3.connect('tarot_game.db').execute('select version_num from alembic_version').fetchone()[0])")
if [ "$version" != "0002_interpretation_tracking" ]; then echo "FAIL: alembic_version = $version"; exit 1; fi
echo "PASS: alembic_version = $version"

"$PY" -m app.scripts.init_tarot_data > "$SMOKE/init.log" 2>&1 \
  || { echo "FAIL: init_tarot_data"; tail -20 "$SMOKE/init.log"; exit 1; }
echo "PASS: reference data imported"

"$PY" -m uvicorn app.main:app --host 127.0.0.1 --port $PORT > "$SMOKE/uvicorn.log" 2>&1 &
pid=$!
curl -sS --retry 30 --retry-delay 1 --retry-connrefused -o /dev/null "http://127.0.0.1:$PORT/api/v1/health/" 2>/dev/null
"$PY" "$SMOKE/smoke_client.py" "http://127.0.0.1:$PORT/api/v1"
rc=$?
kill "$pid" 2>/dev/null
wait "$pid" 2>/dev/null
if [ $rc -ne 0 ]; then echo "--- uvicorn.log tail ---"; tail -30 "$SMOKE/uvicorn.log"; fi
exit $rc
SH
ls "$SCRATCH/online-a-smoke"
```

Expected：列出 `smoke_async.sh` 和 `smoke_client.py`。

- [ ] **Step 2: 运行冒烟**（Bash timeout 设为 600000 ms）

```bash
SCRATCH=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad
bash "$SCRATCH/online-a-smoke/smoke_async.sh"; echo "exit=$?"
lsof -nP -iTCP:8766 -sTCP:LISTEN >/dev/null 2>&1 && echo "PORT 8766 STILL LISTENING" || echo "server stopped"
```

Expected：
```
PASS: alembic_version = 0002_interpretation_tracking
PASS: reference data imported
PASS: GET /health/ -> 200
PASS: POST /guest-session -> 200
PASS: GET /spreads/ -> 200, 6 spreads
PASS: POST /records/ -> 200
PASS: POST /records/<id>/draw -> 200
PASS: POST /interpret/async -> 202 {'prediction_id': <id>, 'status': 'processing'}
PASS: record status after polling -> completed
PASS: interpretation.model_used -> mock_ai
PASS: mock interpretation text is Chinese
PASS: second POST /interpret/async -> 200
exit=0
server stopped
```

（`<id>` 为本次运行实际创建的记录编号。）只要出现任何 `FAIL`：**STOP**，把输出和 `uvicorn.log` 的末尾报告给用户。

- [ ] **Step 3: 最终检查**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git log --reverse --format='%h %s' 78caed8..HEAD
git status --porcelain; echo "(end status)"
git status --porcelain --ignored -- Server | grep -v '^$'; echo "(end Server ignored)"
PT=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/1cc9046d-217e-41b2-825c-82bc8e70eeaf/scratchpad/online-a-run/pt.sh
bash "$PT" tests 2>&1 | tail -n 1
bash "$PT" . 2>&1 | tail -n 1
git ls-remote --heads origin main
```

Expected：
- 6 个提交，依次为：`feat(server): track interpretation generation on predictions`、`feat(server): atomic claim for interpretation generation`、`refactor(server): extract interpretation generation helpers`、`feat(server): add background interpretation endpoint`、`test(server): cover guest refresh and guest async interpretation`、`feat(server): localize the mock interpretation to Chinese`
- `(end status)` 之前没有输出
- `(end Server ignored)` 之前没有输出
- 两行都是 `188 passed, 1 warning`
- 远端 `main` 仍然是 `4ec3816…`（本计划没有推送）

- [ ] **Step 4: 向用户报告**（中文）

报告内容包括：6 个提交；测试从 166 增加到 188；冒烟结果中的每一行 PASS；新增的文件（仓库内 5 个新文件，以及修改过的文件列表；scratch 中的 `online-a-run/`、`online-a-smoke/`）；计划 2 须知（见下一节）。明确说明：真实 DeepSeek 的延迟与返回格式要到计划 2 的真实联调阶段才会验证。

---

## 计划 2 须知（写给计划 2 的作者）

- **异步接口约定**：`POST /api/v1/records/{id}/interpret/async`。返回 202 时响应体为 `{"prediction_id": id, "status": "processing"}`；返回 200 时响应体结构与 `InterpretationResponse` 相同；400 表示尚未抽牌；404 的 `detail` 为 `"Record not found"`；429 的 `detail` 为 `"Interpretation attempts exhausted"`。
- **建记录时的 429** 与上面不同：`detail` 为 `"Guest daily reading limit reached. Please try again tomorrow."`，并带有 `Retry-After` 响应头。
- **轮询器必须把"`interpretation` 不为空"视为完成，不论 `status` 是什么。**`_store_interpretation` 在遇到唯一约束冲突（同步接口和异步接口同时生成）时，会直接返回已有解读，但不会把状态更新为 `completed`。
- **Unity 轮询总时限 330 秒** = `AI_INTERPRETATION_STALE_SECONDS` 300 秒 + 30 秒余量。
- 访客令牌过期后，要用 cookie + `X-CSRF-Token` 调用 `/api/v1/refresh` 续期（Task 5 已测试），不能重新申请访客会话。
- 后端测试仍然通过 `$SCRATCH/online-a-run/pt.sh` 运行；真实联调时后端使用用户自己的 `Server/.env`。

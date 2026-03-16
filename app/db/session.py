from sqlalchemy import create_engine, text
from sqlalchemy.orm import sessionmaker, Session
from sqlalchemy.pool import StaticPool
from app.core.config import settings

# 数据库配置使用settings中的配置
DATABASE_URL = settings.DATABASE_URL

# 创建引擎
if DATABASE_URL.startswith("sqlite"):
    # SQLite配置:
    # - In-memory SQLite needs StaticPool to keep one connection alive.
    # - File-based SQLite should use normal pooling to avoid single-connection contention.
    sqlite_connect_args = {"check_same_thread": False}
    is_memory_sqlite = DATABASE_URL in {"sqlite://", "sqlite:///:memory:"} or DATABASE_URL.endswith(":memory:")

    if is_memory_sqlite:
        engine = create_engine(
            DATABASE_URL,
            poolclass=StaticPool,
            connect_args=sqlite_connect_args,
            echo=False,
        )
    else:
        engine = create_engine(
            DATABASE_URL,
            connect_args=sqlite_connect_args,
            pool_pre_ping=True,
            echo=False,
        )
else:
    # PostgreSQL配置
    engine = create_engine(
        DATABASE_URL,
        pool_pre_ping=True,
        echo=False
    )

# 创建SessionLocal类
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def get_db() -> Session:
    """获取数据库会话"""
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

def get_db_session() -> Session:
    """获取数据库会话（同步版本）"""
    return SessionLocal()

# 数据库健康检查
def check_db_connection() -> bool:
    """检查数据库连接是否正常"""
    try:
        db = SessionLocal()
        db.execute(text("SELECT 1"))
        db.close()
        return True
    except Exception as e:
        print(f"数据库连接失败: {e}")
        return False

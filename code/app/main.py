import logging

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.core.config import settings
from app.api.v1.api import api_router
from app.db.session import engine
from app.db.base_class import Base

logger = logging.getLogger(__name__)


def create_tables():
    """创建数据库表"""
    Base.metadata.create_all(bind=engine)


def create_start_app_handler(app: FastAPI):
    async def start_app() -> None:
        create_tables()
        print("Database tables created successfully")
        print("Tip: Run 'python -m app.scripts.init_demo_data' to initialize demo data")
    return start_app


def get_application():
    docs_url = "/docs" if settings.DEBUG else None
    redoc_url = "/redoc" if settings.DEBUG else None

    app = FastAPI(
        title=settings.PROJECT_NAME, 
        version=settings.PROJECT_VERSION,
        description="塔罗牌游戏后端API",
        docs_url=docs_url,
        redoc_url=redoc_url
    )
    
    # 添加CORS中间件
    allowed_origins = settings.cors_origins_list
    allow_credentials = settings.CORS_ALLOW_CREDENTIALS and "*" not in allowed_origins

    app.add_middleware(
        CORSMiddleware,
        allow_origins=allowed_origins,
        allow_credentials=allow_credentials,
        allow_methods=["*"],
        allow_headers=["*"],
    )
    logger.info(
        "CORS configured with origins=%s credentials=%s",
        allowed_origins,
        allow_credentials,
    )
    
    app.add_event_handler("startup", create_start_app_handler(app))
    app.include_router(api_router, prefix=settings.API_V1_STR)
    return app


app = get_application() 

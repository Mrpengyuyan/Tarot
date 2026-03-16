from fastapi import APIRouter
from app.api.v1.endpoints import users, auth, cards, spreads, records, health

api_router = APIRouter()
api_router.include_router(auth.router, tags=["auth"])
api_router.include_router(users.router, prefix="/users", tags=["users"])
api_router.include_router(cards.router, prefix="/cards", tags=["cards"])
api_router.include_router(spreads.router, prefix="/spreads", tags=["spreads"])
api_router.include_router(records.router, prefix="/records", tags=["records"])
api_router.include_router(health.router, prefix="/health", tags=["health"])

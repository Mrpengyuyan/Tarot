from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session
from app.db.session import get_db
from app.api.deps import get_current_active_user
from app.crud.user import get_user_by_id, get_user_by_username, get_user_by_email, update_user
from app.schemas.user import User, UserUpdate
from app.models.user import User as UserModel

router = APIRouter()


@router.get("/me", response_model=User, summary="获取当前用户信息")
def get_current_user_info(
    current_user: UserModel = Depends(get_current_active_user)
):
    """
    获取当前登录用户的信息
    """
    return current_user


@router.put("/me", response_model=User, summary="更新当前用户信息")
def update_current_user(
    user_update: UserUpdate,
    db: Session = Depends(get_db),
    current_user: UserModel = Depends(get_current_active_user)
):
    """
    更新当前登录用户的信息
    """
    if user_update.username and user_update.username != current_user.username:
        existing = get_user_by_username(db, username=user_update.username)
        if existing and existing.id != current_user.id:
            raise HTTPException(status_code=400, detail="Username already registered")

    if user_update.email and user_update.email != current_user.email:
        existing = get_user_by_email(db, email=user_update.email)
        if existing and existing.id != current_user.id:
            raise HTTPException(status_code=400, detail="Email already registered")

    try:
        updated_user = update_user(db=db, db_user=current_user, user_update=user_update)
        return updated_user
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.get("/{user_id}", response_model=User, summary="根据ID获取用户信息")
def get_user_by_id_endpoint(
    user_id: int,
    db: Session = Depends(get_db),
    current_user: UserModel = Depends(get_current_active_user)
):
    """
    根据用户ID获取用户信息（需要登录）
    """
    if not current_user.is_superuser and current_user.id != user_id:
        raise HTTPException(status_code=403, detail="无权访问其他用户信息")

    user = get_user_by_id(db, user_id=user_id)
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    return user 

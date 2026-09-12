import secrets
from datetime import timedelta
from typing import Optional, cast

from fastapi import APIRouter, Depends, HTTPException, Request, Response, status
from fastapi.security import OAuth2PasswordRequestForm
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app.api.deps import _ensure_cookie_csrf_protection
from app.core.config import settings
from app.core.security import (
    create_access_token,
    create_refresh_token,
    get_password_hash,
    verify_token,
)
from app.crud.user import (
    authenticate_user,
    create_user,
    get_user_by_email,
    get_user_by_username,
    is_active,
)
from app.db.session import get_db
from app.models.user import User as UserModel
from app.schemas.user import Token, User, UserCreate

router = APIRouter()


def _issue_access_token(username: str) -> str:
    access_token_expires = timedelta(minutes=settings.ACCESS_TOKEN_EXPIRE_MINUTES)
    return create_access_token(
        data={"sub": username},
        expires_delta=access_token_expires,
    )


def _issue_refresh_token(username: str) -> str:
    refresh_token_expires = timedelta(days=settings.REFRESH_TOKEN_EXPIRE_DAYS)
    return create_refresh_token(
        data={"sub": username},
        expires_delta=refresh_token_expires,
    )


def _set_auth_cookie(
    response: Response,
    access_token: str,
    refresh_token: str,
    csrf_token: Optional[str] = None,
) -> None:
    access_max_age = settings.ACCESS_TOKEN_EXPIRE_MINUTES * 60
    refresh_max_age = settings.REFRESH_TOKEN_EXPIRE_DAYS * 24 * 60 * 60

    response.set_cookie(
        key=settings.AUTH_COOKIE_NAME,
        value=access_token,
        max_age=access_max_age,
        expires=access_max_age,
        httponly=True,
        secure=settings.AUTH_COOKIE_SECURE,
        samesite=settings.AUTH_COOKIE_SAMESITE,
        path=settings.AUTH_COOKIE_PATH,
    )

    response.set_cookie(
        key=settings.REFRESH_COOKIE_NAME,
        value=refresh_token,
        max_age=refresh_max_age,
        expires=refresh_max_age,
        httponly=True,
        secure=settings.AUTH_COOKIE_SECURE,
        samesite=settings.AUTH_COOKIE_SAMESITE,
        path=settings.AUTH_COOKIE_PATH,
    )

    csrf_value = csrf_token or secrets.token_urlsafe(32)
    response.set_cookie(
        key=settings.CSRF_COOKIE_NAME,
        value=csrf_value,
        max_age=refresh_max_age,
        expires=refresh_max_age,
        httponly=False,
        secure=settings.AUTH_COOKIE_SECURE,
        samesite=settings.AUTH_COOKIE_SAMESITE,
        path=settings.AUTH_COOKIE_PATH,
    )


def _clear_auth_cookie(response: Response) -> None:
    response.delete_cookie(
        key=settings.AUTH_COOKIE_NAME,
        path=settings.AUTH_COOKIE_PATH,
    )
    response.delete_cookie(
        key=settings.REFRESH_COOKIE_NAME,
        path=settings.AUTH_COOKIE_PATH,
    )
    response.delete_cookie(
        key=settings.CSRF_COOKIE_NAME,
        path=settings.AUTH_COOKIE_PATH,
    )


def _create_guest_user(db: Session) -> UserModel:
    """Create a random account used by the desktop client's guest mode."""
    for _ in range(3):
        username = f"guest_{secrets.token_hex(16)}"
        guest_user = UserModel(
            username=username,
            email=f"{username}@guest.tarot.game",
            hashed_password=get_password_hash(secrets.token_urlsafe(32)),
            nickname="访客",
            is_active=True,
            is_superuser=False,
        )
        db.add(guest_user)
        try:
            db.commit()
            db.refresh(guest_user)
            return guest_user
        except IntegrityError:
            db.rollback()

    raise HTTPException(
        status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
        detail="Could not create a guest session",
    )


@router.post("/register", response_model=User, summary="User register")
def register(
    user_create: UserCreate,
    db: Session = Depends(get_db),
):
    if get_user_by_username(db, username=user_create.username):
        raise HTTPException(status_code=400, detail="Username already registered")

    if get_user_by_email(db, email=user_create.email):
        raise HTTPException(status_code=400, detail="Email already registered")

    try:
        user = create_user(db=db, user_create=user_create)
        return user
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except IntegrityError as exc:
        db.rollback()
        raise HTTPException(status_code=400, detail="Username or email already registered") from exc


@router.post("/guest-session", response_model=Token, summary="Create guest session")
def guest_session(
    response: Response,
    db: Session = Depends(get_db),
):
    """Issue a limited anonymous session for the desktop client."""
    guest_user = _create_guest_user(db)
    access_token = _issue_access_token(cast(str, guest_user.username))
    refresh_token = _issue_refresh_token(cast(str, guest_user.username))
    _set_auth_cookie(response, access_token, refresh_token)
    return {"access_token": access_token, "token_type": "bearer"}


@router.post("/login", response_model=Token, summary="User login")
def login(
    response: Response,
    form_data: OAuth2PasswordRequestForm = Depends(),
    db: Session = Depends(get_db),
):
    user = authenticate_user(db, form_data.username, form_data.password)
    if not user:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Incorrect username or password",
            headers={"WWW-Authenticate": "Bearer"},
        )

    access_token = _issue_access_token(cast(str, user.username))
    refresh_token = _issue_refresh_token(cast(str, user.username))
    _set_auth_cookie(response, access_token, refresh_token)
    return {"access_token": access_token, "token_type": "bearer"}


@router.post("/refresh", response_model=Token, summary="Refresh access token")
def refresh_token(
    request: Request,
    response: Response,
    db: Session = Depends(get_db),
):
    _ensure_cookie_csrf_protection(request)

    refresh_cookie = request.cookies.get(settings.REFRESH_COOKIE_NAME)
    username = verify_token(refresh_cookie, expected_type="refresh") if refresh_cookie else None
    if username is None:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Could not refresh session",
            headers={"WWW-Authenticate": "Bearer"},
        )

    user = get_user_by_username(db, username=username)
    if user is None:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Could not refresh session",
            headers={"WWW-Authenticate": "Bearer"},
        )
    if not is_active(user):
        raise HTTPException(status_code=400, detail="Inactive user")

    access_token = _issue_access_token(cast(str, user.username))
    new_refresh_token = _issue_refresh_token(cast(str, user.username))
    existing_csrf = request.cookies.get(settings.CSRF_COOKIE_NAME)
    _set_auth_cookie(
        response,
        access_token,
        new_refresh_token,
        csrf_token=existing_csrf,
    )
    return {"access_token": access_token, "token_type": "bearer"}


@router.post("/logout", summary="Logout")
def logout(response: Response):
    _clear_auth_cookie(response)
    return {"message": "Logged out"}

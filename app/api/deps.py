from typing import Optional, Protocol
from urllib.parse import urlparse

from fastapi import Depends, HTTPException, Request, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from sqlalchemy.orm import Session

from app.core.config import settings
from app.core.security import verify_token
from app.crud.user import get_user_by_username, is_active
from app.db.session import get_db
from app.models.user import User

# Support Authorization header and cookie-based auth.
security = HTTPBearer(auto_error=False)
_SAFE_METHODS = {"GET", "HEAD", "OPTIONS"}


class _CookieCarrier(Protocol):
    cookies: dict[str, str]


def _extract_token_with_source(
    credentials: Optional[HTTPAuthorizationCredentials],
    request: Request | _CookieCarrier,
) -> tuple[Optional[str], bool]:
    if credentials and credentials.credentials:
        return credentials.credentials, False

    cookie_token = request.cookies.get(settings.AUTH_COOKIE_NAME)
    if not cookie_token:
        return None, False

    # Accept either raw token or "Bearer <token>".
    if cookie_token.lower().startswith("bearer "):
        return cookie_token.split(" ", 1)[1].strip(), True

    return cookie_token, True


def _extract_token(
    credentials: Optional[HTTPAuthorizationCredentials],
    request: Request | _CookieCarrier,
) -> Optional[str]:
    token, _ = _extract_token_with_source(credentials, request)
    return token


def _request_origin(request: Request) -> Optional[str]:
    origin = request.headers.get("origin")
    if origin:
        return origin.strip()

    referer = request.headers.get("referer")
    if not referer:
        return None

    parsed = urlparse(referer)
    if not parsed.scheme or not parsed.netloc:
        return None
    return f"{parsed.scheme}://{parsed.netloc}"


def _ensure_cookie_csrf_protection(request: Request) -> None:
    if not settings.CSRF_PROTECTION_ENABLED:
        return
    if request.method.upper() in _SAFE_METHODS:
        return

    csrf_cookie = request.cookies.get(settings.CSRF_COOKIE_NAME)
    csrf_header = request.headers.get(settings.CSRF_HEADER_NAME)
    if not csrf_cookie or not csrf_header or csrf_cookie != csrf_header:
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="CSRF validation failed",
        )

    origin = _request_origin(request)
    if origin:
        allowed_origins = set(settings.cors_origins_list)
        if origin not in allowed_origins:
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="Origin not allowed",
            )


def get_current_user(
    request: Request,
    db: Session = Depends(get_db),
    credentials: Optional[HTTPAuthorizationCredentials] = Depends(security),
) -> User:
    """Get current authenticated user from bearer token or auth cookie."""
    credentials_exception = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Could not validate credentials",
        headers={"WWW-Authenticate": "Bearer"},
    )

    token, from_cookie = _extract_token_with_source(credentials, request)
    if not token:
        raise credentials_exception

    if from_cookie:
        _ensure_cookie_csrf_protection(request)

    username = verify_token(token)
    if username is None:
        raise credentials_exception

    user = get_user_by_username(db, username=username)
    if user is None:
        raise credentials_exception

    return user


def get_current_user_optional(
    request: Request,
    db: Session = Depends(get_db),
    credentials: Optional[HTTPAuthorizationCredentials] = Depends(security),
) -> Optional[User]:
    """Get current user when token is present; otherwise return None."""
    token, from_cookie = _extract_token_with_source(credentials, request)
    if not token:
        return None

    if from_cookie:
        try:
            _ensure_cookie_csrf_protection(request)
        except HTTPException:
            return None

    username = verify_token(token)
    if username is None:
        return None

    return get_user_by_username(db, username=username)


def get_current_active_user(current_user: User = Depends(get_current_user)) -> User:
    """Get current active user."""
    if not is_active(current_user):
        raise HTTPException(status_code=400, detail="Inactive user")
    return current_user


def get_current_superuser(current_user: User = Depends(get_current_user)) -> User:
    """Get current superuser."""
    if not current_user.is_superuser:
        raise HTTPException(
            status_code=403,
            detail="The user doesn't have enough privileges",
        )
    return current_user

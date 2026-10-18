from datetime import datetime, timedelta, timezone
from typing import Literal, Optional

from jose import JWTError, jwt
from passlib.context import CryptContext

from app.core.config import settings

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")
MAX_PASSWORD_BYTES = 72


TokenType = Literal["access", "refresh"]


def _create_token(
    data: dict,
    expires_delta: Optional[timedelta] = None,
    token_type: TokenType = "access",
) -> str:
    to_encode = data.copy()
    if expires_delta:
        expire = datetime.now(timezone.utc) + expires_delta
    else:
        expire = datetime.now(timezone.utc) + timedelta(minutes=settings.ACCESS_TOKEN_EXPIRE_MINUTES)
    to_encode.update({"exp": expire, "type": token_type})
    return jwt.encode(to_encode, settings.SECRET_KEY, algorithm=settings.ALGORITHM)


def create_access_token(data: dict, expires_delta: Optional[timedelta] = None) -> str:
    return _create_token(data=data, expires_delta=expires_delta, token_type="access")


def create_refresh_token(data: dict, expires_delta: Optional[timedelta] = None) -> str:
    return _create_token(data=data, expires_delta=expires_delta, token_type="refresh")


def validate_password_bytes(password: str) -> str:
    if len(password.encode("utf-8")) > MAX_PASSWORD_BYTES:
        raise ValueError(f"Password must not exceed {MAX_PASSWORD_BYTES} UTF-8 bytes")
    return password


def verify_password(plain_password: str, hashed_password: str) -> bool:
    try:
        return pwd_context.verify(validate_password_bytes(plain_password), hashed_password)
    except ValueError:
        # Invalid credentials must not turn authentication rejection into a 500.
        return False


def get_password_hash(password: str) -> str:
    return pwd_context.hash(validate_password_bytes(password))


def verify_token(token: str, expected_type: TokenType = "access") -> Optional[int]:
    try:
        payload = jwt.decode(
            token,
            settings.SECRET_KEY,
            algorithms=[settings.ALGORITHM],
            options={"require_exp": True, "require_sub": True},
        )
        # Reject legacy username subjects, including names that look like numeric IDs.
        if payload.get("sub_type") != "user_id" or payload.get("type") != expected_type:
            return None
        subject = payload.get("sub")
        if not isinstance(subject, str) or len(subject) > 19 or not subject.isascii() or not subject.isdecimal():
            return None
        user_id = int(subject)
        if not 0 < user_id < 2**63 or str(user_id) != subject:
            return None
        return user_id
    except (JWTError, TypeError, ValueError, OverflowError):
        return None

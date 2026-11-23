from datetime import datetime, timedelta, timezone
from typing import Literal, Optional

import bcrypt
from jose import JWTError, jwt

from app.core.config import settings

MAX_PASSWORD_BYTES = 72


class _BcryptContext:
    """Direct bcrypt password hashing without deprecated passlib/crypt imports."""

    def __init__(self, rounds: int = 12) -> None:
        self.rounds = rounds

    def hash(self, password: str) -> str:
        if not isinstance(password, str):
            raise TypeError("Password must be a string")
        encoded = password.encode("utf-8")
        if b"\x00" in encoded:
            raise ValueError("Password must not contain NUL bytes")
        # Public get_password_hash validates MAX_PASSWORD_BYTES before calling hash();
        # slicing here preserves legacy bcrypt behavior for direct pwd_context.hash callers.
        return bcrypt.hashpw(encoded[:MAX_PASSWORD_BYTES], bcrypt.gensalt(rounds=self.rounds)).decode("ascii")

    def verify(self, plain_password: str, hashed_password: str) -> bool:
        if not isinstance(plain_password, str) or not isinstance(hashed_password, str):
            raise ValueError("Invalid password or hash type")
        if not hashed_password.startswith(("$2a$", "$2b$", "$2y$")):
            raise ValueError("Unsupported password hash format")
        plain_bytes = plain_password.encode("utf-8")
        if b"\x00" in plain_bytes or len(plain_bytes) > MAX_PASSWORD_BYTES:
            raise ValueError("Invalid password input")
        hashed_bytes = hashed_password.encode("ascii")
        if b"\x00" in hashed_bytes:
            raise ValueError("Invalid password hash")
        return bcrypt.checkpw(plain_bytes, hashed_bytes)


pwd_context = _BcryptContext()


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

from datetime import datetime, timedelta, timezone
from typing import Literal, Optional

from jose import JWTError, jwt
from passlib.context import CryptContext

from app.core.config import settings

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")


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


def verify_password(plain_password: str, hashed_password: str) -> bool:
    return pwd_context.verify(plain_password, hashed_password)


def get_password_hash(password: str) -> str:
    return pwd_context.hash(password)


def verify_token(token: str, expected_type: TokenType = "access") -> Optional[str]:
    try:
        payload = jwt.decode(token, settings.SECRET_KEY, algorithms=[settings.ALGORITHM])
        username = payload.get("sub")
        token_type = payload.get("type")
        if not isinstance(username, str):
            return None
        if expected_type == "refresh":
            if token_type != "refresh":
                return None
        elif token_type is not None and token_type != "access":
            return None
        return username
    except JWTError:
        return None

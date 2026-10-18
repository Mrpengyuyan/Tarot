from pydantic import AfterValidator, BaseModel, BeforeValidator, ConfigDict, EmailStr, Field, field_validator
from typing import Annotated, Optional

from app.core.security import MAX_PASSWORD_BYTES, validate_password_bytes


def _strip_username(value):
    return value.strip() if isinstance(value, str) else value


UsernameInput = Annotated[str, Field(min_length=3, max_length=50), BeforeValidator(_strip_username)]
PasswordInput = Annotated[
    str, Field(min_length=8, max_length=MAX_PASSWORD_BYTES), AfterValidator(validate_password_bytes)
]


class UserBase(BaseModel):
    """用户基础信息"""
    username: str = Field(min_length=3, max_length=50)
    email: EmailStr


class UserCreate(UserBase):
    """用户注册数据模型"""
    username: UsernameInput
    email: EmailStr = Field(max_length=100)
    password: PasswordInput


class UserLogin(BaseModel):
    """用户登录数据模型"""
    username: str
    password: str


class UserUpdate(BaseModel):
    """用户信息更新数据模型"""
    username: Optional[UsernameInput] = None
    email: Optional[EmailStr] = Field(default=None, max_length=100)
    password: Optional[PasswordInput] = None

    @field_validator("username", "email", "password")
    @classmethod
    def reject_null(cls, value):
        if value is None:
            raise ValueError("This field cannot be null")
        return value


class UserInDB(UserBase):
    """数据库中的用户信息（包含敏感字段）"""
    id: int
    hashed_password: str
    is_active: bool
    is_superuser: bool

    model_config = ConfigDict(from_attributes=True)


class User(UserBase):
    """用户响应数据模型（不包含敏感信息）"""
    id: int
    is_active: bool
    is_superuser: bool

    model_config = ConfigDict(from_attributes=True)


class Token(BaseModel):
    """JWT Token响应模型"""
    access_token: Optional[str] = None
    token_type: str = "bearer"


class TokenData(BaseModel):
    """Token中包含的数据"""
    username: Optional[str] = None 

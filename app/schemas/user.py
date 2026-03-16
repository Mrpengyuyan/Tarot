from pydantic import BaseModel, ConfigDict, EmailStr, Field
from typing import Optional


class UserBase(BaseModel):
    """用户基础信息"""
    username: str = Field(min_length=3, max_length=50)
    email: EmailStr


class UserCreate(UserBase):
    """用户注册数据模型"""
    password: str = Field(min_length=8, max_length=128)


class UserLogin(BaseModel):
    """用户登录数据模型"""
    username: str
    password: str


class UserUpdate(BaseModel):
    """用户信息更新数据模型"""
    username: Optional[str] = Field(default=None, min_length=3, max_length=50)
    email: Optional[EmailStr] = None
    password: Optional[str] = Field(default=None, min_length=8, max_length=128)


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
    access_token: str
    token_type: str


class TokenData(BaseModel):
    """Token中包含的数据"""
    username: Optional[str] = None 

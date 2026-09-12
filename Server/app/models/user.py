from __future__ import annotations

from datetime import datetime
from typing import TYPE_CHECKING

from sqlalchemy import Boolean, DateTime, Integer, String, Text, func
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.db.base_class import Base

if TYPE_CHECKING:
    from app.models.record import Prediction


class User(Base):
    """User model."""

    __tablename__ = "users"

    id: Mapped[int] = mapped_column(Integer, primary_key=True, index=True)
    username: Mapped[str] = mapped_column(String(50), unique=True, index=True, nullable=False, comment="用户名")
    email: Mapped[str] = mapped_column(String(100), unique=True, index=True, nullable=False, comment="邮箱")
    hashed_password: Mapped[str] = mapped_column(String(255), nullable=False, comment="加密密码")
    nickname: Mapped[str | None] = mapped_column(String(50), nullable=True, comment="昵称")
    avatar_url: Mapped[str | None] = mapped_column(String(500), nullable=True, comment="头像URL")
    birth_date: Mapped[datetime | None] = mapped_column(DateTime, nullable=True, comment="生日")
    zodiac_sign: Mapped[str | None] = mapped_column(String(20), nullable=True, comment="星座")
    bio: Mapped[str | None] = mapped_column(Text, nullable=True, comment="个人简介")
    is_active: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False, comment="账号是否激活")
    is_superuser: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False, comment="是否为超级用户")
    created_at: Mapped[datetime] = mapped_column(DateTime, default=func.now(), nullable=False, comment="创建时间")
    updated_at: Mapped[datetime] = mapped_column(
        DateTime,
        default=func.now(),
        onupdate=func.now(),
        nullable=False,
        comment="更新时间",
    )
    last_login: Mapped[datetime | None] = mapped_column(DateTime, nullable=True, comment="最后登录时间")
    prediction_count: Mapped[int] = mapped_column(Integer, default=0, nullable=False, comment="预测次数")

    predictions: Mapped[list["Prediction"]] = relationship("Prediction", back_populates="user")

    def __repr__(self) -> str:
        return f"<User(id={self.id}, username='{self.username}', email='{self.email}')>"

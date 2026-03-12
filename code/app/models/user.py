from sqlalchemy import Column, Integer, String, DateTime, Boolean, Text
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from app.db.base_class import Base

class User(Base):
    """用户模型 - 管理用户账号信息"""
    __tablename__ = "users"

    id = Column(Integer, primary_key=True, index=True)
    username = Column(String(50), unique=True, index=True, nullable=False, comment="用户名")
    email = Column(String(100), unique=True, index=True, nullable=False, comment="邮箱")
    hashed_password = Column(String(255), nullable=False, comment="加密密码")
    nickname = Column(String(50), nullable=True, comment="昵称")
    avatar_url = Column(String(500), nullable=True, comment="头像URL")
    birth_date = Column(DateTime, nullable=True, comment="生日")
    zodiac_sign = Column(String(20), nullable=True, comment="星座")
    bio = Column(Text, nullable=True, comment="个人简介")
    is_active = Column(Boolean, default=True, comment="账号是否激活")
    is_superuser = Column(Boolean, default=False, comment="是否为超级用户")
    # is_premium = Column(Boolean, default=False, comment="是否为付费用户")
    created_at = Column(DateTime, default=func.now(), nullable=False, comment="创建时间")
    updated_at = Column(DateTime, default=func.now(), onupdate=func.now(), nullable=False, comment="更新时间")
    last_login = Column(DateTime, nullable=True, comment="最后登录时间")
    prediction_count = Column(Integer, default=0, comment="预测次数")
    
    # 关联关系
    predictions = relationship("Prediction", back_populates="user")
    
    # 密码相关方法已移到 crud/user.py 中
    
    def __repr__(self):
        return f"<User(id={self.id}, username='{self.username}', email='{self.email}')>"
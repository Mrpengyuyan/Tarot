from pydantic import BaseModel, ConfigDict, Field, field_validator
from typing import List, Optional, Dict, Any

from .input_text import UTF8InputModel

class SpreadPosition(BaseModel):
    """牌位定义模型"""
    position: int
    name: str
    meaning: str

class SpreadTypeBase(BaseModel):
    """牌阵类型基础信息"""
    name: str
    name_en: Optional[str] = None
    description: str
    card_count: int
    difficulty_level: int = 1
    positions: List[SpreadPosition]
    layout_image_url: Optional[str] = None
    is_active: bool = True
    is_beginner_friendly: bool = False
    
    # 适用的问题类型
    suitable_for_love: bool = True
    suitable_for_career: bool = True
    suitable_for_finance: bool = True
    suitable_for_health: bool = True
    suitable_for_general: bool = True

class SpreadTypeCreate(SpreadTypeBase, UTF8InputModel):
    """创建牌阵类型数据模型"""
    card_count: int = Field(ge=1, le=78)
    difficulty_level: int = Field(default=1, ge=1, le=5)

class SpreadTypeUpdate(UTF8InputModel):
    """更新牌阵类型数据模型"""
    name: Optional[str] = None
    name_en: Optional[str] = None
    description: Optional[str] = None
    card_count: Optional[int] = Field(default=None, ge=1, le=78)
    difficulty_level: Optional[int] = Field(default=None, ge=1, le=5)
    positions: Optional[List[SpreadPosition]] = None
    layout_image_url: Optional[str] = None
    is_active: Optional[bool] = None
    is_beginner_friendly: Optional[bool] = None
    suitable_for_love: Optional[bool] = None
    suitable_for_career: Optional[bool] = None
    suitable_for_finance: Optional[bool] = None
    suitable_for_health: Optional[bool] = None
    suitable_for_general: Optional[bool] = None

    @field_validator(
        "name", "description", "card_count", "difficulty_level", "positions", "is_active",
        "is_beginner_friendly", "suitable_for_love", "suitable_for_career", "suitable_for_finance",
        "suitable_for_health", "suitable_for_general",
    )
    @classmethod
    def reject_null(cls, value):
        if value is None:
            raise ValueError("This field cannot be null")
        return value

class SpreadType(SpreadTypeBase):
    """牌阵类型响应数据模型"""
    id: int
    usage_count: int = 0

    model_config = ConfigDict(from_attributes=True)

class SpreadTypeSimple(BaseModel):
    """简化的牌阵信息（用于列表显示）"""
    id: int
    name: str
    name_en: Optional[str] = None
    description: str
    card_count: int
    difficulty_level: int
    positions: Optional[List[SpreadPosition]] = None
    is_beginner_friendly: bool
    usage_count: int = 0
    suitable_for_love: bool = True
    suitable_for_career: bool = True
    suitable_for_finance: bool = True
    suitable_for_health: bool = True
    suitable_for_general: bool = True

    model_config = ConfigDict(from_attributes=True)

class SpreadSuitability(BaseModel):
    """牌阵适用性信息"""
    id: int
    name: str
    suitable_for_love: bool
    suitable_for_career: bool
    suitable_for_finance: bool
    suitable_for_health: bool
    suitable_for_general: bool

    model_config = ConfigDict(from_attributes=True)

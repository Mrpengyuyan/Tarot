from pydantic import BaseModel, ConfigDict
from typing import Optional, List
from enum import Enum

class CardTypeEnum(str, Enum):
    """塔罗牌类型枚举"""
    MAJOR_ARCANA = "major_arcana"
    MINOR_ARCANA = "minor_arcana"

class SuitEnum(str, Enum):
    """花色枚举"""
    WANDS = "wands"
    CUPS = "cups"
    SWORDS = "swords"
    PENTACLES = "pentacles"

class TarotCardBase(BaseModel):
    """塔罗牌基础信息"""
    name_en: str
    name_zh: str
    card_number: int
    card_type: CardTypeEnum
    suit: Optional[SuitEnum] = None
    image_url: Optional[str] = None
    
    # 正位含义
    upright_meaning: str
    upright_love: Optional[str] = None
    upright_career: Optional[str] = None
    upright_finance: Optional[str] = None
    upright_health: Optional[str] = None
    
    # 逆位含义
    reversed_meaning: str
    reversed_love: Optional[str] = None
    reversed_career: Optional[str] = None
    reversed_finance: Optional[str] = None
    reversed_health: Optional[str] = None
    
    # 牌面描述和关键词
    description: Optional[str] = None
    keywords_upright: Optional[str] = None
    keywords_reversed: Optional[str] = None
    
    # 元素和星座关联
    element: Optional[str] = None
    zodiac: Optional[str] = None
    planet: Optional[str] = None

class TarotCardCreate(TarotCardBase):
    """创建塔罗牌数据模型"""
    pass

class TarotCardUpdate(BaseModel):
    """更新塔罗牌数据模型"""
    name_en: Optional[str] = None
    name_zh: Optional[str] = None
    card_number: Optional[int] = None
    card_type: Optional[CardTypeEnum] = None
    suit: Optional[SuitEnum] = None
    image_url: Optional[str] = None
    upright_meaning: Optional[str] = None
    upright_love: Optional[str] = None
    upright_career: Optional[str] = None
    upright_finance: Optional[str] = None
    upright_health: Optional[str] = None
    reversed_meaning: Optional[str] = None
    reversed_love: Optional[str] = None
    reversed_career: Optional[str] = None
    reversed_finance: Optional[str] = None
    reversed_health: Optional[str] = None
    description: Optional[str] = None
    keywords_upright: Optional[str] = None
    keywords_reversed: Optional[str] = None
    element: Optional[str] = None
    zodiac: Optional[str] = None
    planet: Optional[str] = None

class TarotCard(TarotCardBase):
    """塔罗牌响应数据模型"""
    id: int

    model_config = ConfigDict(from_attributes=True)

class TarotCardSimple(BaseModel):
    """简化的塔罗牌信息（用于列表显示）"""
    id: int
    name_en: str
    name_zh: str
    card_number: int
    card_type: CardTypeEnum
    suit: Optional[SuitEnum] = None
    image_url: Optional[str] = None

    model_config = ConfigDict(from_attributes=True)

class TarotCardMeaning(BaseModel):
    """塔罗牌含义模型（用于解读）"""
    id: int
    name_zh: str
    name_en: str
    is_reversed: bool
    meaning: str
    keywords: List[str]
    position: Optional[int] = None
    position_name: Optional[str] = None
    position_meaning: Optional[str] = None

    model_config = ConfigDict(from_attributes=True)

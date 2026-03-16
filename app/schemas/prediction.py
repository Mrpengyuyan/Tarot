from pydantic import BaseModel, ConfigDict, Field
from typing import List, Optional
from datetime import datetime
from enum import Enum
from .card import TarotCardSimple, TarotCardMeaning
from .spread import SpreadTypeSimple, SpreadPosition

class QuestionTypeEnum(str, Enum):
    """问题类型枚举"""
    LOVE = "love"
    CAREER = "career"
    FINANCE = "finance"
    HEALTH = "health"
    GENERAL = "general"

class PredictionStatusEnum(str, Enum):
    """预测状态枚举"""
    PENDING = "pending"
    PROCESSING = "processing"
    COMPLETED = "completed"
    FAILED = "failed"

# ======= 预测记录相关 =======

class PredictionBase(BaseModel):
    """预测记录基础信息"""
    question: str
    question_type: QuestionTypeEnum

class PredictionCreate(PredictionBase):
    """创建预测记录数据模型"""
    spread_type_id: int

class PredictionUpdate(BaseModel):
    """更新预测记录数据模型"""
    is_favorite: Optional[bool] = None
    user_rating: Optional[int] = None
    user_notes: Optional[str] = None

class Prediction(PredictionBase):
    """预测记录响应数据模型"""
    id: int
    user_id: int
    spread_type_id: int
    status: PredictionStatusEnum
    created_at: datetime
    completed_at: Optional[datetime] = None
    is_favorite: bool = False
    user_rating: Optional[int] = None
    user_notes: Optional[str] = None

    model_config = ConfigDict(from_attributes=True)

class PredictionSimple(BaseModel):
    """简化的预测记录（用于列表显示）"""
    id: int
    question: str
    question_type: QuestionTypeEnum
    status: PredictionStatusEnum
    created_at: datetime
    is_favorite: bool = False
    user_rating: Optional[int] = None

    model_config = ConfigDict(from_attributes=True)

# ======= 抽牌记录相关 =======

class CardDrawBase(BaseModel):
    """抽牌记录基础信息"""
    position: int
    is_reversed: bool = False

class CardDrawCreate(CardDrawBase):
    """创建抽牌记录数据模型"""
    tarot_card_id: int

class CardDraw(CardDrawBase):
    """抽牌记录响应数据模型"""
    id: int
    prediction_id: int
    tarot_card_id: int
    drawn_at: datetime
    tarot_card: Optional[TarotCardSimple] = None

    model_config = ConfigDict(from_attributes=True)

class CardDrawWithMeaning(CardDraw):
    """包含含义的抽牌记录"""
    card_meaning: Optional[TarotCardMeaning] = None
    position_name: Optional[str] = None
    position_meaning: Optional[str] = None

    model_config = ConfigDict(from_attributes=True)

# ======= 解读结果相关 =======

class InterpretationBase(BaseModel):
    """解读结果基础信息"""
    model_config = ConfigDict(protected_namespaces=())
    
    overall_interpretation: str
    card_analysis: Optional[str] = None
    relationship_analysis: Optional[str] = None
    advice: Optional[str] = None
    warning: Optional[str] = None
    summary: Optional[str] = None
    key_themes: Optional[str] = None

class InterpretationCreate(InterpretationBase):
    """创建解读结果数据模型"""
    model_used: Optional[str] = None
    model_version: Optional[str] = None
    confidence_score: Optional[float] = None

class Interpretation(InterpretationBase):
    """解读结果响应数据模型"""
    id: int
    prediction_id: int
    model_used: Optional[str] = None
    model_version: Optional[str] = None
    confidence_score: Optional[float] = None
    generated_at: datetime

    model_config = ConfigDict(from_attributes=True)

class InterpretationWithThemes(Interpretation):
    """包含主题列表的解读结果"""
    key_themes_list: List[str] = Field(default_factory=list)
    model_config = ConfigDict(from_attributes=True)

# ======= 完整预测详情 =======

class PredictionDetail(Prediction):
    """完整的预测详情（包含所有关联数据）"""
    spread_type: Optional[SpreadTypeSimple] = None
    card_draws: List[CardDrawWithMeaning] = Field(default_factory=list)
    interpretation: Optional[InterpretationWithThemes] = None
    model_config = ConfigDict(from_attributes=True)

# ======= 预测统计 =======

class PredictionStats(BaseModel):
    """用户预测统计"""
    total_predictions: int
    completed_predictions: int
    favorite_predictions: int
    most_used_question_type: Optional[QuestionTypeEnum] = None
    average_rating: Optional[float] = None

# ======= 批量抽牌请求 =======

class DrawCardsRequest(BaseModel):
    """抽牌请求模型"""
    prediction_id: int
    card_count: int

class DrawCardsResponse(BaseModel):
    """抽牌响应模型"""
    prediction_id: int
    card_draws: List[CardDraw]
    status: str = "success" 

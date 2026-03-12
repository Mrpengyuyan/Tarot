from sqlalchemy import Column, Integer, String, Text, DateTime, Boolean, ForeignKey, Float, Enum
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from app.db.base_class import Base
import enum

class QuestionType(enum.Enum):
    """问题类型"""
    LOVE = "love"        # 爱情
    CAREER = "career"    # 事业
    FINANCE = "finance"  # 财运
    HEALTH = "health"    # 健康
    GENERAL = "general"  # 一般

class PredictionStatus(enum.Enum):
    """预测状态"""
    PENDING = "pending"      # 待处理
    PROCESSING = "processing" # 处理中
    COMPLETED = "completed"   # 已完成
    FAILED = "failed"        # 失败


def enum_values(enum_cls):
    return [member.value for member in enum_cls]

class Prediction(Base):
    """预测记录模型 - 存储用户的每次预测"""
    __tablename__ = "predictions"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False, comment="用户ID")
    spread_type_id = Column(Integer, ForeignKey("spread_types.id"), nullable=False, comment="牌阵类型ID")
    
    # 用户问题信息
    question = Column(Text, nullable=False, comment="用户问题")
    question_type = Column(Enum(QuestionType, values_callable=enum_values), nullable=False, comment="问题类型")
    
    # 预测状态和结果
    status = Column(Enum(PredictionStatus, values_callable=enum_values), default=PredictionStatus.PENDING, comment="预测状态")
    created_at = Column(DateTime, default=func.now(), nullable=False, comment="创建时间")
    completed_at = Column(DateTime, nullable=True, comment="完成时间")
    
    # 用户标记和评分
    is_favorite = Column(Boolean, default=False, comment="是否收藏")
    user_rating = Column(Integer, nullable=True, comment="用户评分（1-5）")
    user_notes = Column(Text, nullable=True, comment="用户备注")
    
    # 关联关系
    user = relationship("User", back_populates="predictions")
    spread_type = relationship("SpreadType")
    card_draws = relationship("CardDraw", back_populates="prediction", cascade="all, delete-orphan")
    interpretation = relationship("Interpretation", back_populates="prediction", uselist=False, cascade="all, delete-orphan")
    
    def __repr__(self):
        return f"<Prediction(id={self.id}, user_id={self.user_id}, question='{self.question[:50]}...')>"

class CardDraw(Base):
    """抽牌记录模型 - 记录每次预测中抽到的具体牌"""
    __tablename__ = "card_draws"

    id = Column(Integer, primary_key=True, index=True)
    prediction_id = Column(Integer, ForeignKey("predictions.id"), nullable=False, comment="预测记录ID")
    tarot_card_id = Column(Integer, ForeignKey("tarot_cards.id"), nullable=False, comment="塔罗牌ID")
    
    position = Column(Integer, nullable=False, comment="牌位位置")
    is_reversed = Column(Boolean, default=False, comment="是否逆位")
    drawn_at = Column(DateTime, default=func.now(), nullable=False, comment="抽牌时间")
    
    # 关联关系
    prediction = relationship("Prediction", back_populates="card_draws")
    tarot_card = relationship("TarotCard")
    
    def get_card_meaning(self, aspect: str = "general") -> str:
        """获取这张牌在当前预测中的含义"""
        return self.tarot_card.get_meaning(self.is_reversed, aspect)
    
    def get_position_name(self) -> str:
        """获取牌位名称"""
        return self.prediction.spread_type.get_position_name(self.position)
    
    def get_position_meaning(self) -> str:
        """获取牌位含义"""
        return self.prediction.spread_type.get_position_meaning(self.position)
    
    def __repr__(self):
        reversed_text = "逆位" if self.is_reversed else "正位"
        return f"<CardDraw(id={self.id}, card='{self.tarot_card.name_zh}', position={self.position}, {reversed_text})>"

class Interpretation(Base):
    """解读结果模型 - 存储大模型的解读内容"""
    __tablename__ = "interpretations"

    id = Column(Integer, primary_key=True, index=True)
    prediction_id = Column(Integer, ForeignKey("predictions.id"), nullable=False, comment="预测记录ID")
    
    # AI解读内容
    overall_interpretation = Column(Text, nullable=False, comment="整体解读")
    card_analysis = Column(Text, nullable=True, comment="单牌分析")
    relationship_analysis = Column(Text, nullable=True, comment="牌间关系分析")
    advice = Column(Text, nullable=True, comment="建议")
    warning = Column(Text, nullable=True, comment="警告或注意事项")
    
    # 预测概要
    summary = Column(Text, nullable=True, comment="预测概要")
    key_themes = Column(String(500), nullable=True, comment="关键主题（逗号分隔）")
    
    # AI模型信息
    model_used = Column(String(100), nullable=True, comment="使用的AI模型")
    model_version = Column(String(50), nullable=True, comment="模型版本")
    confidence_score = Column(Float, nullable=True, comment="置信度分数")
    
    # 时间戳
    generated_at = Column(DateTime, default=func.now(), nullable=False, comment="生成时间")
    
    # 关联关系
    prediction = relationship("Prediction", back_populates="interpretation")
    
    def get_key_themes_list(self) -> list:
        """获取关键主题列表"""
        return self.key_themes.split(",") if self.key_themes else []
    
    def __repr__(self):
        return f"<Interpretation(id={self.id}, prediction_id={self.prediction_id}, model='{self.model_used}')>"

from sqlalchemy import Column, Integer, String, Text, Boolean, Enum
from app.db.base_class import Base
import enum

class CardType(enum.Enum):
    """塔罗牌类型"""
    MAJOR_ARCANA = "major_arcana"  # 大阿卡纳
    MINOR_ARCANA = "minor_arcana"  # 小阿卡纳

class Suit(enum.Enum):
    """花色（仅用于小阿卡纳）"""
    WANDS = "wands"        # 权杖
    CUPS = "cups"          # 圣杯
    SWORDS = "swords"      # 宝剑
    PENTACLES = "pentacles" # 星币

class TarotCard(Base):
    """塔罗牌模型 - 存储所有塔罗牌的详细信息"""
    __tablename__ = "tarot_cards"

    id = Column(Integer, primary_key=True, index=True)
    name_en = Column(String(100), nullable=False, comment="英文名称")
    name_zh = Column(String(100), nullable=False, comment="中文名称")
    card_number = Column(Integer, nullable=False, comment="牌序号")
    card_type = Column(Enum(CardType), nullable=False, comment="牌类型")
    suit = Column(Enum(Suit), nullable=True, comment="花色（小阿卡纳）")
    image_url = Column(String(500), nullable=True, comment="牌面图片URL")
    
    # 正位含义
    upright_meaning = Column(Text, nullable=False, comment="正位含义")
    upright_love = Column(Text, nullable=True, comment="正位爱情含义")
    upright_career = Column(Text, nullable=True, comment="正位事业含义")
    upright_finance = Column(Text, nullable=True, comment="正位财运含义")
    upright_health = Column(Text, nullable=True, comment="正位健康含义")
    
    # 逆位含义
    reversed_meaning = Column(Text, nullable=False, comment="逆位含义")
    reversed_love = Column(Text, nullable=True, comment="逆位爱情含义")
    reversed_career = Column(Text, nullable=True, comment="逆位事业含义")
    reversed_finance = Column(Text, nullable=True, comment="逆位财运含义")
    reversed_health = Column(Text, nullable=True, comment="逆位健康含义")
    
    # 牌面描述和关键词
    description = Column(Text, nullable=True, comment="牌面描述")
    keywords_upright = Column(Text, nullable=True, comment="正位关键词（逗号分隔）")
    keywords_reversed = Column(Text, nullable=True, comment="逆位关键词（逗号分隔）")
    
    # 元素和星座关联
    element = Column(String(20), nullable=True, comment="对应元素")
    zodiac = Column(String(50), nullable=True, comment="对应星座")
    planet = Column(String(50), nullable=True, comment="对应星球")
    
    def get_meaning(self, is_reversed: bool = False, aspect: str = "general"):
        """获取牌的含义
        Args:
            is_reversed: 是否逆位
            aspect: 方面（general, love, career, finance, health）
        """
        if is_reversed:
            meanings = {
                "general": self.reversed_meaning,
                "love": self.reversed_love,
                "career": self.reversed_career,
                "finance": self.reversed_finance,
                "health": self.reversed_health
            }
        else:
            meanings = {
                "general": self.upright_meaning,
                "love": self.upright_love,
                "career": self.upright_career,
                "finance": self.upright_finance,
                "health": self.upright_health
            }
        
        return meanings.get(aspect, meanings["general"])
    
    def get_keywords(self, is_reversed: bool = False):
        """获取关键词列表"""
        keywords = self.keywords_reversed if is_reversed else self.keywords_upright
        return keywords.split(",") if keywords else []
    
    def __repr__(self):
        return f"<TarotCard(id={self.id}, name='{self.name_zh}', type='{self.card_type.value}')>"
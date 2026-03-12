from sqlalchemy import Column, Integer, String, Text, Boolean, JSON
from app.db.base_class import Base

class SpreadType(Base):
    """牌阵类型模型 - 定义各种塔罗牌阵"""
    __tablename__ = "spread_types"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(100), nullable=False, comment="牌阵名称")
    name_en = Column(String(100), nullable=True, comment="英文名称")
    description = Column(Text, nullable=False, comment="牌阵描述")
    card_count = Column(Integer, nullable=False, comment="需要的牌数量")
    difficulty_level = Column(Integer, default=1, comment="难度等级（1-5）")
    
    # 牌位定义（JSON格式存储）
    positions = Column(JSON, nullable=False, comment="牌位定义和含义")
    # 例如: [
    #   {"position": 1, "name": "过去", "meaning": "影响现状的过去因素"},
    #   {"position": 2, "name": "现在", "meaning": "当前的状况"},
    #   {"position": 3, "name": "未来", "meaning": "可能的发展趋势"}
    # ]
    
    layout_image_url = Column(String(500), nullable=True, comment="牌阵布局图片URL")
    is_active = Column(Boolean, default=True, comment="是否启用")
    is_beginner_friendly = Column(Boolean, default=False, comment="是否适合初学者")
    
    # 适用的问题类型
    suitable_for_love = Column(Boolean, default=True, comment="适用于爱情问题")
    suitable_for_career = Column(Boolean, default=True, comment="适用于事业问题")
    suitable_for_finance = Column(Boolean, default=True, comment="适用于财运问题")
    suitable_for_health = Column(Boolean, default=True, comment="适用于健康问题")
    suitable_for_general = Column(Boolean, default=True, comment="适用于一般问题")
    
    usage_count = Column(Integer, default=0, comment="使用次数")
    
    def get_position_meaning(self, position: int) -> str:
        """获取特定牌位的含义"""
        for pos in self.positions:
            if pos.get("position") == position:
                return pos.get("meaning", "")
        return ""
    
    def get_position_name(self, position: int) -> str:
        """获取特定牌位的名称"""
        for pos in self.positions:
            if pos.get("position") == position:
                return pos.get("name", f"第{position}位")
        return f"第{position}位"
    
    def is_suitable_for_question_type(self, question_type: str) -> bool:
        """检查是否适用于特定问题类型"""
        suitability_map = {
            "love": self.suitable_for_love,
            "career": self.suitable_for_career,
            "finance": self.suitable_for_finance,
            "health": self.suitable_for_health,
            "general": self.suitable_for_general
        }
        return suitability_map.get(question_type, self.suitable_for_general)
    
    def __repr__(self):
        return f"<SpreadType(id={self.id}, name='{self.name}', cards={self.card_count})>" 
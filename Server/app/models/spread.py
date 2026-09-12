from __future__ import annotations

from typing import Any

from sqlalchemy import Boolean, Integer, JSON, String, Text
from sqlalchemy.orm import Mapped, mapped_column

from app.db.base_class import Base


class SpreadType(Base):
    """Tarot spread definition."""

    __tablename__ = "spread_types"

    id: Mapped[int] = mapped_column(Integer, primary_key=True, index=True)
    name: Mapped[str] = mapped_column(String(100), nullable=False, comment="牌阵名称")
    name_en: Mapped[str | None] = mapped_column(String(100), nullable=True, comment="英文名称")
    description: Mapped[str] = mapped_column(Text, nullable=False, comment="牌阵描述")
    card_count: Mapped[int] = mapped_column(Integer, nullable=False, comment="需要的牌数量")
    difficulty_level: Mapped[int] = mapped_column(Integer, default=1, nullable=False, comment="难度等级（1-5）")
    positions: Mapped[list[dict[str, Any]]] = mapped_column(JSON, nullable=False, comment="牌位定义和含义")

    layout_image_url: Mapped[str | None] = mapped_column(String(500), nullable=True, comment="牌阵布局图片URL")
    is_active: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False, comment="是否启用")
    is_beginner_friendly: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False, comment="是否适合初学者")

    suitable_for_love: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False, comment="适用于爱情问题")
    suitable_for_career: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False, comment="适用于事业问题")
    suitable_for_finance: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False, comment="适用于财运问题")
    suitable_for_health: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False, comment="适用于健康问题")
    suitable_for_general: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False, comment="适用于一般问题")

    usage_count: Mapped[int] = mapped_column(Integer, default=0, nullable=False, comment="使用次数")

    def get_position_meaning(self, position: int) -> str:
        for pos in self.positions:
            if pos.get("position") == position:
                return str(pos.get("meaning", ""))
        return ""

    def get_position_name(self, position: int) -> str:
        for pos in self.positions:
            if pos.get("position") == position:
                return str(pos.get("name", f"第{position}位"))
        return f"第{position}位"

    def is_suitable_for_question_type(self, question_type: str) -> bool:
        suitability_map = {
            "love": self.suitable_for_love,
            "career": self.suitable_for_career,
            "finance": self.suitable_for_finance,
            "health": self.suitable_for_health,
            "general": self.suitable_for_general,
        }
        return suitability_map.get(question_type, self.suitable_for_general)

    def __repr__(self) -> str:
        return f"<SpreadType(id={self.id}, name='{self.name}', cards={self.card_count})>"

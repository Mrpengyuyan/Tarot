from __future__ import annotations

import enum

from sqlalchemy import Enum as SAEnum
from sqlalchemy import Integer, String, Text
from sqlalchemy.orm import Mapped, mapped_column

from app.db.base_class import Base


class CardType(enum.Enum):
    """Tarot card type."""

    MAJOR_ARCANA = "major_arcana"
    MINOR_ARCANA = "minor_arcana"


class Suit(enum.Enum):
    """Suit for minor arcana cards."""

    WANDS = "wands"
    CUPS = "cups"
    SWORDS = "swords"
    PENTACLES = "pentacles"


class TarotCard(Base):
    """Tarot card model."""

    __tablename__ = "tarot_cards"

    id: Mapped[int] = mapped_column(Integer, primary_key=True, index=True)
    name_en: Mapped[str] = mapped_column(String(100), nullable=False, comment="英文名称")
    name_zh: Mapped[str] = mapped_column(String(100), nullable=False, comment="中文名称")
    card_number: Mapped[int] = mapped_column(Integer, nullable=False, comment="牌序号")
    card_type: Mapped[CardType] = mapped_column(SAEnum(CardType), nullable=False, comment="牌类型")
    suit: Mapped[Suit | None] = mapped_column(SAEnum(Suit), nullable=True, comment="花色（小阿卡纳）")
    image_url: Mapped[str | None] = mapped_column(String(500), nullable=True, comment="牌面图片URL")

    upright_meaning: Mapped[str] = mapped_column(Text, nullable=False, comment="正位含义")
    upright_love: Mapped[str | None] = mapped_column(Text, nullable=True, comment="正位爱情含义")
    upright_career: Mapped[str | None] = mapped_column(Text, nullable=True, comment="正位事业含义")
    upright_finance: Mapped[str | None] = mapped_column(Text, nullable=True, comment="正位财运含义")
    upright_health: Mapped[str | None] = mapped_column(Text, nullable=True, comment="正位健康含义")

    reversed_meaning: Mapped[str] = mapped_column(Text, nullable=False, comment="逆位含义")
    reversed_love: Mapped[str | None] = mapped_column(Text, nullable=True, comment="逆位爱情含义")
    reversed_career: Mapped[str | None] = mapped_column(Text, nullable=True, comment="逆位事业含义")
    reversed_finance: Mapped[str | None] = mapped_column(Text, nullable=True, comment="逆位财运含义")
    reversed_health: Mapped[str | None] = mapped_column(Text, nullable=True, comment="逆位健康含义")

    description: Mapped[str | None] = mapped_column(Text, nullable=True, comment="牌面描述")
    keywords_upright: Mapped[str | None] = mapped_column(Text, nullable=True, comment="正位关键词（逗号分隔）")
    keywords_reversed: Mapped[str | None] = mapped_column(Text, nullable=True, comment="逆位关键词（逗号分隔）")

    element: Mapped[str | None] = mapped_column(String(20), nullable=True, comment="对应元素")
    zodiac: Mapped[str | None] = mapped_column(String(50), nullable=True, comment="对应星座")
    planet: Mapped[str | None] = mapped_column(String(50), nullable=True, comment="对应星球")

    def get_meaning(self, is_reversed: bool = False, aspect: str = "general") -> str | None:
        if is_reversed:
            meanings: dict[str, str | None] = {
                "general": self.reversed_meaning,
                "love": self.reversed_love,
                "career": self.reversed_career,
                "finance": self.reversed_finance,
                "health": self.reversed_health,
            }
        else:
            meanings = {
                "general": self.upright_meaning,
                "love": self.upright_love,
                "career": self.upright_career,
                "finance": self.upright_finance,
                "health": self.upright_health,
            }
        return meanings.get(aspect, meanings["general"])

    def get_keywords(self, is_reversed: bool = False) -> list[str]:
        keywords = self.keywords_reversed if is_reversed else self.keywords_upright
        return keywords.split(",") if keywords else []

    def __repr__(self) -> str:
        return f"<TarotCard(id={self.id}, name='{self.name_zh}', type='{self.card_type.value}')>"

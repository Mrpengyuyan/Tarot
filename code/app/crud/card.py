from typing import List, Optional
from sqlalchemy.orm import Session
from sqlalchemy import and_, or_
from app.models.tarot_card import TarotCard, CardType, Suit
from app.schemas.card import TarotCardCreate, TarotCardUpdate
import random

def get_card_by_id(db: Session, card_id: int) -> Optional[TarotCard]:
    """根据ID获取塔罗牌"""
    return db.query(TarotCard).filter(TarotCard.id == card_id).first()

def get_card_by_number_and_type(db: Session, card_number: int, card_type: CardType, suit: Optional[Suit] = None) -> Optional[TarotCard]:
    """根据牌号和类型获取塔罗牌"""
    query = db.query(TarotCard).filter(
        and_(
            TarotCard.card_number == card_number,
            TarotCard.card_type == card_type
        )
    )
    if suit:
        query = query.filter(TarotCard.suit == suit)
    return query.first()

def get_cards(db: Session, skip: int = 0, limit: int = 100) -> List[TarotCard]:
    """获取塔罗牌列表"""
    return db.query(TarotCard).offset(skip).limit(limit).all()

def get_cards_by_type(db: Session, card_type: CardType, skip: int = 0, limit: int = 100) -> List[TarotCard]:
    """根据类型获取塔罗牌列表"""
    return db.query(TarotCard).filter(TarotCard.card_type == card_type).offset(skip).limit(limit).all()

def get_cards_by_suit(db: Session, suit: Suit, skip: int = 0, limit: int = 100) -> List[TarotCard]:
    """根据花色获取塔罗牌列表"""
    return db.query(TarotCard).filter(TarotCard.suit == suit).offset(skip).limit(limit).all()

def search_cards(db: Session, search_term: str, skip: int = 0, limit: int = 100) -> List[TarotCard]:
    """搜索塔罗牌"""
    search_pattern = f"%{search_term}%"
    return db.query(TarotCard).filter(
        or_(
            TarotCard.name_zh.ilike(search_pattern),
            TarotCard.name_en.ilike(search_pattern),
            TarotCard.keywords_upright.ilike(search_pattern),
            TarotCard.keywords_reversed.ilike(search_pattern)
        )
    ).offset(skip).limit(limit).all()

def create_card(db: Session, card_create: TarotCardCreate) -> TarotCard:
    """创建塔罗牌"""
    db_card = TarotCard(**card_create.model_dump())
    db.add(db_card)
    db.commit()
    db.refresh(db_card)
    return db_card

def update_card(db: Session, db_card: TarotCard, card_update: TarotCardUpdate) -> TarotCard:
    """更新塔罗牌信息"""
    update_data = card_update.model_dump(exclude_unset=True)
    for field, value in update_data.items():
        setattr(db_card, field, value)
    
    db.commit()
    db.refresh(db_card)
    return db_card

def delete_card(db: Session, card_id: int) -> bool:
    """删除塔罗牌"""
    db_card = get_card_by_id(db, card_id)
    if db_card:
        db.delete(db_card)
        db.commit()
        return True
    return False

def get_total_cards_count(db: Session) -> int:
    """获取塔罗牌总数"""
    return db.query(TarotCard).count()

def get_major_arcana_cards(db: Session) -> List[TarotCard]:
    """获取大阿卡纳牌"""
    return db.query(TarotCard).filter(TarotCard.card_type == CardType.MAJOR_ARCANA).order_by(TarotCard.card_number).all()

def get_minor_arcana_cards(db: Session) -> List[TarotCard]:
    """获取小阿卡纳牌"""
    return db.query(TarotCard).filter(TarotCard.card_type == CardType.MINOR_ARCANA).order_by(TarotCard.suit, TarotCard.card_number).all()

def draw_random_cards(db: Session, count: int, exclude_ids: Optional[List[int]] = None) -> List[TarotCard]:
    """随机抽取指定数量的塔罗牌"""
    query = db.query(TarotCard)
    
    # 排除指定的卡牌
    if exclude_ids:
        query = query.filter(~TarotCard.id.in_(exclude_ids))
    
    all_cards = query.all()
    
    # 如果请求的数量超过可用卡牌数量，返回所有可用卡牌
    if count >= len(all_cards):
        return all_cards
    
    # 随机抽取
    return random.sample(all_cards, count)

def get_card_meaning(db: Session, card_id: int, is_reversed: bool = False, aspect: str = "general") -> Optional[str]:
    """获取塔罗牌含义"""
    card = get_card_by_id(db, card_id)
    if card:
        return card.get_meaning(is_reversed, aspect)
    return None

def get_card_keywords(db: Session, card_id: int, is_reversed: bool = False) -> List[str]:
    """获取塔罗牌关键词"""
    card = get_card_by_id(db, card_id)
    if card:
        return card.get_keywords(is_reversed)
    return []

def validate_card_exists(db: Session, card_id: int) -> bool:
    """验证塔罗牌是否存在"""
    return db.query(TarotCard).filter(TarotCard.id == card_id).first() is not None

def batch_create_cards(db: Session, cards_data: List[TarotCardCreate]) -> List[TarotCard]:
    """批量创建塔罗牌"""
    db_cards = []
    for card_data in cards_data:
        db_card = TarotCard(**card_data.model_dump())
        db.add(db_card)
        db_cards.append(db_card)
    
    db.commit()
    for db_card in db_cards:
        db.refresh(db_card)
    
    return db_cards 

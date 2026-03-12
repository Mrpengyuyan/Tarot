from typing import List, Optional
from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session

from app.db.session import get_db
from app.api.deps import get_current_active_user
from app.crud import card as card_crud
from app.schemas.card import (
    TarotCard, TarotCardCreate, TarotCardUpdate, TarotCardSimple,
    TarotCardMeaning, CardTypeEnum, SuitEnum
)
from app.schemas.user import User
from app.models.tarot_card import CardType, Suit

router = APIRouter()

@router.get("/", response_model=List[TarotCardSimple], summary="获取塔罗牌列表")
def get_cards(
    skip: int = Query(0, ge=0, description="跳过的数量"),
    limit: int = Query(100, ge=1, le=500, description="返回的数量"),
    card_type: Optional[CardTypeEnum] = Query(None, description="牌类型筛选"),
    suit: Optional[SuitEnum] = Query(None, description="花色筛选"),
    search: Optional[str] = Query(None, description="搜索关键词"),
    db: Session = Depends(get_db)
):
    """
    获取塔罗牌列表
    - 支持分页
    - 支持按类型和花色筛选
    - 支持关键词搜索
    """
    if search:
        cards = card_crud.search_cards(db, search_term=search, skip=skip, limit=limit)
    elif card_type and suit:
        # 转换枚举
        db_card_type = CardType(card_type.value)
        db_suit = Suit(suit.value)
        cards = card_crud.get_cards_by_suit(db, suit=db_suit, skip=skip, limit=limit)
        # 进一步筛选类型
        cards = [card for card in cards if card.card_type == db_card_type]
    elif card_type:
        db_card_type = CardType(card_type.value)
        cards = card_crud.get_cards_by_type(db, card_type=db_card_type, skip=skip, limit=limit)
    elif suit:
        db_suit = Suit(suit.value)
        cards = card_crud.get_cards_by_suit(db, suit=db_suit, skip=skip, limit=limit)
    else:
        cards = card_crud.get_cards(db, skip=skip, limit=limit)
    
    return cards

@router.get("/count", summary="获取塔罗牌总数")
def get_cards_count(db: Session = Depends(get_db)):
    """获取塔罗牌总数统计"""
    total_count = card_crud.get_total_cards_count(db)
    major_arcana = card_crud.get_major_arcana_cards(db)
    minor_arcana = card_crud.get_minor_arcana_cards(db)
    
    return {
        "total_cards": total_count,
        "major_arcana_count": len(major_arcana),
        "minor_arcana_count": len(minor_arcana)
    }

@router.get("/major-arcana", response_model=List[TarotCardSimple], summary="获取大阿卡纳牌")
def get_major_arcana_cards(db: Session = Depends(get_db)):
    """获取所有大阿卡纳牌，按序号排序"""
    return card_crud.get_major_arcana_cards(db)

@router.get("/minor-arcana", response_model=List[TarotCardSimple], summary="获取小阿卡纳牌")
def get_minor_arcana_cards(db: Session = Depends(get_db)):
    """获取所有小阿卡纳牌，按花色和序号排序"""
    return card_crud.get_minor_arcana_cards(db)

@router.get("/draw", response_model=List[TarotCardSimple], summary="随机抽牌")
def draw_random_cards(
    count: int = Query(1, ge=1, le=78, description="抽牌数量"),
    exclude_ids: Optional[List[int]] = Query(None, description="排除的牌ID列表"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user)
):
    """
    随机抽取指定数量的塔罗牌
    - 可以排除指定的牌
    - 需要用户登录
    """
    cards = card_crud.draw_random_cards(db, count=count, exclude_ids=exclude_ids)
    return cards

@router.get("/search", response_model=List[TarotCardSimple], summary="搜索塔罗牌")
def search_cards(
    q: str = Query(..., description="搜索关键词"),
    skip: int = Query(0, ge=0),
    limit: int = Query(50, ge=1, le=100),
    db: Session = Depends(get_db)
):
    """根据关键词搜索塔罗牌"""
    return card_crud.search_cards(db, search_term=q, skip=skip, limit=limit)

@router.get("/{card_id}", response_model=TarotCard, summary="获取塔罗牌详情")
def get_card_detail(
    card_id: int,
    db: Session = Depends(get_db)
):
    """根据ID获取塔罗牌的详细信息"""
    card = card_crud.get_card_by_id(db, card_id=card_id)
    if not card:
        raise HTTPException(status_code=404, detail="塔罗牌不存在")
    return card

@router.get("/{card_id}/meaning", response_model=TarotCardMeaning, summary="获取塔罗牌含义")
def get_card_meaning(
    card_id: int,
    is_reversed: bool = Query(False, description="是否逆位"),
    aspect: str = Query("general", description="解读方面"),
    position: Optional[int] = Query(None, description="牌位位置"),
    db: Session = Depends(get_db)
):
    """
    获取塔罗牌的含义和关键词
    - aspect: general, love, career, finance, health
    """
    card = card_crud.get_card_by_id(db, card_id=card_id)
    if not card:
        raise HTTPException(status_code=404, detail="塔罗牌不存在")
    
    meaning = card_crud.get_card_meaning(db, card_id=card_id, is_reversed=is_reversed, aspect=aspect)
    keywords = card_crud.get_card_keywords(db, card_id=card_id, is_reversed=is_reversed)
    
    return TarotCardMeaning(
        id=card.id,
        name_zh=card.name_zh,
        name_en=card.name_en,
        is_reversed=is_reversed,
        meaning=meaning or "",
        keywords=keywords,
        position=position,
        position_name=f"第{position}位" if position else None,
        position_meaning=None  # 这个需要结合牌阵信息
    )

@router.post("/", response_model=TarotCard, summary="创建塔罗牌")
def create_card(
    card_create: TarotCardCreate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user)
):
    """
    创建新的塔罗牌
    - 需要超级用户权限
    """
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="需要管理员权限")
    
    # 检查是否已存在相同的牌
    existing_card = card_crud.get_card_by_number_and_type(
        db, 
        card_number=card_create.card_number,
        card_type=CardType(card_create.card_type.value),
        suit=Suit(card_create.suit.value) if card_create.suit else None
    )
    if existing_card:
        raise HTTPException(status_code=400, detail="相同的塔罗牌已存在")
    
    return card_crud.create_card(db=db, card_create=card_create)

@router.put("/{card_id}", response_model=TarotCard, summary="更新塔罗牌")
def update_card(
    card_id: int,
    card_update: TarotCardUpdate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user)
):
    """
    更新塔罗牌信息
    - 需要超级用户权限
    """
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="需要管理员权限")
    
    card = card_crud.get_card_by_id(db, card_id=card_id)
    if not card:
        raise HTTPException(status_code=404, detail="塔罗牌不存在")
    
    return card_crud.update_card(db=db, db_card=card, card_update=card_update)

@router.delete("/{card_id}", summary="删除塔罗牌")
def delete_card(
    card_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user)
):
    """
    删除塔罗牌
    - 需要超级用户权限
    """
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="需要管理员权限")
    
    success = card_crud.delete_card(db, card_id=card_id)
    if not success:
        raise HTTPException(status_code=404, detail="塔罗牌不存在")
    
    return {"message": "塔罗牌删除成功"}

@router.post("/batch", response_model=List[TarotCard], summary="批量创建塔罗牌")
def batch_create_cards(
    cards_data: List[TarotCardCreate],
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user)
):
    """
    批量创建塔罗牌
    - 需要超级用户权限
    - 用于初始化数据
    """
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="需要管理员权限")
    
    if len(cards_data) > 100:
        raise HTTPException(status_code=400, detail="单次最多创建100张牌")
    
    return card_crud.batch_create_cards(db=db, cards_data=cards_data) 
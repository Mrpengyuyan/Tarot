from typing import List, Optional
from sqlalchemy.orm import Session
from sqlalchemy import and_, or_
from app.models.spread import SpreadType
from app.schemas.spread import SpreadTypeCreate, SpreadTypeUpdate

def get_spread_by_id(db: Session, spread_id: int) -> Optional[SpreadType]:
    """根据ID获取牌阵类型"""
    return db.query(SpreadType).filter(SpreadType.id == spread_id).first()

def get_spread_by_name(db: Session, name: str) -> Optional[SpreadType]:
    """根据名称获取牌阵类型"""
    return db.query(SpreadType).filter(SpreadType.name == name).first()

def get_spreads(db: Session, skip: int = 0, limit: int = 100, active_only: bool = True) -> List[SpreadType]:
    """获取牌阵类型列表"""
    query = db.query(SpreadType)
    if active_only:
        query = query.filter(SpreadType.is_active == True)
    return query.offset(skip).limit(limit).all()

def get_spreads_by_difficulty(db: Session, difficulty_level: int, skip: int = 0, limit: int = 100) -> List[SpreadType]:
    """根据难度等级获取牌阵"""
    return db.query(SpreadType).filter(
        and_(
            SpreadType.difficulty_level == difficulty_level,
            SpreadType.is_active == True
        )
    ).offset(skip).limit(limit).all()

def get_spreads_by_card_count(db: Session, card_count: int, skip: int = 0, limit: int = 100) -> List[SpreadType]:
    """根据牌数获取牌阵"""
    return db.query(SpreadType).filter(
        and_(
            SpreadType.card_count == card_count,
            SpreadType.is_active == True
        )
    ).offset(skip).limit(limit).all()

def get_beginner_friendly_spreads(db: Session, skip: int = 0, limit: int = 100) -> List[SpreadType]:
    """获取适合初学者的牌阵"""
    return db.query(SpreadType).filter(
        and_(
            SpreadType.is_beginner_friendly == True,
            SpreadType.is_active == True
        )
    ).offset(skip).limit(limit).all()

def get_spreads_for_question_type(db: Session, question_type: str, skip: int = 0, limit: int = 100) -> List[SpreadType]:
    """根据问题类型获取适合的牌阵"""
    suitability_map = {
        "love": SpreadType.suitable_for_love,
        "career": SpreadType.suitable_for_career,
        "finance": SpreadType.suitable_for_finance,
        "health": SpreadType.suitable_for_health,
        "general": SpreadType.suitable_for_general
    }
    
    suitability_field = suitability_map.get(question_type, SpreadType.suitable_for_general)
    
    return db.query(SpreadType).filter(
        and_(
            suitability_field == True,
            SpreadType.is_active == True
        )
    ).offset(skip).limit(limit).all()

def search_spreads(db: Session, search_term: str, skip: int = 0, limit: int = 100) -> List[SpreadType]:
    """搜索牌阵"""
    search_pattern = f"%{search_term}%"
    return db.query(SpreadType).filter(
        and_(
            or_(
                SpreadType.name.ilike(search_pattern),
                SpreadType.name_en.ilike(search_pattern),
                SpreadType.description.ilike(search_pattern)
            ),
            SpreadType.is_active == True
        )
    ).offset(skip).limit(limit).all()

def create_spread(db: Session, spread_create: SpreadTypeCreate) -> SpreadType:
    """创建牌阵类型"""
    # 将positions列表转换为字典格式存储
    positions_data = [pos.model_dump() for pos in spread_create.positions]
    
    spread_data = spread_create.model_dump()
    spread_data['positions'] = positions_data
    
    db_spread = SpreadType(**spread_data)
    db.add(db_spread)
    db.commit()
    db.refresh(db_spread)
    return db_spread

def update_spread(db: Session, db_spread: SpreadType, spread_update: SpreadTypeUpdate) -> SpreadType:
    """更新牌阵类型信息"""
    update_data = spread_update.model_dump(exclude_unset=True)
    
    # 处理positions字段
    if 'positions' in update_data and update_data['positions']:
        update_data['positions'] = [pos.model_dump() for pos in update_data['positions']]
    
    for field, value in update_data.items():
        setattr(db_spread, field, value)
    
    db.commit()
    db.refresh(db_spread)
    return db_spread

def delete_spread(db: Session, spread_id: int) -> bool:
    """删除牌阵类型（软删除，设置为不活跃）"""
    db_spread = get_spread_by_id(db, spread_id)
    if db_spread:
        db_spread.is_active = False
        db.commit()
        return True
    return False

def hard_delete_spread(db: Session, spread_id: int) -> bool:
    """硬删除牌阵类型"""
    db_spread = get_spread_by_id(db, spread_id)
    if db_spread:
        db.delete(db_spread)
        db.commit()
        return True
    return False

def get_total_spreads_count(db: Session, active_only: bool = True) -> int:
    """获取牌阵总数"""
    query = db.query(SpreadType)
    if active_only:
        query = query.filter(SpreadType.is_active == True)
    return query.count()

def get_popular_spreads(db: Session, limit: int = 10) -> List[SpreadType]:
    """获取热门牌阵（按使用次数排序）"""
    return db.query(SpreadType).filter(
        SpreadType.is_active == True
    ).order_by(SpreadType.usage_count.desc()).limit(limit).all()

def increment_spread_usage(db: Session, spread_id: int) -> bool:
    """增加牌阵使用次数"""
    db_spread = get_spread_by_id(db, spread_id)
    if db_spread:
        db_spread.usage_count += 1
        db.commit()
        return True
    return False

def validate_spread_exists(db: Session, spread_id: int) -> bool:
    """验证牌阵是否存在且活跃"""
    return db.query(SpreadType).filter(
        and_(
            SpreadType.id == spread_id,
            SpreadType.is_active == True
        )
    ).first() is not None

def get_spreads_by_difficulty_range(db: Session, min_difficulty: int, max_difficulty: int, skip: int = 0, limit: int = 100) -> List[SpreadType]:
    """根据难度范围获取牌阵"""
    return db.query(SpreadType).filter(
        and_(
            SpreadType.difficulty_level >= min_difficulty,
            SpreadType.difficulty_level <= max_difficulty,
            SpreadType.is_active == True
        )
    ).offset(skip).limit(limit).all()

def get_spreads_by_card_count_range(db: Session, min_cards: int, max_cards: int, skip: int = 0, limit: int = 100) -> List[SpreadType]:
    """根据牌数范围获取牌阵"""
    return db.query(SpreadType).filter(
        and_(
            SpreadType.card_count >= min_cards,
            SpreadType.card_count <= max_cards,
            SpreadType.is_active == True
        )
    ).offset(skip).limit(limit).all()

def batch_create_spreads(db: Session, spreads_data: List[SpreadTypeCreate]) -> List[SpreadType]:
    """批量创建牌阵类型"""
    db_spreads = []
    for spread_data in spreads_data:
        # 处理positions数据
        positions_data = [pos.model_dump() for pos in spread_data.positions]
        spread_dict = spread_data.model_dump()
        spread_dict['positions'] = positions_data
        
        db_spread = SpreadType(**spread_dict)
        db.add(db_spread)
        db_spreads.append(db_spread)
    
    db.commit()
    for db_spread in db_spreads:
        db.refresh(db_spread)
    
    return db_spreads 

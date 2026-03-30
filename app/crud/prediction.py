from typing import List, Optional
from datetime import datetime
from sqlalchemy.orm import Session, joinedload
from sqlalchemy import and_, or_, desc, asc, func, update
from app.models.record import Prediction, CardDraw, Interpretation, QuestionType, PredictionStatus
from app.models.user import User
from app.models.tarot_card import TarotCard
from app.models.spread import SpreadType
from app.schemas.prediction import (
    PredictionCreate, PredictionUpdate,
    CardDrawCreate, InterpretationCreate,
    PredictionStats
)

# ======= 预测记录相关 =======

def _apply_prediction_filters(
    query,
    user_id: int,
    status: Optional[PredictionStatus] = None,
    question_type: Optional[QuestionType] = None,
    favorites_only: bool = False,
    search_term: Optional[str] = None,
):
    query = query.filter(Prediction.user_id == user_id)

    if status is not None:
        query = query.filter(Prediction.status == status)

    if question_type is not None:
        query = query.filter(Prediction.question_type == question_type)

    if favorites_only:
        query = query.filter(Prediction.is_favorite.is_(True))

    if search_term:
        search_pattern = f"%{search_term}%"
        query = query.filter(
            or_(
                Prediction.question.ilike(search_pattern),
                Prediction.user_notes.ilike(search_pattern),
            )
        )

    return query


def _apply_prediction_sort(query, sort_by: str = "created_at", sort_order: str = "desc"):
    sort_field_map = {
        "created_at": Prediction.created_at,
        "completed_at": Prediction.completed_at,
        "status": Prediction.status,
        "question_type": Prediction.question_type,
    }
    sort_column = sort_field_map.get(sort_by, Prediction.created_at)
    sort_direction = asc if sort_order == "asc" else desc
    return query.order_by(sort_direction(sort_column), desc(Prediction.created_at))

def get_prediction_by_id(db: Session, prediction_id: int) -> Optional[Prediction]:
    """根据ID获取预测记录"""
    return db.query(Prediction).filter(Prediction.id == prediction_id).first()

def get_prediction_with_details(db: Session, prediction_id: int) -> Optional[Prediction]:
    """获取包含所有关联数据的预测记录"""
    return db.query(Prediction).options(
        joinedload(Prediction.user),
        joinedload(Prediction.spread_type),
        joinedload(Prediction.card_draws).joinedload(CardDraw.tarot_card),
        joinedload(Prediction.interpretation)
    ).filter(Prediction.id == prediction_id).first()

def get_user_predictions(db: Session, user_id: int, skip: int = 0, limit: int = 100) -> List[Prediction]:
    """获取用户的预测记录"""
    return get_filtered_user_predictions(db, user_id=user_id, skip=skip, limit=limit)

def get_filtered_user_predictions(
    db: Session,
    user_id: int,
    skip: int = 0,
    limit: int = 100,
    status: Optional[PredictionStatus] = None,
    question_type: Optional[QuestionType] = None,
    favorites_only: bool = False,
    search_term: Optional[str] = None,
    sort_by: str = "created_at",
    sort_order: str = "desc",
) -> List[Prediction]:
    """按搜索、筛选、排序条件获取用户预测记录"""
    query = db.query(Prediction)
    query = _apply_prediction_filters(
        query,
        user_id=user_id,
        status=status,
        question_type=question_type,
        favorites_only=favorites_only,
        search_term=search_term,
    )
    query = _apply_prediction_sort(query, sort_by=sort_by, sort_order=sort_order)
    return query.offset(skip).limit(limit).all()

def get_user_predictions_by_status(db: Session, user_id: int, status: PredictionStatus, skip: int = 0, limit: int = 100) -> List[Prediction]:
    """根据状态获取用户预测记录"""
    return get_filtered_user_predictions(
        db,
        user_id=user_id,
        status=status,
        skip=skip,
        limit=limit,
    )

def get_user_predictions_by_question_type(db: Session, user_id: int, question_type: QuestionType, skip: int = 0, limit: int = 100) -> List[Prediction]:
    """根据问题类型获取用户预测记录"""
    return get_filtered_user_predictions(
        db,
        user_id=user_id,
        question_type=question_type,
        skip=skip,
        limit=limit,
    )

def get_user_favorite_predictions(db: Session, user_id: int, skip: int = 0, limit: int = 100) -> List[Prediction]:
    """获取用户收藏的预测记录"""
    return get_filtered_user_predictions(
        db,
        user_id=user_id,
        favorites_only=True,
        skip=skip,
        limit=limit,
    )


def get_recent_prediction_overview(db: Session, user_id: int, limit: int = 4) -> List[Prediction]:
    """获取仪表盘最近记录所需的聚合数据"""
    return (
        db.query(Prediction)
        .options(
            joinedload(Prediction.spread_type),
            joinedload(Prediction.interpretation),
        )
        .filter(Prediction.user_id == user_id)
        .order_by(desc(Prediction.created_at))
        .limit(limit)
        .all()
    )

def create_prediction(db: Session, user_id: int, prediction_create: PredictionCreate) -> Prediction:
    """创建预测记录"""
    db_prediction = Prediction(
        user_id=user_id,
        **prediction_create.model_dump()
    )
    db.add(db_prediction)
    db.commit()
    db.refresh(db_prediction)
    return db_prediction


def create_prediction_with_stats(db: Session, user_id: int, prediction_create: PredictionCreate) -> Prediction:
    """Create prediction and update related counters in one transaction."""
    db_prediction = Prediction(
        user_id=user_id,
        **prediction_create.model_dump()
    )
    db.add(db_prediction)
    db.flush()

    # Use SQL-level increments to avoid lost updates under concurrent requests.
    db.execute(
        update(User)
        .where(User.id == user_id)
        .values(prediction_count=func.coalesce(User.prediction_count, 0) + 1)
    )
    db.execute(
        update(SpreadType)
        .where(SpreadType.id == prediction_create.spread_type_id)
        .values(usage_count=func.coalesce(SpreadType.usage_count, 0) + 1)
    )

    db.commit()
    db.refresh(db_prediction)
    return db_prediction

def update_prediction(db: Session, db_prediction: Prediction, prediction_update: PredictionUpdate) -> Prediction:
    """更新预测记录"""
    update_data = prediction_update.model_dump(exclude_unset=True)
    for field, value in update_data.items():
        setattr(db_prediction, field, value)
    
    db.commit()
    db.refresh(db_prediction)
    return db_prediction

def update_prediction_status(db: Session, prediction_id: int, status: PredictionStatus) -> bool:
    """更新预测状态"""
    db_prediction = get_prediction_by_id(db, prediction_id)
    if db_prediction:
        db_prediction.status = status
        if status == PredictionStatus.COMPLETED:
            db_prediction.completed_at = datetime.utcnow()
        db.commit()
        return True
    return False

def delete_prediction(db: Session, prediction_id: int) -> bool:
    """删除预测记录"""
    db_prediction = get_prediction_by_id(db, prediction_id)
    if db_prediction:
        db.delete(db_prediction)
        db.commit()
        return True
    return False

# ======= 抽牌记录相关 =======

def get_card_draw_by_id(db: Session, card_draw_id: int) -> Optional[CardDraw]:
    """根据ID获取抽牌记录"""
    return db.query(CardDraw).filter(CardDraw.id == card_draw_id).first()

def get_prediction_card_draws(db: Session, prediction_id: int) -> List[CardDraw]:
    """获取预测的所有抽牌记录"""
    return db.query(CardDraw).options(
        joinedload(CardDraw.tarot_card)
    ).filter(CardDraw.prediction_id == prediction_id).order_by(CardDraw.position).all()

def create_card_draw(db: Session, prediction_id: int, card_draw_create: CardDrawCreate) -> CardDraw:
    """创建抽牌记录"""
    db_card_draw = CardDraw(
        prediction_id=prediction_id,
        **card_draw_create.model_dump()
    )
    db.add(db_card_draw)
    db.commit()
    db.refresh(db_card_draw)
    return db_card_draw

def batch_create_card_draws(db: Session, prediction_id: int, card_draws_data: List[CardDrawCreate]) -> List[CardDraw]:
    """批量创建抽牌记录"""
    db_card_draws = []
    for card_draw_data in card_draws_data:
        db_card_draw = CardDraw(
            prediction_id=prediction_id,
            **card_draw_data.model_dump()
        )
        db.add(db_card_draw)
        db_card_draws.append(db_card_draw)
    
    db.commit()
    for db_card_draw in db_card_draws:
        db.refresh(db_card_draw)
    
    return db_card_draws

def update_card_draw_reversed_status(db: Session, card_draw_id: int, is_reversed: bool) -> bool:
    """更新抽牌的正逆位状态"""
    db_card_draw = get_card_draw_by_id(db, card_draw_id)
    if db_card_draw:
        db_card_draw.is_reversed = is_reversed
        db.commit()
        return True
    return False

def delete_card_draw(db: Session, card_draw_id: int) -> bool:
    """删除抽牌记录"""
    db_card_draw = get_card_draw_by_id(db, card_draw_id)
    if db_card_draw:
        db.delete(db_card_draw)
        db.commit()
        return True
    return False

# ======= 解读结果相关 =======

def get_interpretation_by_id(db: Session, interpretation_id: int) -> Optional[Interpretation]:
    """根据ID获取解读结果"""
    return db.query(Interpretation).filter(Interpretation.id == interpretation_id).first()

def get_prediction_interpretation(db: Session, prediction_id: int) -> Optional[Interpretation]:
    """获取预测的解读结果"""
    return db.query(Interpretation).filter(Interpretation.prediction_id == prediction_id).first()

def create_interpretation(db: Session, prediction_id: int, interpretation_create: InterpretationCreate) -> Interpretation:
    """创建解读结果"""
    db_interpretation = Interpretation(
        prediction_id=prediction_id,
        **interpretation_create.model_dump()
    )
    db.add(db_interpretation)
    db.commit()
    db.refresh(db_interpretation)
    return db_interpretation

def update_interpretation(db: Session, db_interpretation: Interpretation, interpretation_update: InterpretationCreate) -> Interpretation:
    """更新解读结果"""
    update_data = interpretation_update.model_dump(exclude_unset=True)
    for field, value in update_data.items():
        setattr(db_interpretation, field, value)
    
    db.commit()
    db.refresh(db_interpretation)
    return db_interpretation

def delete_interpretation(db: Session, interpretation_id: int) -> bool:
    """删除解读结果"""
    db_interpretation = get_interpretation_by_id(db, interpretation_id)
    if db_interpretation:
        db.delete(db_interpretation)
        db.commit()
        return True
    return False

# ======= 统计和查询功能 =======

def get_user_prediction_stats(db: Session, user_id: int) -> PredictionStats:
    """获取用户预测统计"""
    # 总预测数
    total_predictions = db.query(Prediction).filter(Prediction.user_id == user_id).count()
    
    # 已完成预测数
    completed_predictions = db.query(Prediction).filter(
        and_(
            Prediction.user_id == user_id,
            Prediction.status == PredictionStatus.COMPLETED
        )
    ).count()
    
    # 收藏预测数
    favorite_predictions = db.query(Prediction).filter(
        and_(
            Prediction.user_id == user_id,
            Prediction.is_favorite == True
        )
    ).count()
    
    # 最常用的问题类型
    most_used_type_result = db.query(
        Prediction.question_type,
        func.count(Prediction.question_type).label('count')
    ).filter(
        Prediction.user_id == user_id
    ).group_by(Prediction.question_type).order_by(desc('count')).first()
    
    most_used_question_type = most_used_type_result[0] if most_used_type_result else None
    
    # 平均评分
    avg_rating_result = db.query(
        func.avg(Prediction.user_rating)
    ).filter(
        and_(
            Prediction.user_id == user_id,
            Prediction.user_rating.isnot(None)
        )
    ).scalar()
    
    average_rating = float(avg_rating_result) if avg_rating_result else None
    
    return PredictionStats(
        total_predictions=total_predictions,
        completed_predictions=completed_predictions,
        favorite_predictions=favorite_predictions,
        most_used_question_type=most_used_question_type,
        average_rating=average_rating
    )

def search_user_predictions(db: Session, user_id: int, search_term: str, skip: int = 0, limit: int = 100) -> List[Prediction]:
    """搜索用户的预测记录"""
    return get_filtered_user_predictions(
        db,
        user_id=user_id,
        search_term=search_term,
        skip=skip,
        limit=limit,
    )

def get_recent_predictions(db: Session, user_id: int, days: int = 7, limit: int = 10) -> List[Prediction]:
    """获取用户最近的预测记录"""
    from datetime import datetime, timedelta
    start_date = datetime.utcnow() - timedelta(days=days)
    
    return db.query(Prediction).filter(
        and_(
            Prediction.user_id == user_id,
            Prediction.created_at >= start_date
        )
    ).order_by(desc(Prediction.created_at)).limit(limit).all()

def validate_prediction_ownership(db: Session, prediction_id: int, user_id: int) -> bool:
    """验证预测记录是否属于指定用户"""
    return db.query(Prediction).filter(
        and_(
            Prediction.id == prediction_id,
            Prediction.user_id == user_id
        )
    ).first() is not None

def get_predictions_by_spread_type(db: Session, spread_type_id: int, skip: int = 0, limit: int = 100) -> List[Prediction]:
    """根据牌阵类型获取预测记录"""
    return db.query(Prediction).filter(
        Prediction.spread_type_id == spread_type_id
    ).order_by(desc(Prediction.created_at)).offset(skip).limit(limit).all()

def get_total_user_predictions_count(db: Session, user_id: int) -> int:
    """获取用户预测总数"""
    return db.query(Prediction).filter(Prediction.user_id == user_id).count()

def get_total_predictions_count(db: Session) -> int:
    """获取预测总数"""
    return db.query(Prediction).count()

def increment_user_prediction_count(db: Session, user_id: int) -> bool:
    """增加用户预测次数"""
    result = db.execute(
        update(User)
        .where(User.id == user_id)
        .values(prediction_count=func.coalesce(User.prediction_count, 0) + 1)
    )
    if (result.rowcount or 0) <= 0:
        return False
    db.commit()
    return True 

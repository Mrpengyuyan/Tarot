from typing import List, Optional
from datetime import date, datetime, timedelta, timezone
from sqlalchemy.orm import Session, joinedload
from sqlalchemy import and_, or_, case, desc, asc, func, select, update
from app.models.record import Prediction, CardDraw, Interpretation, InterpretationRunState, QuestionType, PredictionStatus
from app.models.user import User
from app.models.tarot_card import TarotCard
from app.models.spread import SpreadType
from app.schemas.prediction import (
    PredictionCreate, PredictionUpdate,
    CardDrawCreate, InterpretationCreate, InterpretationUpdate,
    PredictionStats
)

# ======= 预测记录相关 =======


class DailyReadingLimitExceeded(Exception):
    pass


class InterpretationClaimLost(Exception):
    pass

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


def count_predictions_created_since(db: Session, user_id: int, since: datetime) -> int:
    """Count reading attempts for a user from the supplied UTC boundary onward."""
    count = (
        db.query(func.count(Prediction.id))
        .filter(
            Prediction.user_id == user_id,
            Prediction.created_at >= since,
        )
        .scalar()
    )
    return int(count or 0)

def create_prediction(db: Session, user_id: int, prediction_create: PredictionCreate) -> Prediction:
    """创建预测记录"""
    prediction_data = prediction_create.model_dump()
    prediction_data["question_type"] = QuestionType(prediction_create.question_type.value)
    db_prediction = Prediction(
        user_id=user_id,
        **prediction_data
    )
    db.add(db_prediction)
    db.commit()
    db.refresh(db_prediction)
    return db_prediction


def create_prediction_with_stats(
    db: Session,
    user_id: int,
    prediction_create: PredictionCreate,
    *,
    daily_limit: int = 0,
    quota_day: date | None = None,
) -> Prediction:
    """Create prediction and update related counters in one transaction."""
    if daily_limit > 0:
        quota_day = quota_day or datetime.now(timezone.utc).date()
        claimed = db.execute(
            update(User)
            .where(User.id == user_id)
            .where(
                or_(
                    User.reading_quota_day.is_(None),
                    User.reading_quota_day != quota_day,
                    User.reading_quota_count < daily_limit,
                )
            )
            .values(
                reading_quota_day=quota_day,
                reading_quota_count=case(
                    (User.reading_quota_day == quota_day, User.reading_quota_count + 1),
                    else_=1,
                ),
            )
            .execution_options(synchronize_session=False)
        )
        if claimed.rowcount != 1:
            db.rollback()
            raise DailyReadingLimitExceeded

    prediction_data = prediction_create.model_dump()
    prediction_data["question_type"] = QuestionType(prediction_create.question_type.value)
    db_prediction = Prediction(
        user_id=user_id,
        **prediction_data
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
        _set_prediction_status(db_prediction, status)
        db.commit()
        return True
    return False


def _set_prediction_status(prediction: Prediction, status: PredictionStatus) -> None:
    prediction.status = status
    if status == PredictionStatus.COMPLETED:
        prediction.completed_at = datetime.now(timezone.utc)

def claim_interpretation_generation(
    db: Session,
    prediction_id: int,
    now: datetime,
    stale_before: datetime,
    max_attempts: int,
) -> bool:
    """原子地抢占解读生成权。

    只有在记录还没有解读、生成次数未达上限，并且不处于有效的生成中
    （状态不是 PROCESSING，或者 PROCESSING 已经早于 stale_before）时才会成功。
    并发调用时最多只有一个返回 True。
    """
    has_interpretation = select(Interpretation.id).where(Interpretation.prediction_id == prediction_id).exists()
    statement = (
        update(Prediction)
        .where(Prediction.id == prediction_id)
        .where(Prediction.interpretation_attempts < max_attempts)
        .where(Prediction.ai_requests_started == 0)
        .where(Prediction.ai_run_state.in_((
            InterpretationRunState.NOT_STARTED.value,
            InterpretationRunState.SCHEDULED.value,
        )))
        .where(~has_interpretation)
        .where(
            or_(
                Prediction.status != PredictionStatus.PROCESSING,
                Prediction.interpretation_started_at.is_(None),
                Prediction.interpretation_started_at < stale_before,
            )
        )
        .values(
            status=PredictionStatus.PROCESSING,
            ai_run_state=InterpretationRunState.SCHEDULED.value,
            ai_last_error=None,
            interpretation_started_at=now,
            interpretation_attempts=Prediction.interpretation_attempts + 1,
        )
        .execution_options(synchronize_session=False)
    )
    result = db.execute(statement)
    db.commit()
    return result.rowcount == 1


def claim_manual_interpretation_retry(db: Session, prediction_id: int, now: datetime) -> bool:
    has_interpretation = select(Interpretation.id).where(Interpretation.prediction_id == prediction_id).exists()
    claimed = db.execute(
        update(Prediction)
        .where(Prediction.id == prediction_id)
        .where(Prediction.ai_run_state.in_((
            InterpretationRunState.RETRY_REQUIRED.value,
            InterpretationRunState.BUDGET_BLOCKED.value,
        )))
        .where(Prediction.ai_requests_started < 2)
        .where(~has_interpretation)
        .values(
            status=PredictionStatus.PROCESSING,
            ai_run_state=InterpretationRunState.SCHEDULED.value,
            ai_last_error=None,
            ai_budget_retry_at=None,
            interpretation_started_at=now,
            interpretation_attempts=Prediction.interpretation_attempts + 1,
        )
        .execution_options(synchronize_session=False)
    )
    db.commit()
    return claimed.rowcount == 1


def mark_interpretation_request_sending(db: Session, prediction_id: int, started_at: datetime) -> bool:
    """Consume one possible-send slot durably before the outbound HTTP POST."""
    has_interpretation = select(Interpretation.id).where(Interpretation.prediction_id == prediction_id).exists()
    marked = db.execute(
        update(Prediction)
        .where(Prediction.id == prediction_id)
        .where(Prediction.status == PredictionStatus.PROCESSING)
        .where(Prediction.interpretation_started_at == started_at)
        .where(Prediction.ai_run_state == InterpretationRunState.SCHEDULED.value)
        .where(Prediction.ai_requests_started < 2)
        .where(~has_interpretation)
        .values(
            ai_run_state=InterpretationRunState.SENDING.value,
            ai_requests_started=Prediction.ai_requests_started + 1,
            ai_request_started_at=datetime.now(timezone.utc),
        )
        .execution_options(synchronize_session=False)
    )
    db.commit()
    return marked.rowcount == 1


def expire_stale_interpretation_run(
    db: Session, prediction_id: int, now: datetime, *, sending_seconds: int = 120,
    scheduled_seconds: int = 300,
) -> bool:
    current = db.execute(
        select(Prediction.ai_run_state, Prediction.ai_request_started_at, Prediction.interpretation_started_at)
        .where(Prediction.id == prediction_id)
    ).first()
    if current is None:
        return False
    state, sent_at, scheduled_at = current
    if sent_at is not None and sent_at.tzinfo is None:
        sent_at = sent_at.replace(tzinfo=timezone.utc)
    if scheduled_at is not None and scheduled_at.tzinfo is None:
        scheduled_at = scheduled_at.replace(tzinfo=timezone.utc)
    if not (
        state == InterpretationRunState.SENDING.value
        and sent_at is not None and sent_at < now - timedelta(seconds=sending_seconds)
        or state == InterpretationRunState.SCHEDULED.value
        and scheduled_at is not None and scheduled_at < now - timedelta(seconds=scheduled_seconds)
    ):
        return False
    has_interpretation = select(Interpretation.id).where(Interpretation.prediction_id == prediction_id).exists()
    stale_sending = and_(
        Prediction.ai_run_state == InterpretationRunState.SENDING.value,
        Prediction.ai_request_started_at < now - timedelta(seconds=sending_seconds),
    )
    stale_scheduled = and_(
        Prediction.ai_run_state == InterpretationRunState.SCHEDULED.value,
        Prediction.interpretation_started_at < now - timedelta(seconds=scheduled_seconds),
    )
    expired = db.execute(
        update(Prediction)
        .where(Prediction.id == prediction_id)
        .where(or_(stale_sending, stale_scheduled))
        .where(~has_interpretation)
        .values(
            status=PredictionStatus.FAILED,
            ai_run_state=case(
                (Prediction.ai_requests_started >= 2, InterpretationRunState.EXHAUSTED.value),
                else_=InterpretationRunState.RETRY_REQUIRED.value,
            ),
            ai_last_error="worker_timeout",
        )
        .execution_options(synchronize_session=False)
    )
    db.commit()
    return expired.rowcount == 1


def release_interpretation_generation(
    db: Session, prediction_id: int, started_at: datetime | None,
    *, budget_retry_at: datetime | None = None,
) -> bool:
    """Return an unused claim when budget admission refused before any paid request."""
    if started_at is None:
        return False
    released = db.execute(
        update(Prediction)
        .where(Prediction.id == prediction_id)
        .where(Prediction.status == PredictionStatus.PROCESSING)
        .where(Prediction.ai_run_state == InterpretationRunState.SCHEDULED.value)
        .where(Prediction.interpretation_started_at == started_at)
        .where(Prediction.interpretation_attempts > 0)
        .values(
            status=PredictionStatus.FAILED,
            ai_run_state=InterpretationRunState.BUDGET_BLOCKED.value,
            ai_last_error="budget",
            ai_budget_retry_at=budget_retry_at,
            interpretation_started_at=None,
            interpretation_attempts=Prediction.interpretation_attempts - 1,
        )
        .execution_options(synchronize_session=False)
    )
    db.commit()
    return released.rowcount == 1


def fail_interpretation_generation(
    db: Session, prediction_id: int, started_at: datetime | None, *, error: str = "upstream",
) -> bool:
    """Only the active claim may mark a reading failed."""
    if started_at is None:
        return False
    has_interpretation = select(Interpretation.id).where(Interpretation.prediction_id == prediction_id).exists()
    failed = db.execute(
        update(Prediction)
        .where(Prediction.id == prediction_id)
        .where(Prediction.status == PredictionStatus.PROCESSING)
        .where(Prediction.interpretation_started_at == started_at)
        .where(~has_interpretation)
        .values(
            status=PredictionStatus.FAILED,
            ai_run_state=case(
                (Prediction.ai_requests_started >= 2, InterpretationRunState.EXHAUSTED.value),
                else_=InterpretationRunState.RETRY_REQUIRED.value,
            ),
            ai_last_error=error,
        )
        .execution_options(synchronize_session=False)
    )
    db.commit()
    return failed.rowcount == 1


def interpretation_claim_is_current(db: Session, prediction_id: int, started_at: datetime | None) -> bool:
    """Read the claim directly so a cached ORM prediction cannot authorize an AI call."""
    if started_at is None:
        return False
    has_interpretation = select(Interpretation.id).where(Interpretation.prediction_id == prediction_id).exists()
    return db.execute(
        select(Prediction.id)
        .where(Prediction.id == prediction_id)
        .where(Prediction.status == PredictionStatus.PROCESSING)
        .where(Prediction.interpretation_started_at == started_at)
        .where(~has_interpretation)
    ).first() is not None

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

def batch_create_card_draws(
    db: Session,
    prediction_id: int,
    card_draws_data: List[CardDrawCreate],
    *,
    prediction_status: PredictionStatus | None = None,
) -> List[CardDraw]:
    """批量创建抽牌记录"""
    db_card_draws = []
    for card_draw_data in card_draws_data:
        db_card_draw = CardDraw(
            prediction_id=prediction_id,
            **card_draw_data.model_dump()
        )
        db.add(db_card_draw)
        db_card_draws.append(db_card_draw)

    if prediction_status is not None:
        prediction = get_prediction_by_id(db, prediction_id)
        if prediction is None:
            raise ValueError("Prediction not found")
        _set_prediction_status(prediction, prediction_status)
    
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

def create_interpretation(
    db: Session,
    prediction_id: int,
    interpretation_create: InterpretationCreate,
    *,
    prediction_status: PredictionStatus | None = None,
    expected_started_at: datetime | None = None,
) -> Interpretation:
    """创建解读结果"""
    db_interpretation = Interpretation(
        prediction_id=prediction_id,
        **interpretation_create.model_dump()
    )
    db.add(db_interpretation)
    if expected_started_at is not None:
        if prediction_status != PredictionStatus.COMPLETED:
            raise ValueError("A claimed interpretation must complete the prediction")
        updated = db.execute(
            update(Prediction)
            .where(Prediction.id == prediction_id)
            .where(Prediction.status == PredictionStatus.PROCESSING)
            .where(Prediction.interpretation_started_at == expected_started_at)
            .where(Prediction.ai_run_state.in_((
                InterpretationRunState.SCHEDULED.value,
                InterpretationRunState.SENDING.value,
            )))
            .values(
                status=PredictionStatus.COMPLETED,
                ai_run_state=InterpretationRunState.COMPLETED.value,
                ai_last_error=None,
                completed_at=datetime.now(timezone.utc),
            )
            .execution_options(synchronize_session=False)
        )
        if updated.rowcount != 1:
            db.rollback()
            raise InterpretationClaimLost
    elif prediction_status is not None:
        prediction = get_prediction_by_id(db, prediction_id)
        if prediction is None:
            raise ValueError("Prediction not found")
        _set_prediction_status(prediction, prediction_status)
        if prediction_status == PredictionStatus.COMPLETED:
            prediction.ai_run_state = InterpretationRunState.COMPLETED.value
    db.commit()
    db.refresh(db_interpretation)
    return db_interpretation

def update_interpretation(db: Session, db_interpretation: Interpretation, interpretation_update: InterpretationUpdate) -> Interpretation:
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
    start_date = datetime.now(timezone.utc) - timedelta(days=days)
    
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

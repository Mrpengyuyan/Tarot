from typing import List, Optional
from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session

from app.db.session import get_db
from app.api.deps import get_current_active_user
from app.crud import spread as spread_crud
from app.schemas.spread import (
    SpreadType, SpreadTypeCreate, SpreadTypeUpdate, SpreadTypeSimple,
    SpreadSuitability
)
from app.schemas.user import User

router = APIRouter()

@router.get("/", response_model=List[SpreadTypeSimple], summary="获取牌阵列表")
def get_spreads(
    skip: int = Query(0, ge=0, description="跳过的数量"),
    limit: int = Query(100, ge=1, le=500, description="返回的数量"),
    difficulty: Optional[int] = Query(None, ge=1, le=5, description="难度等级筛选"),
    card_count: Optional[int] = Query(None, ge=1, le=78, description="牌数筛选"),
    question_type: Optional[str] = Query(None, description="问题类型筛选"),
    beginner_friendly: Optional[bool] = Query(None, description="是否适合初学者"),
    search: Optional[str] = Query(None, description="搜索关键词"),
    active_only: bool = Query(True, description="只显示活跃的牌阵"),
    db: Session = Depends(get_db)
):
    """
    获取牌阵列表
    - 支持分页
    - 支持多种筛选条件
    - 支持关键词搜索
    """
    if search:
        spreads = spread_crud.search_spreads(db, search_term=search, skip=skip, limit=limit)
    elif beginner_friendly is not None and beginner_friendly:
        spreads = spread_crud.get_beginner_friendly_spreads(db, skip=skip, limit=limit)
    elif question_type:
        spreads = spread_crud.get_spreads_for_question_type(db, question_type=question_type, skip=skip, limit=limit)
    elif difficulty:
        spreads = spread_crud.get_spreads_by_difficulty(db, difficulty_level=difficulty, skip=skip, limit=limit)
    elif card_count:
        spreads = spread_crud.get_spreads_by_card_count(db, card_count=card_count, skip=skip, limit=limit)
    else:
        spreads = spread_crud.get_spreads(db, skip=skip, limit=limit, active_only=active_only)
    
    return spreads

@router.get("/popular", response_model=List[SpreadTypeSimple], summary="获取热门牌阵")
def get_popular_spreads(
    limit: int = Query(10, ge=1, le=50, description="返回数量"),
    db: Session = Depends(get_db)
):
    """获取最受欢迎的牌阵（按使用次数排序）"""
    return spread_crud.get_popular_spreads(db, limit=limit)

@router.get("/beginner", response_model=List[SpreadTypeSimple], summary="获取初学者牌阵")
def get_beginner_spreads(
    skip: int = Query(0, ge=0),
    limit: int = Query(20, ge=1, le=100),
    db: Session = Depends(get_db)
):
    """获取适合初学者的牌阵"""
    return spread_crud.get_beginner_friendly_spreads(db, skip=skip, limit=limit)

@router.get("/by-question-type/{question_type}", response_model=List[SpreadTypeSimple], summary="根据问题类型获取牌阵")
def get_spreads_by_question_type(
    question_type: str,
    skip: int = Query(0, ge=0),
    limit: int = Query(50, ge=1, le=100),
    db: Session = Depends(get_db)
):
    """
    根据问题类型获取适合的牌阵
    - question_type: love, career, finance, health, general
    """
    valid_types = ["love", "career", "finance", "health", "general"]
    if question_type not in valid_types:
        raise HTTPException(status_code=400, detail=f"问题类型必须是: {', '.join(valid_types)}")
    
    return spread_crud.get_spreads_for_question_type(db, question_type=question_type, skip=skip, limit=limit)

@router.get("/count", summary="获取牌阵统计")
def get_spreads_count(db: Session = Depends(get_db)):
    """获取牌阵总数和分类统计"""
    total_count = spread_crud.get_total_spreads_count(db, active_only=True)
    beginner_count = len(spread_crud.get_beginner_friendly_spreads(db, limit=1000))
    
    # 按难度分组统计
    difficulty_stats = {}
    for level in range(1, 6):
        count = len(spread_crud.get_spreads_by_difficulty(db, difficulty_level=level, limit=1000))
        difficulty_stats[f"level_{level}"] = count
    
    return {
        "total_spreads": total_count,
        "beginner_friendly": beginner_count,
        "difficulty_distribution": difficulty_stats
    }

@router.get("/search", response_model=List[SpreadTypeSimple], summary="搜索牌阵")
def search_spreads(
    q: str = Query(..., description="搜索关键词"),
    skip: int = Query(0, ge=0),
    limit: int = Query(50, ge=1, le=100),
    db: Session = Depends(get_db)
):
    """根据关键词搜索牌阵"""
    return spread_crud.search_spreads(db, search_term=q, skip=skip, limit=limit)

@router.get("/{spread_id}", response_model=SpreadType, summary="获取牌阵详情")
def get_spread_detail(
    spread_id: int,
    db: Session = Depends(get_db)
):
    """根据ID获取牌阵的详细信息"""
    spread = spread_crud.get_spread_by_id(db, spread_id=spread_id)
    if not spread:
        raise HTTPException(status_code=404, detail="牌阵不存在")
    return spread

@router.get("/{spread_id}/suitability", response_model=SpreadSuitability, summary="获取牌阵适用性")
def get_spread_suitability(
    spread_id: int,
    db: Session = Depends(get_db)
):
    """获取牌阵的适用性信息"""
    spread = spread_crud.get_spread_by_id(db, spread_id=spread_id)
    if not spread:
        raise HTTPException(status_code=404, detail="牌阵不存在")
    
    return SpreadSuitability(
        id=spread.id,
        name=spread.name,
        suitable_for_love=spread.suitable_for_love,
        suitable_for_career=spread.suitable_for_career,
        suitable_for_finance=spread.suitable_for_finance,
        suitable_for_health=spread.suitable_for_health,
        suitable_for_general=spread.suitable_for_general
    )

@router.post("/", response_model=SpreadType, summary="创建牌阵")
def create_spread(
    spread_create: SpreadTypeCreate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user)
):
    """
    创建新的牌阵类型
    - 需要超级用户权限
    """
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="需要管理员权限")
    
    # 检查是否已存在相同名称的牌阵
    existing_spread = spread_crud.get_spread_by_name(db, name=spread_create.name)
    if existing_spread:
        raise HTTPException(status_code=400, detail="相同名称的牌阵已存在")
    
    # 验证牌位数量与positions列表长度一致
    if len(spread_create.positions) != spread_create.card_count:
        raise HTTPException(status_code=400, detail="牌位定义数量与所需牌数不匹配")
    
    return spread_crud.create_spread(db=db, spread_create=spread_create)

@router.put("/{spread_id}", response_model=SpreadType, summary="更新牌阵")
def update_spread(
    spread_id: int,
    spread_update: SpreadTypeUpdate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user)
):
    """
    更新牌阵信息
    - 需要超级用户权限
    """
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="需要管理员权限")
    
    spread = spread_crud.get_spread_by_id(db, spread_id=spread_id)
    if not spread:
        raise HTTPException(status_code=404, detail="牌阵不存在")
    
    # 如果更新了牌位定义，验证数量一致性
    if spread_update.positions and spread_update.card_count:
        if len(spread_update.positions) != spread_update.card_count:
            raise HTTPException(status_code=400, detail="牌位定义数量与所需牌数不匹配")
    
    return spread_crud.update_spread(db=db, db_spread=spread, spread_update=spread_update)

@router.delete("/{spread_id}", summary="删除牌阵")
def delete_spread(
    spread_id: int,
    hard_delete: bool = Query(False, description="是否硬删除"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user)
):
    """
    删除牌阵
    - 默认软删除（设置为不活跃）
    - 需要超级用户权限
    """
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="需要管理员权限")
    
    if hard_delete:
        success = spread_crud.hard_delete_spread(db, spread_id=spread_id)
        message = "牌阵彻底删除成功"
    else:
        success = spread_crud.delete_spread(db, spread_id=spread_id)
        message = "牌阵已设置为不活跃"
    
    if not success:
        raise HTTPException(status_code=404, detail="牌阵不存在")
    
    return {"message": message}

@router.post("/{spread_id}/use", summary="记录牌阵使用")
def use_spread(
    spread_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user)
):
    """
    记录牌阵的使用
    - 增加使用次数计数
    - 需要用户登录
    """
    if not spread_crud.validate_spread_exists(db, spread_id=spread_id):
        raise HTTPException(status_code=404, detail="牌阵不存在或已停用")
    
    success = spread_crud.increment_spread_usage(db, spread_id=spread_id)
    if not success:
        raise HTTPException(status_code=500, detail="记录使用失败")
    
    return {"message": "使用记录成功"}

@router.post("/batch", response_model=List[SpreadType], summary="批量创建牌阵")
def batch_create_spreads(
    spreads_data: List[SpreadTypeCreate],
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user)
):
    """
    批量创建牌阵
    - 需要超级用户权限
    - 用于初始化数据
    """
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="需要管理员权限")
    
    if len(spreads_data) > 50:
        raise HTTPException(status_code=400, detail="单次最多创建50个牌阵")
    
    # 验证每个牌阵的有效性
    for spread_data in spreads_data:
        if len(spread_data.positions) != spread_data.card_count:
            raise HTTPException(
                status_code=400, 
                detail=f"牌阵 '{spread_data.name}' 的牌位定义数量与所需牌数不匹配"
            )
    
    return spread_crud.batch_create_spreads(db=db, spreads_data=spreads_data) 
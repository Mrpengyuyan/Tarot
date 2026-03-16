"""
塔罗牌和牌阵数据初始化脚本
将JSON文件中的数据导入到数据库中
"""
import json
import os
from pathlib import Path
from sqlalchemy.orm import Session
from app.db.session import SessionLocal
from app.models.tarot_card import TarotCard, CardType, Suit
from app.models.spread import SpreadType
from app.crud.card import get_card_by_number_and_type
from app.crud.spread import get_spread_by_name

# 获取项目根目录
PROJECT_ROOT = Path(__file__).parent.parent.parent
DATA_DIR = PROJECT_ROOT / "data"

def load_json_data(filename: str):
    """加载JSON数据文件"""
    file_path = DATA_DIR / filename
    if not file_path.exists():
        print(f"❌ 数据文件不存在: {file_path}")
        return None
    
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)
        print(f"✅ 成功加载数据文件: {filename}, 共 {len(data)} 条记录")
        return data
    except Exception as e:
        print(f"❌ 加载数据文件失败: {filename}, 错误: {e}")
        return None

def convert_card_type(type_str: str) -> CardType:
    """转换卡牌类型"""
    if type_str == "major":
        return CardType.MAJOR_ARCANA
    elif type_str == "minor":
        return CardType.MINOR_ARCANA
    else:
        raise ValueError(f"未知的卡牌类型: {type_str}")

def convert_suit(suit_str: str) -> Suit:
    """转换花色"""
    if suit_str is None:
        return None
    
    suit_mapping = {
        "wands": Suit.WANDS,
        "cups": Suit.CUPS,
        "swords": Suit.SWORDS,
        "pentacles": Suit.PENTACLES
    }
    
    if suit_str.lower() in suit_mapping:
        return suit_mapping[suit_str.lower()]
    else:
        raise ValueError(f"未知的花色: {suit_str}")

def init_tarot_cards(db: Session) -> int:
    """初始化塔罗牌数据"""
    print("\n🃏 开始初始化塔罗牌数据...")
    
    # 加载塔罗牌数据
    cards_data = load_json_data("tarotCards.json")
    if not cards_data:
        return 0
    
    created_count = 0
    updated_count = 0
    
    for card_data in cards_data:
        try:
            # 转换数据格式
            card_type = convert_card_type(card_data["type"])
            suit = convert_suit(card_data.get("suit"))
            
            # 检查是否已存在
            existing_card = get_card_by_number_and_type(
                db, 
                card_number=card_data["cardNumber"],
                card_type=card_type,
                suit=suit
            )
            
            # 准备数据
            card_params = {
                "name_en": card_data["nameEn"],
                "name_zh": card_data["nameZh"],
                "card_number": card_data["cardNumber"],
                "card_type": card_type,
                "suit": suit,
                "image_url": card_data.get("imageUrl"),
                "upright_meaning": card_data["uprightMeaning"],
                "upright_love": card_data.get("uprightLove"),
                "upright_career": card_data.get("uprightCareer"),
                "upright_finance": card_data.get("uprightFinance"),
                "upright_health": card_data.get("uprightHealth"),
                "reversed_meaning": card_data["reversedMeaning"],
                "reversed_love": card_data.get("reversedLove"),
                "reversed_career": card_data.get("reversedCareer"),
                "reversed_finance": card_data.get("reversedFinance"),
                "reversed_health": card_data.get("reversedHealth"),
                "description": card_data.get("description"),
                "keywords_upright": ",".join(card_data.get("keywordsUpright", [])),
                "keywords_reversed": ",".join(card_data.get("keywordsReversed", [])),
                "element": card_data.get("element"),
                "zodiac": card_data.get("zodiac"),
                "planet": card_data.get("planet")
            }
            
            if existing_card:
                # 更新现有卡牌
                for key, value in card_params.items():
                    setattr(existing_card, key, value)
                updated_count += 1
                print(f"  ✏️ 更新塔罗牌: {card_data['nameZh']} ({card_data['nameEn']})")
            else:
                # 创建新卡牌
                new_card = TarotCard(**card_params)
                db.add(new_card)
                created_count += 1
                print(f"  ➕ 创建塔罗牌: {card_data['nameZh']} ({card_data['nameEn']})")
                
        except Exception as e:
            print(f"  ❌ 处理塔罗牌数据失败: {card_data.get('nameZh', 'Unknown')}, 错误: {e}")
            continue
    
    # 提交事务
    try:
        db.commit()
        print(f"✅ 塔罗牌数据初始化完成! 创建: {created_count}, 更新: {updated_count}")
        return created_count + updated_count
    except Exception as e:
        print(f"❌ 提交塔罗牌数据失败: {e}")
        db.rollback()
        return 0

def init_spreads(db: Session) -> int:
    """初始化牌阵数据"""
    print("\n🔮 开始初始化牌阵数据...")
    
    # 加载牌阵数据
    spreads_data = load_json_data("spreads.json")
    if not spreads_data:
        return 0
    
    created_count = 0
    updated_count = 0
    
    for spread_data in spreads_data:
        try:
            # 检查是否已存在
            existing_spread = get_spread_by_name(db, name=spread_data["name"])
            
            # 准备数据
            spread_params = {
                "name": spread_data["name"],
                "name_en": spread_data.get("nameEn"),
                "description": spread_data["description"],
                "card_count": spread_data["cardCount"],
                "difficulty_level": spread_data.get("difficulty", 1),
                "positions": spread_data["positions"],  # JSON数据直接存储
                "layout_image_url": spread_data.get("layoutImageUrl"),
                "is_active": True,
                "is_beginner_friendly": spread_data.get("isBeginnerFriendly", False),
                "usage_count": spread_data.get("usageCount", 0)
            }
            
            # 处理适用性字段
            suitable_for = spread_data.get("suitableFor", [])
            spread_params.update({
                "suitable_for_love": "love" in suitable_for,
                "suitable_for_career": "career" in suitable_for,
                "suitable_for_finance": "finance" in suitable_for,
                "suitable_for_health": "health" in suitable_for,
                "suitable_for_general": "general" in suitable_for
            })
            
            if existing_spread:
                # 更新现有牌阵
                for key, value in spread_params.items():
                    setattr(existing_spread, key, value)
                updated_count += 1
                print(f"  ✏️ 更新牌阵: {spread_data['name']} ({spread_data['cardCount']}张牌)")
            else:
                # 创建新牌阵
                new_spread = SpreadType(**spread_params)
                db.add(new_spread)
                created_count += 1
                print(f"  ➕ 创建牌阵: {spread_data['name']} ({spread_data['cardCount']}张牌)")
                
        except Exception as e:
            print(f"  ❌ 处理牌阵数据失败: {spread_data.get('name', 'Unknown')}, 错误: {e}")
            continue
    
    # 提交事务
    try:
        db.commit()
        print(f"✅ 牌阵数据初始化完成! 创建: {created_count}, 更新: {updated_count}")
        return created_count + updated_count
    except Exception as e:
        print(f"❌ 提交牌阵数据失败: {e}")
        db.rollback()
        return 0

def main():
    """主函数"""
    print("🚀 开始初始化塔罗牌游戏数据...")
    print(f"📁 数据目录: {DATA_DIR}")
    
    # 检查数据目录
    if not DATA_DIR.exists():
        print(f"❌ 数据目录不存在: {DATA_DIR}")
        return
    
    # 创建数据库会话
    db = SessionLocal()
    
    try:
        # 初始化塔罗牌数据
        cards_count = init_tarot_cards(db)
        
        # 初始化牌阵数据
        spreads_count = init_spreads(db)
        
        print(f"\n🎉 数据初始化完成!")
        print(f"📊 统计信息:")
        print(f"  - 塔罗牌: {cards_count} 条记录")
        print(f"  - 牌阵: {spreads_count} 条记录")
        
        if cards_count > 0 or spreads_count > 0:
            print(f"\n💡 现在可以:")
            print(f"  1. 启动API服务: uvicorn app.main:app --reload")
            print(f"  2. 访问API文档: http://localhost:8000/docs")
            print(f"  3. 测试塔罗牌抽取功能")
        else:
            print(f"\n⚠️ 没有数据被处理，请检查数据文件格式")
            
    except Exception as e:
        print(f"❌ 数据初始化过程中出现错误: {e}")
        db.rollback()
    finally:
        db.close()

if __name__ == "__main__":
    main() 
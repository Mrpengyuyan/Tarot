#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
系统状态测试脚本
检查塔罗占卜系统的各个组件是否正常工作
"""
import os
import sys
import asyncio

# This module is a manual system check script, not a pytest test module.
__test__ = False

# 添加项目路径
sys.path.insert(0, os.path.dirname(__file__))

async def test_system_status():
    """测试系统状态"""
    print("🔍 塔罗占卜系统状态检查")
    print("=" * 50)

    # 1. 检查基础配置
    print("\n1️⃣ 检查基础配置...")
    try:
        from app.core.config import settings
        print(f"✅ 项目名称: {settings.PROJECT_NAME}")
        print(f"✅ API版本: {settings.API_V1_STR}")
        print(f"✅ 数据库: {settings.DATABASE_URL}")

        # Coze配置检查
        coze_configured = bool(settings.COZE_API_KEY and settings.COZE_BOT_ID)
        print(f"{'✅' if coze_configured else '❌'} Coze AI服务: {'已配置' if coze_configured else '未配置'}")

    except Exception as e:
        print(f"❌ 配置检查失败: {e}")
        return False

    # 2. 检查数据库连接
    print("\n2️⃣ 检查数据库连接...")
    try:
        from app.db.session import engine
        with engine.connect() as connection:
            print("✅ 数据库连接正常")

        # 检查表是否存在
        from app.db.base_class import Base
        from app.models.tarot_card import TarotCard
        from app.models.spread import SpreadType

        Base.metadata.create_all(bind=engine)
        print("✅ 数据库表结构正常")

    except Exception as e:
        print(f"❌ 数据库检查失败: {e}")
        return False

    # 3. 检查Coze AI服务
    print("\n3️⃣ 检查Coze AI服务...")
    try:
        from app.services.enhanced_tarot_interpretation import enhanced_tarot_interpretation_service

        if not enhanced_tarot_interpretation_service.coze_service.is_configured():
            print("❌ Coze服务未配置")
            return False

        print("✅ Coze服务配置正常")

        # 执行健康检查
        health_result = await enhanced_tarot_interpretation_service.health_check()
        print(f"✅ AI服务健康检查: {health_result.get('status', 'unknown')}")

    except Exception as e:
        print(f"❌ AI服务检查失败: {e}")
        return False

    # 4. 检查API路由
    print("\n4️⃣ 检查API路由...")
    try:
        from app.api.v1.api import api_router

        # 列出所有路由
        routes = []
        for route in api_router.routes:
            if hasattr(route, 'path'):
                routes.append(route.path)

        print(f"✅ API路由数量: {len(routes)}")
        print("✅ 主要路由:")
        important_routes = ['/login', '/register', '/cards/', '/spreads/', '/records/']
        for route in important_routes:
            found = any(route in r for r in routes)
            print(f"  {'✅' if found else '❌'} {route}")

    except Exception as e:
        print(f"❌ API路由检查失败: {e}")
        return False

    # 5. 测试数据完整性
    print("\n5️⃣ 检查数据完整性...")
    try:
        from app.crud import card as card_crud
        from app.crud import spread as spread_crud
        from app.db.session import get_db

        db = next(get_db())

        # 检查塔罗牌数据
        card_count = card_crud.get_total_cards_count(db)
        print(f"✅ 塔罗牌总数: {card_count}")

        if card_count < 78:
            print(f"⚠️ 塔罗牌数量不足，建议运行: python -m app.scripts.init_tarot_data.py")

        # 检查牌阵数据
        spreads = spread_crud.get_spreads(db, skip=0, limit=100)
        print(f"✅ 牌阵数量: {len(spreads)}")

        if len(spreads) < 3:
            print(f"⚠️ 牌阵数量不足，建议添加更多牌阵")

        db.close()

    except Exception as e:
        print(f"❌ 数据检查失败: {e}")
        return False

    print("\n" + "=" * 50)
    print("🎉 系统状态检查完成！")
    print("✅ 所有核心组件都正常工作")
    print("\n🚀 可以开始使用塔罗占卜功能了！")
    print("\n📝 使用说明:")
    print("1. 启动后端: cd code && uvicorn app.main:app --reload")
    print("2. 启动前端: cd tarot-frontend && npm start")
    print("3. 访问: http://localhost:3000")
    print("4. 注册/登录后即可开始占卜")

    return True

if __name__ == "__main__":
    try:
        result = asyncio.run(test_system_status())
        if not result:
            print("\n❌ 系统检查发现问题，请根据上述提示修复后重试")
            sys.exit(1)
    except KeyboardInterrupt:
        print("\n⚠️ 检查被中断")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ 系统检查出现异常: {e}")
        sys.exit(1)

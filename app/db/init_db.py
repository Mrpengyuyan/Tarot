"""
新版数据库初始化脚本
仅负责创建表结构，数据初始化请使用 app/scripts/init_demo_data.py
"""
from sqlalchemy import create_engine
from app.models import Base
from app.db.session import engine
from app.core.config import settings
import logging

# 配置日志
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

def create_tables():
    """创建所有数据表"""
    try:
        logger.info("开始创建数据表...")
        Base.metadata.create_all(bind=engine)
        logger.info("数据表创建成功！")
        return True
    except Exception as e:
        logger.error(f"创建数据表失败: {e}")
        return False

def drop_tables():
    """删除所有数据表（慎用！）"""
    try:
        logger.warning("开始删除所有数据表...")
        Base.metadata.drop_all(bind=engine)
        logger.warning("所有数据表已删除！")
        return True
    except Exception as e:
        logger.error(f"删除数据表失败: {e}")
        return False

def init_database(reset=False):
    """初始化数据库表结构
    Args:
        reset: 是否重置数据库（删除现有表）
    """
    if reset:
        logger.warning("重置模式：将删除现有表！")
        if not drop_tables():
            return False
    
    # 创建表
    if not create_tables():
        return False
        
    logger.info("数据库表结构初始化完成！")
    logger.info("💡 提示：运行 'python -m app.scripts.init_demo_data' 来初始化测试数据")
    return True

if __name__ == "__main__":
    import sys
    
    # 检查命令行参数
    reset = "--reset" in sys.argv
    
    if reset:
        confirm = input("这将删除所有现有表！确认继续吗？(yes/no): ")
        if confirm.lower() != "yes":
            print("操作已取消。")
            sys.exit(0)
    
    # 初始化数据库
    success = init_database(reset=reset)
    
    if success:
        print("✅ 数据库表结构初始化成功！")
        print("💡 运行 'python -m app.scripts.init_demo_data' 来初始化测试数据")
    else:
        print("❌ 数据库初始化失败！")
        sys.exit(1) 
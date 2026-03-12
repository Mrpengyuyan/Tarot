# -*- coding: utf-8 -*-
"""
UTF-8编码启动脚本
解决Windows系统下的中文乱码问题
"""
import os
import sys
import locale

def setup_utf8_environment():
    """设置UTF-8编码环境"""
    # 设置环境变量
    os.environ['PYTHONIOENCODING'] = 'utf-8'
    os.environ['LANG'] = 'zh_CN.UTF-8'
    os.environ['LC_ALL'] = 'zh_CN.UTF-8'

    # 对于Windows系统
    if sys.platform.startswith('win'):
        try:
            # 设置控制台代码页为UTF-8
            os.system('chcp 65001 >nul 2>&1')

            # 重新配置标准输出
            if hasattr(sys.stdout, 'reconfigure'):
                sys.stdout.reconfigure(encoding='utf-8', errors='ignore')
                sys.stderr.reconfigure(encoding='utf-8', errors='ignore')

            print("UTF-8编码环境设置完成")
        except Exception as e:
            print(f"编码设置警告: {e}")

    # 验证编码设置
    print(f"系统编码: {sys.getdefaultencoding()}")
    print(f"输出编码: {sys.stdout.encoding}")
    print(f"区域设置: {locale.getpreferredencoding()}")

def start_server():
    """启动服务器"""
    try:
        import uvicorn
        from app.main import app

        print("正在启动塔罗游戏服务器...")
        uvicorn.run(
            app,
            host="0.0.0.0",
            port=8000,
            log_level="info",
            reload=True
        )
    except ImportError:
        print("请先安装 uvicorn: pip install uvicorn")
    except Exception as e:
        print(f"启动失败: {e}")

if __name__ == "__main__":
    setup_utf8_environment()

    print("=" * 50)
    print("塔罗游戏服务器 - UTF-8编码启动")
    print("=" * 50)

    start_server()
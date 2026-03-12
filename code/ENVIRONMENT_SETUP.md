# 🔧 环境配置指导

## 当前状态
✅ 已创建conda虚拟环境 `tarot-game`  
✅ 已激活虚拟环境  
✅ 已创建 `setup.py` 文件

## 下一步操作指南

### 🚀 步骤1：以可编辑模式安装项目
在项目根目录 `D:\Zhu-LORA\game\code` 执行：

```bash
pip install -e .
```

这个命令会：
- 安装所有项目依赖
- 将项目安装为可编辑的Python包
- 解决所有导入路径问题

### 🧪 步骤2：验证安装
安装完成后，测试导入：

```bash
python -c "from app.models import Base; print('✅ 模型导入成功！')"
```

### 🗄️ 步骤3：初始化数据库
```bash
python -m app.scripts.init_demo_data
```

### 🌐 步骤4：启动服务
```bash
uvicorn app.main:app --reload
```

## 可能遇到的问题

### Q1: pip install -e . 失败
**解决方案：**
```bash
# 升级pip
pip install --upgrade pip setuptools wheel
# 重新安装
pip install -e .
```

### Q2: 仍然有导入错误
**解决方案：**
```bash
# 确保在正确的目录
cd D:\Zhu-LORA\game\code
# 检查虚拟环境是否激活
conda info --envs
# 重新激活环境
conda activate tarot-game
```

### Q3: PostgreSQL连接问题
**解决方案：**
- 确保Docker正在运行
- 检查 `.env` 文件配置
- 先用Docker启动数据库：`docker-compose up db`

## 🎯 成功标志
当你能成功运行以下命令时，说明环境配置完成：

```bash
# 1. 模型导入测试
python -c "from app.models import Base; print('✅ 导入成功')"

# 2. 数据库初始化
python -m app.scripts.init_demo_data

# 3. 启动服务
uvicorn app.main:app --reload
```

## 📞 如果遇到问题
请将完整的错误信息发给我，我会帮你解决！ 
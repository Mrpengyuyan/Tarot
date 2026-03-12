# 🤖 AI服务集成指南

## 📋 概述

塔罗牌游戏后端现已完全集成Coze AI服务，提供智能塔罗牌解读功能。系统支持真实AI解读和模拟解读两种模式，确保在任何情况下都能正常工作。

## 🚀 快速开始

### 1. 配置环境变量

要启用真实AI功能，需要设置以下环境变量：

```bash
# Windows CMD
set COZE_API_KEY=your_actual_coze_api_key
set COZE_BOT_ID=your_actual_bot_id

# Windows PowerShell
$env:COZE_API_KEY="your_actual_coze_api_key"
$env:COZE_BOT_ID="your_actual_bot_id"

# Linux/Mac
export COZE_API_KEY=your_actual_coze_api_key
export COZE_BOT_ID=your_actual_bot_id
```

### 2. 检查服务状态

访问健康检查端点：
```bash
GET /api/v1/health/ai
```

## 🎯 核心功能

### 1. AI解读服务 (`app/services/tarot_service.py`)

- **智能提示词生成**: 根据问题类型、牌阵、卡牌信息自动生成专业提示词
- **多场景适配**: 支持爱情、事业、财运、健康、综合等不同类型的问题
- **优雅降级**: AI服务不可用时自动切换到模拟解读模式
- **错误恢复**: 完善的异常处理和重试机制

### 2. Coze API客户端 (`app/services/coze_service.py`)

- **异步通信**: 完全异步的API调用，支持长时间等待
- **健康检查**: 实时监控AI服务状态
- **错误处理**: 详细的错误分类和日志记录
- **超时控制**: 可配置的请求超时时间

### 3. API端点集成

#### 创建AI解读
```http
POST /api/v1/records/{prediction_id}/interpret
Content-Type: application/json
Authorization: Bearer <your_token>

{
    "user_context": "我是一名软件工程师，正在考虑跳槽",
    "force_ai": true
}
```

#### 响应示例
```json
{
    "id": 1,
    "prediction_id": 123,
    "overall_interpretation": "🔮 塔罗解读 - 三牌占卜\n\n亲爱的占卜者...",
    "model_used": "coze_ai",
    "confidence_score": 0.85,
    "created_at": "2024-01-20T10:30:00",
    "updated_at": "2024-01-20T10:30:00"
}
```

## 🔧 技术实现

### AI提示词模板系统

系统使用专业的提示词模板，包含：

1. **基础身份设定**: 塔罗师身份和解读要求
2. **问题类型指导**: 针对不同问题类型的特殊指导
3. **牌阵信息**: 牌阵名称、描述、位置含义
4. **卡牌详情**: 每张牌的名称、位置、正逆位、含义
5. **用户背景**: 可选的用户背景信息

### 解读流程

```
用户创建预测 → 选择牌阵 → 抽取塔罗牌 → 生成AI解读 → 返回结果
       ↓              ↓           ↓            ↓
    数据验证    →   牌阵验证  →  随机算法  →   AI服务调用
```

## 📊 监控和健康检查

### 健康检查端点

| 端点 | 功能 | 响应 |
|------|------|------|
| `GET /api/v1/health/` | 基础健康检查 | 服务状态 |
| `GET /api/v1/health/status` | 详细系统状态 | 全面系统信息 |
| `GET /api/v1/health/ai` | AI服务专用检查 | AI服务状态 |
| `GET /api/v1/health/metrics` | 系统指标 | 数据统计 |

### 状态类型

- **healthy**: 所有服务正常
- **degraded**: AI服务未配置，使用模拟模式
- **unhealthy**: 关键服务故障

## 🔄 运行模式

### 1. 真实AI模式
- 需要有效的Coze API密钥和Bot ID
- 生成真实的AI解读内容
- 支持复杂的上下文理解

### 2. 模拟模式
- 无需API密钥，开箱即用
- 基于塔罗牌含义生成模拟解读
- 保证系统完整功能可用

## 🧪 测试

### 运行集成测试
```bash
python -m app.scripts.test_ai_integration
```

### 测试覆盖
- ✅ AI服务健康检查
- ✅ 完整占卜流程测试
- ✅ 模拟解读功能
- ✅ 错误处理验证

## 📝 日志和调试

### 日志级别
- `INFO`: 正常操作记录
- `WARNING`: AI服务未配置警告
- `ERROR`: API调用失败、解读生成失败

### 常见问题

1. **AI解读生成失败**
   - 检查API密钥是否正确
   - 验证网络连接
   - 查看Coze服务状态

2. **解读内容为空**
   - 检查Bot配置
   - 验证提示词模板
   - 查看API响应日志

3. **性能问题**
   - 调整超时时间设置
   - 监控API调用频率
   - 考虑使用缓存

## 🚦 API配额管理

### 使用建议
- 设置合理的超时时间（60秒）
- 实现请求频率限制
- 监控API使用量
- 准备降级方案

## 🔮 未来扩展

### 计划功能
- [ ] 多语言解读支持
- [ ] 解读质量评分
- [ ] 个性化解读风格
- [ ] 解读历史分析
- [ ] 批量解读处理

## 📞 技术支持

如需技术支持，请检查：

1. 📋 健康检查端点状态
2. 📝 应用日志输出
3. 🔧 环境变量配置
4. 🌐 网络连接状况

---

✨ **恭喜！你的塔罗牌游戏现已拥有强大的AI解读能力！** ✨ 
# 🃏 塔罗牌游戏 API 端点指南

## 📋 **API概览**

所有API端点都已完善并可用，包含完整的CRUD操作和业务逻辑。

### 🔐 **认证端点**
- `POST /api/v1/register` - 用户注册
- `POST /api/v1/login` - 用户登录

### 👤 **用户管理**
- `GET /api/v1/users/me` - 获取当前用户信息
- `PUT /api/v1/users/me` - 更新当前用户信息
- `GET /api/v1/users/{user_id}` - 根据ID获取用户信息

## 🃏 **塔罗牌管理** (`/api/v1/cards`)

### **基础查询**
- `GET /cards/` - 获取塔罗牌列表
  - 支持参数：`skip`, `limit`, `card_type`, `suit`, `search`
- `GET /cards/count` - 获取塔罗牌统计
- `GET /cards/{card_id}` - 获取塔罗牌详情
- `GET /cards/{card_id}/meaning` - 获取塔罗牌含义
  - 参数：`is_reversed`, `aspect`, `position`

### **分类查询**
- `GET /cards/major-arcana` - 获取大阿卡纳牌
- `GET /cards/minor-arcana` - 获取小阿卡纳牌
- `GET /cards/search?q={关键词}` - 搜索塔罗牌

### **游戏功能**
- `GET /cards/draw?count={数量}` - 随机抽牌 🎯
  - 需要登录
  - 可排除指定牌ID

### **管理功能** (需要管理员权限)
- `POST /cards/` - 创建塔罗牌
- `PUT /cards/{card_id}` - 更新塔罗牌
- `DELETE /cards/{card_id}` - 删除塔罗牌
- `POST /cards/batch` - 批量创建塔罗牌

## 🔮 **牌阵管理** (`/api/v1/spreads`)

### **基础查询**
- `GET /spreads/` - 获取牌阵列表
  - 支持参数：`difficulty`, `card_count`, `question_type`, `beginner_friendly`, `search`
- `GET /spreads/count` - 获取牌阵统计
- `GET /spreads/{spread_id}` - 获取牌阵详情
- `GET /spreads/{spread_id}/suitability` - 获取牌阵适用性

### **特殊查询**
- `GET /spreads/popular` - 获取热门牌阵
- `GET /spreads/beginner` - 获取初学者牌阵
- `GET /spreads/by-question-type/{question_type}` - 根据问题类型获取牌阵
  - 支持：`love`, `career`, `finance`, `health`, `general`
- `GET /spreads/search?q={关键词}` - 搜索牌阵

### **使用功能**
- `POST /spreads/{spread_id}/use` - 记录牌阵使用 📊
  - 需要登录，增加使用计数

### **管理功能** (需要管理员权限)
- `POST /spreads/` - 创建牌阵
- `PUT /spreads/{spread_id}` - 更新牌阵
- `DELETE /spreads/{spread_id}` - 删除牌阵（软删除）
- `POST /spreads/batch` - 批量创建牌阵

## 🔮 **预测记录管理** (`/api/v1/records`)

### **预测记录**
- `GET /records/` - 获取用户预测记录
  - 支持参数：`status`, `question_type`, `favorites_only`, `search`
- `GET /records/stats` - 获取用户预测统计
- `GET /records/recent` - 获取最近预测
- `GET /records/{prediction_id}` - 获取预测详情
- `POST /records/` - 创建预测记录
- `PUT /records/{prediction_id}` - 更新预测记录
- `DELETE /records/{prediction_id}` - 删除预测记录

### **抽牌功能** 🎲
- `POST /records/{prediction_id}/draw` - 为预测抽牌
  - 自动根据牌阵类型确定抽牌数量
  - 自动设置正逆位（30%概率逆位）
- `GET /records/{prediction_id}/cards` - 获取预测的抽牌结果
  - 包含牌的含义和牌位信息

### **解读功能** 🤖
- `POST /records/{prediction_id}/interpret` - 创建预测解读
- `GET /records/{prediction_id}/interpretation` - 获取预测解读
- `PUT /records/{prediction_id}/interpretation` - 更新预测解读

### **管理功能**
- `GET /records/admin/all` - 管理员获取所有预测

## 🎯 **完整的预测流程**

### 1. **用户注册/登录**
```http
POST /api/v1/register
POST /api/v1/login
```

### 2. **选择牌阵**
```http
GET /api/v1/spreads/by-question-type/love
```

### 3. **创建预测记录**
```http
POST /api/v1/records/
{
  "question": "我的爱情运势如何？",
  "question_type": "love",
  "spread_type_id": 1
}
```

### 4. **进行抽牌**
```http
POST /api/v1/records/{prediction_id}/draw
```

### 5. **获取抽牌结果**
```http
GET /api/v1/records/{prediction_id}/cards
```

### 6. **生成AI解读**
```http
POST /api/v1/records/{prediction_id}/interpret
{
  "overall_interpretation": "整体解读内容...",
  "advice": "建议内容...",
  "model_used": "coze"
}
```

### 7. **查看完整预测**
```http
GET /api/v1/records/{prediction_id}
```

## 🔑 **权限说明**

### **公开接口**（无需登录）
- 塔罗牌查询
- 牌阵查询
- 用户注册/登录

### **需要登录**
- 抽牌功能
- 预测记录管理
- 用户信息管理

### **需要管理员权限**
- 创建/编辑/删除塔罗牌
- 创建/编辑/删除牌阵
- 批量操作
- 管理功能

## 📊 **数据格式示例**

### **塔罗牌数据**
```json
{
  "id": 1,
  "name_zh": "愚者",
  "name_en": "The Fool",
  "card_number": 0,
  "card_type": "major_arcana",
  "upright_meaning": "新开始，冒险精神...",
  "reversed_meaning": "鲁莽，缺乏计划..."
}
```

### **牌阵数据**
```json
{
  "id": 1,
  "name": "三牌阵",
  "card_count": 3,
  "positions": [
    {"position": 1, "name": "过去", "meaning": "影响现状的过去因素"},
    {"position": 2, "name": "现在", "meaning": "当前的状况"},
    {"position": 3, "name": "未来", "meaning": "可能的发展趋势"}
  ]
}
```

### **预测记录数据**
```json
{
  "id": 1,
  "question": "我的事业发展如何？",
  "question_type": "career",
  "status": "completed",
  "card_draws": [...],
  "interpretation": {...}
}
```

## 🚀 **下一步**

1. **启动服务**：`uvicorn app.main:app --reload`
2. **查看文档**：访问 `http://localhost:8000/docs`
3. **测试API**：使用Swagger UI或Postman
4. **初始化数据**：运行数据初始化脚本
5. **集成AI服务**：接入Coze API

所有API端点都已完善并可立即使用！🎉 
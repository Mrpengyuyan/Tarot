# 塔罗牌游戏数据库架构设计

## 概述

这是一个基于大模型的塔罗牌游戏的数据库设计，支持用户管理、塔罗牌数据、牌阵系统、预测记录和AI解读等核心功能。

## 数据库表结构

### 1. 用户表 (users)

用户账号管理和基本信息存储。

| 字段名 | 类型 | 约束 | 说明 |
|--------|------|------|------|
| id | Integer | PK, Auto | 用户唯一标识 |
| username | String(50) | Unique, Index | 用户名 |
| email | String(100) | Unique, Index | 邮箱地址 |
| hashed_password | String(255) | Not Null | 加密后的密码 |
| nickname | String(50) | Nullable | 用户昵称 |
| avatar_url | String(500) | Nullable | 头像URL |
| birth_date | DateTime | Nullable | 生日 |
| zodiac_sign | String(20) | Nullable | 星座 |
| bio | Text | Nullable | 个人简介 |
| is_active | Boolean | Default: True | 账号是否激活 |
| created_at | DateTime | Default: now() | 创建时间 |
| updated_at | DateTime | Auto Update | 更新时间 |
| last_login | DateTime | Nullable | 最后登录时间 |
| prediction_count | Integer | Default: 0 | 预测次数统计 |

**特殊方法:**
- `set_password(password)`: 设置加密密码
- `verify_password(password)`: 验证密码

### 2. 塔罗牌表 (tarot_cards)

存储78张塔罗牌的完整信息和含义。

| 字段名 | 类型 | 约束 | 说明 |
|--------|------|------|------|
| id | Integer | PK, Auto | 牌的唯一标识 |
| name_en | String(100) | Not Null | 英文名称 |
| name_zh | String(100) | Not Null | 中文名称 |
| card_number | Integer | Not Null | 牌序号 |
| card_type | Enum | Not Null | 牌类型 (大/小阿卡纳) |
| suit | Enum | Nullable | 花色 (仅小阿卡纳) |
| image_url | String(500) | Nullable | 牌面图片URL |
| upright_meaning | Text | Not Null | 正位含义 |
| upright_love | Text | Nullable | 正位爱情含义 |
| upright_career | Text | Nullable | 正位事业含义 |
| upright_finance | Text | Nullable | 正位财运含义 |
| upright_health | Text | Nullable | 正位健康含义 |
| reversed_meaning | Text | Not Null | 逆位含义 |
| reversed_love | Text | Nullable | 逆位爱情含义 |
| reversed_career | Text | Nullable | 逆位事业含义 |
| reversed_finance | Text | Nullable | 逆位财运含义 |
| reversed_health | Text | Nullable | 逆位健康含义 |
| description | Text | Nullable | 牌面描述 |
| keywords_upright | Text | Nullable | 正位关键词 |
| keywords_reversed | Text | Nullable | 逆位关键词 |
| element | String(20) | Nullable | 对应元素 |
| zodiac | String(50) | Nullable | 对应星座 |
| planet | String(50) | Nullable | 对应星球 |

**枚举类型:**
- `CardType`: MAJOR_ARCANA (大阿卡纳), MINOR_ARCANA (小阿卡纳)
- `Suit`: WANDS (权杖), CUPS (圣杯), SWORDS (宝剑), PENTACLES (星币)

**特殊方法:**
- `get_meaning(is_reversed, aspect)`: 获取特定方面的含义
- `get_keywords(is_reversed)`: 获取关键词列表

### 3. 牌阵类型表 (spread_types)

定义各种塔罗牌阵的结构和用途。

| 字段名 | 类型 | 约束 | 说明 |
|--------|------|------|------|
| id | Integer | PK, Auto | 牌阵唯一标识 |
| name | String(100) | Not Null | 牌阵名称 |
| name_en | String(100) | Nullable | 英文名称 |
| description | Text | Not Null | 牌阵描述 |
| card_count | Integer | Not Null | 需要的牌数量 |
| difficulty_level | Integer | Default: 1 | 难度等级 (1-5) |
| positions | JSON | Not Null | 牌位定义和含义 |
| layout_image_url | String(500) | Nullable | 牌阵布局图片URL |
| is_active | Boolean | Default: True | 是否启用 |
| is_beginner_friendly | Boolean | Default: False | 是否适合初学者 |
| suitable_for_love | Boolean | Default: True | 适用于爱情问题 |
| suitable_for_career | Boolean | Default: True | 适用于事业问题 |
| suitable_for_finance | Boolean | Default: True | 适用于财运问题 |
| suitable_for_health | Boolean | Default: True | 适用于健康问题 |
| suitable_for_general | Boolean | Default: True | 适用于一般问题 |
| usage_count | Integer | Default: 0 | 使用次数统计 |

**JSON字段格式 (positions):**
```json
[
  {
    "position": 1,
    "name": "过去",
    "meaning": "影响现状的过去因素"
  },
  {
    "position": 2,
    "name": "现在",
    "meaning": "当前的状况"
  }
]
```

**特殊方法:**
- `get_position_meaning(position)`: 获取特定牌位的含义
- `get_position_name(position)`: 获取特定牌位的名称
- `is_suitable_for_question_type(question_type)`: 检查是否适用于特定问题类型

### 4. 预测记录表 (predictions)

存储用户的每次塔罗预测请求。

| 字段名 | 类型 | 约束 | 说明 |
|--------|------|------|------|
| id | Integer | PK, Auto | 预测记录唯一标识 |
| user_id | Integer | FK(users.id) | 用户ID |
| spread_type_id | Integer | FK(spread_types.id) | 牌阵类型ID |
| question | Text | Not Null | 用户问题 |
| question_type | Enum | Not Null | 问题类型 |
| status | Enum | Default: PENDING | 预测状态 |
| created_at | DateTime | Default: now() | 创建时间 |
| completed_at | DateTime | Nullable | 完成时间 |
| is_favorite | Boolean | Default: False | 是否收藏 |
| user_rating | Integer | Nullable | 用户评分 (1-5) |
| user_notes | Text | Nullable | 用户备注 |

**枚举类型:**
- `QuestionType`: LOVE (爱情), CAREER (事业), FINANCE (财运), HEALTH (健康), GENERAL (一般)
- `PredictionStatus`: PENDING (待处理), PROCESSING (处理中), COMPLETED (已完成), FAILED (失败)

### 5. 抽牌记录表 (card_draws)

记录每次预测中抽到的具体牌和牌位。

| 字段名 | 类型 | 约束 | 说明 |
|--------|------|------|------|
| id | Integer | PK, Auto | 抽牌记录唯一标识 |
| prediction_id | Integer | FK(predictions.id) | 预测记录ID |
| tarot_card_id | Integer | FK(tarot_cards.id) | 塔罗牌ID |
| position | Integer | Not Null | 牌位位置 |
| is_reversed | Boolean | Default: False | 是否逆位 |
| drawn_at | DateTime | Default: now() | 抽牌时间 |

**特殊方法:**
- `get_card_meaning(aspect)`: 获取这张牌在当前预测中的含义
- `get_position_name()`: 获取牌位名称
- `get_position_meaning()`: 获取牌位含义

### 6. 解读结果表 (interpretations)

存储AI大模型对塔罗牌组合的解读内容。

| 字段名 | 类型 | 约束 | 说明 |
|--------|------|------|------|
| id | Integer | PK, Auto | 解读结果唯一标识 |
| prediction_id | Integer | FK(predictions.id) | 预测记录ID |
| overall_interpretation | Text | Not Null | 整体解读 |
| card_analysis | Text | Nullable | 单牌分析 |
| relationship_analysis | Text | Nullable | 牌间关系分析 |
| advice | Text | Nullable | 建议 |
| warning | Text | Nullable | 警告或注意事项 |
| summary | Text | Nullable | 预测概要 |
| key_themes | String(500) | Nullable | 关键主题 |
| model_used | String(100) | Nullable | 使用的AI模型 |
| model_version | String(50) | Nullable | 模型版本 |
| confidence_score | Float | Nullable | 置信度分数 |
| generated_at | DateTime | Default: now() | 生成时间 |

**特殊方法:**
- `get_key_themes_list()`: 获取关键主题列表

## 表关系图

```
users (1) ----< predictions (N)
             |
             |-- spread_types (N) ---- predictions (1)
             |
             |-- card_draws (1) ----< tarot_cards (N)
             |
             |-- interpretations (1:1)
```

## 关键特性

### 1. 数据完整性
- 所有外键关系都有适当的约束
- 使用枚举类型确保数据一致性
- 关键字段都有非空约束

### 2. 查询性能
- 在常用查询字段上建立索引
- 用户名和邮箱的唯一索引
- 外键字段自动索引

### 3. 可扩展性
- JSON字段存储复杂的牌阵定义
- 支持多种问题类型和牌阵
- 预留了图片URL字段用于未来扩展

### 4. 数据追溯
- 所有重要操作都有时间戳
- 保留预测的完整历史记录
- 支持用户评分和备注

## 使用示例

### 创建一次预测的完整流程

1. **用户选择牌阵和提出问题**
2. **创建预测记录**
3. **根据牌阵抽取相应数量的牌**
4. **记录每张抽到的牌和牌位**
5. **调用AI模型生成解读**
6. **保存解读结果**
7. **更新预测状态为完成**

这个数据库设计支持游戏的所有核心功能，并为未来的功能扩展预留了足够的灵活性。 
# Tarot · 塔罗仪式

> 一款基于 **Unity 6 (URP)** 与 **FastAPI** 构建的 3D 桌面端塔罗牌仪式体验游戏，支持完整 78 张实体牌扇抽取、双模式解读（在线 AI / 本地离线牌义）与历史占卜回顾。

## 核心特色

- **完整 78 张实体牌桌仪式**：从桌边牌堆抬起洗牌（支持设置中开启 `2.5x` 快速洗牌），展开双排 78 张实体牌扇凭直觉选牌，落位翻牌后自动呈现高清近景检视。
- **三种经典牌阵**：支持 **一张牌（洞察此刻）**、**三张牌（过去·现在·未来）** 与 **凯尔特十字（10 张全貌）**，自适应 16:9 / 4:3 与高分屏窗口。
- **在线 AI + 本地离线双模式**：
  - **在线模式**：连接 FastAPI 后端调用 DeepSeek 生成结构化牌阵解读，支持后台异步轮询与失败后手动重试。
  - **离线模式**：无需部署后端、不消耗任何 API 额度，即可使用本地 78 张传统牌义完成完整流程；也可在右上角“设置 → 解读模式”随时手动切至离线牌义。
- **桌面端交互细节**：结果页支持**一键复制占卜摘要**与**再占一局**；主菜单内置**历史记录抽屉**；提问区提供轻量灵感方向提示（情感 / 事业 / 抉择，不代填问题）；内置丝绒暗场场景过渡与完整快捷键。
- **生产级安全与成本保护**：后端内置数据库级日/月 AI 预算预留、单局最多两次计费调用上限、访客会话与每日占卜限流、受信任反代 `X-Forwarded-For` 解析与自动化秘密边界扫描。

## 桌面快捷键

| 按键 | 作用 |
| --- | --- |
| `Enter` | 在提问阶段确认并开始洗牌；在全部翻牌就绪后进入结果页 |
| `Space` | 在翻牌阶段依次翻开下一张未揭开的牌；全部揭开后进入结果页 |
| `Esc` | 退出问题输入框，或关闭当前打开的近景检视 / 设置 / 历史记录抽屉 |
| 鼠标悬停 / 点击 | 牌扇悬停抬牌与分让、牌桌卡牌微倾提亮、点击已翻开卡牌放大检视 |

## 目录结构

```text
Tarot/
├── README.md                                  本文件
├── LICENSE                                    MIT 开源协议（含第三方素材声明）
├── UnityClient/
│   ├── README.md
│   └── TarotUnity/                            Unity 6000.3.16f1 (URP) 客户端工程
│       ├── Assets/Scenes/                     Boot / MainMenu / ReadingRoom / Result
│       ├── Assets/Scripts/
│       │   ├── Core/                          音频、偏好设置、场景暗场流转、历史记录存储
│       │   ├── Data/                          API 模型与会话快照
│       │   ├── Gameplay/                      牌组、78 张牌扇（对象池）、翻牌与离线模拟
│       │   ├── Network/                       统一 ApiClient 与异步解读轮询
│       │   ├── Presentation/                  3D 牌桌镜头、洗牌编排、纸张质感与牌桌悬停
│       │   └── UI/                            封面/牌桌/结果视图、设置与历史抽屉、分享格式化
│       ├── Assets/Editor/
│       │   ├── Release/                       桌面发行构建与视觉审查工具
│       │   └── LegacyPhases/                  历史阶段搭建与抓图归档
│       └── Assets/Tests/                      649 EditMode + 280+ PlayMode 自动化测试
├── Server/
│   ├── README.md                              后端启动、配置和测试说明
│   ├── check_config.py                        运行配置查看与 --production-check 体检
│   ├── check_release_hygiene.py               发布前秘密与仓库卫生扫描
│   ├── .env.example                           本地开发配置模板
│   ├── .env.production.example                低预算小范围公测生产模板
│   ├── deploy/                                Docker Compose + Caddy 自动 HTTPS 部署模板
│   ├── app/
│   │   ├── api/v1/endpoints/                  auth / records / cards / spreads / health
│   │   ├── core/                              配置、原生 bcrypt 安全与邮箱规范化
│   │   ├── crud/ & models/ & schemas/         数据持久化与 Pydantic 契约
│   │   └── services/                          AI 客户端、路由/计费、预算存储、反代 IP 限流与解读编排
│   ├── tests/                                 1,640+ 项隔离后端 pytest 用例
│   ├── alembic/                               数据库迁移
│   └── data/                                  78 张牌和 6 个牌阵的参考数据
└── .github/workflows/
    └── backend-tests.yml                      Server/ 变更自动运行全量后端测试
```

## 快速开始

### 1. 玩家试玩（无需开发环境）

从 **GitHub Releases** 下载对应平台的桌面发行包（macOS / Windows），解压后直接运行即可，无需安装 Unity、Python 或 Docker。

- 默认连接不到在线服务时，自动进入本地离线解读模式；
- 也可点击右上角 **设置 → 解读模式** 手动切换“离线牌义 / 在线优先”。

### 2. Unity 本地开发

1. 使用 Unity **`6000.3.16f1`** 打开 `UnityClient/TarotUnity/`。
2. 打开入口场景 `Assets/Scenes/Boot.unity` 并运行。
3. 在线模式地址读取自 `UnityClient/TarotUnity/Assets/StreamingAssets/tarot_desktop_config.json`（开发默认 `http://localhost:8000/api/v1`）。

### 3. 本地后端联调与测试

```bash
cd Server
python3 -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env
alembic upgrade head
python -m app.scripts.init_tarot_data
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

运行全量隔离测试：

```bash
cd Server
python -m pytest -q
```

### 4. 小范围公测低预算自托管（自动 HTTPS）

如果你使用自费 DeepSeek API 并希望分享给朋友试玩，请参考 [`Server/deploy/README.md`](Server/deploy/README.md) 使用单机 Docker Compose + Caddy 自动 HTTPS 模板：

- 模板已默认关闭高价 `reasoner` 回退，预设每日 1 美元 / 每月 10 美元硬预算上限与每人每日 2 次访客额度；
- 上线前一键体检：

```bash
python3 Server/check_release_hygiene.py
python3 Server/check_config.py --production-check
```

## 安全边界

- API Key、JWT `SECRET_KEY`、数据库凭据**只存在于后端环境变量中**，绝不写入 Unity 工程或客户端安装包。
- Unity 配置文件仅保存公开服务地址与请求超时时间。
- `.env`、本地 SQLite 数据库、构建产物与 Unity 缓存均已由 `.gitignore` 隔离，并通过 `Server/check_release_hygiene.py` 自动化校验。

## 开源协议与素材致谢

- 项目源码与原创 UI 资源采用 [MIT License](LICENSE)。
- 第三方公共领域 / CC0 1.0 美术素材（1909 Rider-Waite-Smith 牌面、ambientCG 材质）与 SIL OFL 1.1 中文字体（霞鹜文楷、马善政楷书、思源宋体）来源与授权明细见 [`UnityClient/TarotUnity/Docs/THIRD_PARTY_ASSETS.md`](UnityClient/TarotUnity/Docs/THIRD_PARTY_ASSETS.md)。

## 相关文档

- [`UnityClient/README.md`](UnityClient/README.md) / [`UnityClient/TarotUnity/README.md`](UnityClient/TarotUnity/README.md)：Unity 客户端结构与本地无成本联调测试
- [`Server/README.md`](Server/README.md)：后端接口、配置与迁移说明
- [`Server/deploy/README.md`](Server/deploy/README.md)：小范围公测生产部署指南

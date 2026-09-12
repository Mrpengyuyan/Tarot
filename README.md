# Tarot

Tarot 3D 塔罗牌仪式游戏的产品仓库，包含 Unity 桌面客户端和 FastAPI 后端。

- 客户端：Unity `6000.3.16f1`、URP、版本 `0.9.0`，目标平台为 macOS 和 Windows 桌面端。
- 后端：FastAPI、SQLAlchemy、Alembic，负责认证与访客会话、抽牌、阅读记录、AI 解读、限流和预算。

## 目录

```text
Tarot/
├── README.md                     本文件
├── PROJECT_COMPLETION_PLAN.md    1.0 完结路线
├── UNITY_FRONTEND_PLAN.md        Unity 前端方向和后端边界
├── UnityClient/
│   ├── README.md
│   └── TarotUnity/               Unity 工程
├── Server/
│   ├── README.md                 后端启动、配置和测试
│   ├── app/                      FastAPI 应用
│   ├── tests/                    后端测试
│   ├── alembic/                  数据库迁移
│   └── data/                     78 张牌和 6 个牌阵的参考数据
├── .github/workflows/
│   └── backend-tests.yml         只在 Server/ 变动时运行
└── docs/superpowers/             设计与实施文档
```

Unity 工程位于 `UnityClient/TarotUnity/`。构建场景包括：

- `Boot`：启动和持久化服务初始化。
- `MainMenu`：牌阵选择和进入仪式。
- `ReadingRoom`：问题输入、洗牌、发牌和翻牌。
- `Result`：多张牌展示和 AI 解读阅读。

## 运行方式

### Unity 本地开发

使用 Unity `6000.3.16f1` 打开 `UnityClient/TarotUnity/`，然后从
`Assets/Scenes/Boot.unity` 运行。默认可以使用本地模拟数据完成完整流程。

### 本地后端联调

1. 按 [`Server/README.md`](Server/README.md) 启动后端，默认地址为 `http://localhost:8000`。
2. Unity 的在线模式通过
   `UnityClient/TarotUnity/Assets/StreamingAssets/tarot_desktop_config.json`
   读取后端地址，默认为 `http://localhost:8000/api/v1`。该配置面向本地开发，不代表公开发行地址。
3. 客户端启动时先访问 `/api/v1/health/`，服务可达后再访问 `/api/v1/guest-session` 申请访客会话。

后端负责认证、阅读记录、抽牌、AI 解读、限流、预算和 API Key；Unity 只负责客户端表现和交互。

### 玩家发行包

玩家不需要安装 Python、Conda、Docker 或 Unity 编辑器。正式发行包应从
GitHub Releases 下载并解压运行；在线模式必须指向部署后的 HTTPS 后端，
不能指向开发者电脑的 `localhost`。

## 安全边界

- API Key、数据库密码和管理员令牌只允许存在于后端安全配置中（`Server/.env` 或部署平台的密钥配置）。
- Unity 配置文件只能包含公开的服务地址、超时时间和客户端选项。
- `.env`、本地数据库、日志、构建目录和 Unity 缓存不得提交；根目录 `.gitignore` 已忽略它们。
- 公开发行前必须完成客户端、构建产物和 Git 历史的秘密扫描。

## 仓库历史

- 从 2026-09-11 起，本仓库是唯一的提交入口。后端以一次提交并入 `Server/`：除 `Server/README.md` 外的 80 个文件与旧后端仓库提交 `f19c7cd` 逐字节一致。
- 旧后端仓库的 16 个提交保留在远端分支 `backend-main`（`f19c7cd`）。查看方法：先执行 `git fetch origin backend-main`，再执行 `git log origin/backend-main`。
- 已废弃的 React Web 前端（`src/`、`public/`）和旧的 `docker-compose.yml` 只保留在 `backend-main`，不在 `main` 中维护。
- 设计依据：[`docs/superpowers/specs/2026-09-11-repo-unification-design.md`](docs/superpowers/specs/2026-09-11-repo-unification-design.md)。

## 文档入口

- [`PROJECT_COMPLETION_PLAN.md`](PROJECT_COMPLETION_PLAN.md)：1.0 完结路线、阶段任务和验收标准。
- [`UNITY_FRONTEND_PLAN.md`](UNITY_FRONTEND_PLAN.md)：Unity 前端方向和后端边界。
- [`UnityClient/README.md`](UnityClient/README.md)：Unity 工程目录、运行和发行说明。
- [`Server/README.md`](Server/README.md)：后端启动、配置和测试。
- [`UnityClient/TarotUnity/Docs/PROJECT_CHRONICLE.md`](UnityClient/TarotUnity/Docs/PROJECT_CHRONICLE.md)：Phase 1-64 的整理记录。
- [`UnityClient/TarotUnity/Docs/THIRD_PARTY_ASSETS.md`](UnityClient/TarotUnity/Docs/THIRD_PARTY_ASSETS.md)：资源来源和授权记录。

## 当前下一步

1. 激活 Unity Editor 许可证，重新执行 EditMode、PlayMode 和真实客户端联调。
2. 将后端部署到 HTTPS 地址，并把发行配置从 `localhost` 切换到正式地址。
3. 完善部署级访客频率限制和 AI 预算保护，再验证 macOS/Windows 干净环境。
4. 通过 1.0 验收后，将发行包放入 GitHub Release，而不是提交到源码历史。

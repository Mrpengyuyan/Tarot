# Tarot

Tarot 3D 塔罗牌仪式游戏的产品仓库，包含 Unity 桌面客户端和 FastAPI 后端。

- 客户端：Unity `6000.3.16f1`、URP、版本 `0.9.0`，目标平台为 macOS 和 Windows 桌面端。
- 后端：FastAPI、SQLAlchemy、Alembic，负责认证与访客会话、抽牌、阅读记录、AI 解读、限流和预算。

## 目录

```text
Tarot/
├── README.md                     本文件
├── UnityClient/
│   ├── README.md
│   └── TarotUnity/               Unity 工程
├── Server/
│   ├── README.md                 后端启动、配置和测试
│   ├── app/                      FastAPI 应用
│   ├── tests/                    后端测试
│   ├── alembic/                  数据库迁移
│   └── data/                     78 张牌和 6 个牌阵的参考数据
└── .github/workflows/
│   └── backend-tests.yml         只在 Server/ 变动时运行
```

Unity 工程位于 `UnityClient/TarotUnity/`。构建场景包括：

- `Boot`：启动和持久化服务初始化。
- `MainMenu`：封面和入席入口。
- `ReadingRoom`：选择牌阵、输入问题、洗牌、选牌和翻牌。
- `Result`：多张牌展示和 AI 解读阅读。

## 运行方式

### Unity 本地开发

使用 Unity `6000.3.16f1` 打开 `UnityClient/TarotUnity/`，然后从
`Assets/Scenes/Boot.unity` 运行。默认可以使用本地模拟数据完成完整流程。

开始洗牌和选牌前必须输入自己的问题；空内容或只有空白字符不能开始，也不会自动代填示例问题。
在线和离线模式都遵守此规则；输入框中的示例仅为提示。

界面按阶段显示操作，不再同时铺开牌阵、输入框和结果按钮。
洗牌时不显示文字提示；选中的牌先以牌背停留在上方，全部选齐后再落入牌阵。
单牌和三牌使用一行暂存，凯尔特十字使用两行；牌堆始终是同一副实体牌，不另留静态副本。
桌边牌堆使用同一套暗紫金色牌背，按窗口比例显示；洗牌时从桌边抬起，经过分叠、交错、再切和收齐，
选牌展开时逐层消失，收牌后回到桌边。抬牌约 0.9 秒，洗牌与收齐约 5.8 秒。
选牌时完整的 78 张实体牌以双排牌扇展开，选中牌用 0.9 秒飞入上方牌位；
全部选齐后停留 1.25 秒，再转入翻牌。牌扇和暂存区随窗口比例、高清缩放调整，不改变选牌顺序或重新抽牌。
封面使用题字字体，其余界面采用衬线字体，并保留完整中文字库作为后备。
封面、牌阵选择和提问区按同一套参考比例适配 16:9、4:3 及高清窗口；固定文案的补字随包提供，不依赖系统字体。
选牌计数、翻牌提示和牌位文字使用统一的暖金色层级，字距随字号适配高清窗口；重新应用主题不会覆盖这些文字的颜色。
解读页沿用无边框阅读区和金色细线操作，保留多牌展示、长文滚动、卡牌定位及在线等待、重试和离线提示。

单牌、三牌和凯尔特十字翻牌后会自动展示牌面近景，清晰停留 1 秒后回到原牌位。
翻牌牌桌按预览的屏幕比例呈现卡牌及牌位标注，支持 16:9 和 4:3；调整窗口不会重新抽牌。
牌阵选择、提问、选牌和翻牌共用稳定的牌桌取景，洗牌仍保留靠近实体牌堆的镜头。
实体卡牌的印刷图案独立于环境调色，避免暗紫牌背被压黑，同时保留牌桌的烛光和阴影。
翻牌牌桌上的卡牌采用细金边和柔和落影；翻动或近景查看时自动隐藏这层装饰，不遮挡牌面内容。
点击已翻开的牌可以不限时放大查看；点击空白处或按 `Esc` 返回牌桌。
近景显示牌名、牌位和正逆位，不会重新抽牌或额外请求 AI。

### 本地后端联调

1. 按 [`Server/README.md`](Server/README.md) 启动后端，默认地址为 `http://localhost:8000`。
2. Unity 的在线模式通过
   `UnityClient/TarotUnity/Assets/StreamingAssets/tarot_desktop_config.json`
   读取后端地址，默认为 `http://localhost:8000/api/v1`。该配置面向本地开发，不代表公开发行地址。
3. 客户端启动时先访问 `/api/v1/health/`，服务可达后再访问 `/api/v1/guest-session` 申请访客会话。

后端负责认证、阅读记录、抽牌、AI 解读、限流、预算和 API Key；Unity 只负责客户端表现和交互。
已在无模型密钥的本机 FastAPI 模拟环境中验证 Unity 的 1、3、10 张牌完整场景流程；
这不等同于真实 AI 或公开发行环境验收。

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

## 文档入口

- [`UnityClient/README.md`](UnityClient/README.md)：Unity 工程目录、运行和发行说明。
- [`Server/README.md`](Server/README.md)：后端启动、配置和测试。
- [`UnityClient/TarotUnity/Docs/THIRD_PARTY_ASSETS.md`](UnityClient/TarotUnity/Docs/THIRD_PARTY_ASSETS.md)：资源来源和授权记录。

## 当前下一步

当前继续对齐已确认的 Unity 视觉预览，并完成独立程序试玩验收；正式部署地址在最终部署阶段配置。

1. 继续校准光影、文字亮度和边缘细节，与预览逐阶段对照，再完成独立程序试玩。
2. 确定后端托管方式和预算，补齐部署级访客限流，再部署到 HTTPS 地址。
3. 将发行配置从 `localhost` 切换到正式地址；经明确同意后，以小额真实 AI 调用验证 1、3、10 张牌流程。
4. 检查密钥与构建产物，并在 macOS/Windows 干净环境中验证下载安装和离线模式。
5. 通过 1.0 验收后，将发行包放入 GitHub Release，而不是提交到源码历史。

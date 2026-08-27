# Tarot

这是 Tarot 3D 卡牌游戏的 Unity 客户端仓库。当前客户端基线为 Unity
`6000.3.16f1`、URP、版本 `0.9.0`，目标平台为 macOS 和 Windows 桌面端。

## 当前目录

```text
Tarot/
  README.md
  PROJECT_COMPLETION_PLAN.md
  UNITY_FRONTEND_PLAN.md
  UnityClient/
    README.md
    TarotUnity/
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

Unity 的在线模式通过
`UnityClient/TarotUnity/Assets/StreamingAssets/tarot_desktop_config.json`
读取后端地址。该配置默认面向本地开发，不代表公开发行地址。

FastAPI 后端目前仍保存在本机独立的 `Tarot` 工程中，尚未合并到这个 Unity
仓库。后端负责认证、阅读记录、抽牌、AI 解读、限流、预算和 API Key；Unity
只负责客户端表现和交互。两个工程的合并或拆分方案必须在真实后端接入前明确，
不能把两个独立 Git 历史直接覆盖拼接。

### 玩家发行包

玩家不需要安装 Python、Conda、Docker 或 Unity 编辑器。正式发行包应从
GitHub Releases 下载并解压运行；在线模式必须指向部署后的 HTTPS 后端，
不能指向开发者电脑的 `localhost`。

## 安全边界

- API Key、数据库密码和管理员令牌只允许存在后端安全配置中。
- Unity 配置文件只能包含公开的服务地址、超时时间和客户端选项。
- `.env`、本地数据库、日志、构建目录和 Unity 缓存不得提交。
- 公开发行前必须完成客户端、构建产物和 Git 历史的秘密扫描。

## 文档入口

- [`PROJECT_COMPLETION_PLAN.md`](PROJECT_COMPLETION_PLAN.md)：1.0 完结路线、阶段任务和验收标准。
- [`UNITY_FRONTEND_PLAN.md`](UNITY_FRONTEND_PLAN.md)：Unity 前端方向和后端边界。
- [`UnityClient/README.md`](UnityClient/README.md)：Unity 工程目录、运行和发行说明。
- [`UnityClient/TarotUnity/Docs/PROJECT_CHRONICLE.md`](UnityClient/TarotUnity/Docs/PROJECT_CHRONICLE.md)：Phase 1-64 的整理记录。
- [`UnityClient/TarotUnity/Docs/THIRD_PARTY_ASSETS.md`](UnityClient/TarotUnity/Docs/THIRD_PARTY_ASSETS.md)：资源来源和授权记录。

## 当前下一步

1. 激活 Unity Editor 许可证，重新执行 EditMode、PlayMode 和真实客户端联调。
2. 将后端部署到 HTTPS 地址，并把发行配置从 `localhost` 切换到正式地址。
3. 完善部署级访客频率限制和 AI 预算保护，再验证 macOS/Windows 干净环境。
4. 通过 1.0 验收后，将发行包放入 GitHub Release，而不是提交到源码历史。

# Codex 完整代码审查与测试提示词

下面这段提示词已经按当前仓库定制，可直接复制给 Codex 使用。

```text
你现在是这个仓库的资深审查工程师和测试工程师。请对当前项目做一次“完整、严格、可复现”的代码审查与测试，并用中文输出结果。本次任务为只读审查与测试任务，不要修改任何代码文件。如果发现低风险且修复范围明确的问题，请直接做最小修复，并在完成后复跑相关测试。

项目背景：
- 这是一个前后端同仓项目，仓库根目录是当前工作目录。
- 前端在 `src/` 和 `public/`，技术栈是 React + TypeScript。
- 后端在 `app/`，技术栈是 FastAPI + SQLAlchemy + Alembic。
- 后端测试主要在 `tests/`。
- 前端已有测试文件，包括：
  - `src/App.test.tsx`
  - `src/pages/Reading/DrawCards.test.tsx`
  - `src/services/aiService.test.ts`
  - `src/services/cozeService.test.ts`
  - `src/services/enhancedTarotService.test.ts`
- 后端 CI 参考 `.github/workflows/backend-tests.yml`，其中使用 Python 3.10，并执行：
  - `python -m pytest -q tests`
  - `python -m pytest -q`
- 仓库说明文档在 `README.md`，依赖入口为 `package.json` 和 `requirements.txt`。

目标：
1. 全面理解项目结构、关键模块和测试入口。
2. 对代码进行严格审查，优先识别真实缺陷、回归风险、安全问题、配置问题、边界条件遗漏和测试盲区。
3. 尽可能运行可执行的测试、构建和校验命令。
4. 明确区分：
   - 已确认问题
   - 可能风险
   - 环境阻塞导致无法验证的部分
5. 除非我明确要求，不要进行大规模重构；审查和测试优先。如果为了验证问题必须做最小改动，请先说明原因，并在输出中单独列出。

执行要求：
- 先读代码和配置，再执行测试，不要直接下结论。
- 优先使用仓库现有脚本和官方入口，不要自创测试路径。
- 不要跳过失败测试；如果某步失败，继续收集足够上下文并分析原因。
- 不要伪造通过结果、覆盖率、日志或结论。
- 如果某项测试因为缺少依赖、环境变量、数据库、浏览器或外部 API key 无法执行，要明确标记为“环境阻塞”，不要误报为代码缺陷。
- 如果发现用户工作区已有未提交改动，不要覆盖它们。
- 不要使用破坏性命令，例如 `git reset --hard`、`git checkout --`、删除用户文件等。

建议工作流：
1. 代码与配置盘点
   - 阅读以下文件并总结关键信息：
     - `README.md`
     - `package.json`
     - `requirements.txt`
     - `.github/workflows/backend-tests.yml`
     - 如有需要，再补充查看 `.env.example`、`Dockerfile`、`docker-compose.yml`
   - 梳理前后端模块边界、API 调用链、认证方式、数据库迁移、AI 服务接入点和烟测脚本。

2. 审查重点
   - 前端：
     - 路由、状态管理、异步请求、错误处理、表单校验、组件边界、UI 逻辑回归
     - `src/services/` 中 API 封装与后端契约是否一致
     - `src/pages/Reading/` 与抽牌、翻牌、结果页流程是否存在状态错乱或空值问题
   - 后端：
     - `app/api/`、`app/services/`、`app/crud/`、`app/db/` 的职责边界和错误处理
     - 鉴权、Cookie/CSRF、依赖注入、配置项默认值、数据库初始化与迁移逻辑
     - AI/DeepSeek/Coze 相关服务的超时、重试、fallback、预算保护和异常路径
   - 测试质量：
     - 现有测试是否覆盖关键业务流、边界输入、异常分支和回归点
     - 是否存在容易脆断、误报、过度 mock 或遗漏集成层的问题

3. 测试与验证
   - 先确认本机可用环境和版本。
   - 如未安装依赖，则安装依赖。
   - 优先执行以下命令，并记录每一步结果：
     - `python -m pip install -r requirements.txt`
     - `python -m pytest -q tests`
     - `python -m pytest -q`
     - `npm install`
     - `CI=true npm test -- --watchAll=false`
     - `npm run build`
   - 如果环境允许，再尝试仓库中的烟测脚本，并说明前置条件是否满足：
     - `python scripts/smoke_ui_interpretation.py`
     - `python scripts/smoke_ui_pages.py`
   - 如果烟测依赖前后端服务已启动、浏览器路径或外部 API key，请先检查是否满足条件；不满足就报告阻塞点。

4. 分析原则
   - 每个审查结论都尽量给出证据：文件路径、关键函数、触发路径、失败日志或测试现象。
   - 不要把代码风格偏好包装成缺陷。
   - 优先报告高价值问题：
     - 会导致功能错误
     - 会导致安全风险
     - 会导致数据错误或状态不一致
     - 会导致生产构建或 CI 失败
     - 会导致测试虚假通过或关键路径未覆盖
   - 如果你不确定一个点是否为缺陷，请明确写成“风险/待确认”，并说明还缺什么证据。

输出格式必须严格遵守：

第一部分：Findings
- 按严重级别排序：`Critical`、`High`、`Medium`、`Low`
- 每条使用下面结构：
  - `[严重级别] 文件路径:行号 - 简短标题`
  - `问题：`
  - `影响：`
  - `证据：`
  - `建议：`
- 如果没有发现可确认缺陷，明确写：`未发现可确认缺陷。`

第二部分：Test Results
- 列出每个实际执行过的命令
- 标记结果：`passed`、`failed`、`blocked`
- 如果失败，给出关键报错和你的判断
- 如果阻塞，说明缺少什么前置条件

第三部分：Coverage Gaps
- 列出你认为当前测试体系没有覆盖到、但业务上重要的场景
- 尤其关注：
  - 登录与鉴权
  - 抽牌与解读流程
  - 记录落库与详情回查
  - AI fallback 与预算保护
  - 配置项和启动开关

第四部分：Open Questions / Assumptions
- 列出你在审查过程中无法从仓库直接确认的点

第五部分：Change Summary
- 只有在你实际修改了代码时才输出
- 简要说明改了什么、为什么改、改后如何验证

附加要求：
- 文件引用要精确到路径，能给行号就给行号。
- 如果一个问题可以通过新增测试来稳定复现，请指出应补在哪个测试文件层级。
- 如果你认为某个问题会影响 PR 合并，请直接说明“建议阻塞合并”。
- 在结束前，给出一个总判断：
  - `建议通过`
  - `建议修复后再审`
  - `建议阻塞合并`
  并用 2 到 5 句话解释理由。
```

# Phase 70 — 界面小修

用户第一次在 Unity 里试玩 Phase 69 后提出这几处，本期逐项处理。实现在 `Editor/Phase70UiPolishBootstrapper.cs`（菜单 `Tools/Tarot Unity/Run Phase 70 UI Polish Bootstrap`）。

## 改了什么

| 问题 | 处理 |
|---|---|
| 窗口比 16:9 窄时，主菜单和占卜房的界面两侧被裁掉 | 两个画布加上结果页从 Phase 67 起就在用的 `ResultCanvasAspectFit`：窄屏按宽度缩放、宽屏按高度缩放。这两屏没有需要贴边的元素，`pinned` 为空。 |
| 问题输入框的文字不在框的中间 | 提示文字和输入文字都改为水平居中，与金色下划线对齐。 |
| 按钮行一直给隐藏的「揭示结果」留着位置，看得见的四个按钮整体偏左 | 「揭示结果」与「洗牌抽取」占同一个位置、同样大小。`ReadingRoomController.SetResultButtonVisible` 在揭示结果出现时隐藏洗牌抽取（一局里它不会再被启用）。按钮行只按四个按钮居中排列，操作栏宽度和输入框宽度（62%）随之收窄。 |
| 步骤条显得低级 | `Phase7_RitualHudRoot` 加 `CanvasGroup`：alpha 0，不可交互，不拦截点击。 |
| 牌桌上一直在转的黄色光 | 这是 Phase 16 留下的「仪式光环」：两个转动的符文环、一个会呼吸的光池、四个漂浮光点，没有玩法作用。现在关掉这 7 个对象的 `MeshRenderer`。结果页那套同款光环在 Phase 35 已经用同样的办法隐藏。 |

步骤条和光环都只是不再绘制，对象保持激活，原因有三：

- Phase 7、Phase 16–21 的测试用 `GameObject.Find` 查找它们，这个方法找不到未激活的对象。
- 光环根节点上还挂着 Phase 18 的粒子和 Phase 19 的卡牌动作特效。
- `RitualStepIndicator` 挂在画布上，照常运行，抽牌时点亮牌位的光不受影响。

## 运行顺序

先运行 Phase 69 的 restyle，再运行 Phase 70。Phase 69 会按五个按钮重新排布按钮行，所以重跑 Phase 69 之后必须再跑一遍 Phase 70。Phase 70 可以重复运行：连续运行两次，场景文件的哈希值不变。

## 测试

- `Tests/EditMode/Phase70UiPolishTests`：检查场景状态。
- `Tests/PlayMode/Phase70RevealReplacesDrawTests`：完整跑一局单张牌阵，确认揭示结果出现时洗牌抽取消失。
- `Phase22UiCompositionTests.ActionTrayControlsDoNotOverlapHorizontally` 原来要求揭示结果排在洗牌抽取右边。两者现在有意共用一个位置、不会同时出现，所以改为检查玩家实际看到的四个按钮。

## 没做的

窗口太窄时，3D 画面两侧（蜡烛、牌堆）仍会被裁掉。原因是相机使用固定的竖向视角，属于相机问题，留到之后的 3D 工作处理。

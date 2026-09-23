# 占卜房桌面（Phase 68）实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 桌上只显示所选牌阵的牌位，并让放牌的中央成为桌布最亮的地方。

**Architecture:** 运行时新增 `SpreadSocketVisibility`（按牌数分组切换牌位显隐，订阅 `ReadingFlowController` 新增的 `SpreadSelected` 事件）；新的 Phase 68 bootstrapper 负责连线、把场景存为开场状态，并创建聚光灯 `MP_TablePool`；Phase 68 截图脚本按真实机位渲染，供调参和人工对照。

**Tech Stack:** Unity 6000.3.16f1、URP 17（Forward+）、C#；批处理命令行运行 bootstrapper 与截图。

**Spec:** `docs/superpowers/specs/2026-09-23-reading-room-parlor-design.md`

## Global Constraints

- 相机、机位和 UI 布局都不动。
- 不新增贴图，不引入新的外部资源。
- 不修改 Phase 37 / 38 / 49 / 63 的 bootstrapper 源码、共享材质、主菜单与结果页场景。
- `MP_CardSockets` 与 `MP_CelticSockets` 两个组始终保持激活、层级不变；只切换每个 `MP_Socket_*` 自身的显隐。
- 保留 `MP_RoomFill` 原样不动。
- 不新增测试（用户决定 D5）；现有 EditMode 456、PlayMode 75（72 通过 + 3 跳过）、后端 190 必须保持通过。
- 字体图集 `Assets/Fonts/LXGWWenKai-Regular SDF.asset` 与 `LiberationSans SDF - Fallback.asset` 的工作区改动不提交、不还原。
- 同一时间只运行一个 Unity 进程；截图不能用 `-nographics`。
- 提交信息结尾：`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`。

## Review Focus

本期不新增测试（D5），以下情形交由用户在 Unity 中人工检查（对应 spec 第 5 节）：

1. 在「写问题」阶段来回切换牌阵：状态不变，牌位仍要跟着切换——靠 `SpreadSelected` 事件，不能靠 `StateChanged`。
2. 一局结束后回到占卜房（场景重新加载）：应回到只显示单张牌位的开场状态。
3. 牌位组 `MP_CelticSockets` 不能被隐藏，否则 `Phase63SpreadDefinitionTests` 找不到它。
4. 光池下金色（牌位描边、牌堆）不能被 Bloom 糊开。
5. 牌堆在画面左缘，处在光池外沿，不能比现在暗。

## 文件结构

| 文件 | 职责 |
|---|---|
| `Assets/Scripts/Gameplay/ReadingFlowController.cs`（修改） | 新增 `SpreadSelected` 事件 |
| `Assets/Scripts/Presentation/SpreadSocketVisibility.cs`（新增） | 按牌数切换牌位显隐 |
| `Assets/Editor/Phase68TableBootstrapper.cs`（新增） | 挂组件并连线、存为开场状态、创建光池 |
| `Assets/Editor/Phase68ParlorCaptureBuilder.cs`（已新增，修改） | 按真实机位渲染，每个机位先应用对应牌阵的牌位 |
| `Docs/PHASE68_READING_ROOM_TABLE.md`（新增） | 本期说明 |
| `Docs/VisualReview/Phase68/*.png`（新增） | 评审截图 |

以下路径以 `UnityClient/TarotUnity/` 为根，命令中的 `$S` 指会话临时目录 `/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/790b0dfd-6ecb-40b1-a435-0828f97da096/scratchpad`，`$UNITY` 指 `/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity`，`$PROJECT` 指 `/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity`。

---

### Task 1: 牌位按所选牌阵显示（运行时）

**Files:**
- Modify: `Assets/Scripts/Gameplay/ReadingFlowController.cs:28-49`
- Create: `Assets/Scripts/Presentation/SpreadSocketVisibility.cs`

**Interfaces:**
- Produces: `ReadingFlowController.SpreadSelected`（`event Action<int>`，参数为牌数）；`SpreadSocketVisibility.Apply(int cardCount)`；私有序列化字段 `flowController`（`ReadingFlowController`）、`socketSets`（`SpreadSocketSet[]`，每项含 `int cardCount` 与 `GameObject[] sockets`）。

- [ ] **Step 1: 在 `ReadingFlowController` 上加事件**

在 `public event Action<ReadingFlowState> StateChanged;` 下一行加：

```csharp
        /// <summary>
        /// Phase 68: raised on every spread selection. SelectSpread moves the flow to
        /// QuestionInput, but SetState returns early when the state is unchanged, so picking
        /// another spread while writing the question raised nothing.
        /// </summary>
        public event Action<int> SpreadSelected;
```

把 `SelectSpread` 改成：

```csharp
        public void SelectSpread(int spreadId, int cardCount)
        {
            SelectedSpreadId = spreadId;
            SelectedSpreadCardCount = cardCount;
            expectedFlipCount = Mathf.Max(0, cardCount);
            SpreadSelected?.Invoke(cardCount);
            SetState(ReadingFlowState.QuestionInput);
        }
```

- [ ] **Step 2: 新建 `SpreadSocketVisibility.cs`**

```csharp
using System;
using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Presentation
{
    /// <summary>
    /// Phase 68: only the selected spread's card sockets are on the table. Phase 63 added the
    /// ten Celtic sockets beside the four original ones and nothing hid either set, so all
    /// fourteen outlines were always drawn on top of each other.
    /// Sockets are grouped by card count, the way RitualStepIndicator groups their glows.
    /// Only each socket's own active flag is switched: MP_CardSockets and MP_CelticSockets
    /// stay active, because tests find the Celtic group with GameObject.Find, which skips
    /// inactive objects.
    /// </summary>
    public sealed class SpreadSocketVisibility : MonoBehaviour
    {
        [Serializable]
        public sealed class SpreadSocketSet
        {
            public int cardCount;
            public GameObject[] sockets = Array.Empty<GameObject>();
        }

        [SerializeField] private ReadingFlowController flowController;
        [SerializeField] private SpreadSocketSet[] socketSets = Array.Empty<SpreadSocketSet>();

        private void OnEnable()
        {
            if (flowController == null)
            {
                return;
            }

            flowController.SpreadSelected += Apply;
            Apply(flowController.SelectedSpreadCardCount);
        }

        private void OnDisable()
        {
            if (flowController != null)
            {
                flowController.SpreadSelected -= Apply;
            }
        }

        /// <summary>Shows the sockets of the spread with this many cards and hides every other set.</summary>
        public void Apply(int cardCount)
        {
            foreach (var set in socketSets)
            {
                if (set == null)
                {
                    continue;
                }

                var show = set.cardCount == cardCount;
                foreach (var socket in set.sockets)
                {
                    if (socket != null && socket.activeSelf != show)
                    {
                        socket.SetActive(show);
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 3: 编译检查**

Task 2 的 bootstrapper 批处理运行会编译整个工程；这里不单独跑。继续 Task 2。

### Task 2: Phase 68 bootstrapper（连线、开场状态、光池）

**Files:**
- Create: `Assets/Editor/Phase68TableBootstrapper.cs`
- Modify（由 bootstrapper 写入）: `Assets/Scenes/ReadingRoom.unity`

**Interfaces:**
- Consumes: Task 1 的 `SpreadSocketVisibility`（私有字段名 `flowController`、`socketSets`，`SpreadSocketSet` 的 `cardCount`、`sockets`）与 `Apply(int)`。
- Produces: 场景中 `MP_TableStage` 上的 `SpreadSocketVisibility` 组件；`MP_TableStage/MP_TablePool` 聚光灯；公开常量 `Phase68TableBootstrapper.PoolName`。

- [ ] **Step 1: 新建 bootstrapper**

```csharp
using System;
using System.Collections.Generic;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 68 stages the reading-room table for the poses the player actually sits at.
    /// 1. Only the selected spread's sockets show (SpreadSocketVisibility). The scene is saved
    ///    in the state the room opens in - one card selected - so the editor matches the game.
    /// 2. MP_TablePool, a soft spot straight above the card row, makes the play area the
    ///    brightest part of the cloth. It reads as a lamp hanging above the table, out of
    ///    frame: a deliberate, single exception to Phase 49's candles-only room, which removed
    ///    seven lights that washed the table flat. MP_RoomFill stays as it is; it is what lets
    ///    gold read as gold at the edges of the frame, the deck included.
    /// Re-runnable. Running Phase 49 again does not touch MP_TablePool.
    /// </summary>
    public static class Phase68TableBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string PoolName = "MP_TablePool";

        // Spec section 4 starting values; tuned against the Phase 68 captures.
        private static readonly Vector3 PoolPosition = new Vector3(0f, 4.0f, 0.3f);
        private const float PoolInnerAngle = 40f;
        private const float PoolOuterAngle = 75f;
        private const float PoolRange = 8f;
        private const float PoolIntensity = 12f;
        private static readonly Color PoolColor = new Color(1f, 0.8f, 0.58f, 1f);

        private static readonly (int CardCount, string[] Sockets)[] SocketSets =
        {
            (1, new[] { "MP_Socket_OneCardSlot" }),
            (3, new[] { "MP_Socket_PastSlot", "MP_Socket_PresentSlot", "MP_Socket_AdviceSlot" }),
            (10, new[]
            {
                "MP_Socket_Celtic_00", "MP_Socket_Celtic_01", "MP_Socket_Celtic_02", "MP_Socket_Celtic_03",
                "MP_Socket_Celtic_04", "MP_Socket_Celtic_05", "MP_Socket_Celtic_06", "MP_Socket_Celtic_07",
                "MP_Socket_Celtic_08", "MP_Socket_Celtic_09",
            }),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 68 Table Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var stage = GameObject.Find("MP_TableStage");
            if (stage == null)
            {
                throw new InvalidOperationException("MP_TableStage is missing - run the Phase 38 bootstrapper first.");
            }

            WireSocketVisibility(scene, stage);
            StageTablePool(stage);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 68 table bootstrap complete.");
        }

        private static void WireSocketVisibility(Scene scene, GameObject stage)
        {
            var flow = UnityEngine.Object.FindFirstObjectByType<ReadingFlowController>();
            if (flow == null)
            {
                throw new InvalidOperationException("No ReadingFlowController in the reading room.");
            }

            var byName = IndexScene(scene);
            var visibility = stage.GetComponent<SpreadSocketVisibility>();
            if (visibility == null)
            {
                visibility = stage.AddComponent<SpreadSocketVisibility>();
            }

            var so = new SerializedObject(visibility);
            so.FindProperty("flowController").objectReferenceValue = flow;
            var sets = so.FindProperty("socketSets");
            sets.arraySize = SocketSets.Length;
            for (var i = 0; i < SocketSets.Length; i++)
            {
                var (cardCount, names) = SocketSets[i];
                var element = sets.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("cardCount").intValue = cardCount;
                var sockets = element.FindPropertyRelative("sockets");
                sockets.arraySize = names.Length;
                for (var j = 0; j < names.Length; j++)
                {
                    if (!byName.TryGetValue(names[j], out var socket))
                    {
                        throw new InvalidOperationException($"{names[j]} is missing from the reading room.");
                    }

                    sockets.GetArrayElementAtIndex(j).objectReferenceValue = socket;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // Save the scene the way the room opens: ReadingRoomController.Start selects one card.
            visibility.Apply(1);
            EditorUtility.SetDirty(visibility);
        }

        // Sockets hidden by an earlier run are inactive, and GameObject.Find skips inactive objects.
        private static Dictionary<string, GameObject> IndexScene(Scene scene)
        {
            var byName = new Dictionary<string, GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!byName.ContainsKey(t.name))
                    {
                        byName[t.name] = t.gameObject;
                    }
                }
            }

            return byName;
        }

        private static void StageTablePool(GameObject stage)
        {
            var existing = stage.transform.Find(PoolName);
            var go = existing != null ? existing.gameObject : new GameObject(PoolName);
            go.transform.SetParent(stage.transform, false);
            go.transform.localPosition = PoolPosition;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var light = go.GetComponent<Light>();
            if (light == null)
            {
                light = go.AddComponent<Light>();
            }

            light.type = LightType.Spot;
            light.innerSpotAngle = PoolInnerAngle;
            light.spotAngle = PoolOuterAngle;
            light.range = PoolRange;
            light.intensity = PoolIntensity;
            light.color = PoolColor;
            light.shadows = LightShadows.None;
            EditorUtility.SetDirty(light);
            EditorUtility.SetDirty(go);
        }
    }
}
```

- [ ] **Step 2: 运行 bootstrapper**

```bash
LOG="$S/p68-bootstrap-1.log"
ulimit -n 10240 2>/dev/null || true
"$UNITY" -projectPath "$PROJECT" -batchmode -nographics -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase68TableBootstrapper.Run -quit -logFile "$LOG"
echo "exit=$?"; grep -E "Phase 68|error CS|Exception" "$LOG" | head
```

Expected: `exit=0`，日志含 `Tarot Unity Phase 68 table bootstrap complete.`，无 `error CS`。

- [ ] **Step 3: 核对场景改动**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
git diff --stat -- UnityClient/TarotUnity/Assets/Scenes/ReadingRoom.unity
grep -c "m_Name: MP_TablePool" UnityClient/TarotUnity/Assets/Scenes/ReadingRoom.unity
```

Expected: 场景有改动；`MP_TablePool` 出现 1 次。

### Task 3: 按真实机位渲染并调光池

**Files:**
- Modify: `Assets/Editor/Phase68ParlorCaptureBuilder.cs`
- Modify（调参时）: `Assets/Editor/Phase68TableBootstrapper.cs` 的 `PoolIntensity` 等常量
- Create: `Docs/VisualReview/Phase68/*.png`

**Interfaces:**
- Consumes: `SpreadSocketVisibility.Apply(int)`（Task 1）。

- [ ] **Step 1: 截图脚本按机位应用牌位**

在 `Run()` 的 `try` 块开头取组件：

```csharp
                var sockets = UnityEngine.Object.FindFirstObjectByType<SpreadSocketVisibility>();
```

渲染 `ReadingRoom_scenecamera.png` 之前加 `sockets?.Apply(1);`。在 `foreach` 循环里、设置相机之后加：

```csharp
                    sockets?.Apply(CardCountFor(label));
```

把 `if (label == "default")` 那段改成同时给三牌机位出无 UI 图：

```csharp
                    if (label == "default" || label == "threeCard")
                    {
                        RenderToFile(camera, $"ReadingRoom_{label}_noui.png", false);
                    }
```

在 `finally` 里恢复相机之后加 `UnityEngine.Object.FindFirstObjectByType<SpreadSocketVisibility>()?.Apply(1);`，并在类中新增：

```csharp
        // The spread each pose is seen with in play: the room opens on one card, the three-card
        // and result poses follow a three-card draw, and a spread pose shows its own sockets.
        private static int CardCountFor(string label)
        {
            if (label.StartsWith("spread", StringComparison.Ordinal)
                && int.TryParse(label.Substring("spread".Length), out var count))
            {
                return count;
            }

            return label == "threeCard" || label == "result" ? 3 : 1;
        }
```

文件顶部 `using` 已有 `System` 与 `TarotUnity.Presentation`，不需要新增。

- [ ] **Step 2: 渲染第 1 轮到临时目录**

```bash
OUT="$S/p68-round1"; mkdir -p "$OUT"
ulimit -n 10240 2>/dev/null || true
PHASE68_CAPTURE_DIR="$OUT" "$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase68ParlorCaptureBuilder.Run -quit -logFile "$OUT/capture.log"
echo "exit=$?"; grep -E "Phase 68|error CS|Exception" "$OUT/capture.log" | head
```

Expected: `exit=0`，六个机位各一张，外加 `scenecamera`、`default_noui`、`threeCard_noui`，共 9 张。

- [ ] **Step 3: 对照 `$S/p68-before/` 看图并调参**

逐张对照同名的调整前截图，按下面的规则判断，最多调 3 轮：

| 看到的情况 | 调整 |
|---|---|
| 桌上出现不属于该机位牌阵的牌位 | 停下排查 Task 1 / Task 2，不调光 |
| 中央桌布仍接近纯黑 | `PoolIntensity` × 1.5 |
| 牌位描边或牌堆金色出现泛光光晕 | `PoolIntensity` × 0.7 |
| 光池边缘生硬，看得出一个圆 | `PoolInnerAngle` 减 10 |
| 牌堆比调整前暗 | `PoolPosition.x` 向牌堆方向（负）移 0.4 |

每轮改完常量后，重跑 Task 2 Step 2 的 bootstrapper，再用新的目录名（`p68-round2`、`p68-round3`）重跑本任务 Step 2。

- [ ] **Step 4: 渲染最终截图到 Docs**

```bash
ulimit -n 10240 2>/dev/null || true
"$UNITY" -projectPath "$PROJECT" -batchmode -enableUnityConnectPrefs false \
  -executeMethod TarotUnity.Editor.Phase68ParlorCaptureBuilder.Run -quit -logFile "$S/p68-final-capture.log"
echo "exit=$?"; ls /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Docs/VisualReview/Phase68/
```

Expected: `exit=0`，`Docs/VisualReview/Phase68/` 下 9 张图（均为新文件）。

### Task 4: 文档、回归与提交

**Files:**
- Create: `Docs/PHASE68_READING_ROOM_TABLE.md`

- [ ] **Step 1: 写本期说明**

内容包括：为什么以前的截图不是玩家看到的画面（真实机位与保存位置的差异）；牌位按牌阵显示的做法与两条层级约束；光池参数（写最终值）与「画面外吊灯」这一对 Phase 49 原则的有意例外；执行顺序（Phase 68 在 Phase 38 / 49 / 63 之后，可重复运行）；截图脚本用法与 `PHASE68_CAPTURE_DIR`。

- [ ] **Step 2: 全量回归**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
R=.superpowers/sdd/2026-09-12-online-interpretation-unity/scratch/online-b-run/ut.sh
bash $R EditMode p68-edit && bash $R PlayMode p68-play
bash .superpowers/sdd/2026-09-12-online-interpretation-unity/scratch/online-a-run/pt.sh tests | tail -1
```

Expected: EditMode `total=456 passed=456`；PlayMode `total=75 passed=72 skipped=3`；后端 `190 passed`。任何失败都先排查，不改断言。

- [ ] **Step 3: 提交（分两次）**

先提交运行时、bootstrapper 与场景：

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot
U=UnityClient/TarotUnity
git add $U/Assets/Scripts/Gameplay/ReadingFlowController.cs \
  $U/Assets/Scripts/Presentation/SpreadSocketVisibility.cs $U/Assets/Scripts/Presentation/SpreadSocketVisibility.cs.meta \
  $U/Assets/Editor/Phase68TableBootstrapper.cs $U/Assets/Editor/Phase68TableBootstrapper.cs.meta \
  $U/Assets/Scenes/ReadingRoom.unity
git commit -m "feat(unity): show only the selected spread's sockets and light the play area"
```

再提交截图脚本、截图与文档：

```bash
git add $U/Assets/Editor/Phase68ParlorCaptureBuilder.cs $U/Assets/Editor/Phase68ParlorCaptureBuilder.cs.meta \
  $U/Docs/PHASE68_READING_ROOM_TABLE.md $U/Docs/VisualReview/Phase68
git commit -m "docs(unity): capture the reading room from its real poses"
git status --porcelain=v1 -uall
```

Expected: 两次提交的信息都以 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` 结尾；`git status` 只剩两个字体资产的改动。

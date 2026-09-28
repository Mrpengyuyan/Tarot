# 抽牌仪式（Phase 72）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让玩家亲手抽牌：三拍洗牌 → 22 张弧形扇面 → 玩家逐张点选 → 每张抽出、悬空、带光轨落到牌位 → 扇面收拢 → 绑定牌面 → 进入原有翻牌流程。

**Architecture:** 新增 `SpreadFanController` 管扇面（生成、摊开、悬停波浪、点选排队、收拢）。`DeckController` 新增「把选中的牌送到牌位」的四拍动作、绑定与退回。`DeckShuffleChoreographer` 重写为三拍。`ReadingRoomController.DrawRoutine` 改为「洗牌 → 摊牌 → 选牌（后台开局并行）→ 收拢 → 等开局 → 绑定 → 翻牌」。场景接线全部由可重复运行的 `Phase72DrawRitualBootstrapper` 完成。

**Tech Stack:** Unity 6000.3.16f1、URP、C#、Unity Test Framework（EditMode 与 PlayMode）、uGUI、TMP。

**Spec:** `docs/superpowers/specs/2026-09-28-draw-ritual-design.md`

## Global Constraints

- 所有项目路径相对 `UnityClient/TarotUnity/`。命令中的 `$W` = `/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-28-draw-ritual`。
- 批量跑 Unity（测试、引导、截图）前，用户的 Unity 编辑器必须关闭。`ut.sh` 检测到 Unity 在运行时会拒绝执行。遇到这种情况：停下，请用户关闭 Unity。
- 运行引导或截图时不加 `-nographics`；跑测试时加（`ut.sh` 已内置）。
- 不新增 `ReadingFlowState`，`Drawing` 表示「选牌 + 飞牌」。
- 扇面固定 22 张，总张角约 70°。
- 飞行段必须满足 Phase 9 下限：`dealDuration ≥ 0.52`，场景当前值 0.56，保持不变。
- 洗牌总时长约 1.8 秒（测试区间 1.5–2.2 秒）。结束后牌堆亚毫米回位（Phase 55 PlayMode 断言不改）。
- 不新增音频素材；不改翻牌、结果页、后端接口。
- Unity 假 null：`GetComponent` 结果永远不用 `??`，要写显式 `if (x == null)`。
- `GameObject.Find` 找不到 inactive 对象；需要隐藏的 UI 用 `CanvasGroup` alpha，不用 `SetActive(false)`。
- 字体图集 `LXGWWenKai-Regular SDF.asset` 与 `LiberationSans SDF - Fallback.asset` 永不提交、永不还原。
- 不删除任何文件（用户规则）。
- 提交信息结尾：`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`。
- 与用户沟通一律中文。

## Review Focus

1. **扇面悬停抖动：** 悬停的牌朝玩家滑出后，指针可能落到别的牌或空处，导致 enter/exit 来回触发，牌来回抽动。期望：有离开延迟（`hoverReleaseDelay`），指针停在一处时牌稳定不动。Task 3 的 PlayMode 测试覆盖「exit 后在延迟内再 enter 同一张，不回落」。
2. **飞行中连点：** 飞行中连点三次，期望只排队第一次，不会有两张牌同时飞、也不会超过 N 张。Task 3 测试 `ExtraClicksWhileBusyAreDroppedAfterOneQueuedPick`。
3. **BackendOnly 失败后重抽：** 旧代码失败后流程停在 `Shuffling`，再按「洗牌抽取」时 `BeginShuffle` 直接返回，无法重抽。期望：失败后可以立即再抽一次。Task 7 测试 `BackendOnlyFailureReturnsCardsAndAllowsARetry`。
4. **窄屏（4:3）取景：** 扇面两端或凯尔特第 7–10 位被裁掉。期望：1、3、10 张牌阵在 16:9 与 4:3 下全部在画面内。Task 6 的 EditMode 视锥测试覆盖。
5. **扇面牌的悬停倾斜与扇面位移互相冲突：** `CardHoverTiltController` 捕获静止位姿后每帧写 transform，会把扇面牌拉回旧位置。期望：扇面牌的倾斜被暂停，落位后恢复。Task 2 测试 `ResumeLetsTheTiltRecaptureItsRest`，加上 Task 4 测试检查落位后倾斜恢复。

---

## File Structure

| 文件 | 动作 | 职责 |
|---|---|---|
| `Assets/Scripts/Presentation/DeckShuffleChoreographer.cs` | 改 | 三拍洗牌（切牌 → 两次交错搓洗 → 方齐），接口不变 |
| `Assets/Scripts/Gameplay/CardView.cs` | 改 | 可调悬停光晕倍数、光晕增益（悬空时的高光） |
| `Assets/Scripts/Gameplay/CardClickHandler.cs` | 改 | 新增 `HoverChanged` 事件 |
| `Assets/Scripts/Gameplay/CardHoverTiltController.cs` | 改 | 新增 `Resume()` |
| `Assets/Scripts/Gameplay/SpreadFanController.cs` | 新 | 扇面：几何、摊开、波浪、点选排队、收拢 |
| `Assets/Scripts/Gameplay/DeckController.cs` | 改 | `DealPickedCard` / `BindDealtCards` / `ReturnDealtCards` |
| `Assets/Scripts/Gameplay/ReadingFlowController.cs` | 改 | `AbortDraw()` |
| `Assets/Scripts/Presentation/CameraChoreographyController.cs` | 改 | 选牌镜头 `FocusDraw(cardCount)` |
| `Assets/Scripts/UI/RitualStepIndicator.cs` | 改 | 只亮第 k 个牌位、闪一下 |
| `Assets/Scripts/UI/ReadingRoomController.cs` | 改 | 新 `DrawRoutine`，选牌时隐藏动作坞 |
| `Assets/Scripts/UI/ReleaseUxCopy.cs` | 改 | 选牌文案 |
| `Assets/Editor/Phase72DrawRitualBootstrapper.cs` | 新 | 场景与预制体接线，可重复运行 |
| `Assets/Editor/Phase72CaptureBuilder.cs` | 新 | 验收截图 |
| `Assets/Tests/EditMode/Phase72DrawRitualTests.cs` | 新 | 配置、几何、视锥 |
| `Assets/Tests/PlayMode/Phase72ShuffleTests.cs` | 新 | 洗牌时长与分叠 |
| `Assets/Tests/PlayMode/Phase72FanTests.cs` | 新 | 扇面、点选排队、悬停 |
| `Assets/Tests/PlayMode/Phase72DrawFlowTests.cs` | 新 | 全流程、同一实例、BackendOnly 失败、凯尔特 |
| `Assets/Tests/PlayMode/DrawRitualTestDriver.cs` | 新 | 测试工具：替玩家点选直到发满 |
| 若干旧 PlayMode 测试 | 改 | 点「洗牌抽取」后调用 driver 完成点选 |
| `Docs/PHASE72_DRAW_RITUAL.md` | 新 | 本期说明 |

---

### Task 0: 工作区

- [ ] **Step 1: 建工作区并复制测试脚本**

```bash
W=/Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-28-draw-ritual
mkdir -p $W/run
cp /Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-27-ui-polish-p70/run/ut.sh $W/run/
cp /Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-27-ui-polish-p70/run/summarize.py $W/run/
echo "# SDD ledger — plan: docs/superpowers/plans/2026-09-28-draw-ritual.md" > $W/progress.md
git -C /Users/maochuandou/BUPT/Game/UnityTarot branch --show-current
```

Expected: 输出 `feat/draw-ritual`。

---

### Task 1: 三拍洗牌

**Files:**
- Modify: `Assets/Scripts/Presentation/DeckShuffleChoreographer.cs`
- Create: `Assets/Tests/PlayMode/Phase72ShuffleTests.cs`
- Create: `Assets/Tests/EditMode/Phase72DrawRitualTests.cs`（本任务只放洗牌参数测试，后续任务往里加）

**Interfaces:**
- Produces: `DeckShuffleChoreographer.Play()`、`IsPlaying`（不变）、`public float PlannedSeconds { get; }`（新：按当前参数和子物数算出的总时长）。

- [ ] **Step 1: 写失败的 PlayMode 测试**

`Assets/Tests/PlayMode/Phase72ShuffleTests.cs`：

```csharp
using System.Collections;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>Phase 72: the shuffle is three beats - cut, two riffles, square - about 1.8 s.</summary>
    public sealed class Phase72ShuffleTests
    {
        [UnityTest]
        public IEnumerator ShuffleCutsTheDeckIntoTwoPilesAndLastsAboutOnePointEightSeconds()
        {
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            var choreographer = Object.FindFirstObjectByType<DeckShuffleChoreographer>();
            var stack = choreographer.transform;
            var minX = float.MaxValue;
            var maxX = float.MinValue;

            var started = Time.time;
            choreographer.Play();
            while (choreographer.IsPlaying && Time.time - started < 10f)
            {
                foreach (Transform card in stack)
                {
                    minX = Mathf.Min(minX, card.localPosition.x);
                    maxX = Mathf.Max(maxX, card.localPosition.x);
                }

                yield return null;
            }

            var seconds = Time.time - started;
            Assert.That(seconds, Is.InRange(1.5f, 2.2f), "three beats, not a single shiver");
            Assert.That(minX, Is.LessThan(-0.15f), "one pile goes left");
            Assert.That(maxX, Is.GreaterThan(0.15f), "the other goes right");
            Assert.That(choreographer.PlannedSeconds, Is.InRange(1.5f, 2.2f));
        }
    }
}
```

- [ ] **Step 2: 写失败的 EditMode 参数测试**

`Assets/Tests/EditMode/Phase72DrawRitualTests.cs`：

```csharp
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 72: the draw ritual - three-beat shuffle, a fan the player picks from, picked cards fly to their slots.</summary>
    public sealed class Phase72DrawRitualTests
    {
        [Test]
        public void ShuffleHasCutAndRiffleKnobsInATastefulEnvelope()
        {
            var probe = new GameObject("Phase72_StackProbe");
            try
            {
                var so = new SerializedObject(probe.AddComponent<DeckShuffleChoreographer>());
                Assert.That(so.FindProperty("cutSpread")?.floatValue, Is.InRange(0.2f, 0.6f), "piles clear each other");
                Assert.That(so.FindProperty("cutSeconds")?.floatValue, Is.InRange(0.2f, 0.45f));
                Assert.That(so.FindProperty("recutSeconds")?.floatValue, Is.InRange(0.1f, 0.3f));
                Assert.That(so.FindProperty("cutYawDegrees")?.floatValue, Is.InRange(2f, 12f));
                Assert.That(so.FindProperty("riffleBendDegrees")?.floatValue, Is.InRange(4f, 20f), "inner edges lift");
                Assert.That(so.FindProperty("secondRiffleSpeedup")?.floatValue, Is.InRange(1.05f, 1.5f));
                Assert.That(so.FindProperty("squareHoldSeconds")?.floatValue, Is.InRange(0.05f, 0.3f));
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }
    }
}
```

- [ ] **Step 3: 跑测试确认失败**

```bash
bash $W/run/ut.sh EditMode t1-red -testFilter Phase72DrawRitualTests
bash $W/run/ut.sh PlayMode t1-red -testFilter Phase72ShuffleTests
```

Expected: EditMode 编译失败或断言失败（`cutSpread` 为 null）；PlayMode 因 `PlannedSeconds` 不存在编译失败。两个测试文件同在一个程序集，编译错误也算 RED。

- [ ] **Step 4: 重写 `DeckShuffleChoreographer`**

保留字段 `anticipationDip`、`anticipationSeconds`、`riffleLift`、`riffleCardSeconds`、`riffleStagger`、`riffleYawDegrees`、`contactSquash`、`settleSeconds`、`contactCameraKick`（Phase 55 测试引用它们的取值区间），并修改默认值与注释含义。完整替换类体中 `[Header("Riffle")]` 到文件末尾的部分：

```csharp
        [Header("Phase72 Cut")]
        [Tooltip("How far each pile moves sideways from the stack centre, in the stack's local units.")]
        [SerializeField] private float cutSpread = 0.42f;
        [Tooltip("Seconds the first cut takes to part the deck.")]
        [SerializeField] private float cutSeconds = 0.33f;
        [Tooltip("Seconds the second cut takes - quicker, the hands know the move now.")]
        [SerializeField] private float recutSeconds = 0.2f;
        [Tooltip("Opposite yaw each pile takes as it is held apart.")]
        [SerializeField] private float cutYawDegrees = 6f;

        [Header("Riffle")]
        [Tooltip("How high each pile rides while held apart.")]
        [SerializeField] private float riffleLift = 0.045f;
        [Tooltip("Seconds one card takes to fall from its pile onto the stack.")]
        [SerializeField] private float riffleCardSeconds = 0.14f;
        [Tooltip("Delay between cards falling - left and right piles alternate.")]
        [SerializeField] private float riffleStagger = 0.035f;
        [Tooltip("Yaw shiver as each card falls.")]
        [SerializeField] private float riffleYawDegrees = 4f;
        [Tooltip("How far the piles' inner edges lift before the cards fall (roll about the long axis).")]
        [SerializeField] private float riffleBendDegrees = 10f;
        [Tooltip("The second riffle runs this much faster than the first.")]
        [SerializeField] private float secondRiffleSpeedup = 1.25f;

        [Header("Contact and settle")]
        [Tooltip("Squash on the frame the deck squares up (0.06 = -6% height).")]
        [SerializeField] private float contactSquash = 0.06f;
        [Tooltip("Seconds the squash takes to spring back to an exact rest.")]
        [SerializeField] private float settleSeconds = 0.15f;
        [Tooltip("A held beat after the square-up so the deck reads as ready before the fan.")]
        [SerializeField] private float squareHoldSeconds = 0.18f;
        [Tooltip("Camera shake on the contact frame. Quieter than the flip's reveal - the shuffle is a prelude.")]
        [SerializeField] private float contactCameraKick = 0.03f;

        private readonly List<Transform> cards = new();
        private readonly List<Vector3> restPositions = new();
        private readonly List<Quaternion> restRotations = new();
        private Vector3 restRootPosition;
        private Vector3 restRootScale;
        private bool restCaptured;
        private Coroutine active;
        private CameraChoreographyController cameraChoreography;

        private CameraChoreographyController CameraChoreography
        {
            get
            {
                if (cameraChoreography == null)
                {
                    cameraChoreography = FindFirstObjectByType<CameraChoreographyController>();
                }

                return cameraChoreography;
            }
        }

        public bool IsPlaying => active != null;

        /// <summary>The whole shuffle's length for the current knobs and stack size.</summary>
        public float PlannedSeconds
        {
            get
            {
                var n = Mathf.Max(1, transform.childCount);
                var riffle = riffleCardSeconds + riffleStagger * (n - 1);
                return anticipationSeconds + cutSeconds + riffle
                    + recutSeconds + riffle / secondRiffleSpeedup
                    + settleSeconds + squareHoldSeconds;
            }
        }

        public void Play()
        {
            // The childCount guard keeps ShuffleRoutine from completing synchronously
            // (its empty-stack yield break would run before `active` is assigned,
            // leaving IsPlaying stuck true forever).
            if (active == null && isActiveAndEnabled && transform.childCount > 0)
            {
                active = StartCoroutine(ShuffleRoutine());
            }
        }

        private void CaptureRestPose()
        {
            if (restCaptured)
            {
                return;
            }

            cards.Clear();
            restPositions.Clear();
            restRotations.Clear();
            foreach (Transform child in transform)
            {
                cards.Add(child);
            }

            // Bottom to top: the riffle drops cards in this order, so each lands on its own rest.
            cards.Sort((a, b) => a.localPosition.y.CompareTo(b.localPosition.y));
            foreach (var card in cards)
            {
                restPositions.Add(card.localPosition);
                restRotations.Add(card.localRotation);
            }

            restRootPosition = transform.localPosition;
            restRootScale = transform.localScale;
            restCaptured = true;
        }

        /// <summary>
        /// Even cards (from the bottom) form the left pile, odd cards the right. The riffle
        /// then drops them bottom-to-top, which alternates left and right, and every card
        /// lands exactly on its own authored rest - the interleave costs no re-ordering.
        /// </summary>
        private void PilePose(int i, out Vector3 position, out Quaternion rotation)
        {
            var left = i % 2 == 0;
            var side = left ? -1f : 1f;
            var baseY = restPositions[0].y;
            var step = cards.Count > 1 ? (restPositions[cards.Count - 1].y - baseY) / (cards.Count - 1) : 0f;
            var heightInPile = i / 2;
            position = new Vector3(side * cutSpread, baseY + riffleLift + heightInPile * step, restPositions[i].z);
            rotation = Quaternion.AngleAxis(-side * cutYawDegrees, Vector3.up)
                * Quaternion.AngleAxis(side * riffleBendDegrees, Vector3.forward)
                * restRotations[i];
        }

        private IEnumerator ShuffleRoutine()
        {
            CaptureRestPose();
            if (cards.Count == 0)
            {
                active = null;
                yield break;
            }

            // Beat 1a - anticipation: the stack presses down, easing out into the pose.
            for (var elapsed = 0f; elapsed < anticipationSeconds; elapsed += Time.deltaTime)
            {
                transform.localPosition = restRootPosition + Vector3.down * (anticipationDip * EaseOut(elapsed / anticipationSeconds));
                yield return null;
            }

            transform.localPosition = restRootPosition;
            yield return Cut(cutSeconds);
            yield return Riffle(1f);
            yield return Cut(recutSeconds);
            yield return Riffle(secondRiffleSpeedup);

            // Beat 3 - square: the deck squares up. The kick lands on this frame.
            for (var i = 0; i < cards.Count; i++)
            {
                cards[i].localPosition = restPositions[i];
                cards[i].localRotation = restRotations[i];
            }

            CameraChoreography?.Kick(contactCameraKick);
            var squashed = new Vector3(
                restRootScale.x * (1f + contactSquash * 0.4f),
                restRootScale.y * (1f - contactSquash),
                restRootScale.z * (1f + contactSquash * 0.4f));
            for (var elapsed = 0f; elapsed < settleSeconds; elapsed += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(squashed, restRootScale, EaseOut(elapsed / settleSeconds));
                yield return null;
            }

            transform.localScale = restRootScale;
            transform.localPosition = restRootPosition;
            for (var elapsed = 0f; elapsed < squareHoldSeconds; elapsed += Time.deltaTime)
            {
                yield return null;
            }

            active = null;
        }

        /// <summary>Beat 2a - the cut: every card eases from its rest into its pile.</summary>
        private IEnumerator Cut(float seconds)
        {
            var from = new Vector3[cards.Count];
            var fromRot = new Quaternion[cards.Count];
            for (var i = 0; i < cards.Count; i++)
            {
                from[i] = cards[i].localPosition;
                fromRot[i] = cards[i].localRotation;
            }

            for (var elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
            {
                var k = EaseInOut(elapsed / seconds);
                for (var i = 0; i < cards.Count; i++)
                {
                    PilePose(i, out var p, out var r);
                    cards[i].localPosition = Vector3.Lerp(from[i], p, k);
                    cards[i].localRotation = Quaternion.Slerp(fromRot[i], r, k);
                }

                yield return null;
            }
        }

        /// <summary>
        /// Beat 2b - the riffle: bottom to top, each card drops from its pile onto its rest,
        /// alternating left and right, with a small yaw shiver.
        /// </summary>
        private IEnumerator Riffle(float speed)
        {
            var cardSeconds = riffleCardSeconds / speed;
            var stagger = riffleStagger / speed;
            var total = cardSeconds + stagger * (cards.Count - 1);
            for (var elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
            {
                for (var i = 0; i < cards.Count; i++)
                {
                    var phase = Mathf.Clamp01((elapsed - i * stagger) / Mathf.Max(0.01f, cardSeconds));
                    PilePose(i, out var p, out var r);
                    var fall = EaseIn(phase);
                    cards[i].localPosition = Vector3.Lerp(p, restPositions[i], fall);
                    var shiver = Mathf.Sin(phase * Mathf.PI) * riffleYawDegrees;
                    cards[i].localRotation = Quaternion.Slerp(r, restRotations[i], fall) * Quaternion.AngleAxis(shiver, Vector3.up);
                }

                yield return null;
            }

            for (var i = 0; i < cards.Count; i++)
            {
                cards[i].localPosition = restPositions[i];
                cards[i].localRotation = restRotations[i];
            }
        }

        private static float EaseIn(float t) => t * t;

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        private static float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
    }
}
```

同时把类顶部的 `<summary>` 改为描述三拍洗牌（切牌 → 两次交错搓洗 → 方齐，结束精确回位），保留 Phase 55 的历史说明一句。

检查：9 张子物时，`PlannedSeconds` = 0.12 + 0.33 + (0.14 + 0.035×8 = 0.42) + 0.2 + 0.336 + 0.15 + 0.18 ≈ 1.74 秒，落在 1.5–2.2 区间内。

- [ ] **Step 5: 跑测试确认通过，并跑 Phase 55 回归**

```bash
bash $W/run/ut.sh EditMode t1-green -testFilter "Phase72DrawRitualTests|Phase55ShuffleFeelTests|Phase9MotionAudioRhythmTests"
bash $W/run/ut.sh PlayMode t1-green -testFilter "Phase72ShuffleTests|Phase55ShuffleFeelTests"
```

Expected: 全部 PASS。Phase 55 PlayMode 要求 `maxLift > 0.02`：两叠牌抬高 `riffleLift` = 0.045，满足。

- [ ] **Step 6: 提交**

```bash
git add Assets/Scripts/Presentation/DeckShuffleChoreographer.cs Assets/Tests/PlayMode/Phase72ShuffleTests.cs* Assets/Tests/EditMode/Phase72DrawRitualTests.cs*
git commit -m "feat(fx): shuffle in three beats - cut, two riffles, square (Phase 72)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: 牌上的钩子（光晕倍数与增益、悬停事件、倾斜恢复）

**Files:**
- Modify: `Assets/Scripts/Gameplay/CardView.cs`
- Modify: `Assets/Scripts/Gameplay/CardClickHandler.cs`
- Modify: `Assets/Scripts/Gameplay/CardHoverTiltController.cs`
- Test: `Assets/Tests/EditMode/Phase72DrawRitualTests.cs`

**Interfaces:**
- Produces:
  - `CardView.HoverHaloScale { get; set; }`（float，默认取序列化的 `hoverHaloScale` = 1.45）
  - `CardView.SetHaloBoost(float multiplier)`（1 = 无增益；> 1 时光晕放大，并且即使没有等待或悬停也显示）
  - `CardClickHandler.HoverChanged`：`event Action<CardView, bool>`
  - `CardHoverTiltController.Resume()`：清除暂停，下次悬停时重新捕获静止位姿

- [ ] **Step 1: 写失败的测试**（追加到 `Phase72DrawRitualTests`，并加上 `using TarotUnity.Gameplay;`、`using UnityEngine.EventSystems;`）

```csharp
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        private static CardView SpawnCard()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<CardView>(CardPrefabPath);
            var card = Object.Instantiate(prefab);
            card.SetFaceUp(false);
            return card;
        }

        private static Transform Halo(CardView card)
        {
            foreach (var t in card.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Highlight")
                {
                    return t;
                }
            }

            return null;
        }

        [Test]
        public void HoverHaloScaleIsAdjustableForTheFan()
        {
            var card = SpawnCard();
            try
            {
                var halo = Halo(card);
                card.SetHighlighted(true);
                var rest = halo.localScale;
                card.HoverHaloScale = 1f;
                card.SetHovered(true);
                Assert.That(halo.localScale.x, Is.EqualTo(rest.x).Within(1e-4f), "a fan card's hover keeps the rest halo");
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void HaloBoostShowsAndWidensTheGlow()
        {
            var card = SpawnCard();
            try
            {
                var halo = Halo(card);
                card.SetHighlighted(true);
                var rest = halo.localScale;
                card.SetHighlighted(false);
                Assert.That(halo.gameObject.activeSelf, Is.False, "control: no glow at rest");

                card.SetHaloBoost(1.6f);
                Assert.That(halo.gameObject.activeSelf, Is.True, "a boost lights the glow on its own");
                Assert.That(halo.localScale.x, Is.EqualTo(rest.x * 1.6f).Within(1e-3f));

                card.SetHaloBoost(1f);
                Assert.That(halo.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void ClickHandlerReportsHoverChanges()
        {
            var card = SpawnCard();
            try
            {
                var handler = card.GetComponent<CardClickHandler>();
                typeof(CardClickHandler).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(handler, null);
                var seen = new System.Collections.Generic.List<bool>();
                handler.HoverChanged += (_, on) => seen.Add(on);
                handler.OnPointerEnter(null);
                handler.OnPointerExit(null);
                Assert.That(seen, Is.EqualTo(new[] { true, false }));
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void ResumeLetsTheTiltRecaptureItsRest()
        {
            var card = SpawnCard();
            try
            {
                var tilt = card.GetComponent<CardHoverTiltController>();
                typeof(CardHoverTiltController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(tilt, null);
                tilt.Suspend();
                tilt.HoverEnter();
                Assert.That(tilt.IsHovering, Is.False, "control: suspended");

                card.transform.position = new Vector3(1f, 0f, 2f);
                tilt.Resume();
                tilt.HoverEnter();
                Assert.That(tilt.IsSuspended, Is.False);
                Assert.That(tilt.IsHovering, Is.True);
                tilt.ReleaseImmediate();
                Assert.That(card.transform.position, Is.EqualTo(new Vector3(1f, 0f, 2f)), "the new rest, not the old one");
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }
```

- [ ] **Step 2: 确认失败**

```bash
bash $W/run/ut.sh EditMode t2-red -testFilter Phase72DrawRitualTests
```

Expected: 编译失败，因为 `HoverHaloScale`、`SetHaloBoost`、`HoverChanged`、`Resume` 都还不存在。

- [ ] **Step 3: 实现**

`CardView.cs`：在 `private Vector3 haloRestScale;` 后加上：

```csharp
        private float haloBoost = 1f;
        private float? hoverHaloScaleOverride;

        /// <summary>Phase 72: the fan keeps its hover glow at rest size; landed cards use the serialized default.</summary>
        public float HoverHaloScale
        {
            get => hoverHaloScaleOverride ?? hoverHaloScale;
            set
            {
                hoverHaloScaleOverride = value;
                ApplyHalo();
            }
        }

        /// <summary>Phase 72: the pick's hover beat swells the glow (1 = none). A boost shows the glow on its own.</summary>
        public void SetHaloBoost(float multiplier)
        {
            haloBoost = Mathf.Max(0f, multiplier);
            ApplyHalo();
        }

        /// <summary>Back to the serialized hover scale once a fan card has left the fan.</summary>
        public void ClearHoverHaloScale()
        {
            hoverHaloScaleOverride = null;
            ApplyHalo();
        }
```

`ApplyHalo` 改为：

```csharp
        private void ApplyHalo()
        {
            var boosted = haloBoost > 1.001f;
            var visible = !IsFaceUp && (awaitingFlip || hovered || boosted);
            if (highlightRenderer != null)
            {
                highlightRenderer.enabled = visible;
            }

            if (highlightRoot == null)
            {
                return;
            }

            if (!haloScaleCaptured)
            {
                haloRestScale = highlightRoot.transform.localScale;
                haloScaleCaptured = true;
            }

            var scale = (hovered ? HoverHaloScale : 1f) * haloBoost;
            highlightRoot.transform.localScale = haloRestScale * scale;
            highlightRoot.SetActive(visible);
        }
```

`CardClickHandler.cs`：加上 `public event Action<CardView, bool> HoverChanged;`，并在 `OnPointerEnter` / `OnPointerExit` 中 `SetHovered` 之后分别调用 `HoverChanged?.Invoke(cardView, true);` / `HoverChanged?.Invoke(cardView, false);`。

`CardHoverTiltController.cs`：在 `Suspend()` 后加：

```csharp
        /// <summary>
        /// Phase 72: a fan card is suspended while the fan drives it; once it has landed on
        /// its slot the tilt comes back and captures the slot as its new rest on the next hover.
        /// </summary>
        public void Resume()
        {
            suspended = false;
            restCaptured = false;
            hovering = false;
            currentLift = 0f;
            currentTilt = Vector2.zero;
            targetTilt = Vector2.zero;
        }
```

- [ ] **Step 4: 确认通过，并回归 Phase 71**

```bash
bash $W/run/ut.sh EditMode t2-green -testFilter "Phase72DrawRitualTests|Phase71AnimationFixTests|Phase23CardFeelTests"
bash $W/run/ut.sh PlayMode t2-green -testFilter "Phase71CardHoverTests|Phase23CardFeelPlayModeTests"
```

Expected: 全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Gameplay/CardView.cs Assets/Scripts/Gameplay/CardClickHandler.cs Assets/Scripts/Gameplay/CardHoverTiltController.cs Assets/Tests/EditMode/Phase72DrawRitualTests.cs
git commit -m "feat(cards): adjustable hover glow, a glow boost, hover events and a tilt that resumes (Phase 72)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: 扇面 `SpreadFanController`

**Files:**
- Create: `Assets/Scripts/Gameplay/SpreadFanController.cs`
- Create: `Assets/Tests/PlayMode/Phase72FanTests.cs`
- Test: `Assets/Tests/EditMode/Phase72DrawRitualTests.cs`（几何部分）

**Interfaces:**
- Consumes: `CardView.HoverHaloScale`、`CardView.ClearHoverHaloScale()`、`CardClickHandler.HoverChanged`、`CardClickHandler.Clicked`、`CardHoverTiltController.Suspend()`（均来自 Task 2）。
- Produces（`namespace TarotUnity.Gameplay`）：
  - `public int CardCount { get; }`、`public float ArcDegrees { get; }`
  - `public void GetFanPose(int index, out Vector3 position, out Quaternion rotation)`
  - `public IEnumerator Spread(Transform origin)`：从 `origin` 位置逐张滑到扇面
  - `public IEnumerator PickCards(int count, Func<CardView, int, IEnumerator> deliver)`：等玩家点 `count` 张；每张调用 `deliver(card, index)` 并等它结束
  - `public void RequestPick(CardView card)`：点击入口（`Clicked` 与测试都走这里）
  - `public IEnumerator Gather(Transform origin)`：剩余牌收回 `origin` 并销毁
  - `public bool AcceptingPicks { get; }`、`public IReadOnlyList<CardView> FanCards { get; }`（仍在扇面上的牌）
  - `public void SetHovered(CardView card, bool on)`：悬停入口（`HoverChanged` 与测试都走这里）
  - `public event Action<CardView> CardPicked`

- [ ] **Step 1: 写失败的 EditMode 几何测试**（追加到 `Phase72DrawRitualTests`）

```csharp
        [Test]
        public void FanPosesSpanTheArcAndLayerLeftToRight()
        {
            var root = new GameObject("Phase72_FanProbe");
            try
            {
                var fan = root.AddComponent<SpreadFanController>();
                var center = new GameObject("FanCenter").transform;
                center.SetParent(root.transform, false);
                var so = new SerializedObject(fan);
                so.FindProperty("fanCenter").objectReferenceValue = center;
                so.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(fan.CardCount, Is.EqualTo(22));
                Assert.That(fan.ArcDegrees, Is.InRange(64f, 76f));

                fan.GetFanPose(0, out var first, out var firstRot);
                fan.GetFanPose(fan.CardCount - 1, out var last, out var lastRot);
                fan.GetFanPose(fan.CardCount / 2, out var mid, out _);
                Assert.That(first.x, Is.LessThan(0f));
                Assert.That(last.x, Is.GreaterThan(0f));
                Assert.That(mid.z, Is.GreaterThan(first.z), "the arc bows toward the slots");
                Assert.That(last.y, Is.GreaterThan(first.y), "later cards lie on top");
                Assert.That(Quaternion.Angle(firstRot, lastRot), Is.EqualTo(fan.ArcDegrees).Within(0.5f));

                fan.GetFanPose(1, out var second, out _);
                var gap = Vector3.Distance(new Vector3(first.x, 0, first.z), new Vector3(second.x, 0, second.z));
                Assert.That(gap, Is.InRange(0.15f, 0.3f), "neighbours overlap by about two thirds of a 0.74 card");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
```

- [ ] **Step 2: 写失败的 PlayMode 测试**

`Assets/Tests/PlayMode/Phase72FanTests.cs`：

```csharp
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>Phase 72: the fan the player picks from.</summary>
    public sealed class Phase72FanTests
    {
        private GameObject root;
        private SpreadFanController fan;
        private Transform origin;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject("Phase72_FanRig");
            root.SetActive(false);
            fan = root.AddComponent<SpreadFanController>();
            var center = new GameObject("FanCenter").transform;
            center.SetParent(root.transform, false);
            origin = new GameObject("Origin").transform;
            origin.SetParent(root.transform, false);
            origin.localPosition = new Vector3(-2f, 0f, 0f);
            var prefab = AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            var so = new SerializedObject(fan);
            so.FindProperty("fanCenter").objectReferenceValue = center;
            so.FindProperty("cardPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            yield return fan.Spread(origin);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpreadLaysTwentyTwoFaceDownCardsOnTheArc()
        {
            Assert.That(fan.FanCards.Count, Is.EqualTo(22));
            for (var i = 0; i < fan.FanCards.Count; i++)
            {
                fan.GetFanPose(i, out var pose, out _);
                Assert.That(Vector3.Distance(fan.FanCards[i].transform.position, pose), Is.LessThan(0.002f), $"card {i} at rest");
                Assert.That(fan.FanCards[i].IsFaceUp, Is.False);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator HoverLiftsTheCardAndItsNeighboursLess()
        {
            var cards = fan.FanCards;
            fan.SetHovered(cards[10], true);
            yield return new WaitForSeconds(0.4f);
            fan.GetFanPose(10, out var p10, out _);
            fan.GetFanPose(11, out var p11, out _);
            fan.GetFanPose(15, out var p15, out _);
            var lift10 = cards[10].transform.position.y - p10.y;
            var lift11 = cards[11].transform.position.y - p11.y;
            var lift15 = cards[15].transform.position.y - p15.y;
            Assert.That(lift10, Is.GreaterThan(0.02f));
            Assert.That(lift11, Is.InRange(0.002f, lift10 - 0.001f), "a neighbour rises less");
            Assert.That(lift15, Is.LessThan(0.001f), "the wave is local");
        }

        [UnityTest]
        public IEnumerator BriefExitAndReEnterDoesNotDropTheCard()
        {
            var card = fan.FanCards[8];
            fan.SetHovered(card, true);
            yield return new WaitForSeconds(0.4f);
            fan.GetFanPose(8, out var rest, out _);
            var lifted = card.transform.position.y - rest.y;

            fan.SetHovered(card, false);
            yield return null;
            fan.SetHovered(card, true);
            yield return new WaitForSeconds(0.2f);
            Assert.That(card.transform.position.y - rest.y, Is.GreaterThan(lifted * 0.9f), "no flicker back down");
        }

        [UnityTest]
        public IEnumerator PicksDeliverInOrderAndLeaveAGap()
        {
            var delivered = new List<(CardView, int)>();
            var picked = new[] { fan.FanCards[3], fan.FanCards[12], fan.FanCards[20] };
            var routine = root.GetComponent<SpreadFanController>().StartCoroutine(
                fan.PickCards(3, (c, i) => Deliver(delivered, c, i)));
            yield return null;
            Assert.That(fan.AcceptingPicks, Is.True);

            foreach (var card in picked)
            {
                fan.RequestPick(card);
                yield return new WaitForSeconds(0.25f);
            }

            yield return new WaitForSeconds(0.3f);
            Assert.That(delivered.Select(d => d.Item1), Is.EqualTo(picked));
            Assert.That(delivered.Select(d => d.Item2), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(fan.FanCards.Count, Is.EqualTo(19));
            Assert.That(fan.AcceptingPicks, Is.False);
            Assert.That(picked.All(c => c.HoverHaloScale > 1.01f), "picked cards get their normal hover glow back");
        }

        [UnityTest]
        public IEnumerator ExtraClicksWhileBusyAreDroppedAfterOneQueuedPick()
        {
            var delivered = new List<(CardView, int)>();
            fan.StartCoroutine(fan.PickCards(3, (c, i) => Deliver(delivered, c, i, 0.5f)));
            yield return null;

            var first = fan.FanCards[0];
            fan.RequestPick(first);
            while (fan.FanCards.Contains(first))   // wait until it is actually in flight
            {
                yield return null;
            }

            fan.RequestPick(fan.FanCards[4]);   // queued
            fan.RequestPick(fan.FanCards[5]);   // dropped
            fan.RequestPick(fan.FanCards[6]);   // dropped
            yield return new WaitForSeconds(1.3f);

            Assert.That(delivered.Count, Is.EqualTo(2), "one in flight, one queued, the rest dropped");
            Assert.That(fan.AcceptingPicks, Is.True);
        }

        [UnityTest]
        public IEnumerator GatherClearsTheRemainingCards()
        {
            var cards = fan.FanCards.ToList();
            yield return fan.Gather(origin);
            yield return null;
            Assert.That(fan.FanCards.Count, Is.EqualTo(0));
            Assert.That(cards.All(c => c == null), "gathered cards are destroyed");
        }

        private static IEnumerator Deliver(List<(CardView, int)> log, CardView card, int index, float seconds = 0.1f)
        {
            log.Add((card, index));
            yield return new WaitForSeconds(seconds);
        }
    }
}
```

- [ ] **Step 3: 确认失败**

```bash
bash $W/run/ut.sh EditMode t3-red -testFilter Phase72DrawRitualTests
```

Expected: 编译失败，`SpreadFanController` 不存在。

- [ ] **Step 4: 实现 `SpreadFanController`**

`Assets/Scripts/Gameplay/SpreadFanController.cs`：

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    /// <summary>
    /// Phase 72: after the shuffle the deck spreads into an arc on the table and the player
    /// picks the cards. The fan owns its props - face-down PF_TarotCard instances with no
    /// draw bound - and their motion: the spread, the hover wave that follows the pointer,
    /// the pick queue (one pick waits while a card is in flight; further clicks are dropped),
    /// and the gather back into the deck. A picked card leaves the fan and becomes the card
    /// the player later flips; which card it is is decided by the backend or the local
    /// simulator, never by where the player clicked.
    /// </summary>
    public sealed class SpreadFanController : MonoBehaviour
    {
        [SerializeField] private CardView cardPrefab;
        [Tooltip("The middle card's resting pose. Forward points from the player toward the slots.")]
        [SerializeField] private Transform fanCenter;

        [Header("Arc")]
        [SerializeField] private int cardCount = 22;
        [SerializeField] private float radius = 3.4f;
        [SerializeField] private float arcDegrees = 70f;
        [Tooltip("Each card lies this much above the one to its left, so the fan layers left to right.")]
        [SerializeField] private float layerStep = 0.004f;

        [Header("Spread")]
        [SerializeField] private float spreadStagger = 0.035f;
        [SerializeField] private float spreadCardSeconds = 0.28f;
        [SerializeField] private float spreadArcHeight = 0.12f;
        [SerializeField] private float settleSquash = 0.05f;
        [SerializeField] private float settleSeconds = 0.12f;

        [Header("Hover wave")]
        [SerializeField] private float hoverLift = 0.05f;
        [Tooltip("How far the hovered card slides out toward the player.")]
        [SerializeField] private float hoverSlide = 0.3f;
        [Tooltip("How many neighbours on each side rise with the hovered card.")]
        [SerializeField] private float waveRadius = 2.5f;
        [SerializeField] private float hoverResponseSeconds = 0.08f;
        [Tooltip("A pointer exit only drops the card after this long, so a card sliding out from under the pointer does not flicker.")]
        [SerializeField] private float hoverReleaseDelay = 0.12f;
        [Tooltip("Hover glow scale for a fan card - restrained, the fan is crowded.")]
        [SerializeField] private float fanHoverHaloScale = 1f;

        [Header("Gather")]
        [SerializeField] private float gatherSeconds = 0.6f;

        private readonly List<CardView> fanCards = new();
        private readonly Dictionary<CardView, int> slotOf = new();
        private readonly Dictionary<CardView, float> lift = new();
        private readonly Dictionary<CardView, float> slide = new();
        private bool spreadDone;
        private CardView hovered;
        private float hoverReleaseAt = -1f;
        private CardView pending;
        private bool busy;
        private int remainingPicks;

        public event Action<CardView> CardPicked;

        public int CardCount => cardCount;
        public float ArcDegrees => arcDegrees;
        public IReadOnlyList<CardView> FanCards => fanCards;
        public bool AcceptingPicks => remainingPicks > 0;

        public void GetFanPose(int index, out Vector3 position, out Quaternion rotation)
        {
            var center = fanCenter != null ? fanCenter : transform;
            var t = cardCount > 1 ? (float)index / (cardCount - 1) - 0.5f : 0f;
            var angle = t * arcDegrees;
            var pivot = center.position - center.forward * radius;
            var turn = Quaternion.AngleAxis(angle, center.up);
            position = pivot + turn * center.forward * radius + center.up * (index * layerStep);
            rotation = turn * center.rotation;
        }

        public IEnumerator Spread(Transform origin)
        {
            Clear();
            spreadDone = false;
            if (cardPrefab == null)
            {
                yield break;
            }

            var starts = new List<Vector3>();
            for (var i = 0; i < cardCount; i++)
            {
                var card = Instantiate(cardPrefab, transform);
                card.name = $"FanCard_{i:00}";
                card.transform.SetPositionAndRotation(origin.position, origin.rotation);
                card.SetFaceUp(false);
                card.HoverHaloScale = fanHoverHaloScale;
                var tilt = card.GetComponent<CardHoverTiltController>();
                if (tilt != null)
                {
                    tilt.Suspend();
                }

                var click = card.GetComponent<CardClickHandler>();
                if (click == null)
                {
                    click = card.gameObject.AddComponent<CardClickHandler>();
                }

                click.Clicked += RequestPick;
                click.HoverChanged += SetHovered;
                fanCards.Add(card);
                slotOf[card] = i;
                lift[card] = 0f;
                slide[card] = 0f;
                starts.Add(origin.position);
            }

            var total = spreadCardSeconds + spreadStagger * (cardCount - 1);
            for (var elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
            {
                for (var i = 0; i < fanCards.Count; i++)
                {
                    var k = Mathf.Clamp01((elapsed - i * spreadStagger) / spreadCardSeconds);
                    var e = 1f - (1f - k) * (1f - k);
                    GetFanPose(i, out var p, out var r);
                    fanCards[i].transform.position = Vector3.Lerp(starts[i], p, e) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * spreadArcHeight);
                    fanCards[i].transform.rotation = Quaternion.Slerp(origin.rotation, r, e);
                }

                yield return null;
            }

            for (var i = 0; i < fanCards.Count; i++)
            {
                GetFanPose(i, out var p, out var r);
                fanCards[i].transform.SetPositionAndRotation(p, r);
            }

            // The whole row settles with one small squash.
            var baseScale = fanCards.Count > 0 ? fanCards[0].transform.localScale : Vector3.one;
            var squashed = new Vector3(baseScale.x * (1f + settleSquash * 0.6f), baseScale.y * (1f - settleSquash), baseScale.z);
            for (var elapsed = 0f; elapsed < settleSeconds; elapsed += Time.deltaTime)
            {
                var k = elapsed / settleSeconds;
                foreach (var card in fanCards)
                {
                    card.transform.localScale = Vector3.Lerp(squashed, baseScale, 1f - (1f - k) * (1f - k));
                }

                yield return null;
            }

            foreach (var card in fanCards)
            {
                card.transform.localScale = baseScale;
            }

            spreadDone = true;
        }

        public void SetHovered(CardView card, bool on)
        {
            if (card == null || !slotOf.ContainsKey(card))
            {
                return;
            }

            if (on)
            {
                hovered = card;
                hoverReleaseAt = -1f;
            }
            else if (hovered == card)
            {
                hoverReleaseAt = Time.time + hoverReleaseDelay;
            }
        }

        public void RequestPick(CardView card)
        {
            if (card == null || !slotOf.ContainsKey(card) || remainingPicks <= 0)
            {
                return;
            }

            if (!busy)
            {
                pending = card;
                return;
            }

            // One pick may wait while a card is in flight; further clicks are dropped.
            if (pending == null && remainingPicks > 1)
            {
                pending = card;
            }
        }

        public IEnumerator PickCards(int count, Func<CardView, int, IEnumerator> deliver)
        {
            remainingPicks = Mathf.Min(count, fanCards.Count);
            pending = null;
            for (var index = 0; index < count && remainingPicks > 0; index++)
            {
                while (pending == null)
                {
                    yield return null;
                }

                var card = pending;
                pending = null;
                busy = true;
                Release(card);
                remainingPicks--;
                CardPicked?.Invoke(card);
                if (deliver != null)
                {
                    yield return deliver(card, index);
                }

                busy = false;
            }

            remainingPicks = 0;
            pending = null;
        }

        public IEnumerator Gather(Transform origin)
        {
            spreadDone = false;
            hovered = null;
            var cards = new List<CardView>(fanCards);
            var starts = new List<Vector3>();
            foreach (var card in cards)
            {
                starts.Add(card.transform.position);
            }

            var mid = (cards.Count - 1) / 2f;
            var stagger = cards.Count > 1 ? gatherSeconds * 0.4f / mid : 0f;
            var cardSeconds = gatherSeconds * 0.6f;
            for (var elapsed = 0f; elapsed < gatherSeconds; elapsed += Time.deltaTime)
            {
                for (var i = 0; i < cards.Count; i++)
                {
                    // From both ends toward the middle.
                    var order = mid - Mathf.Abs(i - mid);
                    var k = Mathf.Clamp01((elapsed - order * stagger) / cardSeconds);
                    var e = k * k;
                    cards[i].transform.position = Vector3.Lerp(starts[i], origin.position, e);
                }

                yield return null;
            }

            Clear();
        }

        private void Release(CardView card)
        {
            var click = card.GetComponent<CardClickHandler>();
            if (click != null)
            {
                click.Clicked -= RequestPick;
                click.HoverChanged -= SetHovered;
            }

            card.SetHovered(false);
            card.ClearHoverHaloScale();
            fanCards.Remove(card);
            slotOf.Remove(card);
            lift.Remove(card);
            slide.Remove(card);
            if (hovered == card)
            {
                hovered = null;
            }
        }

        private void Clear()
        {
            foreach (var card in fanCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            fanCards.Clear();
            slotOf.Clear();
            lift.Clear();
            slide.Clear();
            hovered = null;
            pending = null;
            remainingPicks = 0;
        }

        private void Update()
        {
            if (!spreadDone)
            {
                return;
            }

            if (hovered != null && hoverReleaseAt > 0f && Time.time >= hoverReleaseAt)
            {
                hovered = null;
                hoverReleaseAt = -1f;
            }

            var h = hovered != null && slotOf.TryGetValue(hovered, out var hs) ? hs : -1;
            var blend = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, hoverResponseSeconds));
            foreach (var card in fanCards)
            {
                var i = slotOf[card];
                var distance = h >= 0 ? Mathf.Abs(i - h) : float.MaxValue;
                var weight = distance <= waveRadius ? 0.5f + 0.5f * Mathf.Cos(Mathf.PI * distance / (waveRadius + 1f)) : 0f;
                lift[card] = Mathf.Lerp(lift[card], hoverLift * weight, blend);
                slide[card] = Mathf.Lerp(slide[card], i == h ? hoverSlide : 0f, blend);
                GetFanPose(i, out var p, out var r);
                var towardPlayer = -(r * Vector3.forward);
                card.transform.SetPositionAndRotation(p + Vector3.up * lift[card] + towardPlayer * slide[card], r);
            }
        }
    }
}
```

说明：
- `Update` 只驱动仍在扇面上的牌。被选中的牌从 `fanCards` 移除后，由 `deliver`（`DeckController.DealPickedCard`）接手 transform。
- 波浪权重用余弦衰减。`waveRadius` = 2.5 时，相邻牌的权重约 0.83，距离 5 张为 0，满足测试里「第 15 张不动」。

- [ ] **Step 5: 确认通过**

```bash
bash $W/run/ut.sh EditMode t3-green -testFilter Phase72DrawRitualTests
bash $W/run/ut.sh PlayMode t3-green -testFilter Phase72FanTests
```

Expected: 全部 PASS。如果 `PicksDeliverInOrderAndLeaveAGap` 因为 0.25 秒的间隔撞上排队规则而失败：`Deliver` 默认 0.1 秒，间隔 0.25 秒 > 0.1 秒，所以不会排队。失败时先检查 `busy` 是否正确复位，再考虑改测试。

- [ ] **Step 6: 提交**

```bash
git add Assets/Scripts/Gameplay/SpreadFanController.cs* Assets/Tests/PlayMode/Phase72FanTests.cs* Assets/Tests/EditMode/Phase72DrawRitualTests.cs
git commit -m "feat(draw): the fan - spread, hover wave, one queued pick, gather (Phase 72)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: 飞牌、绑定与退回（`DeckController`）

**Files:**
- Modify: `Assets/Scripts/Gameplay/DeckController.cs`
- Create: `Assets/Tests/PlayMode/Phase72DealPickedTests.cs`

**Interfaces:**
- Consumes: `CardView.SetHaloBoost`、`CardHoverTiltController.Resume`（Task 2）。
- Produces:
  - `public IEnumerator DealPickedCard(CardView card, Transform slot)`：抽出、悬空、飞行、落位；落位后把牌加入 `ActiveCards`，并触发 `CardDealt`
  - `public event Action<CardView> CardHovering`：悬空那一拍开始时触发（Task 7 用它闪牌位）
  - `public void BindDealtCards(IList<CardDrawData> draws)`
  - `public IEnumerator ReturnDealtCards()`：飞回牌堆并清空
  - `public void AdoptForTest(CardView card)` 不需要；测试直接用 `DealPickedCard`。

- [ ] **Step 1: 写失败的 PlayMode 测试**

`Assets/Tests/PlayMode/Phase72DealPickedTests.cs`：

```csharp
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>Phase 72: a picked card is pulled, hovers glowing, flies trailing light, and lands.</summary>
    public sealed class Phase72DealPickedTests
    {
        private DeckController deck;
        private SpreadLayoutController layout;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            deck = Object.FindFirstObjectByType<DeckController>();
            layout = Object.FindFirstObjectByType<SpreadLayoutController>();
        }

        private static CardView SpawnFaceDown(Vector3 at)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            var card = Object.Instantiate(prefab, at, Quaternion.identity);
            card.SetFaceUp(false);
            card.GetComponent<CardHoverTiltController>()?.Suspend();
            return card;
        }

        [UnityTest]
        public IEnumerator PickedCardHoversGlowingThenLandsOnItsSlot()
        {
            var slot = layout.GetSlots(3)[1];
            var card = SpawnFaceDown(new Vector3(0f, 0.13f, -2.4f));
            var halo = card.GetComponentsInChildren<Transform>(true).First(t => t.name == "Highlight");
            var trail = card.GetComponentInChildren<TrailRenderer>(true);
            var hovering = false;
            deck.CardHovering += _ => hovering = true;

            var maxHaloScale = 0f;
            var maxHeight = 0f;
            var trailSeen = false;
            var routine = deck.StartCoroutine(deck.DealPickedCard(card, slot));
            var started = Time.time;
            while (!deck.ActiveCards.Contains(card) && Time.time - started < 5f)
            {
                maxHaloScale = Mathf.Max(maxHaloScale, halo.localScale.x);
                maxHeight = Mathf.Max(maxHeight, card.transform.position.y);
                trailSeen |= trail != null && trail.emitting;
                yield return null;
            }

            var seconds = Time.time - started;
            Assert.That(deck.ActiveCards, Does.Contain(card));
            Assert.That(hovering, Is.True, "the hover beat is announced");
            Assert.That(seconds, Is.InRange(0.9f, 1.6f), "pull + hover + flight + landing is about 1.1 s");
            Assert.That(maxHeight, Is.GreaterThan(0.4f), "the card rises and arcs");
            Assert.That(maxHaloScale, Is.GreaterThan(1.5f * 1.2f), "the glow swells while it hovers");
            Assert.That(trailSeen, Is.True, "a light trail follows the flight");
            yield return new WaitForSeconds(0.2f);
            Assert.That(Vector3.Distance(card.transform.position, slot.position), Is.LessThan(0.002f));
            Assert.That(trail.emitting, Is.False);
            Assert.That(halo.gameObject.activeSelf, Is.True, "a landed card waits to be flipped");
            Assert.That(card.GetComponent<CardHoverTiltController>().IsSuspended, Is.False, "the tilt comes back");
        }

        [UnityTest]
        public IEnumerator BindGivesLandedCardsTheirDrawsInSlotOrder()
        {
            var slots = layout.GetSlots(3);
            var cards = Enumerable.Range(0, 3).Select(i => SpawnFaceDown(new Vector3(i, 0.13f, -2.4f))).ToArray();
            for (var i = 0; i < 3; i++)
            {
                yield return deck.DealPickedCard(cards[i], slots[i]);
            }

            var draws = LocalReadingSimulator.CreateSession(12, "过去现在未来", "q", "general", null).cardDraws;
            deck.BindDealtCards(draws);
            for (var i = 0; i < 3; i++)
            {
                Assert.That(deck.ActiveCards[i], Is.SameAs(cards[i]));
                Assert.That(cards[i].DrawData, Is.SameAs(draws[i]));
                Assert.That(cards[i].IsFaceUp, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator ReturnFliesTheCardsBackAndClears()
        {
            var slot = layout.GetSlots(1)[0];
            var card = SpawnFaceDown(new Vector3(0f, 0.13f, -2.4f));
            yield return deck.DealPickedCard(card, slot);
            yield return deck.ReturnDealtCards();
            yield return null;
            Assert.That(deck.ActiveCards.Count, Is.EqualTo(0));
            Assert.That(card == null, Is.True, "returned cards are destroyed");
        }
    }
}
```

注：`LocalReadingSimulator.CreateSession(..., draws: null)` 需要能生成三张牌。执行前先读 `LocalReadingSimulator.CreateSession` 的签名。如果它要求传入 draws，就改用 `ReadingRoomController` 同样的 `CreateLocalDraws` 路径：`LocalReadingSimulator` 里生成 N 张牌的公开方法（读代码确认名称），并在 ledger 里记一条 Ruling。

- [ ] **Step 2: 确认失败**

```bash
bash $W/run/ut.sh PlayMode t4-red -testFilter Phase72DealPickedTests
```

Expected: 编译失败，`DealPickedCard` / `CardHovering` / `BindDealtCards` / `ReturnDealtCards` 不存在。

- [ ] **Step 3: 实现**（`DeckController.cs`）

在 `[Header("Phase54 Landing")]` 字段组之后加：

```csharp
        // Phase 72: a card the player picked from the fan is pulled out, hovers glowing,
        // flies to its slot trailing light, and lands with the Phase 54 weight.
        [Header("Phase72 Pick")]
        [SerializeField] private float pickPullSeconds = 0.15f;
        [SerializeField] private float pickPullDistance = 0.25f;
        [SerializeField] private float pickRise = 0.55f;
        [SerializeField] private float pickHoverSeconds = 0.25f;
        [SerializeField] private float pickHoverBob = 0.02f;
        [SerializeField] private float pickGlowBoost = 1.6f;
        [SerializeField] private float returnSeconds = 0.45f;

        public event Action<CardView> CardHovering;
```

新增方法（放在 `MoveCardToSlot` 之前）：

```csharp
        public IEnumerator DealPickedCard(CardView card, Transform slot)
        {
            if (card == null || slot == null)
            {
                yield break;
            }

            var t = card.transform;
            t.SetParent(cardParent != null ? cardParent : transform, true);
            card.SetHighlighted(true);
            CardDealStarted?.Invoke(card);

            // Beat 1 - pull: out of the fan toward the player and up, accelerating.
            var start = t.position;
            var toward = -(t.rotation * Vector3.forward);
            toward.y = 0f;
            var pulled = start + toward.normalized * pickPullDistance + Vector3.up * pickRise;
            for (var elapsed = 0f; elapsed < pickPullSeconds; elapsed += Time.deltaTime)
            {
                var k = elapsed / pickPullSeconds;
                t.position = Vector3.Lerp(start, pulled, k * k);
                yield return null;
            }

            t.position = pulled;

            // Beat 2 - hover: turn to the slot's heading, bob, and let the glow swell and settle.
            CardHovering?.Invoke(card);
            var fromRotation = t.rotation;
            for (var elapsed = 0f; elapsed < pickHoverSeconds; elapsed += Time.deltaTime)
            {
                var k = elapsed / pickHoverSeconds;
                var swell = Mathf.Sin(k * Mathf.PI);
                t.rotation = Quaternion.Slerp(fromRotation, slot.rotation, k * k * (3f - 2f * k));
                t.position = pulled + Vector3.up * (Mathf.Sin(k * Mathf.PI * 2f) * pickHoverBob);
                card.SetHaloBoost(1f + (pickGlowBoost - 1f) * swell);
                yield return null;
            }

            card.SetHaloBoost(1f);
            t.position = pulled;

            // Beat 3 - flight, trailing light.
            var trail = card.GetComponentInChildren<TrailRenderer>(true);
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }

            yield return MoveCardToSlot(t, slot);
            if (trail != null)
            {
                trail.emitting = false;
            }

            // Beat 4 - landing.
            yield return LandingSettle(t);
            activeCards.Add(card);
            var tilt = card.GetComponent<CardHoverTiltController>();
            if (tilt != null)
            {
                tilt.Resume();
            }

            CardDealt?.Invoke(card);
        }

        public void BindDealtCards(IList<CardDrawData> draws)
        {
            if (draws == null)
            {
                return;
            }

            if (draws.Count != activeCards.Count)
            {
                Debug.LogWarning($"DeckController: {draws.Count} draws for {activeCards.Count} dealt cards.");
            }

            for (var i = 0; i < Mathf.Min(draws.Count, activeCards.Count); i++)
            {
                var card = activeCards[i];
                card.Bind(draws[i]);
                card.SetFaceArtwork(ResolveArtwork(draws[i]));
                card.SetHighlighted(true);
            }
        }

        public IEnumerator ReturnDealtCards()
        {
            var cards = new List<CardView>(activeCards);
            var starts = new List<Vector3>();
            foreach (var card in cards)
            {
                starts.Add(card.transform.position);
            }

            for (var elapsed = 0f; elapsed < returnSeconds; elapsed += Time.deltaTime)
            {
                var k = elapsed / returnSeconds;
                var e = k * k * (3f - 2f * k);
                for (var i = 0; i < cards.Count; i++)
                {
                    if (cards[i] != null)
                    {
                        cards[i].transform.position = Vector3.Lerp(starts[i], transform.position, e)
                            + Vector3.up * (Mathf.Sin(k * Mathf.PI) * dealArcHeight * 0.5f);
                    }
                }

                yield return null;
            }

            Clear();
        }
```

检查总时长：0.15 + 0.25 + 0.56（场景里的 `dealDuration`）+ 0.14（`landingSeconds`）≈ 1.1 秒。

- [ ] **Step 4: 确认通过**

```bash
bash $W/run/ut.sh PlayMode t4-green -testFilter "Phase72DealPickedTests|Phase54DealLandingTests"
```

Expected: 注意 `PickedCardHoversGlowingThenLandsOnItsSlot` 的 `trailSeen` 断言要等 Task 6 给预制体加上 TrailRenderer 才能通过。本任务先把这个断言用 `Assume.That(trail, Is.Not.Null)` 包住：没有拖尾时跳过它，而不是让整个测试失败。在 ledger 里记：「Ruling: trail 断言依赖 Task 6 的预制体接线，Task 6 完成后移除 Assume，改成硬断言」。其余断言全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Gameplay/DeckController.cs Assets/Tests/PlayMode/Phase72DealPickedTests.cs*
git commit -m "feat(draw): a picked card is pulled, hovers glowing, flies trailing light and lands; bind and return (Phase 72)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: 选牌镜头与牌位聚焦

**Files:**
- Modify: `Assets/Scripts/Presentation/CameraChoreographyController.cs`
- Modify: `Assets/Scripts/UI/RitualStepIndicator.cs`
- Test: `Assets/Tests/EditMode/Phase72DrawRitualTests.cs`

**Interfaces:**
- Produces:
  - `CameraChoreographyController.FocusDraw(int cardCount)`
  - `CameraChoreographyController.TryGetDrawPose(int cardCount, out Transform pose, out float fov)`（`drawPoses` 为 `SpreadPose[]`，按牌数取；找不到时回退到数组第一项）
  - `RitualStepIndicator.FocusSocket(int index)`（-1 = 全部亮）
  - `RitualStepIndicator.FlashFocusedSocket()`
  - `RitualStepIndicator.FocusedSocket { get; }`

**Ruling（写计划时已定，执行时抄进 ledger）：** spec 写的是单个 `drawPose`。但凯尔特十字需要拉得很远，如果 1 张和 3 张牌阵也用这个远镜头，扇面上每张牌露出的边在 1920 宽的屏幕上只有约 24 像素，很难点中。所以改为按牌数取的 `drawPoses`：1 张和 3 张共用一个较近的镜头，10 张用远镜头。代价：多一个位姿需要调，视锥测试按牌数分别检查。

- [ ] **Step 1: 写失败的测试**（追加到 `Phase72DrawRitualTests`，并加 `using TarotUnity.UI;`）

```csharp
        [Test]
        public void DrawPoseFallsBackToTheFirstEntry()
        {
            var go = new GameObject("Phase72_CamProbe");
            try
            {
                var cam = go.AddComponent<CameraChoreographyController>();
                var near = new GameObject("Near").transform;
                var far = new GameObject("Far").transform;
                near.SetParent(go.transform);
                far.SetParent(go.transform);
                var so = new SerializedObject(cam);
                var poses = so.FindProperty("drawPoses");
                poses.arraySize = 2;
                poses.GetArrayElementAtIndex(0).FindPropertyRelative("cardCount").intValue = 3;
                poses.GetArrayElementAtIndex(0).FindPropertyRelative("pose").objectReferenceValue = near;
                poses.GetArrayElementAtIndex(0).FindPropertyRelative("fov").floatValue = 45f;
                poses.GetArrayElementAtIndex(1).FindPropertyRelative("cardCount").intValue = 10;
                poses.GetArrayElementAtIndex(1).FindPropertyRelative("pose").objectReferenceValue = far;
                poses.GetArrayElementAtIndex(1).FindPropertyRelative("fov").floatValue = 50f;
                so.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(cam.TryGetDrawPose(10, out var p10, out var f10), Is.True);
                Assert.That(p10, Is.SameAs(far));
                Assert.That(f10, Is.EqualTo(50f));
                Assert.That(cam.TryGetDrawPose(5, out var p5, out _), Is.True);
                Assert.That(p5, Is.SameAs(near), "unknown counts use the first entry");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FocusSocketLightsOnlyThatSocket()
        {
            var go = new GameObject("Phase72_StepProbe");
            try
            {
                var flowGo = new GameObject("Flow");
                flowGo.transform.SetParent(go.transform);
                var flow = flowGo.AddComponent<TarotUnity.Gameplay.ReadingFlowController>();
                flow.SelectSpread(1, 3);
                var glows = Enumerable.Range(0, 3).Select(i => new GameObject($"Glow{i}")).ToArray();
                foreach (var g in glows)
                {
                    g.transform.SetParent(go.transform);
                }

                var indicator = go.AddComponent<RitualStepIndicator>();
                var so = new SerializedObject(indicator);
                so.FindProperty("flowController").objectReferenceValue = flow;
                var sets = so.FindProperty("socketGlowSets");
                sets.arraySize = 1;
                sets.GetArrayElementAtIndex(0).FindPropertyRelative("cardCount").intValue = 3;
                var arr = sets.GetArrayElementAtIndex(0).FindPropertyRelative("glows");
                arr.arraySize = 3;
                for (var i = 0; i < 3; i++)
                {
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = glows[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                indicator.ApplyFlowState(ReadingFlowState.Drawing);
                Assert.That(glows.All(g => g.activeSelf), Is.True, "control: the draw lights the spread");

                indicator.FocusSocket(1);
                Assert.That(glows.Select(g => g.activeSelf), Is.EqualTo(new[] { false, true, false }));
                Assert.That(indicator.FocusedSocket, Is.EqualTo(1));

                indicator.FocusSocket(-1);
                Assert.That(glows.All(g => g.activeSelf), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
```

（在文件顶部加 `using System.Linq;` 和 `using TarotUnity.Gameplay;`；`ReadingFlowState` 在 `TarotUnity.Gameplay` 命名空间下。）

- [ ] **Step 2: 确认失败**

```bash
bash $W/run/ut.sh EditMode t5-red -testFilter Phase72DrawRitualTests
```

Expected: 编译失败，`drawPoses`、`TryGetDrawPose`、`FocusSocket` 都不存在。

- [ ] **Step 3: 实现**

`CameraChoreographyController.cs`：在 `spreadPoses` 字段后加：

```csharp
        // Phase 72: where the camera looks while the player picks from the fan - high enough
        // to hold the fan and every slot of the spread. Keyed by card count like spreadPoses;
        // an unknown count uses the first entry.
        [SerializeField] private SpreadPose[] drawPoses = new SpreadPose[0];
```

在 `FocusResult` 之前加：

```csharp
        public bool TryGetDrawPose(int cardCount, out Transform pose, out float fov)
        {
            pose = null;
            fov = defaultFov;
            SpreadPose fallback = null;
            foreach (var entry in drawPoses)
            {
                if (entry == null || entry.pose == null)
                {
                    continue;
                }

                fallback ??= entry;
                if (entry.cardCount == cardCount)
                {
                    pose = entry.pose;
                    fov = entry.fov;
                    return true;
                }
            }

            if (fallback == null)
            {
                return false;
            }

            pose = fallback.pose;
            fov = fallback.fov;
            return true;
        }

        public void FocusDraw(int cardCount)
        {
            if (TryGetDrawPose(cardCount, out var pose, out var fov))
            {
                MoveTo(pose, fov);
            }
            else
            {
                FocusDeck();
            }
        }
```

（`fallback ??= entry` 是普通 C# 对象，不是 `UnityEngine.Object`，所以可以用 `??=`。）

`RitualStepIndicator.cs`：
- 字段：`private int focusedSocket = -1; private float flashStartedAt = -10f;`，以及 `[SerializeField] private float flashSeconds = 0.25f; [SerializeField] private float flashScale = 1.3f;`
- `public int FocusedSocket => focusedSocket;`
- 方法：

```csharp
        /// <summary>Phase 72: while the player picks, only the next slot glows (-1 = the whole spread).</summary>
        public void FocusSocket(int index)
        {
            focusedSocket = index;
            SetSocketsLit(socketsLit);
        }

        /// <summary>Phase 72: the target slot answers the picked card with a brief swell.</summary>
        public void FlashFocusedSocket()
        {
            flashStartedAt = Time.time;
        }
```

- `SetSocketsLit` 内层循环改为按下标判断：

```csharp
                var on = lit && set.cardCount == count;
                for (var i = 0; i < set.glows.Length; i++)
                {
                    var glow = set.glows[i];
                    if (glow != null)
                    {
                        glow.SetActive(on && (focusedSocket < 0 || i == focusedSocket));
                    }
                }
```

- `Update` 中计算 `t` 之后乘上闪光：

```csharp
            var flashK = (Time.time - flashStartedAt) / Mathf.Max(0.01f, flashSeconds);
            if (flashK >= 0f && flashK < 1f)
            {
                t *= 1f + (flashScale - 1f) * Mathf.Sin(flashK * Mathf.PI);
            }
```

- [ ] **Step 4: 确认通过，并回归 Phase 61/63**

```bash
bash $W/run/ut.sh EditMode t5-green -testFilter "Phase72DrawRitualTests|Phase61ReadingRoomSlotStepTests|Phase63SpreadDefinitionTests|Phase21"
```

Expected: 全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Presentation/CameraChoreographyController.cs Assets/Scripts/UI/RitualStepIndicator.cs Assets/Tests/EditMode/Phase72DrawRitualTests.cs
git commit -m "feat(draw): draw camera poses per spread; light only the next slot, flash it on a pick (Phase 72)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: 场景与预制体接线（`Phase72DrawRitualBootstrapper`）

**Files:**
- Create: `Assets/Editor/Phase72DrawRitualBootstrapper.cs`
- Modify（由引导脚本写入）：`Assets/Scenes/ReadingRoom.unity`、`Assets/Prefabs/Cards/PF_TarotCard.prefab`
- Modify: `Assets/Scripts/UI/ReadingRoomController.cs`（本任务只加三个序列化字段，供引导脚本接线）
- Test: `Assets/Tests/EditMode/Phase72DrawRitualTests.cs`

**Interfaces:**
- Consumes: Tasks 3 和 5 的字段名：`SpreadFanController.cardPrefab/fanCenter`、`CameraChoreographyController.drawPoses`。
- Produces（场景对象，Task 7 与截图脚本依赖）：
  - `MP_SpreadFan`（根物体，挂 `SpreadFanController`）及其子物 `FanCenter`：世界坐标 (0, 0.13, -2.45)，forward = +z
  - `ReadingRoomCameraChoreography/DrawPoseNear`：世界坐标 (0, 5.0, -6.3)，Euler(50, 0, 0)，FOV 45（cardCount 1 和 3）
  - `ReadingRoomCameraChoreography/DrawPoseCeltic`：世界坐标 (0.7, 6.8, -7.0)，Euler(45, 0, 0)，FOV 50（cardCount 10）
  - `ReadingRoomController` 新字段 `spreadFan`、`stepIndicator`、`pickHiddenUi`（`CanvasGroup[]`）。其中 `pickHiddenUi` 覆盖 `Phase11_ActionDock`、`QuestionInput`、`OneCardButton`、`ThreeCardButton`、`CelticCrossButton`、`DrawButton`，引导脚本给每个对象加 `CanvasGroup`（alpha 1）。
  - `PF_TarotCard/FlightTrail`：`TrailRenderer`，`emitting` = false，time 0.3，宽度 0.12→0，MP_WarmGlow 材质，颜色 (1, .78, .4, .8)→(1, .5, .2, 0)，`minVertexDistance` 0.02，不投影。

- [ ] **Step 1: 写失败的 EditMode 测试**（追加到 `Phase72DrawRitualTests`，加 `using UnityEditor.SceneManagement;`）

```csharp
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";

        [Test]
        public void TheCardCarriesAQuietFlightTrail()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            var trail = prefab.GetComponentInChildren<TrailRenderer>(true);
            Assert.That(trail, Is.Not.Null);
            Assert.That(trail.emitting, Is.False, "only the flight emits");
            Assert.That(trail.sharedMaterial.name, Is.EqualTo("MP_WarmGlow"));
            Assert.That(trail.time, Is.InRange(0.2f, 0.4f));
            Assert.That(trail.widthCurve.Evaluate(1f), Is.LessThan(trail.widthCurve.Evaluate(0f)), "it tapers");
        }

        [Test]
        public void TheRoomHasAFanWiredToTheController()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            Assert.That(fan, Is.Not.Null);
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var so = new SerializedObject(room);
            Assert.That(so.FindProperty("spreadFan").objectReferenceValue, Is.SameAs(fan));
            Assert.That(so.FindProperty("stepIndicator").objectReferenceValue, Is.Not.Null);
            Assert.That(so.FindProperty("pickHiddenUi").arraySize, Is.EqualTo(6));
            var fanSo = new SerializedObject(fan);
            Assert.That(fanSo.FindProperty("cardPrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(fanSo.FindProperty("fanCenter").objectReferenceValue, Is.Not.Null);
        }

        [TestCase(1, 16f / 9f)]
        [TestCase(3, 16f / 9f)]
        [TestCase(10, 16f / 9f)]
        [TestCase(1, 4f / 3f)]
        [TestCase(3, 4f / 3f)]
        [TestCase(10, 4f / 3f)]
        public void TheDrawCameraHoldsTheFanAndEverySlot(int cardCount, float aspect)
        {
            EditorSceneManager.OpenScene(ScenePath);
            var choreography = Object.FindFirstObjectByType<CameraChoreographyController>();
            Assert.That(choreography.TryGetDrawPose(cardCount, out var pose, out var fov), Is.True);
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var slots = Object.FindFirstObjectByType<SpreadLayoutController>().GetSlots(cardCount);
            Assert.That(slots.Count, Is.EqualTo(cardCount), "control: the spread's slots");

            var go = new GameObject("Phase72_FrustumProbe");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.transform.SetPositionAndRotation(pose.position, pose.rotation);
                cam.fieldOfView = fov;
                cam.aspect = aspect;

                var half = new Vector3(0.37f, 0f, 0.525f);
                void Check(Vector3 center, Quaternion rotation, string what)
                {
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        foreach (var sz in new[] { -1f, 1f })
                        {
                            var corner = center + rotation * new Vector3(half.x * sx, 0f, half.z * sz);
                            var v = cam.WorldToViewportPoint(corner);
                            Assert.That(v.z, Is.GreaterThan(0f), $"{what} in front");
                            Assert.That(v.x, Is.InRange(0.02f, 0.98f), $"{what} x at {cardCount} cards, aspect {aspect:0.00}");
                            Assert.That(v.y, Is.InRange(0.02f, 0.98f), $"{what} y at {cardCount} cards, aspect {aspect:0.00}");
                        }
                    }
                }

                for (var i = 0; i < fan.CardCount; i++)
                {
                    fan.GetFanPose(i, out var p, out var r);
                    Check(p, r, $"fan card {i}");
                }

                foreach (var slot in slots)
                {
                    Check(slot.position, slot.rotation, slot.name);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
```

- [ ] **Step 2: 确认失败**

```bash
bash $W/run/ut.sh EditMode t6-red -testFilter Phase72DrawRitualTests
```

Expected: 编译失败，因为 `ReadingRoomController` 还没有这三个字段（测试用 `SerializedObject` 取，不会编译失败，而是断言失败），并且场景里没有 `SpreadFanController`。

- [ ] **Step 3: 给 `ReadingRoomController` 加字段**

```csharp
        [Header("Phase72 Draw ritual")]
        [SerializeField] private SpreadFanController spreadFan;
        [SerializeField] private RitualStepIndicator stepIndicator;
        [Tooltip("Hidden while the player picks from the fan - the dock sits over the fan's table.")]
        [SerializeField] private CanvasGroup[] pickHiddenUi = System.Array.Empty<CanvasGroup>();
```

- [ ] **Step 4: 写引导脚本**

`Assets/Editor/Phase72DrawRitualBootstrapper.cs`：

```csharp
using System.Linq;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 72 wires the draw ritual: the fan the player picks from (MP_SpreadFan with its
    /// FanCenter), the draw camera poses (near for one and three cards, far for the Celtic
    /// Cross), the reading-room controller's new references and the UI it hides while the
    /// player picks, and the flight trail on the card prefab. Idempotent.
    /// </summary>
    public static class Phase72DrawRitualBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string WarmGlowPath = "Assets/Art/MidnightParlor/Materials/MP_WarmGlow.mat";

        public static readonly Vector3 FanCenterPosition = new Vector3(0f, 0.13f, -2.45f);
        public static readonly Vector3 NearPosePosition = new Vector3(0f, 5.0f, -6.3f);
        public static readonly Vector3 NearPoseEuler = new Vector3(50f, 0f, 0f);
        public const float NearPoseFov = 45f;
        public static readonly Vector3 CelticPosePosition = new Vector3(0.7f, 6.8f, -7.0f);
        public static readonly Vector3 CelticPoseEuler = new Vector3(45f, 0f, 0f);
        public const float CelticPoseFov = 50f;

        private static readonly string[] PickHiddenNames =
        {
            "Phase11_ActionDock", "QuestionInput", "OneCardButton", "ThreeCardButton", "CelticCrossButton", "DrawButton",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 72 Draw Ritual Bootstrap")]
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

            var glow = AssetDatabase.LoadAssetAtPath<Material>(WarmGlowPath);
            if (glow == null)
            {
                Debug.LogError("Phase 72: MP_WarmGlow missing; run the Phase 37 bootstrap first.");
                return;
            }

            AddFlightTrail(glow);
            WireScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 72 draw ritual bootstrap complete.");
        }

        private static void AddFlightTrail(Material glow)
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var existing = root.transform.Find("FlightTrail");
                var go = existing != null ? existing.gameObject : new GameObject("FlightTrail");
                if (existing == null)
                {
                    go.transform.SetParent(root.transform, false);
                }

                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                var trail = go.GetComponent<TrailRenderer>();
                if (trail == null)
                {
                    trail = go.AddComponent<TrailRenderer>();
                }

                trail.emitting = false;
                trail.time = 0.3f;
                trail.minVertexDistance = 0.02f;
                trail.widthMultiplier = 1f;
                trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(1f, 0f));
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(1f, 0.78f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.2f), 1f) },
                    new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
                trail.colorGradient = gradient;
                trail.sharedMaterial = glow;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.alignment = LineAlignment.View;

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var choreography = Object.FindFirstObjectByType<CameraChoreographyController>();
            var indicator = Object.FindFirstObjectByType<RitualStepIndicator>(FindObjectsInactive.Include);
            var canvas = GameObject.Find("ReadingRoomCanvas");
            if (room == null || choreography == null || indicator == null || canvas == null)
            {
                Debug.LogError("Phase 72: ReadingRoom is missing its controller, camera choreography, step indicator or canvas.");
                return;
            }

            // The fan.
            var fanGo = GameObject.Find("MP_SpreadFan") ?? new GameObject("MP_SpreadFan");
            fanGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var fan = fanGo.GetComponent<SpreadFanController>();
            if (fan == null)
            {
                fan = fanGo.AddComponent<SpreadFanController>();
            }

            var center = Child(fanGo.transform, "FanCenter");
            center.SetPositionAndRotation(FanCenterPosition, Quaternion.identity);
            var fanSo = new SerializedObject(fan);
            fanSo.FindProperty("cardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardView>(CardPrefabPath);
            fanSo.FindProperty("fanCenter").objectReferenceValue = center;
            fanSo.ApplyModifiedPropertiesWithoutUndo();

            // The draw camera poses.
            var near = Child(choreography.transform, "DrawPoseNear");
            near.SetPositionAndRotation(NearPosePosition, Quaternion.Euler(NearPoseEuler));
            var celtic = Child(choreography.transform, "DrawPoseCeltic");
            celtic.SetPositionAndRotation(CelticPosePosition, Quaternion.Euler(CelticPoseEuler));
            var camSo = new SerializedObject(choreography);
            var poses = camSo.FindProperty("drawPoses");
            poses.arraySize = 3;
            SetPose(poses.GetArrayElementAtIndex(0), 1, near, NearPoseFov);
            SetPose(poses.GetArrayElementAtIndex(1), 3, near, NearPoseFov);
            SetPose(poses.GetArrayElementAtIndex(2), 10, celtic, CelticPoseFov);
            camSo.ApplyModifiedPropertiesWithoutUndo();

            // The controller's references and the UI hidden while picking.
            var groups = PickHiddenNames.Select(n =>
            {
                var t = canvas.transform.Find(n);
                var group = t.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = t.gameObject.AddComponent<CanvasGroup>();
                }

                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
                EditorUtility.SetDirty(group);
                return group;
            }).ToArray();

            var roomSo = new SerializedObject(room);
            roomSo.FindProperty("spreadFan").objectReferenceValue = fan;
            roomSo.FindProperty("stepIndicator").objectReferenceValue = indicator;
            var hidden = roomSo.FindProperty("pickHiddenUi");
            hidden.arraySize = groups.Length;
            for (var i = 0; i < groups.Length; i++)
            {
                hidden.GetArrayElementAtIndex(i).objectReferenceValue = groups[i];
            }

            roomSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetPose(SerializedProperty entry, int cardCount, Transform pose, float fov)
        {
            entry.FindPropertyRelative("cardCount").intValue = cardCount;
            entry.FindPropertyRelative("pose").objectReferenceValue = pose;
            entry.FindPropertyRelative("fov").floatValue = fov;
        }

        private static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(parent, false);
            }

            return t;
        }
    }
}
```

注意：`GameObject.Find("MP_SpreadFan") ?? new GameObject(...)` 可以用，因为 `GameObject.Find` 找不到时返回真 null，不是 `GetComponent` 那种假 null。但为了和项目约定一致，执行时改成显式 `if` 判断。

- [ ] **Step 5: 运行引导（两次，验证幂等）**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
U=/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
$U -projectPath . -batchmode -executeMethod TarotUnity.Editor.Phase72DrawRitualBootstrapper.Run -quit -logFile $W/run/boot1.log; grep -n "Phase 72" $W/run/boot1.log
shasum Assets/Scenes/ReadingRoom.unity Assets/Prefabs/Cards/PF_TarotCard.prefab > $W/run/sha1
$U -projectPath . -batchmode -executeMethod TarotUnity.Editor.Phase72DrawRitualBootstrapper.Run -quit -logFile $W/run/boot2.log
shasum Assets/Scenes/ReadingRoom.unity Assets/Prefabs/Cards/PF_TarotCard.prefab | diff - $W/run/sha1 && echo IDEMPOTENT
```

Expected: 日志出现 `Tarot Unity Phase 72 draw ritual bootstrap complete.`，并输出 `IDEMPOTENT`。如果不幂等，先对比两次运行的 diff 找出每次都变的字段（例如 TMP 缓存）。

- [ ] **Step 6: 确认 EditMode 通过；视锥失败时调位姿**

```bash
bash $W/run/ut.sh EditMode t6-green -testFilter Phase72DrawRitualTests
```

Expected: 全部 PASS。如果 `TheDrawCameraHoldsTheFanAndEverySlot` 某个用例失败，按失败信息调整引导脚本里的常量，再重跑 Step 5 和 Step 6：
- x 越界：拉远 z 或增大 FOV。
- y 越界：调俯角。

每次调整都在 ledger 记下新数值。然后去掉 Task 4 里的 `Assume`，重跑：

```bash
bash $W/run/ut.sh PlayMode t6-trail -testFilter Phase72DealPickedTests
```

Expected: PASS，`trailSeen` 为 true。

- [ ] **Step 7: 提交**

```bash
git add Assets/Editor/Phase72DrawRitualBootstrapper.cs* Assets/Scenes/ReadingRoom.unity Assets/Prefabs/Cards/PF_TarotCard.prefab Assets/Scripts/UI/ReadingRoomController.cs Assets/Tests/EditMode/Phase72DrawRitualTests.cs Assets/Tests/PlayMode/Phase72DealPickedTests.cs
git status --short   # 确认字体图集不在暂存区
git commit -m "feat(draw): wire the fan, draw camera poses, hidden-while-picking UI and the flight trail (Phase 72)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: 新的抽牌流程（`DrawRoutine`）与旧测试迁移

**Files:**
- Modify: `Assets/Scripts/UI/ReadingRoomController.cs`
- Modify: `Assets/Scripts/Gameplay/ReadingFlowController.cs`
- Modify: `Assets/Scripts/UI/ReleaseUxCopy.cs`
- Create: `Assets/Tests/PlayMode/DrawRitualTestDriver.cs`
- Create: `Assets/Tests/PlayMode/Phase72DrawFlowTests.cs`
- Modify: `Assets/Tests/PlayMode/Phase36PerformanceProbeTests.cs`、`Phase66ReadingRoomOnlineFlowTests.cs`、`Phase66LiveBackendTests.cs`、`Phase70RevealReplacesDrawTests.cs`、`Phase71CardHoverTests.cs`、`VerticalSliceFlowTests.cs`

**Interfaces:**
- Consumes: 以上所有任务的产出。
- Produces:
  - `ReadingFlowController.AbortDraw()`
  - `ReleaseUxCopy.FlowPickPrompt(int remaining, int total)`
  - `ReleaseUxCopy.FlowReadingTheCards`（常量）
  - `DrawRitualTestDriver.PickAll(float timeoutSeconds = 30f)`：测试工具

- [ ] **Step 1: 写测试工具**

`Assets/Tests/PlayMode/DrawRitualTestDriver.cs`：

```csharp
using System.Collections;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 72: the draw waits for the player to pick from the fan. Tests that press
    /// 洗牌抽取 call this to pick for the player - always the middle-most remaining card -
    /// until the spread's cards are all dealt or the fan stops accepting picks.
    /// </summary>
    public static class DrawRitualTestDriver
    {
        public static IEnumerator PickAll(float timeoutSeconds = 30f)
        {
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            Assert.That(fan, Is.Not.Null, "the reading room has a fan");
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!fan.AcceptingPicks)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "the fan never opened for picks");
                yield return null;
            }

            while (fan.AcceptingPicks)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "picking did not finish");
                if (fan.FanCards.Count > 0)
                {
                    fan.RequestPick(fan.FanCards[fan.FanCards.Count / 2]);
                }

                yield return null;
            }
        }
    }
}
```

- [ ] **Step 2: 写失败的流程测试**

`Assets/Tests/PlayMode/Phase72DrawFlowTests.cs`：

```csharp
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Core;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Networking;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>Phase 72: the draw ritual end to end.</summary>
    public sealed class Phase72DrawFlowTests
    {
        private ReadingRoomController room;
        private ReadingFlowController flow;
        private DeckController deck;
        private SpreadFanController fan;

        private IEnumerator LoadRoom()
        {
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            yield return null;
            room = Object.FindFirstObjectByType<ReadingRoomController>();
            flow = Object.FindFirstObjectByType<ReadingFlowController>();
            deck = Object.FindFirstObjectByType<DeckController>();
            fan = Object.FindFirstObjectByType<SpreadFanController>();
        }

        [UnityTest]
        public IEnumerator ThreeCardRitualPicksLandsBindsAndFlips()
        {
            yield return LoadRoom();
            Get<Button>("threeCardButton").onClick.Invoke();
            Get<Button>("drawButton").onClick.Invoke();

            yield return Until(() => fan.AcceptingPicks, "the fan opens after the shuffle");
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.Drawing));
            Assert.That(fan.FanCards.Count, Is.EqualTo(22));
            Assert.That(Get<CanvasGroup[]>("pickHiddenUi").All(g => g.alpha < 0.01f && !g.blocksRaycasts), Is.True,
                "the dock steps aside while picking");
            Assert.That(Get<TMP_Text>("flowStatusText").text, Is.EqualTo(ReleaseUxCopy.FlowPickPrompt(3, 3)));

            var picked = new[] { fan.FanCards[2], fan.FanCards[10], fan.FanCards[19] };
            foreach (var card in picked)
            {
                fan.RequestPick(card);
                yield return Until(() => !fan.FanCards.Contains(card) && deck.ActiveCards.Contains(card), "the card lands");
            }

            yield return Until(() => flow.State == ReadingFlowState.WaitingForFlip, "the flip phase starts");
            Assert.That(deck.ActiveCards, Is.EqualTo(picked), "the cards picked are the cards on the table");
            Assert.That(picked.All(c => c.DrawData != null), Is.True, "bound after the picks");
            Assert.That(fan.FanCards.Count, Is.EqualTo(0), "the fan is gathered");
            Assert.That(Get<CanvasGroup[]>("pickHiddenUi").All(g => g.alpha > 0.99f), Is.True, "the dock returns");

            var flipper = Object.FindFirstObjectByType<CardFlipController>();
            foreach (var card in picked)
            {
                card.GetComponent<CardClickHandler>().OnPointerClick(
                    new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
                yield return Until(() => card.IsFaceUp && (flipper == null || !flipper.IsFlipping), "the card flips");
            }

            yield return Until(() => flow.State == ReadingFlowState.ResultReady, "the reading is ready");
        }

        [UnityTest]
        public IEnumerator CardsOnTheTableDoNotFlipBeforeTheyAreBound()
        {
            yield return LoadRoom();
            Get<Button>("threeCardButton").onClick.Invoke();
            Get<Button>("drawButton").onClick.Invoke();
            yield return Until(() => fan.AcceptingPicks, "the fan opens");

            var first = fan.FanCards[5];
            fan.RequestPick(first);
            yield return Until(() => deck.ActiveCards.Contains(first), "the first card lands");
            first.GetComponent<CardClickHandler>().OnPointerClick(
                new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            yield return new WaitForSeconds(0.8f);
            Assert.That(first.IsFaceUp, Is.False, "no flipping mid-pick");
        }

        [UnityTest]
        public IEnumerator CelticCrossPicksAllTenCards()
        {
            yield return LoadRoom();
            Get<Button>("celticCrossButton").onClick.Invoke();
            Get<Button>("drawButton").onClick.Invoke();
            yield return DrawRitualTestDriver.PickAll(60f);
            yield return Until(() => flow.State == ReadingFlowState.WaitingForFlip, "the flip phase starts", 60f);
            Assert.That(deck.ActiveCards.Count, Is.EqualTo(10));
            Assert.That(deck.ActiveCards.All(c => c.DrawData != null), Is.True);
        }

        private T Get<T>(string name) where T : class
        {
            var field = typeof(ReadingRoomController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return field.GetValue(room) as T;
        }

        private static IEnumerator Until(System.Func<bool> condition, string message, float seconds = 20f)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), message);
                yield return null;
            }
        }
    }
}
```

BackendOnly 失败的测试需要 mock 后端，放进 `Phase66ReadingRoomOnlineFlowTests`（它已有 `MockTarotBackend` 的 SetUp）。在该类中新增：

```csharp
        [UnityTest]
        public IEnumerator BackendOnlyFailureReturnsCardsAndAllowsARetry()
        {
            server.Script("POST", "/api/v1/records/", MockTarotBackend.Json(500, "{\"detail\":\"boom\"}"));

            yield return LoadReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            typeof(ReadingRoomController).GetField("backendMode", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(room, BackendIntegrationMode.BackendOnly);
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "oneCardButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            yield return DrawRitualTestDriver.PickAll();
            yield return WaitUntil(() => GetField<Button>(room, "drawButton").interactable, 30f, "the draw comes back");

            Assert.That(deck.ActiveCards.Count, Is.EqualTo(0), "the picked card went back to the deck");
            Assert.That(GetField<TMP_Text>(room, "flowStatusText").text, Does.StartWith("后端连接失败"));
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.ReadyToDraw), "ready to try again");

            GetField<Button>(room, "drawButton").onClick.Invoke();
            yield return null;
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.Shuffling), "a retry starts a new shuffle");
        }
```

执行时先读 `MockTarotBackend.Json` 的用法和 `BackendIntegrationMode` 的命名空间（`grep -rn "enum BackendIntegrationMode" Assets/Scripts`），按实际情况补 `using`。如果 `POST /records/` 返回 500 时后端服务走的是离线回退而不是 `OfflineMessage != null`，就改为让 spreads 列表返回 503（BackendOnly 下的失败路径），并在 ledger 记一条 Ruling。

- [ ] **Step 3: 确认失败**

```bash
bash $W/run/ut.sh PlayMode t7-red -testFilter "Phase72DrawFlowTests|BackendOnlyFailureReturnsCardsAndAllowsARetry"
```

Expected: 编译失败（`FlowPickPrompt` 不存在），或 `fan.AcceptingPicks` 超时。

- [ ] **Step 4: 实现**

`ReleaseUxCopy.cs`（放在 `FlowDealing` 附近）：

```csharp
        public const string FlowReadingTheCards = "正在感应牌面……";

        public static string FlowPickPrompt(int remaining, int total)
        {
            return remaining >= total ? $"凭直觉，选出 {total} 张牌。" : $"还要再选 {remaining} 张。";
        }
```

`ReadingFlowController.cs`：

```csharp
        /// <summary>
        /// Phase 72: a BackendOnly start that failed leaves the draw; the question and spread
        /// still stand, so the player can press 洗牌抽取 again.
        /// </summary>
        public void AbortDraw()
        {
            if (State == ReadingFlowState.Shuffling || State == ReadingFlowState.Drawing)
            {
                flippedCards.Clear();
                SetState(ReadingFlowState.ReadyToDraw);
            }
        }
```

`ReadingRoomController.cs`：
- 在 `using` 中确认有 `TarotUnity.Gameplay`。
- `Awake` 中订阅 `deckController.CardHovering += HandleCardHovering;`，`OnDestroy` 中取消订阅，并新增：

```csharp
        private void HandleCardHovering(CardView card)
        {
            stepIndicator?.FlashFocusedSocket();
        }
```

- 新增：

```csharp
        private void SetPickUiHidden(bool hidden)
        {
            foreach (var group in pickHiddenUi)
            {
                if (group == null)
                {
                    continue;
                }

                group.alpha = hidden ? 0f : 1f;
                group.interactable = !hidden;
                group.blocksRaycasts = !hidden;
            }
        }

        private IEnumerator DeliverPick(CardView card, int index, IList<Transform> slots)
        {
            if (index >= slots.Count)
            {
                yield break;
            }

            yield return deckController.DealPickedCard(card, slots[index]);
            var next = index + 1;
            stepIndicator?.FocusSocket(next < slots.Count ? next : -1);
            var remaining = slots.Count - next;
            if (remaining > 0)
            {
                SetStatus(ReleaseUxCopy.FlowPickPrompt(remaining, slots.Count));
            }
        }
```

- 用下面这段替换 `DrawRoutine` 中从 `yield return new WaitForSeconds(rhythmDirector != null ...` 到方法结尾的部分（前面的问题、`BeginShuffle`、`FocusDeck`、cue、`deckShuffle?.Play()`、`SetStatus(FlowShuffling)`、`OnlineStartAttempt` 启动都保留）：

```csharp
            // Phase 72: wait for the shuffle itself, not a fixed breath.
            while (deckShuffle != null && deckShuffle.IsPlaying)
            {
                yield return null;
            }

            // The fan opens at once - the online start keeps running behind the picks.
            flowController?.BeginDeal();
            deckController?.Clear();
            var slots = flowController != null ? flowController.GetSelectedSpreadSlots() : new List<Transform>();
            var deckOrigin = deckShuffle != null ? deckShuffle.transform : deckController != null ? deckController.transform : transform;
            cameraChoreography?.FocusDraw(selectedCardCount);
            SetPickUiHidden(true);
            if (spreadFan != null && deckController != null)
            {
                yield return spreadFan.Spread(deckOrigin);
                stepIndicator?.FocusSocket(0);
                SetStatus(ReleaseUxCopy.FlowPickPrompt(slots.Count, slots.Count));
                yield return spreadFan.PickCards(slots.Count, (card, index) => DeliverPick(card, index, slots));
                stepIndicator?.FocusSocket(-1);
                yield return spreadFan.Gather(deckOrigin);
            }

            SetPickUiHidden(false);
            SetStatus(ReleaseUxCopy.FlowReadingTheCards);

            var onlineStartTimeoutSeconds = OnlineStartTimeoutSeconds();
            var giveUpAt = Time.realtimeSinceStartup + onlineStartTimeoutSeconds;
            yield return new WaitUntil(() => attempt.Done || Time.realtimeSinceStartup > giveUpAt);
            if (!attempt.Done)
            {
                attempt.Session = null;
                attempt.OfflineMessage = ReleaseUxCopy.OfflineBecauseNetwork;
                attempt.RawError = $"the online start did not finish within {onlineStartTimeoutSeconds} s";
                Debug.Log($"ReadingRoom: {attempt.RawError}");
            }

            var session = attempt.Session;
            if (session == null && attempt.OfflineMessage != null && backendMode == BackendIntegrationMode.BackendOnly)
            {
                if (deckController != null)
                {
                    yield return deckController.ReturnDealtCards();
                }

                flowController?.AbortDraw();
                cameraChoreography?.PlayOpening();
                var message = ReleaseUxCopy.BackendOnlyFailure(attempt.RawError);
                SetStatus(message);
                SetReleaseStatus(message);
                SetDrawControls(true);
                drawInProgress = false;
                yield break;
            }

            if (session == null)
            {
                if (attempt.OfflineMessage != null)
                {
                    SetReleaseStatus(attempt.OfflineMessage);
                }

                session = LocalReadingSimulator.CreateSession(
                    selectedSpreadId,
                    selectedSpreadName,
                    question,
                    "general",
                    CreateLocalDraws());
            }

            ReadingSessionStore.Save(session);
            if (session.source == ReadingSource.Online)
            {
                BeginInterpretation(session);
            }

            var draws = session.cardDraws ?? CreateLocalDraws();
            if (deckController != null)
            {
                deckController.BindDealtCards(draws);
                if (rhythmDirector != null && rhythmDirector.DealSettleSeconds > 0f)
                {
                    yield return new WaitForSeconds(rhythmDirector.DealSettleSeconds);
                }

                WireActiveCards();
            }

            flowController?.WaitForCardFlips();
            cameraChoreography?.FocusSpread(selectedCardCount);
            SetStatus(ReleaseUxCopy.FlowFlipPrompt);
            drawInProgress = false;
```

说明：
- `deckOrigin` 用 `deckShuffle.transform`（`MP_DeckStack`，可见的牌堆），扇面牌从真正看得见的牌堆滑出。
- 离线路径（`LocalSimulation`）下 `attempt.Done` 在开头就已为 true，选完立即绑定。
- `rhythmDirector.ResolvePause(ShuffleStarted)` 不再被 `DrawRoutine` 使用。字段和 `ResolvePause` 都保留，因为 Phase 9 测试引用它们。

- [ ] **Step 5: 迁移旧测试**

在每个「点 `drawButton` 之后等发牌完成」的测试里，紧跟 `drawButton.onClick.Invoke()` 加一行 `yield return DrawRitualTestDriver.PickAll();`。凯尔特的用例用 `PickAll(60f)`，`Phase66LiveBackendTests` 的用例用 `PickAll(120f)`。

原来只等 `deck.ActiveCards.Count == N` 的等待条件，改为同时要求 `flow.State == ReadingFlowState.WaitingForFlip`。原因：落位早于绑定，只看张数会在会话保存之前就去读 `ReadingSessionStore.Current`。需要 `flow` 的地方用 `Object.FindFirstObjectByType<ReadingFlowController>()`。

涉及位置（用 `grep -n "drawButton\").onClick.Invoke\|DrawButton\").GetComponent<Button>().onClick.Invoke\|draw.onClick.Invoke" Assets/Tests/PlayMode/*.cs` 复核）：
- `Phase36PerformanceProbeTests.cs` 第 52 行后。这个测试在 `SampleWhile(flow.State != WaitingForFlip)` 里采样帧时间。改为先 `PickAll()` 再采样；PickAll 期间不计入发牌样本，并在注释里说明。
- `Phase66ReadingRoomOnlineFlowTests.cs` 的 7 处。
- `Phase66LiveBackendTests.cs` 中所有点 `drawButton` 的地方。
- `Phase70RevealReplacesDrawTests.cs` 第 40 行后。
- `Phase71CardHoverTests.cs` 第 33 行后。
- `VerticalSliceFlowTests.cs` 第 47 行后。
- `Phase69RuntimeSkinTests.cs` 第 72 行只检查「按下后按钮不可用」，不需要 PickAll。但测试结束时抽牌协程仍在等点选，TearDown 换场景会销毁它，这没有问题，不改。

- [ ] **Step 6: 确认新测试与迁移后的旧测试全部通过**

```bash
bash $W/run/ut.sh PlayMode t7-green
```

Expected: 全部 PASS（Phase66Live 的 3 个仍然 skipped）。失败时逐个读失败信息：
- 等待超时：先确认 `PickAll` 被调用到。
- 断言文案不符：对照 `ReleaseUxCopy`。

- [ ] **Step 7: 跑 EditMode 全量**

```bash
bash $W/run/ut.sh EditMode t7-edit
```

Expected: 全部 PASS。如果有 EditMode 测试断言 `DrawRoutine` 用了 `ResolvePause` 或 `DealCards`（用 `grep -rn "DealCards\|ResolvePause" Assets/Tests/EditMode` 复核），按新行为改写，并在 `Docs/PHASE72_DRAW_RITUAL.md` 里记录原因。

- [ ] **Step 8: 提交**

```bash
git add Assets/Scripts/UI/ReadingRoomController.cs Assets/Scripts/Gameplay/ReadingFlowController.cs Assets/Scripts/UI/ReleaseUxCopy.cs Assets/Tests/PlayMode/
git status --short
git commit -m "feat(draw): the draw routine - shuffle, spread, the player picks, gather, then bind and flip (Phase 72)

Old flow tests pick through DrawRitualTestDriver; a BackendOnly failure returns the cards and allows a retry.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: 截图、文档、全量回归

**Files:**
- Create: `Assets/Editor/Phase72CaptureBuilder.cs`
- Create: `Docs/PHASE72_DRAW_RITUAL.md`
- Test: `Assets/Tests/EditMode/Phase72DrawRitualTests.cs`（文档存在性）

- [ ] **Step 1: 写失败的文档测试**

```csharp
        [Test]
        public void Phase72DocumentationExists()
        {
            const string path = "Docs/PHASE72_DRAW_RITUAL.md";
            Assert.That(System.IO.File.Exists(path), Is.True);
            var text = System.IO.File.ReadAllText(path);
            Assert.That(text, Does.Contain("SpreadFanController"));
            Assert.That(text, Does.Contain("DealPickedCard"));
        }
```

```bash
bash $W/run/ut.sh EditMode t8-red -testFilter Phase72DrawRitualTests.Phase72DocumentationExists
```

Expected: FAIL（文件不存在）。

- [ ] **Step 2: 写截图脚本**

`Assets/Editor/Phase72CaptureBuilder.cs`：打开 ReadingRoom，对 1、3、10 张三种牌阵，以及 16:9（2560×1440）和 4:3（1920×1440）两种比例，逐一出图：
- 相机放到 `TryGetDrawPose` 给出的位姿，关闭所有 Canvas。
- 在 `SpreadFanController` 的扇面位姿上摆 22 张牌背朝上的牌（`PrefabUtility.InstantiatePrefab` + `SetFaceUp(false)`），文件名 `Fan_<n>_<aspect>.png`。
- 3 张牌阵另出三张：
  - `Fan_hover.png`：第 10 张抬高 0.05、滑出 0.3，第 9 和第 11 张抬高 0.04，模拟波浪。
  - `Pick_hover.png`：一张牌在第 12 张位置上方 0.55 处，`SetHaloBoost(1.6f)`。
  - `Pick_flight.png`：一张牌在飞行弧线中点，TrailRenderer 用 `AddPosition` 填入它身后的 8 个点。

渲染统一用 `CaptureRig.RenderConverged`（参照 `Phase71CaptureBuilder.Render`）。输出目录取环境变量 `PHASE72_CAPTURE_DIR`，不写进仓库。脚本结构照搬 `Phase71CaptureBuilder`：`Run()` → 检查环境变量 → 各 Capture 方法 → `Render(camera, path)`。

- [ ] **Step 3: 出图并自检**

```bash
cd /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
CAP=/private/tmp/claude-501/-Users-maochuandou-BUPT-Game/790b0dfd-6ecb-40b1-a435-0828f97da096/scratchpad/p72-captures
PHASE72_CAPTURE_DIR=$CAP $U -projectPath . -batchmode -executeMethod TarotUnity.Editor.Phase72CaptureBuilder.Run -quit -logFile $W/run/cap1.log; ls $CAP
```

用 Read 逐张查看，检查：
- 扇面完整、没有被裁；
- 与凯尔特牌位不重叠；
- 悬停的牌明显突出；
- 悬空光晕不过曝（必要时用 `scratchpad/lum.py` 测亮度）；
- 光轨可见但不刺眼。

发现问题就调 Task 3、4、6 的参数，重跑引导脚本和截图，并在 ledger 记下调整。

- [ ] **Step 4: 写文档**

`Docs/PHASE72_DRAW_RITUAL.md`，结构参照 `Docs/PHASE71_ANIMATION_FIXES.md`，中文，内容包括：
1. 用户反馈和目标。
2. 新流程：三拍洗牌 → 扇面 → 点选 → 四拍飞牌 → 收拢 → 绑定 → 翻牌。
3. 组件：
   - `DeckShuffleChoreographer`：三拍；奇偶分叠，保证每张牌落回自己原位。
   - `SpreadFanController`：几何、波浪、离开延迟、排队规则。
   - `DeckController.DealPickedCard / BindDealtCards / ReturnDealtCards`。
   - `CameraChoreographyController.drawPoses`：说明为什么按牌数分近景和远景。
   - `RitualStepIndicator.FocusSocket / FlashFocusedSocket`。
   - `ReadingFlowController.AbortDraw`：修复 BackendOnly 失败后无法重抽。
4. 引导脚本 `Phase72DrawRitualBootstrapper`：菜单名、可重复运行、写入了哪些对象。
5. 改写过的旧测试及原因：`DrawRitualTestDriver`；「只看张数」改为「看状态」。
6. 可调参数：各拍时长、扇面半径和张角、波浪、光晕增益、光轨。
7. 截图文件说明，以及需要在 Unity 里验收的手感项。

- [ ] **Step 5: 全量回归**

```bash
bash $W/run/ut.sh EditMode t8-full
bash $W/run/ut.sh PlayMode t8-full
bash /Users/maochuandou/BUPT/Game/UnityTarot/.superpowers/sdd/2026-09-12-online-interpretation-unity/scratch/online-a-run/pt.sh tests
```

Expected：
- EditMode：全部 PASS，在 512 基础上新增约 12 个。
- PlayMode：全部 PASS，3 个 skipped（Phase66Live）。
- 后端：190 PASS。

- [ ] **Step 6: 提交**

```bash
git add Assets/Editor/Phase72CaptureBuilder.cs* Docs/PHASE72_DRAW_RITUAL.md* Assets/Tests/EditMode/Phase72DrawRitualTests.cs
git status --short   # 字体图集不入库
git commit -m "docs(draw): Phase 72 draw ritual notes and review captures

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Self-Review 记录

- **Spec 覆盖：**
  - 第 1 段流程 → Task 7。
  - 第 2 段扇面与悬停 → Task 3；镜头 → Task 5 和 6；动作坞隐藏 → Task 6 和 7。
  - 第 3 段飞牌与光轨 → Task 4 和 6；下一格发光与闪光 → Task 5 和 7。
  - 第 4 段洗牌 → Task 1；等动画播完 → Task 7。
  - 第 6 段出错与边界 → Task 7（BackendOnly、AbortDraw）；4:3 → Task 6。
  - 第 7 段测试 → 各任务。
  - 扇面牌数 ≥ 最大牌阵的守护：`FanPosesSpanTheArcAndLayerLeftToRight` 断言 22 张，凯尔特用例覆盖 10 张。
- **与 spec 的有意偏离（执行时抄进 ledger 作为 Ruling）：**
  1. `drawPose` 改为按牌数的 `drawPoses`（理由见 Task 5）。
  2. spec 第 5 段写「`CardView` 新增可翻开关」。计划不加这个开关，因为 `HandleCardClicked` 已经要求 `State == WaitingForFlip`，而且落位的牌要等绑定后 `WireActiveCards` 才接上翻牌事件，已有门控足够。由 `CardsOnTheTableDoNotFlipBeforeTheyAreBound` 测试守住。
  3. 点选提示音不加。现有音效是按提示类型合成的音调，加新提示需要新增提示类型，属于新增音效，超出本期范围。
- **类型一致性：** `DealPickedCard(CardView, Transform)`、`PickCards(int, Func<CardView,int,IEnumerator>)`、`FocusSocket(int)`、`TryGetDrawPose(int, out Transform, out float)`、`HoverHaloScale`、`SetHaloBoost(float)`、`ClearHoverHaloScale()`、`HoverChanged`、`Resume()`、`AbortDraw()`、`FlowPickPrompt(int,int)` 在各任务中名称一致。

using System.Collections.Generic;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 25 re-composes the Result screen, the payoff of the whole flow and
    /// the weakest screen after Phase 24. Thirteen phases of additive bootstraps
    /// left it unbalanced: the gold section labels sat at x -390, inside the card
    /// showcase footprint, while the body text drifted right at x 70 / width 650
    /// into dead space, so labels and values never read as pairs.
    ///
    /// The data flow (ResultPanelPresenter) shows the first drawn card's real
    /// artwork on the left and the AI interpretation on the right, so the
    /// composition follows it: a full-width question/spread header, a card hero
    /// in the left third, and a backed reading panel in the right two-thirds with
    /// each section as an aligned gold header + body pair, then a centered footer.
    /// Representative placeholder copy is written so the static capture reflects a
    /// real reading; ResultPanelPresenter overwrites it at runtime.
    ///
    /// Everything is repositioned, restyled, or (for conflicting legacy overlays)
    /// deactivated - never renamed or deleted - so the serialized presenter
    /// references stay intact.
    /// </summary>
    public static class Phase25ResultCompositionBootstrapper
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";

        private static readonly Color HeaderGold = new(0.87f, 0.66f, 0.30f, 1f);
        private static readonly Color BodyIvory = new(0.92f, 0.88f, 0.78f, 1f);
        private static readonly Color MutedIvory = new(0.78f, 0.74f, 0.70f, 1f);
        private static readonly Color ReadingPanelColor = new(0.045f, 0.032f, 0.060f, 0.62f);
        private static readonly Color OracleFrameColor = new(0.05f, 0.035f, 0.072f, 0.30f);
        private static readonly Color CardShowcaseColor = new(0.10f, 0.070f, 0.045f, 0.80f);
        private static readonly Color CardPlateColor = new(0.86f, 0.78f, 0.58f, 0.20f);
        private static readonly Color EmptyArtworkColor = new(0.80f, 0.74f, 0.60f, 1f);

        // Right reading column and left card hero share a vertical band.
        private const float ColumnCenterX = 178f;
        private const float ColumnWidth = 704f;
        private const float CardCenterX = -392f;

        private static Dictionary<string, Transform> sceneLookup;

        [MenuItem("Tools/Tarot Unity/Run Phase 25 Result Composition Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 25 Result Composition Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 25 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ResultScenePath);
            BuildLookup();

            ComposeHeader();
            ComposeOracleFrame();
            ComposeCardHero();
            ComposeReadingPanel();
            ComposeFooter();
            QuietLegacyOverlays();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ResultScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 25 result composition bootstrap complete.");
        }

        private static void ComposeHeader()
        {
            PlaceText("QuestionText", 0f, 300f, 1060f, 46f, 24, TextAnchor.MiddleCenter, BodyIvory,
                "我们之间还会有未来吗？");
            PlaceText("SpreadNameText", 0f, 264f, 1060f, 30f, 18, TextAnchor.MiddleCenter, MutedIvory,
                "三牌阵 · 过去 · 现在 · 建议");
            PlaceImage("Phase8_ResultGoldDividerTop", 0f, 238f, 1064f, 3f, HeaderGold);
        }

        private static void ComposeOracleFrame()
        {
            // The Phase 7 oracle frame is kept (an earlier sibling, so it draws
            // behind) and repurposed as a faint unifying stage spanning the card
            // hero and the reading column, rather than a tight box around text.
            var frame = Find("Phase7_ResultOracleFrame");
            if (frame != null && !frame.gameObject.activeSelf)
            {
                frame.gameObject.SetActive(true);
                EditorUtility.SetDirty(frame.gameObject);
            }

            PlaceImage("Phase7_ResultOracleFrame", -8f, 2f, 1150f, 474f, OracleFrameColor);
        }

        private static void ComposeCardHero()
        {
            // The card hero sits centred in the left third, aligned to the same
            // vertical band as the reading column; the layered showcase, plate,
            // and artwork slot share a centre so the real card art reads as one
            // framed object.
            PlaceImage("Phase12_ResultCardShowcase", CardCenterX, 2f, 268f, 420f, CardShowcaseColor);
            PlaceImage("Phase12_ResultCardPlaceholder", CardCenterX, 2f, 236f, 388f, CardPlateColor);
            PlaceImage("Phase12_ResultCardArtworkSlot", CardCenterX, 2f, 210f, 356f, EmptyArtworkColor);
            Place("Phase14_ResultCardHalo", CardCenterX, 2f, 322f, 478f);
            Place("Phase14_ResultCardShadow", CardCenterX, -12f, 252f, 400f);
        }

        private static void ComposeReadingPanel()
        {
            PlaceImage("Phase8_ResultScrollPanel", ColumnCenterX, 2f, ColumnWidth + 20f, 440f, ReadingPanelColor);

            // Top-down cursor: each section is a gold accent header stacked
            // directly above its body, all sharing the column's left edge, so the
            // eye reads clean header/body pairs from top to bottom with no
            // overlap. Bodies stay at 19 to honour the OverallText >= 19 invariant
            // other phases assert.
            var top = 200f;

            float Stack(string name, float height, int fontSize, Color color, string copy, bool accent)
            {
                StackText(name, top, height, fontSize, color, copy, accent);
                return top - height;
            }

            top = Stack("Phase7_ResultSectionSummary", 34f, 20, HeaderGold, "概要", true) - 2f;
            top = Stack("SummaryText", 32f, 19, BodyIvory,
                "你在等一个明确的答复，却又怕听见它。", false) - 16f;
            top = Stack("Phase7_ResultSectionOverall", 34f, 20, HeaderGold, "整体解读", true) - 2f;
            top = Stack("OverallText", 62f, 19, BodyIvory,
                "过去的牌显示你们曾有真实的联结，但被未说出口的顾虑慢慢磨蚀。现在的牌停在犹豫上：双方都在等对方先伸手。", false) - 16f;
            top = Stack("Phase7_ResultSectionCards", 34f, 20, HeaderGold, "牌面分析", true) - 2f;
            top = Stack("CardAnalysisText", 62f, 19, BodyIvory,
                "宝剑二代表回避与僵持，星币六提示一次重新平衡的机会，圣杯王后则邀请你以真诚而非试探回应。", false) - 16f;
            top = Stack("Phase7_ResultSectionAdvice", 34f, 20, HeaderGold, "建议", true) - 2f;
            Stack("AdviceText", 58f, 19, BodyIvory,
                "先放下输赢的算计，主动开启一次坦诚的对话，未来才有重新生长的空间。", false);
        }

        private static void ComposeFooter()
        {
            PlaceImage("Phase8_ResultGoldDividerBottom", 0f, -226f, 1064f, 3f, HeaderGold);
            PlaceText("WarningText", 0f, -254f, 980f, 28f, 15, TextAnchor.MiddleCenter, MutedIvory, string.Empty);
            Place("BackToMenuButton", 0f, -300f, 248f, 48f);
        }

        private static void QuietLegacyOverlays()
        {
            // Early result-layout experiments that now overlap the cleaned
            // composition. Deactivated (not deleted) like the Phase 22 overlays.
            foreach (var name in new[]
                     {
                         "Phase11_ResultReadingColumns",
                         "Phase11_ResultCardPresence",
                         "Phase11_ResultCardFace",
                         "Phase14_ResultTextBridge",
                         // Redundant decorative crest ("牌面回声") that floated over
                         // the new reading column; the question header sets context now.
                         "Phase8_ResultCrest",
                     })
            {
                var t = Find(name);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                    EditorUtility.SetDirty(t.gameObject);
                }
            }
        }

        // --- placement helpers -------------------------------------------------

        private static void Place(string name, float x, float y, float width, float height)
        {
            var rect = FindRect(name);
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            EditorUtility.SetDirty(rect);
        }

        private static void PlaceImage(string name, float x, float y, float width, float height, Color color)
        {
            Place(name, x, y, width, height);
            var rect = FindRect(name);
            var image = rect != null ? rect.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = color;
                EditorUtility.SetDirty(image);
            }
        }

        // Stacks a reading-column element from a top line downward (top pivot),
        // so callers can lay sections out with a simple descending cursor.
        private static void StackText(string name, float top, float height, int fontSize, Color color, string copy, bool accent)
        {
            var rect = FindRect(name);
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(ColumnCenterX, top);
            rect.sizeDelta = new Vector2(ColumnWidth, height);

            var text = rect.GetComponent<Text>();
            if (text != null)
            {
                text.fontSize = fontSize;
                text.alignment = TextAnchor.UpperLeft;
                text.color = color;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                if (copy != null)
                {
                    text.text = copy;
                }

                EditorUtility.SetDirty(text);
            }

            // Accent headers keep their gold at runtime: TarotUiTheme repaints
            // text by size on Awake, and this marker opts them out of that.
            if (accent && rect.GetComponent<TarotUiAccentText>() == null)
            {
                rect.gameObject.AddComponent<TarotUiAccentText>();
            }

            EditorUtility.SetDirty(rect);
        }

        private static void PlaceText(string name, float x, float y, float width, float height,
            int fontSize, TextAnchor anchor, Color color, string copy)
        {
            Place(name, x, y, width, height);
            var rect = FindRect(name);
            var text = rect != null ? rect.GetComponent<Text>() : null;
            if (text == null)
            {
                return;
            }

            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (copy != null)
            {
                text.text = copy;
            }

            EditorUtility.SetDirty(text);
        }

        // --- scene lookup ------------------------------------------------------

        private static void BuildLookup()
        {
            sceneLookup = new Dictionary<string, Transform>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                sceneLookup[t.name] = t;
            }
        }

        private static Transform Find(string name)
        {
            if (sceneLookup != null && sceneLookup.TryGetValue(name, out var t))
            {
                return t;
            }

            Debug.LogWarning($"Phase 25 bootstrap could not find '{name}' in {ResultScenePath}.");
            return null;
        }

        private static RectTransform FindRect(string name)
        {
            var t = Find(name);
            return t as RectTransform;
        }
    }
}

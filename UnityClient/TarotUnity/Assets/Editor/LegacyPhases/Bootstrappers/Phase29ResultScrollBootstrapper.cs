using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 29 replaces Phase 26's best-fit shrink with a real scrollable reading
    /// panel. Phase 25 laid the four interpretation sections out in fixed boxes;
    /// long backend AI copy either overflowed (pre-26) or shrank to stay in the
    /// box (26). This wraps the sections in a ScrollRect whose Content auto-sizes
    /// to the text (VerticalLayoutGroup + ContentSizeFitter), so any length renders
    /// at full size and scrolls if it exceeds the viewport. Short copy still shows
    /// fully with no scrolling.
    ///
    /// The four header/body pairs are reparented into the scroll Content (their
    /// serialized presenter references stay intact); best-fit is turned off so the
    /// layout drives size. The card hero, question header, dividers, and footer are
    /// untouched.
    /// </summary>
    public static class Phase29ResultScrollBootstrapper
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const float ColumnCenterX = 178f;
        private static readonly Color ViewportBg = new(0.045f, 0.032f, 0.060f, 0.66f);

        // Header then body, in reading order.
        private static readonly string[] SectionOrder =
        {
            "Phase7_ResultSectionSummary", "SummaryText",
            "Phase7_ResultSectionOverall", "OverallText",
            "Phase7_ResultSectionCards", "CardAnalysisText",
            "Phase7_ResultSectionAdvice", "AdviceText",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 29 Result Scroll Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ResultScenePath);
            var canvas = GameObject.Find("ResultCanvas");
            if (canvas == null)
            {
                Debug.LogError("Phase 29: ResultCanvas not found.");
                return;
            }

            var lookup = BuildLookup();
            var scrollView = EnsureScrollView(canvas.transform, lookup, out var content);
            if (scrollView == null)
            {
                return;
            }

            ReparentSections(content, lookup);

            // The Phase 25 backing panel is now redundant (the viewport carries the
            // panel colour); deactivate it, do not delete.
            if (lookup.TryGetValue("Phase8_ResultScrollPanel", out var panel))
            {
                panel.gameObject.SetActive(false);
                EditorUtility.SetDirty(panel.gameObject);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ResultScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 29 result scroll bootstrap complete.");
        }

        private static RectTransform EnsureScrollView(Transform canvas, Dictionary<string, Transform> lookup, out RectTransform content)
        {
            var existing = lookup.TryGetValue("ResultReadingScroll", out var found) ? found as RectTransform : null;
            RectTransform scrollRT;
            ScrollRect scroll;
            if (existing != null)
            {
                scrollRT = existing;
                scroll = scrollRT.GetComponent<ScrollRect>();
            }
            else
            {
                var go = new GameObject("ResultReadingScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
                scrollRT = (RectTransform)go.transform;
                scrollRT.SetParent(canvas, false);
                scroll = go.GetComponent<ScrollRect>();
            }

            // 736 wide with 14px side padding leaves 708px of body width, clearing the
            // Phase 8 (>=700) and Phase 12 (>=660) reading-column width invariants.
            Configure(scrollRT, new Vector2(0.5f, 0.5f), new Vector2(ColumnCenterX, 4f), new Vector2(736f, 448f));
            var scrollImg = scrollRT.GetComponent<Image>();
            scrollImg.color = ViewportBg;
            scrollImg.raycastTarget = true;

            var viewport = EnsureChild(scrollRT, "Viewport");
            Stretch(viewport);
            EnsureComponent<RectMask2D>(viewport.gameObject);
            var vpImg = EnsureComponent<Image>(viewport.gameObject);
            vpImg.color = new Color(1f, 1f, 1f, 0.001f);
            vpImg.raycastTarget = true;

            content = EnsureChild(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var vlg = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
            vlg.padding = new RectOffset(14, 14, 12, 14);
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = EnsureComponent<ContentSizeFitter>(content.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 22f;
            scroll.inertia = true;
            scroll.viewport = viewport;
            scroll.content = content;
            EditorUtility.SetDirty(scrollRT);
            return scrollRT;
        }

        private static void ReparentSections(RectTransform content, Dictionary<string, Transform> lookup)
        {
            for (var i = 0; i < SectionOrder.Length; i++)
            {
                if (!lookup.TryGetValue(SectionOrder[i], out var section) || section == null)
                {
                    Debug.LogWarning($"Phase 29: missing section {SectionOrder[i]}");
                    continue;
                }

                section.SetParent(content, false);
                section.SetSiblingIndex(i);

                var rt = (RectTransform)section;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;

                var text = section.GetComponent<Text>();
                if (text != null)
                {
                    text.resizeTextForBestFit = false;
                    text.alignment = TextAnchor.UpperLeft;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                    EditorUtility.SetDirty(text);
                }

                EditorUtility.SetDirty(section);
            }
        }

        private static void Configure(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static RectTransform EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private static Dictionary<string, Transform> BuildLookup()
        {
            var map = new Dictionary<string, Transform>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                map[t.name] = t;
            }

            return map;
        }
    }
}

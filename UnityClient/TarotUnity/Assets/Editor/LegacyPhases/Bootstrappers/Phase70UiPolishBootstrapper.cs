using System.Linq;
using TMPro;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 70 polishes the Phase 69 UI after the user's first play-through:
    /// - the menu and the reading room keep the whole 1280-wide design on windows narrower than
    ///   16:9, with the same match switch the result page has used since Phase 67;
    /// - the question text is centred over its underline;
    /// - 揭示结果 shares 洗牌抽取's slot (ReadingRoomController swaps them), so the row is the four
    ///   buttons a player actually sees, centred, and the dock hugs it;
    /// - the step bar and the Phase 16 table aura (rotating rune rings, glow pool, light spheres)
    ///   stop drawing. Both objects stay active: Phase 7 and Phase 16-21 tests find them with
    ///   GameObject.Find, and the aura root also carries the Phase 18/19 particles. This follows
    ///   Phase 35, which hid the result page's aura the same way.
    /// Runs after the Phase 69 restyle (which lays the row out for five buttons). Idempotent.
    /// </summary>
    public static class Phase70UiPolishBootstrapper
    {
        public const string ReadingRoomPath = "Assets/Scenes/ReadingRoom.unity";
        public const string MenuPath = "Assets/Scenes/MainMenu.unity";

        private static readonly string[] RowButtons = { "OneCardButton", "ThreeCardButton", "CelticCrossButton", "DrawButton" };

        private static readonly string[] TableAuraVisuals =
        {
            "Phase16_GlowPool", "Phase16_RuneRingOuter", "Phase16_RuneRingInner",
            "Phase16_ParticleAnchorNorth", "Phase16_ParticleAnchorEast",
            "Phase16_ParticleAnchorSouth", "Phase16_ParticleAnchorWest",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 70 UI Polish Bootstrap")]
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

            PolishMenu();
            PolishReadingRoom();
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 70 UI polish complete.");
        }

        private static void PolishMenu()
        {
            var scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            FitNarrowWindows(GameObject.Find("MainMenuCanvas"));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void PolishReadingRoom()
        {
            var scene = EditorSceneManager.OpenScene(ReadingRoomPath, OpenSceneMode.Single);
            var canvas = GameObject.Find("ReadingRoomCanvas");
            var root = canvas.transform;

            FitNarrowWindows(canvas);
            CentreQuestion(root);
            LayOutFourButtonRow(root);
            HideStepBar(root);
            HideTableAura();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void FitNarrowWindows(GameObject canvas)
        {
            var fit = canvas.GetComponent<ResultCanvasAspectFit>();
            if (fit == null)
            {
                fit = canvas.AddComponent<ResultCanvasAspectFit>();
            }

            var so = new SerializedObject(fit);
            so.FindProperty("scaler").objectReferenceValue = canvas.GetComponent<CanvasScaler>();
            so.FindProperty("canvasRect").objectReferenceValue = canvas.transform;
            so.FindProperty("pinned").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CentreQuestion(Transform root)
        {
            var input = root.Find("QuestionInput").GetComponent<TMP_InputField>();
            foreach (var text in new[] { input.textComponent, (TMP_Text)input.placeholder })
            {
                text.horizontalAlignment = HorizontalAlignmentOptions.Center;
                text.ForceMeshUpdate(true, true);   // settle TMP's cached mesh so a re-run is a no-op
                EditorUtility.SetDirty(text);
            }
        }

        private static void LayOutFourButtonRow(Transform root)
        {
            var draw = (RectTransform)root.Find("DrawButton");
            var reveal = (RectTransform)root.Find("RevealResultButton");
            var slot = Vector2.Max(draw.sizeDelta, reveal.sizeDelta);
            draw.sizeDelta = slot;
            reveal.sizeDelta = slot;

            var rects = RowButtons.Select(n => (RectTransform)root.Find(n)).ToArray();
            var widths = rects.Select(r => r.sizeDelta.x).ToArray();
            var centers = UiFitLayout.RowCenters(widths, UiFitLayout.RowGap);
            for (var i = 0; i < rects.Length; i++)
            {
                rects[i].anchoredPosition = new Vector2(centers[i], rects[i].anchoredPosition.y);
                EditorUtility.SetDirty(rects[i]);
            }

            reveal.anchoredPosition = draw.anchoredPosition;
            EditorUtility.SetDirty(reveal);

            var rowWidth = widths.Sum() + UiFitLayout.RowGap * (widths.Length - 1);
            var dock = (RectTransform)root.Find("Phase11_ActionDock");
            dock.sizeDelta = new Vector2(rowWidth + 2f * UiFitLayout.DockPad.x + 2f * UiFitLayout.SkinMargin, dock.sizeDelta.y);
            EditorUtility.SetDirty(dock);

            var input = (RectTransform)root.Find("QuestionInput");
            input.sizeDelta = new Vector2(Mathf.Round(UiFitLayout.InputWidthRatio * dock.sizeDelta.x), input.sizeDelta.y);
            EditorUtility.SetDirty(input);
        }

        private static void HideStepBar(Transform root)
        {
            var hud = root.Find("Phase7_RitualHudRoot").gameObject;
            var group = hud.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = hud.AddComponent<CanvasGroup>();
            }

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            EditorUtility.SetDirty(group);
        }

        private static void HideTableAura()
        {
            var aura = GameObject.Find("Phase16_RitualAuraRoot");
            foreach (var name in TableAuraVisuals)
            {
                var renderer = aura.transform.Find(name)?.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }
    }
}

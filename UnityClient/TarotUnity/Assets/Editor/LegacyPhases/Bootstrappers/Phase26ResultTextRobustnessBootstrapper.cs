using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 26 hardens the Result reading for real, variable-length AI text.
    ///
    /// Phase 25 laid the interpretation out in fixed-height boxes sized for the
    /// short local-mode placeholder copy. Real backend interpretation can be much
    /// longer, and with fixed boxes long copy would overflow downward and overlap
    /// the next section. Enabling Unity Text best-fit caps each section to its own
    /// box: short copy still renders at full size, long copy shrinks to fit, so
    /// sections never overlap regardless of length.
    ///
    /// This is the robustness floor; a scrollable reading panel is the eventual
    /// ideal once card/AI work is tuned with feedback.
    /// </summary>
    public static class Phase26ResultTextRobustnessBootstrapper
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const int MinSize = 13;

        private static readonly (string name, int maxSize)[] BodyTexts =
        {
            ("SummaryText", 19),
            ("OverallText", 19),
            ("CardAnalysisText", 19),
            ("AdviceText", 19),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 26 Result Text Robustness Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 26 Result Text Robustness Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 26 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ResultScenePath);

            var applied = 0;
            foreach (var (name, maxSize) in BodyTexts)
            {
                var text = FindText(name);
                if (text == null)
                {
                    Debug.LogWarning($"Phase 26 bootstrap could not find {name} in {ResultScenePath}.");
                    continue;
                }

                // Keep fontSize as the design size (also the best-fit ceiling) so
                // the OverallText >= 19 invariant other phases assert still holds.
                text.fontSize = Mathf.Max(text.fontSize, maxSize);
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = MinSize;
                text.resizeTextMaxSize = maxSize;
                EditorUtility.SetDirty(text);
                applied++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ResultScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Tarot Unity Phase 26 result text robustness bootstrap complete. Texts updated: {applied}.");
        }

        private static Text FindText(string name)
        {
            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }
    }
}

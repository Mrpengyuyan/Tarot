using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 35 fixes three objective defects found in the Phase 31/34 visual review:
    ///
    /// 1. Result: the pre-Phase-25 3D card stage (pedestal, glow pool, rune ring, two
    ///    anchor marker cubes) pokes out from behind the translucent reading panel,
    ///    visible as a stray lit plate next to the 建议 copy. Their MeshRenderers are
    ///    disabled; the GameObjects stay ACTIVE because Phase 15-21 tests locate them
    ///    with GameObject.Find and the particle anchors still parent live systems.
    ///
    /// 2. Result: ResultReadingFrame (a Phase 5 flat-era overlay missed by the Phase 25
    ///    cleanup) reaches into the hero card slot and darkens its right side with a
    ///    visible seam - on top of real card art at runtime too. Deactivated, not
    ///    deleted, matching the Phase 25 convention; the scroll panel has its own back.
    ///
    /// 3. MainMenu: the status line sat on the start plate touching its bottom edge,
    ///    and the quit button was a full-prominence clone of the primary button whose
    ///    inherited gold trim rendered a dark notch. The status line moves just below
    ///    the plate, and the quit button becomes a quiet secondary action: trim clone
    ///    off, smaller plate, smaller muted label (matching the runtime TarotUiTheme
    ///    rule that colors 16pt-and-under text with the muted color).
    ///
    /// Idempotent: every step assigns absolute values.
    /// </summary>
    public static class Phase35UiPolishBootstrapper
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        private static readonly string[] ResultStageVisualPaths =
        {
            "Phase15_ResultCardStageRoot/Phase15_ResultCardPedestal",
            "Phase15_ResultCardStageRoot/Phase16_ResultAuraRoot/Phase16_ResultGlowPool",
            "Phase15_ResultCardStageRoot/Phase16_ResultAuraRoot/Phase16_ResultRuneRing",
            "Phase15_ResultCardStageRoot/Phase16_ResultAuraRoot/Phase16_ResultParticleAnchorLeft",
            "Phase15_ResultCardStageRoot/Phase16_ResultAuraRoot/Phase16_ResultParticleAnchorRight",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 35 UI Polish Bootstrap")]
        public static void Run()
        {
            PolishResult();
            PolishMainMenu();
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 35 UI polish bootstrap complete.");
        }

        private static void PolishResult()
        {
            var scene = EditorSceneManager.OpenScene(ResultScenePath);

            foreach (var path in ResultStageVisualPaths)
            {
                var t = FindInScene(path);
                if (t == null)
                {
                    Debug.LogWarning($"Phase 35: missing result stage visual '{path}'");
                    continue;
                }

                var renderer = t.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            var readingFrame = GameObject.Find("ResultCanvas")?.transform.Find("ResultReadingFrame");
            if (readingFrame != null)
            {
                readingFrame.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Phase 35: ResultReadingFrame not found under ResultCanvas");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Phase 35: Result stage visuals hidden, ResultReadingFrame deactivated.");
        }

        private static void PolishMainMenu()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath);
            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas == null)
            {
                Debug.LogError("Phase 35: MainMenuCanvas not found");
                return;
            }

            // Start plate: (0,-76) 308x56 -> bottom edge -104. Clear it by ~9px.
            var status = canvas.transform.Find("StatusText") as RectTransform;
            if (status != null)
            {
                status.anchoredPosition = new Vector2(0f, -122f);
            }

            var quit = canvas.transform.Find("QuitButton") as RectTransform;
            if (quit != null)
            {
                quit.sizeDelta = new Vector2(200f, 40f);
                quit.anchoredPosition = new Vector2(0f, -182f);

                var trim = quit.Find("Phase8_StartButtonGoldTrim");
                if (trim != null)
                {
                    trim.gameObject.SetActive(false);
                }

                var label = quit.Find("Label") as RectTransform;
                if (label != null)
                {
                    label.sizeDelta = new Vector2(200f, 40f);
                    var text = label.GetComponent<Text>();
                    if (text != null)
                    {
                        text.fontSize = 16;
                        text.fontStyle = FontStyle.Normal;
                        text.color = MutedThemeColor(canvas);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Phase 35: QuitButton not found (run Phase 34 first)");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Phase 35: status line cleared of the start plate, quit button de-emphasized.");
        }

        private static Color MutedThemeColor(GameObject canvas)
        {
            // Mirror TarotUiTheme's runtime rule (<=16pt text gets mutedTextColor) so the
            // editor-baked look matches play mode.
            var theme = Object.FindFirstObjectByType<TarotUnity.UI.TarotUiTheme>();
            if (theme != null)
            {
                var muted = new SerializedObject(theme).FindProperty("mutedTextColor");
                if (muted != null)
                {
                    return muted.colorValue;
                }
            }

            return new Color(0.78f, 0.74f, 0.70f, 1f);
        }

        private static Transform FindInScene(string path)
        {
            var rootName = path.Split('/')[0];
            var root = GameObject.Find(rootName);
            if (root == null)
            {
                return null;
            }

            var rest = path.Substring(rootName.Length).TrimStart('/');
            return rest.Length == 0 ? root.transform : root.transform.Find(rest);
        }
    }
}

using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 51 brings the Result screen into line with the other two: its 13
    /// legacy UI.Text become TextMeshPro SDF (this is the last screen still on the
    /// bitmap-atlas text that softens at the built resolution), and its scene
    /// RenderSettings adopt the same Flat ambient contract the menu and ReadingRoom
    /// carry.
    ///
    /// A deliberate honesty note on the lighting: this screen's canvas is a
    /// Screen-Space Overlay and its camera clears to a solid near-black colour, so
    /// the daylight-skybox ambient it shipped with was lighting only 3D dressing
    /// that never reaches the frame. Fixing it here is correctness and cross-scene
    /// consistency (a midnight product should not carry daylight-sky ambient in its
    /// data), not a visible change - verified inert by before/after capture. The
    /// visible win on this screen is the text.
    /// </summary>
    public static class Phase51ResultTmpMigrationBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/Result.unity";
        private const string CanvasName = "ResultCanvas";

        [MenuItem("Tools/Tarot Unity/Run Phase 51 - Migrate Result Text to TMP")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            var body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                Phase43TmpFoundationBootstrapper.BodyFontAssetPath);
            var display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                Phase43TmpFoundationBootstrapper.DisplayFontAssetPath);
            if (body == null || display == null)
            {
                Debug.LogError("Phase 51: SDF font assets missing; run Phase 43 steps 1 and 2 first.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find(CanvasName);
            if (canvas == null)
            {
                Debug.LogError($"Phase 51: {CanvasName} missing.");
                return;
            }

            StageAmbient();

            var theme = canvas.GetComponent<TarotUiTheme>();
            AssignThemeFonts(theme, body, display);

            var legacy = canvas.GetComponentsInChildren<Text>(true);
            var converted = 0;
            foreach (var text in legacy)
            {
                if (Convert(text, theme, body, display))
                {
                    converted++;
                }
            }

            ApplyTypography(canvas, theme, body, display);
            RewireResultPresenter(canvas);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Phase 51: migrated {converted} Result Text components to TMP SDF.");
        }

        /// <summary>
        /// The same Flat ambient contract the menu (Phase 42) and ReadingRoom
        /// (Phase 49) carry. Lights are left alone: this screen's 4 tuned lights
        /// only touch off-frame dressing, and the canvas is Overlay, so there is
        /// nothing on screen for them to over- or under-light.
        /// </summary>
        private static void StageAmbient()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.085f, 0.068f, 0.072f, 1f);
            RenderSettings.reflectionIntensity = 0.06f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
        }

        private static void AssignThemeFonts(TarotUiTheme theme, TMP_FontAsset body, TMP_FontAsset display)
        {
            if (theme == null)
            {
                Debug.LogWarning("Phase 51: no TarotUiTheme on the Result canvas.");
                return;
            }

            var so = new SerializedObject(theme);
            so.FindProperty("tmpBodyFont").objectReferenceValue = body;
            so.FindProperty("tmpDisplayFont").objectReferenceValue = display;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        /// <summary>
        /// The in-place Text -> TMP conversion shared with Phases 43 and 50. The
        /// authored colour carries over verbatim, so the gold section headers stay
        /// gold and the ivory bodies stay ivory (the TarotUiAccentText markers keep
        /// them gold at runtime too).
        /// </summary>
        private static bool Convert(Text text, TarotUiTheme theme, TMP_FontAsset body, TMP_FontAsset display)
        {
            if (text == null)
            {
                return false;
            }

            var go = text.gameObject;
            var content = text.text;
            var fontSize = text.fontSize;
            var color = text.color;
            var alignment = MapAlignment(text.alignment);
            var wasActive = go.activeSelf;

            var threshold = theme != null ? theme.DisplaySizeThreshold : 30;
            var isDisplay = fontSize >= threshold;
            var isButtonLabel = text.GetComponentInParent<Button>(true) != null;

            var outline = go.GetComponent<Outline>();
            if (outline != null)
            {
                Object.DestroyImmediate(outline, true);
            }

            var shadow = go.GetComponent<Shadow>();
            if (shadow != null)
            {
                Object.DestroyImmediate(shadow, true);
            }

            Object.DestroyImmediate(text, true);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.font = isDisplay || isButtonLabel ? display : body;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;

            go.SetActive(wasActive);
            EditorUtility.SetDirty(go);
            return true;
        }

        private static void ApplyTypography(GameObject canvas, TarotUiTheme theme,
            TMP_FontAsset body, TMP_FontAsset display)
        {
            var threshold = theme != null ? theme.DisplaySizeThreshold : 30;

            foreach (var tmp in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                var isDisplay = tmp.fontSize >= threshold;
                var isButtonLabel = tmp.GetComponentInParent<Button>(true) != null;
                tmp.font = isDisplay || isButtonLabel ? display : body;
                EditorUtility.SetDirty(tmp);
            }
        }

        /// <summary>The presenter's seven readout fields are TMP_Text now; re-point them.</summary>
        private static void RewireResultPresenter(GameObject canvas)
        {
            var presenter = canvas.GetComponentInChildren<ResultPanelPresenter>(true);
            if (presenter == null)
            {
                Debug.LogWarning("Phase 51: ResultPanelPresenter not found.");
                return;
            }

            var so = new SerializedObject(presenter);
            Wire(so, "questionText", FindTmpText(canvas, "QuestionText"));
            Wire(so, "spreadNameText", FindTmpText(canvas, "SpreadNameText"));
            Wire(so, "summaryText", FindTmpText(canvas, "SummaryText"));
            Wire(so, "overallText", FindTmpText(canvas, "OverallText"));
            Wire(so, "cardAnalysisText", FindTmpText(canvas, "CardAnalysisText"));
            Wire(so, "adviceText", FindTmpText(canvas, "AdviceText"));
            Wire(so, "warningText", FindTmpText(canvas, "WarningText"));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }

        private static void Wire(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"Phase 51: field '{field}' not found on presenter.");
                return;
            }

            if (value == null)
            {
                Debug.LogWarning($"Phase 51: no TMP target for field '{field}'.");
                return;
            }

            prop.objectReferenceValue = value;
        }

        private static TMP_Text FindTmpText(GameObject canvas, string name)
        {
            foreach (var tmp in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.gameObject.name == name)
                {
                    return tmp;
                }
            }

            return null;
        }

        private static TextAlignmentOptions MapAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }
    }
}

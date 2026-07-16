using System.Collections.Generic;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 43 step 3: migrates the MainMenu's text from legacy UI.Text to
    /// TextMeshPro SDF. The menu is the migration bridgehead because it holds no
    /// InputField (ReadingRoom's question field needs the separate TMP_InputField
    /// swap), so the whole screen converts cleanly.
    ///
    /// Each Text is replaced in place: the RectTransform, sibling order and rect
    /// stay untouched, content/size/alignment/colour carry over, and the legacy
    /// Outline component (four offset copies of the mesh - the muddy look) is
    /// dropped in favour of the SDF shader's real outline on the display cuts.
    /// </summary>
    public static class Phase43MenuTmpMigrationBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";

        /// <summary>Display-weight elements that earn a shader outline.</summary>
        private static readonly HashSet<string> OutlinedTitles = new() { "TitleText" };

        [MenuItem("Tools/Tarot Unity/Run Phase 43 Step 3 - Migrate MainMenu Text to TMP")]
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
                Debug.LogError("Phase 43: SDF font assets missing; run steps 1 and 2 first.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas == null)
            {
                Debug.LogError("Phase 43: MainMenuCanvas missing.");
                return;
            }

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

            // Typography runs as its own pass over whatever TMP is present, so a
            // re-run restyles an already-migrated scene instead of no-opping.
            ApplyTypography(canvas, theme, body, display);
            RewireMenuController(canvas);

            // "✦ ✧ ✦" is a dingbat row LXGW WenKai has no glyphs for; legacy Text
            // hid that behind an OS font fallback and TMP exposed it as tofu. It
            // was generic decoration restating the title's own mood, so it retires
            // rather than getting propped up by a fallback face. Kept in scene.
            var constellation = canvas.transform.Find("Phase8_TitleConstellation");
            if (constellation != null && constellation.gameObject.activeSelf)
            {
                constellation.gameObject.SetActive(false);
                EditorUtility.SetDirty(constellation.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Phase 43: migrated {converted} MainMenu Text components to TMP SDF.");
        }

        private static void AssignThemeFonts(TarotUiTheme theme, TMP_FontAsset body, TMP_FontAsset display)
        {
            if (theme == null)
            {
                Debug.LogWarning("Phase 43: no TarotUiTheme on the menu canvas.");
                return;
            }

            var so = new SerializedObject(theme);
            so.FindProperty("tmpBodyFont").objectReferenceValue = body;
            so.FindProperty("tmpDisplayFont").objectReferenceValue = display;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

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
            var isAccent = go.GetComponent<TarotUiAccentText>() != null;

            var threshold = theme != null ? theme.DisplaySizeThreshold : 30;
            var isDisplay = fontSize >= threshold;
            var isButtonLabel = text.GetComponentInParent<Button>(true) != null;

            // The legacy outline is four offset mesh copies; SDF does it properly.
            var outline = go.GetComponent<Outline>();
            var wantsOutline = OutlinedTitles.Contains(go.name) || (outline != null && isDisplay);
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

        /// <summary>
        /// Assigns role fonts and gilds the title. Idempotent: safe to re-run over
        /// a scene that is already TMP.
        /// </summary>
        private static void ApplyTypography(GameObject canvas, TarotUiTheme theme,
            TMP_FontAsset body, TMP_FontAsset display)
        {
            var threshold = theme != null ? theme.DisplaySizeThreshold : 30;

            foreach (var tmp in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                var isDisplay = tmp.fontSize >= threshold;
                var isButtonLabel = tmp.GetComponentInParent<Button>(true) != null;
                tmp.font = isDisplay || isButtonLabel ? display : body;

                if (OutlinedTitles.Contains(tmp.gameObject.name))
                {
                    Gild(tmp);
                }

                EditorUtility.SetDirty(tmp);
            }
        }

        /// <summary>
        /// The thing SDF buys that the legacy Outline component never could: a
        /// vertical gradient down the glyph plus a crisp stroke, both
        /// resolution-independent. Touching fontMaterial instantiates a
        /// per-instance material, so this cannot restyle every element sharing the
        /// font asset.
        /// </summary>
        private static void Gild(TMP_Text tmp)
        {
            tmp.enableVertexGradient = true;
            var gildTop = new Color(1.00f, 0.94f, 0.72f, 1f);
            var gildBottom = new Color(0.78f, 0.53f, 0.16f, 1f);
            tmp.colorGradient = new VertexGradient(gildTop, gildTop, gildBottom, gildBottom);

            var mat = tmp.fontMaterial;
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.14f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.14f, 0.07f, 0.04f, 1f));
        }

        /// <summary>The controller's status field is a TMP_Text now; re-point it.</summary>
        private static void RewireMenuController(GameObject canvas)
        {
            var controller = canvas.GetComponent<MainMenuController>();
            if (controller == null)
            {
                return;
            }

            var status = canvas.transform.Find("StatusText")?.GetComponent<TMP_Text>();
            if (status == null)
            {
                Debug.LogWarning("Phase 43: StatusText not found as TMP.");
                return;
            }

            var so = new SerializedObject(controller);
            so.FindProperty("statusText").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
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

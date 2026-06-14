using System.Collections.Generic;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 24 gives the game a real typographic identity. Every Chinese glyph
    /// was previously drawn by Unity's built-in LegacyRuntime font, which has no
    /// CJK glyphs of its own and silently falls back to whatever sans-serif the
    /// operating system ships (PingFang on macOS, YaHei or tofu on a shipped
    /// Windows build). That reads as a system dialog, not a game, and changes
    /// between machines.
    ///
    /// This bootstrap bundles LXGW WenKai (SIL OFL) - an elegant calligraphic
    /// serif that suits the quiet, hand-inked mood of the reading table - and
    /// bakes it into every active Text element across the three UI scenes and
    /// the UI prefabs, split into a body cut and a slightly heavier display cut.
    ///
    /// Baking matters because the screenshot pipeline renders scenes in edit
    /// mode (camera.Render, no Play Mode), so TarotUiTheme.Awake never runs; the
    /// serialized assets themselves must already carry the font. TarotUiTheme is
    /// also given the font references so it stays the runtime source of truth.
    /// </summary>
    public static class Phase24TypographyBootstrapper
    {
        private const string BodyFontPath = "Assets/Fonts/LXGWWenKai-Regular.ttf";
        private const string DisplayFontPath = "Assets/Fonts/LXGWWenKai-Medium.ttf";
        private const int DisplaySizeThreshold = 30;

        private static readonly string[] UiScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/ReadingRoom.unity",
            "Assets/Scenes/Result.unity",
        };

        private static readonly Color TitleOutlineColor = new(0f, 0f, 0f, 0.55f);
        private static readonly Vector2 TitleOutlineDistance = new(1.5f, -1.5f);

        [MenuItem("Tools/Tarot Unity/Run Phase 24 Typography Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 24 Typography Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 24 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            AssetDatabase.Refresh();

            ConfigureFontImporter(BodyFontPath);
            ConfigureFontImporter(DisplayFontPath);

            var bodyFont = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath);
            var displayFont = AssetDatabase.LoadAssetAtPath<Font>(DisplayFontPath);
            if (bodyFont == null || displayFont == null)
            {
                Debug.LogError($"Phase 24 bootstrap could not load fonts. Body: {bodyFont}, Display: {displayFont}. " +
                               "Ensure the LXGW WenKai TTFs exist under Assets/Fonts/.");
                return;
            }

            foreach (var scenePath in UiScenePaths)
            {
                ApplyFontsToScene(scenePath, bodyFont, displayFont);
            }

            ApplyFontsToPrefabs(bodyFont, displayFont);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 24 typography bootstrap complete.");
        }

        private static void ConfigureFontImporter(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TrueTypeFontImporter importer)
            {
                Debug.LogWarning($"Phase 24 bootstrap found no font importer at {path}.");
                return;
            }

            // Dynamic so any runtime glyph (including AI-generated interpretation
            // text) rasterizes on demand; HintedSmooth keeps small CJK strokes
            // crisp; includeFontData embeds the face so the build never depends
            // on a system font.
            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.fontRenderingMode = FontRenderingMode.HintedSmooth;
            importer.includeFontData = true;
            importer.SaveAndReimport();
        }

        private static void ApplyFontsToScene(string scenePath, Font bodyFont, Font displayFont)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);

            foreach (var theme in Object.FindObjectsByType<TarotUiTheme>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var serialized = new SerializedObject(theme);
                SetObjectReference(serialized, "bodyFont", bodyFont);
                SetObjectReference(serialized, "displayFont", displayFont);
                var threshold = serialized.FindProperty("displaySizeThreshold");
                if (threshold != null)
                {
                    threshold.intValue = DisplaySizeThreshold;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(theme);
            }

            // Only active text gets restyled: the card prefab keeps two
            // fontless graybox labels deactivated on purpose (Phase 21), and we
            // must not touch or revive them.
            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var text in texts)
            {
                BakeText(text, bodyFont, displayFont);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void ApplyFontsToPrefabs(Font bodyFont, Font displayFont)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                {
                    continue;
                }

                try
                {
                    // includeInactive: false leaves the deactivated card labels alone.
                    var texts = root.GetComponentsInChildren<Text>(false);
                    if (texts.Length == 0)
                    {
                        continue;
                    }

                    foreach (var text in texts)
                    {
                        BakeText(text, bodyFont, displayFont);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void BakeText(Text text, Font bodyFont, Font displayFont)
        {
            if (text == null)
            {
                return;
            }

            var role = TarotUiTheme.ClassifyRole(text, DisplaySizeThreshold);
            text.font = role == TarotUiTheme.TextRole.Body ? bodyFont : displayFont;

            // Display titles sit over busy candle-lit backgrounds; a soft dark
            // outline keeps the gold legible without the cartoon look of a thick
            // stroke. Body and button text stay clean.
            if (role == TarotUiTheme.TextRole.Display)
            {
                var outline = text.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = text.gameObject.AddComponent<Outline>();
                }

                outline.effectColor = TitleOutlineColor;
                outline.effectDistance = TitleOutlineDistance;
                outline.useGraphicAlpha = true;
            }

            EditorUtility.SetDirty(text);
        }

        private static void SetObjectReference(SerializedObject serialized, string propertyName, Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Phase 24 bootstrap: missing serialized property {propertyName} on {serialized.targetObject.name}");
                return;
            }

            property.objectReferenceValue = value;
        }
    }
}

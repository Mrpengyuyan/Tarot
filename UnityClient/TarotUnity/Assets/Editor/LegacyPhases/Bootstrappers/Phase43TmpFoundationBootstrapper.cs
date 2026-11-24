using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 43 replaces the text rendering method itself.
    ///
    /// The UI ran on Unity's legacy Text with a dynamic bitmap font: glyphs are
    /// rasterised into an atlas at one pixel size, so any scaling softens them,
    /// and the Outline component fakes a stroke by drawing four offset copies of
    /// the mesh, which reads muddy rather than crisp. This builds the TextMeshPro
    /// SDF foundation instead - glyphs stored as signed distance fields stay sharp
    /// at any resolution, and outline/glow/gradient become real shader features.
    ///
    /// Phase 24 rejected TMP because the Result screen renders arbitrary runtime
    /// Chinese from the backend and a static glyph atlas cannot hold it. That
    /// reasoning is out of date: TMP dynamic atlas population rasterises glyphs on
    /// demand at runtime, so arbitrary CJK works. The font assets below are
    /// explicitly Dynamic with multi-atlas support for exactly that reason.
    ///
    /// Import and font-asset creation are separate entry points because importing
    /// the TMP essentials unitypackage does not settle within one batch run.
    /// </summary>
    public static class Phase43TmpFoundationBootstrapper
    {
        public const string TmpEssentialsFolder = "Assets/TextMesh Pro";
        public const string FontFolder = "Assets/Fonts";
        public const string BodyFontAssetPath = FontFolder + "/LXGWWenKai-Regular SDF.asset";
        public const string DisplayFontAssetPath = FontFolder + "/LXGWWenKai-Medium SDF.asset";

        // 90pt sampling with 9px padding gives CJK strokes enough distance-field
        // resolution to stay crisp at display sizes, and leaves ~10% of an em of
        // headroom for a real shader outline.
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasSize = 2048;

        [MenuItem("Tools/Tarot Unity/Run Phase 43 Step 1 - Import TMP Essentials")]
        public static void ImportTmpEssentials()
        {
            if (Directory.Exists(TmpEssentialsFolder))
            {
                Debug.Log("Phase 43: TMP essentials already present.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            // ImportPackage is asynchronous, so a batch run with -quit exits before
            // a single file lands. Drive it off the completion callbacks instead and
            // exit from there; the caller must NOT pass -quit.
            AssetDatabase.importPackageCompleted += OnImportCompleted;
            AssetDatabase.importPackageFailed += OnImportFailed;
            AssetDatabase.importPackageCancelled += OnImportCancelled;

            TMP_PackageResourceImporter.ImportResources(true, false, false);
            Debug.Log("Phase 43: TMP essentials import requested.");
        }

        private static void OnImportCompleted(string packageName)
        {
            AssetDatabase.Refresh();
            Debug.Log($"Phase 43: TMP essentials import completed ({packageName}).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void OnImportFailed(string packageName, string error)
        {
            Debug.LogError($"Phase 43: TMP essentials import failed ({packageName}): {error}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void OnImportCancelled(string packageName)
        {
            Debug.LogError($"Phase 43: TMP essentials import cancelled ({packageName}).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Tools/Tarot Unity/Run Phase 43 Step 2 - Create SDF Font Assets")]
        public static void CreateFontAssets()
        {
            if (!Directory.Exists(TmpEssentialsFolder))
            {
                Debug.LogError("Phase 43: TMP essentials missing; run step 1 first.");
                return;
            }

            CreateFontAsset("LXGWWenKai-Regular", BodyFontAssetPath);
            CreateFontAsset("LXGWWenKai-Medium", DisplayFontAssetPath);
            AssignFallbacks();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 43: SDF font assets created.");
        }

        /// <summary>
        /// LXGW WenKai has full CJK coverage but no dingbats, so glyphs like the
        /// title's star decoration rendered as tofu the moment legacy Text's OS
        /// font fallback went away. This matters beyond decoration: the Result
        /// screen renders arbitrary backend Chinese, and any glyph outside the
        /// face would tofu at runtime with no fallback in the chain.
        /// </summary>
        private static void AssignFallbacks()
        {
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (fallback == null)
            {
                Debug.LogWarning("Phase 43: LiberationSans SDF not found; no fallback assigned.");
                return;
            }

            foreach (var path in new[] { BodyFontAssetPath, DisplayFontAssetPath })
            {
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fontAsset == null)
                {
                    continue;
                }

                fontAsset.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>();
                if (!fontAsset.fallbackFontAssetTable.Contains(fallback))
                {
                    fontAsset.fallbackFontAssetTable.Add(fallback);
                    EditorUtility.SetDirty(fontAsset);
                    Debug.Log($"Phase 43: fallback assigned on {path}.");
                }
            }
        }

        private static void CreateFontAsset(string sourceName, string assetPath)
        {
            var sourcePath = $"{FontFolder}/{sourceName}.ttf";
            var font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (font == null)
            {
                Debug.LogError($"Phase 43: source font missing at {sourcePath}.");
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                Debug.Log($"Phase 43: {assetPath} already exists.");
                return;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (fontAsset == null)
            {
                Debug.LogError($"Phase 43: CreateFontAsset failed for {sourceName}.");
                return;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // The atlas texture and material must live inside the font asset or
            // they are lost on reload.
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
            {
                var atlas = fontAsset.atlasTextures[0];
                atlas.name = fontAsset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            Debug.Log($"Phase 43: created {assetPath} (dynamic SDF, {SamplingPointSize}pt).");
        }
    }
}

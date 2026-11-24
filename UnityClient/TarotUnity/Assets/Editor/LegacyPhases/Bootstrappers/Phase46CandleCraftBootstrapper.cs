using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 46 rebuilds the candles as wax rather than tubing, and lights them.
    ///
    /// They were perfect cylinders in one flat cream with one flat emission over
    /// the whole body - which is why they read as plastic. Wax runs down the side
    /// in drips, is translucent so it glows only near the flame and goes opaque
    /// toward the base, and sits in its own spill. And a still flame is the
    /// clearest tell that a lit scene is a render.
    /// </summary>
    public static class Phase46CandleCraftBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MaterialFolder = Phase37AssetFoundationBootstrapper.MaterialFolder;
        private const string SpriteFolder = Phase37AssetFoundationBootstrapper.SpriteFolder;

        /// <summary>Front candles carry a taller flame; the back pair are burnt lower.</summary>
        private static readonly (string Name, float Intensity)[] Candles =
        {
            ("Phase8_LeftCandle", 4.2f),
            ("Phase8_RightCandle", 4.2f),
            ("MP_BackCandle_L", 1.5f),
            ("MP_BackCandle_R", 1.5f),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 46 Candle Craft Bootstrap")]
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

            ConfigureWaxTextures();
            SkinWax();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var (name, intensity) in Candles)
            {
                StageFlicker(name, intensity);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 46 candle craft complete.");
        }

        private static void ConfigureWaxTextures()
        {
            foreach (var name in new[] { "WaxColor", "WaxEmission" })
            {
                var path = $"{SpriteFolder}/{name}.png";
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    Debug.LogError($"Phase 46: missing {path}; run gen_wax.py first.");
                    continue;
                }

                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = false;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 512;
                // The side of a cylinder wraps, so the wax grain must tile across.
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
        }

        private static void SkinWax()
        {
            var wax = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/MP_CandleWax.mat");
            if (wax == null)
            {
                Debug.LogError("Phase 46: MP_CandleWax missing; run the Phase 42 bootstrap first.");
                return;
            }

            wax.SetTexture("_BaseMap", Load("WaxColor"));
            wax.SetColor("_BaseColor", Color.white);
            wax.SetFloat("_Smoothness", 0.30f);

            // The translucency map replaces Phase 42's flat emission: the flame
            // lights the wax it sits in, not the whole stick.
            wax.EnableKeyword("_EMISSION");
            wax.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            wax.SetTexture("_EmissionMap", Load("WaxEmission"));
            wax.SetColor("_EmissionColor", new Color(1.15f, 1.15f, 1.15f, 1f));
            EditorUtility.SetDirty(wax);
        }

        private static void StageFlicker(string candleName, float baseIntensity)
        {
            var candle = GameObject.Find(candleName);
            if (candle == null)
            {
                Debug.LogWarning($"Phase 46: {candleName} missing.");
                return;
            }

            var flicker = candle.GetComponent<CandleFlickerController>();
            if (flicker == null)
            {
                flicker = candle.AddComponent<CandleFlickerController>();
            }

            var so = new SerializedObject(flicker);
            so.FindProperty("flameLight").objectReferenceValue = candle.GetComponent<Light>();
            so.FindProperty("flameBillboard").objectReferenceValue = candle.transform.Find("Flame");
            so.FindProperty("baseIntensity").floatValue = baseIntensity;
            // The back pair are further off and should not draw the eye.
            var isBack = candleName.StartsWith("MP_BackCandle");
            so.FindProperty("intensityFlicker").floatValue = isBack ? 0.20f : 0.15f;
            so.FindProperty("flameScaleFlicker").floatValue = isBack ? 0.08f : 0.12f;
            so.FindProperty("flickerSpeed").floatValue = isBack ? 5.2f : 4.3f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flicker);
        }

        private static Texture2D Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{SpriteFolder}/{name}.png");
        }
    }
}

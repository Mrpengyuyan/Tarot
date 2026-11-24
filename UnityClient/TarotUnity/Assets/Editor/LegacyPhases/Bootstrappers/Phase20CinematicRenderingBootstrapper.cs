using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TarotUnity.Editor
{
    public static class Phase20CinematicRenderingBootstrapper
    {
        private const string VolumeProfilePath = "Assets/Settings/Phase20_CinematicVolumeProfile.asset";
        private const string VolumeObjectName = "Phase20_CinematicVolume";
        private const string PcPipelineAssetPath = "Assets/Settings/PC_RPAsset.asset";
        private const string MobilePipelineAssetPath = "Assets/Settings/Mobile_RPAsset.asset";

        private const float TransparentGlowBoost = 2.2f;

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/ReadingRoom.unity",
            "Assets/Scenes/Result.unity",
        };

        private static readonly string[] TransparentGlowMaterialPaths =
        {
            "Assets/Materials/MAT_CardGlow.mat",
            "Assets/Materials/MAT_RitualGlow.mat",
            "Assets/Materials/MAT_RitualSpark.mat",
            "Assets/Materials/MAT_Phase14_RevealGlow.mat",
            "Assets/Materials/MAT_Phase14_FaceRimLight.mat",
            "Assets/Materials/MAT_Phase16_AuraGlowPool.mat",
            "Assets/Materials/MAT_Phase16_AuraParticle.mat",
            "Assets/Materials/MAT_Phase16_RuneRing.mat",
            "Assets/Materials/MAT_Phase18_RitualParticles.mat",
        };

        private static readonly (string Path, Color Emission)[] EmissiveMaterialTargets =
        {
            ("Assets/Materials/MAT_Phase8_CandleAmber.mat", new Color(2.05f, 1.18f, 0.42f)),
            ("Assets/Materials/MAT_Phase7_MoonGold.mat", new Color(1.45f, 1.18f, 0.62f)),
            ("Assets/Materials/MAT_Phase8_EdgeGold.mat", new Color(0.85f, 0.64f, 0.28f)),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 20 Cinematic Rendering Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 20 Cinematic Rendering Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 20 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            var profile = EnsureVolumeProfile();
            UpgradeRenderPipelineAssets();
            BoostGlowMaterials();

            foreach (var scenePath in ScenePaths)
            {
                UpgradeScene(scenePath, profile);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 20 cinematic rendering bootstrap complete.");
        }

        private static VolumeProfile EnsureVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            var bloom = EnsureProfileComponent<Bloom>(profile);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(0.95f);
            bloom.scatter.Override(0.68f);
            bloom.tint.Override(new Color(1f, 0.93f, 0.78f));
            bloom.highQualityFiltering.Override(true);

            var tonemapping = EnsureProfileComponent<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            // ACES darkens authored LDR mid-tones, so exposure compensates to keep
            // the candlelit table readable instead of crushing it to black.
            var colorAdjustments = EnsureProfileComponent<ColorAdjustments>(profile);
            colorAdjustments.postExposure.Override(0.45f);
            colorAdjustments.contrast.Override(8f);
            colorAdjustments.saturation.Override(8f);

            var whiteBalance = EnsureProfileComponent<WhiteBalance>(profile);
            whiteBalance.temperature.Override(10f);
            whiteBalance.tint.Override(2f);

            var vignette = EnsureProfileComponent<Vignette>(profile);
            vignette.intensity.Override(0.26f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.012f, 0.008f, 0.02f));

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T EnsureProfileComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var component))
            {
                component = profile.Add<T>(true);
                component.name = typeof(T).Name;
                if (EditorUtility.IsPersistent(profile))
                {
                    AssetDatabase.AddObjectToAsset(component, profile);
                }
            }

            component.active = true;
            EditorUtility.SetDirty(component);
            return component;
        }

        private static void UpgradeRenderPipelineAssets()
        {
            ApplyPipelineSettings(PcPipelineAssetPath, msaaSamples: 4);
            ApplyPipelineSettings(MobilePipelineAssetPath, msaaSamples: 0);
        }

        private static void ApplyPipelineSettings(string assetPath, int msaaSamples)
        {
            var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (pipelineAsset == null)
            {
                Debug.LogWarning($"Phase 20 bootstrap skipped pipeline asset at {assetPath} because it is missing.");
                return;
            }

            pipelineAsset.supportsHDR = true;
            pipelineAsset.colorGradingMode = ColorGradingMode.HighDynamicRange;
            pipelineAsset.colorGradingLutSize = 32;
            if (msaaSamples > 0)
            {
                pipelineAsset.msaaSampleCount = msaaSamples;
            }

            EditorUtility.SetDirty(pipelineAsset);
        }

        private static void BoostGlowMaterials()
        {
            foreach (var materialPath in TransparentGlowMaterialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    continue;
                }

                BoostColorProperty(material, "_BaseColor");
                BoostColorProperty(material, "_Color");
                EditorUtility.SetDirty(material);
            }

            foreach (var target in EmissiveMaterialTargets)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(target.Path);
                if (material == null || !material.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                material.SetColor("_EmissionColor", target.Emission);
                EditorUtility.SetDirty(material);
            }
        }

        private static void BoostColorProperty(Material material, string propertyName)
        {
            if (!material.HasProperty(propertyName))
            {
                return;
            }

            var color = material.GetColor(propertyName);
            var maxComponent = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            if (maxComponent > 1.001f)
            {
                return;
            }

            material.SetColor(propertyName, new Color(
                color.r * TransparentGlowBoost,
                color.g * TransparentGlowBoost,
                color.b * TransparentGlowBoost,
                color.a));
        }

        private static void UpgradeScene(string scenePath, VolumeProfile profile)
        {
            EditorSceneManager.OpenScene(scenePath);

            EnsureSceneVolume(profile);
            EnsureCinematicCamera();

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scenePath);
        }

        private static void EnsureSceneVolume(VolumeProfile profile)
        {
            var volumeObject = GameObject.Find(VolumeObjectName);
            if (volumeObject == null)
            {
                volumeObject = new GameObject(VolumeObjectName);
            }

            var volume = volumeObject.GetComponent<Volume>();
            if (volume == null)
            {
                volume = volumeObject.AddComponent<Volume>();
            }

            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volumeObject);
        }

        private static void EnsureCinematicCamera()
        {
            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogWarning("Phase 20 bootstrap found no camera in the active scene.");
                return;
            }

            camera.allowHDR = true;

            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            cameraData.dithering = true;

            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(cameraData);
        }
    }
}

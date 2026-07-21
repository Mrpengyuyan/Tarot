using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase20CinematicRenderingTests
    {
        private const string VolumeProfilePath = "Assets/Settings/Phase20_CinematicVolumeProfile.asset";
        private const string VolumeObjectName = "Phase20_CinematicVolume";
        private const string PcPipelineAssetPath = "Assets/Settings/PC_RPAsset.asset";
        private const string MobilePipelineAssetPath = "Assets/Settings/Mobile_RPAsset.asset";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string Phase20DocPath = "Docs/PROJECT_CHRONICLE.md";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/ReadingRoom.unity",
            "Assets/Scenes/Result.unity",
        };

        [Test]
        public void CinematicVolumeProfileHasCoreComponents()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            Assert.That(profile, Is.Not.Null, $"Missing volume profile at {VolumeProfilePath}");

            Assert.That(profile.TryGet<Bloom>(out var bloom), Is.True, "Profile must contain Bloom");
            Assert.That(bloom.active, Is.True);
            Assert.That(bloom.intensity.value, Is.GreaterThan(0.3f));
            Assert.That(bloom.threshold.value, Is.InRange(0.5f, 1.2f));

            Assert.That(profile.TryGet<Tonemapping>(out var tonemapping), Is.True, "Profile must contain Tonemapping");
            Assert.That(tonemapping.mode.value, Is.EqualTo(TonemappingMode.ACES));

            Assert.That(profile.TryGet<ColorAdjustments>(out var colorAdjustments), Is.True, "Profile must contain ColorAdjustments");
            Assert.That(colorAdjustments.active, Is.True);

            Assert.That(profile.TryGet<Vignette>(out var vignette), Is.True, "Profile must contain Vignette");
            Assert.That(vignette.intensity.value, Is.InRange(0.15f, 0.5f));

            Assert.That(profile.TryGet<WhiteBalance>(out var whiteBalance), Is.True, "Profile must contain WhiteBalance");
            Assert.That(whiteBalance.active, Is.True);
        }

        [Test]
        public void RenderPipelineAssetsSupportHdrColorGrading()
        {
            var pcAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PcPipelineAssetPath);
            Assert.That(pcAsset, Is.Not.Null);
            Assert.That(pcAsset.supportsHDR, Is.True, "PC pipeline asset must support HDR");
            Assert.That(pcAsset.colorGradingMode, Is.EqualTo(ColorGradingMode.HighDynamicRange));
            Assert.That(pcAsset.msaaSampleCount, Is.GreaterThanOrEqualTo(4), "PC pipeline asset should use MSAA");

            var mobileAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobilePipelineAssetPath);
            Assert.That(mobileAsset, Is.Not.Null);
            Assert.That(mobileAsset.supportsHDR, Is.True, "Mobile pipeline asset must support HDR");
            Assert.That(mobileAsset.colorGradingMode, Is.EqualTo(ColorGradingMode.HighDynamicRange));
        }

        [Test]
        public void ScenesHaveCinematicVolumeAndPostProcessingCamera()
        {
            foreach (var scenePath in ScenePaths)
            {
                EditorSceneManager.OpenScene(scenePath);

                var volumeObject = GameObject.Find(VolumeObjectName);
                Assert.That(volumeObject, Is.Not.Null, $"{scenePath} must contain {VolumeObjectName}");

                var volume = volumeObject.GetComponent<Volume>();
                Assert.That(volume, Is.Not.Null, $"{VolumeObjectName} in {scenePath} must have a Volume component");
                Assert.That(volume.isGlobal, Is.True, $"{VolumeObjectName} in {scenePath} must be a global volume");
                Assert.That(volume.sharedProfile, Is.Not.Null, $"{VolumeObjectName} in {scenePath} must reference the cinematic profile");

                var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
                Assert.That(camera, Is.Not.Null, $"{scenePath} must contain a camera");
                Assert.That(camera.allowHDR, Is.True, $"Camera in {scenePath} must allow HDR");

                var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                Assert.That(cameraData, Is.Not.Null, $"Camera in {scenePath} must have URP camera data");
                Assert.That(cameraData.renderPostProcessing, Is.True, $"Camera in {scenePath} must render post-processing");
            }
        }

        [Test]
        public void GlowMaterialsUseHdrIntensity()
        {
            AssertHdrBaseColor("Assets/Materials/MAT_Phase16_AuraGlowPool.mat");
            AssertHdrBaseColor("Assets/Materials/MAT_CardGlow.mat");
            AssertHdrBaseColor("Assets/Materials/MAT_Phase18_RitualParticles.mat");

            var candle = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MAT_Phase8_CandleAmber.mat");
            Assert.That(candle, Is.Not.Null);
            Assert.That(candle.IsKeywordEnabled("_EMISSION"), Is.True, "Candle material must have emission enabled");

            var emission = candle.GetColor("_EmissionColor");
            Assert.That(Mathf.Max(emission.r, Mathf.Max(emission.g, emission.b)), Is.GreaterThan(0.5f),
                "Candle emission must be bright enough to feed bloom");
        }

        [Test]
        public void Phase20KeepsPriorVisualWiring()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");
            Assert.That(GameObject.Find("Phase16_RitualAuraRoot"), Is.Not.Null);

            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");
            Assert.That(GameObject.Find("Phase16_ResultAuraRoot"), Is.Not.Null);

            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var entries = new SerializedObject(catalog).FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(78));
        }

        [Test]
        public void Phase20DocumentationExists()
        {
            Assert.That(File.Exists(Phase20DocPath), Is.True, $"Missing Phase20 doc at {Phase20DocPath}");

            var text = File.ReadAllText(Phase20DocPath);
            Assert.That(text, Does.Contain("Cinematic Rendering"));
            Assert.That(text, Does.Contain("Phase20_CinematicVolume"));
            Assert.That(text, Does.Contain("ACES"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        private static void AssertHdrBaseColor(string materialPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.That(material, Is.Not.Null, $"Missing material at {materialPath}");
            Assert.That(material.HasProperty("_BaseColor"), Is.True, $"{materialPath} must expose _BaseColor");

            var color = material.GetColor("_BaseColor");
            Assert.That(Mathf.Max(color.r, Mathf.Max(color.g, color.b)), Is.GreaterThan(1.001f),
                $"{materialPath} base color should be boosted above LDR range so bloom can pick it up");
        }
    }
}

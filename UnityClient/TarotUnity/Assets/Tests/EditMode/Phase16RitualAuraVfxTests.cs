using System;
using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase16RitualAuraVfxTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string Phase16DocPath = "Docs/PHASE16_RITUAL_AURA_VFX.md";

        private static readonly string[] TransparentMaterialPaths =
        {
            "Assets/Materials/MAT_Phase16_AuraGlowPool.mat",
            "Assets/Materials/MAT_Phase16_RuneRing.mat",
            "Assets/Materials/MAT_Phase16_AuraParticle.mat",
        };

        [Test]
        public void RitualAuraControllerRuntimeTypeExists()
        {
            var type = Type.GetType("TarotUnity.Presentation.RitualAuraController, TarotUnity.Runtime");
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(type.GetMethod("SetAuraVisible", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(type.GetMethod("SetRuneVisible", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(type.GetMethod("SetParticlesVisible", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(type.GetMethod("SetIntensity", new[] { typeof(float) }), Is.Not.Null);
            Assert.That(type.GetProperty("CurrentIntensity"), Is.Not.Null);
        }

        [Test]
        public void ReadingRoomHasPhase16RitualAuraAnchors()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = GameObject.Find("Phase16_RitualAuraRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_GlowPool"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_RuneRingOuter"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_RuneRingInner"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ParticleAnchorNorth"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ParticleAnchorEast"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ParticleAnchorSouth"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ParticleAnchorWest"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_AuraFocusAnchor"), Is.Not.Null);

            var controllerType = Type.GetType("TarotUnity.Presentation.RitualAuraController, TarotUnity.Runtime");
            Assert.That(controllerType, Is.Not.Null);

            var controller = root.GetComponent(controllerType);
            Assert.That(controller, Is.Not.Null);

            var serialized = new SerializedObject(controller);
            Assert.That(serialized.FindProperty("auraRoot")?.objectReferenceValue, Is.SameAs(root));
            Assert.That(serialized.FindProperty("glowRenderers")?.arraySize, Is.GreaterThanOrEqualTo(1));
            Assert.That(serialized.FindProperty("runeRenderers")?.arraySize, Is.GreaterThanOrEqualTo(2));
            Assert.That(serialized.FindProperty("particleRenderers")?.arraySize, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void ResultHasPhase16RitualAuraAnchors()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var root = GameObject.Find("Phase16_ResultAuraRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ResultGlowPool"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ResultRuneRing"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ResultParticleAnchorLeft"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ResultParticleAnchorRight"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ResultAuraFocusAnchor"), Is.Not.Null);

            var controllerType = Type.GetType("TarotUnity.Presentation.RitualAuraController, TarotUnity.Runtime");
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(root.GetComponent(controllerType), Is.Not.Null);
        }

        [Test]
        public void Phase16MaterialsUseTransparentRenderSettings()
        {
            foreach (var materialPath in TransparentMaterialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                Assert.That(material, Is.Not.Null, $"Missing material at {materialPath}");
                Assert.That(material.renderQueue, Is.EqualTo((int)RenderQueue.Transparent), materialPath);
                Assert.That(material.GetTag("RenderType", false), Is.EqualTo("Transparent"), materialPath);
                AssertMaterialFloat(material, "_Surface", 1f, materialPath);
                AssertMaterialFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha, materialPath);
                AssertMaterialFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha, materialPath);
                AssertMaterialFloat(material, "_ZWrite", 0f, materialPath);
                Assert.That(material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), Is.True, materialPath);
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.False, materialPath);
            }
        }

        [Test]
        public void Phase16AuraObjectsDoNotBlockInput()
        {
            AssertNoBlockingObjects(ReadingRoomScenePath, "Phase16_RitualAuraRoot");
            AssertNoBlockingObjects(ResultScenePath, "Phase16_ResultAuraRoot");
        }

        [Test]
        public void Phase16KeepsCardArtAndPriorVisualWiring()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var card = prefab.GetComponent<CardView>();
            Assert.That(card, Is.Not.Null);

            var serializedCard = new SerializedObject(card);
            Assert.That(serializedCard.FindProperty("faceArtworkRenderer")?.objectReferenceValue, Is.Not.Null);
            Assert.That(serializedCard.FindProperty("dimensionalRevealController")?.objectReferenceValue, Is.Not.Null);
            Assert.That(serializedCard.FindProperty("threeDPresentationController")?.objectReferenceValue, Is.Not.Null);

            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase15_CardMeshRoot"), Is.Not.Null);
        }

        [Test]
        public void Phase16KeepsPhase13ArtworkCatalogAvailable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var entries = new SerializedObject(catalog).FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(78));
        }

        [Test]
        public void Phase16DocumentationExists()
        {
            Assert.That(File.Exists(Phase16DocPath), Is.True, $"Missing Phase16 doc at {Phase16DocPath}");

            var text = File.ReadAllText(Phase16DocPath);
            Assert.That(text, Does.Contain("Ritual Aura"));
            Assert.That(text, Does.Contain("RitualAuraController"));
            Assert.That(text, Does.Contain("Phase16_RitualAuraRoot"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        private static void AssertNoBlockingObjects(string scenePath, string rootName)
        {
            EditorSceneManager.OpenScene(scenePath);

            var root = GameObject.Find(rootName);
            Assert.That(root, Is.Not.Null, rootName);

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                Assert.Fail($"{collider.name} under {rootName} should not have a Collider.");
            }

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False, $"{graphic.name} under {rootName} should not block raycasts.");
            }
        }

        private static void AssertMaterialFloat(Material material, string propertyName, float expected, string path)
        {
            Assert.That(material.HasProperty(propertyName), Is.True, $"{path} missing {propertyName}");
            Assert.That(material.GetFloat(propertyName), Is.EqualTo(expected), $"{path} {propertyName}");
        }
    }
}

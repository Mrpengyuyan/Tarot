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
    public sealed class Phase15ThreeDTableFoundationTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string Phase15DocPath = "Docs/PROJECT_CHRONICLE.md";

        private static readonly string[] OpaqueMaterialPaths =
        {
            "Assets/Materials/MAT_Phase15_CardBody.mat",
            "Assets/Materials/MAT_Phase15_CardFacePlane.mat",
            "Assets/Materials/MAT_Phase15_CardBackPlane.mat",
            "Assets/Materials/MAT_Phase15_CardSideEdge.mat",
            // MAT_Phase15_RitualTableSurface was deleted with its plane (Phase 41).
            "Assets/Materials/MAT_Phase15_ResultPedestal.mat",
        };

        private static readonly string[] TransparentMaterialPaths =
        {
            "Assets/Materials/MAT_Phase15_CardDropShadow.mat",
            // MAT_Phase15_TableDepthRing was deleted with its ring (Phase 41).
        };

        [Test]
        public void ThreeDCardPresentationControllerRuntimeTypeExists()
        {
            var type = Type.GetType("TarotUnity.Presentation.ThreeDCardPresentationController, TarotUnity.Runtime");
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(type.GetMethod("SetFaceVisible", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(type.GetMethod("SetDropShadowVisible", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(type.GetMethod("SetShellVisible", new[] { typeof(bool) }), Is.Not.Null);
        }

        [Test]
        public void Phase15TableStageControllerRuntimeTypeExists()
        {
            var type = Type.GetType("TarotUnity.Presentation.Phase15TableStageController, TarotUnity.Runtime");
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(type.GetMethod("SetStageVisible", new[] { typeof(bool) }), Is.Not.Null);
        }

        [Test]
        public void CardViewHasThreeDPresentationControllerWiredOnPrefab()
        {
            var controllerType = Type.GetType("TarotUnity.Presentation.ThreeDCardPresentationController, TarotUnity.Runtime");
            Assert.That(controllerType, Is.Not.Null);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var card = prefab.GetComponent<CardView>();
            Assert.That(card, Is.Not.Null);

            var controller = prefab.GetComponent(controllerType);
            Assert.That(controller, Is.Not.Null);

            var serializedCard = new SerializedObject(card);
            var threeDPresentationController = serializedCard.FindProperty("threeDPresentationController");
            Assert.That(threeDPresentationController, Is.Not.Null);
            Assert.That(threeDPresentationController.objectReferenceValue, Is.SameAs(controller));
        }

        [Test]
        public void CardPrefabHasPhase15CardMeshHierarchy()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var meshRoot = prefab.transform.Find("Phase15_CardMeshRoot");
            Assert.That(meshRoot, Is.Not.Null);
            Assert.That(meshRoot.Find("Phase15_CardBody"), Is.Not.Null);
            Assert.That(meshRoot.Find("Phase15_CardFacePlane"), Is.Not.Null);
            Assert.That(meshRoot.Find("Phase15_CardBackPlane"), Is.Not.Null);
            Assert.That(meshRoot.Find("Phase15_CardSideEdge"), Is.Not.Null);
            Assert.That(meshRoot.Find("Phase15_CardDropShadow"), Is.Not.Null);
        }

        [Test]
        public void CardPrefabRetainsPhase13AndPhase14Wiring()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var card = prefab.GetComponent<CardView>();
            Assert.That(card, Is.Not.Null);

            var serializedCard = new SerializedObject(card);
            var faceArtworkRenderer = serializedCard.FindProperty("faceArtworkRenderer");
            Assert.That(faceArtworkRenderer, Is.Not.Null);
            Assert.That(faceArtworkRenderer.objectReferenceValue, Is.Not.Null);

            var dimensionalRevealController = serializedCard.FindProperty("dimensionalRevealController");
            Assert.That(dimensionalRevealController, Is.Not.Null);
            Assert.That(dimensionalRevealController.objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void DimensionalRevealControllerStillTargetsPhase14Root()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var dimensionalRoot = prefab.transform.Find("Phase14_DimensionalRoot");
            Assert.That(dimensionalRoot, Is.Not.Null);

            var controller = prefab.GetComponent<DimensionalCardRevealController>();
            Assert.That(controller, Is.Not.Null);

            var serializedController = new SerializedObject(controller);
            var cardRoot = serializedController.FindProperty("cardRoot");
            Assert.That(cardRoot, Is.Not.Null);
            Assert.That(cardRoot.objectReferenceValue, Is.SameAs(dimensionalRoot));
        }

        [Test]
        public void Phase15OpaqueMaterialsUseOpaqueRenderSettings()
        {
            foreach (var materialPath in OpaqueMaterialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                Assert.That(material, Is.Not.Null, $"Missing material at {materialPath}");
                Assert.That(material.renderQueue, Is.EqualTo((int)RenderQueue.Geometry), materialPath);
                Assert.That(material.GetTag("RenderType", false), Is.EqualTo("Opaque"), materialPath);
                AssertMaterialFloat(material, "_Surface", 0f, materialPath);
                AssertMaterialFloat(material, "_SrcBlend", (float)BlendMode.One, materialPath);
                AssertMaterialFloat(material, "_DstBlend", (float)BlendMode.Zero, materialPath);
                AssertMaterialFloat(material, "_ZWrite", 1f, materialPath);
                Assert.That(material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), Is.False, materialPath);
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.False, materialPath);
            }
        }

        [Test]
        public void Phase15TransparentMaterialsUseTransparentBlendSettings()
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
        public void ReadingRoomHasPhase15ThreeDTableFoundation()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = GameObject.Find("Phase15_ThreeDTableRoot");
            Assert.That(root, Is.Not.Null);
            // Phase 38 retired the flat table planes in favor of the Midnight
            // Parlor velvet stage; they must stay gone.
            Assert.That(root.transform.Find("Phase15_RitualTableSurface"), Is.Null);
            Assert.That(root.transform.Find("Phase15_TableDepthRing"), Is.Null);
            Assert.That(GameObject.Find("MP_TableStage"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase15_DeckFocusAnchor"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase15_SpreadFocusAnchor"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase15_FlipFocusAnchor"), Is.Not.Null);

            Assert.That(GameObject.Find("Phase15_WarmKeyLight")?.GetComponent<Light>(), Is.Not.Null);
            Assert.That(GameObject.Find("Phase15_CoolRimLight")?.GetComponent<Light>(), Is.Not.Null);

            var stageControllerType = Type.GetType("TarotUnity.Presentation.Phase15TableStageController, TarotUnity.Runtime");
            Assert.That(stageControllerType, Is.Not.Null);
            var controller = root.GetComponent(stageControllerType);
            Assert.That(controller, Is.Not.Null);

            var serializedController = new SerializedObject(controller);
            Assert.That(serializedController.FindProperty("tableRoot")?.objectReferenceValue, Is.SameAs(root));
            Assert.That(serializedController.FindProperty("warmKeyLight")?.objectReferenceValue, Is.SameAs(GameObject.Find("Phase15_WarmKeyLight")?.GetComponent<Light>()));
            Assert.That(serializedController.FindProperty("coolRimLight")?.objectReferenceValue, Is.SameAs(GameObject.Find("Phase15_CoolRimLight")?.GetComponent<Light>()));
        }

        [Test]
        public void ResultHasPhase15ResultCardStage()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var stageRoot = GameObject.Find("Phase15_ResultCardStageRoot");
            Assert.That(stageRoot, Is.Not.Null);
            Assert.That(stageRoot.transform.Find("Phase15_ResultCardPedestal"), Is.Not.Null);
            Assert.That(stageRoot.transform.Find("Phase15_ResultFocusAnchor"), Is.Not.Null);

            Assert.That(GameObject.Find("Phase15_ResultWarmFocusLight")?.GetComponent<Light>(), Is.Not.Null);
            Assert.That(GameObject.Find("Phase15_ResultCoolEdgeLight")?.GetComponent<Light>(), Is.Not.Null);
        }

        [Test]
        public void ResultPhase15ImagesDoNotBlockRaycasts()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var canvas = GameObject.Find("ResultCanvas");
            Assert.That(canvas, Is.Not.Null);

            foreach (var image in canvas.GetComponentsInChildren<Image>(true))
            {
                if (!image.name.StartsWith("Phase15_", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.That(image.raycastTarget, Is.False, $"{image.name} should not block result UI raycasts.");
            }
        }

        [Test]
        public void Phase15KeepsPhase13ArtworkCatalogAvailable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var serializedCatalog = new SerializedObject(catalog);
            var entries = serializedCatalog.FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(78));
        }

        [Test]
        public void Phase15DocumentationExists()
        {
            Assert.That(File.Exists(Phase15DocPath), Is.True, $"Missing Phase15 doc at {Phase15DocPath}");

            var text = File.ReadAllText(Phase15DocPath);
            Assert.That(text, Does.Contain("3D table foundation"));
            Assert.That(text, Does.Contain("ThreeDCardPresentationController"));
            Assert.That(text, Does.Contain("Phase15_ThreeDTableRoot"));
        }

        private static void AssertMaterialFloat(Material material, string propertyName, float expected, string path)
        {
            Assert.That(material.HasProperty(propertyName), Is.True, $"{path} missing {propertyName}");
            Assert.That(material.GetFloat(propertyName), Is.EqualTo(expected), $"{path} {propertyName}");
        }
    }
}

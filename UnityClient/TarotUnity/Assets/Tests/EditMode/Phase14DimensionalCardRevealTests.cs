using System;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase14DimensionalCardRevealTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string Phase14DocPath = "Docs/PHASE14_DIMENSIONAL_CARD_REVEAL.md";
        // Phase 38/41 deleted the always-on dressing quads and their materials;
        // only the animated reveal glow survives from the Phase 14 material set.
        private static readonly string[] Phase14MaterialPaths =
        {
            "Assets/Materials/MAT_Phase14_RevealGlow.mat",
        };

        [Test]
        public void DimensionalRevealControllerTypeExists()
        {
            var type = Type.GetType("TarotUnity.Presentation.DimensionalCardRevealController, TarotUnity.Runtime");
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(type.GetMethod("PlayReveal", Type.EmptyTypes), Is.Not.Null);
            Assert.That(type.GetMethod("SetGlowVisible", new[] { typeof(bool) }), Is.Not.Null);
        }

        [Test]
        public void CardPrefabHasPhase14DimensionalAnchors()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot"), Is.Not.Null);
            // Phase 38 deleted the always-on 2.5D dressing quads; only the animated
            // reveal glow survives on the dimensional root.
            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot/Phase14_CardEdge"), Is.Null);
            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot/Phase14_CastShadow"), Is.Null);
            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot/Phase14_FaceRimLight"), Is.Null);
            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot/Phase14_ArtworkGlass"), Is.Null);
            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot/Phase14_RevealGlow"), Is.Not.Null);

            var card = prefab.GetComponent<CardView>();
            Assert.That(card, Is.Not.Null);

            var serializedCard = new SerializedObject(card);
            var revealController = serializedCard.FindProperty("dimensionalRevealController");
            Assert.That(revealController, Is.Not.Null);
            Assert.That(revealController.objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void CardPrefabRevealControllerTargetsDimensionalRoot()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var dimensionalRoot = prefab.transform.Find("Phase14_DimensionalRoot");
            Assert.That(dimensionalRoot, Is.Not.Null);

            var revealGlow = dimensionalRoot.Find("Phase14_RevealGlow");
            Assert.That(revealGlow, Is.Not.Null);

            var controller = prefab.GetComponent<DimensionalCardRevealController>();
            Assert.That(controller, Is.Not.Null);

            var serializedController = new SerializedObject(controller);
            var cardRoot = serializedController.FindProperty("cardRoot");
            var revealGlowRenderer = serializedController.FindProperty("revealGlowRenderer");

            Assert.That(cardRoot, Is.Not.Null);
            Assert.That(cardRoot.objectReferenceValue, Is.SameAs(dimensionalRoot));
            Assert.That(revealGlowRenderer, Is.Not.Null);
            Assert.That(revealGlowRenderer.objectReferenceValue, Is.SameAs(revealGlow.GetComponent<MeshRenderer>()));
        }

        [Test]
        public void Phase14MaterialsUseTransparentBlendSettings()
        {
            foreach (var materialPath in Phase14MaterialPaths)
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
        public void CardViewAllowsMissingDimensionalRevealController()
        {
            var cardObject = new GameObject("Card");
            try
            {
                var card = cardObject.AddComponent<CardView>();
                Assert.DoesNotThrow(() => card.SetFaceUp(true));
                Assert.DoesNotThrow(() => card.SetFaceUp(false));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void ReadingRoomHasPhase14RevealStageAnchors()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            // Phase 38 deleted the flat depth/pool planes; the warm light stays.
            Assert.That(GameObject.Find("Phase14_TableDepthPlane"), Is.Null);
            Assert.That(GameObject.Find("Phase14_CardRevealPool"), Is.Null);
            Assert.That(GameObject.Find("Phase14_RevealLightWarm"), Is.Not.Null);
        }

        [Test]
        public void ResultHasPhase14CardFocusAnchors()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var canvas = GameObject.Find("ResultCanvas");
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase14_ResultCardHalo"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase14_ResultCardShadow"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase14_ResultTextBridge"), Is.Not.Null);
        }

        [Test]
        public void Phase14KeepsPhase13ArtworkCatalogAvailable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var serializedCatalog = new SerializedObject(catalog);
            var entries = serializedCatalog.FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(78));
        }

        [Test]
        public void Phase14DocumentationExists()
        {
            Assert.That(System.IO.File.Exists(Phase14DocPath), Is.True, $"Missing Phase14 doc at {Phase14DocPath}");
            var text = System.IO.File.ReadAllText(Phase14DocPath);
            Assert.That(text, Does.Contain("2.5D dimensional card reveal"));
            Assert.That(text, Does.Contain("DimensionalCardRevealController"));
            Assert.That(text, Does.Contain("Phase14_DimensionalRoot"));
        }

        private static void AssertMaterialFloat(Material material, string propertyName, float expected, string materialPath)
        {
            Assert.That(material.HasProperty(propertyName), Is.True, $"{materialPath} missing {propertyName}");
            Assert.That(material.GetFloat(propertyName), Is.EqualTo(expected), $"{materialPath} {propertyName}");
        }
    }
}

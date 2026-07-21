using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase28HolographicCardTests
    {
        private const string ShaderName = "TarotUnity/HolographicCard";
        private const string MaterialPath = "Assets/Materials/MAT_HolographicCardFace.mat";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string DocPath = "Docs/PROJECT_CHRONICLE.md";

        [Test]
        public void HolographicShaderCompilesAndIsSupported()
        {
            var shader = Shader.Find(ShaderName);
            Assert.That(shader, Is.Not.Null, $"Shader {ShaderName} not found");
            Assert.That(shader.isSupported, Is.True, "Holographic shader failed to compile / is not supported");
        }

        [Test]
        public void MaterialUsesHolographicShader()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Assert.That(material, Is.Not.Null, $"Missing material at {MaterialPath}");
            Assert.That(material.shader.name, Is.EqualTo(ShaderName));
            Assert.That(material.GetFloat("_GlareIntensity"), Is.InRange(0.1f, 1.5f),
                "Glare should be tuned to a tasteful range, not blown out");
        }

        [Test]
        public void CardFaceRendererUsesHolographicMaterial()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var cardView = prefab.GetComponentInChildren<CardView>(true);
            Assert.That(cardView, Is.Not.Null);

            var faceRenderer = new SerializedObject(cardView)
                .FindProperty("faceArtworkRenderer").objectReferenceValue as SpriteRenderer;
            Assert.That(faceRenderer, Is.Not.Null, "CardView should have a faceArtworkRenderer");
            Assert.That(faceRenderer.sharedMaterial, Is.Not.Null);
            Assert.That(faceRenderer.sharedMaterial.shader.name, Is.EqualTo(ShaderName),
                "The flipped card face should render through the holographic material");
        }

        [Test]
        public void Phase28DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase28 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("holographic"));
            Assert.That(text, Does.Contain("sheen"));
            Assert.That(text, Does.Contain("Sprites/Default"));
        }
    }
}

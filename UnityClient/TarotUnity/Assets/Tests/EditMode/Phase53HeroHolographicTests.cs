using System.IO;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 53 puts the holographic foil on the Result hero card. Because it is a
    /// UI Image on an Overlay canvas (no view angle), the sheen is a dedicated UI
    /// shader driven by HolographicHeroCard. These guard the wiring: the shader and
    /// material exist, the hero Image wears the material and can hear the pointer,
    /// and the driver is attached and pointed at both.
    /// </summary>
    public sealed class Phase53HeroHolographicTests
    {
        private const string ScenePath = "Assets/Scenes/Result.unity";
        private const string ShaderName = "TarotUnity/HolographicCardUI";
        private const string MaterialPath = "Assets/Materials/MAT_HolographicHeroCardUI.mat";
        private const string HeroName = "Phase12_ResultCardArtworkSlot";
        private const string DocPath = "Docs/PROJECT_CHRONICLE.md";

        [Test]
        public void TheUiHolographicShaderExists()
        {
            Assert.That(Shader.Find(ShaderName), Is.Not.Null,
                "the hero card needs a UI foil shader, distinct from the 3D face shader");
        }

        [Test]
        public void TheMaterialUsesTheUiHolographicShader()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Assert.That(material, Is.Not.Null, $"missing {MaterialPath}");
            Assert.That(material.shader, Is.Not.Null);
            Assert.That(material.shader.name, Is.EqualTo(ShaderName));
        }

        [Test]
        public void TheHeroCardWearsTheFoilAndCanHearThePointer()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var hero = GameObject.Find(HeroName)
                ?? FindInactive(HeroName);
            Assert.That(hero, Is.Not.Null, $"{HeroName} missing");

            var image = hero.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.material, Is.Not.Null);
            Assert.That(image.material.shader.name, Is.EqualTo(ShaderName),
                "the hero Image must render through the foil shader");
            Assert.That(image.raycastTarget, Is.True,
                "the pointer-reactive foil needs the Image to receive hovers");
        }

        [Test]
        public void TheDriverIsAttachedAndWired()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var hero = GameObject.Find(HeroName) ?? FindInactive(HeroName);
            Assert.That(hero, Is.Not.Null);

            var holo = hero.GetComponent<HolographicHeroCard>();
            Assert.That(holo, Is.Not.Null, "HolographicHeroCard must drive the sheen");

            var so = new SerializedObject(holo);
            Assert.That(so.FindProperty("heroImage").objectReferenceValue, Is.Not.Null,
                "the driver must point at the hero Image");
            Assert.That(so.FindProperty("holographicMaterial").objectReferenceValue, Is.Not.Null,
                "the driver must hold the material it instances at runtime");
        }

        [Test]
        public void Phase53DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase53 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("holographic"));
            Assert.That(text, Does.Contain("Overlay"));
        }

        private static GameObject FindInactive(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }

            return null;
        }
    }
}

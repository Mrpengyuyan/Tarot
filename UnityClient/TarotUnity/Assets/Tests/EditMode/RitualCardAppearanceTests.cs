using System.Linq;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.Data;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class RitualCardAppearanceTests
    {
        [Test]
        public void PreviewPaperSurfacePreservesOpaqueArtworkAndDisablesOnlyFoilProperties()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            var root = Object.Instantiate(prefab);
            var destination = new GameObject("PaperSurfaceTest", typeof(RitualSpreadSlot));
            try
            {
                var card = root.GetComponent<CardView>();
                var draw = LocalReadingSimulator.CreatePlaceholderDraws(1)[0];
                card.Bind(draw);
                card.SetFaceArtwork(Resources.Load<CardArtworkCatalog>("TarotArt/RWS1909_CardArtworkCatalog").FindSprite(draw));
                var image = card.FaceArtwork;
                var material = image.sharedMaterial;
                var oldGlare = material.GetFloat("_GlareIntensity");
                var block = new MaterialPropertyBlock();
                block.SetFloat("_GlareIntensity", 0.75f);
                block.SetFloat("_Iridescence", 0.5f);
                block.SetFloat("_GlareShift", 1.23f);
                image.SetPropertyBlock(block);
                destination.GetComponent<RitualSpreadSlot>().Dock(card);
                card.SetFaceUp(true);
                Assert.That(image.color, Is.EqualTo(Color.white), "Do not tint the source artwork gold or show the table through it.");
                image.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_GlareIntensity"), Is.EqualTo(0f));
                Assert.That(block.GetFloat("_Iridescence"), Is.EqualTo(0f));
                Assert.That(block.GetFloat("_GlareShift"), Is.EqualTo(1.23f));
                Assert.That(image.sharedMaterial, Is.SameAs(material));
                Assert.That(material.GetFloat("_GlareIntensity"), Is.EqualTo(oldGlare), "Other screens keep their shared material.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(destination);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PrintedFaceDoesNotSitOnTheLegacySlab(bool ritual)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            var root = Object.Instantiate(prefab);
            try
            {
                if (ritual) root.GetComponent<CardFlipController>().ApplyRitualPresentation();
                root.GetComponent<CardView>().SetFaceUp(true);
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Single(r => r.name == "Phase15_CardBody").enabled, Is.EqualTo(!ritual));
                Assert.That(renderers.Single(r => r.name == "Phase15_CardDropShadow").enabled, Is.EqualTo(!ritual));
                if (ritual)
                    Assert.That(renderers.Where(r => r != root.GetComponent<CardView>().FaceArtwork)
                        .Any(r => r.enabled && r.gameObject.activeInHierarchy), Is.False,
                        "the old shell's edge rails and backing must not remain beside the printed face");
                root.GetComponent<CardView>().SetFaceUp(false);
                Assert.That(renderers.Single(r => r.name == "Phase15_CardBody").enabled, Is.True,
                    "the face-down physical card keeps its thickness");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}

using System.Linq;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class RitualCardAppearanceTests
    {
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

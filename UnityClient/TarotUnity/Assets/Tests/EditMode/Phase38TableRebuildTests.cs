using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 38 guards the Midnight Parlor table rebuild: the velvet stage exists
    /// with rim, backdrop and four gold sockets, the deck is a staggered stack
    /// wearing the composed back, the card prefab carries the real back texture,
    /// and the superseded flat-plane era stays deleted.
    /// </summary>
    public sealed class Phase38TableRebuildTests
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string Materials = "Assets/Art/MidnightParlor/Materials";

        [Test]
        public void ReadingRoomHasMidnightParlorStage()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var stage = GameObject.Find("MP_TableStage");
            Assert.That(stage, Is.Not.Null);
            foreach (var child in new[]
            {
                "MP_TableCloth", "MP_TableRimFar", "MP_TableRimLeft", "MP_TableRimRight", "MP_ParlorBackdrop",
            })
            {
                Assert.That(stage.transform.Find(child), Is.Not.Null, child);
            }

            var sockets = stage.transform.Find("MP_CardSockets");
            Assert.That(sockets, Is.Not.Null);
            Assert.That(sockets.childCount, Is.EqualTo(4), "one gold socket per card slot");
        }

        [Test]
        public void LegacyPlanesStayDeleted()
        {
            EditorSceneManager.OpenScene(ScenePath);

            foreach (var name in new[]
            {
                "Graybox Tarot Table", "Phase8_ReadingVisualRoot", "Phase12_CardRevealStage",
                "Phase12_RevealBackdrop", "Phase14_TableDepthPlane", "Phase14_CardRevealPool",
            })
            {
                Assert.That(GameObject.Find(name), Is.Null, $"{name} must stay deleted");
            }
        }

        [Test]
        public void SlotSlabsAreHiddenButKeepTheirAnchors()
        {
            EditorSceneManager.OpenScene(ScenePath);

            foreach (var slotName in new[] { "OneCardSlot", "PastSlot", "PresentSlot", "AdviceSlot" })
            {
                var slot = GameObject.Find(slotName);
                Assert.That(slot, Is.Not.Null, slotName);
                var renderer = slot.GetComponent<MeshRenderer>();
                Assert.That(renderer, Is.Not.Null, slotName);
                Assert.That(renderer.enabled, Is.False, $"{slotName} slab should be invisible");
            }
        }

        [Test]
        public void DeckIsAStaggeredStackWearingTheBack()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var deck = GameObject.Find("DeckStack");
            Assert.That(deck, Is.Not.Null);

            var graybox = deck.transform.Find("Graybox Deck");
            Assert.That(graybox, Is.Not.Null);
            Assert.That(graybox.GetComponent<MeshRenderer>().enabled, Is.False);

            var stack = deck.transform.Find("MP_DeckStack");
            Assert.That(stack, Is.Not.Null);
            Assert.That(stack.childCount, Is.GreaterThanOrEqualTo(8));

            var top = stack.Find("MP_DeckTopBack");
            Assert.That(top, Is.Not.Null);
            Assert.That(top.GetComponent<MeshRenderer>().sharedMaterial.name, Is.EqualTo("MP_CardBack"));
        }

        [Test]
        public void CardPrefabWearsTheComposedBack()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var face = prefab.transform.Find("Back/MP_CardBackFace");
            Assert.That(face, Is.Not.Null);
            Assert.That(face.GetComponent<MeshRenderer>().sharedMaterial.name, Is.EqualTo("MP_CardBack"));

            var shellBack = prefab.transform.Find("Phase15_CardMeshRoot/Phase15_CardBackPlane");
            Assert.That(shellBack, Is.Not.Null);
            Assert.That(shellBack.GetComponent<MeshRenderer>().sharedMaterial.name, Is.EqualTo("MP_CardBack"));
        }

        [Test]
        public void RebuildMaterialsExist()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_DeckBody.mat"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_ParlorBackdrop.mat"), Is.Not.Null);
        }

        [Test]
        public void Phase38DocumentationExists()
        {
            const string doc = "Docs/PHASE38_TABLE_REBUILD.md";
            Assert.That(File.Exists(doc), Is.True);
            var text = File.ReadAllText(doc);
            Assert.That(text, Does.Contain("deleted"));
            Assert.That(text, Does.Contain("MP_TableStage"));
        }
    }
}

using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 74: the user's third pass on the draw - a three-second shuffle, a pick whose flight
    /// grows with the distance (the camera travels with the last one), and a flip that turns the
    /// card over its long edge and sweeps a light across the face.
    /// </summary>
    public sealed class Phase74DrawFeelTests
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string DocPath = "Docs/PHASE74_DRAW_FEEL.md";

        [Test]
        public void TheShuffleTakesAtLeastThreeSeconds()
        {
            // Phase 75 stretched it again, to about four (Phase75DrawTruthTests).
            EditorSceneManager.OpenScene(ScenePath);
            var shuffle = Object.FindFirstObjectByType<DeckShuffleChoreographer>();
            Assert.That(shuffle.PlannedSeconds, Is.GreaterThanOrEqualTo(2.9f));
        }

        // The deck is eight blocks in the tan paper-edge material; a cut used to bare a tan top on each pile.
        [Test]
        public void EveryDeckCardShowsACardBackOnTop()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var stack = GameObject.Find("DeckStack").transform.Find("MP_DeckStack");
            var blocks = 0;
            foreach (Transform block in stack)
            {
                if (!block.name.StartsWith("MP_DeckCard_"))
                {
                    continue;
                }

                blocks++;
                var back = block.Find("Phase74_CardBack");
                Assert.That(back, Is.Not.Null, $"{block.name} has a back");
                var renderer = back.GetComponent<MeshRenderer>();
                Assert.That(renderer.enabled, Is.True);
                Assert.That(renderer.sharedMaterial.name, Is.EqualTo("MP_CardBack"));
                Assert.That(Vector3.Dot(-back.forward, Vector3.up), Is.GreaterThan(0.99f), "the quad faces up");
                var top = block.GetComponent<Renderer>().bounds.max.y;
                Assert.That(back.position.y - top, Is.InRange(0f, 0.004f), "on the block's top face");
                var size = renderer.bounds.size;
                var blockSize = block.GetComponent<Renderer>().bounds.size;
                Assert.That(size.x / blockSize.x, Is.InRange(0.93f, 1f), "as wide as the card");
                Assert.That(size.z / blockSize.z, Is.InRange(0.95f, 1f), "as long as the card");
            }

            Assert.That(blocks, Is.EqualTo(8), "control: the eight blocks");
        }

        [Test]
        public void APickFlightGrowsWithDistanceWithinALimit()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var deck = Object.FindFirstObjectByType<DeckController>();
            Assert.That(deck.PickFlightSeconds(5f), Is.GreaterThan(deck.PickFlightSeconds(1f) + 0.2f), "farther flies longer");
            Assert.That(deck.PickFlightSeconds(5f), Is.InRange(0.75f, 1f), "a five-metre flight is unhurried but not slow");
            Assert.That(deck.PickFlightSeconds(50f), Is.LessThanOrEqualTo(1f), "capped");
        }

        [Test]
        public void TheCardCarriesAHiddenSheenOverItsFace()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            var flip = new SerializedObject(prefab.GetComponent<CardFlipController>());
            var sheen = flip.FindProperty("revealSheen").objectReferenceValue as MeshRenderer;
            Assert.That(sheen, Is.Not.Null, "the flip has its sheen");
            Assert.That(sheen.transform.IsChildOf(prefab.transform), Is.True);
            Assert.That(sheen.enabled, Is.False, "off until the sweep");
            Assert.That(sheen.GetComponent<Collider>(), Is.Null, "never a pointer target");
            Assert.That(sheen.sharedMaterial, Is.Not.Null);
            Assert.That(sheen.sharedMaterial.GetFloat("_DstBlend"), Is.EqualTo((float)UnityEngine.Rendering.BlendMode.One), "additive: it only adds light");
            Assert.That(sheen.sharedMaterial.renderQueue, Is.GreaterThan(3000), "drawn over the face art");

            var art = prefab.transform.Find("Front/Phase12_FaceArtworkPlaceholder");
            Assert.That(sheen.transform.parent, Is.EqualTo(art), "it rides on the artwork (the flip sizes it to the sprite)");
            Assert.That(Quaternion.Angle(sheen.transform.localRotation, Quaternion.identity), Is.LessThan(0.01f), "facing the way the art faces");
            var lift = prefab.transform.InverseTransformPoint(sheen.transform.position).y - prefab.transform.InverseTransformPoint(art.position).y;
            Assert.That(lift, Is.InRange(0.001f, 0.01f), "a hair in front of the art");
        }

        [Test]
        public void TheSheenTextureIsClearAtItsSideEdges()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/MidnightParlor/Textures/MP_FlipSheen.png");
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), "clamped, so an offset band leaves the face clear");
            var readable = new Texture2D(2, 2);
            readable.LoadImage(File.ReadAllBytes("Assets/Art/MidnightParlor/Textures/MP_FlipSheen.png"));
            var peak = 0f;
            for (var y = 0; y < readable.height; y += 8)
            {
                Assert.That(readable.GetPixel(0, y).a, Is.EqualTo(0f), "left edge clear");
                Assert.That(readable.GetPixel(readable.width - 1, y).a, Is.EqualTo(0f), "right edge clear");
                peak = Mathf.Max(peak, readable.GetPixel(readable.width / 2, y).a);
            }

            Assert.That(peak, Is.GreaterThan(0.8f), "a real band down the middle");
            Object.DestroyImmediate(readable);
        }

        [Test]
        public void TheFlipIsUnhurriedButUnderTwoSeconds()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            var flip = new SerializedObject(prefab.GetComponent<CardFlipController>());
            var total = flip.FindProperty("anticipationPause").floatValue
                        + flip.FindProperty("flipDuration").floatValue
                        + flip.FindProperty("faceRevealPause").floatValue
                        + flip.FindProperty("settleSeconds").floatValue
                        + flip.FindProperty("setDownSquashSeconds").floatValue;
            Assert.That(total, Is.InRange(1f, 1.6f));
            Assert.That(flip.FindProperty("raiseHeight").floatValue, Is.GreaterThan(0.4f),
                "lifted clear of the cloth when it stands on its edge (half its width is 0.39)");
        }

        [Test]
        public void Phase74DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("洗牌"));
            Assert.That(text, Does.Contain("翻牌"));
            Assert.That(text, Does.Contain("镜头"));
        }
    }
}

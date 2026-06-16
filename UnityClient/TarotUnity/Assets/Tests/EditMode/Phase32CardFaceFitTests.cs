using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 32 guards the fix for the oversized face-up card art. After Phase 27 swapped
    /// in HD sprites (imported at PPU 100), the face artwork rendered far larger than the
    /// card body because SetFaceArtwork assigned the sprite without sizing it. The face
    /// must now fit within the card footprint regardless of the sprite's resolution/PPU.
    /// </summary>
    public sealed class Phase32CardFaceFitTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";

        [Test]
        public void FaceArtworkFitsCardFootprintAfterFlip()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null, "card prefab should exist");

            var catalog = AssetDatabase.LoadAssetAtPath<CardArtworkCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, "artwork catalog should exist");
            var sprite = catalog.FindSprite("major_01");
            Assert.That(sprite, Is.Not.Null, "HD sprite should resolve");

            var go = Object.Instantiate(prefab);
            try
            {
                var view = go.GetComponentInChildren<CardView>(true);
                Assert.That(view, Is.Not.Null);

                view.SetFaceUp(true);
                view.SetFaceArtwork(sprite);

                var face = new SerializedObject(view).FindProperty("faceArtworkRenderer").objectReferenceValue as SpriteRenderer;
                Assert.That(face, Is.Not.Null, "faceArtworkRenderer must be wired");
                Assert.That(face.enabled, Is.True, "face artwork should be visible when face up");

                // Use the front face (active when the card is face up) as the reference.
                var cardFront = FindRenderer(go.transform, "Front");
                Assert.That(cardFront, Is.Not.Null, "card front renderer should exist for the footprint reference");

                var faceSize = face.bounds.size;
                var bodySize = cardFront.bounds.size;

                // The face artwork must sit within the card footprint (small tolerance for
                // a frame margin), not overflow it like the raw HD sprite did (2.65 x 9.06
                // vs a 0.78 x 1.18 body).
                Assert.That(faceSize.x, Is.LessThanOrEqualTo(bodySize.x * 1.05f),
                    $"face width {faceSize.x} overflows card width {bodySize.x}");
                Assert.That(faceSize.z, Is.LessThanOrEqualTo(bodySize.z * 1.05f),
                    $"face length {faceSize.z} overflows card length {bodySize.z}");

                // And it should still be a meaningful size (not collapsed to nothing).
                Assert.That(faceSize.x, Is.GreaterThan(bodySize.x * 0.3f),
                    "face width should fill a reasonable portion of the card");
                Assert.That(faceSize.z, Is.GreaterThan(bodySize.z * 0.3f),
                    "face length should fill a reasonable portion of the card");

                // The art must sit clear of the opaque front plane so it is not occluded
                // (it sat only ~0.003 above before the lift, which the front plane hid).
                Assert.That(face.bounds.center.y, Is.GreaterThan(cardFront.bounds.center.y + 0.01f),
                    $"face center Y {face.bounds.center.y} should clear the card front {cardFront.bounds.center.y}");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static Renderer FindRenderer(Transform root, string name)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == name)
                {
                    return r;
                }
            }

            return null;
        }
    }
}

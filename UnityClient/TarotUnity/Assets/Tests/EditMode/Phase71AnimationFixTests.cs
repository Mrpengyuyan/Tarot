using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 71: the menu censer throws embers instead of its old smoke (a bead-string of glowing
    /// blobs), and hovering a card lights a warm halo under its edges
    /// instead of the Phase 2 graybox slab, an opaque amber box laid over the whole card.
    /// </summary>
    public sealed class Phase71AnimationFixTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        [Test]
        public void CenserThrowsFlickeringEmbers()
        {
            var embers = CenserChild("Embers");
            var main = embers.main;
            Assert.That(main.startSize.constantMax, Is.LessThanOrEqualTo(0.05f), "embers are sparks, not blobs");
            Assert.That(main.startSize.constantMin, Is.LessThan(main.startSize.constantMax), "sizes vary");
            Assert.That(main.startLifetime.constantMin, Is.LessThan(main.startLifetime.constantMax), "lifetimes vary");
            Assert.That(main.gravityModifier.constant, Is.LessThan(0f), "embers rise and speed up");
            Assert.That(embers.emission.burstCount, Is.GreaterThanOrEqualTo(1), "an occasional burst sets a rhythm");
            Assert.That(embers.noise.enabled, Is.True, "they swirl");
            Assert.That(embers.noise.sizeAmount.constant, Is.GreaterThan(0f), "they flicker");
            Assert.That(embers.sizeOverLifetime.enabled, Is.True, "they burn out");
            Assert.That(embers.sizeOverLifetime.size.curve.Evaluate(1f), Is.LessThan(embers.sizeOverLifetime.size.curve.Evaluate(0.2f)));
            Assert.That(embers.GetComponent<ParticleSystemRenderer>().renderMode, Is.EqualTo(ParticleSystemRenderMode.Stretch),
                "a short streak along the motion reads as a spark");
        }

        // With the warm glow sprite the smoke read as blobs, a gold column or two bright smudges;
        // the embers carry the censer and the smoke object stays only for Phase45MenuDepthTests.
        [Test]
        public void SmokeNoLongerDraws()
        {
            var smoke = CenserChild("Smoke");
            Assert.That(smoke.emission.enabled, Is.False);
            Assert.That(smoke.main.playOnAwake, Is.False);
            Assert.That(smoke.GetComponent<ParticleSystemRenderer>().enabled, Is.False);
        }

        [Test]
        public void CardHoverIsAWarmHaloUnderTheCard()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            var so = new SerializedObject(prefab.GetComponent<CardView>());
            var highlight = (GameObject)so.FindProperty("highlightRoot").objectReferenceValue;
            Assert.That(highlight, Is.Not.Null);
            Assert.That(highlight.activeSelf, Is.False, "off until hovered");

            var renderer = highlight.GetComponent<MeshRenderer>();
            Assert.That(renderer.sharedMaterial.name, Is.EqualTo("MP_WarmGlow"), "the socket glow's language, not an opaque amber slab");
            Assert.That(highlight.GetComponent<MeshFilter>().sharedMesh.name, Is.EqualTo("Quad"));
            Assert.That(highlight.GetComponent<Collider>(), Is.Null, "the halo must not catch the pointer");

            var body = prefab.transform.Find("Phase15_CardMeshRoot/Phase15_CardBody") ?? FindDeep(prefab.transform, "Phase15_CardBody");
            Assert.That(body, Is.Not.Null, "control: the card body");
            var bodyBottom = prefab.transform.InverseTransformPoint(body.position).y - body.lossyScale.y / 2f;
            var haloY = prefab.transform.InverseTransformPoint(highlight.transform.position).y;
            Assert.That(haloY, Is.LessThan(bodyBottom), "the halo sits under the card, so only its edges show");
            Assert.That(Vector3.Dot(highlight.transform.forward, prefab.transform.up), Is.LessThan(-0.99f),
                "the quad lies flat, facing up");
        }

        private static ParticleSystem CenserChild(string name)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var censer = GameObject.Find("MP_MenuStage").transform.Find("MP_Censer");
            Assert.That(censer, Is.Not.Null, "control: the censer");
            var child = censer.Find(name);
            Assert.That(child, Is.Not.Null, $"MP_Censer/{name}");
            return child.GetComponent<ParticleSystem>();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }
    }
}

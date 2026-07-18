using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase12CardFirstRevealTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string Phase11ReviewFolder = "Docs/VisualReview/Phase11";

        [Test]
        public void Phase12CardRevealTypesExist()
        {
            var bootstrapperType = System.Type.GetType("TarotUnity.Editor.Phase12CardRevealBootstrapper, Assembly-CSharp-Editor");
            Assert.That(bootstrapperType, Is.Not.Null);

            Assert.That(GetPublicStaticStringField(bootstrapperType, "CardPrefabPath"), Is.EqualTo(CardPrefabPath));
            Assert.That(GetPublicStaticStringField(bootstrapperType, "ReadingRoomScenePath"), Is.EqualTo(ReadingRoomScenePath));
            Assert.That(GetPublicStaticStringField(bootstrapperType, "ResultScenePath"), Is.EqualTo(ResultScenePath));
        }

        [Test]
        public void CardPrefabHasFaceArtworkPlaceholder()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var card = prefab.GetComponent<CardView>();
            Assert.That(card, Is.Not.Null);

            var serializedCard = new SerializedObject(card);
            var faceArtworkRenderer = serializedCard.FindProperty("faceArtworkRenderer");
            Assert.That(faceArtworkRenderer, Is.Not.Null);
            Assert.That(faceArtworkRenderer.objectReferenceValue, Is.Not.Null);

            Assert.That(card.transform.Find("Front/Phase12_FaceArtworkFrame"), Is.Not.Null);
            Assert.That(card.transform.Find("Front/Phase12_FaceArtworkPlaceholder"), Is.Not.Null);

            var artworkLabel = card.transform
                .Find("Front/Phase12_FaceArtworkPlaceholder/Phase12_FaceArtworkLabel")
                ?.GetComponent<TextMesh>();
            Assert.That(artworkLabel, Is.Not.Null);
            Assert.That(artworkLabel.text, Does.Contain("Tarot Art"));
        }

        [Test]
        public void ReadingRoomHasCardFirstRevealStage()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            // Phase 38 deleted the flat reveal stage planes; the focused light
            // remains part of the reveal rhythm.
            Assert.That(GameObject.Find("Phase12_CardRevealStage"), Is.Null);
            Assert.That(GameObject.Find("Phase12_RevealBackdrop"), Is.Null);
            Assert.That(GameObject.Find("Phase12_FocusedCardLight"), Is.Not.Null);

            var canvas = GameObject.Find("ReadingRoomCanvas");
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase12_CardFocusVignette"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase12_RevealInstruction"), Is.Not.Null);
        }

        [Test]
        public void ResultSceneHasCardShowcase()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var canvas = GameObject.Find("ResultCanvas");
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase12_ResultCardShowcase"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase12_ResultCardPlaceholder"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase12_ResultCardArtworkSlot"), Is.Not.Null);

            // Phase 29 reparents OverallText into the scroll Content; resolve it
            // recursively and assert layout-driven width instead of sizeDelta.
            var overall = FindDescendantText(canvas.transform, "OverallText");
            Assert.That(overall, Is.Not.Null);

            var overallRect = overall.GetComponent<RectTransform>();
            // Width is layout-driven by the parent VerticalLayoutGroup; rebuild Content.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)overallRect.parent);
            Assert.That(overallRect.rect.width, Is.GreaterThanOrEqualTo(660f));
        }

        private static TMP_Text FindDescendantText(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        [Test]
        public void Phase12DoesNotBreakPhase11ScreenshotReviewArtifacts()
        {
            AssertReviewArtifact("MainMenu.png");
            AssertReviewArtifact("ReadingRoom.png");
            AssertReviewArtifact("Result.png");
            AssertReviewArtifact("phase11_visual_review_manifest.json");
        }

        private static string GetPublicStaticStringField(System.Type type, string fieldName)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"Missing public static field {fieldName}");
            Assert.That(field.FieldType, Is.EqualTo(typeof(string)), $"Field {fieldName} should be a string constant.");
            Assert.That(field.IsLiteral, Is.True, $"Field {fieldName} should be const.");
            Assert.That(field.IsInitOnly, Is.False, $"Field {fieldName} should be const, not readonly.");
            return field.GetRawConstantValue() as string;
        }

        private static void AssertReviewArtifact(string fileName)
        {
            var path = Path.Combine(Phase11ReviewFolder, fileName);
            Assert.That(File.Exists(path), Is.True, $"Missing Phase 11 review artifact {path}");
        }
    }
}

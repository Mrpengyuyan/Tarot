using System.Reflection;
using NUnit.Framework;
using TarotUnity.Core;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase5ArtUiPolishTests
    {
        [Test]
        public void RuntimePolishTypesExist()
        {
            Assert.That(typeof(TarotUiTheme), Is.Not.Null);
            Assert.That(typeof(CardPresentationPolish), Is.Not.Null);
        }

        [Test]
        public void MainMenuReadingRoomAndResultHaveUiTheme()
        {
            AssertSceneHasTheme("Assets/Scenes/MainMenu.unity", "MainMenuCanvas");
            AssertSceneHasTheme("Assets/Scenes/ReadingRoom.unity", "ReadingRoomCanvas");
            AssertSceneHasTheme("Assets/Scenes/Result.unity", "ResultCanvas");
        }

        [Test]
        public void VerticalSliceScenesUseInputSystemUiModule()
        {
            AssertSceneUsesInputSystemUiModule("Assets/Scenes/MainMenu.unity");
            AssertSceneUsesInputSystemUiModule("Assets/Scenes/ReadingRoom.unity");
            AssertSceneUsesInputSystemUiModule("Assets/Scenes/Result.unity");
        }

        [Test]
        public void CardPrefabHasPolishedPresentationElements()
        {
            var card = AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            Assert.That(card, Is.Not.Null);

            var polish = card.GetComponent<CardPresentationPolish>();
            Assert.That(polish, Is.Not.Null);
            Assert.That(GetObjectReference(polish, "frontFrame"), Is.Not.Null);
            Assert.That(GetObjectReference(polish, "innerGlow"), Is.Not.Null);

            Assert.That(card.transform.Find("Front/FrontFrame"), Is.Not.Null);
            // Phase 38 deleted the primitive back sigil/constellation blocks (and
            // cleared the polish component's stale sigil references).
            Assert.That(GetObjectReference(polish, "backSigil"), Is.Null);
            Assert.That(card.transform.Find("Back/BackSigil"), Is.Null);
            Assert.That(card.transform.Find("Back/BackConstellation"), Is.Null);
        }

        [Test]
        public void AudioManagerHasProceduralCueFallback()
        {
            var owner = new GameObject("AudioManagerTest");
            try
            {
                var audioManager = owner.AddComponent<AudioManager>();
                Assert.That(audioManager.ProceduralCuesEnabled, Is.True);
                Assert.That(audioManager.GetProceduralCueCountForTesting(), Is.GreaterThanOrEqualTo(5));

                audioManager.PlayCue(PresentationCueId.CardFlipped);
                Assert.That(audioManager.LastCue, Is.EqualTo(PresentationCueId.CardFlipped));
                Assert.That(audioManager.PlayedCueCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ResultSceneTextSupportsChineseReadingComfort()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");

            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            Assert.That(presenter, Is.Not.Null);

            var overallText = GetTextReference(presenter, "overallText");
            var cardAnalysisText = GetTextReference(presenter, "cardAnalysisText");
            var adviceText = GetTextReference(presenter, "adviceText");

            Assert.That(overallText.fontSize, Is.GreaterThanOrEqualTo(19));
            Assert.That(overallText.lineSpacing, Is.GreaterThanOrEqualTo(1.12f));
            Assert.That(cardAnalysisText.lineSpacing, Is.GreaterThanOrEqualTo(1.16f));
            Assert.That(cardAnalysisText.verticalOverflow, Is.EqualTo(VerticalWrapMode.Overflow));
            Assert.That(adviceText.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
        }

        [Test]
        public void ReadingRoomSkipsBackendSpreadProbeWithoutAuthenticatedFallbackSession()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");

            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var apiClient = Object.FindFirstObjectByType<ApiClient>();
            Assert.That(room, Is.Not.Null);
            Assert.That(apiClient, Is.Not.Null);
            Assert.That(apiClient.HasSession, Is.False);

            Assert.That(InvokeShouldLoadBackendSpreads(room), Is.False);

            apiClient.SetAccessToken("test-access-token");
            Assert.That(InvokeShouldLoadBackendSpreads(room), Is.True);

            apiClient.ClearSession();
            SetEnum(room, "backendMode", (int)BackendIntegrationMode.BackendOnly);
            Assert.That(InvokeShouldLoadBackendSpreads(room), Is.True);
        }

        private static void AssertSceneHasTheme(string scenePath, string expectedCanvasName)
        {
            EditorSceneManager.OpenScene(scenePath);

            var theme = Object.FindFirstObjectByType<TarotUiTheme>();
            Assert.That(theme, Is.Not.Null, $"{scenePath} is missing TarotUiTheme");
            Assert.That(theme.ThemeName, Is.EqualTo("Moonlit Tarot"));
            Assert.That(theme.GetComponentInParent<Canvas>(), Is.Not.Null);
            Assert.That(theme.GetComponentInParent<Canvas>().name, Is.EqualTo(expectedCanvasName));
            Assert.That(GetFloat(theme, "bodyLineSpacing"), Is.GreaterThanOrEqualTo(1.12f));
        }

        private static void AssertSceneUsesInputSystemUiModule(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath);

            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null, $"{scenePath} is missing EventSystem");
            Assert.That(eventSystem.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
            Assert.That(eventSystem.GetComponent<StandaloneInputModule>(), Is.Null);
        }

        private static Object GetObjectReference(Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName} on {target.GetType().Name}");
            return property.objectReferenceValue;
        }

        private static Text GetTextReference(Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName} on {target.GetType().Name}");
            return property.objectReferenceValue as Text;
        }

        private static float GetFloat(Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName} on {target.GetType().Name}");
            return property.floatValue;
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName} on {target.GetType().Name}");
            property.enumValueIndex = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool InvokeShouldLoadBackendSpreads(ReadingRoomController room)
        {
            var method = typeof(ReadingRoomController).GetMethod(
                "ShouldLoadBackendSpreads",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(room, null);
        }
    }
}

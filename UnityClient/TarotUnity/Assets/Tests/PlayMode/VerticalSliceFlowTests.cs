using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TMPro;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class VerticalSliceFlowTests
    {
        private const string SmokeQuestion = "Phase 2 smoke test question";

        [UnityTest]
        public IEnumerator MainMenuToResultVerticalSliceRuns()
        {
            ReadingSessionStore.Clear();

            yield return LoadScene("MainMenu");
            yield return WaitForScene("MainMenu");

            var menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.That(menu, Is.Not.Null);

            GetField<Button>(menu, "startReadingButton").onClick.Invoke();
            yield return WaitForScene("ReadingRoom");

            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            Assert.That(room, Is.Not.Null);
            Assert.That(flow, Is.Not.Null);
            Assert.That(deck, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<CameraChoreographyController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<RitualFeedbackController>(), Is.Not.Null);

            GetField<Button>(room, "oneCardButton").onClick.Invoke();
            // Phase 50: the question field is a TMP_InputField now (same .text API).
            GetField<TMP_InputField>(room, "questionInput").text = SmokeQuestion;
            GetField<Button>(room, "drawButton").onClick.Invoke();

            yield return WaitUntil(() => deck.ActiveCards.Count == 1, "Expected one dealt card.");
            yield return WaitUntil(
                () => flow.State == ReadingFlowState.WaitingForFlip,
                $"Expected flow to wait for card flips, but was {flow.State}.");

            var card = deck.ActiveCards[0];
            Assert.That(card, Is.Not.Null);

            var clickHandler = card.GetComponent<CardClickHandler>();
            Assert.That(clickHandler, Is.Not.Null);

            clickHandler.OnPointerClick(new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
            });

            yield return WaitUntil(
                () => flow.State == ReadingFlowState.ResultReady,
                $"Expected flow to reach ResultReady, but was {flow.State}.");
            Assert.That(ReadingSessionStore.HasCurrent, Is.True);
            Assert.That(ReadingSessionStore.Current.question, Is.EqualTo(SmokeQuestion));

            var revealButton = GetField<Button>(room, "revealResultButton");
            Assert.That(revealButton.gameObject.activeSelf, Is.True);
            revealButton.onClick.Invoke();

            yield return WaitForScene("Result");
            Assert.That(Object.FindFirstObjectByType<ResultSceneController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<ResultPanelPresenter>(), Is.Not.Null);

            var revealDirector = Object.FindFirstObjectByType<ResultRevealDirector>();
            Assert.That(revealDirector, Is.Not.Null);
            yield return WaitUntil(
                () => revealDirector.IsRevealComplete,
                "Expected staged result reveal to complete.");
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
            yield return null;
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == sceneName,
                $"Expected active scene {sceneName}, but was {SceneManager.GetActiveScene().name}.");
        }

        private static IEnumerator WaitUntil(System.Func<bool> predicate, string failureMessage)
        {
            var timeoutAt = Time.realtimeSinceStartup + 5f;
            while (!predicate() && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(predicate(), Is.True, failureMessage);
        }

        private static T GetField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}");

            var value = field.GetValue(target) as T;
            Assert.That(value, Is.Not.Null, $"Field {fieldName} on {target.GetType().Name} is null");
            return value;
        }
    }
}

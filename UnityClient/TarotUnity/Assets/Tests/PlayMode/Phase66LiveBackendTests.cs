using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TarotUnity.Core;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 66 live acceptance (spec 8.5) against the real FastAPI backend and the
    /// user's DeepSeek key. Ignored unless TAROT_LIVE_BACKEND=1. It is driven by
    /// live.sh, which stops or restarts the backend when a test writes a marker file.
    /// </summary>
    public sealed class Phase66LiveBackendTests
    {
        private const float InterpretationWaitSeconds = 360f;

        private static string MarkerDir => Environment.GetEnvironmentVariable("TAROT_LIVE_MARKERS");
        private static string ReportPath => Environment.GetEnvironmentVariable("TAROT_LIVE_REPORT");

        [SetUp]
        public void RequireLiveBackend()
        {
            if (Environment.GetEnvironmentVariable("TAROT_LIVE_BACKEND") != "1")
            {
                Assert.Ignore("Live backend run only: set TAROT_LIVE_BACKEND=1 (plan 2, Task 9).");
            }
        }

        [UnityTearDown]
        public IEnumerator DestroyBootObjects()
        {
            foreach (var bootstrap in Object.FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None))
            {
                Object.Destroy(bootstrap.gameObject);
            }

            ApiClient.ClearShared();
            ReadingSessionStore.Clear();
            yield return null;
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator LiveDrillBackendDownBeforeTheDraw()
        {
            yield return BootToOnlineMenu();
            yield return EnterReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            yield return WaitForBackendSpreads(room);

            yield return Handshake("drill1-stop-backend", "drill1-backend-stopped", 120f);

            GetField<Button>(room, "oneCardButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitUntil(() => deck.ActiveCards.Count == 1, 90f, "expected an offline card");

            var release = GetField<TMP_Text>(room, "releaseStatusText").text;
            Report($"drill1 source={ReadingSessionStore.Current.source} release={release}");
            Assert.That(ReadingSessionStore.Current.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(release, Is.EqualTo(ReleaseUxCopy.OfflineBecauseNetwork));

            yield return Handshake("drill1-start-backend", "drill1-backend-started", 180f);
        }

        [UnityTest]
        [Timeout(1500000)]
        public IEnumerator LiveDrillBackendLostWhileGenerating()
        {
            yield return BootToOnlineMenu();
            yield return EnterReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "threeCardButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitUntil(() => deck.ActiveCards.Count >= 1, 60f, "expected the first card");

            var session = ReadingSessionStore.Current;
            Assert.That(session.source, Is.EqualTo(ReadingSource.Online), "control: the drill needs an online reading");
            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending), "control: the AI is still generating");
            yield return Handshake("drill2-stop-backend", "drill2-backend-stopped", 120f);

            yield return FlipAllCardsAndReveal(room, 3);
            yield return WaitUntil(() => session.interpretationState == InterpretationState.Failed, 120f,
                "expected the lost connection to surface (Ready here means the AI beat the drill - rerun it)");
            Report($"drill2 failure={session.failureMessage} canRetry={session.canRetry}");
            Assert.That(session.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationConnectionLost));
            Assert.That(session.canRetry, Is.True);

            yield return Handshake("drill2-start-backend", "drill2-backend-started", 180f);

            var controller = Object.FindFirstObjectByType<ResultSceneController>();
            var retry = GetField<Button>(controller, "retryInterpretationButton");
            var startedAt = Time.realtimeSinceStartup;
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                Assert.That(retry.gameObject.activeInHierarchy, Is.True, $"retry button hidden before attempt {attempt}");
                retry.onClick.Invoke();
                yield return WaitUntil(() => session.interpretationState != InterpretationState.Pending,
                    InterpretationWaitSeconds, $"retry {attempt} never finished");
                Report($"drill2 retry={attempt} state={session.interpretationState} failure={session.failureMessage} " +
                    $"elapsed={Time.realtimeSinceStartup - startedAt:F0}s");
                if (session.interpretationState == InterpretationState.Ready || !session.canRetry)
                {
                    break;
                }
            }

            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Ready), session.failureMessage);
            Assert.That(session.modelUsed, Is.Not.EqualTo("mock_ai"));
        }

        [UnityTest]
        [Timeout(1500000)]
        public IEnumerator LiveReadingsForOneThreeAndTenCards()
        {
            yield return BootToOnlineMenu();

            foreach (var (buttonField, cardCount) in new[] { ("oneCardButton", 1), ("threeCardButton", 3), ("celticCrossButton", 10) })
            {
                yield return EnterReadingRoom();
                var room = Object.FindFirstObjectByType<ReadingRoomController>();
                yield return WaitForBackendSpreads(room);

                GetField<Button>(room, buttonField).onClick.Invoke();
                GetField<TMP_InputField>(room, "questionInput").text = $"联调 {cardCount} 张：此刻我最需要留意什么？";
                var clickedAt = Time.realtimeSinceStartup;
                GetField<Button>(room, "drawButton").onClick.Invoke();

                var deck = Object.FindFirstObjectByType<DeckController>();
                yield return WaitUntil(() => deck.ActiveCards.Count >= 1, 60f, "expected the first card to be dealt");
                var firstCardSeconds = Time.realtimeSinceStartup - clickedAt;

                var session = ReadingSessionStore.Current;
                var release = GetField<TMP_Text>(room, "releaseStatusText").text;
                Assert.That(session, Is.Not.Null);
                Assert.That(session.source, Is.EqualTo(ReadingSource.Online), $"{cardCount}-card reading went offline: {release}");
                Assert.That(session.cardDraws, Has.Length.EqualTo(cardCount));
                Assert.That(Regex.IsMatch(release, "[A-Za-z]"), Is.False, $"English status in the online flow: {release}");

                yield return FlipAllCardsAndReveal(room, cardCount);
                var resultAt = Time.realtimeSinceStartup;
                yield return WaitUntil(() => session.interpretationState != InterpretationState.Pending,
                    InterpretationWaitSeconds, "expected the interpretation to finish");
                var textSeconds = Time.realtimeSinceStartup - resultAt;

                Report($"cards={cardCount} prediction={session.predictionId} state={session.interpretationState} " +
                    $"model={session.modelUsed} firstCard={firstCardSeconds:F1}s resultToText={textSeconds:F1}s " +
                    $"failure={session.failureMessage}");
                Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Ready), session.failureMessage);
                Assert.That(session.modelUsed, Is.Not.EqualTo("mock_ai"), "the backend answered with mock text - check DEEPSEEK_API_KEY");
                Assert.That(firstCardSeconds, Is.LessThan(15f), "dealing must not wait for the AI");

                yield return BackToMenu();
            }
        }

        private static IEnumerator BootToOnlineMenu()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu", 30f, "expected Boot to open the menu");

            var sessionBootstrap = Object.FindFirstObjectByType<BackendSessionBootstrap>();
            Assert.That(sessionBootstrap, Is.Not.Null, "control: Boot created the session bootstrap");
            yield return WaitUntil(
                () => sessionBootstrap.Status == BackendSessionStatus.Online || sessionBootstrap.Status == BackendSessionStatus.Offline,
                30f,
                "expected the guest session to settle");
            Assert.That(sessionBootstrap.Status, Is.EqualTo(BackendSessionStatus.Online), $"guest session failed: {sessionBootstrap.LastError}");
            Assert.That(ApiClient.Shared, Is.Not.Null, "control: Boot shared its ApiClient");
            Assert.That(ApiClient.Shared.HasAccessToken, Is.True);
            Assert.That(InterpretationPoller.Instance, Is.Not.Null, "control: Boot hosts the poller");
        }

        private static IEnumerator EnterReadingRoom()
        {
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.That(menu, Is.Not.Null, "expected the main menu");
            GetField<Button>(menu, "startReadingButton").onClick.Invoke();
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "ReadingRoom" && Object.FindFirstObjectByType<ReadingRoomController>() != null,
                30f,
                "expected the reading room");
            yield return null;
        }

        private static IEnumerator WaitForBackendSpreads(ReadingRoomController room)
        {
            var field = typeof(ReadingRoomController).GetField("backendSpreads", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "control: backendSpreads field exists");
            yield return WaitUntil(() => field.GetValue(room) != null, 30f, "expected backend spreads to load");
        }

        private static IEnumerator FlipAllCardsAndReveal(ReadingRoomController room, int cardCount)
        {
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitUntil(
                () => deck.ActiveCards.Count == cardCount && flow.State == ReadingFlowState.WaitingForFlip,
                60f,
                "expected every card dealt");

            for (var i = 0; i < cardCount; i++)
            {
                var card = deck.ActiveCards[i];
                card.GetComponent<CardClickHandler>().OnPointerClick(new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left,
                });
                yield return WaitUntil(() => card.IsFaceUp, 10f, $"expected card {i + 1} to flip");
            }

            yield return WaitUntil(() => flow.State == ReadingFlowState.ResultReady, 20f, "expected ResultReady");
            var reveal = GetField<Button>(room, "revealResultButton");
            yield return WaitUntil(() => reveal.gameObject.activeInHierarchy, 10f, "expected the reveal button");
            reveal.onClick.Invoke();
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "Result" && Object.FindFirstObjectByType<ResultSceneController>() != null,
                30f,
                "expected the Result screen");
            yield return null;
        }

        private static IEnumerator BackToMenu()
        {
            var controller = Object.FindFirstObjectByType<ResultSceneController>();
            GetField<Button>(controller, "backToMenuButton").onClick.Invoke();
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "MainMenu" && Object.FindFirstObjectByType<MainMenuController>() != null,
                30f,
                "expected the main menu");
            yield return null;
        }

        private static IEnumerator Handshake(string request, string response, float seconds)
        {
            Assert.That(string.IsNullOrEmpty(MarkerDir), Is.False, "TAROT_LIVE_MARKERS is not set");
            File.WriteAllText(Path.Combine(MarkerDir, request), DateTime.UtcNow.ToString("O"));
            var responsePath = Path.Combine(MarkerDir, response);
            yield return WaitUntil(() => File.Exists(responsePath), seconds, $"live.sh did not answer {request}");
        }

        private static void Report(string line)
        {
            Debug.Log("Phase66Live: " + line);
            if (!string.IsNullOrEmpty(ReportPath))
            {
                File.AppendAllText(ReportPath, line + Environment.NewLine);
            }
        }

        private static IEnumerator WaitUntil(Func<bool> predicate, float seconds, string message)
        {
            var timeoutAt = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(predicate(), Is.True, message);
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

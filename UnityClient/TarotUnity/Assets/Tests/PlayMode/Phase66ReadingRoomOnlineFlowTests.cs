using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 66: the reading room against a scripted backend. The table deals the
    /// backend's cards while the interpretation is still generating, and every failed
    /// start falls back to an offline reading that shows the spec 7.3 copy.
    /// </summary>
    public sealed class Phase66ReadingRoomOnlineFlowTests
    {
        private MockTarotBackend server;
        private GameObject boot;
        private ApiClient client;
        private InterpretationPoller poller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ReadingSessionStore.Clear();
            server = MockTarotBackend.Start();
            boot = new GameObject("Phase66_RoomTestBoot");
            Object.DontDestroyOnLoad(boot);
            client = boot.AddComponent<ApiClient>();
            client.BaseUrl = server.ApiBaseUrl;
            client.SetAccessToken("test-access-token");
            ApiClient.SetShared(client);
            poller = boot.AddComponent<InterpretationPoller>();
            poller.Configure(client, 0.05f, 30f);
            server.Script("GET", "/api/v1/spreads/", MockTarotBackend.Json(200,
                MockTarotJson.Spreads((11, "单牌抽取", 1), (12, "过去现在未来", 3), (13, "爱情牌阵", 5))));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ApiClient.ClearShared();
            Object.Destroy(boot);
            ReadingSessionStore.Clear();
            yield return null;
            server.Dispose();
        }

        [UnityTest]
        public IEnumerator OnlineReadingDealsBackendCardsBeforeTheInterpretationIsReady()
        {
            server.Script("POST", "/api/v1/records/", MockTarotBackend.Json(200, MockTarotJson.Record(701, 12)));
            server.Script("POST", "/api/v1/records/701/draw", MockTarotBackend.Json(200, MockTarotJson.Draw(701, 3)));
            server.Script("GET", "/api/v1/records/701/cards", MockTarotBackend.Json(200, MockTarotJson.Cards(701, 3)));
            server.Script("POST", "/api/v1/records/701/interpret/async", MockTarotBackend.Json(202, MockTarotJson.Accepted(701)));
            server.Script("GET", "/api/v1/records/701",
                MockTarotBackend.Json(200, MockTarotJson.Detail(701, "processing", 3, null)));

            yield return LoadReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "threeCardButton").onClick.Invoke();
            GetField<TMP_InputField>(room, "questionInput").text = string.Empty;
            GetField<Button>(room, "drawButton").onClick.Invoke();

            yield return WaitUntil(
                () => deck.ActiveCards.Count == 3 && flow.State == ReadingFlowState.WaitingForFlip,
                20f,
                "expected three dealt cards waiting for flips");

            var session = ReadingSessionStore.Current;
            Assert.That(session.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(session.predictionId, Is.EqualTo(701));
            Assert.That(session.spreadId, Is.EqualTo(12));
            Assert.That(session.spreadName, Is.EqualTo("过去现在未来"));
            Assert.That(session.question, Is.EqualTo(ReleaseUxCopy.DefaultQuestion));
            Assert.That(server.LastBody("POST", "/api/v1/records/"), Does.Contain("\"spread_type_id\":12"));
            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending),
                "the table dealt before the interpretation existed");
            Assert.That(GetField<TMP_Text>(room, "flowStatusText").text, Is.EqualTo(ReleaseUxCopy.FlowFlipPrompt));
            Assert.That(GetField<TMP_Text>(room, "releaseStatusText").text, Is.EqualTo(ReleaseUxCopy.InterpretationGenerating));

            server.Script("GET", "/api/v1/records/701", MockTarotBackend.Json(200,
                MockTarotJson.Detail(701, "completed", 3, MockTarotJson.Interpretation(701, "deepseek-chat"))));
            yield return WaitUntil(() => session.interpretationState == InterpretationState.Ready, 10f,
                "expected the interpretation to arrive");

            Assert.That(session.modelUsed, Is.EqualTo("deepseek-chat"));
            Assert.That(GetField<TMP_Text>(room, "releaseStatusText").text, Is.EqualTo(ReleaseUxCopy.InterpretationReadyHint));
        }

        [UnityTest]
        public IEnumerator GuestQuotaOnStartFallsBackToAnOfflineReading()
        {
            server.Script("POST", "/api/v1/records/",
                MockTarotBackend.Json(429, MockTarotJson.GuestLimitDetail).WithHeader("Retry-After", "5400"));

            yield return LoadReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "oneCardButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            yield return WaitUntil(() => deck.ActiveCards.Count == 1, 20f, "expected one dealt card");

            var session = ReadingSessionStore.Current;
            Assert.That(server.Count("POST", "/api/v1/records/"), Is.EqualTo(1), "control: the online start was attempted");
            Assert.That(session.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(session.warning, Is.EqualTo(ReleaseUxCopy.OfflineWarning));
            Assert.That(GetField<TMP_Text>(room, "releaseStatusText").text,
                Is.EqualTo("今天的访客占卜次数已用完，约 2 小时后恢复。这一局使用离线解读。"));
            foreach (var entry in server.RequestLog)
            {
                Assert.That(entry, Does.Not.Contain("/draw"));
                Assert.That(entry, Does.Not.Contain("interpret"));
            }
        }

        [UnityTest]
        public IEnumerator SpreadMissingOnTheBackendStaysOfflineWithoutCreatingARecord()
        {
            yield return LoadReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "celticCrossButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            yield return WaitUntil(() => deck.ActiveCards.Count == 10, 30f, "expected ten dealt cards");

            Assert.That(ReadingSessionStore.Current.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(ReadingSessionStore.Current.cardDraws, Has.Length.EqualTo(10));
            Assert.That(GetField<TMP_Text>(room, "releaseStatusText").text, Is.EqualTo(ReleaseUxCopy.OfflineBecauseSpread));
            Assert.That(server.Count("POST", "/api/v1/records/"), Is.EqualTo(0),
                "a ten-card reading must not be sent under another spread's id");
        }

        [UnityTest]
        public IEnumerator CardCountMismatchFromTheBackendGoesOffline()
        {
            server.Script("POST", "/api/v1/records/", MockTarotBackend.Json(200, MockTarotJson.Record(702, 12)));
            server.Script("POST", "/api/v1/records/702/draw", MockTarotBackend.Json(200, MockTarotJson.Draw(702, 5)));
            server.Script("GET", "/api/v1/records/702/cards", MockTarotBackend.Json(200, MockTarotJson.Cards(702, 5)));

            yield return LoadReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "threeCardButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            yield return WaitUntil(() => deck.ActiveCards.Count == 3, 20f, "expected three dealt cards");

            Assert.That(server.Count("GET", "/api/v1/records/702/cards"), Is.EqualTo(1), "control: the backend dealt five");
            Assert.That(ReadingSessionStore.Current.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(ReadingSessionStore.Current.cardDraws, Has.Length.EqualTo(3));
            Assert.That(GetField<TMP_Text>(room, "releaseStatusText").text, Is.EqualTo(ReleaseUxCopy.OfflineBecauseUnavailable));
            Assert.That(server.Count("POST", "/api/v1/records/702/interpret/async"), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator UnreachableSpreadListShowsTheConnectionCopyWithoutCreatingARecord()
        {
            server.Replace("GET", "/api/v1/spreads/", MockTarotBackend.Json(503, "{\"detail\":\"Service Unavailable\"}"));

            yield return LoadReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitUntil(() => server.Count("GET", "/api/v1/spreads/") >= 1, 10f,
                "control: the room asked for the spread list");

            GetField<Button>(room, "oneCardButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            yield return WaitUntil(() => deck.ActiveCards.Count == 1, 20f, "expected one dealt card");

            Assert.That(ReadingSessionStore.Current.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(GetField<TMP_Text>(room, "releaseStatusText").text, Is.EqualTo(ReleaseUxCopy.OfflineBecauseNetwork));
            Assert.That(server.Count("POST", "/api/v1/records/"), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator EverySpreadButtonStaysLockedWhileTheDealtCardsWait()
        {
            yield return LoadReadingRoom();
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return WaitForBackendSpreads(room);

            GetField<Button>(room, "oneCardButton").onClick.Invoke();
            GetField<Button>(room, "drawButton").onClick.Invoke();
            yield return WaitUntil(
                () => deck.ActiveCards.Count == 1 && flow.State == ReadingFlowState.WaitingForFlip,
                20f,
                "expected one dealt card waiting for its flip");

            Assert.That(GetField<Button>(room, "oneCardButton").interactable, Is.False, "control: the existing lock works");
            Assert.That(GetField<Button>(room, "threeCardButton").interactable, Is.False);
            Assert.That(GetField<Button>(room, "celticCrossButton").interactable, Is.False,
                "凯尔特十字 must not re-open spread selection over dealt cards");
        }

        private static IEnumerator LoadReadingRoom()
        {
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            yield return null;
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "ReadingRoom"
                    && Object.FindFirstObjectByType<ReadingRoomController>() != null,
                10f,
                "expected the ReadingRoom scene");
        }

        private static IEnumerator WaitForBackendSpreads(ReadingRoomController room)
        {
            var field = typeof(ReadingRoomController).GetField("backendSpreads", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "control: backendSpreads field exists");
            yield return WaitUntil(() => field.GetValue(room) != null, 10f, "expected backend spreads to load");
        }

        private static IEnumerator WaitUntil(System.Func<bool> predicate, float seconds, string message)
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

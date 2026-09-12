using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 66: the persistent InterpretationPoller against a scripted backend (spec
    /// 8.4). Poll delays are scaled by 0.05, so the 2/2/3/3/5 s schedule runs in
    /// tenths of a second.
    /// </summary>
    public sealed class Phase66InterpretationPollerTests
    {
        private const int PredictionId = 601;
        private const string AsyncPath = "/api/v1/records/601/interpret/async";
        private const string DetailPath = "/api/v1/records/601";
        private const string RefreshPath = "/api/v1/refresh";

        private MockTarotBackend server;
        private GameObject boot;
        private ApiClient client;
        private InterpretationPoller poller;
        private List<InterpretationState> observed;

        [SetUp]
        public void SetUp()
        {
            ApiClient.ClearShared();
            server = MockTarotBackend.Start();
            boot = new GameObject("Phase66_PollerBoot");
            client = boot.AddComponent<ApiClient>();
            client.BaseUrl = server.ApiBaseUrl;
            client.SetAccessToken("test-access-token");
            poller = boot.AddComponent<InterpretationPoller>();
            poller.Configure(client, 0.05f, 30f);
            observed = new List<InterpretationState>();
            poller.StateChanged += changed => observed.Add(changed.interpretationState);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(boot);
            server.Dispose();
        }

        [UnityTest]
        public IEnumerator ProcessingTwiceThenCompletedBecomesReady()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath, Processing(), Processing(), Completed("deepseek-chat"));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Ready, 10f, "expected Ready");

            Assert.That(snapshot.predictionId, Is.EqualTo(PredictionId));
            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(snapshot.modelUsed, Is.EqualTo("deepseek-chat"));
            Assert.That(snapshot.summary, Is.EqualTo("概要来自测试桩。"));
            Assert.That(server.Count("POST", AsyncPath), Is.EqualTo(1));
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(3));
            Assert.That(observed, Is.EqualTo(new[] { InterpretationState.Pending, InterpretationState.Ready }));
            Assert.That(poller.IsPolling, Is.False);
        }

        [UnityTest]
        public IEnumerator GenerationSlowerThanARequestTimeoutStaysOnline()
        {
            var unscaled = 0f;
            for (var i = 0; i < 6; i++)
            {
                unscaled += InterpretationPoller.PollDelaySeconds(i);
            }

            Assert.That(unscaled, Is.GreaterThan(15f), "control: at real speed these waits exceed the 15 s request timeout");

            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath,
                Processing(), Processing(), Processing(), Processing(), Processing(), Processing(), Completed("deepseek-chat"));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState != InterpretationState.Pending, 15f, "expected the poll to finish");

            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(7));
        }

        [UnityTest]
        public IEnumerator FailedThenRetrySucceeds()
        {
            server.Script("POST", AsyncPath, Accepted(), Accepted());
            server.Script("GET", DetailPath, Failed(), Completed("deepseek-chat"));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 10f, "expected Failed");
            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationBackendFailed));
            Assert.That(snapshot.canRetry, Is.True);

            poller.Retry();
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Ready, 10f, "expected Ready after Retry");

            Assert.That(server.Count("POST", AsyncPath), Is.EqualTo(2));
            Assert.That(observed, Is.EqualTo(new[]
            {
                InterpretationState.Pending, InterpretationState.Failed, InterpretationState.Pending, InterpretationState.Ready,
            }));
        }

        [UnityTest]
        public IEnumerator ExhaustedAttemptsCannotBeRetried()
        {
            server.Script("POST", AsyncPath, MockTarotBackend.Json(429, MockTarotJson.AttemptsExhaustedDetail));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 10f, "expected Failed");

            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationAttemptsExhausted));
            Assert.That(snapshot.canRetry, Is.False);
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator MissingRecordCannotBeInterpreted()
        {
            server.Script("POST", AsyncPath, MockTarotBackend.Json(404, MockTarotJson.RecordNotFoundDetail));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 10f, "expected Failed");

            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationUnavailable));
            Assert.That(snapshot.canRetry, Is.False);
        }

        [UnityTest]
        public IEnumerator UnauthorizedPollRefreshesAndKeepsPolling()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath,
                MockTarotBackend.Json(401, "{\"detail\":\"Could not validate credentials\"}"), Processing(), Completed("deepseek-chat"));
            server.Script("POST", RefreshPath, MockTarotBackend.Json(200, MockTarotJson.Token("refreshed-token")));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Ready, 10f, "expected Ready");

            Assert.That(server.Count("POST", RefreshPath), Is.EqualTo(1));
            Assert.That(client.AccessToken, Is.EqualTo("refreshed-token"));
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator UnauthorizedWithFailedRefreshExpiresTheSession()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath, MockTarotBackend.Json(401, "{\"detail\":\"Could not validate credentials\"}"));
            server.Script("POST", RefreshPath, MockTarotBackend.Json(401, "{\"detail\":\"Invalid refresh token\"}"));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 10f, "expected Failed");

            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationSessionExpired));
            Assert.That(snapshot.canRetry, Is.False);
            Assert.That(server.Count("POST", RefreshPath), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ThreeConnectionErrorsInARowLoseTheConnection()
        {
            server.Script("POST", AsyncPath, Accepted());
            // Ruling 7: this mock cannot fake a dropped connection on macOS/Mono; a 503
            // goes through the same consecutive-failure path in the poller.
            server.Script("GET", DetailPath, ServiceUnavailable());
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 15f, "expected Failed");

            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationConnectionLost));
            Assert.That(snapshot.canRetry, Is.True);
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator TwoConnectionErrorsThenRecoveryKeepsPolling()
        {
            server.Script("POST", AsyncPath, Accepted());
            // Ruling 7: 503 stands in for a dropped connection (see ThreeConnectionErrorsInARow...).
            server.Script("GET", DetailPath,
                ServiceUnavailable(), ServiceUnavailable(), Processing(), Completed("deepseek-chat"));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState != InterpretationState.Pending, 15f, "expected the poll to finish");

            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(4));
            Assert.That(observed, Is.EqualTo(new[] { InterpretationState.Pending, InterpretationState.Ready }));
        }

        [UnityTest]
        public IEnumerator PastTheDeadlineTheReadingTimesOut()
        {
            poller.Configure(client, 0.05f, 0.8f);
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath, Processing());
            var snapshot = OnlineSnapshot(1);
            var startedAt = Time.realtimeSinceStartup;

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Failed, 10f, "expected Failed");

            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationTimedOut));
            Assert.That(snapshot.canRetry, Is.True);
            Assert.That(Time.realtimeSinceStartup - startedAt, Is.LessThan(5f));
            Assert.That(server.Count("GET", DetailPath), Is.GreaterThan(0), "control: it polled before timing out");
        }

        [UnityTest]
        public IEnumerator PollingSurvivesTheCallerBeingDestroyed()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath, Processing(), Processing(), Processing(), Completed("deepseek-chat"));
            var snapshot = OnlineSnapshot(1);
            var room = new GameObject("Phase66_ReadingRoomStandIn");
            var roomService = room.AddComponent<BackendReadingService>();
            roomService.StartCoroutine(BeginFromRoom(poller, snapshot));

            yield return WaitUntil(
                () => snapshot.interpretationState == InterpretationState.Pending && server.Count("GET", DetailPath) >= 1,
                10f,
                "expected polling to start");
            Object.Destroy(room);
            yield return null;
            Assert.That(room == null, Is.True, "control: the caller is gone");

            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Ready, 10f,
                "expected Ready after the caller was destroyed");
            Assert.That(observed, Is.EqualTo(new[] { InterpretationState.Pending, InterpretationState.Ready }));
        }

        [UnityTest]
        public IEnumerator UseOfflineStopsPollingAndKeepsTheCards()
        {
            server.Script("POST", AsyncPath, Accepted());
            server.Script("GET", DetailPath, Processing());
            var snapshot = OnlineSnapshot(3);
            var cards = snapshot.cardDraws;

            poller.Begin(snapshot);
            yield return WaitUntil(() => server.Count("GET", DetailPath) >= 1, 10f, "expected polling to start");

            poller.UseOffline();
            var pollsAtSwitch = server.Count("GET", DetailPath);
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(snapshot.cardDraws, Is.SameAs(cards));
            Assert.That(snapshot.warning, Is.EqualTo(ReleaseUxCopy.OfflineWarning));
            Assert.That(server.Count("GET", DetailPath), Is.LessThanOrEqualTo(pollsAtSwitch + 1),
                "at most the request already in flight may still arrive");
            Assert.That(observed, Is.EqualTo(new[] { InterpretationState.Pending, InterpretationState.Ready }));
            Assert.That(poller.IsPolling, Is.False);
        }

        [UnityTest]
        public IEnumerator AlreadyStoredInterpretationCompletesWithoutPolling()
        {
            server.Script("POST", AsyncPath, MockTarotBackend.Json(200, MockTarotJson.Interpretation(PredictionId, "mock_ai")));
            var snapshot = OnlineSnapshot(1);

            poller.Begin(snapshot);
            yield return WaitUntil(() => snapshot.interpretationState == InterpretationState.Ready, 10f, "expected Ready");

            Assert.That(snapshot.modelUsed, Is.EqualTo("mock_ai"));
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(0));
        }

        private static IEnumerator BeginFromRoom(InterpretationPoller target, ReadingSessionSnapshot snapshot)
        {
            yield return null;
            target.Begin(snapshot);
        }

        private static ReadingSessionSnapshot OnlineSnapshot(int cardCount)
        {
            var snapshot = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse
                {
                    id = PredictionId,
                    spread_type_id = 1,
                    question = "此刻我最需要留意什么？",
                    question_type = "general",
                },
                LocalReadingSimulator.CreatePlaceholderDraws(cardCount));
            snapshot.spreadName = "单牌抽取";
            return snapshot;
        }

        private static MockTarotBackend.Reply Accepted()
        {
            return MockTarotBackend.Json(202, MockTarotJson.Accepted(PredictionId));
        }

        private static MockTarotBackend.Reply Processing()
        {
            return MockTarotBackend.Json(200, MockTarotJson.Detail(PredictionId, "processing", 1, null));
        }

        private static MockTarotBackend.Reply Failed()
        {
            return MockTarotBackend.Json(200, MockTarotJson.Detail(PredictionId, "failed", 1, null));
        }

        private static MockTarotBackend.Reply Completed(string model)
        {
            return MockTarotBackend.Json(200, MockTarotJson.Detail(
                PredictionId, "completed", 1, MockTarotJson.Interpretation(PredictionId, model)));
        }

        private static MockTarotBackend.Reply ServiceUnavailable()
        {
            return MockTarotBackend.Json(503, "{\"detail\":\"Service Unavailable\"}");
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
    }
}

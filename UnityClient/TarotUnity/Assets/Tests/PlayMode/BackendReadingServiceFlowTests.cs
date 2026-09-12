using System.Collections;
using NUnit.Framework;
using TarotUnity.Core;
using TarotUnity.Data;
using TarotUnity.Network;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 66: BackendReadingService starts an online reading (record + draw +
    /// cards, no interpretation) and reports structured errors. The refused-connection
    /// and slow tests are instrument controls: they prove the mock really produces a
    /// network error and a timeout before the poller tests rely on either.
    /// </summary>
    public sealed class BackendReadingServiceFlowTests
    {
        private MockTarotBackend server;
        private GameObject owner;
        private ApiClient client;
        private BackendReadingService service;

        [SetUp]
        public void SetUp()
        {
            server = MockTarotBackend.Start();
            owner = new GameObject("BackendReadingServiceFlowTest");
            client = owner.AddComponent<ApiClient>();
            client.BaseUrl = server.ApiBaseUrl;
            client.SetAccessToken("test-access-token");
            ApiClient.SetShared(client);
            service = owner.AddComponent<BackendReadingService>();
        }

        [TearDown]
        public void TearDown()
        {
            ApiClient.ClearShared();
            Object.Destroy(owner);
            server.Dispose();
        }

        [UnityTest]
        public IEnumerator StartReadingCreatesAnOnlinePendingSnapshotWithoutInterpreting()
        {
            server.Script("POST", "/api/v1/records/", MockTarotBackend.Json(200, MockTarotJson.Record(501, 2)));
            server.Script("POST", "/api/v1/records/501/draw", MockTarotBackend.Json(200, MockTarotJson.Draw(501, 3)));
            server.Script("GET", "/api/v1/records/501/cards", MockTarotBackend.Json(200, MockTarotJson.Cards(501, 3)));

            ReadingSessionSnapshot session = null;
            ApiError error = null;
            yield return service.StartReading(Payload(2), value => session = value, value => error = value);

            Assert.That(error, Is.Null, error?.RawMessage);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.predictionId, Is.EqualTo(501));
            Assert.That(session.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending));
            Assert.That(session.cardDraws, Has.Length.EqualTo(3));
            Assert.That(session.cardDraws[0].tarot_card.name_zh, Is.EqualTo("愚者"));
            Assert.That(server.LastBody("POST", "/api/v1/records/"), Does.Contain("\"spread_type_id\":2"));
            Assert.That(server.Count("POST", "/api/v1/records/501/interpret"), Is.EqualTo(0));
            Assert.That(server.Count("POST", "/api/v1/records/501/interpret/async"), Is.EqualTo(0),
                "starting a reading must not ask for the interpretation");
        }

        [UnityTest]
        public IEnumerator GuestQuotaOnCreateReportsRateLimitWithRetryAfter()
        {
            server.Script("POST", "/api/v1/records/",
                MockTarotBackend.Json(429, MockTarotJson.GuestLimitDetail).WithHeader("Retry-After", "5400"));

            ReadingSessionSnapshot session = null;
            ApiError error = null;
            yield return service.StartReading(Payload(1), value => session = value, value => error = value);

            Assert.That(session, Is.Null);
            Assert.That(error, Is.Not.Null);
            Assert.That(error.Kind, Is.EqualTo(ApiErrorKind.RateLimited));
            Assert.That(error.RetryAfterSeconds, Is.EqualTo(5400));
            Assert.That(ReleaseUxCopy.ForStartReadingFailure(error),
                Is.EqualTo("今天的访客占卜次数已用完，约 2 小时后恢复。这一局使用离线解读。"));
            Assert.That(server.RequestLog, Is.EqualTo(new[] { "POST /api/v1/records/" }), "no draw after a refused record");
        }

        [UnityTest]
        public IEnumerator RefusedConnectionSurfacesAsNetworkError()
        {
            server.Script("GET", "/api/v1/records/777",
                MockTarotBackend.Json(200, MockTarotJson.Detail(777, "processing", 1, null)));

            PredictionDetailResponse reachable = null;
            ApiError controlError = null;
            yield return client.FetchRecordDetail(777, value => reachable = value, value => controlError = value);
            Assert.That(controlError, Is.Null, "control: the same client reaches a live backend");
            Assert.That(reachable, Is.Not.Null, "control: the same client reaches a live backend");

            client.BaseUrl = $"http://127.0.0.1:{MockTarotBackend.GetFreePort()}/api/v1";
            PredictionDetailResponse detail = null;
            ApiError error = null;
            yield return client.FetchRecordDetail(777, value => detail = value, value => error = value);

            Assert.That(detail, Is.Null);
            Assert.That(error, Is.Not.Null, "a refused connection must reach onError");
            Assert.That(error.StatusCode, Is.EqualTo(0), error.RawMessage);
            Assert.That(error.Kind, Is.EqualTo(ApiErrorKind.Network), error.RawMessage);
        }

        [UnityTest]
        public IEnumerator SlowResponseSurfacesAsTimeoutError()
        {
            var config = DesktopRuntimeConfig.CreateDefault();
            config.requestTimeoutSeconds = 1;
            client.ApplyRuntimeConfig(config);
            client.BaseUrl = server.ApiBaseUrl;
            server.Script("GET", "/api/v1/records/778", MockTarotBackend.Json(200, "{}").WithDelay(3000));

            ApiError error = null;
            yield return client.FetchRecordDetail(778, _ => { }, value => error = value);

            Assert.That(client.RequestTimeoutSeconds, Is.EqualTo(1), "control: the short timeout was applied");
            Assert.That(error, Is.Not.Null);
            Assert.That(error.Kind, Is.EqualTo(ApiErrorKind.Timeout), error.RawMessage);
        }

        [UnityTest]
        public IEnumerator AsyncInterpretReportsAcceptedThenAlreadyReady()
        {
            server.Script("POST", "/api/v1/records/501/interpret/async",
                MockTarotBackend.Json(202, MockTarotJson.Accepted(501)),
                MockTarotBackend.Json(200, MockTarotJson.Interpretation(501, "deepseek-chat")));

            AsyncInterpretationResult first = null;
            AsyncInterpretationResult second = null;
            ApiError error = null;
            yield return service.RequestInterpretationAsync(501, value => first = value, value => error = value);
            yield return service.RequestInterpretationAsync(501, value => second = value, value => error = value);

            Assert.That(error, Is.Null, error?.RawMessage);
            Assert.That(first.outcome, Is.EqualTo(AsyncInterpretationOutcome.Accepted));
            Assert.That(second.outcome, Is.EqualTo(AsyncInterpretationOutcome.AlreadyReady));
            Assert.That(second.interpretation.model_used, Is.EqualTo("deepseek-chat"));
        }

        [UnityTest]
        public IEnumerator RecoverSessionUsesRefreshWhenItWorks()
        {
            server.Script("POST", "/api/v1/refresh", MockTarotBackend.Json(200, MockTarotJson.Token("refreshed-token")));

            var recovered = false;
            yield return service.RecoverSession(value => recovered = value);

            Assert.That(recovered, Is.True);
            Assert.That(client.AccessToken, Is.EqualTo("refreshed-token"));
            Assert.That(server.Count("POST", "/api/v1/guest-session"), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator RecoverSessionStartsANewGuestWhenRefreshFails()
        {
            server.Script("POST", "/api/v1/refresh", MockTarotBackend.Json(401, "{\"detail\":\"Invalid refresh token\"}"));
            server.Script("POST", "/api/v1/guest-session", MockTarotBackend.Json(200, MockTarotJson.Token("fresh-guest")));

            var recovered = false;
            yield return service.RecoverSession(value => recovered = value);

            Assert.That(recovered, Is.True);
            Assert.That(client.AccessToken, Is.EqualTo("fresh-guest"));
            Assert.That(server.RequestLog, Is.EqualTo(new[] { "POST /api/v1/refresh", "POST /api/v1/guest-session" }));
        }

        private static PredictionCreateRequest Payload(int spreadId)
        {
            return new PredictionCreateRequest
            {
                question = "此刻我最需要留意什么？",
                question_type = "general",
                spread_type_id = spreadId,
            };
        }
    }
}

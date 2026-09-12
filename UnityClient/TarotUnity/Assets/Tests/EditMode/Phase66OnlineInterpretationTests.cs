using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 66: the online interpretation loop - the shared guest session, structured
    /// API errors, Chinese copy, snapshot state, the poller contract, and the Result
    /// screen's interpretation-state UI.
    /// </summary>
    public sealed class Phase66OnlineInterpretationTests
    {
        [TearDown]
        public void ClearSharedClient()
        {
            ApiClient.ClearShared();
        }

        [Test]
        public void ReadingServicePrefersTheSharedSessionOverTheSceneClient()
        {
            var boot = new GameObject("Phase66_BootClient");
            var room = new GameObject("Phase66_RoomServices");
            try
            {
                var shared = boot.AddComponent<ApiClient>();
                shared.SetAccessToken("guest-token");
                ApiClient.SetShared(shared);

                var sceneClient = room.AddComponent<ApiClient>();
                var service = room.AddComponent<BackendReadingService>();
                var serialized = new SerializedObject(service);
                serialized.FindProperty("apiClient").objectReferenceValue = sceneClient;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(sceneClient.HasSession, Is.False, "control: the scene client has no token");
                Assert.That(service.Client, Is.SameAs(shared));
                Assert.That(service.CanCreateAuthenticatedReading, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(room);
                Object.DestroyImmediate(boot);
            }
        }

        [Test]
        public void ReadingServiceFallsBackToItsSerializedClientWithoutShared()
        {
            var room = new GameObject("Phase66_RoomServices");
            try
            {
                var sceneClient = room.AddComponent<ApiClient>();
                var service = room.AddComponent<BackendReadingService>();
                var serialized = new SerializedObject(service);
                serialized.FindProperty("apiClient").objectReferenceValue = sceneClient;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(ApiClient.Shared, Is.Null, "control: no shared client in this test");
                Assert.That(service.Client, Is.SameAs(sceneClient));
                Assert.That(service.CanCreateAuthenticatedReading, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(room);
            }
        }

        [Test]
        public void AsyncInterpretRouteMatchesBackendContract()
        {
            Assert.That(ApiRoutes.RecordInterpretAsync(42), Is.EqualTo("/records/42/interpret/async"));
        }

        [TestCase(401L, "", ApiErrorKind.Unauthorized)]
        [TestCase(404L, "", ApiErrorKind.NotFound)]
        [TestCase(400L, "", ApiErrorKind.BadRequest)]
        [TestCase(422L, "", ApiErrorKind.BadRequest)]
        [TestCase(429L, "", ApiErrorKind.RateLimited)]
        [TestCase(500L, "", ApiErrorKind.Server)]
        [TestCase(503L, "", ApiErrorKind.Server)]
        [TestCase(0L, "Request timeout", ApiErrorKind.Timeout)]
        [TestCase(0L, "Cannot connect to destination host", ApiErrorKind.Network)]
        [TestCase(403L, "", ApiErrorKind.Unexpected)]
        public void ApiErrorKindFollowsStatusCode(long statusCode, string requestError, ApiErrorKind expected)
        {
            Assert.That(ApiError.Classify(statusCode, requestError), Is.EqualTo(expected));
        }

        [TestCase("5400", 5400)]
        [TestCase(" 60 ", 60)]
        [TestCase(null, -1)]
        [TestCase("", -1)]
        [TestCase("Wed, 21 Oct 2026 07:28:00 GMT", -1)]
        public void RetryAfterParsesSecondsOrMinusOne(string header, int expected)
        {
            Assert.That(ApiError.ParseRetryAfter(header), Is.EqualTo(expected));
        }

        [Test]
        public void ApiErrorKeepsTheRawBodyOutOfPlayerCopy()
        {
            var error = ApiError.FromResponse(
                429,
                "HTTP/1.1 429 Too Many Requests",
                "{\"detail\":\"Guest daily reading limit reached. Please try again tomorrow.\"}",
                "3601");

            Assert.That(error.Kind, Is.EqualTo(ApiErrorKind.RateLimited));
            Assert.That(error.RetryAfterSeconds, Is.EqualTo(3601));
            Assert.That(error.RawMessage, Does.Contain("Guest daily reading limit"), "control: the raw body is kept for logs");

            var copy = ReleaseUxCopy.ForStartReadingFailure(error);
            Assert.That(copy, Is.EqualTo("今天的访客占卜次数已用完，约 2 小时后恢复。这一局使用离线解读。"));
            Assert.That(copy, Does.Not.Contain("Guest"));
        }

        [TestCase(3599, "今天的访客占卜次数已用完，不到 1 小时后恢复。这一局使用离线解读。")]
        [TestCase(3600, "今天的访客占卜次数已用完，约 1 小时后恢复。这一局使用离线解读。")]
        [TestCase(3601, "今天的访客占卜次数已用完，约 2 小时后恢复。这一局使用离线解读。")]
        [TestCase(-1, "今天的访客占卜次数已用完，这一局使用离线解读。")]
        public void GuestQuotaCopyRoundsHoursUp(int retryAfterSeconds, string expected)
        {
            Assert.That(ReleaseUxCopy.GuestQuotaExhausted(retryAfterSeconds), Is.EqualTo(expected));
        }

        [TestCase(0L, "Cannot connect to destination host", "暂时连不上占卜服务，这一局使用离线解读。")]
        [TestCase(0L, "Request timeout", "暂时连不上占卜服务，这一局使用离线解读。")]
        [TestCase(502L, "", "暂时连不上占卜服务，这一局使用离线解读。")]
        [TestCase(401L, "", "访客会话连接失败，这一局使用离线解读。")]
        [TestCase(400L, "", "在线占卜暂时不可用，这一局使用离线解读。")]
        [TestCase(404L, "", "在线占卜暂时不可用，这一局使用离线解读。")]
        public void StartReadingFailuresMapToOfflineCopy(long statusCode, string requestError, string expected)
        {
            var error = ApiError.FromResponse(statusCode, requestError, string.Empty, null);
            Assert.That(ReleaseUxCopy.ForStartReadingFailure(error), Is.EqualTo(expected));
        }

        [TestCase(InterpretationFailure.BackendFailed, "这次解读没有顺利生成，可以再试一次。", true)]
        [TestCase(InterpretationFailure.TimedOut, "解读花的时间比预期长，可以再试一次。", true)]
        [TestCase(InterpretationFailure.ConnectionLost, "与占卜服务的连接中断了，可以再试一次。", true)]
        [TestCase(InterpretationFailure.AttemptsExhausted, "这一局已经尝试多次仍未成功，先看看离线解读吧。", false)]
        [TestCase(InterpretationFailure.SessionExpired, "连接已过期，这次解读无法取回。", false)]
        [TestCase(InterpretationFailure.Unavailable, "这次解读无法生成，先看看离线解读吧。", false)]
        public void InterpretationFailuresMapToCopyAndRetryability(InterpretationFailure failure, string expected, bool canRetry)
        {
            Assert.That(ReleaseUxCopy.ForInterpretationFailure(failure), Is.EqualTo(expected));
            Assert.That(ReleaseUxCopy.CanRetry(failure), Is.EqualTo(canRetry));
        }

        [Test]
        public void SnapshotDefaultsDescribeAnOfflineReadyReading()
        {
            var snapshot = new ReadingSessionSnapshot();

            Assert.That(snapshot.predictionId, Is.EqualTo(0));
            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(snapshot.modelUsed, Is.Empty);
            Assert.That(snapshot.failureMessage, Is.Empty);
            Assert.That(snapshot.canRetry, Is.False);
            Assert.That(default(InterpretationState), Is.EqualTo(InterpretationState.Ready), "the enum default must be Ready too");
        }

        [Test]
        public void OfflineSessionsCarryTheOfflineWarning()
        {
            var session = LocalReadingSimulator.CreateSession(
                1, "单张牌", "问题？", "general", LocalReadingSimulator.CreatePlaceholderDraws(1));

            Assert.That(session.warning, Is.EqualTo("这是离线解读，由本地牌义生成，未经过 AI。"));
            Assert.That(session.source, Is.EqualTo(ReadingSource.Offline));
        }

        // Phase 66: later tasks append tests above this line.
    }
}

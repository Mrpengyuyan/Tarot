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

        private const string DetailWithoutInterpretationJson =
            "{\"id\":601,\"user_id\":7,\"spread_type_id\":1,\"question\":\"问题\",\"question_type\":\"general\","
            + "\"status\":\"processing\",\"card_draws\":[],\"interpretation\":null}";

        private const string DetailWithInterpretationJson =
            "{\"id\":601,\"user_id\":7,\"spread_type_id\":1,\"question\":\"问题\",\"question_type\":\"general\","
            + "\"status\":\"processing\",\"card_draws\":[],\"interpretation\":{\"id\":3001,\"prediction_id\":601,"
            + "\"overall_interpretation\":\"整体\",\"card_analysis\":\"牌面\",\"advice\":\"建议\",\"warning\":\"提醒\","
            + "\"summary\":\"概要\",\"model_used\":\"deepseek-chat\"}}";

        [Test]
        public void JsonNullInterpretationIsNotTreatedAsReady()
        {
            var detail = JsonUtility.FromJson<PredictionDetailResponse>(DetailWithoutInterpretationJson);

            Assert.That(detail, Is.Not.Null, "control: the JSON parsed");
            Assert.That(detail.id, Is.EqualTo(601), "control: fields were read");
            Assert.That(ReadingSessionMapper.HasInterpretation(detail), Is.False);
        }

        [Test]
        public void InterpretationPresentCountsAsReadyWhateverTheStatus()
        {
            var detail = JsonUtility.FromJson<PredictionDetailResponse>(DetailWithInterpretationJson);

            Assert.That(detail.status, Is.EqualTo("processing"), "control: status is not completed");
            Assert.That(ReadingSessionMapper.HasInterpretation(detail), Is.True);
        }

        [Test]
        public void StartMappingProducesAnOnlinePendingSnapshot()
        {
            var prediction = new PredictionResponse { id = 601, spread_type_id = 2, question = "问题", question_type = "general" };
            var cards = LocalReadingSimulator.CreatePlaceholderDraws(3);

            var snapshot = ReadingSessionMapper.FromBackendStart(prediction, cards);

            Assert.That(snapshot.predictionId, Is.EqualTo(601));
            Assert.That(snapshot.spreadId, Is.EqualTo(2));
            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Pending));
            Assert.That(snapshot.cardDraws, Is.SameAs(cards));
            Assert.That(snapshot.cardCount, Is.EqualTo(3));
            Assert.That(snapshot.question, Is.EqualTo("问题"));
            Assert.That(snapshot.summary, Is.Empty);
            Assert.That(snapshot.warning, Is.Empty);
            Assert.That(ReadingSessionMapper.FromBackendStart(null, cards), Is.Null);
        }

        [Test]
        public void ApplyingAnInterpretationMakesTheSnapshotReady()
        {
            var snapshot = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 601 }, LocalReadingSimulator.CreatePlaceholderDraws(1));
            snapshot.interpretationState = InterpretationState.Failed;
            snapshot.failureMessage = "旧的失败原因";
            snapshot.canRetry = true;

            ReadingSessionMapper.ApplyInterpretation(snapshot, new InterpretationResponse
            {
                id = 3001,
                summary = "概要",
                overall_interpretation = "整体",
                card_analysis = "牌面",
                advice = "建议",
                warning = "提醒",
                model_used = "deepseek-chat",
            });

            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(snapshot.summary, Is.EqualTo("概要"));
            Assert.That(snapshot.overallInterpretation, Is.EqualTo("整体"));
            Assert.That(snapshot.cardAnalysis, Is.EqualTo("牌面"));
            Assert.That(snapshot.advice, Is.EqualTo("建议"));
            Assert.That(snapshot.warning, Is.EqualTo("提醒"));
            Assert.That(snapshot.modelUsed, Is.EqualTo("deepseek-chat"));
            Assert.That(snapshot.failureMessage, Is.Empty);
            Assert.That(snapshot.canRetry, Is.False);
        }

        [Test]
        public void DetailMappingRecordsTheOnlineStateAndModel()
        {
            var ready = ReadingSessionMapper.FromBackendDetail(
                JsonUtility.FromJson<PredictionDetailResponse>(DetailWithInterpretationJson));
            Assert.That(ready.predictionId, Is.EqualTo(601));
            Assert.That(ready.source, Is.EqualTo(ReadingSource.Online));
            Assert.That(ready.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(ready.modelUsed, Is.EqualTo("deepseek-chat"));

            var pending = ReadingSessionMapper.FromBackendDetail(
                JsonUtility.FromJson<PredictionDetailResponse>(DetailWithoutInterpretationJson));
            Assert.That(pending.interpretationState, Is.EqualTo(InterpretationState.Pending));
            Assert.That(pending.modelUsed, Is.Empty);
        }

        [Test]
        public void PollDelaysFollowTwoTwoThreeThreeThenFive()
        {
            var expected = new[] { 2f, 2f, 3f, 3f, 5f, 5f, 5f };
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(InterpretationPoller.PollDelaySeconds(i), Is.EqualTo(expected[i]), $"attempt {i}");
            }

            Assert.That(InterpretationPoller.PollDelaySeconds(40), Is.EqualTo(5f));
        }

        [Test]
        public void PollerLimitsMatchTheBackendContract()
        {
            Assert.That(InterpretationPoller.TotalDeadlineSeconds, Is.EqualTo(330f), "backend stale window 300 s + 30 s");
            Assert.That(InterpretationPoller.MaxConsecutiveNetworkErrors, Is.EqualTo(3));
        }

        [Test]
        public void ApplyingAFailureRecordsCopyAndRetryability()
        {
            var snapshot = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 601 }, LocalReadingSimulator.CreatePlaceholderDraws(1));

            InterpretationPoller.ApplyFailure(snapshot, InterpretationFailure.ConnectionLost);
            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Failed));
            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationConnectionLost));
            Assert.That(snapshot.canRetry, Is.True);

            InterpretationPoller.ApplyFailure(snapshot, InterpretationFailure.SessionExpired);
            Assert.That(snapshot.failureMessage, Is.EqualTo(ReleaseUxCopy.InterpretationSessionExpired));
            Assert.That(snapshot.canRetry, Is.False);
        }

        [Test]
        public void UsingOfflineTextKeepsTheDrawnCardsAndSwitchesSource()
        {
            var cards = LocalReadingSimulator.CreatePlaceholderDraws(3);
            var snapshot = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 601, question = "问题" }, cards);
            snapshot.spreadName = "过去现在未来";
            InterpretationPoller.ApplyFailure(snapshot, InterpretationFailure.Unavailable);

            InterpretationPoller.ApplyOffline(snapshot);

            Assert.That(snapshot.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(snapshot.interpretationState, Is.EqualTo(InterpretationState.Ready));
            Assert.That(snapshot.cardDraws, Is.SameAs(cards));
            Assert.That(snapshot.predictionId, Is.EqualTo(601));
            Assert.That(snapshot.warning, Is.EqualTo(ReleaseUxCopy.OfflineWarning));
            Assert.That(snapshot.cardAnalysis, Does.Contain(cards[0].tarot_card.name_zh));
            Assert.That(snapshot.failureMessage, Is.Empty);
            Assert.That(snapshot.canRetry, Is.False);
        }

        [Test]
        public void GameBootstrapSharesTheClientAndHostsThePoller()
        {
            var source = File.ReadAllText("Assets/Scripts/Core/GameBootstrap.cs");

            Assert.That(source, Does.Contain("EnsureService<ApiClient>()"), "control: the scan reads the real bootstrap");
            Assert.That(source, Does.Contain("ApiClient.SetShared(GetComponent<ApiClient>())"));
            Assert.That(source, Does.Contain("EnsureService<InterpretationPoller>()"));
        }

        private static readonly string[] RetiredReadingRoomLiterals =
        {
            "\"Shuffling...\"",
            "\"Creating backend reading...\"",
            "\"Dealing cards...\"",
            "\"Click each card to flip it.\"",
            "\"The reading is almost ready.\"",
            "\"The reading is ready.\"",
            "\"What should I notice now?\"",
            "\"Backend spreads loaded.\"",
        };

        [Test]
        public void ReadingRoomFlowCopyIsChineseAndCentralised()
        {
            var flowCopy = new[]
            {
                ReleaseUxCopy.DefaultQuestion,
                ReleaseUxCopy.FlowShuffling,
                ReleaseUxCopy.FlowDealing,
                ReleaseUxCopy.FlowFlipPrompt,
                ReleaseUxCopy.FlowAllRevealed,
                ReleaseUxCopy.FlowResultReady,
                ReleaseUxCopy.InterpretationGenerating,
                ReleaseUxCopy.InterpretationReadyHint,
                ReleaseUxCopy.OnlineReady,
            };
            foreach (var line in flowCopy)
            {
                Assert.That(line, Is.Not.Empty);
                Assert.That(Regex.IsMatch(line, "[A-Za-z]"), Is.False, $"'{line}' should not contain ASCII letters");
            }

            var source = File.ReadAllText("Assets/Scripts/UI/ReadingRoomController.cs");
            Assert.That(source, Does.Contain("ReleaseUxCopy.FlowShuffling"), "control: the scan reads the real controller");
            foreach (var literal in RetiredReadingRoomLiterals)
            {
                Assert.That(source, Does.Not.Contain(literal), $"{literal} should come from ReleaseUxCopy");
            }
        }

        [Test]
        public void CompleteReadingIsRetired()
        {
            Assert.That(typeof(BackendReadingService).GetMethod("StartReading"), Is.Not.Null,
                "control: reflection sees the service");
            Assert.That(typeof(BackendReadingService).GetMethod("CompleteReading"), Is.Null,
                "the old path waited on the AI before dealing; online readings use StartReading");
        }

        // Phase 66: later tasks append tests above this line.
    }
}

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
    /// Phase 66: the Result screen follows the persistent poller. A Pending reading
    /// turns into its AI text, and a failed one offers 重新解读 and 查看离线解读.
    /// </summary>
    public sealed class Phase66ResultInterpretationStateTests
    {
        private const int PredictionId = 801;
        private const string AsyncPath = "/api/v1/records/801/interpret/async";
        private const string DetailPath = "/api/v1/records/801";

        private MockTarotBackend server;
        private GameObject boot;
        private InterpretationPoller poller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ReadingSessionStore.Clear();
            server = MockTarotBackend.Start();
            boot = new GameObject("Phase66_ResultTestBoot");
            Object.DontDestroyOnLoad(boot);
            var client = boot.AddComponent<ApiClient>();
            client.BaseUrl = server.ApiBaseUrl;
            client.SetAccessToken("test-access-token");
            ApiClient.SetShared(client);
            poller = boot.AddComponent<InterpretationPoller>();
            poller.Configure(client, 0.05f, 30f);
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
        public IEnumerator PendingReadingSwitchesToTheInterpretationWhenItArrives()
        {
            server.Script("POST", AsyncPath, MockTarotBackend.Json(202, MockTarotJson.Accepted(PredictionId)));
            server.Script("GET", DetailPath,
                MockTarotBackend.Json(200, MockTarotJson.Detail(PredictionId, "processing", 3, null)));
            var session = StartOnlineSession(3);

            yield return LoadResult();
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            var status = GetField<TMP_Text>(presenter, "interpretationStatusText");
            var summary = GetField<TMP_Text>(presenter, "summaryText");
            var group = GetField<CanvasGroup>(presenter, "readingContentGroup");

            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending), "control: still generating");
            Assert.That(status.gameObject.activeInHierarchy, Is.True, "pending: the status line shows");
            Assert.That(status.text, Does.StartWith(ReleaseUxCopy.ResultPending));
            Assert.That(group.alpha, Is.EqualTo(0f));

            server.Script("GET", DetailPath, MockTarotBackend.Json(200,
                MockTarotJson.Detail(PredictionId, "completed", 3, MockTarotJson.Interpretation(PredictionId, "deepseek-chat"))));
            yield return WaitUntil(() => session.interpretationState == InterpretationState.Ready, 10f, "expected Ready");
            yield return WaitUntil(() => group.alpha >= 1f, 5f, "expected the reading to fade in");

            Assert.That(status.gameObject.activeSelf, Is.False);
            Assert.That(summary.text, Is.EqualTo("概要来自测试桩。"));
            Assert.That(GetField<TMP_Text>(presenter, "modeLabelText").text, Is.Empty);
        }

        [UnityTest]
        public IEnumerator FailedReadingOffersRetryAndOfflineText()
        {
            server.Script("POST", AsyncPath,
                MockTarotBackend.Json(202, MockTarotJson.Accepted(PredictionId)),
                MockTarotBackend.Json(202, MockTarotJson.Accepted(PredictionId)));
            server.Script("GET", DetailPath,
                MockTarotBackend.Json(200, MockTarotJson.Detail(PredictionId, "failed", 3, null)));
            var session = StartOnlineSession(3);

            yield return LoadResult();
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            var controller = Object.FindFirstObjectByType<ResultSceneController>();
            var status = GetField<TMP_Text>(presenter, "interpretationStatusText");
            yield return WaitUntil(() => session.interpretationState == InterpretationState.Failed, 10f, "expected Failed");

            var retry = GetField<Button>(controller, "retryInterpretationButton");
            var offline = GetField<Button>(controller, "offlineInterpretationButton");
            Assert.That(status.text, Is.EqualTo(ReleaseUxCopy.InterpretationBackendFailed));
            Assert.That(retry.gameObject.activeInHierarchy, Is.True);
            Assert.That(offline.gameObject.activeInHierarchy, Is.True);

            retry.onClick.Invoke();
            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending), "retry restarts the poll");
            yield return WaitUntil(() => session.interpretationState == InterpretationState.Failed, 10f,
                "expected the retry to fail again");
            Assert.That(server.Count("POST", AsyncPath), Is.EqualTo(2));

            offline.onClick.Invoke();
            Assert.That(session.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(GetField<TMP_Text>(presenter, "modeLabelText").text, Is.EqualTo(ReleaseUxCopy.ModeOffline));
            Assert.That(GetField<TMP_Text>(presenter, "warningText").text, Is.EqualTo(ReleaseUxCopy.OfflineWarning));
            Assert.That(status.gameObject.activeSelf, Is.False);
            Assert.That(retry.gameObject.activeSelf || offline.gameObject.activeSelf, Is.False);
        }

        private ReadingSessionSnapshot StartOnlineSession(int cardCount)
        {
            var session = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse
                {
                    id = PredictionId,
                    spread_type_id = 12,
                    question = "此刻我最需要留意什么？",
                    question_type = "general",
                },
                LocalReadingSimulator.CreatePlaceholderDraws(cardCount));
            session.spreadName = "过去现在未来";
            ReadingSessionStore.Save(session);
            poller.Begin(session);
            return session;
        }

        private static IEnumerator LoadResult()
        {
            SceneManager.LoadScene("Result");
            yield return null;
            yield return null;
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "Result"
                    && Object.FindFirstObjectByType<ResultSceneController>() != null,
                10f,
                "expected the Result scene");
            yield return null;
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

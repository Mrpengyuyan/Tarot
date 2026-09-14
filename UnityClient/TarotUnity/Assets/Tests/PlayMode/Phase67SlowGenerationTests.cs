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
    /// Phase 67 (spec C, decision C3): after 20 seconds of generating, the Result screen offers
    /// 查看离线解读 together with the slow notice; picking it shows the offline reading and stops
    /// polling.
    /// </summary>
    public sealed class Phase67SlowGenerationTests
    {
        private const int PredictionId = 867;
        private const string AsyncPath = "/api/v1/records/867/interpret/async";
        private const string DetailPath = "/api/v1/records/867";

        private MockTarotBackend server;
        private GameObject boot;
        private InterpretationPoller poller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ReadingSessionStore.Clear();
            server = MockTarotBackend.Start();
            boot = new GameObject("Phase67_SlowGenerationTestBoot");
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
        public IEnumerator SlowGenerationOffersOfflineTextAndStopsPolling()
        {
            server.Script("POST", AsyncPath, MockTarotBackend.Json(202, MockTarotJson.Accepted(PredictionId)));
            server.Script("GET", DetailPath,
                MockTarotBackend.Json(200, MockTarotJson.Detail(PredictionId, "processing", 3, null)));
            var session = StartOnlineSession(3);

            yield return LoadResult();
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            var controller = Object.FindFirstObjectByType<ResultSceneController>();
            var offline = GetField<Button>(controller, "offlineInterpretationButton");
            var status = GetField<TMP_Text>(presenter, "interpretationStatusText");
            yield return WaitUntil(() => server.Count("GET", DetailPath) >= 1, 10f, "control: the reading is being polled");

            Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Pending));
            Assert.That(offline.gameObject.activeSelf, Is.False, "control: no offline button in the first seconds");

            // Pretend generation started 21 seconds ago instead of waiting for it.
            SetPendingSince(presenter, Time.unscaledTime - (ResultPanelPresenter.PendingSlowNoticeSeconds + 1f));
            yield return null;
            yield return null;

            Assert.That(offline.gameObject.activeInHierarchy, Is.True, "查看离线解读 appears after 20 seconds");
            Assert.That(status.text, Does.Contain(ReleaseUxCopy.ResultPendingSlow));

            offline.onClick.Invoke();
            Assert.That(session.source, Is.EqualTo(ReadingSource.Offline));
            Assert.That(GetField<TMP_Text>(presenter, "offlineNoticeText").gameObject.activeInHierarchy, Is.True,
                "the offline reading says so in its first line");

            // A request already in flight may still land; after that, no more polling.
            yield return new WaitForSecondsRealtime(0.3f);
            var polled = server.Count("GET", DetailPath);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(server.Count("GET", DetailPath), Is.EqualTo(polled), "polling stops once the player picks offline text");
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

        private static void SetPendingSince(ResultPanelPresenter presenter, float value)
        {
            var field = typeof(ResultPanelPresenter).GetField("pendingSince", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "ResultPanelPresenter.pendingSince");
            field.SetValue(presenter, value);
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

using System;
using System.Collections;
using System.Reflection;
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
using UnityEngine.Networking;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    internal static class LocalMockSafetyGuard
    {
        [Serializable]
        private sealed class AiStatus
        {
            public string status;
            public bool is_configured;
        }

        public static IEnumerator RequireKeyFreeBackend(ApiClient client)
        {
            Assert.That(client, Is.Not.Null);
            var expected = Environment.GetEnvironmentVariable("TAROT_LOCAL_MOCK_URL");
            Assert.That(string.IsNullOrWhiteSpace(expected), Is.False, "Set TAROT_LOCAL_MOCK_URL explicitly.");
            Assert.That(Uri.TryCreate(expected, UriKind.Absolute, out var parsed), Is.True);
            Assert.That(parsed.Scheme, Is.EqualTo("http"));
            Assert.That(parsed.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(client.BaseUrl, Is.EqualTo(DesktopRuntimeConfig.NormalizeBackendBaseUrl(expected)),
                "The effective Unity API address must match the isolated mock backend.");

            using var request = UnityWebRequest.Get(client.BuildUrl("/health/ai"));
            yield return request.SendWebRequest();
            var payload = request.downloadHandler?.text;
            var ai = string.IsNullOrWhiteSpace(payload) ? null : JsonUtility.FromJson<AiStatus>(payload);
            Assert.That(request.responseCode, Is.EqualTo(503), "Expected the key-free AI status response.");
            Assert.That(ai?.status, Is.EqualTo("not_configured"));
            Assert.That(ai.is_configured, Is.False, "A configured model could cause a paid request.");
        }
    }

    public sealed class LocalMockBackendIntegrationTests
    {
        private GameObject owner;
        private ApiClient client;
        private BackendReadingService service;

        [SetUp]
        public void SetUp()
        {
            if (Environment.GetEnvironmentVariable("TAROT_LOCAL_MOCK_BACKEND") != "1")
            {
                Assert.Ignore("Opt-in local mock FastAPI test only.");
            }

            var url = Environment.GetEnvironmentVariable("TAROT_LOCAL_MOCK_URL");
            Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var parsed), Is.True);
            Assert.That(parsed.Host, Is.EqualTo("127.0.0.1"), "The mock backend must be bound to loopback.");

            owner = new GameObject("LocalMockBackendIntegrationTest");
            client = owner.AddComponent<ApiClient>();
            client.BaseUrl = url;
            ApiClient.SetShared(client);
            service = owner.AddComponent<BackendReadingService>();
        }

        [TearDown]
        public void TearDown()
        {
            ApiClient.ClearShared();
            if (owner != null)
            {
                Object.Destroy(owner);
            }
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator OneThreeAndTenCardReadingsUseTheRealLocalApiWithoutModelCalls()
        {
            HealthCheckResponse health = null;
            string textError = null;
            yield return client.CheckHealth(value => health = value, value => textError = value);
            Assert.That(textError, Is.Null);
            Assert.That(health?.status, Is.EqualTo("healthy"));
            yield return LocalMockSafetyGuard.RequireKeyFreeBackend(client);

            yield return client.CreateGuestSession(_ => { }, value => textError = value);
            Assert.That(textError, Is.Null);
            Assert.That(client.HasAccessToken, Is.True);

            SpreadSummary[] spreads = null;
            yield return client.GetSpreads(value => spreads = value, value => textError = value);
            Assert.That(textError, Is.Null);
            Assert.That(spreads, Is.Not.Null);

            foreach (var count in new[] { 1, 3, 10 })
            {
                var spread = Array.Find(spreads, item => item.card_count == count);
                Assert.That(spread, Is.Not.Null, $"Missing {count}-card spread");

                ReadingSessionSnapshot session = null;
                ApiError error = null;
                var payload = new PredictionCreateRequest
                {
                    question = $"本地模拟联调：{count} 张牌",
                    question_type = "general",
                    spread_type_id = spread.id,
                };
                yield return service.StartReading(payload, value => session = value, value => error = value);
                Assert.That(error, Is.Null, error?.RawMessage);
                Assert.That(session?.cardDraws, Has.Length.EqualTo(count));

                AsyncInterpretationResult started = null;
                yield return service.RequestInterpretationAsync(session.predictionId, value => started = value, value => error = value);
                Assert.That(error, Is.Null, error?.RawMessage);
                Assert.That(started, Is.Not.Null);

                PredictionDetailResponse detail = null;
                var deadline = Time.realtimeSinceStartup + 20f;
                do
                {
                    yield return service.GetRecord(session.predictionId, value => detail = value, value => error = value);
                    Assert.That(error, Is.Null, error?.RawMessage);
                    if (detail?.interpretation_run?.state == "completed")
                    {
                        break;
                    }

                    yield return new WaitForSecondsRealtime(0.2f);
                } while (Time.realtimeSinceStartup < deadline);

                Assert.That(detail?.interpretation_run?.state, Is.EqualTo("completed"));
                Assert.That(detail.card_draws, Has.Length.EqualTo(count));
                Assert.That(detail.interpretation?.model_used, Is.EqualTo("mock_ai"));
                Assert.That(detail.interpretation?.overall_interpretation, Is.Not.Empty);
                Assert.That(detail.interpretation_run.requests_started, Is.Zero,
                    "The isolated mock backend must never send a model request.");
            }
        }
    }

    public sealed class LocalMockSceneIntegrationTests
    {
        [SetUp]
        public void RequireLocalMockBackend()
        {
            if (Environment.GetEnvironmentVariable("TAROT_LOCAL_MOCK_BACKEND") != "1")
            {
                Assert.Ignore("Opt-in local mock FastAPI test only.");
            }

            var url = Environment.GetEnvironmentVariable("TAROT_BACKEND_URL");
            Assert.That(Uri.TryCreate(url, UriKind.Absolute, out var parsed), Is.True);
            Assert.That(parsed.Host, Is.EqualTo("127.0.0.1"), "The scene must use the local mock backend.");
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            SceneManager.LoadScene("MainMenu");
            yield return null;
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
        public IEnumerator OneThreeAndTenCardScenesReachTheMockResult()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu", 30f);
            var bootstrap = Object.FindFirstObjectByType<BackendSessionBootstrap>();
            yield return WaitUntil(() => bootstrap != null &&
                (bootstrap.Status == BackendSessionStatus.Online || bootstrap.Status == BackendSessionStatus.Offline), 30f);
            Assert.That(bootstrap.Status, Is.EqualTo(BackendSessionStatus.Online), bootstrap.LastError);
            yield return LocalMockSafetyGuard.RequireKeyFreeBackend(ApiClient.Shared);

            foreach (var (buttonName, count) in new[]
            {
                ("oneCardButton", 1), ("threeCardButton", 3), ("celticCrossButton", 10),
            })
            {
                var menu = Object.FindFirstObjectByType<MainMenuController>();
                GetField<Button>(menu, "startReadingButton").onClick.Invoke();
                yield return WaitUntil(() => SceneManager.GetActiveScene().name == "ReadingRoom", 30f);
                var room = Object.FindFirstObjectByType<ReadingRoomController>();
                var spreadField = typeof(ReadingRoomController).GetField("backendSpreads", BindingFlags.Instance | BindingFlags.NonPublic);
                yield return WaitUntil(() => spreadField?.GetValue(room) != null, 30f);

                GetField<Button>(room, buttonName).onClick.Invoke();
                GetField<TMP_InputField>(room, "questionInput").text = $"本地模拟场景联调：{count} 张牌";
                GetField<Button>(room, "drawButton").onClick.Invoke();
                yield return DrawRitualTestDriver.PickAll(120f);

                var deck = Object.FindFirstObjectByType<DeckController>();
                var flow = Object.FindFirstObjectByType<ReadingFlowController>();
                yield return WaitUntil(() => deck.ActiveCards.Count == count && flow.State == ReadingFlowState.WaitingForFlip, 60f);
                var session = ReadingSessionStore.Current;
                Assert.That(session.source, Is.EqualTo(ReadingSource.Online));
                Assert.That(session.cardDraws, Has.Length.EqualTo(count));

                for (var i = 0; i < count; i++)
                {
                    var card = deck.ActiveCards[i];
                    card.GetComponent<CardClickHandler>().OnPointerClick(new PointerEventData(EventSystem.current)
                    {
                        button = PointerEventData.InputButton.Left,
                    });
                    yield return WaitUntil(() => card.IsFaceUp && !card.GetComponent<CardFlipController>().IsFlipping, 10f);
                }

                yield return WaitUntil(() => flow.State == ReadingFlowState.ResultReady, 20f);
                var reveal = GetField<Button>(room, "revealResultButton");
                yield return WaitUntil(() => reveal.gameObject.activeInHierarchy, 10f);
                reveal.onClick.Invoke();
                yield return WaitUntil(() => SceneManager.GetActiveScene().name == "Result", 30f);
                yield return WaitUntil(() => session.interpretationState != InterpretationState.Pending, 30f);

                Assert.That(session.interpretationState, Is.EqualTo(InterpretationState.Ready), session.failureMessage);
                Assert.That(session.modelUsed, Is.EqualTo("mock_ai"));

                var result = Object.FindFirstObjectByType<ResultSceneController>();
                var presenter = GetField<ResultPanelPresenter>(result, "resultPanel");
                var summary = GetField<TMP_Text>(presenter, "summaryText");
                yield return WaitUntil(() => !string.IsNullOrWhiteSpace(summary.text), 20f);
                Assert.That(summary.gameObject.activeInHierarchy, Is.True);
                Assert.That(GetField<TMP_Text>(presenter, "overallText").text, Is.Not.Empty);
                Assert.That(GetField<TMP_Text>(presenter, "questionText").text,
                    Does.Contain($"{count} 张牌"));
                if (count == 1)
                {
                    var singleRoots = GetField<GameObject[]>(presenter, "singleModeRoots");
                    Assert.That(Array.Exists(singleRoots, root => root != null && root.activeInHierarchy), Is.True);
                }
                else
                {
                    var cells = GetField<ResultPanelPresenter.SpreadCardCell[]>(presenter, "spreadCards");
                    Assert.That(Array.FindAll(cells, cell => cell?.root != null && cell.root.activeInHierarchy).Length,
                        Is.EqualTo(count));
                }

                GetField<Button>(result, "backToMenuButton").onClick.Invoke();
                yield return WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu", 30f);
                yield return null;
            }
        }

        private static IEnumerator WaitUntil(Func<bool> predicate, float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(predicate(), Is.True, "Timed out while completing the local mock scene flow.");
        }

        private static T GetField<T>(object target, string name) where T : class
        {
            Assert.That(target, Is.Not.Null);
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing {name} on {target.GetType().Name}");
            var value = field.GetValue(target) as T;
            Assert.That(value, Is.Not.Null);
            return value;
        }
    }
}

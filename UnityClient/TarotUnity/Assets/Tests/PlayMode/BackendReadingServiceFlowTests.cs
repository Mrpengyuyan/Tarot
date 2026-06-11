using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Network;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class BackendReadingServiceFlowTests
    {
        [UnityTest]
        public IEnumerator CompletesReadingAgainstHttpBackend()
        {
            using var server = MockTarotBackend.Start();
            var owner = new GameObject("BackendReadingServiceFlowTest");

            try
            {
                var apiClient = owner.AddComponent<ApiClient>();
                apiClient.BaseUrl = server.ApiBaseUrl;
                apiClient.SetAccessToken("test-access-token");

                var service = owner.AddComponent<BackendReadingService>();
                var payload = new PredictionCreateRequest
                {
                    question = "What should the Unity client verify?",
                    question_type = "general",
                    spread_type_id = 1,
                };

                ReadingSessionSnapshot session = null;
                string error = null;

                yield return service.CompleteReading(payload, value => session = value, value => error = value);

                Assert.That(error, Is.Null.Or.Empty);
                Assert.That(session, Is.Not.Null);
                Assert.That(session.spreadId, Is.EqualTo(1));
                Assert.That(session.spreadName, Is.EqualTo("One Card Focus"));
                Assert.That(session.cardDraws, Has.Length.EqualTo(1));
                Assert.That(session.cardDraws[0].tarot_card.name_zh, Is.EqualTo("The Star"));
                Assert.That(session.overallInterpretation, Is.EqualTo("Mock backend interpretation."));

                CollectionAssert.Contains(server.RequestPaths, "/api/v1/records/");
                CollectionAssert.Contains(server.RequestPaths, "/api/v1/records/501/draw");
                CollectionAssert.Contains(server.RequestPaths, "/api/v1/records/501/cards");
                CollectionAssert.Contains(server.RequestPaths, "/api/v1/records/501/interpret");
                CollectionAssert.Contains(server.RequestPaths, "/api/v1/records/501");
            }
            finally
            {
                UnityEngine.Object.Destroy(owner);
            }
        }

        private sealed class MockTarotBackend : IDisposable
        {
            private readonly HttpListener listener;
            private readonly CancellationTokenSource cancellation = new();
            private readonly Task serverTask;

            private MockTarotBackend(HttpListener listener, int port)
            {
                this.listener = listener;
                ApiBaseUrl = $"http://127.0.0.1:{port}/api/v1";
                serverTask = Task.Run(ServerLoop);
            }

            public string ApiBaseUrl { get; }
            public List<string> RequestPaths { get; } = new();

            public static MockTarotBackend Start()
            {
                var port = GetFreePort();
                var listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                listener.Start();
                return new MockTarotBackend(listener, port);
            }

            public void Dispose()
            {
                cancellation.Cancel();
                listener.Stop();
                listener.Close();

                try
                {
                    serverTask.Wait(500);
                }
                catch (AggregateException)
                {
                    // The listener is intentionally stopped during teardown.
                }
            }

            private async Task ServerLoop()
            {
                while (!cancellation.IsCancellationRequested)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await listener.GetContextAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (HttpListenerException)
                    {
                        return;
                    }

                    _ = Task.Run(() => Handle(context), cancellation.Token);
                }
            }

            private void Handle(HttpListenerContext context)
            {
                var path = context.Request.Url?.AbsolutePath ?? string.Empty;
                lock (RequestPaths)
                {
                    RequestPaths.Add(path);
                }

                var json = ResolveResponse(path, context.Request.HttpMethod, out var statusCode);
                var bytes = Encoding.UTF8.GetBytes(json);
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.Close();
            }

            private static string ResolveResponse(string path, string method, out int statusCode)
            {
                statusCode = 200;

                if (path == "/api/v1/records/" && method == "POST")
                {
                    return "{\"id\":501,\"user_id\":7,\"spread_type_id\":1,\"question\":\"What should the Unity client verify?\",\"question_type\":\"general\",\"status\":\"pending\",\"created_at\":\"2026-05-23T00:00:00Z\",\"completed_at\":null,\"is_favorite\":false,\"user_rating\":0,\"user_notes\":\"\"}";
                }

                if (path == "/api/v1/records/501/draw" && method == "POST")
                {
                    return "{\"prediction_id\":501,\"status\":\"success\",\"card_draws\":[{\"id\":9001,\"prediction_id\":501,\"tarot_card_id\":17,\"position\":1,\"is_reversed\":false,\"drawn_at\":\"2026-05-23T00:00:01Z\",\"tarot_card\":{\"id\":17,\"name_zh\":\"The Star\",\"name_en\":\"The Star\",\"arcana\":\"major\",\"suit\":\"\",\"number\":17,\"image_url\":\"\"}}]}";
                }

                if (path == "/api/v1/records/501/cards" && method == "GET")
                {
                    return "[{\"id\":9001,\"prediction_id\":501,\"tarot_card_id\":17,\"position\":1,\"is_reversed\":false,\"drawn_at\":\"2026-05-23T00:00:01Z\",\"tarot_card\":{\"id\":17,\"name_zh\":\"The Star\",\"name_en\":\"The Star\",\"arcana\":\"major\",\"suit\":\"\",\"number\":17,\"image_url\":\"\"},\"card_meaning\":{\"id\":17,\"name_zh\":\"The Star\",\"name_en\":\"The Star\",\"is_reversed\":false,\"meaning\":\"hope and repair\",\"keywords\":[\"hope\",\"repair\"],\"position\":1,\"position_name\":\"Focus\",\"position_meaning\":\"The clearest signal.\"},\"position_name\":\"Focus\",\"position_meaning\":\"The clearest signal.\"}]";
                }

                if (path == "/api/v1/records/501/interpret" && method == "POST")
                {
                    return "{\"id\":3001,\"prediction_id\":501,\"overall_interpretation\":\"Mock backend interpretation.\",\"card_analysis\":\"The Star keeps the slice hopeful.\",\"relationship_analysis\":\"\",\"advice\":\"Keep the backend path small and verified.\",\"warning\":\"Do not skip error checks.\",\"summary\":\"Mock summary\",\"key_themes\":\"hope,verification\",\"model_used\":\"mock\",\"model_version\":\"test\",\"confidence_score\":0.91,\"generated_at\":\"2026-05-23T00:00:02Z\"}";
                }

                if (path == "/api/v1/records/501" && method == "GET")
                {
                    return "{\"id\":501,\"user_id\":7,\"spread_type_id\":1,\"question\":\"What should the Unity client verify?\",\"question_type\":\"general\",\"status\":\"completed\",\"created_at\":\"2026-05-23T00:00:00Z\",\"completed_at\":\"2026-05-23T00:00:02Z\",\"is_favorite\":false,\"user_rating\":0,\"user_notes\":\"\",\"spread_type\":{\"id\":1,\"name\":\"One Card Focus\",\"name_en\":\"One Card Focus\",\"description\":\"Single card\",\"card_count\":1,\"difficulty_level\":1,\"positions\":[{\"position\":1,\"name\":\"Focus\",\"meaning\":\"The clearest signal.\"}],\"is_beginner_friendly\":true,\"usage_count\":0,\"suitable_for_love\":true,\"suitable_for_career\":true,\"suitable_for_finance\":true,\"suitable_for_health\":true,\"suitable_for_general\":true},\"card_draws\":[{\"id\":9001,\"prediction_id\":501,\"tarot_card_id\":17,\"position\":1,\"is_reversed\":false,\"drawn_at\":\"2026-05-23T00:00:01Z\",\"tarot_card\":{\"id\":17,\"name_zh\":\"The Star\",\"name_en\":\"The Star\",\"arcana\":\"major\",\"suit\":\"\",\"number\":17,\"image_url\":\"\"},\"card_meaning\":{\"id\":17,\"name_zh\":\"The Star\",\"name_en\":\"The Star\",\"is_reversed\":false,\"meaning\":\"hope and repair\",\"keywords\":[\"hope\",\"repair\"],\"position\":1,\"position_name\":\"Focus\",\"position_meaning\":\"The clearest signal.\"},\"position_name\":\"Focus\",\"position_meaning\":\"The clearest signal.\"}],\"interpretation\":{\"id\":3001,\"prediction_id\":501,\"overall_interpretation\":\"Mock backend interpretation.\",\"card_analysis\":\"The Star keeps the slice hopeful.\",\"relationship_analysis\":\"\",\"advice\":\"Keep the backend path small and verified.\",\"warning\":\"Do not skip error checks.\",\"summary\":\"Mock summary\",\"key_themes\":\"hope,verification\",\"model_used\":\"mock\",\"model_version\":\"test\",\"confidence_score\":0.91,\"generated_at\":\"2026-05-23T00:00:02Z\"}}";
                }

                statusCode = 404;
                return "{\"detail\":\"not found\"}";
            }

            private static int GetFreePort()
            {
                var tcp = new TcpListener(IPAddress.Loopback, 0);
                tcp.Start();
                var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
                tcp.Stop();
                return port;
            }
        }
    }
}

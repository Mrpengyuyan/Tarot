using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 66: a scripted stand-in for the FastAPI backend. Each route (method +
    /// path) holds a queue of replies and the last reply keeps repeating, so a test
    /// scripts only the transitions it cares about. Unscripted routes answer 404.
    /// </summary>
    internal sealed class MockTarotBackend : IDisposable
    {
        internal sealed class Reply
        {
            public int StatusCode = 200;
            public string Body = "{}";
            public int DelayMilliseconds;
            public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();

            public Reply WithHeader(string name, string value)
            {
                Headers[name] = value;
                return this;
            }

            public Reply WithDelay(int milliseconds)
            {
                DelayMilliseconds = milliseconds;
                return this;
            }
        }

        private readonly HttpListener listener;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly Task serverTask;
        private readonly Dictionary<string, Queue<Reply>> routes = new Dictionary<string, Queue<Reply>>();
        private readonly Dictionary<string, string> lastBodies = new Dictionary<string, string>();
        private readonly List<string> requestLog = new List<string>();

        private MockTarotBackend(HttpListener listener, int port)
        {
            this.listener = listener;
            ApiBaseUrl = $"http://127.0.0.1:{port}/api/v1";
            serverTask = Task.Run(ServerLoop);
        }

        public string ApiBaseUrl { get; }

        public string[] RequestLog
        {
            get
            {
                lock (requestLog)
                {
                    return requestLog.ToArray();
                }
            }
        }

        public static MockTarotBackend Start()
        {
            var port = GetFreePort();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
            return new MockTarotBackend(listener, port);
        }

        public static Reply Json(int statusCode, string body)
        {
            return new Reply { StatusCode = statusCode, Body = body };
        }

        public void Script(string method, string path, params Reply[] replies)
        {
            lock (routes)
            {
                var key = Key(method, path);
                if (!routes.TryGetValue(key, out var queue))
                {
                    queue = new Queue<Reply>();
                    routes[key] = queue;
                }

                foreach (var reply in replies)
                {
                    queue.Enqueue(reply);
                }
            }
        }

        // Replaces a route's whole reply queue (Script only appends, and the last
        // reply repeats, so a fixture-wide reply could not otherwise be overridden).
        public void Replace(string method, string path, params Reply[] replies)
        {
            lock (routes)
            {
                routes[Key(method, path)] = new Queue<Reply>(replies);
            }
        }

        public int Count(string method, string path)
        {
            var key = Key(method, path);
            var count = 0;
            foreach (var entry in RequestLog)
            {
                if (entry == key)
                {
                    count++;
                }
            }

            return count;
        }

        public string LastBody(string method, string path)
        {
            lock (lastBodies)
            {
                return lastBodies.TryGetValue(Key(method, path), out var body) ? body : null;
            }
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

        private static string Key(string method, string path)
        {
            return $"{method.ToUpperInvariant()} {path}";
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

                _ = Task.Run(() => Handle(context));
            }
        }

        private async Task Handle(HttpListenerContext context)
        {
            var key = Key(context.Request.HttpMethod, context.Request.Url?.AbsolutePath ?? string.Empty);
            string body;
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                body = reader.ReadToEnd();
            }

            lock (requestLog)
            {
                requestLog.Add(key);
            }

            lock (lastBodies)
            {
                lastBodies[key] = body;
            }

            Reply reply;
            lock (routes)
            {
                if (routes.TryGetValue(key, out var queue) && queue.Count > 0)
                {
                    reply = queue.Count > 1 ? queue.Dequeue() : queue.Peek();
                }
                else
                {
                    reply = Json(404, "{\"detail\":\"not scripted\"}");
                }
            }

            try
            {
                if (reply.DelayMilliseconds > 0)
                {
                    await Task.Delay(reply.DelayMilliseconds);
                }

                var bytes = Encoding.UTF8.GetBytes(reply.Body ?? string.Empty);
                context.Response.StatusCode = reply.StatusCode;
                context.Response.ContentType = "application/json";
                foreach (var header in reply.Headers)
                {
                    context.Response.AddHeader(header.Key, header.Value);
                }

                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.Close();
            }
            catch (Exception)
            {
                // The client gave up (timeout tests) or the listener was stopped.
            }
        }

        internal static int GetFreePort()
        {
            var tcp = new TcpListener(IPAddress.Loopback, 0);
            tcp.Start();
            var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
            tcp.Stop();
            return port;
        }
    }

    /// <summary>Phase 66: response bodies shaped like the FastAPI schemas.</summary>
    internal static class MockTarotJson
    {
        public const string GuestLimitDetail =
            "{\"detail\":\"Guest daily reading limit reached. Please try again tomorrow.\"}";
        public const string AttemptsExhaustedDetail = "{\"detail\":\"Interpretation attempts exhausted\"}";
        public const string RecordNotFoundDetail = "{\"detail\":\"Record not found\"}";

        private static readonly string[] CardNames =
        {
            "愚者", "魔术师", "女祭司", "皇后", "皇帝", "教皇", "恋人", "战车", "力量", "隐者",
        };

        public static string Record(int id, int spreadId)
        {
            return "{\"id\":" + id + ",\"user_id\":7,\"spread_type_id\":" + spreadId
                + ",\"question\":\"此刻我最需要留意什么？\",\"question_type\":\"general\",\"status\":\"pending\","
                + "\"created_at\":\"2026-09-12T00:00:00Z\",\"completed_at\":null,\"is_favorite\":false,"
                + "\"user_rating\":0,\"user_notes\":\"\"}";
        }

        public static string Cards(int predictionId, int count)
        {
            var items = new string[count];
            for (var i = 0; i < count; i++)
            {
                var name = CardNames[i % CardNames.Length];
                var position = i + 1;
                items[i] = "{\"id\":" + (9000 + i) + ",\"prediction_id\":" + predictionId + ",\"tarot_card_id\":" + i
                    + ",\"position\":" + position + ",\"is_reversed\":" + (i == 2 ? "true" : "false")
                    + ",\"drawn_at\":\"2026-09-12T00:00:01Z\",\"tarot_card\":{\"id\":" + i + ",\"name_zh\":\"" + name
                    + "\",\"name_en\":\"Card " + i + "\",\"arcana\":\"major\",\"suit\":\"\",\"number\":" + i
                    + ",\"image_url\":\"\"},\"card_meaning\":{\"id\":" + i + ",\"name_zh\":\"" + name
                    + "\",\"name_en\":\"Card " + i + "\",\"is_reversed\":false,\"meaning\":\"测试牌义\","
                    + "\"keywords\":[\"测试\"],\"position\":" + position + ",\"position_name\":\"第" + position
                    + "位\",\"position_meaning\":\"测试牌位\"},\"position_name\":\"第" + position
                    + "位\",\"position_meaning\":\"测试牌位\"}";
            }

            return "[" + string.Join(",", items) + "]";
        }

        public static string Draw(int predictionId, int count)
        {
            return "{\"prediction_id\":" + predictionId + ",\"status\":\"success\",\"card_draws\":"
                + Cards(predictionId, count) + "}";
        }

        public static string Accepted(int predictionId)
        {
            return "{\"prediction_id\":" + predictionId + ",\"status\":\"processing\"}";
        }

        public static string Interpretation(int predictionId, string model)
        {
            return "{\"id\":" + (3000 + predictionId) + ",\"prediction_id\":" + predictionId
                + ",\"overall_interpretation\":\"整体解读来自测试桩。\",\"card_analysis\":\"牌面分析来自测试桩。\","
                + "\"relationship_analysis\":\"\",\"advice\":\"建议来自测试桩。\",\"warning\":\"提醒来自测试桩。\","
                + "\"summary\":\"概要来自测试桩。\",\"key_themes\":\"测试\",\"model_used\":\"" + model
                + "\",\"model_version\":\"test\",\"confidence_score\":0.9,\"generated_at\":\"2026-09-12T00:00:02Z\"}";
        }

        public static string Detail(int predictionId, string status, int cardCount, string interpretationJson)
        {
            return "{\"id\":" + predictionId + ",\"user_id\":7,\"spread_type_id\":2,"
                + "\"question\":\"此刻我最需要留意什么？\",\"question_type\":\"general\",\"status\":\"" + status
                + "\",\"created_at\":\"2026-09-12T00:00:00Z\",\"completed_at\":null,\"is_favorite\":false,"
                + "\"user_rating\":0,\"user_notes\":\"\",\"spread_type\":null,\"card_draws\":"
                + Cards(predictionId, cardCount) + ",\"interpretation\":" + (interpretationJson ?? "null") + "}";
        }

        public static string Token(string accessToken)
        {
            return "{\"access_token\":\"" + accessToken + "\",\"token_type\":\"bearer\"}";
        }

        public static string Spreads(params (int id, string name, int cardCount)[] spreads)
        {
            var items = new string[spreads.Length];
            for (var i = 0; i < spreads.Length; i++)
            {
                items[i] = "{\"id\":" + spreads[i].id + ",\"name\":\"" + spreads[i].name + "\",\"name_en\":\"Spread "
                    + spreads[i].id + "\",\"description\":\"\",\"card_count\":" + spreads[i].cardCount
                    + ",\"difficulty_level\":1,\"positions\":[],\"is_beginner_friendly\":true,\"usage_count\":0,"
                    + "\"suitable_for_love\":true,\"suitable_for_career\":true,\"suitable_for_finance\":true,"
                    + "\"suitable_for_health\":true,\"suitable_for_general\":true}";
            }

            return "[" + string.Join(",", items) + "]";
        }
    }
}

using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Network;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase4BackendIntegrationTests
    {
        [Test]
        public void ApiRoutesMatchUnityV1BackendScope()
        {
            Assert.That(ApiRoutes.Login, Is.EqualTo("/login"));
            Assert.That(ApiRoutes.UsersMe, Is.EqualTo("/users/me"));
            Assert.That(ApiRoutes.Refresh, Is.EqualTo("/refresh"));
            Assert.That(ApiRoutes.Logout, Is.EqualTo("/logout"));
            Assert.That(ApiRoutes.Spreads, Is.EqualTo("/spreads/"));
            Assert.That(ApiRoutes.SpreadDetail(7), Is.EqualTo("/spreads/7"));
            Assert.That(ApiRoutes.Records, Is.EqualTo("/records/"));
            Assert.That(ApiRoutes.RecordDraw(11), Is.EqualTo("/records/11/draw"));
            Assert.That(ApiRoutes.RecordCards(11), Is.EqualTo("/records/11/cards"));
            Assert.That(ApiRoutes.RecordInterpret(11), Is.EqualTo("/records/11/interpret"));
            Assert.That(ApiRoutes.RecordDetail(11), Is.EqualTo("/records/11"));
        }

        [Test]
        public void ApiClientExportsAuthCookieAndCsrfSession()
        {
            var owner = new GameObject("ApiClientTest");
            try
            {
                var client = owner.AddComponent<ApiClient>();
                client.BaseUrl = "http://localhost:8000/api/v1/";
                client.SetAccessToken("bearer-token");
                client.StoreCookieHeaderForTesting(
                    "access_token=cookie-token; Path=/; HttpOnly, refresh_token=refresh-token; Path=/; HttpOnly, csrf_token=csrf-123; Path=/");

                Assert.That(client.BuildUrl(ApiRoutes.RecordDetail(42)), Is.EqualTo("http://localhost:8000/api/v1/records/42"));
                Assert.That(client.HasSession, Is.True);
                Assert.That(client.CsrfToken, Is.EqualTo("csrf-123"));
                Assert.That(client.CookieHeader, Does.Contain("access_token=cookie-token"));
                Assert.That(client.CookieHeader, Does.Contain("refresh_token=refresh-token"));

                var session = client.ExportSession();
                Assert.That(session.accessToken, Is.EqualTo("bearer-token"));
                Assert.That(session.csrfToken, Is.EqualTo("csrf-123"));

                client.ClearSession();
                Assert.That(client.HasSession, Is.False);
                Assert.That(client.CookieHeader, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BackendPredictionDetailMapsToReadingSessionSnapshot()
        {
            var detail = new PredictionDetailResponse
            {
                id = 77,
                question = "How should I move next?",
                question_type = "career",
                spread_type_id = 2,
                spread_type = new SpreadSummary
                {
                    id = 2,
                    name = "Past / Present / Advice",
                    card_count = 3,
                },
                card_draws = new[]
                {
                    CreateCardDraw(1, "Past", "The Fool"),
                    CreateCardDraw(2, "Present", "The Star"),
                    CreateCardDraw(3, "Advice", "The Chariot"),
                },
                interpretation = new InterpretationResponse
                {
                    summary = "Keep a steady direction.",
                    overall_interpretation = "The cards point toward deliberate momentum.",
                    card_analysis = "Past, present, and advice are aligned.",
                    advice = "Choose one high-impact next step.",
                    warning = "Avoid scattering effort.",
                },
            };

            var session = ReadingSessionMapper.FromBackendDetail(detail);

            Assert.That(session.spreadId, Is.EqualTo(2));
            Assert.That(session.spreadName, Is.EqualTo("Past / Present / Advice"));
            Assert.That(session.cardCount, Is.EqualTo(3));
            Assert.That(session.question, Is.EqualTo("How should I move next?"));
            Assert.That(session.summary, Is.EqualTo("Keep a steady direction."));
            Assert.That(session.cardDraws[2].tarot_card.name_zh, Is.EqualTo("The Chariot"));
        }

        [Test]
        public void ScenesHavePhase4BackendWiring()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
            Assert.That(Object.FindFirstObjectByType<ApiClient>(), Is.Not.Null);

            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            Assert.That(room, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<ApiClient>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<BackendReadingService>(), Is.Not.Null);
            Assert.That(GetObjectReference(room, "apiClient"), Is.Not.Null);
            Assert.That(GetObjectReference(room, "backendReadingService"), Is.Not.Null);
            Assert.That(GetEnumValue(room, "backendMode"), Is.EqualTo((int)BackendIntegrationMode.BackendWithLocalFallback));
        }

        private static CardDrawData CreateCardDraw(int position, string positionName, string cardName)
        {
            return new CardDrawData
            {
                id = position,
                prediction_id = 77,
                tarot_card_id = position,
                position = position,
                position_name = positionName,
                position_meaning = $"Meaning for {positionName}",
                tarot_card = new TarotCardSimple
                {
                    id = position,
                    name_zh = cardName,
                    name_en = cardName,
                },
            };
        }

        private static Object GetObjectReference(Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName} on {target.GetType().Name}");
            return property.objectReferenceValue;
        }

        private static int GetEnumValue(Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName} on {target.GetType().Name}");
            return property.enumValueIndex;
        }
    }
}

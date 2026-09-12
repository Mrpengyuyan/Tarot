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

        // Phase 66: later tasks append tests above this line.
    }
}

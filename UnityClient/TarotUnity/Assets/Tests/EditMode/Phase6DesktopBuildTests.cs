using System.IO;
using NUnit.Framework;
using TarotUnity.Core;
using TarotUnity.Network;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase6DesktopBuildTests
    {
        private static readonly string[] RequiredBuildScenes =
        {
            "Assets/Scenes/Boot.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/ReadingRoom.unity",
            "Assets/Scenes/Result.unity",
        };

        [Test]
        public void DesktopRuntimeConfigDefaultsToLocalApiBase()
        {
            var config = DesktopRuntimeConfig.Resolve(
                _ => null,
                _ => false,
                _ => string.Empty,
                "/missing-streaming-assets",
                DesktopConfigLoader.ConfigFileName);

            Assert.That(config.BackendBaseUrl, Is.EqualTo("http://localhost:8000/api/v1"));
            Assert.That(config.RequestTimeoutSeconds, Is.EqualTo(15));
        }

        [Test]
        public void DesktopRuntimeConfigUsesFileAndEnvironmentOverride()
        {
            var fileJson = "{\"backendBaseUrl\":\"http://file-host:9000\",\"requestTimeoutSeconds\":9}";

            var config = DesktopRuntimeConfig.Resolve(
                key => key == DesktopConfigLoader.BackendUrlEnvironmentVariable
                    ? "https://api.example.com"
                    : null,
                _ => true,
                _ => fileJson,
                "/streaming-assets",
                DesktopConfigLoader.ConfigFileName);

            Assert.That(config.BackendBaseUrl, Is.EqualTo("https://api.example.com/api/v1"));
            Assert.That(config.RequestTimeoutSeconds, Is.EqualTo(9));
        }

        [Test]
        public void ApiClientAppliesDesktopConfigBaseUrlAndTimeout()
        {
            var owner = new GameObject("ApiClientConfigTest");
            try
            {
                var client = owner.AddComponent<ApiClient>();
                client.ApplyRuntimeConfig(new DesktopRuntimeConfig
                {
                    backendBaseUrl = "http://backend.internal:7777",
                    requestTimeoutSeconds = 4,
                });

                Assert.That(client.BaseUrl, Is.EqualTo("http://backend.internal:7777/api/v1"));
                Assert.That(client.RequestTimeoutSeconds, Is.EqualTo(4));
                Assert.That(client.BuildUrl("spreads/"), Is.EqualTo("http://backend.internal:7777/api/v1/spreads/"));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BootSceneHasDesktopConfigLoader()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");

            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.GetComponent<ApiClient>(), Is.Not.Null);
            Assert.That(bootstrap.GetComponent<DesktopConfigLoader>(), Is.Not.Null);
        }

        [Test]
        public void SceneFlowManagerRejectsUnknownSceneWithoutEnteringLoadingState()
        {
            var owner = new GameObject("SceneFlowManagerTest");
            try
            {
                var flow = owner.AddComponent<SceneFlowManager>();

                LogAssert.Expect(LogType.Warning, "Scene '999' is not a known scene or is missing from Build Settings.");
                var accepted = flow.LoadScene((GameSceneId)999);

                Assert.That(accepted, Is.False);
                Assert.That(flow.IsLoading, Is.False);
                Assert.That(flow.LastError, Does.Contain("not a known scene"));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void DesktopBuildSettingsAreReadyForPrototypeBuild()
        {
            CollectionAssert.AreEqual(RequiredBuildScenes, GetEnabledBuildScenePaths());
            Assert.That(PlayerSettings.productName, Is.EqualTo("Tarot Unity"));
            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("0.9.0"));
            Assert.That(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone), Is.EqualTo(ScriptingImplementation.Mono2x));
            Assert.That(UnityEditor.CrashReporting.CrashReportingSettings.enabled, Is.False);
            Assert.That(UnityEditor.CrashReporting.CrashReportingSettings.captureEditorExceptions, Is.False);
            Assert.That(UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled, Is.False);
            Assert.That(
                File.ReadAllText("ProjectSettings/UnityConnectSettings.asset"),
                Does.Contain("UnityConnectSettings:\n  m_ObjectHideFlags: 0\n  serializedVersion: 1\n  m_Enabled: 0"));
            Assert.That(File.Exists("Assets/StreamingAssets/tarot_desktop_config.json"), Is.True);
        }

        private static string[] GetEnabledBuildScenePaths()
        {
            var scenes = EditorBuildSettings.scenes;
            var paths = new System.Collections.Generic.List<string>();
            foreach (var scene in scenes)
            {
                if (scene.enabled)
                {
                    paths.Add(scene.path);
                }
            }

            return paths.ToArray();
        }
    }
}

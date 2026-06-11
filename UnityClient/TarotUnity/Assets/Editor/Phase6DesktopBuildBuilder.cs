using System;
using System.IO;
using System.Linq;
using TarotUnity.Core;
using TarotUnity.Network;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class Phase6DesktopBuildBuilder
    {
        public const string ProductName = "Tarot Unity";
        public const string BundleVersion = "0.6.0";
        public const string BuildRoot = "Builds/Desktop";
        public const string MacBuildPath = BuildRoot + "/macOS/Tarot Unity.app";
        public const string WindowsBuildPath = BuildRoot + "/Windows/TarotUnity.exe";

        private static readonly string[] BuildScenePaths =
        {
            "Assets/Scenes/Boot.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/ReadingRoom.unity",
            "Assets/Scenes/Result.unity",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 6 Desktop Setup")]
        public static void RunSetup()
        {
            EnsureStreamingAssetsConfig();
            EnsureBuildSettings();
            ApplyDesktopPlayerSettings();
            DisableCloudDiagnosticsForLocalPrototypeBuilds();
            EnhanceBootScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 6 desktop setup complete.");
        }

        [MenuItem("Tools/Tarot Unity/Build Desktop/macOS Prototype")]
        public static void BuildMacOS()
        {
            RunSetup();
            Build(BuildTarget.StandaloneOSX, MacBuildPath);
        }

        [MenuItem("Tools/Tarot Unity/Build Desktop/Windows Prototype")]
        public static void BuildWindows()
        {
            RunSetup();
            Build(BuildTarget.StandaloneWindows64, WindowsBuildPath);
        }

        private static void EnsureStreamingAssetsConfig()
        {
            if (!AssetDatabase.IsValidFolder("Assets/StreamingAssets"))
            {
                AssetDatabase.CreateFolder("Assets", "StreamingAssets");
            }

            var configPath = $"Assets/StreamingAssets/{DesktopConfigLoader.ConfigFileName}";
            if (!File.Exists(configPath))
            {
                File.WriteAllText(
                    configPath,
                    "{\n" +
                    "  \"backendBaseUrl\": \"http://localhost:8000/api/v1\",\n" +
                    "  \"requestTimeoutSeconds\": 15\n" +
                    "}\n");
            }
        }

        private static void EnsureBuildSettings()
        {
            EditorBuildSettings.scenes = BuildScenePaths
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        private static void ApplyDesktopPlayerSettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = BundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.maochuandou.tarotunity");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        }

        private static void DisableCloudDiagnosticsForLocalPrototypeBuilds()
        {
            UnityEditor.CrashReporting.CrashReportingSettings.enabled = false;
            UnityEditor.CrashReporting.CrashReportingSettings.captureEditorExceptions = false;
            UnityEditor.CrashReporting.CrashReportingSettings.logBufferSize = 0;
            UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled = false;
            DisableUnityConnectProjectServices();
        }

        private static void DisableUnityConnectProjectServices()
        {
            var settingsPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProjectSettings", "UnityConnectSettings.asset"));
            if (!File.Exists(settingsPath))
            {
                return;
            }

            var settings = File.ReadAllText(settingsPath);
            var disabled = settings.Replace(
                "UnityConnectSettings:\n  m_ObjectHideFlags: 0\n  serializedVersion: 1\n  m_Enabled: 1",
                "UnityConnectSettings:\n  m_ObjectHideFlags: 0\n  serializedVersion: 1\n  m_Enabled: 0");

            if (disabled != settings)
            {
                File.WriteAllText(settingsPath, disabled);
            }
        }

        private static void EnhanceBootScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");

            var bootstrapper = GameObject.Find("Bootstrapper") ?? new GameObject("Bootstrapper");
            if (bootstrapper.GetComponent<GameBootstrap>() == null)
            {
                bootstrapper.AddComponent<GameBootstrap>();
            }

            var apiClient = bootstrapper.GetComponent<ApiClient>();
            if (apiClient == null)
            {
                apiClient = bootstrapper.AddComponent<ApiClient>();
            }

            if (bootstrapper.GetComponent<DesktopConfigLoader>() == null)
            {
                bootstrapper.AddComponent<DesktopConfigLoader>();
            }

            EditorUtility.SetDirty(bootstrapper);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void Build(BuildTarget buildTarget, string locationPathName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(locationPathName) ?? BuildRoot);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = BuildScenePaths,
                target = buildTarget,
                locationPathName = locationPathName,
                options = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Desktop build failed for {buildTarget}: {report.summary.result} ({report.summary.totalErrors} errors)");
            }

            Debug.Log($"Desktop prototype build created at {locationPathName}");
        }
    }
}

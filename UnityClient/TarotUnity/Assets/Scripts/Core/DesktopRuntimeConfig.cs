using System;
using System.IO;
using UnityEngine;

namespace TarotUnity.Core
{
    [Serializable]
    public sealed class DesktopRuntimeConfig
    {
        public const string DefaultBackendBaseUrl = "http://localhost:8000/api/v1";
        public const int DefaultRequestTimeoutSeconds = 15;

        public string backendBaseUrl = DefaultBackendBaseUrl;
        public int requestTimeoutSeconds = DefaultRequestTimeoutSeconds;

        public string BackendBaseUrl => NormalizeBackendBaseUrl(backendBaseUrl);
        public int RequestTimeoutSeconds => requestTimeoutSeconds > 0
            ? requestTimeoutSeconds
            : DefaultRequestTimeoutSeconds;

        public static DesktopRuntimeConfig CreateDefault()
        {
            return new DesktopRuntimeConfig();
        }

        public static DesktopRuntimeConfig FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateDefault();
            }

            var config = JsonUtility.FromJson<DesktopRuntimeConfig>(json) ?? CreateDefault();
            config.backendBaseUrl = NormalizeBackendBaseUrl(config.backendBaseUrl);
            if (config.requestTimeoutSeconds <= 0)
            {
                config.requestTimeoutSeconds = DefaultRequestTimeoutSeconds;
            }

            return config;
        }

        public static DesktopRuntimeConfig Resolve(
            Func<string, string> environmentProvider,
            Func<string, bool> fileExists,
            Func<string, string> readFile,
            string streamingAssetsPath,
            string configFileName)
        {
            var config = CreateDefault();
            var configPath = Path.Combine(streamingAssetsPath ?? string.Empty, configFileName ?? string.Empty);

            if (fileExists != null && fileExists(configPath) && readFile != null)
            {
                config = FromJson(readFile(configPath));
            }

            var environmentUrl = environmentProvider?.Invoke(DesktopConfigLoader.BackendUrlEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentUrl))
            {
                config.backendBaseUrl = NormalizeBackendBaseUrl(environmentUrl);
            }

            return config;
        }

        public static string NormalizeBackendBaseUrl(string value)
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? DefaultBackendBaseUrl
                : value.Trim().TrimEnd('/');

            return normalized.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase)
                ? normalized
                : $"{normalized}/api/v1";
        }
    }
}

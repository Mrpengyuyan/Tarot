using System;
using System.IO;
using TarotUnity.Network;
using UnityEngine;

namespace TarotUnity.Core
{
    public sealed class DesktopConfigLoader : MonoBehaviour
    {
        public const string ConfigFileName = "tarot_desktop_config.json";
        public const string BackendUrlEnvironmentVariable = "TAROT_BACKEND_URL";

        [SerializeField] private ApiClient apiClient;
        [SerializeField] private string configFileName = ConfigFileName;

        public DesktopRuntimeConfig CurrentConfig { get; private set; }
        public string LastError { get; private set; } = string.Empty;

        private void Awake()
        {
            LoadAndApply();
        }

        public void LoadAndApply()
        {
            try
            {
                CurrentConfig = DesktopRuntimeConfig.Resolve(
                    Environment.GetEnvironmentVariable,
                    File.Exists,
                    File.ReadAllText,
                    Application.streamingAssetsPath,
                    configFileName);

                if (apiClient == null)
                {
                    apiClient = GetComponent<ApiClient>() ?? FindFirstObjectByType<ApiClient>();
                }

                apiClient?.ApplyRuntimeConfig(CurrentConfig);
                LastError = string.Empty;
            }
            catch (Exception exception)
            {
                CurrentConfig = DesktopRuntimeConfig.CreateDefault();
                LastError = exception.Message;
                Debug.LogWarning($"Desktop config load failed. Using defaults. {exception.Message}");

                if (apiClient == null)
                {
                    apiClient = GetComponent<ApiClient>() ?? FindFirstObjectByType<ApiClient>();
                }

                apiClient?.ApplyRuntimeConfig(CurrentConfig);
            }
        }
    }
}

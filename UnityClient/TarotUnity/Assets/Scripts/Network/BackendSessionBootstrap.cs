using System;
using System.Collections;
using UnityEngine;

namespace TarotUnity.Network
{
    public enum BackendSessionStatus
    {
        NotStarted,
        Connecting,
        Online,
        Offline,
    }

    public sealed class BackendSessionBootstrap : MonoBehaviour
    {
        [SerializeField] private ApiClient apiClient;
        [SerializeField] private bool requestGuestSessionOnStart = true;

        public event Action<BackendSessionStatus, string> StatusChanged;

        public BackendSessionStatus Status { get; private set; } = BackendSessionStatus.NotStarted;
        public string LastError { get; private set; } = string.Empty;
        public bool IsRequesting { get; private set; }
        public bool IsOnline => Status == BackendSessionStatus.Online;

        private void Start()
        {
            if (requestGuestSessionOnStart)
            {
                BeginGuestSession();
            }
        }

        public void BeginGuestSession()
        {
            if (IsRequesting)
            {
                return;
            }

            if (apiClient == null)
            {
                apiClient = FindFirstObjectByType<ApiClient>();
            }

            if (apiClient == null)
            {
                SetOffline("ApiClient is not available.");
                return;
            }

            if (apiClient.HasSession)
            {
                SetStatus(BackendSessionStatus.Online, string.Empty);
                return;
            }

            StartCoroutine(RequestGuestSessionRoutine());
        }

        private IEnumerator RequestGuestSessionRoutine()
        {
            IsRequesting = true;
            SetStatus(BackendSessionStatus.Connecting, string.Empty);

            TokenResponse token = null;
            string error = null;
            yield return apiClient.CreateGuestSession(
                value => token = value,
                value => error = value);

            IsRequesting = false;

            if (!string.IsNullOrWhiteSpace(error))
            {
                SetOffline(error);
                yield break;
            }

            if (token == null || string.IsNullOrWhiteSpace(token.access_token) || !apiClient.HasSession)
            {
                SetOffline("Guest session response was empty.");
                yield break;
            }

            SetStatus(BackendSessionStatus.Online, string.Empty);
        }

        private void SetOffline(string error)
        {
            SetStatus(BackendSessionStatus.Offline, error);
        }

        private void SetStatus(BackendSessionStatus status, string error)
        {
            Status = status;
            LastError = error ?? string.Empty;
            StatusChanged?.Invoke(Status, LastError);
        }
    }
}

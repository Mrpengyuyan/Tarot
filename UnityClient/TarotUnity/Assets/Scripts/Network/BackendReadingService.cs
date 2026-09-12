using System;
using System.Collections;
using TarotUnity.Data;
using UnityEngine;

namespace TarotUnity.Network
{
    public sealed class BackendReadingService : MonoBehaviour
    {
        [SerializeField] private ApiClient apiClient;

        // Phase 66: prefer the Boot client that holds the guest session (see
        // ApiClient.Shared); the scene reference is only a fallback.
        public ApiClient Client
        {
            get
            {
                if (ApiClient.Shared != null)
                {
                    return ApiClient.Shared;
                }

                if (apiClient == null)
                {
                    apiClient = FindFirstObjectByType<ApiClient>();
                }

                return apiClient;
            }
        }

        public bool CanCreateAuthenticatedReading => Client != null && Client.HasSession;

        public IEnumerator LoadSpreads(Action<SpreadSummary[]> onSuccess, Action<string> onError)
        {
            if (Client == null)
            {
                onError?.Invoke("ApiClient is not available.");
                yield break;
            }

            yield return Client.GetSpreads(onSuccess, onError);
        }

        // Phase 66: an online reading starts with record + draw + cards only. The
        // interpretation is generated in the background and fetched by the
        // persistent InterpretationPoller, so the table deals as soon as the cards
        // exist instead of waiting on the AI the way CompleteReading did.
        public IEnumerator StartReading(
            PredictionCreateRequest payload,
            Action<ReadingSessionSnapshot> onSuccess,
            Action<ApiError> onError)
        {
            var client = Client;
            if (client == null)
            {
                onError?.Invoke(ApiError.Local("ApiClient is not available."));
                yield break;
            }

            PredictionResponse prediction = null;
            DrawCardsResponse drawn = null;
            CardDrawData[] cards = null;
            ApiError error = null;

            yield return client.PostRecord(payload, value => prediction = value, value => error = value);
            if (error == null && (prediction == null || prediction.id <= 0))
            {
                error = new ApiError(200, ApiErrorKind.Unexpected, -1, "200: the record response had no id");
            }

            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            yield return client.PostDraw(prediction.id, value => drawn = value, value => error = value);
            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            yield return client.FetchRecordCards(prediction.id, value => cards = value, value => error = value);
            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            if (cards == null || cards.Length == 0)
            {
                cards = drawn?.card_draws;
            }

            var snapshot = ReadingSessionMapper.FromBackendStart(prediction, cards);
            if (snapshot.cardDraws.Length == 0)
            {
                onError?.Invoke(new ApiError(200, ApiErrorKind.Unexpected, -1, "200: the backend reading returned no cards"));
                yield break;
            }

            onSuccess?.Invoke(snapshot);
        }

        // Phase 66: recover a guest whose access token was rejected before any record
        // exists (spec 6.6 step 2): refresh first; if that fails, start a new guest.
        // A new guest is safe here only because nothing has been created yet.
        public IEnumerator RecoverSession(Action<bool> onDone)
        {
            var client = Client;
            if (client == null)
            {
                onDone?.Invoke(false);
                yield break;
            }

            var refreshed = false;
            yield return client.Refresh(
                token => refreshed = token != null && !string.IsNullOrWhiteSpace(token.access_token),
                _ => refreshed = false);
            if (refreshed)
            {
                onDone?.Invoke(true);
                yield break;
            }

            client.ClearSession();
            var created = false;
            yield return client.CreateGuestSession(
                token => created = token != null && !string.IsNullOrWhiteSpace(token.access_token),
                _ => created = false);
            onDone?.Invoke(created && client.HasAccessToken);
        }

        public IEnumerator RequestInterpretationAsync(
            int predictionId,
            Action<AsyncInterpretationResult> onSuccess,
            Action<ApiError> onError)
        {
            var client = Client;
            if (client == null)
            {
                onError?.Invoke(ApiError.Local("ApiClient is not available."));
                yield break;
            }

            yield return client.PostInterpretAsync(predictionId, onSuccess, onError);
        }

        public IEnumerator GetRecord(
            int predictionId,
            Action<PredictionDetailResponse> onSuccess,
            Action<ApiError> onError)
        {
            var client = Client;
            if (client == null)
            {
                onError?.Invoke(ApiError.Local("ApiClient is not available."));
                yield break;
            }

            yield return client.FetchRecordDetail(predictionId, onSuccess, onError);
        }
    }
}

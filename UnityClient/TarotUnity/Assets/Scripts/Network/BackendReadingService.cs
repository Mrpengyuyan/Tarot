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

        public IEnumerator CompleteReading(
            PredictionCreateRequest payload,
            Action<ReadingSessionSnapshot> onSuccess,
            Action<string> onError)
        {
            if (Client == null)
            {
                onError?.Invoke("ApiClient is not available.");
                yield break;
            }

            if (!Client.HasSession)
            {
                onError?.Invoke("Backend reading requires an authenticated ApiClient session.");
                yield break;
            }

            PredictionResponse prediction = null;
            DrawCardsResponse drawResponse = null;
            CardDrawData[] cardDraws = null;
            InterpretationResponse interpretation = null;
            PredictionDetailResponse detail = null;
            string error = null;

            yield return Client.CreateRecord(payload, value => prediction = value, value => error = value);
            if (HasError(error, onError) || prediction == null)
            {
                onError?.Invoke(error ?? "Backend did not return a prediction.");
                yield break;
            }

            yield return Client.DrawCards(prediction.id, value => drawResponse = value, value => error = value);
            if (HasError(error, onError))
            {
                yield break;
            }

            if (drawResponse != null && drawResponse.card_draws != null && drawResponse.card_draws.Length > 0)
            {
                cardDraws = drawResponse.card_draws;
            }

            yield return Client.GetRecordCards(prediction.id, value => cardDraws = value, value => error = value);
            if (HasError(error, onError))
            {
                yield break;
            }

            yield return Client.CreateInterpretation(prediction.id, value => interpretation = value, value => error = value);
            if (HasError(error, onError))
            {
                yield break;
            }

            yield return Client.GetRecord(prediction.id, value => detail = value, value => error = value);
            if (HasError(error, onError))
            {
                yield break;
            }

            var session = detail != null
                ? ReadingSessionMapper.FromBackendDetail(detail, cardDraws)
                : ReadingSessionMapper.FromBackendParts(prediction, null, cardDraws, interpretation);

            if (session == null)
            {
                onError?.Invoke("Backend reading did not produce a usable session.");
                yield break;
            }

            onSuccess?.Invoke(session);
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

        private static bool HasError(string error, Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return false;
            }

            onError?.Invoke(error);
            return true;
        }
    }
}

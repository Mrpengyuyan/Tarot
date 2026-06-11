using System;
using System.Collections;
using TarotUnity.Data;
using UnityEngine;

namespace TarotUnity.Network
{
    public sealed class BackendReadingService : MonoBehaviour
    {
        [SerializeField] private ApiClient apiClient;

        public ApiClient Client
        {
            get
            {
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

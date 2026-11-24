using System.Collections;
using TarotUnity.Core;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using UnityEngine;

namespace TarotUnity.UI
{
    public sealed partial class ReadingRoomController
    {
        private sealed class OnlineStartAttempt
        {
            public bool Done;
            public ReadingSessionSnapshot Session;
            public string OfflineMessage;
            public string RawError;
        }

        // Phase 66: the longest online start is nine requests (spread list, record,
        // draw, cards, refresh, guest session, then record, draw and cards again), each
        // bounded by the client's request timeout, plus a little slack.
        private float OnlineStartTimeoutSeconds()
        {
            var client = backendReadingService != null ? backendReadingService.Client : null;
            var perRequest = client != null
                ? client.RequestTimeoutSeconds
                : DesktopRuntimeConfig.DefaultRequestTimeoutSeconds;
            return perRequest * 9f + 15f;
        }

        private IEnumerator StartOnlineReadingRoutine(string question, OnlineStartAttempt attempt)
        {
            if (backendSpreads == null)
            {
                yield return LoadBackendSpreadsRoutine();
            }

            // Phase 66: spreads that could not be loaded mean the service is unreachable
            // (spec 7.3 row 1), not that this particular spread is unsupported.
            if (backendSpreads == null)
            {
                attempt.OfflineMessage = ReleaseUxCopy.OfflineBecauseNetwork;
                attempt.RawError = "backend spreads could not be loaded";
                Debug.Log($"ReadingRoom: online reading skipped - {attempt.RawError}");
                attempt.Done = true;
                yield break;
            }

            // Phase 66: only ask for a spread the backend really has with this card
            // count - a local spread id would name a different backend spread.
            var backendSpread = FindBackendSpread(selectedCardCount);
            if (backendSpread == null)
            {
                attempt.OfflineMessage = ReleaseUxCopy.OfflineBecauseSpread;
                attempt.RawError = $"no backend spread with {selectedCardCount} cards";
                Debug.Log($"ReadingRoom: online reading skipped - {attempt.RawError}");
                attempt.Done = true;
                yield break;
            }

            var payload = new PredictionCreateRequest
            {
                question = question,
                question_type = "general",
                spread_type_id = backendSpread.id,
            };

            ReadingSessionSnapshot session = null;
            ApiError error = null;
            yield return backendReadingService.StartReading(payload, value => session = value, value => error = value);

            // Spec 6.6 step 2: nothing exists yet, so a rejected token may refresh or
            // even become a new guest before one retry.
            if (error != null && error.Kind == ApiErrorKind.Unauthorized)
            {
                var recovered = false;
                yield return backendReadingService.RecoverSession(value => recovered = value);
                if (recovered)
                {
                    error = null;
                    session = null;
                    yield return backendReadingService.StartReading(payload, value => session = value, value => error = value);
                }
            }

            if (error == null && session != null && session.cardDraws.Length != selectedCardCount)
            {
                error = new ApiError(
                    200,
                    ApiErrorKind.Unexpected,
                    -1,
                    $"200: the backend dealt {session.cardDraws.Length} cards for a {selectedCardCount}-card spread");
            }

            if (error != null || session == null)
            {
                Debug.Log($"ReadingRoom: online reading unavailable - {error?.RawMessage}");
                attempt.OfflineMessage = ReleaseUxCopy.ForStartReadingFailure(error);
                attempt.RawError = error?.RawMessage;
                attempt.Session = null;
            }
            else
            {
                session.spreadId = backendSpread.id;
                session.spreadName = !string.IsNullOrWhiteSpace(backendSpread.name) ? backendSpread.name : selectedSpreadName;
                attempt.Session = session;
            }

            attempt.Done = true;
        }

        private void BeginInterpretation(ReadingSessionSnapshot session)
        {
            var poller = InterpretationPoller.Instance;
            if (poller == null)
            {
                // Without Boot (this scene run on its own) nothing survives into the
                // Result screen to fetch the interpretation, so use the offline text.
                InterpretationPoller.ApplyOffline(session);
                SetReleaseStatus(ReleaseUxCopy.OfflineBecauseUnavailable);
                return;
            }

            if (subscribedPoller != poller)
            {
                UnsubscribePoller();
                subscribedPoller = poller;
                poller.StateChanged += HandleInterpretationStateChanged;
            }

            poller.Begin(session);
        }

        private void HandleInterpretationStateChanged(ReadingSessionSnapshot session)
        {
            if (session == null || session != ReadingSessionStore.Current)
            {
                return;
            }

            if (session.source == ReadingSource.Offline)
            {
                SetReleaseStatus(ReleaseUxCopy.OfflineWarning);
                return;
            }

            switch (session.interpretationState)
            {
                case InterpretationState.Pending:
                    SetReleaseStatus(ReleaseUxCopy.InterpretationGenerating);
                    break;
                case InterpretationState.Ready:
                    SetReleaseStatus(ReleaseUxCopy.InterpretationReadyHint);
                    break;
                default:
                    SetReleaseStatus(session.failureMessage);
                    break;
            }
        }

        private void UnsubscribePoller()
        {
            if (subscribedPoller != null)
            {
                subscribedPoller.StateChanged -= HandleInterpretationStateChanged;
            }

            subscribedPoller = null;
        }

        private void EnsureBackendReferences()
        {
            if (ApiClient.Shared != null)
            {
                apiClient = ApiClient.Shared;
            }
            else if (apiClient == null)
            {
                apiClient = FindFirstObjectByType<ApiClient>();
            }

            if (backendReadingService == null)
            {
                backendReadingService = FindFirstObjectByType<BackendReadingService>();
            }
        }

        private IEnumerator LoadBackendSpreadsRoutine()
        {
            var loaded = default(SpreadSummary[]);
            var error = default(string);
            yield return backendReadingService.LoadSpreads(value => loaded = value, value => error = value);

            if (loaded != null && loaded.Length > 0)
            {
                backendSpreads = loaded;
                SelectSpread(selectedSpreadId, selectedCardCount, selectedSpreadName);
                SetReleaseStatus(ReleaseUxCopy.OnlineReady);
                yield break;
            }

            if (backendMode == BackendIntegrationMode.BackendOnly)
            {
                var message = ReleaseUxCopy.BackendOnlyFailure(error);
                SetStatus(message);
                SetReleaseStatus(message);
            }
        }

        private bool ShouldTryBackend()
        {
            if (UserPreferences.PreferOfflineMode && backendMode != BackendIntegrationMode.BackendOnly)
            {
                return false;
            }

            return backendMode != BackendIntegrationMode.LocalSimulation
                && backendReadingService != null
                && backendReadingService.CanCreateAuthenticatedReading;
        }

        private bool ShouldLoadBackendSpreads()
        {
            if (backendMode == BackendIntegrationMode.LocalSimulation || backendReadingService == null)
            {
                return false;
            }

            if (UserPreferences.PreferOfflineMode && backendMode != BackendIntegrationMode.BackendOnly)
            {
                return false;
            }

            return backendMode == BackendIntegrationMode.BackendOnly
                || backendReadingService.CanCreateAuthenticatedReading;
        }

        private SpreadCatalog ResolveCatalog()
        {
            if (spreadCatalog == null)
            {
                spreadCatalog = Resources.Load<SpreadCatalog>(SpreadCatalog.ResourcePath);
            }

            return spreadCatalog;
        }

        private CardDrawData[] CreateLocalDraws()
        {
            var catalog = ResolveCatalog();
            var def = catalog != null ? catalog.GetByCardCount(selectedCardCount) : null;
            // Phase 75: a real draw from the shuffled 78-card deck - no repeats, half reversed.
            return LocalReadingSimulator.DrawFromDeck(
                selectedCardCount, def != null ? def.positionNames : null, def != null ? def.positionMeanings : null);
        }

        private SpreadSummary FindBackendSpread(int cardCount)
        {
            if (backendSpreads == null)
            {
                return null;
            }

            foreach (var spread in backendSpreads)
            {
                if (spread != null && spread.card_count == cardCount)
                {
                    return spread;
                }
            }

            return null;
        }
    }
}

using System;
using System.Collections;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEngine;

namespace TarotUnity.Network
{
    /// <summary>
    /// Phase 66: fetches the background AI interpretation for the current online
    /// reading. It lives on the persistent Boot object (GameBootstrap), so leaving the
    /// reading room for the Result screen never interrupts it. It mutates the snapshot
    /// it was given in place - ReadingSessionStore.Current keeps pointing at the same
    /// object - and raises StateChanged on every state change.
    /// </summary>
    public sealed class InterpretationPoller : MonoBehaviour
    {
        // Backend AI_INTERPRETATION_STALE_SECONDS (300) plus 30 s of slack.
        public const float TotalDeadlineSeconds = 330f;
        public const int MaxConsecutiveNetworkErrors = 3;

        [SerializeField] private ApiClient apiClient;
        [SerializeField] private float delayScale = 1f;
        [SerializeField] private float deadlineSeconds = TotalDeadlineSeconds;

        // Bumped by Begin, Stop, UseOffline and OnDestroy. A running poll re-checks its
        // own id after every yield and exits once it is stale, so an in-flight request
        // finishes and disposes normally instead of being cut off mid-coroutine.
        private int runId;

        public static InterpretationPoller Instance { get; private set; }

        public event Action<ReadingSessionSnapshot> StateChanged;

        public ReadingSessionSnapshot Current { get; private set; }

        public bool IsPolling { get; private set; }

        private ApiClient Client
        {
            get
            {
                if (apiClient != null)
                {
                    return apiClient;
                }

                return ApiClient.Shared != null ? ApiClient.Shared : GetComponent<ApiClient>();
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            runId++;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Configure(ApiClient client, float pollDelayScale, float totalDeadlineSeconds)
        {
            apiClient = client;
            delayScale = Mathf.Max(0f, pollDelayScale);
            deadlineSeconds = totalDeadlineSeconds;
        }

        public static float PollDelaySeconds(int attemptIndex)
        {
            if (attemptIndex < 2)
            {
                return 2f;
            }

            return attemptIndex < 4 ? 3f : 5f;
        }

        public void Begin(ReadingSessionSnapshot snapshot)
        {
            runId++;
            IsPolling = false;
            Current = snapshot;
            if (snapshot == null || snapshot.source != ReadingSource.Online || snapshot.predictionId <= 0)
            {
                return;
            }

            snapshot.interpretationState = InterpretationState.Pending;
            snapshot.failureMessage = string.Empty;
            snapshot.canRetry = false;
            IsPolling = true;
            var run = runId;
            StateChanged?.Invoke(snapshot);
            StartCoroutine(PollRoutine(run, snapshot));
        }

        public void Retry()
        {
            if (Current != null)
            {
                Begin(Current);
            }
        }

        public void Stop()
        {
            runId++;
            IsPolling = false;
        }

        public void UseOffline()
        {
            runId++;
            IsPolling = false;
            if (Current == null)
            {
                return;
            }

            ApplyOffline(Current);
            StateChanged?.Invoke(Current);
        }

        public static void ApplyFailure(ReadingSessionSnapshot snapshot, InterpretationFailure failure)
        {
            if (snapshot == null)
            {
                return;
            }

            snapshot.interpretationState = InterpretationState.Failed;
            snapshot.failureMessage = ReleaseUxCopy.ForInterpretationFailure(failure);
            snapshot.canRetry = ReleaseUxCopy.CanRetry(failure);
        }

        // Keeps the dealt cards and the record id; only the text becomes the local
        // reading, and the snapshot never switches back to online afterwards.
        public static void ApplyOffline(ReadingSessionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            var offline = LocalReadingSimulator.CreateSession(
                snapshot.spreadId,
                snapshot.spreadName,
                snapshot.question,
                snapshot.questionType,
                snapshot.cardDraws);
            snapshot.summary = offline.summary;
            snapshot.overallInterpretation = offline.overallInterpretation;
            snapshot.cardAnalysis = offline.cardAnalysis;
            snapshot.advice = offline.advice;
            snapshot.warning = offline.warning;
            snapshot.source = ReadingSource.Offline;
            snapshot.interpretationState = InterpretationState.Ready;
            snapshot.modelUsed = string.Empty;
            snapshot.failureMessage = string.Empty;
            snapshot.canRetry = false;
        }

        private IEnumerator PollRoutine(int run, ReadingSessionSnapshot snapshot)
        {
            var client = Client;
            if (client == null)
            {
                Fail(run, snapshot, InterpretationFailure.ConnectionLost, "no ApiClient");
                yield break;
            }

            var startedAt = Time.realtimeSinceStartup;
            var accepted = false;
            var attempt = 0;
            var networkErrors = 0;
            var refreshedThisRequest = false;

            while (run == runId)
            {
                if (Time.realtimeSinceStartup - startedAt >= deadlineSeconds)
                {
                    Fail(run, snapshot, InterpretationFailure.TimedOut, "deadline reached");
                    yield break;
                }

                ApiError error = null;
                if (!accepted)
                {
                    AsyncInterpretationResult result = null;
                    yield return client.PostInterpretAsync(snapshot.predictionId, value => result = value, value => error = value);
                    if (run != runId)
                    {
                        yield break;
                    }

                    if (error == null)
                    {
                        if (result.outcome == AsyncInterpretationOutcome.AlreadyReady)
                        {
                            Complete(run, snapshot, result.interpretation);
                            yield break;
                        }

                        accepted = true;
                    }
                    else if (error.Kind == ApiErrorKind.RateLimited)
                    {
                        Fail(run, snapshot, InterpretationFailure.AttemptsExhausted, error.RawMessage);
                        yield break;
                    }
                    else if (error.Kind != ApiErrorKind.Unauthorized && !error.IsTransient)
                    {
                        Fail(run, snapshot, InterpretationFailure.Unavailable, error.RawMessage);
                        yield break;
                    }
                }
                else
                {
                    PredictionDetailResponse detail = null;
                    yield return client.FetchRecordDetail(snapshot.predictionId, value => detail = value, value => error = value);
                    if (run != runId)
                    {
                        yield break;
                    }

                    if (error == null)
                    {
                        // Plan 1 contract: a stored interpretation means done, whatever the status says.
                        if (ReadingSessionMapper.HasInterpretation(detail))
                        {
                            Complete(run, snapshot, detail.interpretation);
                            yield break;
                        }

                        if (detail != null && string.Equals(detail.status, "failed", StringComparison.OrdinalIgnoreCase))
                        {
                            Fail(run, snapshot, InterpretationFailure.BackendFailed, "status failed");
                            yield break;
                        }
                    }
                    else if (error.Kind == ApiErrorKind.NotFound || error.Kind == ApiErrorKind.BadRequest)
                    {
                        Fail(run, snapshot, InterpretationFailure.Unavailable, error.RawMessage);
                        yield break;
                    }
                }

                if (error == null)
                {
                    networkErrors = 0;
                    refreshedThisRequest = false;
                }
                else if (error.Kind == ApiErrorKind.Unauthorized)
                {
                    if (refreshedThisRequest)
                    {
                        Fail(run, snapshot, InterpretationFailure.SessionExpired, error.RawMessage);
                        yield break;
                    }

                    refreshedThisRequest = true;
                    var refreshed = false;
                    yield return client.Refresh(
                        token => refreshed = token != null && !string.IsNullOrWhiteSpace(token.access_token),
                        _ => refreshed = false);
                    if (run != runId)
                    {
                        yield break;
                    }

                    if (!refreshed)
                    {
                        Fail(run, snapshot, InterpretationFailure.SessionExpired, error.RawMessage);
                        yield break;
                    }

                    // Repeat the same request straight away with the refreshed token.
                    continue;
                }
                else
                {
                    networkErrors++;
                    Debug.Log($"InterpretationPoller: prediction {snapshot.predictionId} request failed " +
                        $"({networkErrors}/{MaxConsecutiveNetworkErrors}): {error.RawMessage}");
                    if (networkErrors >= MaxConsecutiveNetworkErrors)
                    {
                        Fail(run, snapshot, InterpretationFailure.ConnectionLost, error.RawMessage);
                        yield break;
                    }
                }

                var remaining = deadlineSeconds - (Time.realtimeSinceStartup - startedAt);
                var delay = Mathf.Min(PollDelaySeconds(attempt) * delayScale, Mathf.Max(0f, remaining));
                attempt++;
                if (delay > 0f)
                {
                    yield return new WaitForSecondsRealtime(delay);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private void Complete(int run, ReadingSessionSnapshot snapshot, InterpretationResponse interpretation)
        {
            if (run != runId)
            {
                return;
            }

            ReadingSessionMapper.ApplyInterpretation(snapshot, interpretation);
            IsPolling = false;
            StateChanged?.Invoke(snapshot);
        }

        private void Fail(int run, ReadingSessionSnapshot snapshot, InterpretationFailure failure, string detail)
        {
            if (run != runId)
            {
                return;
            }

            ApplyFailure(snapshot, failure);
            IsPolling = false;
            Debug.Log($"InterpretationPoller: prediction {snapshot.predictionId} stopped with {failure} ({detail}).");
            StateChanged?.Invoke(snapshot);
        }
    }
}

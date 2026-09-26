using System.Collections;
using System.Collections.Generic;
using TarotUnity.Core;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    public sealed class ReadingRoomController : MonoBehaviour
    {
        [SerializeField] private ReadingFlowController flowController;
        [SerializeField] private DeckController deckController;
        [SerializeField] private Button oneCardButton;
        [SerializeField] private Button threeCardButton;
        [SerializeField] private Button celticCrossButton;
        [SerializeField] private Button drawButton;
        [SerializeField] private Button revealResultButton;
        [SerializeField] private SpreadCatalog spreadCatalog;
        // Phase 50: the ReadingRoom migrates to TMP SDF like the menu did. The
        // question field needed the TMP_InputField swap the menu never had, and
        // the three status readouts become TMP_Text. Both expose the same .text
        // API the controller already used, so only the field types change here.
        [SerializeField] private TMP_InputField questionInput;
        [SerializeField] private TMP_Text spreadStatusText;
        [SerializeField] private TMP_Text flowStatusText;
        [SerializeField] private TMP_Text releaseStatusText;
        [SerializeField] private CameraChoreographyController cameraChoreography;
        [SerializeField] private RitualFeedbackController ritualFeedback;
        [SerializeField] private DeckShuffleChoreographer deckShuffle;
        [SerializeField] private RitualRhythmDirector rhythmDirector;
        [SerializeField] private ApiClient apiClient;
        [SerializeField] private BackendReadingService backendReadingService;
        [SerializeField] private BackendIntegrationMode backendMode = BackendIntegrationMode.LocalSimulation;

        private int selectedSpreadId = 1;
        private int selectedCardCount = 1;
        private string selectedSpreadName = "单牌抽取";
        private bool drawInProgress;
        private SpreadSummary[] backendSpreads;
        private InterpretationPoller subscribedPoller;

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

        private void Awake()
        {
            oneCardButton?.onClick.AddListener(SelectOneCard);
            threeCardButton?.onClick.AddListener(SelectThreeCards);
            celticCrossButton?.onClick.AddListener(SelectCelticCross);
            drawButton?.onClick.AddListener(BeginDraw);
            revealResultButton?.onClick.AddListener(LoadResult);

            if (flowController != null)
            {
                flowController.StateChanged += HandleStateChanged;
            }

            if (deckController != null)
            {
                deckController.CardDealt += HandleCardDealt;
            }
        }

        private void Start()
        {
            EnsureBackendReferences();
            SelectOneCard();
            SetResultButtonVisible(false);
            SetStatus("先选一个牌阵，写下你的问题，再抽牌。");
            SetReleaseStatus(ReleaseUxCopy.LocalModeReady);
            cameraChoreography?.PlayOpening();

            if (ShouldLoadBackendSpreads())
            {
                StartCoroutine(LoadBackendSpreadsRoutine());
            }
        }

        private void OnDestroy()
        {
            oneCardButton?.onClick.RemoveListener(SelectOneCard);
            threeCardButton?.onClick.RemoveListener(SelectThreeCards);
            celticCrossButton?.onClick.RemoveListener(SelectCelticCross);
            drawButton?.onClick.RemoveListener(BeginDraw);
            revealResultButton?.onClick.RemoveListener(LoadResult);

            if (flowController != null)
            {
                flowController.StateChanged -= HandleStateChanged;
            }

            if (deckController != null)
            {
                deckController.CardDealt -= HandleCardDealt;
            }

            UnsubscribePoller();
        }

        private void SelectOneCard()
        {
            SelectFromCatalog(1, 1, "单牌抽取");
        }

        private void SelectThreeCards()
        {
            SelectFromCatalog(2, 3, "过去现在未来");
        }

        private void SelectCelticCross()
        {
            SelectFromCatalog(3, 10, "凯尔特十字");
        }

        // The catalog holds the spread names the offline reading also uses, so both screens agree;
        // the literal is only the fallback for a missing or half-built catalog.
        private void SelectFromCatalog(int fallbackSpreadId, int cardCount, string fallbackName)
        {
            var def = ResolveCatalog()?.GetByCardCount(cardCount);
            var name = def != null && !string.IsNullOrWhiteSpace(def.displayName) ? def.displayName : fallbackName;
            SelectSpread(def != null ? def.spreadId : fallbackSpreadId, cardCount, name);
        }

        private void SelectSpread(int spreadId, int cardCount, string spreadName)
        {
            if (drawInProgress)
            {
                return;
            }

            var backendSpread = FindBackendSpread(cardCount);
            if (backendSpread != null)
            {
                spreadId = backendSpread.id;
                cardCount = backendSpread.card_count;
                spreadName = !string.IsNullOrWhiteSpace(backendSpread.name)
                    ? backendSpread.name
                    : spreadName;
            }

            selectedSpreadId = spreadId;
            selectedCardCount = cardCount;
            selectedSpreadName = spreadName;
            ApplySpreadEmphasis(cardCount);

            flowController?.EnterSpreadSelect();
            flowController?.SelectSpread(spreadId, cardCount);
            cameraChoreography?.FocusSpread(cardCount);
            ritualFeedback?.PlayCue(PresentationCueId.SpreadSelected);

            if (spreadStatusText != null)
            {
                spreadStatusText.text = $"{spreadName} · {cardCount} 张";
            }
        }

        private void BeginDraw()
        {
            if (drawInProgress)
            {
                return;
            }

            StartCoroutine(DrawRoutine());
        }

        private IEnumerator DrawRoutine()
        {
            drawInProgress = true;
            SetResultButtonVisible(false);
            SetDrawControls(false);

            var question = string.IsNullOrWhiteSpace(questionInput?.text)
                ? ReleaseUxCopy.DefaultQuestion
                : questionInput.text.Trim();

            flowController?.SetQuestion(question, "general");
            flowController?.BeginShuffle();
            cameraChoreography?.FocusDeck();
            ritualFeedback?.PlayCue(PresentationCueId.ShuffleStarted, deckController != null ? deckController.transform : null);
            deckShuffle?.Play();
            SetStatus(ReleaseUxCopy.FlowShuffling);

            // Phase 66: the online start (record + draw + cards, no AI) runs while the
            // shuffle plays. The interpretation is generated in the background and
            // fetched by the persistent InterpretationPoller once the deal begins.
            var attempt = new OnlineStartAttempt();
            if (ShouldTryBackend())
            {
                StartCoroutine(StartOnlineReadingRoutine(question, attempt));
            }
            else
            {
                attempt.Done = true;
            }

            yield return new WaitForSeconds(rhythmDirector != null
                ? rhythmDirector.ResolvePause(PresentationCueId.ShuffleStarted)
                : 0.8f);

            var onlineStartTimeoutSeconds = OnlineStartTimeoutSeconds();
            var giveUpAt = Time.realtimeSinceStartup + onlineStartTimeoutSeconds;
            yield return new WaitUntil(() => attempt.Done || Time.realtimeSinceStartup > giveUpAt);
            if (!attempt.Done)
            {
                attempt.Session = null;
                attempt.OfflineMessage = ReleaseUxCopy.OfflineBecauseNetwork;
                attempt.RawError = $"the online start did not finish within {onlineStartTimeoutSeconds} s";
                Debug.Log($"ReadingRoom: {attempt.RawError}");
            }

            var session = attempt.Session;
            if (session == null && attempt.OfflineMessage != null && backendMode == BackendIntegrationMode.BackendOnly)
            {
                var message = ReleaseUxCopy.BackendOnlyFailure(attempt.RawError);
                SetStatus(message);
                SetReleaseStatus(message);
                SetDrawControls(true);
                drawInProgress = false;
                yield break;
            }

            if (session == null)
            {
                if (attempt.OfflineMessage != null)
                {
                    SetReleaseStatus(attempt.OfflineMessage);
                }

                session = LocalReadingSimulator.CreateSession(
                    selectedSpreadId,
                    selectedSpreadName,
                    question,
                    "general",
                    CreateLocalDraws());
            }

            ReadingSessionStore.Save(session);
            if (session.source == ReadingSource.Online)
            {
                BeginInterpretation(session);
            }

            flowController?.BeginDeal();
            SetStatus(ReleaseUxCopy.FlowDealing);

            var draws = session.cardDraws ?? CreateLocalDraws();
            var slots = flowController != null ? flowController.GetSelectedSpreadSlots() : new List<Transform>();
            if (deckController != null)
            {
                yield return deckController.DealCards(draws, slots);
                if (rhythmDirector != null && rhythmDirector.DealSettleSeconds > 0f)
                {
                    yield return new WaitForSeconds(rhythmDirector.DealSettleSeconds);
                }

                WireActiveCards();
            }

            flowController?.WaitForCardFlips();
            cameraChoreography?.FocusSpread(selectedCardCount);
            SetStatus(ReleaseUxCopy.FlowFlipPrompt);
            drawInProgress = false;
        }

        private sealed class OnlineStartAttempt
        {
            public bool Done;
            public ReadingSessionSnapshot Session;
            public string OfflineMessage;
            public string RawError;
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

        private void HandleCardDealt(CardView card)
        {
            ritualFeedback?.PlayCue(PresentationCueId.CardDealt, card != null ? card.transform : null);
        }

        private void WireActiveCards()
        {
            if (deckController == null)
            {
                return;
            }

            foreach (var card in deckController.ActiveCards)
            {
                if (card == null)
                {
                    continue;
                }

                var clickHandler = card.GetComponent<CardClickHandler>();
                if (clickHandler == null)
                {
                    clickHandler = card.gameObject.AddComponent<CardClickHandler>();
                }

                clickHandler.Clicked += HandleCardClicked;
                card.FaceChanged += HandleCardFaceChanged;
            }
        }

        private void HandleCardClicked(CardView card)
        {
            if (card == null || card.IsFaceUp || flowController == null || flowController.State != ReadingFlowState.WaitingForFlip)
            {
                return;
            }

            var flipController = card.GetComponent<CardFlipController>();
            if (flipController == null)
            {
                flipController = card.gameObject.AddComponent<CardFlipController>();
            }

            flipController.Flip(card);
        }

        private void HandleCardFaceChanged(CardView card, bool faceUp)
        {
            if (faceUp)
            {
                flowController?.RegisterCardFlipped(card);
            }
        }

        private void HandleStateChanged(ReadingFlowState state)
        {
            if (state == ReadingFlowState.ResultReady)
            {
                StartCoroutine(ResultReadyRoutine());
            }
        }

        private IEnumerator ResultReadyRoutine()
        {
            SetStatus(ReleaseUxCopy.FlowAllRevealed);
            cameraChoreography?.FocusResult();
            ritualFeedback?.PlayCue(PresentationCueId.ResultReady);
            SetResultButtonVisible(true);

            if (rhythmDirector != null && rhythmDirector.ResultBreathSeconds > 0f)
            {
                yield return new WaitForSeconds(rhythmDirector.ResultBreathSeconds);
            }

            SetStatus(ReleaseUxCopy.FlowResultReady);
        }

        private void LoadResult()
        {
            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.LoadScene(GameSceneId.Result);
                return;
            }

            SceneManager.LoadScene(GameSceneId.Result.ToString());
        }

        /// <summary>
        /// Phase 69: the chosen spread's button wears card stock, the others glass. Called on
        /// every selection, including a switch while the question is being written.
        /// </summary>
        public void ApplySpreadEmphasis(int cardCount)
        {
            SetEmphasis(oneCardButton, cardCount == 1);
            SetEmphasis(threeCardButton, cardCount == 3);
            SetEmphasis(celticCrossButton, cardCount == 10);
        }

        private static void SetEmphasis(Button button, bool on)
        {
            var skin = button != null ? button.GetComponent<UiSkinState>() : null;
            if (skin != null)
            {
                skin.SetEmphasis(on);
            }
        }

        private void SetDrawControls(bool enabled)
        {
            if (oneCardButton != null)
            {
                oneCardButton.interactable = enabled;
            }

            if (threeCardButton != null)
            {
                threeCardButton.interactable = enabled;
            }

            if (celticCrossButton != null)
            {
                celticCrossButton.interactable = enabled;
            }

            if (drawButton != null)
            {
                drawButton.interactable = enabled;
            }
        }

        private void SetResultButtonVisible(bool visible)
        {
            if (revealResultButton != null)
            {
                revealResultButton.gameObject.SetActive(visible);
            }
        }

        private void SetStatus(string text)
        {
            if (flowStatusText != null)
            {
                flowStatusText.text = text;
            }
        }

        private void SetReleaseStatus(string text)
        {
            if (releaseStatusText != null)
            {
                releaseStatusText.text = text;
            }
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
            var def = ResolveCatalog()?.GetByCardCount(selectedCardCount);
            return LocalReadingSimulator.CreatePlaceholderDraws(
                selectedCardCount, def?.positionNames, def?.positionMeanings);
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

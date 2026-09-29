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

        [Header("Phase72 Draw ritual")]
        [SerializeField] private SpreadFanController spreadFan;
        [SerializeField] private RitualStepIndicator stepIndicator;
        [Tooltip("Hidden while the player picks from the fan - the dock sits over the fan's table.")]
        [SerializeField] private CanvasGroup[] pickHiddenUi = System.Array.Empty<CanvasGroup>();

        private int selectedSpreadId = 1;
        private int selectedCardCount = 1;
        private string selectedSpreadName = "单牌抽取";
        private bool drawInProgress;
        private System.Exception drawFault;

        // Phase 74: the last pick's flight carries the camera to the spread and gathers the fan.
        [Tooltip("Seconds the camera arrives after the last picked card lands.")]
        [SerializeField] private float pickCameraLagSeconds = 0.35f;
        private bool followLastPick;
        private bool cameraFollowedPick;
        private Coroutine gatherRoutine;
        private Transform pickDeckOrigin;
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
            if (oneCardButton != null)
            {
                oneCardButton.onClick.AddListener(SelectOneCard);
            }

            if (threeCardButton != null)
            {
                threeCardButton.onClick.AddListener(SelectThreeCards);
            }

            if (celticCrossButton != null)
            {
                celticCrossButton.onClick.AddListener(SelectCelticCross);
            }

            if (drawButton != null)
            {
                drawButton.onClick.AddListener(BeginDraw);
            }

            if (revealResultButton != null)
            {
                revealResultButton.onClick.AddListener(LoadResult);
            }

            if (flowController != null)
            {
                flowController.StateChanged += HandleStateChanged;
            }

            if (deckController != null)
            {
                deckController.CardDealt += HandleCardDealt;
                deckController.CardHovering += HandleCardHovering;
                deckController.CardFlightStarted += HandleCardFlightStarted;
            }
        }

        private void Start()
        {
            EnsureBackendReferences();
            SelectOneCard();
            SetResultButtonVisible(false);
            SetStatus("先选一个牌阵，写下你的问题，再抽牌。");
            SetReleaseStatus(ReleaseUxCopy.LocalModeReady);
            if (cameraChoreography != null)
            {
                cameraChoreography.PlayOpening();
            }

            if (ShouldLoadBackendSpreads())
            {
                StartCoroutine(LoadBackendSpreadsRoutine());
            }
        }

        private void OnDestroy()
        {
            if (oneCardButton != null)
            {
                oneCardButton.onClick.RemoveListener(SelectOneCard);
            }

            if (threeCardButton != null)
            {
                threeCardButton.onClick.RemoveListener(SelectThreeCards);
            }

            if (celticCrossButton != null)
            {
                celticCrossButton.onClick.RemoveListener(SelectCelticCross);
            }

            if (drawButton != null)
            {
                drawButton.onClick.RemoveListener(BeginDraw);
            }

            if (revealResultButton != null)
            {
                revealResultButton.onClick.RemoveListener(LoadResult);
            }

            if (flowController != null)
            {
                flowController.StateChanged -= HandleStateChanged;
            }

            if (deckController != null)
            {
                deckController.CardDealt -= HandleCardDealt;
                deckController.CardHovering -= HandleCardHovering;
                deckController.CardFlightStarted -= HandleCardFlightStarted;
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
            var catalog = ResolveCatalog();
            var def = catalog != null ? catalog.GetByCardCount(cardCount) : null;
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

            if (flowController != null)
            {
                flowController.EnterSpreadSelect();
            }

            if (flowController != null)
            {
                flowController.SelectSpread(spreadId, cardCount);
            }

            if (cameraChoreography != null)
            {
                cameraChoreography.FocusSpread(cardCount);
            }

            if (ritualFeedback != null)
            {
                ritualFeedback.PlayCue(PresentationCueId.SpreadSelected);
            }

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
            drawFault = null;
            followLastPick = false;
            cameraFollowedPick = false;
            SetResultButtonVisible(false);
            SetDrawControls(false);

            var question = questionInput == null || string.IsNullOrWhiteSpace(questionInput.text)
                ? ReleaseUxCopy.DefaultQuestion
                : questionInput.text.Trim();

            // Phase 72 review: explicit Unity null checks (not `?.`) so a destroyed reference
            // is skipped instead of called.
            if (flowController != null)
            {
                flowController.SetQuestion(question, "general");
                flowController.BeginShuffle();
            }

            if (cameraChoreography != null)
            {
                cameraChoreography.FocusDeck();
            }

            if (ritualFeedback != null)
            {
                ritualFeedback.PlayCue(PresentationCueId.ShuffleStarted, deckController != null ? deckController.transform : null);
            }

            if (deckShuffle != null)
            {
                deckShuffle.Play();
            }

            // Phase 73 review: the fan's 78 cards are made a few per frame while the shuffle plays.
            var deckOrigin = deckShuffle != null ? deckShuffle.transform : deckController != null ? deckController.transform : transform;
            if (FanCanDeal(selectedCardCount))
            {
                StartCoroutine(spreadFan.Prepare(deckOrigin));
            }

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

            // Phase 72: wait for the shuffle itself, not a fixed breath.
            while (deckShuffle != null && deckShuffle.IsPlaying)
            {
                yield return null;
            }

            // The fan opens at once - the online start keeps running behind the picks.
            if (flowController != null)
            {
                flowController.BeginDeal();
            }

            if (deckController != null)
            {
                deckController.Clear();
            }

            var slots = flowController != null ? flowController.GetSelectedSpreadSlots() : new List<Transform>();
            if (FanCanDeal(slots.Count))
            {
                if (cameraChoreography != null)
                {
                    cameraChoreography.FocusDraw(selectedCardCount);
                }

                SetPickUiHidden(true);
                yield return Guarded(PickFromFan(slots, deckOrigin));
                SetPickUiHidden(false);
                if (drawFault != null)
                {
                    yield return RecoverFromDrawFault(deckOrigin);
                    yield break;
                }
            }
            else
            {
                Debug.LogError("ReadingRoom: the spread fan is missing, inactive or unwired - dealing the cards automatically instead.");
            }

            SetStatus(ReleaseUxCopy.FlowReadingTheCards);

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
                if (deckController != null)
                {
                    yield return deckController.ReturnDealtCards();
                }

                if (flowController != null)
                {
                    flowController.AbortDraw();
                }

                if (cameraChoreography != null)
                {
                    cameraChoreography.PlayOpening();
                }

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

            var draws = session.cardDraws ?? CreateLocalDraws();
            if (deckController != null)
            {
                if (deckController.ActiveCards.Count == 0)
                {
                    // No fan (see the error above): the pre-Phase 72 automatic deal.
                    yield return deckController.DealCards(draws, slots);
                }
                else
                {
                    deckController.BindDealtCards(draws);
                }

                if (rhythmDirector != null && rhythmDirector.DealSettleSeconds > 0f)
                {
                    yield return new WaitForSeconds(rhythmDirector.DealSettleSeconds);
                }

                WireActiveCards();
            }

            if (flowController != null)
            {
                flowController.WaitForCardFlips();
            }

            // Phase 74: when the last pick already carried the camera here, don't restart the move.
            if (cameraChoreography != null && !cameraFollowedPick)
            {
                cameraChoreography.FocusSpread(selectedCardCount);
            }

            SetStatus(ReleaseUxCopy.FlowFlipPrompt);
            drawInProgress = false;
        }

        /// <summary>Phase 72 review: the fan must be there, awake, wired and big enough, or the draw deals automatically.</summary>
        private bool FanCanDeal(int cardCount)
        {
            return spreadFan != null && deckController != null && spreadFan.CanSpread(cardCount);
        }

        /// <summary>Phase 72: spread the fan, let the player pick, gather the rest.</summary>
        private IEnumerator PickFromFan(IList<Transform> slots, Transform deckOrigin)
        {
            pickDeckOrigin = deckOrigin;
            yield return spreadFan.Spread(deckOrigin);
            FocusSocket(0);
            SetStatus(ReleaseUxCopy.FlowPickPrompt(slots.Count, slots.Count));
            yield return spreadFan.PickCards(slots.Count, (card, index) => DeliverPick(card, index, slots));
            FocusSocket(-1);
            if (gatherRoutine == null && spreadFan.FanCards.Count > 0)
            {
                // No flight carried the gather (a delivery without one): gather now.
                yield return spreadFan.Gather(deckOrigin);
            }

            while (gatherRoutine != null)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Phase 72 review: an exception inside the pick (fan, flight, a cue handler) used to stop
        /// the draw with the dock hidden and the draw locked. Runs a routine - and every routine it
        /// yields - catching the first exception into <see cref="drawFault"/> and stopping there.
        /// </summary>
        private IEnumerator Guarded(IEnumerator routine)
        {
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext())
                    {
                        yield break;
                    }

                    current = routine.Current;
                }
                catch (System.Exception exception)
                {
                    drawFault = exception;
                    Debug.LogException(exception);
                    yield break;
                }

                if (current is IEnumerator nested)
                {
                    yield return Guarded(nested);
                    if (drawFault != null)
                    {
                        yield break;
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }

        /// <summary>Phase 72 review: after a fault mid-pick, clear the table and give the player the draw back.</summary>
        private IEnumerator RecoverFromDrawFault(Transform deckOrigin)
        {
            if (gatherRoutine != null)
            {
                StopCoroutine(gatherRoutine);
                gatherRoutine = null;
            }

            if (spreadFan != null)
            {
                spreadFan.Abandon();
            }

            if (deckController != null)
            {
                yield return Guarded(deckController.ReturnDealtCards());
                deckController.Clear();
            }

            FocusSocket(-1);
            if (flowController != null)
            {
                flowController.AbortDraw();
            }

            if (cameraChoreography != null)
            {
                cameraChoreography.PlayOpening();
            }

            SetStatus(ReleaseUxCopy.FlowDrawInterrupted);
            SetDrawControls(true);
            drawInProgress = false;
        }

        /// <summary>Phase 72: one picked card flies to its slot; then the next slot lights and the prompt counts down.</summary>
        private IEnumerator DeliverPick(CardView card, int index, IList<Transform> slots)
        {
            if (index >= slots.Count)
            {
                yield break;
            }

            followLastPick = index == slots.Count - 1;
            yield return deckController.DealPickedCard(card, slots[index]);
            followLastPick = false;
            var next = index + 1;
            FocusSocket(next < slots.Count ? next : -1);
            var remaining = slots.Count - next;
            if (remaining > 0)
            {
                SetStatus(ReleaseUxCopy.FlowPickPrompt(remaining, slots.Count));
            }
        }

        private void FocusSocket(int index)
        {
            if (stepIndicator != null)
            {
                stepIndicator.FocusSocket(index);
            }
        }

        /// <summary>Phase 72: the dock sits over the fan's table, so it steps aside while the player picks.</summary>
        private void SetPickUiHidden(bool hidden)
        {
            foreach (var group in pickHiddenUi)
            {
                if (group == null)
                {
                    continue;
                }

                group.alpha = hidden ? 0f : 1f;
                group.interactable = !hidden;
                group.blocksRaycasts = !hidden;
            }
        }

        private void HandleCardHovering(CardView card)
        {
            if (stepIndicator != null)
            {
                stepIndicator.FlashFocusedSocket();
            }
        }

        /// <summary>
        /// Phase 74: the last pick's flight takes the camera down to the spread with it (arriving
        /// just after the card lands), and the rest of the fan gathers home while it flies, so the
        /// pick flows into the flip without a wait and a cut.
        /// </summary>
        private void HandleCardFlightStarted(CardView card, float seconds)
        {
            if (!followLastPick)
            {
                return;
            }

            followLastPick = false;
            if (cameraChoreography != null)
            {
                cameraChoreography.FocusSpread(selectedCardCount, seconds + pickCameraLagSeconds);
                cameraFollowedPick = true;
            }

            if (spreadFan != null && gatherRoutine == null)
            {
                gatherRoutine = StartCoroutine(GatherBehindLastPick());
            }
        }

        private IEnumerator GatherBehindLastPick()
        {
            yield return Guarded(spreadFan.Gather(pickDeckOrigin));
            gatherRoutine = null;
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
            if (ritualFeedback != null)
            {
                ritualFeedback.PlayCue(PresentationCueId.CardDealt, card != null ? card.transform : null);
            }
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
                if (flowController != null)
                {
                    flowController.RegisterCardFlipped(card);
                }
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
            if (cameraChoreography != null)
            {
                cameraChoreography.FocusResult();
            }

            if (ritualFeedback != null)
            {
                ritualFeedback.PlayCue(PresentationCueId.ResultReady);
            }

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

            // Phase 69: card stock marks the next action; a draw button that cannot be pressed drops to glass.
            SetEmphasis(drawButton, enabled);
        }

        private void SetResultButtonVisible(bool visible)
        {
            if (revealResultButton != null)
            {
                revealResultButton.gameObject.SetActive(visible);
            }

            // Phase 70: 揭示结果 shares 洗牌抽取's slot; the draw cannot be pressed again this round.
            if (drawButton != null)
            {
                drawButton.gameObject.SetActive(!visible);
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
            var catalog = ResolveCatalog();
            var def = catalog != null ? catalog.GetByCardCount(selectedCardCount) : null;
            return LocalReadingSimulator.CreatePlaceholderDraws(
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

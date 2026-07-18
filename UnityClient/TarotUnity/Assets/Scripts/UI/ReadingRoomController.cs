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
        [SerializeField] private Button drawButton;
        [SerializeField] private Button revealResultButton;
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
        [SerializeField] private RitualRhythmDirector rhythmDirector;
        [SerializeField] private ApiClient apiClient;
        [SerializeField] private BackendReadingService backendReadingService;
        [SerializeField] private BackendIntegrationMode backendMode = BackendIntegrationMode.LocalSimulation;

        private int selectedSpreadId = 1;
        private int selectedCardCount = 1;
        private string selectedSpreadName = "One Card Focus";
        private bool drawInProgress;
        private SpreadSummary[] backendSpreads;

        private void Awake()
        {
            oneCardButton?.onClick.AddListener(SelectOneCard);
            threeCardButton?.onClick.AddListener(SelectThreeCards);
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
            SetStatus("Choose a spread, ask a question, then draw.");
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
        }

        private void SelectOneCard()
        {
            SelectSpread(1, 1, "One Card Focus");
        }

        private void SelectThreeCards()
        {
            SelectSpread(2, 3, "Past / Present / Advice");
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

            flowController?.EnterSpreadSelect();
            flowController?.SelectSpread(spreadId, cardCount);
            cameraChoreography?.FocusSpread(cardCount);
            ritualFeedback?.PlayCue(PresentationCueId.SpreadSelected);

            if (spreadStatusText != null)
            {
                spreadStatusText.text = $"{spreadName} - {cardCount} card{(cardCount == 1 ? string.Empty : "s")}";
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
                ? "What should I notice now?"
                : questionInput.text.Trim();

            flowController?.SetQuestion(question, "general");
            flowController?.BeginShuffle();
            cameraChoreography?.FocusDeck();
            ritualFeedback?.PlayCue(PresentationCueId.ShuffleStarted, deckController != null ? deckController.transform : null);
            SetStatus("Shuffling...");

            var session = default(ReadingSessionSnapshot);
            var backendError = default(string);
            if (ShouldTryBackend())
            {
                SetStatus("Creating backend reading...");
                yield return backendReadingService.CompleteReading(
                    flowController != null
                        ? flowController.BuildCreateRecordPayload()
                        : new PredictionCreateRequest
                        {
                            question = question,
                            question_type = "general",
                            spread_type_id = selectedSpreadId,
                        },
                    value => session = value,
                    value => backendError = value);

                if (session == null && backendMode == BackendIntegrationMode.BackendOnly)
                {
                    var message = ReleaseUxCopy.BackendOnlyFailure(backendError);
                    SetStatus(message);
                    SetReleaseStatus(message);
                    SetDrawControls(true);
                    drawInProgress = false;
                    yield break;
                }

                if (session == null)
                {
                    var message = ReleaseUxCopy.BackendFallback(backendError);
                    SetStatus(message);
                    SetReleaseStatus(message);
                }
            }

            yield return new WaitForSeconds(rhythmDirector != null
                ? rhythmDirector.ResolvePause(PresentationCueId.ShuffleStarted)
                : 0.8f);

            if (session == null)
            {
                var localDraws = LocalReadingSimulator.CreatePlaceholderDraws(selectedCardCount);
                session = LocalReadingSimulator.CreateSession(
                    selectedSpreadId,
                    selectedSpreadName,
                    question,
                    "general",
                    localDraws);
            }

            var draws = session.cardDraws ?? LocalReadingSimulator.CreatePlaceholderDraws(selectedCardCount);
            ReadingSessionStore.Save(session);

            flowController?.BeginDeal();
            SetStatus("Dealing cards...");

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
            SetStatus("Click each card to flip it.");
            drawInProgress = false;
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
            SetStatus("The reading is almost ready.");
            cameraChoreography?.FocusResult();
            ritualFeedback?.PlayCue(PresentationCueId.ResultReady);
            SetResultButtonVisible(true);

            if (rhythmDirector != null && rhythmDirector.ResultBreathSeconds > 0f)
            {
                yield return new WaitForSeconds(rhythmDirector.ResultBreathSeconds);
            }

            SetStatus("The reading is ready.");
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
            if (apiClient == null)
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
                SetStatus("Backend spreads loaded.");
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

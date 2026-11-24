using System.Collections;
using TarotUnity.Core;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TarotUnity.UI
{
    public sealed partial class ReadingRoomController
    {
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

                var flip = card.GetComponent<CardFlipController>();
                if (flip != null) flip.ApplyRitualPresentation();

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
            if (card == null || flowController == null || AnyCardIsFlipping() || (cardInspection != null && cardInspection.IsOpen))
            {
                return;
            }

            if (card.IsFaceUp)
            {
                if (cardInspection != null && (flowController.State == ReadingFlowState.WaitingForFlip
                    || flowController.State == ReadingFlowState.ResultReady))
                {
                    cardInspection.Show(card, false);
                }

                return;
            }

            if (flowController.State != ReadingFlowState.WaitingForFlip)
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

        private bool AnyCardIsFlipping()
        {
            if (deckController != null)
            {
                foreach (var card in deckController.ActiveCards)
                {
                    var flip = card != null ? card.GetComponent<CardFlipController>() : null;
                    if (flip != null && flip.IsFlipping)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void HandleCardFaceChanged(CardView card, bool faceUp)
        {
            if (faceUp)
            {
                StartCoroutine(RegisterAfterInspection(card));
            }
        }

        private IEnumerator RegisterAfterInspection(CardView card)
        {
            var flip = card.GetComponent<CardFlipController>();
            while ((flip != null && flip.IsFlipping) || (cardInspection != null && cardInspection.IsOpen))
            {
                yield return null;
            }

            if (card != null && card.IsFaceUp && flowController != null)
            {
                flowController.RegisterCardFlipped(card);
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

        private void HandleQuestionSubmit(string _)
        {
            if (IsOverlayModalOpen() || drawInProgress)
            {
                return;
            }

            if (drawButton != null && drawButton.gameObject.activeInHierarchy && drawButton.interactable)
            {
                BeginDraw();
            }
        }

        /// <summary>Flips the next unrevealed dealt card when in WaitingForFlip state.</summary>
        public bool TryFlipNextCard()
        {
            if (flowController == null || flowController.State != ReadingFlowState.WaitingForFlip
                || deckController == null || AnyCardIsFlipping() || IsOverlayModalOpen())
            {
                return false;
            }

            foreach (var card in deckController.ActiveCards)
            {
                if (card != null && !card.IsFaceUp)
                {
                    HandleCardClicked(card);
                    return true;
                }
            }

            return false;
        }

        private bool IsOverlayModalOpen()
        {
            if (cardInspection != null && cardInspection.IsOpen)
            {
                return true;
            }

            var settings = FindFirstObjectByType<RitualSettingsOverlay>();
            if (settings != null && settings.IsOpen)
            {
                return true;
            }

            var history = FindFirstObjectByType<RitualHistoryOverlay>();
            return history != null && history.IsOpen;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || IsOverlayModalOpen())
            {
                return;
            }

            var inputFocused = questionInput != null && questionInput.isFocused;
            if (inputFocused)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    questionInput.DeactivateInputField();
                }
                else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    HandleQuestionSubmit(questionInput.text);
                }

                return;
            }

            var spacePressed = keyboard.spaceKey.wasPressedThisFrame;
            var enterPressed = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            if (!spacePressed && !enterPressed)
            {
                return;
            }

            if (spacePressed && flowController != null && flowController.State == ReadingFlowState.WaitingForFlip)
            {
                TryFlipNextCard();
                return;
            }

            if (flowController != null && flowController.State == ReadingFlowState.ResultReady
                && revealResultButton != null && revealResultButton.gameObject.activeInHierarchy
                && revealResultButton.interactable)
            {
                LoadResult();
            }
        }
    }
}

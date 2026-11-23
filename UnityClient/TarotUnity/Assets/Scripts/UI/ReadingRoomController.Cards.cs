using System.Collections;
using TarotUnity.Core;
using TarotUnity.Gameplay;
using UnityEngine;
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
    }
}

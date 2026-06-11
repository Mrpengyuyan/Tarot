using System;
using System.Collections.Generic;
using TarotUnity.Data;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    public enum ReadingFlowState
    {
        MainMenu,
        SpreadSelect,
        QuestionInput,
        ReadyToDraw,
        Shuffling,
        Drawing,
        WaitingForFlip,
        ResultReady,
        Error
    }

    public sealed class ReadingFlowController : MonoBehaviour
    {
        [SerializeField] private DeckController deckController;
        [SerializeField] private SpreadLayoutController spreadLayoutController;

        private readonly HashSet<CardView> flippedCards = new();

        public event Action<ReadingFlowState> StateChanged;

        public ReadingFlowState State { get; private set; } = ReadingFlowState.MainMenu;
        public int SelectedSpreadId { get; private set; }
        public int SelectedSpreadCardCount { get; private set; }
        public string Question { get; private set; }
        public string QuestionType { get; private set; } = "general";

        private int expectedFlipCount;

        public void EnterSpreadSelect()
        {
            SetState(ReadingFlowState.SpreadSelect);
        }

        public void SelectSpread(int spreadId, int cardCount)
        {
            SelectedSpreadId = spreadId;
            SelectedSpreadCardCount = cardCount;
            expectedFlipCount = Mathf.Max(0, cardCount);
            SetState(ReadingFlowState.QuestionInput);
        }

        public void SetQuestion(string question, string questionType)
        {
            Question = question?.Trim();
            QuestionType = string.IsNullOrWhiteSpace(questionType) ? "general" : questionType;

            if (!string.IsNullOrWhiteSpace(Question) && SelectedSpreadId > 0)
            {
                SetState(ReadingFlowState.ReadyToDraw);
            }
        }

        public void BeginShuffle()
        {
            if (State != ReadingFlowState.ReadyToDraw)
            {
                return;
            }

            flippedCards.Clear();
            expectedFlipCount = Mathf.Max(expectedFlipCount, SelectedSpreadCardCount);
            SetState(ReadingFlowState.Shuffling);
        }

        public void BeginDeal()
        {
            if (State != ReadingFlowState.Shuffling)
            {
                return;
            }

            SetState(ReadingFlowState.Drawing);
        }

        public void WaitForCardFlips()
        {
            if (State == ReadingFlowState.Drawing)
            {
                SetState(ReadingFlowState.WaitingForFlip);
            }
        }

        public void RegisterCardFlipped(CardView card)
        {
            if (card == null)
            {
                return;
            }

            flippedCards.Add(card);

            var activeCardCount = deckController != null ? deckController.ActiveCards.Count : 0;
            var targetFlipCount = activeCardCount > 0 ? activeCardCount : expectedFlipCount;

            if (targetFlipCount > 0 && flippedCards.Count >= targetFlipCount)
            {
                SetState(ReadingFlowState.ResultReady);
            }
        }

        public List<Transform> GetSelectedSpreadSlots()
        {
            return spreadLayoutController == null
                ? new List<Transform>()
                : spreadLayoutController.GetSlots(SelectedSpreadCardCount);
        }

        public PredictionCreateRequest BuildCreateRecordPayload()
        {
            return new PredictionCreateRequest
            {
                question = Question,
                question_type = QuestionType,
                spread_type_id = SelectedSpreadId,
            };
        }

        private void SetState(ReadingFlowState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            State = nextState;
            StateChanged?.Invoke(State);
        }
    }
}

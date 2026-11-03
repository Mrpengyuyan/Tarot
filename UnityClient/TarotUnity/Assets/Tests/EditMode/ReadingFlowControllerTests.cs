using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class ReadingFlowControllerTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\r\n")]
        [TestCase("\u3000\u00a0")]
        public void ClearingAValidQuestionRevokesReadiness(string question)
        {
            var owner = new GameObject("RequiredQuestionFlow");
            try
            {
                var flow = owner.AddComponent<ReadingFlowController>();
                flow.SelectSpread(2, 3);
                flow.SetQuestion("What comes next?", "general");
                Assert.That(flow.State, Is.EqualTo(ReadingFlowState.ReadyToDraw));
                flow.SetQuestion(question, "general");
                flow.BeginShuffle();
                Assert.That(flow.State, Is.EqualTo(ReadingFlowState.QuestionInput));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ATrimmedPlayerQuestionCanStartTheShuffle()
        {
            var owner = new GameObject("RequiredQuestionFlow");
            try
            {
                var flow = owner.AddComponent<ReadingFlowController>();
                flow.SelectSpread(1, 1);
                flow.SetQuestion("  My own question?  ", "general");
                flow.BeginShuffle();
                Assert.That(flow.Question, Is.EqualTo("My own question?"));
                Assert.That(flow.State, Is.EqualTo(ReadingFlowState.Shuffling));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void RegisterCardFlippedMovesToResultReadyAfterSelectedSpreadCardCount()
        {
            var flowObject = new GameObject("ReadingFlow");
            var flow = flowObject.AddComponent<ReadingFlowController>();

            flow.EnterSpreadSelect();
            flow.SelectSpread(spreadId: 2, cardCount: 3);
            flow.SetQuestion("What should I notice today?", "general");
            flow.BeginShuffle();
            flow.BeginDeal();
            flow.WaitForCardFlips();

            for (var i = 0; i < 3; i++)
            {
                var cardObject = new GameObject($"Card {i + 1}");
                var card = cardObject.AddComponent<CardView>();
                flow.RegisterCardFlipped(card);
            }

            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.ResultReady));
        }
    }
}


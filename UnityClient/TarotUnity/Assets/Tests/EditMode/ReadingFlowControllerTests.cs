using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class ReadingFlowControllerTests
    {
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


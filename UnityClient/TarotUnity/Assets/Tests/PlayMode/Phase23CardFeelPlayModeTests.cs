using System.Collections;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase23CardFeelPlayModeTests
    {
        private GameObject cardObject;

        [TearDown]
        public void TearDown()
        {
            if (cardObject != null)
            {
                Object.Destroy(cardObject);
            }
        }

        [UnityTest]
        public IEnumerator HoverLiftsAndTiltsTowardPointer()
        {
            var hover = CreateHoverCard();

            hover.HoverEnter();
            hover.HoverMove(new Vector3(0.2f, 0f, 0.3f));
            yield return new WaitForSeconds(0.3f);

            Assert.That(cardObject.transform.localPosition.y, Is.GreaterThan(0.02f),
                "Hover should lift the card");
            Assert.That(Quaternion.Angle(cardObject.transform.localRotation, Quaternion.identity), Is.GreaterThan(1f),
                "Hover should tilt the card toward the pointer");
        }

        [UnityTest]
        public IEnumerator HoverExitSettlesBackToRest()
        {
            var hover = CreateHoverCard();

            hover.HoverEnter();
            hover.HoverMove(new Vector3(0.2f, 0f, 0.3f));
            yield return new WaitForSeconds(0.25f);

            hover.HoverExit();
            yield return new WaitForSeconds(0.5f);

            Assert.That(cardObject.transform.localPosition.y, Is.LessThan(0.01f),
                "Card should settle back down after hover ends");
            Assert.That(Quaternion.Angle(cardObject.transform.localRotation, Quaternion.identity), Is.LessThan(0.5f),
                "Card should level out after hover ends");
        }

        [UnityTest]
        public IEnumerator SuspendBlocksHoverAndRestoresRestPose()
        {
            var hover = CreateHoverCard();

            hover.HoverEnter();
            hover.HoverMove(new Vector3(0.2f, 0f, 0.3f));
            yield return new WaitForSeconds(0.25f);

            hover.Suspend();

            Assert.That(cardObject.transform.localPosition.y, Is.LessThan(0.001f),
                "Suspend must restore the rest pose immediately");

            hover.HoverEnter();
            yield return new WaitForSeconds(0.2f);

            Assert.That(cardObject.transform.localPosition.y, Is.LessThan(0.001f),
                "Suspended cards must ignore new hover attempts");
            Assert.That(hover.IsHovering, Is.False);
        }

        private CardHoverTiltController CreateHoverCard()
        {
            cardObject = new GameObject("Phase23_TestCard");
            cardObject.transform.localPosition = Vector3.zero;
            cardObject.transform.localRotation = Quaternion.identity;
            return cardObject.AddComponent<CardHoverTiltController>();
        }
    }
}

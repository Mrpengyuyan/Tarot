using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 72 final-review follow-ups: the draw must not soft-lock when the fan is missing or
    /// inactive, must recover when something throws mid-pick, and must not touch destroyed
    /// references through `?.`.
    /// </summary>
    public sealed class Phase72RobustnessTests
    {
        private ReadingRoomController room;
        private ReadingFlowController flow;
        private DeckController deck;

        private IEnumerator LoadRoom()
        {
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            yield return null;
            room = Object.FindFirstObjectByType<ReadingRoomController>();
            flow = Object.FindFirstObjectByType<ReadingFlowController>();
            deck = Object.FindFirstObjectByType<DeckController>();
        }

        [UnityTest]
        public IEnumerator AMissingFanFallsBackToTheAutomaticDealAndSaysSo()
        {
            yield return LoadRoom();
            Set("spreadFan", null);
            LogAssert.Expect(LogType.Error, new Regex("fan"));

            Get<Button>("threeCardButton").onClick.Invoke();
            Get<Button>("drawButton").onClick.Invoke();
            yield return Until(() => flow.State == ReadingFlowState.WaitingForFlip, "the draw still reaches the flip");
            Assert.That(deck.ActiveCards.Count, Is.EqualTo(3));
            Assert.That(deck.ActiveCards.All(c => c.DrawData != null), Is.True);
        }

        [UnityTest]
        public IEnumerator AnInactiveFanFallsBackToTheAutomaticDealAndSaysSo()
        {
            yield return LoadRoom();
            Object.FindFirstObjectByType<SpreadFanController>().gameObject.SetActive(false);
            LogAssert.Expect(LogType.Error, new Regex("fan"));

            Get<Button>("oneCardButton").onClick.Invoke();
            Get<Button>("drawButton").onClick.Invoke();
            yield return Until(() => flow.State == ReadingFlowState.WaitingForFlip, "the draw still reaches the flip");
            Assert.That(deck.ActiveCards.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AnErrorMidPickBringsTheRoomBack()
        {
            yield return LoadRoom();
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            deck.CardHovering += _ => throw new InvalidOperationException("boom mid-pick");
            LogAssert.Expect(LogType.Exception, new Regex("boom mid-pick"));

            Get<Button>("threeCardButton").onClick.Invoke();
            Get<Button>("drawButton").onClick.Invoke();
            yield return Until(() => fan.AcceptingPicks, "the fan opens");
            fan.RequestPick(fan.FanCards[4]);
            yield return Until(() => Get<Button>("drawButton").interactable, "the draw comes back");

            Assert.That(Get<CanvasGroup[]>("pickHiddenUi").All(g => g.alpha > 0.99f && g.blocksRaycasts), Is.True, "the dock is back");
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.ReadyToDraw));
            Assert.That(deck.ActiveCards.Count, Is.EqualTo(0));
            Assert.That(fan.FanCards.Count, Is.EqualTo(0));
            Assert.That(fan.AcceptingPicks, Is.False);
            Assert.That(Get<TarotUnity.UI.RitualStepIndicator>("stepIndicator").FocusedSocket, Is.EqualTo(-1));
        }

        [UnityTest]
        public IEnumerator DestroyedCameraAndStepBarAreSkippedNotCalled()
        {
            yield return LoadRoom();
            Object.Destroy(Object.FindFirstObjectByType<CameraChoreographyController>());
            Object.Destroy(Object.FindFirstObjectByType<RitualStepIndicator>(FindObjectsInactive.Include));
            yield return null;

            Get<Button>("oneCardButton").onClick.Invoke();
            Get<Button>("drawButton").onClick.Invoke();
            yield return DrawRitualTestDriver.PickAll();
            yield return Until(() => flow.State == ReadingFlowState.WaitingForFlip, "the draw still reaches the flip");
        }

        private T Get<T>(string name) where T : class
        {
            var field = typeof(ReadingRoomController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return field.GetValue(room) as T;
        }

        private void Set(string name, object value)
        {
            typeof(ReadingRoomController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(room, value);
        }

        private static IEnumerator Until(Func<bool> condition, string message, float seconds = 30f)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), message);
                yield return null;
            }
        }
    }
}

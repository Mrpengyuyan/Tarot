using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 36 performance probe: drives the real three-card vertical slice and samples
    /// main-thread frame time and managed allocations in each scene state (reading room
    /// idle with breathing camera + aura motion + particles, the flip sequence, and the
    /// result reveal). The numbers are logged as a report; assertions are generous sanity
    /// bounds only, so the probe documents performance without becoming a flaky gate.
    /// Editor/batch numbers are indicative for CPU and GC - GPU cost needs a real player
    /// build (see PHASE36 doc for the Metal HUD workflow).
    /// </summary>
    public sealed class Phase36PerformanceProbeTests
    {
        private const int IdleSampleFrames = 120;

        [UnityTest]
        public IEnumerator ThreeCardSliceFrameAndAllocationProbe()
        {
            ReadingSessionStore.Clear();

            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "ReadingRoom",
                "Expected ReadingRoom to load.");

            var room = UnityEngine.Object.FindFirstObjectByType<TarotUnity.UI.ReadingRoomController>();
            var flow = UnityEngine.Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = UnityEngine.Object.FindFirstObjectByType<DeckController>();
            Assert.That(room, Is.Not.Null);
            Assert.That(flow, Is.Not.Null);
            Assert.That(deck, Is.Not.Null);

            GetField<Button>(room, "threeCardButton").onClick.Invoke();
            GetField<InputField>(room, "questionInput").text = "Phase 36 performance probe";
            GetField<Button>(room, "drawButton").onClick.Invoke();

            var dealSamples = new List<float>();
            yield return SampleWhile(
                () => flow.State != ReadingFlowState.WaitingForFlip,
                dealSamples,
                "Expected the deal to finish.");

            var idleSamples = new List<float>();
            var idleAllocs = new List<long>();
            for (var i = 0; i < IdleSampleFrames; i++)
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                yield return null;
                idleAllocs.Add(GC.GetAllocatedBytesForCurrentThread() - before);
                idleSamples.Add(Time.unscaledDeltaTime);
            }

            var flipSamples = new List<float>();
            var flipController = UnityEngine.Object.FindFirstObjectByType<CardFlipController>();
            Assert.That(flipController, Is.Not.Null);
            foreach (var card in deck.ActiveCards.ToArray())
            {
                card.GetComponent<CardClickHandler>().OnPointerClick(
                    new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
                yield return SampleWhile(() => flipController.IsFlipping, flipSamples, "Expected the flip to finish.");
            }

            yield return WaitUntil(
                () => flow.State == ReadingFlowState.ResultReady,
                $"Expected ResultReady, but was {flow.State}.");
            GetField<Button>(room, "revealResultButton").onClick.Invoke();
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "Result",
                "Expected Result to load.");

            var resultSamples = new List<float>();
            for (var i = 0; i < IdleSampleFrames; i++)
            {
                yield return null;
                resultSamples.Add(Time.unscaledDeltaTime);
            }

            Report("deal", dealSamples);
            Report("reading-room idle", idleSamples);
            Report("flips", flipSamples);
            Report("result reveal+idle", resultSamples);
            var sortedAllocs = idleAllocs.OrderBy(v => v).ToList();
            Debug.Log(
                $"[Phase36] reading-room idle managed alloc/frame: median {sortedAllocs[sortedAllocs.Count / 2]} B, " +
                $"mean {idleAllocs.Average():F0} B, max {idleAllocs.Max()} B over {idleAllocs.Count} frames");

            Assert.That(idleSamples.Count, Is.GreaterThanOrEqualTo(60), "probe must sample a meaningful window");
            Assert.That(idleSamples.Average(), Is.LessThan(0.1f),
                "reading-room idle should average well under 100ms/frame even in editor/batch runs");
        }

        private static void Report(string label, List<float> samples)
        {
            if (samples.Count == 0)
            {
                Debug.Log($"[Phase36] {label}: no frames sampled");
                return;
            }

            var sorted = samples.OrderBy(v => v).ToList();
            var p95 = sorted[Mathf.Min(sorted.Count - 1, Mathf.FloorToInt(sorted.Count * 0.95f))];
            Debug.Log(
                $"[Phase36] {label}: {samples.Count} frames, mean {samples.Average() * 1000f:F2} ms, " +
                $"p95 {p95 * 1000f:F2} ms, max {samples.Max() * 1000f:F2} ms");
        }

        private static IEnumerator SampleWhile(Func<bool> condition, List<float> samples, string message)
        {
            var deadline = Time.realtimeSinceStartup + 30f;
            while (condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail(message);
                }

                yield return null;
                samples.Add(Time.unscaledDeltaTime);
            }
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string message)
        {
            var deadline = Time.realtimeSinceStartup + 30f;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail(message);
                }

                yield return null;
            }
        }

        private static T GetField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"missing field {fieldName} on {target.GetType().Name}");
            return field.GetValue(target) as T;
        }
    }
}

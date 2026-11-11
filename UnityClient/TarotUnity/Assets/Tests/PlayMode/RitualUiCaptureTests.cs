using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualUiCaptureTests
    {
        [UnityTest]
        public IEnumerator CaptureTheActualMenuAndOneThreeTenCardRituals()
        {
            var directory = Environment.GetEnvironmentVariable("TAROT_RITUAL_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory)) Assert.Ignore("Opt-in actual Unity visual review.");
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("MainMenu");
            yield return null;
            yield return new WaitForSeconds(1.5f);
            yield return CaptureBoth(directory, "cover");
            foreach (var count in new[] { 1, 3, 10 })
            {
                SceneManager.LoadScene("ReadingRoom");
                yield return null;
                yield return new WaitForSeconds(1f);
                if (count == 1) yield return CaptureBoth(directory, "spread");
                var canvas = GameObject.Find("ReadingRoomCanvas").transform;
                canvas.Find(count == 1 ? "OneCardButton" : count == 3 ? "ThreeCardButton" : "CelticCrossButton")
                    .GetComponent<Button>().onClick.Invoke();
                yield return null;
                if (count == 1) yield return CaptureBoth(directory, "question-empty");
                canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text = "下一阶段，我最需要关注什么？";
                yield return null;
                if (count == 1) yield return CaptureBoth(directory, "question");
                canvas.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
                yield return new WaitForSeconds(2.1f);
                if (count == 1) yield return CaptureBoth(directory, "shuffle");
                var fan = Object.FindFirstObjectByType<SpreadFanController>();
                var deck = Object.FindFirstObjectByType<DeckController>();
                var flow = Object.FindFirstObjectByType<ReadingFlowController>();
                yield return Until(() => fan.AcceptingPicks, "fan opens");
                yield return CaptureBoth(directory, $"fan-{count}");
                for (var i = 0; i < count; i++)
                {
                    fan.RequestPick(fan.FanCards[fan.FanCards.Count / 2]);
                    var picked = i + 1;
                    yield return Until(() => deck.ActiveCards.Count >= picked, "picked card lands");
                    if (i == 0) yield return CaptureBoth(directory, $"selected-{count}");
                    if (count > 1 && picked == count) yield return CaptureBoth(directory, $"selected-full-{count}");
                }
                yield return Until(() => flow.State == ReadingFlowState.WaitingForFlip, "ready to reveal", 45f);
                yield return Until(() => !Object.FindFirstObjectByType<RitualDeckPresentation>().IsReturning, "deck finishes returning to the edge");
                if (Environment.GetEnvironmentVariable("TAROT_VISUAL_PARITY_CAPTURE") == "1")
                {
                    var session = ReadingSessionStore.Current;
                    Assert.That(session.source, Is.EqualTo(ReadingSource.Offline));
                    var prototype = LocalReadingSimulator.CreatePlaceholderDraws(3);
                    var fixture = new CardDrawData[count];
                    for (var i = 0; i < count; i++)
                    {
                        fixture[i] = JsonUtility.FromJson<CardDrawData>(JsonUtility.ToJson(prototype[i % 3]));
                        fixture[i].id = fixture[i].position = i + 1;
                        fixture[i].is_reversed = i % 3 == 1;
                        fixture[i].position_name = count == 1 ? "\u6b64\u523b" : count == 3
                            ? new[] { "\u8fc7\u53bb", "\u73b0\u5728", "\u672a\u6765" }[i] : (i + 1).ToString();
                        fixture[i].card_meaning.position = i + 1;
                        fixture[i].card_meaning.position_name = fixture[i].position_name;
                        fixture[i].card_meaning.is_reversed = fixture[i].is_reversed;
                    }
                    // Fixed reference artwork is opt-in test data; production still draws from all 78 cards.
                    deck.BindDealtCards(fixture);
                    ReadingSessionStore.Save(LocalReadingSimulator.CreateSession(session.spreadId, session.spreadName,
                        session.question, "general", fixture));
                }
                yield return CaptureBoth(directory, $"waiting-{count}");
                foreach (var card in deck.ActiveCards.ToArray())
                {
                    card.GetComponent<CardFlipController>().Flip(card);
                    yield return Until(() => !card.GetComponent<CardFlipController>().IsFlipping, "flip finishes");
                    var inspection = Object.FindFirstObjectByType<CardInspectionController>();
                    yield return Until(() => inspection == null || !inspection.IsOpen, "inspection finishes");
                }
                yield return Until(() => flow.State == ReadingFlowState.ResultReady, "result becomes ready");
                yield return CaptureBoth(directory, $"revealed-{count}");
                canvas.Find("RevealResultButton").GetComponent<Button>().onClick.Invoke();
                yield return Until(() => SceneManager.GetActiveScene().name == "Result", "reading result opens");
                yield return Until(() => Object.FindFirstObjectByType<ResultRevealDirector>().IsRevealComplete,
                    "the result has fully faded in");
                yield return CaptureBoth(directory, $"result-{count}");
                if (count == 3)
                {
                    var resultCanvas = GameObject.Find("ResultCanvas").transform;
                    var presenter = resultCanvas.GetComponent<ResultPanelPresenter>();
                    var scroll = resultCanvas.Find("ResultReadingScroll").GetComponent<ScrollRect>();
                    Canvas.ForceUpdateCanvases();
                    scroll.verticalNormalizedPosition = 0f;
                    yield return null;
                    yield return CaptureBoth(directory, "result-scroll-bottom");
                    var session = ReadingSessionStore.Current;
                    presenter.ShowPending(session);
                    yield return CaptureBoth(directory, "result-pending");
                    typeof(ResultPanelPresenter).GetField("pendingSince", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(presenter, Time.unscaledTime - ResultPanelPresenter.PendingSlowNoticeSeconds - 1f);
                    yield return null;
                    yield return CaptureBoth(directory, "result-pending-slow");
                    session.failureMessage = ReleaseUxCopy.InterpretationRetryMayCost;
                    session.canRetry = true;
                    presenter.ShowFailed(session);
                    yield return CaptureBoth(directory, "result-failed");
                    presenter.ShowOffline(session);
                }
            }
        }

        private static IEnumerator CaptureBoth(string directory, string name)
        {
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768) })
            {
                var camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                    .Where(c => c.isActiveAndEnabled && c.gameObject.scene == SceneManager.GetActiveScene())
                    .OrderByDescending(c => c.depth).First();
                var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                    .First(c => c.isRootCanvas && c.gameObject.scene == SceneManager.GetActiveScene());
                using (var capture = new RitualFrameCapture(camera, canvas, size.x, size.y))
                {
                    Canvas.ForceUpdateCanvases();
                    canvas.GetComponent<RitualReadingView>()?.RefreshLayout();
                    var resultFit = canvas.GetComponent<ResultCanvasAspectFit>();
                    if (resultFit != null)
                    {
                        resultFit.Apply(size.x, size.y);
                        var scaler = canvas.GetComponent<CanvasScaler>();
                        scaler.enabled = false;
                        scaler.enabled = true;
                        Canvas.ForceUpdateCanvases();
                        var canvasSize = ((RectTransform)canvas.transform).rect.size;
                        resultFit.Repin(canvasSize.y);
                        canvas.GetComponent<ResultPanelPresenter>()?.ApplyLayout(canvasSize);
                    }
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    Assert.That(((RectTransform)canvas.transform).rect.width / ((RectTransform)canvas.transform).rect.height,
                        Is.EqualTo((float)size.x / size.y).Within(0.001f), "UI and world must use the same capture frame.");
                    if (name == "result-pending-slow")
                    {
                        var status = canvas.GetComponentsInChildren<TMP_Text>(true)
                            .First(t => t.name == "Phase66_InterpretationStatus");
                        Assert.That(status.text, Does.Contain(ReleaseUxCopy.ResultPendingSlow),
                            "The slow notice must survive Update and actually appear in the captured frame.");
                    }
                    var pixels = capture.Render();
                    File.WriteAllBytes(Path.Combine(directory, $"{name}-{size.x}x{size.y}.png"), pixels.EncodeToPNG());
                    Object.Destroy(pixels);
                }
            }
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

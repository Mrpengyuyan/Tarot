using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualResultViewTests
    {
        private GameObject canvas;
        private Button[] buttons;
        private Button.ButtonClickedEvent[] originalClicks;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ReadingSessionStore.Save(LocalReadingSimulator.CreateSession(2, "Spread", "A player question", "general",
                LocalReadingSimulator.CreatePlaceholderDraws(3)));
            SceneManager.sceneLoaded += CaptureButtons;
            yield return SceneManager.LoadSceneAsync("Result", LoadSceneMode.Single);
            yield return null;
            yield return null;
            SceneManager.sceneLoaded -= CaptureButtons;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= CaptureButtons;
            ReadingSessionStore.Clear();
            var result = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("RitualResultCleanup");
            SceneManager.SetActiveScene(empty);
            if (result.IsValid() && result.isLoaded) yield return SceneManager.UnloadSceneAsync(result);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResultStartsWithBorderlessReadingAndPrintedCardFaces()
        {
            var panel = canvas.transform.Find("ResultReadingScroll");
            Assert.That(panel.GetComponent<Image>().color.a, Is.EqualTo(0f), "The old glass reading box must not return.");
            Assert.That(panel.GetComponent<ScrollRect>().enabled, Is.True);
            Assert.That(panel.Find("Viewport").GetComponent<RectMask2D>().enabled, Is.True);
            foreach (var frame in canvas.transform.Find("MP_ResultSpreadBand").GetComponentsInChildren<Image>(true))
            {
                if (frame.name == "Frame") Assert.That(frame.enabled, Is.False, "Keep artwork, not the old tray frame.");
            }
            Assert.That(canvas.transform.Find("Phase12_ResultCardShowcase").GetComponent<Image>().enabled, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActionsKeepOriginalListenersAndPointerTargetsWithoutIvoryPlates()
        {
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            var session = LocalReadingSimulator.CreateSession(2, "Spread", "Question", "general",
                LocalReadingSimulator.CreatePlaceholderDraws(3));
            session.failureMessage = "Please try again";
            session.canRetry = true;
            presenter.ShowFailed(session);
            yield return null;
            Canvas.ForceUpdateCanvases();

            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                Assert.That(button.onClick, Is.SameAs(originalClicks[i]));
                Assert.That(button.IsActive() && button.IsInteractable(), Is.True);
                var plate = button.GetComponent<Image>();
                Assert.That(plate.color.a, Is.EqualTo(0f), button.name);
                Assert.That(plate.enabled && plate.raycastTarget, Is.True, "Transparent actions still need a hit area.");
                var label = button.GetComponentInChildren<TMP_Text>();
                Assert.That(label.color.r, Is.GreaterThan(0.7f), "Gold ink must not revert to dark card-stock ink.");
                var rect = (RectTransform)button.transform;
                var pointer = new PointerEventData(EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                };
                var hits = new List<RaycastResult>();
                canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button));
            }
        }

        [UnityTest]
        public IEnumerator ReapplicationIsIdempotentAndDoesNotChangePresentationStateOrGeometry()
        {
            var type = typeof(ResultPanelPresenter).Assembly.GetType("TarotUnity.UI.RitualResultView");
            Assert.That(type, Is.Not.Null, "The result needs the same runtime presentation ownership as the other scenes.");
            var attach = type.GetMethod("Attach", BindingFlags.Public | BindingFlags.Static);
            Assert.That(attach, Is.Not.Null);
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            var session = LocalReadingSimulator.CreateSession(2, "Spread", "Question", "general",
                LocalReadingSimulator.CreatePlaceholderDraws(10));
            presenter.ShowPending(session);
            var reading = canvas.transform.Find("ResultReadingScroll").GetComponent<RectTransform>();
            var position = reading.anchoredPosition;
            var size = reading.sizeDelta;
            var count = RuleCount();
            attach.Invoke(null, new object[] { canvas });
            attach.Invoke(null, new object[] { canvas });
            canvas.GetComponent<TarotUiTheme>().Apply();
            yield return null;
            yield return null;

            Assert.That(RuleCount(), Is.EqualTo(count));
            Assert.That(canvas.GetComponents(type).Length, Is.EqualTo(1));
            Assert.That(reading.anchoredPosition, Is.EqualTo(position));
            Assert.That(reading.sizeDelta, Is.EqualTo(size));
            Assert.That(reading.GetComponent<Image>().color.a, Is.EqualTo(0f));
            Assert.That(buttons[1].gameObject.activeSelf, Is.False, "Pending state must not acquire a retry button.");
            Assert.That(buttons[2].gameObject.activeSelf, Is.False);
            Assert.That(reading.Find("Viewport").GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f));
            foreach (var button in buttons)
            {
                Assert.That(button.GetComponent<Image>().color.a, Is.EqualTo(0f));
                Assert.That(button.GetComponentInChildren<TMP_Text>(true).color.r, Is.GreaterThan(0.7f));
            }

            presenter.ShowOffline(session);
            yield return null;
            Assert.That(reading.Find("Viewport").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            var notice = Array.Find(canvas.GetComponentsInChildren<TMP_Text>(true), t => t.name == "Phase67_OfflineNotice");
            Assert.That(notice, Is.Not.Null);
            Assert.That(notice.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator CopyAndReadAgainActionsWorkAndPendingSealTracksPendingState()
        {
            var view = canvas.GetComponent<RitualResultView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.ReadAgainButton, Is.Not.Null);
            Assert.That(view.CopyReadingButton, Is.Not.Null);
            Assert.That(view.ReadAgainButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(view.CopyReadingButton.gameObject.activeInHierarchy, Is.True);

            var formatted = ReadingShareFormatter.Format(ReadingSessionStore.Current);
            Assert.That(formatted, Does.Contain("A player question"));
            Assert.That(formatted, Does.Contain("Spread"));

            GUIUtility.systemCopyBuffer = string.Empty;
            view.CopyReadingButton.onClick.Invoke();
            yield return null;

            Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo(formatted));
            Assert.That(view.CopyReadingButton.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("\u5df2\u590d\u5236"));

            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            var pendingSession = LocalReadingSimulator.CreateSession(2, "Spread", "Pending question", "general",
                LocalReadingSimulator.CreatePlaceholderDraws(3));
            pendingSession.source = ReadingSource.Online;
            pendingSession.interpretationState = InterpretationState.Pending;
            presenter.ShowPending(pendingSession);
            view.SetCopyAvailable(false);
            yield return null;

            Assert.That(view.CopyReadingButton.gameObject.activeSelf, Is.False);
            var seal = canvas.transform.Find("RitualPendingSeal");
            if (seal != null)
            {
                Assert.That(seal.gameObject.activeSelf, Is.True);
            }
        }

        private int RuleCount()
        {
            return Array.FindAll(canvas.GetComponentsInChildren<Transform>(true),
                t => t.name == "RitualActionRuleTop" || t.name == "RitualActionRuleBottom").Length;
        }

        private void CaptureButtons(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Result") return;
            canvas = GameObject.Find("ResultCanvas");
            buttons = new Button[3];
            originalClicks = new Button.ButtonClickedEvent[3];
            var names = new[] { "BackToMenuButton", "Phase66_RetryInterpretationButton", "Phase66_OfflineInterpretationButton" };
            for (var i = 0; i < names.Length; i++)
            {
                buttons[i] = canvas.transform.Find(names[i]).GetComponent<Button>();
                originalClicks[i] = buttons[i].onClick;
            }
        }
    }
}

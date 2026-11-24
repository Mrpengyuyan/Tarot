using System;
using System.Collections;
using NUnit.Framework;
using TarotUnity.Core;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class SceneTransitionAndHotkeysTests
    {
        private GameObject managerHost;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (managerHost != null)
            {
                Object.Destroy(managerHost);
                managerHost = null;
            }

            var active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.isLoaded && !active.name.StartsWith("TransitionCleanup"))
            {
                var cleanup = SceneManager.CreateScene($"TransitionCleanup_{Guid.NewGuid():N}");
                SceneManager.SetActiveScene(cleanup);
                yield return SceneManager.UnloadSceneAsync(active);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneFlowManagerFadesOutAndBackInAcrossScenes()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            managerHost = new GameObject("SceneFlowFadeHost");
            var flow = managerHost.AddComponent<SceneFlowManager>();
            flow.FadeDuration = 0.08f;

            var loadedScene = GameSceneId.Boot;
            flow.SceneLoaded += id => loadedScene = id;

            Assert.That(flow.LoadScene(GameSceneId.ReadingRoom), Is.True);
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(flow.FadeAlpha, Is.GreaterThan(0f));

            var timeout = Time.realtimeSinceStartup + 5f;
            while ((flow.IsLoading || flow.IsFading) && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(loadedScene, Is.EqualTo(GameSceneId.ReadingRoom));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ReadingRoom"));
            Assert.That(flow.IsLoading, Is.False);
            Assert.That(flow.IsFading, Is.False);
            Assert.That(flow.FadeAlpha, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator SettingsAndHistoryOverlaysAreMutuallyExclusiveOnMainMenu()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var settings = Object.FindFirstObjectByType<RitualSettingsOverlay>();
            var history = Object.FindFirstObjectByType<RitualHistoryOverlay>();
            Assert.That(settings, Is.Not.Null);
            Assert.That(history, Is.Not.Null);

            settings.Open();
            yield return null;
            Assert.That(settings.IsOpen, Is.True);
            Assert.That(history.IsOpen, Is.False);

            history.Open();
            yield return null;
            Assert.That(history.IsOpen, Is.True);
            Assert.That(settings.IsOpen, Is.False);

            settings.Open();
            yield return null;
            Assert.That(settings.IsOpen, Is.True);
            Assert.That(history.IsOpen, Is.False);

            settings.Close();
        }

        [UnityTest]
        public IEnumerator QuestionSubmitStartsDrawAndTryFlipNextCardSafelyGuardsState()
        {
            yield return SceneManager.LoadSceneAsync("ReadingRoom", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            Assert.That(room, Is.Not.Null);
            Assert.That(room.TryFlipNextCard(), Is.False, "Cannot flip cards before cards are dealt.");

            var canvas = GameObject.Find("ReadingRoomCanvas").transform;
            canvas.Find("OneCardButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            var input = canvas.Find("QuestionInput").GetComponent<TMP_InputField>();
            input.text = "Testing submit shortcut";
            yield return null;

            input.onSubmit.Invoke(input.text);
            yield return null;

            var overlay = Object.FindFirstObjectByType<RitualSettingsOverlay>();
            Assert.That(overlay.TriggerButton.GetComponent<CanvasGroup>().alpha, Is.Zero.Within(0.01f),
                "Submitting the question should enter the shuffle/draw phase.");
        }
    }
}

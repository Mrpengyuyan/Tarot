using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualHistoryTests
    {
        private string tempPath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            tempPath = Path.Combine(Path.GetTempPath(), $"tarot_history_{Guid.NewGuid():N}.json");
            ReadingHistoryStore.SetStoragePathForTesting(tempPath);
            ReadingSessionStore.Clear();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ReadingSessionStore.Clear();
            ReadingHistoryStore.ResetStoragePathForTesting();
            if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded && !scene.name.StartsWith("HistoryCleanup"))
            {
                var cleanup = SceneManager.CreateScene($"HistoryCleanup_{Guid.NewGuid():N}");
                SceneManager.SetActiveScene(cleanup);
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator HistoryStorePersistsDeduplicatesUpdatesAndRemovesEntries()
        {
            var first = LocalReadingSimulator.CreateSession(1, "单牌抽取", "今天应该关注什么？", "general",
                LocalReadingSimulator.CreatePlaceholderDraws(1));
            first.predictionId = 42;
            first.source = ReadingSource.Online;
            first.interpretationState = InterpretationState.Pending;

            var entry1 = ReadingHistoryStore.Record(first);
            Assert.That(entry1, Is.Not.Null);
            Assert.That(ReadingHistoryStore.LoadAll().Count, Is.EqualTo(1));

            first.interpretationState = InterpretationState.Ready;
            first.summary = "已完成的在线解读摘要";
            ReadingHistoryStore.Record(first);

            ReadingHistoryStore.Reload();
            var reloaded = ReadingHistoryStore.LoadAll();
            Assert.That(reloaded.Count, Is.EqualTo(1), "Same predictionId should update in place.");
            Assert.That(reloaded[0].snapshot.summary, Is.EqualTo("已完成的在线解读摘要"));

            var second = LocalReadingSimulator.CreateSession(2, "三张牌", "近期发展如何？", "career",
                LocalReadingSimulator.CreatePlaceholderDraws(3));
            var entry2 = ReadingHistoryStore.Record(second);
            Assert.That(ReadingHistoryStore.LoadAll().Count, Is.EqualTo(2));
            Assert.That(ReadingHistoryStore.LoadAll()[0].historyId, Is.EqualTo(entry2.historyId), "Newest first.");

            Assert.That(ReadingHistoryStore.Remove(entry1.historyId), Is.True);
            ReadingHistoryStore.Reload();
            Assert.That(ReadingHistoryStore.LoadAll().Count, Is.EqualTo(1));

            ReadingHistoryStore.Clear();
            Assert.That(ReadingHistoryStore.LoadAll().Count, Is.EqualTo(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenuHistoryOverlayListsDeletesAndOpensSavedReadingInResultScene()
        {
            var saved = LocalReadingSimulator.CreateSession(2, "三张牌", "我的职业方向是什么？", "career",
                LocalReadingSimulator.CreatePlaceholderDraws(3));
            var second = LocalReadingSimulator.CreateSession(1, "单牌抽取", "今天的心境？", "general",
                LocalReadingSimulator.CreatePlaceholderDraws(1));
            var keepEntry = ReadingHistoryStore.Record(saved);
            var dropEntry = ReadingHistoryStore.Record(second);

            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            yield return null;

            var overlay = Object.FindFirstObjectByType<RitualHistoryOverlay>();
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.TriggerButton, Is.Not.Null);
            Assert.That(overlay.IsOpen, Is.False);

            overlay.TriggerButton.onClick.Invoke();
            yield return null;
            Assert.That(overlay.IsOpen, Is.True);

            var modal = GameObject.Find("RitualHistoryModal");
            Assert.That(modal, Is.Not.Null);
            var content = modal.transform.Find("Panel/HistoryScroll/Viewport/Content");
            Assert.That(content.Find($"HistoryRow_{dropEntry.historyId}"), Is.Not.Null);
            Assert.That(content.Find($"HistoryRow_{keepEntry.historyId}"), Is.Not.Null);

            content.Find($"HistoryRow_{dropEntry.historyId}/DeleteButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(ReadingHistoryStore.LoadAll().Count, Is.EqualTo(1));

            content.Find($"HistoryRow_{keepEntry.historyId}/ViewButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Result"));
            Assert.That(ReadingSessionStore.HasCurrent, Is.True);
            Assert.That(ReadingSessionStore.Current.question, Is.EqualTo("我的职业方向是什么？"));
            Assert.That(ReadingSessionStore.Current.cardCount, Is.EqualTo(3));
        }
    }
}

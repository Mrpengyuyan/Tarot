using System.Collections;
using NUnit.Framework;
using TarotUnity.Core;
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
    public sealed class RitualSettingsOverlayTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            UserPreferences.ResetToDefaults();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UserPreferences.ResetToDefaults();
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded && !scene.name.StartsWith("SettingsOverlayCleanup"))
            {
                var cleanup = SceneManager.CreateScene($"SettingsOverlayCleanup_{System.Guid.NewGuid():N}");
                SceneManager.SetActiveScene(cleanup);
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator PreferencesPersistAndDriveAudioManagerLive()
        {
            var host = new GameObject("PrefsAudioHost");
            var audio = host.AddComponent<AudioManager>();
            yield return null;

            UserPreferences.MusicVolume = 0.25f;
            UserPreferences.SfxVolume = 0.6f;
            UserPreferences.FastShuffle = true;
            UserPreferences.Muted = true;

            UserPreferences.Reload();
            Assert.That(UserPreferences.MusicVolume, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(UserPreferences.SfxVolume, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(UserPreferences.FastShuffle, Is.True);
            Assert.That(UserPreferences.Muted, Is.True);
            Assert.That(UserPreferences.EffectiveMusicVolume, Is.EqualTo(0f));
            Assert.That(UserPreferences.EffectiveSfxVolume, Is.EqualTo(0f));
            Assert.That(UserPreferences.ShuffleSpeedMultiplier, Is.EqualTo(UserPreferences.FastShuffleMultiplier));

            var sources = host.GetComponents<AudioSource>();
            Assert.That(sources.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(sources[0].mute, Is.True);
            Assert.That(sources[1].mute, Is.True);

            UserPreferences.Muted = false;
            Assert.That(sources[0].mute, Is.False);
            Assert.That(sources[0].volume, Is.EqualTo(0.25f).Within(0.001f));

            Object.Destroy(host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenuSettingsOverlayTogglesPreferencesAndClosesCleanly()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            yield return null;

            var overlay = Object.FindFirstObjectByType<RitualSettingsOverlay>();
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.TriggerButton, Is.Not.Null);
            Assert.That(overlay.IsOpen, Is.False);

            overlay.TriggerButton.onClick.Invoke();
            yield return null;
            Assert.That(overlay.IsOpen, Is.True);

            var modal = GameObject.Find("RitualSettingsModal");
            Assert.That(modal, Is.Not.Null);
            modal.transform.Find("Panel/FastShuffleRow/ToggleButton").GetComponent<Button>().onClick.Invoke();
            modal.transform.Find("Panel/MuteRow/ToggleButton").GetComponent<Button>().onClick.Invoke();
            modal.transform.Find("Panel/OfflineModeRow/ToggleButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(UserPreferences.FastShuffle, Is.True);
            Assert.That(UserPreferences.Muted, Is.True);
            Assert.That(UserPreferences.PreferOfflineMode, Is.True);
            Assert.That(overlay.ConnectionBadge, Is.Not.Null);
            Assert.That(overlay.ConnectionBadge.text, Is.EqualTo("\u79bb\u7ebf\u724c\u4e49\u6a21\u5f0f"));

            UserPreferences.Reload();
            Assert.That(UserPreferences.PreferOfflineMode, Is.True);

            modal.transform.Find("Panel/CloseSettingsButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(overlay.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator ReadingRoomHidesSettingsDuringShuffleAndAppliesFastShuffleTiming()
        {
            yield return SceneManager.LoadSceneAsync("ReadingRoom");
            yield return null;
            yield return null;

            var deckShuffle = Object.FindFirstObjectByType<DeckShuffleChoreographer>();
            Assert.That(deckShuffle, Is.Not.Null);
            var normalPlanned = deckShuffle.PlannedSeconds;

            UserPreferences.FastShuffle = true;
            var fastPlanned = deckShuffle.PlannedSeconds;
            Assert.That(fastPlanned, Is.EqualTo(normalPlanned / UserPreferences.FastShuffleMultiplier).Within(0.05f));

            var overlay = Object.FindFirstObjectByType<RitualSettingsOverlay>();
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.TriggerButton.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.01f));

            var canvas = GameObject.Find("ReadingRoomCanvas").transform;
            canvas.Find("OneCardButton").GetComponent<Button>().onClick.Invoke();
            canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text = "Testing fast shuffle hiding settings";
            yield return null;
            canvas.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(overlay.TriggerButton.GetComponent<CanvasGroup>().alpha, Is.Zero.Within(0.01f));
            Assert.That(overlay.ConnectionBadge.GetComponent<CanvasGroup>().alpha, Is.Zero.Within(0.01f));
            Assert.That(overlay.IsOpen, Is.False);
        }
    }
}

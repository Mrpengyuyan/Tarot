using System.Collections.Generic;
using NUnit.Framework;
using TarotUnity.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 33 guards the audio wiring: the persistent AudioManager in Boot binds every
    /// presentation cue (1..7) to a real, non-empty AudioClip and has an ambient music bed,
    /// and the synthesized WAV assets import as valid clips. Before this, cueMap was empty
    /// and cues fell back to single-sine beeps.
    /// </summary>
    public sealed class Phase33AudioTests
    {
        private const string BootScenePath = "Assets/Scenes/Boot.unity";
        private const string DocPath = "Docs/PROJECT_CHRONICLE.md";

        private static readonly string[] AudioAssets =
        {
            "Assets/Audio/SFX/SFX_MenuOpen.wav",
            "Assets/Audio/SFX/SFX_SpreadSelected.wav",
            "Assets/Audio/SFX/SFX_ShuffleStarted.wav",
            "Assets/Audio/SFX/SFX_CardDealt.wav",
            "Assets/Audio/SFX/SFX_CardFlipped.wav",
            "Assets/Audio/SFX/SFX_ResultReady.wav",
            "Assets/Audio/SFX/SFX_ResultReveal.wav",
            "Assets/Audio/Music/MUS_AmbientMoonlit.wav",
        };

        [Test]
        public void SynthesizedClipsImportAsValidAudio()
        {
            foreach (var path in AudioAssets)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Assert.That(clip, Is.Not.Null, $"missing/!imported AudioClip at {path}");
                Assert.That(clip.length, Is.GreaterThan(0.05f), $"{path} should have real duration");
                Assert.That(clip.frequency, Is.GreaterThan(0), $"{path} should have a sample rate");
            }
        }

        [Test]
        public void BootAudioManagerBindsEveryCueAndAmbientMusic()
        {
            EditorSceneManager.OpenScene(BootScenePath);
            var audio = Object.FindFirstObjectByType<AudioManager>();
            Assert.That(audio, Is.Not.Null, "AudioManager should exist in Boot");

            var so = new SerializedObject(audio);
            var cueMap = so.FindProperty("cueMap");
            Assert.That(cueMap, Is.Not.Null);
            Assert.That(cueMap.arraySize, Is.EqualTo(7), "all seven presentation cues should be bound");

            var seen = new HashSet<int>();
            for (var i = 0; i < cueMap.arraySize; i++)
            {
                var element = cueMap.GetArrayElementAtIndex(i);
                var cue = element.FindPropertyRelative("cue").intValue;
                var clip = element.FindPropertyRelative("clip").objectReferenceValue;
                var volume = element.FindPropertyRelative("volume").floatValue;

                Assert.That(cue, Is.InRange(1, 7), "cue must be a real PresentationCueId (not None)");
                Assert.That(seen.Add(cue), Is.True, $"cue {cue} bound more than once");
                Assert.That(clip, Is.Not.Null, $"cue {cue} must have a clip");
                Assert.That(volume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), $"cue {cue} volume out of range");
            }

            Assert.That(seen.Count, Is.EqualTo(7), "cues 1..7 must all be covered");

            var music = so.FindProperty("ambientMusic").objectReferenceValue;
            Assert.That(music, Is.Not.Null, "ambient music bed should be assigned");
            Assert.That(so.FindProperty("musicVolume").floatValue, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
        }

        [Test]
        public void Phase33DocumentationExists()
        {
            Assert.That(System.IO.File.Exists(DocPath), Is.True, $"missing {DocPath}");
            var text = System.IO.File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("royalty-free"));
            Assert.That(text, Does.Contain("AudioManager"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }
    }
}

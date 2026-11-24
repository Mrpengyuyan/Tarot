using System.Collections.Generic;
using TarotUnity.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 33 wires the original royalty-free audio into the game. The synthesized WAVs
    /// (Assets/Audio/*) are imported with sensible settings, then assigned to the persistent
    /// AudioManager in Boot.unity: the seven presentation cues get SFX clips (with balanced
    /// per-cue volumes) and an ambient music bed is set to auto-play. Until now cueMap was
    /// empty, so cues fell back to crude single-sine beeps; now they play real clips.
    /// </summary>
    public static class Phase33AudioBootstrapper
    {
        private const string BootScenePath = "Assets/Scenes/Boot.unity";
        private const string MusicPath = "Assets/Audio/Music/MUS_AmbientMoonlit.wav";

        // cue underlying value (PresentationCueId), sfx asset path, playback volume.
        private static readonly (int cue, string path, float volume)[] CueClips =
        {
            (1, "Assets/Audio/SFX/SFX_MenuOpen.wav", 0.55f),       // MenuOpen
            (2, "Assets/Audio/SFX/SFX_SpreadSelected.wav", 0.60f), // SpreadSelected
            (3, "Assets/Audio/SFX/SFX_ShuffleStarted.wav", 0.85f), // ShuffleStarted
            (4, "Assets/Audio/SFX/SFX_CardDealt.wav", 0.60f),      // CardDealt
            (5, "Assets/Audio/SFX/SFX_CardFlipped.wav", 0.60f),    // CardFlipped
            (6, "Assets/Audio/SFX/SFX_ResultReady.wav", 0.60f),    // ResultReady
            (7, "Assets/Audio/SFX/SFX_ResultReveal.wav", 0.70f),   // ResultReveal
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 33 Audio Bootstrap")]
        public static void Run()
        {
            AssetDatabase.Refresh();

            // Import settings: SFX decompress-on-load PCM (tiny, low latency); music
            // streamed Vorbis (small in the build).
            foreach (var (_, path, _) in CueClips)
            {
                ConfigureAudio(path, isMusic: false);
            }

            ConfigureAudio(MusicPath, isMusic: true);
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene(BootScenePath);
            var audio = Object.FindFirstObjectByType<AudioManager>();
            if (audio == null)
            {
                Debug.LogError("Phase 33: AudioManager not found in Boot scene.");
                return;
            }

            var so = new SerializedObject(audio);
            var cueMap = so.FindProperty("cueMap");
            cueMap.arraySize = CueClips.Length;
            for (var i = 0; i < CueClips.Length; i++)
            {
                var (cue, path, volume) = CueClips[i];
                var element = cueMap.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("cue").intValue = cue;
                element.FindPropertyRelative("clip").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                element.FindPropertyRelative("volume").floatValue = volume;
            }

            so.FindProperty("ambientMusic").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
            so.FindProperty("musicVolume").floatValue = 0.4f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(audio);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BootScenePath);
            AssetDatabase.SaveAssets();

            var missing = new List<string>();
            foreach (var (_, path, _) in CueClips)
            {
                if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
                {
                    missing.Add(path);
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning("Phase 33: missing clips -> " + string.Join(", ", missing));
            }

            Debug.Log($"Tarot Unity Phase 33 audio bootstrap complete ({CueClips.Length} cues + ambient music).");
        }

        private static void ConfigureAudio(string path, bool isMusic)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Phase 33: no AudioImporter at {path}");
                return;
            }

            importer.forceToMono = true;
            importer.loadInBackground = isMusic;

            var settings = importer.defaultSampleSettings;
            settings.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = isMusic ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = 0.7f;
            importer.defaultSampleSettings = settings;

            importer.SaveAndReimport();
        }
    }
}

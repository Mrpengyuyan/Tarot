using System;
using UnityEngine;

namespace TarotUnity.Core
{
    public sealed class AudioManager : MonoBehaviour
    {
        [Serializable]
        private struct AudioCueBinding
        {
            public PresentationCueId cue;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioCueBinding[] cueMap = Array.Empty<AudioCueBinding>();
        [SerializeField] private bool proceduralSfxEnabled = true;
        [SerializeField] private float proceduralCueDuration = 0.18f;
        [SerializeField] private AudioClip ambientMusic;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.45f;

        public PresentationCueId LastCue { get; private set; }
        public int PlayedCueCount { get; private set; }
        public bool ProceduralCuesEnabled => proceduralSfxEnabled;

        private readonly System.Collections.Generic.Dictionary<PresentationCueId, AudioClip> proceduralCueCache = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();

            if (ambientMusic != null)
            {
                PlayMusic(ambientMusic, musicVolume);
            }
        }

        public void PlayMusic(AudioClip clip, float volume = 0.75f)
        {
            if (clip == null)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.volume = volume;
            musicSource.loop = true;
            musicSource.Play();
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, volume);
        }

        public void PlayCue(PresentationCueId cue, float volumeScale = 1f)
        {
            if (cue == PresentationCueId.None)
            {
                return;
            }

            EnsureSources();
            LastCue = cue;
            PlayedCueCount++;

            foreach (var binding in cueMap)
            {
                if (binding.cue != cue || binding.clip == null)
                {
                    continue;
                }

                var volume = Mathf.Clamp01((binding.volume <= 0f ? 1f : binding.volume) * volumeScale);
                PlaySfx(binding.clip, volume);
                return;
            }

            if (proceduralSfxEnabled)
            {
                PlaySfx(GetProceduralCue(cue), Mathf.Clamp01(volumeScale));
            }
        }

        public int GetProceduralCueCountForTesting()
        {
            return Enum.GetValues(typeof(PresentationCueId)).Length - 1;
        }

        private void EnsureSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        private AudioClip GetProceduralCue(PresentationCueId cue)
        {
            if (proceduralCueCache.TryGetValue(cue, out var clip) && clip != null)
            {
                return clip;
            }

            var sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            var sampleCount = Mathf.Max(64, Mathf.RoundToInt(sampleRate * Mathf.Max(0.04f, proceduralCueDuration)));
            var data = new float[sampleCount];
            var frequency = ResolveCueFrequency(cue);

            for (var i = 0; i < sampleCount; i++)
            {
                var time = i / (float)sampleRate;
                var normalized = i / (float)(sampleCount - 1);
                var envelope = Mathf.Sin(normalized * Mathf.PI);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * 0.18f;
            }

            clip = AudioClip.Create($"SFX_{cue}", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            proceduralCueCache[cue] = clip;
            return clip;
        }

        private static float ResolveCueFrequency(PresentationCueId cue)
        {
            return cue switch
            {
                PresentationCueId.MenuOpen => 196f,
                PresentationCueId.SpreadSelected => 261.63f,
                PresentationCueId.ShuffleStarted => 174.61f,
                PresentationCueId.CardDealt => 329.63f,
                PresentationCueId.CardFlipped => 392f,
                PresentationCueId.ResultReady => 523.25f,
                PresentationCueId.ResultReveal => 659.25f,
                _ => 220f,
            };
        }
    }
}

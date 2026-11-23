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

        private AudioSource subtleSfxSource;
        private readonly System.Collections.Generic.Dictionary<PresentationCueId, AudioClip> proceduralCueCache = new();
        private readonly System.Collections.Generic.Dictionary<PresentationCueId, float> lastCueTime = new();

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
            UserPreferences.Changed += ApplyPreferences;
            ApplyPreferences();

            if (ambientMusic != null)
            {
                PlayMusic(ambientMusic, musicVolume);
            }
        }

        private void OnDestroy()
        {
            UserPreferences.Changed -= ApplyPreferences;
            foreach (var clip in proceduralCueCache.Values)
            {
                if (clip != null)
                {
                    Destroy(clip);
                }
            }

            proceduralCueCache.Clear();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ApplyPreferences()
        {
            EnsureSources();
            musicSource.mute = UserPreferences.Muted;
            sfxSource.mute = UserPreferences.Muted;
            if (subtleSfxSource != null) subtleSfxSource.mute = UserPreferences.Muted;
            musicSource.volume = UserPreferences.EffectiveMusicVolume;
        }

        public void PlayMusic(AudioClip clip, float volume = 0.75f)
        {
            if (clip == null)
            {
                return;
            }

            EnsureSources();
            musicSource.clip = clip;
            var scale = UserPreferences.DefaultMusicVolume > 0f
                ? UserPreferences.EffectiveMusicVolume / UserPreferences.DefaultMusicVolume
                : 1f;
            musicSource.volume = Mathf.Clamp01(volume * scale);
            musicSource.mute = UserPreferences.Muted;
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

            EnsureSources();
            sfxSource.mute = UserPreferences.Muted;
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume * UserPreferences.EffectiveSfxVolume));
        }

        public void PlayCue(PresentationCueId cue, float volumeScale = 1f)
        {
            if (cue == PresentationCueId.None)
            {
                return;
            }

            var cooldown = ResolveCueCooldown(cue);
            if (cooldown > 0f)
            {
                var now = Time.unscaledTime;
                if (lastCueTime.TryGetValue(cue, out var previous) && now - previous < cooldown)
                {
                    return;
                }

                lastCueTime[cue] = now;
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
                var clip = GetProceduralCue(cue);
                if (IsSubtleCue(cue) && subtleSfxSource != null)
                {
                    subtleSfxSource.mute = UserPreferences.Muted;
                    subtleSfxSource.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
                    subtleSfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale * UserPreferences.EffectiveSfxVolume));
                }
                else
                {
                    PlaySfx(clip, Mathf.Clamp01(volumeScale));
                }
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

            if (subtleSfxSource == null)
            {
                subtleSfxSource = gameObject.AddComponent<AudioSource>();
                subtleSfxSource.playOnAwake = false;
            }
        }

        private static bool IsSubtleCue(PresentationCueId cue)
        {
            return cue == PresentationCueId.CardHoverRustle
                || cue == PresentationCueId.UiHoverSoft
                || cue == PresentationCueId.UiClickSoft;
        }

        private static float ResolveCueCooldown(PresentationCueId cue)
        {
            return cue switch
            {
                PresentationCueId.CardHoverRustle => 0.065f,
                PresentationCueId.UiHoverSoft => 0.08f,
                PresentationCueId.UiClickSoft => 0.04f,
                _ => 0f,
            };
        }

        private AudioClip GetProceduralCue(PresentationCueId cue)
        {
            if (proceduralCueCache.TryGetValue(cue, out var clip) && clip != null)
            {
                return clip;
            }

            var sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            var duration = cue switch
            {
                PresentationCueId.CardHoverRustle => 0.06f,
                PresentationCueId.UiHoverSoft => 0.07f,
                PresentationCueId.UiClickSoft => 0.045f,
                _ => Mathf.Max(0.04f, proceduralCueDuration),
            };
            var sampleCount = Mathf.Max(64, Mathf.RoundToInt(sampleRate * duration));
            var data = new float[sampleCount];

            if (cue == PresentationCueId.CardHoverRustle)
            {
                // Filtered cardstock brush: deterministic LCG noise shaped with a quick parchment envelope.
                uint seed = 0x51A7BEEFu;
                var lp = 0f;
                var hp = 0f;
                var prev = 0f;
                for (var i = 0; i < sampleCount; i++)
                {
                    seed = seed * 1664525u + 1013904223u;
                    var white = ((seed >> 9) / 4194304f) - 1f;
                    hp = 0.72f * (hp + white - prev);
                    prev = white;
                    lp += 0.28f * (hp - lp);
                    var normalized = i / (float)(sampleCount - 1);
                    var env = Mathf.Pow(Mathf.Sin(normalized * Mathf.PI), 0.75f) * (1f - 0.55f * normalized);
                    data[i] = lp * env * 0.24f;
                }
            }
            else if (cue == PresentationCueId.UiClickSoft)
            {
                for (var i = 0; i < sampleCount; i++)
                {
                    var time = i / (float)sampleRate;
                    var normalized = i / (float)(sampleCount - 1);
                    var env = Mathf.Exp(-normalized * 5.5f) * Mathf.Sin(Mathf.Min(1f, normalized * 12f) * Mathf.PI * 0.5f);
                    data[i] = (Mathf.Sin(2f * Mathf.PI * 310f * time) * 0.7f + Mathf.Sin(2f * Mathf.PI * 620f * time) * 0.3f) * env * 0.14f;
                }
            }
            else
            {
                var frequency = ResolveCueFrequency(cue);
                var amp = cue == PresentationCueId.UiHoverSoft ? 0.08f : 0.15f;
                for (var i = 0; i < sampleCount; i++)
                {
                    var time = i / (float)sampleRate;
                    var normalized = i / (float)(sampleCount - 1);
                    var attack = Mathf.Clamp01(normalized * 8f);
                    var decay = Mathf.Exp(-normalized * 2.8f) * (1f - normalized);
                    var env = attack * decay;
                    var tone = Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.75f
                        + Mathf.Sin(2f * Mathf.PI * frequency * 1.5f * time) * 0.25f;
                    data[i] = tone * env * amp;
                }
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
                PresentationCueId.UiHoverSoft => 440f,
                _ => 220f,
            };
        }
    }
}

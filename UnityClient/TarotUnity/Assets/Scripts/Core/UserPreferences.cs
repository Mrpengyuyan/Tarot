using System;
using UnityEngine;

namespace TarotUnity.Core
{
    /// <summary>Persistent desktop user preferences backed by PlayerPrefs.</summary>
    public static class UserPreferences
    {
        private const string MusicVolumeKey = "TarotUnity.Prefs.MusicVolume";
        private const string SfxVolumeKey = "TarotUnity.Prefs.SfxVolume";
        private const string MutedKey = "TarotUnity.Prefs.Muted";
        private const string FastShuffleKey = "TarotUnity.Prefs.FastShuffle";
        private const string FullScreenKey = "TarotUnity.Prefs.FullScreen";
        private const string PreferOfflineModeKey = "TarotUnity.Prefs.PreferOfflineMode";

        public const float DefaultMusicVolume = 0.45f;
        public const float DefaultSfxVolume = 1f;
        public const float FastShuffleMultiplier = 2.5f;

        private static bool loaded;
        private static float musicVolume = DefaultMusicVolume;
        private static float sfxVolume = DefaultSfxVolume;
        private static bool muted;
        private static bool fastShuffle;
        private static bool fullScreen;
        private static bool preferOfflineMode;

        public static event Action Changed;

        public static float MusicVolume
        {
            get { EnsureLoaded(); return musicVolume; }
            set
            {
                EnsureLoaded();
                var clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(musicVolume, clamped)) return;
                musicVolume = clamped;
                PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static float SfxVolume
        {
            get { EnsureLoaded(); return sfxVolume; }
            set
            {
                EnsureLoaded();
                var clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(sfxVolume, clamped)) return;
                sfxVolume = clamped;
                PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static bool Muted
        {
            get { EnsureLoaded(); return muted; }
            set
            {
                EnsureLoaded();
                if (muted == value) return;
                muted = value;
                PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static bool FastShuffle
        {
            get { EnsureLoaded(); return fastShuffle; }
            set
            {
                EnsureLoaded();
                if (fastShuffle == value) return;
                fastShuffle = value;
                PlayerPrefs.SetInt(FastShuffleKey, fastShuffle ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static bool FullScreen
        {
            get { EnsureLoaded(); return fullScreen; }
            set
            {
                EnsureLoaded();
                if (fullScreen == value) return;
                fullScreen = value;
                PlayerPrefs.SetInt(FullScreenKey, fullScreen ? 1 : 0);
                PlayerPrefs.Save();
                ApplyDisplayMode();
                Changed?.Invoke();
            }
        }

        public static bool PreferOfflineMode
        {
            get { EnsureLoaded(); return preferOfflineMode; }
            set
            {
                EnsureLoaded();
                if (preferOfflineMode == value) return;
                preferOfflineMode = value;
                PlayerPrefs.SetInt(PreferOfflineModeKey, preferOfflineMode ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static float EffectiveMusicVolume => Muted ? 0f : MusicVolume;
        public static float EffectiveSfxVolume => Muted ? 0f : SfxVolume;
        public static float ShuffleSpeedMultiplier => FastShuffle ? FastShuffleMultiplier : 1f;

        public static void Reload()
        {
            loaded = false;
            EnsureLoaded();
            Changed?.Invoke();
        }

        public static void ResetToDefaults()
        {
            loaded = true;
            musicVolume = DefaultMusicVolume;
            sfxVolume = DefaultSfxVolume;
            muted = false;
            fastShuffle = false;
            fullScreen = Screen.fullScreen;
            preferOfflineMode = false;
            PlayerPrefs.DeleteKey(MusicVolumeKey);
            PlayerPrefs.DeleteKey(SfxVolumeKey);
            PlayerPrefs.DeleteKey(MutedKey);
            PlayerPrefs.DeleteKey(FastShuffleKey);
            PlayerPrefs.DeleteKey(FullScreenKey);
            PlayerPrefs.DeleteKey(PreferOfflineModeKey);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));
            muted = PlayerPrefs.GetInt(MutedKey, 0) != 0;
            fastShuffle = PlayerPrefs.GetInt(FastShuffleKey, 0) != 0;
            fullScreen = PlayerPrefs.HasKey(FullScreenKey)
                ? PlayerPrefs.GetInt(FullScreenKey, 0) != 0
                : Screen.fullScreen;
            preferOfflineMode = PlayerPrefs.GetInt(PreferOfflineModeKey, 0) != 0;
        }

        private static void ApplyDisplayMode()
        {
            if (!Application.isBatchMode)
            {
                Screen.fullScreen = fullScreen;
            }
        }
    }
}

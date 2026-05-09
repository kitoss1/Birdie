using System;
using Birdie.Data;
using Birdie.Debug;
using Birdie.Save;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages all game audio through three dedicated channels: SFX, Music, and Ambient.
    /// Provides volume control, mute toggles, and clip playback via SoundLibrary keys
    /// or direct AudioClip references.
    /// </summary>
    public class SoundManager : BaseManager
    {
        [Header("Sound Library")]
        [SerializeField]
        private SoundLibrary m_soundLibrary;

        [Header("Audio Sources")]
        [SerializeField]
        private AudioSource m_sfxSource;

        [SerializeField]
        private AudioSource m_musicSource;

        [SerializeField]
        private AudioSource m_ambientSource;

        private float m_masterVolume = 1f;
        private float m_sfxVolume = 1f;
        private float m_musicVolume = 1f;
        private float m_ambientVolume = 1f;

        private bool m_masterMuted;
        private bool m_sfxMuted;
        private bool m_musicMuted;
        private bool m_ambientMuted;

        public event Action<AudioChannel, float> OnVolumeChanged;
        public event Action<AudioChannel, bool> OnMuteChanged;

        public float MasterVolume => m_masterVolume;

        public float SfxVolume => m_sfxVolume;

        public float MusicVolume => m_musicVolume;

        public float AmbientVolume => m_ambientVolume;

        public bool MasterMuted => m_masterMuted;

        public bool SfxMuted => m_sfxMuted;

        public bool MusicMuted => m_musicMuted;

        public bool AmbientMuted => m_ambientMuted;

        public override void Initialize(SaveManager saveManager = null)
        {
            base.Initialize(saveManager);

            ValidateAudioSources();

            if (m_soundLibrary != null)
            {
                m_soundLibrary.Initialize();
            }

            ConfigureMusicSource();
            ConfigureAmbientSource();

            if (m_saveManager != null)
                LoadFromSaveData();

            DebugBase.Log($"[{nameof(SoundManager)}] Sound system initialized", DebugCategory.Audio);
        }

        // --- SFX Playback ---

        /// <summary>
        /// Plays a one-shot SFX clip.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (!EnsureInitialized() || clip == null || m_sfxSource == null)
            {
                return;
            }

            m_sfxSource.volume = CalculateEffectiveVolume(m_sfxVolume, m_sfxMuted);
            m_sfxSource.PlayOneShot(clip, volumeScale);

            DebugBase.Log($"[{nameof(SoundManager)}] Playing SFX: {clip.name}", DebugCategory.Audio);
        }

        /// <summary>
        /// Plays a one-shot SFX by looking up a key in the SoundLibrary.
        /// </summary>
        public void PlaySFX(string key, float volumeScale = 1f)
        {
            if (m_soundLibrary == null)
            {
                DebugBase.LogWarning($"[{nameof(SoundManager)}] SoundLibrary is not assigned", DebugCategory.Audio);
                return;
            }

            AudioClip clip = m_soundLibrary.GetClip(key);
            if (clip != null)
            {
                PlaySFX(clip, volumeScale);
            }
        }

        // --- Music Playback ---

        /// <summary>
        /// Starts playing a music track. Stops any currently playing music.
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (!EnsureInitialized() || clip == null || m_musicSource == null)
            {
                return;
            }

            m_musicSource.clip = clip;
            m_musicSource.loop = loop;
            m_musicSource.volume = CalculateEffectiveVolume(m_musicVolume, m_musicMuted);
            m_musicSource.Play();

            DebugBase.Log($"[{nameof(SoundManager)}] Playing music: {clip.name}", DebugCategory.Audio);
        }

        /// <summary>
        /// Starts playing a music track by SoundLibrary key.
        /// </summary>
        public void PlayMusic(string key, bool loop = true)
        {
            if (m_soundLibrary == null)
            {
                DebugBase.LogWarning($"[{nameof(SoundManager)}] SoundLibrary is not assigned", DebugCategory.Audio);
                return;
            }

            AudioClip clip = m_soundLibrary.GetClip(key);
            if (clip != null)
            {
                PlayMusic(clip, loop);
            }
        }

        /// <summary>
        /// Stops the currently playing music track.
        /// </summary>
        public void StopMusic()
        {
            if (m_musicSource != null && m_musicSource.isPlaying)
            {
                m_musicSource.Stop();
                DebugBase.Log($"[{nameof(SoundManager)}] Music stopped", DebugCategory.Audio);
            }
        }

        // --- Ambient Playback ---

        /// <summary>
        /// Starts playing an ambient track. Stops any currently playing ambient audio.
        /// </summary>
        public void PlayAmbient(AudioClip clip, bool loop = true)
        {
            if (!EnsureInitialized() || clip == null || m_ambientSource == null)
            {
                return;
            }

            m_ambientSource.clip = clip;
            m_ambientSource.loop = loop;
            m_ambientSource.volume = CalculateEffectiveVolume(m_ambientVolume, m_ambientMuted);
            m_ambientSource.Play();

            DebugBase.Log($"[{nameof(SoundManager)}] Playing ambient: {clip.name}", DebugCategory.Audio);
        }

        /// <summary>
        /// Starts playing an ambient track by SoundLibrary key.
        /// </summary>
        public void PlayAmbient(string key, bool loop = true)
        {
            if (m_soundLibrary == null)
            {
                DebugBase.LogWarning($"[{nameof(SoundManager)}] SoundLibrary is not assigned", DebugCategory.Audio);
                return;
            }

            AudioClip clip = m_soundLibrary.GetClip(key);
            if (clip != null)
            {
                PlayAmbient(clip, loop);
            }
        }

        /// <summary>
        /// Stops the currently playing ambient track.
        /// </summary>
        public void StopAmbient()
        {
            if (m_ambientSource != null && m_ambientSource.isPlaying)
            {
                m_ambientSource.Stop();
                DebugBase.Log($"[{nameof(SoundManager)}] Ambient stopped", DebugCategory.Audio);
            }
        }

        /// <summary>
        /// Stops all audio on all channels.
        /// </summary>
        public void StopAll()
        {
            StopMusic();
            StopAmbient();

            if (m_sfxSource != null)
            {
                m_sfxSource.Stop();
            }

            DebugBase.Log($"[{nameof(SoundManager)}] All audio stopped", DebugCategory.Audio);
        }

        // --- Volume Setters ---

        public void SetMasterVolume(float volume)
        {
            m_masterVolume = Mathf.Clamp01(volume);
            ApplyAllVolumes();
            OnVolumeChanged?.Invoke(AudioChannel.Master, m_masterVolume);
            SaveToSaveData();
        }

        public void SetSFXVolume(float volume)
        {
            m_sfxVolume = Mathf.Clamp01(volume);
            ApplySfxVolume();
            OnVolumeChanged?.Invoke(AudioChannel.SFX, m_sfxVolume);
            SaveToSaveData();
        }

        public void SetMusicVolume(float volume)
        {
            m_musicVolume = Mathf.Clamp01(volume);
            ApplyMusicVolume();
            OnVolumeChanged?.Invoke(AudioChannel.Music, m_musicVolume);
            SaveToSaveData();
        }

        public void SetAmbientVolume(float volume)
        {
            m_ambientVolume = Mathf.Clamp01(volume);
            ApplyAmbientVolume();
            OnVolumeChanged?.Invoke(AudioChannel.Ambient, m_ambientVolume);
            SaveToSaveData();
        }

        // --- Mute Setters ---

        public void SetMasterMute(bool muted)
        {
            m_masterMuted = muted;
            ApplyAllVolumes();
            OnMuteChanged?.Invoke(AudioChannel.Master, m_masterMuted);
            SaveToSaveData();
        }

        public void SetSFXMute(bool muted)
        {
            m_sfxMuted = muted;
            ApplySfxVolume();
            OnMuteChanged?.Invoke(AudioChannel.SFX, m_sfxMuted);
            SaveToSaveData();
        }

        public void SetMusicMute(bool muted)
        {
            m_musicMuted = muted;
            ApplyMusicVolume();
            OnMuteChanged?.Invoke(AudioChannel.Music, m_musicMuted);
            SaveToSaveData();
        }

        public void SetAmbientMute(bool muted)
        {
            m_ambientMuted = muted;
            ApplyAmbientVolume();
            OnMuteChanged?.Invoke(AudioChannel.Ambient, m_ambientMuted);
            SaveToSaveData();
        }

        // --- Effective Volume Helpers (for external AudioSources) ---

        /// <summary>
        /// Calculates the effective SFX volume for external AudioSources.
        /// Used by components that manage their own AudioSource but want consistent volume.
        /// </summary>
        public float GetEffectiveSfxVolume(float volumeScale = 1f)
        {
            return CalculateEffectiveVolume(m_sfxVolume, m_sfxMuted) * volumeScale;
        }

        /// <summary>
        /// Calculates the effective music volume for external AudioSources.
        /// </summary>
        public float GetEffectiveMusicVolume()
        {
            return CalculateEffectiveVolume(m_musicVolume, m_musicMuted);
        }

        /// <summary>
        /// Calculates the effective ambient volume for external AudioSources.
        /// </summary>
        public float GetEffectiveAmbientVolume()
        {
            return CalculateEffectiveVolume(m_ambientVolume, m_ambientMuted);
        }

        // --- Private Helpers ---

        private void ValidateAudioSources()
        {
            if (m_sfxSource == null)
            {
                DebugBase.LogError($"[{nameof(SoundManager)}] SFX AudioSource is not assigned!");
            }

            if (m_musicSource == null)
            {
                DebugBase.LogError($"[{nameof(SoundManager)}] Music AudioSource is not assigned!");
            }

            if (m_ambientSource == null)
            {
                DebugBase.LogError($"[{nameof(SoundManager)}] Ambient AudioSource is not assigned!");
            }
        }

        private void ConfigureMusicSource()
        {
            if (m_musicSource != null)
            {
                m_musicSource.loop = true;
                m_musicSource.playOnAwake = false;
            }
        }

        private void ConfigureAmbientSource()
        {
            if (m_ambientSource != null)
            {
                m_ambientSource.loop = true;
                m_ambientSource.playOnAwake = false;
            }
        }

        private float CalculateEffectiveVolume(float channelVolume, bool channelMuted)
        {
            if (m_masterMuted || channelMuted)
            {
                return 0f;
            }

            return m_masterVolume * channelVolume;
        }

        private void ApplyAllVolumes()
        {
            ApplySfxVolume();
            ApplyMusicVolume();
            ApplyAmbientVolume();
        }

        private void ApplySfxVolume()
        {
            if (m_sfxSource != null)
            {
                m_sfxSource.volume = CalculateEffectiveVolume(m_sfxVolume, m_sfxMuted);
            }
        }

        private void ApplyMusicVolume()
        {
            if (m_musicSource != null)
            {
                m_musicSource.volume = CalculateEffectiveVolume(m_musicVolume, m_musicMuted);
            }
        }

        private void ApplyAmbientVolume()
        {
            if (m_ambientSource != null)
            {
                m_ambientSource.volume = CalculateEffectiveVolume(m_ambientVolume, m_ambientMuted);
            }
        }

        // --- Save/Load ---

        private void LoadFromSaveData()
        {
            if (m_saveManager == null || m_saveManager.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(SoundManager)}] SaveManager or SaveData is null, cannot load", DebugCategory.Audio);
                return;
            }

            AudioSaveData audioData = m_saveManager.CurrentSaveData.audio;

            m_masterVolume = audioData.masterVolume;
            m_sfxVolume = audioData.sfxVolume;
            m_musicVolume = audioData.musicVolume;
            m_ambientVolume = audioData.ambientVolume;
            m_masterMuted = audioData.masterMuted;
            m_sfxMuted = audioData.sfxMuted;
            m_musicMuted = audioData.musicMuted;
            m_ambientMuted = audioData.ambientMuted;

            ApplyAllVolumes();

            DebugBase.Log($"[{nameof(SoundManager)}] Loaded audio settings. Master: {m_masterVolume}, Muted: {m_masterMuted}", DebugCategory.Audio);
        }

        private void SaveToSaveData()
        {
            if (m_saveManager == null || m_saveManager.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(SoundManager)}] SaveManager or SaveData is null, cannot save", DebugCategory.Audio);
                return;
            }

            AudioSaveData audioData = m_saveManager.CurrentSaveData.audio;
            audioData.masterVolume = m_masterVolume;
            audioData.sfxVolume = m_sfxVolume;
            audioData.musicVolume = m_musicVolume;
            audioData.ambientVolume = m_ambientVolume;
            audioData.masterMuted = m_masterMuted;
            audioData.sfxMuted = m_sfxMuted;
            audioData.musicMuted = m_musicMuted;
            audioData.ambientMuted = m_ambientMuted;

            m_saveManager.SaveGame();

            DebugBase.Log($"[{nameof(SoundManager)}] Saved audio settings", DebugCategory.Audio);
        }
    }
}

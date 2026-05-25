using System;
using Birdie.Data;
using Birdie.Debug;
using Birdie.Save;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages all game audio through two dedicated channels: SFX and Music.
    /// Provides volume control, mute toggles, and clip playback via SoundLibrary keys
    /// or direct AudioClip references.
    /// </summary>
    public class SoundManager : BaseManager
    {
        [Header("Sound Library")]
        [SerializeField]
        private SoundLibrary m_soundLibrary;

        [Header("Music Playlist")]
        [SerializeField]
        private MusicPlaylist m_defaultPlaylist;

        [Header("Audio Sources")]
        [SerializeField]
        private AudioSource m_sfxSource;

        [SerializeField]
        private AudioSource m_musicSource;

        private float m_masterVolume = 1f;
        private float m_sfxVolume = 1f;
        private float m_musicVolume = 1f;

        private bool m_masterMuted;
        private bool m_sfxMuted;
        private bool m_musicMuted;

        private MusicPlaylistPlayer m_playlistPlayer;

        public event Action<AudioChannel, float> OnVolumeChanged;
        public event Action<AudioChannel, bool> OnMuteChanged;

        public float MasterVolume => m_masterVolume;
        public float SfxVolume => m_sfxVolume;
        public float MusicVolume => m_musicVolume;
        public bool MasterMuted => m_masterMuted;
        public bool SfxMuted => m_sfxMuted;
        public bool MusicMuted => m_musicMuted;

        public override void Initialize(SaveManager saveManager = null)
        {
            base.Initialize(saveManager);

            ValidateAudioSources();

            if (m_soundLibrary != null)
            {
                m_soundLibrary.Initialize();
            }

            ConfigureMusicSource();

            if (m_saveManager != null)
                LoadFromSaveData();

            m_playlistPlayer = new MusicPlaylistPlayer(m_musicSource);

            if (m_defaultPlaylist != null && m_defaultPlaylist.TrackCount > 0)
                PlayPlaylist(m_defaultPlaylist);

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
        /// Starts playing a music track. Stops any active playlist and currently playing music.
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (!EnsureInitialized() || clip == null || m_musicSource == null)
            {
                return;
            }

            m_playlistPlayer?.Stop();

            m_musicSource.clip = clip;
            m_musicSource.loop = loop;
            m_musicSource.volume = CalculateEffectiveVolume(m_musicVolume, m_musicMuted);
            m_musicSource.Play();

            DebugBase.Log($"[{nameof(SoundManager)}] Playing music: {clip.name}", DebugCategory.Audio);
        }

        /// <summary>
        /// Starts playing a music playlist, cycling through tracks sequentially or in shuffled order.
        /// Stops any currently playing music or active playlist first.
        /// </summary>
        public void PlayPlaylist(MusicPlaylist playlist)
        {
            if (!EnsureInitialized() || playlist == null || playlist.TrackCount == 0)
            {
                return;
            }

            m_playlistPlayer.Play(playlist, () => CalculateEffectiveVolume(m_musicVolume, m_musicMuted));

            DebugBase.Log($"[{nameof(SoundManager)}] Started playlist ({playlist.TrackCount} tracks)", DebugCategory.Audio);
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
        /// Stops the currently playing music track and any active playlist.
        /// </summary>
        public void StopMusic()
        {
            m_playlistPlayer?.Stop();

            if (m_musicSource != null && m_musicSource.isPlaying)
            {
                m_musicSource.Stop();
                DebugBase.Log($"[{nameof(SoundManager)}] Music stopped", DebugCategory.Audio);
            }
        }

        /// <summary>
        /// Stops all audio on all channels.
        /// </summary>
        public void StopAll()
        {
            StopMusic();

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
        }

        private void ConfigureMusicSource()
        {
            if (m_musicSource != null)
            {
                m_musicSource.loop = true;
                m_musicSource.playOnAwake = false;
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

        private void OnDestroy()
        {
            m_playlistPlayer?.Stop();
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
            m_masterMuted = audioData.masterMuted;
            m_sfxMuted = audioData.sfxMuted;
            m_musicMuted = audioData.musicMuted;

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
            audioData.masterMuted = m_masterMuted;
            audioData.sfxMuted = m_sfxMuted;
            audioData.musicMuted = m_musicMuted;

            m_saveManager.SaveGame();

            DebugBase.Log($"[{nameof(SoundManager)}] Saved audio settings", DebugCategory.Audio);
        }
    }
}

using Birdie.Debug;
using Birdie.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// UI component for the Settings overlay menu.
    /// Controls volume sliders and mute toggles for all audio channels.
    /// </summary>
    public class SettingsMenuUI : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField]
        [Tooltip("Button to close the settings menu")]
        private Button m_closeButton;

        [SerializeField]
        [Tooltip("Button to quit the application")]
        private Button m_quitButton;

        [Header("Quit Warning")]
        [SerializeField]
        [Tooltip("Panel shown to confirm quitting the application")]
        private GameObject m_quitWarningPanel;

        [SerializeField]
        [Tooltip("Confirms quitting the application")]
        private Button m_quitWarningYesButton;

        [SerializeField]
        [Tooltip("Cancels quitting and returns to settings")]
        private Button m_quitWarningNoButton;

        [Header("Master Audio")]
        [SerializeField]
        [Tooltip("Slider for master volume (0-1)")]
        private Slider m_masterVolumeSlider;

        [SerializeField]
        [Tooltip("Toggle for master mute (on = audio enabled)")]
        private Toggle m_masterMuteToggle;

        [Header("SFX Audio")]
        [SerializeField]
        [Tooltip("Slider for SFX volume (0-1)")]
        private Slider m_sfxVolumeSlider;

        [SerializeField]
        [Tooltip("Toggle for SFX mute (on = audio enabled)")]
        private Toggle m_sfxMuteToggle;

        [Header("Music Audio")]
        [SerializeField]
        [Tooltip("Slider for music volume (0-1)")]
        private Slider m_musicVolumeSlider;

        [SerializeField]
        [Tooltip("Toggle for music mute (on = audio enabled)")]
        private Toggle m_musicMuteToggle;

        [Header("Ambient Audio")]
        [SerializeField]
        [Tooltip("Slider for ambient volume (0-1)")]
        private Slider m_ambientVolumeSlider;

        [SerializeField]
        [Tooltip("Toggle for ambient mute (on = audio enabled)")]
        private Toggle m_ambientMuteToggle;

        private bool m_isUpdatingUI;

        private void Awake()
        {
            SetupListeners();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            RefreshUI();
            HideQuitWarning();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void SetupListeners()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (m_quitButton != null)
            {
                m_quitButton.onClick.AddListener(OnQuitButtonClicked);
            }

            if (m_quitWarningYesButton != null)
            {
                m_quitWarningYesButton.onClick.AddListener(OnQuitWarningYesClicked);
            }

            if (m_quitWarningNoButton != null)
            {
                m_quitWarningNoButton.onClick.AddListener(OnQuitWarningNoClicked);
            }

            if (m_masterVolumeSlider != null)
            {
                m_masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (m_sfxVolumeSlider != null)
            {
                m_sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (m_musicVolumeSlider != null)
            {
                m_musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (m_ambientVolumeSlider != null)
            {
                m_ambientVolumeSlider.onValueChanged.AddListener(OnAmbientVolumeChanged);
            }

            if (m_masterMuteToggle != null)
            {
                m_masterMuteToggle.onValueChanged.AddListener(OnMasterMuteChanged);
            }

            if (m_sfxMuteToggle != null)
            {
                m_sfxMuteToggle.onValueChanged.AddListener(OnSfxMuteChanged);
            }

            if (m_musicMuteToggle != null)
            {
                m_musicMuteToggle.onValueChanged.AddListener(OnMusicMuteChanged);
            }

            if (m_ambientMuteToggle != null)
            {
                m_ambientMuteToggle.onValueChanged.AddListener(OnAmbientMuteChanged);
            }
        }

        private void RemoveListeners()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            if (m_quitButton != null)
            {
                m_quitButton.onClick.RemoveListener(OnQuitButtonClicked);
            }

            if (m_quitWarningYesButton != null)
            {
                m_quitWarningYesButton.onClick.RemoveListener(OnQuitWarningYesClicked);
            }

            if (m_quitWarningNoButton != null)
            {
                m_quitWarningNoButton.onClick.RemoveListener(OnQuitWarningNoClicked);
            }

            if (m_masterVolumeSlider != null)
            {
                m_masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            }

            if (m_sfxVolumeSlider != null)
            {
                m_sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            }

            if (m_musicVolumeSlider != null)
            {
                m_musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            }

            if (m_ambientVolumeSlider != null)
            {
                m_ambientVolumeSlider.onValueChanged.RemoveListener(OnAmbientVolumeChanged);
            }

            if (m_masterMuteToggle != null)
            {
                m_masterMuteToggle.onValueChanged.RemoveListener(OnMasterMuteChanged);
            }

            if (m_sfxMuteToggle != null)
            {
                m_sfxMuteToggle.onValueChanged.RemoveListener(OnSfxMuteChanged);
            }

            if (m_musicMuteToggle != null)
            {
                m_musicMuteToggle.onValueChanged.RemoveListener(OnMusicMuteChanged);
            }

            if (m_ambientMuteToggle != null)
            {
                m_ambientMuteToggle.onValueChanged.RemoveListener(OnAmbientMuteChanged);
            }
        }

        private void SubscribeToEvents()
        {
            SoundManager soundManager = GameManager.Instance?.SoundManager;
            if (soundManager != null)
            {
                soundManager.OnVolumeChanged += OnSoundManagerVolumeChanged;
                soundManager.OnMuteChanged += OnSoundManagerMuteChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            SoundManager soundManager = GameManager.Instance?.SoundManager;
            if (soundManager != null)
            {
                soundManager.OnVolumeChanged -= OnSoundManagerVolumeChanged;
                soundManager.OnMuteChanged -= OnSoundManagerMuteChanged;
            }
        }

        // --- UI Refresh ---

        private void RefreshUI()
        {
            SoundManager soundManager = GameManager.Instance?.SoundManager;
            if (soundManager == null)
            {
                DebugBase.LogWarning($"[{nameof(SettingsMenuUI)}] SoundManager not available, cannot refresh UI");
                return;
            }

            m_isUpdatingUI = true;

            if (m_masterVolumeSlider != null)
            {
                m_masterVolumeSlider.value = soundManager.MasterVolume;
            }

            if (m_sfxVolumeSlider != null)
            {
                m_sfxVolumeSlider.value = soundManager.SfxVolume;
            }

            if (m_musicVolumeSlider != null)
            {
                m_musicVolumeSlider.value = soundManager.MusicVolume;
            }

            if (m_ambientVolumeSlider != null)
            {
                m_ambientVolumeSlider.value = soundManager.AmbientVolume;
            }

            if (m_masterMuteToggle != null)
            {
                m_masterMuteToggle.isOn = !soundManager.MasterMuted;
            }

            if (m_sfxMuteToggle != null)
            {
                m_sfxMuteToggle.isOn = !soundManager.SfxMuted;
            }

            if (m_musicMuteToggle != null)
            {
                m_musicMuteToggle.isOn = !soundManager.MusicMuted;
            }

            if (m_ambientMuteToggle != null)
            {
                m_ambientMuteToggle.isOn = !soundManager.AmbientMuted;
            }

            m_isUpdatingUI = false;

            DebugBase.Log($"[{nameof(SettingsMenuUI)}] UI refreshed from SoundManager state", DebugCategory.UI);
        }

        // --- Slider Handlers ---

        private void OnMasterVolumeChanged(float value)
        {
            if (m_isUpdatingUI)
            {
                return;
            }

            GameManager.Instance?.SoundManager?.SetMasterVolume(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (m_isUpdatingUI)
            {
                return;
            }

            GameManager.Instance?.SoundManager?.SetSFXVolume(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (m_isUpdatingUI)
            {
                return;
            }

            GameManager.Instance?.SoundManager?.SetMusicVolume(value);
        }

        private void OnAmbientVolumeChanged(float value)
        {
            if (m_isUpdatingUI)
            {
                return;
            }

            GameManager.Instance?.SoundManager?.SetAmbientVolume(value);
        }

        // --- Toggle Handlers ---

        private void OnMasterMuteChanged(bool isOn)
        {
            if (m_isUpdatingUI)
            {
                return;
            }

            GameManager.Instance?.SoundManager?.SetMasterMute(!isOn);
        }

        private void OnSfxMuteChanged(bool isOn)
        {
            if (m_isUpdatingUI)
            {
                return;
            }

            GameManager.Instance?.SoundManager?.SetSFXMute(!isOn);
        }

        private void OnMusicMuteChanged(bool isOn)
        {
            if (m_isUpdatingUI)
            {
                return;
            }

            GameManager.Instance?.SoundManager?.SetMusicMute(!isOn);
        }

        private void OnAmbientMuteChanged(bool isOn)
        {
            if (m_isUpdatingUI)
            {
                return;
            }

            GameManager.Instance?.SoundManager?.SetAmbientMute(!isOn);
        }

        // --- SoundManager Event Handlers ---

        private void OnSoundManagerVolumeChanged(AudioChannel channel, float volume)
        {
            m_isUpdatingUI = true;

            switch (channel)
            {
                case AudioChannel.Master:
                    if (m_masterVolumeSlider != null)
                    {
                        m_masterVolumeSlider.value = volume;
                    }

                    break;

                case AudioChannel.SFX:
                    if (m_sfxVolumeSlider != null)
                    {
                        m_sfxVolumeSlider.value = volume;
                    }

                    break;

                case AudioChannel.Music:
                    if (m_musicVolumeSlider != null)
                    {
                        m_musicVolumeSlider.value = volume;
                    }

                    break;

                case AudioChannel.Ambient:
                    if (m_ambientVolumeSlider != null)
                    {
                        m_ambientVolumeSlider.value = volume;
                    }

                    break;
            }

            m_isUpdatingUI = false;
        }

        private void OnSoundManagerMuteChanged(AudioChannel channel, bool muted)
        {
            m_isUpdatingUI = true;

            switch (channel)
            {
                case AudioChannel.Master:
                    if (m_masterMuteToggle != null)
                    {
                        m_masterMuteToggle.isOn = !muted;
                    }

                    break;

                case AudioChannel.SFX:
                    if (m_sfxMuteToggle != null)
                    {
                        m_sfxMuteToggle.isOn = !muted;
                    }

                    break;

                case AudioChannel.Music:
                    if (m_musicMuteToggle != null)
                    {
                        m_musicMuteToggle.isOn = !muted;
                    }

                    break;

                case AudioChannel.Ambient:
                    if (m_ambientMuteToggle != null)
                    {
                        m_ambientMuteToggle.isOn = !muted;
                    }

                    break;
            }

            m_isUpdatingUI = false;
        }

        // --- Close ---

        private void OnCloseButtonClicked()
        {
            if (GameManager.Instance?.MenuManager != null)
            {
                GameManager.Instance.MenuManager.CloseCurrentMenu();
            }
        }

        // --- Quit ---

        private void OnQuitButtonClicked()
        {
            DebugBase.Log($"[{nameof(SettingsMenuUI)}] Quit clicked, showing warning", DebugCategory.UI);
            ShowQuitWarning();
        }

        private void OnQuitWarningYesClicked()
        {
            DebugBase.Log($"[{nameof(SettingsMenuUI)}] Quit confirmed", DebugCategory.UI);
            GameManager.Instance?.SaveGame();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnQuitWarningNoClicked()
        {
            DebugBase.Log($"[{nameof(SettingsMenuUI)}] Quit cancelled", DebugCategory.UI);
            HideQuitWarning();
        }

        private void ShowQuitWarning()
        {
            if (m_quitWarningPanel != null)
            {
                m_quitWarningPanel.transform.SetAsLastSibling();
                m_quitWarningPanel.SetActive(true);
            }
        }

        private void HideQuitWarning()
        {
            if (m_quitWarningPanel != null)
            {
                m_quitWarningPanel.SetActive(false);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_closeButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] Close Button reference is missing!", this);
            }

            if (m_masterVolumeSlider == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] Master Volume Slider reference is missing!", this);
            }

            if (m_masterMuteToggle == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] Master Mute Toggle reference is missing!", this);
            }

            if (m_sfxVolumeSlider == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] SFX Volume Slider reference is missing!", this);
            }

            if (m_sfxMuteToggle == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] SFX Mute Toggle reference is missing!", this);
            }

            if (m_musicVolumeSlider == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] Music Volume Slider reference is missing!", this);
            }

            if (m_musicMuteToggle == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] Music Mute Toggle reference is missing!", this);
            }

            if (m_ambientVolumeSlider == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] Ambient Volume Slider reference is missing!", this);
            }

            if (m_ambientMuteToggle == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] Ambient Mute Toggle reference is missing!", this);
            }

            if (m_quitButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] Quit Button reference is missing!", this);
            }

            if (m_quitWarningPanel == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] Quit Warning Panel reference is missing!", this);
            }

            if (m_quitWarningYesButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] Quit Warning Yes Button reference is missing!", this);
            }

            if (m_quitWarningNoButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsMenuUI)}] Quit Warning No Button reference is missing!", this);
            }
        }
#endif
    }
}

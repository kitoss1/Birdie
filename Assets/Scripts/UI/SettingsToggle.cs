using System;
using Birdie.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// Custom toggle for settings that shows "On"/"Off" text and swaps a sprite instead of using a checkmark.
    /// Attach alongside a Unity Toggle component. Set the Toggle's Graphic field to None in the Inspector
    /// to suppress the default checkmark.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class SettingsToggle : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField]
        [Tooltip("Image whose sprite changes based on the toggle state")]
        private Image m_backgroundImage;

        [SerializeField]
        [Tooltip("Sprite shown when the toggle is On")]
        private Sprite m_onSprite;

        [SerializeField]
        [Tooltip("Sprite shown when the toggle is Off")]
        private Sprite m_offSprite;

        [Header("Label")]
        [SerializeField]
        [Tooltip("Text label that shows the current state")]
        private TextMeshProUGUI m_stateLabel;

        [SerializeField]
        [Tooltip("Text displayed when the toggle is On")]
        private string m_onText = "On";

        [SerializeField]
        [Tooltip("Text displayed when the toggle is Off")]
        private string m_offText = "Off";

        private Toggle m_toggle;

        /// <summary>
        /// Mirrors Unity Toggle's onValueChanged so callers can subscribe without
        /// needing a direct reference to the underlying Toggle.
        /// </summary>
        public event Action<bool> OnValueChanged;

        /// <summary>
        /// Gets or sets the toggle state programmatically without triggering listeners.
        /// </summary>
        public bool IsOn
        {
            get => m_toggle != null && m_toggle.isOn;
            set
            {
                if (m_toggle != null)
                {
                    m_toggle.isOn = value;
                }
            }
        }

        private void Awake()
        {
            m_toggle = GetComponent<Toggle>();
            m_toggle.onValueChanged.AddListener(HandleValueChanged);
        }

        private void OnEnable()
        {
            RefreshVisuals(m_toggle != null && m_toggle.isOn);
        }

        private void OnDestroy()
        {
            if (m_toggle != null)
            {
                m_toggle.onValueChanged.RemoveListener(HandleValueChanged);
            }
        }

        private void HandleValueChanged(bool isOn)
        {
            RefreshVisuals(isOn);
            OnValueChanged?.Invoke(isOn);
        }

        private void RefreshVisuals(bool isOn)
        {
            if (m_stateLabel != null)
            {
                m_stateLabel.text = isOn ? m_onText : m_offText;
            }

            if (m_backgroundImage != null)
            {
                m_backgroundImage.sprite = isOn ? m_onSprite : m_offSprite;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_backgroundImage == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsToggle)}] Background Image reference is missing!", this);
            }

            if (m_onSprite == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsToggle)}] On Sprite is not assigned!", this);
            }

            if (m_offSprite == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsToggle)}] Off Sprite is not assigned!", this);
            }

            if (m_stateLabel == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SettingsToggle)}] State Label reference is missing!", this);
            }
        }
#endif
    }
}

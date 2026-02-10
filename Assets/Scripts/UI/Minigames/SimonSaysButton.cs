using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Individual color button for the Simon Says minigame.
    /// Handles highlight and press animations via DOTween.
    /// </summary>
    public sealed class SimonSaysButton : MonoBehaviour
    {
        [Tooltip("Image component used to display the button color")]
        [SerializeField] private Image m_buttonImage;

        [Tooltip("Button component for click interaction")]
        [SerializeField] private Button m_button;

        [Tooltip("Index identifying this button's color (0-3)")]
        [SerializeField] private int m_colorIndex;

        [Tooltip("The button's default resting color")]
        [SerializeField] private Color m_normalColor = Color.white;

        [Tooltip("The bright color shown during highlights and presses")]
        [SerializeField] private Color m_highlightColor = Color.yellow;

        [Header("Animation Settings")]
        [Tooltip("Duration of the fade to highlight color")]
        [SerializeField] private float m_highlightFadeIn = 0.2f;

        [Tooltip("How long the highlight color is held")]
        [SerializeField] private float m_highlightHold = 0.3f;
        
        [Tooltip("Duration of the fade back to normal color")]
        [SerializeField] private float m_highlightFadeOut = 0.2f;

        [Tooltip("Duration of the fade back to normal color when clicked")]
        [SerializeField] private float m_highlightFadeOutWhenClicked = 0.6f;
        /// <summary>
        /// Fired when the player clicks this button. Passes the color index.
        /// </summary>
        public event Action<int> Pressed;

        private void Awake()
        {
            if (m_button != null)
            {
                m_button.onClick.AddListener(OnButtonClicked);
            }

            if (m_buttonImage != null)
            {
                m_buttonImage.color = m_normalColor;
            }
        }

        private void OnDestroy()
        {
            if (m_button != null)
            {
                m_button.onClick.RemoveListener(OnButtonClicked);
            }

            if (m_buttonImage != null)
            {
                m_buttonImage.DOKill();
            }
        }

        /// <summary>
        /// Plays the full highlight animation sequence used during sequence playback.
        /// Fades to highlight color, holds, then fades back.
        /// </summary>
        public async UniTask PlayHighlightAsync()
        {
            if (m_buttonImage == null)
            {
                return;
            }

            m_buttonImage.DOKill();

            await m_buttonImage.DOColor(m_highlightColor, m_highlightFadeIn)
                .AsyncWaitForCompletion();

            await UniTask.Delay(TimeSpan.FromSeconds(m_highlightHold));

            await m_buttonImage.DOColor(m_normalColor, m_highlightFadeOut)
                .AsyncWaitForCompletion();
        }

        /// <summary>
        /// Plays a quick press animation when the player clicks the button.
        /// Snaps to highlight color and fades back (fire-and-forget).
        /// </summary>
        public void PlayPressAnimation()
        {
            if (m_buttonImage == null)
            {
                return;
            }

            m_buttonImage.DOKill();
            m_buttonImage.color = m_highlightColor;
            m_buttonImage.DOColor(m_normalColor, m_highlightFadeOutWhenClicked);
        }

        /// <summary>
        /// Toggles whether the button can be clicked.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (m_button != null)
            {
                m_button.interactable = interactable;
            }
        }

        private void OnButtonClicked()
        {
            Pressed?.Invoke(m_colorIndex);
        }
    }
}

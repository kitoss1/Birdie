using Birdie.Debug;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// Handles the loading screen UI, including progress display and fade transitions.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField]
        [Tooltip("Canvas group for fading the entire loading screen")]
        private CanvasGroup m_canvasGroup;

        [SerializeField]
        [Tooltip("Progress bar fill image (optional)")]
        private Image m_progressBar;

        [SerializeField]
        [Tooltip("Progress percentage text (optional)")]
        private TextMeshProUGUI m_progressText;

        [SerializeField]
        [Tooltip("Loading status message text (optional)")]
        private TextMeshProUGUI m_statusText;

        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("Duration for fade in/out animations")]
        private float m_fadeDuration = 0.5f;

        [SerializeField]
        [Tooltip("Delay before starting fade out after loading completes")]
        private float m_fadeOutDelay = 0.3f;

        private bool m_isVisible = false;

        private void Awake()
        {
            // Ensure loading screen starts visible
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 1f;
                m_canvasGroup.blocksRaycasts = true;
            }

            gameObject.SetActive(true);
            m_isVisible = true;

            DebugBase.Log($"[{nameof(LoadingScreen)}] Initialized and visible");
        }

        /// <summary>
        /// Updates the loading progress.
        /// </summary>
        /// <param name="progress">Progress value between 0 and 1.</param>
        /// <param name="statusMessage">Optional status message to display.</param>
        public void UpdateProgress(float progress, string statusMessage = null)
        {
            progress = Mathf.Clamp01(progress);

            if (m_progressBar != null)
            {
                m_progressBar.fillAmount = progress;
            }

            if (m_progressText != null)
            {
                m_progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }

            if (m_statusText != null && !string.IsNullOrEmpty(statusMessage))
            {
                m_statusText.text = statusMessage;
            }
        }

        /// <summary>
        /// Fades in the loading screen.
        /// </summary>
        public async UniTask FadeInAsync()
        {
            if (!m_isVisible)
            {
                gameObject.SetActive(true);

                if (m_canvasGroup != null)
                {
                    m_canvasGroup.blocksRaycasts = true;
                    await m_canvasGroup.DOFade(1f, m_fadeDuration).AsyncWaitForCompletion();
                }

                m_isVisible = true;
                DebugBase.Log($"[{nameof(LoadingScreen)}] Faded in");
            }
        }

        /// <summary>
        /// Fades out the loading screen and deactivates it.
        /// </summary>
        public async UniTask FadeOutAsync()
        {
            if (m_isVisible)
            {
                // Optional delay before fade out
                if (m_fadeOutDelay > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(m_fadeOutDelay));
                }

                if (m_canvasGroup != null)
                {
                    m_canvasGroup.blocksRaycasts = false;
                    await m_canvasGroup.DOFade(0f, m_fadeDuration).AsyncWaitForCompletion();
                }

                gameObject.SetActive(false);
                m_isVisible = false;

                DebugBase.Log($"[{nameof(LoadingScreen)}] Faded out");
            }
        }

        /// <summary>
        /// Shows the loading screen immediately without animation.
        /// </summary>
        public void ShowImmediate()
        {
            gameObject.SetActive(true);

            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 1f;
                m_canvasGroup.blocksRaycasts = true;
            }

            m_isVisible = true;
        }

        /// <summary>
        /// Hides the loading screen immediately without animation.
        /// </summary>
        public void HideImmediate()
        {
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 0f;
                m_canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
            m_isVisible = false;
        }
    }
}

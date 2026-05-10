using System;
using System.Threading;
using Birdie.Debug;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Birdie.UI.Toast
{
    public sealed class ToastUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_label;
        [SerializeField] private CanvasGroup m_canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float m_fadeDuration = 0.25f;
        [SerializeField] private float m_displayDuration = 1.5f;
        [SerializeField] private float m_floatDistance = 60f;
        [SerializeField] private float m_floatDuration = 0.4f;

        private const float k_dismissDuration = 0.1f;

        public async UniTask ShowAsync(string text, ToastSettings settings = null, CancellationToken cancellationToken = default)
        {
            float fadeDuration = settings != null ? settings.FadeDuration : m_fadeDuration;
            float displayDuration = settings != null ? settings.DisplayDuration : m_displayDuration;
            float floatDistance = settings != null ? settings.FloatDistance : m_floatDistance;
            float floatDuration = settings != null ? settings.FloatDuration : m_floatDuration;

            m_label.text = text;
            if (settings != null) m_label.color = settings.TextColor;
            m_canvasGroup.alpha = 0f;
            m_canvasGroup.blocksRaycasts = false;

            // Fire fade-in without awaiting — UniTask.Delay drives the timing so the
            // cancellation token is checked by UniTask, not by DOTween.
            m_canvasGroup.DOFade(1f, fadeDuration);
            if (await WaitCancellableAsync(fadeDuration, cancellationToken))
            {
                await QuickDismissAsync();
                Destroy(gameObject);
                return;
            }

            if (await WaitCancellableAsync(displayDuration, cancellationToken))
            {
                await QuickDismissAsync();
                Destroy(gameObject);
                return;
            }

            await PlayFadeOutAsync(fadeDuration, floatDistance, floatDuration);
            DebugBase.Log($"[{nameof(ToastUI)}] Toast finished: \"{text}\"");
            Destroy(gameObject);
        }

        /// <summary>Returns true if cancelled before the duration elapsed, false otherwise.</summary>
        private static async UniTask<bool> WaitCancellableAsync(float duration, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken);
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }

        private async UniTask QuickDismissAsync()
        {
            // Kill any in-progress tweens then fade to transparent.
            m_canvasGroup.DOKill();
            transform.DOKill();
            await m_canvasGroup.DOFade(0f, k_dismissDuration).AsyncWaitForCompletion();
        }

        private async UniTask PlayFadeOutAsync(float fadeDuration, float floatDistance, float floatDuration)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            float targetY = rectTransform.anchoredPosition.y + floatDistance;

            await UniTask.WhenAll(
                rectTransform.DOAnchorPosY(targetY, floatDuration).AsyncWaitForCompletion().AsUniTask(),
                m_canvasGroup.DOFade(0f, fadeDuration).AsyncWaitForCompletion().AsUniTask()
            );
        }
    }
}

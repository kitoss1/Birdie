using System;
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

        public async UniTask ShowAsync(string text)
        {
            m_label.text = text;
            m_canvasGroup.alpha = 0f;

            await m_canvasGroup.DOFade(1f, m_fadeDuration).AsyncWaitForCompletion();
            await UniTask.Delay(TimeSpan.FromSeconds(m_displayDuration));
            await PlayFadeOutAsync();

            DebugBase.Log($"[{nameof(ToastUI)}] Toast finished: \"{text}\"");
            Destroy(gameObject);
        }

        private async UniTask PlayFadeOutAsync()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            float targetY = rectTransform.anchoredPosition.y + m_floatDistance;

            await UniTask.WhenAll(
                rectTransform.DOAnchorPosY(targetY, m_fadeDuration).AsyncWaitForCompletion().AsUniTask(),
                m_canvasGroup.DOFade(0f, m_fadeDuration).AsyncWaitForCompletion().AsUniTask()
            );
        }
    }
}

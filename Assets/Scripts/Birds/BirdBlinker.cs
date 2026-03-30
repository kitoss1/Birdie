using System.Threading;
using Birdie.Debug;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Simulates bird blinking by periodically enabling and disabling the eye sprite.
    /// </summary>
    public sealed class BirdBlinker : MonoBehaviour
    {
        [SerializeField] private GameObject m_eyeSprite;

        [Header("Blink Timing")]
        [SerializeField] [Range(0.05f, 0.5f)] private float m_blinkDuration = 0.15f;
        [SerializeField] [Range(1f, 10f)] private float m_blinkIntervalMin = 2f;
        [SerializeField] [Range(1f, 15f)] private float m_blinkIntervalMax = 6f;

        private CancellationTokenSource m_cancellationTokenSource;

        private void OnEnable()
        {
            m_cancellationTokenSource = new CancellationTokenSource();
            BlinkLoopAsync(m_cancellationTokenSource.Token).Forget();
        }

        private void OnDisable()
        {
            m_cancellationTokenSource?.Cancel();
            m_cancellationTokenSource?.Dispose();
            m_cancellationTokenSource = null;

            SetEyeActive(true);
        }

        private async UniTaskVoid BlinkLoopAsync(CancellationToken cancellationToken)
        {
            if (m_eyeSprite == null)
            {
                DebugBase.LogWarning($"[{nameof(BirdBlinker)}] Eye sprite is not assigned!", DebugCategory.Birds);
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                float interval = Random.Range(m_blinkIntervalMin, m_blinkIntervalMax);
                await UniTask.WaitForSeconds(interval, cancellationToken: cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await BlinkOnceAsync(cancellationToken);
            }
        }

        private async UniTask BlinkOnceAsync(CancellationToken cancellationToken)
        {
            SetEyeActive(false);
            await UniTask.WaitForSeconds(m_blinkDuration, cancellationToken: cancellationToken);
            SetEyeActive(true);
        }

        private void SetEyeActive(bool active)
        {
            if (m_eyeSprite != null)
            {
                m_eyeSprite.SetActive(active);
            }
        }
    }
}

using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    public class ResourceBarTracker : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Image component with fill type set to Filled")]
        private Image m_bar;

        [SerializeField]
        [Tooltip("Text displaying the current/max values")]
        private TextMeshProUGUI m_valueText;

        private int m_resourceCurrent;
        private int m_resourceMax;

        /// <summary>
        /// Sets the current and max values, then updates the bar fill and text.
        /// </summary>
        public void SetValues(int current, int max)
        {
            m_resourceCurrent = current;
            m_resourceMax = max;
            UpdateBar();
        }

        /// <summary>
        /// Animates the bar from a starting fill ratio to the new values.
        /// </summary>
        public async UniTask AnimateAsync(float fromFill, int toCurrent, int toMax, float duration)
        {
            m_resourceCurrent = toCurrent;
            m_resourceMax = toMax;

            if (m_bar != null)
            {
                m_bar.fillAmount = fromFill;
                float targetFill = m_resourceMax <= 0 ? 0f : Mathf.Clamp01((float)m_resourceCurrent / m_resourceMax);
                await m_bar.DOFillAmount(targetFill, duration)
                    .SetEase(Ease.OutCubic)
                    .AsyncWaitForCompletion();
            }

            if (m_valueText != null)
            {
                m_valueText.text = $"{m_resourceCurrent} / {m_resourceMax}";
            }
        }

        private void UpdateBar()
        {
            if (m_bar != null)
            {
                if (m_resourceMax <= 0)
                {
                    m_bar.fillAmount = 0f;
                }
                else
                {
                    m_bar.fillAmount = (float)m_resourceCurrent / m_resourceMax;
                }
            }

            if (m_valueText != null)
            {
                m_valueText.text = $"{m_resourceCurrent} / {m_resourceMax}";
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_bar == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(ResourceBarTracker)}] Bar Image reference is missing!", this);
            }

            if (m_valueText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(ResourceBarTracker)}] Value Text reference is missing!", this);
            }
        }
#endif
    }
}

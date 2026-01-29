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

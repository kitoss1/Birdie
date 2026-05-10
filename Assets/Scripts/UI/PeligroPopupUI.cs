using Birdie.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    public sealed class PeligroPopupUI : MonoBehaviour
    {
        [SerializeField] private Image m_icon;
        [SerializeField] private TextMeshProUGUI m_descriptionText;
        [SerializeField] private Button m_backdropButton;

        private void Awake()
        {
            if (m_backdropButton != null)
                m_backdropButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (m_backdropButton != null)
                m_backdropButton.onClick.RemoveListener(Hide);
        }

        public void Show(Sprite icon, string description)
        {
            if (m_icon != null && icon != null)
                m_icon.sprite = icon;

            if (m_descriptionText != null)
                m_descriptionText.text = description;

            gameObject.SetActive(true);
            DebugBase.Log($"[{nameof(PeligroPopupUI)}] Showing peligro popup");
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_icon == null)
                UnityEngine.Debug.LogWarning($"[{nameof(PeligroPopupUI)}] Icon reference is missing!", this);

            if (m_descriptionText == null)
                UnityEngine.Debug.LogWarning($"[{nameof(PeligroPopupUI)}] Description Text reference is missing!", this);

            if (m_backdropButton == null)
                UnityEngine.Debug.LogWarning($"[{nameof(PeligroPopupUI)}] Backdrop Button reference is missing!", this);
        }
#endif
    }
}

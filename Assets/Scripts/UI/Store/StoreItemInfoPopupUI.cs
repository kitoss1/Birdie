using Birdie.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI.Store
{
    /// <summary>
    /// Popup that displays detailed information about a store item,
    /// including its image and description.
    /// </summary>
    public sealed class StoreItemInfoPopupUI : MonoBehaviour
    {
        [SerializeField] private Image m_itemImage;
        [SerializeField] private TextMeshProUGUI m_itemNameText;
        [SerializeField] private TextMeshProUGUI m_descriptionText;
        [SerializeField] private Button m_closeButton;

        private void Awake()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(Hide);
            }
        }

        private void OnDestroy()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(Hide);
            }
        }

        public void Show(StoreItemData itemData)
        {
            if (itemData == null)
            {
                return;
            }

            if (m_itemImage != null && itemData.Icon != null)
            {
                m_itemImage.sprite = itemData.Icon;
            }

            if (m_itemNameText != null)
            {
                m_itemNameText.text = itemData.ItemName;
            }

            if (m_descriptionText != null)
            {
                m_descriptionText.text = itemData.Description;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_itemImage == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemInfoPopupUI)}] Item Image reference is missing!", this);
            }

            if (m_descriptionText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemInfoPopupUI)}] Description Text reference is missing!", this);
            }

            if (m_closeButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemInfoPopupUI)}] Close Button reference is missing!", this);
            }
        }
#endif
    }
}

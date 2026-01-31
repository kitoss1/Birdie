using System;
using Birdie.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI.Store
{
    /// <summary>
    /// UI component for displaying a single store item.
    /// Shows icon, name, price, and handles purchase button interaction.
    /// </summary>
    public class StoreItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Image displaying the item icon")]
        private Image m_iconImage;

        [SerializeField]
        [Tooltip("Text displaying the item name")]
        private TextMeshProUGUI m_nameText;

        [SerializeField]
        [Tooltip("Text displaying the item price")]
        private TextMeshProUGUI m_priceText;

        [SerializeField]
        [Tooltip("Button to purchase the item")]
        private Button m_buyButton;

        [SerializeField]
        [Tooltip("Text on the buy button")]
        private TextMeshProUGUI m_buyButtonText;

        [Header("Visual States")]
        [SerializeField]
        [Tooltip("GameObject shown when item is already owned")]
        private GameObject m_ownedOverlay;

        [SerializeField]
        [Tooltip("Color for price text when player can afford")]
        private Color m_affordableColor = Color.white;

        [SerializeField]
        [Tooltip("Color for price text when player cannot afford")]
        private Color m_unaffordableColor = Color.red;

        private StoreItemData m_itemData;
        private bool m_isOwned;

        /// <summary>
        /// Event fired when the buy button is clicked.
        /// </summary>
        public event Action<StoreItemData> OnBuyClicked;

        public StoreItemData ItemData => m_itemData;

        /// <summary>
        /// Initializes the item display with data.
        /// </summary>
        public void Setup(StoreItemData itemData, bool isOwned, bool canAfford)
        {
            m_itemData = itemData;
            m_isOwned = isOwned;

            if (m_iconImage != null && itemData.Icon != null)
            {
                m_iconImage.sprite = itemData.Icon;
            }

            if (m_nameText != null)
            {
                m_nameText.text = itemData.ItemName;
            }

            if (m_priceText != null)
            {
                m_priceText.text = itemData.Price.ToString();
            }

            UpdateVisualState(isOwned, canAfford);
        }

        /// <summary>
        /// Updates the visual state based on ownership and affordability.
        /// </summary>
        public void UpdateVisualState(bool isOwned, bool canAfford)
        {
            m_isOwned = isOwned;

            if (m_ownedOverlay != null)
            {
                m_ownedOverlay.SetActive(isOwned);
            }

            if (m_buyButton != null)
            {
                m_buyButton.interactable = !isOwned && canAfford;
            }

            if (m_buyButtonText != null)
            {
                m_buyButtonText.text = isOwned ? "Owned" : "Buy";
            }

            if (m_priceText != null && !isOwned)
            {
                m_priceText.color = canAfford ? m_affordableColor : m_unaffordableColor;
            }
        }

        private void Awake()
        {
            if (m_buyButton != null)
            {
                m_buyButton.onClick.AddListener(OnBuyButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (m_buyButton != null)
            {
                m_buyButton.onClick.RemoveListener(OnBuyButtonClicked);
            }
        }

        private void OnBuyButtonClicked()
        {
            if (m_itemData != null && !m_isOwned)
            {
                OnBuyClicked?.Invoke(m_itemData);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_iconImage == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemUI)}] Icon Image reference is missing!", this);
            }

            if (m_nameText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemUI)}] Name Text reference is missing!", this);
            }

            if (m_priceText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemUI)}] Price Text reference is missing!", this);
            }

            if (m_buyButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemUI)}] Buy Button reference is missing!", this);
            }
        }
#endif
    }
}

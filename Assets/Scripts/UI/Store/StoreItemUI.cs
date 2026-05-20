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
        [Tooltip("Currency icon shown next to the price")]
        private GameObject m_currencyIcon;

        [SerializeField]
        [Tooltip("Button to purchase the item")]
        private Button m_buyButton;

        [SerializeField]
        [Tooltip("Button to show item info popup")]
        private Button m_infoButton;

        [SerializeField]
        [Tooltip("Button to move the item in the scene")]
        private Button m_moveButton;

        [Header("Visual States")]
        [SerializeField]
        [Tooltip("Color for price text when player can afford")]
        private Color m_affordableColor = Color.white;

        [SerializeField]
        [Tooltip("Color for price text when player cannot afford")]
        private Color m_unaffordableColor = Color.red;

        [Header("Move Button Sprites")]
        [SerializeField]
        [Tooltip("Sprite for the move button when interactable")]
        private Sprite m_moveEnabledSprite;

        [SerializeField]
        [Tooltip("Sprite for the move button when not interactable")]
        private Sprite m_moveDisabledSprite;

        [Header("Buy Button Sprites")]
        [SerializeField]
        [Tooltip("Sprite for the buy button when item is not owned (showing price)")]
        private Sprite m_buySprite;

        [SerializeField]
        [Tooltip("Sprite for the buy button when item is owned and enabled (showing Disable)")]
        private Sprite m_disableSprite;

        [SerializeField]
        [Tooltip("Sprite for the buy button when item is owned and disabled (showing Enable)")]
        private Sprite m_enableSprite;

        private StoreItemData m_itemData;
        private bool m_isOwned;
        private Image m_buyButtonImage;
        private Image m_moveButtonImage;

        /// <summary>
        /// Event fired when the buy button is clicked (for non-owned items).
        /// </summary>
        public event Action<StoreItemData> OnBuyClicked;

        /// <summary>
        /// Event fired when the toggle button is clicked (for owned items).
        /// </summary>
        public event Action<StoreItemData> OnToggleClicked;

        /// <summary>
        /// Event fired when the info button is clicked.
        /// </summary>
        public event Action<StoreItemData> OnInfoClicked;

        /// <summary>
        /// Event fired when the move button is clicked.
        /// </summary>
        public event Action<StoreItemData> OnMoveClicked;

        public StoreItemData ItemData => m_itemData;

        /// <summary>
        /// Initializes the item display with data.
        /// </summary>
        public void Setup(StoreItemData itemData, bool isOwned, bool isEnabled, bool canAfford)
        {
            m_itemData = itemData;

            if (m_iconImage != null && itemData.Icon != null)
            {
                m_iconImage.sprite = itemData.Icon;
            }

            if (m_nameText != null)
            {
                m_nameText.text = itemData.ItemName;
            }

            UpdateVisualState(isOwned, isEnabled, canAfford);
        }

        /// <summary>
        /// Updates the visual state based on ownership, enabled state, affordability, and whether disabling is allowed.
        /// </summary>
        public void UpdateVisualState(bool isOwned, bool isEnabled, bool canAfford)
        {
            m_isOwned = isOwned;

            if (m_buyButton != null)
            {
                m_buyButton.interactable = isOwned || canAfford;

                if (m_buyButtonImage != null)
                {
                    m_buyButtonImage.sprite = !isOwned ? m_buySprite : isEnabled ? m_disableSprite : m_enableSprite;
                }
            }

            if (m_currencyIcon != null)
            {
                m_currencyIcon.SetActive(!isOwned);
            }

            if (m_priceText != null)
            {
                if (isOwned)
                {
                    m_priceText.text = isEnabled ? "Desactivar" : "Activar";
                    m_priceText.color = m_affordableColor;
                }
                else
                {
                    m_priceText.text = m_itemData != null ? m_itemData.Price.ToString() : "0";
                    m_priceText.color = canAfford ? m_affordableColor : m_unaffordableColor;
                }
            }

            if (m_moveButton != null)
            {
                bool isMovable = m_itemData?.IsMovable ?? false;
                m_moveButton.gameObject.SetActive(isMovable);

                if (isMovable)
                {
                    bool moveInteractable = isOwned && isEnabled;
                    m_moveButton.interactable = moveInteractable;

                    if (m_moveButtonImage != null)
                    {
                        m_moveButtonImage.sprite = moveInteractable ? m_moveEnabledSprite : m_moveDisabledSprite;
                    }
                }
            }
        }

        private void Awake()
        {
            if (m_buyButton != null)
            {
                m_buyButton.onClick.AddListener(OnBuyButtonClicked);
                m_buyButtonImage = m_buyButton.GetComponent<Image>();
            }

            if (m_infoButton != null)
            {
                m_infoButton.onClick.AddListener(OnInfoButtonClicked);
            }

            if (m_moveButton != null)
            {
                m_moveButton.onClick.AddListener(OnMoveButtonClicked);
                m_moveButtonImage = m_moveButton.GetComponent<Image>();
            }
        }

        private void OnDestroy()
        {
            if (m_buyButton != null)
            {
                m_buyButton.onClick.RemoveListener(OnBuyButtonClicked);
            }

            if (m_infoButton != null)
            {
                m_infoButton.onClick.RemoveListener(OnInfoButtonClicked);
            }

            if (m_moveButton != null)
            {
                m_moveButton.onClick.RemoveListener(OnMoveButtonClicked);
            }
        }

        private void OnBuyButtonClicked()
        {
            if (m_itemData == null)
            {
                return;
            }

            if (m_isOwned)
            {
                OnToggleClicked?.Invoke(m_itemData);
            }
            else
            {
                OnBuyClicked?.Invoke(m_itemData);
            }
        }

        private void OnInfoButtonClicked()
        {
            if (m_itemData == null)
            {
                return;
            }

            OnInfoClicked?.Invoke(m_itemData);
        }

        private void OnMoveButtonClicked()
        {
            if (m_itemData == null)
            {
                return;
            }

            OnMoveClicked?.Invoke(m_itemData);
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

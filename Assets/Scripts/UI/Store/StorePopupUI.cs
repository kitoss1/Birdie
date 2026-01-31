using System;
using System.Collections.Generic;
using Birdie.Birds;
using Birdie.Data;
using Birdie.Debug;
using Birdie.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI.Store
{
    /// <summary>
    /// Main UI component for the store popup.
    /// Handles tab switching, item display, and purchase interactions.
    /// </summary>
    public class StorePopupUI : MonoBehaviour
    {
        [Header("Tab System")]
        [SerializeField]
        [Tooltip("Button for the Feeders tab")]
        private Button m_feedersTabButton;

        [SerializeField]
        [Tooltip("Button for the Baths tab")]
        private Button m_bathsTabButton;

        [SerializeField]
        [Tooltip("Visual indicator for selected Feeders tab")]
        private GameObject m_feedersTabSelected;

        [SerializeField]
        [Tooltip("Visual indicator for selected Baths tab")]
        private GameObject m_bathsTabSelected;

        [Header("Content")]
        [SerializeField]
        [Tooltip("Container where store items are instantiated")]
        private Transform m_itemsContainer;

        [SerializeField]
        [Tooltip("Prefab for store item UI")]
        private GameObject m_storeItemPrefab;

        [Header("Currency Display")]
        [SerializeField]
        [Tooltip("Text displaying the player's current golden seeds")]
        private TextMeshProUGUI m_currencyText;

        [Header("Controls")]
        [SerializeField]
        [Tooltip("Button to close the store popup")]
        private Button m_closeButton;

        private BirdObjectType m_currentTab = BirdObjectType.Feeder;
        private List<StoreItemUI> m_instantiatedItems = new List<StoreItemUI>();

        /// <summary>
        /// Event fired when an item is purchased.
        /// </summary>
        public event Action<StoreItemData> OnItemPurchased;

        /// <summary>
        /// Event fired when close button is clicked.
        /// </summary>
        public event Action OnCloseClicked;

        private void Awake()
        {
            SetupButtonListeners();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnEnable()
        {
            RefreshCurrencyDisplay();
            SwitchToTab(m_currentTab);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            RemoveButtonListeners();
        }

        private void SetupButtonListeners()
        {
            if (m_feedersTabButton != null)
            {
                m_feedersTabButton.onClick.AddListener(() => SwitchToTab(BirdObjectType.Feeder));
            }

            if (m_bathsTabButton != null)
            {
                m_bathsTabButton.onClick.AddListener(() => SwitchToTab(BirdObjectType.BirdBath));
            }

            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        private void RemoveButtonListeners()
        {
            if (m_feedersTabButton != null)
            {
                m_feedersTabButton.onClick.RemoveAllListeners();
            }

            if (m_bathsTabButton != null)
            {
                m_bathsTabButton.onClick.RemoveAllListeners();
            }

            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance?.EconomyManager != null)
            {
                GameManager.Instance.EconomyManager.OnGoldenSeedsChanged += OnCurrencyChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance?.EconomyManager != null)
            {
                GameManager.Instance.EconomyManager.OnGoldenSeedsChanged -= OnCurrencyChanged;
            }
        }

        /// <summary>
        /// Switches to the specified tab and refreshes the item display.
        /// </summary>
        public void SwitchToTab(BirdObjectType category)
        {
            m_currentTab = category;
            UpdateTabVisuals();
            RefreshItemsDisplay();

            DebugBase.Log($"[{nameof(StorePopupUI)}] Switched to tab: {category}");
        }

        private void UpdateTabVisuals()
        {
            bool isFeedersTab = m_currentTab == BirdObjectType.Feeder;

            if (m_feedersTabSelected != null)
            {
                m_feedersTabSelected.SetActive(isFeedersTab);
            }

            if (m_bathsTabSelected != null)
            {
                m_bathsTabSelected.SetActive(!isFeedersTab);
            }
        }

        private void RefreshItemsDisplay()
        {
            ClearItems();

            if (GameManager.Instance?.StoreManager == null)
            {
                return;
            }

            int currentCurrency = GameManager.Instance?.EconomyManager?.GoldenSeeds ?? 0;
            HashSet<string> ownedItems = GetOwnedItemIDs();

            foreach (StoreItemData itemData in GameManager.Instance.StoreManager.AllStoreItems)
            {
                if (itemData == null || itemData.Category != m_currentTab)
                {
                    continue;
                }

                CreateItemUI(itemData, ownedItems.Contains(itemData.ItemID), currentCurrency >= itemData.Price);
            }
        }

        private void ClearItems()
        {
            foreach (StoreItemUI item in m_instantiatedItems)
            {
                if (item != null)
                {
                    item.OnBuyClicked -= OnItemBuyClicked;
                    Destroy(item.gameObject);
                }
            }

            m_instantiatedItems.Clear();
        }

        private void CreateItemUI(StoreItemData itemData, bool isOwned, bool canAfford)
        {
            if (m_storeItemPrefab == null || m_itemsContainer == null)
            {
                return;
            }

            GameObject itemObj = Instantiate(m_storeItemPrefab, m_itemsContainer);
            StoreItemUI itemUI = itemObj.GetComponent<StoreItemUI>();

            if (itemUI != null)
            {
                itemUI.Setup(itemData, isOwned, canAfford);
                itemUI.OnBuyClicked += OnItemBuyClicked;
                m_instantiatedItems.Add(itemUI);
            }
        }

        private void OnItemBuyClicked(StoreItemData itemData)
        {
            if (GameManager.Instance?.StoreManager == null)
            {
                return;
            }

            if (GameManager.Instance.StoreManager.TryPurchaseItem(itemData.ItemID))
            {
                DebugBase.Log($"[{nameof(StorePopupUI)}] Purchased item: {itemData.ItemName}");
                OnItemPurchased?.Invoke(itemData);
                RefreshItemsDisplay();
            }
        }

        private void OnCurrencyChanged(int newBalance, int change)
        {
            RefreshCurrencyDisplay();
            UpdateItemAffordability();
        }

        private void RefreshCurrencyDisplay()
        {
            if (m_currencyText != null)
            {
                int currency = GameManager.Instance?.EconomyManager?.GoldenSeeds ?? 0;
                m_currencyText.text = currency.ToString();
            }
        }

        private void UpdateItemAffordability()
        {
            int currentCurrency = GameManager.Instance?.EconomyManager?.GoldenSeeds ?? 0;
            HashSet<string> ownedItems = GetOwnedItemIDs();

            foreach (StoreItemUI itemUI in m_instantiatedItems)
            {
                if (itemUI?.ItemData != null)
                {
                    bool isOwned = ownedItems.Contains(itemUI.ItemData.ItemID);
                    bool canAfford = currentCurrency >= itemUI.ItemData.Price;
                    itemUI.UpdateVisualState(isOwned, canAfford);
                }
            }
        }

        private HashSet<string> GetOwnedItemIDs()
        {
            HashSet<string> ownedItems = new HashSet<string>();

            if (GameManager.Instance?.StoreManager != null)
            {
                foreach (string itemID in GameManager.Instance.StoreManager.GetOwnedItemIDs())
                {
                    ownedItems.Add(itemID);
                }
            }

            return ownedItems;
        }

        private void OnCloseButtonClicked()
        {
            OnCloseClicked?.Invoke();

            if (GameManager.Instance?.MenuManager != null)
            {
                GameManager.Instance.MenuManager.CloseCurrentMenu();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_feedersTabButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Feeders Tab Button reference is missing!", this);
            }

            if (m_bathsTabButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Baths Tab Button reference is missing!", this);
            }

            if (m_itemsContainer == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Items Container reference is missing!", this);
            }

            if (m_storeItemPrefab == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Store Item Prefab reference is missing!", this);
            }

            if (m_currencyText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Currency Text reference is missing!", this);
            }

            if (m_closeButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Close Button reference is missing!", this);
            }
        }
#endif
    }
}

using System;
using System.Collections;
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
        /// <summary>
        /// Configuration for a single store tab.
        /// </summary>
        [Serializable]
        private struct StoreTabConfig
        {
            [Tooltip("Category this tab displays")]
            public BirdObjectType Category;

            [Tooltip("Button that activates this tab")]
            public Button TabButton;

            [Tooltip("Visual indicator shown when this tab is selected")]
            public GameObject SelectedIndicator;
        }

        [Header("Tab System")]
        [SerializeField]
        [Tooltip("All available store tabs. Add entries here to support new categories without code changes.")]
        private StoreTabConfig[] m_tabs;

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

        [Header("Item Info")]
        [SerializeField]
        [Tooltip("Popup that displays item info when the info button is clicked")]
        private StoreItemInfoPopupUI m_itemInfoPopup;

        [Header("Item Movement")]
        [SerializeField]
        [Tooltip("Handler for moving store items in the scene")]
        private StoreItemMoveHandler m_moveHandler;

        [Header("Feedback")]
        [SerializeField]
        [Tooltip("Text used to show temporary feedback messages to the player")]
        private TextMeshProUGUI m_feedbackText;

        [SerializeField]
        [Tooltip("How long the feedback message stays visible")]
        private float m_feedbackDuration = 2f;

        [Header("Controls")]
        [SerializeField]
        [Tooltip("Button to close the store popup")]
        private Button m_closeButton;

        private BirdObjectType m_currentTab = BirdObjectType.Feeder;
        private List<StoreItemUI> m_instantiatedItems = new List<StoreItemUI>();
        private Coroutine m_feedbackCoroutine;

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
            if (m_itemInfoPopup != null)
            {
                m_itemInfoPopup.Hide();
            }

            HideFeedback();
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
            foreach (StoreTabConfig tab in m_tabs)
            {
                if (tab.TabButton != null)
                {
                    BirdObjectType category = tab.Category;
                    tab.TabButton.onClick.AddListener(() => SwitchToTab(category));
                }
            }

            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        private void RemoveButtonListeners()
        {
            foreach (StoreTabConfig tab in m_tabs)
            {
                if (tab.TabButton != null)
                {
                    tab.TabButton.onClick.RemoveAllListeners();
                }
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
            foreach (StoreTabConfig tab in m_tabs)
            {
                if (tab.SelectedIndicator != null)
                {
                    tab.SelectedIndicator.SetActive(tab.Category == m_currentTab);
                }
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

                bool isOwned = ownedItems.Contains(itemData.ItemID);
                bool isEnabled = GameManager.Instance.StoreManager.IsItemEnabled(itemData.ItemID);
                bool canToggle = GameManager.Instance.StoreManager.CanDisableItem(itemData.ItemID);
                CreateItemUI(itemData, isOwned, isEnabled, currentCurrency >= itemData.Price, canToggle);
            }
        }

        private void ClearItems()
        {
            foreach (StoreItemUI item in m_instantiatedItems)
            {
                if (item != null)
                {
                    item.OnBuyClicked -= OnItemBuyClicked;
                    item.OnToggleClicked -= OnItemToggleClicked;
                    item.OnInfoClicked -= OnItemInfoClicked;
                    item.OnMoveClicked -= OnItemMoveClicked;
                    Destroy(item.gameObject);
                }
            }

            m_instantiatedItems.Clear();
        }

        private void CreateItemUI(StoreItemData itemData, bool isOwned, bool isEnabled, bool canAfford, bool canToggle)
        {
            if (m_storeItemPrefab == null || m_itemsContainer == null)
            {
                return;
            }

            GameObject itemObj = Instantiate(m_storeItemPrefab, m_itemsContainer);
            StoreItemUI itemUI = itemObj.GetComponent<StoreItemUI>();

            if (itemUI != null)
            {
                itemUI.Setup(itemData, isOwned, isEnabled, canAfford, canToggle);
                itemUI.OnBuyClicked += OnItemBuyClicked;
                itemUI.OnToggleClicked += OnItemToggleClicked;
                itemUI.OnInfoClicked += OnItemInfoClicked;
                itemUI.OnMoveClicked += OnItemMoveClicked;
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

        private void OnItemInfoClicked(StoreItemData itemData)
        {
            if (m_itemInfoPopup != null)
            {
                m_itemInfoPopup.Show(itemData);
            }
        }

        private void OnItemMoveClicked(StoreItemData itemData)
        {
            if (GameManager.Instance?.StoreManager == null || m_moveHandler == null)
            {
                return;
            }

            GameObject sceneObject = GameManager.Instance.StoreManager.GetSceneObject(itemData.ItemID);
            if (sceneObject == null)
            {
                DebugBase.LogWarning($"[{nameof(StorePopupUI)}] No scene object found for item: {itemData.ItemName}");
                return;
            }

            BirdObject birdObject = sceneObject.GetComponent<BirdObject>();
            if (birdObject != null && birdObject.IsBeingUsed)
            {
                ShowFeedback("A bird is currently using this item!");
                DebugBase.Log($"[{nameof(StorePopupUI)}] Cannot move item {itemData.ItemName}: a bird is using it");
                return;
            }

            m_moveHandler.StartMoving(sceneObject, itemData.ItemID);

            if (GameManager.Instance?.MenuManager != null)
            {
                GameManager.Instance.MenuManager.CloseCurrentMenu();
            }
        }

        private void OnItemToggleClicked(StoreItemData itemData)
        {
            if (GameManager.Instance?.StoreManager == null)
            {
                return;
            }

            GameManager.Instance.StoreManager.ToggleItemEnabled(itemData.ItemID);
            RefreshItemsDisplay();
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
            if (GameManager.Instance?.StoreManager == null)
            {
                return;
            }

            int currentCurrency = GameManager.Instance?.EconomyManager?.GoldenSeeds ?? 0;
            HashSet<string> ownedItems = GetOwnedItemIDs();

            foreach (StoreItemUI itemUI in m_instantiatedItems)
            {
                if (itemUI?.ItemData != null)
                {
                    bool isOwned = ownedItems.Contains(itemUI.ItemData.ItemID);
                    bool isEnabled = GameManager.Instance.StoreManager.IsItemEnabled(itemUI.ItemData.ItemID);
                    bool canAfford = currentCurrency >= itemUI.ItemData.Price;
                    bool canToggle = GameManager.Instance.StoreManager.CanDisableItem(itemUI.ItemData.ItemID);
                    itemUI.UpdateVisualState(isOwned, isEnabled, canAfford, canToggle);
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

        private void ShowFeedback(string message)
        {
            if (m_feedbackText == null)
            {
                return;
            }

            m_feedbackText.text = message;
            m_feedbackText.gameObject.SetActive(true);

            if (m_feedbackCoroutine != null)
            {
                StopCoroutine(m_feedbackCoroutine);
            }

            m_feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
        }

        private IEnumerator HideFeedbackAfterDelay()
        {
            yield return new WaitForSeconds(m_feedbackDuration);
            HideFeedback();
        }

        private void HideFeedback()
        {
            if (m_feedbackText != null)
            {
                m_feedbackText.gameObject.SetActive(false);
            }

            m_feedbackCoroutine = null;
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
            if (m_tabs == null || m_tabs.Length == 0)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] No tabs configured!", this);
            }
            else
            {
                for (int i = 0; i < m_tabs.Length; i++)
                {
                    if (m_tabs[i].TabButton == null)
                    {
                        UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Tab [{i}] ({m_tabs[i].Category}) is missing a TabButton reference!", this);
                    }

                    if (m_tabs[i].SelectedIndicator == null)
                    {
                        UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Tab [{i}] ({m_tabs[i].Category}) is missing a SelectedIndicator reference!", this);
                    }
                }
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

            if (m_itemInfoPopup == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Item Info Popup reference is missing!", this);
            }

            if (m_moveHandler == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Move Handler reference is missing!", this);
            }

            if (m_feedbackText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Feedback Text reference is missing!", this);
            }

            if (m_closeButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StorePopupUI)}] Close Button reference is missing!", this);
            }
        }
#endif
    }
}

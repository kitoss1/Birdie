using System;
using System.Collections.Generic;
using Birdie.Data;
using Birdie.Debug;
using Birdie.Save;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages the store system: owned items, purchases, and spawning objects in the scene.
    /// Integrates with EconomyManager for purchases and SaveManager for persistence.
    /// </summary>
    public class StoreManager : BaseManager
    {
        [Header("Store Configuration")]
        [SerializeField]
        [Tooltip("Store items mapped to their scene objects")]
        private List<StoreItemSceneReference> m_storeItemReferences = new List<StoreItemSceneReference>();

        private HashSet<string> m_ownedItemIDs = new HashSet<string>();
        private HashSet<string> m_disabledItemIDs = new HashSet<string>();
        private Dictionary<string, StoreItemData> m_itemLookup = new Dictionary<string, StoreItemData>();
        private Dictionary<string, GameObject> m_sceneObjects = new Dictionary<string, GameObject>();
        private SaveManager m_saveManager;

        /// <summary>
        /// Event fired when an item is purchased.
        /// </summary>
        public event Action<StoreItemData> OnItemPurchased;

        /// <summary>
        /// Event fired when an item's enabled state is toggled.
        /// Parameters: itemData, isEnabled
        /// </summary>
        public event Action<StoreItemData, bool> OnItemToggled;

        /// <summary>
        /// Event fired when owned items are loaded from save.
        /// </summary>
        public event Action OnOwnedItemsLoaded;

        private List<StoreItemData> m_allStoreItems = new List<StoreItemData>();

        public IReadOnlyList<StoreItemData> AllStoreItems => m_allStoreItems;

        public override void Initialize()
        {
            base.Initialize();
            BuildItemLookup();
            DebugBase.Log($"[{nameof(StoreManager)}] Store system initialized with {m_allStoreItems.Count} items");
        }

        /// <summary>
        /// Sets the save manager reference and loads owned items.
        /// </summary>
        public void SetSaveManager(SaveManager saveManager)
        {
            m_saveManager = saveManager;
            LoadFromSaveData();
            RefreshSceneObjects();
        }

        /// <summary>
        /// Attempts to purchase an item.
        /// </summary>
        public bool TryPurchaseItem(string itemID)
        {
            if (!EnsureInitialized())
            {
                return false;
            }

            if (IsItemOwned(itemID))
            {
                DebugBase.LogWarning($"[{nameof(StoreManager)}] Item already owned: {itemID}");
                return false;
            }

            if (!m_itemLookup.TryGetValue(itemID, out StoreItemData itemData))
            {
                DebugBase.LogWarning($"[{nameof(StoreManager)}] Unknown item ID: {itemID}");
                return false;
            }

            EconomyManager economyManager = GameManager.Instance?.EconomyManager;
            if (economyManager == null)
            {
                DebugBase.LogWarning($"[{nameof(StoreManager)}] EconomyManager not available");
                return false;
            }

            if (!economyManager.TryPurchase(itemData.Price))
            {
                return false;
            }

            m_ownedItemIDs.Add(itemID);
            SaveToSaveData();
            SetSceneObjectActive(itemID, true);

            DebugBase.Log($"[{nameof(StoreManager)}] Purchased item: {itemData.ItemName}");
            OnItemPurchased?.Invoke(itemData);

            return true;
        }

        /// <summary>
        /// Checks if an item is owned.
        /// </summary>
        public bool IsItemOwned(string itemID)
        {
            return m_ownedItemIDs.Contains(itemID);
        }

        /// <summary>
        /// Checks if an owned item is enabled (visible in scene).
        /// </summary>
        public bool IsItemEnabled(string itemID)
        {
            return IsItemOwned(itemID) && !m_disabledItemIDs.Contains(itemID);
        }

        /// <summary>
        /// Toggles an owned item's enabled state.
        /// </summary>
        public void ToggleItemEnabled(string itemID)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            if (!IsItemOwned(itemID))
            {
                DebugBase.LogWarning($"[{nameof(StoreManager)}] Cannot toggle unowned item: {itemID}");
                return;
            }

            if (!m_itemLookup.TryGetValue(itemID, out StoreItemData itemData))
            {
                DebugBase.LogWarning($"[{nameof(StoreManager)}] Unknown item ID: {itemID}");
                return;
            }

            bool isEnabled;
            if (m_disabledItemIDs.Contains(itemID))
            {
                m_disabledItemIDs.Remove(itemID);
                isEnabled = true;
            }
            else
            {
                m_disabledItemIDs.Add(itemID);
                isEnabled = false;
            }

            SetSceneObjectActive(itemID, isEnabled);

            SaveToSaveData();
            OnItemToggled?.Invoke(itemData, isEnabled);

            DebugBase.Log($"[{nameof(StoreManager)}] Toggled item {itemData.ItemName}: {(isEnabled ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// Gets all owned item IDs.
        /// </summary>
        public IEnumerable<string> GetOwnedItemIDs()
        {
            return m_ownedItemIDs;
        }

        /// <summary>
        /// Gets the scene GameObject associated with a store item.
        /// </summary>
        public GameObject GetSceneObject(string itemID)
        {
            m_sceneObjects.TryGetValue(itemID, out GameObject sceneObject);
            return sceneObject;
        }

        /// <summary>
        /// Saves the X position of a store item and persists to save data.
        /// </summary>
        public void SaveItemPosition(string itemID, float xPosition)
        {
            if (!IsItemOwned(itemID))
            {
                DebugBase.LogWarning($"[{nameof(StoreManager)}] Cannot save position for unowned item: {itemID}");
                return;
            }

            SaveToSaveData();
            DebugBase.Log($"[{nameof(StoreManager)}] Saved position for item {itemID}: x={xPosition}");
        }

        /// <summary>
        /// Gets a store item by ID.
        /// </summary>
        public StoreItemData GetItemData(string itemID)
        {
            m_itemLookup.TryGetValue(itemID, out StoreItemData itemData);
            return itemData;
        }

        private void BuildItemLookup()
        {
            m_itemLookup.Clear();
            m_sceneObjects.Clear();
            m_allStoreItems.Clear();

            foreach (StoreItemSceneReference reference in m_storeItemReferences)
            {
                StoreItemData item = reference.ItemData;
                if (item == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(item.ItemID))
                {
                    DebugBase.LogWarning($"[{nameof(StoreManager)}] Store item has empty ID: {item.name}");
                    continue;
                }

                if (m_itemLookup.ContainsKey(item.ItemID))
                {
                    DebugBase.LogWarning($"[{nameof(StoreManager)}] Duplicate item ID: {item.ItemID}");
                    continue;
                }

                m_itemLookup[item.ItemID] = item;
                m_allStoreItems.Add(item);

                if (reference.SceneObject != null)
                {
                    m_sceneObjects[item.ItemID] = reference.SceneObject;
                }
                else
                {
                    DebugBase.LogWarning($"[{nameof(StoreManager)}] No scene object assigned for item: {item.ItemName}");
                }
            }
        }

        private void LoadFromSaveData()
        {
            if (m_saveManager == null || m_saveManager.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(StoreManager)}] SaveManager or SaveData is null, cannot load");
                return;
            }

            EconomySaveData economyData = m_saveManager.CurrentSaveData.economy;
            m_ownedItemIDs.Clear();

            if (economyData.ownedItemIDs != null)
            {
                foreach (string itemID in economyData.ownedItemIDs)
                {
                    m_ownedItemIDs.Add(itemID);
                }
            }

            m_disabledItemIDs.Clear();
            if (economyData.disabledItemIDs != null)
            {
                foreach (string itemID in economyData.disabledItemIDs)
                {
                    m_disabledItemIDs.Add(itemID);
                }
            }

            LoadItemPositions(economyData);

            DebugBase.Log($"[{nameof(StoreManager)}] Loaded {m_ownedItemIDs.Count} owned items, {m_disabledItemIDs.Count} disabled");
            OnOwnedItemsLoaded?.Invoke();
        }

        private void LoadItemPositions(EconomySaveData economyData)
        {
            if (economyData.itemPositions == null)
            {
                return;
            }

            foreach (ItemPositionEntry entry in economyData.itemPositions)
            {
                if (entry == null || string.IsNullOrEmpty(entry.itemID))
                {
                    continue;
                }

                if (m_sceneObjects.TryGetValue(entry.itemID, out GameObject sceneObject) && sceneObject != null)
                {
                    Vector3 position = sceneObject.transform.position;
                    position.x = entry.xPosition;
                    sceneObject.transform.position = position;
                }
            }
        }

        private void SaveToSaveData()
        {
            if (m_saveManager == null || m_saveManager.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(StoreManager)}] SaveManager or SaveData is null, cannot save");
                return;
            }

            EconomySaveData economyData = m_saveManager.CurrentSaveData.economy;
            economyData.ownedItemIDs = new List<string>(m_ownedItemIDs);
            economyData.disabledItemIDs = new List<string>(m_disabledItemIDs);
            SaveItemPositionsToData(economyData);

            m_saveManager.SaveGame();

            DebugBase.Log($"[{nameof(StoreManager)}] Saved {m_ownedItemIDs.Count} owned items, {m_disabledItemIDs.Count} disabled");
        }

        private void SaveItemPositionsToData(EconomySaveData economyData)
        {
            economyData.itemPositions = new List<ItemPositionEntry>();

            foreach (KeyValuePair<string, GameObject> pair in m_sceneObjects)
            {
                if (pair.Value == null || !m_ownedItemIDs.Contains(pair.Key))
                {
                    continue;
                }

                var entry = new ItemPositionEntry
                {
                    itemID = pair.Key,
                    xPosition = pair.Value.transform.position.x
                };
                economyData.itemPositions.Add(entry);
            }
        }

        private void RefreshSceneObjects()
        {
            int enabledCount = 0;

            foreach (KeyValuePair<string, GameObject> pair in m_sceneObjects)
            {
                bool shouldBeActive = IsItemEnabled(pair.Key);
                pair.Value.SetActive(shouldBeActive);

                if (shouldBeActive)
                {
                    enabledCount++;
                }
            }

            DebugBase.Log($"[{nameof(StoreManager)}] Refreshed scene objects: {enabledCount} enabled");
        }

        private void SetSceneObjectActive(string itemID, bool active)
        {
            if (!m_sceneObjects.TryGetValue(itemID, out GameObject sceneObject))
            {
                DebugBase.LogWarning($"[{nameof(StoreManager)}] No scene object found for item: {itemID}");
                return;
            }

            sceneObject.SetActive(active);
        }

        /// <summary>
        /// Clears all owned items and destroys spawned objects. Used for debugging/reset.
        /// </summary>
        public void ClearOwnedItems()
        {
            m_ownedItemIDs.Clear();
            m_disabledItemIDs.Clear();
            SaveToSaveData();
            RefreshSceneObjects();

            DebugBase.Log($"[{nameof(StoreManager)}] Cleared all owned items");
        }
    }
}

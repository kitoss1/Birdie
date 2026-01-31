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
        [Tooltip("All available store items")]
        private List<StoreItemData> m_allStoreItems = new List<StoreItemData>();

        [SerializeField]
        [Tooltip("Parent transform for spawned scene objects")]
        private Transform m_sceneObjectsParent;

        private HashSet<string> m_ownedItemIDs = new HashSet<string>();
        private Dictionary<string, StoreItemData> m_itemLookup = new Dictionary<string, StoreItemData>();
        private Dictionary<string, GameObject> m_spawnedObjects = new Dictionary<string, GameObject>();
        private SaveManager m_saveManager;

        /// <summary>
        /// Event fired when an item is purchased.
        /// </summary>
        public event Action<StoreItemData> OnItemPurchased;

        /// <summary>
        /// Event fired when owned items are loaded from save.
        /// </summary>
        public event Action OnOwnedItemsLoaded;

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
            SpawnOwnedObjects();
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
            SpawnObject(itemData);

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
        /// Gets all owned item IDs.
        /// </summary>
        public IEnumerable<string> GetOwnedItemIDs()
        {
            return m_ownedItemIDs;
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

            foreach (StoreItemData item in m_allStoreItems)
            {
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

            DebugBase.Log($"[{nameof(StoreManager)}] Loaded {m_ownedItemIDs.Count} owned items");
            OnOwnedItemsLoaded?.Invoke();
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

            m_saveManager.SaveGame();

            DebugBase.Log($"[{nameof(StoreManager)}] Saved {m_ownedItemIDs.Count} owned items");
        }

        private void SpawnOwnedObjects()
        {
            foreach (string itemID in m_ownedItemIDs)
            {
                if (m_itemLookup.TryGetValue(itemID, out StoreItemData itemData))
                {
                    SpawnObject(itemData);
                }
            }

            DebugBase.Log($"[{nameof(StoreManager)}] Spawned {m_spawnedObjects.Count} owned objects");
        }

        private void SpawnObject(StoreItemData itemData)
        {
            if (itemData.Prefab == null)
            {
                DebugBase.LogWarning($"[{nameof(StoreManager)}] Item has no prefab: {itemData.ItemName}");
                return;
            }

            if (m_spawnedObjects.ContainsKey(itemData.ItemID))
            {
                DebugBase.LogWarning($"[{nameof(StoreManager)}] Object already spawned: {itemData.ItemName}");
                return;
            }

            Transform parent = m_sceneObjectsParent != null ? m_sceneObjectsParent : null;
            GameObject spawnedObj = Instantiate(itemData.Prefab, itemData.SpawnPosition, itemData.SpawnRotation, parent);
            spawnedObj.name = $"{itemData.ItemName}_{itemData.ItemID}";

            m_spawnedObjects[itemData.ItemID] = spawnedObj;

            DebugBase.Log($"[{nameof(StoreManager)}] Spawned object: {itemData.ItemName} at {itemData.SpawnPosition}");
        }

        /// <summary>
        /// Clears all owned items and destroys spawned objects. Used for debugging/reset.
        /// </summary>
        public void ClearOwnedItems()
        {
            foreach (GameObject obj in m_spawnedObjects.Values)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            m_spawnedObjects.Clear();
            m_ownedItemIDs.Clear();
            SaveToSaveData();

            DebugBase.Log($"[{nameof(StoreManager)}] Cleared all owned items");
        }
    }
}

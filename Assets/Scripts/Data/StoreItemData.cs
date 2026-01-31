using Birdie.Birds;
using UnityEngine;

namespace Birdie.Data
{
    /// <summary>
    /// ScriptableObject that defines a purchasable item in the store.
    /// Wraps a BirdObject prefab with store-specific metadata (price, icon, spawn position).
    /// </summary>
    [CreateAssetMenu(fileName = "New Store Item", menuName = "Birdie/Store Item")]
    public class StoreItemData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField]
        [Tooltip("Unique identifier for the item (used in save system)")]
        private string m_itemID;

        [SerializeField]
        [Tooltip("Display name of the item")]
        private string m_itemName;

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Description shown in the store")]
        private string m_description;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Icon displayed in the store")]
        private Sprite m_icon;

        [Header("Economy")]
        [SerializeField]
        [Tooltip("Price in golden seeds")]
        [Min(0)]
        private int m_price;

        [Header("Category")]
        [SerializeField]
        [Tooltip("Category determines which tab the item appears in")]
        private BirdObjectType m_category;

        [Header("Scene Placement")]
        [SerializeField]
        [Tooltip("BirdObject prefab to instantiate when purchased")]
        private GameObject m_prefab;

        [SerializeField]
        [Tooltip("Fixed position where the item spawns in the scene")]
        private Vector3 m_spawnPosition;

        [SerializeField]
        [Tooltip("Fixed rotation for the spawned item (euler angles)")]
        private Vector3 m_spawnRotation;

        public string ItemID => m_itemID;
        public string ItemName => m_itemName;
        public string Description => m_description;
        public Sprite Icon => m_icon;
        public int Price => m_price;
        public BirdObjectType Category => m_category;
        public GameObject Prefab => m_prefab;
        public Vector3 SpawnPosition => m_spawnPosition;
        public Quaternion SpawnRotation => Quaternion.Euler(m_spawnRotation);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(m_itemID))
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemData)}] Item ID is empty on {name}");
            }

            if (m_prefab != null && m_prefab.GetComponent<BirdObject>() == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemData)}] Prefab on {name} does not have a BirdObject component");
            }
        }
#endif
    }
}

using Birdie.Birds;
using UnityEngine;

namespace Birdie.Data
{
    /// <summary>
    /// ScriptableObject that defines a purchasable item in the store.
    /// Wraps store-specific metadata (price, icon, category).
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

        [Header("Capabilities")]
        [SerializeField]
        [Tooltip("Whether the player can reposition this item in the scene after purchase")]
        private bool m_isMovable = true;

        [SerializeField]
        [Tooltip("Items sharing the same non-empty group name are mutually exclusive: enabling one disables the others. Leave empty for no restriction.")]
        private string m_exclusivityGroup;

        [SerializeField]
        [Tooltip("If true, this item is automatically owned and enabled from the first session, without requiring a purchase.")]
        private bool m_ownedByDefault;

        public string ItemID => m_itemID;
        public string ItemName => m_itemName;
        public string Description => m_description;
        public Sprite Icon => m_icon;
        public int Price => m_price;
        public BirdObjectType Category => m_category;
        public bool IsMovable => m_isMovable;
        public string ExclusivityGroup => m_exclusivityGroup;
        public bool OwnedByDefault => m_ownedByDefault;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(m_itemID))
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemData)}] Item ID is empty on {name}");
            }
        }
#endif
    }
}

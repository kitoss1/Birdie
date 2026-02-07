using System;
using UnityEngine;

namespace Birdie.Data
{
    [Serializable]
    public struct StoreItemSceneReference
    {
        [SerializeField]
        [Tooltip("Store item data asset")]
        private StoreItemData m_itemData;

        [SerializeField]
        [Tooltip("Scene object to enable/disable when purchased or toggled")]
        private GameObject m_sceneObject;

        public StoreItemData ItemData => m_itemData;
        public GameObject SceneObject => m_sceneObject;
    }
}

using System;
using UnityEngine;

namespace Birdie.Data
{
    [Serializable]
    public sealed class GiftItem
    {
        [SerializeField]
        private string m_itemName;

        [SerializeField]
        private Sprite m_itemIcon;

        [SerializeField]
        private string m_itemDescription;

        public string ItemName
        {
            get => m_itemName;
            set => m_itemName = value;
        }

        public Sprite ItemIcon
        {
            get => m_itemIcon;
            set => m_itemIcon = value;
        }

        public string ItemDescription
        {
            get => m_itemDescription;
            set => m_itemDescription = value;
        }
    }
}

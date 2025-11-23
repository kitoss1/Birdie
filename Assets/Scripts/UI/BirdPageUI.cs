using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// Component that holds references to all UI elements in a bird diary page.
    /// Attach this to the bird page prefab and assign references in the inspector.
    /// </summary>
    public class BirdPageUI : MonoBehaviour
    {
        [Header("Bird Photo")]
        [SerializeField]
        [Tooltip("The image component that displays the bird's photo")]
        private Image m_birdPhoto;

        [Header("Left Page Texts")]
        [SerializeField]
        [Tooltip("Text displaying the bird's rarity level")]
        private TextMeshProUGUI m_rarityText;

        [SerializeField]
        [Tooltip("Text displaying the bird's scientific name")]
        private TextMeshProUGUI m_scientificNameText;

        [SerializeField]
        [Tooltip("Text displaying the bird's diet type")]
        private TextMeshProUGUI m_foodText;

        [Header("Right Page Texts")]
        [SerializeField]
        [Tooltip("Text displaying the bird's common name")]
        private TextMeshProUGUI m_nameText;

        [SerializeField] [Tooltip("Text displaying the bird's description")]
        private TextMeshProUGUI m_descriptionText;
            
        [SerializeField]
        [Tooltip("Text displaying the numbers of times a player interacted with a bird")]
        private TextMeshProUGUI m_interactionCounterText;

        public Image BirdPhoto => m_birdPhoto;
        public TextMeshProUGUI RarityText => m_rarityText;
        public TextMeshProUGUI ScientificNameText => m_scientificNameText;
        public TextMeshProUGUI FoodText => m_foodText;
        public TextMeshProUGUI NameText => m_nameText;
        public TextMeshProUGUI DescriptionText => m_descriptionText;
        public TextMeshProUGUI InteractionCounterText => m_interactionCounterText;

#if UNITY_EDITOR
        /// <summary>
        /// Validates that all required UI references are assigned.
        /// </summary>
        private void OnValidate()
        {
            if (m_birdPhoto == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Bird Photo reference is missing!", this);
            }

            if (m_rarityText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Rarity Text reference is missing!", this);
            }

            if (m_scientificNameText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Scientific Name Text reference is missing!", this);
            }

            if (m_foodText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Food Text reference is missing!", this);
            }

            if (m_nameText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Name Text reference is missing!", this);
            }

            if (m_descriptionText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Description Text reference is missing!", this);
            }
            
            if (m_interactionCounterText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Interaction Counter Text reference is missing!", this);
            }
        }
#endif
    }
}

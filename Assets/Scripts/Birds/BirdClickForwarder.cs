using Birdie.Debug;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Birdie.Birds
{
    /// <summary>
    /// Forwards click events from child sprite to parent Bird component.
    /// Place this on the child GameObject that has the sprite.
    /// Works with UI elements using the Event System.
    /// </summary>
    public class BirdClickForwarder : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        [Tooltip("Reference to the Bird component")]
        private Bird m_bird;

        [SerializeField]
        [Tooltip("Alpha threshold for hit testing (0-1). Only pixels above this alpha value will be clickable.")]
        [Range(0f, 1f)]
        private float m_alphaHitTestThreshold = 0.5f;

        private Image m_image;

        private void Awake()
        {
            m_image = GetComponent<Image>();
            if (m_image != null)
            {
                m_image.alphaHitTestMinimumThreshold = m_alphaHitTestThreshold;
            }
            else
            {
                DebugBase.LogWarning($"[{nameof(BirdClickForwarder)}] No Image component found! Alpha hit testing will not work.", DebugCategory.Birds);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            DebugBase.LogWarning($"[{nameof(BirdClickForwarder)}] Bird clicked", DebugCategory.Birds);
            if (m_bird == null)
            {
                DebugBase.LogWarning($"[{nameof(BirdClickForwarder)}] Bird reference is not assigned!", DebugCategory.Birds);
                return;
            }

            DebugBase.Log($"[{nameof(BirdClickForwarder)}] Click detected on {gameObject.name}, forwarding to Bird", DebugCategory.Birds);
            m_bird.OnBirdClicked();
        }
    }
}

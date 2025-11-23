using Birdie.Debug;
using UnityEngine;
using UnityEngine.EventSystems;

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

        public void OnPointerClick(PointerEventData eventData)
        {
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

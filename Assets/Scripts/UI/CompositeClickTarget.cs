using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// Add to a root object composed of multiple child Image sprites.
    /// Enables alpha hit testing on all child Images so transparent pixels
    /// pass through, while clicks on visible pixels bubble up to the root's
    /// IPointerClickHandler (e.g. BirdObject).
    /// Requires all child sprite textures to have Read/Write Enabled.
    /// </summary>
    public sealed class CompositeClickTarget : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Pixels with alpha below this value will not block mouse input.")]
        private float m_alphaThreshold = 0.1f;

        private void Awake()
        {
            foreach (Image image in GetComponentsInChildren<Image>(includeInactive: true))
            {
                image.raycastTarget = true;
                image.alphaHitTestMinimumThreshold = m_alphaThreshold;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// Add to any UI Image to make transparent pixels non-interactive.
    /// Clicks on pixels with alpha below the threshold pass through to objects underneath.
    /// Requires the sprite/texture to have Read/Write Enabled in its import settings.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class AlphaHitTestImage : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Pixels with alpha below this value will not block mouse input. 0.1 is a good default.")]
        private float m_alphaThreshold = 0.1f;

        private void Awake()
        {
            GetComponent<Image>().alphaHitTestMinimumThreshold = m_alphaThreshold;
        }
    }
}

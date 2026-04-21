using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Drop zone component for the Seed Sorting minigame.
    /// Attached to the bowl and trash UI elements to identify valid drop targets.
    /// </summary>
    public sealed class SeedSortingDropZone : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Which drop target this zone represents")]
        private SeedSortingDropTarget m_dropTarget;

        [SerializeField]
        [Tooltip("Image to tint when a seed is hovering over this zone")]
        private Image m_highlightImage;

        [SerializeField]
        [Tooltip("Default color when no seed is hovering")]
        private Color m_normalColor = Color.white;

        [SerializeField]
        [Tooltip("Color when a seed is hovering over the zone")]
        private Color m_highlightColor = new Color(1f, 1f, 1f, 0.8f);

        [Header("Hit Area")]
        [SerializeField]
        [Tooltip("Override RectTransform used as the drop hit area. Defaults to this GameObject's RectTransform if not set.")]
        private RectTransform m_hitArea;

        private RectTransform m_rectTransform;

        public SeedSortingDropTarget DropTarget => m_dropTarget;

        public RectTransform RectTransform
        {
            get
            {
                if (m_rectTransform == null)
                {
                    m_rectTransform = GetComponent<RectTransform>();
                }

                return m_rectTransform;
            }
        }

        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera cam)
        {
            RectTransform hitRect = m_hitArea != null ? m_hitArea : m_rectTransform;
            return RectTransformUtility.RectangleContainsScreenPoint(hitRect, screenPoint, cam);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (m_highlightImage != null)
            {
                m_highlightImage.color = highlighted ? m_highlightColor : m_normalColor;
            }
        }
    }
}

using UnityEngine;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Individual falling seed component for the Seed Catcher minigame.
    /// Moves downward each frame at a configurable speed.
    /// </summary>
    public sealed class SeedCatcherSeed : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Fall speed in pixels per second")]
        private float m_fallSpeed = 300f;

        private RectTransform m_rectTransform;

        /// <summary>
        /// The seed's RectTransform, used for position-based collision detection.
        /// </summary>
        public RectTransform RectTransform => m_rectTransform;

        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            m_rectTransform.anchoredPosition -= new Vector2(0f, m_fallSpeed * Time.deltaTime);
        }
    }
}

using UnityEngine;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Individual falling spike component for the Seed Catcher minigame.
    /// Moves downward each frame at a configurable speed. Catching a spike costs the player a life.
    /// </summary>
    public sealed class SeedCatcherSpike : MonoBehaviour
    {
        private float m_fallSpeed;

        private RectTransform m_rectTransform;

        /// <summary>
        /// The spike's RectTransform, used for position-based collision detection.
        /// </summary>
        public RectTransform RectTransform => m_rectTransform;

        /// <summary>
        /// Sets the fall speed for this spike.
        /// </summary>
        /// <param name="speed">Fall speed in pixels per second.</param>
        public void SetFallSpeed(float speed)
        {
            m_fallSpeed = speed;
        }

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

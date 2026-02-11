using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Basket controller for the Seed Catcher minigame.
    /// Handles keyboard arrow movement and exposes position control for touch/drag input.
    /// </summary>
    public sealed class SeedCatcherBasket : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        [Tooltip("Keyboard movement speed in pixels per second")]
        private float m_keyboardSpeed = 400f;

        [SerializeField]
        [Tooltip("Horizontal padding from screen edges in pixels")]
        private float m_horizontalPadding = 50f;

        private RectTransform m_rectTransform;
        private RectTransform m_parentRect;
        private float m_minX;
        private float m_maxX;
        private bool m_isInputEnabled;

        /// <summary>
        /// Fired when a seed enters the basket's trigger collider.
        /// </summary>
        public event Action<SeedCatcherSeed> SeedCaught;

        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();
            m_parentRect = m_rectTransform.parent as RectTransform;
        }

        private void Start()
        {
            CalculateBounds();
        }

        private void Update()
        {
            if (!m_isInputEnabled)
            {
                return;
            }

            float direction = 0f;
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed)
                {
                    direction = -1f;
                }
                else if (keyboard.rightArrowKey.isPressed)
                {
                    direction = 1f;
                }
            }

            if (direction != 0f)
            {
                Vector2 position = m_rectTransform.anchoredPosition;
                position.x += direction * m_keyboardSpeed * Time.deltaTime;
                position.x = Mathf.Clamp(position.x, m_minX, m_maxX);
                m_rectTransform.anchoredPosition = position;
            }
        }

        /// <summary>
        /// Sets the basket's horizontal position directly, used by touch/drag input.
        /// </summary>
        /// <param name="x">Target X position in local space.</param>
        public void SetPositionX(float x)
        {
            Vector2 position = m_rectTransform.anchoredPosition;
            position.x = Mathf.Clamp(x, m_minX, m_maxX);
            m_rectTransform.anchoredPosition = position;
        }

        /// <summary>
        /// Enables or disables basket input.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            m_isInputEnabled = enabled;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            UnityEngine.Debug.LogWarning("Collider enter basket");
            if (other.TryGetComponent<SeedCatcherSeed>(out SeedCatcherSeed seed))
            {
                SeedCaught?.Invoke(seed);
            }
        }

        private void CalculateBounds()
        {
            if (m_parentRect == null)
            {
                return;
            }

            float halfParentWidth = m_parentRect.rect.width / 2f;
            float halfBasketWidth = m_rectTransform.rect.width / 2f;
            m_minX = -halfParentWidth + halfBasketWidth + m_horizontalPadding;
            m_maxX = halfParentWidth - halfBasketWidth - m_horizontalPadding;
        }
    }
}

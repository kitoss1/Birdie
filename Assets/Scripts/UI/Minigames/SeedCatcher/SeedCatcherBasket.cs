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
        private float m_pointerTargetX;
        private bool m_hasPointerTarget;

        /// <summary>
        /// Fired when a seed enters the basket's trigger collider.
        /// </summary>
        public event Action<SeedCatcherSeed> SeedCaught;

        /// <summary>
        /// Fired when a spike enters the basket's trigger collider.
        /// </summary>
        public event Action<SeedCatcherSpike> SpikeCaught;

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

            if (direction == 0f && m_hasPointerTarget)
            {
                float currentX = m_rectTransform.anchoredPosition.x;
                float diff = m_pointerTargetX - currentX;

                if (Mathf.Abs(diff) > 0.5f)
                {
                    direction = Mathf.Sign(diff);
                }
            }

            if (direction != 0f)
            {
                Vector2 position = m_rectTransform.anchoredPosition;
                float step = direction * m_keyboardSpeed * Time.deltaTime;

                if (m_hasPointerTarget)
                {
                    float diff = m_pointerTargetX - position.x;
                    if (Mathf.Abs(step) > Mathf.Abs(diff))
                    {
                        step = diff;
                    }
                }

                position.x += step;
                position.x = Mathf.Clamp(position.x, m_minX, m_maxX);
                m_rectTransform.anchoredPosition = position;
            }
        }

        /// <summary>
        /// Sets a target X position for the basket to move toward at keyboard speed.
        /// </summary>
        /// <param name="x">Target X position in local space.</param>
        public void SetTargetX(float x)
        {
            m_pointerTargetX = Mathf.Clamp(x, m_minX, m_maxX);
            m_hasPointerTarget = true;
        }

        /// <summary>
        /// Clears the pointer target so the basket stops moving toward it.
        /// </summary>
        public void ClearTarget()
        {
            m_hasPointerTarget = false;
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
            if (other.TryGetComponent<SeedCatcherSeed>(out SeedCatcherSeed seed))
            {
                SeedCaught?.Invoke(seed);
            }
            else if (other.TryGetComponent<SeedCatcherSpike>(out SeedCatcherSpike spike))
            {
                SpikeCaught?.Invoke(spike);
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

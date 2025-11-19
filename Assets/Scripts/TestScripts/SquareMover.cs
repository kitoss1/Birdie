using UnityEngine;
using UnityEngine.InputSystem;

namespace Birdie.Testing
{
    /// <summary>
    /// Test script that moves a square across the screen and changes color on click.
    /// Used for testing window transparency.
    /// </summary>
    public class SquareMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField]
        [Tooltip("Speed of the square movement")]
        private float m_moveSpeed = 200f;

        [SerializeField]
        [Tooltip("Screen padding to stay within bounds")]
        private float m_padding = 50f;

        [Header("Color Settings")]
        [SerializeField]
        [Tooltip("Available colors for the square")]
        private Color[] m_colors = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            Color.white
        };

        private Vector2 m_direction;
        private RectTransform m_rectTransform;
        private UnityEngine.UI.Image m_image;
        private int m_currentColorIndex = 0;

        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();
            m_image = GetComponent<UnityEngine.UI.Image>();

            if (m_rectTransform == null)
            {
                Debug.LogError($"[{nameof(SquareMover)}] RectTransform component not found!");
            }

            if (m_image == null)
            {
                Debug.LogError($"[{nameof(SquareMover)}] Image component not found!");
            }

            m_direction = new Vector2(1, 1).normalized;

            if (m_colors.Length > 0)
            {
                m_image.color = m_colors[0];
            }

            Debug.Log($"[{nameof(SquareMover)}] Initialized");
        }

        private void Update()
        {
            MoveSquare();
            CheckForClick();
        }

        private void MoveSquare()
        {
            if (m_rectTransform == null)
            {
                return;
            }

            Vector2 currentPos = m_rectTransform.anchoredPosition;
            Vector2 newPos = currentPos + m_direction * m_moveSpeed * Time.deltaTime;

            float halfWidth = m_rectTransform.rect.width / 2f;
            float halfHeight = m_rectTransform.rect.height / 2f;

            RectTransform canvasRect = m_rectTransform.parent as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            float maxX = (canvasRect.rect.width / 2f) - halfWidth - m_padding;
            float maxY = (canvasRect.rect.height / 2f) - halfHeight - m_padding;

            if (newPos.x > maxX || newPos.x < -maxX)
            {
                m_direction.x *= -1;
                newPos.x = Mathf.Clamp(newPos.x, -maxX, maxX);
            }

            if (newPos.y > maxY || newPos.y < -maxY)
            {
                m_direction.y *= -1;
                newPos.y = Mathf.Clamp(newPos.y, -maxY, maxY);
            }

            m_rectTransform.anchoredPosition = newPos;
        }

        private void CheckForClick()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector2 localMousePosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_rectTransform,
                    mousePosition,
                    null,
                    out localMousePosition
                );

                if (m_rectTransform.rect.Contains(localMousePosition))
                {
                    ChangeColor();
                }
            }
        }

        private void ChangeColor()
        {
            if (m_colors.Length == 0)
            {
                return;
            }

            m_currentColorIndex = (m_currentColorIndex + 1) % m_colors.Length;
            m_image.color = m_colors[m_currentColorIndex];

            Debug.Log($"[{nameof(SquareMover)}] Color changed to {m_colors[m_currentColorIndex]}");
        }
    }
}

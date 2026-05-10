using UnityEngine;

namespace Birdie.UI.Toast
{
    [CreateAssetMenu(fileName = "ToastSettings", menuName = "Birdie/Toast Settings")]
    public class ToastSettings : ScriptableObject
    {
        [SerializeField] private Color m_textColor = Color.black;

        [SerializeField]
        [Range(0.05f, 1f)]
        private float m_fadeDuration = 0.15f;

        [SerializeField]
        [Range(0.1f, 5f)]
        private float m_displayDuration = 1f;

        [SerializeField]
        [Range(0f, 200f)]
        private float m_floatDistance = 60f;

        [SerializeField]
        [Range(0.05f, 2f)]
        private float m_floatDuration = 0.4f;

        [Tooltip("Offset applied to the anchor position in canvas units (e.g. positive Y moves the toast above the anchor)")]
        [SerializeField] private Vector2 m_positionOffset = Vector2.zero;

        public Color TextColor => m_textColor;
        public float FadeDuration => m_fadeDuration;
        public float DisplayDuration => m_displayDuration;
        public float FloatDistance => m_floatDistance;
        public float FloatDuration => m_floatDuration;
        public Vector2 PositionOffset => m_positionOffset;
    }
}

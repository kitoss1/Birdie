using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Individual tile for the Sliding Puzzle minigame.
    /// Displays a portion of the source image and handles click input and slide animation.
    /// </summary>
    public sealed class SlidingPuzzleTile : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("RawImage displaying the tile's portion of the puzzle image")]
        private RawImage m_rawImage;

        [SerializeField]
        [Tooltip("Button for click interaction")]
        private Button m_button;

        private int m_tileId;
        private int m_gridIndex;
        private RectTransform m_rectTransform;

        /// <summary>
        /// Fired when the player clicks this tile. Passes the tile's current grid index.
        /// </summary>
        public event Action<int> Pressed;

        /// <summary>
        /// The tile's permanent identity (which image portion it represents).
        /// </summary>
        public int TileId => m_tileId;

        /// <summary>
        /// The tile's current position in the grid.
        /// </summary>
        public int GridIndex => m_gridIndex;

        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();

            if (m_button != null)
            {
                m_button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (m_button != null)
            {
                m_button.onClick.RemoveListener(OnButtonClicked);
            }

            if (m_rectTransform != null)
            {
                m_rectTransform.DOKill();
            }
        }

        /// <summary>
        /// Configures the tile's visuals, identity, and initial grid position.
        /// </summary>
        public void Initialize(Texture texture, Rect uvRect, int tileId, int gridIndex)
        {
            if (m_rawImage != null)
            {
                m_rawImage.texture = texture;
                m_rawImage.uvRect = uvRect;
            }

            m_tileId = tileId;
            m_gridIndex = gridIndex;
        }

        /// <summary>
        /// Updates the tile's current position in the grid.
        /// </summary>
        public void SetGridIndex(int index)
        {
            m_gridIndex = index;
        }

        /// <summary>
        /// Toggles whether the tile can be clicked.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (m_button != null)
            {
                m_button.interactable = interactable;
            }
        }

        /// <summary>
        /// Slides the tile to the target anchored position using DOTween.
        /// </summary>
        public async UniTask SlideToAsync(Vector2 targetPosition, float duration)
        {
            if (m_rectTransform == null)
            {
                return;
            }

            m_rectTransform.DOKill();

            await m_rectTransform
                .DOAnchorPos(targetPosition, duration)
                .SetEase(Ease.OutQuad)
                .AsyncWaitForCompletion();
        }

        /// <summary>
        /// Sets the tile's anchored position immediately without animation.
        /// </summary>
        public void SetPosition(Vector2 position)
        {
            if (m_rectTransform != null)
            {
                m_rectTransform.anchoredPosition = position;
            }
        }

        /// <summary>
        /// Sets the tile's size.
        /// </summary>
        public void SetSize(Vector2 size)
        {
            if (m_rectTransform != null)
            {
                m_rectTransform.sizeDelta = size;
            }
        }

        private void OnButtonClicked()
        {
            Pressed?.Invoke(m_gridIndex);
        }
    }
}

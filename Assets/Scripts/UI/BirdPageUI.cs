using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// Component that holds references to all UI elements in a bird diary page.
    /// Attach this to the bird page prefab and assign references in the inspector.
    /// </summary>
    public class BirdPageUI : MonoBehaviour
    {
        [Header("Page Structure")]
        [SerializeField]
        [Tooltip("Parent GameObject for back side elements")]
        public GameObject m_backParent;

        [SerializeField]
        [Tooltip("Parent GameObject for front side elements")]
        public GameObject m_frontParent;

        [SerializeField]
        [Tooltip("CanvasGroup for front content visibility control")]
        private CanvasGroup m_frontCanvasGroup;

        [SerializeField]
        [Tooltip("CanvasGroup for back content visibility control")]
        private CanvasGroup m_backCanvasGroup;

        [Header("Back Elements")]
        [SerializeField]
        [Tooltip("The image component that displays the bird's photo")]
        private Image m_birdPhoto;

        [SerializeField]
        [Tooltip("Text displaying the bird's rarity level")]
        private TextMeshProUGUI m_rarityText;

        [SerializeField]
        [Tooltip("Text displaying the bird's scientific name")]
        private TextMeshProUGUI m_scientificNameText;

        [SerializeField]
        [Tooltip("Text displaying the bird's diet type")]
        private TextMeshProUGUI m_foodText;

        [SerializeField]
        [Tooltip("Text displaying the numbers of times a player interacted with a bird")]
        private TextMeshProUGUI m_interactionCounterText;

        [Header("Front Elements")]
        [SerializeField]
        [Tooltip("Text displaying the bird's common name")]
        private TextMeshProUGUI m_nameText;

        [SerializeField]
        [Tooltip("Text displaying the bird's description")]
        private TextMeshProUGUI m_descriptionText;
        

        public Image BirdPhoto => m_birdPhoto;
        public TextMeshProUGUI RarityText => m_rarityText;
        public TextMeshProUGUI ScientificNameText => m_scientificNameText;
        public TextMeshProUGUI FoodText => m_foodText;
        public TextMeshProUGUI NameText => m_nameText;
        public TextMeshProUGUI DescriptionText => m_descriptionText;
        public TextMeshProUGUI InteractionCounterText => m_interactionCounterText;

        private int m_originalSiblingIndex;

        /// <summary>
        /// Updates content visibility based on current rotation angle.
        /// Call this during rotation animations to hide content on the back of pages.
        /// </summary>
        /// <param name="currentRotationY">Current Y rotation angle (0-360)</param>
        public void UpdateContentVisibility(float currentRotationY)
        {
            // Normalize angle to 0-360
            float angle = (currentRotationY + 360f) % 360f;

            // Determine if we're viewing the front or back of the page
            // Front is visible when angle is between 0-90 or 270-360
            bool isShowingFront = angle < 90f || angle > 270f;

            // Update canvas group alphas for smooth visibility transitions
            if (m_frontCanvasGroup != null)
            {
                m_frontCanvasGroup.alpha = isShowingFront ? 1f : 0f;
            }

            if (m_backCanvasGroup != null)
            {
                m_backCanvasGroup.alpha = isShowingFront ? 0f : 1f;
            }
        }

        /// <summary>
        /// Animates a page turn to show the next side.
        /// </summary>
        /// <param name="turnDuration">Duration of the turn animation in seconds</param>
        /// <param name="showingBack">True to turn to show the back, false to turn to show the front</param>
        public async UniTask TurnPageAsync(float turnDuration, bool showingBack)
        {
            float targetRotation = showingBack ? 180f : 0f;

            // Create the rotation tween
            await transform.DOLocalRotate(new Vector3(0f, targetRotation, 0f), turnDuration)
                .SetEase(Ease.InOutCubic)
                .OnUpdate(() =>
                {
                    // Update visibility during rotation
                    UpdateContentVisibility(transform.localEulerAngles.y);
                })
                .AsyncWaitForCompletion();
        }

        /// <summary>
        /// Immediately sets the page to show a specific side without animation.
        /// </summary>
        /// <param name="showingBack">True to show the back, false to show the front</param>
        public void SetPageSide(bool showingBack)
        {
            float rotation = showingBack ? 180f : 0f;
            transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
            UpdateContentVisibility(rotation);
        }

        /// <summary>
        /// Brings the page to the front of the hierarchy to render on top during page turn animation.
        /// Call this before starting the flip animation.
        /// </summary>
        public void BringToFront()
        {
            // Store current sibling index before changing it
            m_originalSiblingIndex = transform.GetSiblingIndex();

            // Move to last sibling position (renders on top in UI)
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// Resets the page to its original hierarchy position after the flip animation.
        /// Call this after the flip animation completes.
        /// </summary>
        public void ResetPosition()
        {
            // Restore original sibling index
            transform.SetSiblingIndex(m_originalSiblingIndex);
        }

        /// <summary>
        /// Completes any running DOTween animation on this page instantly,
        /// snapping it to its target rotation. Used when interrupting an animation
        /// to start a new one.
        /// </summary>
        public void CompleteCurrentAnimation()
        {
            transform.DOComplete();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validates that all required UI references are assigned.
        /// </summary>
        private void OnValidate()
        {
            // Validate parent objects
            if (m_frontParent == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Front Parent reference is missing!", this);
            }

            if (m_backParent == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Back Parent reference is missing!", this);
            }

            // Validate canvas groups
            if (m_frontCanvasGroup == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Front CanvasGroup reference is missing!", this);
            }

            if (m_backCanvasGroup == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Back CanvasGroup reference is missing!", this);
            }

            // Validate back elements
            if (m_birdPhoto == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Bird Photo reference is missing!", this);
            }

            if (m_rarityText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Rarity Text reference is missing!", this);
            }

            if (m_scientificNameText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Scientific Name Text reference is missing!", this);
            }

            if (m_foodText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Food Text reference is missing!", this);
            }

            // Validate front elements
            if (m_nameText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Name Text reference is missing!", this);
            }

            if (m_descriptionText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Description Text reference is missing!", this);
            }

            if (m_interactionCounterText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Interaction Counter Text reference is missing!", this);
            }
        }
#endif
    }
}

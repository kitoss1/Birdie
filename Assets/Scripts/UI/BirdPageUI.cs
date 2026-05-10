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
        [Tooltip("Parent GameObject for back side elements")]
        [SerializeField] private GameObject m_backParent;

        [Tooltip("Parent GameObject for front side elements")]
        [SerializeField] private GameObject m_frontParent;

        [Tooltip("CanvasGroup for front content visibility control")]
        [SerializeField] private CanvasGroup m_frontCanvasGroup;

        [Tooltip("CanvasGroup for back content visibility control")]
        [SerializeField] private CanvasGroup m_backCanvasGroup;

        [Header("Front Elements")]
        [Tooltip("Mark as true for intro page which has different front elements")]
        [SerializeField] private bool m_isIntroPage;

        [Tooltip("Text displaying the bird's description")]
        [SerializeField] private TextMeshProUGUI m_descriptionText;

        [Tooltip("Image displaying the bird's habitat map")]
        [SerializeField] private Image m_mapImage;

        [Tooltip("Image displaying the feather decoration")]
        [SerializeField] private Image m_featherImage;

        [Header("Back Elements")]
        [Tooltip("The image component that displays the bird's photo")]
        [SerializeField] private Image m_birdPhoto;

        [Tooltip("Text displaying the bird's common name")]
        [SerializeField] private TextMeshProUGUI m_nameText;

        [Tooltip("Text displaying the bird's scientific name")]
        [SerializeField] private TextMeshProUGUI m_scientificNameText;

        [Tooltip("Text displaying the numbers of times a player interacted with a bird")]
        [SerializeField] private TextMeshProUGUI m_interactionCounterText;

        [Tooltip("Text displaying the current friendship level")]
        [SerializeField] private TextMeshProUGUI m_friendshipLevelText;

        [Tooltip("Friendship progress bar tracker")]
        [SerializeField] private ResourceBarTracker m_friendshipBar;

        [Tooltip("Text displaying the bird's visit hours")]
        [SerializeField] private TextMeshProUGUI m_visitHoursText;

        [Tooltip("Icon displaying the conservation danger level")]
        [SerializeField] private Image m_peligroIcon;

        [Tooltip("Text displaying the bird's diet type (shown when diet is locked)")]
        [SerializeField] private TextMeshProUGUI m_foodText;

        [Tooltip("Container for instantiated diet icons (shown when diet is unlocked)")]
        [SerializeField] private Transform m_dietIconContainer;

        [Tooltip("Prefab instantiated for each diet icon (must have an Image component)")]
        [SerializeField] private GameObject m_dietIconPrefab;

        public GameObject BackParent => m_backParent;
        public GameObject FrontParent => m_frontParent;
        public TextMeshProUGUI DescriptionText => m_descriptionText;
        public Image MapImage => m_mapImage;
        public Image FeatherImage => m_featherImage;
        public Image BirdPhoto => m_birdPhoto;
        public TextMeshProUGUI NameText => m_nameText;
        public TextMeshProUGUI ScientificNameText => m_scientificNameText;
        public TextMeshProUGUI InteractionCounterText => m_interactionCounterText;
        public TextMeshProUGUI FriendshipLevelText => m_friendshipLevelText;
        public ResourceBarTracker FriendshipBar => m_friendshipBar;
        public TextMeshProUGUI VisitHoursText => m_visitHoursText;
        public Image PeligroIcon => m_peligroIcon;
        public TextMeshProUGUI FoodText => m_foodText;
        public Transform DietIconContainer => m_dietIconContainer;
        public GameObject DietIconPrefab => m_dietIconPrefab;

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
                m_frontCanvasGroup.blocksRaycasts = isShowingFront;
            }

            if (m_backCanvasGroup != null)
            {
                m_backCanvasGroup.alpha = isShowingFront ? 0f : 1f;
                m_backCanvasGroup.blocksRaycasts = !isShowingFront;
            }
        }

        /// <summary>
        /// Animates a page turn to show the next side.
        /// </summary>
        /// <param name="turnDuration">Duration of the turn animation in seconds</param>
        /// <param name="showingBack">True to turn to show the back, false to turn to show the front</param>
        public async UniTask TurnPageAsync(float turnDuration, bool showingBack)
        {
            // Use a signed delta so forward (+180°) and backward (-180°) always rotate
            // in opposite directions, regardless of DOTween's path-selection for 180° targets.
            float rotationDelta = showingBack ? 180f : -180f;

            await transform.DOLocalRotate(new Vector3(0f, rotationDelta, 0f), turnDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.InOutCubic)
                .OnUpdate(() =>
                {
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
            if (m_frontParent == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Front Parent reference is missing!", this);
            }

            if (m_backParent == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Back Parent reference is missing!", this);
            }

            if (m_frontCanvasGroup == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Front CanvasGroup reference is missing!", this);
            }

            if (m_backCanvasGroup == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Back CanvasGroup reference is missing!", this);
            }

            if (!m_isIntroPage)
            {
                if (m_descriptionText == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Description Text reference is missing!", this);
                }

                if (m_birdPhoto == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Bird Photo reference is missing!", this);
                }

                if (m_nameText == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Name Text reference is missing!", this);
                }

                if (m_scientificNameText == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Scientific Name Text reference is missing!", this);
                }

                if (m_interactionCounterText == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Interaction Counter Text reference is missing!", this);
                }

                if (m_friendshipLevelText == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Friendship Level Text reference is missing!", this);
                }

                if (m_friendshipBar == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Friendship Bar reference is missing!", this);
                }

                if (m_visitHoursText == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Visit Hours Text reference is missing!", this);
                }

                if (m_peligroIcon == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Peligro Icon reference is missing!", this);
                }

                if (m_foodText == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Food Text reference is missing!", this);
                }

                if (m_dietIconContainer == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Diet Icon Container reference is missing!", this);
                }

                if (m_dietIconPrefab == null)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(BirdPageUI)}] Diet Icon Prefab reference is missing!", this);
                }
            }
        }
#endif
    }
}

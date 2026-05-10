using System;
using System.Collections.Generic;
using Birdie.Data;
using Birdie.Debug;
using Birdie.Save;
using Birdie.UI;
using Birdie.UI.Toast;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages the diary UI, handling page display and navigation.
    /// Gets all data from DiaryManager via GameManager.Instance.
    /// Initialized by GameManager after DiaryManager and FriendshipManager are ready.
    /// </summary>
    public class DiaryUIManager : BaseManager
    {
        [Header("General UI")]
        [Tooltip("Prefab for bird page layout (two-page spread)")]
        [SerializeField] private GameObject m_birdPagePrefab;

        [Tooltip("Container to hold all instantiated bird pages")]
        [SerializeField] private Transform m_pagesContainer;

        [Tooltip("Previous page button")]
        [SerializeField] private Button m_previousButton;

        [Tooltip("Next page button")]
        [SerializeField] private Button m_nextButton;

        [SerializeField] private GameObject m_firstPage;

        [Header("Parameters to customize")]
        [Tooltip("Duration of page turn animation in seconds")]
        [SerializeField] private float m_pageTurnDuration = 0.5f;

        [Tooltip("Duration of the friendship bar fill animation in seconds")]
        [SerializeField] private float m_friendshipBarAnimationDuration = 1f;

        [Header("Locked Bird Settings")]
        [Tooltip("Text to display for undiscovered bird descriptions")]
        [SerializeField] private string m_lockedDescriptionText = "Este pájaro todavía no ha sido descubierto.";

        [Tooltip("Text to display for undiscovered bird names")]
        [SerializeField] private string m_lockedNameText = "???";

        [Tooltip("Color to tint undiscovered bird photos")]
        [SerializeField] private Color m_lockedPhotoTint = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        [Tooltip("Button to close the diary popup")]
        [SerializeField] private Button m_closeButton;

        [Header("Toast Settings")]
        [Tooltip("Toast appearance settings for diet icon toasts. Leave empty to use prefab defaults.")]
        [SerializeField] private ToastSettings m_dietIconToastSettings;

        [Header("Peligro Popup")]
        [Tooltip("Popup shown when clicking the conservation danger icon")]
        [SerializeField] private PeligroPopupUI m_peligroPopup;

        [Header("Map Popup")]
        [Tooltip("Popup shown when clicking the habitat map")]
        [SerializeField] private MapPopupUI m_mapPopup;


        private readonly List<GameObject> m_instantiatedPages = new List<GameObject>();
        private readonly Dictionary<string, int> m_birdIDToPageIndex = new Dictionary<string, int>();
        private readonly Dictionary<string, BirdData> m_birdDataByID = new Dictionary<string, BirdData>();
        private readonly HashSet<string> m_animatedThisSession = new HashSet<string>();
        private int m_currentPageIndex = 0;

        private bool m_isAnimating = false;
        private int m_targetPageIndex = 0;
        private BirdPageUI m_currentlyAnimatingPageUI = null;
        
        /// <summary>
        /// Event fired when close button is clicked.
        /// </summary>
        public event Action OnCloseClicked;

        private void OnEnable()
        {
            m_animatedThisSession.Clear();
            TriggerFriendshipAnimationAsync(m_currentPageIndex);
        }

        public override void Initialize(SaveManager saveManager = null)
        {
            base.Initialize(saveManager);

            SetupNavigation();
            CreateBirdPages();
            SubscribeToEvents();

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Diary UI initialized");
        }

        /// <summary>
        /// Subscribes to DiaryManager events.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null && GameManager.Instance.DiaryManager != null)
            {
                GameManager.Instance.DiaryManager.OnBirdDiscovered += OnBirdDiscovered;
                GameManager.Instance.DiaryManager.OnBirdEncountered += OnBirdEncountered;
            }

            if (GameManager.Instance != null && GameManager.Instance.FriendshipManager != null)
            {
                GameManager.Instance.FriendshipManager.OnFriendshipChanged += OnFriendshipChanged;
            }
        }

        /// <summary>
        /// Called when a new bird is discovered.
        /// </summary>
        private void OnBirdDiscovered(BirdData birdData)
        {
            DebugBase.Log($"[{nameof(DiaryUIManager)}] New bird discovered: {birdData.BirdName}, refreshing page", DebugCategory.UI);
            RefreshSinglePage(birdData);
        }

        /// <summary>
        /// Called when a bird is encountered (interaction count updated).
        /// </summary>
        private void OnBirdEncountered(BirdData birdData)
        {
            DebugBase.Log($"[{nameof(DiaryUIManager)}] Bird encountered: {birdData.BirdName}, refreshing page", DebugCategory.UI);
            RefreshSinglePage(birdData);
        }

        private void OnFriendshipChanged(string birdID)
        {
            if (!m_birdDataByID.TryGetValue(birdID, out BirdData bird))
            {
                return;
            }

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Friendship changed for {bird.BirdName}, refreshing page", DebugCategory.UI);
            RefreshSinglePage(bird);
        }

        /// <summary>
        /// Sets up navigation button listeners.
        /// </summary>
        private void SetupNavigation()
        {
            if (m_previousButton != null)
            {
                m_previousButton.onClick.AddListener(ShowPreviousPage);
            }

            if (m_nextButton != null)
            {
                m_nextButton.onClick.AddListener(ShowNextPage);
            }
            
            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            UpdateNavigationButtons();
        }

        /// <summary>
        /// Creates pages for each bird in the database.
        /// Structure: Each page shows two parts of birds:
        /// - Front: Right page (name, description) of previous bird
        /// - Back: Left page (photo, stats) of next bird
        /// Special case: Introduction page (m_firstPage) shows first bird's left page on back.
        /// </summary>
        private void CreateBirdPages()
        {
            if (m_birdPagePrefab == null)
            {
                DebugBase.LogError($"[{nameof(DiaryUIManager)}] Bird page prefab is not assigned!", DebugCategory.UI);
                return;
            }

            if (m_pagesContainer == null)
            {
                DebugBase.LogError($"[{nameof(DiaryUIManager)}] Pages container is not assigned!", DebugCategory.UI);
                return;
            }

            if (m_firstPage == null)
            {
                DebugBase.LogError($"[{nameof(DiaryUIManager)}] First page reference is not assigned!", DebugCategory.UI);
                return;
            }

            // Clear existing pages
            foreach (GameObject page in m_instantiatedPages)
            {
                if (page != null)
                {
                    Destroy(page);
                }
            }

            m_instantiatedPages.Clear();
            m_birdIDToPageIndex.Clear();
            m_birdDataByID.Clear();

            // Get birds from DiaryManager (already sorted)
            List<BirdData> allBirds = GameManager.Instance.DiaryManager.GetAllBirdsForDiary();

            if (allBirds.Count == 0)
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] No birds found in diary!", DebugCategory.UI);
                return;
            }

            // Populate introduction page's BACK with first bird's back page
            BirdPageUI introPageUI = m_firstPage.GetComponent<BirdPageUI>();
            if (introPageUI != null)
            {
                PopulateBackPage(introPageUI.BackParent, introPageUI.BirdPhoto, introPageUI.NameText,
                    introPageUI.ScientificNameText, introPageUI.InteractionCounterText,
                    introPageUI.FriendshipLevelText, introPageUI.FriendshipBar,
                    introPageUI.VisitHoursText, introPageUI.FoodText,
                    introPageUI.DietIconContainer, introPageUI.DietIconPrefab, allBirds[0]);

                // Initialize introduction page to show front (0 degrees rotation)
                introPageUI.SetPageSide(showingBack: false);
            }
            else
            {
                DebugBase.LogError($"[{nameof(DiaryUIManager)}] BirdPageUI component not found on first page!", DebugCategory.UI);
            }

            // Create pages for each bird (page i shows bird[i-1] right and bird[i] left)
            for (int i = 0; i < allBirds.Count; i++)
            {
                GameObject pageInstance = Instantiate(m_birdPagePrefab, m_pagesContainer);
                BirdPageUI pageUI = pageInstance.GetComponent<BirdPageUI>();

                if (pageUI == null)
                {
                    DebugBase.LogError($"[{nameof(DiaryUIManager)}] BirdPageUI component not found on instantiated page!", DebugCategory.UI);
                    continue;
                }

                // Populate FRONT with bird[i]'s front page (description, map, peligro icon)
                PopulateFrontPage(pageUI.FrontParent, pageUI.DescriptionText, pageUI.MapImage, pageUI.FeatherImage, pageUI.PeligroIcon, allBirds[i]);

                // Populate BACK with bird[i+1]'s back page (photo, name, stats)
                if (i + 1 < allBirds.Count)
                {
                    PopulateBackPage(pageUI.BackParent, pageUI.BirdPhoto, pageUI.NameText,
                        pageUI.ScientificNameText, pageUI.InteractionCounterText,
                        pageUI.FriendshipLevelText, pageUI.FriendshipBar,
                        pageUI.VisitHoursText, pageUI.FoodText,
                        pageUI.DietIconContainer, pageUI.DietIconPrefab, allBirds[i + 1]);
                }
                else
                {
                    // Last page - disable back content
                    if (pageUI.BackParent != null)
                    {
                        pageUI.BackParent.SetActive(false);
                    }
                }

                // Initialize all bird pages to show front (0 degrees rotation)
                pageUI.SetPageSide(showingBack: false);

                m_instantiatedPages.Add(pageInstance);
                m_birdIDToPageIndex[allBirds[i].BirdID] = i;
                m_birdDataByID[allBirds[i].BirdID] = allBirds[i];
            }

            // Show introduction page initially (index -1), hide all instantiated pages
            ShowPage(-1);

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Created {m_instantiatedPages.Count} bird pages for {allBirds.Count} birds", DebugCategory.UI);
        }

        /// <summary>
        /// Populates the FRONT page (description, map) of a bird.
        /// Shown on the front of pages (right side of the spread).
        /// </summary>
        private void PopulateFrontPage(GameObject parentObject, TextMeshProUGUI descriptionText,
            Image mapImage, Image featherImage, Image peligroIcon, BirdData birdData)
        {
            if (parentObject != null)
            {
                parentObject.SetActive(true);
            }

            bool isDiscovered = GameManager.Instance.DiaryManager.IsBirdDiscovered(birdData);

            if (!isDiscovered)
            {
                if (descriptionText != null) descriptionText.text = m_lockedDescriptionText;
                if (mapImage != null) mapImage.color = m_lockedPhotoTint;
                if (featherImage != null) featherImage.enabled = false;
                if (peligroIcon != null) peligroIcon.gameObject.SetActive(false);
                return;
            }

            int currentLevel = GameManager.Instance.FriendshipManager.GetFriendshipLevel(birdData.BirdID, birdData);

            if (descriptionText != null)
            {
                descriptionText.text = currentLevel >= birdData.DescriptionUnlockLevel
                    ? birdData.BasicDescription
                    : "???";
            }

            if (mapImage != null)
            {
                bool mapUnlocked = currentLevel >= birdData.HabitatMapUnlockLevel;
                if (mapUnlocked && birdData.HabitatMap != null)
                {
                    mapImage.sprite = birdData.HabitatMap;
                    mapImage.color = Color.white;
                }
                else
                {
                    mapImage.color = m_lockedPhotoTint;
                }

                mapImage.raycastTarget = true;
                Button mapBtn = mapImage.GetComponent<Button>();
                if (mapBtn == null) mapBtn = mapImage.gameObject.AddComponent<Button>();
                mapBtn.onClick.RemoveAllListeners();
                if (mapUnlocked && birdData.HabitatMap != null && m_mapPopup != null)
                {
                    Sprite map = birdData.HabitatMap;
                    mapBtn.onClick.AddListener(() => m_mapPopup.Show(map));
                }
            }

            if (featherImage != null)
            {
                bool featherVisible = currentLevel >= birdData.FeatherUnlockLevel;
                featherImage.enabled = featherVisible;
                if (featherVisible && birdData.FeatherSprite != null)
                {
                    featherImage.sprite = birdData.FeatherSprite;
                }
            }

            if (peligroIcon != null)
            {
                bool peligroUnlocked = currentLevel >= birdData.PeligroUnlockLevel;
                if (birdData.PeligroSprite != null)
                    peligroIcon.sprite = birdData.PeligroSprite;
                peligroIcon.gameObject.SetActive(peligroUnlocked);

                peligroIcon.raycastTarget = true;
                Button btn = peligroIcon.GetComponent<Button>();
                if (btn == null) btn = peligroIcon.gameObject.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                if (peligroUnlocked && m_peligroPopup != null)
                {
                    Sprite sprite = birdData.PeligroSprite;
                    string description = birdData.PeligroDescription;
                    btn.onClick.AddListener(() => m_peligroPopup.Show(sprite, description));
                }
            }
        }

        /// <summary>
        /// Populates the BACK page (photo, name, stats) of a bird.
        /// Shown on the back of pages (left side of the spread).
        /// </summary>
        private void PopulateBackPage(GameObject parentObject, Image birdPhoto, TextMeshProUGUI nameText,
            TextMeshProUGUI scientificNameText, TextMeshProUGUI interactionCounterText,
            TextMeshProUGUI friendshipLevelText, ResourceBarTracker friendshipBar,
            TextMeshProUGUI visitHoursText, TextMeshProUGUI foodText,
            Transform dietIconContainer, GameObject dietIconPrefab, BirdData birdData)
        {
            if (parentObject != null)
            {
                parentObject.SetActive(true);
            }

            bool isDiscovered = GameManager.Instance.DiaryManager.IsBirdDiscovered(birdData);

            if (!isDiscovered)
            {
                if (birdPhoto != null) birdPhoto.color = m_lockedPhotoTint;
                if (nameText != null) nameText.text = m_lockedNameText;
                if (scientificNameText != null) scientificNameText.text = "???";
                if (interactionCounterText != null) interactionCounterText.text = "Visitas: ???";
                if (visitHoursText != null) visitHoursText.text = "???";
                SetDietLocked(foodText, dietIconContainer);
                PopulateFriendshipBar(friendshipBar, friendshipLevelText, birdData, isDiscovered: false);
                return;
            }

            int currentLevel = GameManager.Instance.FriendshipManager.GetFriendshipLevel(birdData.BirdID, birdData);

            if (birdPhoto != null && birdData.BirdPhoto != null)
            {
                birdPhoto.sprite = birdData.BirdPhoto;
                birdPhoto.color = currentLevel >= birdData.FullPhotoUnlockLevel ? Color.white : m_lockedPhotoTint;
            }

            if (nameText != null)
            {
                nameText.text = birdData.BirdName;
            }

            if (scientificNameText != null)
            {
                scientificNameText.text = currentLevel >= birdData.ScientificNameUnlockLevel
                    ? birdData.ScientificName
                    : "???";
            }

            if (interactionCounterText != null)
            {
                int encounterCount = GameManager.Instance.DiaryManager.GetEncounterCount(birdData);
                interactionCounterText.text = $"Visitas: {encounterCount}";
            }

            PopulateFriendshipBar(friendshipBar, friendshipLevelText, birdData, isDiscovered: true);

            if (visitHoursText != null)
            {
                if (currentLevel >= birdData.VisitHoursUnlockLevel)
                {
                    visitHoursText.text = birdData.AppearsAnytime
                        ? "Cualquier hora"
                        : $"{birdData.AppearanceTimeRange.StartHour}h - {birdData.AppearanceTimeRange.EndHour}h";
                }
                else
                {
                    visitHoursText.text = "???";
                }
            }

            if (currentLevel >= birdData.DietUnlockLevel)
            {
                PopulateDietIcons(foodText, dietIconContainer, dietIconPrefab, birdData);
            }
            else
            {
                SetDietLocked(foodText, dietIconContainer);
            }
        }

        private void PopulateDietIcons(TextMeshProUGUI foodText, Transform dietIconContainer,
            GameObject dietIconPrefab, BirdData birdData)
        {
            if (foodText != null)
            {
                foodText.gameObject.SetActive(true);
                foodText.text = $"{birdData.DietType}";
            }

            if (dietIconContainer == null)
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] Diet icon container is null for {birdData.BirdName}", DebugCategory.UI);
                return;
            }

            for (int i = dietIconContainer.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(dietIconContainer.GetChild(i).gameObject);
            }

            dietIconContainer.gameObject.SetActive(true);

            if (dietIconPrefab == null)
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] Diet icon prefab is null for {birdData.BirdName}", DebugCategory.UI);
                return;
            }

            if (birdData.DietIcons.Count == 0)
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] No diet icons assigned on {birdData.BirdName}", DebugCategory.UI);
                return;
            }

            foreach (DietIconEntry entry in birdData.DietIcons)
            {
                if (entry.icon == null) continue;

                GameObject iconObj = Instantiate(dietIconPrefab, dietIconContainer);

                Image iconImage = iconObj.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = entry.icon;
                    iconImage.enabled = true;
                }

                Button button = iconObj.GetComponent<Button>();
                if (button == null) button = iconObj.AddComponent<Button>();

                string foodName = entry.name;
                Transform iconTransform = iconObj.transform;
                ToastSettings dietSettings = m_dietIconToastSettings;
                button.onClick.AddListener(() => GameManager.Instance.ToastManager.ShowToast(foodName, iconTransform, dietSettings, "diary_diet"));
            }

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Spawned {birdData.DietIcons.Count} diet icons for {birdData.BirdName}", DebugCategory.UI);
        }

        private static void SetDietLocked(TextMeshProUGUI foodText, Transform dietIconContainer)
        {
            if (dietIconContainer != null) dietIconContainer.gameObject.SetActive(false);
            if (foodText != null)
            {
                foodText.gameObject.SetActive(true);
                foodText.text = "???";
            }
        }

        /// <summary>
        /// Populates the friendship bar and level text for a bird.
        /// Shows progress within the current level toward the next.
        /// </summary>
        private void PopulateFriendshipBar(ResourceBarTracker friendshipBar,
            TextMeshProUGUI friendshipLevelText, BirdData birdData, bool isDiscovered)
        {
            if (!isDiscovered)
            {
                if (friendshipBar != null)
                {
                    friendshipBar.SetValues(0, 0);
                }

                if (friendshipLevelText != null)
                {
                    friendshipLevelText.text = "Amistad: 0";
                }

                return;
            }

            FriendshipManager friendshipManager = GameManager.Instance.FriendshipManager;
            int currentPoints = friendshipManager.GetFriendship(birdData.BirdID);
            int currentLevel = friendshipManager.GetFriendshipLevel(birdData.BirdID, birdData);

            if (friendshipLevelText != null)
            {
                friendshipLevelText.text = $"Amistad: {currentLevel}";
            }

            if (friendshipBar != null)
            {
                // If a friendship animation is pending for this bird, display the bar at the
                // last-seen value so it doesn't flash the new value before the animation starts.
                int displayPoints = currentPoints;
                if (!m_animatedThisSession.Contains(birdData.BirdID))
                {
                    int lastSeen = GameManager.Instance.FriendshipManager.GetLastSeenFriendship(birdData.BirdID);
                    if (currentPoints > lastSeen)
                    {
                        displayPoints = lastSeen;
                    }
                }

                int displayLevel = friendshipManager.GetFriendshipLevelForPoints(birdData, displayPoints);
                int currentThreshold = birdData.GetFriendshipRequirement(displayLevel);
                int nextThreshold = birdData.GetFriendshipRequirement(displayLevel + 1);

                if (nextThreshold == int.MaxValue)
                {
                    int prevThreshold = displayLevel > 0 ? birdData.GetFriendshipRequirement(displayLevel - 1) : 0;
                    int levelRange = currentThreshold - prevThreshold;
                    friendshipBar.SetValues(levelRange, levelRange);
                }
                else
                {
                    int levelProgress = displayPoints - currentThreshold;
                    int levelRange = nextThreshold - currentThreshold;
                    friendshipBar.SetValues(levelProgress, levelRange);
                }
            }
        }

        /// <summary>
        /// Shows a specific page by index, displaying it as a book spread.
        /// Index -1 shows the introduction page alone.
        /// Index 0+ shows the back of the previous page (left) and front of current page (right).
        /// </summary>
        private void ShowPage(int pageIndex)
        {
            if (pageIndex < -1 || pageIndex >= m_instantiatedPages.Count)
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] Invalid page index: {pageIndex}", DebugCategory.UI);
                return;
            }

            // Hide all instantiated pages first
            foreach (GameObject page in m_instantiatedPages)
            {
                if (page != null)
                {
                    page.SetActive(false);
                }
            }

            if (pageIndex == -1)
            {
                // Show only introduction page (front side visible)
                if (m_firstPage != null)
                {
                    m_firstPage.SetActive(true);
                    BirdPageUI introPageUI = m_firstPage.GetComponent<BirdPageUI>();
                    if (introPageUI != null)
                    {
                        introPageUI.SetPageSide(showingBack: false);
                    }
                }
                DebugBase.Log($"[{nameof(DiaryUIManager)}] Showing introduction page", DebugCategory.UI);
            }
            else
            {
                // Hide introduction page when viewing bird pages
                if (m_firstPage != null)
                {
                    m_firstPage.SetActive(false);
                }

                // For bird pages, show as a spread:
                // - Previous page (to see its back/left side)
                // - Current page (to see its front/right side)

                // Show previous page for its back content (left side of spread)
                if (pageIndex == 0)
                {
                    // First bird page: show introduction page for its back
                    if (m_firstPage != null)
                    {
                        m_firstPage.SetActive(true);
                        BirdPageUI introPageUI = m_firstPage.GetComponent<BirdPageUI>();
                        if (introPageUI != null)
                        {
                            introPageUI.SetPageSide(showingBack: true);
                        }
                    }
                }
                else if (pageIndex > 0)
                {
                    // Other pages: show previous instantiated page for its back
                    GameObject prevPage = m_instantiatedPages[pageIndex - 1];
                    prevPage.SetActive(true);
                    BirdPageUI prevPageUI = prevPage.GetComponent<BirdPageUI>();
                    if (prevPageUI != null)
                    {
                        prevPageUI.SetPageSide(showingBack: true);
                    }
                }

                // Show current page for its front content (right side of spread)
                if (pageIndex >= 0 && pageIndex < m_instantiatedPages.Count)
                {
                    GameObject currentPage = m_instantiatedPages[pageIndex];
                    currentPage.SetActive(true);
                    BirdPageUI currentPageUI = currentPage.GetComponent<BirdPageUI>();
                    if (currentPageUI != null)
                    {
                        currentPageUI.SetPageSide(showingBack: false);
                    }
                }

                DebugBase.Log($"[{nameof(DiaryUIManager)}] Showing page {pageIndex + 1}/{m_instantiatedPages.Count} as spread", DebugCategory.UI);
            }

            m_currentPageIndex = pageIndex;
            UpdateNavigationButtons();
        }

        /// <summary>
        /// Shows the next page with animation.
        /// Uses m_targetPageIndex when animating so rapid clicks advance correctly.
        /// </summary>
        public async void ShowNextPage()
        {
            try
            {
                int effectiveIndex = m_isAnimating ? m_targetPageIndex : m_currentPageIndex;
                if (effectiveIndex < m_instantiatedPages.Count - 1)
                {
                    await ShowPageWithAnimationAsync(effectiveIndex + 1);
                }
            }
            catch (Exception e)
            {
                DebugBase.LogError($"[{nameof(DiaryUIManager)}] ShowNextPage failed: {e.Message}");
            }
        }

        /// <summary>
        /// Shows the previous page with animation.
        /// Uses m_targetPageIndex when animating so rapid clicks go back correctly.
        /// </summary>
        public async void ShowPreviousPage()
        {
            try
            {
                int effectiveIndex = m_isAnimating ? m_targetPageIndex : m_currentPageIndex;
                if (effectiveIndex > -1)
                {
                    await ShowPageWithAnimationAsync(effectiveIndex - 1);
                }
            }
            catch (Exception e)
            {
                DebugBase.LogError($"[{nameof(DiaryUIManager)}] ShowPreviousPage failed: {e.Message}");
            }
        }

        /// <summary>
        /// Shows a specific page with page turn animation.
        /// Supports interruption: if called while animating, completes the current
        /// animation instantly and starts the new one.
        /// </summary>
        private async UniTask ShowPageWithAnimationAsync(int pageIndex)
        {
            if (pageIndex < -1 || pageIndex >= m_instantiatedPages.Count)
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] Invalid page index: {pageIndex}", DebugCategory.UI);
                return;
            }

            // If already animating, complete the current animation instantly and snap to its target state
            if (m_isAnimating && m_currentlyAnimatingPageUI != null)
            {
                m_currentlyAnimatingPageUI.CompleteCurrentAnimation();
                m_currentlyAnimatingPageUI.ResetPosition();
                m_currentlyAnimatingPageUI = null;

                // Snap to the target state of the interrupted animation
                ShowPage(m_targetPageIndex);
            }

            // Track the target so rapid clicks can advance from the right index
            m_targetPageIndex = pageIndex;
            m_isAnimating = true;

            bool isMovingForward = pageIndex > m_currentPageIndex;
            GameObject pageToFlip = null;
            BirdPageUI pageToFlipUI = null;

            if (isMovingForward)
            {
                // Moving forward: flip the current right-side page to show its back on the left

                // Step 1: Identify which page to flip
                if (m_currentPageIndex == -1)
                {
                    pageToFlip = m_firstPage;
                }
                else if (m_currentPageIndex >= 0)
                {
                    pageToFlip = m_instantiatedPages[m_currentPageIndex];
                }

                // Step 2: Make the next page visible (showing front)
                if (pageIndex >= 0 && pageIndex < m_instantiatedPages.Count)
                {
                    m_instantiatedPages[pageIndex].SetActive(true);
                    BirdPageUI nextPageUI = m_instantiatedPages[pageIndex].GetComponent<BirdPageUI>();
                    if (nextPageUI != null)
                    {
                        nextPageUI.SetPageSide(showingBack: false);
                    }
                }

                // Step 3: Animate the page flip
                if (pageToFlip != null)
                {
                    pageToFlipUI = pageToFlip.GetComponent<BirdPageUI>();
                    if (pageToFlipUI != null)
                    {
                        pageToFlipUI.BringToFront();
                        m_currentlyAnimatingPageUI = pageToFlipUI;

                        await pageToFlipUI.TurnPageAsync(m_pageTurnDuration, showingBack: true);

                        // If this animation was interrupted, skip cleanup
                        if (m_targetPageIndex != pageIndex)
                        {
                            return;
                        }

                        pageToFlipUI.ResetPosition();
                        m_currentlyAnimatingPageUI = null;
                    }
                }

                // Step 4: Clean up - hide pages no longer in view
                if (pageIndex > 0)
                {
                    if (m_firstPage != null)
                    {
                        m_firstPage.SetActive(false);
                    }

                    if (pageIndex > 1 && m_instantiatedPages.Count > pageIndex - 2)
                    {
                        m_instantiatedPages[pageIndex - 2].SetActive(false);
                    }
                }
            }
            else
            {
                // Moving backward: flip the current left-side page to show its front on the right

                // Step 1: Identify which page to flip
                if (m_currentPageIndex == 0)
                {
                    pageToFlip = m_firstPage;
                }
                else if (m_currentPageIndex > 0)
                {
                    pageToFlip = m_instantiatedPages[m_currentPageIndex - 1];
                }

                // Step 2: Make necessary pages visible for the target spread
                if (pageIndex == -1)
                {
                    if (m_firstPage != null)
                    {
                        m_firstPage.SetActive(true);
                    }
                }
                else if (pageIndex == 0)
                {
                    if (m_firstPage != null)
                    {
                        m_firstPage.SetActive(true);
                        BirdPageUI introUI = m_firstPage.GetComponent<BirdPageUI>();
                        if (introUI != null)
                        {
                            introUI.SetPageSide(showingBack: true);
                        }
                    }
                    if (m_instantiatedPages.Count > 0)
                    {
                        m_instantiatedPages[0].SetActive(true);
                    }
                }
                else
                {
                    if (pageIndex - 1 >= 0)
                    {
                        m_instantiatedPages[pageIndex - 1].SetActive(true);
                    }
                }

                // Step 3: Animate the page flip
                if (pageToFlip != null)
                {
                    pageToFlipUI = pageToFlip.GetComponent<BirdPageUI>();
                    if (pageToFlipUI != null)
                    {
                        pageToFlipUI.BringToFront();
                        m_currentlyAnimatingPageUI = pageToFlipUI;

                        await pageToFlipUI.TurnPageAsync(m_pageTurnDuration, showingBack: false);

                        // If this animation was interrupted, skip cleanup
                        if (m_targetPageIndex != pageIndex)
                        {
                            return;
                        }

                        pageToFlipUI.ResetPosition();
                        m_currentlyAnimatingPageUI = null;
                    }
                }

                // Step 4: Clean up - hide the page we just left
                if (m_currentPageIndex >= 0 && m_currentPageIndex < m_instantiatedPages.Count)
                {
                    m_instantiatedPages[m_currentPageIndex].SetActive(false);
                }

                if (pageIndex >= 0 && m_currentPageIndex < m_instantiatedPages.Count && m_currentPageIndex + 1 < m_instantiatedPages.Count)
                {
                    m_instantiatedPages[m_currentPageIndex + 1].SetActive(false);
                }
            }

            // Update the current index
            m_currentPageIndex = pageIndex;
            m_isAnimating = false;
            UpdateNavigationButtons();
            TriggerFriendshipAnimationAsync(pageIndex);

            if (pageIndex == -1)
            {
                DebugBase.Log($"[{nameof(DiaryUIManager)}] Animated to introduction page", DebugCategory.UI);
            }
            else
            {
                DebugBase.Log($"[{nameof(DiaryUIManager)}] Animated to page {pageIndex + 1}/{m_instantiatedPages.Count}", DebugCategory.UI);
            }
        }

        /// <summary>
        /// Animates the friendship bar for the bird shown at pageIndex, if new points were gained
        /// since the last time the player viewed that page.
        /// </summary>
        private async void TriggerFriendshipAnimationAsync(int pageIndex)
        {
            try
            {
                if (pageIndex < 0)
                {
                    return;
                }

                List<BirdData> allBirds = GameManager.Instance.DiaryManager.GetAllBirdsForDiary();
                if (pageIndex >= allBirds.Count)
                {
                    return;
                }

                BirdData birdData = allBirds[pageIndex];
                if (!GameManager.Instance.DiaryManager.IsBirdDiscovered(birdData))
                {
                    return;
                }

                string birdID = birdData.BirdID;
                if (m_animatedThisSession.Contains(birdID))
                {
                    return;
                }

                int currentPoints = GameManager.Instance.FriendshipManager.GetFriendship(birdID);
                int lastSeenPoints = GameManager.Instance.FriendshipManager.GetLastSeenFriendship(birdID);
                GameManager.Instance.FriendshipManager.UpdateLastSeenFriendship(birdID, currentPoints);

                if (currentPoints <= lastSeenPoints)
                {
                    return;
                }

                ResourceBarTracker friendshipBar = GetFriendshipBarForBird(pageIndex);
                if (friendshipBar == null)
                {
                    return;
                }

                m_animatedThisSession.Add(birdID);

                FriendshipManager fm = GameManager.Instance.FriendshipManager;
                int lastLevel = fm.GetFriendshipLevelForPoints(birdData, lastSeenPoints);
                int currentLevel = fm.GetFriendshipLevelForPoints(birdData, currentPoints);

                float fromFill = ComputeFriendshipFill(birdData, lastSeenPoints);
                GetFriendshipBarValues(birdData, currentPoints, out int toCurrent, out int toMax);

                if (lastLevel == currentLevel)
                {
                    await friendshipBar.AnimateAsync(fromFill, toCurrent, toMax, m_friendshipBarAnimationDuration);
                    return;
                }

                // Leveled up: animate through each level boundary at constant speed.

                // Step 1: fill the remainder of the starting level
                float remaining = 1f - fromFill;
                if (remaining > 0f)
                {
                    int startThreshold = birdData.GetFriendshipRequirement(lastLevel);
                    int startNextThreshold = birdData.GetFriendshipRequirement(lastLevel + 1);
                    int startRange = startNextThreshold - startThreshold;
                    await friendshipBar.AnimateAsync(fromFill, startRange, startRange, m_friendshipBarAnimationDuration * remaining);
                }

                // Step 2: animate through each fully-completed intermediate level
                for (int level = lastLevel + 1; level < currentLevel; level++)
                {
                    int threshold = birdData.GetFriendshipRequirement(level);
                    int nextThreshold = birdData.GetFriendshipRequirement(level + 1);
                    if (nextThreshold == int.MaxValue)
                    {
                        break;
                    }

                    int range = nextThreshold - threshold;
                    await friendshipBar.AnimateAsync(0f, range, range, m_friendshipBarAnimationDuration);
                }

                // Final step: animate from 0 to the correct position in the new level
                float finalRatio = toMax > 0 ? Mathf.Clamp01((float)toCurrent / toMax) : 0f;
                await friendshipBar.AnimateAsync(0f, toCurrent, toMax, m_friendshipBarAnimationDuration * finalRatio);
            }
            catch (Exception e)
            {
                DebugBase.LogError($"[{nameof(DiaryUIManager)}] TriggerFriendshipAnimationAsync failed: {e.Message}");
            }
        }

        private ResourceBarTracker GetFriendshipBarForBird(int birdIndex)
        {
            if (birdIndex == 0)
            {
                return m_firstPage.GetComponent<BirdPageUI>()?.FriendshipBar;
            }

            if (birdIndex - 1 < m_instantiatedPages.Count)
            {
                return m_instantiatedPages[birdIndex - 1].GetComponent<BirdPageUI>()?.FriendshipBar;
            }

            return null;
        }

        private float ComputeFriendshipFill(BirdData birdData, int points)
        {
            FriendshipManager fm = GameManager.Instance.FriendshipManager;
            int level = fm.GetFriendshipLevelForPoints(birdData, points);
            int currentThreshold = birdData.GetFriendshipRequirement(level);
            int nextThreshold = birdData.GetFriendshipRequirement(level + 1);

            if (nextThreshold == int.MaxValue)
            {
                return 1f;
            }

            int levelRange = nextThreshold - currentThreshold;
            if (levelRange <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)(points - currentThreshold) / levelRange);
        }

        private void GetFriendshipBarValues(BirdData birdData, int points, out int current, out int max)
        {
            FriendshipManager fm = GameManager.Instance.FriendshipManager;
            int level = fm.GetFriendshipLevelForPoints(birdData, points);
            int currentThreshold = birdData.GetFriendshipRequirement(level);
            int nextThreshold = birdData.GetFriendshipRequirement(level + 1);

            if (nextThreshold == int.MaxValue)
            {
                int prevThreshold = level > 0 ? birdData.GetFriendshipRequirement(level - 1) : 0;
                int levelRange = currentThreshold - prevThreshold;
                current = levelRange;
                max = levelRange;
                return;
            }

            current = points - currentThreshold;
            max = nextThreshold - currentThreshold;
        }

        /// <summary>
        /// Updates navigation button states based on current page.
        /// Index -1 is the introduction page.
        /// </summary>
        private void UpdateNavigationButtons()
        {
            if (m_previousButton != null)
            {
                // Can go back if we're beyond the introduction page (index > -1)
                m_previousButton.interactable = m_currentPageIndex > -1;
            }

            if (m_nextButton != null)
            {
                // Can go forward if we're not on the last instantiated page
                m_nextButton.interactable = m_currentPageIndex < m_instantiatedPages.Count - 1;
            }
        }

        /// <summary>
        /// Refreshes all bird pages (useful when a new bird is discovered).
        /// Preserves the current page index, including the introduction page (index -1).
        /// </summary>
        public void RefreshAllPages()
        {
            int currentIndex = m_currentPageIndex;
            CreateBirdPages();

            // Restore the previous page, clamping to valid range
            // -1 is valid for introduction page, 0 to Count-1 for bird pages
            int targetIndex = Mathf.Clamp(currentIndex, -1, m_instantiatedPages.Count - 1);
            ShowPage(targetIndex);

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Refreshed all diary pages", DebugCategory.UI);
        }

        /// <summary>
        /// Refreshes a single bird's pages without recreating all pages.
        /// Since bird data is split across two sides, we refresh both left and right pages.
        /// </summary>
        private void RefreshSinglePage(BirdData birdData)
        {
            if (birdData == null)
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] Cannot refresh page for null BirdData", DebugCategory.UI);
                return;
            }

            if (!m_birdIDToPageIndex.TryGetValue(birdData.BirdID, out int birdIndex))
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] Bird {birdData.BirdName} ({birdData.BirdID}) not found in page index", DebugCategory.UI);
                return;
            }

            List<BirdData> allBirds = GameManager.Instance.DiaryManager.GetAllBirdsForDiary();

            // Refresh BACK page (photo, name, stats) - shown on back of intro page or previous page
            if (birdIndex == 0)
            {
                BirdPageUI introPageUI = m_firstPage.GetComponent<BirdPageUI>();
                if (introPageUI != null)
                {
                    PopulateBackPage(introPageUI.BackParent, introPageUI.BirdPhoto, introPageUI.NameText,
                        introPageUI.ScientificNameText, introPageUI.InteractionCounterText,
                        introPageUI.FriendshipLevelText, introPageUI.FriendshipBar,
                        introPageUI.VisitHoursText, introPageUI.FoodText,
                        introPageUI.DietIconContainer, introPageUI.DietIconPrefab, birdData);
                }
            }
            else if (birdIndex - 1 >= 0 && birdIndex - 1 < m_instantiatedPages.Count)
            {
                GameObject prevPage = m_instantiatedPages[birdIndex - 1];
                BirdPageUI prevPageUI = prevPage.GetComponent<BirdPageUI>();
                if (prevPageUI != null)
                {
                    PopulateBackPage(prevPageUI.BackParent, prevPageUI.BirdPhoto, prevPageUI.NameText,
                        prevPageUI.ScientificNameText, prevPageUI.InteractionCounterText,
                        prevPageUI.FriendshipLevelText, prevPageUI.FriendshipBar,
                        prevPageUI.VisitHoursText, prevPageUI.FoodText,
                        prevPageUI.DietIconContainer, prevPageUI.DietIconPrefab, birdData);
                }
            }

            // Refresh FRONT page (description, map) - shown on front of current page
            if (birdIndex >= 0 && birdIndex < m_instantiatedPages.Count)
            {
                GameObject currentPage = m_instantiatedPages[birdIndex];
                BirdPageUI currentPageUI = currentPage.GetComponent<BirdPageUI>();
                if (currentPageUI != null)
                {
                    PopulateFrontPage(currentPageUI.FrontParent, currentPageUI.DescriptionText,
                        currentPageUI.MapImage, currentPageUI.FeatherImage, currentPageUI.PeligroIcon, birdData);
                }
            }

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Refreshed pages for {birdData.BirdName}", DebugCategory.UI);
        }

        private void OnDestroy()
        {
            if (m_previousButton != null)
            {
                m_previousButton.onClick.RemoveListener(ShowPreviousPage);
            }

            if (m_nextButton != null)
            {
                m_nextButton.onClick.RemoveListener(ShowNextPage);
            }
            
            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            if (GameManager.Instance != null && GameManager.Instance.DiaryManager != null)
            {
                GameManager.Instance.DiaryManager.OnBirdDiscovered -= OnBirdDiscovered;
                GameManager.Instance.DiaryManager.OnBirdEncountered -= OnBirdEncountered;
            }

            if (GameManager.Instance != null && GameManager.Instance.FriendshipManager != null)
            {
                GameManager.Instance.FriendshipManager.OnFriendshipChanged -= OnFriendshipChanged;
            }
        }
        
        private void OnCloseButtonClicked()
        {
            OnCloseClicked?.Invoke();

            if (GameManager.Instance?.MenuManager != null)
            {
                GameManager.Instance.MenuManager.CloseCurrentMenu();
            }
        }

#if UNITY_EDITOR
        [DebugCommand("RefreshDiary", "UI")]
        [ContextMenu("Refresh Diary Pages")]
        private void DebugRefreshPages()
        {
            RefreshAllPages();
        }

        [DebugCommand("NextPage", "UI")]
        [ContextMenu("Next Page")]
        private void DebugNextPage()
        {
            ShowNextPage();
        }

        [DebugCommand("PreviousPage", "UI")]
        [ContextMenu("Previous Page")]
        private void DebugPreviousPage()
        {
            ShowPreviousPage();
        }
#endif
    }
}

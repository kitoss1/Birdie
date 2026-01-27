using Birdie.Data;
using Birdie.Debug;
using Birdie.UI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages the diary UI, handling page display and navigation.
    /// Gets all data from DiaryManager via GameManager.Instance.
    /// </summary>
    public class DiaryUIManager : MonoBehaviour
    {

        [Header("General UI")]
        [SerializeField]
        [Tooltip("Prefab for bird page layout (two-page spread)")]
        private GameObject m_birdPagePrefab;

        [SerializeField]
        [Tooltip("Container to hold all instantiated bird pages")]
        private Transform m_pagesContainer;

        [SerializeField]
        [Tooltip("Previous page button")]
        private Button m_previousButton;

        [SerializeField]
        [Tooltip("Next page button")]
        private Button m_nextButton;

        [SerializeField] private GameObject m_firstPage;

        [Header("Parameters to costumize")]
        [SerializeField]
        [Tooltip("Duration of page turn animation in seconds")]
        private float m_pageTurnDuration = 0.5f;
        
        [Header("Locked Bird Settings")]
        [SerializeField]
        [Tooltip("Text to display for undiscovered bird names")]
        private string m_lockedNameText = "???";

        [SerializeField]
        [Tooltip("Text to display for undiscovered bird descriptions")]
        private string m_lockedDescriptionText = "This bird has not been discovered yet.";

        [SerializeField]
        [Tooltip("Color to tint undiscovered bird photos")]
        private Color m_lockedPhotoTint = new Color(0.2f, 0.2f, 0.2f, 1f);

        private List<GameObject> m_instantiatedPages = new List<GameObject>();
        private Dictionary<string, int> m_birdIDToPageIndex = new Dictionary<string, int>();
        private int m_currentPageIndex = 0;

        private bool m_isAnimating = false;
        private int m_targetPageIndex = 0;
        private BirdPageUI m_currentlyAnimatingPageUI = null;

        private async void Start()
        {
            await WaitForGameManagerAsync();

            SetupNavigation();
            CreateBirdPages();
            SubscribeToEvents();

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Diary UI initialized");
        }

        /// <summary>
        /// Waits for GameManager to be fully initialized before proceeding.
        /// </summary>
        private async UniTask WaitForGameManagerAsync()
        {
            while (GameManager.Instance == null || !GameManager.Instance.AreAllManagersReady())
            {
                await UniTask.Yield();
            }

            DebugBase.Log($"[{nameof(DiaryUIManager)}] GameManager ready, proceeding with initialization", DebugCategory.UI);
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

            // Get birds from DiaryManager (already sorted)
            List<BirdData> allBirds = GameManager.Instance.DiaryManager.GetAllBirdsForDiary();

            if (allBirds.Count == 0)
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] No birds found in diary!", DebugCategory.UI);
                return;
            }

            // Populate introduction page's BACK with first bird's LEFT page
            BirdPageUI introPageUI = m_firstPage.GetComponent<BirdPageUI>();
            if (introPageUI != null)
            {
                PopulateLeftPage(introPageUI.m_backParent, introPageUI.BirdPhoto, introPageUI.RarityText,
                    introPageUI.ScientificNameText, introPageUI.FoodText, introPageUI.InteractionCounterText, allBirds[0]);

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

                // Populate FRONT with bird[i]'s RIGHT page (name, description)
                PopulateRightPage(pageUI.m_frontParent, pageUI.NameText, pageUI.DescriptionText, allBirds[i]);

                // Populate BACK with bird[i+1]'s LEFT page (if exists)
                if (i + 1 < allBirds.Count)
                {
                    PopulateLeftPage(pageUI.m_backParent, pageUI.BirdPhoto, pageUI.RarityText,
                        pageUI.ScientificNameText, pageUI.FoodText, pageUI.InteractionCounterText, allBirds[i + 1]);
                }
                else
                {
                    // Last page - disable back content
                    if (pageUI.m_backParent != null)
                    {
                        pageUI.m_backParent.SetActive(false);
                    }
                }

                // Initialize all bird pages to show front (0 degrees rotation)
                pageUI.SetPageSide(showingBack: false);

                m_instantiatedPages.Add(pageInstance);
                m_birdIDToPageIndex[allBirds[i].BirdID] = i;
            }

            // Show introduction page initially (index -1), hide all instantiated pages
            ShowPage(-1);

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Created {m_instantiatedPages.Count} bird pages for {allBirds.Count} birds", DebugCategory.UI);
        }

        /// <summary>
        /// Populates the LEFT page (photo, stats) of a bird.
        /// Shown on the back of pages.
        /// </summary>
        private void PopulateLeftPage(GameObject parentObject, Image birdPhoto, TextMeshProUGUI rarityText,
            TextMeshProUGUI scientificNameText, TextMeshProUGUI foodText, TextMeshProUGUI interactionCounterText,
            BirdData birdData)
        {
            if (parentObject != null)
            {
                parentObject.SetActive(true);
            }

            bool isDiscovered = GameManager.Instance.DiaryManager.IsBirdDiscovered(birdData);

            // Set bird photo
            if (birdPhoto != null)
            {
                if (isDiscovered && birdData.BirdPhoto != null)
                {
                    birdPhoto.sprite = birdData.BirdPhoto;
                    birdPhoto.color = Color.white;
                }
                else
                {
                    birdPhoto.color = m_lockedPhotoTint;
                }
            }

            // Set rarity text
            if (rarityText != null)
            {
                rarityText.text = isDiscovered ? $"Rarity: {birdData.Rarity}" : "Rarity: ???";
            }

            // Set scientific name
            if (scientificNameText != null)
            {
                scientificNameText.text = isDiscovered ? birdData.ScientificName : "???";
            }

            // Set food/diet text
            if (foodText != null)
            {
                foodText.text = isDiscovered ? $"Diet: {birdData.DietType}" : "Diet: ???";
            }

            // Set interaction counter
            if (interactionCounterText != null)
            {
                if (isDiscovered)
                {
                    int encounterCount = GameManager.Instance.DiaryManager.GetEncounterCount(birdData);
                    interactionCounterText.text = $"Interactions: {encounterCount}";
                }
                else
                {
                    interactionCounterText.text = "Interactions: ???";
                }
            }
        }

        /// <summary>
        /// Populates the RIGHT page (name, description) of a bird.
        /// Shown on the front of pages.
        /// </summary>
        private void PopulateRightPage(GameObject parentObject, TextMeshProUGUI nameText,
            TextMeshProUGUI descriptionText, BirdData birdData)
        {
            if (parentObject != null)
            {
                parentObject.SetActive(true);
            }

            bool isDiscovered = GameManager.Instance.DiaryManager.IsBirdDiscovered(birdData);

            // Set bird name
            if (nameText != null)
            {
                nameText.text = isDiscovered ? birdData.BirdName : m_lockedNameText;
            }

            // Set description
            if (descriptionText != null)
            {
                descriptionText.text = isDiscovered ? birdData.BasicDescription : m_lockedDescriptionText;
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
                // Show only introduction page
                if (m_firstPage != null)
                {
                    m_firstPage.SetActive(true);
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
                    }
                }
                else if (pageIndex > 0)
                {
                    // Other pages: show previous instantiated page for its back
                    m_instantiatedPages[pageIndex - 1].SetActive(true);
                }

                // Show current page for its front content (right side of spread)
                if (pageIndex >= 0 && pageIndex < m_instantiatedPages.Count)
                {
                    m_instantiatedPages[pageIndex].SetActive(true);
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
            int effectiveIndex = m_isAnimating ? m_targetPageIndex : m_currentPageIndex;
            if (effectiveIndex < m_instantiatedPages.Count - 1)
            {
                await ShowPageWithAnimationAsync(effectiveIndex + 1);
            }
        }

        /// <summary>
        /// Shows the previous page with animation.
        /// Uses m_targetPageIndex when animating so rapid clicks go back correctly.
        /// </summary>
        public async void ShowPreviousPage()
        {
            int effectiveIndex = m_isAnimating ? m_targetPageIndex : m_currentPageIndex;
            if (effectiveIndex > -1)
            {
                await ShowPageWithAnimationAsync(effectiveIndex - 1);
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

            // Refresh LEFT page (photo, stats) - shown on back of intro page or previous page
            if (birdIndex == 0)
            {
                // First bird's left page is on the intro page back
                BirdPageUI introPageUI = m_firstPage.GetComponent<BirdPageUI>();
                if (introPageUI != null)
                {
                    PopulateLeftPage(introPageUI.m_backParent, introPageUI.BirdPhoto, introPageUI.RarityText,
                        introPageUI.ScientificNameText, introPageUI.FoodText, introPageUI.InteractionCounterText, birdData);
                }
            }
            else if (birdIndex - 1 >= 0 && birdIndex - 1 < m_instantiatedPages.Count)
            {
                // Bird's left page is on the back of the previous instantiated page
                GameObject prevPage = m_instantiatedPages[birdIndex - 1];
                BirdPageUI prevPageUI = prevPage.GetComponent<BirdPageUI>();
                if (prevPageUI != null)
                {
                    PopulateLeftPage(prevPageUI.m_backParent, prevPageUI.BirdPhoto, prevPageUI.RarityText,
                        prevPageUI.ScientificNameText, prevPageUI.FoodText, prevPageUI.InteractionCounterText, birdData);
                }
            }

            // Refresh RIGHT page (name, description) - shown on front of current page
            if (birdIndex >= 0 && birdIndex < m_instantiatedPages.Count)
            {
                GameObject currentPage = m_instantiatedPages[birdIndex];
                BirdPageUI currentPageUI = currentPage.GetComponent<BirdPageUI>();
                if (currentPageUI != null)
                {
                    PopulateRightPage(currentPageUI.m_frontParent, currentPageUI.NameText,
                        currentPageUI.DescriptionText, birdData);
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

            if (GameManager.Instance != null && GameManager.Instance.DiaryManager != null)
            {
                GameManager.Instance.DiaryManager.OnBirdDiscovered -= OnBirdDiscovered;
                GameManager.Instance.DiaryManager.OnBirdEncountered -= OnBirdEncountered;
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

using Birdie.Data;
using Birdie.Debug;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages the diary UI, handling page display and navigation.
    /// Gets all data from DiaryManager.
    /// </summary>
    public class DiaryUIManager : MonoBehaviour
    {
        [Header("Manager References")]
        [SerializeField]
        [Tooltip("Reference to DiaryManager for bird data and discovery status")]
        private DiaryManager m_diaryManager;

        [Header("UI Components")]
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
        private int m_currentPageIndex = 0;

        private void Start()
        {
            if (m_diaryManager == null)
            {
                DebugBase.LogError($"[{nameof(DiaryUIManager)}] DiaryManager reference is not assigned!", DebugCategory.UI);
                return;
            }

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
            if (m_diaryManager != null)
            {
                m_diaryManager.OnBirdDiscovered += OnBirdDiscovered;
            }
        }

        /// <summary>
        /// Called when a new bird is discovered.
        /// </summary>
        private void OnBirdDiscovered(BirdData birdData)
        {
            DebugBase.Log($"[{nameof(DiaryUIManager)}] New bird discovered: {birdData.BirdName}, refreshing diary pages", DebugCategory.UI);
            RefreshAllPages();
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
        /// Creates a page for each bird in the database.
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

            // Clear existing pages
            foreach (GameObject page in m_instantiatedPages)
            {
                if (page != null)
                {
                    Destroy(page);
                }
            }

            m_instantiatedPages.Clear();

            // Get birds from DiaryManager (already sorted)
            List<BirdData> allBirds = m_diaryManager.GetAllBirdsForDiary();

            // Instantiate a page for each bird
            foreach (BirdData bird in allBirds)
            {
                GameObject pageInstance = Instantiate(m_birdPagePrefab, m_pagesContainer);
                PopulateBirdPage(pageInstance, bird);
                m_instantiatedPages.Add(pageInstance);
            }

            // Show first page, hide others
            if (m_instantiatedPages.Count > 0)
            {
                ShowPage(0);
            }

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Created {m_instantiatedPages.Count} bird pages", DebugCategory.UI);
        }

        /// <summary>
        /// Populates a bird page with data, showing locked state if not discovered.
        /// </summary>
        private void PopulateBirdPage(GameObject pageInstance, BirdData birdData)
        {
            bool isDiscovered = m_diaryManager.IsBirdDiscovered(birdData);

            // Find UI elements in the page
            Transform leftPage = pageInstance.transform.Find("LeftPage");
            Transform rightPage = pageInstance.transform.Find("RightPage");

            if (leftPage == null || rightPage == null)
            {
                DebugBase.LogError($"[{nameof(DiaryUIManager)}] Could not find LeftPage or RightPage in prefab!", DebugCategory.UI);
                return;
            }

            // Left page elements
            Transform photoMask = leftPage.Find("PhotoMask");
            Transform leftTexts = leftPage.Find("LeftTexts");

            // Right page elements
            TextMeshProUGUI nameText = rightPage.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descriptionText = rightPage.Find("DescriptionContainer/DescriptionText")?.GetComponent<TextMeshProUGUI>();

            if (isDiscovered)
            {
                // Show discovered bird data
                PopulateDiscoveredBird(birdData, photoMask, leftTexts, nameText, descriptionText);
            }
            else
            {
                // Show locked bird placeholder
                PopulateLockedBird(photoMask, leftTexts, nameText, descriptionText);
            }
        }

        /// <summary>
        /// Populates page with discovered bird data.
        /// </summary>
        private void PopulateDiscoveredBird(BirdData birdData, Transform photoMask, Transform leftTexts, TextMeshProUGUI nameText, TextMeshProUGUI descriptionText)
        {
            // Set bird photo
            if (photoMask != null)
            {
                Image photoImage = photoMask.Find("BirdPhoto")?.GetComponent<Image>();
                if (photoImage != null && birdData.BirdPhoto != null)
                {
                    photoImage.sprite = birdData.BirdPhoto;
                    photoImage.color = Color.white;
                }
            }

            // Set left page texts
            if (leftTexts != null)
            {
                TextMeshProUGUI rarityText = leftTexts.Find("RarityText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI scientificNameText = leftTexts.Find("ScientificNameText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI foodText = leftTexts.Find("FoodText")?.GetComponent<TextMeshProUGUI>();

                if (rarityText != null)
                {
                    rarityText.text = $"Rarity: {birdData.Rarity}";
                }

                if (scientificNameText != null)
                {
                    scientificNameText.text = birdData.ScientificName;
                }

                if (foodText != null)
                {
                    foodText.text = $"Diet: {birdData.DietType}";
                }
            }

            // Set right page texts
            if (nameText != null)
            {
                nameText.text = birdData.BirdName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = birdData.BasicDescription;
            }
        }

        /// <summary>
        /// Populates page with locked/undiscovered bird placeholder.
        /// </summary>
        private void PopulateLockedBird(Transform photoMask, Transform leftTexts, TextMeshProUGUI nameText, TextMeshProUGUI descriptionText)
        {
            // Set locked photo tint
            if (photoMask != null)
            {
                Image photoImage = photoMask.Find("BirdPhoto")?.GetComponent<Image>();
                if (photoImage != null)
                {
                    photoImage.color = m_lockedPhotoTint;
                }
            }

            // Set locked left page texts
            if (leftTexts != null)
            {
                TextMeshProUGUI rarityText = leftTexts.Find("RarityText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI scientificNameText = leftTexts.Find("ScientificNameText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI foodText = leftTexts.Find("FoodText")?.GetComponent<TextMeshProUGUI>();

                if (rarityText != null)
                {
                    rarityText.text = "Rarity: ???";
                }

                if (scientificNameText != null)
                {
                    scientificNameText.text = "???";
                }

                if (foodText != null)
                {
                    foodText.text = "Diet: ???";
                }
            }

            // Set locked right page texts
            if (nameText != null)
            {
                nameText.text = m_lockedNameText;
            }

            if (descriptionText != null)
            {
                descriptionText.text = m_lockedDescriptionText;
            }
        }

        /// <summary>
        /// Shows a specific page by index.
        /// </summary>
        private void ShowPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= m_instantiatedPages.Count)
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] Invalid page index: {pageIndex}", DebugCategory.UI);
                return;
            }

            // Hide all pages
            foreach (GameObject page in m_instantiatedPages)
            {
                if (page != null)
                {
                    page.SetActive(false);
                }
            }

            // Show requested page
            m_instantiatedPages[pageIndex].SetActive(true);
            m_currentPageIndex = pageIndex;

            UpdateNavigationButtons();

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Showing page {pageIndex + 1}/{m_instantiatedPages.Count}", DebugCategory.UI);
        }

        /// <summary>
        /// Shows the next page.
        /// </summary>
        public void ShowNextPage()
        {
            if (m_currentPageIndex < m_instantiatedPages.Count - 1)
            {
                ShowPage(m_currentPageIndex + 1);
            }
        }

        /// <summary>
        /// Shows the previous page.
        /// </summary>
        public void ShowPreviousPage()
        {
            if (m_currentPageIndex > 0)
            {
                ShowPage(m_currentPageIndex - 1);
            }
        }

        /// <summary>
        /// Updates navigation button states based on current page.
        /// </summary>
        private void UpdateNavigationButtons()
        {
            if (m_previousButton != null)
            {
                m_previousButton.interactable = m_currentPageIndex > 0;
            }

            if (m_nextButton != null)
            {
                m_nextButton.interactable = m_currentPageIndex < m_instantiatedPages.Count - 1;
            }
        }

        /// <summary>
        /// Refreshes all bird pages (useful when a new bird is discovered).
        /// </summary>
        public void RefreshAllPages()
        {
            int currentIndex = m_currentPageIndex;
            CreateBirdPages();

            if (m_instantiatedPages.Count > 0)
            {
                ShowPage(Mathf.Min(currentIndex, m_instantiatedPages.Count - 1));
            }

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Refreshed all diary pages", DebugCategory.UI);
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

            if (m_diaryManager != null)
            {
                m_diaryManager.OnBirdDiscovered -= OnBirdDiscovered;
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

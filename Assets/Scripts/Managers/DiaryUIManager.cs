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
        private Dictionary<string, int> m_birdIDToPageIndex = new Dictionary<string, int>();
        private int m_currentPageIndex = 0;

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
            m_birdIDToPageIndex.Clear();

            // Get birds from DiaryManager (already sorted)
            List<BirdData> allBirds = GameManager.Instance.DiaryManager.GetAllBirdsForDiary();

            // Instantiate a page for each bird
            int pageIndex = 0;
            foreach (BirdData bird in allBirds)
            {
                GameObject pageInstance = Instantiate(m_birdPagePrefab, m_pagesContainer);
                PopulateBirdPage(pageInstance, bird);
                m_instantiatedPages.Add(pageInstance);
                m_birdIDToPageIndex[bird.BirdID] = pageIndex;
                pageIndex++;
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
            bool isDiscovered = GameManager.Instance.DiaryManager.IsBirdDiscovered(birdData);

            // Get the BirdPageUI component
            BirdPageUI pageUI = pageInstance.GetComponent<BirdPageUI>();
            if (pageUI == null)
            {
                DebugBase.LogError($"[{nameof(DiaryUIManager)}] BirdPageUI component not found on page instance!", DebugCategory.UI);
                return;
            }

            if (isDiscovered)
            {
                // Show discovered bird data
                PopulateDiscoveredBird(birdData, pageUI);
            }
            else
            {
                // Show locked bird placeholder
                PopulateLockedBird(pageUI);
            }
        }

        /// <summary>
        /// Populates page with discovered bird data.
        /// </summary>
        private void PopulateDiscoveredBird(BirdData birdData, BirdPageUI pageUI)
        {
            // Set bird photo
            if (pageUI.BirdPhoto != null && birdData.BirdPhoto != null)
            {
                pageUI.BirdPhoto.sprite = birdData.BirdPhoto;
                pageUI.BirdPhoto.color = Color.white;
            }

            // Set left page texts
            if (pageUI.RarityText != null)
            {
                pageUI.RarityText.text = $"Rarity: {birdData.Rarity}";
            }

            if (pageUI.ScientificNameText != null)
            {
                pageUI.ScientificNameText.text = birdData.ScientificName;
            }

            if (pageUI.FoodText != null)
            {
                pageUI.FoodText.text = $"Diet: {birdData.DietType}";
            }

            // Set right page texts
            if (pageUI.NameText != null)
            {
                pageUI.NameText.text = birdData.BirdName;
            }

            if (pageUI.DescriptionText != null)
            {
                pageUI.DescriptionText.text = birdData.BasicDescription;
            }

            // Set interaction counter
            if (pageUI.InteractionCounterText != null)
            {
                int encounterCount = GameManager.Instance.DiaryManager.GetEncounterCount(birdData);
                pageUI.InteractionCounterText.text = $"Interactions: {encounterCount}";
            }
        }

        /// <summary>
        /// Populates page with locked/undiscovered bird placeholder.
        /// </summary>
        private void PopulateLockedBird(BirdPageUI pageUI)
        {
            // Set locked photo tint
            if (pageUI.BirdPhoto != null)
            {
                pageUI.BirdPhoto.color = m_lockedPhotoTint;
            }

            // Set locked left page texts
            if (pageUI.RarityText != null)
            {
                pageUI.RarityText.text = "Rarity: ???";
            }

            if (pageUI.ScientificNameText != null)
            {
                pageUI.ScientificNameText.text = "???";
            }

            if (pageUI.FoodText != null)
            {
                pageUI.FoodText.text = "Diet: ???";
            }

            // Set locked right page texts
            if (pageUI.NameText != null)
            {
                pageUI.NameText.text = m_lockedNameText;
            }

            if (pageUI.DescriptionText != null)
            {
                pageUI.DescriptionText.text = m_lockedDescriptionText;
            }

            // Set locked interaction counter
            if (pageUI.InteractionCounterText != null)
            {
                pageUI.InteractionCounterText.text = "Interactions: ???";
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

        /// <summary>
        /// Refreshes a single bird page without recreating all pages.
        /// </summary>
        private void RefreshSinglePage(BirdData birdData)
        {
            if (birdData == null)
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] Cannot refresh page for null BirdData", DebugCategory.UI);
                return;
            }

            if (!m_birdIDToPageIndex.TryGetValue(birdData.BirdID, out int pageIndex))
            {
                DebugBase.LogWarning($"[{nameof(DiaryUIManager)}] Bird {birdData.BirdName} ({birdData.BirdID}) not found in page index", DebugCategory.UI);
                return;
            }

            if (pageIndex < 0 || pageIndex >= m_instantiatedPages.Count)
            {
                DebugBase.LogError($"[{nameof(DiaryUIManager)}] Invalid page index {pageIndex} for bird {birdData.BirdName}", DebugCategory.UI);
                return;
            }

            GameObject pageInstance = m_instantiatedPages[pageIndex];
            PopulateBirdPage(pageInstance, birdData);

            DebugBase.Log($"[{nameof(DiaryUIManager)}] Refreshed page for {birdData.BirdName}", DebugCategory.UI);
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

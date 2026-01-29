using System.Collections.Generic;
using Birdie.Data;
using Birdie.Managers;
using UnityEngine;

namespace Birdie.Debug
{
    /// <summary>
    /// Debug tool for editing bird friendship values.
    /// Provides a popup interface to select birds and modify their friendship points.
    /// </summary>
    public class DebugBirdEditor : MonoBehaviour
    {
        [Header("Popup Prefabs")]
        [SerializeField]
        [Tooltip("Prefab for the bird list popup")]
        private DebugBirdListPopupUI m_birdListPopupPrefab;

        [SerializeField]
        [Tooltip("Prefab for the bird detail popup")]
        private DebugBirdDetailPopupUI m_birdDetailPopupPrefab;

        [SerializeField]
        [Tooltip("Parent transform for instantiated popups (usually Canvas)")]
        private Transform m_popupParent;

        private DebugBirdListPopupUI m_currentBirdListPopup;
        private DebugBirdDetailPopupUI m_currentBirdDetailPopup;
        private BirdData m_selectedBird;
        private List<GameObject> m_instantiatedButtons = new List<GameObject>();

        /// <summary>
        /// Opens the bird list popup. Called from debug menu.
        /// </summary>
        [DebugCommand("Birds", "Debug")]
        public void OpenBirdListPopup()
        {
            if (m_birdListPopupPrefab == null)
            {
                DebugBase.LogError($"[{nameof(DebugBirdEditor)}] Bird list popup prefab is not assigned!");
                return;
            }

            if (m_popupParent == null)
            {
                DebugBase.LogError($"[{nameof(DebugBirdEditor)}] Popup parent is not assigned!");
                return;
            }

            // Close existing popups first
            CloseAllPopups();

            // Instantiate the bird list popup
            m_currentBirdListPopup = Instantiate(m_birdListPopupPrefab, m_popupParent);

            // Wire up close button
            if (m_currentBirdListPopup.CloseButton != null)
            {
                m_currentBirdListPopup.CloseButton.onClick.AddListener(CloseAllPopups);
            }

            // Populate bird list
            PopulateBirdList();

            DebugBase.Log($"[{nameof(DebugBirdEditor)}] Opened bird list popup", DebugCategory.Debug);
        }

        private void PopulateBirdList()
        {
            if (m_currentBirdListPopup == null || m_currentBirdListPopup.ContentParent == null)
            {
                return;
            }

            // Clear any existing buttons
            ClearInstantiatedButtons();

            // Get all birds
            if (GameManager.Instance == null || GameManager.Instance.DiaryManager == null)
            {
                DebugBase.LogWarning($"[{nameof(DebugBirdEditor)}] GameManager or DiaryManager not available", DebugCategory.Debug);
                return;
            }

            List<BirdData> allBirds = GameManager.Instance.DiaryManager.GetAllBirdsForDiary();

            if (m_currentBirdListPopup.ButtonPrefab == null)
            {
                DebugBase.LogError($"[{nameof(DebugBirdEditor)}] Button prefab not assigned on bird list popup!");
                return;
            }

            // Create a button for each bird
            foreach (BirdData bird in allBirds)
            {
                DebugButton button = Instantiate(m_currentBirdListPopup.ButtonPrefab, m_currentBirdListPopup.ContentParent);
                m_instantiatedButtons.Add(button.gameObject);

                if (button.ButtonText != null)
                {
                    button.ButtonText.text = bird.BirdName;
                }

                // Capture bird in closure
                BirdData capturedBird = bird;
                button.ButtonElement.onClick.AddListener(() => OpenBirdDetailPopup(capturedBird));
            }

            DebugBase.Log($"[{nameof(DebugBirdEditor)}] Populated bird list with {allBirds.Count} birds", DebugCategory.Debug);
        }

        private void OpenBirdDetailPopup(BirdData bird)
        {
            if (m_birdDetailPopupPrefab == null)
            {
                DebugBase.LogError($"[{nameof(DebugBirdEditor)}] Bird detail popup prefab is not assigned!");
                return;
            }

            m_selectedBird = bird;

            // Hide the list popup but keep it around
            if (m_currentBirdListPopup != null)
            {
                m_currentBirdListPopup.gameObject.SetActive(false);
            }

            // Instantiate the detail popup
            m_currentBirdDetailPopup = Instantiate(m_birdDetailPopupPrefab, m_popupParent);

            // Wire up buttons
            if (m_currentBirdDetailPopup.ApplyButton != null)
            {
                m_currentBirdDetailPopup.ApplyButton.onClick.AddListener(ApplyFriendshipChange);
            }

            if (m_currentBirdDetailPopup.BackButton != null)
            {
                m_currentBirdDetailPopup.BackButton.onClick.AddListener(GoBackToList);
            }

            if (m_currentBirdDetailPopup.CloseButton != null)
            {
                m_currentBirdDetailPopup.CloseButton.onClick.AddListener(CloseAllPopups);
            }

            // Populate the detail popup
            UpdateDetailPopupDisplay();

            DebugBase.Log($"[{nameof(DebugBirdEditor)}] Opened detail popup for {bird.BirdName}", DebugCategory.Debug);
        }

        private void UpdateDetailPopupDisplay()
        {
            if (m_currentBirdDetailPopup == null || m_selectedBird == null)
            {
                return;
            }

            FriendshipManager friendshipManager = GameManager.Instance.FriendshipManager;
            int currentPoints = friendshipManager.GetFriendship(m_selectedBird.BirdID);
            int currentLevel = friendshipManager.GetFriendshipLevel(m_selectedBird.BirdID, m_selectedBird);

            if (m_currentBirdDetailPopup.HeaderText != null)
            {
                m_currentBirdDetailPopup.HeaderText.text = m_selectedBird.BirdName;
            }

            if (m_currentBirdDetailPopup.CurrentFriendshipText != null)
            {
                m_currentBirdDetailPopup.CurrentFriendshipText.text = $"Current: {currentPoints}";
            }

            if (m_currentBirdDetailPopup.CurrentLevelText != null)
            {
                m_currentBirdDetailPopup.CurrentLevelText.text = $"Level: {currentLevel}";
            }

            if (m_currentBirdDetailPopup.FriendshipInput != null)
            {
                m_currentBirdDetailPopup.FriendshipInput.text = currentPoints.ToString();
            }
        }

        private void ApplyFriendshipChange()
        {
            if (m_selectedBird == null || m_currentBirdDetailPopup == null)
            {
                return;
            }

            if (m_currentBirdDetailPopup.FriendshipInput == null)
            {
                return;
            }

            string inputText = m_currentBirdDetailPopup.FriendshipInput.text;

            if (!int.TryParse(inputText, out int newValue))
            {
                DebugBase.LogWarning($"[{nameof(DebugBirdEditor)}] Invalid friendship value: {inputText}", DebugCategory.Debug);
                return;
            }

            // Set the new friendship value
            GameManager.Instance.FriendshipManager.SetFriendship(m_selectedBird.BirdID, newValue);

            // Update the display
            UpdateDetailPopupDisplay();

            // Refresh the diary UI if it exists
            RefreshDiaryUI();

            DebugBase.Log($"[{nameof(DebugBirdEditor)}] Set friendship for {m_selectedBird.BirdName} to {newValue}", DebugCategory.Debug);
        }

        private void RefreshDiaryUI()
        {
            DiaryUIManager diaryUIManager = FindFirstObjectByType<DiaryUIManager>();

            if (diaryUIManager != null)
            {
                diaryUIManager.RefreshAllPages();
                DebugBase.Log($"[{nameof(DebugBirdEditor)}] Refreshed diary UI", DebugCategory.Debug);
            }
        }

        private void GoBackToList()
        {
            // Destroy detail popup
            if (m_currentBirdDetailPopup != null)
            {
                Destroy(m_currentBirdDetailPopup.gameObject);
                m_currentBirdDetailPopup = null;
            }

            // Show list popup again
            if (m_currentBirdListPopup != null)
            {
                m_currentBirdListPopup.gameObject.SetActive(true);
            }

            m_selectedBird = null;

            DebugBase.Log($"[{nameof(DebugBirdEditor)}] Returned to bird list", DebugCategory.Debug);
        }

        private void CloseAllPopups()
        {
            ClearInstantiatedButtons();

            if (m_currentBirdListPopup != null)
            {
                Destroy(m_currentBirdListPopup.gameObject);
                m_currentBirdListPopup = null;
            }

            if (m_currentBirdDetailPopup != null)
            {
                Destroy(m_currentBirdDetailPopup.gameObject);
                m_currentBirdDetailPopup = null;
            }

            m_selectedBird = null;

            DebugBase.Log($"[{nameof(DebugBirdEditor)}] Closed all popups", DebugCategory.Debug);
        }

        private void ClearInstantiatedButtons()
        {
            foreach (GameObject button in m_instantiatedButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }

            m_instantiatedButtons.Clear();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_birdListPopupPrefab == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdEditor)}] Bird List Popup Prefab reference is missing!", this);
            }

            if (m_birdDetailPopupPrefab == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdEditor)}] Bird Detail Popup Prefab reference is missing!", this);
            }

            if (m_popupParent == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdEditor)}] Popup Parent reference is missing!", this);
            }
        }
#endif
    }
}

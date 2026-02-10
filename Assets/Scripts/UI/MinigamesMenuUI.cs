using System.Collections.Generic;
using Birdie.Data;
using Birdie.Debug;
using Birdie.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// UI component for the minigames menu panel.
    /// Handles minigame selection, instantiation, and cleanup.
    /// </summary>
    public sealed class MinigamesMenuUI : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField]
        [Tooltip("Button to close the minigames menu")]
        private Button m_closeButton;

        [Header("Minigame Container")]
        [SerializeField]
        [Tooltip("Parent transform for instantiated minigame prefabs")]
        private Transform m_minigameContainer;

        private GameObject m_currentMinigameInstance;
        private BirdData m_currentBirdData;

        private void Awake()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDestroy()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            CleanupCurrentMinigame();
        }

        /// <summary>
        /// Sets up the minigames panel with a random minigame from the bird's available list.
        /// </summary>
        public void Setup(BirdData birdData)
        {
            m_currentBirdData = birdData;
            CleanupCurrentMinigame();

            MinigameData selectedMinigame = SelectRandomMinigame(birdData.AvailableMinigames);
            if (selectedMinigame == null)
            {
                DebugBase.LogWarning($"[{nameof(MinigamesMenuUI)}] No valid minigames available for {birdData.BirdName}", DebugCategory.UI);
                return;
            }

            InstantiateMinigame(selectedMinigame);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartMinigame();
            }

            DebugBase.Log($"[{nameof(MinigamesMenuUI)}] Started minigame '{selectedMinigame.MinigameName}' for {birdData.BirdName}", DebugCategory.UI);
        }

        private MinigameData SelectRandomMinigame(List<MinigameData> minigames)
        {
            if (minigames == null || minigames.Count == 0)
            {
                return null;
            }

            var validMinigames = new List<MinigameData>();
            foreach (MinigameData minigame in minigames)
            {
                if (minigame != null && minigame.MinigamePrefab != null)
                {
                    validMinigames.Add(minigame);
                }
            }

            if (validMinigames.Count == 0)
            {
                return null;
            }

            int randomIndex = Random.Range(0, validMinigames.Count);
            return validMinigames[randomIndex];
        }

        private void InstantiateMinigame(MinigameData minigameData)
        {
            if (m_minigameContainer == null)
            {
                DebugBase.LogError($"[{nameof(MinigamesMenuUI)}] Minigame container is not assigned", DebugCategory.UI);
                return;
            }

            m_currentMinigameInstance = Instantiate(minigameData.MinigamePrefab, m_minigameContainer);
            DebugBase.Log($"[{nameof(MinigamesMenuUI)}] Instantiated minigame: {minigameData.MinigameName}", DebugCategory.UI);
        }

        private void CleanupCurrentMinigame()
        {
            if (m_currentMinigameInstance != null)
            {
                Destroy(m_currentMinigameInstance);
                m_currentMinigameInstance = null;
            }
        }

        private void OnCloseClicked()
        {
            DebugBase.Log($"[{nameof(MinigamesMenuUI)}] Close clicked", DebugCategory.UI);

            CleanupCurrentMinigame();
            m_currentBirdData = null;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndMinigame();
            }

            if (GameManager.Instance?.MenuManager != null)
            {
                GameManager.Instance.MenuManager.CloseCurrentMenu();
            }
        }
    }
}

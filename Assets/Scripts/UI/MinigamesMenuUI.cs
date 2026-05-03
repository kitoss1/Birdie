using System.Collections.Generic;
using Birdie.Data;
using Birdie.Debug;
using Birdie.Managers;
using Birdie.UI.Minigames;
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

        [Header("Close Warning")]
        [SerializeField]
        [Tooltip("Panel shown to confirm closing the minigame")]
        private GameObject m_closeWarningPanel;

        [SerializeField]
        [Tooltip("Confirms closing the minigame")]
        private Button m_closeWarningYesButton;

        [SerializeField]
        [Tooltip("Cancels closing and returns to the minigame")]
        private Button m_closeWarningNoButton;

        [Header("Minigame Container")]
        [SerializeField]
        [Tooltip("Parent transform for instantiated minigame prefabs")]
        private Transform m_minigameContainer;

        private GameObject m_currentMinigameInstance;
        private IMinigame m_currentMinigame;
        private BirdData m_currentBirdData;

        private void Awake()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (m_closeWarningYesButton != null)
            {
                m_closeWarningYesButton.onClick.AddListener(OnCloseWarningYesClicked);
            }

            if (m_closeWarningNoButton != null)
            {
                m_closeWarningNoButton.onClick.AddListener(OnCloseWarningNoClicked);
            }

            HideCloseWarning();
        }

        private void OnDestroy()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            if (m_closeWarningYesButton != null)
            {
                m_closeWarningYesButton.onClick.RemoveListener(OnCloseWarningYesClicked);
            }

            if (m_closeWarningNoButton != null)
            {
                m_closeWarningNoButton.onClick.RemoveListener(OnCloseWarningNoClicked);
            }

            CleanupCurrentMinigame();
        }

        /// <summary>
        /// Sets up the minigames panel with a random minigame from the bird's available list.
        /// </summary>
        public void Setup(BirdData birdData)
        {
            m_currentBirdData = birdData;
            HideCloseWarning();
            CleanupCurrentMinigame();

            MinigameData selectedMinigame = SelectRandomMinigame(birdData.AvailableMinigames);
            if (selectedMinigame == null)
            {
                DebugBase.LogWarning($"[{nameof(MinigamesMenuUI)}] No valid minigames available for {birdData.BirdName}", DebugCategory.UI);
                return;
            }

            InstantiateMinigame(selectedMinigame);

            m_currentMinigame = m_currentMinigameInstance != null
                ? m_currentMinigameInstance.GetComponentInChildren<IMinigame>()
                : null;

            if (m_currentMinigame != null)
            {
                m_currentMinigame.SetRewardTiers(selectedMinigame.RewardTiers, selectedMinigame.CompletionReward);

                int friendshipLevel = 0;
                if (GameManager.Instance?.FriendshipManager != null)
                {
                    friendshipLevel = GameManager.Instance.FriendshipManager.GetFriendshipLevel(
                        birdData.BirdID, birdData);
                }

                MinigameDifficultySettings difficulty = selectedMinigame.GetDifficultyForLevel(friendshipLevel);
                if (difficulty != null)
                {
                    m_currentMinigame.SetDifficulty(difficulty);
                }

                m_currentMinigame.GameClosed += OnMinigameFinished;
                m_currentMinigame.StartGame();
            }

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
            if (m_currentMinigame != null)
            {
                m_currentMinigame.GameClosed -= OnMinigameFinished;
                m_currentMinigame = null;
            }

            if (m_currentMinigameInstance != null)
            {
                Destroy(m_currentMinigameInstance);
                m_currentMinigameInstance = null;
            }
        }

        private void OnCloseClicked()
        {
            DebugBase.Log($"[{nameof(MinigamesMenuUI)}] Close clicked, showing warning", DebugCategory.UI);
            ShowCloseWarning();
        }

        private void OnCloseWarningYesClicked()
        {
            DebugBase.Log($"[{nameof(MinigamesMenuUI)}] Close confirmed", DebugCategory.UI);
            CloseMinigameMenu();
        }

        private void OnMinigameFinished()
        {
            DebugBase.Log($"[{nameof(MinigamesMenuUI)}] Minigame finished, closing menu", DebugCategory.UI);
            RewardFriendship();
            CloseMinigameMenu();
        }

        private void RewardFriendship()
        {
            if (m_currentBirdData == null || m_currentMinigame == null || GameManager.Instance?.FriendshipManager == null)
            {
                return;
            }

            int reward = m_currentMinigame.FriendshipReward;
            if (reward <= 0)
            {
                return;
            }

            GameManager.Instance.FriendshipManager.AddFriendship(m_currentBirdData.BirdID, reward);
            DebugBase.Log(
                $"[{nameof(MinigamesMenuUI)}] Awarded {reward} friendship to {m_currentBirdData.BirdName}",
                DebugCategory.UI);
        }

        private void CloseMinigameMenu()
        {
            HideCloseWarning();

            CleanupCurrentMinigame();
            m_currentBirdData = null;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndMinigame();
            }

            if (GameManager.Instance?.MenuManager != null)
            {
                GameManager.Instance.MenuManager.SetMenuButtonsInteractable(true);
                GameManager.Instance.MenuManager.CloseMainMenuPanel();
            }
        }

        private void OnCloseWarningNoClicked()
        {
            DebugBase.Log($"[{nameof(MinigamesMenuUI)}] Close cancelled", DebugCategory.UI);
            HideCloseWarning();
        }

        private void ShowCloseWarning()
        {
            if (m_closeWarningPanel != null)
            {
                m_closeWarningPanel.transform.SetAsLastSibling();
                m_closeWarningPanel.SetActive(true);
            }
        }

        private void HideCloseWarning()
        {
            if (m_closeWarningPanel != null)
            {
                m_closeWarningPanel.SetActive(false);
            }
        }
    }
}

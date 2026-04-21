using System;
using System.Collections.Generic;
using Birdie.Data;
using Birdie.Debug;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Main controller for the Seed Sorting minigame.
    /// Seeds are scattered on the floor. The player drags liked seeds to the bowl
    /// and disliked seeds to the trash. Score is based on correct placements.
    /// </summary>
    public sealed class SeedSortingUI : MonoBehaviour, IMinigame
    {
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Reusable score display component")]
        private MinigameScoreDisplay m_scoreDisplay;

        [SerializeField]
        [Tooltip("Container transform where seeds are spawned")]
        private RectTransform m_seedContainer;

        [SerializeField]
        [Tooltip("Inactive seed template to clone for each spawned seed")]
        private GameObject m_seedTemplate;

        [Header("Drop Zones")]
        [SerializeField]
        [Tooltip("Drop zone for seeds the bird likes")]
        private SeedSortingDropZone m_bowlZone;

        [SerializeField]
        [Tooltip("Drop zone for seeds the bird dislikes")]
        private SeedSortingDropZone m_trashZone;

        [Header("Reference Card")]
        [SerializeField]
        [Tooltip("Container for the reference card showing liked seed types")]
        private Transform m_referenceCardContainer;

        [SerializeField]
        [Tooltip("Inactive template for each seed type icon in the reference card")]
        private GameObject m_referenceItemTemplate;

        [Header("Seed Sprites")]
        [SerializeField]
        [Tooltip("Sprites for seed types the bird likes")]
        private Sprite[] m_likedSeedSprites;

        [SerializeField]
        [Tooltip("Sprites for seed types the bird dislikes")]
        private Sprite[] m_dislikedSeedSprites;

        [Header("Scatter Settings")]
        [SerializeField]
        [Tooltip("Horizontal padding from container edges when scattering seeds")]
        private float m_scatterPaddingX = 50f;

        [SerializeField]
        [Tooltip("Vertical padding from container edges when scattering seeds")]
        private float m_scatterPaddingY = 50f;

        [SerializeField]
        [Tooltip("Minimum distance between scattered seeds")]
        private float m_minSeedSpacing = 40f;


        [Header("Reward Bar")]
        [SerializeField]
        [Tooltip("Progress bar showing score thresholds and friendship rewards")]
        private MinigameRewardBar m_rewardBar;

        [Header("Game Over")]
        [SerializeField]
        [Tooltip("Reusable game over panel component")]
        private MinigameGameOverPanel m_gameOverPanel;

        [Header("Difficulty Defaults")]
        [SerializeField]
        [Tooltip("Total number of seeds to scatter")]
        private int m_totalSeedCount = 12;

        [SerializeField]
        [Tooltip("Number of seeds the bird likes")]
        private int m_likedSeedCount = 6;


        private readonly List<SeedSortingSeed> m_activeSeeds = new List<SeedSortingSeed>();
        private readonly List<GameObject> m_referenceItems = new List<GameObject>();

        public event Action GameClosed;

        public int FriendshipReward => MinigameRewardTier.ResolveReward(m_rewardTiers, m_score, m_completionReward);

        private MinigameRewardTier[] m_rewardTiers;
        private int m_completionReward;
        private MinigameErrorTier[] m_errorTiers;
        private int m_score;
        private int m_errors;
        private int m_remainingSeeds;
        private SeedSortingState m_currentState;
        private Canvas m_canvas;

        private enum SeedSortingState
        {
            WaitingToStart,
            Playing,
            GameOver,
        }

        public void SetRewardTiers(MinigameRewardTier[] rewardTiers, int completionReward)
        {
            m_rewardTiers = rewardTiers;
            m_completionReward = completionReward;

            if (m_rewardBar != null)
            {
                m_rewardBar.Initialize(rewardTiers, completionReward);
            }
        }

        public void SetDifficulty(MinigameDifficultySettings settings)
        {
            if (settings is SeedSortingDifficultySettings sortingSettings)
            {
                m_totalSeedCount = sortingSettings.TotalSeedCount;
                m_likedSeedCount = sortingSettings.LikedSeedCount;
                m_errorTiers = sortingSettings.ErrorTiers;
            }
            else if (settings != null)
            {
                DebugBase.LogWarning(
                    $"[{nameof(SeedSortingUI)}] Received unexpected difficulty settings type: {settings.GetType().Name}",
                    DebugCategory.UI);
            }
        }

        public void StartGame()
        {
            m_score = 0;
            m_errors = 0;
            m_remainingSeeds = m_totalSeedCount;
            m_currentState = SeedSortingState.WaitingToStart;

            BuildRewardTiersFromErrors();

            m_scoreDisplay?.UpdateScore(m_score);
            m_rewardBar?.UpdateScore(0);
            m_gameOverPanel?.Hide();

            CleanupSeeds();
            CleanupReferenceCard();

            BuildReferenceCard();
            ScatterSeeds();

            m_currentState = SeedSortingState.Playing;
            SetAllSeedsInputEnabled(true);

            DebugBase.Log($"[{nameof(SeedSortingUI)}] Game started with {m_totalSeedCount} seeds ({m_likedSeedCount} liked)", DebugCategory.UI);
        }

        private void Awake()
        {
            m_canvas = GetComponentInParent<Canvas>();

            if (m_gameOverPanel != null)
            {
                m_gameOverPanel.CloseClicked += OnCloseClicked;
            }
        }

        private void OnDestroy()
        {
            if (m_gameOverPanel != null)
            {
                m_gameOverPanel.CloseClicked -= OnCloseClicked;
            }

            CleanupSeeds();
            CleanupReferenceCard();
        }

        private void BuildRewardTiersFromErrors()
        {
            MinigameRewardTier[] tiers = MinigameErrorTier.ToRewardTiers(m_errorTiers, m_totalSeedCount);
            if (tiers == null)
            {
                return;
            }

            m_rewardTiers = tiers;

            if (m_rewardBar != null)
            {
                m_rewardBar.Initialize(m_rewardTiers, m_completionReward);
            }
        }

        private void BuildReferenceCard()
        {
            if (m_referenceCardContainer == null || m_referenceItemTemplate == null || m_likedSeedSprites == null)
            {
                return;
            }

            foreach (Sprite likedSprite in m_likedSeedSprites)
            {
                GameObject item = Instantiate(m_referenceItemTemplate, m_referenceCardContainer);
                item.SetActive(true);

                Image itemImage = item.GetComponent<Image>();
                if (itemImage != null)
                {
                    itemImage.sprite = likedSprite;
                }

                m_referenceItems.Add(item);
            }
        }

        private void ScatterSeeds()
        {
            if (m_seedTemplate == null || m_seedContainer == null)
            {
                return;
            }

            bool hasLikedSprites = m_likedSeedSprites != null && m_likedSeedSprites.Length > 0;
            bool hasDislikedSprites = m_dislikedSeedSprites != null && m_dislikedSeedSprites.Length > 0;

            if (!hasLikedSprites && !hasDislikedSprites)
            {
                return;
            }

            List<Vector2> usedPositions = new List<Vector2>();

            // Spawn liked seeds
            if (hasLikedSprites)
            {
                for (int i = 0; i < m_likedSeedCount; i++)
                {
                    Sprite sprite = m_likedSeedSprites[i % m_likedSeedSprites.Length];
                    SpawnSeed(i % m_likedSeedSprites.Length, true, sprite, usedPositions);
                }
            }

            // Spawn disliked seeds
            if (hasDislikedSprites)
            {
                int dislikedCount = m_totalSeedCount - m_likedSeedCount;
                for (int i = 0; i < dislikedCount; i++)
                {
                    Sprite sprite = m_dislikedSeedSprites[i % m_dislikedSeedSprites.Length];
                    SpawnSeed(i % m_dislikedSeedSprites.Length, false, sprite, usedPositions);
                }
            }
        }

        private void SpawnSeed(int typeIndex, bool isLiked, Sprite sprite, List<Vector2> usedPositions)
        {
            GameObject seedObj = Instantiate(m_seedTemplate, m_seedContainer);
            seedObj.SetActive(true);

            SeedSortingSeed seed = seedObj.GetComponent<SeedSortingSeed>();
            if (seed == null)
            {
                Destroy(seedObj);
                return;
            }

            seed.Initialize(typeIndex, isLiked, sprite);
            seed.Dropped += OnSeedDropped;

            Vector2 position = FindScatterPosition(usedPositions);
            seed.RectTransform.anchoredPosition = position;
            seed.RectTransform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            usedPositions.Add(position);

            m_activeSeeds.Add(seed);
        }

        private Vector2 FindScatterPosition(List<Vector2> usedPositions)
        {
            Rect containerRect = m_seedContainer.rect;
            float halfWidth = (containerRect.width / 2f) - m_scatterPaddingX;
            float halfHeight = (containerRect.height / 2f) - m_scatterPaddingY;

            const int maxAttempts = 50;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector2 candidate = new Vector2(
                    UnityEngine.Random.Range(-halfWidth, halfWidth),
                    UnityEngine.Random.Range(-halfHeight, halfHeight));

                bool tooClose = false;
                foreach (Vector2 existing in usedPositions)
                {
                    if (Vector2.Distance(candidate, existing) < m_minSeedSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    return candidate;
                }
            }

            // Fallback: return random position even if overlapping
            return new Vector2(
                UnityEngine.Random.Range(-halfWidth, halfWidth),
                UnityEngine.Random.Range(-halfHeight, halfHeight));
        }

        private void OnSeedDropped(SeedSortingSeed seed, Vector2 screenPosition)
        {
            if (m_currentState != SeedSortingState.Playing)
            {
                return;
            }

            SeedSortingDropTarget target = ResolveDropTarget(screenPosition);

            if (target == SeedSortingDropTarget.None)
            {
                seed.SnapBack();
                ClearHighlights();
                return;
            }

            bool isCorrect = (seed.IsLiked && target == SeedSortingDropTarget.Bowl)
                || (!seed.IsLiked && target == SeedSortingDropTarget.Trash);

            if (isCorrect)
            {
                m_score++;
            }
            else
            {
                m_errors++;
            }

            m_scoreDisplay?.UpdateScore(m_score);
            m_rewardBar?.UpdateScore(m_score);

            DebugBase.Log(
                $"[{nameof(SeedSortingUI)}] Seed dropped on {target}. Liked: {seed.IsLiked}, Correct: {isCorrect}. Score: {m_score}, Errors: {m_errors}",
                DebugCategory.UI);

            seed.Dropped -= OnSeedDropped;
            seed.SetInputEnabled(false);
            ClearHighlights();

            seed.AnimateRemoval(() =>
            {
                m_activeSeeds.Remove(seed);
                Destroy(seed.gameObject);
            });

            m_remainingSeeds--;

            if (m_remainingSeeds <= 0)
            {
                OnAllSeedsSorted();
            }
        }

        private SeedSortingDropTarget ResolveDropTarget(Vector2 screenPosition)
        {
            Camera renderCamera = m_canvas != null ? m_canvas.worldCamera : null;

            if (m_bowlZone != null && m_bowlZone.ContainsScreenPoint(screenPosition, renderCamera))
            {
                return SeedSortingDropTarget.Bowl;
            }

            if (m_trashZone != null && m_trashZone.ContainsScreenPoint(screenPosition, renderCamera))
            {
                return SeedSortingDropTarget.Trash;
            }

            return SeedSortingDropTarget.None;
        }

        private void ClearHighlights()
        {
            m_bowlZone?.SetHighlighted(false);
            m_trashZone?.SetHighlighted(false);
        }

        private void SetAllSeedsInputEnabled(bool enabled)
        {
            foreach (SeedSortingSeed seed in m_activeSeeds)
            {
                seed.SetInputEnabled(enabled);
            }
        }

        private void OnAllSeedsSorted()
        {
            m_currentState = SeedSortingState.GameOver;

            DebugBase.Log(
                $"[{nameof(SeedSortingUI)}] All seeds sorted! Final score: {m_score}/{m_totalSeedCount}",
                DebugCategory.UI);

            m_gameOverPanel?.Show(m_score, FriendshipReward);
        }

        private void OnCloseClicked()
        {
            DebugBase.Log($"[{nameof(SeedSortingUI)}] Close clicked after game over", DebugCategory.UI);
            GameClosed?.Invoke();
        }

        private void CleanupSeeds()
        {
            foreach (SeedSortingSeed seed in m_activeSeeds)
            {
                if (seed != null)
                {
                    seed.Dropped -= OnSeedDropped;
                    Destroy(seed.gameObject);
                }
            }

            m_activeSeeds.Clear();
        }

        private void CleanupReferenceCard()
        {
            foreach (GameObject item in m_referenceItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }

            m_referenceItems.Clear();
        }
    }
}

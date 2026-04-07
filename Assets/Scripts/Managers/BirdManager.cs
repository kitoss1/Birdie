using Birdie.Birds;
using Birdie.Data;
using Birdie.Debug;
using System.Collections.Generic;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages all bird-related logic: spawning, appearance times, weighted selection, pity system.
    /// Responsible for the core loop of birds visiting the habitat.
    /// </summary>
    public class BirdManager : BaseManager
    {
        [Header("Bird Database")]
        [Tooltip("Path to load bird data from (relative to Resources folder)")]
        [SerializeField] private string m_birdDataPath = "ScriptableObjects/Birds";

        [Header("Bird Spawning")]
        [SerializeField] private float m_spawnCheckInterval = 30f;

        [Tooltip("Reference to BirdSpawnPoints component that defines spawn locations")]
        [SerializeField] private BirdSpawnPoints m_spawnPoints;

        [Tooltip("Maximum number of birds that can be present at once")]
        [SerializeField] private int m_maxSimultaneousBirds = 1;

        private bool m_isSpawningPaused = false;
        private float m_nextSpawnCheckTime = 0f;
        private readonly List<Bird> m_activeBirds = new List<Bird>();
        private readonly List<BirdData> m_availableBirds = new List<BirdData>();

        public override void Initialize()
        {
            base.Initialize();

            DebugBase.Log($"[{nameof(BirdManager)}] Setting up bird spawning system...", DebugCategory.Birds);

            LoadBirdDatabase();

            DebugBase.Log($"[{nameof(BirdManager)}] Loaded {m_availableBirds.Count} bird species", DebugCategory.Birds);

            m_nextSpawnCheckTime = Time.time + m_spawnCheckInterval;
        }

        /// <summary>
        /// Loads all bird data from the specified Resources folder path.
        /// </summary>
        private void LoadBirdDatabase()
        {
            BirdData[] loadedBirds = Resources.LoadAll<BirdData>(m_birdDataPath);

            if (loadedBirds == null || loadedBirds.Length == 0)
            {
                DebugBase.LogWarning($"[{nameof(BirdManager)}] No birds found at path: Resources/{m_birdDataPath}", DebugCategory.Birds);
                return;
            }

            m_availableBirds.Clear();
            m_availableBirds.AddRange(loadedBirds);

            foreach (BirdData bird in m_availableBirds)
            {
                DebugBase.Log($"[{nameof(BirdManager)}] Loaded bird: {bird.BirdName}", DebugCategory.Birds);
            }
        }

        private void Update()
        {
            if (m_isSpawningPaused)
            {
                return;
            }

            if (Time.time >= m_nextSpawnCheckTime)
            {
                CheckAndSpawnBird();
                m_nextSpawnCheckTime = Time.time + m_spawnCheckInterval;
            }
        }

        /// <summary>
        /// Checks conditions and attempts to spawn a bird.
        /// </summary>
        private void CheckAndSpawnBird()
        {
            CleanupInactiveBirds();

            if (m_activeBirds.Count >= m_maxSimultaneousBirds)
            {
                DebugBase.Log($"[{nameof(BirdManager)}] Max birds reached ({m_maxSimultaneousBirds}), skipping spawn", DebugCategory.Birds);
                return;
            }

            if (m_availableBirds.Count == 0)
            {
                DebugBase.LogWarning($"[{nameof(BirdManager)}] No birds available to spawn!", DebugCategory.Birds);
                return;
            }

            BirdData selectedBird = SelectBirdToSpawn();
            if (selectedBird != null)
            {
                SpawnBird(selectedBird);
            }
        }

        /// <summary>
        /// Selects a bird to spawn using weighted random selection and time validation.
        /// </summary>
        private BirdData SelectBirdToSpawn()
        {
            int currentHour = System.DateTime.Now.Hour;
            List<BirdData> eligibleBirds = new List<BirdData>();
            List<int> weights = new List<int>();

            foreach (BirdData bird in m_availableBirds)
            {
                if (bird.CanAppearAtTime(currentHour))
                {
                    eligibleBirds.Add(bird);
                    weights.Add(bird.BaseSpawnWeight);
                }
            }

            if (eligibleBirds.Count == 0)
            {
                DebugBase.Log($"[{nameof(BirdManager)}] No birds eligible to spawn at hour {currentHour}", DebugCategory.Birds);
                return null;
            }

            int totalWeight = 0;
            foreach (int weight in weights)
            {
                totalWeight += weight;
            }

            int randomValue = Random.Range(0, totalWeight);
            int cumulativeWeight = 0;

            for (int i = 0; i < eligibleBirds.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue < cumulativeWeight)
                {
                    return eligibleBirds[i];
                }
            }

            return eligibleBirds[eligibleBirds.Count - 1];
        }

        /// <summary>
        /// Spawns a bird instance at a random spawn point location.
        /// </summary>
        private void SpawnBird(BirdData birdData)
        {
            if (birdData.BirdPrefab == null)
            {
                DebugBase.LogError($"[{nameof(BirdManager)}] {birdData.BirdName} has no prefab assigned!", DebugCategory.Birds);
                return;
            }

            if (m_spawnPoints == null)
            {
                DebugBase.LogError($"[{nameof(BirdManager)}] BirdSpawnPoints not assigned!", DebugCategory.Birds);
                return;
            }

            Transform spawnParent = m_spawnPoints.BirdsContainer;
            m_spawnPoints.GetRandomSpawnAndLanding(out Transform spawnTransform, out Transform landingTransform);

            GameObject birdInstance = Instantiate(birdData.BirdPrefab, spawnParent);

            RectTransform birdRect = birdInstance.GetComponent<RectTransform>();
            RectTransform parentRect = spawnParent as RectTransform;
            if (birdRect != null && parentRect != null && spawnTransform != null)
            {
                birdRect.localPosition = parentRect.InverseTransformPoint(spawnTransform.position);
            }
            else
            {
                birdInstance.transform.position = spawnTransform != null ? spawnTransform.position : spawnParent.position;
            }
            birdInstance.name = $"{birdData.BirdName}_{System.DateTime.Now:HHmmss}";

            // Resolve landing world position: use landing transform if available, otherwise use spawn position as fallback.
            Vector3 landingWorldPosition = landingTransform != null
                ? landingTransform.position
                : (spawnTransform != null ? spawnTransform.position : spawnParent.position);

            Bird birdComponent = birdInstance.GetComponent<Bird>();
            if (birdComponent != null)
            {
                Vector3 spawnWorldPosition = spawnTransform != null ? spawnTransform.position : spawnParent.position;
                birdComponent.Initialize(birdData, landingWorldPosition, spawnWorldPosition);
                m_activeBirds.Add(birdComponent);
                DebugBase.Log($"[{nameof(BirdManager)}] Spawned {birdData.BirdName} at {birdInstance.transform.position}, landing at {landingWorldPosition}", DebugCategory.Birds);
            }
            else
            {
                DebugBase.LogError($"[{nameof(BirdManager)}] Bird prefab is missing Bird component!", DebugCategory.Birds);
                Destroy(birdInstance);
            }
        }

        /// <summary>
        /// Removes null references from the active birds list.
        /// </summary>
        private void CleanupInactiveBirds()
        {
            m_activeBirds.RemoveAll(bird => bird == null);
        }

        /// <summary>
        /// Pauses bird spawning (called when minigame starts)
        /// </summary>
        public void PauseBirdSpawning()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            m_isSpawningPaused = true;
            DebugBase.Log($"[{nameof(BirdManager)}] Bird spawning paused", DebugCategory.Birds);
        }

        /// <summary>
        /// Resumes bird spawning (called when minigame ends)
        /// </summary>
        public void ResumeBirdSpawning()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            m_isSpawningPaused = false;
            DebugBase.Log($"[{nameof(BirdManager)}] Bird spawning resumed", DebugCategory.Birds);
        }

        /// <summary>
        /// Pauses all active birds. Used when a minigame starts.
        /// </summary>
        public void PauseAllBirds()
        {
            CleanupInactiveBirds();

            foreach (Bird bird in m_activeBirds)
            {
                if (bird != null)
                {
                    bird.Pause();
                }
            }

            DebugBase.Log($"[{nameof(BirdManager)}] All birds paused", DebugCategory.Birds);
        }

        /// <summary>
        /// Resumes all active birds. Used when a minigame ends.
        /// </summary>
        public void ResumeAllBirds()
        {
            CleanupInactiveBirds();

            foreach (Bird bird in m_activeBirds)
            {
                if (bird != null)
                {
                    bird.Resume();
                }
            }

            DebugBase.Log($"[{nameof(BirdManager)}] All birds resumed", DebugCategory.Birds);
        }

        /// <summary>
        /// Forces all active birds to leave immediately.
        /// </summary>
        public void ClearAllBirds()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            foreach (Bird bird in m_activeBirds)
            {
                if (bird != null)
                {
                    bird.ForceDeparture();
                }
            }

            DebugBase.Log($"[{nameof(BirdManager)}] Cleared all active birds", DebugCategory.Birds);
        }

        /// <summary>
        /// Gets the count of currently active birds.
        /// </summary>
        public int GetActiveBirdCount()
        {
            if (!EnsureInitialized())
            {
                return 0;
            }

            CleanupInactiveBirds();
            return m_activeBirds.Count;
        }

        /// <summary>
        /// Gets all available bird data (read-only).
        /// </summary>
        public IReadOnlyList<BirdData> AvailableBirds => m_availableBirds;
    }
}

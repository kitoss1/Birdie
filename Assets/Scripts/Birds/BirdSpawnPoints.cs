using Birdie.Debug;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Pairs a spawn transform (where the bird appears, placed at height) with a
    /// landing transform (where the bird touches down at ground level).
    /// </summary>
    [Serializable]
    public class BirdSpawnEntry
    {
        [SerializeField]
        [Tooltip("Where the bird appears — place this above the scene for the fly-in")]
        private Transform m_spawnPoint;

        [SerializeField]
        [Tooltip("Where the bird lands after flying in")]
        private Transform m_landingPoint;

        public Transform SpawnPoint => m_spawnPoint;
        public Transform LandingPoint => m_landingPoint;
    }

    /// <summary>
    /// Defines spawn points where birds can appear in the scene.
    /// Add this to a GameObject and configure the spawn locations.
    /// </summary>
    public class BirdSpawnPoints : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField]
        [Tooltip("Each entry pairs a high spawn point with a ground-level landing point")]
        private List<BirdSpawnEntry> m_spawnPoints = new List<BirdSpawnEntry>();

        [SerializeField]
        [Tooltip("Parent transform for spawned birds")]
        private Transform m_birdsContainer;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Visualize spawn points in Scene view")]
        private bool m_showGizmos = true;

        [SerializeField]
        [Tooltip("Color of spawn point gizmos")]
        private Color m_gizmosColor = Color.cyan;

        [SerializeField]
        [Tooltip("Size of spawn point gizmos")]
        private float m_gizmosSize = 0.3f;

        public Transform BirdsContainer => m_birdsContainer != null ? m_birdsContainer : transform;

        private void Awake()
        {
            ValidateSpawnPoints();
        }

        /// <summary>
        /// Gets a random spawn position from the available spawn points.
        /// </summary>
        public Vector3 GetRandomSpawnPosition()
        {
            GetRandomSpawnAndLanding(out Transform spawnTransform, out _);
            return spawnTransform != null ? spawnTransform.position : BirdsContainer.position;
        }

        /// <summary>
        /// Gets a random entry's spawn and landing transforms.
        /// landingTransform will be null if the entry has no landing point assigned.
        /// </summary>
        public void GetRandomSpawnAndLanding(out Transform spawnTransform, out Transform landingTransform)
        {
            spawnTransform = null;
            landingTransform = null;

            if (m_spawnPoints.Count == 0)
            {
                DebugBase.LogWarning($"[{nameof(BirdSpawnPoints)}] No spawn entries defined, using container position", DebugCategory.Birds);
                return;
            }

            BirdSpawnEntry entry = m_spawnPoints[UnityEngine.Random.Range(0, m_spawnPoints.Count)];
            spawnTransform = entry.SpawnPoint;
            landingTransform = entry.LandingPoint;

            if (spawnTransform == null)
            {
                DebugBase.LogWarning($"[{nameof(BirdSpawnPoints)}] Selected spawn entry has no spawn point assigned", DebugCategory.Birds);
            }
        }

        /// <summary>
        /// Gets a random spawn point Transform, or null if none are defined.
        /// </summary>
        public Transform GetRandomSpawnTransform()
        {
            GetRandomSpawnAndLanding(out Transform spawnTransform, out _);
            return spawnTransform;
        }

        /// <summary>
        /// Gets a specific spawn point by index.
        /// </summary>
        public Vector3 GetSpawnPosition(int index)
        {
            if (index < 0 || index >= m_spawnPoints.Count)
            {
                DebugBase.LogError($"[{nameof(BirdSpawnPoints)}] Invalid spawn entry index: {index}", DebugCategory.Birds);
                return BirdsContainer.position;
            }

            Transform spawnPoint = m_spawnPoints[index].SpawnPoint;
            if (spawnPoint == null)
            {
                DebugBase.LogWarning($"[{nameof(BirdSpawnPoints)}] Spawn entry at index {index} has no spawn point assigned", DebugCategory.Birds);
                return BirdsContainer.position;
            }

            return spawnPoint.position;
        }

        /// <summary>
        /// Gets the total number of spawn points.
        /// </summary>
        public int GetSpawnPointCount()
        {
            return m_spawnPoints.Count;
        }

        /// <summary>
        /// Validates that all spawn points are assigned and logs warnings for null entries.
        /// </summary>
        private void ValidateSpawnPoints()
        {
            int nullCount = 0;
            for (int i = 0; i < m_spawnPoints.Count; i++)
            {
                if (m_spawnPoints[i] == null)
                {
                    nullCount++;
                }
            }

            if (nullCount > 0)
            {
                DebugBase.LogWarning($"[{nameof(BirdSpawnPoints)}] {nullCount} spawn points are null and will be ignored", DebugCategory.Birds);
            }

            DebugBase.Log($"[{nameof(BirdSpawnPoints)}] Initialized with {m_spawnPoints.Count - nullCount} valid spawn points", DebugCategory.Birds);
        }

        private void OnDrawGizmos()
        {
            if (!m_showGizmos)
            {
                return;
            }

            Gizmos.color = m_gizmosColor;

            foreach (BirdSpawnEntry entry in m_spawnPoints)
            {
                if (entry.SpawnPoint != null)
                {
                    Gizmos.DrawWireSphere(entry.SpawnPoint.position, m_gizmosSize);
                    Gizmos.DrawLine(entry.SpawnPoint.position, entry.SpawnPoint.position + Vector3.up * m_gizmosSize * 2f);
                }

                if (entry.LandingPoint != null)
                {
                    Gizmos.DrawWireSphere(entry.LandingPoint.position, m_gizmosSize);

                    if (entry.SpawnPoint != null)
                    {
                        Gizmos.DrawLine(entry.SpawnPoint.position, entry.LandingPoint.position);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!m_showGizmos)
            {
                return;
            }

            Gizmos.color = Color.yellow;

            foreach (BirdSpawnEntry entry in m_spawnPoints)
            {
                if (entry.SpawnPoint != null)
                {
                    Gizmos.DrawSphere(entry.SpawnPoint.position, m_gizmosSize * 0.5f);
                }

                if (entry.LandingPoint != null)
                {
                    Gizmos.DrawSphere(entry.LandingPoint.position, m_gizmosSize * 0.5f);
                }
            }
        }
    }
}

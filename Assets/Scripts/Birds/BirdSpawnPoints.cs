using Birdie.Debug;
using System.Collections.Generic;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Defines spawn points where birds can appear in the scene.
    /// Add this to a GameObject and configure the spawn locations.
    /// </summary>
    public class BirdSpawnPoints : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField]
        [Tooltip("List of transforms defining where birds can spawn")]
        private List<Transform> m_spawnPoints = new List<Transform>();

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
            Transform spawnPoint = GetRandomSpawnTransform();
            return spawnPoint != null ? spawnPoint.position : BirdsContainer.position;
        }

        /// <summary>
        /// Gets a random spawn point Transform, or null if none are defined.
        /// Use this when you need anchoredPosition for canvas-based spawning.
        /// </summary>
        public Transform GetRandomSpawnTransform()
        {
            if (m_spawnPoints.Count == 0)
            {
                DebugBase.LogWarning($"[{nameof(BirdSpawnPoints)}] No spawn points defined, using container position", DebugCategory.Birds);
                return null;
            }

            int randomIndex = Random.Range(0, m_spawnPoints.Count);
            Transform spawnPoint = m_spawnPoints[randomIndex];

            if (spawnPoint == null)
            {
                DebugBase.LogWarning($"[{nameof(BirdSpawnPoints)}] Spawn point at index {randomIndex} is null", DebugCategory.Birds);
                return null;
            }

            return spawnPoint;
        }

        /// <summary>
        /// Gets a specific spawn point by index.
        /// </summary>
        public Vector3 GetSpawnPosition(int index)
        {
            if (index < 0 || index >= m_spawnPoints.Count)
            {
                DebugBase.LogError($"[{nameof(BirdSpawnPoints)}] Invalid spawn point index: {index}", DebugCategory.Birds);
                return BirdsContainer.position;
            }

            Transform spawnPoint = m_spawnPoints[index];
            if (spawnPoint == null)
            {
                DebugBase.LogWarning($"[{nameof(BirdSpawnPoints)}] Spawn point at index {index} is null", DebugCategory.Birds);
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

            foreach (Transform spawnPoint in m_spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, m_gizmosSize);
                    Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + Vector3.up * m_gizmosSize * 2f);
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

            foreach (Transform spawnPoint in m_spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawSphere(spawnPoint.position, m_gizmosSize * 0.5f);
                }
            }
        }
    }
}

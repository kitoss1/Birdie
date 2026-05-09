using System;
using System.Collections.Generic;
using System.Threading;
using Birdie.Debug;
using Birdie.Environment;
using Birdie.Save;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Birdie.Managers
{
    /// <summary>
    /// Spawns trash items on the windowsill over time by picking a random prefab from the configured array.
    /// Uses the EnvironmentManager movement bounds for spawn X positions.
    /// Supports offline accumulation and save/load persistence.
    /// </summary>
    public class WindowsillManager : BaseManager
    {
        [Header("Trash Prefabs")]
        [SerializeField] private GameObject[] m_trashPrefabs;

        [Header("Spawn Settings")]
        [SerializeField] private Transform m_trashParent;
        [SerializeField] private Transform m_windowsillAnchor;
        [SerializeField] private int m_maxTrashCount = 5;
        [SerializeField] private float m_minSpawnInterval = 60f;
        [SerializeField] private float m_maxSpawnInterval = 180f;
        [SerializeField] private float m_yVariation = 0.5f;

        private readonly List<TrashItem> m_activeTrash = new List<TrashItem>();
        private CancellationTokenSource m_cts;

        public event Action<TrashItem> OnTrashSpawned;
        public event Action<TrashItem> OnTrashRemoved;

        public int ActiveTrashCount => m_activeTrash.Count;

        public override void Initialize(SaveManager saveManager = null)
        {
            base.Initialize(saveManager);
            if (m_saveManager != null)
            {
                LoadFromSaveData();
                StartSpawnLoop();
            }
            DebugBase.Log($"[{nameof(WindowsillManager)}] Initialized", DebugCategory.Managers);
        }

        private void StartSpawnLoop()
        {
            m_cts = new CancellationTokenSource();
            SpawnLoopAsync(m_cts.Token).Forget();
        }

        private async UniTaskVoid SpawnLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                float interval = Random.Range(m_minSpawnInterval, m_maxSpawnInterval);
                await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct);

                if (m_activeTrash.Count < m_maxTrashCount)
                {
                    SpawnRandomTrash();
                    SaveToSaveData();
                }
            }
        }

        private bool SpawnRandomTrash()
        {
            if (m_trashPrefabs == null || m_trashPrefabs.Length == 0)
            {
                DebugBase.LogWarning($"[{nameof(WindowsillManager)}] No trash prefabs assigned");
                return false;
            }

            int index = Random.Range(0, m_trashPrefabs.Length);
            float rotation = Random.Range(0f, 360f);

            if (!TryGetSpawnPosition(out Vector3 position))
            {
                return false;
            }

            SpawnTrash(index, position, rotation);
            return true;
        }

        private void SpawnTrash(int prefabIndex, Vector3 position, float rotation)
        {
            if (m_trashPrefabs == null || prefabIndex < 0 || prefabIndex >= m_trashPrefabs.Length)
            {
                DebugBase.LogError($"[{nameof(WindowsillManager)}] Invalid prefab index {prefabIndex}");
                return;
            }

            GameObject prefab = m_trashPrefabs[prefabIndex];

            if (prefab == null)
            {
                DebugBase.LogWarning($"[{nameof(WindowsillManager)}] Prefab at index {prefabIndex} is null");
                return;
            }

            GameObject go = Instantiate(prefab, position, Quaternion.identity, m_trashParent);
            TrashItem item = go.GetComponent<TrashItem>();

            if (item == null)
            {
                DebugBase.LogError($"[{nameof(WindowsillManager)}] Prefab at index {prefabIndex} is missing a {nameof(TrashItem)} component");
                Destroy(go);
                return;
            }

            item.Initialize(prefabIndex);
            item.transform.localEulerAngles = new Vector3(0f, 0f, rotation);
            item.OnRemoved += HandleTrashRemoved;
            m_activeTrash.Add(item);

            OnTrashSpawned?.Invoke(item);
            DebugBase.Log($"[{nameof(WindowsillManager)}] Spawned trash (prefab index {prefabIndex}) at {position}", DebugCategory.Managers);
        }

        private bool TryGetSpawnPosition(out Vector3 position)
        {
            if (m_windowsillAnchor == null)
            {
                DebugBase.LogError($"[{nameof(WindowsillManager)}] Windowsill anchor is not assigned");
                position = Vector3.zero;
                return false;
            }

            EnvironmentManager envManager = GameManager.Instance?.EnvironmentManager;

            if (envManager == null || !envManager.TryGetMovementBoundsWorldX(out float minX, out float maxX))
            {
                DebugBase.LogError($"[{nameof(WindowsillManager)}] Cannot get movement bounds for trash spawn — check EnvironmentManager bounds transforms");
                position = Vector3.zero;
                return false;
            }

            float y = m_windowsillAnchor.position.y + Random.Range(-m_yVariation, m_yVariation);
            position = new Vector3(Random.Range(minX, maxX), y, 0f);
            return true;
        }

        private void HandleTrashRemoved(TrashItem item)
        {
            item.OnRemoved -= HandleTrashRemoved;
            m_activeTrash.Remove(item);

            OnTrashRemoved?.Invoke(item);
            SaveToSaveData();

            DebugBase.Log($"[{nameof(WindowsillManager)}] Trash removed by player", DebugCategory.Managers);
        }

        private void LoadFromSaveData()
        {
            if (m_saveManager?.CurrentSaveData?.windowsill == null)
            {
                return;
            }

            WindowsillSaveData data = m_saveManager.CurrentSaveData.windowsill;

            foreach (TrashItemSaveEntry entry in data.activeTrash)
            {
                Vector3 position = new Vector3(entry.positionX, entry.positionY, 0f);
                SpawnTrash(entry.prefabIndex, position, entry.rotation);
            }

            if (data.lastSpawnTimestamp != 0)
            {
                AccumulateOfflineTrash(data.lastSpawnTimestamp);
            }

            DebugBase.Log($"[{nameof(WindowsillManager)}] Loaded {m_activeTrash.Count} trash items", DebugCategory.Managers);
        }

        private void AccumulateOfflineTrash(long lastSpawnTimestamp)
        {
            DateTime lastSpawn = DateTime.FromBinary(lastSpawnTimestamp);
            double elapsedSeconds = (DateTime.Now - lastSpawn).TotalSeconds;
            float avgInterval = (m_minSpawnInterval + m_maxSpawnInterval) * 0.5f;
            int offlineSpawns = Mathf.FloorToInt((float)(elapsedSeconds / avgInterval));

            int spawned = 0;
            for (int i = 0; i < offlineSpawns && m_activeTrash.Count < m_maxTrashCount; i++)
            {
                if (SpawnRandomTrash())
                {
                    spawned++;
                }
            }

            if (spawned > 0)
            {
                DebugBase.Log($"[{nameof(WindowsillManager)}] Spawned {spawned} trash items from offline accumulation ({elapsedSeconds:F0}s elapsed)", DebugCategory.Managers);
            }
        }

        private void SaveToSaveData()
        {
            if (m_saveManager?.CurrentSaveData?.windowsill == null)
            {
                return;
            }

            WindowsillSaveData data = m_saveManager.CurrentSaveData.windowsill;
            data.activeTrash.Clear();

            foreach (TrashItem item in m_activeTrash)
            {
                if (item == null)
                {
                    continue;
                }

                data.activeTrash.Add(new TrashItemSaveEntry
                {
                    prefabIndex = item.PrefabIndex,
                    positionX = item.transform.position.x,
                    positionY = item.transform.position.y,
                    rotation = item.transform.localEulerAngles.z,
                });
            }

            data.lastSpawnTimestamp = DateTime.Now.ToBinary();
            m_saveManager.SaveGame();
        }

        public void ClearAllTrash()
        {
            for (int i = m_activeTrash.Count - 1; i >= 0; i--)
            {
                TrashItem item = m_activeTrash[i];
                if (item == null)
                {
                    continue;
                }

                item.OnRemoved -= HandleTrashRemoved;
                Destroy(item.gameObject);
            }

            m_activeTrash.Clear();
            SaveToSaveData();

            DebugBase.Log($"[{nameof(WindowsillManager)}] All trash cleared", DebugCategory.Managers);
        }

        private void OnDestroy()
        {
            m_cts?.Cancel();
            m_cts?.Dispose();
        }
    }
}

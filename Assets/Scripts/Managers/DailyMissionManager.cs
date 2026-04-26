using System;
using System.Collections.Generic;
using Birdie.Birds;
using Birdie.Debug;
using Birdie.Environment;
using Birdie.Missions;
using Birdie.Save;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages daily missions: selects 3 missions per day from a ScriptableObject pool,
    /// tracks progress via game events, and handles reward claiming.
    /// </summary>
    public class DailyMissionManager : BaseManager
    {
        private const int DailyMissionCount = 3;
        private const string DateFormat = "yyyy-MM-dd";

        [SerializeField] private DailyMissionDefinition[] m_missionPool;

        private SaveManager m_saveManager;
        private DailyMissionDefinition[] m_activeMissions;
        private readonly HashSet<string> m_visitedBirdIDsToday = new HashSet<string>();

        public event Action<int> OnMissionProgressChanged;
        public event Action<int> OnMissionCompleted;
        public event Action OnDailyMissionsRefreshed;

        public IReadOnlyList<DailyMissionDefinition> ActiveMissions => m_activeMissions;

        public override void Initialize()
        {
            base.Initialize();

            if (m_missionPool == null || m_missionPool.Length < DailyMissionCount)
            {
                DebugBase.LogWarning($"[{nameof(DailyMissionManager)}] Mission pool has fewer than {DailyMissionCount} entries");
            }

            DebugBase.Log($"[{nameof(DailyMissionManager)}] Daily mission system initialized");
        }

        /// <summary>
        /// Sets the save manager reference, loads or generates today's missions, and subscribes to game events.
        /// </summary>
        public void SetSaveManager(SaveManager saveManager)
        {
            m_saveManager = saveManager;
            RefreshDailyMissions();
            SubscribeToEvents();
        }

        private void RefreshDailyMissions()
        {
            DailyMissionSaveData saveData = m_saveManager.CurrentSaveData.missions;
            string today = DateTime.Now.ToString(DateFormat);

            if (saveData.lastMissionDate == today && saveData.activeMissionIDs.Count == DailyMissionCount)
            {
                LoadExistingMissions(saveData);
            }
            else
            {
                GenerateNewDailyMissions(today);
            }
        }

        private void LoadExistingMissions(DailyMissionSaveData saveData)
        {
            m_activeMissions = new DailyMissionDefinition[DailyMissionCount];

            for (int i = 0; i < DailyMissionCount; i++)
            {
                m_activeMissions[i] = FindMissionByID(saveData.activeMissionIDs[i]);
            }

            m_visitedBirdIDsToday.Clear();
            foreach (string birdID in saveData.visitedBirdIDsToday)
            {
                m_visitedBirdIDsToday.Add(birdID);
            }

            DebugBase.Log($"[{nameof(DailyMissionManager)}] Loaded existing daily missions for {saveData.lastMissionDate}");
        }

        private void GenerateNewDailyMissions(string today)
        {
            DailyMissionSaveData saveData = m_saveManager.CurrentSaveData.missions;

            ResetSaveData(saveData, today);
            m_visitedBirdIDsToday.Clear();
            m_activeMissions = SelectDailyMissions(today);

            foreach (DailyMissionDefinition mission in m_activeMissions)
            {
                saveData.activeMissionIDs.Add(mission.MissionID);
                saveData.missionProgress.Add(0);
                saveData.missionClaimed.Add(false);
            }

            SaveToSaveData();
            OnDailyMissionsRefreshed?.Invoke();
            DebugBase.Log($"[{nameof(DailyMissionManager)}] Generated new daily missions for {today}");
        }

        private void ResetSaveData(DailyMissionSaveData saveData, string today)
        {
            saveData.lastMissionDate = today;
            saveData.activeMissionIDs.Clear();
            saveData.missionProgress.Clear();
            saveData.missionClaimed.Clear();
            saveData.visitedBirdIDsToday.Clear();
        }

        private DailyMissionDefinition[] SelectDailyMissions(string dateSeed)
        {
            System.Random rng = new System.Random(dateSeed.GetHashCode());
            List<DailyMissionDefinition> pool = new List<DailyMissionDefinition>(m_missionPool);
            DailyMissionDefinition[] selected = new DailyMissionDefinition[DailyMissionCount];

            for (int i = 0; i < DailyMissionCount && pool.Count > 0; i++)
            {
                int index = rng.Next(pool.Count);
                selected[i] = pool[index];
                pool.RemoveAt(index);
            }

            return selected;
        }

        private DailyMissionDefinition FindMissionByID(string missionID)
        {
            foreach (DailyMissionDefinition mission in m_missionPool)
            {
                if (mission != null && mission.MissionID == missionID)
                {
                    return mission;
                }
            }

            DebugBase.LogWarning($"[{nameof(DailyMissionManager)}] Mission not found in pool: {missionID}");
            return null;
        }

        private void SubscribeToEvents()
        {
            Bird.BirdLanded += OnBirdLanded;

            WindowsillManager windowsillManager = GameManager.Instance?.WindowsillManager;
            if (windowsillManager != null)
            {
                windowsillManager.OnTrashRemoved += OnTrashRemoved;
            }

            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.OnMinigameEnded += OnMinigameEnded;
            }
        }

        private void OnDestroy()
        {
            Bird.BirdLanded -= OnBirdLanded;

            WindowsillManager windowsillManager = GameManager.Instance?.WindowsillManager;
            if (windowsillManager != null)
            {
                windowsillManager.OnTrashRemoved -= OnTrashRemoved;
            }

            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.OnMinigameEnded -= OnMinigameEnded;
            }
        }

        private void OnBirdLanded(Bird bird)
        {
            if (!EnsureInitialized() || bird?.BirdData == null)
            {
                return;
            }

            string birdID = bird.BirdData.BirdID;

            if (m_visitedBirdIDsToday.Contains(birdID))
            {
                return;
            }

            m_visitedBirdIDsToday.Add(birdID);
            m_saveManager.CurrentSaveData.missions.visitedBirdIDsToday = new List<string>(m_visitedBirdIDsToday);

            IncrementProgressForType(MissionType.UniqueBirdsVisiting);
        }

        private void OnTrashRemoved(TrashItem _)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            IncrementProgressForType(MissionType.TrashCleaned);
        }

        private void OnMinigameEnded()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            IncrementProgressForType(MissionType.MinigamesPlayed);
        }

        private void IncrementProgressForType(MissionType missionType)
        {
            bool anyUpdated = false;

            for (int i = 0; i < m_activeMissions.Length; i++)
            {
                if (m_activeMissions[i]?.MissionType != missionType || IsMissionComplete(i))
                {
                    continue;
                }

                m_saveManager.CurrentSaveData.missions.missionProgress[i]++;
                anyUpdated = true;

                OnMissionProgressChanged?.Invoke(i);
                DebugBase.Log($"[{nameof(DailyMissionManager)}] Mission {i} ({missionType}): {GetProgress(i)}/{m_activeMissions[i].TargetCount}");

                if (IsMissionComplete(i))
                {
                    OnMissionCompleted?.Invoke(i);
                    DebugBase.Log($"[{nameof(DailyMissionManager)}] Mission {i} completed: {m_activeMissions[i].Description}");
                }
            }

            if (anyUpdated)
            {
                SaveToSaveData();
            }
        }

        /// <summary>
        /// Claims the golden seed reward for a completed mission. Returns false if ineligible.
        /// </summary>
        public bool ClaimReward(int missionIndex)
        {
            if (!EnsureInitialized())
            {
                return false;
            }

            if (!IsValidMissionIndex(missionIndex) || !IsMissionComplete(missionIndex) || IsMissionClaimed(missionIndex))
            {
                return false;
            }

            m_saveManager.CurrentSaveData.missions.missionClaimed[missionIndex] = true;

            int reward = m_activeMissions[missionIndex].GoldenSeedsReward;
            GameManager.Instance.EconomyManager.AddGoldenSeeds(reward);

            SaveToSaveData();
            DebugBase.Log($"[{nameof(DailyMissionManager)}] Reward claimed for mission {missionIndex}: {reward} golden seeds");
            return true;
        }

        /// <summary>
        /// Returns true when the mission's progress has reached the target count.
        /// </summary>
        public bool IsMissionComplete(int missionIndex)
        {
            if (!IsValidMissionIndex(missionIndex))
            {
                return false;
            }

            return GetProgress(missionIndex) >= m_activeMissions[missionIndex].TargetCount;
        }

        /// <summary>
        /// Returns true when the mission reward has already been collected.
        /// </summary>
        public bool IsMissionClaimed(int missionIndex)
        {
            DailyMissionSaveData saveData = m_saveManager?.CurrentSaveData?.missions;
            if (saveData == null || missionIndex >= saveData.missionClaimed.Count)
            {
                return false;
            }

            return saveData.missionClaimed[missionIndex];
        }

        /// <summary>
        /// Returns the current progress count for the given mission slot.
        /// </summary>
        public int GetProgress(int missionIndex)
        {
            DailyMissionSaveData saveData = m_saveManager?.CurrentSaveData?.missions;
            if (saveData == null || missionIndex >= saveData.missionProgress.Count)
            {
                return 0;
            }

            return saveData.missionProgress[missionIndex];
        }

        /// <summary>
        /// Returns the mission definition for the given slot, or null if the index is out of range.
        /// </summary>
        public DailyMissionDefinition GetMission(int missionIndex)
        {
            return IsValidMissionIndex(missionIndex) ? m_activeMissions[missionIndex] : null;
        }

        private bool IsValidMissionIndex(int missionIndex)
        {
            return m_activeMissions != null &&
                   missionIndex >= 0 &&
                   missionIndex < m_activeMissions.Length &&
                   m_activeMissions[missionIndex] != null;
        }

        private void SaveToSaveData()
        {
            m_saveManager?.SaveGame();
        }
    }
}

using System;
using System.Collections.Generic;
using Birdie.Birds;
using Birdie.Data;
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

        private DailyMissionDefinition[] m_activeMissions;
        private readonly HashSet<string> m_visitedBirdIDsToday = new HashSet<string>();

        // Parallel to m_activeMissions: resolved targets for specific-target mission types.
        private BirdData[] m_targetBirdData;
        private MinigameData[] m_targetMinigameData;

        public event Action<int> OnMissionProgressChanged;
        public event Action<int> OnMissionCompleted;
        public event Action OnDailyMissionsRefreshed;

        public IReadOnlyList<DailyMissionDefinition> ActiveMissions => m_activeMissions;

        public override void Initialize(SaveManager saveManager = null)
        {
            base.Initialize(saveManager);

            if (m_missionPool == null || m_missionPool.Length < DailyMissionCount)
            {
                DebugBase.LogWarning($"[{nameof(DailyMissionManager)}] Mission pool has fewer than {DailyMissionCount} entries");
            }

            if (m_saveManager != null)
            {
                RefreshDailyMissions();
                SubscribeToEvents();
            }

            DebugBase.Log($"[{nameof(DailyMissionManager)}] Daily mission system initialized");
        }

        private bool HasMissingTargets()
        {
            for (int i = 0; i < m_activeMissions.Length; i++)
            {
                MissionType type = m_activeMissions[i]?.MissionType ?? MissionType.UniqueBirdsVisiting;

                if (type == MissionType.SpecificBirdVisiting && m_targetBirdData?[i] == null)
                {
                    return true;
                }

                if (type == MissionType.SpecificMinigamePlayed && m_targetMinigameData?[i] == null)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshDailyMissions()
        {
            DailyMissionSaveData saveData = m_saveManager.CurrentSaveData.missions;
            string today = DateTime.Now.ToString(DateFormat);

            bool sameDayAndCount = saveData.lastMissionDate == today &&
                                   saveData.activeMissionIDs.Count == DailyMissionCount;
            bool targetIDsComplete = saveData.missionTargetIDs.Count == DailyMissionCount;

            if (sameDayAndCount && targetIDsComplete)
            {
                LoadExistingMissions(saveData);

                if (HasMissingTargets())
                {
                    DebugBase.Log($"[{nameof(DailyMissionManager)}] Detected missing targets after load, regenerating missions");
                    GenerateNewDailyMissions(today);
                }
            }
            else
            {
                GenerateNewDailyMissions(today);
            }
        }

        private void LoadExistingMissions(DailyMissionSaveData saveData)
        {
            m_activeMissions = new DailyMissionDefinition[DailyMissionCount];
            m_targetBirdData = new BirdData[DailyMissionCount];
            m_targetMinigameData = new MinigameData[DailyMissionCount];

            for (int i = 0; i < DailyMissionCount; i++)
            {
                m_activeMissions[i] = FindMissionByID(saveData.activeMissionIDs[i]);

                string targetID = i < saveData.missionTargetIDs.Count
                    ? saveData.missionTargetIDs[i]
                    : string.Empty;

                if (m_activeMissions[i]?.MissionType == MissionType.SpecificBirdVisiting)
                {
                    m_targetBirdData[i] = FindBirdInPool(m_activeMissions[i], targetID);
                }
                else if (m_activeMissions[i]?.MissionType == MissionType.SpecificMinigamePlayed)
                {
                    m_targetMinigameData[i] = FindMinigameInPool(m_activeMissions[i], targetID);
                }
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
            m_targetBirdData = new BirdData[DailyMissionCount];
            m_targetMinigameData = new MinigameData[DailyMissionCount];

            System.Random rng = new System.Random(today.GetHashCode() ^ 0x1F2E3D);

            for (int i = 0; i < m_activeMissions.Length; i++)
            {
                DailyMissionDefinition mission = m_activeMissions[i];
                saveData.activeMissionIDs.Add(mission.MissionID);
                saveData.missionProgress.Add(0);
                saveData.missionClaimed.Add(false);

                if (mission.MissionType == MissionType.SpecificBirdVisiting)
                {
                    BirdData picked = PickRandomBirdFromPool(mission, rng);
                    m_targetBirdData[i] = picked;
                    saveData.missionTargetIDs.Add(picked != null ? picked.BirdID : string.Empty);
                    DebugBase.Log($"[{nameof(DailyMissionManager)}] SpecificBirdVisiting target for slot {i}: {picked?.BirdName ?? "none"}");
                }
                else if (mission.MissionType == MissionType.SpecificMinigamePlayed)
                {
                    MinigameData picked = PickRandomMinigameFromPool(mission, rng);
                    m_targetMinigameData[i] = picked;
                    saveData.missionTargetIDs.Add(picked != null ? GetMinigameKey(picked) : string.Empty);
                    DebugBase.Log($"[{nameof(DailyMissionManager)}] SpecificMinigamePlayed target for slot {i}: {picked?.MinigameName ?? "none"}");
                }
                else
                {
                    saveData.missionTargetIDs.Add(string.Empty);
                }
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
            saveData.missionTargetIDs.Clear();
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

        private BirdData PickRandomBirdFromPool(DailyMissionDefinition mission, System.Random rng)
        {
            if (mission.BirdPool == null || mission.BirdPool.Length == 0)
            {
                DebugBase.LogWarning($"[{nameof(DailyMissionManager)}] SpecificBirdVisiting mission '{mission.MissionID}' has no bird pool assigned");
                return null;
            }

            int index = rng.Next(mission.BirdPool.Length);
            return mission.BirdPool[index];
        }

        private MinigameData PickRandomMinigameFromPool(DailyMissionDefinition mission, System.Random rng)
        {
            if (mission.MinigamePool == null || mission.MinigamePool.Length == 0)
            {
                DebugBase.LogWarning($"[{nameof(DailyMissionManager)}] SpecificMinigamePlayed mission '{mission.MissionID}' has no minigame pool assigned");
                return null;
            }

            int index = rng.Next(mission.MinigamePool.Length);
            return mission.MinigamePool[index];
        }

        private MinigameData FindMinigameInPool(DailyMissionDefinition mission, string key)
        {
            if (string.IsNullOrEmpty(key) || mission.MinigamePool == null)
            {
                return null;
            }

            foreach (MinigameData minigame in mission.MinigamePool)
            {
                if (minigame != null && GetMinigameKey(minigame) == key)
                {
                    return minigame;
                }
            }

            DebugBase.LogWarning($"[{nameof(DailyMissionManager)}] Target minigame '{key}' not found in pool for mission '{mission.MissionID}'");
            return null;
        }

        // Uses MinigameID if set, otherwise falls back to the asset name.
        private static string GetMinigameKey(MinigameData minigame)
        {
            return !string.IsNullOrEmpty(minigame.MinigameID) ? minigame.MinigameID : minigame.name;
        }

        private BirdData FindBirdInPool(DailyMissionDefinition mission, string birdID)
        {
            if (string.IsNullOrEmpty(birdID) || mission.BirdPool == null)
            {
                return null;
            }

            foreach (BirdData bird in mission.BirdPool)
            {
                if (bird != null && bird.BirdID == birdID)
                {
                    return bird;
                }
            }

            DebugBase.LogWarning($"[{nameof(DailyMissionManager)}] Target bird '{birdID}' not found in pool for mission '{mission.MissionID}'");
            return null;
        }

        /// <summary>
        /// Returns the display description for the given mission slot.
        /// For SpecificBirdVisiting, substitutes {0} in the template with the target bird name.
        /// </summary>
        public string GetMissionDescription(int missionIndex)
        {
            if (!IsValidMissionIndex(missionIndex))
            {
                return string.Empty;
            }

            DailyMissionDefinition mission = m_activeMissions[missionIndex];

            if (mission.MissionType == MissionType.SpecificBirdVisiting)
            {
                BirdData target = m_targetBirdData != null ? m_targetBirdData[missionIndex] : null;
                string birdName = target != null ? target.BirdName : "?";
                return $"{mission.Description} {birdName}";
            }

            if (mission.MissionType == MissionType.SpecificMinigamePlayed)
            {
                MinigameData target = m_targetMinigameData != null ? m_targetMinigameData[missionIndex] : null;
                string minigameName = target != null ? target.MinigameName : "?";
                return $"{mission.Description} {minigameName}";
            }

            return mission.Description;
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

            if (!m_visitedBirdIDsToday.Contains(birdID))
            {
                m_visitedBirdIDsToday.Add(birdID);
                m_saveManager.CurrentSaveData.missions.visitedBirdIDsToday = new List<string>(m_visitedBirdIDsToday);

                IncrementProgressForType(MissionType.UniqueBirdsVisiting);
            }

            HandleSpecificBirdMissions(birdID);
        }

        private void HandleSpecificBirdMissions(string birdID)
        {
            bool anyUpdated = false;

            for (int i = 0; i < m_activeMissions.Length; i++)
            {
                if (m_activeMissions[i]?.MissionType != MissionType.SpecificBirdVisiting || IsMissionComplete(i))
                {
                    continue;
                }

                BirdData target = m_targetBirdData != null ? m_targetBirdData[i] : null;
                if (target == null || target.BirdID != birdID)
                {
                    continue;
                }

                m_saveManager.CurrentSaveData.missions.missionProgress[i]++;
                anyUpdated = true;

                OnMissionProgressChanged?.Invoke(i);
                DebugBase.Log($"[{nameof(DailyMissionManager)}] SpecificBirdVisiting mission {i} completed: {target.BirdName} visited");

                if (IsMissionComplete(i))
                {
                    OnMissionCompleted?.Invoke(i);
                }
            }

            if (anyUpdated)
            {
                SaveToSaveData();
            }
        }

        private void OnTrashRemoved(TrashItem _)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            IncrementProgressForType(MissionType.TrashCleaned);
        }

        private void OnMinigameEnded(MinigameData minigameData)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            IncrementProgressForType(MissionType.MinigamesPlayed);
            HandleSpecificMinigameMissions(minigameData);
        }

        private void HandleSpecificMinigameMissions(MinigameData minigameData)
        {
            if (minigameData == null)
            {
                return;
            }

            bool anyUpdated = false;

            for (int i = 0; i < m_activeMissions.Length; i++)
            {
                if (m_activeMissions[i]?.MissionType != MissionType.SpecificMinigamePlayed || IsMissionComplete(i))
                {
                    continue;
                }

                MinigameData target = m_targetMinigameData != null ? m_targetMinigameData[i] : null;
                if (target == null || target.MinigameID != minigameData.MinigameID)
                {
                    continue;
                }

                m_saveManager.CurrentSaveData.missions.missionProgress[i]++;
                anyUpdated = true;

                OnMissionProgressChanged?.Invoke(i);
                DebugBase.Log($"[{nameof(DailyMissionManager)}] SpecificMinigamePlayed mission {i} completed: {target.MinigameName} played");

                if (IsMissionComplete(i))
                {
                    OnMissionCompleted?.Invoke(i);
                }
            }

            if (anyUpdated)
            {
                SaveToSaveData();
            }
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
            GameManager.Instance.EconomyManager?.AddGoldenSeeds(reward);

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

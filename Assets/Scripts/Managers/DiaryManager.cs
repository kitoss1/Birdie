using Birdie.Data;
using Birdie.Debug;
using Birdie.Save;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages the bird diary/collection system.
    /// Tracks which birds have been discovered and their discovery data.
    /// </summary>
    public class DiaryManager : BaseManager
    {
        private readonly HashSet<string> m_discoveredBirdIDs = new HashSet<string>();
        private readonly Dictionary<string, DateTime> m_discoveryDates = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, int> m_encounterCounts = new Dictionary<string, int>();

        public event Action<BirdData> OnBirdDiscovered;
        public event Action<BirdData> OnBirdEncountered;

        public override void Initialize(SaveManager saveManager = null)
        {
            base.Initialize(saveManager);
            if (m_saveManager != null)
                LoadFromSaveData();
            DebugBase.Log($"[{nameof(DiaryManager)}] Diary system initialized", DebugCategory.General);
        }

        /// <summary>
        /// Gets all birds sorted by ID for diary display.
        /// </summary>
        public List<BirdData> GetAllBirdsForDiary()
        {
            if (GameManager.Instance == null || GameManager.Instance.BirdManager == null)
            {
                DebugBase.LogError($"[{nameof(DiaryManager)}] GameManager or BirdManager is not available!", DebugCategory.General);
                return new List<BirdData>();
            }

            List<BirdData> sortedBirds = new List<BirdData>(GameManager.Instance.BirdManager.AvailableBirds);
            sortedBirds.Sort((a, b) => string.Compare(a.BirdID, b.BirdID, System.StringComparison.Ordinal));

            return sortedBirds;
        }

        /// <summary>
        /// Checks if a bird has been discovered before.
        /// </summary>
        public bool IsBirdDiscovered(string birdID)
        {
            return m_discoveredBirdIDs.Contains(birdID);
        }

        /// <summary>
        /// Checks if a bird has been discovered before using BirdData.
        /// </summary>
        public bool IsBirdDiscovered(BirdData birdData)
        {
            if (birdData == null)
            {
                return false;
            }

            return IsBirdDiscovered(birdData.BirdID);
        }

        /// <summary>
        /// Records a bird encounter. If it's the first time, adds it to the diary.
        /// Returns true if this was a new discovery.
        /// </summary>
        public bool RecordBirdEncounter(BirdData birdData)
        {
            if (birdData == null)
            {
                DebugBase.LogError($"[{nameof(DiaryManager)}] Cannot record encounter for null BirdData", DebugCategory.General);
                return false;
            }

            string birdID = birdData.BirdID;
            bool isNewDiscovery = false;

            if (!m_discoveredBirdIDs.Contains(birdID))
            {
                m_discoveredBirdIDs.Add(birdID);
                m_discoveryDates[birdID] = DateTime.Now;
                m_encounterCounts[birdID] = 1;
                isNewDiscovery = true;

                DebugBase.Log($"[{nameof(DiaryManager)}] New bird discovered: {birdData.BirdName} ({birdID})", DebugCategory.Birds);
                OnBirdDiscovered?.Invoke(birdData);
            }
            else
            {
                if (m_encounterCounts.ContainsKey(birdID))
                {
                    m_encounterCounts[birdID]++;
                }
                else
                {
                    m_encounterCounts[birdID] = 1;
                }

                DebugBase.Log($"[{nameof(DiaryManager)}] Bird encountered: {birdData.BirdName} (Total: {m_encounterCounts[birdID]})", DebugCategory.Birds);
                OnBirdEncountered?.Invoke(birdData);
            }

            SaveToSaveData();
            return isNewDiscovery;
        }

        /// <summary>
        /// Gets the number of times a bird has been encountered.
        /// </summary>
        public int GetEncounterCount(string birdID)
        {
            return m_encounterCounts.TryGetValue(birdID, out int count) ? count : 0;
        }

        /// <summary>
        /// Gets the number of times a bird has been encountered using BirdData.
        /// </summary>
        public int GetEncounterCount(BirdData birdData)
        {
            if (birdData == null)
            {
                return 0;
            }

            return GetEncounterCount(birdData.BirdID);
        }

        /// <summary>
        /// Gets the discovery date for a bird.
        /// </summary>
        public DateTime? GetDiscoveryDate(string birdID)
        {
            return m_discoveryDates.TryGetValue(birdID, out DateTime date) ? date : null;
        }

        /// <summary>
        /// Gets the total number of discovered bird species.
        /// </summary>
        public int GetDiscoveredBirdCount()
        {
            return m_discoveredBirdIDs.Count;
        }

        /// <summary>
        /// Gets all discovered bird IDs.
        /// </summary>
        public IReadOnlyCollection<string> GetDiscoveredBirdIDs()
        {
            return m_discoveredBirdIDs;
        }

        /// <summary>
        /// Loads diary data from the save manager.
        /// </summary>
        private void LoadFromSaveData()
        {
            if (m_saveManager == null || m_saveManager.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(DiaryManager)}] SaveManager or SaveData is null, cannot load", DebugCategory.General);
                return;
            }

            DiarySaveData diaryData = m_saveManager.CurrentSaveData.diary;

            m_discoveredBirdIDs.Clear();
            m_discoveryDates.Clear();
            m_encounterCounts.Clear();

            if (diaryData.discoveredBirdIDs != null)
            {
                foreach (string birdID in diaryData.discoveredBirdIDs)
                {
                    m_discoveredBirdIDs.Add(birdID);
                }
            }

            if (diaryData.encounterBirdIDs != null && diaryData.encounterCounts != null)
            {
                for (int i = 0; i < diaryData.encounterBirdIDs.Count && i < diaryData.encounterCounts.Count; i++)
                {
                    m_encounterCounts[diaryData.encounterBirdIDs[i]] = diaryData.encounterCounts[i];
                }
            }

            if (diaryData.discoveryDateBirdIDs != null && diaryData.discoveryDateTimestamps != null)
            {
                for (int i = 0; i < diaryData.discoveryDateBirdIDs.Count && i < diaryData.discoveryDateTimestamps.Count; i++)
                {
                    try
                    {
                        m_discoveryDates[diaryData.discoveryDateBirdIDs[i]] = DateTime.FromBinary(diaryData.discoveryDateTimestamps[i]);
                    }
                    catch (Exception e)
                    {
                        DebugBase.LogWarning($"[{nameof(DiaryManager)}] Failed to parse discovery date: {e.Message}", DebugCategory.General);
                    }
                }
            }

            DebugBase.Log($"[{nameof(DiaryManager)}] Loaded diary data: {m_discoveredBirdIDs.Count} birds discovered", DebugCategory.General);
        }

        /// <summary>
        /// Saves diary data to the save manager.
        /// </summary>
        private void SaveToSaveData()
        {
            if (m_saveManager == null || m_saveManager.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(DiaryManager)}] SaveManager or SaveData is null, cannot save", DebugCategory.General);
                return;
            }

            DiarySaveData diaryData = m_saveManager.CurrentSaveData.diary;

            diaryData.discoveredBirdIDs = new List<string>(m_discoveredBirdIDs);

            diaryData.encounterBirdIDs = new List<string>(m_encounterCounts.Keys);
            diaryData.encounterCounts = new List<int>(m_encounterCounts.Values);

            diaryData.discoveryDateBirdIDs = new List<string>(m_discoveryDates.Keys);
            diaryData.discoveryDateTimestamps = new List<long>();
            foreach (DateTime date in m_discoveryDates.Values)
            {
                diaryData.discoveryDateTimestamps.Add(date.ToBinary());
            }

            m_saveManager.SaveGame();
        }

        /// <summary>
        /// Clears all diary data (for testing or reset functionality).
        /// </summary>
        public void ClearDiaryData()
        {
            m_discoveredBirdIDs.Clear();
            m_discoveryDates.Clear();
            m_encounterCounts.Clear();

            SaveToSaveData();

            DebugBase.Log($"[{nameof(DiaryManager)}] Diary data cleared", DebugCategory.General);
        }
    }
}

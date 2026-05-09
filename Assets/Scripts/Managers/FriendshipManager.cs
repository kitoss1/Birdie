using System;
using System.Collections.Generic;
using Birdie.Data;
using Birdie.Debug;
using Birdie.Save;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages friendship data for all individual birds.
    /// Tracks friendship points, levels, and unlocked information per bird.
    /// </summary>
    public class FriendshipManager : BaseManager
    {
        private readonly Dictionary<string, int> m_birdFriendshipPoints = new Dictionary<string, int>();
        private readonly Dictionary<string, int> m_lastSeenFriendshipPoints = new Dictionary<string, int>();

        public event Action<string> OnFriendshipChanged;

        public override void Initialize(SaveManager saveManager = null)
        {
            base.Initialize(saveManager);
            if (m_saveManager != null)
                LoadFromSaveData();
            DebugBase.Log($"[{nameof(FriendshipManager)}] Friendship system initialized");
        }

        /// <summary>
        /// Adds friendship points to a specific bird
        /// </summary>
        public void AddFriendship(string birdID, int points)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            if (!m_birdFriendshipPoints.ContainsKey(birdID))
            {
                m_birdFriendshipPoints[birdID] = 0;
            }

            m_birdFriendshipPoints[birdID] += points;
            DebugBase.Log($"[{nameof(FriendshipManager)}] Added {points} friendship to {birdID}. Total: {m_birdFriendshipPoints[birdID]}");

            SaveToSaveData();
            OnFriendshipChanged?.Invoke(birdID);
        }

        /// <summary>
        /// Gets the friendship points for a specific bird
        /// </summary>
        public int GetFriendship(string birdID)
        {
            return m_birdFriendshipPoints.TryGetValue(birdID, out int points) ? points : 0;
        }

        /// <summary>
        /// Sets the friendship points for a specific bird to an exact value.
        /// Primarily used for debugging purposes.
        /// </summary>
        public void SetFriendship(string birdID, int points)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            m_birdFriendshipPoints[birdID] = Mathf.Max(0, points);
            DebugBase.Log($"[{nameof(FriendshipManager)}] Set friendship for {birdID} to {points}", DebugCategory.Friendship);

            SaveToSaveData();
        }

        /// <summary>
        /// Gets the friendship level for a specific bird based on thresholds in BirdData
        /// </summary>
        public int GetFriendshipLevel(string birdID, BirdData birdData)
        {
            int points = GetFriendship(birdID);
            return GetFriendshipLevelForPoints(birdData, points);
        }

        /// <summary>
        /// Computes the friendship level for an arbitrary point value.
        /// </summary>
        public int GetFriendshipLevelForPoints(BirdData birdData, int points)
        {
            for (int i = birdData.FriendshipLevelThresholds.Count - 1; i >= 0; i--)
            {
                if (points >= birdData.FriendshipLevelThresholds[i])
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// Loads friendship data from the save manager.
        /// </summary>
        private void LoadFromSaveData()
        {
            if (m_saveManager == null || m_saveManager.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(FriendshipManager)}] SaveManager or SaveData is null, cannot load", DebugCategory.Friendship);
                return;
            }

            FriendshipSaveData friendshipData = m_saveManager.CurrentSaveData.friendship;

            m_birdFriendshipPoints.Clear();
            m_lastSeenFriendshipPoints.Clear();

            if (friendshipData.birdIDs != null && friendshipData.friendshipPoints != null)
            {
                for (int i = 0; i < friendshipData.birdIDs.Count && i < friendshipData.friendshipPoints.Count; i++)
                {
                    m_birdFriendshipPoints[friendshipData.birdIDs[i]] = friendshipData.friendshipPoints[i];
                }
            }

            if (friendshipData.birdIDs != null && friendshipData.lastSeenFriendshipPoints != null)
            {
                for (int i = 0; i < friendshipData.birdIDs.Count && i < friendshipData.lastSeenFriendshipPoints.Count; i++)
                {
                    m_lastSeenFriendshipPoints[friendshipData.birdIDs[i]] = friendshipData.lastSeenFriendshipPoints[i];
                }
            }

            DebugBase.Log($"[{nameof(FriendshipManager)}] Loaded friendship data: {m_birdFriendshipPoints.Count} birds", DebugCategory.Friendship);
        }

        /// <summary>
        /// Saves friendship data to the save manager.
        /// </summary>
        private void SaveToSaveData()
        {
            if (m_saveManager == null || m_saveManager.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(FriendshipManager)}] SaveManager or SaveData is null, cannot save", DebugCategory.Friendship);
                return;
            }

            FriendshipSaveData friendshipData = m_saveManager.CurrentSaveData.friendship;

            friendshipData.birdIDs = new List<string>(m_birdFriendshipPoints.Keys);
            friendshipData.friendshipPoints = new List<int>(m_birdFriendshipPoints.Values);

            friendshipData.lastSeenFriendshipPoints = new List<int>();
            foreach (string id in friendshipData.birdIDs)
            {
                friendshipData.lastSeenFriendshipPoints.Add(
                    m_lastSeenFriendshipPoints.TryGetValue(id, out int p) ? p : 0);
            }

            m_saveManager.SaveGame();

            DebugBase.Log($"[{nameof(FriendshipManager)}] Saved friendship data: {m_birdFriendshipPoints.Count} birds", DebugCategory.Friendship);
        }

        public int GetLastSeenFriendship(string birdID)
        {
            return m_lastSeenFriendshipPoints.TryGetValue(birdID, out int points) ? points : 0;
        }

        public void UpdateLastSeenFriendship(string birdID, int points)
        {
            m_lastSeenFriendshipPoints[birdID] = points;
            SaveToSaveData();
        }

        /// <summary>
        /// Clears all friendship data (for testing or reset functionality).
        /// </summary>
        public void ClearFriendshipData()
        {
            m_birdFriendshipPoints.Clear();
            SaveToSaveData();

            DebugBase.Log($"[{nameof(FriendshipManager)}] Friendship data cleared", DebugCategory.Friendship);
        }
    }
}

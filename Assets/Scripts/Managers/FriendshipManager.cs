using System.Collections.Generic;
using Birdie.Data;
using Birdie.Debug;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages friendship data for all individual birds.
    /// Tracks friendship points, levels, and unlocked information per bird.
    /// </summary>
    public class FriendshipManager : BaseManager
    {
        private Dictionary<string, int> m_birdFriendshipPoints = new Dictionary<string, int>();

        public override void Initialize()
        {
            base.Initialize();
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
        }

        /// <summary>
        /// Gets the friendship points for a specific bird
        /// </summary>
        public int GetFriendship(string birdID)
        {
            return m_birdFriendshipPoints.ContainsKey(birdID) ? m_birdFriendshipPoints[birdID] : 0;
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
        }

        /// <summary>
        /// Gets the friendship level for a specific bird based on thresholds in BirdData
        /// </summary>
        public int GetFriendshipLevel(string birdID, BirdData birdData)
        {
            int points = GetFriendship(birdID);

            for (int i = birdData.FriendshipLevelThresholds.Count - 1; i >= 0; i--)
            {
                if (points >= birdData.FriendshipLevelThresholds[i])
                {
                    return i;
                }
            }

            return 0;
        }
    }
}

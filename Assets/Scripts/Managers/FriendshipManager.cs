using System.Collections.Generic;
using Birdie.Data;
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

        public override void Initialize(GameManager gameManager)
        {
            base.Initialize(gameManager);
            Debug.Log($"[{nameof(FriendshipManager)}] Friendship system initialized");
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
            Debug.Log($"[{nameof(FriendshipManager)}] Added {points} friendship to {birdID}. Total: {m_birdFriendshipPoints[birdID]}");
        }

        /// <summary>
        /// Gets the friendship points for a specific bird
        /// </summary>
        public int GetFriendship(string birdID)
        {
            return m_birdFriendshipPoints.ContainsKey(birdID) ? m_birdFriendshipPoints[birdID] : 0;
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

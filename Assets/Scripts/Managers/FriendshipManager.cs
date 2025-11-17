using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages friendship data for all individual birds.
/// Tracks friendship points, levels, and unlocked information per bird.
/// </summary>
public class FriendshipManager : BaseManager
{
    // Dictionary to store friendship data: key = birdID, value = friendship points
    private Dictionary<string, int> _birdFriendshipPoints = new Dictionary<string, int>();

    public override void Initialize(GameManager gameManager)
    {
        base.Initialize(gameManager);
        Debug.Log("[FriendshipManager] Friendship system initialized");
    }

    /// <summary>
    /// Adds friendship points to a specific bird
    /// </summary>
    public void AddFriendship(string birdID, int points)
    {
        if (!EnsureInitialized()) return;
        
        if (!_birdFriendshipPoints.ContainsKey(birdID))
        {
            _birdFriendshipPoints[birdID] = 0;
        }
        
        _birdFriendshipPoints[birdID] += points;
        Debug.Log($"[FriendshipManager] Added {points} friendship to {birdID}. Total: {_birdFriendshipPoints[birdID]}");
    }

    /// <summary>
    /// Gets the friendship points for a specific bird
    /// </summary>
    public int GetFriendship(string birdID)
    {
        return _birdFriendshipPoints.ContainsKey(birdID) ? _birdFriendshipPoints[birdID] : 0;
    }

    /// <summary>
    /// Gets the friendship level for a specific bird based on thresholds in BirdData
    /// </summary>
    public int GetFriendshipLevel(string birdID, BirdData birdData)
    {
        int points = GetFriendship(birdID);
        
        for (int i = birdData.friendshipLevelThresholds.Count - 1; i >= 0; i--)
        {
            if (points >= birdData.friendshipLevelThresholds[i])
            {
                return i;
            }
        }
        
        return 0;
    }
}

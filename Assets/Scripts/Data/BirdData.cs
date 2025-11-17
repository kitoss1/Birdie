using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that contains all static information for a bird species.
/// This data is used by the BirdManager to spawn and manage birds.
/// Based on the design document specifications.
/// </summary>
[CreateAssetMenu(fileName = "New Bird", menuName = "Idle Bird/Bird Data")]
public class BirdData : ScriptableObject
{
    [Header("Basic Information")]
    [Tooltip("Common name of the bird (e.g., 'Pit-roig')")]
    public string birdName;
    
    [Tooltip("Scientific name (e.g., 'Erithacus rubecula')")]
    public string scientificName;
    
    [Tooltip("Bird species ID for save system")]
    public string birdID;
    
    [TextArea(3, 5)]
    [Tooltip("Basic description (3 lines max)")]
    public string basicDescription;
    
    [Header("Visual Data")]
    [Tooltip("Prefab of the bird for spawning")]
    public GameObject birdPrefab;
    
    [Tooltip("Photo/sprite for the diary")]
    public Sprite birdPhoto;
    
    [Tooltip("Size category")]
    public BirdSize size;
    
    [Header("Rarity System")]
    [Tooltip("Rarity affects spawn probability and rewards")]
    public BirdRarity rarity = BirdRarity.Common;
    
    [Tooltip("Base weight for weighted spawn system (higher = more likely)")]
    [Range(1, 100)]
    public int baseSpawnWeight = 50;
    
    [Header("Time Availability")]
    [Tooltip("Time range when this bird can appear (24h format)")]
    public TimeRange appearanceTimeRange;
    
    [Tooltip("Can this bird appear at any time? (overrides time range)")]
    public bool appearsAnytime = false;
    
    [Header("Friendship System")]
    [Tooltip("Maximum friendship level achievable with this bird")]
    [Range(1, 10)]
    public int maxFriendshipLevel = 4;
    
    [Tooltip("Friendship points needed for each level")]
    public List<int> friendshipLevelThresholds = new List<int> { 0, 25, 75, 150 };
    
    [Header("Unlockable Information")]
    [Tooltip("Information revealed at each friendship level")]
    public List<FriendshipLevelInfo> friendshipUnlocks = new List<FriendshipLevelInfo>();
    
    [Header("Habitat and Diet")]
    [Tooltip("Preferred diet type")]
    public DietType dietType;
    
    [Tooltip("Natural habitat description")]
    [TextArea(2, 4)]
    public string habitat;
    
    [Tooltip("Habitat map/sprite (optional)")]
    public Sprite habitatMap;
    
    [Header("Identification")]
    [Tooltip("Extended information on how to identify this bird")]
    [TextArea(3, 6)]
    public string identificationInfo;
    
    [Tooltip("Is this species threatened/endangered?")]
    public bool isThreatened = false;
    
    [Tooltip("Conservation status")]
    public string conservationStatus;
    
    [Header("Audio")]
    [Tooltip("Bird song/call audio clip")]
    public AudioClip birdSong;
    
    [Header("Gifts and Rewards")]
    [Tooltip("Items this bird can leave as gifts at high friendship")]
    public List<GiftItem> possibleGifts = new List<GiftItem>();
    
    [Header("Requirements")]
    [Tooltip("Habitat upgrades required for this bird to appear")]
    public List<string> requiredUpgradeIDs = new List<string>();
    
    [Tooltip("Minimum habitat level required")]
    public int minimumHabitatLevel = 0;
    
    [Header("Special Behaviors")]
    [Tooltip("Does this bird have special animations? (e.g., woodpecker pecking)")]
    public bool hasSpecialAnimation = false;
    
    [Tooltip("Special animation description")]
    public string specialAnimationDescription;
    
    /// <summary>
    /// Gets the friendship points required to reach a specific level
    /// </summary>
    public int GetFriendshipRequirement(int level)
    {
        if (level < 0 || level >= friendshipLevelThresholds.Count)
            return int.MaxValue;
        return friendshipLevelThresholds[level];
    }
    
    /// <summary>
    /// Checks if the bird can appear at the current time
    /// </summary>
    public bool CanAppearAtTime(int currentHour)
    {
        if (appearsAnytime) return true;
        return appearanceTimeRange.IsTimeInRange(currentHour);
    }
    
    /// <summary>
    /// Gets the rarity multiplier for golden seed rewards
    /// </summary>
    public float GetRarityMultiplier()
    {
        return rarity switch
        {
            BirdRarity.Common => 1.0f,
            BirdRarity.Uncommon => 1.5f,
            BirdRarity.Rare => 2.5f,
            BirdRarity.VeryRare => 3.5f,
            BirdRarity.Legendary => 5.0f,
            _ => 1.0f
        };
    }
}

[Serializable]
public class TimeRange
{
    [Range(0, 23)]
    public int startHour = 8;
    
    [Range(0, 23)]
    public int endHour = 18;
    
    /// <summary>
    /// Checks if a given hour is within this time range
    /// Handles wrap-around (e.g., 22:00 to 4:00)
    /// </summary>
    public bool IsTimeInRange(int hour)
    {
        if (startHour <= endHour)
        {
            // Normal range (e.g., 8 to 18)
            return hour >= startHour && hour <= endHour;
        }
        else
        {
            // Wrap-around range (e.g., 22 to 4)
            return hour >= startHour || hour <= endHour;
        }
    }
}

[Serializable]
public class FriendshipLevelInfo
{
    public int level;
    
    [Tooltip("Title of the unlock (e.g., 'First Contact', 'Known', 'Friend')")]
    public string levelTitle;
    
    [TextArea(2, 4)]
    [Tooltip("Information or anecdote unlocked at this level")]
    public string unlockedInfo;
    
    [Tooltip("Does this level unlock gifts?")]
    public bool unlocksGifts = false;
}

[Serializable]
public class GiftItem
{
    public string itemName;
    public Sprite itemIcon;
    public string itemDescription;
}

public enum BirdRarity
{
    Common,      // x1.0 multiplier
    Uncommon,    // x1.5 multiplier
    Rare,        // x2.5 multiplier
    VeryRare,    // x3.5 multiplier
    Legendary    // x5.0 multiplier
}

public enum BirdSize
{
    VerySmall,  // Hummingbird
    Small,      // Sparrow
    Medium,     // Robin
    Large,      // Crow
    VeryLarge   // Eagle
}

public enum DietType
{
    Seeds,
    Insects,
    Nectar,
    Fruits,
    Mixed,
    Carnivorous
}

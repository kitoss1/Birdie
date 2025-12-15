using System;
using System.Collections.Generic;
using Birdie.Birds;
using UnityEngine;

namespace Birdie.Data
{
    /// <summary>
    /// ScriptableObject that contains all static information for a bird species.
    /// This data is used by the BirdManager to spawn and manage birds.
    /// Based on the design document specifications.
    /// </summary>
    [CreateAssetMenu(fileName = "New Bird", menuName = "Birdie/Bird Data")]
    public class BirdData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField]
        [Tooltip("Common name of the bird (e.g., 'Pit-roig')")]
        private string m_birdName;

        [SerializeField]
        [Tooltip("Scientific name (e.g., 'Erithacus rubecula')")]
        private string m_scientificName;

        [SerializeField]
        [Tooltip("Bird species ID for save system")]
        private string m_birdID;

        [SerializeField]
        [TextArea(3, 5)]
        [Tooltip("Basic description (3 lines max)")]
        private string m_basicDescription;

        [Header("Visual Data")]
        [SerializeField]
        [Tooltip("Prefab of the bird for spawning")]
        private GameObject m_birdPrefab;

        [SerializeField]
        [Tooltip("Photo/sprite for the diary")]
        private Sprite m_birdPhoto;

        [SerializeField]
        [Tooltip("Size category")]
        private BirdSize m_size;

        [Header("Rarity System")]
        [SerializeField]
        [Tooltip("Rarity affects spawn probability and rewards")]
        private BirdRarity m_rarity = BirdRarity.Common;

        [SerializeField]
        [Tooltip("Base weight for weighted spawn system (higher = more likely)")]
        [Range(1, 100)]
        private int m_baseSpawnWeight = 50;

        [Header("Time Availability")]
        [SerializeField]
        [Tooltip("Time range when this bird can appear (24h format)")]
        private TimeRange m_appearanceTimeRange;

        [SerializeField]
        [Tooltip("Can this bird appear at any time? (overrides time range)")]
        private bool m_appearsAnytime = false;

        [Header("Friendship System")]
        [SerializeField]
        [Tooltip("Maximum friendship level achievable with this bird")]
        [Range(1, 10)]
        private int m_maxFriendshipLevel = 4;

        [SerializeField]
        [Tooltip("Friendship points needed for each level")]
        private List<int> m_friendshipLevelThresholds = new List<int> { 0, 25, 75, 150 };

        [Header("Unlockable Information")]
        [SerializeField]
        [Tooltip("Information revealed at each friendship level")]
        private List<FriendshipLevelInfo> m_friendshipUnlocks = new List<FriendshipLevelInfo>();

        [Header("Habitat and Diet")]
        [SerializeField]
        [Tooltip("Preferred diet type")]
        private DietType m_dietType;

        [SerializeField]
        [Tooltip("Natural habitat description")]
        [TextArea(2, 4)]
        private string m_habitat;

        [SerializeField]
        [Tooltip("Habitat map/sprite (optional)")]
        private Sprite m_habitatMap;

        [Header("Identification")]
        [SerializeField]
        [Tooltip("Extended information on how to identify this bird")]
        [TextArea(3, 6)]
        private string m_identificationInfo;

        [SerializeField]
        [Tooltip("Is this species threatened/endangered?")]
        private bool m_isThreatened = false;

        [SerializeField]
        [Tooltip("Conservation status")]
        private string m_conservationStatus;

        [Header("Audio")]
        [SerializeField]
        [Tooltip("Bird song/call audio clip")]
        private AudioClip m_birdSong;

        [Header("Gifts and Rewards")]
        [SerializeField]
        [Tooltip("Items this bird can leave as gifts at high friendship")]
        private List<GiftItem> m_possibleGifts = new List<GiftItem>();

        [Header("Requirements")]
        [SerializeField]
        [Tooltip("Habitat upgrades required for this bird to appear")]
        private List<string> m_requiredUpgradeIDs = new List<string>();

        [SerializeField]
        [Tooltip("Minimum habitat level required")]
        private int m_minimumHabitatLevel = 0;

        [Header("Visit Behavior")]
        [SerializeField]
        [Tooltip("Minimum duration this bird stays during a visit (seconds)")]
        private float m_visitDurationMin = 60f;

        [SerializeField]
        [Tooltip("Maximum duration this bird stays during a visit (seconds)")]
        private float m_visitDurationMax = 180f;

        [SerializeField]
        [Tooltip("Bonus seconds added to visit duration per nearby object")]
        [Range(0, 30)]
        private float m_objectBonusSeconds = 5f;

        [SerializeField]
        [Tooltip("Movement speed when moving between objects (units per second)")]
        private float m_movementSpeed = 2f;

        [SerializeField]
        [Tooltip("List of possible behaviors this bird species can perform")]
        private List<BirdBehaviorState> m_possibleBehaviors = new List<BirdBehaviorState>();

        [Header("Special Behaviors")]
        [SerializeField]
        [Tooltip("Does this bird have special animations? (e.g., woodpecker pecking)")]
        private bool m_hasSpecialAnimation = false;

        [SerializeField]
        [Tooltip("Special animation description")]
        private string m_specialAnimationDescription;

        public string BirdName
        {
            get => m_birdName;
            set => m_birdName = value;
        }

        public string ScientificName
        {
            get => m_scientificName;
            set => m_scientificName = value;
        }

        public string BirdID
        {
            get => m_birdID;
            set => m_birdID = value;
        }

        public string BasicDescription
        {
            get => m_basicDescription;
            set => m_basicDescription = value;
        }

        public GameObject BirdPrefab
        {
            get => m_birdPrefab;
            set => m_birdPrefab = value;
        }

        public Sprite BirdPhoto
        {
            get => m_birdPhoto;
            set => m_birdPhoto = value;
        }

        public BirdSize Size
        {
            get => m_size;
            set => m_size = value;
        }

        public BirdRarity Rarity
        {
            get => m_rarity;
            set => m_rarity = value;
        }

        public int BaseSpawnWeight
        {
            get => m_baseSpawnWeight;
            set => m_baseSpawnWeight = value;
        }

        public TimeRange AppearanceTimeRange
        {
            get => m_appearanceTimeRange;
            set => m_appearanceTimeRange = value;
        }

        public bool AppearsAnytime
        {
            get => m_appearsAnytime;
            set => m_appearsAnytime = value;
        }

        public int MaxFriendshipLevel
        {
            get => m_maxFriendshipLevel;
            set => m_maxFriendshipLevel = value;
        }

        public List<int> FriendshipLevelThresholds
        {
            get => m_friendshipLevelThresholds;
            set => m_friendshipLevelThresholds = value;
        }

        public List<FriendshipLevelInfo> FriendshipUnlocks
        {
            get => m_friendshipUnlocks;
            set => m_friendshipUnlocks = value;
        }

        public DietType DietType
        {
            get => m_dietType;
            set => m_dietType = value;
        }

        public string Habitat
        {
            get => m_habitat;
            set => m_habitat = value;
        }

        public Sprite HabitatMap
        {
            get => m_habitatMap;
            set => m_habitatMap = value;
        }

        public string IdentificationInfo
        {
            get => m_identificationInfo;
            set => m_identificationInfo = value;
        }

        public bool IsThreatened
        {
            get => m_isThreatened;
            set => m_isThreatened = value;
        }

        public string ConservationStatus
        {
            get => m_conservationStatus;
            set => m_conservationStatus = value;
        }

        public AudioClip BirdSong
        {
            get => m_birdSong;
            set => m_birdSong = value;
        }

        public List<GiftItem> PossibleGifts
        {
            get => m_possibleGifts;
            set => m_possibleGifts = value;
        }

        public List<string> RequiredUpgradeIDs
        {
            get => m_requiredUpgradeIDs;
            set => m_requiredUpgradeIDs = value;
        }

        public int MinimumHabitatLevel
        {
            get => m_minimumHabitatLevel;
            set => m_minimumHabitatLevel = value;
        }

        public bool HasSpecialAnimation
        {
            get => m_hasSpecialAnimation;
            set => m_hasSpecialAnimation = value;
        }

        public string SpecialAnimationDescription
        {
            get => m_specialAnimationDescription;
            set => m_specialAnimationDescription = value;
        }

        public float VisitDurationMin
        {
            get => m_visitDurationMin;
            set => m_visitDurationMin = value;
        }

        public float VisitDurationMax
        {
            get => m_visitDurationMax;
            set => m_visitDurationMax = value;
        }

        public float MovementSpeed
        {
            get => m_movementSpeed;
            set => m_movementSpeed = value;
        }

        public float ObjectBonusSeconds
        {
            get => m_objectBonusSeconds;
            set => m_objectBonusSeconds = value;
        }

        public List<BirdBehaviorState> PossibleBehaviors
        {
            get => m_possibleBehaviors;
            set => m_possibleBehaviors = value;
        }

        /// <summary>
        /// Gets the friendship points required to reach a specific level
        /// </summary>
        public int GetFriendshipRequirement(int level)
        {
            if (level < 0 || level >= m_friendshipLevelThresholds.Count)
            {
                return int.MaxValue;
            }

            return m_friendshipLevelThresholds[level];
        }

        /// <summary>
        /// Checks if the bird can appear at the current time
        /// </summary>
        public bool CanAppearAtTime(int currentHour)
        {
            if (m_appearsAnytime)
            {
                return true;
            }

            return m_appearanceTimeRange.IsTimeInRange(currentHour);
        }

        /// <summary>
        /// Gets the rarity multiplier for golden seed rewards
        /// </summary>
        public float GetRarityMultiplier()
        {
            return m_rarity switch
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
}

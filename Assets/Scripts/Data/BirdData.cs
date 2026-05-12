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
    [Serializable]
    public struct DietIconEntry
    {
        [Tooltip("Icon sprite to display in the diary grid")]
        public Sprite icon;

        [Tooltip("Food name shown in the toast when the icon is clicked")]
        public string name;
    }

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
        [Tooltip("Friendship points needed for each level (index = level, last entry = max level)")]
        private List<int> m_friendshipLevelThresholds = new List<int> { 0, 25, 75, 150 };

        [Header("Diary Unlock Levels")]
        [SerializeField]
        [Tooltip("Friendship level required to show the full bird photo (below this level the photo is shown darkened)")]
        [Range(0, 10)]
        private int m_fullPhotoUnlockLevel = 1;

        [SerializeField]
        [Tooltip("Friendship level required to reveal the scientific name")]
        [Range(0, 10)]
        private int m_scientificNameUnlockLevel = 1;

        [SerializeField]
        [Tooltip("Friendship level required to reveal the visit hours")]
        [Range(0, 10)]
        private int m_visitHoursUnlockLevel = 1;

        [SerializeField]
        [Tooltip("Friendship level required to reveal the diet type")]
        [Range(0, 10)]
        private int m_dietUnlockLevel = 1;

        [SerializeField]
        [Tooltip("Friendship level required to reveal the description text")]
        [Range(0, 10)]
        private int m_descriptionUnlockLevel = 2;

        [SerializeField]
        [Tooltip("Friendship level required to reveal the habitat map")]
        [Range(0, 10)]
        private int m_habitatMapUnlockLevel = 3;

        [SerializeField]
        [Tooltip("Friendship level required to reveal the conservation danger icon")]
        [Range(0, 10)]
        private int m_peligroUnlockLevel = 1;

        [SerializeField]
        [Tooltip("Sprite shown as the conservation danger icon for this bird")]
        private Sprite m_peligroSprite;

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Description shown in the popup when clicking the peligro icon")]
        private string m_peligroDescription;

        [SerializeField]
        [Tooltip("Friendship level required to show the feather decoration on the front page")]
        [Range(0, 10)]
        private int m_featherUnlockLevel = 4;

        [SerializeField]
        [Tooltip("Feather sprite shown on the diary front page at max friendship")]
        private Sprite m_featherSprite;

        [Header("Habitat and Diet")]
        [SerializeField]
        [Tooltip("Preferred diet type (used for the feeding mechanic)")]
        private DietType m_dietType;

        [SerializeField]
        [Tooltip("Diet icons to display in the diary page grid")]
        private List<DietIconEntry> m_dietIcons = new List<DietIconEntry>();

        [SerializeField]
        [Tooltip("Natural habitat description")]
        [TextArea(2, 4)]
        private string m_habitat;

        [SerializeField]
        [Tooltip("Habitat map/sprite (optional)")]
        private Sprite m_habitatMap;

        [Header("Audio")]
        [SerializeField]
        [Tooltip("Audio clips for this bird's song. A random one is picked each time the singing animation triggers playback.")]
        private List<AudioClip> m_songParts = new List<AudioClip>();

        [Header("Requirements")]
        [SerializeField]
        [Tooltip("Habitat upgrades required for this bird to appear")]
        private List<string> m_requiredUpgradeIDs = new List<string>();

        [Header("Visit Behavior")]
        [SerializeField]
        [Tooltip("Behavior played when the bird first arrives. Handles the fly-in before the visit starts.")]
        private BirdBehaviorState m_arrivingBehavior;

        [SerializeField]
        [Tooltip("Behavior played when the bird leaves. Handles the fly-out to the spawn point.")]
        private BirdBehaviorState m_leavingBehavior;

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
        [Tooltip("Horizontal offset from the feeder interaction point. Adjust per bird size so the sprite aligns correctly.")]
        private float m_feederInteractionOffset = 0f;

        [Header("Walk Hop Settings")]
        [SerializeField]
        [Tooltip("Peak height of each hop while walking, in local units (pixels for canvas, world units otherwise). Set to 0 to disable hopping.")]
        [Range(0f, 100f)]
        private float m_walkHopHeight = 20f;

        [SerializeField]
        [Tooltip("Duration of a single hop arc in seconds.")]
        [Range(0.05f, 0.5f)]
        private float m_walkHopDuration = 0.2f;

        [SerializeField]
        [Tooltip("Minimum time between hops while walking, in seconds.")]
        [Range(0.1f, 5f)]
        private float m_walkHopIntervalMin = 0.4f;

        [SerializeField]
        [Tooltip("Maximum time between hops while walking, in seconds.")]
        [Range(0.1f, 8f)]
        private float m_walkHopIntervalMax = 1.5f;

        [SerializeField]
        [Tooltip("Behaviors this bird species can perform, each with a per-species weight")]
        private List<BirdBehaviorEntry> m_possibleBehaviors = new List<BirdBehaviorEntry>();

        [Header("Minigames")]
        [SerializeField]
        [Tooltip("Seconds before the play action is available again after one play. 0 = locked for the full visit.")]
        private float m_minigameCooldownDuration = 0f;

        [SerializeField]
        [Tooltip("List of minigames this bird species can play")]
        private List<MinigameData> m_availableMinigames = new List<MinigameData>();

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

        public int MaxFriendshipLevel =>
            m_friendshipLevelThresholds != null && m_friendshipLevelThresholds.Count > 0
                ? m_friendshipLevelThresholds.Count - 1
                : 0;

        public List<int> FriendshipLevelThresholds
        {
            get => m_friendshipLevelThresholds;
            set => m_friendshipLevelThresholds = value;
        }

        public int FullPhotoUnlockLevel => m_fullPhotoUnlockLevel;
        public int ScientificNameUnlockLevel => m_scientificNameUnlockLevel;
        public int VisitHoursUnlockLevel => m_visitHoursUnlockLevel;
        public int DietUnlockLevel => m_dietUnlockLevel;
        public int PeligroUnlockLevel => m_peligroUnlockLevel;
        public Sprite PeligroSprite => m_peligroSprite;
        public string PeligroDescription => m_peligroDescription;
        public int DescriptionUnlockLevel => m_descriptionUnlockLevel;
        public int HabitatMapUnlockLevel => m_habitatMapUnlockLevel;
        public int FeatherUnlockLevel => m_featherUnlockLevel;
        public Sprite FeatherSprite => m_featherSprite;

        public DietType DietType
        {
            get => m_dietType;
            set => m_dietType = value;
        }

        public List<DietIconEntry> DietIcons
        {
            get => m_dietIcons;
            set => m_dietIcons = value;
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

        /// <summary>
        /// Audio clips available for this bird's song.
        /// </summary>
        public List<AudioClip> SongParts
        {
            get => m_songParts;
            set => m_songParts = value;
        }

        public List<string> RequiredUpgradeIDs
        {
            get => m_requiredUpgradeIDs;
            set => m_requiredUpgradeIDs = value;
        }

        public BirdBehaviorState ArrivingBehavior => m_arrivingBehavior;

        public BirdBehaviorState LeavingBehavior => m_leavingBehavior;

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

        public float FeederInteractionOffset => m_feederInteractionOffset;

        public float WalkHopHeight => m_walkHopHeight;

        public float WalkHopDuration => m_walkHopDuration;

        public float WalkHopIntervalMin => m_walkHopIntervalMin;

        public float WalkHopIntervalMax => m_walkHopIntervalMax;

        public float ObjectBonusSeconds
        {
            get => m_objectBonusSeconds;
            set => m_objectBonusSeconds = value;
        }

        public List<BirdBehaviorEntry> PossibleBehaviors
        {
            get => m_possibleBehaviors;
            set => m_possibleBehaviors = value;
        }

        public float MinigameCooldownDuration => m_minigameCooldownDuration;

        public List<MinigameData> AvailableMinigames
        {
            get => m_availableMinigames;
            set => m_availableMinigames = value;
        }

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

    }
}

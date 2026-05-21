using Birdie.Data;
using UnityEngine;

namespace Birdie.Missions
{
    /// <summary>
    /// Defines a single daily mission — its type, goal, and reward.
    /// Create instances via Assets > Create > Birdie > Daily Mission.
    /// </summary>
    [CreateAssetMenu(fileName = "New Daily Mission", menuName = "Birdie/Daily Mission")]
    public class DailyMissionDefinition : ScriptableObject
    {
        [SerializeField] private string m_missionID;
        [SerializeField] private MissionType m_missionType;

        [Tooltip("For SpecificBirdVisiting/SpecificMinigamePlayed: the target name is appended automatically (e.g. 'Receive the visit of' → 'Receive the visit of Robin').")]
        [SerializeField] private string m_description;

        [SerializeField] private int m_targetCount = 1;
        [SerializeField] private int m_goldenSeedsReward = 10;

        [Tooltip("SpecificBirdVisiting only: pool of birds to randomly pick from each day.")]
        [SerializeField] private BirdData[] m_birdPool;

        [Tooltip("SpecificMinigamePlayed only: pool of minigames to randomly pick from each day.")]
        [SerializeField] private MinigameData[] m_minigamePool;

        public string MissionID => m_missionID;
        public MissionType MissionType => m_missionType;
        public string Description => m_description;
        public int TargetCount => m_targetCount;
        public int GoldenSeedsReward => m_goldenSeedsReward;
        public BirdData[] BirdPool => m_birdPool;
        public MinigameData[] MinigamePool => m_minigamePool;
    }
}

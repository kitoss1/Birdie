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
        [SerializeField] private string m_description;
        [SerializeField] private int m_targetCount = 1;
        [SerializeField] private int m_goldenSeedsReward = 10;

        public string MissionID => m_missionID;
        public MissionType MissionType => m_missionType;
        public string Description => m_description;
        public int TargetCount => m_targetCount;
        public int GoldenSeedsReward => m_goldenSeedsReward;
    }
}

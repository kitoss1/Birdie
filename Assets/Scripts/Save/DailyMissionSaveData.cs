using System;
using System.Collections.Generic;

namespace Birdie.Save
{
    /// <summary>
    /// Save data for the daily mission system.
    /// Tracks which missions are active today, progress, claimed state, and visited birds.
    /// </summary>
    [Serializable]
    public class DailyMissionSaveData
    {
        public string lastMissionDate = string.Empty;
        public List<string> activeMissionIDs = new List<string>();
        public List<int> missionProgress = new List<int>();
        public List<bool> missionClaimed = new List<bool>();
        public List<string> visitedBirdIDsToday = new List<string>();

        /// <summary>
        /// Validates the save data integrity.
        /// </summary>
        public bool IsValid()
        {
            return activeMissionIDs != null &&
                   missionProgress != null &&
                   missionClaimed != null &&
                   visitedBirdIDsToday != null;
        }
    }
}

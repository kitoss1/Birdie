using System;
using System.Collections.Generic;

namespace Birdie.Save
{
    /// <summary>
    /// Save data for the diary/collection system.
    /// Tracks discovered birds and encounter information.
    /// </summary>
    [Serializable]
    public class DiarySaveData
    {
        public List<string> discoveredBirdIDs = new List<string>();
        public List<string> encounterBirdIDs = new List<string>();
        public List<int> encounterCounts = new List<int>();
        public List<string> discoveryDateBirdIDs = new List<string>();
        public List<long> discoveryDateTimestamps = new List<long>();

        /// <summary>
        /// Creates an empty diary save data.
        /// </summary>
        public DiarySaveData()
        {
        }

        /// <summary>
        /// Validates the save data integrity.
        /// </summary>
        public bool IsValid()
        {
            return discoveredBirdIDs != null &&
                   encounterBirdIDs != null &&
                   encounterCounts != null &&
                   discoveryDateBirdIDs != null &&
                   discoveryDateTimestamps != null &&
                   encounterBirdIDs.Count == encounterCounts.Count &&
                   discoveryDateBirdIDs.Count == discoveryDateTimestamps.Count;
        }
    }
}

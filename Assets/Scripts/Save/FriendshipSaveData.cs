using System;
using System.Collections.Generic;

namespace Birdie.Save
{
    /// <summary>
    /// Save data for the friendship system.
    /// Tracks friendship levels and points with each bird species.
    /// </summary>
    [Serializable]
    public class FriendshipSaveData
    {
        public List<string> birdIDs = new List<string>();
        public List<int> friendshipPoints = new List<int>();
        public List<int> friendshipLevels = new List<int>();

        /// <summary>
        /// Creates an empty friendship save data.
        /// </summary>
        public FriendshipSaveData()
        {
        }

        /// <summary>
        /// Validates the save data integrity.
        /// </summary>
        public bool IsValid()
        {
            return birdIDs != null &&
                   friendshipPoints != null &&
                   friendshipLevels != null &&
                   birdIDs.Count == friendshipPoints.Count &&
                   birdIDs.Count == friendshipLevels.Count;
        }
    }
}

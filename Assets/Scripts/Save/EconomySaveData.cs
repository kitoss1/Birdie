using System;
using System.Collections.Generic;

namespace Birdie.Save
{
    /// <summary>
    /// Save data for the economy system.
    /// Tracks currency, purchases, and economic progression.
    /// </summary>
    [Serializable]
    public class EconomySaveData
    {
        public int regularSeeds = 0;
        public int goldenSeeds = 0;
        public List<string> purchasedUpgradeIDs = new List<string>();
        public List<string> ownedItemIDs = new List<string>();
        public int habitatLevel = 0;

        /// <summary>
        /// Creates an empty economy save data.
        /// </summary>
        public EconomySaveData()
        {
        }

        /// <summary>
        /// Validates the save data integrity.
        /// </summary>
        public bool IsValid()
        {
            return purchasedUpgradeIDs != null &&
                   ownedItemIDs != null &&
                   regularSeeds >= 0 &&
                   goldenSeeds >= 0 &&
                   habitatLevel >= 0;
        }
    }
}

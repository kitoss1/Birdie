using System;
using UnityEngine;

namespace Birdie.Data
{
    [Serializable]
    public sealed class SeedSortingDifficultySettings : MinigameDifficultySettings
    {
        [SerializeField]
        [Tooltip("Total number of seeds scattered on the floor")]
        [Min(2)]
        private int m_totalSeedCount = 12;

        [SerializeField]
        [Tooltip("How many of the total seeds are liked by the bird")]
        [Min(1)]
        private int m_likedSeedCount = 6;

        [SerializeField]
        [Tooltip("Reward tiers based on max errors allowed")]
        private MinigameErrorTier[] m_errorTiers;

        public int TotalSeedCount => m_totalSeedCount;
        public int LikedSeedCount => m_likedSeedCount;
        public MinigameErrorTier[] ErrorTiers => m_errorTiers;
    }
}

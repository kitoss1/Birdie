using System;
using UnityEngine;

namespace Birdie.Data
{
    [Serializable]
    public abstract class MinigameDifficultySettings
    {
        [SerializeField]
        [Tooltip("Per-difficulty reward tiers. If set, overrides the MinigameData reward tiers for this level.")]
        private MinigameRewardTier[] m_rewardTiers;

        public MinigameRewardTier[] RewardTiers => m_rewardTiers;
    }
}

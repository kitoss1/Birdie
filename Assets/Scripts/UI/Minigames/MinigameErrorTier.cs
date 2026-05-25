using System;
using UnityEngine;

namespace Birdie.Data
{
    /// <summary>
    /// Defines a reward tier based on the maximum number of errors allowed.
    /// Reusable across any minigame where scoring is error-based
    /// (perfect play = max score, each mistake reduces score).
    /// </summary>
    [Serializable]
    public sealed class MinigameErrorTier
    {
        [SerializeField]
        [Tooltip("Maximum number of errors allowed to earn this reward")]
        [Min(0)]
        private int m_maxErrors;

        [SerializeField]
        [Tooltip("Friendship points awarded when finishing within this error limit")]
        [Min(1)]
        private int m_friendshipReward;

        public int MaxErrors => m_maxErrors;

        public int FriendshipReward => m_friendshipReward;

        /// <summary>
        /// Converts an array of error tiers into score-based reward tiers.
        /// Each error tier's score threshold is computed as maxScore - maxErrors.
        /// </summary>
        /// <param name="errorTiers">Error-based tiers to convert.</param>
        /// <param name="maxScore">The maximum achievable score (e.g. total seed count).</param>
        /// <returns>Array of score-based reward tiers, or null if input is null or empty.</returns>
        public static MinigameRewardTier[] ToRewardTiers(MinigameErrorTier[] errorTiers, int maxScore)
        {
            if (errorTiers == null || errorTiers.Length == 0)
            {
                return null;
            }

            MinigameRewardTier[] rewardTiers = new MinigameRewardTier[errorTiers.Length];

            for (int i = 0; i < errorTiers.Length; i++)
            {
                int scoreThreshold = Mathf.Max(0, maxScore - errorTiers[i].MaxErrors);
                rewardTiers[i] = new MinigameRewardTier(scoreThreshold, errorTiers[i].FriendshipReward);
            }

            return rewardTiers;
        }
    }
}

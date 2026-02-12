using System;
using UnityEngine;

namespace Birdie.Data
{
    [Serializable]
    public class MinigameRewardTier
    {
        [SerializeField]
        [Tooltip("Minimum score required to earn this reward")]
        private int m_scoreThreshold;

        [SerializeField]
        [Tooltip("Friendship points awarded for reaching this threshold")]
        private int m_friendshipReward;

        public int ScoreThreshold => m_scoreThreshold;

        public int FriendshipReward => m_friendshipReward;

        public static int ResolveReward(MinigameRewardTier[] tiers, int score)
        {
            if (tiers == null)
            {
                return 0;
            }

            int reward = 0;

            foreach (MinigameRewardTier tier in tiers)
            {
                if (tier != null && score >= tier.ScoreThreshold && tier.FriendshipReward > reward)
                {
                    reward = tier.FriendshipReward;
                }
            }

            return reward;
        }
    }
}

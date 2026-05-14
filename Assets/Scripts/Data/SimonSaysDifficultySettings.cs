using System;
using UnityEngine;

namespace Birdie.Data
{
    [Serializable]
    public sealed class SimonSaysDifficultySettings : MinigameDifficultySettings
    {
        [SerializeField]
        [Tooltip("Delay before the first sequence plays after starting")]
        [Min(0f)]
        private float m_sequenceStartDelay = 1f;

        [SerializeField]
        [Tooltip("Gap between each button highlight in the sequence")]
        [Min(0f)]
        private float m_gapBetweenHighlights = 0.2f;

        [SerializeField]
        [Tooltip("Delay before the next round starts after a correct sequence")]
        [Min(0f)]
        private float m_nextRoundDelay = 0.8f;

        [SerializeField]
        [Tooltip("Maximum number of rounds before the game ends. Set to 0 to derive from reward tiers.")]
        [Min(0)]
        private int m_maxRounds = 0;

        [SerializeField]
        [Tooltip("Per-difficulty reward tiers. If set, overrides the MinigameData reward tiers for this level.")]
        private MinigameRewardTier[] m_rewardTiers;

        public float SequenceStartDelay => m_sequenceStartDelay;
        public float GapBetweenHighlights => m_gapBetweenHighlights;
        public float NextRoundDelay => m_nextRoundDelay;
        public int MaxRounds => m_maxRounds;
        public MinigameRewardTier[] RewardTiers => m_rewardTiers;
    }
}

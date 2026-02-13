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

        public float SequenceStartDelay => m_sequenceStartDelay;
        public float GapBetweenHighlights => m_gapBetweenHighlights;
        public float NextRoundDelay => m_nextRoundDelay;
    }
}

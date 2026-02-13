using System;
using UnityEngine;

namespace Birdie.Data
{
    [Serializable]
    public sealed class SlidingPuzzleDifficultySettings : MinigameDifficultySettings
    {
        [SerializeField]
        [Tooltip("Grid dimension (e.g. 3 for 3x3, 4 for 4x4)")]
        [Min(2)]
        private int m_gridSize = 3;

        [SerializeField]
        [Tooltip("Number of random moves used to shuffle the puzzle")]
        [Min(1)]
        private int m_shuffleMoves = 50;

        [SerializeField]
        [Tooltip("Base score before move penalty is applied")]
        [Min(1)]
        private int m_maxScore = 100;

        [SerializeField]
        [Tooltip("Duration of the tile slide animation in seconds")]
        [Min(0.01f)]
        private float m_slideDuration = 0.15f;

        public int GridSize => m_gridSize;

        public int ShuffleMoves => m_shuffleMoves;

        public int MaxScore => m_maxScore;

        public float SlideDuration => m_slideDuration;
    }
}

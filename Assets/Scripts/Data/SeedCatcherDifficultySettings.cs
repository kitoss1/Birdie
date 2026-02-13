using System;
using UnityEngine;

namespace Birdie.Data
{
    [Serializable]
    public sealed class SeedCatcherDifficultySettings : MinigameDifficultySettings
    {
        [SerializeField]
        [Tooltip("Game duration in seconds")]
        [Min(1f)]
        private float m_gameDuration = 30f;

        [SerializeField]
        [Tooltip("Spawn interval at the start of the game (easy)")]
        [Min(0.05f)]
        private float m_initialSpawnInterval = 1.0f;

        [SerializeField]
        [Tooltip("Spawn interval at the end of the game (hard)")]
        [Min(0.05f)]
        private float m_finalSpawnInterval = 0.3f;

        [SerializeField]
        [Tooltip("Seed fall speed at the start of the game (easy)")]
        [Min(1f)]
        private float m_initialFallSpeed = 200f;

        [SerializeField]
        [Tooltip("Seed fall speed at the end of the game (hard)")]
        [Min(1f)]
        private float m_finalFallSpeed = 500f;

        [SerializeField]
        [Tooltip("Chance to spawn a spike instead of a seed at the start (0-1)")]
        [Range(0f, 1f)]
        private float m_initialSpikeChance = 0.1f;

        [SerializeField]
        [Tooltip("Chance to spawn a spike instead of a seed at the end (0-1)")]
        [Range(0f, 1f)]
        private float m_finalSpikeChance = 0.4f;

        [SerializeField]
        [Tooltip("Number of lives the player starts with")]
        [Min(1)]
        private int m_initialLives = 3;

        public float GameDuration => m_gameDuration;
        public float InitialSpawnInterval => m_initialSpawnInterval;
        public float FinalSpawnInterval => m_finalSpawnInterval;
        public float InitialFallSpeed => m_initialFallSpeed;
        public float FinalFallSpeed => m_finalFallSpeed;
        public float InitialSpikeChance => m_initialSpikeChance;
        public float FinalSpikeChance => m_finalSpikeChance;
        public int InitialLives => m_initialLives;
    }
}

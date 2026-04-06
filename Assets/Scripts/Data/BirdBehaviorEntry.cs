using System;
using Birdie.Birds;
using UnityEngine;

namespace Birdie.Data
{
    /// <summary>
    /// Pairs a behavior with a per-species base weight.
    /// Stored in BirdData so different species can share the same behavior asset
    /// but perform it with different frequencies.
    /// </summary>
    [Serializable]
    public class BirdBehaviorEntry
    {
        [SerializeField]
        [Tooltip("The behavior this bird species can perform")]
        private BirdBehaviorState m_behavior;

        [SerializeField]
        [Tooltip("How likely this species is to choose this behavior (higher = more frequent)")]
        [Range(1, 100)]
        private int m_weight = 50;

        public BirdBehaviorState Behavior => m_behavior;
        public int Weight => m_weight;
    }
}

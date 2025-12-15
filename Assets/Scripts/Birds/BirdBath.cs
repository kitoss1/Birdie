using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Bird bath object where birds can bathe and clean themselves.
    /// Birds with bathing behavior will be attracted to this object.
    /// </summary>
    public class BirdBath : BirdObject
    {
        [Header("Bath Settings")]
        [SerializeField]
        [Tooltip("Does this bath have water available?")]
        private bool m_hasWater = true;

        [SerializeField]
        [Tooltip("Water level (0-1, affects attractiveness)")]
        [Range(0f, 1f)]
        private float m_waterLevel = 1f;

        [SerializeField]
        [Tooltip("Visual representation of water (for animations)")]
        private GameObject m_waterVisual;

        [SerializeField]
        [Tooltip("Particle system for splashing effects")]
        private ParticleSystem m_splashParticles;

        public bool HasWater => m_hasWater;
        public float WaterLevel => m_waterLevel;

        private void Awake()
        {
            // Initialize as bath type if not set in inspector
            if (string.IsNullOrEmpty(ObjectID))
            {
                DebugBase.Log($"[{nameof(BirdBath)}] Bird bath initialized at {transform.position}", DebugCategory.Birds);
            }
        }

        public override void OnBirdStartInteraction(Bird bird)
        {
            base.OnBirdStartInteraction(bird);

            DebugBase.Log($"[{nameof(BirdBath)}] {bird.BirdData?.BirdName} started bathing", DebugCategory.Birds);

            // Show water visual if available
            if (m_waterVisual != null)
            {
                m_waterVisual.SetActive(true);
            }

            // Start splash particles
            if (m_splashParticles != null)
            {
                m_splashParticles.Play();
            }

            // TODO: Play water ripple animation
        }

        public override void OnBirdEndInteraction(Bird bird)
        {
            base.OnBirdEndInteraction(bird);

            DebugBase.Log($"[{nameof(BirdBath)}] {bird.BirdData?.BirdName} finished bathing", DebugCategory.Birds);

            // Stop splash particles
            if (m_splashParticles != null)
            {
                m_splashParticles.Stop();
            }

            // TODO: Reduce water level slightly
            // m_waterLevel -= 0.1f;
        }

        public override bool CanBeUsedBy(Bird bird)
        {
            // Only usable if water is available
            if (!m_hasWater || m_waterLevel <= 0f)
            {
                return false;
            }

            return base.CanBeUsedBy(bird);
        }

        /// <summary>
        /// Refills the bird bath with fresh water.
        /// Called when player refills or when it rains.
        /// </summary>
        public void Refill()
        {
            m_hasWater = true;
            m_waterLevel = 1f;
            DebugBase.Log($"[{nameof(BirdBath)}] Bird bath refilled", DebugCategory.Birds);

            // TODO: Play refill animation/effects
        }

        /// <summary>
        /// Simulates water evaporation over time.
        /// </summary>
        public void ReduceWaterLevel(float amount)
        {
            m_waterLevel = Mathf.Max(0f, m_waterLevel - amount);
            
            if (m_waterLevel <= 0f)
            {
                m_hasWater = false;
            }
        }
    }
}

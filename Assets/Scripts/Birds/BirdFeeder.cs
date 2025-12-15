using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Bird feeder object that provides food for birds.
    /// Birds with eating behavior will be attracted to this object.
    /// </summary>
    public class BirdFeeder : BirdObject
    {
        [Header("Feeder Settings")]
        [SerializeField]
        [Tooltip("Type of food provided by this feeder")]
        private Data.DietType m_foodType = Data.DietType.Seeds;

        [SerializeField]
        [Tooltip("Does this feeder have food available?")]
        private bool m_hasFoodAvailable = true;

        [SerializeField]
        [Tooltip("Visual representation of food (for animations)")]
        private GameObject m_foodVisual;

        public Data.DietType FoodType => m_foodType;
        public bool HasFoodAvailable => m_hasFoodAvailable;

        private void Awake()
        {
            // Initialize as feeder type if not set in inspector
            if (string.IsNullOrEmpty(ObjectID))
            {
                // ObjectID will be set via inspector, but we log for debugging
                DebugBase.Log($"[{nameof(BirdFeeder)}] Feeder initialized at {transform.position}", DebugCategory.Birds);
            }
        }

        public override void OnBirdStartInteraction(Bird bird)
        {
            base.OnBirdStartInteraction(bird);

            DebugBase.Log($"[{nameof(BirdFeeder)}] {bird.BirdData?.BirdName} started eating from feeder", DebugCategory.Birds);

            // Show food visual if available
            if (m_foodVisual != null)
            {
                m_foodVisual.SetActive(true);
            }

            // TODO: Play feeder animation (seeds visible, etc.)
            // TODO: Spawn particle effects (seeds falling)
        }

        public override void OnBirdEndInteraction(Bird bird)
        {
            base.OnBirdEndInteraction(bird);

            DebugBase.Log($"[{nameof(BirdFeeder)}] {bird.BirdData?.BirdName} finished eating from feeder", DebugCategory.Birds);

            // Hide food visual
            if (m_foodVisual != null)
            {
                m_foodVisual.SetActive(false);
            }

            // TODO: Reduce food amount
            // TODO: Check if feeder is empty
        }

        public override bool CanBeUsedBy(Bird bird)
        {
            // Only usable if food is available
            if (!m_hasFoodAvailable)
            {
                return false;
            }

            // TODO: Check if bird's diet matches feeder food type
            // For now, all birds can use any feeder
            return base.CanBeUsedBy(bird);
        }

        /// <summary>
        /// Refills the feeder with food.
        /// Called when player upgrades or refills the feeder.
        /// </summary>
        public void Refill()
        {
            m_hasFoodAvailable = true;
            DebugBase.Log($"[{nameof(BirdFeeder)}] Feeder refilled", DebugCategory.Birds);

            // TODO: Play refill animation/effects
        }
    }
}

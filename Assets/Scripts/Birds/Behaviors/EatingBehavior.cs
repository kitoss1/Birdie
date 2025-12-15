using Birdie.Debug;
using Birdie.Managers;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Eating behavior where the bird moves to a feeder and eats.
    /// Requires a BirdObject of type Feeder to be in the scene.
    /// Uses EnvironmentManager to find feeders.
    /// </summary>
    [CreateAssetMenu(fileName = "EatingBehavior", menuName = "Birdie/Bird Behaviors/Eating Behavior")]
    public class EatingBehavior : BirdBehaviorState
    {
        private BirdObject m_targetFeeder;
        private bool m_hasReachedFeeder = false;

        public override void OnEnter(Bird bird)
        {
            DebugBase.Log($"[{nameof(EatingBehavior)}] {bird.BirdData?.BirdName} looking for food", DebugCategory.Birds);

            // Find nearest feeder
            m_targetFeeder = FindNearestFeeder(bird);
            m_hasReachedFeeder = false;

            if (m_targetFeeder != null)
            {
                DebugBase.Log($"[{nameof(EatingBehavior)}] Found feeder at {m_targetFeeder.InteractionPosition}", DebugCategory.Birds);
                m_targetFeeder.OnBirdStartInteraction(bird);
            }
            else
            {
                DebugBase.LogWarning($"[{nameof(EatingBehavior)}] No feeder found! Bird cannot eat.", DebugCategory.Birds);
            }
        }

        public override void Execute(Bird bird)
        {
            if (m_targetFeeder == null)
            {
                return;
            }

            // Move toward feeder if not reached yet
            if (!m_hasReachedFeeder)
            {
                Vector3 targetPosition = m_targetFeeder.InteractionPosition;
                float moveSpeed = bird.BirdData?.MovementSpeed ?? 60f;
                bird.transform.position = Vector3.MoveTowards(
                    bird.transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );

                // Check if reached
                if (Vector3.Distance(bird.transform.position, targetPosition) < 0.1f)
                {
                    m_hasReachedFeeder = true;
                    DebugBase.Log($"[{nameof(EatingBehavior)}] Reached feeder, starting to eat", DebugCategory.Birds);
                    
                    // TODO: Play eating animation
                    // Example: bird.SpineSkeleton.AnimationState.SetAnimation(0, "peck", true);
                }
            }
            else
            {
                // Bird is eating
                // TODO: Spawn particle effects (seeds, crumbs, etc.)
            }
        }

        public override void OnExit(Bird bird)
        {
            DebugBase.Log($"[{nameof(EatingBehavior)}] {bird.BirdData?.BirdName} finished eating", DebugCategory.Birds);

            if (m_targetFeeder != null)
            {
                m_targetFeeder.OnBirdEndInteraction(bird);
            }

            m_targetFeeder = null;
            m_hasReachedFeeder = false;
        }

        public override bool CanExecute(Bird bird)
        {
            // Check base conditions (friendship level, etc.)
            if (!base.CanExecute(bird))
            {
                return false;
            }

            // Must have a feeder in the scene
            BirdObject feeder = FindNearestFeeder(bird);
            return feeder != null;
        }

        public override int CalculateWeight(Bird bird)
        {
            int weight = base.CalculateWeight(bird);

            // Find feeder and boost weight based on attractiveness
            BirdObject feeder = FindNearestFeeder(bird);
            if (feeder != null)
            {
                weight += feeder.Attractiveness;
            }

            return weight;
        }

        /// <summary>
        /// Finds the nearest feeder using EnvironmentManager.
        /// </summary>
        private BirdObject FindNearestFeeder(Bird bird)
        {
            if (GameManager.Instance?.EnvironmentManager == null)
            {
                DebugBase.LogWarning($"[{nameof(EatingBehavior)}] EnvironmentManager not found!", DebugCategory.Birds);
                return null;
            }

            // Use EnvironmentManager to find nearest usable feeder
            return GameManager.Instance.EnvironmentManager.GetNearestUsableObject(BirdObjectType.Feeder, bird);
        }
    }
}

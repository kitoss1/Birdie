using Birdie.Debug;
using Birdie.Managers;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Bathing behavior where the bird moves to a bird bath and bathes.
    /// Requires a BirdObject of type BirdBath to be in the scene.
    /// Uses EnvironmentManager to find bird baths.
    /// </summary>
    [CreateAssetMenu(fileName = "BathingBehavior", menuName = "Birdie/Bird Behaviors/Bathing Behavior")]
    public class BathingBehavior : BirdBehaviorState
    {
        private BirdObject m_targetBath;
        private bool m_hasReachedBath = false;

        public override void OnEnter(Bird bird)
        {
            DebugBase.Log($"[{nameof(BathingBehavior)}] {bird.BirdData?.BirdName} looking for bird bath", DebugCategory.Birds);

            // Find nearest bird bath
            m_targetBath = FindNearestBirdBath(bird);
            m_hasReachedBath = false;

            if (m_targetBath != null)
            {
                DebugBase.Log($"[{nameof(BathingBehavior)}] Found bird bath at {m_targetBath.InteractionPosition}", DebugCategory.Birds);
                m_targetBath.OnBirdStartInteraction(bird);

                // Play movement animation while walking to bath
                if (!string.IsNullOrEmpty(MovementAnimationStateName))
                {
                    bird.PlayAnimation(MovementAnimationStateName);
                }
            }
            else
            {
                base.OnEnter(bird);
                DebugBase.LogWarning($"[{nameof(BathingBehavior)}] No bird bath found! Bird cannot bathe.", DebugCategory.Birds);
            }
        }

        public override void Execute(Bird bird)
        {
            if (m_targetBath == null)
            {
                return;
            }

            // Move toward bird bath if not reached yet
            if (!m_hasReachedBath)
            {
                float moveSpeed = bird.BirdData?.MovementSpeed ?? 60f;
                bool reached = MoveTowardsTarget(bird, m_targetBath, moveSpeed);

                if (reached)
                {
                    m_hasReachedBath = true;
                    DebugBase.Log($"[{nameof(BathingBehavior)}] Reached bird bath, starting to bathe", DebugCategory.Birds);

                    if (!string.IsNullOrEmpty(AnimationStateName))
                    {
                        bird.PlayAnimation(AnimationStateName);
                    }
                }
            }
            else
            {
                // Bird is bathing
                // TODO: Spawn water splash particle effects
                // The BirdBath component handles splash particles, but we could add more here
            }
        }

        public override void OnExit(Bird bird)
        {
            DebugBase.Log($"[{nameof(BathingBehavior)}] {bird.BirdData?.BirdName} finished bathing", DebugCategory.Birds);

            if (m_targetBath != null)
            {
                m_targetBath.OnBirdEndInteraction(bird);
            }

            m_targetBath = null;
            m_hasReachedBath = false;

            base.OnExit(bird);
        }

        public override bool IsTimerActive(Bird bird)
        {
            return m_hasReachedBath;
        }

        public override bool CanExecute(Bird bird)
        {
            // Check base conditions (friendship level, etc.)
            if (!base.CanExecute(bird))
            {
                return false;
            }

            // Must have a bird bath in the scene
            BirdObject bath = FindNearestBirdBath(bird);
            return bath != null;
        }


        /// <summary>
        /// Finds the nearest bird bath using EnvironmentManager.
        /// </summary>
        private BirdObject FindNearestBirdBath(Bird bird)
        {
            if (GameManager.Instance?.EnvironmentManager == null)
            {
                DebugBase.LogWarning($"[{nameof(BathingBehavior)}] EnvironmentManager not found!", DebugCategory.Birds);
                return null;
            }

            // Use EnvironmentManager to find nearest usable bird bath
            return GameManager.Instance.EnvironmentManager.GetNearestUsableObject(BirdObjectType.BirdBath, bird);
        }
    }
}

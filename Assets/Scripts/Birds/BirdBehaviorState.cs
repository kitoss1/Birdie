using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Base class for all bird behavior states.
    /// Behaviors are ScriptableObject assets that can be reused across different bird species.
    /// Each behavior defines what a bird does during that state (idle, singing, eating, etc.).
    /// </summary>
    public abstract class BirdBehaviorState : ScriptableObject
    {
        [Header("Animation")]
        [SerializeField]
        [Tooltip("Name of the Animator state to crossfade into when this behavior starts")]
        private string m_animationStateName;

        [SerializeField]
        [Tooltip("Animation to play while the bird is moving to a target (leave empty if behavior has no movement)")]
        private string m_movementAnimationStateName;

        [Header("Behavior Settings")]
        [SerializeField]
        [Tooltip("Minimum duration this behavior lasts (seconds)")]
        private float m_minDuration = 1f;

        [SerializeField]
        [Tooltip("Maximum duration this behavior lasts (seconds)")]
        private float m_maxDuration = 5f;

        [Header("Unlock Requirements")]
        [SerializeField]
        [Tooltip("Minimum friendship level required to perform this behavior (0 = always available)")]
        private int m_requiredFriendshipLevel = 0;

        [SerializeField]
        [Tooltip("Does this behavior require specific objects in the scene? (e.g., feeder for eating)")]
        private bool m_requiresSceneObject = false;

        [Header("Behavior Properties")]
        [SerializeField]
        [Tooltip("Can this behavior be interrupted by player actions?")]
        private bool m_canBeInterrupted = true;

        [SerializeField]
        [Tooltip("Base weight for behavior selection (higher = more likely to be chosen)")]
        [Range(1, 100)]
        private int m_baseWeight = 50;

        [SerializeField]
        [Tooltip("Friendship points gained when completing this behavior")]
        private int m_friendshipReward = 0;

        // Properties
        public string AnimationStateName => m_animationStateName;
        public string MovementAnimationStateName => m_movementAnimationStateName;
        public float MinDuration => m_minDuration;
        public float MaxDuration => m_maxDuration;
        public int RequiredFriendshipLevel => m_requiredFriendshipLevel;
        public bool RequiresSceneObject => m_requiresSceneObject;
        public bool CanBeInterrupted => m_canBeInterrupted;
        public int BaseWeight => m_baseWeight;
        public int FriendshipReward => m_friendshipReward;

        /// <summary>
        /// Called when the bird enters this behavior state.
        /// Base implementation crossfades to the configured animation state.
        /// Override to add custom initialization (audio, movement, etc.) and call base.OnEnter(bird).
        /// </summary>
        /// <param name="bird">The bird controller executing this behavior</param>
        public virtual void OnEnter(Bird bird)
        {
            if (!string.IsNullOrEmpty(m_animationStateName))
            {
                bird.PlayAnimation(m_animationStateName);
            }
            else
            {
                DebugBase.LogWarning($"[{nameof(BirdBehaviorState)}] No animation state configured for {name}", DebugCategory.Birds);
            }
        }

        /// <summary>
        /// Called every frame while the bird is in this behavior state.
        /// Use this for continuous logic, animations, or checking conditions.
        /// </summary>
        /// <param name="bird">The bird controller executing this behavior</param>
        public abstract void Execute(Bird bird);

        /// <summary>
        /// Called when the bird exits this behavior state.
        /// Override to add cleanup logic (stop audio, reset state, etc.).
        /// </summary>
        /// <param name="bird">The bird controller executing this behavior</param>
        public virtual void OnExit(Bird bird)
        {
            bird.ResetWalkHop();
        }

        /// <summary>
        /// Checks if this behavior can be executed given the current conditions.
        /// Override this to add custom conditions (e.g., check for specific objects).
        /// </summary>
        /// <param name="bird">The bird controller</param>
        /// <returns>True if the behavior can be executed</returns>
        public virtual bool CanExecute(Bird bird)
        {
            // Check friendship level requirement
            int currentFriendshipLevel = GetBirdFriendshipLevel(bird);
            if (currentFriendshipLevel < m_requiredFriendshipLevel)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates the final weight for this behavior based on environmental conditions.
        /// Override this to add modifiers (e.g., increase weight if food is nearby).
        /// </summary>
        /// <param name="bird">The bird controller</param>
        /// <returns>The calculated weight</returns>
        public virtual int CalculateWeight(Bird bird)
        {
            return m_baseWeight;
        }

        /// <summary>
        /// Moves the bird toward a BirdObject's interaction point at the given speed.
        /// Uses anchoredPosition when both the bird and target are RectTransforms (canvas UI),
        /// otherwise falls back to world-space movement.
        /// Returns true when the bird has reached the target.
        /// </summary>
        protected bool MoveTowardsTarget(Bird bird, BirdObject target, float speed)
        {
            RectTransform birdRect = bird.transform as RectTransform;

            bool reached;

            if (birdRect != null && birdRect.parent != null)
            {
                // Convert target world position into bird's parent local space so the
                // comparison is valid regardless of where each object sits in the hierarchy.
                Vector2 targetLocal = birdRect.parent.InverseTransformPoint(target.InteractionPosition);
                Vector2 birdLocal = birdRect.localPosition;

                float directionX = targetLocal.x - birdLocal.x;
                bird.SetFacingDirection(directionX);

                birdRect.localPosition = Vector2.MoveTowards(birdLocal, targetLocal, speed * Time.deltaTime);
                reached = Vector2.Distance(birdRect.localPosition, targetLocal) < 1f;
            }
            else
            {
                Vector3 targetPosition = target.InteractionPosition;
                float worldDirectionX = targetPosition.x - bird.transform.position.x;
                bird.SetFacingDirection(worldDirectionX);

                bird.transform.position = Vector3.MoveTowards(
                    bird.transform.position,
                    targetPosition,
                    speed * Time.deltaTime
                );
                reached = Vector3.Distance(bird.transform.position, targetPosition) < 0.1f;
            }

            if (reached)
            {
                bird.ResetWalkHop();
            }
            else
            {
                bird.SampleAndApplyWalkHop(Time.deltaTime);
            }

            return reached;
        }

        /// <summary>
        /// Helper method to get the current friendship level of the bird.
        /// </summary>
        protected int GetBirdFriendshipLevel(Bird bird)
        {
            if (bird?.BirdData == null)
            {
                return 0;
            }

            // TODO: Get actual friendship level from FriendshipManager
            // For now, return 0 as placeholder
            return 0;
        }
    }
}

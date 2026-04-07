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
        [Tooltip("When disabled the behavior runs until IsBehaviorComplete returns true, ignoring the duration fields below")]
        private bool m_useTimer = true;

        [SerializeField]
        [Tooltip("Minimum duration this behavior lasts (seconds). Only used when Use Timer is enabled.")]
        private float m_minDuration = 1f;

        [SerializeField]
        [Tooltip("Maximum duration this behavior lasts (seconds). Only used when Use Timer is enabled.")]
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
        [Tooltip("Seconds before this behavior can be selected again after it finishes (0 = no cooldown)")]
        private float m_cooldownDuration = 0f;

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
        public float CooldownDuration => m_cooldownDuration;
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

        [SerializeField]
        [Tooltip("If set, this behavior is always followed by the specified behavior instead of a random pick. Leave empty for normal weighted selection.")]
        private BirdBehaviorState m_forcedNextBehavior;

        public BirdBehaviorState ForcedNextBehavior => m_forcedNextBehavior;

        /// <summary>
        /// Returns true when the behavior timer should be ticking.
        /// The default implementation returns the Use Timer inspector flag.
        /// Override for behaviors that need to activate the timer conditionally
        /// (e.g. only once the bird has reached its target).
        /// </summary>
        /// <param name="bird">The bird controller executing this behavior</param>
        public virtual bool IsTimerActive(Bird bird)
        {
            return m_useTimer;
        }

        /// <summary>
        /// Returns true when the behavior is ready to hand control back to the bird.
        /// The default implementation completes when the behavior timer expires.
        /// Override to complete early (e.g. on arrival) or to delay completion until an outro finishes.
        /// </summary>
        /// <param name="bird">The bird controller executing this behavior</param>
        public virtual bool IsBehaviorComplete(Bird bird)
        {
            return bird.BehaviorTimer >= bird.BehaviorDuration;
        }

        /// <summary>
        /// Calculates the final weight for this behavior based on environmental conditions.
        /// The base weight comes from BirdData so each species can weight the same behavior differently.
        /// Override to add environmental modifiers on top of the base weight (e.g. feeder attractiveness).
        /// </summary>
        /// <param name="bird">The bird controller</param>
        /// <param name="baseWeight">Per-species base weight from BirdData</param>
        /// <returns>The calculated weight</returns>
        public virtual int CalculateWeight(Bird bird, int baseWeight)
        {
            return baseWeight;
        }

        /// <summary>
        /// Moves the bird toward a BirdObject's interaction point at the given speed.
        /// Returns true when the bird has reached the target.
        /// </summary>
        protected bool MoveTowardsTarget(Bird bird, BirdObject target, float speed)
        {
            RectTransform birdRect = bird.transform as RectTransform;

            if (birdRect != null && birdRect.parent != null)
            {
                Vector2 targetLocal = birdRect.parent.InverseTransformPoint(target.InteractionPosition);
                return MoveTowardsLocalPosition(bird, birdRect, targetLocal, speed);
            }

            return MoveTowardsWorldPosition(bird, target.InteractionPosition, speed);
        }

        /// <summary>
        /// Moves the bird toward a position in its parent's local space at the given speed.
        /// Returns true when the bird has reached the target.
        /// </summary>
        protected bool MoveTowardsLocalPosition(Bird bird, Vector2 localTargetPosition, float speed)
        {
            RectTransform birdRect = bird.transform as RectTransform;

            if (birdRect != null && birdRect.parent != null)
            {
                return MoveTowardsLocalPosition(bird, birdRect, localTargetPosition, speed);
            }

            // Fallback: treat local position as world position
            return MoveTowardsWorldPosition(bird, localTargetPosition, speed);
        }

        private bool MoveTowardsLocalPosition(Bird bird, RectTransform birdRect, Vector2 localTargetPosition, float speed)
        {
            Vector2 birdLocal = birdRect.localPosition;

            float directionX = localTargetPosition.x - birdLocal.x;
            bird.SetFacingDirection(directionX);

            birdRect.localPosition = Vector2.MoveTowards(birdLocal, localTargetPosition, speed * Time.deltaTime);
            bool reached = Vector2.Distance(birdRect.localPosition, localTargetPosition) < 1f;

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

        private bool MoveTowardsWorldPosition(Bird bird, Vector3 worldTargetPosition, float speed)
        {
            float directionX = worldTargetPosition.x - bird.transform.position.x;
            bird.SetFacingDirection(directionX);

            bird.transform.position = Vector3.MoveTowards(
                bird.transform.position,
                worldTargetPosition,
                speed * Time.deltaTime
            );
            bool reached = Vector3.Distance(bird.transform.position, worldTargetPosition) < 0.1f;

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

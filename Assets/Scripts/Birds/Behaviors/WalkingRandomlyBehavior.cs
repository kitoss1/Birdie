using Birdie.Debug;
using Birdie.Managers;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Behavior that makes the bird walk to a randomly chosen nearby point.
    /// Adds life-like movement between other behaviors.
    /// The random destination is picked relative to the bird's position when the behavior starts.
    /// Uses MovementAnimationStateName while walking. On arrival it completes immediately and defers to ForcedNextBehavior (set to IdleBehavior) for the settle animation.
    /// </summary>
    [CreateAssetMenu(fileName = "WalkingRandomlyBehavior", menuName = "Birdie/Bird Behaviors/Walking Randomly Behavior")]
    public class WalkingRandomlyBehavior : BirdBehaviorState
    {
        [Header("Walk Settings")]
        [SerializeField]
        [Tooltip("Maximum horizontal distance (in local canvas units) the bird can wander from its current position")]
        private float m_walkRangeX = 150f;

        [SerializeField]
        [Tooltip("Minimum horizontal distance the bird must walk (avoids picking a point too close to start)")]
        private float m_minWalkDistanceX = 30f;

        private Vector2 m_targetLocalPosition;
        private bool m_hasReachedTarget;

        public override void OnEnter(Bird bird)
        {
            m_hasReachedTarget = false;
            m_targetLocalPosition = PickRandomTarget(bird);

            DebugBase.Log($"[{nameof(WalkingRandomlyBehavior)}] {bird.BirdData?.BirdName} walking to {m_targetLocalPosition}", DebugCategory.Birds);

            if (!string.IsNullOrEmpty(MovementAnimationStateName))
            {
                bird.PlayAnimation(MovementAnimationStateName);
            }
        }

        public override void Execute(Bird bird)
        {
            if (m_hasReachedTarget)
            {
                return;
            }

            float moveSpeed = bird.BirdData?.MovementSpeed ?? 60f;
            m_hasReachedTarget = MoveTowardsLocalPosition(bird, m_targetLocalPosition, moveSpeed);

            if (m_hasReachedTarget)
            {
                DebugBase.Log($"[{nameof(WalkingRandomlyBehavior)}] {bird.BirdData?.BirdName} reached destination", DebugCategory.Birds);
            }
        }

        public override bool IsBehaviorComplete(Bird bird)
        {
            // Complete as soon as the bird arrives, or fall back to the timer if it never reaches the target.
            return m_hasReachedTarget || bird.BehaviorTimer >= bird.BehaviorDuration;
        }

        public override void OnExit(Bird bird)
        {
            base.OnExit(bird);
            m_hasReachedTarget = false;
        }

        private Vector2 PickRandomTarget(Bird bird)
        {
            RectTransform birdRect = bird.transform as RectTransform;
            Vector2 currentLocal = birdRect != null
                ? (Vector2)birdRect.localPosition
                : (Vector2)bird.transform.position;

            // Pick a signed offset that is at least m_minWalkDistanceX away.
            float sign = Random.value > 0.5f ? 1f : -1f;
            float offsetX = sign * Random.Range(m_minWalkDistanceX, m_walkRangeX);
            float targetX = currentLocal.x + offsetX;

            // Clamp to the scene movement bounds from EnvironmentManager.
            targetX = ClampToMovementBounds(targetX, birdRect);

            return new Vector2(targetX, currentLocal.y);
        }

        private float ClampToMovementBounds(float localTargetX, RectTransform birdRect)
        {
            EnvironmentManager env = GameManager.Instance?.EnvironmentManager;
            if (env == null || birdRect == null)
            {
                return localTargetX;
            }

            RectTransform parentRect = birdRect.parent as RectTransform;
            if (parentRect == null)
            {
                return localTargetX;
            }

            if (!env.TryGetMovementBoundsWorldX(out float worldMinX, out float worldMaxX))
            {
                DebugBase.LogWarning($"[{nameof(WalkingRandomlyBehavior)}] Movement bounds not set on EnvironmentManager, bird position is unclamped", DebugCategory.Birds);
                return localTargetX;
            }

            // Convert world-space X bounds into the bird's parent local space.
            float localMinX = parentRect.InverseTransformPoint(new Vector3(worldMinX, 0f, 0f)).x;
            float localMaxX = parentRect.InverseTransformPoint(new Vector3(worldMaxX, 0f, 0f)).x;

            return Mathf.Clamp(localTargetX, localMinX, localMaxX);
        }
    }
}

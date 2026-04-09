using Birdie.Debug;
using Birdie.Managers;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Eating behavior where the bird moves to a feeder and eats.
    /// Requires a BirdObject of type Feeder to be in the scene.
    /// Uses EnvironmentManager to find feeders.
    ///
    /// Animation phases:
    ///   Walking     — bird walks to the feeder (MovementAnimationStateName)
    ///   EatingStart — one-shot intro clip played once on arrival (m_eatingStartStateName)
    ///   EatingLoop  — looping eat clip; waits for MinDuration then exits at the next loop boundary (AnimationStateName)
    ///   EatingLeave — one-shot outro clip played at the next loop boundary (m_eatingLeaveStateName)
    /// </summary>
    [CreateAssetMenu(fileName = "EatingBehavior", menuName = "Birdie/Bird Behaviors/Eating Behavior")]
    public class EatingBehavior : BirdBehaviorState
    {
        private enum EatingPhase
        {
            Walking,
            EatingStart,
            EatingLoop,
            EatingLeave,
        }

        [Header("Eating Animation Phases")]
        [SerializeField]
        [Tooltip("Animator state for the one-shot eating-start clip (leave empty to skip straight to the loop)")]
        private string m_eatingStartStateName;

        [SerializeField]
        [Tooltip("Animator state for the one-shot eating-leave clip (leave empty to skip the outro)")]
        private string m_eatingLeaveStateName;

        private BirdObject m_targetFeeder;
        private EatingPhase m_phase;
        private float m_lastLoopNormalizedTime;
        private bool m_wantsToLeave;
        private float m_approachSign;

        public override void OnEnter(Bird bird)
        {
            DebugBase.Log($"[{nameof(EatingBehavior)}] {bird.BirdData?.BirdName} looking for food", DebugCategory.Birds);

            m_targetFeeder = FindNearestFeeder(bird);
            m_phase = EatingPhase.Walking;
            m_lastLoopNormalizedTime = 0f;
            m_wantsToLeave = false;
            m_approachSign = 1f;

            if (m_targetFeeder != null)
            {
                m_approachSign = Mathf.Sign(bird.transform.position.x - m_targetFeeder.InteractionPosition.x);
                DebugBase.Log($"[{nameof(EatingBehavior)}] Found feeder at {m_targetFeeder.InteractionPosition}", DebugCategory.Birds);
                m_targetFeeder.OnBirdStartInteraction(bird);

                if (!string.IsNullOrEmpty(MovementAnimationStateName))
                {
                    bird.PlayAnimation(MovementAnimationStateName);
                }
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

            switch (m_phase)
            {
                case EatingPhase.Walking:
                    ExecuteWalking(bird);
                    break;

                case EatingPhase.EatingStart:
                    ExecuteEatingStart(bird);
                    break;

                case EatingPhase.EatingLoop:
                    ExecuteEatingLoop(bird);
                    break;

                case EatingPhase.EatingLeave:
                    // Nothing to drive — Bird.cs timer will call OnExit when the behavior duration expires.
                    break;
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
            m_phase = EatingPhase.Walking;
            m_lastLoopNormalizedTime = 0f;
            m_wantsToLeave = false;
            m_approachSign = 1f;

            base.OnExit(bird);
        }

        public override bool IsTimerActive(Bird bird)
        {
            return m_phase == EatingPhase.EatingLoop || m_phase == EatingPhase.EatingLeave;
        }

        public override bool IsBehaviorComplete(Bird bird)
        {
            if (m_phase != EatingPhase.EatingLeave)
            {
                return false;
            }

            return bird.GetCurrentAnimationNormalizedTime() >= 1f;
        }

        public override bool CanExecute(Bird bird)
        {
            if (!base.CanExecute(bird))
            {
                return false;
            }

            return FindNearestFeeder(bird) != null;
        }


        private void ExecuteWalking(Bird bird)
        {
            float moveSpeed = bird.BirdData?.MovementSpeed ?? 60f;
            float xOffset = (bird.BirdData?.FeederInteractionOffset ?? 0f) * m_approachSign;
            bool reached = MoveTowardsTarget(bird, m_targetFeeder, moveSpeed, xOffset);

            if (!reached)
            {
                return;
            }

            DebugBase.Log($"[{nameof(EatingBehavior)}] Reached feeder", DebugCategory.Birds);

            if (!string.IsNullOrEmpty(m_eatingStartStateName))
            {
                m_phase = EatingPhase.EatingStart;
                bird.PlayAnimation(m_eatingStartStateName);
            }
            else
            {
                EnterEatingLoop(bird);
            }
        }

        private void ExecuteEatingStart(Bird bird)
        {
            // Wait for the one-shot clip to finish (normalizedTime reaches 1).
            if (bird.GetCurrentAnimationNormalizedTime() >= 1f)
            {
                EnterEatingLoop(bird);
            }
        }

        private void EnterEatingLoop(Bird bird)
        {
            DebugBase.Log($"[{nameof(EatingBehavior)}] Entering eating loop", DebugCategory.Birds);
            m_phase = EatingPhase.EatingLoop;
            m_lastLoopNormalizedTime = 0f;

            if (!string.IsNullOrEmpty(AnimationStateName))
            {
                bird.PlayAnimation(AnimationStateName);
            }
        }

        private void ExecuteEatingLoop(Bird bird)
        {
            if (bird.BehaviorTimer >= bird.BehaviorDuration)
            {
                m_wantsToLeave = true;
            }

            // Detect loop boundary: normalizedTime % 1f wraps back toward 0.
            float normalizedTime = bird.GetCurrentAnimationNormalizedTime() % 1f;
            bool loopBoundaryReached = normalizedTime < m_lastLoopNormalizedTime;
            m_lastLoopNormalizedTime = normalizedTime;

            if (m_wantsToLeave && loopBoundaryReached)
            {
                EnterEatingLeave(bird);
            }
        }

        private void EnterEatingLeave(Bird bird)
        {
            DebugBase.Log($"[{nameof(EatingBehavior)}] Playing eating leave animation", DebugCategory.Birds);
            m_phase = EatingPhase.EatingLeave;

            if (!string.IsNullOrEmpty(m_eatingLeaveStateName))
            {
                bird.PlayAnimation(m_eatingLeaveStateName);
            }
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

            return GameManager.Instance.EnvironmentManager.GetNearestUsableObject(BirdObjectType.Feeder, bird);
        }
    }
}

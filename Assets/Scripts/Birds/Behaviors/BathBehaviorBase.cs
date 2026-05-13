using Birdie.Debug;
using Birdie.Managers;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Base class for behaviors that use the bird bath (bathing and drinking).
    ///
    /// Animation phases:
    ///   Walking   — bird walks to the bath interaction point (MovementAnimationStateName)
    ///   BathJump  — bird jumps from the interaction point into the bath (m_jumpAnimationStateName).
    ///               The arc is driven in code: the bird's RectTransform traces a linear path
    ///               from interaction point to BirdBath.BathingPosition with a sine bump added to Y.
    ///               Leave empty to skip (e.g. drinking stays at the interaction point).
    ///   BathLoop  — looping clip; waits for MinDuration then exits at the next loop boundary (AnimationStateName)
    ///   LeaveJump — mirrors BathJump back to the interaction point using the same animation.
    ///               Leave m_jumpAnimationStateName empty to skip.
    /// </summary>
    public abstract class BathBehaviorBase : BirdBehaviorState
    {
        private enum BathPhase
        {
            Walking,
            BathJump,
            BathLoop,
            LeaveJump,
        }

        [Header("Bath Animation Phases")]
        [SerializeField]
        [Tooltip("Animator state played during the jump-in and jump-out. Leave empty to skip both jumps (e.g. drinking).")]
        private string m_jumpAnimationStateName;

        [SerializeField]
        [Tooltip("Peak height of the sine arc added on top of the straight-line path during a jump, in canvas units.")]
        private float m_jumpHeight = 60f;

        [SerializeField]
        [Tooltip("Multiplier applied to the bird's movement speed during a jump. Values above 1 make the jump faster.")]
        private float m_jumpSpeedMultiplier = 1f;

        protected virtual bool ConsumesWater => true;

        private BirdObject m_targetBath;
        private BathPhase m_phase;
        private float m_lastLoopNormalizedTime;
        private bool m_wantsToLeave;
        private float m_approachSign;
        private Vector2 m_interactionLocal;
        private bool m_leaveJumpLanded;

        // Jump arc state — configured by StartJump, consumed by StepJumpArc.
        private Vector2 m_jumpStartLocal;
        private Vector2 m_jumpTargetLocal;
        private float m_jumpTimer;
        private float m_jumpDuration;

        public override void OnEnter(Bird bird)
        {
            DebugBase.Log($"[{GetType().Name}] {bird.BirdData?.BirdName} looking for bird bath", DebugCategory.Birds);

            m_targetBath = FindNearestBath(bird);
            m_phase = BathPhase.Walking;
            m_lastLoopNormalizedTime = 0f;
            m_wantsToLeave = false;
            m_approachSign = 1f;
            m_interactionLocal = Vector2.zero;
            m_leaveJumpLanded = false;
            m_jumpStartLocal = Vector2.zero;
            m_jumpTargetLocal = Vector2.zero;
            m_jumpTimer = 0f;
            m_jumpDuration = 0f;

            if (m_targetBath != null)
            {
                m_approachSign = Mathf.Sign(bird.transform.position.x - m_targetBath.InteractionPosition.x);
                DebugBase.Log($"[{GetType().Name}] Found bird bath at {m_targetBath.InteractionPosition}", DebugCategory.Birds);
                m_targetBath.OnBirdStartInteraction(bird);

                if (!string.IsNullOrEmpty(MovementAnimationStateName))
                {
                    bird.PlayAnimation(MovementAnimationStateName);
                }
            }
            else
            {
                DebugBase.LogWarning($"[{GetType().Name}] No bird bath found! Bird cannot use bath.", DebugCategory.Birds);
            }
        }

        public override void Execute(Bird bird)
        {
            if (m_targetBath == null)
            {
                return;
            }

            switch (m_phase)
            {
                case BathPhase.Walking:
                    ExecuteWalking(bird);
                    break;

                case BathPhase.BathJump:
                    ExecuteBathJump(bird);
                    break;

                case BathPhase.BathLoop:
                    ExecuteBathLoop(bird);
                    break;

                case BathPhase.LeaveJump:
                    ExecuteLeaveJump(bird);
                    break;
            }
        }

        public override void OnExit(Bird bird)
        {
            DebugBase.Log($"[{GetType().Name}] {bird.BirdData?.BirdName} finished using bird bath", DebugCategory.Birds);

            if (m_targetBath != null)
            {
                if (ConsumesWater && m_targetBath is BirdBath consumableBath)
                {
                    consumableBath.ConsumeWater();
                }

                m_targetBath.OnBirdEndInteraction(bird);
            }

            bird.ApplyJumpArcOffset(0f);

            m_targetBath = null;
            m_phase = BathPhase.Walking;
            m_lastLoopNormalizedTime = 0f;
            m_wantsToLeave = false;
            m_approachSign = 1f;
            m_interactionLocal = Vector2.zero;
            m_leaveJumpLanded = false;
            m_jumpStartLocal = Vector2.zero;
            m_jumpTargetLocal = Vector2.zero;
            m_jumpTimer = 0f;
            m_jumpDuration = 0f;

            base.OnExit(bird);
        }

        public override bool IsTimerActive(Bird bird)
        {
            return m_phase == BathPhase.BathLoop;
        }

        public override bool IsBehaviorComplete(Bird bird)
        {
            if (m_phase != BathPhase.LeaveJump)
            {
                return false;
            }

            if (string.IsNullOrEmpty(m_jumpAnimationStateName))
            {
                return true;
            }

            return m_leaveJumpLanded;
        }

        public override bool CanExecute(Bird bird)
        {
            if (!base.CanExecute(bird))
            {
                return false;
            }

            return FindNearestBath(bird) != null;
        }

        private void ExecuteWalking(Bird bird)
        {
            float moveSpeed = bird.BirdData?.MovementSpeed ?? 60f;
            float xOffset = (bird.BirdData?.BathInteractionOffset ?? 0f) * m_approachSign;
            bool reached = MoveTowardsTarget(bird, m_targetBath, moveSpeed, xOffset);

            if (!reached)
            {
                return;
            }

            DebugBase.Log($"[{GetType().Name}] Reached bird bath interaction point", DebugCategory.Birds);

            RectTransform birdRect = bird.transform as RectTransform;
            if (birdRect != null)
            {
                m_interactionLocal = birdRect.localPosition;
            }

            if (!string.IsNullOrEmpty(m_jumpAnimationStateName) && m_targetBath is BirdBath bath)
            {
                if (birdRect != null && birdRect.parent != null)
                {
                    Vector2 bathLocal = birdRect.parent.InverseTransformPoint(bath.BathingPosition);
                    StartJump(bird, birdRect.localPosition, bathLocal);
                    m_phase = BathPhase.BathJump;
                    bird.PlayAnimation(m_jumpAnimationStateName);
                }
                else
                {
                    DebugBase.LogWarning($"[{GetType().Name}] Bath jump requires a RectTransform bird, skipping to loop", DebugCategory.Birds);
                    EnterBathLoop(bird);
                }
            }
            else
            {
                EnterBathLoop(bird);
            }
        }

        private void ExecuteBathJump(Bird bird)
        {
            if (StepJumpArc(bird))
            {
                DebugBase.Log($"[{GetType().Name}] Jump-in complete, entering bath loop", DebugCategory.Birds);
                EnterBathLoop(bird);
            }
        }

        private void EnterBathLoop(Bird bird)
        {
            DebugBase.Log($"[{GetType().Name}] Entering bath loop", DebugCategory.Birds);
            m_phase = BathPhase.BathLoop;
            m_lastLoopNormalizedTime = 0f;

            if (!string.IsNullOrEmpty(AnimationStateName))
            {
                bird.PlayAnimation(AnimationStateName, 0f);
            }
        }

        private void ExecuteBathLoop(Bird bird)
        {
            if (bird.BehaviorTimer >= bird.BehaviorDuration)
            {
                m_wantsToLeave = true;
            }

            float normalizedTime = bird.GetCurrentAnimationNormalizedTime() % 1f;
            bool loopBoundaryReached = normalizedTime < m_lastLoopNormalizedTime;
            m_lastLoopNormalizedTime = normalizedTime;

            if (m_wantsToLeave && loopBoundaryReached)
            {
                EnterLeaveJump(bird);
            }
        }

        private void EnterLeaveJump(Bird bird)
        {
            DebugBase.Log($"[{GetType().Name}] Jumping back to floor", DebugCategory.Birds);
            m_phase = BathPhase.LeaveJump;
            m_leaveJumpLanded = false;

            RectTransform birdRect = bird.transform as RectTransform;
            if (birdRect != null)
            {
                StartJump(bird, birdRect.localPosition, m_interactionLocal);
            }
            else
            {
                m_leaveJumpLanded = true;
            }

            if (!string.IsNullOrEmpty(m_jumpAnimationStateName))
            {
                bird.PlayAnimation(m_jumpAnimationStateName);
            }
        }

        private void ExecuteLeaveJump(Bird bird)
        {
            if (StepJumpArc(bird))
            {
                m_leaveJumpLanded = true;
                DebugBase.Log($"[{GetType().Name}] Leave jump complete", DebugCategory.Birds);
            }
        }

        // Configures arc state for a new jump. Must be called before the first StepJumpArc.
        private void StartJump(Bird bird, Vector2 startLocal, Vector2 targetLocal)
        {
            m_jumpStartLocal = startLocal;
            m_jumpTargetLocal = targetLocal;
            m_jumpTimer = 0f;

            float distance = Vector2.Distance(startLocal, targetLocal);
            float speed = (bird.BirdData?.MovementSpeed ?? 60f) * Mathf.Max(m_jumpSpeedMultiplier, 0.1f);
            m_jumpDuration = speed > 0f ? distance / speed : 0.1f;
        }

        // Advances the jump arc one frame. The arc is baked directly into the bird's RectTransform:
        // position = Lerp(start, target, t) + (arcHeight * Sin(t * PI)) on Y.
        // This avoids conflicts with the visual root / walk hop system.
        // Returns true when the bird has reached the target.
        private bool StepJumpArc(Bird bird)
        {
            RectTransform birdRect = bird.transform as RectTransform;
            if (birdRect == null)
            {
                return true;
            }

            m_jumpTimer += Time.deltaTime;
            float progress = m_jumpDuration > 0f ? Mathf.Clamp01(m_jumpTimer / m_jumpDuration) : 1f;

            bird.SetFacingDirection(m_jumpTargetLocal.x - m_jumpStartLocal.x);

            Vector2 linearPos = Vector2.Lerp(m_jumpStartLocal, m_jumpTargetLocal, progress);
            float arcY = m_jumpHeight * Mathf.Sin(progress * Mathf.PI);
            birdRect.localPosition = new Vector2(linearPos.x, linearPos.y + arcY);

            if (progress >= 1f)
            {
                birdRect.localPosition = m_jumpTargetLocal;
                return true;
            }

            return false;
        }

        private BirdObject FindNearestBath(Bird bird)
        {
            if (GameManager.Instance?.EnvironmentManager == null)
            {
                DebugBase.LogWarning($"[{GetType().Name}] EnvironmentManager not found!", DebugCategory.Birds);
                return null;
            }

            return GameManager.Instance.EnvironmentManager.GetNearestUsableObject(BirdObjectType.BirdBath, bird);
        }
    }
}

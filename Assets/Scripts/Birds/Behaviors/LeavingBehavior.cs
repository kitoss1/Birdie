using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Last behavior played when a bird leaves. Plays a takeoff animation then flies the bird
    /// upward to the spawn point, disappearing off-screen. On completion Bird.LeaveAsync
    /// destroys the GameObject. Set CanBeInterrupted to false so the player cannot open
    /// the context menu mid-flight.
    /// </summary>
    [CreateAssetMenu(fileName = "LeavingBehavior", menuName = "Birdie/Bird Behaviors/Leaving Behavior")]
    public class LeavingBehavior : FlyingBehaviorBase
    {
        private enum LeavingPhase
        {
            Takeoff,
            Flying,
        }

        private LeavingPhase m_phase;

        public override void OnEnter(Bird bird)
        {
            m_phase = LeavingPhase.Takeoff;

            // Bird flies from wherever it currently is up to the spawn point (off-screen).
            SetupFlight(bird, bird.transform.position, bird.SpawnWorldPosition);

            DebugBase.Log($"[{nameof(LeavingBehavior)}] {bird.BirdData?.BirdName} taking off", DebugCategory.Birds);

            if (!string.IsNullOrEmpty(AnimationStateName))
            {
                bird.PlayAnimation(AnimationStateName);
            }
            else
            {
                DebugBase.LogWarning($"[{nameof(LeavingBehavior)}] No takeoff animation set, skipping takeoff phase", DebugCategory.Birds);
                StartFlying(bird);
            }
        }

        public override void Execute(Bird bird)
        {
            if (m_phase == LeavingPhase.Takeoff)
            {
                if (bird.GetCurrentAnimationNormalizedTime() >= 1f)
                {
                    StartFlying(bird);
                }
            }
            else if (m_phase == LeavingPhase.Flying)
            {
                StepFlight(bird);
            }
        }

        public override bool CanExecute(Bird bird) => false;

        public override bool IsBehaviorComplete(Bird bird)
        {
            return m_phase == LeavingPhase.Flying && m_progress >= 1f;
        }

        public override void OnExit(Bird bird)
        {
            m_phase = LeavingPhase.Takeoff;
            m_progress = 0f;
            bird.TiltVisual(0f);
            // No base call — no walk hop state to reset during a fly-out.
        }

        private void StartFlying(Bird bird)
        {
            DebugBase.Log($"[{nameof(LeavingBehavior)}] {bird.BirdData?.BirdName} took flight, flying out", DebugCategory.Birds);
            m_phase = LeavingPhase.Flying;
            m_progress = 0f;

            if (!string.IsNullOrEmpty(MovementAnimationStateName))
            {
                bird.PlayAnimation(MovementAnimationStateName);
            }
        }
    }
}

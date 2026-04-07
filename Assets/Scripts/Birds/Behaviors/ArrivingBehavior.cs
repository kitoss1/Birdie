using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// First behavior played when a bird visits. Spawns the bird above its landing position
    /// and flies it down. On landing, transitions to ForcedNextBehavior (set to IdleBehavior).
    /// Set CanBeInterrupted to false so the player cannot open the context menu mid-flight.
    /// </summary>
    [CreateAssetMenu(fileName = "ArrivingBehavior", menuName = "Birdie/Bird Behaviors/Arriving Behavior")]
    public class ArrivingBehavior : FlyingBehaviorBase
    {
        private enum ArrivingPhase
        {
            Flying,
            Landing,
        }

        private ArrivingPhase m_phase;

        public override void OnEnter(Bird bird)
        {
            m_phase = ArrivingPhase.Flying;

            // Bird starts at the spawn point (current position) and flies down to the landing point.
            SetupFlight(bird, bird.transform.position, bird.LandingWorldPosition);

            DebugBase.Log($"[{nameof(ArrivingBehavior)}] {bird.BirdData?.BirdName} arriving from above", DebugCategory.Birds);

            if (!string.IsNullOrEmpty(MovementAnimationStateName))
            {
                bird.PlayAnimation(MovementAnimationStateName);
            }
        }

        public override void Execute(Bird bird)
        {
            if (m_phase != ArrivingPhase.Flying)
            {
                return;
            }

            if (StepFlight(bird))
            {
                DebugBase.Log($"[{nameof(ArrivingBehavior)}] {bird.BirdData?.BirdName} touched down, playing landing animation", DebugCategory.Birds);
                bird.TiltVisual(0f);
                m_phase = ArrivingPhase.Landing;

                if (!string.IsNullOrEmpty(AnimationStateName))
                {
                    bird.PlayAnimation(AnimationStateName);
                }
                else
                {
                    DebugBase.LogWarning($"[{nameof(ArrivingBehavior)}] No landing animation set, skipping landing phase", DebugCategory.Birds);
                }
            }
        }

        public override bool CanExecute(Bird bird) => false;

        public override bool IsBehaviorComplete(Bird bird)
        {
            if (m_phase != ArrivingPhase.Landing)
            {
                return false;
            }

            // If no landing clip was configured the phase is still set to Landing as a signal to complete.
            if (string.IsNullOrEmpty(AnimationStateName))
            {
                return true;
            }

            return bird.GetCurrentAnimationNormalizedTime() >= 1f;
        }

        public override void OnExit(Bird bird)
        {
            m_phase = ArrivingPhase.Flying;
            m_progress = 0f;
            bird.TiltVisual(0f);
            // No base call — no walk hop state to reset during a fly-in.
        }
    }
}

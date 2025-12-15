using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Idle behavior where the bird sits and looks around.
    /// This is a default behavior available to all birds at all friendship levels.
    /// </summary>
    [CreateAssetMenu(fileName = "IdleBehavior", menuName = "Birdie/Bird Behaviors/Idle Behavior")]
    public class IdleBehavior : BirdBehaviorState
    {
        public override void OnEnter(Bird bird)
        {
            DebugBase.Log($"[{nameof(IdleBehavior)}] {bird.BirdData?.BirdName} entered idle state", DebugCategory.Birds);
            
            // TODO: Play idle animation on Spine skeleton
            // Example: bird.SpineSkeleton.AnimationState.SetAnimation(0, "idle", true);
        }

        public override void Execute(Bird bird)
        {
            // Idle behavior doesn't need continuous updates
            // The bird just sits there looking around
            // Animation handles the visual part
        }

        public override void OnExit(Bird bird)
        {
            DebugBase.Log($"[{nameof(IdleBehavior)}] {bird.BirdData?.BirdName} exiting idle state", DebugCategory.Birds);
            
            // No cleanup needed for idle
        }

        public override bool CanExecute(Bird bird)
        {
            // Idle is always available - it's the fallback behavior
            return true;
        }
    }
}

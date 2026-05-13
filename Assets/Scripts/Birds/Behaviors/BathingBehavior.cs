namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Behavior where a bird walks to a bird bath and bathes.
    /// Configure the bathing animation states in the inspector.
    /// </summary>
    [UnityEngine.CreateAssetMenu(fileName = "BathingBehavior", menuName = "Birdie/Bird Behaviors/Bathing Behavior")]
    public class BathingBehavior : BathBehaviorBase
    {
        protected override bool ConsumesWater => false;
    }
}

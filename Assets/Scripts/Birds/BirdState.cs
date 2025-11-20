namespace Birdie.Birds
{
    /// <summary>
    /// Represents the current state of a bird during its visit.
    /// </summary>
    public enum BirdState
    {
        /// <summary>
        /// Bird is appearing/arriving with intro animation.
        /// </summary>
        Appearing,

        /// <summary>
        /// Bird is idle and can be interacted with.
        /// </summary>
        Idle,

        /// <summary>
        /// Bird is eating food.
        /// </summary>
        Eating,

        /// <summary>
        /// Bird is leaving/departing with outro animation.
        /// </summary>
        Leaving
    }
}

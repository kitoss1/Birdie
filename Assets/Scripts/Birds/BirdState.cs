namespace Birdie.Birds
{
    /// <summary>
    /// Represents the current state of a bird during its visit.
    /// </summary>
    public enum BirdState
    {
        /// <summary>
        /// Bird is appearing on screen before the visit starts.
        /// </summary>
        Appearing,

        /// <summary>
        /// Bird is actively visiting — performing behaviors, interactable by the player.
        /// </summary>
        Visiting,

        /// <summary>
        /// Bird is leaving — flying out to the spawn point before being destroyed.
        /// </summary>
        Leaving,

        /// <summary>
        /// Bird is paused during a minigame. Cannot be clicked, change behavior, or leave.
        /// </summary>
        Paused
    }
}

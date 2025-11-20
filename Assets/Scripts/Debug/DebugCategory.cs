namespace Birdie.Debug
{
    /// <summary>
    /// Categories for debug logging that can be enabled/disabled individually.
    /// </summary>
    public enum DebugCategory
    {
        /// <summary>
        /// General game-related logs.
        /// </summary>
        General,

        /// <summary>
        /// Mouse and input-related logs.
        /// </summary>
        Mouse,

        /// <summary>
        /// UI and canvas-related logs.
        /// </summary>
        UI,

        /// <summary>
        /// Transparent window and click-through logs.
        /// </summary>
        Transparency,

        /// <summary>
        /// Manager initialization and lifecycle logs.
        /// </summary>
        Managers,

        /// <summary>
        /// Bird spawning and behavior logs.
        /// </summary>
        Birds,

        /// <summary>
        /// Economy and currency-related logs.
        /// </summary>
        Economy,

        /// <summary>
        /// Friendship system logs.
        /// </summary>
        Friendship,

        /// <summary>
        /// Camera and rendering logs.
        /// </summary>
        Camera,

        /// <summary>
        /// Physics and collision logs.
        /// </summary>
        Physics
    }
}

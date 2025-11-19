namespace Birdie.Core
{
    /// <summary>
    /// Enum representing the different states the game can be in
    /// Note: No "Paused" state - this is a real-time idle game that always runs
    /// </summary>
    public enum GameState
    {
        Playing,        // Normal gameplay on main habitat view - player can interact with birds
        MenuOpen,       // A menu (diary, shop, settings) is open - covers main view but time continues
        InMinigame,     // Player is currently playing a minigame - bird spawning paused
        Loading         // Game is loading data (initial state)
    }
}

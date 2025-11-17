using UnityEngine;

/// <summary>
/// Manages all bird-related logic: spawning, appearance times, weighted selection, pity system.
/// Responsible for the core loop of birds visiting the habitat.
/// </summary>
public class BirdManager : BaseManager
{
    [Header("Bird Spawning")]
    [SerializeField] private float spawnCheckInterval = 30f;
    [SerializeField] private Transform spawnParent;
    
    private bool _isSpawningPaused = false;

    public override void Initialize(GameManager gameManager)
    {
        base.Initialize(gameManager);
        
        // Custom initialization here
        Debug.Log("[BirdManager] Setting up bird spawning system...");
    }

    /// <summary>
    /// Pauses bird spawning (called when minigame starts)
    /// </summary>
    public void PauseBirdSpawning()
    {
        _isSpawningPaused = true;
        Debug.Log("[BirdManager] Bird spawning paused");
    }

    /// <summary>
    /// Resumes bird spawning (called when minigame ends)
    /// </summary>
    public void ResumeBirdSpawning()
    {
        _isSpawningPaused = false;
        Debug.Log("[BirdManager] Bird spawning resumed");
    }
}

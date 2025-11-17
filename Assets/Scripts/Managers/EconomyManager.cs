using UnityEngine;

/// <summary>
/// Manages the game's economy: golden seeds, rewards calculation, purchases.
/// Handles the dual reward system (golden seeds + friendship points).
/// </summary>
public class EconomyManager : BaseManager
{
    [Header("Economy")]
    [SerializeField] private int _goldenSeeds = 0;
    
    public int GoldenSeeds => _goldenSeeds;

    public override void Initialize(GameManager gameManager)
    {
        base.Initialize(gameManager);
        Debug.Log("[EconomyManager] Economy system initialized");
    }

    /// <summary>
    /// Adds golden seeds to the player's balance
    /// </summary>
    public void AddGoldenSeeds(int amount)
    {
        if (!EnsureInitialized()) return;
        
        _goldenSeeds += amount;
        Debug.Log($"[EconomyManager] Added {amount} golden seeds. Total: {_goldenSeeds}");
    }

    /// <summary>
    /// Checks if the player can afford a purchase
    /// </summary>
    public bool CanAfford(int cost)
    {
        return _goldenSeeds >= cost;
    }

    /// <summary>
    /// Attempts to make a purchase
    /// </summary>
    public bool TryPurchase(int cost)
    {
        if (!EnsureInitialized()) return false;
        
        if (CanAfford(cost))
        {
            _goldenSeeds -= cost;
            Debug.Log($"[EconomyManager] Purchase successful. Remaining: {_goldenSeeds}");
            return true;
        }
        
        Debug.LogWarning("[EconomyManager] Insufficient golden seeds");
        return false;
    }
}

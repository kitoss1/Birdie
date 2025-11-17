using UnityEngine;

/// <summary>
/// Base class for all manager classes in the game.
/// Provides a standard initialization pattern using Dependency Injection.
/// All managers should inherit from this class.
/// </summary>
public abstract class BaseManager : MonoBehaviour
{
    protected GameManager _gameManager;
    protected bool _isInitialized = false;

    /// <summary>
    /// Initializes the manager with a reference to the GameManager.
    /// This is called by GameManager during its initialization phase.
    /// Override this in child classes to add custom initialization logic.
    /// </summary>
    public virtual void Initialize(GameManager gameManager)
    {
        if (_isInitialized)
        {
            Debug.LogWarning($"[{GetType().Name}] Already initialized!");
            return;
        }

        _gameManager = gameManager;
        _isInitialized = true;

        Debug.Log($"[{GetType().Name}] Initialized successfully");
    }

    /// <summary>
    /// Checks if the manager has been properly initialized
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Protected helper to ensure manager is initialized before use
    /// </summary>
    protected bool EnsureInitialized()
    {
        if (!_isInitialized)
        {
            Debug.LogError($"[{GetType().Name}] Not initialized! Call Initialize() first.");
            return false;
        }
        return true;
    }
}

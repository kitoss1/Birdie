using UnityEngine;
using System;

/// <summary>
/// Central manager that maintains the overall game state and provides access to other managers.
/// This is the core coordinator that manages all game systems.
/// Based on the design document's architecture.
/// Uses Dependency Injection instead of singleton pattern.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Game State
    [Header("Game State")]
    [SerializeField] private GameState _currentState = GameState.Playing;
    
    public GameState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                GameState previousState = _currentState;
                _currentState = value;
                OnGameStateChanged?.Invoke(previousState, _currentState);
                Debug.Log($"[GameManager] State changed: {previousState} → {_currentState}");
            }
        }
    }

    // Events for state changes
    public event Action<GameState, GameState> OnGameStateChanged;
    public event Action OnMenuOpened;
    public event Action OnMenuClosed;
    public event Action OnGameStarted;
    public event Action OnMinigameStarted;
    public event Action OnMinigameEnded;
    #endregion

    #region Manager References
    [Header("Manager References")]
    [SerializeField] private BirdManager _birdManager;
    [SerializeField] private EconomyManager _economyManager;
    [SerializeField] private FriendshipManager _friendshipManager;
    /*[SerializeField] private UpgradeManager _upgradeManager;
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private SaveManager _saveManager;
    [SerializeField] private MinigameManager _minigameManager;
    [SerializeField] private HabitatManager _habitatManager;*/

    // Public accessors for other systems to get manager references
    public BirdManager BirdManager => _birdManager;
    public EconomyManager EconomyManager => _economyManager;
    public FriendshipManager FriendshipManager => _friendshipManager;
    /*public UpgradeManager UpgradeManager => _upgradeManager;
    public TimeManager TimeManager => _timeManager;
    public SaveManager SaveManager => _saveManager;
    public MinigameManager MinigameManager => _minigameManager;
    public HabitatManager HabitatManager => _habitatManager;*/
    #endregion

    #region Current Menu Tracking
    private MenuType _currentOpenMenu = MenuType.None;
    public MenuType CurrentOpenMenu => _currentOpenMenu;
    #endregion

    #region Initialization
    private bool _isInitialized = false;

    private void Awake()
    {
        InitializeManagers();
    }

    private void Start()
    {
        // Load game data
        /*if (_saveManager != null)
        {
            _saveManager.LoadGame();
        }*/

        // Start the game
        StartGame();
    }

    /// <summary>
    /// Initializes all manager references and injects dependencies
    /// </summary>
    private void InitializeManagers()
    {
        Debug.Log("[GameManager] Initializing managers...");

        // Validate that all managers are assigned
        ValidateManagerReferences();

        // Inject GameManager reference into managers that need it
        InjectDependencies();

        _isInitialized = true;
        Debug.Log("[GameManager] Managers initialized successfully");
    }

    /// <summary>
    /// Validates that all required managers are assigned in the inspector
    /// </summary>
    private void ValidateManagerReferences()
    {
        if (_birdManager == null)
            Debug.LogWarning("[GameManager] BirdManager is not assigned!");
        
        if (_economyManager == null)
            Debug.LogWarning("[GameManager] EconomyManager is not assigned!");
        
        if (_friendshipManager == null)
            Debug.LogWarning("[GameManager] FriendshipManager is not assigned!");
        
        /*if (_upgradeManager == null)
            Debug.LogWarning("[GameManager] UpgradeManager is not assigned!");
        
        if (_timeManager == null)
            Debug.LogWarning("[GameManager] TimeManager is not assigned!");
        
        if (_saveManager == null)
            Debug.LogWarning("[GameManager] SaveManager is not assigned!");
        
        if (_minigameManager == null)
            Debug.LogWarning("[GameManager] MinigameManager is not assigned!");
        
        if (_habitatManager == null)
            Debug.LogWarning("[GameManager] HabitatManager is not assigned!");*/
    }

    /// <summary>
    /// Injects this GameManager into other managers that need it
    /// Each manager should have an Initialize(GameManager) or similar method
    /// </summary>
    private void InjectDependencies()
    {
        // Inject GameManager reference into managers
        // Each manager can access other managers through this GameManager
        
        if (_birdManager != null)
            _birdManager.Initialize(this);
        
        if (_economyManager != null)
            _economyManager.Initialize(this);
        
        if (_friendshipManager != null)
            _friendshipManager.Initialize(this);
        
        /*if (_upgradeManager != null)
            _upgradeManager.Initialize(this);
        
        if (_timeManager != null)
            _timeManager.Initialize(this);
        
        if (_saveManager != null)
            _saveManager.Initialize(this);
        
        if (_minigameManager != null)
            _minigameManager.Initialize(this);
        
        if (_habitatManager != null)
            _habitatManager.Initialize(this);*/
    }
    #endregion

    #region Game State Management
    /// <summary>
    /// Starts the game (initial state)
    /// </summary>
    public void StartGame()
    {
        if (CurrentState == GameState.Playing)
        {
            Debug.LogWarning("[GameManager] Game is already playing");
            return;
        }

        CurrentState = GameState.Playing;
        _currentOpenMenu = MenuType.None;
        OnGameStarted?.Invoke();
        
        Debug.Log("[GameManager] Game started");
    }

    /// <summary>
    /// Opens a menu (diary, shop, settings, etc.)
    /// This changes the view but doesn't stop time - birds can still visit in the background
    /// </summary>
    public void OpenMenu(MenuType menuType)
    {
        if (CurrentState == GameState.InMinigame)
        {
            Debug.LogWarning("[GameManager] Cannot open menu while in minigame");
            return;
        }

        CurrentState = GameState.MenuOpen;
        _currentOpenMenu = menuType;
        
        // Note: We don't pause bird spawning - birds can still arrive while menu is open
        // The UI layer simply covers the habitat view
        
        OnMenuOpened?.Invoke();
        Debug.Log($"[GameManager] Menu opened: {menuType}");
    }

    /// <summary>
    /// Closes the current menu and returns to main view
    /// </summary>
    public void CloseMenu()
    {
        if (CurrentState != GameState.MenuOpen)
        {
            Debug.LogWarning("[GameManager] No menu is open");
            return;
        }

        MenuType closedMenu = _currentOpenMenu;
        CurrentState = GameState.Playing;
        _currentOpenMenu = MenuType.None;
        
        OnMenuClosed?.Invoke();
        Debug.Log($"[GameManager] Menu closed: {closedMenu}");
    }

    /// <summary>
    /// Enters minigame state
    /// During minigames, time continues but bird spawning is paused
    /// </summary>
    public void StartMinigame()
    {
        CurrentState = GameState.InMinigame;
        
        // Pause bird spawning during minigame (player is focused on the minigame)
        if (_birdManager != null)
        {
            _birdManager.PauseBirdSpawning();
        }
        
        OnMinigameStarted?.Invoke();
        Debug.Log("[GameManager] Minigame started");
    }

    /// <summary>
    /// Exits minigame state and returns to main view
    /// </summary>
    public void EndMinigame()
    {
        if (CurrentState != GameState.InMinigame)
        {
            Debug.LogWarning("[GameManager] Not in minigame state");
            return;
        }

        CurrentState = GameState.Playing;
        
        // Resume bird spawning
        if (_birdManager != null)
        {
            _birdManager.ResumeBirdSpawning();
        }
        
        OnMinigameEnded?.Invoke();
        Debug.Log("[GameManager] Minigame ended");
    }

    /// <summary>
    /// Checks if the player is on the main habitat view (not in menu or minigame)
    /// </summary>
    public bool IsOnMainView()
    {
        return CurrentState == GameState.Playing;
    }

    /// <summary>
    /// Checks if the player can interact with birds (main view only)
    /// </summary>
    public bool CanInteractWithBirds()
    {
        return CurrentState == GameState.Playing;
    }

    /// <summary>
    /// Checks if a menu is currently open
    /// </summary>
    public bool IsMenuOpen()
    {
        return CurrentState == GameState.MenuOpen;
    }

    /// <summary>
    /// Checks if currently in a minigame
    /// </summary>
    public bool IsInMinigame()
    {
        return CurrentState == GameState.InMinigame;
    }

    /// <summary>
    /// Checks if a specific menu is currently open
    /// </summary>
    public bool IsMenuOpen(MenuType menuType)
    {
        return CurrentState == GameState.MenuOpen && _currentOpenMenu == menuType;
    }
    #endregion

    #region Save/Load
    /// <summary>
    /// Triggers a game save
    /// </summary>
    public void SaveGame()
    {
        /*if (_saveManager != null)
        {
            _saveManager.SaveGame();
            Debug.Log("[GameManager] Game saved");
        }
        else
        {
            Debug.LogError("[GameManager] SaveManager not found");
        }*/
    }

    /// <summary>
    /// Triggers a game load
    /// </summary>
    public void LoadGame()
    {
        /*if (_saveManager != null)
        {
            _saveManager.LoadGame();
            Debug.Log("[GameManager] Game loaded");
        }
        else
        {
            Debug.LogError("[GameManager] SaveManager not found");
        }*/
    }
    #endregion

    #region Manager Status Queries
    /// <summary>
    /// Gets the status of all managers (useful for debugging)
    /// </summary>
    public string GetManagersStatus()
    {
        string status = "=== MANAGERS STATUS ===\n";
        status += $"BirdManager: {(_birdManager != null ? "✓" : "✗")}\n";
        status += $"EconomyManager: {(_economyManager != null ? "✓" : "✗")}\n";
        status += $"FriendshipManager: {(_friendshipManager != null ? "✓" : "✗")}\n";
        /*status += $"UpgradeManager: {(_upgradeManager != null ? "✓" : "✗")}\n";
        status += $"TimeManager: {(_timeManager != null ? "✓" : "✗")}\n";
        status += $"SaveManager: {(_saveManager != null ? "✓" : "✗")}\n";
        status += $"MinigameManager: {(_minigameManager != null ? "✓" : "✗")}\n";
        status += $"HabitatManager: {(_habitatManager != null ? "✓" : "✗")}\n";*/
        status += $"Game State: {CurrentState}\n";
        status += $"Current Menu: {_currentOpenMenu}";
        return status;
    }

    /// <summary>
    /// Checks if all critical managers are initialized
    /// </summary>
    public bool AreAllManagersReady()
    {
        return _birdManager != null &&
               _economyManager != null &&
               _friendshipManager != null; /*&&
               _timeManager != null &&
               _saveManager != null;*/
    }
    #endregion

    #region Application Lifecycle
    private void OnApplicationQuit()
    {
        // Auto-save on quit
        SaveGame();
        Debug.Log("[GameManager] Application quitting, game saved");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Auto-save when application loses focus (minimized, etc.)
        if (pauseStatus)
        {
            SaveGame();
            Debug.Log("[GameManager] Application paused, game saved");
        }
    }
    #endregion

    #region Debug Methods
#if UNITY_EDITOR
    [ContextMenu("Log Managers Status")]
    private void DebugLogManagersStatus()
    {
        Debug.Log(GetManagersStatus());
    }

    [ContextMenu("Force Save")]
    private void DebugForceSave()
    {
        SaveGame();
    }

    [ContextMenu("Force Load")]
    private void DebugForceLoad()
    {
        LoadGame();
    }

    [ContextMenu("Simulate Open Diary")]
    private void DebugOpenDiary()
    {
        OpenMenu(MenuType.Diary);
    }

    [ContextMenu("Simulate Close Menu")]
    private void DebugCloseMenu()
    {
        CloseMenu();
    }
#endif
    #endregion
}

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

/// <summary>
/// Enum representing the different menus in the game
/// </summary>
public enum MenuType
{
    None,
    Diary,
    Shop,
    Settings,
    Tutorial
}

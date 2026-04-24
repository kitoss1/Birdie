using System;
using Birdie.Core;
using Birdie.Debug;
using Birdie.Save;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Central manager that maintains the overall game state and provides access to other managers.
    /// This is the core coordinator that manages all game systems.
    /// Based on the design document's architecture.
    /// Uses Singleton pattern for easy global access.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager s_instance;

        /// <summary>
        /// Singleton instance of GameManager. Provides global access to all game systems.
        /// </summary>
        public static GameManager Instance => s_instance;
        private GameState m_currentState = GameState.Loading;

        public GameState CurrentState
        {
            get => m_currentState;
            private set
            {
                if (m_currentState != value)
                {
                    GameState previousState = m_currentState;
                    m_currentState = value;
                    OnGameStateChanged?.Invoke(previousState, m_currentState);
                    DebugBase.Log($"[{nameof(GameManager)}] State changed: {previousState} → {m_currentState}");
                }
            }
        }

        public event Action<GameState, GameState> OnGameStateChanged;
        public event Action OnMenuOpened;
        public event Action OnMenuClosed;
        public event Action OnGameStarted;
        public event Action OnMinigameStarted;
        public event Action OnMinigameEnded;

        /// <summary>
        /// Event fired when initialization progress updates.
        /// Parameters: progress (0-1), phase description
        /// </summary>
        public event Action<float, string> OnInitializationProgress;

        [Header("Manager References")]
        [SerializeField] private BirdManager m_birdManager;
        [SerializeField] private EconomyManager m_economyManager;
        [SerializeField] private FriendshipManager m_friendshipManager;
        [SerializeField] private DiaryManager m_diaryManager;
        [SerializeField] private MenuManager m_menuManager;
        [SerializeField] private EnvironmentManager m_environmentManager;
        [SerializeField] private StoreManager m_storeManager;
        [SerializeField] private DiaryUIManager m_diaryUIManager;
        [SerializeField] private SoundManager m_soundManager;
        [SerializeField] private ToastManager m_toastManager;
        [SerializeField] private WindowsillManager m_windowsillManager;

        public BirdManager BirdManager => m_birdManager;
        public EconomyManager EconomyManager => m_economyManager;
        public FriendshipManager FriendshipManager => m_friendshipManager;
        public DiaryManager DiaryManager => m_diaryManager;
        public MenuManager MenuManager => m_menuManager;
        public EnvironmentManager EnvironmentManager => m_environmentManager;
        public StoreManager StoreManager => m_storeManager;
        public DiaryUIManager DiaryUIManager => m_diaryUIManager;
        public SoundManager SoundManager => m_soundManager;
        public ToastManager ToastManager => m_toastManager;
        public WindowsillManager WindowsillManager => m_windowsillManager;

        private MenuType m_currentOpenMenu = MenuType.None;
        public MenuType CurrentOpenMenu => m_currentOpenMenu;

        private SaveManager m_saveManager;
        private bool m_initializationComplete = false;

        public SaveManager SaveManager => m_saveManager;

        /// <summary>
        /// Returns true when GameManager has completed async initialization.
        /// </summary>
        public bool IsInitializationComplete => m_initializationComplete;

        private async void Awake()
        {
            if (!ValidateSingleton())
            {
                return;
            }

            try
            {
                await InitializeAsync();
            }
            catch (Exception e)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] Initialization failed: {e.Message}");
            }
        }

        /// <summary>
        /// Validates the singleton instance. Ensures only one GameManager exists.
        /// </summary>
        private bool ValidateSingleton()
        {
            if (s_instance != null && s_instance != this)
            {
                DebugBase.LogWarning($"[{nameof(GameManager)}] Duplicate instance destroyed");
                Destroy(gameObject);
                return false;
            }

            s_instance = this;

            // Only call DontDestroyOnLoad if this is a root GameObject
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                DebugBase.LogWarning($"[{nameof(GameManager)}] GameManager is not a root GameObject. DontDestroyOnLoad not applied.");
            }

            return true;
        }

        /// <summary>
        /// Initializes the game in clear, sequential phases.
        /// Each phase can depend on previous phases being complete.
        /// </summary>
        private async UniTask InitializeAsync()
        {
            DebugBase.Log($"[{nameof(GameManager)}] Starting initialization...");
            OnInitializationProgress?.Invoke(0f, "Starting...");

            // PHASE 1: Load persistent data (must happen first)
            OnInitializationProgress?.Invoke(0.1f, "Loading save data...");
            await InitializeSaveSystemAsync();

            // PHASE 2: Initialize core managers (can depend on save data)
            OnInitializationProgress?.Invoke(0.3f, "Initializing systems...");
            await InitializeManagersAsync();

            // PHASE 3: Post-initialization setup (all managers ready)
            OnInitializationProgress?.Invoke(0.9f, "Finalizing...");
            await PostInitializationAsync();

            m_initializationComplete = true;
            OnInitializationProgress?.Invoke(1f, "Ready!");
            DebugBase.Log($"[{nameof(GameManager)}] Initialization complete!");

            StartGame();
        }

        /// <summary>
        /// Phase 1: Initialize save system and load game data.
        /// All subsequent phases can depend on save data being available.
        /// </summary>
        private async UniTask InitializeSaveSystemAsync()
        {
            DebugBase.Log($"[{nameof(GameManager)}] Phase 1: Initializing save system...");

            m_saveManager = new SaveManager();
            m_saveManager.Initialize();
            m_saveManager.LoadGame();

            // Ensure save data is fully processed
            await UniTask.Yield();

            DebugBase.Log($"[{nameof(GameManager)}] Save system ready");
        }

        /// <summary>
        /// Phase 2: Initialize all game managers.
        /// Save data is guaranteed to be loaded at this point.
        /// </summary>
        private async UniTask InitializeManagersAsync()
        {
            DebugBase.Log($"[{nameof(GameManager)}] Phase 2: Initializing managers...");

            ValidateManagerReferences();

            // Initialize managers that don't depend on each other (can run in parallel)
            await UniTask.WhenAll(
                InitializeEconomyManagerAsync(),
                InitializeBirdManagerAsync(),
                InitializeFriendshipManagerAsync(),
                InitializeEnvironmentManagerAsync(),
                InitializeSoundManagerAsync(),
                InitializeToastManagerAsync(),
                InitializeWindowsillManagerAsync()
            );

            // Initialize managers that depend on other managers (sequential)
            await InitializeDiaryManagerAsync();   // Depends on BirdManager, SaveManager
            await InitializeStoreManagerAsync();   // Depends on SaveManager, EconomyManager
            await InitializeDiaryUIManagerAsync(); // Depends on DiaryManager, FriendshipManager
            await InitializeMenuManagerAsync();    // Depends on all other managers

            DebugBase.Log($"[{nameof(GameManager)}] All managers initialized");
        }

        /// <summary>
        /// Phase 3: Post-initialization tasks.
        /// All managers are guaranteed to be ready.
        /// </summary>
        private async UniTask PostInitializationAsync()
        {
            DebugBase.Log($"[{nameof(GameManager)}] Phase 3: Post-initialization...");

            // Placeholder for future post-initialization logic
            await UniTask.Yield();

            DebugBase.Log($"[{nameof(GameManager)}] Post-initialization complete");
        }

        /// <summary>
        /// Validates that all required managers are assigned in the inspector
        /// </summary>
        private void ValidateManagerReferences()
        {
            if (m_birdManager == null)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] BirdManager is not assigned!");
            }

            if (m_economyManager == null)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] EconomyManager is not assigned!");
            }

            if (m_friendshipManager == null)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] FriendshipManager is not assigned!");
            }

            if (m_diaryManager == null)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] DiaryManager is not assigned!");
            }

            if (m_menuManager == null)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] MenuManager is not assigned!");
            }

            if (m_environmentManager == null)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] EnvironmentManager is not assigned!");
            }

            if (m_storeManager == null)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] StoreManager is not assigned!");
            }

            if (m_diaryUIManager == null)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] DiaryUIManager is not assigned!");
            }

            if (m_soundManager == null)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] SoundManager is not assigned!");
            }

            if (m_toastManager == null)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] ToastManager is not assigned!");
            }

            if (m_windowsillManager == null)
            {
                DebugBase.LogError($"[{nameof(GameManager)}] WindowsillManager is not assigned!");
            }
        }

        /// <summary>
        /// Initializes EconomyManager. Depends on SaveManager.
        /// </summary>
        private async UniTask InitializeEconomyManagerAsync()
        {
            if (m_economyManager != null)
            {
                m_economyManager.Initialize();
                m_economyManager.SetSaveManager(m_saveManager);
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Initializes BirdManager.
        /// </summary>
        private async UniTask InitializeBirdManagerAsync()
        {
            if (m_birdManager != null)
            {
                m_birdManager.Initialize();
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Initializes FriendshipManager. Depends on SaveManager.
        /// </summary>
        private async UniTask InitializeFriendshipManagerAsync()
        {
            if (m_friendshipManager != null)
            {
                m_friendshipManager.Initialize();
                m_friendshipManager.SetSaveManager(m_saveManager);
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Initializes EnvironmentManager.
        /// </summary>
        private async UniTask InitializeEnvironmentManagerAsync()
        {
            if (m_environmentManager != null)
            {
                m_environmentManager.Initialize();
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Initializes SoundManager. Depends on SaveManager.
        /// </summary>
        private async UniTask InitializeSoundManagerAsync()
        {
            if (m_soundManager != null)
            {
                m_soundManager.Initialize();
                m_soundManager.SetSaveManager(m_saveManager);
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Initializes ToastManager.
        /// </summary>
        private async UniTask InitializeToastManagerAsync()
        {
            if (m_toastManager != null)
            {
                m_toastManager.Initialize();
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Initializes WindowsillManager. Depends on SaveManager.
        /// </summary>
        private async UniTask InitializeWindowsillManagerAsync()
        {
            if (m_windowsillManager != null)
            {
                m_windowsillManager.Initialize();
                m_windowsillManager.SetSaveManager(m_saveManager);
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Initializes DiaryManager. Depends on SaveManager and BirdManager.
        /// </summary>
        private async UniTask InitializeDiaryManagerAsync()
        {
            if (m_diaryManager != null)
            {
                m_diaryManager.Initialize();
                m_diaryManager.SetSaveManager(m_saveManager);
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Initializes StoreManager. Depends on SaveManager and EconomyManager.
        /// </summary>
        private async UniTask InitializeStoreManagerAsync()
        {
            if (m_storeManager != null)
            {
                m_storeManager.Initialize();
                m_storeManager.SetSaveManager(m_saveManager);
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Initializes DiaryUIManager. Depends on DiaryManager and FriendshipManager.
        /// </summary>
        private async UniTask InitializeDiaryUIManagerAsync()
        {
            if (m_diaryUIManager != null)
            {
                m_diaryUIManager.Initialize();
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Initializes MenuManager.
        /// </summary>
        private async UniTask InitializeMenuManagerAsync()
        {
            if (m_menuManager != null)
            {
                m_menuManager.Initialize();
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Starts the game (initial state)
        /// </summary>
        public void StartGame()
        {
            if (CurrentState == GameState.Playing)
            {
                DebugBase.LogWarning($"[{nameof(GameManager)}] Game is already playing");
                return;
            }

            CurrentState = GameState.Playing;
            m_currentOpenMenu = MenuType.None;
            OnGameStarted?.Invoke();

            DebugBase.Log($"[{nameof(GameManager)}] Game started");
        }

        /// <summary>
        /// Opens a menu (diary, shop, settings, etc.)
        /// This changes the view but doesn't stop time - birds can still visit in the background
        /// </summary>
        public void OpenMenu(MenuType menuType)
        {
            if (CurrentState == GameState.InMinigame)
            {
                DebugBase.LogWarning($"[{nameof(GameManager)}] Cannot open menu while in minigame");
                return;
            }

            CurrentState = GameState.MenuOpen;
            m_currentOpenMenu = menuType;

            OnMenuOpened?.Invoke();
            DebugBase.Log($"[{nameof(GameManager)}] Menu opened: {menuType}");
        }

        /// <summary>
        /// Closes the current menu and returns to main view
        /// </summary>
        public void CloseMenu()
        {
            if (CurrentState != GameState.MenuOpen)
            {
                DebugBase.LogWarning($"[{nameof(GameManager)}] No menu is open");
                return;
            }

            MenuType closedMenu = m_currentOpenMenu;
            CurrentState = GameState.Playing;
            m_currentOpenMenu = MenuType.None;

            OnMenuClosed?.Invoke();
            DebugBase.Log($"[{nameof(GameManager)}] Menu closed: {closedMenu}");
        }

        /// <summary>
        /// Enters minigame state
        /// During minigames, time continues but bird spawning is paused
        /// </summary>
        public void StartMinigame()
        {
            CurrentState = GameState.InMinigame;

            if (m_birdManager != null)
            {
                m_birdManager.PauseBirdSpawning();
            }

            OnMinigameStarted?.Invoke();
            DebugBase.Log($"[{nameof(GameManager)}] Minigame started");
        }

        /// <summary>
        /// Exits minigame state and returns to main view
        /// </summary>
        public void EndMinigame()
        {
            if (CurrentState != GameState.InMinigame)
            {
                DebugBase.LogWarning($"[{nameof(GameManager)}] Not in minigame state");
                return;
            }

            CurrentState = GameState.Playing;

            if (m_birdManager != null)
            {
                m_birdManager.ResumeBirdSpawning();
            }

            OnMinigameEnded?.Invoke();
            DebugBase.Log($"[{nameof(GameManager)}] Minigame ended");
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
            return CurrentState == GameState.MenuOpen && m_currentOpenMenu == menuType;
        }

        /// <summary>
        /// Triggers a game save
        /// </summary>
        public void SaveGame()
        {
            if (m_saveManager != null)
            {
                m_saveManager.SaveGame();
                DebugBase.Log($"[{nameof(GameManager)}] Game saved", DebugCategory.General);
            }
        }

        /// <summary>
        /// Triggers a game load
        /// </summary>
        public void LoadGame()
        {
            if (m_saveManager != null)
            {
                m_saveManager.LoadGame();
                DebugBase.Log($"[{nameof(GameManager)}] Game loaded", DebugCategory.General);
            }
        }

        /// <summary>
        /// Gets the status of all managers (useful for debugging)
        /// </summary>
        public string GetManagersStatus()
        {
            string status = "=== MANAGERS STATUS ===\n";
            status += $"SaveManager: {(m_saveManager != null ? "✓" : "✗")}\n";
            status += $"BirdManager: {(m_birdManager != null ? "✓" : "✗")}\n";
            status += $"EconomyManager: {(m_economyManager != null ? "✓" : "✗")}\n";
            status += $"FriendshipManager: {(m_friendshipManager != null ? "✓" : "✗")}\n";
            status += $"DiaryManager: {(m_diaryManager != null ? "✓" : "✗")}\n";
            status += $"MenuManager: {(m_menuManager != null ? "✓" : "✗")}\n";
            status += $"EnvironmentManager: {(m_environmentManager != null ? "✓" : "✗")}\n";
            status += $"StoreManager: {(m_storeManager != null ? "✓" : "✗")}\n";
            status += $"DiaryUIManager: {(m_diaryUIManager != null ? "✓" : "✗")}\n";
            status += $"SoundManager: {(m_soundManager != null ? "✓" : "✗")}\n";
            status += $"ToastManager: {(m_toastManager != null ? "✓" : "✗")}\n";
            status += $"WindowsillManager: {(m_windowsillManager != null ? "✓" : "✗")}\n";
            status += $"Game State: {CurrentState}\n";
            status += $"Current Menu: {m_currentOpenMenu}";
            return status;
        }

        /// <summary>
        /// Checks if all critical managers are initialized and async initialization is complete.
        /// </summary>
        public bool AreAllManagersReady()
        {
            return m_initializationComplete &&
                   m_saveManager != null &&
                   m_birdManager != null && m_birdManager.IsInitialized &&
                   m_economyManager != null && m_economyManager.IsInitialized &&
                   m_friendshipManager != null && m_friendshipManager.IsInitialized &&
                   m_diaryManager != null && m_diaryManager.IsInitialized &&
                   m_menuManager != null && m_menuManager.IsInitialized &&
                   m_environmentManager != null && m_environmentManager.IsInitialized &&
                   m_storeManager != null && m_storeManager.IsInitialized &&
                   m_diaryUIManager != null && m_diaryUIManager.IsInitialized &&
                   m_soundManager != null && m_soundManager.IsInitialized &&
                   m_toastManager != null && m_toastManager.IsInitialized &&
                   m_windowsillManager != null && m_windowsillManager.IsInitialized;
        }

        private void OnApplicationQuit()
        {
            SaveGame();
            DebugBase.Log($"[{nameof(GameManager)}] Application quitting, game saved");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGame();
                DebugBase.Log($"[{nameof(GameManager)}] Application paused, game saved");
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Log Managers Status")]
        private void DebugLogManagersStatus()
        {
            DebugBase.Log(GetManagersStatus());
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
    }
}

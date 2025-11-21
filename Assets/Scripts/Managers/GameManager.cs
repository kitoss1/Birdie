using System;
using Birdie.Core;
using Birdie.Debug;
using Birdie.Save;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Central manager that maintains the overall game state and provides access to other managers.
    /// This is the core coordinator that manages all game systems.
    /// Based on the design document's architecture.
    /// Uses Dependency Injection instead of singleton pattern.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game State")]
        [SerializeField]
        private GameState m_currentState = GameState.Playing;

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

        [Header("Manager References")]
        [SerializeField]
        private BirdManager m_birdManager;

        [SerializeField]
        private EconomyManager m_economyManager;

        [SerializeField]
        private FriendshipManager m_friendshipManager;

        [SerializeField]
        private DiaryManager m_diaryManager;

        public BirdManager BirdManager => m_birdManager;
        public EconomyManager EconomyManager => m_economyManager;
        public FriendshipManager FriendshipManager => m_friendshipManager;
        public DiaryManager DiaryManager => m_diaryManager;

        private MenuType m_currentOpenMenu = MenuType.None;
        public MenuType CurrentOpenMenu => m_currentOpenMenu;

        private bool m_isInitialized = false;
        private SaveManager m_saveManager;

        public SaveManager SaveManager => m_saveManager;

        private void Awake()
        {
            InitializeManagers();
        }

        private void Start()
        {
            StartGame();
        }

        /// <summary>
        /// Initializes all manager references and injects dependencies
        /// </summary>
        private void InitializeManagers()
        {
            DebugBase.Log($"[{nameof(GameManager)}] Initializing managers...");

            InitializeSaveSystem();

            ValidateManagerReferences();

            InjectDependencies();

            m_isInitialized = true;
            DebugBase.Log($"[{nameof(GameManager)}] Managers initialized successfully");
        }

        /// <summary>
        /// Initializes the save system and loads game data.
        /// </summary>
        private void InitializeSaveSystem()
        {
            m_saveManager = new SaveManager();
            m_saveManager.Initialize();
            m_saveManager.LoadGame();

            DebugBase.Log($"[{nameof(GameManager)}] Save system initialized", DebugCategory.General);
        }

        /// <summary>
        /// Validates that all required managers are assigned in the inspector
        /// </summary>
        private void ValidateManagerReferences()
        {
            if (m_birdManager == null)
            {
                DebugBase.LogWarning($"[{nameof(GameManager)}] BirdManager is not assigned!");
            }

            if (m_economyManager == null)
            {
                DebugBase.LogWarning($"[{nameof(GameManager)}] EconomyManager is not assigned!");
            }

            if (m_friendshipManager == null)
            {
                DebugBase.LogWarning($"[{nameof(GameManager)}] FriendshipManager is not assigned!");
            }

            if (m_diaryManager == null)
            {
                DebugBase.LogWarning($"[{nameof(GameManager)}] DiaryManager is not assigned!");
            }
        }

        /// <summary>
        /// Injects this GameManager into other managers that need it
        /// Each manager should have an Initialize(GameManager) or similar method
        /// </summary>
        private void InjectDependencies()
        {
            if (m_birdManager != null)
            {
                m_birdManager.Initialize(this);
            }

            if (m_economyManager != null)
            {
                m_economyManager.Initialize(this);
            }

            if (m_friendshipManager != null)
            {
                m_friendshipManager.Initialize(this);
            }

            if (m_diaryManager != null)
            {
                m_diaryManager.Initialize(this);
                m_diaryManager.SetSaveManager(m_saveManager);
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
            status += $"Game State: {CurrentState}\n";
            status += $"Current Menu: {m_currentOpenMenu}";
            return status;
        }

        /// <summary>
        /// Checks if all critical managers are initialized
        /// </summary>
        public bool AreAllManagersReady()
        {
            return m_saveManager != null &&
                   m_birdManager != null &&
                   m_economyManager != null &&
                   m_friendshipManager != null &&
                   m_diaryManager != null;
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

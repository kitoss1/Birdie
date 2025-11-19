using System;
using Birdie.Core;
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
                    Debug.Log($"[{nameof(GameManager)}] State changed: {previousState} → {m_currentState}");
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

        public BirdManager BirdManager => m_birdManager;
        public EconomyManager EconomyManager => m_economyManager;
        public FriendshipManager FriendshipManager => m_friendshipManager;

        private MenuType m_currentOpenMenu = MenuType.None;
        public MenuType CurrentOpenMenu => m_currentOpenMenu;

        private bool m_isInitialized = false;

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
            Debug.Log($"[{nameof(GameManager)}] Initializing managers...");

            ValidateManagerReferences();

            InjectDependencies();

            m_isInitialized = true;
            Debug.Log($"[{nameof(GameManager)}] Managers initialized successfully");
        }

        /// <summary>
        /// Validates that all required managers are assigned in the inspector
        /// </summary>
        private void ValidateManagerReferences()
        {
            if (m_birdManager == null)
            {
                Debug.LogWarning($"[{nameof(GameManager)}] BirdManager is not assigned!");
            }

            if (m_economyManager == null)
            {
                Debug.LogWarning($"[{nameof(GameManager)}] EconomyManager is not assigned!");
            }

            if (m_friendshipManager == null)
            {
                Debug.LogWarning($"[{nameof(GameManager)}] FriendshipManager is not assigned!");
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
        }

        /// <summary>
        /// Starts the game (initial state)
        /// </summary>
        public void StartGame()
        {
            if (CurrentState == GameState.Playing)
            {
                Debug.LogWarning($"[{nameof(GameManager)}] Game is already playing");
                return;
            }

            CurrentState = GameState.Playing;
            m_currentOpenMenu = MenuType.None;
            OnGameStarted?.Invoke();

            Debug.Log($"[{nameof(GameManager)}] Game started");
        }

        /// <summary>
        /// Opens a menu (diary, shop, settings, etc.)
        /// This changes the view but doesn't stop time - birds can still visit in the background
        /// </summary>
        public void OpenMenu(MenuType menuType)
        {
            if (CurrentState == GameState.InMinigame)
            {
                Debug.LogWarning($"[{nameof(GameManager)}] Cannot open menu while in minigame");
                return;
            }

            CurrentState = GameState.MenuOpen;
            m_currentOpenMenu = menuType;

            OnMenuOpened?.Invoke();
            Debug.Log($"[{nameof(GameManager)}] Menu opened: {menuType}");
        }

        /// <summary>
        /// Closes the current menu and returns to main view
        /// </summary>
        public void CloseMenu()
        {
            if (CurrentState != GameState.MenuOpen)
            {
                Debug.LogWarning($"[{nameof(GameManager)}] No menu is open");
                return;
            }

            MenuType closedMenu = m_currentOpenMenu;
            CurrentState = GameState.Playing;
            m_currentOpenMenu = MenuType.None;

            OnMenuClosed?.Invoke();
            Debug.Log($"[{nameof(GameManager)}] Menu closed: {closedMenu}");
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
            Debug.Log($"[{nameof(GameManager)}] Minigame started");
        }

        /// <summary>
        /// Exits minigame state and returns to main view
        /// </summary>
        public void EndMinigame()
        {
            if (CurrentState != GameState.InMinigame)
            {
                Debug.LogWarning($"[{nameof(GameManager)}] Not in minigame state");
                return;
            }

            CurrentState = GameState.Playing;

            if (m_birdManager != null)
            {
                m_birdManager.ResumeBirdSpawning();
            }

            OnMinigameEnded?.Invoke();
            Debug.Log($"[{nameof(GameManager)}] Minigame ended");
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
            // TODO: Implement save system
        }

        /// <summary>
        /// Triggers a game load
        /// </summary>
        public void LoadGame()
        {
            // TODO: Implement load system
        }

        /// <summary>
        /// Gets the status of all managers (useful for debugging)
        /// </summary>
        public string GetManagersStatus()
        {
            string status = "=== MANAGERS STATUS ===\n";
            status += $"BirdManager: {(m_birdManager != null ? "✓" : "✗")}\n";
            status += $"EconomyManager: {(m_economyManager != null ? "✓" : "✗")}\n";
            status += $"FriendshipManager: {(m_friendshipManager != null ? "✓" : "✗")}\n";
            status += $"Game State: {CurrentState}\n";
            status += $"Current Menu: {m_currentOpenMenu}";
            return status;
        }

        /// <summary>
        /// Checks if all critical managers are initialized
        /// </summary>
        public bool AreAllManagersReady()
        {
            return m_birdManager != null &&
                   m_economyManager != null &&
                   m_friendshipManager != null;
        }

        private void OnApplicationQuit()
        {
            SaveGame();
            Debug.Log($"[{nameof(GameManager)}] Application quitting, game saved");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGame();
                Debug.Log($"[{nameof(GameManager)}] Application paused, game saved");
            }
        }

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
    }
}

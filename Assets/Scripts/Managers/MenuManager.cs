using System.Collections.Generic;
using Birdie.Core;
using Birdie.Debug;
using Birdie.UI;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages menu UI panels and handles menu transitions.
    /// Coordinates with GameManager for state management and BaseMenuButton for user input.
    /// Settings menu is treated as an overlay that can open on top of other menus.
    /// </summary>
    public class MenuManager : BaseManager
    {
        [Header("Menu Panels")]
        [SerializeField]
        [Tooltip("Parent canvas/transform containing all menu panels")]
        private Transform m_menuContainer;

        [SerializeField]
        [Tooltip("Diary menu panel")]
        private GameObject m_diaryPanel;

        [SerializeField]
        [Tooltip("Shop menu panel")]
        private GameObject m_shopPanel;

        [SerializeField]
        [Tooltip("Settings menu panel (overlay)")]
        private GameObject m_settingsPanel;

        [SerializeField]
        [Tooltip("Tutorial menu panel")]
        private GameObject m_tutorialPanel;

        /*[Header("Animation Settings")]
        [SerializeField]
        [Tooltip("Duration for menu fade in/out animations")]
        private float m_animationDuration = 0.3f;*/

        private Dictionary<MenuType, GameObject> m_menuPanels;
        private MenuType m_currentOpenMenu = MenuType.None;
        private bool m_isSettingsOpen = false;

        public override void Initialize()
        {
            base.Initialize();

            InitializeMenuPanels();
            SubscribeToEvents();
            RegisterMenuButtons();

            DebugBase.Log($"[{nameof(MenuManager)}] Menu system initialized");
        }

        /// <summary>
        /// Initializes the menu panels dictionary and ensures all panels are hidden.
        /// </summary>
        private void InitializeMenuPanels()
        {
            m_menuPanels = new Dictionary<MenuType, GameObject>
            {
                { MenuType.Diary, m_diaryPanel },
                { MenuType.Shop, m_shopPanel },
                { MenuType.Settings, m_settingsPanel },
                { MenuType.Tutorial, m_tutorialPanel }
            };

            HideAllMenus();
            DebugBase.Log($"[{nameof(MenuManager)}] Menu panels initialized");
        }

        /// <summary>
        /// Subscribes to GameManager events for menu state tracking.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMenuOpened += OnMenuOpenedFromGameManager;
                GameManager.Instance.OnMenuClosed += OnMenuClosedFromGameManager;
            }
        }

        /// <summary>
        /// Finds all BaseMenuButton components in the scene and subscribes to their events.
        /// </summary>
        private void RegisterMenuButtons()
        {
            BaseMenuButton[] menuButtons = FindObjectsByType<BaseMenuButton>(FindObjectsSortMode.None);
            foreach (BaseMenuButton button in menuButtons)
            {
                button.OnMenuButtonClicked += OnMenuButtonClicked;
                DebugBase.Log($"[{nameof(MenuManager)}] Registered button for menu: {button.MenuType}");
            }

            DebugBase.Log($"[{nameof(MenuManager)}] Registered {menuButtons.Length} menu buttons");
        }

        /// <summary>
        /// Called when a menu button is clicked.
        /// If the menu is already open, it will be closed (toggle behavior).
        /// </summary>
        private void OnMenuButtonClicked(MenuType menuType)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            if (menuType == MenuType.None)
            {
                DebugBase.LogWarning($"[{nameof(MenuManager)}] Cannot open menu of type None");
                return;
            }

            if (IsOverlayMenu(menuType))
            {
                if (IsMenuOpen(menuType))
                {
                    CloseOverlayMenu(menuType);
                }
                else
                {
                    OpenOverlayMenu(menuType);
                }
            }
            else
            {
                if (m_currentOpenMenu == menuType)
                {
                    CloseCurrentMenu();
                }
                else
                {
                    OpenMenu(menuType);
                }
            }
        }

        /// <summary>
        /// Checks if a menu type is an overlay menu (like Settings).
        /// </summary>
        private bool IsOverlayMenu(MenuType menuType)
        {
            return menuType == MenuType.Settings;
        }

        /// <summary>
        /// Opens a specific menu panel and updates GameManager state.
        /// Closes any previously open main menu.
        /// </summary>
        public void OpenMenu(MenuType menuType)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            if (menuType == MenuType.None)
            {
                DebugBase.LogWarning($"[{nameof(MenuManager)}] Cannot open menu of type None");
                return;
            }

            if (IsOverlayMenu(menuType))
            {
                DebugBase.LogWarning($"[{nameof(MenuManager)}] {menuType} is an overlay menu. Use OpenOverlayMenu() instead");
                OpenOverlayMenu(menuType);
                return;
            }

            if (!m_menuPanels.ContainsKey(menuType))
            {
                DebugBase.LogError($"[{nameof(MenuManager)}] Menu panel not found for type: {menuType}");
                return;
            }

            GameObject menuPanel = m_menuPanels[menuType];
            if (menuPanel == null)
            {
                DebugBase.LogError($"[{nameof(MenuManager)}] Menu panel GameObject is null for type: {menuType}");
                return;
            }

            if (m_currentOpenMenu != MenuType.None)
            {
                DebugBase.Log($"[{nameof(MenuManager)}] Closing current menu before opening new one");
                CloseMainMenu();
            }

            ShowMenuPanel(menuType, menuPanel);
            m_currentOpenMenu = menuType;

            GameManager.Instance.OpenMenu(menuType);

            DebugBase.Log($"[{nameof(MenuManager)}] Opened menu: {menuType}");
        }

        /// <summary>
        /// Opens an overlay menu (like Settings) on top of the current menu without closing it.
        /// </summary>
        public void OpenOverlayMenu(MenuType menuType)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            if (!IsOverlayMenu(menuType))
            {
                DebugBase.LogWarning($"[{nameof(MenuManager)}] {menuType} is not an overlay menu");
                return;
            }

            if (!m_menuPanels.ContainsKey(menuType))
            {
                DebugBase.LogError($"[{nameof(MenuManager)}] Menu panel not found for type: {menuType}");
                return;
            }

            GameObject menuPanel = m_menuPanels[menuType];
            if (menuPanel == null)
            {
                DebugBase.LogError($"[{nameof(MenuManager)}] Menu panel GameObject is null for type: {menuType}");
                return;
            }

            if (menuType == MenuType.Settings)
            {
                if (m_isSettingsOpen)
                {
                    DebugBase.LogWarning($"[{nameof(MenuManager)}] Settings menu is already open");
                    return;
                }

                ShowMenuPanel(menuType, menuPanel);
                m_isSettingsOpen = true;
                DebugBase.Log($"[{nameof(MenuManager)}] Opened overlay menu: {menuType}");
            }
        }

        /// <summary>
        /// Closes an overlay menu (like Settings) and returns to the previous state.
        /// </summary>
        public void CloseOverlayMenu(MenuType menuType)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            if (!IsOverlayMenu(menuType))
            {
                DebugBase.LogWarning($"[{nameof(MenuManager)}] {menuType} is not an overlay menu");
                return;
            }

            if (menuType == MenuType.Settings)
            {
                if (!m_isSettingsOpen)
                {
                    DebugBase.LogWarning($"[{nameof(MenuManager)}] Settings menu is not open");
                    return;
                }

                if (m_menuPanels.ContainsKey(menuType) && m_menuPanels[menuType] != null)
                {
                    HideMenuPanel(menuType, m_menuPanels[menuType]);
                }

                m_isSettingsOpen = false;
                DebugBase.Log($"[{nameof(MenuManager)}] Closed overlay menu: {menuType}");
            }
        }

        /// <summary>
        /// Closes the currently open main menu (not overlay menus).
        /// </summary>
        private void CloseMainMenu()
        {
            if (m_currentOpenMenu == MenuType.None)
            {
                return;
            }

            MenuType menuToClose = m_currentOpenMenu;

            if (m_menuPanels.ContainsKey(menuToClose) && m_menuPanels[menuToClose] != null)
            {
                HideMenuPanel(menuToClose, m_menuPanels[menuToClose]);
            }

            m_currentOpenMenu = MenuType.None;
            DebugBase.Log($"[{nameof(MenuManager)}] Closed main menu: {menuToClose}");
        }

        /// <summary>
        /// Closes the currently open menu and updates GameManager state.
        /// If an overlay menu is open, closes that first. Otherwise closes the main menu.
        /// </summary>
        public void CloseCurrentMenu()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            if (m_isSettingsOpen)
            {
                CloseOverlayMenu(MenuType.Settings);
                return;
            }

            if (m_currentOpenMenu == MenuType.None)
            {
                DebugBase.LogWarning($"[{nameof(MenuManager)}] No menu is currently open");
                return;
            }

            CloseMainMenu();
            GameManager.Instance.CloseMenu();

            DebugBase.Log($"[{nameof(MenuManager)}] Closed current menu");
        }

        /// <summary>
        /// Shows a specific menu panel with optional animation.
        /// Override this method to add custom transitions.
        /// </summary>
        protected virtual void ShowMenuPanel(MenuType menuType, GameObject menuPanel)
        {
            menuPanel.SetActive(true);

            // TODO: Add DOTween animation here when needed
            // Example:
            // CanvasGroup canvasGroup = menuPanel.GetComponent<CanvasGroup>();
            // if (canvasGroup != null)
            // {
            //     canvasGroup.alpha = 0f;
            //     canvasGroup.DOFade(1f, m_animationDuration);
            // }

            DebugBase.Log($"[{nameof(MenuManager)}] Showing panel for menu: {menuType}");
        }

        /// <summary>
        /// Hides a specific menu panel with optional animation.
        /// Override this method to add custom transitions.
        /// </summary>
        protected virtual void HideMenuPanel(MenuType menuType, GameObject menuPanel)
        {
            // TODO: Add DOTween animation here when needed
            // Example:
            // CanvasGroup canvasGroup = menuPanel.GetComponent<CanvasGroup>();
            // if (canvasGroup != null)
            // {
            //     canvasGroup.DOFade(0f, m_animationDuration).OnComplete(() => menuPanel.SetActive(false));
            // }
            // else
            // {
            //     menuPanel.SetActive(false);
            // }

            menuPanel.SetActive(false);
            DebugBase.Log($"[{nameof(MenuManager)}] Hiding panel for menu: {menuType}");
        }

        /// <summary>
        /// Hides all menu panels. Useful for initialization or reset.
        /// </summary>
        private void HideAllMenus()
        {
            if (m_diaryPanel != null)
            {
                m_diaryPanel.SetActive(false);
            }

            if (m_shopPanel != null)
            {
                m_shopPanel.SetActive(false);
            }

            if (m_settingsPanel != null)
            {
                m_settingsPanel.SetActive(false);
            }

            if (m_tutorialPanel != null)
            {
                m_tutorialPanel.SetActive(false);
            }

            DebugBase.Log($"[{nameof(MenuManager)}] All menu panels hidden");
        }

        /// <summary>
        /// Called when GameManager opens a menu (external trigger).
        /// </summary>
        private void OnMenuOpenedFromGameManager()
        {
            // GameManager has already updated its state
            // This is a hook for additional logic if needed
            DebugBase.Log($"[{nameof(MenuManager)}] Menu opened event received from GameManager");
        }

        /// <summary>
        /// Called when GameManager closes a menu (external trigger).
        /// </summary>
        private void OnMenuClosedFromGameManager()
        {
            // GameManager has already updated its state
            // This is a hook for additional logic if needed
            DebugBase.Log($"[{nameof(MenuManager)}] Menu closed event received from GameManager");
        }

        /// <summary>
        /// Checks if a specific menu is currently open.
        /// </summary>
        public bool IsMenuOpen(MenuType menuType)
        {
            if (menuType == MenuType.Settings)
            {
                return m_isSettingsOpen;
            }

            return m_currentOpenMenu == menuType;
        }

        /// <summary>
        /// Gets the currently open main menu type (excludes overlay menus).
        /// </summary>
        public MenuType CurrentOpenMenu => m_currentOpenMenu;

        /// <summary>
        /// Checks if Settings overlay is currently open.
        /// </summary>
        public bool IsSettingsOpen => m_isSettingsOpen;

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMenuOpened -= OnMenuOpenedFromGameManager;
                GameManager.Instance.OnMenuClosed -= OnMenuClosedFromGameManager;
            }

            BaseMenuButton[] menuButtons = FindObjectsByType<BaseMenuButton>(FindObjectsSortMode.None);
            foreach (BaseMenuButton button in menuButtons)
            {
                button.OnMenuButtonClicked -= OnMenuButtonClicked;
            }
        }

#if UNITY_EDITOR
        [DebugCommand("CloseMenu", "UI")]
        [ContextMenu("Close Current Menu")]
        private void DebugCloseMenu()
        {
            CloseCurrentMenu();
        }

        [DebugCommand("OpenDiary", "UI")]
        [ContextMenu("Open Diary")]
        private void DebugOpenDiary()
        {
            OpenMenu(MenuType.Diary);
        }

        [DebugCommand("OpenShop", "UI")]
        [ContextMenu("Open Shop")]
        private void DebugOpenShop()
        {
            OpenMenu(MenuType.Shop);
        }

        [DebugCommand("OpenSettings(Overlay)", "UI")]
        [ContextMenu("Open Settings (Overlay)")]
        private void DebugOpenSettings()
        {
            OpenOverlayMenu(MenuType.Settings);
        }
        
        [DebugCommand("CloseSettings(Overlay)", "UI")]
        [ContextMenu("Close Settings (Overlay)")]
        private void DebugCloseSettings()
        {
            CloseOverlayMenu(MenuType.Settings);
        }
#endif
    }
}

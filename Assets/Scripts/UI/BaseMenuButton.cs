using System;
using Birdie.Core;
using Birdie.Debug;
using Birdie.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// Base class for all buttons that open menus in the game.
    /// Can be used directly or extended for specific menu behavior.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BaseMenuButton : MonoBehaviour
    {
        [Header("Menu Configuration")]
        [SerializeField]
        [Tooltip("The type of menu this button opens")]
        protected MenuType m_menuType;

        [Header("Optional: Audio")]
        [SerializeField]
        [Tooltip("Sound to play when button is clicked")]
        protected AudioClip m_clickSound;

        protected Button m_button;
        protected bool m_isInitialized = false;

        /// <summary>
        /// Event fired when this button is clicked. Passes the MenuType.
        /// A MenuManager can subscribe to this to handle menu opening.
        /// </summary>
        public event Action<MenuType> OnMenuButtonClicked;

        protected virtual void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the button component and sets up the click listener.
        /// </summary>
        protected virtual void Initialize()
        {
            if (m_isInitialized)
            {
                DebugBase.LogWarning($"[{GetType().Name}] Already initialized!");
                return;
            }

            m_button = GetComponent<Button>();
            if (m_button == null)
            {
                DebugBase.LogError($"[{GetType().Name}] Button component not found!");
                return;
            }

            m_button.onClick.AddListener(OnButtonClicked);
            m_isInitialized = true;

            DebugBase.Log($"[{GetType().Name}] Initialized for menu type: {m_menuType}");
        }

        /// <summary>
        /// Called when the button is clicked. Handles common logic before opening the menu.
        /// </summary>
        private void OnButtonClicked()
        {
            if (!CanOpenMenu())
            {
                DebugBase.LogWarning($"[{GetType().Name}] Cannot open menu {m_menuType} at this time");
                return;
            }

            PlayClickSound();
            OnBeforeMenuOpen();
            OpenMenu();
            OnAfterMenuOpen();
        }

        /// <summary>
        /// Checks if the menu can be opened. Override to add specific conditions.
        /// </summary>
        protected virtual bool CanOpenMenu()
        {
            return m_isInitialized && m_button != null && m_button.interactable;
        }

        /// <summary>
        /// Plays the button click sound if configured.
        /// </summary>
        protected virtual void PlayClickSound()
        {
            if (m_clickSound == null)
            {
                return;
            }

            if (GameManager.Instance?.SoundManager != null)
            {
                GameManager.Instance.SoundManager.PlaySFX(m_clickSound);
            }
            else
            {
                DebugBase.LogWarning($"[{GetType().Name}] SoundManager not available, cannot play click sound");
            }
        }

        /// <summary>
        /// Called before the menu is opened. Override for custom pre-open logic.
        /// </summary>
        protected virtual void OnBeforeMenuOpen()
        {
            // Default: nothing to do
        }

        /// <summary>
        /// Opens the menu. Default implementation fires an event for a MenuManager to handle.
        /// Override for custom menu opening logic.
        /// </summary>
        protected virtual void OpenMenu()
        {
            DebugBase.Log($"[{GetType().Name}] Opening menu: {m_menuType}");
            OnMenuButtonClicked?.Invoke(m_menuType);
        }

        /// <summary>
        /// Called after the menu is opened. Override for custom post-open logic.
        /// </summary>
        protected virtual void OnAfterMenuOpen()
        {
            // Default: nothing to do
        }

        protected virtual void OnDestroy()
        {
            if (m_button != null)
            {
                m_button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// Gets the menu type this button opens.
        /// </summary>
        public MenuType MenuType => m_menuType;

        /// <summary>
        /// Gets the button component.
        /// </summary>
        public Button Button => m_button;
    }
}

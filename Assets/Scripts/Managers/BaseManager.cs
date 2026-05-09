using Birdie.Debug;
using Birdie.Save;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Base class for all manager classes in the game.
    /// Provides a standard initialization pattern.
    /// All managers should inherit from this class.
    /// Managers can access GameManager via GameManager.Instance.
    /// </summary>
    public abstract class BaseManager : MonoBehaviour
    {
        protected bool m_isInitialized = false;
        protected SaveManager m_saveManager;

        /// <summary>
        /// Initializes the manager.
        /// This is called by GameManager during its initialization phase.
        /// Override this in child classes to add custom initialization logic.
        /// </summary>
        public virtual void Initialize(SaveManager saveManager = null)
        {
            if (m_isInitialized)
            {
                DebugBase.LogWarning($"[{GetType().Name}] Already initialized!");
                return;
            }

            m_isInitialized = true;
            m_saveManager = saveManager;

            DebugBase.Log($"[{GetType().Name}] Initialized successfully");
        }

        /// <summary>
        /// Checks if the manager has been properly initialized
        /// </summary>
        public bool IsInitialized => m_isInitialized;

        /// <summary>
        /// Protected helper to ensure manager is initialized before use
        /// </summary>
        protected bool EnsureInitialized()
        {
            if (!m_isInitialized)
            {
                DebugBase.LogError($"[{GetType().Name}] Not initialized! Call Initialize() first.");
                return false;
            }

            return true;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Birdie.Debug
{
    /// <summary>
    /// Singleton manager that controls which debug log categories are enabled.
    /// Can be configured in the Unity Inspector or at runtime.
    /// </summary>
    public class DebugManager : MonoBehaviour
    {
        private static DebugManager s_instance;

        [Header("Debug Categories")]
        [SerializeField]
        [Tooltip("Enable/disable logging for each category")]
        private List<DebugCategoryToggle> m_categoryToggles = new List<DebugCategoryToggle>();

        [Header("Global Settings")]
        [SerializeField]
        [Tooltip("Master toggle for all debug logging")]
        private bool m_enableAllLogging = true;

        private Dictionary<DebugCategory, bool> m_categoryStates = new Dictionary<DebugCategory, bool>();

        /// <summary>
        /// Singleton instance of the DebugManager.
        /// </summary>
        public static DebugManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindFirstObjectByType<DebugManager>();
                    if (s_instance == null)
                    {
                        GameObject debugManagerObject = new GameObject("DebugManager");
                        s_instance = debugManagerObject.AddComponent<DebugManager>();
                        DontDestroyOnLoad(debugManagerObject);
                    }
                }

                return s_instance;
            }
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            InitializeCategoryStates();
        }

        private void InitializeCategoryStates()
        {
            m_categoryStates.Clear();

            foreach (DebugCategoryToggle toggle in m_categoryToggles)
            {
                m_categoryStates[toggle.Category] = toggle.IsEnabled;
            }
        }

        /// <summary>
        /// Checks if logging is enabled for a specific category.
        /// </summary>
        /// <param name="category">The debug category to check.</param>
        /// <returns>True if logging is enabled for this category.</returns>
        public bool IsCategoryEnabled(DebugCategory category)
        {
            if (!m_enableAllLogging)
            {
                return false;
            }

            if (m_categoryStates.TryGetValue(category, out bool isEnabled))
            {
                return isEnabled;
            }

            return false;
        }

        /// <summary>
        /// Enables or disables logging for a specific category at runtime.
        /// </summary>
        /// <param name="category">The debug category to modify.</param>
        /// <param name="enabled">Whether to enable or disable this category.</param>
        public void SetCategoryEnabled(DebugCategory category, bool enabled)
        {
            m_categoryStates[category] = enabled;

            for (int i = 0; i < m_categoryToggles.Count; i++)
            {
                if (m_categoryToggles[i].Category == category)
                {
                    m_categoryToggles[i] = new DebugCategoryToggle(category, enabled);
                    break;
                }
            }
        }

        /// <summary>
        /// Enables or disables all debug logging.
        /// </summary>
        /// <param name="enabled">Whether to enable all logging.</param>
        public void SetAllLoggingEnabled(bool enabled)
        {
            m_enableAllLogging = enabled;
        }

        /// <summary>
        /// Gets the current state of all logging.
        /// </summary>
        /// <returns>True if all logging is enabled.</returns>
        public bool IsAllLoggingEnabled()
        {
            return m_enableAllLogging;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            InitializeCategoryStates();
        }
#endif
    }

    /// <summary>
    /// Serializable class to represent a debug category toggle in the Inspector.
    /// </summary>
    [System.Serializable]
    public struct DebugCategoryToggle
    {
        [SerializeField]
        private DebugCategory m_category;

        [SerializeField]
        private bool m_isEnabled;

        public DebugCategoryToggle(DebugCategory category, bool isEnabled)
        {
            m_category = category;
            m_isEnabled = isEnabled;
        }

        public DebugCategory Category => m_category;
        public bool IsEnabled => m_isEnabled;
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.Debug
{
    /// <summary>
    /// Dynamically generates a debug menu with buttons for methods marked with DebugCommandAttribute.
    /// The menu can be toggled on/off with a button and automatically discovers test methods at runtime.
    /// </summary>
    public sealed class DebugMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        [Tooltip("The button that toggles the debug menu visibility")]
        private Button m_toggleButton;

        [SerializeField]
        [Tooltip("The parent transform where menu content will be instantiated")]
        private Transform m_menuContent;

        [SerializeField]
        [Tooltip("The game object containing the menu panel")]
        private GameObject m_menuPanel;

        [Header("Button Settings")]
        [SerializeField]
        [Tooltip("Prefab for debug command buttons")]
        private DebugButton m_buttonPrefab;

        private bool m_isMenuVisible = false;
        private readonly List<DebugCommandInfo> m_debugCommands = new List<DebugCommandInfo>();

        private void Start()
        {
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            if (m_toggleButton == null)
            {
                DebugBase.LogError($"[{nameof(DebugMenu)}] Toggle button is not assigned!");
                return;
            }

            if (m_menuContent == null)
            {
                DebugBase.LogError($"[{nameof(DebugMenu)}] Menu content transform is not assigned!");
                return;
            }

            if (m_menuPanel == null)
            {
                DebugBase.LogError($"[{nameof(DebugMenu)}] Menu panel is not assigned!");
                return;
            }

            m_toggleButton.onClick.AddListener(ToggleMenu);
            m_menuPanel.SetActive(false);

            DiscoverDebugCommands();
            CreateButtonsForCommands();
        }

        private void DiscoverDebugCommands()
        {
            m_debugCommands.Clear();

            // Get all assemblies in the current domain
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                // Skip system assemblies for performance
                if (assembly.FullName.StartsWith("Unity") ||
                    assembly.FullName.StartsWith("System") ||
                    assembly.FullName.StartsWith("mscorlib"))
                {
                    continue;
                }

                try
                {
                    // Get all types in the assembly
                    Type[] types = assembly.GetTypes();

                    foreach (Type type in types)
                    {
                        // Get all methods in the type
                        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                        foreach (MethodInfo method in methods)
                        {
                            // Check if method has DebugCommandAttribute
                            DebugCommandAttribute attribute = method.GetCustomAttribute<DebugCommandAttribute>();

                            if (attribute != null)
                            {
                                // Validate method signature (should have no parameters)
                                if (method.GetParameters().Length == 0)
                                {
                                    m_debugCommands.Add(new DebugCommandInfo(type, method, attribute));
                                }
                                else
                                {
                                    DebugBase.LogWarning($"[{nameof(DebugMenu)}] Method {type.Name}.{method.Name} has DebugCommandAttribute but has parameters. Debug commands must be parameterless.", DebugCategory.Debug);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Some assemblies might throw exceptions when trying to get types
                    DebugBase.LogWarning($"[{nameof(DebugMenu)}] Could not load types from assembly {assembly.FullName}: {ex.Message}", DebugCategory.Debug);
                }
            }

            // Sort commands by category and then by display name
            m_debugCommands.Sort((a, b) =>
            {
                int categoryComparison = string.Compare(a.Category, b.Category, StringComparison.Ordinal);
                return categoryComparison != 0 ? categoryComparison : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
            });

            DebugBase.Log($"[{nameof(DebugMenu)}] Found {m_debugCommands.Count} debug commands", DebugCategory.Debug);
        }

        private void CreateButtonsForCommands()
        {
            if (m_buttonPrefab == null)
            {
                DebugBase.LogError($"[{nameof(DebugMenu)}] There is no prefab for debug button", DebugCategory.Debug);
            }
            else
            {
                CreateButtonsFromPrefab();
            }
        }

        private void CreateButtonsFromPrefab()
        {
            foreach (DebugCommandInfo commandInfo in m_debugCommands)
            {
                DebugButton button = Instantiate(m_buttonPrefab, m_menuContent);
                button.name = commandInfo.DisplayName;
                TMP_Text buttonText = button.ButtonText;
                if (buttonText != null)
                {
                    buttonText.text = commandInfo.DisplayName;
                }

                button.ButtonElement.onClick.AddListener(() => ExecuteCommand(commandInfo));

                DebugBase.Log($"[{nameof(DebugMenu)}] Created button for {commandInfo.DisplayName}", DebugCategory.Debug);
            }
        }

        private void ToggleMenu()
        {
            m_isMenuVisible = !m_isMenuVisible;
            m_menuPanel.SetActive(m_isMenuVisible);

            DebugBase.Log($"[{nameof(DebugMenu)}] Menu {(m_isMenuVisible ? "opened" : "closed")}", DebugCategory.Debug);
        }

        private void ExecuteCommand(DebugCommandInfo commandInfo)
        {
            try
            {
                DebugBase.Log($"[{nameof(DebugMenu)}] Executing command: {commandInfo.DisplayName}", DebugCategory.Debug);

                if (commandInfo.Method.IsStatic)
                {
                    commandInfo.Method.Invoke(null, null);
                }
                else
                {
                    object instance = FindFirstObjectByType(commandInfo.Type);

                    if (instance == null)
                    {
                        DebugBase.LogWarning($"[{nameof(DebugMenu)}] No instance of {commandInfo.Type.Name} found in scene. Cannot execute {commandInfo.DisplayName}.", DebugCategory.Debug);
                        return;
                    }

                    commandInfo.Method.Invoke(instance, null);
                }
            }
            catch (Exception ex)
            {
                DebugBase.LogError($"[{nameof(DebugMenu)}] Error executing command {commandInfo.DisplayName}: {ex.Message}");
                DebugBase.LogException(ex);
            }
        }

        private void OnDestroy()
        {
            if (m_toggleButton != null)
            {
                m_toggleButton.onClick.RemoveAllListeners();
            }
        }

        private sealed class DebugCommandInfo
        {
            private readonly Type m_type;
            private readonly MethodInfo m_method;
            private readonly DebugCommandAttribute m_attribute;

            public DebugCommandInfo(Type type, MethodInfo method, DebugCommandAttribute attribute)
            {
                m_type = type;
                m_method = method;
                m_attribute = attribute;
            }

            public Type Type => m_type;
            public MethodInfo Method => m_method;

            public string DisplayName => string.IsNullOrEmpty(m_attribute.DisplayName) ? m_method.Name : m_attribute.DisplayName;

            public string Category => m_attribute.Category;
        }
    }
}

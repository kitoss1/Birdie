using System;

namespace Birdie.Debug
{
    /// <summary>
    /// Attribute to mark methods as debug commands that should appear in the debug menu.
    /// Methods marked with this attribute will automatically generate buttons in the debug UI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class DebugCommandAttribute : Attribute
    {
        private readonly string m_displayName;
        private readonly string m_category;

        /// <summary>
        /// Creates a debug command attribute with optional display name and category.
        /// </summary>
        /// <param name="displayName">The name to display on the button. If null, uses the method name.</param>
        /// <param name="category">Optional category to group related commands.</param>
        public DebugCommandAttribute(string displayName = null, string category = "General")
        {
            m_displayName = displayName;
            m_category = category;
        }

        /// <summary>
        /// Gets the display name for this debug command.
        /// </summary>
        public string DisplayName => m_displayName;

        /// <summary>
        /// Gets the category for this debug command.
        /// </summary>
        public string Category => m_category;
    }
}

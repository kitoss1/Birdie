using UnityEngine;

namespace Birdie.Debug
{
    /// <summary>
    /// Static wrapper around Unity's Debug class that respects DebugManager category settings.
    /// Use this instead of Debug.Log directly to allow category-based filtering.
    /// </summary>
    public static class DebugBase
    {
        /// <summary>
        /// Logs a message to the console if the specified category is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="category">The debug category for this log.</param>
        public static void Log(string message, DebugCategory category = DebugCategory.General)
        {
            if (DebugManager.Instance.IsCategoryEnabled(category))
            {
                UnityEngine.Debug.Log(message);
            }
        }

        /// <summary>
        /// Logs a message with a context object if the specified category is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="context">The Unity object this log is related to.</param>
        /// <param name="category">The debug category for this log.</param>
        public static void Log(string message, Object context, DebugCategory category = DebugCategory.General)
        {
            if (DebugManager.Instance.IsCategoryEnabled(category))
            {
                UnityEngine.Debug.Log(message, context);
            }
        }

        /// <summary>
        /// Logs a warning to the console if the specified category is enabled.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <param name="category">The debug category for this log.</param>
        public static void LogWarning(string message, DebugCategory category = DebugCategory.General)
        {
            if (DebugManager.Instance.IsCategoryEnabled(category))
            {
                UnityEngine.Debug.LogWarning(message);
            }
        }

        /// <summary>
        /// Logs a warning with a context object if the specified category is enabled.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <param name="context">The Unity object this log is related to.</param>
        /// <param name="category">The debug category for this log.</param>
        public static void LogWarning(string message, Object context, DebugCategory category = DebugCategory.General)
        {
            if (DebugManager.Instance.IsCategoryEnabled(category))
            {
                UnityEngine.Debug.LogWarning(message, context);
            }
        }

        /// <summary>
        /// Logs an error to the console. Errors are always logged regardless of category settings.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }

        /// <summary>
        /// Logs an error with a context object. Errors are always logged regardless of category settings.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="context">The Unity object this log is related to.</param>
        /// <param name="category"></param>
        public static void LogError(string message, Object context, DebugCategory category = DebugCategory.General)
        {
            UnityEngine.Debug.LogError(message, context);
        }

        /// <summary>
        /// Logs an exception. Exceptions are always logged regardless of category settings.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        public static void LogException(System.Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
        }

        /// <summary>
        /// Logs an exception with a context object. Exceptions are always logged regardless of category settings.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="context">The Unity object this log is related to.</param>
        public static void LogException(System.Exception exception, Object context)
        {
            UnityEngine.Debug.LogException(exception, context);
        }
    }
}

using Birdie.Debug;
using Birdie.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages the loading screen and coordinates with GameManager initialization.
    /// Ensures the loading screen is visible during initialization.
    /// </summary>
    public class LoadingManager : MonoBehaviour
    {
        [Header("Loading Screen")]
        [SerializeField]
        [Tooltip("Reference to the LoadingScreen UI component")]
        private LoadingScreen m_loadingScreen;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("Minimum time to show loading screen (prevents flash for fast loads)")]
        private float m_minimumLoadingTime = 1.0f;

        private float m_loadingStartTime;

        private void Awake()
        {
            // Ensure loading screen is shown immediately
            if (m_loadingScreen != null)
            {
                m_loadingScreen.ShowImmediate();
                m_loadingStartTime = Time.time;
                DebugBase.Log($"[{nameof(LoadingManager)}] Loading screen shown");
            }
            else
            {
                DebugBase.LogError($"[{nameof(LoadingManager)}] LoadingScreen reference not assigned!");
            }
        }

        private async void Start()
        {
            await WaitForInitializationAsync();
            await HideLoadingScreenAsync();
        }

        /// <summary>
        /// Waits for GameManager to complete initialization, updating progress along the way.
        /// </summary>
        private async UniTask WaitForInitializationAsync()
        {
            DebugBase.Log($"[{nameof(LoadingManager)}] Waiting for GameManager initialization...");

            // Wait for GameManager instance to exist
            while (GameManager.Instance == null)
            {
                await UniTask.Yield();
            }

            // Subscribe to initialization progress events
            GameManager.Instance.OnInitializationProgress += OnInitializationProgress;

            // Wait for initialization to complete
            while (!GameManager.Instance.IsInitializationComplete)
            {
                await UniTask.Yield();
            }

            // Unsubscribe from events
            GameManager.Instance.OnInitializationProgress -= OnInitializationProgress;

            DebugBase.Log($"[{nameof(LoadingManager)}] GameManager initialization complete");
        }

        /// <summary>
        /// Called when GameManager reports initialization progress.
        /// </summary>
        private void OnInitializationProgress(float progress, string message)
        {
            if (m_loadingScreen != null)
            {
                m_loadingScreen.UpdateProgress(progress, message);
            }
        }

        /// <summary>
        /// Hides the loading screen, ensuring minimum loading time has elapsed.
        /// </summary>
        private async UniTask HideLoadingScreenAsync()
        {
            if (m_loadingScreen == null)
            {
                return;
            }

            // Ensure minimum loading time has elapsed
            float elapsedTime = Time.time - m_loadingStartTime;
            if (elapsedTime < m_minimumLoadingTime)
            {
                float remainingTime = m_minimumLoadingTime - elapsedTime;
                await UniTask.Delay(System.TimeSpan.FromSeconds(remainingTime));
            }

            // Fade out the loading screen
            await m_loadingScreen.FadeOutAsync();

            DebugBase.Log($"[{nameof(LoadingManager)}] Loading screen hidden");
        }

        /// <summary>
        /// Shows the loading screen (useful for scene transitions).
        /// </summary>
        public async UniTask ShowLoadingScreenAsync()
        {
            if (m_loadingScreen != null)
            {
                await m_loadingScreen.FadeInAsync();
            }
        }

        /// <summary>
        /// Updates loading progress manually (useful for custom loading sequences).
        /// </summary>
        /// <param name="progress">Progress value between 0 and 1.</param>
        /// <param name="message">Optional status message.</param>
        public void UpdateProgress(float progress, string message = null)
        {
            if (m_loadingScreen != null)
            {
                m_loadingScreen.UpdateProgress(progress, message);
            }
        }
    }
}

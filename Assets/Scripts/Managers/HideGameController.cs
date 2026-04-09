using System;
using System.Collections.Generic;
using Birdie.Debug;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Controls game-wide visual visibility while keeping all game logic and audio running.
    /// Sets camera culling masks to zero (renders nothing but still clears the buffer to
    /// transparent) and disables Canvas components so the desktop shows through the overlay.
    /// Scripts, timers, bird behavior, and sounds continue uninterrupted.
    /// The button canvas is always excluded from hiding so the single toggle button
    /// remains visible in both states.
    /// </summary>
    public class HideGameController : MonoBehaviour
    {
        [Header("Toggle Button")]
        [SerializeField]
        [Tooltip("Canvas containing the toggle button — always visible in both hidden and shown states")]
        private Canvas m_buttonCanvas;

        private readonly List<Camera> m_cameras = new List<Camera>();
        private readonly List<int> m_originalCullingMasks = new List<int>();
        private readonly List<Canvas> m_hiddenCanvases = new List<Canvas>();
        private bool m_isHidden;

        /// <summary>
        /// True while the game visuals are hidden.
        /// </summary>
        public bool IsHidden => m_isHidden;

        /// <summary>
        /// Fired immediately after the game visuals are hidden.
        /// </summary>
        public event Action OnGameHidden;

        /// <summary>
        /// Fired immediately after the game visuals are restored.
        /// </summary>
        public event Action OnGameShown;

        private void Awake()
        {
            EnsureButtonCanvasOnTop();
            DisableButtonCanvas();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStarted += OnGameStarted;
            }
            else
            {
                DebugBase.LogWarning($"[{nameof(HideGameController)}] GameManager not available, button canvas will remain hidden");
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStarted -= OnGameStarted;
            }
        }

        private void OnGameStarted()
        {
            EnableButtonCanvas();
            DebugBase.Log($"[{nameof(HideGameController)}] Game started, hide button enabled");
        }

        /// <summary>
        /// Toggles between hidden and visible states.
        /// </summary>
        public void ToggleVisibility()
        {
            if (m_isHidden)
            {
                ShowGame();
            }
            else
            {
                HideGame();
            }
        }

        /// <summary>
        /// Zeroes all camera culling masks and hides all canvases except the button canvas.
        /// Cameras keep clearing to transparent so the desktop shows through the overlay.
        /// Audio and game logic continue running.
        /// </summary>
        public void HideGame()
        {
            if (m_isHidden)
            {
                return;
            }

            HideCameras();
            HideCanvases();
            m_isHidden = true;

            OnGameHidden?.Invoke();
            DebugBase.Log($"[{nameof(HideGameController)}] Game hidden");
        }

        /// <summary>
        /// Restores all camera culling masks and re-enables all hidden canvases.
        /// </summary>
        public void ShowGame()
        {
            if (!m_isHidden)
            {
                return;
            }

            RestoreCameras();
            RestoreCanvases();
            m_isHidden = false;

            OnGameShown?.Invoke();
            DebugBase.Log($"[{nameof(HideGameController)}] Game shown");
        }

        private void HideCameras()
        {
            m_cameras.Clear();
            m_originalCullingMasks.Clear();

            foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                m_cameras.Add(cam);
                m_originalCullingMasks.Add(cam.cullingMask);
                cam.cullingMask = 0;
            }
        }

        private void RestoreCameras()
        {
            for (int i = 0; i < m_cameras.Count; i++)
            {
                if (m_cameras[i] != null)
                {
                    m_cameras[i].cullingMask = m_originalCullingMasks[i];
                }
            }

            m_cameras.Clear();
            m_originalCullingMasks.Clear();
        }

        private void HideCanvases()
        {
            m_hiddenCanvases.Clear();

            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (IsButtonCanvas(canvas) || !canvas.enabled)
                {
                    continue;
                }

                m_hiddenCanvases.Add(canvas);
                canvas.enabled = false;
            }
        }

        private void RestoreCanvases()
        {
            foreach (Canvas canvas in m_hiddenCanvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = true;
                }
            }

            m_hiddenCanvases.Clear();
        }

        private bool IsButtonCanvas(Canvas canvas)
        {
            if (m_buttonCanvas == null)
            {
                return false;
            }

            return canvas == m_buttonCanvas || canvas.transform.IsChildOf(m_buttonCanvas.transform);
        }

        private void EnableButtonCanvas()
        {
            if (m_buttonCanvas != null)
            {
                m_buttonCanvas.enabled = true;
            }
        }

        private void DisableButtonCanvas()
        {
            if (m_buttonCanvas != null)
            {
                m_buttonCanvas.enabled = false;
            }
        }

        private void EnsureButtonCanvasOnTop()
        {
            if (m_buttonCanvas == null)
            {
                DebugBase.LogWarning($"[{nameof(HideGameController)}] Button canvas is not assigned!");
                return;
            }

            m_buttonCanvas.sortingOrder = short.MaxValue;
        }
    }
}

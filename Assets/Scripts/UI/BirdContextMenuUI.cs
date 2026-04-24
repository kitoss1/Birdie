using System.Collections.Generic;
using Birdie.Birds;
using Birdie.Data;
using Birdie.Debug;
using Birdie.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// Transient popup menu that appears above a clicked bird with action buttons.
    /// Subscribes to static Bird events for decoupled communication.
    /// </summary>
    public sealed class BirdContextMenuUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject m_menuPanel;
        [SerializeField] private RectTransform m_menuPanelRect;

        [Header("Backdrop")]
        [SerializeField] private GameObject m_backdrop;
        [SerializeField] private Button m_backdropButton;

        [Header("Action Buttons")]
        [SerializeField] private List<PopupMenuButtonEntry> m_actionButtons;

        [Header("Play Cooldown Timer")]
        [SerializeField] private GameObject m_playTimerContainer;
        [SerializeField] private TextMeshProUGUI m_playTimerLabel;
        [SerializeField] private Image m_playTimerFill;

        [Header("Positioning")]
        [SerializeField] private float m_verticalOffset = 80f;

        private Bird m_currentBird;
        private RectTransform m_canvasRect;
        private Camera m_mainCamera;

        private void Awake()
        {
            CacheReferences();
            SetupButtonListeners();
            Hide();
        }

        private void OnEnable()
        {
            Bird.BirdClicked += OnBirdClicked;
            Bird.BirdLeaving += OnBirdLeaving;
        }

        private void OnDisable()
        {
            Bird.BirdClicked -= OnBirdClicked;
            Bird.BirdLeaving -= OnBirdLeaving;
        }

        private void Update()
        {
            if (m_currentBird == null || !m_menuPanel.activeSelf)
            {
                return;
            }

            UpdatePlayTimer();
        }

        private void OnDestroy()
        {
            RemoveButtonListeners();
        }

        private void CacheReferences()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                m_canvasRect = canvas.GetComponent<RectTransform>();
            }

            m_mainCamera = Camera.main;
            if (m_mainCamera == null)
            {
                m_mainCamera = FindFirstObjectByType<Camera>();
            }
        }

        private void SetupButtonListeners()
        {
            if (m_backdropButton != null)
            {
                m_backdropButton.onClick.AddListener(Hide);
            }

            foreach (PopupMenuButtonEntry entry in m_actionButtons)
            {
                if (entry.Button == null)
                {
                    continue;
                }

                PopupMenuAction action = entry.Action;
                entry.Button.onClick.AddListener(() => OnActionClicked(action));
            }
        }

        private void RemoveButtonListeners()
        {
            if (m_backdropButton != null)
            {
                m_backdropButton.onClick.RemoveListener(Hide);
            }

            foreach (PopupMenuButtonEntry entry in m_actionButtons)
            {
                if (entry.Button != null)
                {
                    entry.Button.onClick.RemoveAllListeners();
                }
            }
        }

        private void OnBirdClicked(Bird bird)
        {
            if (bird == null)
            {
                return;
            }

            // Toggle: clicking the same bird closes the menu
            if (m_currentBird == bird && m_menuPanel.activeSelf)
            {
                Hide();
                return;
            }

            if (!bird.AllowClickWhileActive)
            {
                return;
            }

            Show(bird);
        }

        private void OnBirdLeaving(Bird bird)
        {
            if (m_currentBird == bird)
            {
                Hide();
            }
        }

        private void Show(Bird bird)
        {
            if (m_currentBird != null)
            {
                m_currentBird.Resume();
            }

            m_currentBird = bird;
            bird.Pause();
            m_backdrop.SetActive(true);
            m_menuPanel.SetActive(true);

            RefreshButtonStates();
            PositionMenuAboveBird(bird);

            DebugBase.Log($"[{nameof(BirdContextMenuUI)}] Showing menu for {bird.BirdData.BirdName}", DebugCategory.UI);
        }

        public void Hide()
        {
            if (m_currentBird != null)
            {
                m_currentBird.Resume();
            }

            m_menuPanel.SetActive(false);
            m_backdrop.SetActive(false);
            m_currentBird = null;

            if (m_playTimerContainer != null)
            {
                m_playTimerContainer.SetActive(false);
            }

            DebugBase.Log($"[{nameof(BirdContextMenuUI)}] Menu hidden", DebugCategory.UI);
        }

        private void RefreshButtonStates()
        {
            foreach (PopupMenuButtonEntry entry in m_actionButtons)
            {
                if (entry.Button != null)
                {
                    entry.Button.interactable = IsActionAvailable(entry.Action);
                }
            }
        }

        private void UpdatePlayTimer()
        {
            float remaining = m_currentBird.MinigameCooldownRemaining;
            bool onCooldown = !m_currentBird.CanPlayMinigame && remaining > 0f;

            if (m_playTimerContainer != null)
            {
                m_playTimerContainer.SetActive(onCooldown);
            }

            if (onCooldown)
            {
                if (m_playTimerLabel != null)
                {
                    m_playTimerLabel.text = FormatCooldownTime(remaining);
                }

                if (m_playTimerFill != null)
                {
                    float total = m_currentBird.BirdData.MinigameCooldownDuration;
                    m_playTimerFill.fillAmount = total > 0f ? remaining / total : 0f;
                }
            }

            if (!m_currentBird.CanPlayMinigame && remaining <= 0f)
            {
                RefreshButtonStates();
            }
        }

        private static string FormatCooldownTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return mins > 0 ? $"{mins}:{secs:D2}" : $"{secs}s";
        }

        private bool IsActionAvailable(PopupMenuAction action)
        {
            return action switch
            {
                PopupMenuAction.Play => m_currentBird != null && m_currentBird.CanPlayMinigame,
                _ => true,
            };
        }

        private void OnActionClicked(PopupMenuAction action)
        {
            if (m_currentBird == null)
            {
                return;
            }

            switch (action)
            {
                case PopupMenuAction.Play:
                    OnPlayClicked();
                    break;
                case PopupMenuAction.ScareAway:
                    OnScareAwayClicked();
                    break;
            }
        }

        private void OnPlayClicked()
        {
            Bird bird = m_currentBird;
            BirdData birdData = bird.BirdData;
            bird.MarkMinigamePlayed();
            DebugBase.Log($"[{nameof(BirdContextMenuUI)}] Play clicked for {birdData.BirdName}", DebugCategory.UI);
            Hide();

            if (GameManager.Instance?.MenuManager != null)
            {
                GameManager.Instance.MenuManager.OpenMinigamesMenu(birdData);
            }
        }

        private void OnScareAwayClicked()
        {
            Bird bird = m_currentBird;
            DebugBase.Log($"[{nameof(BirdContextMenuUI)}] Scare Away clicked for {bird.BirdData.BirdName}", DebugCategory.UI);
            Hide();
            bird.ForceDeparture();
        }

        private void PositionMenuAboveBird(Bird bird)
        {
            if (m_mainCamera == null || m_canvasRect == null)
            {
                return;
            }

            Vector3 worldPosition = bird.transform.position;
            Vector2 screenPosition = m_mainCamera.WorldToScreenPoint(worldPosition);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_canvasRect,
                screenPosition,
                m_mainCamera,
                out Vector2 localPoint);

            localPoint.y += m_verticalOffset;
            localPoint = ClampToCanvas(localPoint);
            m_menuPanelRect.anchoredPosition = localPoint;
        }

        private Vector2 ClampToCanvas(Vector2 localPoint)
        {
            Vector2 canvasSize = m_canvasRect.rect.size;
            Vector2 panelSize = m_menuPanelRect.rect.size;
            float halfCanvasWidth = canvasSize.x * 0.5f;
            float halfCanvasHeight = canvasSize.y * 0.5f;
            float halfPanelWidth = panelSize.x * 0.5f;

            localPoint.x = Mathf.Clamp(
                localPoint.x,
                -halfCanvasWidth + halfPanelWidth,
                halfCanvasWidth - halfPanelWidth);

            // If the menu goes above the top, flip it below the bird instead
            if (localPoint.y + panelSize.y > halfCanvasHeight)
            {
                localPoint.y -= m_verticalOffset * 2f + panelSize.y;
            }

            if (localPoint.y < -halfCanvasHeight)
            {
                localPoint.y = -halfCanvasHeight;
            }

            return localPoint;
        }
    }
}

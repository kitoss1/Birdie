using Birdie.Birds;
using Birdie.Core;
using Birdie.Data;
using Birdie.Debug;
using Birdie.Managers;
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
        [SerializeField] private Button m_feedButton;
        [SerializeField] private Button m_playSongButton;
        [SerializeField] private Button m_playButton;
        [SerializeField] private Button m_scareAwayButton;

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

            if (m_feedButton != null)
            {
                m_feedButton.onClick.AddListener(OnFeedClicked);
            }

            if (m_playSongButton != null)
            {
                m_playSongButton.onClick.AddListener(OnPlaySongClicked);
            }

            if (m_playButton != null)
            {
                m_playButton.onClick.AddListener(OnPlayClicked);
            }

            if (m_scareAwayButton != null)
            {
                m_scareAwayButton.onClick.AddListener(OnScareAwayClicked);
            }
        }

        private void RemoveButtonListeners()
        {
            if (m_backdropButton != null)
            {
                m_backdropButton.onClick.RemoveListener(Hide);
            }

            if (m_feedButton != null)
            {
                m_feedButton.onClick.RemoveAllListeners();
            }

            if (m_playSongButton != null)
            {
                m_playSongButton.onClick.RemoveAllListeners();
            }

            if (m_playButton != null)
            {
                m_playButton.onClick.RemoveAllListeners();
            }

            if (m_scareAwayButton != null)
            {
                m_scareAwayButton.onClick.RemoveAllListeners();
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
            // Resume previous bird if switching between birds
            if (m_currentBird != null)
            {
                m_currentBird.Resume();
            }

            m_currentBird = bird;
            bird.Pause();
            m_backdrop.SetActive(true);
            m_menuPanel.SetActive(true);

            if (m_playButton != null)
            {
                m_playButton.interactable = bird.CanPlayMinigame;
            }

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

            DebugBase.Log($"[{nameof(BirdContextMenuUI)}] Menu hidden", DebugCategory.UI);
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

            // Position above the bird
            localPoint.y += m_verticalOffset;

            // Clamp to canvas bounds
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

            // Clamp horizontal
            localPoint.x = Mathf.Clamp(
                localPoint.x,
                -halfCanvasWidth + halfPanelWidth,
                halfCanvasWidth - halfPanelWidth);

            // If menu goes above the top, place it below the bird instead
            if (localPoint.y + panelSize.y > halfCanvasHeight)
            {
                localPoint.y -= m_verticalOffset * 2f + panelSize.y;
            }

            // Clamp bottom
            if (localPoint.y < -halfCanvasHeight)
            {
                localPoint.y = -halfCanvasHeight;
            }

            return localPoint;
        }

        private void OnFeedClicked()
        {
            if (m_currentBird == null)
            {
                return;
            }

            Bird bird = m_currentBird;
            DebugBase.Log($"[{nameof(BirdContextMenuUI)}] Feed clicked for {bird.BirdData.BirdName}", DebugCategory.UI);
            Hide();
            bird.OnBirdFed(bird.BirdData.DietType);
        }

        private void OnPlaySongClicked()
        {
            if (m_currentBird == null)
            {
                return;
            }

            DebugBase.Log($"[{nameof(BirdContextMenuUI)}] Play Song clicked for {m_currentBird.BirdData.BirdName}", DebugCategory.UI);
            m_currentBird.PlaySong();
        }

        private void OnPlayClicked()
        {
            if (m_currentBird == null)
            {
                return;
            }

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
            if (m_currentBird == null)
            {
                return;
            }

            Bird bird = m_currentBird;
            DebugBase.Log($"[{nameof(BirdContextMenuUI)}] Scare Away clicked for {bird.BirdData.BirdName}", DebugCategory.UI);
            Hide();
            bird.ForceDeparture();
        }
    }
}

using System.Collections.Generic;
using Birdie.Birds;
using Birdie.Debug;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// Transient popup menu that appears above a clicked BirdObject with context-sensitive action buttons.
    /// Subscribes to the static BirdObject.ObjectClicked event for decoupled communication.
    /// </summary>
    public sealed class ObjectContextMenuUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject m_menuPanel;
        [SerializeField] private RectTransform m_menuPanelRect;

        [Header("Backdrop")]
        [SerializeField] private GameObject m_backdrop;
        [SerializeField] private Button m_backdropButton;

        [Header("Action Buttons")]
        [SerializeField] private List<ObjectPopupMenuButtonEntry> m_actionButtons;

        [Header("Positioning")]
        [SerializeField] private float m_verticalOffset = 80f;

        private BirdObject m_currentObject;
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
            BirdObject.ObjectClicked += OnObjectClicked;
        }

        private void OnDisable()
        {
            BirdObject.ObjectClicked -= OnObjectClicked;
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

            foreach (ObjectPopupMenuButtonEntry entry in m_actionButtons)
            {
                if (entry.Button == null)
                {
                    continue;
                }

                ObjectPopupMenuAction action = entry.Action;
                entry.Button.onClick.AddListener(() => OnActionClicked(action));
            }
        }

        private void RemoveButtonListeners()
        {
            if (m_backdropButton != null)
            {
                m_backdropButton.onClick.RemoveListener(Hide);
            }

            foreach (ObjectPopupMenuButtonEntry entry in m_actionButtons)
            {
                if (entry.Button != null)
                {
                    entry.Button.onClick.RemoveAllListeners();
                }
            }
        }

        private void OnObjectClicked(BirdObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (m_currentObject == obj && m_menuPanel.activeSelf)
            {
                Hide();
                return;
            }

            Show(obj);
        }

        private void Show(BirdObject obj)
        {
            m_currentObject = obj;
            m_backdrop.SetActive(true);
            m_menuPanel.SetActive(true);

            RefreshButtonStates(obj);
            PositionMenuAboveObject(obj);

            DebugBase.Log($"[{nameof(ObjectContextMenuUI)}] Showing menu for {obj.ObjectID}", DebugCategory.UI);
        }

        public void Hide()
        {
            m_menuPanel.SetActive(false);
            m_backdrop.SetActive(false);
            m_currentObject = null;

            DebugBase.Log($"[{nameof(ObjectContextMenuUI)}] Menu hidden", DebugCategory.UI);
        }

        private void RefreshButtonStates(BirdObject obj)
        {
            foreach (ObjectPopupMenuButtonEntry entry in m_actionButtons)
            {
                if (entry.Button == null)
                {
                    continue;
                }

                bool visible = IsActionVisible(entry.Action, obj);
                entry.Button.gameObject.SetActive(visible);

                if (visible)
                {
                    entry.Button.interactable = IsActionAvailable(entry.Action, obj);
                }
            }
        }

        private bool IsActionVisible(ObjectPopupMenuAction action, BirdObject obj)
        {
            return action switch
            {
                ObjectPopupMenuAction.Refill => obj is BirdFeeder,
                _ => true,
            };
        }

        private bool IsActionAvailable(ObjectPopupMenuAction action, BirdObject obj)
        {
            return action switch
            {
                ObjectPopupMenuAction.Refill => obj is BirdFeeder feeder && feeder.CurrentFoodLevel < feeder.MaxFoodLevel,
                _ => true,
            };
        }

        private void OnActionClicked(ObjectPopupMenuAction action)
        {
            if (m_currentObject == null)
            {
                return;
            }

            switch (action)
            {
                case ObjectPopupMenuAction.Refill:
                    OnRefillClicked();
                    break;
            }
        }

        private void OnRefillClicked()
        {
            if (m_currentObject is not BirdFeeder feeder)
            {
                return;
            }

            DebugBase.Log($"[{nameof(ObjectContextMenuUI)}] Refill clicked for {feeder.ObjectID}", DebugCategory.UI);
            feeder.Refill();
            Hide();
        }

        private void PositionMenuAboveObject(BirdObject obj)
        {
            if (m_mainCamera == null || m_canvasRect == null)
            {
                return;
            }

            Vector3 worldPosition = obj.transform.position;
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

            // If the menu goes above the top, flip it below the object instead
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

using Birdie.Debug;
using Birdie.Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Birdie.UI.Store
{
    /// <summary>
    /// Handles the world-space horizontal movement for repositioning store items.
    /// Object follows the mouse until the player clicks to place it,
    /// then confirm/cancel buttons appear to save or revert.
    /// </summary>
    public sealed class StoreItemMoveHandler : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField]
        [Tooltip("Fullscreen transparent image that blocks input from passing through during move mode")]
        private GameObject m_backdrop;

        [SerializeField] private GameObject m_buttonsPanel;
        [SerializeField] private Button m_confirmButton;
        [SerializeField] private Button m_cancelButton;

        [Header("Movement Bounds")]
        [SerializeField]
        [Tooltip("Left boundary transform. The object cannot move past this X position")]
        private Transform m_leftBound;

        [SerializeField]
        [Tooltip("Right boundary transform. The object cannot move past this X position")]
        private Transform m_rightBound;

        private GameObject m_targetObject;
        private string m_targetItemID;
        private Vector3 m_originalPosition;
        private Canvas m_targetCanvas;
        private RectTransform m_canvasRect;
        private bool m_isMoving;
        private bool m_isPlaced;
        private bool m_skipFirstFrame;

        private void Awake()
        {
            if (m_confirmButton != null)
            {
                m_confirmButton.onClick.AddListener(ConfirmPlacement);
            }

            if (m_cancelButton != null)
            {
                m_cancelButton.onClick.AddListener(CancelPlacement);
            }
        }

        private void OnDestroy()
        {
            if (m_confirmButton != null)
            {
                m_confirmButton.onClick.RemoveListener(ConfirmPlacement);
            }

            if (m_cancelButton != null)
            {
                m_cancelButton.onClick.RemoveListener(CancelPlacement);
            }
        }

        private void Update()
        {
            if (!m_isMoving || m_isPlaced || m_targetObject == null)
            {
                return;
            }

            if (m_skipFirstFrame)
            {
                m_skipFirstFrame = false;
                return;
            }

            if (Mouse.current == null)
            {
                return;
            }

            FollowMouse();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                PlaceObject();
            }
        }

        public void StartMoving(GameObject targetObject, string itemID)
        {
            if (targetObject == null)
            {
                DebugBase.LogWarning($"[{nameof(StoreItemMoveHandler)}] Target object is null");
                return;
            }

            m_targetObject = targetObject;
            m_targetItemID = itemID;
            m_originalPosition = targetObject.transform.position;
            m_targetCanvas = targetObject.GetComponentInParent<Canvas>();
            m_canvasRect = m_targetCanvas != null ? m_targetCanvas.GetComponent<RectTransform>() : null;
            m_isMoving = true;
            m_isPlaced = false;
            m_skipFirstFrame = true;

            gameObject.SetActive(true);
            SetBackdropVisible(true);
            SetButtonsPanelVisible(false);

            if (GameManager.Instance?.MenuManager != null)
            {
                GameManager.Instance.MenuManager.SetMenuButtonsInteractable(false);
            }

            DebugBase.Log($"[{nameof(StoreItemMoveHandler)}] Started moving item: {itemID}");
        }

        private void FollowMouse()
        {
            if (m_targetCanvas == null || m_canvasRect == null)
            {
                return;
            }

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Camera eventCamera = m_targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : m_targetCanvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(m_canvasRect, screenPos, eventCamera, out Vector3 worldPoint))
            {
                return;
            }

            Vector3 position = m_targetObject.transform.position;
            position.x = ClampToVisibleArea(worldPoint.x);
            m_targetObject.transform.position = position;
        }

        private void PlaceObject()
        {
            m_isPlaced = true;
            SetButtonsPanelVisible(true);
            DebugBase.Log($"[{nameof(StoreItemMoveHandler)}] Object placed, waiting for confirm/cancel");
        }

        private void SetButtonsPanelVisible(bool visible)
        {
            if (m_buttonsPanel != null)
            {
                m_buttonsPanel.SetActive(visible);
            }
        }

        private void SetBackdropVisible(bool visible)
        {
            if (m_backdrop != null)
            {
                m_backdrop.SetActive(visible);
            }
        }

        private float ClampToVisibleArea(float x)
        {
            if (m_leftBound != null && m_rightBound != null)
            {
                float min = Mathf.Min(m_leftBound.position.x, m_rightBound.position.x);
                float max = Mathf.Max(m_leftBound.position.x, m_rightBound.position.x);

                if (!Mathf.Approximately(min, max))
                {
                    return Mathf.Clamp(x, min, max);
                }
            }

            if (m_canvasRect != null)
            {
                Vector3[] corners = new Vector3[4];
                m_canvasRect.GetWorldCorners(corners);
                float canvasMin = corners[0].x;
                float canvasMax = corners[2].x;
                return Mathf.Clamp(x, canvasMin, canvasMax);
            }

            return x;
        }

        private void ConfirmPlacement()
        {
            if (!m_isMoving || m_targetObject == null)
            {
                return;
            }

            if (GameManager.Instance?.StoreManager != null)
            {
                GameManager.Instance.StoreManager.SaveItemPosition(m_targetItemID, m_targetObject.transform.position.x);
            }

            DebugBase.Log($"[{nameof(StoreItemMoveHandler)}] Confirmed placement for item: {m_targetItemID}");
            FinishMoving();
        }

        private void CancelPlacement()
        {
            if (!m_isMoving || m_targetObject == null)
            {
                return;
            }

            m_targetObject.transform.position = m_originalPosition;

            DebugBase.Log($"[{nameof(StoreItemMoveHandler)}] Cancelled placement for item: {m_targetItemID}");
            FinishMoving();
        }

        private void FinishMoving()
        {
            m_isMoving = false;
            m_isPlaced = false;
            m_targetObject = null;
            m_targetItemID = null;

            SetButtonsPanelVisible(false);
            SetBackdropVisible(false);

            if (GameManager.Instance?.MenuManager != null)
            {
                GameManager.Instance.MenuManager.SetMenuButtonsInteractable(true);
            }

            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_confirmButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemMoveHandler)}] Confirm Button reference is missing!", this);
            }

            if (m_cancelButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(StoreItemMoveHandler)}] Cancel Button reference is missing!", this);
            }
        }
#endif
    }
}

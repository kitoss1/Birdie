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

        private Camera m_mainCamera;
        private GameObject m_targetObject;
        private string m_targetItemID;
        private Vector3 m_originalPosition;
        private bool m_isMoving;
        private bool m_isPlaced;
        private bool m_skipFirstFrame;

        private void Awake()
        {
            m_mainCamera = Camera.main;
            if (m_mainCamera == null)
            {
                m_mainCamera = FindFirstObjectByType<Camera>();
            }

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
            if (!m_isMoving || m_isPlaced || m_targetObject == null || m_mainCamera == null)
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
            m_isMoving = true;
            m_isPlaced = false;
            m_skipFirstFrame = true;

            gameObject.SetActive(true);
            SetButtonsPanelVisible(false);

            if (GameManager.Instance?.MenuManager != null)
            {
                GameManager.Instance.MenuManager.SetMenuButtonsInteractable(false);
            }

            DebugBase.Log($"[{nameof(StoreItemMoveHandler)}] Started moving item: {itemID}");
        }

        private void FollowMouse()
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            if (Mathf.Approximately(delta.x, 0f))
            {
                return;
            }

            Canvas canvas = m_targetObject.GetComponentInParent<Canvas>();
            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            float canvasDeltaX = delta.x / scaleFactor;

            Vector3 position = m_targetObject.transform.position;
            position.x = ClampX(position.x + canvasDeltaX);
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

        private float ClampX(float x)
        {
            if (m_leftBound == null || m_rightBound == null)
            {
                return x;
            }

            float min = Mathf.Min(m_leftBound.position.x, m_rightBound.position.x);
            float max = Mathf.Max(m_leftBound.position.x, m_rightBound.position.x);

            if (Mathf.Approximately(min, max))
            {
                return x;
            }

            return Mathf.Clamp(x, min, max);
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

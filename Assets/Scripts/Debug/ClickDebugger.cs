using Birdie.Debug;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Birdie.Debug
{
    /// <summary>
    /// Debug utility to log all mouse clicks and what objects they hit.
    /// Add this to any GameObject in the scene to debug click detection.
    /// </summary>
    public class ClickDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField]
        [Tooltip("Enable/disable click logging")]
        private bool m_enableLogging = true;

        [SerializeField]
        [Tooltip("Show detailed raycast information")]
        private bool m_showDetailedInfo = true;

        private Camera m_mainCamera;

        private void Start()
        {
            m_mainCamera = Camera.main;
            if (m_mainCamera == null)
            {
                DebugBase.LogWarning($"[{nameof(ClickDebugger)}] No main camera found!", DebugCategory.General);
            }
        }

        private void Update()
        {
            if (!m_enableLogging)
            {
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                LogClickInformation();
            }
        }

        private void LogClickInformation()
        {
            Vector3 mousePosition = Mouse.current.position.ReadValue();
            DebugBase.Log($"[{nameof(ClickDebugger)}] ========== CLICK DETECTED ==========", DebugCategory.General);
            DebugBase.Log($"[{nameof(ClickDebugger)}] Mouse Position: {mousePosition}", DebugCategory.General);

            if (m_mainCamera == null)
            {
                DebugBase.LogWarning($"[{nameof(ClickDebugger)}] Cannot raycast - no camera!", DebugCategory.General);
                return;
            }

            // Check 3D physics
            Ray ray3D = m_mainCamera.ScreenPointToRay(mousePosition);
            RaycastHit hit3D;
            if (Physics.Raycast(ray3D, out hit3D))
            {
                DebugBase.Log($"[{nameof(ClickDebugger)}] 3D Hit: {hit3D.collider.gameObject.name}", DebugCategory.General);
                if (m_showDetailedInfo)
                {
                    DebugBase.Log($"[{nameof(ClickDebugger)}]   - Layer: {LayerMask.LayerToName(hit3D.collider.gameObject.layer)}", DebugCategory.General);
                    DebugBase.Log($"[{nameof(ClickDebugger)}]   - Tag: {hit3D.collider.gameObject.tag}", DebugCategory.General);
                    DebugBase.Log($"[{nameof(ClickDebugger)}]   - Position: {hit3D.point}", DebugCategory.General);
                }
            }
            else
            {
                DebugBase.Log($"[{nameof(ClickDebugger)}] 3D Hit: NONE", DebugCategory.General);
            }

            // Check 2D physics
            Vector2 worldPoint2D = m_mainCamera.ScreenToWorldPoint(mousePosition);
            RaycastHit2D hit2D = Physics2D.Raycast(worldPoint2D, Vector2.zero);
            if (hit2D.collider != null)
            {
                DebugBase.Log($"[{nameof(ClickDebugger)}] 2D Hit: {hit2D.collider.gameObject.name}", DebugCategory.General);
                if (m_showDetailedInfo)
                {
                    DebugBase.Log($"[{nameof(ClickDebugger)}]   - Layer: {LayerMask.LayerToName(hit2D.collider.gameObject.layer)}", DebugCategory.General);
                    DebugBase.Log($"[{nameof(ClickDebugger)}]   - Tag: {hit2D.collider.gameObject.tag}", DebugCategory.General);
                    DebugBase.Log($"[{nameof(ClickDebugger)}]   - Position: {hit2D.point}", DebugCategory.General);

                    Component[] components = hit2D.collider.GetComponents<Component>();
                    string componentNames = string.Join(", ", System.Array.ConvertAll(components, c => c.GetType().Name));
                    DebugBase.Log($"[{nameof(ClickDebugger)}]   - Components: {componentNames}", DebugCategory.General);
                }
            }
            else
            {
                DebugBase.Log($"[{nameof(ClickDebugger)}] 2D Hit: NONE", DebugCategory.General);
            }

            // Check if UI is blocking
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                bool isOverUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
                DebugBase.Log($"[{nameof(ClickDebugger)}] Is Over UI: {isOverUI}", DebugCategory.General);
            }

            DebugBase.Log($"[{nameof(ClickDebugger)}] ========================================", DebugCategory.General);
        }
    }
}

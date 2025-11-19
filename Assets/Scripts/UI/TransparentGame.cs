using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class TransparentGame : MonoBehaviour
{
    [Header("Window Settings")]
    [SerializeField]
    [Tooltip("Enable click-through for the window")]
    private bool m_enableClickThrough = true;
    private bool m_isClickThroughEnabled = false;

    private Camera m_mainCamera;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    const int GWL_STYLE   = -16;
    const int GWL_EXSTYLE = -20;
    const int WS_BORDER   = 0x00800000;
    const int WS_DLGFRAME = 0x00400000;
    const int WS_CAPTION  = WS_BORDER | WS_DLGFRAME;
    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;

    [StructLayout(LayoutKind.Sequential)]
    struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong
        (IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("dwmapi.dll")]
    static extern int DwmExtendFrameIntoClientArea
        (IntPtr hWnd, ref MARGINS pMargins);

    private IntPtr hwnd;
#endif

    private void Awake()
    {
        // Cache camera reference to avoid Camera.main issues in builds
        m_mainCamera = Camera.main;
        if (m_mainCamera == null)
        {
            m_mainCamera = FindFirstObjectByType<Camera>();
        }

        if (m_mainCamera == null)
        {
            Debug.LogError($"[{nameof(TransparentGame)}] No camera found in scene!");
        }
    }

    void Start()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        hwnd = GetActiveWindow();

        int style = GetWindowLong(hwnd, GWL_STYLE);
        style &= ~WS_CAPTION;
        SetWindowLong(hwnd, GWL_STYLE, style);

        var margins = new MARGINS
        {
            cxLeftWidth = -1,
            cxRightWidth = 0,
            cyTopHeight = 0,
            cyBottomHeight = 0
        };
        DwmExtendFrameIntoClientArea(hwnd, ref margins);

        // Set WS_EX_LAYERED to enable transparency effects
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        exStyle |= (int)WS_EX_LAYERED;
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
#else
        Debug.LogWarning($"[{nameof(TransparentGame)}] Transparent Game only works in Windows builds");

#endif
    }

    private void Update()
    {
        if (m_enableClickThrough)
        {
            UpdateClickThrough();
        }
    }

    private Vector2 GetMousePosition()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // Use Windows API to get cursor position, which works even with WS_EX_TRANSPARENT
        if (GetCursorPos(out POINT cursorPos))
        {
            // Convert screen coordinates to window-relative coordinates
            ScreenToClient(hwnd, ref cursorPos);

            // Unity's coordinate system has Y increasing upward, Windows has Y increasing downward
            // We need to flip the Y coordinate
            return new Vector2(cursorPos.x, Screen.height - cursorPos.y);
        }

        Debug.LogError($"[{nameof(TransparentGame)}] GetCursorPos failed!");
        return Vector2.zero;
#else
        // In editor, use Input System
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return Vector2.zero;
#endif
    }

    private bool IsMouseOverUI(Vector2 mousePosition)
    {
        // Manual UI raycast since EventSystem.IsPointerOverGameObject() doesn't work with WS_EX_TRANSPARENT
        if (EventSystem.current == null)
        {
            return false;
        }

        // Create pointer event data
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = mousePosition
        };

        // Raycast against all UI canvases
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            Debug.Log($"[{nameof(TransparentGame)}] UI element detected: {results[0].gameObject.name}");
            return true;
        }

        return false;
    }

    private void UpdateClickThrough()
    {
        bool shouldBlockClick = ShouldBlockClick();
        
        if (shouldBlockClick && m_isClickThroughEnabled)
        {
            DisableClickThrough();
        }
        else if (!shouldBlockClick && !m_isClickThroughEnabled)
        {
            EnableClickThrough();
        }
    }

    private bool ShouldBlockClick()
    {
        // Get mouse position using Windows API (works even with WS_EX_TRANSPARENT)
        Vector2 mousePosition = GetMousePosition();
        Debug.Log($"[{nameof(TransparentGame)}] Checking raycast at screen position: {mousePosition}");

        // Check if mouse is over UI element using manual raycast
        if (IsMouseOverUI(mousePosition))
        {
            return true;
        }

        // Early return if camera is not available
        if (m_mainCamera == null)
        {
            Debug.LogError($"[{nameof(TransparentGame)}] Camera is NULL in ShouldBlockClick!");
            return false;
        }

        // Check if mouse is over a 3D object with collider
        Ray ray = m_mainCamera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log($"[{nameof(TransparentGame)}] 3D object detected: {hit.collider.gameObject.name}");
            return true;
        }

        // Check if mouse is over a 2D object with collider
        Vector2 worldPoint = m_mainCamera.ScreenToWorldPoint(mousePosition);
        RaycastHit2D hit2D = Physics2D.Raycast(worldPoint, Vector2.zero);
        if (hit2D.collider != null)
        {
            Debug.Log($"[{nameof(TransparentGame)}] 2D object detected: {hit2D.collider.gameObject.name}");
            return true;
        }

        Debug.Log($"[{nameof(TransparentGame)}] No object detected - click should pass through");
        return false;
    }
    
    private void EnableClickThrough()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= (int)WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
#endif
        
        m_isClickThroughEnabled = true;
        Debug.Log("Enable click-through for the window");
    }

    private void DisableClickThrough()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle &= ~(int)WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
#endif
        
        m_isClickThroughEnabled = false;
        Debug.Log("Disable click-through for the window");
    }

    public void SetClickThrough(bool enabled)
    {
        m_enableClickThrough = enabled;

        if (!enabled && m_isClickThroughEnabled)
        {
            DisableClickThrough();
        }
    }
}
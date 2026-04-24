using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Birdie.Birds;
using Birdie.Debug;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    // SetWindowPos constants for always on top
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_SHOWWINDOW = 0x0040;

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

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
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

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

    const uint SPI_GETWORKAREA = 0x0030;

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
        // Enable running in background to prevent pause when window loses focus
        Application.runInBackground = true;

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

        // Make window always on top
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        DebugBase.Log($"[{nameof(TransparentGame)}] Window set to always on top");

        // Resize window to work area (screen minus taskbar)
        FitWindowToWorkArea();
#else
        DebugBase.LogWarning($"[{nameof(TransparentGame)}] Transparent Game only works in Windows builds", DebugCategory.Transparency);

#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private void FitWindowToWorkArea()
    {
        var workArea = new RECT();
        if (!SystemParametersInfo(SPI_GETWORKAREA, 0, ref workArea, 0))
        {
            DebugBase.LogError($"[{nameof(TransparentGame)}] SystemParametersInfo failed, cannot fit window to work area");
            return;
        }

        int width  = workArea.right  - workArea.left;
        int height = workArea.bottom - workArea.top;

        const uint SWP_NOZORDER = 0x0004;
        SetWindowPos(hwnd, IntPtr.Zero, workArea.left, workArea.top, width, height, SWP_NOZORDER | SWP_SHOWWINDOW);
        Screen.SetResolution(width, height, false);

        DebugBase.Log($"[{nameof(TransparentGame)}] Window fitted to work area: {width}x{height} at ({workArea.left},{workArea.top})");
    }
#endif

    private void Update()
    {
        if (m_enableClickThrough)
        {
            UpdateClickThrough();
        }

        HandleSpriteClick();
    }

    private void HandleSpriteClick()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 mousePosition = GetMousePosition();

        if (TryGetUIClickable(mousePosition, out IClickable uiClickable))
        {
            DebugBase.Log($"[{nameof(TransparentGame)}] Click sent to UI clickable", DebugCategory.Mouse);
            uiClickable.OnClicked();
            return;
        }

        SpriteRenderer hit = GetSpriteRendererAtPosition(mousePosition);
        if (hit == null)
        {
            return;
        }

        IClickable clickable = hit.GetComponentInParent<IClickable>();
        if (clickable == null)
        {
            return;
        }

        DebugBase.Log($"[{nameof(TransparentGame)}] Click sent to {hit.gameObject.name}", DebugCategory.Mouse);
        clickable.OnClicked();
    }

    private bool TryGetUIClickable(Vector2 mousePosition, out IClickable clickable)
    {
        clickable = null;

        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = mousePosition,
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            IClickable found = result.gameObject.GetComponentInParent<IClickable>();
            if (found != null)
            {
                clickable = found;
                return true;
            }
        }

        return false;
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

        DebugBase.LogError($"[{nameof(TransparentGame)}] GetCursorPos failed!");
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
            DebugBase.Log($"[{nameof(TransparentGame)}] UI element detected: {results[0].gameObject.name}", DebugCategory.Mouse);
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
        DebugBase.Log($"[{nameof(TransparentGame)}] Checking raycast at screen position: {mousePosition}", DebugCategory.Mouse);

        // Check if mouse is over UI element using manual raycast
        if (IsMouseOverUI(mousePosition))
        {
            return true;
        }

        // Early return if camera is not available
        if (m_mainCamera == null)
        {
            DebugBase.LogError($"[{nameof(TransparentGame)}] Camera is NULL in ShouldBlockClick!");
            return false;
        }

        // Check if mouse is over a 3D object with collider
        Ray ray = m_mainCamera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            DebugBase.Log($"[{nameof(TransparentGame)}] 3D object detected: {hit.collider.gameObject.name}", DebugCategory.Mouse);
            return true;
        }

        // Check if mouse is over a 2D object with collider
        Vector2 worldPoint = m_mainCamera.ScreenToWorldPoint(mousePosition);
        RaycastHit2D hit2D = Physics2D.Raycast(worldPoint, Vector2.zero);
        if (hit2D.collider != null)
        {
            DebugBase.Log($"[{nameof(TransparentGame)}] 2D object detected: {hit2D.collider.gameObject.name}", DebugCategory.Mouse);
            return true;
        }

        // Check if mouse is over a visible (non-transparent) pixel of any SpriteRenderer
        if (IsMouseOverSprite(mousePosition))
        {
            return true;
        }

        DebugBase.Log($"[{nameof(TransparentGame)}] No object detected - click should pass through", DebugCategory.Mouse);
        return false;
    }

    private bool IsMouseOverSprite(Vector2 mousePosition)
    {
        return GetSpriteRendererAtPosition(mousePosition) != null;
    }

    private SpriteRenderer GetSpriteRendererAtPosition(Vector2 mousePosition)
    {
        Vector2 worldPoint = m_mainCamera.ScreenToWorldPoint(mousePosition);
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);

        foreach (SpriteRenderer sr in renderers)
        {
            if (!sr.enabled || sr.sprite == null)
            {
                continue;
            }

            Sprite sprite = sr.sprite;
            Vector2 localPoint = sr.transform.InverseTransformPoint(worldPoint);
            float pixelsPerUnit = sprite.pixelsPerUnit;

            float x = localPoint.x * pixelsPerUnit + sprite.pivot.x;
            float y = localPoint.y * pixelsPerUnit + sprite.pivot.y;

            if (sr.flipX)
            {
                x = sprite.rect.width - 1 - x;
            }

            if (sr.flipY)
            {
                y = sprite.rect.height - 1 - y;
            }

            if (x < 0 || x >= sprite.rect.width || y < 0 || y >= sprite.rect.height)
            {
                continue;
            }

            Color pixel = sprite.texture.GetPixel(
                (int)(sprite.rect.x + x),
                (int)(sprite.rect.y + y)
            );

            if (pixel.a > 0f)
            {
                DebugBase.Log($"[{nameof(TransparentGame)}] Sprite pixel detected on: {sr.gameObject.name}", DebugCategory.Mouse);
                return sr;
            }
        }

        return null;
    }
    
    private void EnableClickThrough()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= (int)WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
#endif

        m_isClickThroughEnabled = true;
        DebugBase.Log($"[{nameof(TransparentGame)}] Click-through enabled for the window", DebugCategory.Transparency);
    }

    private void DisableClickThrough()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle &= ~(int)WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
#endif

        m_isClickThroughEnabled = false;
        DebugBase.Log($"[{nameof(TransparentGame)}] Click-through disabled for the window", DebugCategory.Transparency);
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
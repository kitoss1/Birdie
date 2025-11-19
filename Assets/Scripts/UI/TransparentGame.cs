using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TransparentGame : MonoBehaviour
{
    [Header("Window Settings")]
    [SerializeField]
    [Tooltip("Enable click-through for the window")]
    private bool m_enableClickThrough = true;
    private bool m_isClickThroughEnabled = false;

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

    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

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
        // Check if mouse is over UI element
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        // Check if mouse is over a 3D object with collider
        if (Camera.main != null && Mouse.current != null)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return true;
            }

            // Check if mouse is over a 2D object with collider
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(mousePosition);
            RaycastHit2D hit2D = Physics2D.Raycast(worldPoint, Vector2.zero);
            if (hit2D.collider != null)
            {
                return true;
            }
        }

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
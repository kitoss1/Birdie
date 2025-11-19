using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.EventSystems;

public class TransparentGame : MonoBehaviour
{
    [Header("Window Settings")]
    [SerializeField]
    [Tooltip("Enable click-through for the window")]
    private bool m_enableClickThrough = true;
    private bool m_isClickThroughEnabled = false;
    
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    const int GWL_STYLE   = -16;
    const int WS_BORDER   = 0x00800000;
    const int WS_DLGFRAME = 0x00400000;
    const int WS_CAPTION  = WS_BORDER | WS_DLGFRAME;

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
#endif
    void Start()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IntPtr hwnd = GetActiveWindow();

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
#else
        Debug.LogWarning($"[{nameof(TransparentGame)}] Transparent Game only works in Windows builds");

#endif
    }

    private void Update()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (m_enableClickThrough)
            {
                UpdateClickThrough();
            }
#endif
    }
    
    private void UpdateClickThrough()
    {
#if !UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
            bool shouldBlockClick = IsMouseOverUI();

            if (shouldBlockClick && m_isClickThroughEnabled)
            {
                DisableClickThrough();
            }
            else if (!shouldBlockClick && !m_isClickThroughEnabled)
            {
                EnableClickThrough();
            }
#endif
    }
    
    private bool IsMouseOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
    
    private void EnableClickThrough()
    {
#if !UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
            
            m_isClickThroughEnabled = true;
#endif
    }

    private void DisableClickThrough()
    {
#if !UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
            
            m_isClickThroughEnabled = false;
#endif
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
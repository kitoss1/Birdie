using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class TransparentGame : MonoBehaviour
{
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

        void Start()
        {
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
        }
#endif
}
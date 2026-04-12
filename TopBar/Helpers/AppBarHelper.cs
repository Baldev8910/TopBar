using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TopBar.Helpers
{
    public static class AppBarHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attr,
            ref int attrValue,
            int attrSize);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        public static void RegisterBar(Window window, double height)
        {
            // Position the window at top, full width
            window.Left = 0;
            window.Top = 0;
            window.Width = SystemParameters.PrimaryScreenWidth;
            window.Height = height;

            // Force always on top using Win32
            var handle = new WindowInteropHelper(window).Handle;
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);

            // Apply Mica Alt effect
            ApplyMicaAlt(window);
        }

        public static void UnregisterBar(Window window)
        {
            // Nothing to unregister
        }

        private static void ApplyMicaAlt(Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;

            // Extend glass frame across the whole window
            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(handle, ref margins);

            // Enable dark mode
            int darkMode = 1;
            DwmSetWindowAttribute(handle, 20, ref darkMode, sizeof(int));

            // Apply Mica Alt backdrop
            int backdropType = 4;
            DwmSetWindowAttribute(handle, 38, ref backdropType, sizeof(int));
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
        }
    }
}
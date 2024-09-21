using System;
using System.Runtime.InteropServices;

namespace MonoconsoleLib
{
    internal class NativeInterop
    {
        #region ConsoleConstants
        public const uint STD_INPUT_HANDLE = 0xFFFFFFF6;
        public const uint STD_OUTPUT_HANDLE = 0xFFFFFFF5;
        public const uint STD_ERROR_HANDLE = 0xFFFFFFF4;
        #endregion

        #region WindowConstants
        public const int GWL_STYLE = -16;
        public const int WS_SYSMENU = 0x80000;
        public const int GWL_EXSTYLE = -20;

        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_APPWINDOW = 0x00040000;

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        #endregion

        #region Kernel32
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();
        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetStdHandle(uint nStdHandle);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, uint wAttributes);
        #endregion

        #region User32
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        #endregion
    }
}

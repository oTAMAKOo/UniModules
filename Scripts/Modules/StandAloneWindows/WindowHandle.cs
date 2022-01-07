
#if UNITY_STANDALONE_WIN

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AOT;

namespace Modules.StandAloneWindows
{
    public static class WindowHandle
    {
        //----- params -----

        //----- field -----

        private static IntPtr windowHandle = IntPtr.Zero;

        private static int processId = 0;

        #region WINAPI
        
        private delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError=true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        #endregion

        //----- property -----

        public static string WindowTitle { get; set; }

        //----- method -----

        public static IntPtr Get()
        {
            if (windowHandle != IntPtr.Zero)
            {
                return windowHandle;
            }

            if (string.IsNullOrEmpty(WindowTitle))
            {
                throw new ArgumentException();
            }

            var process = Process.GetCurrentProcess();

            processId = process.Id;
            
            EnumWindows(EnumWindowCallBack, IntPtr.Zero);
            
            return windowHandle != IntPtr.Zero ? windowHandle : IntPtr.Zero;
        }

        public static IntPtr GetWindowLong(int nIndex)
        {
            var windowHandle = Get();
            
            return GetWindowLong(windowHandle, nIndex);
        }

        public static IntPtr SetWindowLong(int nIndex, IntPtr dwNewLong)
        {
            var windowHandle = Get();

            if (windowHandle == IntPtr.Zero) { return IntPtr.Zero; }

            if (IntPtr.Size == 4)
            {
                return SetWindowLong32(windowHandle, nIndex, dwNewLong);
            }

            return SetWindowLongPtr64(windowHandle, nIndex, dwNewLong);
        }

        [MonoPInvokeCallback(typeof(EnumWindowsDelegate))]
        private static bool EnumWindowCallBack(IntPtr hWnd, IntPtr lparam)
        {
            var textLen = GetWindowTextLength(hWnd);

            if (0 < textLen)
            {
                var tsb = new StringBuilder(textLen + 1);

                GetWindowText(hWnd, tsb, tsb.Capacity);

                if (tsb.ToString() == WindowTitle)
                {
                    GetWindowThreadProcessId(hWnd, out var pid);
                    
                    if (processId == pid)
                    {
                        windowHandle = hWnd;

                        return false;
                    }
                }
            }

            return true;
        }
    }
}

#endif


#if UNITY_STANDALONE_WIN

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Extensions;

namespace Modules.StandAloneWindows
{
    public sealed class WindowHandle : Singleton<WindowHandle>
    {
        //----- params -----

        //----- field -----

        private IntPtr windowHandle = IntPtr.Zero;

        #region WINAPI
        
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        #endregion

        //----- property -----

        //----- method -----

        /// <summary> Window handle of Unity window. </summary>
        public static IntPtr Get()
        {
            if (Instance.windowHandle != IntPtr.Zero)
            {
                return Instance.windowHandle;
            }

            var title = Application.productName;

            Instance.windowHandle = FindWindow(null, title);
            
            return Instance.windowHandle != IntPtr.Zero ? Instance.windowHandle : IntPtr.Zero;
        }

        public static IntPtr GetWindowLong(int nIndex)
        {
            var windowHandle = Get();
            
            return GetWindowLong(windowHandle, nIndex);
        }

        public static IntPtr SetWindowLong(int nIndex, IntPtr dwNewLong)
        {
            var windowHandle = Get();

            if (IntPtr.Size == 4)
            {
                return SetWindowLong32(windowHandle, nIndex, dwNewLong);
            }

            return SetWindowLongPtr64(windowHandle, nIndex, dwNewLong);
        }
    }
}

#endif

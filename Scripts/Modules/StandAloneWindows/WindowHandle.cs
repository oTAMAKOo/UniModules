
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

        #region WINAPI

        private const string UnityWndClassname = "UnityWndClass";

        #endregion

        //----- field -----

        private IntPtr windowHandle = IntPtr.Zero;

        #region WINAPI
        
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string className, string windowName);
        
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
    }
}

#endif

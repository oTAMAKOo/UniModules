
#if UNITY_STANDALONE_WIN

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Extensions;

namespace Modules.StandAloneWindows
{
    // Window-Style Define.
    // https://docs.microsoft.com/en-us/windows/win32/winmsg/window-styles?redirectedfrom=MSDN
    
    [Flags]
    public enum WindowStyles : uint
    {
        WS_OVERLAPPED = 0x00000000,
        WS_POPUP = 0x80000000,
        WS_CHILD = 0x40000000,
        WS_MINIMIZE = 0x20000000,
        WS_VISIBLE = 0x10000000,
        WS_DISABLED = 0x08000000,
        WS_CLIPSIBLINGS = 0x04000000,
        WS_CLIPCHILDREN = 0x02000000,
        WS_MAXIMIZE = 0x01000000,
        WS_CAPTION = 0x00C00000, // WS_BORDER | WS_DLGFRAME
        WS_BORDER = 0x00800000,
        WS_DLGFRAME = 0x00400000,
        WS_VSCROLL = 0x00200000,
        WS_HSCROLL = 0x00100000,
        WS_SYSMENU = 0x00080000,
        WS_THICKFRAME = 0x00040000,
        WS_GROUP = 0x00020000,
        WS_TABSTOP = 0x00010000,

        WS_MINIMIZEBOX = 0x00020000,
        WS_MAXIMIZEBOX = 0x00010000,

        WS_TILED = WS_OVERLAPPED,
        WS_ICONIC = WS_MINIMIZE,
        WS_SIZEBOX = WS_THICKFRAME,
        WS_TILEDWINDOW = WS_OVERLAPPEDWINDOW,
                      
        //------ Common Window Styles ------

        WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,

        WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,


        WS_CHILDWINDOW = WS_CHILD,
    }
    
    public sealed class WindowStyleHandler : Singleton<WindowStyleHandler>
    {
        //----- params -----

        #region WINAPI
        
        private const Int32 GWL_STYLE = -16;

        private const Int32 HWND_TOP = 0x0;

        private const uint SWP_NOSIZE = 0x1;
        private const uint SWP_NOMOVE = 0x2;
        
        private const int SWP_FRAMECHANGED = 0x0020;
        private const int SWP_SHOWWINDOW = 0x0040;
        
        #endregion

        //----- field -----

        private bool initialize = false;

        #region WINAPI
        
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern int SetWindowPos(IntPtr hwnd, int hwndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        #endregion

        //----- property -----

        public int WindowStyle { get; set; }

        //----- method -----

        public void Initialize()
        {
            if (Application.isEditor) { return; }
            
            if (initialize) { return; }

            WindowStyle = (int)GetStyle();

            initialize = true;
        }

        public IntPtr GetStyle()
        {
            if (Application.isEditor) { return IntPtr.Zero; }
            
            return WindowHandle.GetWindowLong(GWL_STYLE);
        }

        public void Apply()
        {
            if (Application.isEditor) { return; }
            
            var windowHandle = WindowHandle.Get();

            WindowHandle.SetWindowLong(GWL_STYLE, (IntPtr)WindowStyle);

            SetWindowPos(windowHandle, HWND_TOP, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
        }
    }
}

#endif

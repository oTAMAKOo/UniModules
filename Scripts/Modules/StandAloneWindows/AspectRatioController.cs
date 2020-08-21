
#if UNITY_STANDALONE

// Reference from.
// https://github.com/DenchiSoft/UnityAspectRatioController/blob/master/AspectRatioControllerTest/Assets/AspectRatioController/AspectRatioController.cs

using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using UniRx;
using Extensions;

namespace Modules.StandAloneWindows.WindowAspectRatio
{
    public sealed class AspectRatioController : SingletonMonoBehaviour<AspectRatioController>
    {
        //----- params -----

        public sealed class ResolutionChangeInfo
        {
            /// <summary> 幅 </summary>
            public float Width { get; private set; }
            /// <summary> 高さ </summary>
            public float Height { get; private set; }
            /// <summary> フルスクリーンか </summary>
            public bool FullScreen { get; private set; }

            public ResolutionChangeInfo(float width, float height, bool fullScreen)
            {
                Width = width;
                Height = height;
                FullScreen = fullScreen;
            }
        }

        #region WINAPI

        private const int WM_SIZING = 0x214;

        private const int WMSZ_LEFT   = 1;
        private const int WMSZ_RIGHT  = 2;
        private const int WMSZ_TOP    = 3;
        private const int WMSZ_BOTTOM = 6;

        private const int GWLP_WNDPROC = -4;

        private const string UNITY_WND_CLASSNAME = "UnityWndClass";
        
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        //----- field -----

        [SerializeField]
        private bool allowFullscreen = true;
        [SerializeField]
        private float aspectRatioWidth = 16;
        [SerializeField]
        private float aspectRatioHeight = 9;
        
        [SerializeField]
        private int minWidthPixel = 512;
        [SerializeField]
        private int minHeightPixel = 512;
        [SerializeField]
        private int maxWidthPixel = 2048;
        [SerializeField]
        private int maxHeightPixel = 2048;
 
        private Subject<ResolutionChangeInfo> onResolutionChanged = null;

        private int setWidth = -1;
        private int setHeight = -1;

        private bool wasFullscreenLastFrame = false;

        private int pixelHeightOfCurrentScreen = 0;
        private int pixelWidthOfCurrentScreen = 0;

        private bool quitStarted = false;

        #region WINAPI

        // Delegate to set as new WindowProc callback function.
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate wndProcDelegate;

        // Retrieves the thread identifier of the calling thread.
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        // Retrieves the name of the class to which the specified window belongs.
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // Enumerates all nonchild windows associated with a thread by passing the handle to
        // each window, in turn, to an application-defined callback function.
        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // Passes message information to the specified window procedure.
        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // Retrieves the dimensions of the bounding rectangle of the specified window.
        // The dimensions are given in screen coordinates that are relative to the upper-left corner of the screen.
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hwnd, ref RECT lpRect);

        // Retrieves the coordinates of a window's client area. The client coordinates specify the upper-left
        // and lower-right corners of the client area. Because client coordinates are relative to the upper-left
        // corner of a window's client area, the coordinates of the upper-left corner are (0,0).
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

        // Changes an attribute of the specified window. The function also sets the 32-bit (long) value
        // at the specified offset into the extra window memory.
        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // Changes an attribute of the specified window. The function also sets a value at the specified
        // offset in the extra window memory. 
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // Window handle of Unity window.
        private IntPtr unityHWnd;

        // Pointer to old WindowProc callback function.
        private IntPtr oldWndProcPtr;

        // Pointer to our own WindowProc callback function.
        private IntPtr newWndProcPtr;

        #endregion

        private bool initialized = false;

        //----- property -----

        public float AspectRatio { get; private set; }

        //----- method -----

        public void Initialize()
        {
            if (Application.isEditor) { return; }

            if (initialized) { return; }

            Application.wantsToQuit += ApplicationWantsToQuit;

            pixelHeightOfCurrentScreen = Screen.currentResolution.height;
            pixelWidthOfCurrentScreen = Screen.currentResolution.width;

            setHeight = Screen.height;
            setWidth = Mathf.RoundToInt(Screen.height * AspectRatio);

            wasFullscreenLastFrame = Screen.fullScreen;
            
            EnumThreadWindows(GetCurrentThreadId(), FindWindowProc, IntPtr.Zero);

            SetAspectRatio(aspectRatioWidth, aspectRatioHeight);

            Apply();

            wndProcDelegate = WindowProc;

            newWndProcPtr = Marshal.GetFunctionPointerForDelegate(wndProcDelegate);
            oldWndProcPtr = SetWindowLong(unityHWnd, GWLP_WNDPROC, newWndProcPtr);

            initialized = true;
        }

        public void SetAllowFullscreen(bool allowFullscreen)
        {
            this.allowFullscreen = allowFullscreen;
        }

        public void SetMinSize(int minWidthPixel, int minHeightPixel)
        {
            this.minWidthPixel = minWidthPixel;
            this.minHeightPixel = minHeightPixel;
        }

        public void SetMaxSize(int maxWidthPixel, int maxHeightPixel)
        {
            this.maxWidthPixel = maxWidthPixel;
            this.maxHeightPixel = maxHeightPixel;
        }

        public void SetAspectRatio(float newAspectWidth, float newAspectHeight)
        {
            aspectRatioWidth = newAspectWidth;
            aspectRatioHeight = newAspectHeight;
            AspectRatio = aspectRatioWidth / aspectRatioHeight;
        }

        public void Apply()
        {
            var width = Mathf.Clamp(Screen.width, minWidthPixel, maxWidthPixel);

            var height = Mathf.RoundToInt(Screen.width / AspectRatio);

            height = Mathf.Clamp(height, minHeightPixel, maxHeightPixel);

            Screen.SetResolution(width, height, allowFullscreen);
        }

        [MonoPInvokeCallback(typeof(EnumWindowsProc))]
        public static bool FindWindowProc(IntPtr hWnd, IntPtr lParam)
        {
            var classText = new StringBuilder(UNITY_WND_CLASSNAME.Length + 1);

            GetClassName(hWnd, classText, classText.Capacity);

            if (classText.ToString() == UNITY_WND_CLASSNAME)
            {
                Instance.unityHWnd = hWnd;
                return false;
            }

            return true;
        }

        [MonoPInvokeCallback(typeof(WndProcDelegate))]
        private static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_SIZING)
            {
                // Get window size struct.
                var rc = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));

                // Calculate window border width and height.
                var windowRect = new RECT();
                GetWindowRect(Instance.unityHWnd, ref windowRect);

                var clientRect = new RECT();
                GetClientRect(Instance.unityHWnd, ref clientRect);

                var borderWidth = windowRect.Right - windowRect.Left - (clientRect.Right - clientRect.Left);
                var borderHeight = windowRect.Bottom - windowRect.Top - (clientRect.Bottom - clientRect.Top);

                // Remove borders (including window title bar) before applying aspect ratio.
                rc.Right -= borderWidth;
                rc.Bottom -= borderHeight;

                // Clamp window size.
                var newWidth = Mathf.Clamp(rc.Right - rc.Left, Instance.minWidthPixel, Instance.maxWidthPixel);
                var newHeight = Mathf.Clamp(rc.Bottom - rc.Top, Instance.minHeightPixel, Instance.maxHeightPixel);

                // Resize according to aspect ratio and resize direction.
                switch (wParam.ToInt32())
                {
                    case WMSZ_LEFT:
                        rc.Left = rc.Right - newWidth;
                        rc.Bottom = rc.Top + Mathf.RoundToInt(newWidth / Instance.AspectRatio);
                        break;
                    case WMSZ_RIGHT:
                        rc.Right = rc.Left + newWidth;
                        rc.Bottom = rc.Top + Mathf.RoundToInt(newWidth / Instance.AspectRatio);
                        break;
                    case WMSZ_TOP:
                        rc.Top = rc.Bottom - newHeight;
                        rc.Right = rc.Left + Mathf.RoundToInt(newHeight * Instance.AspectRatio);
                        break;
                    case WMSZ_BOTTOM:
                        rc.Bottom = rc.Top + newHeight;
                        rc.Right = rc.Left + Mathf.RoundToInt(newHeight * Instance.AspectRatio);
                        break;
                    case WMSZ_RIGHT + WMSZ_BOTTOM:
                        rc.Right = rc.Left + newWidth;
                        rc.Bottom = rc.Top + Mathf.RoundToInt(newWidth / Instance.AspectRatio);
                        break;
                    case WMSZ_RIGHT + WMSZ_TOP:
                        rc.Right = rc.Left + newWidth;
                        rc.Top = rc.Bottom - Mathf.RoundToInt(newWidth / Instance.AspectRatio);
                        break;
                    case WMSZ_LEFT + WMSZ_BOTTOM:
                        rc.Left = rc.Right - newWidth;
                        rc.Bottom = rc.Top + Mathf.RoundToInt(newWidth / Instance.AspectRatio);
                        break;
                    case WMSZ_LEFT + WMSZ_TOP:
                        rc.Left = rc.Right - newWidth;
                        rc.Top = rc.Bottom - Mathf.RoundToInt(newWidth / Instance.AspectRatio);
                        break;
                }

                Instance.setWidth = rc.Right - rc.Left;
                Instance.setHeight = rc.Bottom - rc.Top;

                rc.Right += borderWidth;
                rc.Bottom += borderHeight;

                if (Instance.onResolutionChanged != null)
                {
                    var resolutionChangeInfo = new ResolutionChangeInfo(Instance.setWidth, Instance.setHeight, Screen.fullScreen);

                    Instance.onResolutionChanged.OnNext(resolutionChangeInfo);
                }

                Marshal.StructureToPtr(rc, lParam, true);
            }
            
            return CallWindowProc(Instance.oldWndProcPtr, hWnd, msg, wParam, lParam);
        }

        void Update()
        {
            if (!initialized) { return; }

            if (!allowFullscreen && Screen.fullScreen)
            {
                Screen.fullScreen = false;
            }

            var isFullScreen = Screen.fullScreen;

            if (isFullScreen && !wasFullscreenLastFrame)
            {
                var width = 0;
                var height = 0;

                var blackBarsLeftRight = AspectRatio < (float)pixelWidthOfCurrentScreen / pixelHeightOfCurrentScreen;

                if (blackBarsLeftRight)
                {
                    height = pixelHeightOfCurrentScreen;
                    width = Mathf.RoundToInt(pixelHeightOfCurrentScreen * AspectRatio);
                }
                else
                {
                    width = pixelWidthOfCurrentScreen;
                    height = Mathf.RoundToInt(pixelWidthOfCurrentScreen / AspectRatio);
                }

                Screen.SetResolution(width, height, isFullScreen);
                
                if (onResolutionChanged != null)
                {
                    var resolutionChangeInfo = new ResolutionChangeInfo(width, height, isFullScreen);

                    onResolutionChanged.OnNext(resolutionChangeInfo);
                }
            }
            else if (!isFullScreen && wasFullscreenLastFrame)
            {
                Screen.SetResolution(setWidth, setHeight, isFullScreen);
                
                if (onResolutionChanged != null)
                {
                    var resolutionChangeInfo = new ResolutionChangeInfo(setWidth, setHeight, isFullScreen);

                    onResolutionChanged.OnNext(resolutionChangeInfo);
                }
            }
            else if (!isFullScreen && (Screen.width != setWidth || Screen.height != setHeight))
            {
                setHeight = Screen.height;
                setWidth = Mathf.RoundToInt(Screen.height * AspectRatio);

                Screen.SetResolution(setWidth, setHeight, isFullScreen);

                if (onResolutionChanged != null)
                {
                    var resolutionChangeInfo = new ResolutionChangeInfo(setWidth, setHeight, isFullScreen);

                    onResolutionChanged.OnNext(resolutionChangeInfo);
                }
            }
            else if (!isFullScreen)
            {
                pixelHeightOfCurrentScreen = Screen.currentResolution.height;
                pixelWidthOfCurrentScreen = Screen.currentResolution.width;
            }
            
            wasFullscreenLastFrame = isFullScreen;
            
            #if UNITY_EDITOR

            if (Screen.width != setWidth || Screen.height != setHeight)
            {
                setWidth = Screen.width;
                setHeight = Screen.height;

                if (onResolutionChanged != null)
                {
                    var resolutionChangeInfo = new ResolutionChangeInfo(setWidth, setHeight, isFullScreen);

                    onResolutionChanged.OnNext(resolutionChangeInfo);
                }
            }

            #endif
        }

        private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 4)
            {
                return SetWindowLong32(hWnd, nIndex, dwNewLong);
            }

            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }

        private bool ApplicationWantsToQuit()
        {
            if (!initialized) { return false; }
            
            if (!quitStarted)
            {
                StartCoroutine(DelayedQuit());

                return false;
            }

            return true;
        }

        private IEnumerator DelayedQuit()
        {
            SetWindowLong(unityHWnd, GWLP_WNDPROC, oldWndProcPtr);

            yield return new WaitForEndOfFrame();

            quitStarted = true;

            Application.Quit();
        }

        public IObservable<ResolutionChangeInfo> OnResolutionChangedAsObservable()
        {
            return onResolutionChanged ?? (onResolutionChanged = new Subject<ResolutionChangeInfo>());
        }
    }
}

#endif

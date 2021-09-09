
#if UNITY_STANDALONE_WIN

// Reference from.
// https://github.com/DenchiSoft/UnityAspectRatioController/blob/master/AspectRatioControllerTest/Assets/AspectRatioController/AspectRatioController.cs

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AOT;
using UniRx;
using Extensions;

namespace Modules.StandAloneWindows
{
    public sealed class AspectRatioHandler : Singleton<AspectRatioHandler>
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

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        #endregion

        //----- field -----

        private bool allowFullscreen = true;

        private float aspectRatioWidth = 16;
        private float aspectRatioHeight = 9;
        
        private int minWidthPixel = 512;
        private int minHeightPixel = 512;

        private int maxWidthPixel = 2048;
        private int maxHeightPixel = 2048;
 
        private Subject<ResolutionChangeInfo> onResolutionChanged = null;

        private int setWidth = -1;
        private int setHeight = -1;

        private bool wasFullscreenLastFrame = false;

        private int pixelHeightOfCurrentScreen = 0;
        private int pixelWidthOfCurrentScreen = 0;

        #region WINAPI
        
        private IntPtr oldWndProcPtr;
        private IntPtr newWndProcPtr;

        private WndProcDelegate wndProcDelegate;

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hwnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

        #endregion

        private bool initialized = false;

        //----- property -----

        public float AspectRatio { get; private set; }

        //----- method -----

        public void Initialize()
        {
            if (Application.isEditor) { return; }

            if (initialized) { return; }

            Application.quitting += ApplicationQuitting;

            pixelHeightOfCurrentScreen = Screen.currentResolution.height;
            pixelWidthOfCurrentScreen = Screen.currentResolution.width;

            setHeight = Screen.height;
            setWidth = Mathf.RoundToInt(Screen.height * AspectRatio);

            wasFullscreenLastFrame = Screen.fullScreen;

            SetAspectRatio(aspectRatioWidth, aspectRatioHeight);
            
            Apply();

            wndProcDelegate = WindowProc;
            
            newWndProcPtr = Marshal.GetFunctionPointerForDelegate(wndProcDelegate);
            oldWndProcPtr = WindowHandle.SetWindowLong(GWLP_WNDPROC, newWndProcPtr);

            Task.Run(() => UpdateAspectRatio());

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

        [MonoPInvokeCallback(typeof(WndProcDelegate))]
        private static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            var windowHandle = WindowHandle.Get();
            
            if (msg == WM_SIZING)
            {
                var rc = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));

                var windowRect = new RECT();
                GetWindowRect(windowHandle, ref windowRect);

                var clientRect = new RECT();
                GetClientRect(windowHandle, ref clientRect);

                var borderWidth = windowRect.Right - windowRect.Left - (clientRect.Right - clientRect.Left);
                var borderHeight = windowRect.Bottom - windowRect.Top - (clientRect.Bottom - clientRect.Top);

                rc.Right -= borderWidth;
                rc.Bottom -= borderHeight;

                var newWidth = Mathf.Clamp(rc.Right - rc.Left, Instance.minWidthPixel, Instance.maxWidthPixel);
                var newHeight = Mathf.Clamp(rc.Bottom - rc.Top, Instance.minHeightPixel, Instance.maxHeightPixel);

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

                Marshal.StructureToPtr(rc, lParam, true);
            }
            
            return CallWindowProc(Instance.oldWndProcPtr, hWnd, msg, wParam, lParam);
        }

        private async Task UpdateAspectRatio()
        {
            while (true)
            {
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

                await Task.Delay(10);
            }
        }

        private void ApplicationQuitting()
        {
            WindowHandle.SetWindowLong(GWLP_WNDPROC, oldWndProcPtr);
        }

        public IObservable<ResolutionChangeInfo> OnResolutionChangedAsObservable()
        {
            return onResolutionChanged ?? (onResolutionChanged = new Subject<ResolutionChangeInfo>());
        }
    }
}

#endif

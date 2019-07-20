
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Extensions;
using UniRx;

using Object = UnityEngine.Object;

namespace Modules.Devkit.WebView
{
    public class WebViewHook : ScriptableObject
    {
        //----- params -----

        private enum WebViewMethodType
        {
            Show,
            Hide,
            Back,
            Reload,
            Forward,
            SetFocus,
            InitWebView,
            SetSizeAndPosition,
            SetHostView,
            AllowRightClickMenu,
            SetDelegateObject,
            ExecuteJavascript,
            LoadURL,
            HasApplicationFocus,
            SetApplicationFocus,
        }

        //----- field -----

        private string url = null;

        private Object webView = null;
        private EditorWindow host = null;
        private object hostCache = null;

        private Subject<string> onLocationChanged = null;
        private Subject<string> onLoadError = null;
        private Subject<Unit> onInitScripting = null;

        private static Type webViewType = null;
        private static Dictionary<WebViewMethodType, MethodInfo> webViewMethods = null;
        private static FieldInfo parentField = null;
        private static Func<Rect, Rect> unclipMethod = null;

        //----- property -----

        public string Url
        {
            get { return url; }
            set
            {
                url = value;
                LoadURL(url);
            }
        }

        //----- method -----

        static WebViewHook()
        {
            const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

            webViewType = Assembly.Load("UnityEditor.dll").GetType("UnityEditor.WebView");

            Action<WebViewMethodType> SetWebViewMethod = (methodType) =>
            {
                var methodName = Enum.GetName(typeof(WebViewMethodType), methodType);
                var method = webViewType.GetMethod(methodName, bindingFlags);

                webViewMethods.Add(methodType, method);
            };

            webViewMethods = new Dictionary<WebViewMethodType, MethodInfo>();

            var webViewMethodTypes = Enum.GetValues(typeof(WebViewMethodType)).Cast<WebViewMethodType>();

            foreach (var webViewMethodType in webViewMethodTypes)
            {
                SetWebViewMethod(webViewMethodType);
            }

            parentField = typeof(EditorWindow).GetField("m_Parent", bindingFlags);

            unclipMethod = (Func<Rect, Rect>)Delegate.CreateDelegate(
                typeof(Func<Rect, Rect>), typeof(GUI).Assembly.GetTypes()
                    .First(x => x.Name == "GUIClip")
                    .GetMethod("Unclip", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Rect) }, null));
        }

        ~WebViewHook()
        {
            OnDisable();
        }

        void OnEnable()
        {
            if (!webView)
            {
                webView = CreateInstance(webViewType);
                webView.hideFlags = HideFlags.DontSave;
                this.hideFlags = HideFlags.DontSave;
            }
        }

        void OnDisable()
        {
            if (webView != null)
            {
                Detach();
            }
        }

        void OnDestroy()
        {
            DestroyImmediate(webView);
            webView = null;
        }

        public bool Hook(EditorWindow host)
        {
            if (this.host == host){ return false; }

            if (webView == null)
            {
                OnEnable();
            }

            Invoke(WebViewMethodType.InitWebView, parentField.GetValue(hostCache = (this.host = host)), 0, 0, 1, 1, false);
            Invoke(WebViewMethodType.SetDelegateObject, this);
            Invoke(WebViewMethodType.AllowRightClickMenu, true);

            return true;
        }

        public void SetFocus(bool focus)
        {
            if (webView == null) { return; }

            Invoke(WebViewMethodType.SetFocus, focus);
        }

        public void Detach()
        {
            Invoke(WebViewMethodType.SetHostView, hostCache = null);
        }

        public void SetHostView(object host)
        {
            Invoke(WebViewMethodType.SetHostView, hostCache = host);
            Hide();
            Show();
            SetApplicationFocus(true);
        }

        private void SetSizeAndPosition(Rect position)
        {
            Invoke(WebViewMethodType.SetSizeAndPosition, (int)position.x, (int)position.y, (int)position.width, (int)position.height);
        }

        void OnGUI() { }

        public void OnGUI(Rect r)
        {
            if (webView == null) { return; }

            if (host != null)
            {
                var h = parentField.GetValue(host);

                if (hostCache != h)
                {
                    SetHostView(h);
                }
                else
                {
                    Invoke(WebViewMethodType.SetHostView, h);
                }
            }

            SetSizeAndPosition(unclipMethod(r));
        }

        public void AllowRightClickMenu(bool yes)
        {
            Invoke(WebViewMethodType.AllowRightClickMenu, yes);
        }

        public void Forward()
        {
            Invoke(WebViewMethodType.Forward);
        }

        public void Back()
        {
            Invoke(WebViewMethodType.Back);
        }

        public void Show()
        {
            Invoke(WebViewMethodType.Show);
        }

        public void Hide()
        {
            Invoke(WebViewMethodType.Hide);
        }

        public void Reload()
        {
            Invoke(WebViewMethodType.Reload);
        }

        public bool HasApplicationFocus()
        {
            var method = webViewMethods.GetValueOrDefault(WebViewMethodType.HasApplicationFocus);

            return (bool)method.Invoke(webView, null);
        }

        public void SetApplicationFocus(bool focus)
        {
            Invoke(WebViewMethodType.SetApplicationFocus, focus);
        }

        public void LoadURL(string url)
        {
            this.url = url;

            Invoke(WebViewMethodType.LoadURL, url);
        }

        public void LoadHTML(string html)
        {
            Invoke(WebViewMethodType.LoadURL, "data:text/html;charset=utf-8," + html);
        }

        public void LoadFile(string path)
        {
            Invoke(WebViewMethodType.LoadURL, "file:///" + path);
        }
        
        public void ExecuteJavascript(string js)
        {
            Invoke(WebViewMethodType.ExecuteJavascript, js);
        }

        void Invoke(WebViewMethodType methodType, params object[] args)
        {
            try
            {
                var method = webViewMethods.GetValueOrDefault(methodType);

                if (method == null)
                {
                    Debug.LogErrorFormat("Method not found. [{0}]", methodType.ToString());
                }

                method.Invoke(webView, args);
            }
            catch
            {
                // ignored
            }
        }

        public IObservable<string> OnLocationChangedAsObservable()
        {
            return onLocationChanged ?? (onLocationChanged = new Subject<string>());
        }

        public IObservable<string> OnLoadErrorAsObservable()
        {
            return onLoadError ?? (onLoadError = new Subject<string>());
        }

        public IObservable<Unit> OnInitScriptingAsObservable()
        {
            return onInitScripting ?? (onInitScripting = new Subject<Unit>());
        }

        /* Default bindings for SetDelegateObject */

        protected virtual void OnLocationChanged(string url)
        {
            if (url.StartsWith("http://") || url.StartsWith("https://"))
            {
                this.url = url;

                if (onLocationChanged != null)
                {
                    onLocationChanged.OnNext(url);
                }
            }
        }

        protected virtual void OnLoadError(string url)
        {
            if (onLoadError != null)
            {
                onLoadError.OnNext(url);
            }
            else
            {
                Debug.LogError("WebView has failed to load " + url);
            }
        }

        protected virtual void OnInitScripting()
        {
            if (onInitScripting != null)
            {
                onInitScripting.OnNext(Unit.Default);
            }
        }

        protected virtual void OnOpenExternalLink(string url)
        {
            if (url.StartsWith("http://") || url.StartsWith("https://"))
            {
                Application.OpenURL(url);
            }
        }

        protected virtual void OnWebViewDirty()
        {
            // This binding may not work
        }

        protected virtual void OnDownloadProgress(string id, string message, ulong bytes, ulong total)
        {
            // This binding may not work
        }

        protected virtual void OnBatchMode()
        {
            // This binding may not work
        }

        protected virtual void OnReceiveTitle(string title)
        {
            // This binding may not work
        }

        protected virtual void OnDomainReload()
        {
            // This binding may not work
        }

    }
}

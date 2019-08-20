
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using Extensions;

using Object = UnityEngine.Object;

namespace Modules.Devkit.WebView
{
    public abstract class EditorWebViewWindow : EditorWindow
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

        private static readonly BindingFlags BindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        //----- field -----

        [SerializeField]
        private string url = null;

        private string currentUrl = null;
        
        private Object webView = null;
        private bool syncingFocus = false;

        private Subject<string> onLocationChanged = null;
        private Subject<string> onLoadError = null;
        private Subject<Unit> onInitScripting = null;

        private static Type webViewType = null;
        private static Dictionary<WebViewMethodType, MethodInfo> webViewMethods = null;
        private static FieldInfo parentField = null;
        private static Func<Rect, Rect> unclipMethod = null;

        private LifetimeDisposable lifetimeDisposable = new LifetimeDisposable();

        private bool initialized = false;

        //----- property -----

        public string Url { get { return url; } }

        //----- method -----

        static EditorWebViewWindow()
        {
            var unityEditorAssembly = Assembly.Load("UnityEditor.dll");

            webViewType = unityEditorAssembly.GetType("UnityEditor.WebView");

            Action<WebViewMethodType> SetWebViewMethod = (methodType) =>
            {
                var methodName = Enum.GetName(typeof(WebViewMethodType), methodType);
                var method = webViewType.GetMethod(methodName, BindingFlags);

                webViewMethods.Add(methodType, method);
            };

            webViewMethods = new Dictionary<WebViewMethodType, MethodInfo>();

            var webViewMethodTypes = Enum.GetValues(typeof(WebViewMethodType)).Cast<WebViewMethodType>();

            foreach (var webViewMethodType in webViewMethodTypes)
            {
                SetWebViewMethod(webViewMethodType);
            }

            parentField = typeof(EditorWindow).GetField("m_Parent", BindingFlags);

            unclipMethod = (Func<Rect, Rect>)Delegate.CreateDelegate(
                 typeof(Func<Rect, Rect>), typeof(GUI).Assembly.GetTypes()
                 .First(x => x.Name == "GUIClip")
                 .GetMethod("Unclip", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Rect) }, null));
        }

        public static T Open<T>(string title, string url) where T : EditorWebViewWindow
        {
            var editorWindow = GetWindow<T>(title, new Type[] { typeof(SceneView) });

            editorWindow.Initialize();

            editorWindow.LoadURL(url);

            editorWindow.Show(true);

            return editorWindow;
        }

        private void Initialize()
        {
            if (initialized) { return; }

            OnLocationChangedAsObservable()
                .Subscribe(x =>
                   {
                       currentUrl = x;

                       if (Event.current.type != EventType.Repaint)
                       {
                           Repaint();
                       }
                   })
                .AddTo(lifetimeDisposable.Disposable);

            var webViewRect = GetWebViewRect();

            InitWebView(webViewRect);

            GUI.FocusControl(string.Empty);

            initialized = true;
        }

        private Rect GetWebViewRect()
        {
            return unclipMethod.Invoke(new Rect(0.0f, 20f, position.width, position.height - 20f));
        }

        void OnBecameInvisible()
        {
            if (webView != null)
            {
                DetachHostView();
                HideWebView();
            }

            Repaint();
        }

        void OnDestroy()
        {
            UnityEngine.Object.DestroyImmediate(webView);
        }

        void OnFocus()
        {
            if (webView != null)
            {
                SetFocus(true);
            }

            Repaint();
        }

        void OnLostFocus()
        {
            if (webView != null)
            {
                SetFocus(false);
            }

            Repaint();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope("Toolbar", GUILayout.Height(26f), GUILayout.ExpandWidth(true)))
            {
                var backIcon = EditorGUIUtility.IconContent("Profiler.PrevFrame");

                if (GUILayout.Button(backIcon, EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    Back();
                }

                var forwardIcon = EditorGUIUtility.IconContent("Profiler.NextFrame");

                if (GUILayout.Button(forwardIcon, EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    Forward();
                }

                var reloadIcon = EditorGUIUtility.IconContent("LookDevResetEnv");

                if (GUILayout.Button(reloadIcon, EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    Reload();
                }

                EditorGUI.BeginChangeCheck();

                var newUrl = EditorGUILayout.DelayedTextField(url);

                if (EditorGUI.EndChangeCheck())
                {
                    LoadURL(newUrl);
                    GUI.FocusControl(string.Empty);
                }
            }


            if (webView != null)
            {
                var webViewRect = GetWebViewRect();

                SetSizeAndPosition(webViewRect);

                if (Event.current.type == EventType.Repaint)
                {
                    SetHostView();
                }
            }
        }

        public void OpenUrl(string url)
        {
            if (webView == null) { return; }

            LoadURL(url);
        }

        public void Refresh()
        {
            HideWebView();
            ShowWebView();
        }

        #region WebView

        private void InitWebView(Rect webViewRect)
        {
            if (webView == null)
            {
                webView = ScriptableObject.CreateInstance(webViewType);
                webView.hideFlags = HideFlags.HideAndDontSave;

                var px = (int)webViewRect.x;
                var py = (int)webViewRect.y;
                var width = (int)webViewRect.width;
                var height = (int)webViewRect.height;

                Invoke(WebViewMethodType.InitWebView, parentField.GetValue(this), px, py, width, height, false);
            }

            Invoke(WebViewMethodType.SetDelegateObject, this);
            Invoke(WebViewMethodType.AllowRightClickMenu, true);
        }

        private void SetFocus(bool focus)
        {
            if (syncingFocus) { return; }

            syncingFocus = true;

            if (webView == null) { return; }

            if (focus)
            {
                SetHostView();
                ShowWebView();
            }
            else
            {
                DetachHostView();
            }

            Invoke(WebViewMethodType.SetFocus, focus);

            syncingFocus = false;
        }

        private void DetachHostView()
        {
            Invoke(WebViewMethodType.SetHostView, null);
        }

        private void SetHostView()
        {
            var parent = parentField.GetValue(this);

            Invoke(WebViewMethodType.SetHostView, parent);
        }

        private void SetSizeAndPosition(Rect webViewRect)
        {
            Invoke(WebViewMethodType.SetSizeAndPosition, (int)webViewRect.x, (int)webViewRect.y, (int)webViewRect.width, (int)webViewRect.height);
        }

        private void ShowWebView()
        {
            Invoke(WebViewMethodType.Show);
        }

        private void HideWebView()
        {
            Invoke(WebViewMethodType.Hide);
        }

        public void Forward()
        {
            Invoke(WebViewMethodType.Forward);
        }

        public void Back()
        {
            Invoke(WebViewMethodType.Back);
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
            url = string.Empty;

            Invoke(WebViewMethodType.LoadURL, "data:text/html;charset=utf-8," + html);
        }

        public void LoadFile(string path)
        {
            url = string.Empty;

            Invoke(WebViewMethodType.LoadURL, "file:///" + path);
        }

        public void ExecuteJavascript(string js)
        {
            Invoke(WebViewMethodType.ExecuteJavascript, js);
        }

        private void Invoke(WebViewMethodType methodType, params object[] args)
        {
            if (webView == null) { return; }

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

        #endregion
    }
}

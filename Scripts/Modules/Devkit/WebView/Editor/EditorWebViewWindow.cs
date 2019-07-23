
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Reflection;
using UniRx;
using Extensions;

namespace Modules.Devkit.WebView
{
    public abstract class EditorWebViewWindow : EditorWindow
    {
        //----- params -----

        private readonly BindingFlags BindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        //----- field -----

        [SerializeField]
        private string url = null;

        private LifetimeDisposable lifetimeDisposable = new LifetimeDisposable();

        private WebViewHook webView = null;
        private FieldInfo parentField = null;
        private MethodInfo parentRepaintMethod = null;
        private bool gotFocus = false;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        public string Url { get { return url; } }

        //----- method -----

        public static T Open<T>(string title, string url) where T : EditorWebViewWindow
        {
            var dockWindowType = Assembly.Load("UnityEditor.dll").GetType("UnityEditor.SceneView");

            var editorWindow = GetWindow<T>(title, new Type[] { dockWindowType });

            editorWindow.Initialize(url);

            editorWindow.ShowUtility();

            return editorWindow;
        }

        private void Initialize(string url)
        {
            if (initialized) { return; }

            this.url = url;

            InitWebView();

            webView.LoadURL(url);

            var hostViewType = Assembly.Load("UnityEditor.dll").GetType("UnityEditor.HostView");

            parentField = typeof(EditorWindow).GetField("m_Parent", BindingFlags);
            parentRepaintMethod = hostViewType.GetMethod("Repaint", BindingFlags);

            initialized = true;
        }

        private void InitWebView()
        {
            if (webView != null) { return; }

            webView = new WebViewHook();

            webView.Hook(this);

            webView.OnLocationChangedAsObservable()
                .Subscribe(x =>
                           {
                               url = x;
                               Repaint();
                           })
                .AddTo(lifetimeDisposable.Disposable);
        }

        void OnBecameInvisible()
        {
            if (webView != null)
            {
                webView.Detach();
            }
        }

        void OnEnable()
        {
            EditorApplication.update += TrackFocusState;
        }

        void OnDisable()
        {
            EditorApplication.update -= TrackFocusState;
        }

        void OnFocus()
        {
            if (webView != null)
            {
                webView.SetFocus(true);
            }

            OnGotFocus();

            Repaint();
        }

        void OnLostFocus()
        {
            if (webView != null)
            {
                webView.SetFocus(false);
            }

            InternalEditorUtility.RepaintAllViews();
        }

        void OnGUI()
        {
            if (webView == null)
            {
                InitWebView();
            }

            using (new EditorGUILayout.HorizontalScope("Toolbar", GUILayout.Height(26f), GUILayout.ExpandWidth(true)))
            {
                var backIcon = EditorGUIUtility.IconContent("Profiler.PrevFrame");

                if (GUILayout.Button(backIcon, EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    webView.Back();
                }

                var forwardIcon = EditorGUIUtility.IconContent("Profiler.NextFrame");

                if (GUILayout.Button(forwardIcon, EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    webView.Forward();
                }

                var reloadIcon = EditorGUIUtility.IconContent("LookDevResetEnv");

                if (GUILayout.Button(reloadIcon, EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    webView.Reload();
                }

                EditorGUI.BeginChangeCheck();

                var newUrl = EditorGUILayout.DelayedTextField(url);

                if (EditorGUI.EndChangeCheck())
                {
                    webView.LoadURL(newUrl);
                    webView.SetApplicationFocus(true);

                    GUI.FocusControl(string.Empty);
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                webView.OnGUI(new Rect(0, 20, position.width, position.height - 20));
            }
        }

        public void OpenUrl(string url)
        {
            if (webView == null) { return; }

            webView.LoadURL(url);
        }

        public void Reload()
        {
            if (webView == null) { return; }

            webView.Reload();
        }
        
        public void Refresh()
        {
            webView.Hide();
            webView.Show();
        }

        private void OnGotFocus()
        {
            gotFocus = true;

            Focus();

            if (parentField != null && parentRepaintMethod != null)
            {
                var parent = parentField.GetValue(this);

                parentRepaintMethod.Invoke(parent, null);
            }
        }

        private void OnTakeFocus()
        {
            gotFocus = false;

            if (parentField != null && parentRepaintMethod != null)
            {
                var parent = parentField.GetValue(this);

                parentRepaintMethod.Invoke(parent, null);
            }

            if (webView != null)
            {
                webView.Detach();
            }
        }

        private void TrackFocusState()
        {
            if (!gotFocus) { return; }

            if (this != focusedWindow)
            {
                OnTakeFocus();
            }
        }
    }
}

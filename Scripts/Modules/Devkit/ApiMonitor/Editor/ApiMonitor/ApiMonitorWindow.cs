
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Networking
{
    public sealed class ApiMonitorWindow : SingletonEditorWindow<ApiMonitorWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(300f, 250f);

        private static readonly Color SelectionLineColor = new Color(0f, 1f, 0f, 0.2f);
        private static readonly Color EvenLineColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
        private static readonly Color OddLineColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);

        //----- field -----

        private int selectionIndex = -1;
        
        private Vector2 scrollPosition = Vector2.zero;

        private WebRequestInfo[] contents = null;

        private GUIStyle statusLabelStyle = null;
        private GUIStyle urlLabelStyle = null;

        private Dictionary<WebRequestInfo.RequestType, Texture2D> statusLabelTexture = null;

        [NonSerialized]
        private bool initialized = false;
        
        //----- property -----

        //----- method -----
        
        public static void Open()
        {
            Instance.Initialize();

            Instance.Show();
        }

        private void Initialize()
        {
            if (initialized) { return; }

            var apiMonitorBridge = ApiMonitorBridge.Instance;

            titleContent = new GUIContent("ApiMonitor");

            minSize = WindowSize;

            contents = apiMonitorBridge.GetHistory();

            //------ Event ------

            apiMonitorBridge.OnUpdateInfoAsObservable()
                .Subscribe(_ => UpdateContents())
                .AddTo(Disposable);

            //------ Texture ------

            statusLabelTexture = new Dictionary<WebRequestInfo.RequestType, Texture2D>()
            {
                { WebRequestInfo.RequestType.Post,   EditorGUIUtility.FindTexture("sv_label_3") },
                { WebRequestInfo.RequestType.Put,    EditorGUIUtility.FindTexture("sv_label_5") },
                { WebRequestInfo.RequestType.Get,    EditorGUIUtility.FindTexture("sv_label_1") },
                { WebRequestInfo.RequestType.Delete, EditorGUIUtility.FindTexture("sv_label_7") },
            };

            initialized = true;
        }

        void OnGUI()
        {
            Initialize();

            InitializeStyle();

            DrawToolbarGUI();

            DrawApiHistoryGUI();

            DrawApiDetailGUI();
        }

        private void DrawToolbarGUI()
        {
            var apiMonitorBridge = ApiMonitorBridge.Instance;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                EditorGUILayout.LabelField(apiMonitorBridge.ServerUrl, urlLabelStyle, GUILayout.Width(500f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                {
                    apiMonitorBridge.Clear();
                    scrollPosition = Vector2.zero;
                }
            }
        }

        private void DrawApiHistoryGUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorLayoutTools.TextAreaStyle))
            {
                GUILayout.Space(2f);

                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
                {
                    for (var i = 0; i < contents.Length; i++)
                    {
                        var content = contents[i];

                        var selection = i == selectionIndex;

                        var backgroundColor = Color.clear;

                        if (!selection)
                        {
                            backgroundColor = i % 2 == 0 ? EvenLineColor : OddLineColor;
                        }
                        else
                        {
                            backgroundColor = SelectionLineColor;
                        }

                        var contentRect = new Rect();

                        using (var horizontalScope = new EditorGUILayout.HorizontalScope(GUILayout.Height(18f)))
                        {
                            contentRect = horizontalScope.rect;

                            using (new BackgroundColorScope(backgroundColor))
                            {
                                var rect = new Rect(contentRect);

                                rect.height += 2.5f;

                                GUI.Box(rect, GUIContent.none);
                            }

                            GUILayout.Space(5f);

                            using (new EditorGUILayout.VerticalScope())
                            {
                                GUILayout.Space(3f);

                                var texture = statusLabelTexture.GetValueOrDefault(content.requestType);

                                if (texture != null)
                                {
                                    statusLabelStyle.normal.background = texture;
                                }

                                var requestName = content.requestType.ToLabelName();

                                GUILayout.Label(requestName, statusLabelStyle, GUILayout.Width(46f), GUILayout.Height(16f));
                            }
                        }

                        using (new BackgroundColorScope(Color.clear))
                        {
                            if (GUI.Button(contentRect, string.Empty))
                            {
                                selectionIndex = i;
                            }
                        }
                    }

                    scrollPosition = scrollView.scrollPosition;
                }

                GUILayout.Space(2f);
            }
        }

        private void DrawApiDetailGUI()
        {
            var info = contents.ElementAtOrDefault(selectionIndex);
            
            using (new ContentsScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Height(150f)))
                {
                    if (info != null)
                    {

                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void InitializeStyle()
        {
            if (urlLabelStyle == null)
            {
                urlLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                };
            }

            if (statusLabelStyle == null)
            {
                statusLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    contentOffset = new Vector2(2f, 0f),
                    border = new RectOffset(10, 10, 4, 4),
                    fontSize = 9,
                    fontStyle = FontStyle.Bold,
                };

                statusLabelStyle.normal.textColor = Color.white;
            }
        }

        private void UpdateContents()
        {
            var apiMonitorBridge = ApiMonitorBridge.Instance;

            var newContents = apiMonitorBridge.GetHistory();

            var selectionInfo = contents.ElementAtOrDefault(selectionIndex);

            if (selectionInfo != null)
            {
                selectionIndex = newContents.IndexOf(x => x == selectionInfo);
            }

            contents = newContents;

            Repaint();
        }
    }
}


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

        private int selectionId = -1;

        private int detailTabIndex = 0;

        private Vector2 historyScrollPosition = Vector2.zero;
        private Vector2 detailScrollPosition = Vector2.zero;

        private WebRequestInfo[] contents = null;

        private object splitterState = null;
        private ApiHistoryView historyView = null;

        private GUIStyle historyStyle = null;
        private GUIStyle serverUrlLabelStyle = null;
        private GUIStyle detailStyle = null;

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

            var apiTracker = ApiTracker.Instance;

            titleContent = new GUIContent("ApiMonitor");

            minSize = WindowSize;

            splitterState = EditorSplitterGUILayout.CreateSplitterState(new float[] { 75f, 25f }, new int[] { 32, 32 }, null);

            historyView = new ApiHistoryView();

            //------ Event ------

            apiTracker.OnUpdateInfoAsObservable()
                .Subscribe(_ => UpdateContents())
                .AddTo(Disposable);

            historyView.OnChangeSelectAsObservable()
                .Subscribe(x =>
                   {
                       selectionId = x;
                       detailTabIndex = 0;
                   })
                .AddTo(Disposable);

            //------ Update ------

            UpdateContents();

            initialized = true;
        }

        void OnGUI()
        {
            Initialize();

            InitializeStyle();

            DrawToolbarGUI();

            EditorSplitterGUILayout.BeginVerticalSplit(splitterState);
            {
                DrawApiHistoryGUI();

                DrawApiDetailGUI();
            }
            EditorSplitterGUILayout.EndVerticalSplit();
        }

        private void DrawToolbarGUI()
        {
            var apiTracker = ApiTracker.Instance;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                EditorGUILayout.LabelField(apiTracker.ServerUrl, serverUrlLabelStyle, GUILayout.Width(500f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                {
                    apiTracker.Clear();

                    historyScrollPosition = Vector2.zero;
                    detailScrollPosition = Vector2.zero;
                    detailTabIndex = 0;
                }
            }
        }

        private void DrawApiHistoryGUI()
        {
            using (new EditorGUILayout.VerticalScope(historyStyle))
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(historyScrollPosition, GUILayout.ExpandWidth(true)))
                {
                    var controlRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

                    historyView.OnGUI(controlRect);

                    historyScrollPosition = scrollView.scrollPosition;
                }
            }
        }

        private void DrawApiDetailGUI()
        {
            var info = contents.FirstOrDefault(x => x.Id == selectionId);
            
            using (new ContentsScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    if (info != null)
                    {
                        var tabs = new List<string>()
                        {
                            info.Result.IsNullOrEmpty() ? string.Empty : "Result",
                            info.Exception == null ? string.Empty : "Exception",
                            info.StackTrace.IsNullOrEmpty() ? string.Empty : "StackTrace",
                        };

                        var tabToggles = tabs.Where(x => !x.IsNullOrEmpty()).ToArray();

                        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                        {
                            EditorGUI.BeginChangeCheck();

                            detailTabIndex = GUILayout.Toolbar(detailTabIndex, tabToggles, new GUIStyle(EditorStyles.toolbarButton), GUI.ToolbarButtonSize.FitToContents);

                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorGUI.FocusTextInControl(string.Empty);
                            }
                        }

                        var detailText = string.Empty;

                        var tabName = tabToggles.ElementAtOrDefault(detailTabIndex);

                        switch (tabName)
                        {
                            case "Result":
                                detailText = info.Result;
                                break;

                            case "Exception":
                                detailText = info.Exception.ToString();
                                break;

                            case "StackTrace":
                                detailText = info.StackTrace;
                                break;
                        }

                        using (var scrollView = new EditorGUILayout.ScrollViewScope(detailScrollPosition))
                        {
                            var vector = detailStyle.CalcSize(new GUIContent(detailText));

                            var layoutOption = new GUILayoutOption[]
                            {
                                GUILayout.ExpandHeight(true),
                                GUILayout.ExpandWidth(true),
                                GUILayout.MinWidth(vector.x),
                                GUILayout.MinHeight(vector.y)
                            };

                            EditorGUILayout.SelectableLabel(detailText, detailStyle, layoutOption);

                            detailScrollPosition = scrollView.scrollPosition;
                        }
                    }
                }
            }
        }

        private void InitializeStyle()
        {
            if (serverUrlLabelStyle == null)
            {
                serverUrlLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                };
            }

            if (historyStyle == null)
            {
                historyStyle = new GUIStyle("CN Box");
                historyStyle.margin.top = 0;
                historyStyle.padding.left = 3;
            }

            if (detailStyle == null)
            {
                detailStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                detailStyle.wordWrap = false;
                detailStyle.stretchHeight = true;
                detailStyle.margin.right = 15;
            }
        }

        private void UpdateContents()
        {
            var apiTracker = ApiTracker.Instance;

            var newContents = apiTracker.GetHistory();

            if (contents != null)
            {
                var selectionInfo = contents.FirstOrDefault(x => x.Id == selectionId);

                if (selectionInfo != null)
                {
                    historyView.SetSelection(new List<int>() { selectionId });
                }
            }

            contents = newContents;

            historyView.SetContents(contents);

            Repaint();
        }
    }
}

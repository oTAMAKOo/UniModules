
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Extensions.Devkit.Style;

namespace Modules.InputControl
{
    public sealed class BlockInputMonitorWindow : SingletonEditorWindow<BlockInputMonitorWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(700f, 500f);

        private static readonly Color SelectionLineColor = new Color(0f, 1f, 0f, 0.05f);

        private sealed class Info
        {
            public ulong id;
            public string from;
            public string stacktrace;
        }

        //----- field -----

        private ulong? selectionId = null;

        private object splitterState = null;

        private Info[] infos = null;

        private Vector2 listScrollPosition = Vector2.zero;
        private Vector2 detailScrollPosition = Vector2.zero;

        private GUIStyle listStyle = null;
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

            var blockInputManager = BlockInputManager.Instance;

            titleContent = new GUIContent("BlockInputMonitor");

            minSize = WindowSize;

            splitterState = EditorSplitterGUILayout.CreateSplitterState(new float[] { 75f, 25f }, new int[] { 32, 32 }, null);

            //------ Event ------

            blockInputManager.OnUpdateStatusAsObservable()
                .Subscribe(_ => UpdateContents())
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
                DrawBlockingListGUI();

                DrawBlockingDetailGUI();
            }
            EditorSplitterGUILayout.EndVerticalSplit();
        }

        private void DrawToolbarGUI()
        {
            var blockInputManager = BlockInputManager.Instance;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                using (new DisableScope(!selectionId.HasValue))
                {
                    if (GUILayout.Button("Unlock", EditorStyles.toolbarButton, GUILayout.Width(60f), GUILayout.Height(16f)))
                    {
                        blockInputManager.Unlock(selectionId.Value);

                        UpdateContents();

                        selectionId = null;
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("ForceUnlock", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    blockInputManager.ForceUnlock();

                    UpdateContents();

                    listScrollPosition = Vector2.zero;
                    detailScrollPosition = Vector2.zero;
                }
            }
        }

        private void DrawBlockingListGUI()
        {
            var lineColor1 = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            var lineColor2 = new Color(0.3f, 0.3f, 0.3f, 0.3f);

            var maxId = infos.Any() ? infos.Select(x => x.id).Max() : 0;

            var idLabelSize = EditorStyles.label.CalcSize(new GUIContent(maxId.ToString()));

            using (new EditorGUILayout.VerticalScope(listStyle))
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(listScrollPosition, GUILayout.ExpandWidth(true)))
                {
                    for (var i = 0; i < infos.Length; i++)
                    {
                        var info = infos[i];

                        var color = i % 2 == 0 ? lineColor1 : lineColor2;

                        if (selectionId == info.id)
                        {
                            color = SelectionLineColor;
                        }
                        
                        var backgroundStyle = BackgroundStyle.Get(color);
                        
                        using (new EditorGUILayout.HorizontalScope(backgroundStyle))
                        {
                            EditorGUILayout.LabelField(info.id.ToString(), GUILayout.Width(idLabelSize.x));

                            GUILayout.Space(4f);

                            var size = EditorStyles.label.CalcSize(new GUIContent(info.from));

                            if (GUILayout.Button(info.from, EditorStyles.label, GUILayout.Width(size.x)))
                            {
                                selectionId = info.id;
                            }
                        }
                    }

                    listScrollPosition = scrollView.scrollPosition;
                }
            }
        }

        private void DrawBlockingDetailGUI()
        {
            var info = infos.FirstOrDefault(x => x.id == selectionId);

            using (new EditorGUILayout.VerticalScope())
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(detailScrollPosition))
                {
                    if (info != null)
                    {
                        var size = detailStyle.CalcSize(new GUIContent(info.stacktrace));

                        var layoutOptions = new GUILayoutOption[]
                        {
                            GUILayout.Width(size.x),
                            GUILayout.Height(size.y),
                            GUILayout.ExpandWidth(true), 
                            GUILayout.ExpandHeight(true),
                        };

                        EditorGUILayout.SelectableLabel(info.stacktrace, detailStyle, layoutOptions);
                    }

                    detailScrollPosition = scrollView.scrollPosition;
                }
            }
        }

        private void InitializeStyle()
        {
            if (listStyle == null)
            {
                listStyle = new GUIStyle("CN Box");
                listStyle.margin.top = 0;
                listStyle.padding.left = 3;
            }

            if (detailStyle == null)
            {
                detailStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                detailStyle.alignment = TextAnchor.UpperLeft;
            }
        }

        private void UpdateContents()
        {
            var blockInputManager = BlockInputManager.Instance;

            var trackContents = blockInputManager.GetTrackContents();

            var list = new List<Info>();

            foreach (var content in trackContents)
            {
                var from = content.Value.Split('\n').FirstOrDefault();

                var endIndex = from.IndexOf("(at ", StringComparison.CurrentCulture);

                from = from.Substring(0, endIndex);

                var info = new Info()
                {
                    id = content.Key,
                    from = from,
                    stacktrace = content.Value
                };

                list.Add(info);
            }

            infos = list.ToArray();

            selectionId = null;

            Repaint();
        }
    }
}
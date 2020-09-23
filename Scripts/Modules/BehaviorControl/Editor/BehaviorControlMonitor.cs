
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.BehaviorControl
{
    public sealed class BehaviorControlMonitor : SingletonEditorWindow<BehaviorControlMonitor>
    {
        //----- params -----

        private static readonly Color LineColor = new Color(0.2f, 0.2f, 0.5f);
        private static readonly Color SelectionColor = new Color(0.2f, 0.4f, 0.6f);

        //----- field -----

        private string selectionControllerName = null;

        private LogData selectionData = null;

        private Vector2 listScrollPosition = Vector2.zero;

        private Vector2 detailScrollPosition = Vector2.zero;

        private GUIContent successIconContent = null;
        private GUIContent failureIconContent = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----
        
        public static void Open()
        {
            Instance.Initialize();

            if (BehaviorControlLogger.Exists)
            {
                Instance.Show(true);
            }
            else
            {
                Instance.Close();
            }
        }

        private void Initialize()
        {
            if (initialized) { return; }

            titleContent = new GUIContent("BehaviorControlMonitor");

            var logger = BehaviorControlLogger.Instance;

            logger.OnLogUpdateAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(Disposable);

            successIconContent = EditorGUIUtility.IconContent("TestPassed");
            failureIconContent = EditorGUIUtility.IconContent("TestFailed");

            initialized = true;
        }

        void OnGUI()
        {
            Initialize();

            var logger = BehaviorControlLogger.Instance;

            var e = Event.current;

            //------- Header -------

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();

                var controllerNames = logger.Logs.Select(x => x.ControllerName).ToArray();

                var index = controllerNames.IndexOf(x => x == selectionControllerName);

                index = EditorGUILayout.Popup(string.Empty, index, controllerNames, EditorStyles.toolbarDropDown, GUILayout.Width(250f));

                if (EditorGUI.EndChangeCheck())
                {
                    selectionControllerName = controllerNames[index];
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
                {
                    logger.Clear();
                }
            }

            if (!string.IsNullOrEmpty(selectionControllerName))
            {
                //------- List -------

                // 新しいログから表示する為逆順にする.
                var logs = logger.Logs
                    .Where(x => x.ControllerName == selectionControllerName)
                    .Reverse()
                    .ToArray();

                using (new ContentsScope())
                {
                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(listScrollPosition))
                    {
                        for (var i = 0; i < logs.Length; i++)
                        {
                            var log = logs[i];

                            var backgroundColor = selectionData == log ? SelectionColor : LineColor;

                            EditorLayoutTools.DrawLabelWithBackground(log.BehaviorName, backgroundColor);

                            if (e.type == EventType.MouseDown)
                            {
                                var rect = GUILayoutUtility.GetLastRect();

                                if (rect.Contains(e.mousePosition))
                                {
                                    switch (e.button)
                                    {
                                        case 0:
                                            OnMouseLeftButton(log);
                                            e.Use();
                                            break;
                                    }
                                }
                            }
                        }

                        listScrollPosition = scrollViewScope.scrollPosition;
                    }
                }

                //------- Detail -------

                if (selectionData != null)
                {
                    var contains = logger.Logs.Contains(selectionData);

                    if (contains)
                    {
                        using (new ContentsScope())
                        {
                            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(detailScrollPosition, GUILayout.Height(300f)))
                            {
                                EditorLayoutTools.DrawLabelWithBackground(selectionData.BehaviorName, SelectionColor);

                                GUILayout.Space(2f);

                                foreach (var element in selectionData.Elements)
                                {
                                    using (new ContentsScope())
                                    {
                                        GUILayout.Space(2f);

                                        EditorLayoutTools.DrawLabelWithBackground("Action");

                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            DrawResultIcon(element.ActionNode.Result);

                                            DrawTextContent(element.ActionNode.Type);

                                            DrawTextContent(element.ActionNode.Parameter);
                                        }

                                        GUILayout.Space(2f);

                                        EditorLayoutTools.DrawLabelWithBackground("Probability");

                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            var isHit = element.Percentage != 0 && element.Percentage <= element.Probability;

                                            var text = string.Format("{0}% ({1}%)", element.Percentage, element.Probability);

                                            DrawResultIcon(isHit);

                                            DrawTextContent(text);
                                        }

                                        GUILayout.Space(2f);

                                        EditorLayoutTools.DrawLabelWithBackground("Target");

                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            DrawResultIcon(element.TargetNode.Result);

                                            DrawTextContent(element.TargetNode.Type);

                                            DrawTextContent(element.TargetNode.Parameter);
                                        }

                                        GUILayout.Space(2f);

                                        if (element.ConditionNodes.Any())
                                        {
                                            EditorLayoutTools.DrawLabelWithBackground("Condition");
                                        }

                                        for (var i = 0; i < element.ConditionNodes.Length; i++)
                                        {
                                            using (new EditorGUILayout.HorizontalScope())
                                            {
                                                var conditionNode = element.ConditionNodes[i];

                                                DrawResultIcon(conditionNode.Result);

                                                var connecter = string.Empty;

                                                switch (element.Connecters[i])
                                                {
                                                    case ConditionConnecter.And:
                                                        connecter = "&";
                                                        break;

                                                    case ConditionConnecter.Or:
                                                        connecter = "|";
                                                        break;
                                                }

                                                if (!string.IsNullOrEmpty(connecter))
                                                {
                                                    DrawTextContent(connecter, GUILayout.Width(20f));
                                                }

                                                DrawTextContent(conditionNode.Type);

                                                DrawTextContent(conditionNode.Parameter);
                                            }
                                        }

                                        GUILayout.Space(2f);
                                    }

                                    GUILayout.Space(4f);
                                }

                                detailScrollPosition = scrollViewScope.scrollPosition;
                            }
                        }
                    }
                }

                EditorGUILayout.Separator();
            }
            else
            {
                EditorGUILayout.HelpBox("Select display controller target.", MessageType.Info);
            }
        }

        private void OnMouseLeftButton(LogData logData)
        {
            selectionData = logData;

            Repaint();
        }

        private void DrawResultIcon(bool? result)
        {
            if (result.HasValue)
            {
                var iconSize = new Vector2(16f, 14f);

                using (new LabelWidthScope(iconSize.x))
                {
                    using (new EditorGUIUtility.IconSizeScope(iconSize))
                    {
                        EditorGUILayout.LabelField(result.Value ? successIconContent : failureIconContent, GUILayout.Width(20f));
                    }
                }
            }
            else
            {
                GUILayout.Space(28f);
            }

            GUILayout.Space(2f);
        }

        private void DrawTextContent(string text, params GUILayoutOption[] options)
        {
            options = new GUILayoutOption[]{ GUILayout.Height(18f) }.Concat(options).ToArray();

            EditorGUILayout.SelectableLabel(text, EditorStyles.textArea, options);
        }
    }
}

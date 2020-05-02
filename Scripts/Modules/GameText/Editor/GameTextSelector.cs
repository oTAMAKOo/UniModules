﻿﻿
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.CompileNotice;

namespace Modules.GameText.Components
{
	public class GameTextSelector : ScriptableWizard
    {
        //----- params -----

        private class SelectionInfo
        {
            public string TextGuid { get; set; }
            public string Name { get; set; }
            public string Text { get; set; }

            public SelectionInfo(string textGuid, string name, string text)
            {
                TextGuid = textGuid;
                Name = name;
                Text = text;
            }
        }

        //----- field -----

        private string categoryGuid = null;
        private Vector2 scrollPos = Vector2.zero;
        private string searchText = null;
        private IDisposable disposable = null;

        private SelectionInfo[] selectionCache = null;

        private static GameTextSelector instance = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            if (instance != null)
            {
                instance.Close();
                instance = null;
            }

            var gameText = GameText.Instance;
            
            var setter = GameTextSetterInspector.Current.Instance;

            var category = gameText.FindCategoryDefinitionEnum(setter.CategoryGuid);

            var titleText = string.Format("GameTextSelector : {0}", category.ToLabelName());

            instance = DisplayWizard<GameTextSelector>(titleText);

            instance.Initialize();
        }

        private void Initialize()
        {
            disposable = CompileNotification.OnCompileStartAsObservable().Subscribe(_ => Close());

            Selection.selectionChanged += () => { Repaint(); };

            BuildSelectionInfos();
        }

        void OnDestroy()
        {
            if(disposable != null)
            {
                disposable.Dispose();
                disposable = null;
            }
        }

        void OnGUI()
        {
            var setterInspector = GameTextSetterInspector.Current;

            if(setterInspector == null)
            {
                EditorGUILayout.HelpBox("Need Select GameTextSetter GameObject.", MessageType.Info);
                return;
            }

            var gameText = GameText.Instance;

            var setter = GameTextSetterInspector.Current.Instance;

            if (setter == null) { return; }

            if(categoryGuid != setter.CategoryGuid)
            {
                BuildSelectionInfos();
            }

            if (selectionCache.Any())
            {
                EditorGUILayout.Separator();

                var infos = GetMatchOfList();

                var categoryTexts = gameText.FindCategoryTexts(setter.CategoryGuid);

                // Toolbar.

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
                {
                    // クリア.

                    if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
                    {
                        setter.SetText(null);
                    }

                    GUILayout.FlexibleSpace();

                    // 検索.

                    Action<string> onChangeSearchText = x =>
                    {
                        searchText = x;

                        EditorApplication.delayCall += () =>
                        {
                            Repaint();
                        };
                    };

                    Action onSearchCancel = () =>
                    {
                        searchText = string.Empty;

                        EditorApplication.delayCall += () =>
                        {
                            Repaint();
                        };
                    };

                    EditorLayoutTools.DrawDelayedToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.Width(250f));
                }

                EditorGUILayout.Separator();

                // Contents.

                using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPos))
                {
                    foreach (var info in infos)
                    {
                        GUILayout.Space(-1f);

                        var highlight = setter.TextGuid == info.TextGuid;

                        var originBackgroundColor = GUI.backgroundColor;

                        using (new BackgroundColorScope(highlight ? new Color(0.9f, 1f, 0.9f) : new Color(0.95f, 0.95f, 0.95f)))
                        {
                            var size = EditorStyles.label.CalcSize(new GUIContent(info.Text));

                            size.y += 6f;

                            using (new EditorGUILayout.HorizontalScope(EditorLayoutTools.TextAreaStyle, GUILayout.Height(size.y)))
                            {
                                var labelStyle = new GUIStyle("IN TextField")
                                {
                                    alignment = TextAnchor.MiddleLeft,
                                };

                                GUILayout.Space(10f);
                                
                                GUILayout.Label(info.Name, labelStyle, GUILayout.MinWidth(220f), GUILayout.Height(size.y));

                                GUILayout.Label(info.Text, labelStyle, GUILayout.MaxWidth(500f), GUILayout.Height(size.y));

                                GUILayout.FlexibleSpace();

                                using (new EditorGUILayout.VerticalScope())
                                {
                                    var buttonHeight = 18f;

                                    GUILayout.Space((size.y - buttonHeight) * 0.5f);

                                    using (new BackgroundColorScope(originBackgroundColor))
                                    {
                                        if (GUILayout.Button("Select", GUILayout.Width(75f), GUILayout.Height(buttonHeight)))
                                        {
                                            UnityEditorUtility.RegisterUndo("GameTextSelector-Select", setter);

                                            var textInfo = categoryTexts.FirstOrDefault(x => x.Value == info.TextGuid);

                                            if (!textInfo.Equals(default(KeyValuePair<Enum, string>)))
                                            {
                                                setter.SetText(textInfo.Key);
                                                setterInspector.Repaint();

                                                Close();
                                            }
                                        }
                                    }

                                    GUILayout.Space((size.y - buttonHeight) * 0.5f);
                                }

                                GUILayout.Space(8f);
                            }
                        }
                    }

                    scrollPos = scrollViewScope.scrollPosition;
                }
                
            }
            else
            {
                EditorGUILayout.HelpBox("GameText Notfound", MessageType.Warning);
            }
        }

        private void BuildSelectionInfos()
        {
            var gameText = GameText.Instance;

            categoryGuid = GameTextSetterInspector.Current.Instance.CategoryGuid;

            var categoryTexts = gameText.FindCategoryTexts(categoryGuid);

            var list = new List<SelectionInfo>();

            foreach (var textData in categoryTexts)
            {
                var text = gameText.Cache.GetValueOrDefault(textData.Value);

                var info = new SelectionInfo(textData.Value, textData.Key.ToString(), text);

                list.Add(info);
            }
            
            selectionCache = list.ToArray();
        }

        private SelectionInfo[] GetMatchOfList()
        {
            if (string.IsNullOrEmpty(searchText)) { return selectionCache; }

            var list = new List<SelectionInfo>();

            string[] keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < keywords.Length; ++i) keywords[i] = keywords[i].ToLower();

            foreach (var item in selectionCache)
            {
                var isMatch = item.Name.IsMatch(keywords) || item.Text.IsMatch(keywords);

                if (isMatch)
                {
                    list.Add(item);
                }
            }
            
            return list.ToArray();
        }
    }
}

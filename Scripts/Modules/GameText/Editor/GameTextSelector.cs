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
            public int Id { get; set; }
            public string Name { get; set; }
            public string Text { get; set; }

            public SelectionInfo(int id, string name, string text)
            {
                Id = id;
                Name = name;
                Text = text;
            }
        }

        //----- field -----

        private GameTextCategory category = GameTextCategory.None;
        private Vector2 scrollPos = Vector2.zero;
        private string searchText = null;
        private IDisposable disposable = null;

        private SelectionInfo[] selectionCache = null;

        private static GameTextSelector instance = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            if (instance == null)
            {
                instance = DisplayWizard<GameTextSelector>("GameTextSelector");
                instance.Initialize();
            }
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

            var setter = GameTextSetterInspector.Current.Instance;

            if (setter == null) { return; }

            if(category != setter.Category)
            {
                BuildSelectionInfos();
            }

            GUILayout.Space(12f);

            GUILayout.BeginHorizontal(GUILayout.MinHeight(20f));
            {
                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                {
                    string before = searchText;
                    string after = EditorGUILayout.TextField(string.Empty, before, "SearchTextField", GUILayout.Width(200f));

                    if (before != after)
                    {
                        searchText = after;
                        scrollPos = Vector2.zero;
                    }

                    if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                    {
                        searchText = string.Empty;
                        GUIUtility.keyboardControl = 0;
                        scrollPos = Vector2.zero;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(15f);

                if (GUILayout.Button("Clear", GUILayout.Width(70f), GUILayout.Height(16f)))
                {
                    setter.SetCategoryId(null);
                }

                GUILayout.Space(10f);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            if (selectionCache.Any())
            {
                EditorGUILayout.Separator();

                var infos = GetMatchOfList();

                GUILayout.BeginVertical();
                {
                    EditorLayoutTools.DrawLabelWithBackground("Category : " + setter.Category.ToLabelName(), new Color(0.2f, 0.5f, 0.2f));

                    scrollPos = GUILayout.BeginScrollView(scrollPos);
                    {
                        foreach (var info in infos)
                        {
                            GUILayout.Space(-1f);

                            bool highlight = setter.Identifier.ToNullable() == info.Id;

                            GUI.backgroundColor = highlight ? Color.white : new Color(0.8f, 0.8f, 0.8f);

                            var size = EditorStyles.label.CalcSize(new GUIContent(info.Text));

                            size.y += 6f;

                            GUILayout.BeginHorizontal(EditorLayoutTools.TextAreaStyle, GUILayout.Height(size.y));
                            {
                                var labelStyle = new GUIStyle("IN TextField");
                                labelStyle.alignment = TextAnchor.MiddleLeft;

                                GUILayout.Space(10f);

                                GUILayout.Label(info.Id.ToString(), labelStyle, GUILayout.MinWidth(65f), GUILayout.Height(size.y));

                                GUILayout.Label(info.Name, labelStyle, GUILayout.MinWidth(220f), GUILayout.Height(size.y));

                                GUILayout.Label(info.Text, labelStyle, GUILayout.MaxWidth(500f), GUILayout.Height(size.y));

                                GUILayout.FlexibleSpace();

                                GUILayout.BeginVertical();
                                {
                                    var buttonHeight = 20f;

                                    GUILayout.Space((size.y - buttonHeight) * 0.5f);

                                    if (GUILayout.Button("Select", GUILayout.Width(75f), GUILayout.Height(buttonHeight)))
                                    {
                                        UnityEditorUtility.RegisterUndo("GameTextSelector-Select", setter);
                                        setter.SetCategoryId(info.Id);
                                        setterInspector.Repaint();
                                    }

                                    GUILayout.Space((size.y - buttonHeight) * 0.5f);
                                }
                                GUILayout.EndVertical();

                                GUILayout.Space(8f);
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("GameText Notfound", MessageType.Warning);
            }
        }

        private void BuildSelectionInfos()
        {
            category = GameTextSetterInspector.Current.Instance.Category;

            var categoryTable = Reflection.GetPrivateField<GameText, Dictionary<Type, GameTextCategory>>(GameText.Instance, "CategoryTable");
            var gameTexts = GameText.Instance.Cache.GetValueOrDefault((int)category);
            var enumType = categoryTable.Where(x => x.Value == category).Select(x => x.Key).FirstOrDefault();

            selectionCache = gameTexts.Select(x => new SelectionInfo(x.Key, Enum.ToObject(enumType, x.Key).ToString(), x.Value)).ToArray();
        }

        private SelectionInfo[] GetMatchOfList()
        {
            if (string.IsNullOrEmpty(searchText)) { return selectionCache; }

            var list = new List<SelectionInfo>();

            string[] keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < keywords.Length; ++i) keywords[i] = keywords[i].ToLower();

            foreach (var item in selectionCache)
            {
                var isMatch = item.Id.ToString().IsMatch(keywords) ||
                              item.Name.IsMatch(keywords) ||
                              item.Text.IsMatch(keywords);

                if (isMatch)
                {
                    list.Add(item);
                }
            }
            
            return list.ToArray();
        }
    }
}

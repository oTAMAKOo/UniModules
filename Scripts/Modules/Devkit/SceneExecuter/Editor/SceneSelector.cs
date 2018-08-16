﻿﻿
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.SceneExecuter.Editor
{
    public static class SceneSelectorPrefs
    {
        public static string selectedScenePath
        {
            get { return ProjectPrefs.GetString("SceneSelectorPrefs-SelectedScenePath", null); }
            set { ProjectPrefs.SetString("SceneSelectorPrefs-SelectedScenePath", value); }
        }
    }

    public class SceneSelector : ScriptableWizard
    {
        //----- params -----

        //----- field -----

        private string searchText = null;
        private Vector2 scrollPos = Vector2.zero;
        private List<string> scenePaths = null;
        private Subject<string> onSelected = new Subject<string>();

        private static SceneSelector instance = null;

        //----- property -----

        //----- method -----

        public static IObservable<string> Open()
        {
            if (instance != null)
            {
                instance.Close();
                instance = null;
            }

            instance = DisplayWizard<SceneSelector>("Scene Select");
            

            return instance.onSelected;
        }

        void OnEnable()
        {
            var scenes = EditorBuildSettings.scenes;
            scenePaths = scenes.Select(x => x.path).ToList();
        }

        void OnDisable()
        {
            SceneSelectorPrefs.selectedScenePath = null;
            onSelected.Dispose();
        }

        void OnGUI()
        {
            GUILayout.Space(12f);

            GUILayout.BeginHorizontal(GUILayout.MinHeight(20f));
            {
                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                {
                    var before = searchText;
                    var after = EditorGUILayout.TextField(string.Empty, before, "SearchTextField", GUILayout.Width(200f));

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

                if (GUILayout.Button("Clear", GUILayout.Width(70f), GUILayout.Height(20f)))
                {
                    Apply(null);
                }

                GUILayout.Space(10f);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            if (0 < scenePaths.Count)
            {
                EditorGUILayout.Separator();

                var infos = GetListOfScenePaths();

                GUILayout.BeginVertical();
                {
                    scrollPos = GUILayout.BeginScrollView(scrollPos);
                    {
                        var backgroundColor = GUI.backgroundColor;

                        foreach (var path in infos)
                        {
                            GUILayout.Space(-1f);

                            var highlight = SceneSelectorPrefs.selectedScenePath == path;

                            GUI.backgroundColor = highlight ? Color.white : new Color(0.8f, 0.8f, 0.8f);

                            var size = EditorStyles.label.CalcSize(new GUIContent(path));

                            size.y += 6f;

                            GUILayout.BeginHorizontal(EditorLayoutTools.TextAreaStyle, GUILayout.Height(size.y));
                            {
                                var labelStyle = new GUIStyle("IN TextField");
                                labelStyle.alignment = TextAnchor.MiddleLeft;

                                GUILayout.Space(10f);

                                GUILayout.Label(path, labelStyle, GUILayout.MinWidth(65f), GUILayout.Height(size.y));

                                GUILayout.FlexibleSpace();

                                GUILayout.BeginVertical();
                                {
                                    var buttonHeight = 20f;

                                    GUILayout.Space((size.y - buttonHeight) * 0.5f);

                                    if (GUILayout.Button("Select", GUILayout.Width(75f), GUILayout.Height(buttonHeight)))
                                    {
                                        Apply(path);
                                    }

                                    GUILayout.Space((size.y - buttonHeight) * 0.5f);
                                }
                                GUILayout.EndVertical();

                                GUILayout.Space(8f);
                            }
                            GUILayout.EndHorizontal();
                        }

                        GUI.backgroundColor = backgroundColor;
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }
        }

        private void Apply(string select)
        {
            onSelected.OnNext(select);
            Close();
        }

        private List<string> GetListOfScenePaths()
        {
            if (string.IsNullOrEmpty(searchText)) { return scenePaths; }

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            return scenePaths.Where( x => x.IsMatch(keywords)).ToList();
        }
    }
}
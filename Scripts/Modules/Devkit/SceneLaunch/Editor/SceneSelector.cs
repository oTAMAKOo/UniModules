﻿﻿
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.SceneLaunch
{
    public static class SceneSelectorPrefs
    {
        public static string selectedScenePath
        {
            get { return ProjectPrefs.GetString("SceneSelectorPrefs-SelectedScenePath", null); }
            set { ProjectPrefs.SetString("SceneSelectorPrefs-SelectedScenePath", value); }
        }
    }

    public sealed class SceneSelector : ScriptableWizard
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
            GUILayout.Space(2f);

            using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                Action<string> onChangeSearchText = x =>
                {
                    searchText = x;
                    scrollPos = Vector2.zero;
                    Repaint();
                };

                Action onSearchCancel = () =>
                {
                    searchText = string.Empty;
                    GUIUtility.keyboardControl = 0;
                    scrollPos = Vector2.zero;

                    Apply(null);
                };

                EditorLayoutTools.DrawSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.Width(250f));
            }

            GUILayout.Space(4f);

            if (0 < scenePaths.Count)
            {
                var infos = GetListOfScenePaths();

                using(new EditorGUILayout.VerticalScope())
                {
                    using(var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPos))
                    {
                        foreach (var path in infos)
                        {
                            var highlight = SceneSelectorPrefs.selectedScenePath == path;

                            var backgroundColor = highlight ? new Color(0.8f, 0.8f, 1f) : Color.white;

                            using (new BackgroundColorScope(backgroundColor))
                            {
                                var size = EditorStyles.label.CalcSize(new GUIContent(path));

                                size.y += 6f;

                                using(new EditorGUILayout.HorizontalScope(EditorStyles.textArea, GUILayout.Height(size.y)))
                                {
                                    var labelStyle = new GUIStyle("IN TextField");
                                    labelStyle.alignment = TextAnchor.MiddleLeft;

                                    GUILayout.Space(10f);

                                    GUILayout.Label(path, labelStyle, GUILayout.MinWidth(65f), GUILayout.Height(size.y));

                                    GUILayout.FlexibleSpace();

                                    using(new EditorGUILayout.VerticalScope())
                                    {
                                        var buttonHeight = 20f;

                                        GUILayout.Space((size.y - buttonHeight) * 0.5f);

                                        using (new BackgroundColorScope(Color.white))
                                        {
                                            if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(65f)))
                                            {
                                                Apply(path);
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

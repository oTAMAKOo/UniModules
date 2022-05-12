﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Extensions.Devkit;
using Extensions;

namespace Modules.Devkit.SceneImporter
{
    [CustomEditor(typeof(SceneImporterConfig))]
    public sealed class SceneImporterConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private Vector2 scrollPosition = Vector2.zero;

        private SceneImporterConfig instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as SceneImporterConfig;

            CustomInspector();
        }

        private void CustomInspector()
        {
            var managedFolders = instance.ManagedFolders.ToList();

            var contentHeight = 16f;

            EditorLayoutTools.SetLabelWidth(100f);

            // Style.
            var pathTextStyle = GUI.skin.GetStyle("TextArea");
            pathTextStyle.alignment = TextAnchor.MiddleLeft;

            EditorGUILayout.Separator();

            // InitialScene.
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Initial Scene", GUILayout.Width(80f), GUILayout.Height(contentHeight));
                GUILayout.Label(instance.InitialScene, pathTextStyle, GUILayout.Height(contentHeight));

                if (GUILayout.Button("Select", GUILayout.Width(60f), GUILayout.Height(contentHeight)))
                {
                    UnityEditorUtility.RegisterUndo(instance);

                    var initialScenePath = EditorUtility.OpenFilePanelWithFilters("Select Initial Scene", "Assets", new string[]{ "SceneFile", "unity" });

                    initialScenePath = initialScenePath.Replace(Application.dataPath, "Assets");

                    Reflection.SetPrivateField(instance, "initialScene", initialScenePath);
                }
            }

            EditorGUILayout.Separator();

            // AutoAdditionFolders.
            EditorLayoutTools.Title("Managed Folders");

            var change = false;

            using (new ContentsScope())
            {
                var scrollViewHeight = System.Math.Min(contentHeight * managedFolders.Count + 5f, 300f);

                using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(scrollViewHeight)))
                {
                    for (var i = 0; i < managedFolders.Count; ++i)
                    {
                        var folder = managedFolders[i];

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var label = string.IsNullOrEmpty(folder) ? folder : folder + "/";
                            GUILayout.Label(label, pathTextStyle, GUILayout.Height(contentHeight));

                            if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(contentHeight)))
                            {
                                managedFolders.RemoveAt(i);
                                change = true;
                            }
                        }
                    }

                    scrollPosition = scrollViewScope.scrollPosition;
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(40f)))
                    {
                        var folderPath = EditorUtility.OpenFolderPanel("Select Auto Addition Scene Folder", "Assets", "");

                        if (folderPath.Contains(Application.dataPath))
                        {
                            folderPath = folderPath.Replace(Application.dataPath, "Assets");

                            if (folderPath != "Assets" && !managedFolders.Contains(folderPath))
                            {
                                managedFolders.Add(folderPath);
                                change = true;
                            }
                        }
                    }
                }

                if (change)
                {
                    Func<string, int> sortFunc = x =>
                    {
                        var path = PathUtility.ConvertPathSeparator(x);

                        var separatorCount = path.Split(PathUtility.PathSeparator).Length;

                        return separatorCount;
                    };

                    var folders = managedFolders
                        .OrderBy(x => sortFunc.Invoke(x))
                        .ThenBy(x => x.Length)
                        .ToList();

                    Reflection.SetPrivateField(instance, "managedFolders", folders);
                    UnityEditorUtility.RegisterUndo(instance);
                }
            }
        }
    }
}

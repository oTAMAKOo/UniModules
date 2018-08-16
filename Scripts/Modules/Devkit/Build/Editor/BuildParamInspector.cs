﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.Build.Editor
{
    [CustomEditor(typeof(BuildParam), true)]
    public class BuildParamInfoInspector : UnityEditor.Editor
    {
        //----- params -----

        private static readonly string[] HideInspector = new string[] { "m_Script" };

        //----- field -----

        private BuildParam instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as BuildParam;

            serializedObject.Update();

            CustomInspector(instance, serializedObject);

            DefaultInspector(serializedObject);

            ExtendCustomInspector(instance, serializedObject);
        }

        private static void CustomInspector(BuildParam instance, SerializedObject serializedObject)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("applicationName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("development"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("iconFolder"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("directiveSymbols"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static void DefaultInspector(SerializedObject serializedObject)
        {
            EditorGUI.BeginChangeCheck();

            DrawPropertiesExcluding(serializedObject, HideInspector);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static void ExtendCustomInspector(BuildParam instance, SerializedObject serializedObject)
        {
            // Style.
            var pathTextStyle = GUI.skin.GetStyle("TextArea");
            pathTextStyle.alignment = TextAnchor.MiddleLeft;

            if (EditorLayoutTools.DrawHeader("Version", "BuildParamInspector-Version"))
            {
                using (new ContentsScope())
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("version"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("buildVersion"));

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            var cloneAssets = Reflection.GetPrivateField<BuildParam, BuildParam.CloneAssetInfo[]>(instance, "cloneAssets").ToList();

            var originLabelWidth = EditorLayoutTools.SetLabelWidth(80f);

            if (EditorLayoutTools.DrawHeader("CloneAssets", "BuildParamInspector-CloneAssets"))
            {
                using (new ContentsScope())
                {
                    var size = EditorGUILayout.IntField("Size", cloneAssets.Count, GUILayout.Width(120f));

                    if (size != cloneAssets.Count)
                    {
                        while (size > cloneAssets.Count)
                        {
                            cloneAssets.Add(new BuildParam.CloneAssetInfo());
                        }

                        while (size < cloneAssets.Count)
                        {
                            cloneAssets.RemoveAt(cloneAssets.Count - 1);
                        }

                        Reflection.SetPrivateField<BuildParam, BuildParam.CloneAssetInfo[]>(instance, "cloneAssets", cloneAssets.ToArray());
                    }

                    EditorGUI.indentLevel++;

                    EditorLayoutTools.SetLabelWidth(80f);

                    for (var i = 0; i < cloneAssets.Count; i++)
                    {
                        var cloneAsset = cloneAssets[i];

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Source", cloneAsset.Source, pathTextStyle, GUILayout.Width(200f));

                            if (GUILayout.Button("Edit", GUILayout.Width(50f)))
                            {
                                UnityEditorUtility.RegisterUndo("BuildParamInfoInspector Undo", instance);

                                var projectFolder = UnityPathUtility.GetProjectFolderPath();

                                var path = EditorUtility.OpenFolderPanel("Select folder", Application.dataPath, "");
                                var relativePath = PathUtility.FullPathToRelativePath(projectFolder, path);

                                Reflection.SetPrivateField<BuildParam.CloneAssetInfo, string>(cloneAsset, "source", relativePath);

                                serializedObject.ApplyModifiedProperties();
                            }

                            GUILayout.FlexibleSpace();
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("To", cloneAsset.To, pathTextStyle, GUILayout.Width(200f));

                            if (GUILayout.Button("Edit", GUILayout.Width(50f)))
                            {
                                UnityEditorUtility.RegisterUndo("BuildParamInfoInspector Undo", instance);

                                var projectFolder = UnityPathUtility.GetProjectFolderPath();

                                var path = EditorUtility.OpenFolderPanel("Select folder", Application.dataPath, "");
                                var relativePath = PathUtility.FullPathToRelativePath(projectFolder, path);

                                Reflection.SetPrivateField<BuildParam.CloneAssetInfo, string>(cloneAsset, "to", relativePath);

                                serializedObject.ApplyModifiedProperties();
                            }

                            GUILayout.FlexibleSpace();
                        }

                        GUILayout.Space(2f);
                    }

                    EditorLayoutTools.SetLabelWidth(originLabelWidth);

                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
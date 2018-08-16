
#if ENABLE_CRIWARE

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.CriWare.Editor
{
    public class CriAssetConfigInspectorBase : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void DrawDirectoryInspector(SerializedObject serializedObject)
        {
            serializedObject.Update();

            var rootFolderNameProperty = serializedObject.FindProperty("rootFolderName");
            var criExportDirProperty = serializedObject.FindProperty("criExportDir");

            // Style.
            var pathTextStyle = GUI.skin.GetStyle("TextArea");
            pathTextStyle.alignment = TextAnchor.MiddleLeft;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(rootFolderNameProperty);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("CriAssetConfigInspector Undo", serializedObject.targetObject);
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Separator();

            GUILayout.Label("CRI Export Directory");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(criExportDirProperty.stringValue, pathTextStyle);

                if (GUILayout.Button("Edit", GUILayout.Width(30f)))
                {
                    UnityEditorUtility.RegisterUndo("CriAssetConfigInspector Undo", serializedObject.targetObject);

                    var sourceFolder = EditorUtility.OpenFolderPanel("Select CRI export folder", "", "");

                    var assetFolderUri = new Uri(Application.dataPath);
                    var targetUri = new Uri(sourceFolder);
                    criExportDirProperty.stringValue = assetFolderUri.MakeRelativeUri(targetUri).ToString();

                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Separator();
        }
    }
}

#endif
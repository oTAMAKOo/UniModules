﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.MessagePack
{
    [CustomEditor(typeof(MessagePackConfig), true)]
    public class MessagePackConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private MessagePackConfig instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as MessagePackConfig;

            serializedObject.Update();

            var winCompilerRelativePath = serializedObject.FindProperty("winCompilerRelativePath");
            var osxCompilerRelativePath = serializedObject.FindProperty("osxCompilerRelativePath");
            var scriptExportAssetDir = serializedObject.FindProperty("scriptExportAssetDir");
            var scriptName = serializedObject.FindProperty("scriptName");

            // Style.
            var pathTextStyle = GUI.skin.GetStyle("TextArea");
            pathTextStyle.alignment = TextAnchor.MiddleLeft;

            GUILayout.Label("MessagePack compiler (Win)");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(winCompilerRelativePath.stringValue, pathTextStyle);

                if (GUILayout.Button("Edit", GUILayout.Width(45f)))
                {
                    UnityEditorUtility.RegisterUndo("MessagePackConfigInspector Undo", instance);

                    var path = EditorUtility.OpenFilePanel("Select MessagePack compiler", Application.dataPath, "exe");

                    winCompilerRelativePath.stringValue = UnityPathUtility.MakeRelativePath(path);

                    serializedObject.ApplyModifiedProperties();
                }
            }

            GUILayout.Label("MessagePack compiler (OSX)");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(osxCompilerRelativePath.stringValue, pathTextStyle);

                if (GUILayout.Button("Edit", GUILayout.Width(45f)))
                {
                    UnityEditorUtility.RegisterUndo("MessagePackConfigInspector Undo", instance);

                    var path = EditorUtility.OpenFilePanel("Select MessagePack compiler", Application.dataPath, "");

                    osxCompilerRelativePath.stringValue = UnityPathUtility.MakeRelativePath(path);

                    serializedObject.ApplyModifiedProperties();
                }
            }

            GUILayout.Label("MessagePack Script Export");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(scriptExportAssetDir.stringValue, pathTextStyle);

                if (GUILayout.Button("Edit", GUILayout.Width(45f)))
                {
                    UnityEditorUtility.RegisterUndo("MessagePackConfigInspector Undo", instance);

                    var path = EditorUtility.OpenFolderPanel("Select Export Folder", Application.dataPath, string.Empty);

                    scriptExportAssetDir.stringValue = UnityPathUtility.MakeRelativePath(path);

                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Export FileName");
            scriptName.stringValue = EditorGUILayout.DelayedTextField(scriptName.stringValue);

            if(EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("MessagePackConfigInspector Undo", instance);

                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Separator();
        }
    }
}

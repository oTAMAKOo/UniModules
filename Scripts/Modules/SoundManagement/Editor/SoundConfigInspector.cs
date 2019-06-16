
#if ENABLE_CRIWARE_ADX
﻿﻿
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Extensions.Devkit;
using Extensions;
using Modules.CriWare.Editor;

namespace Modules.SoundManagement.Editor
{
    [CustomEditor(typeof(SoundConfig))]
    public class SoundConfigInspector : CriAssetConfigInspectorBase
    {
        //----- params -----

        //----- field -----

        private SoundConfig instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as SoundConfig;

            serializedObject.Update();

            var acfAssetSourcePathProperty = serializedObject.FindProperty("acfAssetSourceFullPath");
            var acfAssetExportPathProperty = serializedObject.FindProperty("acfAssetExportPath");

            DrawDirectoryInspector(serializedObject);

            EditorGUILayout.Separator();

            // Style.
            var pathTextStyle = GUI.skin.GetStyle("TextArea");
            pathTextStyle.alignment = TextAnchor.MiddleLeft;

            GUILayout.Label("AcfAssetSourcePath");

            using(new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(acfAssetSourcePathProperty.stringValue, pathTextStyle);

                if (GUILayout.Button("Edit", GUILayout.Width(30f)))
                {
                    UnityEditorUtility.RegisterUndo("SoundConfigInspector Undo", instance);

                    var acfAssetSource = EditorUtility.OpenFilePanel("Select ACF", "", "");

                    var assetFolderUri = new Uri(Application.dataPath);
                    var targetUri = new Uri(acfAssetSource);
                    acfAssetSourcePathProperty.stringValue = assetFolderUri.MakeRelativeUri(targetUri).ToString();

                    serializedObject.ApplyModifiedProperties();
                }
            }
            
            GUILayout.Label("AcfAssetExportPath");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(acfAssetExportPathProperty.stringValue, pathTextStyle);

                if (GUILayout.Button("Edit", GUILayout.Width(30f)))
                {
                    UnityEditorUtility.RegisterUndo("SoundConfigInspector Undo", instance);

                    var acfAssetDirectory = EditorUtility.OpenFolderPanel("Select CriSetting Folder", "", "");

                    var assetFolderUri = new Uri(Application.dataPath);
                    var targetUri = new Uri(acfAssetDirectory);
                    acfAssetExportPathProperty.stringValue = assetFolderUri.MakeRelativeUri(targetUri).ToString();

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}

#endif

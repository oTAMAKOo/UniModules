
using UnityEngine;
using UnityEditor;
using System;
using Extensions;
using Extensions.Devkit;

namespace Modules.Master
{
    [CustomEditor(typeof(MasterConfig))]
    public sealed class MasterConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as MasterConfig;

            DrawMasterGeneratorConfigGUI(instance);
        }

        private void DrawMasterGeneratorConfigGUI(MasterConfig instance)
        {
            // Style.
            var pathTextStyle = GUI.skin.GetStyle("TextArea");
            pathTextStyle.alignment = TextAnchor.MiddleLeft;

            // Lz4Compression.

            EditorGUI.BeginChangeCheck();

            var lz4Compression = EditorGUILayout.Toggle("Lz4Compression", instance.Lz4Compression);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);

                Reflection.SetPrivateField(instance, "lz4Compression", lz4Compression);
            }

            // RecordSourceDirectory.

            GUILayout.Label("RecordDirectory");

            using (new EditorGUILayout.HorizontalScope())
            {
                var sourceDirectory = instance.SourceDirectory;

                GUILayout.Label(sourceDirectory, pathTextStyle);

                if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(50f)))
                {
                    UnityEditorUtility.RegisterUndo(instance);

                    var selectDirectory = EditorUtility.OpenFolderPanel("Select Directory", "", "");

                    var assetFolderUri = new Uri(Application.dataPath);
                    var targetUri = new Uri(selectDirectory);

                    sourceDirectory = assetFolderUri.MakeRelativeUri(targetUri).ToString();

                    Reflection.SetPrivateField(instance, "sourceDirectory", sourceDirectory);
                }
            }

            EditorGUILayout.Separator();

            // ExportDirectory.

            GUILayout.Label("ExportDirectory");

            using (new EditorGUILayout.HorizontalScope())
            {
                var exportDirectory = instance.ExportDirectory;

                GUILayout.Label(exportDirectory, pathTextStyle);

                if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(50f)))
                {
                    UnityEditorUtility.RegisterUndo(instance);

                    var selectDirectory = EditorUtility.OpenFolderPanel("Select Directory", "", "");

                    var assetFolderUri = new Uri(Application.dataPath);
                    var targetUri = new Uri(selectDirectory);

                    exportDirectory = assetFolderUri.MakeRelativeUri(targetUri).ToString();

                    Reflection.SetPrivateField(instance, "exportDirectory", exportDirectory);
                }
            }

            EditorGUILayout.Separator();

            // CryptKey Parameter.

            EditorLayoutTools.Title("DataCrypt");

            using (new ContentsScope())
            {
                EditorGUI.BeginChangeCheck();

                var cryptoKey = EditorGUILayout.DelayedTextField("Key", instance.CryptoKey);

                var cryptoIv = EditorGUILayout.DelayedTextField("Iv", instance.CryptoIv);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo(instance);

                    Reflection.SetPrivateField(instance, "cryptoKey", cryptoKey);
                    Reflection.SetPrivateField(instance, "cryptoIv", cryptoIv);
                }
            }

            EditorGUILayout.Separator();
        }
    }
}

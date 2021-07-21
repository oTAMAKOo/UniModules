
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Master
{
    [CustomEditor(typeof(MasterGeneratorConfig))]
    public sealed class MasterGeneratorConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as MasterGeneratorConfig;

            DrawMasterGeneratorConfigGUI(instance);
        }

        private void DrawMasterGeneratorConfigGUI(MasterGeneratorConfig instance)
        {
            // Style.
            var pathTextStyle = GUI.skin.GetStyle("TextArea");
            pathTextStyle.alignment = TextAnchor.MiddleLeft;

            // FileFormat.

            GUILayout.Label("DataFormat");

            EditorGUI.BeginChangeCheck();

            var dataFormat = (SerializationFileUtility.Format)EditorGUILayout.EnumPopup(instance.DataFormat);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("MasterGeneratorConfigInspector Undo", instance);

                Reflection.SetPrivateField(instance, "format", dataFormat);
            }

            // Lz4Compression.

            EditorGUI.BeginChangeCheck();

            var lz4Compression = EditorGUILayout.Toggle("Lz4Compression", instance.Lz4Compression);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("MasterGeneratorConfigInspector Undo", instance);

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
                    UnityEditorUtility.RegisterUndo("MasterGeneratorConfigInspector Undo", instance);

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
                    UnityEditorUtility.RegisterUndo("MasterGeneratorConfigInspector Undo", instance);

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

                var dataCryptKey = EditorGUILayout.DelayedTextField("Key", instance.DataCryptKey);

                var dataCryptIv = EditorGUILayout.DelayedTextField("Iv", instance.DataCryptIv);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("MasterGeneratorConfigInspector Undo", instance);

                    Reflection.SetPrivateField(instance, "dataCryptoKey", dataCryptKey);
                    Reflection.SetPrivateField(instance, "dataCryptIv", dataCryptIv);
                }
            }

            EditorGUILayout.Separator();

            EditorLayoutTools.Title("FileNameCrypt");

            using (new ContentsScope())
            {
                EditorGUI.BeginChangeCheck();

                var fileNameCryptKey = EditorGUILayout.DelayedTextField("Key", instance.FileNameCryptKey);

                var fileNameCryptIv = EditorGUILayout.DelayedTextField("Iv", instance.FileNameCryptIv);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("MasterGeneratorConfigInspector Undo", instance);

                    Reflection.SetPrivateField(instance, "fileNameCryptoKey", fileNameCryptKey);
                    Reflection.SetPrivateField(instance, "fileNameCryptIv", fileNameCryptIv);
                }
            }

            EditorGUILayout.Separator();
        }
    }
}

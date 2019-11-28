
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

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
    [CustomEditor(typeof(CriAssetConfig))]
    public sealed class CriAssetConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as CriAssetConfig;

            serializedObject.Update();

            #if ENABLE_CRIWARE_ADX

            DrawSoundAssetConfigGUI(instance);
            
            #endif

            #if ENABLE_CRIWARE_SOFDEC

            DrawMovieAssetConfigGUI(instance);

            #endif
        }

        //---------------------------------------
        // Sound.
        //---------------------------------------

        #if ENABLE_CRIWARE_ADX

        private void DrawSoundAssetConfigGUI(CriAssetConfig instance)
        {
            var acfAssetSourcePathProperty = serializedObject.FindProperty("acfAssetSourceFullPath");
            var acfAssetExportPathProperty = serializedObject.FindProperty("acfAssetExportPath");

            if (EditorLayoutTools.DrawHeader("Sound", "CriAssetConfigInspector-Sound"))
            {
                using (new ContentsScope())
                {
                    var change = DrawAssetImportInfoGUI(instance.SoundImportInfo);

                    if (change)
                    {
                        UnityEditorUtility.RegisterUndo("CriAssetConfigInspector Undo", instance);
                    }

                    // Style.
                    var pathTextStyle = GUI.skin.GetStyle("TextArea");
                    pathTextStyle.alignment = TextAnchor.MiddleLeft;

                    GUILayout.Label("AcfAssetSourcePath");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(acfAssetSourcePathProperty.stringValue, pathTextStyle);

                        if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(50f)))
                        {
                            UnityEditorUtility.RegisterUndo("CriAssetConfigInspector Undo", instance);

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

                        if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(50f)))
                        {
                            UnityEditorUtility.RegisterUndo("CriAssetConfigInspector Undo", instance);

                            var acfAssetDirectory = EditorUtility.OpenFolderPanel("Select CriSetting Folder", "", "");

                            var assetFolderUri = new Uri(Application.dataPath);
                            var targetUri = new Uri(acfAssetDirectory);
                            acfAssetExportPathProperty.stringValue = assetFolderUri.MakeRelativeUri(targetUri).ToString();

                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }

            GUILayout.Space(4f);
        }

        #endif

        //---------------------------------------
        // Movie.
        //---------------------------------------

        #if ENABLE_CRIWARE_SOFDEC


        private void DrawMovieAssetConfigGUI(CriAssetConfig instance)
        {
            if (EditorLayoutTools.DrawHeader("Movie", "CriAssetConfigInspector-Movie"))
            {
                using (new ContentsScope())
                {
                    var change = DrawAssetImportInfoGUI(instance.MovieImportInfo);

                    if (change)
                    {
                        UnityEditorUtility.RegisterUndo("CriAssetConfigInspector Undo", instance);
                    }
                }
            }

            GUILayout.Space(4f);
        }

        #endif


        public static bool DrawAssetImportInfoGUI(CriAssetConfig.AssetImportInfo assetImportInfo)
        {
            var change = false;

            // Style.
            var pathTextStyle = GUI.skin.GetStyle("TextArea");
            pathTextStyle.alignment = TextAnchor.MiddleLeft;

            GUILayout.Label("AssetFolderName");

            EditorGUI.BeginChangeCheck();

            var folderName = EditorGUILayout.DelayedTextField(assetImportInfo.FolderName);

            if (EditorGUI.EndChangeCheck())
            {
                Reflection.SetPrivateField(assetImportInfo, "folderName", folderName);
                change = true;
            }

            GUILayout.Label("ImportFrom");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(assetImportInfo.ImportPath, pathTextStyle, GUILayout.Height(16f));

                if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(50f)))
                {
                    var sourceFolder = EditorUtility.OpenFolderPanel("Select import folder", "", "");

                    if (!string.IsNullOrEmpty(sourceFolder))
                    {
                        var assetFolderUri = new Uri(Application.dataPath);
                        var targetUri = new Uri(sourceFolder);
                        var importPath = assetFolderUri.MakeRelativeUri(targetUri).ToString();

                        Reflection.SetPrivateField(assetImportInfo, "importPath", importPath);

                        change = true;
                    }
                }
            }

            return change;
        }
    }
}

#endif

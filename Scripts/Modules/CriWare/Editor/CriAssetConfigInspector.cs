
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

using UnityEngine;
using UnityEditor;
using System;
using Extensions;
using Extensions.Devkit;

namespace Modules.CriWare.Editor
{
    [CustomEditor(typeof(CriAssetConfig))]
    public sealed class CriAssetConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private Action onAfterDrawCallback = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as CriAssetConfig;

            onAfterDrawCallback = null;

            serializedObject.Update();

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE

            DrawSoundAssetConfigGUI(instance);
            
            #endif

            #if ENABLE_CRIWARE_SOFDEC

            DrawMovieAssetConfigGUI(instance);

            #endif

            if (onAfterDrawCallback != null)
            {
                onAfterDrawCallback.Invoke();
            }
        }

        //---------------------------------------
        // Sound.
        //---------------------------------------

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE

        private void DrawSoundAssetConfigGUI(CriAssetConfig instance)
        {
            var acfAssetSourcePathProperty = serializedObject.FindProperty("acfAssetSourceFullPath");
            var acfAssetExportPathProperty = serializedObject.FindProperty("acfAssetExportPath");

            if (EditorLayoutTools.Header("Sound", "CriAssetConfigInspector-Sound"))
            {
                using (new ContentsScope())
                {
                    // Style.
                    var pathTextStyle = GUI.skin.GetStyle("TextArea");
                    pathTextStyle.alignment = TextAnchor.MiddleLeft;

                    GUILayout.Label("FolderName");

                    EditorGUI.BeginChangeCheck();

                    var folderName = EditorGUILayout.DelayedTextField(instance.SoundFolderName);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo(instance);

                        Reflection.SetPrivateField(instance, "soundFolderName", folderName);
                    }

                    GUILayout.Label("AcfAssetSourcePath");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(acfAssetSourcePathProperty.stringValue, pathTextStyle);

                        if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(50f)))
                        {
                            onAfterDrawCallback = () =>
                            {
                                var acfAssetSource = EditorUtility.OpenFilePanel("Select ACF", "", "");

                                if (!string.IsNullOrEmpty(acfAssetSource))
                                {
                                    UnityEditorUtility.RegisterUndo(instance);

                                    var assetFolderUri = new Uri(Application.dataPath);
                                    var targetUri = new Uri(acfAssetSource);
                                    acfAssetSourcePathProperty.stringValue = assetFolderUri.MakeRelativeUri(targetUri).ToString();

                                    serializedObject.ApplyModifiedProperties();
                                }
                            };
                        }
                    }

                    GUILayout.Label("AcfAssetExportPath");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(acfAssetExportPathProperty.stringValue, pathTextStyle);

                        if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(50f)))
                        {
                            onAfterDrawCallback = () =>
                            {
                                var acfAssetDirectory = EditorUtility.OpenFolderPanel("Select CriSetting Folder", "", "");

                                if (!string.IsNullOrEmpty(acfAssetDirectory))
                                {
                                    UnityEditorUtility.RegisterUndo(instance);

                                    var assetFolderUri = new Uri(Application.dataPath);
                                    var targetUri = new Uri(acfAssetDirectory);
                                    acfAssetExportPathProperty.stringValue = assetFolderUri.MakeRelativeUri(targetUri).ToString();

                                    serializedObject.ApplyModifiedProperties();
                                }
                            };
                        }
                    }

                    DrawAssetImportInfoGUI(instance, "internalSound", "externalSound");
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
            if (EditorLayoutTools.Header("Movie", "CriAssetConfigInspector-Movie"))
            {
                using (new ContentsScope())
                {
                    GUILayout.Label("FolderName");

                    EditorGUI.BeginChangeCheck();

                    var folderName = EditorGUILayout.DelayedTextField(instance.MovieFolderName);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo(instance);

                        Reflection.SetPrivateField(instance, "movieFolderName", folderName);
                    }

                    DrawAssetImportInfoGUI(instance, "internalMovie", "externalMovie");
                }
            }

            GUILayout.Space(4f);
        }

        #endif

        public void DrawAssetImportInfoGUI(CriAssetConfig instance, string internalFieldName, string externalFieldName)
        {
            var internalInfo = Reflection.GetPrivateField<CriAssetConfig, ImportInfo>(instance, internalFieldName);
            var externalInfo = Reflection.GetPrivateField<CriAssetConfig, ImportInfo>(instance, externalFieldName);

            var labels = new string[] { "Internal", "External" };
            var infos = new ImportInfo[] { internalInfo, externalInfo };
            var fieldNames = new string[] { internalFieldName, externalFieldName };

            for (var i = 0; i < labels.Length; i++)
            {
                GUILayout.Label(labels[i]);

                var info = infos[i];
                var fieldName = fieldNames[i];

                using (new ContentsScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        
                        var relativePath = info.sourceFolderRelativePath;

                        relativePath = EditorGUILayout.DelayedTextField("Source Folder", relativePath, GUILayout.Height(16f), GUILayout.ExpandWidth(true));

                        if (EditorGUI.EndChangeCheck())
                        {
                            info.sourceFolderRelativePath = relativePath;
                        }

                        if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(50f)))
                        {
                            // このタイミングでEditorUtility.OpenFolderPanelを呼ぶとGUIレイアウトエラーが発生するので後で実行する.

                            onAfterDrawCallback = () => 
                            {
                                var selectDirectory = EditorUtility.OpenFolderPanel("Select Directory", "", "");

                                if (!string.IsNullOrEmpty(selectDirectory))
                                {
                                    UnityEditorUtility.RegisterUndo(instance);

                                    var assetFolderUri = new Uri(Application.dataPath);
                                    var targetUri = new Uri(selectDirectory);

                                    relativePath = assetFolderUri.MakeRelativeUri(targetUri).ToString();

                                    info.sourceFolderRelativePath = relativePath;
                                    
                                    Reflection.SetPrivateField(instance, fieldName, info);
                                }
                            };
                        }
                    }

                    GUILayout.Space(2f);

                    EditorGUI.BeginChangeCheck();

                    var folderAsset = UnityEditorUtility.FindMainAsset(info.destFolderGuid);
                    
                    var destFolder = EditorGUILayout.ObjectField("Dest Folder", folderAsset, typeof(UnityEngine.Object),  false, GUILayout.Height(16f), GUILayout.ExpandWidth(true));
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (UnityEditorUtility.IsFolder(destFolder))
                        {
                            var destFolderGuid = destFolder != null ? UnityEditorUtility.GetAssetGUID(destFolder) : null;

                            info.destFolderGuid = destFolderGuid;

                            Reflection.SetPrivateField(instance, fieldName, info);
                        }
                    }
                }
            }
        }
    }
}

#endif

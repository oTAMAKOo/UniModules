
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

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

            #if ENABLE_CRIWARE_ADX

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

        #if ENABLE_CRIWARE_ADX

        private void DrawSoundAssetConfigGUI(CriAssetConfig instance)
        {
            var acfAssetSourcePathProperty = serializedObject.FindProperty("acfAssetSourceFullPath");
            var acfAssetExportPathProperty = serializedObject.FindProperty("acfAssetExportPath");

            if (EditorLayoutTools.Header("Sound", "CriAssetConfigInspector-Sound"))
            {
                using (new ContentsScope())
                {
                    DrawAssetImportInfoGUI(instance, instance.SoundImportInfo);

					// Style.
                    var pathTextStyle = GUI.skin.GetStyle("TextArea");
                    pathTextStyle.alignment = TextAnchor.MiddleLeft;

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
                    DrawAssetImportInfoGUI(instance, instance.MovieImportInfo);
				}
            }

            GUILayout.Space(4f);
        }

        #endif

        public void DrawAssetImportInfoGUI(CriAssetConfig instance, CriAssetConfig.AssetImportInfo assetImportInfo)
        {
            // Style.
            var pathTextStyle = GUI.skin.GetStyle("TextArea");
            pathTextStyle.alignment = TextAnchor.MiddleLeft;

            GUILayout.Label("AssetFolderName");

            EditorGUI.BeginChangeCheck();

            var folderName = EditorGUILayout.DelayedTextField(assetImportInfo.FolderName);

            if (EditorGUI.EndChangeCheck())
            {
				UnityEditorUtility.RegisterUndo(instance);

                Reflection.SetPrivateField(assetImportInfo, "folderName", folderName);
            }

            GUILayout.Label("ImportFrom");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(assetImportInfo.ImportPath, pathTextStyle, GUILayout.Height(16f));

                if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(50f)))
                {
					onAfterDrawCallback = () =>
					{
	                    var sourceFolder = EditorUtility.OpenFolderPanel("Select import folder", "", "");

	                    if (!string.IsNullOrEmpty(sourceFolder))
	                    {
							UnityEditorUtility.RegisterUndo(instance);

	                        var assetFolderUri = new Uri(Application.dataPath);
	                        var targetUri = new Uri(sourceFolder);
	                        var importPath = assetFolderUri.MakeRelativeUri(targetUri).ToString();

	                        Reflection.SetPrivateField(assetImportInfo, "importPath", importPath);
						}
					};
                }
            }
		}
    }
}

#endif

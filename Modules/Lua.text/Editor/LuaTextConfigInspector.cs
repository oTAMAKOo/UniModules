
#if ENABLE_XLUA

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Lua.Text
{
	[CustomEditor(typeof(LuaTextConfig))]
    public sealed class LuaTextConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

		private bool isChanged = false;

		private Vector2 scrollPosition = Vector2.zero;

		private GUIContent toolbarPlusIcon = null;
		private GUIContent toolbarMinusIcon = null;

		private GUIStyle inputFieldStyle = null;

		private Action requestSelectSourceFolder = null;

		[NonSerialized]
		private bool initialized = false;

        //----- property -----

        //----- method -----

		private void Initialize()
        {
            if (initialized){ return; }

            toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Plus");
            toolbarMinusIcon = EditorGUIUtility.IconContent("Toolbar Minus");

            initialized = true;
        }

        private void InitializeStyle()
        {
            if (inputFieldStyle == null)
            {
                inputFieldStyle = new GUIStyle(EditorStyles.miniTextField);
            }
        }

        void OnEnable()
        {
            isChanged = false;
        }

        void OnDisable()
        {
            var instance = target as LuaTextConfig;

            if (isChanged)
            {
                EditorUtility.SetDirty(instance);
                AssetDatabase.SaveAssetIfDirty(instance);
            }
        }

        public override void OnInspectorGUI()
        {
			var instance = target as LuaTextConfig;

			GUILayout.Space(4f);

            Initialize();

            InitializeStyle();

			EditorLayoutTools.Title("File Format");
			{
				GUILayout.Space(2f);

				var format = Reflection.GetPrivateField<LuaTextConfig, FileLoader.Format>(instance, "fileFormat");

				EditorGUI.BeginChangeCheck();
					
				format = (FileLoader.Format)EditorGUILayout.EnumPopup("Format", format);

				if (EditorGUI.EndChangeCheck())
				{
					Reflection.SetPrivateField(instance, "fileFormat", format);
				}

				GUILayout.Space(4f);
			}

			EditorLayoutTools.Title("Crypto");
			{
				GUILayout.Space(2f);

				var cryptoKey = Reflection.GetPrivateField<LuaTextConfig, string>(instance, "cryptoKey");
				var cryptoIv = Reflection.GetPrivateField<LuaTextConfig, string>(instance, "cryptoIv");

				EditorGUI.BeginChangeCheck();
					
				cryptoKey = EditorGUILayout.DelayedTextField("Key", cryptoKey, GUILayout.Height(16f));
				
				GUILayout.Space(2f);

				cryptoIv = EditorGUILayout.DelayedTextField("Iv", cryptoIv, GUILayout.Height(16f));

				if (EditorGUI.EndChangeCheck())
				{
					Reflection.SetPrivateField(instance, "cryptoKey", cryptoKey);
					Reflection.SetPrivateField(instance, "cryptoIv", cryptoIv);
				}

				GUILayout.Space(4f);
			}

			EditorLayoutTools.Title("Converter");
			{
				GUILayout.Space(2f);

				var winConverterPath = Reflection.GetPrivateField<LuaTextConfig, string>(instance, "winConverterPath");

				DrawRelativePathGUI(instance, winConverterPath, "Win", "winConverterPath", true);

				var osxConverterPath = Reflection.GetPrivateField<LuaTextConfig, string>(instance, "osxConverterPath");
				
				DrawRelativePathGUI(instance, osxConverterPath, "OSX", "osxConverterPath", true);

                var workspaceRelativePath = Reflection.GetPrivateField<LuaTextConfig, string>(instance, "workspaceRelativePath");
				
                DrawRelativePathGUI(instance, workspaceRelativePath, "Workspace", "workspaceRelativePath", false);

				var settingIniRelativePath = Reflection.GetPrivateField<LuaTextConfig, string>(instance, "settingIniRelativePath");
				
				DrawRelativePathGUI(instance, settingIniRelativePath, "Settings", "settingIniRelativePath", true);

				GUILayout.Space(4f);
			}

			EditorLayoutTools.Title("Transfer");
			{
				GUILayout.Space(2f);
			
				requestSelectSourceFolder = null;

				DrawTransferInfos(instance);

				if (requestSelectSourceFolder != null)
				{
					requestSelectSourceFolder.Invoke();
					requestSelectSourceFolder = null;
				}
			}
		}

		private void DrawRelativePathGUI(LuaTextConfig config, string relativePath, string label, string fieldName, bool fileSelect)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUI.BeginChangeCheck();
				
				relativePath = EditorGUILayout.DelayedTextField(label, relativePath, GUILayout.Height(16f), GUILayout.ExpandWidth(true));

				if (EditorGUI.EndChangeCheck())
				{
					Reflection.SetPrivateField(config, fieldName, relativePath);
				}

				if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(50f)))
				{
					var selectPath = string.Empty;

                    if (fileSelect)
                    {
                        selectPath = EditorUtility.OpenFilePanel("Select", "", "");
                    }
                    else
                    {
                        selectPath = EditorUtility.OpenFolderPanel("Select", "", "");
                    }

					if (!string.IsNullOrEmpty(selectPath))
					{
						UnityEditorUtility.RegisterUndo(config);

						var assetFolderUri = new Uri(Application.dataPath);
						var targetUri = new Uri(selectPath);

						relativePath = assetFolderUri.MakeRelativeUri(targetUri).ToString();

						Reflection.SetPrivateField(config, fieldName, relativePath);
					}
				}
			}
		}

		private void DrawTransferInfos(LuaTextConfig config)
		{
			var transferInfos = config.TransferInfos.ToList();

            var removeInfos = new List<LuaTextConfig.TransferInfo>();

            var update = false;

			using (new EditorGUILayout.HorizontalScope())
            {
				GUILayout.Space(8f);

                if (GUILayout.Button(toolbarPlusIcon, GUILayout.Width(50f), GUILayout.Height(16f)))
                {
                    var transferInfo = new LuaTextConfig.TransferInfo();

					transferInfos.Add(transferInfo);

                    update = true;
                }
			}

            var swapTargets = new List<Tuple<int, int>>();

            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.ExpandWidth(true)))
            {
                for (var i = 0; i < transferInfos.Count; i++)
                {
                    var transferInfo = transferInfos[i];

                    using (new ContentsScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUI.BeginChangeCheck();

                            var index = EditorGUILayout.DelayedIntField("Index", i, inputFieldStyle, GUILayout.Height(16f), GUILayout.ExpandWidth(true));

                            if (EditorGUI.EndChangeCheck())
                            {
                                if (0 <= index && index < transferInfos.Count)
                                {
                                    swapTargets.Add(Tuple.Create(i, index));
                                }
                            }

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(toolbarMinusIcon, EditorStyles.miniButton, GUILayout.Width(35f)))
                            {
                                removeInfos.Add(transferInfo);
                            }
                        }

						using (new EditorGUILayout.HorizontalScope())
						{
							EditorGUI.BeginChangeCheck();
							
							var relativePath = transferInfo.sourceFolderRelativePath;

							relativePath = EditorGUILayout.DelayedTextField("Source Folder", relativePath, GUILayout.Height(16f), GUILayout.ExpandWidth(true));

							if (EditorGUI.EndChangeCheck())
							{
								transferInfo.sourceFolderRelativePath = relativePath;
							}

							if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(50f)))
							{
								var info = transferInfo;

								// このタイミングでEditorUtility.OpenFolderPanelを呼ぶとGUIレイアウトエラーが発生するので後で実行する.

								requestSelectSourceFolder = () => 
								{
									var selectDirectory = EditorUtility.OpenFolderPanel("Select Directory", "", "");

									if (!string.IsNullOrEmpty(selectDirectory))
									{
										UnityEditorUtility.RegisterUndo(config);

										var assetFolderUri = new Uri(Application.dataPath);
										var targetUri = new Uri(selectDirectory);

										relativePath = assetFolderUri.MakeRelativeUri(targetUri).ToString();

										info.sourceFolderRelativePath = relativePath;
										
										Reflection.SetPrivateField(config, "transferInfos", transferInfos.ToArray());
									}
								};
							}
						}

                        GUILayout.Space(2f);

                        EditorGUI.BeginChangeCheck();

						var folderAsset = UnityEditorUtility.FindMainAsset(transferInfo.destFolderGuid);
                        
						var destFolder = EditorGUILayout.ObjectField("Dest Folder", folderAsset, typeof(Object),  false, GUILayout.Height(16f), GUILayout.ExpandWidth(true));
                        
						if (EditorGUI.EndChangeCheck())
						{
							if (UnityEditorUtility.IsFolder(destFolder))
							{
								var destFolderGuid = destFolder != null ? UnityEditorUtility.GetAssetGUID(destFolder) : null;

								transferInfo.destFolderGuid = destFolderGuid;

								Reflection.SetPrivateField(config, "transferInfos", transferInfos.ToArray());
							}
						}
					}

                    GUILayout.Space(4f);
                }

                scrollPosition = scrollView.scrollPosition;
            }

            if (swapTargets.Any())
            {
                foreach (var swapTarget in swapTargets)
                {
					transferInfos = transferInfos.Swap(swapTarget.Item1, swapTarget.Item2).ToList();
                }

                update = true;
            }

            if (removeInfos.Any())
            {
                foreach (var info in removeInfos)
                {
					transferInfos.Remove(info);
                }

                update = true;
            }

            if (update)
            {
                Reflection.SetPrivateField(config, "transferInfos", transferInfos.ToArray());

                isChanged = true;
            }
		}
    }
}

#endif
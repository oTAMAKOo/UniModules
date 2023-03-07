
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning.TextureAsset
{
	[CustomEditor(typeof(TextureConfig))]
    public sealed class TextureConfigInspector : Editor
    {
        //----- params -----

		private enum DisplayMode
		{
			Asset,
			Path,
		}

		private static readonly Dictionary<TextureImporterType, Type> InspectorDrawerTypeTable = new()
		{
			{ TextureImporterType.Default, typeof(TextureDefaultInspectorDrawer) },
			{ TextureImporterType.Sprite, typeof(TextureSpriteInspectorDrawer) },
			{ TextureImporterType.NormalMap, typeof(TextureNormalMapInspectorDrawer) },
			{ TextureImporterType.GUI, typeof(TextureGUIInspectorDrawer) },
			{ TextureImporterType.Cursor, typeof(TextureCursorInspectorDrawer) },
			{ TextureImporterType.Cookie, typeof(TextureCookieInspectorDrawer) },
			{ TextureImporterType.Lightmap, typeof(TextureLightMapInspectorDrawer) },
			{ TextureImporterType.DirectionalLightmap, typeof(TextureDirectionalLightmapInspectorDrawer) },
			{ TextureImporterType.Shadowmask, typeof(TextureShadowmaskInspectorDrawer) },
			{ TextureImporterType.SingleChannel, typeof(TextureSingleChannelInspectorDrawer) },
		};

		//----- field -----

		private TextureConfig instance = null;

		private TextureData editTarget = null;

		private ReorderableList reorderableList = null;

		private List<TextureData> contents = null;

		private DisplayMode displayMode = DisplayMode.Asset;
		
		private TextureDataInspectorDrawer textureDataInspectorDrawer = null;

		[NonSerialized]
		private bool initialized = false;

        //----- property -----

        //----- method -----

		private void Initialize()
		{
			if (initialized){ return; }

			editTarget = null;
			
			SetupReorderableList();

			LoadContents();

			initialized = true;
		}

		void OnDisable()
		{
			SaveContents();
		}

		private void SetupReorderableList()
		{
			if (reorderableList != null){ return; }

			reorderableList = new ReorderableList(new List<TextureData>(), typeof(TextureData));

			// ヘッダー描画コールバック.
			reorderableList.drawHeaderCallback = r =>
			{
				EditorGUI.LabelField(r, "Target Folders");
			};

			// 要素描画コールバック.
			reorderableList.drawElementCallback = (r, index, isActive, isFocused) => 
			{
				r.position = Vector.SetY(r.position, r.position.y + 2f);
				r.height = EditorGUIUtility.singleLineHeight;

				var content = contents.ElementAtOrDefault(index);

				EditorGUI.BeginChangeCheck();

				content = DrawContent(r, index, content);

				if (EditorGUI.EndChangeCheck())
				{
					contents[index] = content;

					reorderableList.list = contents;
				}
			};

			// 順番入れ替えコールバック.
			reorderableList.onReorderCallback = x =>
			{
				contents = x.list.Cast<TextureData>().ToList();
				
				UpdateContents();
			};

			// 追加コールバック.
			reorderableList.onAddCallback = list =>
			{
				contents.Add(CreateNewContent());

				UpdateContents();
			};

			// 削除コールバック.
			reorderableList.onRemoveCallback = list =>
			{
				contents.RemoveAt(list.index);

				UpdateContents();
			};
		}

		public override void OnInspectorGUI()
		{
			instance = target as TextureConfig;

			Initialize();

			GUILayout.Space(4f);

			if (editTarget != null)
			{
				DrawTextureDataGUI(editTarget);
			}
			else
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					displayMode = (DisplayMode)EditorGUILayout.EnumPopup(displayMode, GUILayout.Width(80f));

					GUILayout.Space(5f);

					if (GUILayout.Button("Sort by AssetPath", EditorStyles.miniButton, GUILayout.Width(125f)))
					{
						contents = contents
							.OrderBy(x =>AssetDatabase.GUIDToAssetPath(x.folderGuid), new NaturalComparer())
							.ToList();

						SaveContents();
					}

					GUILayout.FlexibleSpace();

					if (GUILayout.Button("Default setting", EditorStyles.miniButton, GUILayout.Width(125f)))
					{
						editTarget = instance.DefaultData;
					}
				}

				EditorGUILayout.Separator();

				reorderableList.DoLayoutList();

				EditorGUILayout.Separator();

				EditorLayoutTools.ContentTitle("Option (Unsafe)");

				using (new ContentsScope())
				{
					TextureConfig.Prefs.forceModifyOnImport = EditorGUILayout.Toggle("Force modify on import", TextureConfig.Prefs.forceModifyOnImport);
				}
			}
		}

		private TextureData DrawContent(Rect rect, int index, TextureData info)
		{
			var totalWidth = rect.width;

			var buttonWidth = 80f;
			var padding = 5f;

			EditorGUI.BeginChangeCheck();

			var folderGuid = info.folderGuid;

			switch (displayMode)
			{
				case DisplayMode.Asset:
					{
						var folderAsset = UnityEditorUtility.FindMainAsset(folderGuid);
				
						rect.width = totalWidth - (buttonWidth + padding);

						folderAsset = EditorGUI.ObjectField(rect, folderAsset, typeof(Object), false);
					
						if (EditorGUI.EndChangeCheck())
						{
							if (folderAsset != null)
							{
								var isFolder = UnityEditorUtility.IsFolder(folderAsset);

								if (isFolder)
								{
									var newfolderGuid = UnityEditorUtility.GetAssetGUID(folderAsset);

									// フォルダ登録.
									if (contents.All(x => x.folderGuid != newfolderGuid))
									{
										contents[index].folderGuid = newfolderGuid;
									}
									// 既に登録済み.
									else
									{
										EditorUtility.DisplayDialog("Error", "This folder is already registered.", "close");
									}
								}
								// フォルダではない.
								else
								{
									EditorUtility.DisplayDialog("Error", "This asset is not a folder.", "close");
								}
							}
							// 設定が対象が外れた場合は初期化.
							else
							{
								info.folderGuid = string.Empty;
							}
						}
					}
					break;

				case DisplayMode.Path:
					{
						var assetPath = AssetDatabase.GUIDToAssetPath(folderGuid);

						rect.width = totalWidth - (buttonWidth + padding);

						EditorGUI.TextField(rect, assetPath);
					}
					break;
			}

			using (new DisableScope(string.IsNullOrEmpty(info.folderGuid)))
			{
				var buttonRect = new Rect(totalWidth - buttonWidth * 0.5f + padding, rect.y, buttonWidth, rect.height);

				if (GUI.Button(buttonRect, "edit", EditorStyles.miniButton))
				{
					textureDataInspectorDrawer = null;
					editTarget = info;
				}
			}

			return info;
		}

		private void SetTextureContent(TextureData data)
		{
			textureDataInspectorDrawer = null;

			var inspectorDrawerType = InspectorDrawerTypeTable.GetValueOrDefault(data.textureType);

			if (inspectorDrawerType != null)
			{
				textureDataInspectorDrawer = Activator.CreateInstance(inspectorDrawerType, data) as TextureDataInspectorDrawer;
			}
		}

		private void DrawTextureDataGUI(TextureData data)
		{
			if (textureDataInspectorDrawer == null)
			{
				SetTextureContent(data);
			}
			
			using (new EditorGUILayout.HorizontalScope())
			{
				if (!string.IsNullOrEmpty(data.folderGuid))
				{
					using (new DisableScope(true))
					{
						var folderAsset = UnityEditorUtility.FindMainAsset(data.folderGuid);

						EditorGUILayout.ObjectField(folderAsset, typeof(Object), false);
					}
				}
				else
				{
					GUILayout.FlexibleSpace();
				}

				if (GUILayout.Button("exit", EditorStyles.miniButton, GUILayout.Width(80f)))
				{
					textureDataInspectorDrawer = null;
					
					editTarget = null;

					SaveContents();
				}
			}

			EditorGUILayout.Separator();

			using (new ContentsScope())
			{
				EditorGUI.BeginChangeCheck();

				data.textureType = (TextureImporterType)EditorGUILayout.EnumPopup("Texture Type", data.textureType);

				if (EditorGUI.EndChangeCheck())
				{
					SetTextureContent(data);
				}
			}

			GUILayout.Space(2f);

			if (textureDataInspectorDrawer != null)
			{
				textureDataInspectorDrawer.DrawInspectorGUI();
			}
			else
			{
				EditorGUILayout.HelpBox($"Texture type {data.textureType} is not support.", MessageType.Warning);
			}
		}

		private TextureData CreateNewContent()
		{
			return instance.CreateNewData();
		}

		private void UpdateContents()
		{
			reorderableList.list = contents;
		}

		private void LoadContents()
		{
			var customData = Reflection.GetPrivateField<TextureConfig, TextureData[]>(instance, "customData");

			if (customData == null)
			{
				customData = new TextureData[0];
			}

			contents = customData.ToList();

			UpdateContents();
		}

		private void SaveContents()
		{
			var customData = contents
				.Where(x => !string.IsNullOrEmpty(x.folderGuid))
				.Where(x =>
					{
						var assetPath = AssetDatabase.GUIDToAssetPath(x.folderGuid);
						return AssetDatabase.IsValidFolder(assetPath);
					})
				.ToArray();

			Reflection.SetPrivateField(instance, "customData", customData);

			UnityEditorUtility.SaveAsset(instance);
		}
	}
}
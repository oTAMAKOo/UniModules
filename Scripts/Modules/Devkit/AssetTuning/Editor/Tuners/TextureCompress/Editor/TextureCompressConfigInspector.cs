
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning
{
	[CustomEditor(typeof(TextureCompressConfig))]
    public sealed class TextureCompressConfigInspector : Editor
    {
        //----- params -----

		//----- field -----

		private TextureCompressConfig instance = null;

		private CompressSetting editTarget = null;

		private ReorderableList reorderableList = null;

		private List<CompressSetting> contents = null;

		private int tabSelection = 0;
		
		private bool initialized = false;

        //----- property -----

        //----- method -----

		private void Initialize()
		{
			if (initialized){ return; }
			
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

			reorderableList = new ReorderableList(new List<CompressSetting>(), typeof(CompressSetting));

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
				contents = x.list.Cast<CompressSetting>().ToList();
				
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
			instance = target as TextureCompressConfig;

			Initialize();

			GUILayout.Space(4f);

			if (editTarget != null)
			{
				DrawFolderCompressInfoGUI(editTarget, () => editTarget = null);
			}
			else
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.FlexibleSpace();

					if (GUILayout.Button("Default setting", EditorStyles.miniButton, GUILayout.Width(125f)))
					{
						editTarget = instance.DefaultSetting;
					}
				}

				EditorGUILayout.Separator();

				reorderableList.DoLayoutList();
			}
		}

		private CompressSetting DrawContent(Rect rect, int index, CompressSetting info)
		{
			var totalWidth = rect.width;

			var buttonWidth = 80f;
			var padding = 5f;

			EditorGUI.BeginChangeCheck();

			var folderGuid = info.folderGuid;

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

			using (new DisableScope(string.IsNullOrEmpty(info.folderGuid)))
			{
				var buttonRect = new Rect(totalWidth - buttonWidth * 0.5f + padding, rect.y, buttonWidth, rect.height);

				if (GUI.Button(buttonRect, "edit", EditorStyles.miniButton))
				{
					tabSelection = 0;
					editTarget = info;
				}
			}

			return info;
		}

		private void DrawFolderCompressInfoGUI(CompressSetting info, Action onClose)
		{
		
			using (new EditorGUILayout.HorizontalScope())
			{
				if (!string.IsNullOrEmpty(info.folderGuid))
				{
					using (new DisableScope(true))
					{
						var folderAsset = UnityEditorUtility.FindMainAsset(info.folderGuid);

						EditorGUILayout.ObjectField(folderAsset, typeof(Object), false);
					}
				}
				else
				{
					GUILayout.FlexibleSpace();
				}

				if (GUILayout.Button("exit", EditorStyles.miniButton, GUILayout.Width(80f)))
				{
					tabSelection = 0;
					onClose?.Invoke();
				}
			}

			GUILayout.Space(4f);

			using (new ContentsScope())
			{
				var tabContents = new Dictionary<BuildTargetGroup, CompressInfo>
				{
					{ BuildTargetGroup.Standalone, info.standaloneSetting },
					{ BuildTargetGroup.iOS, info.iosSetting },
					{ BuildTargetGroup.Android, info.androidSetting },
				};

				var toolbarLabels = tabContents.Select(x => x.Key.ToString()).ToArray();

				tabSelection = GUILayout.Toolbar(tabSelection, toolbarLabels);

				EditorGUILayout.Separator();

				var item = tabContents.ElementAtOrDefault(tabSelection);

				if (!item.IsDefault())
				{
					DrawCompressSettingGUI(item.Key, item.Value);
				}
			}
		}

		private void DrawCompressSettingGUI(BuildTargetGroup targetGroup, CompressInfo info)
		{
			// isOverride.

			info.isOverride = EditorGUILayout.Toggle($"Override For {targetGroup}", info.isOverride);

			GUILayout.Space(2f);

			using (new DisableScope(!info.isOverride))
			{
				// MaxSize.

				var sizeTable = new int[]{ 32, 64, 128, 256, 512, 1024, 2048 };

				var maxSizeLabels = sizeTable.Select(x => x.ToString()).ToArray();
				var maxSizeIndex = sizeTable.IndexOf(x => x == info.maxSize); 

				EditorGUI.BeginChangeCheck();

				maxSizeIndex = EditorGUILayout.Popup("MaxSize", maxSizeIndex, maxSizeLabels);

				if (EditorGUI.EndChangeCheck())
				{
					info.maxSize = sizeTable[maxSizeIndex];
				}

				GUILayout.Space(2f);

				// ResizeAlgorithm.

				info.resizeAlgorithm = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("ResizeAlgorithm", info.resizeAlgorithm);

				// Format.

				info.format = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", info.format);

				GUILayout.Space(2f);
				
				// CompressionQuality.

				var qualityTable = new int[]{ 0, 50, 100 };

				var qualityLabels = new string[]{ "Fast", "Normal", "Best" };
				var qualityIndex = qualityTable.IndexOf(x => x == info.compressionQuality); 

				EditorGUI.BeginChangeCheck();

				qualityIndex = EditorGUILayout.Popup("Compression Quality", qualityIndex, qualityLabels);

				if (EditorGUI.EndChangeCheck())
				{
					info.compressionQuality = qualityTable[qualityIndex];
				}

				GUILayout.Space(2f);
			}
		}

		private CompressSetting CreateNewContent()
		{
			return instance.CreateNewCompressSetting();
		}

		private void UpdateContents()
		{
			reorderableList.list = contents;
		}

		private void LoadContents()
		{
			var compressSettings = Reflection.GetPrivateField<TextureCompressConfig, CompressSetting[]>(instance, "compressSettings");

			if (compressSettings == null)
			{
				compressSettings = new CompressSetting[0];
			}

			contents = compressSettings.ToList();

			UpdateContents();
		}

		private void SaveContents()
		{
			var compressSettings = contents
				.Where(x => !string.IsNullOrEmpty(x.folderGuid))
				.Where(x =>
					{
						var assetPath = AssetDatabase.GUIDToAssetPath(x.folderGuid);
						return AssetDatabase.IsValidFolder(assetPath);
					})
				.ToArray();

			Reflection.SetPrivateField(instance, "compressSettings", compressSettings);

			UnityEditorUtility.SaveAsset(instance);
		}
	}
}
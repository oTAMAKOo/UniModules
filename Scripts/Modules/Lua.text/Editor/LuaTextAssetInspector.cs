
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Lua.Text
{
	[CustomEditor(typeof(LuaTextAsset))]
    public sealed class LuaTextAssetInspector : UnityEditor.Editor
    {
        //----- params -----

		public sealed class TextInfo
		{
			public string id = null;
			public string text = null;
		}

		private sealed class TextInfoScrollView : EditorGUIFastScrollView<TextInfo>
        {
            private GUIStyle textAreaStyle = null;

			public override Direction Type
            {
                get { return Direction.Vertical; }
            }

            protected override void DrawContent(int index, TextInfo content)
            {
                if (textAreaStyle == null)
                {
                    textAreaStyle = new GUIStyle(EditorStyles.textArea);
                    textAreaStyle.alignment = TextAnchor.MiddleLeft;
                    textAreaStyle.wordWrap = false;
                    textAreaStyle.stretchWidth = true;
                }
				
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.SelectableLabel(content.id, textAreaStyle, GUILayout.Width(150f), GUILayout.Height(18f));

					EditorGUILayout.TextArea(content.text);
				}

				EditorGUILayout.Space(1f);
            }
        }

        //----- field -----

		private int selectionSheetIndex = 0;

		private string searchText = null;
		
		private List<string> sheetNames = null;

		private string[] displayNames = null;

		private string updateAtText = null;

		private Dictionary<string, string> sheetSummary = null;

		private Dictionary<string, List<LuaTextAsset.TextData>> sheetTexts = null;

		private TextInfoScrollView textInfoScrollView = null;

		private TextInfo[] textInfos = null;

		private string workspace = null;
		private string excelPath = null;

		[NonSerialized]
		private bool initialized = false;

        //----- property -----

        //----- method -----

		private void Initialize()
		{
			if (initialized){ return; }
			
			sheetNames = new List<string>();
			sheetSummary = new Dictionary<string, string>();
			sheetTexts = new Dictionary<string, List<LuaTextAsset.TextData>>();

			var instance = target as LuaTextAsset;

			var config = LuaTextConfig.Instance;

			// テキストデータ.

			var aesCryptoKey = config.GetCryptoKey();

			foreach (var content in instance.Contents)
			{
				var sheetName = content.sheetName.Decrypt(aesCryptoKey);

				if (!sheetNames.Contains(sheetName))
				{
					sheetNames.Add(sheetName);
				}

				var summary = content.summary.Decrypt(aesCryptoKey);
				
				sheetSummary[sheetName] = summary;

				var texts = sheetTexts.GetValueOrDefault(sheetName);

				if (texts == null)
				{
					texts = new List<LuaTextAsset.TextData>();

					sheetTexts[sheetName] = texts;
				}

				texts.AddRange(content.texts);
			}

			// 表示名.

			displayNames = sheetNames
				.Select(x =>
					{
						var summary = sheetSummary.GetValueOrDefault(x);

						return string.IsNullOrEmpty(summary) ? x : $"{x} : {summary}";
					})
				.ToArray();

			// 更新日時テキスト.

			if (instance.UpdateAt.HasValue)
			{
				var updateAtDateTime = instance.UpdateAt.Value.UnixTimeToDateTime();
				var localTime = TimeZoneInfo.ConvertTimeFromUtc(updateAtDateTime, TimeZoneInfo.Local);
				
				updateAtText = localTime.ToString("yyyy/MM/dd HH:mm:ss");
			}

			// リスト構築.

			textInfoScrollView = new TextInfoScrollView();

			UpdateSheetContents();

			// Excelパス.

			var transferInfo = config.TransferInfos.FirstOrDefault(x => x.destFolderGuid == instance.RootFolderGuid);

			if (transferInfo != null)
			{
				workspace = UnityPathUtility.RelativePathToFullPath(transferInfo.sourceFolderRelativePath);

				var rootFolderAssetPath = AssetDatabase.GUIDToAssetPath(instance.RootFolderGuid);

				var assetPath = AssetDatabase.GetAssetPath(instance);

				var localPath = assetPath.Replace(rootFolderAssetPath, string.Empty);

				var localDirectory = Path.GetDirectoryName(localPath);
				
				excelPath = PathUtility.Combine(workspace, localDirectory) + instance.FileName + LuaTextExcel.ExcelExtension;
			}

			initialized = true;
		}
		
		public override void OnInspectorGUI()
		{
			var instance = target as LuaTextAsset;

			Initialize();

			if (!string.IsNullOrEmpty(updateAtText))
			{
				EditorGUILayout.Space(2f);

				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.PrefixLabel("UpdateAt");

					EditorGUILayout.SelectableLabel(updateAtText, EditorStyles.textArea, GUILayout.Height(18f));
				}
			}

			EditorGUILayout.Space(4f);

			EditorLayoutTools.Title("Excel");

			using (new ContentsScope())
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					var isExists = File.Exists(excelPath);
					var isLock = isExists && FileUtility.IsFileLocked(excelPath);

					using (new DisableScope(!isExists))
					{
						if (GUILayout.Button("Open"))
						{
							LuaTextExcel.Open(excelPath);
						}
					}

					using (new DisableScope(isLock))
					{
						if (GUILayout.Button("Import"))
						{
							LuaTextExcel.Import(workspace, new string[]{ excelPath }, true).Forget();
						}
					}

					using (new DisableScope(!isExists))
					{
						if (GUILayout.Button("Export"))
						{
							LuaTextExcel.Export(workspace, new string[]{ excelPath }, true).Forget();
						}
					}
				}
			}

			EditorLayoutTools.Title("Contents");

			using (new ContentsScope())
			{
				EditorGUI.BeginChangeCheck();

				// シート選択.
				
				selectionSheetIndex = EditorGUILayout.Popup(selectionSheetIndex, displayNames);

				if(EditorGUI.EndChangeCheck())
				{
					UpdateSheetContents();
				}

				EditorGUILayout.Space(2f);

				// 検索フィールド.

				Action<string> onChangeSearchText = x =>
                {
                    searchText = x;
					textInfoScrollView.Contents = GetDisplayTextInfos();
                };

                Action onSearchCancel = () =>
                {
                    searchText = string.Empty;
					textInfoScrollView.Contents = GetDisplayTextInfos();
                };

				EditorLayoutTools.DrawDelayedSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.MinWidth(150f));

				EditorGUILayout.Space(4f);

				// シートテキスト一覧.

				textInfoScrollView.Draw();
			}
		}

		private void UpdateSheetContents()
		{
			var config = LuaTextConfig.Instance;

			var aesCryptoKey = config.GetCryptoKey();

			var list = new List<TextInfo>();

			var sheetName = sheetNames[selectionSheetIndex];

			var textDatas = sheetTexts.GetValueOrDefault(sheetName);

			foreach (var textData in textDatas)
			{
				var textInfo = new TextInfo()
				{
					id = textData.Id,
					text = textData.Text.Decrypt(aesCryptoKey),
				};

				list.Add(textInfo);
			}

			textInfos = list.ToArray();

			textInfoScrollView.Contents = GetDisplayTextInfos();

			Repaint();
		}

		private TextInfo[] GetDisplayTextInfos()
		{
			if (string.IsNullOrEmpty(searchText)) { return textInfos; }

			var list = new List<TextInfo>();

			var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			for (var i = 0; i < keywords.Length; ++i)
			{
				keywords[i] = keywords[i].ToLower();
			}

			foreach (var textInfo in textInfos)
			{
				if (textInfo.id.IsMatch(keywords) || textInfo.text.IsMatch(keywords))
				{
					list.Add(textInfo);
				}
			}

			return list.ToArray();
		}
    }
}
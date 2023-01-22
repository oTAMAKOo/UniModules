
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.ExternalAssets;

namespace Modules.Devkit.ExternalAssets
{
    public sealed class ExternalAssetLoadRecorder : SingletonEditorWindow<ExternalAssetLoadRecorder>
    {
        //----- params -----

		private readonly Vector2 WindowSize = new Vector2(600f, 400f);

		private sealed class ContentInfo
		{
			private static uint uniqueId = 0;

			public uint Id { get; private set; }

			public AssetInfo AssetInfo { get; private set; }

			public ContentInfo(AssetInfo assetInfo)
			{
				Id = uniqueId++;
				AssetInfo = assetInfo;
			}
		}

        //----- field -----

		private List<ContentInfo> loadedAssetInfos = null;

		private uint? selection = null;

		private string searchText = null;

		private Vector2 scrollPosition = Vector2.zero;

		[NonSerialized]
		private bool initialized = false;

        //----- property -----

        //----- method -----
		
		public static void Open()
		{
			Instance.Initialize();

			Instance.Show();
		}

		private void Initialize()
		{
			if (initialized){ return; }

			titleContent = new GUIContent("ExternalAssetLoadRecorder");

			minSize = WindowSize;

			loadedAssetInfos = new List<ContentInfo>();

			var externalAsset = ExternalAsset.Instance;

			externalAsset.OnLoadAssetAsObservable()
				.Subscribe(x => OnLoadAsset(x))
				.AddTo(Disposable);

			Observable.EveryEndOfFrame()
				.Subscribe(_ => Repaint())
				.AddTo(Disposable);

			initialized = true;
		}

		void OnGUI()
		{
			Initialize();

			if(!IsExternalAssetSetup())
			{
				EditorGUILayout.HelpBox("ExternalAsset not initialized.", MessageType.Error);

				return;
			}

			if (IsExternalAssetSimulateMode())
			{
				EditorGUILayout.HelpBox("Cannot used in SimulateMode.", MessageType.Error);
				return;
			}

			// Toolbar.

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                Action<string> onChangeSearchText = x =>
                {
                    searchText = x;
                };

                Action onSearchCancel = () =>
                {
                    searchText = string.Empty;
                };

                EditorLayoutTools.DrawToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.MinWidth(150f));

                GUILayout.FlexibleSpace();

				if (GUILayout.Button("Export", EditorStyles.toolbarButton))
				{
					Export().Forget();
				}

				GUILayout.Space(5f);
            }

            // ScrollView.

			EditorGUILayout.Separator();

			AssetInfo selectionAssetInfo = null;

			using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
			{
				var displayContents = GetDisplayContents();

				foreach (var contentInfo in displayContents)
				{
					var resourcePath = contentInfo.AssetInfo.ResourcePath;
					var isSelection = selection == contentInfo.Id;
					var backgroundColor = isSelection ? Color.cyan : Color.white;

					if (isSelection)
					{
						selectionAssetInfo = contentInfo.AssetInfo;
					}

					using (new BackgroundColorScope(backgroundColor))
					{
						if (GUILayout.Button(resourcePath, EditorStyles.textField, GUILayout.Height(18f)))
						{
							selection = contentInfo.Id;
						}
					}

					EditorGUILayout.Space(2f);
				}

				scrollPosition = scrollViewScope.scrollPosition;
			}

			EditorGUILayout.Separator();

			using (new EditorGUILayout.VerticalScope(GUILayout.Height(120f)))
			{
				using (new ContentsScope())
				{
					if (selectionAssetInfo != null)	
					{
						using (new LabelWidthScope(90f))
						{
							EditorGUILayout.LabelField("Group", selectionAssetInfo.Group);
						
							EditorGUILayout.LabelField("ResourcePath", selectionAssetInfo.ResourcePath);
						
							EditorGUILayout.LabelField("FileName", selectionAssetInfo.FileName);

							EditorGUILayout.LabelField("Guid", selectionAssetInfo.Guid);

							EditorGUILayout.LabelField("CRC", selectionAssetInfo.CRC);
						
							EditorGUILayout.LabelField("Hash", selectionAssetInfo.Hash);
						}
					}

					GUILayout.FlexibleSpace();
				}
			}
		}

		private void OnLoadAsset(string resourcePath)
		{
			var externalAsset = ExternalAsset.Instance;

			var assetInfo = externalAsset.GetAssetInfo(resourcePath);

			if (assetInfo == null){ return; }

			var contentInfo = new ContentInfo(assetInfo);

			loadedAssetInfos.Add(contentInfo);
		}

		private bool IsExternalAssetSetup()
		{
			return ExternalAsset.Instance != null && ExternalAsset.Initialized;
		}

		private bool IsExternalAssetSimulateMode()
		{
			return ExternalAsset.Instance.SimulateMode;
		}

		private IEnumerable<ContentInfo> GetDisplayContents()
		{
			if (string.IsNullOrEmpty(searchText)) { return loadedAssetInfos; }

			var list = new List<ContentInfo>();

			var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			for (var i = 0; i < keywords.Length; ++i)
			{
				keywords[i] = keywords[i].ToLower();
			}

			foreach (var contentInfo in loadedAssetInfos)
			{
				var assetInfo = contentInfo.AssetInfo;
				
				if (assetInfo.ResourcePath.IsMatch(keywords))
				{
					list.Add(contentInfo);
				}
				
				if (!string.IsNullOrEmpty(assetInfo.FileName))
				{
					if (assetInfo.FileName.IsMatch(keywords))
					{
						list.Add(contentInfo);
					}
				}
			}

			return list.ToArray();
		}

		private async UniTask Export()
		{
			var externalAsset = ExternalAsset.Instance;

			var folderPath = EditorUtility.OpenFolderPanel("Select export folder", null, null);

			if (string.IsNullOrEmpty(folderPath)){ return; }

			var list = new HashSet<string>();

			foreach (var contentInfo in loadedAssetInfos)
			{
				var filePath = externalAsset.GetFilePath(contentInfo.AssetInfo);

				if (list.Contains(filePath)){ continue; }

				list.Add(filePath);
			}

			var chunck = list.Chunk(25);

			foreach (var items in chunck)
			{
				foreach (var item in items)
				{
					var fileName = Path.GetFileName(item);

					var from = PathUtility.ConvertPathSeparator(item);
					var to = PathUtility.Combine(folderPath, fileName);

					File.Copy(from, to, true);
				}

				await UniTask.NextFrame();
			}

			Debug.Log($"Export complete.");
		}
    }
}

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.AssetBundles;
using Modules.Devkit.Console;
using Modules.ExternalAssets;
using Modules.Devkit.Project;

namespace Modules.Devkit.ExternalAssets
{
    public sealed class SimulationModeAssetFileTracker : SingletonEditorWindow<SimulationModeAssetFileTracker>
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

		private GUIContent recordOnIconContent = null;
		private GUIContent recordOffIconContent = null;

		[NonSerialized]
		private bool initialized = false;

        //----- property -----

		public bool IsRecording { get; private set; }

        //----- method -----
		
		public static void Open()
		{
			Instance.Initialize();

			Instance.Show();
		}

		private void Initialize()
		{
			if (initialized){ return; }

			titleContent = new GUIContent("SimulationModeAssetFileTracker");

			minSize = WindowSize;

			loadedAssetInfos = new List<ContentInfo>();

			recordOffIconContent = EditorGUIUtility.IconContent("Record Off");
			recordOnIconContent = EditorGUIUtility.IconContent("Record On");

			var externalAsset = ExternalAsset.Instance;

			externalAsset.OnLoadAssetAsObservable()
				.Subscribe(x => OnLoadAsset(x))
				.AddTo(Disposable);

			SimulationModeAssetFileTrackerBridge.OnRequestStatusChangeAsObservable()
				.Subscribe(x => IsRecording = x)
				.AddTo(Disposable);

			Observable.EveryEndOfFrame()
				.Subscribe(_ => Repaint())
				.AddTo(Disposable);

			EditorApplication.playModeStateChanged += x =>
			{
				if (x == PlayModeStateChange.EnteredPlayMode)
				{
					IsRecording = false;
					loadedAssetInfos.Clear();
				}
			};

			IsRecording = false;

			initialized = true;
		}

		void OnGUI()
		{
			// 初期化されるまでは処理しない.
			if(!IsExternalAssetSetup())
			{
				EditorGUILayout.HelpBox("ExternalAsset not initialized.", MessageType.Warning);

				return;
			}

			// シミュレーションモード以外では動作させない.
			if(!IsExternalAssetSimulateMode())
			{
				EditorGUILayout.HelpBox("Require simulation mode.", MessageType.Error);

				return;
			}

			// 初期化済みでない場合初期化.
			Initialize();

			// Toolbar.

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
				void OnChangeSearchText(string x)
				{
					searchText = x;
				}

				void OnSearchCancel()
				{
					searchText = string.Empty;
				}

				var recordIconContent = IsRecording ? recordOffIconContent : recordOnIconContent;

				if (GUILayout.Button(recordIconContent, EditorStyles.toolbarButton, GUILayout.Width(32f)))
				{
					IsRecording = !IsRecording;
				}

				if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(45f)))
				{
					loadedAssetInfos.Clear();
				}

				GUILayout.FlexibleSpace();

				EditorLayoutTools.DrawToolbarSearchTextField(searchText, OnChangeSearchText, OnSearchCancel, GUILayout.MinWidth(150f));

				EditorGUILayout.Separator();

				if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(60f)))
				{
					Export().Forget();
				}
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

		// アセット管理情報読み込み.
		private async UniTask<AssetInfoManifest> LoadAssetInfoManifest(string directory)
		{
			if (!IsExternalAssetSetup()){ return null; }

			var assetBundleManager = AssetBundleManager.Instance;

			if (assetBundleManager == null){ return null; }

			var projectResourceFolders = ProjectResourceFolders.Instance;

			if (projectResourceFolders == null){ return null; }

			var externalResourcesPath = projectResourceFolders.ExternalAssetPath;

			var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();
			
			var loadPath = PathUtility.Combine(externalResourcesPath, manifestAssetInfo.ResourcePath);

			var deleteOnLoadError = assetBundleManager.DeleteOnLoadError;
			var isSimulateMode = assetBundleManager.IsSimulateMode;

			// シミュレーションモードを無効化.
			assetBundleManager.SetSimulateMode(false);

			// ロードエラー時の削除を無効化.
			assetBundleManager.DeleteOnLoadError = false;

			// 実ファイルを読み込む.
			var manifest = await assetBundleManager.LoadAsset<AssetInfoManifest>(directory, manifestAssetInfo, loadPath);

			// 元の状態に戻す.
			assetBundleManager.SetSimulateMode(isSimulateMode);
			assetBundleManager.DeleteOnLoadError = deleteOnLoadError;

			return manifest;
		}

		private void OnLoadAsset(string resourcePath)
		{
			if (!IsRecording){ return; }

			var externalAsset = ExternalAsset.Instance;

			var assetInfo = externalAsset.GetAssetInfo(resourcePath);

			if (assetInfo == null){ return; }

			if (loadedAssetInfos.Any(x => x.AssetInfo.ResourcePath == resourcePath)){ return; }

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
			var persistentDataPath = UnityPathUtility.PersistentDataPath;

			var sourceFolder = EditorUtility.OpenFolderPanel("Select source folder", persistentDataPath, null);

			if (string.IsNullOrEmpty(sourceFolder)){ return; }

			var exportFolder = EditorUtility.OpenFolderPanel("Select export folder", null, null);

			if (string.IsNullOrEmpty(exportFolder)){ return; }

			GUI.UnfocusWindow();

			// ※ 参照情報が必要な為、転送元のAssetInfoManifestはビルド成果物の状態でなければいけない.
			var assetInfoManifest = await LoadAssetInfoManifest(sourceFolder);
			
			if (assetInfoManifest == null)
			{
				Debug.LogError("AssetInfoManifest load failed.");

				return;
			}

			assetInfoManifest.BuildCache(true);

			var hashSet = new HashSet<string>();

			var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();

			AddFilePath(hashSet, sourceFolder, manifestAssetInfo);

			var assetInfosByAssetBundleName = assetInfoManifest.GetAssetInfos()
				.Where(x => x.IsAssetBundle)
				.GroupBy(x => x.AssetBundle.AssetBundleName)
				.ToDictionary(x => x.Key, x => x.FirstOrDefault());

			for (var i = 0; i < loadedAssetInfos.Count; i++)
			{
				var contentInfo = loadedAssetInfos[i];

				// シミュレーションモードの情報から本データの情報を取得.
				var resourcePath = contentInfo.AssetInfo.ResourcePath;

				if (manifestAssetInfo.ResourcePath == resourcePath){ continue; }

				var assetInfo = assetInfoManifest.GetAssetInfo(resourcePath);

				if (assetInfo == null)
				{
					Debug.LogError($"AssetInfo not found.\nResourcePath : {resourcePath}");

					continue;
				}
				
				AddFilePath(hashSet, sourceFolder, assetInfo);
                
				if (assetInfo.IsAssetBundle)
				{
					var dependencies = assetInfo.AssetBundle.Dependencies;

					foreach (var item in dependencies)
					{
						var dependantAssetInfo = assetInfosByAssetBundleName[item];

						if (dependantAssetInfo == null)
						{
							Debug.LogError($"Dependant AssetInfo not found.\nResourcePath : {item}");

							continue;
						}

						AddFilePath(hashSet, sourceFolder, dependantAssetInfo);
					}
				}
			}

			var projectFolderPath = UnityPathUtility.GetProjectFolderPath();

			if (exportFolder.StartsWith(projectFolderPath))
			{
				var folderAssetPath = UnityPathUtility.ConvertFullPathToAssetPath(exportFolder);
				
				AssetDatabase.ImportAsset(folderAssetPath, ImportAssetOptions.ImportRecursive);
			}

			var logBuilder = new StringBuilder();

			var array = hashSet.ToArray();

			for (var i = 0; i < array.Length; i++)
			{
				var item = array[i];
			
				var fileName = Path.GetFileName(item);

				EditorUtility.DisplayProgressBar("Export file", fileName, (float)i / array.Length);

				var from = PathUtility.Combine(sourceFolder, fileName);
				var to = PathUtility.Combine(exportFolder, fileName);

				if(!File.Exists(from)){ continue; }

				File.Copy(from, to, true);

				logBuilder.AppendLine(to);
			}

			EditorUtility.ClearProgressBar();

			var logs = logBuilder.ToString();

			LogUtility.ChunkLog(logs, "Export complete", x => UnityConsole.Info(x));
		}

		private void AddFilePath(HashSet<string> hashSet, string directory, AssetInfo assetInfo)
		{
			var externalAsset = ExternalAsset.Instance;

			var filePath = externalAsset.GetFilePath(directory, assetInfo);

			if (!hashSet.Contains(filePath))
			{
				hashSet.Add(filePath);
			}
		}
    }
}
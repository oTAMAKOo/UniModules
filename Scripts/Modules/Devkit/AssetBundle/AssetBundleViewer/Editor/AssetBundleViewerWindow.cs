
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.AssetBundles;
using Modules.Devkit.Project;
using Modules.ExternalAssets;

namespace Modules.Devkit.AssetBundleViewer
{
	public enum DisplayType
	{
		List,
		Detail,
	}

	public sealed class AssetBundleViewerWindow : SingletonEditorWindow<AssetBundleViewerWindow>
	{
		//----- params -----

		private readonly Vector2 WindowSize = new Vector2(700f, 550f);

		//----- field -----
		
		private InfoTreeView infoTreeView = null;

        private DetailView detailView = null;

		private DisplayType displayType = DisplayType.List;

		private Dictionary<string, AssetBundleInfo> assetBundleInfos = null;

		private AssetInfoManifest assetInfoManifest = null;

		private AssetBundleDependencies assetBundleDependencies = null;

		[NonSerialized]
		private bool initialized = false;

		//----- property -----

		//----- method -----

		public static void Open()
		{
			Instance.Initialize();
		}

		private void Initialize()
		{
			if (initialized) { return; }

			titleContent = new GUIContent("AssetBundleViewer");

			minSize = WindowSize;

			var loadResult = LoadAssetInfoManifest();

			if (!loadResult) { return; }

			InitializeViews();

			ShowUtility();

			initialized = true;
		}

		private void InitializeViews()
		{
			//------ InfoTreeView ------

            infoTreeView = new InfoTreeView();
			
			infoTreeView.Initialize();

			var allContents = assetBundleInfos.Values.ToArray();

			infoTreeView.SetContents(allContents);

			infoTreeView.OnContentClickAsObservable()
				.Subscribe(x => SetDetailViewContent(x))
				.AddTo(Disposable);

			//------ DetailView ------

            detailView = new DetailView();

			detailView.Initialize(assetBundleInfos, assetBundleDependencies);

			detailView.OnRequestCloseAsObservable()
                .Subscribe(_ => displayType = DisplayType.List)
                .AddTo(Disposable);
		}

        private bool LoadAssetInfoManifest()
		{
            var projectResourceFolders = ProjectResourceFolders.Instance;

			assetInfoManifest = UnityEditorUtility.FindAssetsByType<AssetInfoManifest>("t:AssetInfoManifest").FirstOrDefault();

			if (assetInfoManifest == null)
			{
				Debug.LogError("AssetInfoManifest not found.");
                
                return false;
			}

			assetInfoManifest.BuildCache(true);

            assetBundleDependencies = new AssetBundleDependencies();

            var contents = new List<AssetBundleInfo>();

            var assetInfosByAssetBundleName = assetInfoManifest.GetAssetInfos()
                .Where(x => x.IsAssetBundle)
                .GroupBy(x => x.AssetBundle.AssetBundleName)
                .ToArray();

			// AssetBundleInfo構築.
            {
				var progressTitle = "Build AssetBundle Info";

                var externalAssetPath = projectResourceFolders.ExternalAssetPath;

                var id = 0;
			    var count = assetInfosByAssetBundleName.Length;

                for (var i = 0; i < count; i++)
                {
                    var items = assetInfosByAssetBundleName[i];
				    
				    if (items.IsEmpty()){ continue; }

				    EditorUtility.DisplayProgressBar(progressTitle, items.Key, (float)i / count);

				    var info = items.FirstOrDefault();

				    var assetGuids = items
                        .Select(x => PathUtility.Combine(externalAssetPath, x.ResourcePath))
                        .Select(x => AssetDatabase.AssetPathToGUID(x))
                        .ToArray();

				    var dependencies = info.AssetBundle.Dependencies;

                    var content = new AssetBundleInfo(id)
				    {
					    AssetBundleName = items.Key,
					    Group = info.Group,
					    Labels = info.Labels,
					    FileSize = info.Size,
					    FileName = info.FileName,
					    Dependencies = dependencies,
					    AssetGuids = assetGuids,
				    };

                    contents.Add(content);

				    assetBundleDependencies.SetDependencies(items.Key, dependencies);

				    id++;
			    }

                EditorUtility.ClearProgressBar();
            }

			// Dependencies情報構築.
            {
                var progressTitle = "Build Dependencies Info";

				var count = contents.Count;

                for (var i = 0; i < count; i++)
                {
                    var content = contents[i];
                
                    EditorUtility.DisplayProgressBar(progressTitle, content.AssetBundleName, (float)i / count);

                    var loadFileSize = 0L;

                    var allDependencies = assetBundleDependencies
                        .GetAllDependencies(content.AssetBundleName)
                        .Distinct();

                    foreach (var element in allDependencies)
                    {
                        var group = assetInfosByAssetBundleName.FirstOrDefault(x => x.Key == element);

                        if (group == null){ continue; }

                        var dependent = group.FirstOrDefault();

                        loadFileSize += dependent.Size;
                    }

                    loadFileSize += content.FileSize;

                    content.LoadFileSize = loadFileSize;
                }

                EditorUtility.ClearProgressBar();
            }

            assetBundleInfos = contents
				.OrderBy(x => x.AssetBundleName, new NaturalComparer())
				.ToDictionary(x => x.AssetBundleName);

			return true;
		}

        void OnGUI()
        {
			if (!initialized)
            {
				Close();
				return;
            }

            switch (displayType)
            {
                case DisplayType.List:
                    infoTreeView.DrawGUI();
                    break;

                case DisplayType.Detail:
                    detailView.DrawGUI();
                    break;
            }
        }

		private void SetDetailViewContent(AssetBundleInfo target)
		{
            displayType = DisplayType.Detail;

			detailView.SetContent(target);
        }
    }
}
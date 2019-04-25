﻿﻿
using System;
using UnityEngine;
using UnityEditor;
using UniRx;
using Extensions;
using Modules.ExternalResource.Editor;
using Modules.Devkit.Project;

namespace Modules.Devkit.AssetTuning
{
	public class AssetBundleAssetTuner : AssetTuner
    {
        private IDisposable rebuildDisposable = null;
        private AssetManageManager assetManageManager = null;

        private AssetManageManager AssetManageManager
        {
            get
            {
                if (assetManageManager == null)
                {
                    BuildAssetManageManager();
                }

                return assetManageManager;
            }
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            AssetTuneManager.Instance.Register<AssetBundleAssetTuner>();
        }

        public override bool Validate(string path)
        {
            return AssetManageManager != null;
        }
        
        public override void OnAssetImport(string path)
        {
            if (path.StartsWith(AssetManageManager.ExternalResourcesPath))
            {
                var infos = AssetManageManager.CollectInfo(path);

                foreach (var info in infos)
                {
                    info.ApplyAssetBundleName();
                }
            }
        }

        public override void OnAssetMove(string path, string from)
        {
            // 対象から外れる場合.

            if (from.StartsWith(AssetManageManager.ExternalResourcesPath))
            {
                if (!path.StartsWith(AssetManageManager.ExternalResourcesPath))
                {
                    AssetManageManager.SetAssetBundleName(path, string.Empty);
                }
            }

            // 対象に追加される場合.

            if (path.StartsWith(AssetManageManager.ExternalResourcesPath))
            {
                var infos = AssetManageManager.CollectInfo(path);

                foreach (var info in infos)
                {
                    info.ApplyAssetBundleName();
                }
            }
        }

        private void BuildAssetManageManager()
        {
            var projectFolders = ProjectFolders.Instance;
            var assetManageConfig = AssetManageConfig.Instance;

            if (projectFolders == null) { return; }

            if (assetManageConfig == null) { return; }

            var externalResourcesPath = projectFolders.ExternalResourcesPath;

            assetManageManager = new AssetManageManager();
            assetManageManager.Initialize(externalResourcesPath, assetManageConfig);

            if(rebuildDisposable != null)
            {
                rebuildDisposable.Dispose();
                rebuildDisposable = null;
            }

            rebuildDisposable = AssetManageConfig.OnReloadAsObservable()
                .Subscribe(_ => BuildAssetManageManager())
                .AddTo(Disposable);
        }
    }
}

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
        private AssetManageManager assetManageManager = null;

        public override int Priority { get { return 75; } }

        public override void OnBegin()
        {
            assetManageManager = null;

            var projectFolders = ProjectFolders.Instance;
            var assetManageConfig = AssetManageConfig.Instance;

            if (projectFolders != null && assetManageConfig != null)
            {
                assetManageManager = AssetManageManager.Instance;

                var externalResourcesPath = projectFolders.ExternalResourcesPath;

                assetManageManager.Initialize(externalResourcesPath, assetManageConfig);
            }
        }

        public override bool Validate(string path)
        {
            return assetManageManager != null;
        }
        
        public override void OnAssetImport(string path)
        {
            if (path.StartsWith(assetManageManager.ExternalResourcesPath))
            {
                var infos = assetManageManager.CollectInfo(path);

                foreach (var info in infos)
                {
                    info.ApplyAssetBundleName();
                }
            }
        }

        public override void OnAssetMove(string path, string from)
        {
            // 対象から外れる場合.

            if (from.StartsWith(assetManageManager.ExternalResourcesPath))
            {
                if (!path.StartsWith(assetManageManager.ExternalResourcesPath))
                {
                    assetManageManager.SetAssetBundleName(path, string.Empty);
                }
            }

            // 対象に追加される場合.

            if (path.StartsWith(assetManageManager.ExternalResourcesPath))
            {
                var infos = assetManageManager.CollectInfo(path);

                foreach (var info in infos)
                {
                    info.ApplyAssetBundleName();
                }
            }
        }
    }
}

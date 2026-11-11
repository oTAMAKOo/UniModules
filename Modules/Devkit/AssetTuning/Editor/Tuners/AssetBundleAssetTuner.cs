
using System;
using System.IO;
using Modules.ExternalAssets;
using Modules.Devkit.Project;
using UnityEngine;

namespace Modules.Devkit.AssetTuning
{
	public sealed class AssetBundleAssetTuner : AssetTuner
    {
        private AssetManagement assetManagement = null;

        public override int Priority { get { return 75; } }

		public override void OnBeforePostprocessAsset()
		{
            assetManagement = null;

            var projectResourceFolders = ProjectResourceFolders.Instance;

            if (projectResourceFolders != null)
            {
                assetManagement = AssetManagement.Instance;
                assetManagement.Initialize();
            }
        }

        public override bool Validate(string assetPath)
        {
            return assetManagement != null;
        }

		public override async void OnPostprocessAsset(string assetPath)
		{
			try
			{
				var externalAssetPath = GetExternalAssetPath();
				var shareResourcesPath = GetShareResourcesPath();

				var targetPaths = new string[]
				{
					externalAssetPath,
					shareResourcesPath,
				};

				foreach (var targetPath in targetPaths)
				{
					if (string.IsNullOrEmpty(targetPath)) { continue; }

					if (!assetPath.StartsWith(targetPath)) { continue; }

					var infos = await assetManagement.GetAssetInfos(assetPath);

					foreach (var info in infos)
					{
						ApplyAssetBundleName(externalAssetPath, shareResourcesPath, info);
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

        public override async void OnAssetMove(string assetPath, string from)
        {
            var externalAssetPath = GetExternalAssetPath();
            var shareResourcesPath = GetShareResourcesPath();

            var targetPaths = new string[]
            {
                externalAssetPath,
                shareResourcesPath,
            };

            foreach (var targetPath in targetPaths)
            {
                if (string.IsNullOrEmpty(targetPath)) { continue; }

                // 対象から外れる場合.

                if (from.StartsWith(targetPath))
                {
                    if (!assetPath.StartsWith(targetPath))
                    {
                        assetManagement.SetAssetBundleName(assetPath, string.Empty);
                    }
                }

                // 対象に追加される場合.

                if (assetPath.StartsWith(targetPath))
                {
                    var infos = await assetManagement.GetAssetInfos(assetPath);

                    foreach (var info in infos)
                    {
                        ApplyAssetBundleName(externalAssetPath, shareResourcesPath, info);
                    }
                }
            }
        }

        private void ApplyAssetBundleName(string externalAssetPath, string shareResourcesPath, AssetInfo assetInfo)
        {
			if (string.IsNullOrEmpty(externalAssetPath) || string.IsNullOrEmpty(shareResourcesPath)){ return; }

            if (assetInfo == null){ return; }

            if (!assetInfo.IsAssetBundle){ return; }

            if (assetInfo.AssetBundle == null){ return; }
            
            var assetPath = ExternalAsset.GetAssetPathFromAssetInfo(externalAssetPath, shareResourcesPath, assetInfo);

            assetManagement.SetAssetBundleName(assetPath, assetInfo.AssetBundle.AssetBundleName);
        }

        private string GetExternalAssetPath()
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

			if (projectResourceFolders == null || string.IsNullOrEmpty(projectResourceFolders.ExternalAssetPath))
			{
				throw new InvalidDataException("Missing data ExternalAssetPath");
			}

            return projectResourceFolders.ExternalAssetPath;
        }

        private string GetShareResourcesPath()
        {
			var projectResourceFolders = ProjectResourceFolders.Instance;

			if (projectResourceFolders == null || string.IsNullOrEmpty(projectResourceFolders.ShareResourcesPath))
			{
				throw new FileNotFoundException("Missing data ShareResourcesPath");
			}

            return projectResourceFolders.ShareResourcesPath;
        }
    }
}

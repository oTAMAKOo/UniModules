
using Modules.ExternalAssets;
using Modules.Devkit.Project;

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

		public override void OnPostprocessAsset(string assetPath)
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

                var infos = assetManagement.GetAssetInfos(assetPath);

                foreach (var info in infos)
                {
                    ApplyAssetBundleName(externalAssetPath, shareResourcesPath, info);
                }
            }
        }

        public override void OnAssetMove(string assetPath, string from)
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
                    var infos = assetManagement.GetAssetInfos(assetPath);

                    foreach (var info in infos)
                    {
                        ApplyAssetBundleName(externalAssetPath, shareResourcesPath, info);
                    }
                }
            }
        }

        private void ApplyAssetBundleName(string externalAssetPath, string shareResourcesPath, AssetInfo assetInfo)
        {
            if (assetInfo == null){ return; }

            if (!assetInfo.IsAssetBundle){ return; }

            if (assetInfo.AssetBundle == null){ return; }
            
            var assetPath = ExternalAsset.GetAssetPathFromAssetInfo(externalAssetPath, shareResourcesPath, assetInfo);

            assetManagement.SetAssetBundleName(assetPath, assetInfo.AssetBundle.AssetBundleName);
        }

        private string GetExternalAssetPath()
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            return projectResourceFolders != null ? projectResourceFolders.ExternalAssetPath : null;
        }

        private string GetShareResourcesPath()
        {
			var projectResourceFolders = ProjectResourceFolders.Instance;

            return projectResourceFolders != null ? projectResourceFolders.ShareResourcesPath : null;
        }
    }
}

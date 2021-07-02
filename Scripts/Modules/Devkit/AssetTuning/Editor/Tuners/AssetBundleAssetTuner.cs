
using Extensions;
using Modules.ExternalResource.Editor;
using Modules.Devkit.Project;
using Modules.ExternalResource;

namespace Modules.Devkit.AssetTuning
{
	public sealed class AssetBundleAssetTuner : AssetTuner
    {
        private AssetManagement assetManagement = null;

        public override int Priority { get { return 75; } }

        public override void OnBegin()
        {
            assetManagement = null;

            var projectFolders = ProjectFolders.Instance;

            if (projectFolders != null)
            {
                assetManagement = AssetManagement.Instance;
                assetManagement.Initialize();
            }
        }

        public override bool Validate(string assetPath)
        {
            return assetManagement != null;
        }

        public override void OnAssetImport(string assetPath)
        {
            var externalResourcesPath = GetExternalResourcesPath();
            var shareResourcesPath = GetShareResourcesPath();

            var targetPaths = new string[]
            {
                externalResourcesPath,
                shareResourcesPath,
            };

            foreach (var targetPath in targetPaths)
            {
                if (string.IsNullOrEmpty(targetPath)) { continue; }

                if (!assetPath.StartsWith(targetPath)) { continue; }

                var infos = assetManagement.GetAssetInfos(assetPath);

                foreach (var info in infos)
                {
                    ApplyAssetBundleName(externalResourcesPath, shareResourcesPath, info);
                }
            }
        }

        public override void OnAssetMove(string assetPath, string from)
        {
            var externalResourcesPath = GetExternalResourcesPath();
            var shareResourcesPath = GetShareResourcesPath();

            var targetPaths = new string[]
            {
                externalResourcesPath,
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
                        ApplyAssetBundleName(externalResourcesPath, shareResourcesPath, info);
                    }
                }
            }
        }

        private void ApplyAssetBundleName(string externalResourcesPath, string shareResourcesPath, AssetInfo assetInfo)
        {
            if (assetInfo == null){ return; }

            if (!assetInfo.IsAssetBundle){ return; }

            if (assetInfo.AssetBundle == null){ return; }
            
            var assetPath = ExternalResources.GetAssetPathFromAssetInfo(externalResourcesPath, shareResourcesPath, assetInfo);

            assetManagement.SetAssetBundleName(assetPath, assetInfo.AssetBundle.AssetBundleName);
        }

        private string GetExternalResourcesPath()
        {
            var projectFolders = ProjectFolders.Instance;

            return projectFolders != null ? projectFolders.ExternalResourcesPath : null;
        }

        private string GetShareResourcesPath()
        {
            var projectFolders = ProjectFolders.Instance;

            return projectFolders != null ? projectFolders.ShareResourcesPath : null;
        }
    }
}


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

            if (string.IsNullOrEmpty(externalResourcesPath)){ return; }

            if (assetPath.StartsWith(externalResourcesPath))
            {
                var infos = assetManagement.GetAssetInfos(assetPath);

                foreach (var info in infos)
                {
                    ApplyAssetBundleName(externalResourcesPath, info);
                }
            }
        }

        public override void OnAssetMove(string assetPath, string from)
        {
            var externalResourcesPath = GetExternalResourcesPath();

            if (string.IsNullOrEmpty(externalResourcesPath)) { return; }

            // 対象から外れる場合.

            if (from.StartsWith(externalResourcesPath))
            {
                if (!assetPath.StartsWith(externalResourcesPath))
                {
                    assetManagement.SetAssetBundleName(assetPath, string.Empty);
                }
            }

            // 対象に追加される場合.

            if (assetPath.StartsWith(externalResourcesPath))
            {
                var infos = assetManagement.GetAssetInfos(assetPath);

                foreach (var info in infos)
                {
                    ApplyAssetBundleName(externalResourcesPath, info);
                }
            }
        }

        private void ApplyAssetBundleName(string externalResourcesPath, AssetInfo assetInfo)
        {
            if (assetInfo == null){ return; }

            if (!assetInfo.IsAssetBundle){ return; }

            if (assetInfo.AssetBundle == null){ return; }

            var assetPath = PathUtility.Combine(externalResourcesPath, assetInfo.ResourcePath);

            assetManagement.SetAssetBundleName(assetPath, assetInfo.AssetBundle.AssetBundleName);
        }

        private string GetExternalResourcesPath()
        {
            var projectFolders = ProjectFolders.Instance;

            return projectFolders != null ? projectFolders.ExternalResourcesPath : null;
        }
    }
}


using Extensions;
using UnityEditor;
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

                var externalResourcesPath = projectFolders.ExternalResourcesPath;

                assetManagement.Initialize(externalResourcesPath);
            }
        }

        public override bool Validate(string assetPath)
        {
            return assetManagement != null;
        }
        
        public override void OnAssetImport(string assetPath)
        {
            if (assetPath.StartsWith(assetManagement.ExternalResourcesPath))
            {
                var infos = assetManagement.GetAssetInfos(assetPath);

                foreach (var info in infos)
                {
                    ApplyAssetBundleName(assetManagement.ExternalResourcesPath, info);
                }
            }
        }

        public override void OnAssetMove(string assetPath, string from)
        {
            // 対象から外れる場合.

            if (from.StartsWith(assetManagement.ExternalResourcesPath))
            {
                if (!assetPath.StartsWith(assetManagement.ExternalResourcesPath))
                {
                    assetManagement.SetAssetBundleName(assetPath, string.Empty);
                }
            }

            // 対象に追加される場合.

            if (assetPath.StartsWith(assetManagement.ExternalResourcesPath))
            {
                var infos = assetManagement.GetAssetInfos(assetPath);

                foreach (var info in infos)
                {
                    ApplyAssetBundleName(assetManagement.ExternalResourcesPath, info);
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
    }
}

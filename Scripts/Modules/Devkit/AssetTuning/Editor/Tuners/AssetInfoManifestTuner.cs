
using UnityEditor;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Project;
using Modules.ExternalResource;

namespace Modules.Devkit.AssetTuning
{
    public sealed class AssetInfoManifestTuner : AssetTuner
    {
        //----- params -----

        //----- field -----

        private AssetManagement assetManagement = null;
        private AssetInfoManifest assetInfoManifest = null;

        private string externalResourcesPath = null;
        private AssetInfo[] assetInfos = null;
        private bool changeAssetInfo = false;

        //----- property -----

        public override int Priority { get { return 70; } }

        //----- method -----

        public override bool Validate(string path)
        {
            return assetManagement != null && assetInfoManifest != null;
        }

		public override void OnBeforePostprocessAsset()
		{
            assetManagement = null;
            assetInfoManifest = null;

            changeAssetInfo = false;

            var projectResourceFolders = ProjectResourceFolders.Instance;
            
            if (projectResourceFolders == null){ return; }
            
            assetManagement = AssetManagement.Instance;

            externalResourcesPath = projectResourceFolders.ExternalResourcesPath;

            assetManagement.Initialize();

            var manifestPath = PathUtility.Combine(externalResourcesPath, AssetInfoManifest.ManifestFileName);

            assetInfoManifest = AssetDatabase.LoadAssetAtPath<AssetInfoManifest>(manifestPath);

            if (assetInfoManifest != null)
            {
                assetInfos = Reflection.GetPrivateField<AssetInfoManifest, AssetInfo[]>(assetInfoManifest, "assetInfos");
            }
        }

		public override void OnAfterPostprocessAsset()
		{
            if (changeAssetInfo && assetInfoManifest != null)
            {
                Reflection.SetPrivateField(assetInfoManifest, "assetInfos", assetInfos);
                UnityEditorUtility.SaveAsset(assetInfoManifest);
            }
        }

        public override void OnAssetCreate(string path)
        {
            if (!IsExternalResources(path)) { return; }

            if (ContainsAssetInfo(path)) { return; }

            AddAssetInfo(path);
        }

        public override void OnAssetDelete(string path)
        {
            if (!IsExternalResources(path)) { return; }

            if (!ContainsAssetInfo(path)) { return; }

            DeleteAssetInfo(path);
        }

        public override void OnAssetMove(string path, string from)
        {
            if (IsExternalResources(from) && ContainsAssetInfo(from))
            {
                DeleteAssetInfo(from);
            }

            if (IsExternalResources(path) && !ContainsAssetInfo(path))
            {
                AddAssetInfo(path);
            }
        }

        private string ConvertAssetPathToResourcePath(string assetPath)
        {
            return assetPath.Replace(externalResourcesPath, string.Empty).TrimStart(PathUtility.PathSeparator);
        }

        private bool IsExternalResources(string path)
        {
            return path.StartsWith(externalResourcesPath);
        }

        private bool ContainsAssetInfo(string path)
        {
            var resourcePath = ConvertAssetPathToResourcePath(path);

            return assetInfos.Any(x => x.ResourcePath == resourcePath);
        }

        private void AddAssetInfo(string path)
        {
            var infos = assetManagement.GetAssetInfos(path);

            foreach (var info in infos)
            {
                assetInfos = assetInfos.Append(info).ToArray();

                changeAssetInfo = true;
            }
        }

        private void DeleteAssetInfo(string path)
        {
            var resourcePath = ConvertAssetPathToResourcePath(path);

            assetInfos = assetInfos.Where(x => x.ResourcePath != resourcePath).ToArray();

            changeAssetInfo = true;
        }
    }
}

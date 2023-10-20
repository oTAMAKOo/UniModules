
using UnityEditor;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Project;
using Modules.ExternalAssets;

namespace Modules.Devkit.AssetTuning
{
    public sealed class AssetInfoManifestTuner : AssetTuner
    {
        //----- params -----

        //----- field -----

        private AssetManagement assetManagement = null;
        private AssetInfoManifest assetInfoManifest = null;

        private string externalAssetPath = null;
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

            externalAssetPath = projectResourceFolders.ExternalAssetPath;

            assetManagement.Initialize();

            var manifestPath = PathUtility.Combine(externalAssetPath, AssetInfoManifest.ManifestFileName);

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
            if (!IsExternalAsset(path)) { return; }

            if (ContainsAssetInfo(path)) { return; }

            AddAssetInfo(path).Forget();
        }

        public override void OnAssetDelete(string path)
        {
            if (!IsExternalAsset(path)) { return; }

            if (!ContainsAssetInfo(path)) { return; }

            DeleteAssetInfo(path);
        }

        public override void OnAssetMove(string path, string from)
        {
            if (IsExternalAsset(from) && ContainsAssetInfo(from))
            {
                DeleteAssetInfo(from);
            }

            if (IsExternalAsset(path) && !ContainsAssetInfo(path))
            {
                AddAssetInfo(path).Forget();
            }
        }

        private string ConvertAssetPathToResourcePath(string assetPath)
        {
            return assetPath.Replace(externalAssetPath, string.Empty).TrimStart(PathUtility.PathSeparator);
        }

        private bool IsExternalAsset(string path)
        {
            return path.StartsWith(externalAssetPath);
        }

        private bool ContainsAssetInfo(string path)
        {
            var resourcePath = ConvertAssetPathToResourcePath(path);

            return assetInfos.Any(x => x.ResourcePath == resourcePath);
        }

        private async UniTask AddAssetInfo(string path)
        {
            var infos = await assetManagement.GetAssetInfos(path);

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

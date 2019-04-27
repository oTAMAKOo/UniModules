
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Project;
using Modules.ExternalResource.Editor;
using Modules.ExternalResource;

namespace Modules.Devkit.AssetTuning
{
    public class AssetInfoManifestTuner : AssetTuner
    {
        //----- params -----

        //----- field -----

        private AssetManageManager assetManageManager = null;
        private AssetInfoManifest assetInfoManifest = null;

        private string externalResourcesPath = null;
        private AssetInfo[] assetInfos = null;
        private bool changeAssetInfo = false;

        //----- property -----

        public override int Priority { get { return 70; } }

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            AssetTuneManager.Instance.Register<AssetInfoManifestTuner>();
        }

        public override bool Validate(string path)
        {
            return assetManageManager != null && assetInfoManifest != null;
        }

        public override void OnBegin()
        {
            assetManageManager = null;
            assetInfoManifest = null;

            changeAssetInfo = false;

            var projectFolders = ProjectFolders.Instance;
            var assetManageConfig = AssetManageConfig.Instance;

            if (projectFolders != null && assetManageConfig != null)
            {
                assetManageManager = AssetManageManager.Instance;

                externalResourcesPath = projectFolders.ExternalResourcesPath;

                assetManageManager.Initialize(externalResourcesPath, assetManageConfig);

                var manifestPath = PathUtility.Combine(externalResourcesPath, AssetInfoManifest.ManifestFileName);

                assetInfoManifest = AssetDatabase.LoadAssetAtPath<AssetInfoManifest>(manifestPath);

                if (assetInfoManifest != null)
                {
                    assetInfos = Reflection.GetPrivateField<AssetInfoManifest, AssetInfo[]>(assetInfoManifest, "assetInfos");
                }
            }        
        }

        public override void OnFinish()
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
            var resourcesPath = ConvertAssetPathToResourcePath(path);

            return assetInfos.Any(x => x.ResourcesPath == resourcesPath);
        }

        private void AddAssetInfo(string path)
        {
            var assetInfo = assetManageManager.GetAssetInfo(path);

            if (assetInfo != null)
            {
                assetInfos = assetInfos.Append(assetInfo).ToArray();

                changeAssetInfo = true;
            }
        }

        private void DeleteAssetInfo(string path)
        {
            var resourcesPath = ConvertAssetPathToResourcePath(path);

            assetInfos = assetInfos.Where(x => x.ResourcesPath != resourcesPath).ToArray();

            changeAssetInfo = true;
        }
    }
}


using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;
using Modules.AssetBundles;

#if ENABLE_CRIWARE

using Modules.CriWare;

#endif

namespace Modules.ExternalResource
{
    public class AssetInfoManifest : ScriptableObject
    {
        //----- params -----

        public const string ManifestFileName = "AssetInfoManifest.asset";

        //----- field -----

        [SerializeField, ReadOnly]
        private AssetInfo[] assetInfos = new AssetInfo[0];

        private ILookup<string, AssetInfo> assetInfoByGroupName = null;
        private Dictionary<string, AssetInfo> assetInfoByResourcesPath = null;

        //----- property -----

        public static string AssetBundleName
        {
            get { return Path.GetFileNameWithoutExtension(ManifestFileName).ToLower(); }
        }

        //----- method -----

        public AssetInfoManifest(AssetInfo[] assetInfos)
        {
            this.assetInfos = assetInfos;
        }

        public IEnumerable<AssetInfo> GetAssetInfos(string groupName = null)
        {
            BuildCache();

            if (string.IsNullOrEmpty(groupName))
            {
                return assetInfos;
            }

            if (!assetInfoByGroupName.Contains(groupName))
            {
                return new AssetInfo[0];
            }

            return assetInfoByGroupName
                .Where(x => x.Key == groupName)
                .SelectMany(x => x);
        }

        public AssetInfo GetAssetInfo(string resourcesPath)
        {
            BuildCache();

            var info = assetInfoByResourcesPath.GetValueOrDefault(resourcesPath);

            return info;
        }

        public void SetAssetFileInfo(string exportPath, UniRx.IProgress<Tuple<string,float>> progress = null)
        {
            for (var i = 0; i < assetInfos.Length; i++)
            {
                var assetInfo = assetInfos[i];

                var elements = new string[0];

                if (string.IsNullOrEmpty(assetInfo.AssetBundleName))
                {
                    #if ENABLE_CRIWARE

                    var extension = Path.GetExtension(assetInfo.ResourcesPath);

                    if (CriAssetDefinition.AssetAllExtensions.Any(x => x == extension))
                    {
                        elements = new string[] { exportPath, CriAssetDefinition.CriAssetFolder, assetInfo.ResourcesPath };
                    }

                    #endif
                }
                else
                {
                    elements = new string[] { exportPath, AssetBundleManager.AssetBundlesFolder, assetInfo.AssetBundleName };
                }

                if (elements.IsEmpty()) { continue; }

                var filePath = PathUtility.Combine(elements);

                assetInfo.SetFileInfo(filePath);

                progress.Report(UniRx.Tuple.Create(assetInfo.ResourcesPath, (float)i / assetInfos.Length));
            }

            BuildCache(true);
        }

        private void BuildCache(bool forceUpdate = false)
        {
            if (assetInfoByGroupName == null || forceUpdate)
            {
                assetInfoByGroupName = assetInfos.ToLookup(x => x.GroupName);
            }

            if (assetInfoByResourcesPath == null || forceUpdate)
            {
                assetInfoByResourcesPath = assetInfos.ToDictionary(x => x.ResourcesPath);
            }
        }
    }

    [Serializable]
    public class AssetInfo
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string resourcesPath = null;
        [SerializeField]
        private string assetBundleName = null;
        [SerializeField]
        private string groupName = null;
        [SerializeField]
        private long fileSize = 0;
        [SerializeField]
        private string fileHash = null;

        //----- property -----

        /// <summary> 読み込みパス </summary>
        public string ResourcesPath { get { return resourcesPath; } }
        /// <summary> アセットバンドル名 </summary>
        public string AssetBundleName { get { return assetBundleName; } }
        /// <summary> グループ名 </summary>
        public string GroupName { get { return groupName; } }
        /// <summary> ファイルサイズ(byte) </summary>
        public long FileSize { get { return fileSize; } }
        /// <summary> ファイルハッシュ </summary>
        public string FileHash { get { return fileHash; } }

        //----- method -----

        public AssetInfo(string resourcesPath, string assetBundleName, string groupName)
        {
            this.resourcesPath = resourcesPath;
            this.assetBundleName = assetBundleName;
            this.groupName = groupName;
        }

        public void SetFileInfo(string filePath)
        {
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);

                fileSize = fileInfo.Length;
                fileHash = FileUtility.GetHash(filePath);
            }
            else
            {
                Debug.LogErrorFormat("File not exists.\nFile : {0}", filePath);
            }
        }
    }
}

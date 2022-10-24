
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Extensions;

namespace Modules.ExternalResource
{
    [Serializable]
    public sealed class AssetInfo
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string resourcePath = null;
        [SerializeField]
        private string group = null;
        [SerializeField]
        private string[] labels = new string[0];
        [SerializeField]
        private string fileName = null;
        [SerializeField]
        private long size = 0;
        [SerializeField]
        private string crc = null;
        [SerializeField]
        private string hash = null;
        [SerializeField]
        private AssetBundleInfo assetBundle = null;

        //----- property -----

        /// <summary> 読み込みパス </summary>
        public string ResourcePath { get { return resourcePath; } }
        /// <summary> グループ </summary>
        public string Group { get { return group; } }
        /// <summary> ラベル </summary>
        public string[] Labels { get { return labels; } }
        /// <summary> ファイル名 </summary>
        public string FileName { get { return fileName; } }
        /// <summary> ファイルサイズ(byte) </summary>
        public long Size { get { return size; } }
        /// <summary> 誤り検出符号(CRC-32) </summary>
        public string CRC { get { return crc; } }
        /// <summary> ハッシュ(SHA256) </summary>
        public string Hash { get { return hash; } }
        /// <summary> アセットバンドル情報 </summary>
        public AssetBundleInfo AssetBundle { get { return assetBundle; } }
        
        /// <summary> アセットバンドルか </summary>
        public bool IsAssetBundle
        {
            get { return assetBundle != null && !string.IsNullOrEmpty(assetBundle.AssetBundleName); }
        }

        //----- method -----

        public AssetInfo(string resourcePath, string group, string[] labels)
        {
            this.resourcePath = PathUtility.ConvertPathSeparator(resourcePath);
            this.group = group;
            this.labels = labels;

            SetFileName();
        }

        public void SetFileInfo(long fileSize, string fileCRC, string fileHash)
        {
            this.size = fileSize;
            this.crc = fileCRC;
            this.hash = fileHash;
        }

        public void SetAssetBundleInfo(AssetBundleInfo assetBundleInfo)
        {
            assetBundle = assetBundleInfo;

            SetFileName();
        }

        private void SetFileName()
        {
            var isManifestFile = false;

            if (!string.IsNullOrEmpty(resourcePath))
            {
                isManifestFile = Path.GetFileName(resourcePath) == AssetInfoManifest.ManifestFileName;
            }

            if (isManifestFile)
            {
                fileName = Path.GetFileNameWithoutExtension(AssetInfoManifest.ManifestFileName);
            }
            else
            {
                if (IsAssetBundle)
                {
                    fileName = assetBundle.AssetBundleName.GetHash();
                }
                else if(!string.IsNullOrEmpty(resourcePath))
                {
                    var extension = Path.GetExtension(resourcePath);

                    fileName = PathUtility.GetPathWithoutExtension(resourcePath).GetHash() + extension;
                }
            }          
        }
	}

    [Serializable]
    public sealed class AssetBundleInfo
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string assetBundleName = null;
        [SerializeField]
        private string[] dependencies = null;
        [SerializeField]
        private uint crc = 0;

        //----- property -----

        /// <summary> アセットバンドル名 </summary>
        public string AssetBundleName { get { return assetBundleName; } }
        /// <summary> 依存関係 </summary>
        public string[] Dependencies { get { return dependencies; } }
        /// <summary> CRC-32 チェックサム </summary>
        public uint CRC { get { return crc; } }

        //----- method -----

        public AssetBundleInfo(string assetBundleName)
        {
            this.assetBundleName = assetBundleName;

            SetDependencies(null);
        }

        public void SetCRC(uint crc)
        {
            this.crc = crc;
        }

        public void SetDependencies(string[] dependencies)
        {
            this.dependencies = dependencies ?? new string[0];
        }
    }

    public sealed class AssetInfoManifest : ScriptableObject
    {
        //----- params -----

        public const string ManifestFileName = "AssetInfoManifest.asset";

        public const string VersionFileExtension = ".version";

        //----- field -----

        [SerializeField, ReadOnly]
        private string versionHash = null;
        [SerializeField, ReadOnly]
        private AssetInfo[] assetInfos = new AssetInfo[0];

        private ILookup<string, AssetInfo> assetInfoByGroup = null;
        private Dictionary<string, AssetInfo> assetInfoByResourcesPath = null;

        //----- property -----

        public string VersionHash
        {
            get { return versionHash; }
        }

        public static string AssetBundleName
        {
            get { return Path.GetFileNameWithoutExtension(ManifestFileName).ToLower(); }
        }

        //----- method -----

        public AssetInfoManifest(AssetInfo[] assetInfos)
        {
            this.assetInfos = assetInfos.DistinctBy(x => x.ResourcePath).ToArray();
        }

        public void AddAssetInfo(AssetInfo assetInfo)
        {
            assetInfos = assetInfos.Append(assetInfo)
                .DistinctBy(x => x.ResourcePath)
                .ToArray();
        }

        public static AssetInfo GetManifestAssetInfo()
        {
            var manifestAssetInfo = new AssetInfo(ManifestFileName, null, null);

            var assetBundleInfo = new AssetBundleInfo(AssetBundleName);

            manifestAssetInfo.SetAssetBundleInfo(assetBundleInfo);

            return manifestAssetInfo;
        }

        public IEnumerable<AssetInfo> GetAssetInfos(string group = null)
        {
            BuildCache();

            if (string.IsNullOrEmpty(group))
            {
                return assetInfos;
            }

            if (!assetInfoByGroup.Contains(group))
            {
                return new AssetInfo[0];
            }

            return assetInfoByGroup
                .Where(x => x.Key == group)
                .SelectMany(x => x);
        }

        public AssetInfo GetAssetInfo(string resourcePath)
        {
            BuildCache();

            var info = assetInfoByResourcesPath.GetValueOrDefault(resourcePath);

            return info;
        }
        
        public void BuildCache(bool forceUpdate = false)
        {
            assetInfos = assetInfos.DistinctBy(x => x.ResourcePath).ToArray();

            if (assetInfoByGroup == null || forceUpdate)
            {
                assetInfoByGroup = assetInfos.ToLookup(x => x.Group);
            }

            if (assetInfoByResourcesPath == null || forceUpdate)
            {
                assetInfoByResourcesPath = assetInfos
                    .Where(x => !string.IsNullOrEmpty(x.ResourcePath))
                    .ToDictionary(x => x.ResourcePath);
            }
        }
    }
}

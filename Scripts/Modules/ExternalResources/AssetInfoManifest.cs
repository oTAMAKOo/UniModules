
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
        private string category = null;
        [SerializeField]
        private string tag = null;
        [SerializeField]
        private string fileName = null;
        [SerializeField]
        private long fileSize = 0;
        [SerializeField]
        private string fileHash = null;
        [SerializeField]
        private AssetBundleInfo assetBundle = null;

        //----- property -----

        /// <summary> 読み込みパス </summary>
        public string ResourcePath { get { return resourcePath; } }
        /// <summary> カテゴリ </summary>
        public string Category { get { return category; } }
        /// <summary> タグ </summary>
        public string Tag { get { return tag; } }
        /// <summary> ファイル名 </summary>
        public string FileName { get { return fileName; } }
        /// <summary> ファイルサイズ(byte) </summary>
        public long FileSize { get { return fileSize; } }
        /// <summary> ファイルハッシュ </summary>
        public string FileHash { get { return fileHash; } }
        /// <summary> アセットバンドル情報 </summary>
        public AssetBundleInfo AssetBundle { get { return assetBundle; } }
        
        /// <summary> アセットバンドルか </summary>
        public bool IsAssetBundle
        {
            get { return assetBundle != null && !string.IsNullOrEmpty(assetBundle.AssetBundleName); }
        }

        //----- method -----

        public AssetInfo(string resourcePath, string category, string tag)
        {
            this.resourcePath = PathUtility.ConvertPathSeparator(resourcePath);
            this.category = category;
            this.tag = tag;

            SetFileName();
        }

        public void SetFileInfo(string filePath)
        {
            if (!File.Exists(filePath)){ return; }

            var fileInfo = new FileInfo(filePath);

            fileSize = fileInfo.Exists ? fileInfo.Length : -1;

            fileHash = FileUtility.GetHash(filePath);
        }

        public void SetAssetBundleInfo(AssetBundleInfo assetBundleInfo)
        {
            assetBundle = assetBundleInfo;

            SetFileName();
        }

        private void SetFileName()
        {
            if (string.IsNullOrEmpty(resourcePath)) { return; }

            var isManifestFile = Path.GetFileName(resourcePath) == AssetInfoManifest.ManifestFileName;

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
                else
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

        //----- field -----

        [SerializeField, ReadOnly]
        private string versionHash = null;
        [SerializeField, ReadOnly]
        private AssetInfo[] assetInfos = new AssetInfo[0];

        private ILookup<string, AssetInfo> assetInfoByCategory = null;
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
            this.assetInfos = assetInfos;
        }

        public static AssetInfo GetManifestAssetInfo()
        {
            var manifestAssetInfo = new AssetInfo(ManifestFileName, null, null);

            var assetBundleInfo = new AssetBundleInfo(AssetBundleName);

            manifestAssetInfo.SetAssetBundleInfo(assetBundleInfo);

            return manifestAssetInfo;
        }

        public IEnumerable<AssetInfo> GetAssetInfos(string category = null)
        {
            BuildCache();

            if (string.IsNullOrEmpty(category))
            {
                return assetInfos;
            }

            if (!assetInfoByCategory.Contains(category))
            {
                return new AssetInfo[0];
            }

            return assetInfoByCategory
                .Where(x => x.Key == category)
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
            if (assetInfoByCategory == null || forceUpdate)
            {
                assetInfoByCategory = assetInfos.ToLookup(x => x.Category);
            }

            if (assetInfoByResourcesPath == null || forceUpdate)
            {
                assetInfoByResourcesPath = assetInfos.ToDictionary(x => x.ResourcePath);
            }
        }
    }
}

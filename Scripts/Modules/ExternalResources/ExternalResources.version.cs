
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UniRx;
using Extensions;
using MessagePack;
using Modules.MessagePack;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare;

#endif

namespace Modules.ExternalResource
{
    public sealed partial class ExternalResources
    {
        //----- params -----

        private const string VersionFileName = "ExternalResources.version";
        private const string AESKey = @"TK4yH6hyD1dz8je24jq0PdF9oYqJ2fCF";

        [MessagePackObject(true)]
        public class Version
        {
            [MessagePackObject(true)]
            public class Info
            {
                public string resourcesPath = string.Empty;
                public string hash = string.Empty;
            }

            public Info[] infos = new Info[0];
        }

        //----- field -----

        private Version version = null;

        private Dictionary<string, Version.Info> versions = null;

        private static AesManaged aesManaged = null;

        //----- property -----

        private AesManaged AesManaged
        {
            get { return aesManaged ?? (aesManaged = AESExtension.CreateAesManaged(AESKey)); }
        }

        //----- method -----
        
        /// <summary>
        /// アセットバンドルのバージョンが最新か確認.
        /// (同梱された別アセットが更新された場合でもtrueを返す)
        /// </summary>
        private bool CheckAssetBundleVersion(AssetInfo assetInfo)
        {
            var filePath = assetBundleManager.BuildFilePath(assetInfo);

            // ※ シュミレート時はpackageファイルをダウンロードしていないので常にファイルが存在しない.

            if (!simulateMode)
            {
                // ファイルがない.
                if (!File.Exists(filePath)) { return false; }
            }

            // バージョン情報が存在しない.
            if (versions.IsEmpty()) { return false; }

            var infos = assetInfosByAssetBundleName.FirstOrDefault(x => x.Key == assetInfo.AssetBundle.AssetBundleName);

            if (infos == null) { return false; }

            foreach (var info in infos)
            {
                var versionInfo = versions.GetValueOrDefault(info.ResourcePath);

                // ローカルにバージョンが存在しない.
                if (versionInfo == null) { return false; }

                // アセットバンドル内のアセットが更新されている.
                if (versionInfo.hash != info.FileHash) { return false; }
            }

            return true;
        }

        /// <summary>
        /// アセットバンドル以外のアセットの更新が必要か確認.
        /// </summary>
        private bool CheckAssetVersion(string resourcesPath, string filePath)
        {
            // ファイルがない.
            if (!File.Exists(filePath)) { return false; }

            // バージョン情報が存在しない.
            if (versions.IsEmpty()) { return false; }

            var assetInfo = GetAssetInfo(resourcesPath);

            // アセット管理情報内に存在しないので最新扱い.
            if (assetInfo == null) { return true; }

            var versionInfo = versions.GetValueOrDefault(resourcesPath);

            // ローカルにバージョンが存在しない.
            if (versionInfo == null) { return false; }

            return versionInfo.hash == assetInfo.FileHash;
        }

        /// <summary>
        /// 更新が必要なアセット情報を取得.
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public AssetInfo[] GetUpdateRequired(string groupName = null)
        {
            var assetInfos = assetInfoManifest.GetAssetInfos(groupName);

            // バージョン情報が存在しないので全更新.
            if (version.infos.IsEmpty()) { return assetInfos.ToArray(); }

            var versionHashTable = version.infos.ToDictionary(x => x.resourcesPath, x => x.hash);

            return assetInfos
                .Where(x => !versionHashTable.ContainsKey(x.ResourcePath) || versionHashTable[x.ResourcePath] != x.FileHash)
                .ToArray();
        }

        private void UpdateVersion(string resourcesPath)
        {
            var versionFilePath = GetVersionFilePath();

            var directory = Path.GetDirectoryName(versionFilePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                // ※ 古いバージョン情報を破棄して最新のバージョン情報を追加.
                
                var assetInfo = GetAssetInfo(resourcesPath);

                if (assetInfo == null)
                {
                    Debug.LogWarningFormat("AssetInfo not found.\n{0}", resourcesPath);
                    return;
                }

                // アセットバンドル.
                if (assetInfo.IsAssetBundle)
                {
                    var allAssetInfos = assetInfoManifest.GetAssetInfos();

                    // 同じアセットバンドル内のバージョンも更新.
                    var assetBundle = allAssetInfos
                        .Where(x => x.IsAssetBundle)
                        .Where(x => x.AssetBundle.AssetBundleName == assetInfo.AssetBundle.AssetBundleName)
                        .GroupBy(x => x.AssetBundle.AssetBundleName)
                        .FirstOrDefault();

                    foreach (var item in assetBundle)
                    {
                        var info = new Version.Info()
                        {
                            resourcesPath = item.ResourcePath,
                            hash = item.FileHash,
                        };

                        versions[item.ResourcePath] = info;
                    }
                }
                // アセットバンドル以外.
                else
                {
                    var info = new Version.Info()
                    {
                        resourcesPath = assetInfo.ResourcePath,
                        hash = assetInfo.FileHash,
                    };

                    versions[assetInfo.ResourcePath] = info;
                }

                version.infos = versions.Select(x => x.Value).ToArray();

                var data = LZ4MessagePackSerializer.Serialize(version, UnityContractResolver.Instance);
                var encrypt = data.Encrypt(aesManaged);

                File.WriteAllBytes(versionFilePath, encrypt);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void LoadVersion()
        {
            var success = true;

            version = new Version();

            var versionFilePath = GetVersionFilePath();

            if (File.Exists(versionFilePath))
            {
                var data = File.ReadAllBytes(versionFilePath);

                // 復号化.
                var decrypt = new byte[0];

                try
                {
                    decrypt = data.Decrypt(aesManaged);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    success = false;
                }

                try
                {
                    version = LZ4MessagePackSerializer.Deserialize<Version>(decrypt, UnityContractResolver.Instance);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    success = false;
                }

                if (!success)
                {
                    version = new Version();
                    File.Delete(versionFilePath);
                }
            }

            versions = version.infos.ToDictionary(x => x.resourcesPath, x => x);
        }

        private static void ClearVersion()
        {
            if (Exists)
            {
                Instance.version = new Version();

                if (Instance.versions != null)
                {
                    Instance.versions.Clear();
                }
            }

            var versionFilePath = GetVersionFilePath();

            if (Directory.Exists(versionFilePath))
            {
                try
                {
                    var cFileInfo = new FileInfo(versionFilePath);

                    // 読み取り専用属性がある場合は、読み取り専用属性を解除.
                    if ((cFileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        cFileInfo.Attributes = FileAttributes.Normal;
                    }

                    File.Delete(versionFilePath);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private static string GetVersionFilePath()
        {
            return PathUtility.Combine(GetInstallDirectory(), VersionFileName);
        }
    }
}

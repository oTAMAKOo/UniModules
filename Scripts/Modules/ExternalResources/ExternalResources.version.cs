
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using MessagePack;
using MessagePack.Resolvers;
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

        [MessagePackObject(true)]
        public sealed class Version
        {
            [MessagePackObject(true)]
            public sealed class Info
            {
                public string resourcePath = string.Empty;
                public string hash = string.Empty;
            }

            public Info[] infos = new Info[0];
        }

        //----- field -----

        private Version version = null;

        private Dictionary<string, Version.Info> versions = null;

        private static AesCryptoKey versionCryptoKey = null;

        //----- property -----

        //----- method -----

        private AesCryptoKey GetVersionCryptoKey()
        {
            if (versionCryptoKey == null)
            {
                versionCryptoKey = new AesCryptoKey("nEPCjiTpNJffJbBWN1YmlTpBQtcAEqwc", "2FGG3clpRJ6TjfuB");
            }

            return versionCryptoKey;
        }

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
        private bool CheckAssetVersion(string resourcePath, string filePath)
        {
            // ファイルがない.
            if (!File.Exists(filePath)) { return false; }

            // バージョン情報が存在しない.
            if (versions.IsEmpty()) { return false; }

            var assetInfo = GetAssetInfo(resourcePath);

            // アセット管理情報内に存在しないので最新扱い.
            if (assetInfo == null) { return true; }

            var versionInfo = versions.GetValueOrDefault(resourcePath);

            // ローカルにバージョンが存在しない.
            if (versionInfo == null) { return false; }

            return versionInfo.hash == assetInfo.FileHash;
        }

        /// <summary>
        /// 更新が必要なアセット情報を取得.
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public IReadOnlyList<AssetInfo> GetRequireUpdateAssetInfos(string groupName = null)
        {
            var assetInfos = assetInfoManifest.GetAssetInfos(groupName);

            // バージョン情報が存在しないので全更新.
            if (version.infos.IsEmpty()) { return assetInfos.ToArray(); }

            return assetInfos.Where(x => IsRequireUpdate(x)).ToArray();
        }

        public bool IsRequireUpdate(AssetInfo assetInfo)
        {
            // バージョン情報が存在しないので更新.
            if (version.infos.IsEmpty()) { return true; }

            var requireUpdate = true;

            #if ENABLE_CRIWARE_FILESYSTEM

            var extension = Path.GetExtension(assetInfo.ResourcePath);

            if (CriAssetDefinition.AssetAllExtensions.Any(x => x == extension))
            {
                var filePath = ConvertCriFilePath(assetInfo.ResourcePath);

                requireUpdate = !CheckAssetVersion(assetInfo.ResourcePath, filePath);
            }
            else

            #endif

            {
                requireUpdate = !CheckAssetBundleVersion(assetInfo);

            }            

            return requireUpdate;
        }

        private void UpdateVersion(string resourcePath)
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
                
                var assetInfo = GetAssetInfo(resourcePath);

                if (assetInfo == null)
                {
                    Debug.LogWarningFormat("AssetInfo not found.\n{0}", resourcePath);
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
                            resourcePath = item.ResourcePath,
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
                        resourcePath = assetInfo.ResourcePath,
                        hash = assetInfo.FileHash,
                    };

                    versions[assetInfo.ResourcePath] = info;
                }

                version.infos = versions.Select(x => x.Value).ToArray();

                var options = StandardResolverAllowPrivate.Options
                    .WithCompression(MessagePackCompression.Lz4BlockArray)
                    .WithResolver(UnityContractResolver.Instance);

                var data = MessagePackSerializer.Serialize(version, options);

                var cryptoKey = GetVersionCryptoKey();

                var encrypt = data.Encrypt(cryptoKey);

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
                    var cryptoKey = GetVersionCryptoKey();

                    decrypt = data.Decrypt(cryptoKey);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    success = false;
                }

                try
                {
                    var options = StandardResolverAllowPrivate.Options
                        .WithCompression(MessagePackCompression.Lz4BlockArray)
                        .WithResolver(UnityContractResolver.Instance);

                    version = MessagePackSerializer.Deserialize<Version>(decrypt, options);
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

            versions = version.infos.ToDictionary(x => x.resourcePath, x => x);
        }

        private void ClearVersion()
        {
            version = new Version();

            if (versions != null)
            {
                versions.Clear();
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

        private string GetVersionFilePath()
        {
            return PathUtility.Combine(InstallDirectory, VersionFileName);
        }
    }
}

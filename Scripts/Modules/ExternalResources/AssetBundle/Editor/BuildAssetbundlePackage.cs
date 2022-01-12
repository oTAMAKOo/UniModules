﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Extensions;
using Modules.ExternalResource;

namespace Modules.AssetBundles.Editor
{
    public sealed class BuildAssetBundlePackage
    {
        //----- params -----

        private const string CryptoFileName = "package_crypto.txt";

        [Serializable]
        private sealed class PackageCrypto
        {
            public string cryptoKey = null;

            public string cryptoIv = null;
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public static bool CheckCryptoFile(string assetBundlePath, string aesKey, string aesIv)
        {
            var changed = true;

            var packageCryptoFilePath = PathUtility.Combine(assetBundlePath, CryptoFileName);

            var packageCrypto = new PackageCrypto
            {
                cryptoKey = aesKey,
                cryptoIv = aesIv,
            };

            var text = string.Empty;

            if (File.Exists(packageCryptoFilePath))
            {
                try
                {
                    text = File.ReadAllText(packageCryptoFilePath);

                    var prev = JsonConvert.DeserializeObject<PackageCrypto>(text);

                    if (prev.cryptoKey == packageCrypto.cryptoKey && prev.cryptoIv == packageCrypto.cryptoIv)
                    {
                        changed = false;
                    }
                }
                catch
                {
                    /* Ignore Exception */
                }
            }

            return changed;
        }

        public static void CreateCryptoFile(string assetBundlePath, string aesKey, string aesIv)
        {
            var packageCryptoFilePath = PathUtility.Combine(assetBundlePath, CryptoFileName);

            var packageCrypto = new PackageCrypto
            {
                cryptoKey = aesKey,
                cryptoIv = aesIv,
            };

            var json = packageCrypto.ToJson(true);

            File.WriteAllText(packageCryptoFilePath, json);
        }

        public static async Task BuildAssetInfoManifestPackage(string exportPath, string assetBundlePath, string aesKey, string aesIv)
        {
            var assetInfo = AssetInfoManifest.GetManifestAssetInfo();

            var cryptoKey = new AesCryptoKey(aesKey, aesIv);

            var task = CreateBuildTask(exportPath, assetBundlePath, assetInfo, true, cryptoKey);

            await task;
        }

        public static async Task BuildAllAssetBundlePackage(string exportPath, string assetBundlePath, AssetInfo[] assetInfos, AssetInfo[] updatedAssetInfos, string aesKey, string aesIv)
        {
            var isBatchMode = Application.isBatchMode;

            var cryptoKey = new AesCryptoKey(aesKey, aesIv);

            var tasks = new List<Task>();

            var count = 0;
            var logBuilder = new StringBuilder();

            foreach (var info in assetInfos)
            {
                var assetInfo = info;

                if (assetInfo == null) { continue; }

                var createPackage = updatedAssetInfos.Contains(assetInfo);

                var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

                var buildTask = CreateBuildTask(exportPath, assetBundlePath, assetInfo, createPackage, cryptoKey);

                var task = Task.Run(async () =>
                {
                    await buildTask;

                    if (isBatchMode)
                    {
                        Debug.LogFormat(assetBundleName);
                    }
                    else
                    {
                        lock (logBuilder)
                        {
                            logBuilder.AppendLine(assetBundleName);

                            count++;

                            if (100 < count)
                            {
                                Debug.Log(logBuilder.ToString());
 
                                logBuilder.Clear();
                                count = 0;
                            }
                        }
                    }
                });

                if (task != null)
                {
                    tasks.Add(task);
                }
            }

            using (new DisableStackTraceScope(LogType.Log))
            {
                await Task.WhenAll(tasks);

                if (!isBatchMode && count != 0)
                {
                    Debug.Log(logBuilder.ToString());
                }
            }
        }

        private static Task CreateBuildTask(string exportPath, string assetBundlePath, AssetInfo assetInfo, bool createPackage, AesCryptoKey cryptoKey)
        {
            if (assetInfo == null) { return null; }

            var task = Task.Run(async () =>
            {
                try
                {
                    // アセットバンドルファイルパス.
                    var assetBundleFilePath = PathUtility.Combine(assetBundlePath, assetInfo.AssetBundle.AssetBundleName);

                    if (!File.Exists(assetBundleFilePath))
                    {
                        throw new FileNotFoundException(assetBundleFilePath);
                    }

                    // 作成するパッケージファイルのパス.
                    var packageFilePath = Path.ChangeExtension(assetBundleFilePath, AssetBundleManager.PackageExtension);

                    // パッケージを作成.
                    if (!File.Exists(packageFilePath) || createPackage)
                    {
                        await CreatePackage(assetBundleFilePath, packageFilePath, cryptoKey);
                    }

                    // 出力先にパッケージファイルをコピー.
                    await ExportPackage(exportPath, assetBundleFilePath, assetInfo);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            });

            return task;
        }

        /// <summary> パッケージファイル化(暗号化). </summary>
        private static async Task CreatePackage(string assetBundleFilePath, string packageFilePath, AesCryptoKey cryptoKey)
        {
            // アセットバンドル読み込み.

            byte[] data = null;

            using (var fileStream = new FileStream(assetBundleFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                data = new byte[fileStream.Length];

                await fileStream.ReadAsync(data, 0, data.Length);
            }

            // 暗号化.

            data = data.Encrypt(cryptoKey);

            // 書き込み.

            using (var fileStream = new FileStream(packageFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                await fileStream.WriteAsync(data, 0, data.Length);
            }
        }

        /// <summary> パッケージファイルの名前を変更し出力先にコピー. </summary>
        private static async Task ExportPackage(string exportPath, string assetBundleFilePath, AssetInfo assetInfo)
        {
            // パッケージファイルパス.
            var packageFilePath = Path.ChangeExtension(assetBundleFilePath, AssetBundleManager.PackageExtension);

            if (!File.Exists(packageFilePath)){ return; }

            // パッケージファイル名.
            var packageFileName = Path.ChangeExtension(assetInfo.FileName, AssetBundleManager.PackageExtension);

            // ファイルの出力先.
            var packageExportPath = PathUtility.Combine(exportPath, packageFileName);

            // ディレクトリ作成.

            var directory = Path.GetDirectoryName(packageExportPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // ファイルコピー.

            using (var sourceStream = File.Open(packageFilePath, FileMode.Open))
            {
                using (var destinationStream = File.Create(packageExportPath))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }
            }
        }
    }
}

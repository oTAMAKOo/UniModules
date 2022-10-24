
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
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

            if (File.Exists(packageCryptoFilePath))
            {
                try
                {
                    var text = File.ReadAllText(packageCryptoFilePath);

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

        public static async UniTask BuildAssetInfoManifestPackage(string exportPath, string assetBundlePath, string aesKey, string aesIv)
        {
            var assetInfo = AssetInfoManifest.GetManifestAssetInfo();

            var cryptoKey = new AesCryptoStreamKey(aesKey, aesIv);

            await ExecuteBuildTask(exportPath, assetBundlePath, assetInfo, true, cryptoKey);
		}

        public static async UniTask BuildAllAssetBundlePackage(string exportPath, string assetBundlePath, AssetInfo[] assetInfos, AssetInfo[] updatedAssetInfos, string aesKey, string aesIv)
        {
            var isBatchMode = Application.isBatchMode;

            var cryptoKey = new AesCryptoStreamKey(aesKey, aesIv);

            using (new DisableStackTraceScope(LogType.Log))
            {
                var logBuilder = new StringBuilder();

                var chunkedAssetInfos = assetInfos.Chunk(25);

                foreach (var infos in chunkedAssetInfos)
                {
                    var tasks = new List<UniTask>();

                    logBuilder.Clear();

                    foreach (var info in infos)
                    {
                        var assetInfo = info;

                        if (assetInfo == null) { continue; }

                        var createPackage = updatedAssetInfos.Contains(assetInfo);

                        var assetBundleName = assetInfo.AssetBundle.AssetBundleName;
						
                        var task = UniTask.RunOnThreadPool(async () =>
                        {
                            await ExecuteBuildTask(exportPath, assetBundlePath, assetInfo, createPackage, cryptoKey);

                            if (isBatchMode)
                            {
                                Debug.LogFormat(assetBundleName);
                            }
                            else
                            {
                                lock (logBuilder)
                                {
                                    logBuilder.AppendLine(assetBundleName);
                                }
                            }
                        });

						tasks.Add(task);
                    }

                    await UniTask.WhenAll(tasks);

                    if (!isBatchMode)
                    {
                        Debug.Log(logBuilder.ToString());
                    }
                }
            }
        }

        private static async UniTask ExecuteBuildTask(string exportPath, string assetBundlePath, AssetInfo assetInfo, bool createPackage, AesCryptoStreamKey cryptoKey)
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
        }

        /// <summary> パッケージファイル化(暗号化). </summary>
        private static async UniTask CreatePackage(string assetBundleFilePath, string packageFilePath, AesCryptoStreamKey cryptoKey)
        {
            // アセットバンドル読み込み.

            byte[] data = null;

            using (var fileStream = new FileStream(assetBundleFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                data = new byte[fileStream.Length];

                await fileStream.ReadAsync(data, 0, data.Length);
            }

            // 暗号化・書き込み.

            using (var fileStream = new FileStream(packageFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                using (var aesStream = new SeekableCryptoStream(fileStream, cryptoKey))
                {
                    aesStream.Write(data, 0, data.Length);
                }
            }
        }

        /// <summary> パッケージファイルの名前を変更し出力先にコピー. </summary>
        private static async UniTask ExportPackage(string exportPath, string assetBundleFilePath, AssetInfo assetInfo)
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

            using (var sourceStream = File.Open(packageFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var destinationStream = File.Create(packageExportPath))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }
            }
        }
    }
}

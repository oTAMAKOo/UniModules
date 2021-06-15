
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Extensions;
using Modules.ExternalResource;

namespace Modules.AssetBundles.Editor
{
    public sealed class BuildAssetBundlePackage
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static async Task BuildAssetInfoManifestPackage(string exportPath, string assetBundlePath, string aesKey, string aesIv)
        {
            var assetInfo = AssetInfoManifest.GetManifestAssetInfo();

            var cryptKey = new AesCryptKey(aesKey, aesIv);

            var task = CreateBuildTask(exportPath, assetBundlePath, assetInfo, cryptKey);

            await task;
        }

        public static async Task BuildAllAssetBundlePackage(string exportPath, string assetBundlePath, AssetInfoManifest assetInfoManifest, string aesKey, string aesIv)
        {
            var assetInfos = assetInfoManifest.GetAssetInfos()
                .Where(x => x.IsAssetBundle)
                .Where(x => !string.IsNullOrEmpty(x.AssetBundle.AssetBundleName))
                .GroupBy(x => x.AssetBundle.AssetBundleName)
                .Select(x => x.FirstOrDefault())
                .ToList();

            var cryptKey = new AesCryptKey(aesKey, aesIv);

            var tasks = new List<Task>();

            foreach (var info in assetInfos)
            {
                var assetInfo = info;

                if (assetInfo == null) { continue; }

                var task = CreateBuildTask(exportPath, assetBundlePath, assetInfo, cryptKey);

                if (task != null)
                {
                    tasks.Add(task);
                }
            }

            await Task.WhenAll(tasks);
        }

        private static Task CreateBuildTask(string exportPath, string assetBundlePath, AssetInfo assetInfo, AesCryptKey cryptKey)
        {
            if (assetInfo == null) { return null; }

            var task = Task.Run(async () =>
            {
                try
                {
                    // アセットバンドルファイルパス.
                    var assetBundleFilePath = PathUtility.Combine(assetBundlePath, assetInfo.AssetBundle.AssetBundleName);

                    // 更新があったパッケージを作成.
                    await CreatePackage(assetBundleFilePath, cryptKey);

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
        private static async Task CreatePackage(string assetBundleFilePath, AesCryptKey cryptKey)
        {
            // 作成するパッケージファイルのパス.
            var packageFilePath = Path.ChangeExtension(assetBundleFilePath, AssetBundleManager.PackageExtension);

            // パッケージファイルが存在する時は内容に変更がない時なのでそのままコピーする.
            if (File.Exists(packageFilePath)){ return; }

            byte[] data = null;

            // アセットバンドル読み込み.

            using (var fileStream = new FileStream(assetBundleFilePath, FileMode.Open, FileAccess.Read))
            {
                data = new byte[fileStream.Length];

                await fileStream.ReadAsync(data, 0, data.Length);
            }

            // 暗号化.

            data = data.Encrypt(cryptKey);

            // 書き込み.

            using (var fileStream = new FileStream(packageFilePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.WriteAsync(data, 0, data.Length);
            }
        }

        /// <summary> パッケージファイルの名前を変更し出力先にコピー. </summary>
        private static async Task ExportPackage(string exportPath, string assetBundleFilePath, AssetInfo assetInfo)
        {
            // パッケージファイルパス.
            var packageFilePath = Path.ChangeExtension(assetBundleFilePath, AssetBundleManager.PackageExtension);

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

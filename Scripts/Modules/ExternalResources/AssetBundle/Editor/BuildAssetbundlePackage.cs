
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

        public static async Task Build(string exportPath, string assetBundlePath, AssetInfoManifest assetInfoManifest, string aesKey, string aesIv)
        {
            var assetInfos = assetInfoManifest.GetAssetInfos()
                .Where(x => x.IsAssetBundle)
                .Where(x => !string.IsNullOrEmpty(x.AssetBundle.AssetBundleName))
                .GroupBy(x => x.AssetBundle.AssetBundleName)
                .Select(x => x.FirstOrDefault())
                .ToList();

            assetInfos.Add(AssetInfoManifest.GetManifestAssetInfo());

            var cryptoKey = new AesCryptoKey(aesKey, aesIv);

            var tasks = new List<Task>();

            foreach (var assetInfo in assetInfos)
            {
                var task = Task.Run(() =>
                {
                    if (assetInfo == null) { return; }

                    // アセットバンドルファイルパス.
                    var assetBundleFilePath = PathUtility.Combine(assetBundlePath, assetInfo.AssetBundle.AssetBundleName);

                    CreatePackage(assetBundlePath, cryptoKey);

                    ExportPackage(exportPath, assetBundlePath, assetInfo);
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }
    
        /// <summary> パッケージファイル化(暗号化). </summary>
        private static void CreatePackage(string assetBundleFilePath, AesCryptoKey cryptoKey)
        {
            try
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

                    fileStream.Read(data, 0, data.Length);
                }

                // 暗号化.

                data = data.Encrypt(cryptoKey);


                // 書き込み.

                using (var fileStream = new FileStream(packageFilePath, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(data, 0, data.Length);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary> パッケージファイルの名前を変更し出力先にコピー. </summary>
        private static void ExportPackage(string exportPath, string assetBundleFilePath, AssetInfo assetInfo)
        {
            try
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
                
                File.Copy(packageFilePath, packageExportPath, true);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}

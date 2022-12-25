
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.ExternalAssets;

namespace Modules.AssetBundles.Editor
{
    public sealed class BuildAssetBundlePackage
    {
        //----- params -----

        //----- field -----

		private IAssetBundleFileHandler assetBundleFileHandler = null;

        //----- property -----

        //----- method -----

		public BuildAssetBundlePackage(IAssetBundleFileHandler assetBundleFileHandler)
		{
			this.assetBundleFileHandler = assetBundleFileHandler;
		}

		public async UniTask BuildAssetInfoManifestPackage(string exportPath, string assetBundlePath)
        {
            var assetInfo = AssetInfoManifest.GetManifestAssetInfo();

            await ExecuteBuildTask(exportPath, assetBundlePath, assetInfo, true);
		}

        public async UniTask BuildAllAssetBundlePackage(string exportPath, string assetBundlePath, AssetInfo[] assetInfos, AssetInfo[] updatedAssetInfos)
        {
            var isBatchMode = Application.isBatchMode;

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
                            await ExecuteBuildTask(exportPath, assetBundlePath, assetInfo, createPackage);

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

        private async UniTask ExecuteBuildTask(string exportPath, string assetBundlePath, AssetInfo assetInfo, bool createPackage)
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
				var packageFilePath = assetBundleFilePath + AssetBundleManager.PackageExtension;

				// パッケージを作成.
				if (!File.Exists(packageFilePath) || createPackage)
				{
					await CreatePackage(assetBundleFilePath, packageFilePath);
				}

				// 出力先にパッケージファイルをコピー.
				await ExportPackage(exportPath, assetBundleFilePath, assetInfo);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
        }

        /// <summary> パッケージファイル化(難読化). </summary>
        private async UniTask CreatePackage(string assetBundleFilePath, string packageFilePath)
        {
            // アセットバンドル読み込み.

            byte[] data = null;

            using (var fileStream = new FileStream(assetBundleFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                data = new byte[fileStream.Length];

                await fileStream.ReadAsync(data, 0, data.Length); 
            }

            // 難読化.
			
			data = assetBundleFileHandler.Encode(data);

			// 書き込み.

            using (var fileStream = new FileStream(packageFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
				await fileStream.WriteAsync(data, 0, data.Length);
            }
        }

        /// <summary> パッケージファイルの名前を変更し出力先にコピー. </summary>
        private async UniTask ExportPackage(string exportPath, string assetBundleFilePath, AssetInfo assetInfo)
        {
            // パッケージファイルパス.
            var packageFilePath = assetBundleFilePath + AssetBundleManager.PackageExtension;

            if (!File.Exists(packageFilePath)){ return; }

            // パッケージファイル名.
            var packageFileName = assetInfo.FileName + AssetBundleManager.PackageExtension;

            // ファイルの出力先.
            var packageExportPath = PathUtility.Combine(exportPath, packageFileName);

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

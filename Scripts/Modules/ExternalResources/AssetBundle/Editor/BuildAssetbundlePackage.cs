
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Extensions;
using Modules.ExternalResource;

namespace Modules.AssetBundles.Editor
{
    public class BuildAssetbundlePackage
    {
        //----- params -----

        private const string AssetBundleManifestName = "AssetBundle";

        private const int MaxWorkerCount = 20;

        private class Progress
        {
            public int count;
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public void Build(string exportPath, string assetBundlePath, AssetInfoManifest assetInfoManifest, string password, Action<int, int> reportProgress)
        {
            var assetInfos = assetInfoManifest.GetAssetInfos()
                .Where(x => x.IsAssetBundle)
                .Where(x => !string.IsNullOrEmpty(x.AssetBundle.AssetBundleName))
                .GroupBy(x => x.AssetBundle.AssetBundleName)
                .Select(x => x.FirstOrDefault())
                .ToList();

            assetInfos.Add(AssetInfoManifest.GetManifestAssetInfo());

            var total = assetInfos.Count;

            reportProgress(0, total);

            var progress = new Progress();

            var events = new List<WaitHandle>();

            // スレッドで分割処理.

            var workerNo = 0;
            var workerPaths = new List<AssetInfo>[MaxWorkerCount];

            foreach (var assetInfo in assetInfos)
            {
                if(workerPaths[workerNo] == null)
                {
                    workerPaths[workerNo] = new List<AssetInfo>();
                }

                workerPaths[workerNo].Add(assetInfo);

                workerNo = (workerNo + 1) % MaxWorkerCount;
            }

            foreach (var item in workerPaths)
            {
                if (item == null) { continue; }

                events.Add(StartWorker(exportPath, assetBundlePath, password, item.ToArray(), progress));
            }

            while (!WaitHandle.WaitAll(events.ToArray(), 100))
            {
                reportProgress(progress.count, total);
            }

            reportProgress(progress.count, total);
        }
        
        private static ManualResetEvent StartWorker(string exportPath, string assetBundlePath, string password, AssetInfo[] assetInfos, Progress progress)
        {
            var queue = new Queue<AssetInfo>(assetInfos);
            var resetEvent = new ManualResetEvent(false);
            var completedCount = 0;

            for (var i = 0; i < Environment.ProcessorCount; i++)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    while (true)
                    {
                        AssetInfo assetInfo = null;

                        lock (queue)
                        {
                            if (queue.Any()) { assetInfo = queue.Dequeue(); }
                        }

                        if (assetInfo == null) { break; }

                        CreatePackage(exportPath, assetBundlePath, assetInfo, password);

                        Interlocked.Increment(ref progress.count);
                    }

                    // 完了.
                    if (Interlocked.Increment(ref completedCount) == Environment.ProcessorCount)
                    {
                        resetEvent.Set();
                    }
                });
            }

            return resetEvent;
        }

        /// <summary>
        /// パッケージファイル化(暗号化).
        /// </summary>
        private static void CreatePackage(string exportPath, string assetBundlePath, AssetInfo assetInfo, string password)
        {
            var aesManaged = AESExtension.CreateAesManaged(password);

            // ※ パッケージファイルが存在する時は内容に変更がない時なのでそのままコピーする.

            try
            {
                // アセットバンドルファイルパス.
                var assetBundleFilePath = PathUtility.Combine(assetBundlePath, assetInfo.AssetBundle.AssetBundleName);

                // パッケージファイル名.
                var packageFileName = Path.ChangeExtension(assetInfo.FileName, AssetBundleManager.PackageExtension);

                // 作成する圧縮ファイルのパス.
                var packageFilePath = PathUtility.Combine(assetBundlePath, packageFileName);

                // 作成したファイルの出力先.
                var packageExportPath = PathUtility.Combine(exportPath, packageFileName);
                
                byte[] data = null;

                // 読み込み.
                using (var fileStream = new FileStream(assetBundleFilePath, FileMode.Open, FileAccess.Read))
                {
                    data = new byte[fileStream.Length];

                    fileStream.Read(data, 0, data.Length);
                }

                // 暗号化して書き込み.

                if (File.Exists(packageFilePath))
                {
                    File.Delete(packageFilePath);
                }

                using (var fileStream = new FileStream(packageFilePath, FileMode.Create, FileAccess.Write))
                {
                    data = data.Encrypt(aesManaged);

                    fileStream.Write(data, 0, data.Length);
                }

                // ディレクトリ作成.
                var directory = Path.GetDirectoryName(packageExportPath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.Copy(packageFilePath, packageExportPath, true);

                File.Delete(packageFilePath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}

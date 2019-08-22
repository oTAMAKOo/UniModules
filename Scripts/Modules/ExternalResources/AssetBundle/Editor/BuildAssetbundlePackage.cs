
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Extensions;
using System.Threading;

namespace Modules.AssetBundles.Editor
{
    public class BuildAssetbundlePackage
    {
        //----- params -----

        private const string AssetBundleManifestName = "AssetBundle";

        private const string ManifestFileExtension = ".manifest";

        private const int MaxWorkerCount = 20;

        private class Progress
        {
            public int Count;
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public void Build(string exportPath, string assetBundlePath, string password, Action<int, int> reportProgress)
        {
            var filePaths = GetPackageTargets(assetBundlePath);

            var total = filePaths.Length;

            reportProgress(0, total);

            var progress = new Progress();

            var events = new List<WaitHandle>();

            // スレッドで分割処理.

            var workerNo = 0;
            var workerPaths = new List<string>[MaxWorkerCount];

            foreach (var filePath in filePaths)
            {
                if(workerPaths[workerNo] == null)
                {
                    workerPaths[workerNo] = new List<string>();
                }

                workerPaths[workerNo].Add(filePath);

                workerNo = (workerNo + 1) % MaxWorkerCount;
            }

            foreach (var item in workerPaths)
            {
                events.Add(StartWorker(exportPath, assetBundlePath, password, item.ToArray(), progress));
            }

            while (!WaitHandle.WaitAll(events.ToArray(), 100))
            {
                reportProgress(progress.Count, total);
            }

            reportProgress(progress.Count, total);
        }

        /// <summary> パッケージ化するアセットバンドルファイル一覧取得 </summary>
        private static string[] GetPackageTargets(string assetBundlePath)
        {
            var allFiles = Directory.GetFiles(assetBundlePath, "*.*", SearchOption.AllDirectories);

            var platformName = UnityPathUtility.GetPlatformName();

            // パッケージ化するファイルパスを抽出.
            var targets = allFiles
                // パス区切り文字を修正
                .Select(x => PathUtility.ConvertPathSeparator(x))
                // Manifest除外.
                .Where(c => Path.GetFileNameWithoutExtension(c) != platformName)
                // AssetBundleManifest除外.
                .Where(c => Path.GetFileNameWithoutExtension(c) != AssetBundleManifestName)
                // ManifestFile除外.
                .Where(c => !c.EndsWith(ManifestFileExtension))
                // PackageFile除外.
                .Where(c => !c.EndsWith(AssetBundleManager.PackageExtension))
                // 重複削除.
                .Distinct();
            
            return targets.ToArray();
        }

        private static ManualResetEvent StartWorker(string exportPath, string assetBundlePath, string password, string[] paths, Progress progress)
        {
            var queue = new Queue<string>(paths);
            var resetEvent = new ManualResetEvent(false);
            var completedCount = 0;

            for (var i = 0; i < Environment.ProcessorCount; i++)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    while (true)
                    {
                        string path = null;

                        lock (queue)
                        {
                            if (queue.Any()) { path = queue.Dequeue(); }
                        }

                        if (path == null) { break; }

                        CreatePackage(exportPath, assetBundlePath, path, password);

                        Interlocked.Increment(ref progress.Count);
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
        private static void CreatePackage(string exportPath, string assetBundlePath, string filePath, string password)
        {
            var aesManaged = AESExtension.CreateAesManaged(password);

            // ※ パッケージファイルが存在する時は内容に変更がない時なのでそのままコピーする.

            try
            {
                // 作成する圧縮ファイルのパス.
                var packageFilePath = Path.ChangeExtension(filePath, AssetBundleManager.PackageExtension);

                // 作成したファイルの出力先.
                var packageExportPath = PathUtility.Combine(
                    new string[]
                    {
                            exportPath,
                            AssetBundleManager.AssetBundlesFolder,
                            packageFilePath.Replace(assetBundlePath, string.Empty)
                    });

                byte[] data = null;

                if (!File.Exists(packageFilePath))
                {
                    // 読み込み.
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        data = new byte[fileStream.Length];

                        fileStream.Read(data, 0, data.Length);
                    }

                    // 暗号化して書き込み.
                    using (var fileStream = new FileStream(packageFilePath, FileMode.Create, FileAccess.Write))
                    {
                        data = data.Encrypt(aesManaged);

                        fileStream.Write(data, 0, data.Length);
                    }
                }

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

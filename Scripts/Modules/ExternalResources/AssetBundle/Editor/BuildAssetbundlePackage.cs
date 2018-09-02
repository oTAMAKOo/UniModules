
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Extensions;
using System.Threading;

namespace Modules.AssetBundles.Editor
{
    public class BuildAssetbundlePackage
    {
        //----- params -----

        private const int ChunkSize = 5;

        private class Progress
        {
            public int Count;
        }

        //----- field -----

        //----- property -----

        //----- method -----

        static StringBuilder b = null;

        public void Build(string[] filePaths, Action<int, int> reportProgress)
        {
            b = new StringBuilder();

            var total = filePaths.Length;

            reportProgress(0, total);

            var progress = new Progress();

            var events = new List<WaitHandle>();

            // ChunkSize数分のスレッドで分割処理.
            var list = filePaths
                .Select((number, index) => new { Index = index, Number = number })
                .GroupBy(x => x.Index / ChunkSize)
                .Select(gr => gr.Select(x => x.Number));

            foreach (var item in list)
            {
                events.Add(StartWorker(item.ToArray(), progress));
            }

            while (!WaitHandle.WaitAll(events.ToArray(), 100))
            {
                reportProgress(progress.Count, total);
            }

            reportProgress(progress.Count, total);

            Debug.Log(b.ToString());
        }

        private static ManualResetEvent StartWorker(string[] paths, Progress progress)
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

                        CreatePackage(path);

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
        /// 暗号化後に圧縮.
        /// </summary>
        private static void CreatePackage(string filePath)
        {
            lock (b)
            {
                b.AppendLine(filePath);
            }

            var rijndael = AESExtension.CreateRijndael(AssetBundleManager.RijndaelKey);

            try
            {
                // 作成する圧縮ファイルのパス
                var compressedPackage = filePath + AssetBundleManager.PackageExtension;

                // 圧縮するデータをすべて読み取る.
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var data = new byte[fileStream.Length];

                    fileStream.Read(data, 0, data.Length);

                    // 暗号化して書き込み.
                    File.WriteAllBytes(compressedPackage, data.Encrypt(rijndael));                   
                }

                // 元のファイル削除.
                File.Delete(filePath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}

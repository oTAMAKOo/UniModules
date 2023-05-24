
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.Console;

namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary> 全キャッシュ削除. </summary>
        public async UniTask DeleteAllCache()
        {
            if (LocalMode) { return; }

            ReleaseManagedAssets();

            UnloadAllAssetBundles(false);

            ClearVersion();

            if (Directory.Exists(InstallDirectory))
            {
                var cacheFiles = await GetInstallDirectoryFilePaths();

                await DeleteCacheFiles(cacheFiles);
            }
        }

        /// <summary> 不要になったキャッシュ削除. </summary>
        public async UniTask DeleteUnUsedCache()
        {
            if (SimulateMode) { return; }

            if (LocalMode) { return; }

            if (assetInfoManifest == null) { return; }

            if (string.IsNullOrEmpty(InstallDirectory)) { return; }

            if (!Directory.Exists(InstallDirectory)) { return; }

            async UniTask<IEnumerable<string>> UnUsedCacheFilesDelete(HashSet<string> manageFilePaths, IEnumerable<string> targets)
            {
                try
                {
                    await UniTask.SwitchToThreadPool();

                    var hashset = new HashSet<string>();

                    foreach (var target in targets)
                    {
                        // InstallDirectory直下の管理情報に含まれていないファイルは削除対象.
                        if (!manageFilePaths.Contains(target))
                        {
                            hashset.Add(target);
                        }
                    }

                    return hashset;
                }
                finally
                {
                    await UniTask.SwitchToMainThread();
                }
            }

            try
            {
                await UniTask.SwitchToThreadPool();

                if (versions.IsEmpty())
                {
                    await LoadVersion();
                }

                var installDirectoryFilePaths = await GetInstallDirectoryFilePaths();

                var assetInfos = assetInfoManifest.GetAssetInfos().Append(AssetInfoManifest.GetManifestAssetInfo());

                // 管理中のファイル情報構築.

                var manageFilePaths = new HashSet<string>();

                {
                    var chunk = assetInfos.Chunk(250);

                    foreach (var items in chunk)
                    {
                        foreach (var item in items)
                        {
                            var filePath = GetFilePath(item);

                            manageFilePaths.Add(filePath);
                        }

                        await UniTask.NextFrame();
                    }
                }

                // 管理対象に存在しないファイル削除.
                {
                    var tasks = new List<UniTask<IEnumerable<string>>>();

                    var chunk = installDirectoryFilePaths.Chunk(250);

                    foreach (var items in chunk)
                    {
                        var task = UnUsedCacheFilesDelete(manageFilePaths, items);

                        tasks.Add(task);
                    }

                    var results = await UniTask.WhenAll(tasks);

                    var deleteFiles = results.SelectMany(x => x);

                    await DeleteCacheFiles(deleteFiles);
                }
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        /// <summary> 指定されたキャッシュ削除. </summary>
        public async UniTask DeleteCache(AssetInfo[] assetInfos)
        {
            if (LocalMode) { return; }

            var targetFilePaths = new List<string>();

            try
            {
                await UniTask.SwitchToThreadPool();

                if (versions.IsEmpty())
                {
                    await LoadVersion();
                }

                var chunk = assetInfos.Chunk(250);

                foreach (var items in chunk)
                {
                    foreach (var item in items)
                    {
                        var filePath = GetFilePath(item);

                        if (!string.IsNullOrEmpty(filePath))
                        {
                            targetFilePaths.Add(filePath);
                        }
                    }

                    await UniTask.NextFrame();
                }
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            if (targetFilePaths.Any())
            {
                await DeleteCacheFiles(targetFilePaths);
            }
        }

        private async UniTask DeleteCacheFiles(IEnumerable<string> filePaths)
        {
            if (LocalMode) { return; }

            var builder = new StringBuilder();

            // ファイル削除.

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var trimLength = InstallDirectory.Length;

            try
            {
                await UniTask.SwitchToThreadPool();

                var chunk = filePaths.Chunk(25);

                foreach (var items in chunk)
                {
                    foreach (var item in items)
                    {
                        var fileName = Path.GetFileName(item);

                        versions.Remove(fileName);

                        if (!File.Exists(item)) { continue; }

                        File.SetAttributes(item, FileAttributes.Normal);

                        File.Delete(item);

                        var path = item.Substring(trimLength).TrimStart(PathUtility.PathSeparator);

                        builder.AppendLine(path);
                    }

                    await UniTask.NextFrame();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            // バージョン情報更新.

            await SaveVersion();

            // ログ.

            sw.Stop();

            var log = builder.ToString();

            if (!string.IsNullOrEmpty(log))
            {
                LogUtility.ChunkLog(log, $"Delete cache files ({sw.Elapsed.TotalMilliseconds:F1}ms)\nDirectory : {InstallDirectory}", x => UnityConsole.Info(x));
            }
        }

        /// <summary> 管理中のファイルを解放. </summary>
        private void ReleaseManagedAssets()
        {
            #if ENABLE_CRIWARE_ADX

            if (Sound.SoundManagement.Exists)
            {
                Sound.SoundManagement.Instance.ReleaseAll();
            }

            #endif

            #if ENABLE_CRIWARE_SOFDEC

            if (Movie.MovieManagement.Exists)
            {
                Movie.MovieManagement.Instance.ReleaseAll();
            }

            #endif
        }
    }
}
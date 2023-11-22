
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Console;

namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset
    {
        //----- params -----

        public static partial class Prefs
        {
            public static DateTime deleteUnUsedCacheTime
            {
                get { return SecurePrefs.GetDateTime(typeof(Prefs).FullName + "-deleteUnUsedCacheTime", null); }
                set { SecurePrefs.SetDateTime(typeof(Prefs).FullName + "-deleteUnUsedCacheTime", value); }
            }
        }

        //----- field -----

        private Subject<Unit> onReleaseManagedAssets = null;

        //----- property -----

        //----- method -----

        private async UniTask<string[]> GetInstallDirectoryFilePaths(bool configureAwait)
        {
            var files = new string[0];

            if (!Directory.Exists(InstallDirectory)) { return files; }

            var directoryInfo = new DirectoryInfo(InstallDirectory);

            await UniTask.RunOnThreadPool(() =>
            {
                files = directoryInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .AsParallel()
                    .Where(x => x.Name != VersionFileName)
                    .Select(x => PathUtility.ConvertPathSeparator(x.FullName))
                    .ToArray(); 
            }, configureAwait);

            return files;
        }

        /// <summary> 全キャッシュ削除. </summary>
        public async UniTask DeleteAllCache()
        {
            if (LocalMode) { return; }

            if (onReleaseManagedAssets != null)
            {
                onReleaseManagedAssets.OnNext(Unit.Default);
            }

            UnloadAllAssetBundles(false);

            ClearVersion();

            if (Directory.Exists(InstallDirectory))
            {
                var cacheFiles = await GetInstallDirectoryFilePaths(true);

                await DeleteCacheFiles(cacheFiles);
            }
        }

        /// <summary> 不要になったキャッシュ削除. </summary>
        private async UniTask DeleteUnUsedCache()
        {
            if (SimulateMode) { return; }

            if (LocalMode) { return; }

            if (assetInfoManifest == null) { return; }

            if (string.IsNullOrEmpty(InstallDirectory)) { return; }

            if (!Directory.Exists(InstallDirectory)) { return; }

            var now = DateTime.Now;

            if (now < Prefs.deleteUnUsedCacheTime) { return; }

            async UniTask<IEnumerable<string>> UnUsedCacheFilesDelete(HashSet<string> manageFilePaths, IEnumerable<string> targets)
            {
                var hashset = new HashSet<string>();

                // スレッド内で呼び出すのでメインスレッドに戻さない.
                await UniTask.RunOnThreadPool(() =>
                {
                    foreach (var target in targets)
                    {
                        // InstallDirectory直下の管理情報に含まれていないファイルは削除対象.
                        if (!manageFilePaths.Contains(target))
                        {
                            hashset.Add(target);
                        }
                    }
                }, false);

                return hashset;
            }

            if (versions.IsEmpty())
            {
                await LoadVersion();
            }

            var deleteFiles = new string[0];

            await UniTask.RunOnThreadPool(async () =>
            {
                var installDirectoryFilePaths = await GetInstallDirectoryFilePaths(false);

                var assetInfos = assetInfoManifest.GetAssetInfos().Append(AssetInfoManifest.GetManifestAssetInfo());

                // 管理中のファイル情報構築.

                var manageFilePaths = new HashSet<string>();

                foreach (var item in assetInfos)
                {
                    var filePath = GetFilePath(item);

                    manageFilePaths.Add(filePath);
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

                    deleteFiles = results.SelectMany(x => x).ToArray();
                }
            });

            if (deleteFiles.Any())
            {
                await DeleteCacheFiles(deleteFiles);
            }

            Prefs.deleteUnUsedCacheTime = now.AddDays(1);
        }

        /// <summary> 指定されたキャッシュ削除. </summary>
        public async UniTask DeleteCache(AssetInfo[] assetInfos)
        {
            if (LocalMode) { return; }

            var targetFilePaths = new List<string>();

            if (versions.IsEmpty())
            {
                await LoadVersion();
            }

            await UniTask.RunOnThreadPool(() =>
            {
                foreach (var item in assetInfos)
                {
                    var filePath = GetFilePath(item);

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        targetFilePaths.Add(filePath);
                    }
                }
            });

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
                await UniTask.RunOnThreadPool(() =>
                {
                    foreach (var item in filePaths)
                    {
                        var fileName = Path.GetFileName(item);

                        versions.Remove(fileName);

                        if (!File.Exists(item)) { continue; }

                        File.SetAttributes(item, FileAttributes.Normal);

                        File.Delete(item);

                        var path = item.Substring(trimLength).TrimStart(PathUtility.PathSeparator);

                        builder.AppendLine(path);
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
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

        public IObservable<Unit> OnReleaseManagedAssetsAsObservable()
        {
            return onReleaseManagedAssets ?? (onReleaseManagedAssets = new Subject<Unit>());
        }
    }
}
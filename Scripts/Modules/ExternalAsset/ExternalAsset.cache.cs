
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.Console;
using Modules.Performance;

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

            await ClearVersion();

			if (Directory.Exists(InstallDirectory))
			{
	            var cacheFiles = Directory.GetFiles(InstallDirectory, "*", SearchOption.TopDirectoryOnly)
	                .Select(x => PathUtility.ConvertPathSeparator(x))
	                .ToArray();

				if (cacheFiles.Any())
				{
					await DeleteCacheFiles(InstallDirectory, cacheFiles);
				}
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

            var assetInfos = assetInfoManifest.GetAssetInfos()
                .Append(AssetInfoManifest.GetManifestAssetInfo())
				.DistinctBy(x => x.FileName);

            // アセット管理情報構築.

			var manageFilePaths = new HashSet<string>();

			var frameLimiter = new FunctionFrameLimiter(500);

			foreach (var assetInfo in assetInfos)
			{
				await frameLimiter.Wait();

				var filePath = GetFilePath(assetInfo);

				manageFilePaths.Add(filePath);
			}

			// 削除対象抽出.

			var deleteFiles = new List<string>();

			frameLimiter.Reset();

			var files = Directory.EnumerateFiles(InstallDirectory, "*", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
			{
				await frameLimiter.Wait();

				var extension = Path.GetExtension(file);

                // バージョンファイルは削除対象外.
                if (extension == AssetInfoManifest.VersionFileExtension){ continue; }

				// FileMagicProの一時ファイルは削除対象外.

				#if ENABLE_CRIWARE_FILESYSTEM

				if(criAssetManager.IsCriInstallTempFile(file)) { continue; }

				#endif

				// InstallDirectory直下の管理情報に含まれていないファイルは削除対象.

				var filePath = PathUtility.ConvertPathSeparator(file);

				if (!manageFilePaths.Contains(filePath))
				{
					deleteFiles.Add(filePath);
				}
			}
			
			await DeleteCacheFiles(InstallDirectory, deleteFiles.ToArray());
		}

        /// <summary> 指定されたキャッシュ削除. </summary>
        public async UniTask DeleteCache(AssetInfo[] assetInfos)
        {
			if (LocalMode) { return; }

            var targetFilePaths = new List<string>();

			var frameLimiter = new FunctionFrameLimiter(1000);

			foreach (var assetInfo in assetInfos)
            {
				await frameLimiter.Wait();

				var filePath = GetFilePath(assetInfo);

				if (!string.IsNullOrEmpty(filePath))
                {
                    targetFilePaths.Add(filePath);
                }
			}

			if (targetFilePaths.Any())
			{
	            await DeleteCacheFiles(InstallDirectory, targetFilePaths);
			}
        }

		private async UniTask DeleteCacheFiles(string installDir, IEnumerable<string> filePaths)
        {
			if (LocalMode) { return; }

            var builder = new StringBuilder();

            // ファイル削除.

            void DeleteFile(string filePath)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                        File.Delete(filePath);
                    }

                    var versionFilePath = filePath + AssetInfoManifest.VersionFileExtension;

                    if (File.Exists(versionFilePath))
                    {
                        File.SetAttributes(versionFilePath, FileAttributes.Normal);
                        File.Delete(versionFilePath);
                    }

                    var path = filePath.Substring(installDir.Length).TrimStart(PathUtility.PathSeparator);

                    builder.AppendLine(path);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

			var frameLimiter = new FunctionFrameLimiter(25);

			foreach (var path in filePaths)
			{
				await frameLimiter.Wait();

				DeleteFile(path);
			}

			// ログ.

			sw.Stop();

            var log = builder.ToString();

            if (!string.IsNullOrEmpty(log))
            {
                UnityConsole.Info("Delete cache files ({0}ms)\nDirectory : {1}\n\n{2}", sw.Elapsed.TotalMilliseconds, InstallDirectory, log);
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
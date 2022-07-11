
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.Console;

namespace Modules.ExternalResource
{
    public sealed partial class ExternalResources
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary> 全キャッシュ削除. </summary>
        public async UniTask DeleteAllCache()
        {
            ReleaseManagedAssets();

            UnloadAllAssetBundles(false);

            ClearVersion();

			if (Directory.Exists(InstallDirectory))
			{
	            var cacheFiles = Directory.GetFiles(InstallDirectory, "*", SearchOption.AllDirectories)
	                .Select(x => PathUtility.ConvertPathSeparator(x))
	                .ToArray();

				if (cacheFiles.Any())
				{
					await DeleteCacheFiles(InstallDirectory, cacheFiles);
				}
			}
        }

        /// <summary> 不要になったキャッシュ削除. </summary>
        public async UniTask DeleteDisUsedCache()
        {
            var targetFilePaths = new List<string>();

            targetFilePaths.AddRange(assetBundleManager.GetDisUsedFilePaths() ?? new string[0]);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            targetFilePaths.AddRange(criAssetManager.GetDisUsedFilePaths() ?? new string[0]);

            #endif

            await DeleteCacheFiles(InstallDirectory, targetFilePaths.ToArray());
        }

        /// <summary> 指定されたキャッシュ削除. </summary>
        public async UniTask DeleteCache(AssetInfo[] assetInfos)
        {
            var targetFilePaths = new List<string>();

            foreach (var assetInfo in assetInfos)
            {
                var filePath = string.Empty;

                if (assetInfo.IsAssetBundle)
                {
                    filePath = assetBundleManager.GetFilePath(assetInfo);
                }
                else
                {
                    #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC
                    
                    if (criAssetManager.IsCriAsset(assetInfo.ResourcePath))
                    {
                        filePath = criAssetManager.GetFilePath(assetInfo);
                    }

                    #endif
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    targetFilePaths.Add(filePath);
                }
            }

            await DeleteCacheFiles(InstallDirectory, targetFilePaths.ToArray());
        }

        private async UniTask DeleteCacheFiles(string installDir, string[] filePaths)
        {
            if (filePaths.IsEmpty()) { return; }

            var builder = new StringBuilder();

            // ファイル削除.

            Action<string> deleteFile = filePath =>
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
            };

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var chunk = filePaths.Chunk(25);

            foreach (var paths in chunk)
            {
                foreach (var path in paths)
                {
                    deleteFile.Invoke(path);
                }
                
                await UniTask.NextFrame();
            }

            // 空ディレクトリ削除.

            var deleteDirectorys = DirectoryUtility.DeleteEmpty(installDir);

            deleteDirectorys.ForEach(x => builder.AppendLine(x));

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

            if (MovieManagement.MovieManagement.Exists)
            {
                MovieManagement.MovieManagement.ReleaseAll();
            }

            #endif
        }
    }
}
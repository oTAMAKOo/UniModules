
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UniRx;
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

        /// <summary> �S�L���b�V���폜. </summary>
        public IObservable<Unit> DeleteAllCache()
        {
            ReleaseManagedAssets();

            UnloadAllAssetBundles(false);

            ClearVersion();

            var cacheFiles =Directory.GetFiles(InstallDirectory, "*", SearchOption.AllDirectories)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            return Observable.FromMicroCoroutine(() => DeleteCacheFiles(InstallDirectory, cacheFiles));
        }

        /// <summary> �s�v�ɂȂ����L���b�V���폜. </summary>
        public IObservable<Unit> DeleteDisUsedCache()
        {
            var targetFilePaths = new List<string>();

            targetFilePaths.AddRange(assetBundleManager.GetDisUsedFilePaths() ?? new string[0]);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            targetFilePaths.AddRange(criAssetManager.GetDisUsedFilePaths() ?? new string[0]);

            #endif

            return Observable.FromMicroCoroutine(() => DeleteCacheFiles(InstallDirectory, targetFilePaths.ToArray()));
        }

        /// <summary> �w�肳�ꂽ�L���b�V���폜. </summary>
        public IObservable<Unit> DeleteCache(AssetInfo[] assetInfos)
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

            return Observable.FromMicroCoroutine(() => DeleteCacheFiles(InstallDirectory, targetFilePaths.ToArray()));
        }

        private IEnumerator DeleteCacheFiles(string installDir, string[] filePaths)
        {
            if (filePaths.IsEmpty()) { yield break; }

            var builder = new StringBuilder();

            // �t�@�C���폜.

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
                
                yield return null;
            }

            // ��f�B���N�g���폜.

            var deleteDirectorys = DirectoryUtility.DeleteEmpty(installDir);

            deleteDirectorys.ForEach(x => builder.AppendLine(x));

            // ���O.

            sw.Stop();

            var log = builder.ToString();

            if (!string.IsNullOrEmpty(log))
            {
                UnityConsole.Info("Delete cache files ({0}ms)\nDirectory : {1}\n\n{2}", sw.Elapsed.TotalMilliseconds, InstallDirectory, log);
            }
        }

        /// <summary> �Ǘ����̃t�@�C�������. </summary>
        private void ReleaseManagedAssets()
        {
            #if ENABLE_CRIWARE_ADX

            if (SoundManagement.SoundManagement.Exists)
            {
                SoundManagement.SoundManagement.Instance.ReleaseAll();
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
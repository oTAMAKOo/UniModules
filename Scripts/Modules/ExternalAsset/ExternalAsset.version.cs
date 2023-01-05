
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare;

#endif

namespace Modules.ExternalAssets
{
	public sealed partial class ExternalAsset
	{
        //----- params -----

        //----- field -----

        private Dictionary<string, string> versions = null;

        //----- property -----

        //----- method -----

        /// <summary>
        /// アセットバンドルのバージョンが最新か確認.
        /// (同梱された別アセットが更新された場合でもtrueを返す)
        /// </summary>
        private bool CheckAssetBundleVersion(AssetInfo assetInfo)
        {
            var filePath = assetBundleManager.GetFilePath(InstallDirectory, assetInfo);

            // ※ シュミレート時はpackageファイルをダウンロードしていないので常にファイルが存在しない.

            if (!simulateMode)
            {
                // ファイルがない.
                if (!File.Exists(filePath)) { return false; }
            }

            // バージョン情報が存在しない.
            if (versions.IsEmpty()) { return false; }

            var infos = assetInfosByAssetBundleName.GetValueOrDefault(assetInfo.AssetBundle.AssetBundleName);

            if (infos == null) { return false; }

            foreach (var info in infos)
            {
                var hash = versions.GetValueOrDefault(info.FileName);

                // ローカルにバージョンが存在しない.
                if (string.IsNullOrEmpty(hash)) { return false; }

                // アセットバンドル内のアセットが更新されている.
                if (hash != info.Hash) { return false; }
            }

            return true;
        }

        /// <summary> アセットバンドル以外のアセットの更新が必要か確認. </summary>
        private bool CheckAssetVersion(AssetInfo assetInfo)
        {
			// バージョン情報が存在しない.
            if (versions.IsEmpty()) { return false; }

			// アセット管理情報内に存在しないので最新扱い.
            if (assetInfo == null) { return true; }

			// ファイルがない.

			var filePath = PathUtility.Combine(InstallDirectory, assetInfo.FileName);

			if (!File.Exists(filePath)) { return false; }

            // バージョン不一致.

            var hash = versions.GetValueOrDefault(assetInfo.FileName);
            
            return hash == assetInfo.Hash;
        }

		/// <summary> 更新が必要なアセット情報を取得. </summary>
		public async UniTask<IEnumerable<AssetInfo>> GetRequireUpdateAssetInfos(string groupName = null)
		{
			if (simulateMode){ return new AssetInfo[0]; }

			var assetInfos = assetInfoManifest.GetAssetInfos(groupName);

			// バージョン情報読み込み.

			if (versions == null)
			{
				await LoadVersion();
			}

			// バージョン情報が存在しないので全更新.
			if (versions.IsEmpty()) { return assetInfos; }

			var list = new List<AssetInfo>();

            void CheckRequireUpdate(IEnumerable<AssetInfo> infos)
            {
                foreach (var info in infos)
                {
                    if (IsRequireUpdate(info))
                    {
                        lock (list)
                        {
                            list.Add(info);
                        }
                    }
                }
            }

            var tasks = new List<UniTask>();

            var chunck = assetInfos.Chunk(250);

			foreach (var items in chunck)
			{
				var infos = items;

                var task = UniTask.RunOnThreadPool(() =>
                {
                    CheckRequireUpdate(infos);
                });

                tasks.Add(task);
			}

            await UniTask.WhenAll(tasks);

            return list;
		}

		/// <summary> 更新が必要か. </summary>
		public bool IsRequireUpdate(AssetInfo assetInfo)
		{
			if (simulateMode){ return false; }

			// バージョン情報が存在しないので更新.
			if (versions.IsEmpty()) { return true; }

			var requireUpdate = true;

			if (assetInfo.IsAssetBundle)
			{
				requireUpdate = !CheckAssetBundleVersion(assetInfo);
			}
			else
			{
				requireUpdate = !CheckAssetVersion(assetInfo);
			}            

			return requireUpdate;
		}

		private void UpdateVersion(string resourcePath)
		{
			if (simulateMode){ return; }

			// ※ 古いバージョン情報を破棄して最新のバージョン情報を追加.
                
			var assetInfo = GetAssetInfo(resourcePath);

			if (assetInfo == null)
			{
				var exception = new AssetInfoNotFoundException(resourcePath);

				OnError(exception);

				return;
			}

			// ※ バージョン文字列だけのデータなので暗号化は行わない.

			if (string.IsNullOrEmpty(assetInfo.Hash)){ return; }

			var filePath = PathUtility.Combine(InstallDirectory, assetInfo.FileName);

			var versionFilePath = filePath + AssetInfoManifest.VersionFileExtension;

			try
			{
				var bytes = Encoding.UTF8.GetBytes(assetInfo.Hash);

				File.WriteAllBytes(versionFilePath, bytes);

                versions[assetInfo.FileName] = assetInfo.Hash;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);

				if (File.Exists(versionFilePath))
				{
					File.Delete(versionFilePath);
				}
			}
		}

		public async UniTask LoadVersion()
		{
			if (simulateMode){ return; }

            versions = new Dictionary<string, string>();

			var versionFilePaths = GetAllVersionFilePaths();

			var versionFileExtensionLength = AssetInfoManifest.VersionFileExtension.Length;

            var tasks = new List<UniTask>();

            // 複数のバージョンファイルを読み込むローカル関数.
            void LoadVersionFiles(string[] paths)
            {
                foreach (var path in paths)
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(path);

                        var hash = Encoding.UTF8.GetString(bytes);

                        var versionFileName = Path.GetFileName(path);

                        var fileName = versionFileName.SafeSubstring(0, versionFileName.Length - versionFileExtensionLength);

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            lock (versions)
                            {
                                versions[fileName] = hash;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);

                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                }
            }

			var chunck = versionFilePaths.Chunk(250);

            foreach (var items in chunck)
            {
                var paths = items.ToArray();

                var task = UniTask.RunOnThreadPool(() =>
                {
                    LoadVersionFiles(paths);
                });

                tasks.Add(task);
            }

            await UniTask.WhenAll(tasks);
		}

		private async UniTask ClearVersion()
		{
			if (simulateMode){ return; }

			if (versions != null)
			{
				versions.Clear();
			}

			var versionFilePaths = GetAllVersionFilePaths();

            var count = 0;

			foreach (var versionFilePath in versionFilePaths)
			{
				try
				{
					var cFileInfo = new FileInfo(versionFilePath);

					// 読み取り専用属性がある場合は、読み取り専用属性を解除.
					if ((cFileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					{
						cFileInfo.Attributes = FileAttributes.Normal;
					}

					File.Delete(versionFilePath);
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}

                if (++count % 100 == 0)
                {
                    await UniTask.NextFrame();
                }
			}
		}

		private IEnumerable<string> GetAllVersionFilePaths()
		{
			if (!Directory.Exists(InstallDirectory)){ return new string[0]; }
            
			var directoryInfo = new DirectoryInfo(InstallDirectory);

			var files = directoryInfo.EnumerateFiles("*" + AssetInfoManifest.VersionFileExtension, SearchOption.AllDirectories);
            
			return files.Select(x => x.FullName);
		}
    }
}

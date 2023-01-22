
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Performance;

namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset
    {
        //----- params -----

        //----- field -----

        private Dictionary<string, string> versions = null;

        private FunctionFrameLimiter requireUpdateCallFrameLimiter = null;

        //----- property -----

        //----- method -----

        private void InitializeVersionCheck()
        {
            requireUpdateCallFrameLimiter = new FunctionFrameLimiter(50);
        }

        /// <summary>
        /// アセットバンドルのバージョンが最新か確認.
        /// (同梱された別アセットが更新された場合でもtrueを返す)
        /// </summary>
        private bool CheckAssetBundleVersion(AssetInfo assetInfo)
        {
            var filePath = assetBundleManager.GetFilePath(InstallDirectory, assetInfo);

            // ※ シュミレート時はpackageファイルをダウンロードしていないので常にファイルが存在しない.

            if (!SimulateMode)
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
			if (SimulateMode){ return new AssetInfo[0]; }

            var list = new List<AssetInfo>();

            // バージョン情報読み込み.
            if (versions == null)
            {
                await LoadVersion();
            }
            
			// 最新のバージョン情報.

            var assetInfos = assetInfoManifest.GetAssetInfos(groupName);

            // ローカルのバージョンとの差分更新.
            if (versions.Any())
            {
                var tasks = new List<UniTask>();

                var chunck = assetInfos.Chunk(250);

                foreach (var items in chunck)
                {
                    tasks.Clear();

                    foreach (var assetInfo in items)
				    {
                        var info = assetInfo;

                        var task = UniTask.Defer(async () =>
                        {
                            try
                            {
                                var requireUpdate = await UniTask.RunOnThreadPool(() => IsRequireUpdate(info, false));

                                if (requireUpdate)
                                {
                                    list.Add(info);
                                }
                            }
                            finally
                            {
                                await UniTask.SwitchToMainThread();
                            }
                        });

                        tasks.Add(task);
                    }

                    await UniTask.WhenAll(tasks);
                }
            }
            // バージョン情報が存在しないので全更新.
            else
            {
                list = assetInfos.ToList();
            }

            return list;
		}

        /// <summary> 更新が必要か. </summary>
		public async UniTask<bool> IsRequireUpdate(AssetInfo assetInfo, bool farameCallLimit = true)
        {
            if (SimulateMode){ return false; }

            // バージョン情報が存在しないので更新.
            if (versions.IsEmpty()) { return true; }

            // フレーム処理数制御.
            if (farameCallLimit)
            {
                await requireUpdateCallFrameLimiter.Wait();
            }

			// バージョンチェック.

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

        private async UniTask UpdateVersion(string resourcePath)
		{
			if (SimulateMode){ return; }

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
                
                using (var fs = new FileStream(versionFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 64, true))
                {
                    await fs.WriteAsync(bytes, 0, bytes.Length);
                }

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

        private async UniTask LoadVersion()
		{
			if (SimulateMode){ return; }
            
            versions = new Dictionary<string, string>();

            var versionFileExtensionLength = AssetInfoManifest.VersionFileExtension.Length;
            
            var versionFilePaths = await GetAllVersionFilePaths();

            var tasks = new List<UniTask>();

            var chunck = versionFilePaths.Chunk(250);

            foreach (var items in chunck)
            {
                tasks.Clear();

                foreach (var versionFilePath in items)
                {
                    var path = versionFilePath;

                    var task = UniTask.Defer(async () =>
				    {
                        try
                        {
                            await UniTask.RunOnThreadPool(() => LoadVersionFile(path, versionFileExtensionLength));
                        }
                        finally
                        {
                            await UniTask.SwitchToMainThread();
                        }
                    });

                    tasks.Add(task);
                }

                await UniTask.WhenAll(tasks);
            }
        }
        
        private void LoadVersionFile(string path, int versionFileExtensionLength)
        {
            try
            {
                if (!File.Exists(path)){ return; }

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

		private async UniTask ClearVersion()
		{
			if (SimulateMode){ return; }

			if (versions != null)
			{
				versions.Clear();
			}

            var frameLimiter = new FunctionFrameLimiter(100);

            var versionFilePaths = await GetAllVersionFilePaths();

            var tasks = new List<UniTask>();
                
            foreach (var versionFilePath in versionFilePaths)
            {
                await frameLimiter.Wait();
                
                var path = versionFilePath;

                var task = UniTask.Defer(async () =>
				{
                    try
                    {
                        await UniTask.RunOnThreadPool(() =>DeleteVersionFile(path));
                    }
                    finally
                    {
                        await UniTask.SwitchToMainThread();
                    }
                });

                tasks.Add(task);
            }

            await UniTask.WhenAll(tasks);
        }

        private void DeleteVersionFile(string versionFilePath)
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
        }

		private async UniTask<IEnumerable<string>> GetAllVersionFilePaths()
		{
            const string searchPattern = "*" + AssetInfoManifest.VersionFileExtension;

			if (!Directory.Exists(InstallDirectory)){ return new string[0]; }

            var frameLimiter = new FunctionFrameLimiter(250);

            var list = new List<string>();

            var directoryInfo = new DirectoryInfo(InstallDirectory);

            var files = directoryInfo.EnumerateFiles(searchPattern, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                await frameLimiter.Wait();

                list.Add(file.FullName);
            }

			return list;
		}
    }
}

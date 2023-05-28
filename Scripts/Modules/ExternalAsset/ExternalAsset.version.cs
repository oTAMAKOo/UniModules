
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using UniRx;
using Modules.Devkit.Console;

namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset
    {
        //----- params -----

        private const string VersionFileName = "version";

        private const char VersionSeparator = '|';

        //----- field -----

        // バージョン情報.
        private Dictionary<string, string> versions = null;

        // バージョンファイル難読化ハンドラー.
        private IVersionFileHandler versionFileHandler = null;

        // バージョン情報定期保存処理.
        private IDisposable saveVersionDisposable = null;

        // バージョン情報保存処理が実行中.
        private bool saveVersionRunning = false;

        // バージョン情報要求ID.
        private string requestSaveVersionIdentifier = null;

        // バージョン情報保存ID.
        private string saveVersionIdentifier = null;

        // ファイル一覧の一時キャッシュ.
        private HashSet<string> filePathTemporaryCache = null;

        //----- property -----

        //----- method -----

        private void InitializeVersionCheck()
        {
            versions = new Dictionary<string, string>();

            versionFileHandler = new DefaultVersionFileHandler();

            saveVersionRunning = false;

            filePathTemporaryCache = null;
        }

        public void SetVersionFileHandler(IVersionFileHandler versionFileHandler)
        {
            this.versionFileHandler = versionFileHandler;
        }

        /// <summary> 更新が必要なアセット情報を取得. </summary>
        public async UniTask<IEnumerable<AssetInfo>> GetRequireUpdateAssetInfos(string groupName = null)
        {
            if (SimulateMode) { return new AssetInfo[0]; }

            IEnumerable<AssetInfo> result = null;

            try
            {
                await UniTask.SwitchToThreadPool();

                var assetInfos = assetInfoManifest.GetAssetInfos(groupName).DistinctBy(x => x.FileName);

                // バージョン情報が存在しないので全更新.

                if (versions.IsEmpty()) { return assetInfos; }

                // ローカルのバージョンとの差分更新.

                var filePaths = await GetInstallDirectoryFilePaths();

                filePathTemporaryCache = filePaths.ToHashSet();

                var tasks = new List<UniTask<IEnumerable<AssetInfo>>>();

                var chunck = assetInfos.Chunk(500);

                foreach (var items in chunck)
                {
                    var task = FilterRequireUpdateAssetInfo(items);

                    tasks.Add(task);
                }

                var filterResults = await UniTask.WhenAll(tasks);

                result = filterResults.SelectMany(x => x);

                filePathTemporaryCache = null;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            return result;
        }

        private async UniTask<IEnumerable<AssetInfo>> FilterRequireUpdateAssetInfo(IEnumerable<AssetInfo> infos)
        {
            var result = new AssetInfo[0];

            try
            {
                await UniTask.SwitchToThreadPool();

                result = infos.Where(x => IsRequireUpdate(x)).ToArray();
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            return result;
        }

        /// <summary> 更新が必要か. </summary>
        public bool IsRequireUpdate(AssetInfo assetInfo)
        {
            if (SimulateMode){ return false; }

            // バージョン情報が存在しないので更新.
            if (versions.IsEmpty()) { return true; }

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
                // ファイルの存在確認.
                if (filePathTemporaryCache != null)
                {
                    if (!filePathTemporaryCache.Contains(filePath)){ return false; }
                }
                else
                {
                    if (!File.Exists(filePath)) { return false; }
                }
            }
            
            if (versions.IsEmpty()) { return false; }

            var hash = versions.GetValueOrDefault(assetInfo.FileName);

            if (string.IsNullOrEmpty(hash)) { return false; }

            if (hash != assetInfo.Hash) { return false; }

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

            // ファイルの存在確認.
            if (filePathTemporaryCache != null)
            {
                if (!filePathTemporaryCache.Contains(filePath)) { return false; }
            }
            else
            {
                if (!File.Exists(filePath)) { return false; }
            }

            // バージョン不一致.

            var hash = versions.GetValueOrDefault(assetInfo.FileName);

            return hash == assetInfo.Hash;
        }

        private void UpdateVersion(string resourcePath)
        {
            if (SimulateMode){ return; }

            // ※ 古いバージョン情報を破棄して最新のバージョン情報を追加.

            AssetInfo assetInfo = null;

            try
            {
                assetInfo = GetAssetInfo(resourcePath);

                if (assetInfo == null)
                {
                    throw new AssetInfoNotFoundException(resourcePath);
                }
            }
            catch (AssetInfoNotFoundException e)
            {
                OnError(e);

                return;
            }

            if (string.IsNullOrEmpty(assetInfo.Hash)){ return; }

            lock (versions)
            {
                versions[assetInfo.FileName] = assetInfo.Hash;
            }

            RequestSaveVersion();
        }

        private void RemoveVersion(string resourcePath)
        {
            if (SimulateMode){ return; }

            AssetInfo assetInfo = null;

            try
            {
                assetInfo = GetAssetInfo(resourcePath);

                if (assetInfo == null)
                {
                    throw new AssetInfoNotFoundException(resourcePath);
                }
            }
            catch (AssetInfoNotFoundException e)
            {
                OnError(e);

                return;
            }

            lock (versions)
            {
                versions.Remove(assetInfo.FileName);
            }

            RequestSaveVersion();
        }

        public void RequestSaveVersion()
        {
            if (SimulateMode) { return; }

            requestSaveVersionIdentifier = Guid.NewGuid().ToString();

            if (saveVersionDisposable == null)
            {
                saveVersionDisposable = Observable.Interval(TimeSpan.FromSeconds(1))
                    .Take(TimeSpan.FromSeconds(10))
                    .Finally(() => saveVersionDisposable = null)
                    .Subscribe(_ =>
                        {
                            if (saveVersionIdentifier != requestSaveVersionIdentifier)
                            {
                                saveVersionIdentifier = requestSaveVersionIdentifier;

                                SaveVersion().Forget();
                            }
                        })
                    .AddTo(Disposable);
            }
        }

        public async UniTask SaveVersion()
        {
            // 実行中は待機.
            while (saveVersionRunning)
            {
                await UniTask.NextFrame();
            }

            saveVersionRunning = true;

            var versionFilePath = PathUtility.Combine(InstallDirectory, VersionFileName);

            // バージョン情報の複製を作成.

            Dictionary<string, string> saveVersions = null;

            lock (versions)
            {
                saveVersions = new Dictionary<string, string>(versions);
            }

            try
            {
                await UniTask.SwitchToThreadPool();
                
                // 保存データ構築.

                var builder = new StringBuilder();

                foreach (var version in saveVersions)
                {
                    builder.Append(version.Key);
                    builder.Append(VersionSeparator);
                    builder.Append(version.Value);
                    builder.AppendLine();
                }

                var text = builder.ToString();

                var bytes = Encoding.UTF8.GetBytes(text);

                if (versionFileHandler != null)
                {
                    bytes = await versionFileHandler.Encode(bytes);
                }

                // ファイル書き込み.

                using (var fs = new FileStream(versionFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true))
                {
                    await fs.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                if (!string.IsNullOrEmpty(versionFilePath) && File.Exists(versionFilePath))
                {
                    File.Delete(versionFilePath);
                }
            }
            finally
            {
                await UniTask.SwitchToMainThread();

                saveVersionRunning = false;
            }
        }

        private async UniTask LoadVersion()
        {
            if (SimulateMode) { return; }

            if (saveVersionDisposable != null)
            {
                saveVersionDisposable.Dispose();
                saveVersionDisposable = null;
            }

            while (saveVersionRunning)
            {
                await UniTask.NextFrame();
            }

            var logText = string.Empty;

            var versionFilePath = PathUtility.Combine(InstallDirectory, VersionFileName);

            try
            {
                await UniTask.SwitchToThreadPool();

                var sw = System.Diagnostics.Stopwatch.StartNew();

                versions.Clear();

                if (File.Exists(versionFilePath))
                {
                    byte[] bytes = null;

                    using (var fs = new FileStream(versionFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                    {
                        bytes = new byte[fs.Length];

                        await fs.ReadAsync(bytes, 0, bytes.Length);
                    }

                    if (versionFileHandler != null)
                    {
                        bytes = await versionFileHandler.Decode(bytes);
                    }

                    var text = Encoding.UTF8.GetString(bytes);

                    using (var sr = new StringReader(text))
                    {
                        while (-1 < sr.Peek())
                        {
                            var line = await sr.ReadLineAsync();

                            var parts = line.Split(VersionSeparator);

                            var fileName = parts.ElementAtOrDefault(0);
                            var version = parts.ElementAtOrDefault(1);

                            if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(version))
                            {
                                versions[fileName] = version;
                            }
                        }
                    }

                    sw.Stop();

                    logText = $"LoadVersion: ({sw.Elapsed.TotalMilliseconds:F2}ms)";
                }
            }
            catch
            {
                if (File.Exists(versionFilePath))
                {
                    File.Delete(versionFilePath);
                }
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            if (!string.IsNullOrEmpty(logText) && LogEnable && UnityConsole.Enable)
            {
                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, logText);
            }
        }

        private void ClearVersion()
        {
            if (SimulateMode){ return; }

            lock (versions)
            {
                versions.Clear();
            }

            try
            {
                var versionFilePath = PathUtility.Combine(InstallDirectory, VersionFileName);

                if (File.Exists(versionFilePath))
                {
                    var cFileInfo = new FileInfo(versionFilePath);

                    // 読み取り専用属性がある場合は、読み取り専用属性を解除.
                    if ((cFileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        cFileInfo.Attributes = FileAttributes.Normal;
                    }

                    File.Delete(versionFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}

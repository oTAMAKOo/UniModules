
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Net;
using Modules.Net.WebRequest;
using Modules.ExternalAssets;
using Modules.Net.WebDownload;
using Modules.Performance;

namespace Modules.AssetBundles
{
    public interface ILocalModeVersionHandler
    {
        bool IsRequireUpdate(AssetInfo assetInfo);

        void OnUpdateLocalFile(AssetInfo assetInfo);
    }

    public sealed partial class AssetBundleManager : Singleton<AssetBundleManager>
    {
        //----- params -----

        public const string PackageExtension = ".package";

        // 非同期で読み込むファイルサイズ (0.1MB).
        private const float AsyncLoadFileSize = 1024.0f * 1024.0f * 0.1f;

        // 1フレームで同時に同期読み込みする最大ファイルサイズ (1MB).
        private const float FrameSyncLoadFileSize = 1024.0f * 1024.0f * 1f;

        // 1フレームで同期読み込みする最大ファイル数.
        private const int FrameSyncLoadFileNum = 20;

        // タイムアウトまでの時間.
        private readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(60f);
        private readonly TimeSpan LoadTimeout = TimeSpan.FromSeconds(10f);

        // リトライする回数.
        private readonly int RetryCount = 5;

        // リトライするまでの時間(秒).
        private readonly TimeSpan RetryDelaySeconds = TimeSpan.FromSeconds(2f);

        //----- field -----

        // 同時ダウンロード数.
        private uint maxDownloadCount = 0;

        // フレーム処理数制限.
        
        private FunctionFrameLimiter syncLoadFileCountLimiter = null;
        private FunctionFrameLimiter syncLoadFileSizeLimiter = null;

        // ダウンロード元URL.
        private string remoteUrl = null;
        private string versionHash = null;

        // URL作成用.
        private StringBuilder urlBuilder = null;

        // ダウンロード中アセットバンドル.
        private HashSet<string> downloadRunning = null;

        // ダウンロードタスク.
        private Dictionary<string, IObservable<Unit>> downloadTasks = null;

        // 読み込み待ちアセットバンドル.
        private Dictionary<string, IObservable<AssetBundle>> loadQueueing = null;

        // 読み込み済みアセットバンドル.
        private Dictionary<string, AssetBundle> loadedAssetBundles = null;

        // 読み込み済みアセットバンドル参照カウント.
        private Dictionary<string, int> assetBundleRefCount = null;

        // アセット情報(アセットバンドル).
        private Dictionary<string, List<AssetInfo>> assetInfosByAssetBundleName = null;

        // 依存関係.
        private AssetBundleDependencies assetBundleDependencies = null;

        // ファイルハンドラ.
        private IAssetBundleFileHandler fileHandler = null;

        // ローカルモードバージョンハンドラ.
        private ILocalModeVersionHandler localModeVersionHandler = null;

        // イベント通知.
        private Subject<string> onLoad = null;
        private Subject<AssetInfo> onTimeOut = null;
        private Subject<Exception> onError = null;

        private bool isInitialized = false;

        //----- property -----

        // シュミュレートモードか.
        public bool IsSimulateMode { get; private set; } = false;

        // ローカルモードか.
        public bool IsLocalMode { get; private set; } = false;

        // 読み込み失敗時にファイルを削除するか.
        public bool DeleteOnLoadError { get; set; } = true;

        //----- method -----

        private AssetBundleManager() { }

        /// <summary> 初期化 </summary>
        /// <param name="simulateMode">AssetDataBaseからアセットを取得(EditorOnly)</param>
        public void Initialize(bool simulateMode = false)
        {
            if (isInitialized) { return; }

            urlBuilder = new StringBuilder();
            downloadRunning = new HashSet<string>();
            downloadTasks = new Dictionary<string, IObservable<Unit>>();
            loadQueueing = new Dictionary<string, IObservable<AssetBundle>>();
            loadedAssetBundles = new Dictionary<string, AssetBundle>();
            assetBundleRefCount = new Dictionary<string, int>();
            assetInfosByAssetBundleName = new Dictionary<string, List<AssetInfo>>();
            assetBundleDependencies = new AssetBundleDependencies();
            fileHandler = new DefaultAssetBundleFileHandler();

            syncLoadFileCountLimiter = new FunctionFrameLimiter(FrameSyncLoadFileNum);
            syncLoadFileSizeLimiter = new FunctionFrameLimiter((ulong)FrameSyncLoadFileSize);

            SetSimulateMode(simulateMode);
            SetLocalMode(false);

            AddManifestAssetInfo();

            isInitialized = true;
        }

        /// <summary> 同時ダウンロード数設定. </summary>
        public void SetMaxDownloadCount(uint maxDownloadCount)
        {
            this.maxDownloadCount = maxDownloadCount;
        }

        /// <summary> シミュレーションモード設定. </summary>
        public void SetSimulateMode(bool simulateMode)
        {
            IsSimulateMode = UnityUtility.IsEditor && simulateMode;
        }

        /// <summary> ローカルモード設定. </summary>
        public void SetLocalMode(bool localMode)
        {
            IsLocalMode= localMode;
        }

        /// <summary> ファイルハンドラ設定. </summary>
        public void SetFileHandler(IAssetBundleFileHandler fileHandler)
        {
            this.fileHandler = fileHandler;
        }

        /// <summary> ローカルモード用バージョンハンドラ設定. </summary>
        public void SetLocalModeVersionHandler(ILocalModeVersionHandler localModeVersionHandler)
        {
            this.localModeVersionHandler = localModeVersionHandler;
        }

        /// <summary> URLを設定. </summary>
        /// <param name="remoteUrl">アセットバンドルのディレクトリURLを指定</param>
        /// <param name="versionHash">バージョンハッシュを指定</param>
        public void SetUrl(string remoteUrl, string versionHash)
        {
            this.remoteUrl = remoteUrl;
            this.versionHash = versionHash;
        }

        public async UniTask SetManifest(AssetInfoManifest manifest)
        {
            assetInfosByAssetBundleName.Clear();
            assetBundleRefCount.Clear();
            assetBundleDependencies.Clear();

            if (manifest == null) { return; }

            var assetBundleAssetInfos = manifest.GetAssetInfos()
                .Where(x => x.IsAssetBundle)
                .ToArray();

            var buildReferencesTask = UniTask.Defer(() => BuildAssetBundleReferences(assetBundleAssetInfos));
            var buildDependenciesTask = UniTask.Defer(() => BuildAssetBundleDependencies(assetBundleAssetInfos));

            await UniTask.WhenAll(buildReferencesTask, buildDependenciesTask);

            AddManifestAssetInfo();
        }

        private async UniTask BuildAssetBundleReferences(AssetInfo[] assetBundleAssetInfos)
        {
            await UniTask.RunOnThreadPool(() =>
            {
                foreach (var assetInfo in assetBundleAssetInfos)
                {
                    var assetBundleInfo = assetInfo.AssetBundle;
                    var assetBundleName = assetBundleInfo.AssetBundleName;

                    var refs = assetInfosByAssetBundleName.GetValueOrDefault(assetBundleName);

                    if (refs == null)
                    {
                        refs = new List<AssetInfo>();
                        assetInfosByAssetBundleName[assetBundleName] = refs;
                    }

                    refs.Add(assetInfo);

                    assetBundleRefCount[assetBundleName] = 0;
                }
            });
        }

        private async UniTask BuildAssetBundleDependencies(AssetInfo[] assetBundleAssetInfos)
        {
            await UniTask.RunOnThreadPool(() =>
            {
                foreach (var assetInfo in assetBundleAssetInfos)
                {
                    var assetBundleInfo = assetInfo.AssetBundle;
                    var assetBundleName = assetBundleInfo.AssetBundleName;

                    assetBundleDependencies.SetDependencies(assetBundleName, assetBundleInfo.Dependencies);
                }
            });
        }

        private void AddManifestAssetInfo()
        {
            // ※ アセット管理情報内にAssetInfoManifestの情報は入っていないので明示的に追加する.

            var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();

            assetInfosByAssetBundleName[manifestAssetInfo.AssetBundle.AssetBundleName] = new List<AssetInfo> { manifestAssetInfo };

            assetBundleRefCount[manifestAssetInfo.AssetBundle.AssetBundleName] = 0;
        }

        /// <summary> 展開中のアセットバンドル名一覧取得 </summary>
        public Tuple<string, int>[] GetLoadedAssetBundleNames()
        {
            return loadedAssetBundles != null ?
                loadedAssetBundles.Select(x => Tuple.Create(x.Key, assetBundleRefCount.GetValueOrDefault(x.Key))).ToArray() :
                new Tuple<string, int>[0];
        }

        private string BuildDownloadUrl(AssetInfo assetInfo)
        {
            urlBuilder.Clear();

            var platformName = PlatformUtility.GetPlatformTypeName();

            var downloadUrl = PathUtility.Combine(remoteUrl, platformName, versionHash, assetInfo.FileName);

            urlBuilder.Append(downloadUrl).Append(PackageExtension);

            if (!assetInfo.Hash.IsNullOrEmpty())
            {
                urlBuilder.AppendFormat("?v={0}", assetInfo.Hash);
            }

            return urlBuilder.ToString();
        }

        public string GetFilePath(string installPath, AssetInfo assetInfo)
        {
            if (assetInfo == null || string.IsNullOrEmpty(assetInfo.FileName)) { return null; }

            var path = Path.Combine(installPath, assetInfo.FileName);

            var filePath = Path.ChangeExtension(path, PackageExtension);

            return PathUtility.ConvertPathSeparator(filePath);
        }

        #region Download

        /// <summary>
        /// アセット情報ファイルを更新.
        /// </summary>
        public async UniTask UpdateAssetInfoManifest(string installPath, CancellationToken cancelToken = default)
        {
            if (IsSimulateMode || IsLocalMode) { return; }

            if (cancelToken.IsCancellationRequested) { return; }

            var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();

            await UpdateAssetBundle(installPath, manifestAssetInfo, null, cancelToken);
        }

        /// <summary>
        /// アセットバンドルを更新.
        /// </summary>
        public async UniTask UpdateAssetBundle(string installPath, AssetInfo assetInfo, IProgress<DownloadProgressInfo> progress = null, CancellationToken cancelToken = default)
        {
            if (IsSimulateMode || IsLocalMode) { return; }

            var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

            var task = downloadTasks.GetValueOrDefault(assetBundleName);

            if (task == null)
            {
                task = UniTask.Defer(() => DownloadAssetBundle(installPath, assetInfo, progress, cancelToken))
                    .ToObservable()
                    .OnErrorRetry((Exception _) => { }, RetryCount, RetryDelaySeconds)
                    .DoOnError(x => OnError(x))
                    .AsUnitObservable()
                    .Share();

                downloadTasks[assetBundleName] = task;
            }

            await task;
        }

        private async UniTask DownloadAssetBundle(string installPath, AssetInfo assetInfo, IProgress<DownloadProgressInfo> progress, CancellationToken cancelToken)
        {
            var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

            try
            {
                var info = assetInfosByAssetBundleName.GetValueOrDefault(assetBundleName).FirstOrDefault();

                if (info == null)
                {
                    throw new AssetInfoNotFoundException(assetBundleName);
                }

                // ロード済みの場合はアンロード.

                if (loadedAssetBundles.ContainsKey(assetBundleName))
                {
                    UnloadAsset(assetBundleName, true);
                }

                // ダウンロード順番待ち.

                while (true)
                {
                    if (cancelToken.IsCancellationRequested) { break; }

                    if (downloadRunning.Count <= maxDownloadCount) { break; }

                    await UniTask.NextFrame(CancellationToken.None);
                }

                if (cancelToken.IsCancellationRequested) { return; }

                // ネットワークの接続待ち.

                await NetworkConnection.WaitNetworkReachable(cancelToken);

                if (cancelToken.IsCancellationRequested) { return; }

                // ダウンロード実行.

                downloadRunning.Add(assetBundleName);

                await FileDownload(installPath, assetInfo, progress, cancelToken)
                    .ToObservable()
                    .Timeout(DownloadTimeout)
                    .OnErrorRetry((TimeoutException ex) => OnTimeout(assetInfo, ex), RetryCount, RetryDelaySeconds);
            }
            catch (OperationCanceledException)
            {
                /* Canceled */
            }
            finally
            {
                // ダウンロード中リストから削除.
                downloadRunning.Remove(assetBundleName);

                // ダウンロードタスク削除.
                if (downloadTasks.ContainsKey(assetBundleName))
                {
                    downloadTasks.Remove(assetBundleName);
                }
            }
        }

        private async UniTask FileDownload(string installPath, AssetInfo assetInfo, IProgress<DownloadProgressInfo> progress, CancellationToken cancelToken)
        {
            var filePath = GetFilePath(installPath, assetInfo);

            if (string.IsNullOrEmpty(filePath)) { return; }

            // ダウンロード.

            IProgress<float> progressReceiver = null;

            DownloadProgressInfo progressInfo = null;

            if (progress != null)
            {
                progressInfo = new DownloadProgressInfo(assetInfo);

                void OnReceiveProgress(float value)
                {
                    progressInfo.SetProgress(value);

                    progress.Report(progressInfo);
                }

                progressReceiver = new Progress<float>(OnReceiveProgress);
            }

            var url = BuildDownloadUrl(assetInfo);

            using (var downloadRequest = new DownloadRequest())
            {
                downloadRequest.Initialize(url, filePath);

                downloadRequest.TimeOutSeconds = (int)DownloadTimeout.TotalSeconds;

                try
                {
                    await downloadRequest.Download(progressReceiver, cancelToken);
                }
                catch (UnityWebRequestErrorException e)
                {
                    throw new Exception($"File download error\nURL:{url}\nResponseCode:{e.Request.responseCode}\n\n{e.Request.error}\n");
                }
            }
        }

        #endregion

        #region Dependencies

        public string[] GetAllDependenciesAndSelf(string assetBundleName)
        {
            if (assetBundleDependencies == null){ return null; }

            var allDependencies = GetAllDependencies(assetBundleName);

            return allDependencies.Append(assetBundleName).Distinct().ToArray();
        }

        public string[] GetAllDependencies(string assetBundleName)
        {
            if (assetBundleDependencies == null){ return null; }

            return assetBundleDependencies.GetAllDependencies(assetBundleName);
        }

        #endregion

        #region ReferenceCount

        private int IncrementReferenceCount(string assetBundleName)
        {
            var referenceCount = assetBundleRefCount.GetValueOrDefault(assetBundleName);

            referenceCount++;

            assetBundleRefCount[assetBundleName] = referenceCount;

            return referenceCount;
        }

        private int DecrementReferenceCount(string assetBundleName)
        {
            var referenceCount = assetBundleRefCount.GetValueOrDefault(assetBundleName);

            referenceCount = Math.Max(0, referenceCount - 1);

            assetBundleRefCount[assetBundleName] = referenceCount;

            return referenceCount;
        }

        #endregion

        #region Load

        /// <summary>
        /// 名前で指定したアセットを取得.
        /// </summary>
        public async UniTask<T> LoadAsset<T>(string installPath, AssetInfo assetInfo, string assetPath, bool autoUnLoad = true, CancellationToken cancelToken = default)
            where T : UnityEngine.Object
        {
            // コンポーネントを取得する場合はGameObjectから取得.
            if (typeof(T).IsSubclassOf(typeof(Component)))
            {
                var go = await LoadAssetInternal<GameObject>(installPath, assetInfo, assetPath, autoUnLoad, cancelToken);

                return go != null ? go.GetComponent<T>() : null;
            }

            return await LoadAssetInternal<T>(installPath, assetInfo, assetPath, autoUnLoad, cancelToken);
        }

        private async UniTask<T> LoadAssetInternal<T>(string installPath, AssetInfo assetInfo, string assetPath, bool autoUnLoad, CancellationToken cancelToken)
            where T : UnityEngine.Object
        {
            T result = null;

            #if UNITY_EDITOR

            if (IsSimulateMode)
            {
                try
                {
                    result = await SimulateLoadAsset<T>(installPath, assetPath, cancelToken);
                }
                catch (OperationCanceledException)
                {
                    /* Canceled */
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            else

            #endif

            {
                try
                {
                    // アセットバンドル名は小文字なので小文字に変換.
                    var assetBundleName = assetInfo.AssetBundle.AssetBundleName.ToLower();

                    // 参照カウントを追加.

                    var allDependencies = assetBundleDependencies.GetAllDependencies(assetBundleName);

                    foreach (var item in allDependencies)
                    {
                        IncrementReferenceCount(item);
                    }

                    IncrementReferenceCount(assetBundleName);

                    // アセットバンドル読み込み.
                    var assetBundle = await LoadAssetBundleWithDependencies(installPath, assetBundleName, null, cancelToken);

                    if (assetBundle != null)
                    {
                        var assetBundleRequest = assetBundle.LoadAssetAsync(assetPath, typeof(T));

                        while (!assetBundleRequest.isDone)
                        {
                            if (cancelToken.IsCancellationRequested) { break; }

                            await UniTask.NextFrame(cancelToken);
                        }

                        result = assetBundleRequest.asset as T;

                        if (result == null)
                        {
                            Debug.LogErrorFormat("[AssetBundle Load Error]\nAssetBundleName = {0}\nAssetPath = {1}\n", assetBundleName, assetPath);
                        }

                        if (autoUnLoad)
                        {
                            UnloadAsset(assetBundleName);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            return result;
        }

        private async UniTask<AssetBundle> LoadAssetBundleWithDependencies(string installPath, string assetBundleName, List<string> internalLoading, CancellationToken cancelToken)
        {
            // 既に読み込み済み.

            var loadedAssetBundle = loadedAssetBundles.GetValueOrDefault(assetBundleName);

            if (loadedAssetBundle != null) { return loadedAssetBundle; }

            // 参照アセットバンドルを再帰読み込み.

            var dependencies = assetBundleDependencies.GetDependencies(assetBundleName);

            if (dependencies != null)
            {
                var dependenciesTasks = new List<UniTask<AssetBundle>>();

                // 相互参照で無限ループに陥るので既に読み込み処理を行った対象を除外.
                if (internalLoading == null)
                {
                    internalLoading = new List<string>();
                }

                foreach (var item in dependencies)
                {
                    if (internalLoading.Contains(item)) { continue; }

                    internalLoading.Add(item);

                    var task = LoadAssetBundleWithDependencies(installPath, item, internalLoading, cancelToken);

                    dependenciesTasks.Add(task);
                }

                await UniTask.WhenAll(dependenciesTasks);
            }

            // アセットバンドルを読み込み.

            var assetBundle = await GetLoadTask(installPath, assetBundleName).ToUniTask(cancellationToken: cancelToken);

            return assetBundle;
        }

        private IObservable<AssetBundle> GetLoadTask(string installPath, string assetBundleName)
        {
            // 既に読み込み済み.

            var loadedAssetBundle = loadedAssetBundles.GetValueOrDefault(assetBundleName);

            if (loadedAssetBundle != null)
            {
                return Observable.Return(loadedAssetBundle);
            }

            // 読み込み中なので共有.

            var loadTask = loadQueueing.GetValueOrDefault(assetBundleName);

            if (loadTask != null)
            {
                return loadTask;
            }

            // 新規で読み込み.

            var info = assetInfosByAssetBundleName.GetValueOrDefault(assetBundleName).FirstOrDefault();

            var task = ObservableEx.FromUniTask(cancelToken => LoadAssetBundle(installPath, info, cancelToken))
                .Timeout(LoadTimeout)
                .OnErrorRetry((TimeoutException ex) => {}, RetryCount, RetryDelaySeconds)
                .OnErrorRetry((FileLoadException ex) => {}, RetryCount, RetryDelaySeconds)
                .DoOnError(error => OnError(error))
                .Finally(() => loadQueueing.Remove(assetBundleName))
                .Share();

            loadQueueing.Add(assetBundleName, task);

            return task;
        }

        private async UniTask<AssetBundle> LoadAssetBundle(string installPath, AssetInfo assetInfo, CancellationToken cancelToken)
        {
            AssetBundle assetBundle = null;

            var filePath = GetFilePath(installPath, assetInfo);

            if (string.IsNullOrEmpty(filePath)) { return null; }

            var assetBundleInfo = assetInfo.AssetBundle;
            var assetBundleName = assetBundleInfo.AssetBundleName;

            #if UNITY_ANDROID

            if (IsLocalMode && filePath.StartsWith(UnityPathUtility.StreamingAssetsPath))
            {
                if (localModeVersionHandler.IsRequireUpdate(assetInfo))
                {
                    try
                    {
                        await AndroidUtility.CopyStreamingToTemporary(filePath, cancelToken);

                        filePath = AndroidUtility.ConvertStreamingAssetsLoadPath(filePath);

                        localModeVersionHandler.OnUpdateLocalFile(assetInfo);
                    }
                    catch (OperationCanceledException)
                    {
                        /* Canceled */
                    }
                }
            }

            #endif

            if (cancelToken.IsCancellationRequested) { return null; }

            var loadedAssetBundle = loadedAssetBundles.GetValueOrDefault(assetBundleName);

            if (loadedAssetBundle != null)
            {
                assetBundle = loadedAssetBundle;
            }
            else
            {
                if (!File.Exists(filePath))
                {
                    var message = $"\nResourcePath: {assetInfo.ResourcePath}\nFilePath:\n{filePath}\n";

                    throw new FileNotFoundException(message);
                }

                byte[] bytes = null;

                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    bytes = new byte[fileStream.Length];

                    if (AsyncLoadFileSize < bytes.Length)
                    {
                        await fileStream.ReadAsync(bytes, 0, bytes.Length, cancelToken);
                    }
                    else
                    {
                        // 同じフレームに同期読み込みできる最大数/最大ファイルサイズを超えた場合次のフレームまで待機.

                        await syncLoadFileCountLimiter.Wait(cancelToken: cancelToken);

                        await syncLoadFileSizeLimiter.Wait((ulong)bytes.Length, cancelToken);

                        if (cancelToken.IsCancellationRequested) { return null; }

                        // 同期処理で読み込む.
                        fileStream.Read(bytes, 0, bytes.Length);
                    }
                }

                if (bytes != null)
                {
                    // 読み込み失敗.

                    if (bytes.Length == 0)
                    {
                        var message = $"\nResourcePath: {assetInfo.ResourcePath}\nFilePath:\n{filePath}\n";

                        throw new FileLoadException(message);
                    }

                    // 復元.

                    bytes = await fileHandler.Decode(bytes);

                    // AssetBundle読み込み.

                    var bundleLoadRequest = AssetBundle.LoadFromMemoryAsync(bytes);

                    while (!bundleLoadRequest.isDone)
                    {
                        if (cancelToken.IsCancellationRequested) { break; }

                        await UniTask.NextFrame(CancellationToken.None);
                    }

                    assetBundle = bundleLoadRequest.assetBundle;

                    if (cancelToken.IsCancellationRequested)
                    {
                        if (assetBundle != null)
                        {
                            assetBundle.Unload(false);
                        }
                        
                        return null;
                    }
                }
            }

            if (assetBundle != null)
            {
                loadedAssetBundles.Add(assetBundleName, assetBundle);

                if (onLoad != null)
                {
                    onLoad.OnNext(filePath);
                }
            }
            else
            {
                UnloadAsset(assetBundleName);

                // ファイルを削除し次回読み込み時に再ダウンロード.
                if (!filePath.StartsWith(UnityPathUtility.StreamingAssetsPath) && DeleteOnLoadError)
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                var builder = new StringBuilder();

                builder.Append($"AssetBundle load error : {assetBundleName}").AppendLine();
                builder.AppendLine();
                builder.AppendFormat("File : {0}", filePath).AppendLine();
                builder.AppendFormat("CRC : {0}", assetBundleInfo.CRC).AppendLine();

                throw new Exception(builder.ToString());
            }

            return assetBundle;
        }

        #endregion

        #region Unload

        /// <summary> 名前で指定したアセットバンドルをメモリから破棄. </summary>
        public void UnloadAsset(string assetBundleName, bool unloadAllLoadedObjects = false, bool force = false)
        {
            if (IsSimulateMode) { return; }

            if (!force && loadQueueing.ContainsKey(assetBundleName)) { return; }

            var dependencies = assetBundleDependencies.GetAllDependencies(assetBundleName);

            foreach (var target in dependencies)
            {
                UnloadAssetBundleInternal(target, unloadAllLoadedObjects, force);
            }

            UnloadAssetBundleInternal(assetBundleName, unloadAllLoadedObjects, force);
        }

        /// <summary> 全てのアセットバンドルをメモリから破棄. </summary>
        public void UnloadAllAsset(bool unloadAllLoadedObjects = false)
        {
            if (IsSimulateMode) { return; }

            var assetBundleNames = loadedAssetBundles.Keys.ToArray();

            foreach (var assetBundleName in assetBundleNames)
            {
                UnloadAsset(assetBundleName, unloadAllLoadedObjects, true);
            }

            loadedAssetBundles.Clear();
        }

        private void UnloadAssetBundleInternal(string assetBundleName, bool unloadAllLoadedObjects, bool force = false)
        {
            if (!force && loadQueueing.ContainsKey(assetBundleName)) { return; }

            var loadedAssetBundle = loadedAssetBundles.GetValueOrDefault(assetBundleName);

            if (loadedAssetBundle == null)
            {
                assetBundleRefCount[assetBundleName] = 0;
                return;
            }

            var referenceCount = DecrementReferenceCount(assetBundleName);

            if (force || referenceCount <= 0)
            {
                loadedAssetBundles.Remove(assetBundleName);

                assetBundleRefCount[assetBundleName] = 0;

                loadedAssetBundle.Unload(unloadAllLoadedObjects);
            }
        }

        #endregion

        public void ClearDownloadQueue()
        {
            downloadRunning.Clear();
            downloadTasks.Clear();
        }

        private void OnTimeout(AssetInfo assetInfo, Exception exception)
        {
            if (onTimeOut != null)
            {
                Debug.LogErrorFormat("[Timeout] {0}", exception);

                onTimeOut.OnNext(assetInfo);
            }
        }

        private void OnError(Exception exception)
        {
            // キャンセルはエラー扱いしない.
            if (exception is OperationCanceledException) { return; }

            if (onError != null)
            {
                Debug.LogErrorFormat("[Error] {0}", exception);

                onError.OnNext(exception);
            }
        }

        /// <summary> 読み込み時イベント. </summary>
        public IObservable<string> OnLoadAsObservable()
        {
            return onLoad ?? (onLoad = new Subject<string>());
        }

        /// <summary> タイムアウト時イベント. </summary>
        public IObservable<AssetInfo> OnTimeOutAsObservable()
        {
            return onTimeOut ?? (onTimeOut = new Subject<AssetInfo>());
        }

        /// <summary> エラー時イベント. </summary>
        public IObservable<Exception> OnErrorAsObservable()
        {
            return onError ?? (onError = new Subject<Exception>());
        }
    }
}

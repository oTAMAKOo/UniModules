
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UniRx;
using Extensions;
using Modules.Devkit;
using Modules.ExternalResource;
using Modules.UniRxExtension;

namespace Modules.AssetBundles
{
    using Debug = UnityEngine.Debug;

    public partial class AssetBundleManager : Singleton<AssetBundleManager>
    {
        //----- params -----

        public const string AssetBundlesFolder = "AssetBundle";
        public const string LibraryFolder = "Library";

        public const string PackageExtension = ".package";

        public const string AesPassword = "QaQaVf7258Whw258";

        private static readonly AesManaged aesManaged = AESExtension.CreateAesManaged(AesPassword);

        // タイムアウトまでの時間.
        private readonly TimeSpan TimeoutLimit = TimeSpan.FromSeconds(60f);

        // リトライする回数.
        private readonly int RetryCount = 3;

        // リトライするまでの時間(秒).
        private readonly TimeSpan RetryDelaySeconds = TimeSpan.FromSeconds(2f);

        private class CachedInfo
        {
            public AssetBundle assetBundle = null;
            public int referencedCount = 0;

            public CachedInfo(AssetBundle assetBundle)
            {
                this.assetBundle = assetBundle;
                this.referencedCount = 1;
            }
        }

        //----- field -----

        // アセット管理.
        private AssetInfoManifest manifest = null;

        // ダウンロード元URL.
        private string remoteUrl = null;

        // ダウンロード待ちアセットバンドル.
        private Dictionary<string, IObservable<string>> downloadQueueing = null;

        // 読み込み済みアセットバンドル.
        private Dictionary<string, CachedInfo> assetBundleCache = null;

        // 読み込み待ちアセットバンドル.
        private Dictionary<string, IObservable<Tuple<AssetBundle, string>>> cacheQueueing = null;

        // ダウンロードエラー一覧.
        private Dictionary<string, string> downloadingErrors = null;

        // 依存関係.
        private Dictionary<string, string[]> dependencies = null;

        // ダウンロードキャンセル.
        private YieldCancell yieldCancell = null;

        // シュミュレートモードか.
        private bool simulateMode = false;

        // イベント通知.
        private Subject<string> onTimeOut = null;
        private Subject<Exception> onError = null;

        private bool isInitialized = false;

        //----- property -----

        //----- method -----

        /// <summary>
        /// 初期設定をします。
        /// Initializeで設定した値はstatic変数として保存されます。
        /// </summary>
        /// <param name="simulateMode">AssetDataBaseからアセットを取得(EditorOnly)</param>
        public void Initialize(bool simulateMode = false)
        {
            if (isInitialized) { return; }
            
            this.simulateMode = Application.isEditor && simulateMode;

            downloadQueueing = new Dictionary<string, IObservable<string>>();
            assetBundleCache = new Dictionary<string, CachedInfo>();
            cacheQueueing = new Dictionary<string, IObservable<Tuple<AssetBundle, string>>>();
            downloadingErrors = new Dictionary<string, string>();
            dependencies = new Dictionary<string, string[]>();

            isInitialized = true;
        }

        /// <summary>
        /// Coroutine中断用のクラスを登録.
        /// </summary>
        public void RegisterYieldCancell(YieldCancell yieldCancell)
        {
            this.yieldCancell = yieldCancell;
        }

        /// <summary>
        /// URLを設定.
        /// </summary>
        /// <param name="remoteUrl">アセットバンドルのディレクトリURLを指定</param>
        public void SetUrl(string remoteUrl)
        {
            this.remoteUrl = remoteUrl;
        }

        public void SetManifest(AssetInfoManifest manifest)
        {
            this.manifest = manifest;

            CleanUnuseCache();
        }

        /// <summary> 展開中のアセットバンドル名一覧取得 </summary>
        public Tuple<string, int>[] GetLoadedAssetBundleNames()
        {
            return assetBundleCache != null ?
                assetBundleCache.Select(x => Tuple.Create(x.Key, x.Value.referencedCount)).ToArray() : 
                new Tuple<string, int>[0];
        }

        public string BuildUrl(string assetBundlePath)
        {
            var platformName = UnityPathUtility.GetPlatformName();
            var assetFolder = AssetBundlesFolder;

            return PathUtility.Combine(new string[] { remoteUrl, platformName, assetFolder, assetBundlePath }) + PackageExtension;
        }

        public string BuildFilePath(string assetBundleName)
        {
            var installPath = UnityPathUtility.GetInstallPath();
            var assetFolder = AssetBundlesFolder;

            var path = PathUtility.Combine(installPath, assetFolder);

            if (!string.IsNullOrEmpty(assetBundleName))
            {
                path = PathUtility.Combine(path, assetBundleName) + PackageExtension;
            }

            return path;
        }

        #region Download

        private IEnumerator ReadyForDownload()
        {
            // wait network disconnect.
            while (Application.internetReachability == NetworkReachability.NotReachable)
            {
                yield return null;
            }
        }

        /// <summary>
        /// アセットバンドルを更新.
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public IObservable<Unit> UpdateAssetBundle(string assetBundleName, IProgress<float> progress = null)
        {
            if (simulateMode) { return Observable.ReturnUnit(); }

            return Observable.FromMicroCoroutine(() => UpdateAssetBundleInternal(assetBundleName, progress));
        }

        private IEnumerator UpdateAssetBundleInternal(string assetBundleName, IProgress<float> progress = null)
        {
            //----------------------------------------------------------------------------------
            // ※ 呼び出し頻度が高いのでFromMicroCoroutineで呼び出される.
            //    戻り値を返す時はyield return null以外使わない.
            //----------------------------------------------------------------------------------

            if (!assetBundleCache.ContainsKey(assetBundleName))
            {
                var allBundles = new string[] { assetBundleName }.Concat(GetDependencies(assetBundleName)).ToArray();

                var loadAllBundles = allBundles
                    .Select(x =>
                        {
                            var cached = assetBundleCache.GetValueOrDefault(x);

                            if (cached != null)
                            {
                                UnloadAsset(x, true);
                            }

                            var downloadTask = downloadQueueing.GetValueOrDefault(x);

                            if (downloadTask != null)
                            {
                                return downloadTask;
                            }

                            downloadQueueing[x] = Observable.FromMicroCoroutine(() => DownloadAssetBundle(x, progress))
                                    .Select(_ => x)
                                    .Share();

                            return downloadQueueing[x];
                        })
                    .ToArray();

                var loadAssetYield = Observable.WhenAll(loadAllBundles)
                    .Do(xs =>
                        {
                            foreach (var item in xs)
                            {
                                if (downloadQueueing.ContainsKey(item))
                                {
                                    downloadQueueing.Remove(item);
                                }
                            }
                        })
                    .ToYieldInstruction(false, yieldCancell.Token);

                while (!loadAssetYield.IsDone)
                {
                    yield return null;
                }
            }
        }

        private IEnumerator FileDownload(string url, string path, IProgress<float> progress = null)
        {
            var webRequest = new UnityWebRequest(url);
            var buffer = new byte[256 * 1024];

            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            webRequest.downloadHandler = new AssetBundleDownloadHandler(path, buffer);

            webRequest.SendWebRequest();

            while (!webRequest.isDone)
            {
                if (progress != null)
                {
                    progress.Report(webRequest.downloadProgress);
                }

                yield return null;
            }
        }

        private IEnumerator DownloadAssetBundle(string assetBundleName, IProgress<float> progress = null)
        {
            //----------------------------------------------------------------------------------
            // ※ 呼び出し頻度が高いのでFromMicroCoroutineで呼び出される.
            //    戻り値を返す時はyield return null以外使わない.
            //----------------------------------------------------------------------------------
            
            var url = BuildUrl(assetBundleName);
            var filePath = BuildFilePath(assetBundleName);

            var downloader = Observable.Defer(() => Observable.FromMicroCoroutine(() => FileDownload(url, filePath, progress)));

            var downloadYield = downloader
                    .Timeout(TimeoutLimit)
                    .OnErrorRetry((TimeoutException ex) => OnTimeout(url, ex), RetryCount, RetryDelaySeconds)
                    .DoOnError(x => OnError(x))
                    .ToYieldInstruction(false, yieldCancell.Token);

            while (!downloadYield.IsDone)
            {
                yield return null;
            }
        }

        #endregion

        #region Dependencies

        public void SetDependencies(Dictionary<string, string[]> dependencies)
        {
            this.dependencies = dependencies;
        }

        private string[] GetDependencies(string assetBundleName)
        {
            // 既に登録済みの場合はそこから取得.
            var dependent = dependencies.GetValueOrDefault(assetBundleName);

            if (dependent != null)
            {
                return dependent;
            }

            // 依存アセット一覧を再帰で取得.
            dependent = GetDependenciesInternal(assetBundleName);

            // 登録.
            if (dependent.Any())
            {
                dependencies.Add(assetBundleName, dependent);
            }

            return dependent;
        }

        /// <summary>
        /// 依存関係にあるアセット一覧取得.
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="dependents"></param>
        /// <returns></returns>
        private string[] GetDependenciesInternal(string assetBundleName, List<string> dependents = null)
        {
            var targets = dependencies.GetValueOrDefault(assetBundleName, new string[0]);

            if (targets.Length == 0) { return new string[0]; }

            if (dependents == null)
            {
                dependents = new List<string>();
            }

            for (var i = 0; i < targets.Length; i++)
            {
                // 既に列挙済みの場合は処理しない.
                if (dependents.Contains(targets[i])) { continue; }

                dependents.Add(targets[i]);

                // 依存先の依存先を取得.
                var internalDependents = GetDependenciesInternal(targets[i], dependents);

                foreach (var internalDependent in internalDependents)
                {
                    // 既に列挙済みの場合は追加しない.
                    if (!dependents.Contains(internalDependent))
                    {
                        dependents.Add(internalDependent);
                    }
                }
            }

            return dependents.ToArray();
        }

        #endregion

        #region Load

        /// <summary>
        /// 名前で指定したアセットを取得.
        /// </summary>
        public IObservable<T> LoadAsset<T>(string assetBundleName, string assetPath, bool autoUnLoad = true) where T : UnityEngine.Object
        {
            // コンポーネントを取得する場合はGameObjectから取得.
            if (typeof(T).IsSubclassOf(typeof(Component)))
            {
                return Observable.FromMicroCoroutine<GameObject>(observer => LoadAssetInternal(observer, assetBundleName, assetPath, autoUnLoad))
                    .Select(x => x != null ? x.GetComponent<T>() : null);                   
            }

            return Observable.FromMicroCoroutine<T>(observer => LoadAssetInternal(observer, assetBundleName, assetPath, autoUnLoad));
        }

        private IEnumerator LoadAssetInternal<T>(IObserver<T> observer, string assetBundleName, string assetPath, bool autoUnLoad) where T : UnityEngine.Object
        {
            //----------------------------------------------------------------------------------
            // ※ 呼び出し頻度が高いのでFromMicroCoroutineで呼び出される.
            //    戻り値を返す時はyield return null以外使わない.
            //----------------------------------------------------------------------------------

            T result = null;

            #if UNITY_EDITOR

            if (simulateMode)
            {
                var loadYield = SimulateLoadAsset<T>(assetPath).ToYieldInstruction(false, yieldCancell.Token);

                while (!loadYield.IsDone)
                {
                    yield return null;
                }

                if (loadYield.HasResult && !loadYield.HasError && !loadYield.IsCanceled)
                {
                    result = loadYield.Result;
                }
            }
            else

            #endif

            {
                IObservable<AssetBundle> loader = null;

                // アセットバンドル名は小文字なので小文字に変換.
                assetBundleName = assetBundleName.ToLower();

                var cache = assetBundleCache.GetValueOrDefault(assetBundleName);

                if (cache == null)
                {
                    var allBundles = new string[] { assetBundleName }.Concat(GetDependencies(assetBundleName)).ToArray();

                    var loadAllBundles = allBundles
                        .Select(x =>
                            {
                                var cached = assetBundleCache.GetValueOrDefault(x);

                                if (cached != null)
                                {
                                    return Observable.Return(Tuple.Create(cached.assetBundle, x));
                                }

                                var loadTask = cacheQueueing.GetValueOrDefault(x);

                                if (loadTask != null)
                                {
                                    return loadTask;
                                }

                                cacheQueueing[x] = Observable.FromMicroCoroutine<AssetBundle>(_observer => LoadAssetBundleFromCache(_observer, assetBundleName))
                                    .Timeout(TimeoutLimit)
                                    .OnErrorRetry((TimeoutException ex) => OnTimeout(assetBundleName, ex), RetryCount, RetryDelaySeconds)
                                    .DoOnError(error => OnError(error))
                                    .Select(y => Tuple.Create(y, x))
                                    .Share();

                                return cacheQueueing[x];
                            })
                        .ToArray();

                    loader = Observable.Defer(() => Observable.WhenAll(loadAllBundles)
                        .Select(xs =>
                        {
                            foreach (var item in xs)
                            {
                                assetBundleCache[item.Item2] = new CachedInfo(item.Item1);

                                if (cacheQueueing.ContainsKey(item.Item2))
                                {
                                    cacheQueueing.Remove(item.Item2);
                                }
                            }

                            return xs[0].Item1;
                        }));
                }
                else
                {
                    cache.referencedCount++;
                    loader = Observable.Defer(() => Observable.Return(cache.assetBundle));
                }

                var loadYield = loader
                    .SelectMany(x => x.LoadAssetAsync(assetPath, typeof(T)).AsAsyncOperationObservable())
                    .Select(x =>
                        {
                            var asset = x.asset as T;

                            if (asset == null)
                            {
                                Debug.LogErrorFormat("[AssetBundle Load Error]\nAssetBundleName = {0}\nAssetPath = {1}", assetBundleName, assetPath);
                            }

                            if (autoUnLoad)
                            {
                                UnloadAsset(assetBundleName);
                            }

                            return asset;
                        })
                    .ToYieldInstruction();

                while (!loadYield.IsDone)
                {
                    yield return null;
                }

                if (loadYield.HasResult && !loadYield.HasError && !loadYield.IsCanceled)
                {
                    result = loadYield.Result;
                }
            }

            observer.OnNext(result);
            observer.OnCompleted();
        }

        private IEnumerator LoadAssetBundleFromCache(IObserver<AssetBundle> observer, string assetBundleName)
        {
            AssetBundle assetBundle = null;

            var filePath = BuildFilePath(assetBundleName);

            Func<string, AesManaged, byte[]> loadCacheFile = (_filePath, _aesManaged) =>
            {
                // ファイル読み込み.
                var data = File.ReadAllBytes(_filePath);

                // 復号化.               
                return data.Decrypt(_aesManaged);
            };

            // ファイルの読み込みと復号化をスレッドプールで実行.
            var loadYield = Observable.Start(() => loadCacheFile(filePath, aesManaged)).ObserveOnMainThread().ToYieldInstruction();

            while (!loadYield.IsDone)
            {
                yield return null;
            }

            if (loadYield.HasResult)
            {
                var bytes = loadYield.Result;

                var bundleLoadRequest = AssetBundle.LoadFromMemoryAsync(bytes);

                while (!bundleLoadRequest.isDone)
                {
                    yield return null;
                }

                assetBundle = bundleLoadRequest.assetBundle;

                if (assetBundle == null)
                {
                    Debug.LogErrorFormat("Failed to load AssetBundle!\nAssetBundleName : {0}", assetBundleName);
                }
            }
            else
            {
                var message = string.Format("Failed to load AssetBundle!\nFile : {0}\nAssetBundleName : {1}", filePath, assetBundleName);

                throw new Exception(message);
            }

            observer.OnNext(assetBundle);
            observer.OnCompleted();
        }

        #endregion

        #region Unload

        /// <summary>
        /// 名前で指定したアセットバンドルをメモリから破棄.
        /// </summary>
        public void UnloadAsset(string assetBundleName, bool unloadAllLoadedObjects = false, bool force = false)
        {
            if (simulateMode) { return; }

            UnloadDependencies(assetBundleName, unloadAllLoadedObjects, force);
            UnloadAssetBundleInternal(assetBundleName, unloadAllLoadedObjects, force);
        }

        /// <summary>
        /// 全てのアセットバンドルをメモリから破棄.
        /// </summary>
        public void UnloadAllAsset(bool unloadAllLoadedObjects = false)
        {
            if (simulateMode) { return; }

            var assetBundleNames = assetBundleCache.Keys.ToArray();

            foreach (var assetBundleName in assetBundleNames)
            {
                UnloadAsset(assetBundleName, unloadAllLoadedObjects, true);
            }

            assetBundleCache.Clear();
        }

        private void UnloadDependencies(string assetBundleName, bool unloadAllLoadedObjects, bool force = false)
        {
            var targets = dependencies.GetValueOrDefault(assetBundleName);

            if (targets == null) { return; }

            foreach (var target in targets)
            {
                UnloadDependencies(target, unloadAllLoadedObjects, force);
                UnloadAssetBundleInternal(target, unloadAllLoadedObjects, force);
            }

            dependencies.Remove(assetBundleName);
        }

        private void UnloadAssetBundleInternal(string assetBundleName, bool unloadAllLoadedObjects, bool force = false)
        {
            var info = GetCachedInfo(assetBundleName);

            if (info == null) { return; }

            info.referencedCount--;

            if (force || info.referencedCount <= 0)
            {
                if (info.assetBundle != null)
                {
                    info.assetBundle.Unload(unloadAllLoadedObjects);
                    info.assetBundle = null;
                }

                assetBundleCache.Remove(assetBundleName);
            }
        }

        #endregion

        /// <summary>
        /// 全てのキャッシュを破棄.
        /// </summary>
        public static void CleanCache()
        {
            var installDir = Instance.BuildFilePath(null);

            if (Directory.Exists(installDir))
            {
                DirectoryUtility.Clean(installDir);

                // 一旦削除するので再度生成.
                Directory.CreateDirectory(installDir);
            }
        }

        /// <summary>
        /// マニフェストファイルに存在しないキャッシュファイルを破棄.
        /// </summary>
        private void CleanUnuseCache()
        {
            if (simulateMode) { return; }

            if (manifest == null) { return; }

            var installDir = BuildFilePath(null);

            if (string.IsNullOrEmpty(installDir)) { return; }

            if (!Directory.Exists(installDir)) { return; }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var builder = new StringBuilder();

            var directory = Path.GetDirectoryName(installDir);

            if (Directory.Exists(directory))
            {
                var cacheFiles = Directory.GetFiles(installDir, "*", SearchOption.AllDirectories);

                var managedFiles = manifest.GetAssetInfos()
                    .Select(x => BuildFilePath(x.AssetBundle.AssetBundleName))
                    .Select(x => PathUtility.ConvertPathSeparator(x))
                    .Distinct()
                    .ToHashSet();

                // 管理情報は必ず管理対象.
                managedFiles.Add(BuildFilePath(AssetInfoManifest.AssetBundleName));

                var targets = cacheFiles
                    .Select(x => PathUtility.ConvertPathSeparator(x))
                    .Where(x => !managedFiles.Contains(x))
                    .ToArray();

                foreach (var target in targets)
                {
                    if (!File.Exists(target)) { continue; }

                    File.SetAttributes(target, FileAttributes.Normal);
                    File.Delete(target);

                    builder.AppendLine(target);
                }

                var deleteDirectorys = DirectoryUtility.DeleteEmpty(installDir);

                deleteDirectorys.ForEach(x => builder.AppendLine(x));

                sw.Stop();

                var log = builder.ToString();

                if (!string.IsNullOrEmpty(log))
                {
                    UnityConsole.Info("Delete unuse cached assetbundles ({0}ms)\n{1}", sw.Elapsed.TotalMilliseconds, log);
                }
            }
        }

        /// <summary>
        /// キャッシュ済みのアセット情報を取得.
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        private CachedInfo GetCachedInfo(string assetBundleName)
        {
            var info = assetBundleCache.GetValueOrDefault(assetBundleName);

            if (info == null) { return null; }

            // 依存関係のアセットがない場合.
            var dependent = dependencies.GetValueOrDefault(assetBundleName);

            if (dependent == null) { return info; }

            // 依存関係のアセットが読み込み済みでない場合は失敗扱い.
            foreach (var item in dependent)
            {
                var error = downloadingErrors.GetValueOrDefault(assetBundleName);

                if (error != null)
                {
                    Debug.LogErrorFormat("[Download Error] {0} dependent from {1}\n{2}", item, assetBundleName, error);
                    return info;
                }

                var dependentBundle = assetBundleCache.GetValueOrDefault(item);

                if (dependentBundle == null)
                {
                    return null;
                }
            }

            return info;
        }

        private void OnTimeout(string url, Exception exception)
        {
            Debug.LogErrorFormat("[Download Timeout] \n{0}", exception);

            if (onTimeOut != null)
            {
                onTimeOut.OnNext(url);
            }
        }

        private void OnError(Exception exception)
        {
            Debug.LogErrorFormat("[Download Error] \n{0}", exception);

            if (onError != null)
            {
                onError.OnNext(exception);
            }
        }

        /// <summary>
        /// タイムアウト時のイベント.
        /// </summary>
        /// <returns></returns>
        public IObservable<string> OnTimeOutAsObservable()
        {
            return onTimeOut ?? (onTimeOut = new Subject<string>());
        }

        /// <summary>
        /// エラー時のイベント.
        /// </summary>
        public IObservable<Exception> OnErrorAsObservable()
        {
            return onError ?? (onError = new Subject<Exception>());
        }
    }
}

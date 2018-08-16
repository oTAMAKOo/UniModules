
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.UniRxExtension;

namespace Modules.AssetBundles
{
    public partial class AssetBundleManager : Singleton<AssetBundleManager>
    {
        //----- params -----

        public const string AssetBundlesFolder = "AssetBundle";
        
        public const string ManifestFileExtension = ".manifest";

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

        // ダウンロード元URL.
        private string remoteUrl = null;

        // 読み分け用.
        private string[] variants = { };

        // マニフェストファイル.
        private AssetBundleManifest assetBundleManifest = null;

        // 読み込み済みアセットバンドル.
        private Dictionary<string, CachedInfo> assetBundleCache = null;

        // 読み込み待ちアセットバンドル.
        private Dictionary<string, IObservable<UniRx.Tuple<AssetBundle, string>>> cacheQueueing = null;

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

        public string DownloadURL { get { return remoteUrl; } }

        //----- method -----

        /// <summary>
        /// 初期設定をします。
        /// Initializeで設定した値はstatic変数として保存されます。
        /// </summary>
        /// <param name="variants">アセットバンドルのバリアント</param>
        /// <param name="simulateMode">AssetDataBaseからアセットを取得(EditorOnly)</param>
        public void Initialize(string[] variants = null, bool simulateMode = false)
        {
            if (isInitialized) { return; }

            this.variants = variants;
            this.simulateMode = Application.isEditor && simulateMode;

            assetBundleCache = new Dictionary<string, CachedInfo>();
            cacheQueueing = new Dictionary<string, IObservable<UniRx.Tuple<AssetBundle, string>>>();
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

        /// <summary> 展開中のアセットバンドル名一覧取得 </summary>
        public UniRx.Tuple<string, int>[] GetLoadedAssetBundleNames()
        {
            return assetBundleCache != null ?
                assetBundleCache.Select(x => UniRx.Tuple.Create(x.Key, x.Value.referencedCount)).ToArray() : 
                new UniRx.Tuple<string, int>[0];
        }

        private string BuildUrl(string assetBundlePath)
        {
            return PathUtility.Combine(new string[] { remoteUrl, UnityPathUtility.GetPlatformName(), AssetBundlesFolder, assetBundlePath });
        }

        #region Download

        /// <summary>
        /// マニフェストファイルを更新.
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        public IObservable<Unit> UpdateManifest(UniRx.IProgress<float> progress = null)
        {
            if (simulateMode) { return Observable.ReturnUnit(); }

            return Observable.FromCoroutine(() => UpdateManifestInternal(progress));
        }

        private IEnumerator UpdateManifestInternal(UniRx.IProgress<float> progress = null)
        {
            // マニフェストファイルはこの名前でないとロード出来ない...
            const string ManifestLoadName = "AssetBundleManifest";

            // アセットバンドルの生成フォルダ名でマニフェストファイルは作成される.
            var manifestName = AssetBundlesFolder;

            var url = BuildUrl(manifestName);

            var error = string.Empty;

            yield return ReadyForDownload().ToYieldInstruction(false);

            //------ 更新 ------

            AssetBundle assetBundle = null;

            //  UniRxのバグでGetWWWはToYieldInstructionで結果が受け取れないのでDoで結果を退避.
            var updateYield = ObservableWWW.GetWWW(url, progress: progress)
                        .Timeout(TimeoutLimit)
                        .OnErrorRetry((TimeoutException ex) => OnTimeout(url, ex), RetryCount, RetryDelaySeconds)
                        .DoOnError(x => OnError(x))
                        .Do(x =>
                            {
                                assetBundle = x.assetBundle;
                                error = x.error;
                            })
                        .ToYieldInstruction(false);

            yield return updateYield;

            if (!string.IsNullOrEmpty(error) || assetBundle == null)
            {
                OnError(new Exception("AssetBundleManifest update error."));
                yield break;
            }

            //------ 読み込み ------

            var loadYield = assetBundle.LoadAssetAsync(ManifestLoadName)
                .AsAsyncOperationObservable()
                .Timeout(TimeoutLimit)
                .OnErrorRetry((TimeoutException ex) => OnTimeout(url, ex), RetryCount, RetryDelaySeconds)
                .DoOnError(x => OnError(x))
                .Do(x =>
                    {
                        assetBundleManifest = x.asset as AssetBundleManifest;
                        assetBundle.Unload(false);
                    })
                .ToYieldInstruction(false);

            yield return loadYield;

            if (loadYield.HasError)
            {
                yield break;
            }

            if (assetBundleManifest == null)
            {
                OnError(new Exception("AssetBundleManifest load error."));
                yield break;
            }
        }

        private IEnumerator ReadyForDownload()
        {
            // wait network disconnect.
            while (Application.internetReachability == NetworkReachability.NotReachable)
            {
                yield return null;
            }

            // wait cache.
            while (!Caching.ready)
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
        public IObservable<Unit> UpdateAssetBundle(string assetBundleName, UniRx.IProgress<float> progress = null)
        {
            if (simulateMode) { return Observable.ReturnUnit(); }

            return Observable.FromMicroCoroutine(() => UpdateAssetBundleInternal(assetBundleName, progress));
        }


        private IEnumerator UpdateAssetBundleInternal(string assetBundleName, UniRx.IProgress<float> progress = null)
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

                            var loadTask = cacheQueueing.GetValueOrDefault(x);

                            if (loadTask != null)
                            {
                                return loadTask;
                            }

                            cacheQueueing[x] = Observable.FromMicroCoroutine<AssetBundle>(observer => DownloadAssetBundle(observer, x, progress))
                                    .Select(y => UniRx.Tuple.Create(y, x))
                                    .Share();

                            return cacheQueueing[x];
                        })
                    .ToArray();

                var loadAssetYield = Observable.WhenAll(loadAllBundles)
                    .Do(xs =>
                        {
                            foreach (var item in xs)
                            {
                                if (item.Item1 != null)
                                {
                                    item.Item1.Unload(true);
                                }

                                if (cacheQueueing.ContainsKey(item.Item2))
                                {
                                    cacheQueueing.Remove(item.Item2);
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

        private IEnumerator DownloadAssetBundle(IObserver<AssetBundle> observer, string assetBundleName, UniRx.IProgress<float> progress = null)
        {
            //----------------------------------------------------------------------------------
            // ※ 呼び出し頻度が高いのでFromMicroCoroutineで呼び出される.
            //    戻り値を返す時はyield return null以外使わない.
            //----------------------------------------------------------------------------------

            AssetBundle result = null;

            var url = RemapVariantName(BuildUrl(assetBundleName));
            var hash = assetBundleManifest.GetAssetBundleHash(assetBundleName);

            string error = null;

            //------ Manifest ------

            string manifest = null;

            //  UniRxのバグでGetWWWはToYieldInstructionで結果が受け取れないのでDoで結果を退避.
            var manifestGetYield = ObservableWWW.GetWWW(url + ManifestFileExtension)
                .Timeout(TimeoutLimit)
                .OnErrorRetry((TimeoutException ex) => OnTimeout(url, ex), RetryCount, RetryDelaySeconds)
                .DoOnError(x => OnError(x))
                .Do(x =>
                    {
                        error = x.error;
                        manifest = x.text;
                    })
                .ToYieldInstruction(false, yieldCancell.Token);

            while (!manifestGetYield.IsDone)
            {
                yield return null;
            }

            //------ Download ------

            if (!manifestGetYield.IsCanceled && !manifestGetYield.HasError && !string.IsNullOrEmpty(error))
            {
                IObservable<AssetBundle> loader = null;

                // manifestが存在していた場合はCRCチェック.
                if (!string.IsNullOrEmpty(manifest))
                {
                    // manifest内部のCRCコードを抽出.
                    var lines = manifest.Split(new string[] { "CRC: " }, StringSplitOptions.None);
                    var crc = uint.Parse(lines[1].Split(new string[] { "\n" }, StringSplitOptions.None)[0]);

                    loader = Observable.Defer(() => ObservableWWW.LoadFromCacheOrDownload(url, hash, crc, progress));
                }
                else
                {
                    loader = Observable.Defer(() => ObservableWWW.LoadFromCacheOrDownload(url, hash, progress));
                }

                var loadYield = loader
                        .Timeout(TimeoutLimit)
                        .OnErrorRetry((TimeoutException ex) => OnTimeout(url, ex), RetryCount, RetryDelaySeconds)
                        .DoOnError(x => OnError(x))
                        .ToYieldInstruction(false, yieldCancell.Token);

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

        #endregion

        #region Dependencies

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
            var targets = assetBundleManifest.GetAllDependencies(assetBundleName);

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
        public IObservable<T> LoadAsset<T>(string assetBundleName, string assetPath, bool autoUnLoad = true, UniRx.IProgress<float> progress = null) where T : UnityEngine.Object
        {
            // コンポーネントを取得する場合はGameObjectから取得.
            if (typeof(T).IsSubclassOf(typeof(Component)))
            {
                return Observable.FromMicroCoroutine<GameObject>(observer => LoadAssetInternal(observer, assetBundleName, assetPath, autoUnLoad, progress))
                    .Select(x => x != null ? x.GetComponent<T>() : null);
            }

            return Observable.FromMicroCoroutine<T>(observer => LoadAssetInternal(observer, assetBundleName, assetPath, autoUnLoad, progress));
        }

        private IEnumerator LoadAssetInternal<T>(IObserver<T> observer, string assetBundleName, string assetPath, bool autoUnLoad, UniRx.IProgress<float> progress) where T : UnityEngine.Object
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
                                    return Observable.Return(UniRx.Tuple.Create(cached.assetBundle, x));
                                }

                                var loadTask = cacheQueueing.GetValueOrDefault(x);

                                if (loadTask != null)
                                {
                                    return loadTask;
                                }

                                var url = RemapVariantName(BuildUrl(x));
                                var hash = assetBundleManifest.GetAssetBundleHash(x);

                                cacheQueueing[x] = ObservableWWW.LoadFromCacheOrDownload(url, hash, progress)
                                    .Timeout(TimeoutLimit)
                                    .OnErrorRetry((TimeoutException ex) => OnTimeout(url, ex), RetryCount, RetryDelaySeconds)
                                    .DoOnError(error => OnError(error))
                                    .Select(y => UniRx.Tuple.Create(y, x))
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

            if (progress != null)
            {
                progress.Report(1f);
            }

            observer.OnNext(result);
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
        /// variantを適用したアセットバンドル名に変換.
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        private string RemapVariantName(string assetBundleName)
        {
            var bundlesWithVariant = assetBundleManifest.GetAllAssetBundlesWithVariant();

            // If the asset bundle doesn't have variant, simply return.
            if (Array.IndexOf(bundlesWithVariant, assetBundleName) < 0)
            {
                return assetBundleName;
            }

            var split = assetBundleName.Split('.');

            var bestFit = int.MaxValue;
            var bestFitIndex = -1;

            // Loop all the assetBundles with variant to find the best fit variant assetBundle.
            for (var i = 0; i < bundlesWithVariant.Length; i++)
            {
                var curSplit = bundlesWithVariant[i].Split('.');

                if (curSplit[0] != split[0]) { continue; }

                var found = Array.IndexOf(variants, curSplit[1]);

                if (found != -1 && found < bestFit)
                {
                    bestFit = found;
                    bestFitIndex = i;
                }
            }

            if (bestFitIndex != -1)
            {
                return bundlesWithVariant[bestFitIndex];
            }

            return assetBundleName;
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
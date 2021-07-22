
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UniRx;
using Extensions;
using Modules.Devkit.Console;
using Modules.ExternalResource;
using Modules.UniRxExtension;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Modules.AssetBundles
{
    public sealed partial class AssetBundleManager : Singleton<AssetBundleManager>
    {
        //----- params -----

        public const string PackageExtension = ".package";

        // タイムアウトまでの時間.
        private readonly TimeSpan TimeoutLimit = TimeSpan.FromSeconds(60f);

        // リトライする回数.
        private readonly int RetryCount = 3;

        // リトライするまでの時間(秒).
        private readonly TimeSpan RetryDelaySeconds = TimeSpan.FromSeconds(2f);

        private sealed class CachedInfo
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

        // 同時ダウンロード数.
        private uint maxDownloadCount = 0;

        // アセット管理.
        private AssetInfoManifest manifest = null;

        // ダウンロード先パス.
        private string installPath = null;

        // ダウンロード元URL.
        private string remoteUrl = null;

        // ダウンロード中アセットバンドル.
        private HashSet<string> downloadList = null;

        // ダウンロード待ちアセットバンドル.
        private Dictionary<string, IObservable<string>> downloadQueueing = null;

        // 読み込み済みアセットバンドル.
        private Dictionary<string, CachedInfo> assetBundleCache = null;

        // 読み込み待ちアセットバンドル.
        private Dictionary<string, IObservable<Tuple<AssetBundle, string>>> cacheQueueing = null;

        // ダウンロードエラー一覧.
        private Dictionary<string, string> downloadingErrors = null;

        // アセット情報(アセットバンドル).
        private Dictionary<string, AssetInfo[]> assetInfosByAssetBundleName = null;

        // 依存関係.
        private Dictionary<string, string[]> dependencies = null;

        // ダウンロードキャンセル.
        private YieldCancel yieldCancel = null;

        // シュミュレートモードか.
        private bool simulateMode = false;

        // ローカルモードか.
        private bool localMode = false;

        // イベント通知.
        private Subject<AssetInfo> onTimeOut = null;
        private Subject<Exception> onError = null;

        private AesCryptoKey aesCryptoKey = null;

        private bool isInitialized = false;

        //----- property -----

        //----- method -----

        private AssetBundleManager() { }

        /// <summary>
        /// 初期設定をします。
        /// Initializeで設定した値はstatic変数として保存されます。
        /// </summary>
        /// <param name="maxDownloadCount">同時ダウンロード数</param>
        /// <param name="simulateMode">AssetDataBaseからアセットを取得(EditorOnly)</param>
        /// <param name="cryptoKey">暗号化キー(Key,IVがModules.ExternalResource.Editor.ManageConfigのAssetのCryptKeyと一致している必要があります.)</param>
        public void Initialize(uint maxDownloadCount, bool simulateMode = false, AesCryptoKey cryptoKey = null)
        {
            if (isInitialized) { return; }

            this.maxDownloadCount = maxDownloadCount;
            this.simulateMode = Application.isEditor && simulateMode;

            downloadList = new HashSet<string>();
            downloadQueueing = new Dictionary<string, IObservable<string>>();
            assetBundleCache = new Dictionary<string, CachedInfo>();
            cacheQueueing = new Dictionary<string, IObservable<Tuple<AssetBundle, string>>>();
            downloadingErrors = new Dictionary<string, string>();
            assetInfosByAssetBundleName = new Dictionary<string, AssetInfo[]>();
            dependencies = new Dictionary<string, string[]>();

            BuildAssetInfoTable();

            isInitialized = true;
        }

        /// <summary> ローカルモード設定.
        /// <see cref="installPath"/>のファイルからアセットを取得
        /// </summary>
        public void SetLocalMode(bool localMode)
        {
            this.localMode = localMode;
        }

        /// <summary> 保存先ディレクトリ設定. </summary>
        public void SetInstallDirectory(string installDirectory)
        {
            installPath = installDirectory;
        }

        /// <summary>
        /// 暗号化キー設定.
        /// Key,IVがModules.ExternalResource.Editor.ManageConfigのAssetのCryptKeyと一致している必要があります.
        /// </summary>
        /// <param name="key">暗号化Key(32文字)</param>
        /// <param name="iv">暗号化IV(16文字)</param>
        public void SetCryptoKey(string key, string iv)
        {
            this.aesCryptoKey = new AesCryptoKey(key, iv);
        }

        /// <summary>
        /// 暗号化キー取得.
        /// </summary>
        public AesCryptoKey GetCryptoKey()
        {
            if (aesCryptoKey == null)
            {
                aesCryptoKey = new AesCryptoKey("MprvQauVXRXUvC532oe861pPVTy5UtFK", "rw6mpYs7jsSgfVEV");
            }

            return aesCryptoKey;
        }

        /// <summary>
        /// コルーチン中断用のクラスを登録.
        /// </summary>
        public void RegisterYieldCancel(YieldCancel yieldCancel)
        {
            this.yieldCancel = yieldCancel;
        }

        /// <summary>
        /// URLを設定.
        /// </summary>
        /// <param name="remoteUrl">アセットバンドルのディレクトリURLを指定</param>
        public void SetUrl(string remoteUrl)
        {
            this.remoteUrl = remoteUrl;
        }

        private void BuildAssetInfoTable()
        {
            assetInfosByAssetBundleName.Clear();
            
            if (manifest != null)
            {
                assetInfosByAssetBundleName = manifest.GetAssetInfos()
                    .Where(x => x.IsAssetBundle)
                    .GroupBy(x => x.AssetBundle.AssetBundleName)
                    .ToDictionary(x => x.Key, x => x.ToArray());
            }

            // ※ アセット管理情報内にAssetInfoManifestの情報は入っていないので明示的に追加する.

            var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();
            
            assetInfosByAssetBundleName[manifestAssetInfo.AssetBundle.AssetBundleName] = new AssetInfo[] { manifestAssetInfo };
        }

        public void SetManifest(AssetInfoManifest manifest)
        {
            this.manifest = manifest;

            BuildAssetInfoTable();

            if (manifest != null)
            {
                CleanUnUseCache();
            }
        }

        /// <summary> 展開中のアセットバンドル名一覧取得 </summary>
        public Tuple<string, int>[] GetLoadedAssetBundleNames()
        {
            return assetBundleCache != null ?
                assetBundleCache.Select(x => Tuple.Create(x.Key, x.Value.referencedCount)).ToArray() : 
                new Tuple<string, int>[0];
        }

        private string BuildDownloadUrl(AssetInfo assetInfo)
        {
            var folderName = PlatformUtility.GetPlatformTypeName();

            var url = PathUtility.Combine(new string[] { remoteUrl, folderName, assetInfo.FileName });
            
            return string.Format("{0}{1}?v={2}", url, PackageExtension, assetInfo.FileHash);
        }

        public string BuildFilePath(AssetInfo assetInfo)
        {
            var path = installPath;

            if (assetInfo != null && !string.IsNullOrEmpty(assetInfo.FileName))
            {
                path = PathUtility.Combine(installPath, assetInfo.FileName) + PackageExtension;
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
        /// アセット情報ファイルを更新.
        /// </summary>
        public IObservable<Unit> UpdateAssetInfoManifest()
        {
            if (simulateMode) { return Observable.ReturnUnit(); }

            if (localMode) { return Observable.ReturnUnit(); }

            var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();
            
            return Observable.FromMicroCoroutine(() => UpdateAssetBundleInternal(manifestAssetInfo));
        }

        /// <summary>
        /// アセットバンドルを更新.
        /// </summary>
        public IObservable<Unit> UpdateAssetBundle(AssetInfo assetInfo, IProgress<float> progress = null)
        {
            if (simulateMode) { return Observable.ReturnUnit(); }

            if (localMode) { return Observable.ReturnUnit(); }

            return Observable.FromMicroCoroutine(() => UpdateAssetBundleInternal(assetInfo, progress));
        }

        private IEnumerator UpdateAssetBundleInternal(AssetInfo assetInfo, IProgress<float> progress = null)
        {
            //----------------------------------------------------------------------------------
            // ※ 呼び出し頻度が高いのでFromMicroCoroutineで呼び出される.
            //    戻り値を返す時はyield return null以外使わない.
            //----------------------------------------------------------------------------------

            var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

            if (!assetBundleCache.ContainsKey(assetBundleName))
            {
                // ネットワークの接続待ち.

                var readyForDownloadYield = ReadyForDownload().ToYieldInstruction(yieldCancel.Token);

                while (!readyForDownloadYield.IsDone)
                {
                    yield return null;
                }

                // アセットバンドルと依存アセットバンドルをまとめてダウンロード.

                var allBundles = new string[] { assetBundleName }.Concat(GetDependencies(assetBundleName)).ToArray();

                var loadAllBundles = new Dictionary<string, IObservable<string>>();

                foreach (var target in allBundles)
                {
                    var cached = assetBundleCache.GetValueOrDefault(target);

                    if (cached != null)
                    {
                        UnloadAsset(target, true);
                    }

                    var downloadTask = downloadQueueing.GetValueOrDefault(target);

                    if (downloadTask == null)
                    {
                        var info = assetInfosByAssetBundleName.GetValueOrDefault(target).FirstOrDefault();
                        
                        downloadQueueing[target] = Observable.FromMicroCoroutine(() => DownloadAssetBundle(info, progress))
                            .Select(_ => target)
                            .Share();

                        downloadTask = downloadQueueing[target];
                    }

                    loadAllBundles[target] = downloadTask;
                }

                foreach (var downloadTask in loadAllBundles)
                {
                    // ダウンロードキューが空くまで待つ.
                    while (maxDownloadCount <= downloadList.Count)
                    {
                        yield return null;
                    }
                    
                    // ダウンロード中でなかったらリストに追加.
                    if (!downloadList.Contains(downloadTask.Key))
                    {
                        downloadList.Add(downloadTask.Key);
                    }
                    
                    // ダウンロード実行.

                    var downloadYield = downloadTask.Value.ToYieldInstruction(yieldCancel.Token);

                    while (!downloadYield.IsDone)
                    {
                        yield return null;
                    }

                    // ダウンロード中リストから除外.
                    if (downloadList.Contains(downloadTask.Key))
                    {
                        downloadList.Remove(downloadTask.Key);
                    }

                    // ダウンロード待ちリストから除外.
                    if (downloadQueueing.ContainsKey(downloadTask.Key))
                    {
                        downloadQueueing.Remove(downloadTask.Key);
                    }
                }
            }
        }

        private IEnumerator DownloadAssetBundle(AssetInfo assetInfo, IProgress<float> progress = null)
        {
            //----------------------------------------------------------------------------------
            // ※ 呼び出し頻度が高いのでFromMicroCoroutineで呼び出される.
            //    戻り値を返す時はyield return null以外使わない.
            //----------------------------------------------------------------------------------
            
            var downloadUrl = BuildDownloadUrl(assetInfo);

            var filePath = BuildFilePath(assetInfo);

            var downloader = Observable.Defer(() => Observable.FromMicroCoroutine(() => FileDownload(downloadUrl, filePath, progress)));

            var downloadYield = downloader
                    .Timeout(TimeoutLimit)
                    .OnErrorRetry((TimeoutException ex) => OnTimeout(assetInfo, ex), RetryCount, RetryDelaySeconds)
                    .DoOnError(x => OnError(x))
                    .ToYieldInstruction(false, yieldCancel.Token);

            while (!downloadYield.IsDone)
            {
                yield return null;
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
        private string[] GetDependenciesInternal(string fileName, List<string> dependents = null)
        {
            var targets = dependencies.GetValueOrDefault(fileName, new string[0]);

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
        public IObservable<T> LoadAsset<T>(AssetInfo assetInfo, string assetPath, bool autoUnLoad = true) where T : UnityEngine.Object
        {
            // コンポーネントを取得する場合はGameObjectから取得.
            if (typeof(T).IsSubclassOf(typeof(Component)))
            {
                return Observable.FromMicroCoroutine<GameObject>(observer => LoadAssetInternal(observer, assetInfo, assetPath, autoUnLoad))
                    .Select(x => x != null ? x.GetComponent<T>() : null);                   
            }

            return Observable.FromMicroCoroutine<T>(observer => LoadAssetInternal(observer, assetInfo, assetPath, autoUnLoad));
        }

        private IEnumerator LoadAssetInternal<T>(IObserver<T> observer, AssetInfo assetInfo, string assetPath, bool autoUnLoad) where T : UnityEngine.Object
        {
            //----------------------------------------------------------------------------------
            // ※ 呼び出し頻度が高いのでFromMicroCoroutineで呼び出される.
            //    戻り値を返す時はyield return null以外使わない.
            //----------------------------------------------------------------------------------

            T result = null;

            #if UNITY_EDITOR

            if (simulateMode)
            {
                var loadYield = SimulateLoadAsset<T>(assetPath).ToYieldInstruction(false, yieldCancel.Token);

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
                var assetBundleName = assetInfo.AssetBundle.AssetBundleName.ToLower();

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

                                var info = assetInfosByAssetBundleName.GetValueOrDefault(x).FirstOrDefault();

                                cacheQueueing[x] = Observable.FromMicroCoroutine<AssetBundle>(_observer => LoadAssetBundleFromCache(_observer, info))
                                    .Timeout(TimeoutLimit)
                                    .OnErrorRetry((TimeoutException ex) => OnTimeout(info, ex), RetryCount, RetryDelaySeconds)
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

        private IEnumerator LoadAssetBundleFromCache(IObserver<AssetBundle> observer, AssetInfo assetInfo)
        {
            AssetBundle assetBundle = null;

            var filePath = BuildFilePath(assetInfo);
            var assetBundleInfo = assetInfo.AssetBundle;
            var assetBundleName = assetBundleInfo.AssetBundleName;

            var cryptoKey = GetCryptoKey();

            Func<byte[]> loadCacheFile = () =>
            {
                var bytes = new byte[0];

                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    bytes = new byte[fileStream.Length];

                    fileStream.Read(bytes, 0, bytes.Length);

                    // 復号化
                    bytes = bytes.Decrypt(cryptoKey);
                }

                return bytes;
            };

            // ファイルの読み込みと復号化をスレッドプールで実行.
            var loadYield = Observable.Start(() => loadCacheFile()).ObserveOnMainThread().ToYieldInstruction();

            while (!loadYield.IsDone)
            {
                yield return null;
            }

            if (loadYield.HasResult)
            {
                var bytes = loadYield.Result;

                var bundleLoadRequest = AssetBundle.LoadFromMemoryAsync(bytes, assetBundleInfo.CRC);

                while (!bundleLoadRequest.isDone)
                {
                    yield return null;
                }

                assetBundle = bundleLoadRequest.assetBundle;
            }

            // 読み込めなかった時はファイルを削除して次回読み込み時にダウンロードし直す.
            if (assetBundle == null)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                var builder = new StringBuilder();

                builder.Append("Failed to load AssetBundle!").AppendLine();
                builder.AppendLine();
                builder.AppendFormat("File : {0}", filePath).AppendLine();
                builder.AppendFormat("AssetBundleName : {0}", assetBundleName).AppendLine();
                builder.AppendFormat("CRC : {0}", assetBundleInfo.CRC).AppendLine();
                
                throw new Exception(builder.ToString());
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
        /// マニフェストファイルに存在しないキャッシュファイルを破棄.
        /// </summary>
        private void CleanUnUseCache()
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

                var allAssetInfos = manifest.GetAssetInfos().ToList();

                allAssetInfos.Add(AssetInfoManifest.GetManifestAssetInfo());

                var managedFiles = allAssetInfos
                    .Select(x => BuildFilePath(x))
                    .Select(x => PathUtility.ConvertPathSeparator(x))
                    .Distinct()
                    .ToHashSet();

                var targets = cacheFiles
                    .Select(x => PathUtility.ConvertPathSeparator(x))
                    .Where(x => Path.GetExtension(x) == PackageExtension)
                    .Where(x => !managedFiles.Contains(x))
                    .ToArray();

                foreach (var target in targets)
                {
                    if (!File.Exists(target)) { continue; }

                    File.SetAttributes(target, FileAttributes.Normal);
                    File.Delete(target);

                    builder.AppendLine(target);
                }

                var deleteDirectories = DirectoryUtility.DeleteEmpty(installDir);

                deleteDirectories.ForEach(x => builder.AppendLine(x));

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

        private void OnTimeout(AssetInfo assetInfo, Exception exception)
        {
            Debug.LogErrorFormat("[Download Timeout] \n{0}", exception);

            if (onTimeOut != null)
            {
                onTimeOut.OnNext(assetInfo);
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
        public IObservable<AssetInfo> OnTimeOutAsObservable()
        {
            return onTimeOut ?? (onTimeOut = new Subject<AssetInfo>());
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

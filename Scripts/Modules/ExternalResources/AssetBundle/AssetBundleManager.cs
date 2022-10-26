
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.ExternalResource;

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

        //----- field -----

        // 同時ダウンロード数.
        private uint maxDownloadCount = 0;

        // アセット管理.
        private AssetInfoManifest manifest = null;

        // ダウンロード先パス.
        private string installPath = null;

        // ダウンロード元URL.
        private string remoteUrl = null;
        private string versionHash = null;

        // ダウンロード中アセットバンドル.
        private HashSet<string> downloadList = null;

        // ダウンロード待ちアセットバンドル.
        private Dictionary<string, IObservable<string>> downloadQueueing = null;

        // 読み込み待ちアセットバンドル.
        private Dictionary<string, IObservable<Tuple<SeekableAssetBundle, string>>> loadQueueing = null;

        // 読み込み済みアセットバンドル.
        private Dictionary<string, LoadedAssetBundle> loadedAssetBundles = null;

        // アセット情報(アセットバンドル).
        private Dictionary<string, AssetInfo[]> assetInfosByAssetBundleName = null;

        // 依存関係.
        private Dictionary<string, string[]> dependencies = null;

		// シュミュレートモードか.
        private bool simulateMode = false;

        // ローカルモードか.
        private bool localMode = false;

        // イベント通知.
        private Subject<AssetInfo> onTimeOut = null;
        private Subject<Exception> onError = null;

        private AesCryptoStreamKey cryptoKey = null;

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
        public void Initialize(uint maxDownloadCount, bool simulateMode = false)
        {
            if (isInitialized) { return; }

            this.maxDownloadCount = maxDownloadCount;
            this.simulateMode = UnityUtility.isEditor && simulateMode;

            downloadList = new HashSet<string>();
            downloadQueueing = new Dictionary<string, IObservable<string>>();
            loadQueueing = new Dictionary<string, IObservable<Tuple<SeekableAssetBundle, string>>>();
            loadedAssetBundles = new Dictionary<string, LoadedAssetBundle>();
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
        /// Key,IVがModules.ExternalResource.ManageConfigのAssetのCryptKeyと一致している必要があります.
        /// </summary>
        /// <param name="key">暗号化Key(32文字)</param>
        /// <param name="iv">暗号化IV(16文字)</param>
        public void SetCryptoKey(string key, string iv)
        {
            cryptoKey = new AesCryptoStreamKey(key, iv);
        }

		/// <summary> URLを設定. </summary>
        /// <param name="remoteUrl">アセットバンドルのディレクトリURLを指定</param>
        /// <param name="versionHash">バージョンハッシュを指定</param>
        public void SetUrl(string remoteUrl, string versionHash)
        {
            this.remoteUrl = remoteUrl;
            this.versionHash = versionHash;
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
        }

        /// <summary> 展開中のアセットバンドル名一覧取得 </summary>
        public Tuple<string, int>[] GetLoadedAssetBundleNames()
        {
            return loadedAssetBundles != null ?
                loadedAssetBundles.Select(x => Tuple.Create(x.Key, x.Value.referencedCount)).ToArray() : 
                new Tuple<string, int>[0];
        }

        private string BuildDownloadUrl(AssetInfo assetInfo)
        {
            var platformName = PlatformUtility.GetPlatformTypeName();

            var url = PathUtility.Combine(new string[] { remoteUrl, platformName, versionHash, assetInfo.FileName });

            var downloadUrl = string.Format("{0}{1}", url, PackageExtension);

            if (!assetInfo.Hash.IsNullOrEmpty())
            {
                downloadUrl = string.Format("{0}?v={1}", downloadUrl, assetInfo.Hash);
            }

            return downloadUrl;
        }

        public string GetFilePath(AssetInfo assetInfo)
        {
            var path = installPath;

            if (assetInfo != null && !string.IsNullOrEmpty(assetInfo.FileName))
            {
                path = PathUtility.Combine(installPath, assetInfo.FileName) + PackageExtension;
            }

            return PathUtility.ConvertPathSeparator(path);
        }

        #region Download

        private async UniTask ReadyForDownload()
        {
            // wait network disconnect.
            while (Application.internetReachability == NetworkReachability.NotReachable)
            {
                await UniTask.NextFrame();
            }
        }

        /// <summary>
        /// アセット情報ファイルを更新.
        /// </summary>
        public async UniTask UpdateAssetInfoManifest(CancellationToken cancelToken)
        {
            if (simulateMode) { return; }

            if (localMode) { return; }

            var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();
            
            await UpdateAssetBundleInternal(manifestAssetInfo);
        }

        /// <summary>
        /// アセットバンドルを更新.
        /// </summary>
        public async UniTask UpdateAssetBundle(AssetInfo assetInfo, IProgress<float> progress = null)
        {
            if (simulateMode) { return; }

            if (localMode) { return; }

            await UpdateAssetBundleInternal(assetInfo, progress);
        }

        private async UniTask UpdateAssetBundleInternal(AssetInfo assetInfo, IProgress<float> progress = null)
        {
			var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

            if (!loadedAssetBundles.ContainsKey(assetBundleName))
            {
                // ネットワークの接続待ち.

				await ReadyForDownload();

				// アセットバンドルと依存アセットバンドルをまとめてダウンロード.

                var allBundles = new string[] { assetBundleName }.Concat(GetDependencies(assetBundleName)).ToArray();

                var loadAllBundles = new Dictionary<string, IObservable<string>>();

                foreach (var target in allBundles)
                {
                    var cached = loadedAssetBundles.GetValueOrDefault(target);

                    if (cached != null)
                    {
                        UnloadAsset(target, true);
                    }

                    var downloadTask = downloadQueueing.GetValueOrDefault(target);

                    if (downloadTask == null)
                    {
                        var info = assetInfosByAssetBundleName.GetValueOrDefault(target).FirstOrDefault();
                        
                        downloadQueueing[target] = DownloadAssetBundle(info, progress)
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
                        await UniTask.NextFrame();
                    }
                    
                    // ダウンロード中でなかったらリストに追加.
                    if (!downloadList.Contains(downloadTask.Key))
                    {
                        downloadList.Add(downloadTask.Key);
                    }

                    // ダウンロード実行.
					try
					{
						var downloadYield = downloadTask.Value.ToYieldInstruction();

						while (!downloadYield.IsDone)
						{
							await UniTask.NextFrame();
						}
					}
					catch (Exception e)
					{
						Debug.LogErrorFormat("Exception : {0}\n{1}\n", downloadTask.Key, e);
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

        private IObservable<Unit> DownloadAssetBundle(AssetInfo assetInfo, IProgress<float> progress = null)
        {
			var downloadUrl = BuildDownloadUrl(assetInfo);

            var filePath = GetFilePath(assetInfo);
			
            return ObservableEx.FromUniTask(_cancelToken => FileDownload(_cancelToken, downloadUrl, filePath, progress))
                    .Timeout(TimeoutLimit)
                    .OnErrorRetry((TimeoutException ex) => OnTimeout(assetInfo, ex), RetryCount, RetryDelaySeconds)
                    .DoOnError(x => OnError(x));
		}

        private async UniTask FileDownload(CancellationToken cancelToken, string url, string path, IProgress<float> progress = null)
        {
            var webRequest = new UnityWebRequest(url);
            var buffer = new byte[256 * 1024];

            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            webRequest.downloadHandler = new AssetBundleDownloadHandler(path, buffer);

            await webRequest.SendWebRequest().ToUniTask(progress, cancellationToken: cancelToken);
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

            if (dependent == null)
            {
                // 依存アセット一覧を再帰で取得.
                dependent = GetDependenciesInternal(assetBundleName);

                // 登録.
                if (dependent.Any())
                {
                    dependencies.Add(assetBundleName, dependent);
                }
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
                return ObservableEx.FromUniTask(cancelToken => LoadAssetInternal<GameObject>(cancelToken, assetInfo, assetPath, autoUnLoad))
                    .Select(x => x != null ? x.GetComponent<T>() : null);                   
            }

            return ObservableEx.FromUniTask(cancelToken => LoadAssetInternal<T>(cancelToken, assetInfo, assetPath, autoUnLoad));
        }

        private async UniTask<T> LoadAssetInternal<T>(CancellationToken cancelToken, AssetInfo assetInfo, string assetPath, bool autoUnLoad) where T : UnityEngine.Object
        {
            T result = null;

            #if UNITY_EDITOR

            if (simulateMode)
            {
				try
				{
					result = await SimulateLoadAsset<T>(assetPath).AttachExternalCancellation(cancelToken);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
            else

            #endif

            {
                IObservable<SeekableAssetBundle> loader = null;

                // アセットバンドル名は小文字なので小文字に変換.
                var assetBundleName = assetInfo.AssetBundle.AssetBundleName.ToLower();

                var loadedAssetBundle = loadedAssetBundles.GetValueOrDefault(assetBundleName);

                if (loadedAssetBundle == null)
                {
                    var allBundles = new string[] { assetBundleName }.Concat(GetDependencies(assetBundleName)).ToArray();

                    var loadAllBundles = allBundles
                        .Select(x =>
                            {
                                var cached = loadedAssetBundles.GetValueOrDefault(x);

                                if (cached != null)
                                {
                                    return Observable.Return(Tuple.Create((SeekableAssetBundle)cached, x));
                                }

                                var loadTask = loadQueueing.GetValueOrDefault(x);

                                if (loadTask != null)
                                {   
                                    return loadTask;
                                }

                                var info = assetInfosByAssetBundleName.GetValueOrDefault(x).FirstOrDefault();

                                loadQueueing[x] = ObservableEx.FromUniTask(_cancelToken => LoadAssetBundle(_cancelToken, info))
                                    .Timeout(TimeoutLimit)
                                    .OnErrorRetry((TimeoutException ex) => OnTimeout(info, ex), RetryCount, RetryDelaySeconds)
                                    .DoOnError(error => OnError(error))
                                    .Select(ab => Tuple.Create(ab, x))
                                    .Share();

                                return loadQueueing[x];
                            })
                        .ToArray();

                    loader = Observable.Defer(() => Observable.WhenAll(loadAllBundles)
                        .Select(tuples =>
                        {
                            foreach (var tuple in tuples)
                            {
                                loadedAssetBundles[tuple.Item2] = new LoadedAssetBundle(tuple.Item1);

                                if (loadQueueing.ContainsKey(tuple.Item2))
                                {
                                    loadQueueing.Remove(tuple.Item2);
                                }
                            }

                            return tuples[0].Item1;
                        }));
                }
                else
                {
                    loadedAssetBundle.referencedCount++;

                    loader = Observable.Defer(() => Observable.Return(loadedAssetBundle));
                }

				try
				{
					var loadYield = loader.ToYieldInstruction(cancelToken);

					while (!loadYield.IsDone)
					{
						await UniTask.NextFrame(cancelToken);
					}

					var seekableAssetBundle = loadYield.Result;

					if (seekableAssetBundle != null)
					{
						var assetBundleRequest = seekableAssetBundle.assetBundle.LoadAssetAsync(assetPath, typeof(T));

						while (!assetBundleRequest.isDone)
						{
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

        private async UniTask<SeekableAssetBundle> LoadAssetBundle(CancellationToken cancelToken, AssetInfo assetInfo)
        {
            var filePath = GetFilePath(assetInfo);
            var assetBundleInfo = assetInfo.AssetBundle;
            var assetBundleName = assetBundleInfo.AssetBundleName;

            await UniTask.SwitchToMainThread();

			#if UNITY_ANDROID && !UNITY_EDITOR

            if (localMode && filePath.StartsWith(UnityPathUtility.StreamingAssetsPath))
            {
				try
				{
					await AndroidUtility.CopyStreamingToTemporary(filePath).AttachExternalCancellation(cancelToken);

					filePath = AndroidUtility.ConvertStreamingAssetsLoadPath(filePath);
				}
				catch (OperationCanceledException)
				{
					/* Canceled */
				}
			}

			#endif
            
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            
            var cryptoStream = new SeekableCryptoStream(fileStream, cryptoKey);
            
            var bundleLoadRequest = AssetBundle.LoadFromStreamAsync(cryptoStream);
            
            while (!bundleLoadRequest.isDone)
            {
                await UniTask.NextFrame(cancelToken);
            }

            var assetBundle = bundleLoadRequest.assetBundle;

            // 読み込めなかった時はファイルを削除して次回読み込み時にダウンロードし直す.
            if (assetBundle == null)
            {
                await UniTask.SwitchToMainThread();

                UnloadAsset(assetBundleName);

				if (cryptoStream != null)
				{
					#if NET_UNITY_4_8

                    await cryptoStream.DisposeAsync();

					#else

					cryptoStream.Dispose();

					#endif
				}

				if (fileStream != null)
				{
					#if NET_UNITY_4_8

                    await fileStream.DisposeAsync();

					#else

					fileStream.Dispose();

					#endif
				}

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

			var seekableAssetBundle = new SeekableAssetBundle(assetBundle, fileStream, cryptoStream);

			return seekableAssetBundle;
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

            var assetBundleNames = loadedAssetBundles.Keys.ToArray();

            foreach (var assetBundleName in assetBundleNames)
            {
                UnloadAsset(assetBundleName, unloadAllLoadedObjects, true);
            }

            loadedAssetBundles.Clear();
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
        }

        private void UnloadAssetBundleInternal(string assetBundleName, bool unloadAllLoadedObjects, bool force = false)
        {
            var info = loadedAssetBundles.GetValueOrDefault(assetBundleName);

            if (info == null) { return; }

            info.referencedCount--;

            if (force || info.referencedCount <= 0)
            {
                if (info.assetBundle != null)
                {
                    info.assetBundle.Unload(unloadAllLoadedObjects);
                    info.assetBundle = null;
                }

                info.Dispose();

                loadedAssetBundles.Remove(assetBundleName);
            }
        }

        #endregion

        /// <summary>
        /// マニフェストファイルに存在しないキャッシュファイルを破棄.
        /// </summary>
        public string[] GetDisUsedFilePaths()
        {
            if (simulateMode) { return null; }

            if (manifest == null) { return null; }

            var installDir = GetFilePath(null);

            if (string.IsNullOrEmpty(installDir)) { return null; }

            if (!Directory.Exists(installDir)) { return null; }

            var directory = Path.GetDirectoryName(installDir);

            if (!Directory.Exists(directory)) { return null; }
            
            var cacheFiles = Directory.GetFiles(installDir, "*", SearchOption.AllDirectories);

            var allAssetInfos = manifest.GetAssetInfos().ToList();

            allAssetInfos.Add(AssetInfoManifest.GetManifestAssetInfo());

            var managedFiles = allAssetInfos
                .Select(x => GetFilePath(x))
                .Distinct()
                .ToHashSet();

            return cacheFiles
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .Where(x => Path.GetExtension(x) == PackageExtension)
                .Where(x => !managedFiles.Contains(x))
                .ToArray();
        }

        private void OnTimeout(AssetInfo assetInfo, Exception exception)
        {
            using (new DisableStackTraceScope())
            {
                Debug.LogErrorFormat("[Timeout] {0}", exception);
            }

            if (onTimeOut != null)
            {
                onTimeOut.OnNext(assetInfo);
            }
        }

        private void OnError(Exception exception)
        {
			// キャンセルはエラー扱いしない.
			if (exception is OperationCanceledException){ return; }

            using (new DisableStackTraceScope())
            {
                Debug.LogErrorFormat("[Error] {0}", exception);
            }

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

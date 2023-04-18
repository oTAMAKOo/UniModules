
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
using Modules.Net;
using Modules.Net.WebRequest;
using Modules.ExternalAssets;
using Modules.Performance;

namespace Modules.AssetBundles
{
    public sealed partial class AssetBundleManager : Singleton<AssetBundleManager>
    {
        //----- params -----

		public const string PackageExtension = ".package";

        // 同時ダウンロード処理実行数.
        private const int MaxDownloadQueueingCount = 25;

		// 非同期で読み込むファイルサイズ (0.1MB).
		private const float AsyncLoadFileSize = 1024.0f * 1024.0f * 0.1f;

		// 1フレームで同時に同期読み込みする最大ファイルサイズ (1MB).
		private const float FrameSyncLoadFileSize = 1024.0f * 1024.0f * 1f;

		// 1フレームで同期読み込みする最大ファイル数.
		private const int FrameSyncLoadFileNum = 25;

		// タイムアウトまでの時間.
        private readonly TimeSpan TimeoutLimit = TimeSpan.FromSeconds(60f);

        // リトライする回数.
        private readonly int RetryCount = 3;

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

        // ダウンロードキュー数.
        private uint downloadQueueingCount = 0;

        // ダウンロード中アセットバンドル.
        private HashSet<string> downloadList = null;

        // ダウンロード待ちアセットバンドル.
        private Dictionary<string, IObservable<Unit>> downloadQueueing = null;

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

		// シュミュレートモードか.
        private bool simulateMode = false;

        // ローカルモードか.
        private bool localMode = false;

		// ファイルハンドラ.
		private IAssetBundleFileHandler fileHandler = null;

        // イベント通知.
		private Subject<AssetInfo> onLoad = null;
        private Subject<AssetInfo> onTimeOut = null;
        private Subject<Exception> onError = null;

		private bool isInitialized = false;

        //----- property -----

        //----- method -----

        private AssetBundleManager() { }

		/// <summary> 初期化 </summary>
		/// <param name="simulateMode">AssetDataBaseからアセットを取得(EditorOnly)</param>
		public void Initialize(bool simulateMode = false)
        {
            if (isInitialized) { return; }
            
            this.simulateMode = UnityUtility.isEditor && simulateMode;

            urlBuilder = new StringBuilder();
            downloadList = new HashSet<string>();
            downloadQueueing = new Dictionary<string, IObservable<Unit>>();
            loadQueueing = new Dictionary<string, IObservable<AssetBundle>>();
            loadedAssetBundles = new Dictionary<string, AssetBundle>();
            assetBundleRefCount = new Dictionary<string, int>();
            assetInfosByAssetBundleName = new Dictionary<string, List<AssetInfo>>();
			assetBundleDependencies = new AssetBundleDependencies();

            syncLoadFileCountLimiter = new FunctionFrameLimiter(FrameSyncLoadFileNum);
            syncLoadFileSizeLimiter = new FunctionFrameLimiter((ulong)FrameSyncLoadFileSize);

            AddManifestAssetInfo();

            isInitialized = true;
        }

        /// <summary> 同時ダウンロード数設定. </summary>
        public void SetMaxDownloadCount(uint maxDownloadCount)
        {
            this.maxDownloadCount = maxDownloadCount;
        }

        /// <summary> ローカルモード設定. </summary>
        public void SetLocalMode(bool localMode)
        {
            this.localMode = localMode;
        }

		/// <summary> ファイルハンドラ設定. </summary>
		public void SetFileHandler(IAssetBundleFileHandler fileHandler)
		{
			this.fileHandler = fileHandler;
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

            if (manifest == null){ return; }

            var assetInfos = manifest.GetAssetInfos();

            var count = 0;

            foreach (var assetInfo in assetInfos)
            {
                if (assetInfo.IsAssetBundle)
                {
                    var assetBundleInfo = assetInfo.AssetBundle;
                    var assetBundleName = assetBundleInfo.AssetBundleName;
                    
                    //------ 参照関係構築 ------

                    var refs = assetInfosByAssetBundleName.GetValueOrDefault(assetBundleName);

                    if (refs == null)
                    {
                        refs = new List<AssetInfo>();
                        assetInfosByAssetBundleName[assetBundleName] = refs;
                    }

                    refs.Add(assetInfo);

                    assetBundleRefCount[assetBundleName] = 0;

                    //------ 依存関係構築 ------

					assetBundleDependencies.SetDependencies(assetBundleName, assetBundleInfo.Dependencies);
				}

                if (++count % 500 == 0)
                {
                    await UniTask.NextFrame();
                }
            }

            AddManifestAssetInfo();
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

            var downloadUrl = PathUtility.Combine(new string[] { remoteUrl, platformName, versionHash, assetInfo.FileName });

            urlBuilder.Append(downloadUrl).Append(PackageExtension);

            if (!assetInfo.Hash.IsNullOrEmpty())
            {
                urlBuilder.AppendFormat("?v={0}", assetInfo.Hash);
            }

            return urlBuilder.ToString();
        }

        public string GetFilePath(string installPath, AssetInfo assetInfo)
        {
            if (assetInfo == null || string.IsNullOrEmpty(assetInfo.FileName)){ return null; }

            var path = PathUtility.Combine(installPath, assetInfo.FileName);

			return Path.ChangeExtension(path, PackageExtension);
        }

        #region Download

		/// <summary>
        /// アセット情報ファイルを更新.
        /// </summary>
        public async UniTask UpdateAssetInfoManifest(string installPath, CancellationToken cancelToken = default)
        {
            if (simulateMode || localMode) { return; }

            var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();
            
            await UpdateAssetBundleInternal(installPath, manifestAssetInfo, null, cancelToken);
        }

        /// <summary>
        /// アセットバンドルを更新.
        /// </summary>
        public async UniTask UpdateAssetBundle(string installPath,AssetInfo assetInfo, IProgress<float> progress = null, CancellationToken cancelToken = default)
        {
            if (simulateMode || localMode) { return; }

            await UpdateAssetBundleInternal(installPath, assetInfo, progress, cancelToken);
        }

		private async UniTask UpdateAssetBundleInternal(string installPath, AssetInfo assetInfo, IProgress<float> progress, CancellationToken cancelToken)
        {
			// 開始待ち.

			try
			{
				// ダウンロードキューが空くまで待つ.
				while (true)
				{
					if (downloadQueueingCount <= MaxDownloadQueueingCount) { break; }

					await UniTask.NextFrame(cancelToken);
				}

				// ネットワークの接続待ち.
				await NetworkConnection.WaitNetworkReachable(cancelToken);
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
			}

			if (cancelToken.IsCancellationRequested) { return; }

			// 開始.

			downloadQueueingCount++;

			// アセットバンドルと依存アセットバンドルをまとめてダウンロード.

            var assetBundleName = assetInfo.AssetBundle.AssetBundleName;
            
            var allBundles = new HashSet<string>();

            allBundles.Add(assetBundleName);

            var allDependencies = assetBundleDependencies.GetAllDependencies(assetBundleName);

            foreach (var item in allDependencies)
            {
                if (allBundles.Contains(item)){ continue; }

                allBundles.Add(item);
            }

            // ダウンロード.

            var loadAllBundles = new Dictionary<string, IObservable<Unit>>();

            foreach (var target in allBundles)
            {
                // ロード済みの場合はアンロード.
                if (loadedAssetBundles.ContainsKey(target))
                {
                    UnloadAsset(target, true);
                }

                // ダウンロード中ならスキップ.
                if (downloadList.Contains(target)){ continue; }

                // 既にダウンロードキューに入っている場合はスキップ.
                if (downloadQueueing.ContainsKey(target)){ continue; }
                
                var info = assetInfosByAssetBundleName.GetValueOrDefault(target).FirstOrDefault();
                    
                if (info != null)
                {
                    var task = DownloadAssetBundle(installPath, info, progress).Share();

                    downloadQueueing[target] = task;

                    loadAllBundles[target] = task;
                }
                else
                {
                    OnError(new AssetInfoNotFoundException(target));
                }
            }

            foreach (var item in loadAllBundles)
            {
				try
				{
					if (!cancelToken.IsCancellationRequested)
					{
						// ダウンロードキューが空くまで待つ.
						while (true)
						{
							if (downloadList.Count <= maxDownloadCount) { break; }

							await UniTask.NextFrame(cancelToken);
						}

						// ネットワークの接続待ち.
						await NetworkConnection.WaitNetworkReachable(cancelToken);

						// ダウンロード中でなかったらリストに追加.
						if (!downloadList.Contains(item.Key))
						{
							downloadList.Add(item.Key);
						}

						// ダウンロード実行.
						await item.Value.ToUniTask(cancellationToken: cancelToken);
					}
				}
				catch (OperationCanceledException)
				{
					/* Canceled */
				}
				catch (Exception e)
				{
					Debug.LogErrorFormat("Download task error : {0}\n{1}\n", item.Key, e);

					OnError(e);
				}
				finally
				{
					// ダウンロード中リストから除外.
					if (downloadList.Contains(item.Key))
					{
						downloadList.Remove(item.Key);
					}

					// ダウンロード待ちリストから除外.
					if (downloadQueueing.ContainsKey(item.Key))
					{
						downloadQueueing.Remove(item.Key);
					}
				}
			}

            downloadQueueingCount--;
        }

        private IObservable<Unit> DownloadAssetBundle(string installPath,AssetInfo assetInfo, IProgress<float> progress = null)
        {
            var downloadUrl = BuildDownloadUrl(assetInfo);

            var filePath = GetFilePath(installPath, assetInfo);

			if (string.IsNullOrEmpty(filePath)){ return Observable.ReturnUnit(); }
			
            return ObservableEx.FromUniTask(cancelToken => FileDownload(downloadUrl, filePath, progress, cancelToken))
                    .Timeout(TimeoutLimit)
                    .OnErrorRetry((TimeoutException ex) => OnTimeout(assetInfo, ex), RetryCount, RetryDelaySeconds)
                    .OnErrorRetry((Exception _) => {}, RetryCount, RetryDelaySeconds)
                    .DoOnError(x => OnError(x));
		}

        private async UniTask FileDownload(string url, string path, IProgress<float> progress, CancellationToken cancelToken)
        {
            using (var webRequest = UnityWebRequest.Get(url))
            {
                var downloadHandler = new DownloadHandlerFile(path);

                downloadHandler.removeFileOnAbort = true;

                webRequest.downloadHandler = downloadHandler;
                webRequest.timeout = (int)TimeoutLimit.TotalSeconds;

                var downloadTask = webRequest.SendWebRequest();

                while (!downloadTask.isDone)
                {
                    if (cancelToken.IsCancellationRequested){ break; }

                    await UniTask.NextFrame(cancelToken);

                    if (progress != null)
                    {
                        progress.Report(downloadTask.progress);
                    }
                }

                if (webRequest.HasError() || webRequest.responseCode != (int)System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"File download error : ResponseCode : {webRequest.responseCode}) {url}\n\n{webRequest.error}");
                }
            }
        }

        #endregion

        #region Dependencies

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

            if (simulateMode)
            {
				try
				{
					result = await SimulateLoadAsset<T>(assetPath, cancelToken);
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
							if (cancelToken.IsCancellationRequested){ break; }

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

            if(loadedAssetBundle != null) { return loadedAssetBundle; }

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

        private IObservable<AssetBundle> GetLoadTask(string installPath,string assetBundleName)
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

            loadQueueing[assetBundleName] = ObservableEx.FromUniTask(_cancelToken => LoadAssetBundle(installPath, _cancelToken, info))
                .Timeout(TimeoutLimit)
                .OnErrorRetry((TimeoutException ex) => OnTimeout(info, ex), RetryCount, RetryDelaySeconds)
                .DoOnError(error => OnError(error))
                .Share();

            return loadQueueing[assetBundleName];
        }

		private async UniTask<AssetBundle> LoadAssetBundle(string installPath, CancellationToken cancelToken, AssetInfo assetInfo)
        {
			AssetBundle assetBundle = null;

            var filePath = GetFilePath(installPath, assetInfo);

            if (string.IsNullOrEmpty(filePath)){ return null; }

			var assetBundleInfo = assetInfo.AssetBundle;
            var assetBundleName = assetBundleInfo.AssetBundleName;

			#if UNITY_ANDROID && !UNITY_EDITOR

            if (localMode && filePath.StartsWith(UnityPathUtility.StreamingAssetsPath))
            {
				try
				{
					await AndroidUtility.CopyStreamingToTemporary(filePath, cancelToken);

					filePath = AndroidUtility.ConvertStreamingAssetsLoadPath(filePath);
				}
				catch (OperationCanceledException)
				{
					/* Canceled */
				}
			}

			#endif

			if (cancelToken.IsCancellationRequested){ return null; }

			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException(filePath);
			}

			var loadedAssetBundle = loadedAssetBundles.GetValueOrDefault(assetBundleName);

            if (loadedAssetBundle != null)
            {
                assetBundle = loadedAssetBundle;
            }
            else
            {
				byte[] bytes = null;

				using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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

                        await syncLoadFileSizeLimiter.Wait((ulong)bytes.Length, cancelToken: cancelToken);

                        if (cancelToken.IsCancellationRequested){ return null; }

                        // 同期処理で読み込む.
                        fileStream.Read(bytes, 0, bytes.Length);
                    }
                }
				
				if (bytes != null)
                {
					// 復元.

					bytes = fileHandler.Decode(bytes);

                    // AssetBundle読み込み.

                    var bundleLoadRequest = AssetBundle.LoadFromMemoryAsync(bytes);

                    while (!bundleLoadRequest.isDone)
                    {
						if (cancelToken.IsCancellationRequested){ break; }

                        await UniTask.NextFrame(cancelToken);
                    }

					if (cancelToken.IsCancellationRequested) { return null; }

					assetBundle = bundleLoadRequest.assetBundle;
                }
            }

            if (loadQueueing.ContainsKey(assetBundleName))
            {
                loadQueueing.Remove(assetBundleName);
            }

            if (assetBundle != null)
            {
                loadedAssetBundles.Add(assetBundleName, assetBundle);

				if (onLoad != null)
				{
					onLoad.OnNext(assetInfo);
				}
            }
            else
            {
                UnloadAsset(assetBundleName);

                // バージョンファイルを削除し次回読み込み時に再ダウンロード.
				var versionFilePath = Path.ChangeExtension(filePath, AssetInfoManifest.VersionFileExtension);

                if (File.Exists(versionFilePath))
                {
                    File.Delete(versionFilePath);
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
            if (simulateMode) { return; }

            if(!force && loadQueueing.ContainsKey(assetBundleName)){ return; }

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
            if (simulateMode) { return; }

            var assetBundleNames = loadedAssetBundles.Keys.ToArray();

            foreach (var assetBundleName in assetBundleNames)
            {
                UnloadAsset(assetBundleName, unloadAllLoadedObjects, true);
            }

            loadedAssetBundles.Clear();
        }

        private void UnloadAssetBundleInternal(string assetBundleName, bool unloadAllLoadedObjects, bool force = false)
        {
            if (!force && loadQueueing.ContainsKey(assetBundleName)){ return; }

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

        private void OnTimeout(AssetInfo assetInfo, Exception exception)
        {
			if (onTimeOut != null)
            {
                onTimeOut.OnNext(assetInfo);
            }
			else
			{
				Debug.LogErrorFormat("[Timeout] {0}", exception);
			}
        }

        private void OnError(Exception exception)
        {
			// キャンセルはエラー扱いしない.
			if (exception is OperationCanceledException){ return; }

			if (onError != null)
            {
                onError.OnNext(exception);
            }
			else
			{
				Debug.LogErrorFormat("[Error] {0}", exception);
			}
        }

		/// <summary> 読み込み時イベント. </summary>
		public IObservable<AssetInfo> OnLoadAsObservable()
		{
			return onLoad ?? (onLoad = new Subject<AssetInfo>());
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

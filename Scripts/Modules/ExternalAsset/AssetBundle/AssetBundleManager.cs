
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
using Modules.ExternalAssets;
using Modules.Net.WebRequest;

namespace Modules.AssetBundles
{
    public sealed partial class AssetBundleManager : Singleton<AssetBundleManager>
    {
        //----- params -----

        public const string PackageExtension = ".package";

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

		// 同期読み込みファイルサイズ.
		private float syncLoadFileSize = 0f;

		// 同期読み込みファイル数.
		private float syncLoadFileCount = 0f;

		// 同期読み込みフレームカウント.
		private int syncLoadFrameCount = 0;

        // アセット管理.
        private AssetInfoManifest manifest = null;

		// ダウンロード元URL.
        private string remoteUrl = null;
        private string versionHash = null;

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
		private Dictionary<string, string[]> dependenciesTable = null;

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

            downloadList = new HashSet<string>();
            downloadQueueing = new Dictionary<string, IObservable<Unit>>();
            loadQueueing = new Dictionary<string, IObservable<AssetBundle>>();
            loadedAssetBundles = new Dictionary<string, AssetBundle>();
            assetBundleRefCount = new Dictionary<string, int>();
            assetInfosByAssetBundleName = new Dictionary<string, List<AssetInfo>>();
            dependenciesTable = new Dictionary<string, string[]>();

            AddManifestAssetInfo();

            isInitialized = true;
        }

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
            this.manifest = manifest;

            assetInfosByAssetBundleName.Clear();
            assetBundleRefCount.Clear();
            dependenciesTable.Clear();

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

                    if(assetBundleInfo.Dependencies != null && assetBundleInfo.Dependencies.Any())
                    {
                        var list = dependenciesTable.GetValueOrDefault(assetBundleName);

                        if (list == null)
                        {
                            var dependencies = assetBundleInfo.Dependencies.Where(y => y != assetBundleName).ToArray();

                            dependenciesTable[assetBundleName] = dependencies;
                        }
                    }
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
            var platformName = PlatformUtility.GetPlatformTypeName();

            var downloadUrl = PathUtility.Combine(new string[] { remoteUrl, platformName, versionHash, assetInfo.FileName }) + PackageExtension;

            if (!assetInfo.Hash.IsNullOrEmpty())
            {
                downloadUrl = string.Format("{0}?v={1}", downloadUrl, assetInfo.Hash);
            }

            return downloadUrl;
        }

        public string GetFilePath(string installPath, AssetInfo assetInfo)
        {
            if (assetInfo == null || string.IsNullOrEmpty(assetInfo.FileName)){ return null; }

			return PathUtility.Combine(installPath, assetInfo.FileName) + PackageExtension;
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
        public async UniTask UpdateAssetInfoManifest(string installPath, CancellationToken cancelToken = default)
        {
            if (simulateMode) { return; }

            if (localMode) { return; }

            var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();
            
            await UpdateAssetBundleInternal(installPath, manifestAssetInfo, null, cancelToken);
        }

        /// <summary>
        /// アセットバンドルを更新.
        /// </summary>
        public async UniTask UpdateAssetBundle(string installPath,AssetInfo assetInfo, IProgress<float> progress = null, CancellationToken cancelToken = default)
        {
            if (simulateMode) { return; }

            if (localMode) { return; }

            await UpdateAssetBundleInternal(installPath, assetInfo, progress, cancelToken);
        }

        private async UniTask UpdateAssetBundleInternal(string installPath, AssetInfo assetInfo, IProgress<float> progress, CancellationToken cancelToken)
        {
			var assetBundleName = assetInfo.AssetBundle.AssetBundleName;
			
            // ネットワークの接続待ち.

			await ReadyForDownload();

			// アセットバンドルと依存アセットバンドルをまとめてダウンロード.

			var allBundles = new string[] { assetBundleName }
				.Concat(GetAllDependencies(assetBundleName))
				.Distinct()
				.ToArray();

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
                    OnError(new FileNotFoundException($"Asset info not found. {target}"));
                }
            }

            foreach (var item in loadAllBundles)
            {
                // ダウンロードキューが空くまで待つ.
                while (maxDownloadCount <= downloadList.Count)
                {
					if (cancelToken.IsCancellationRequested){ break; }

                    await UniTask.NextFrame(cancelToken);
                }

				if (cancelToken.IsCancellationRequested){ return; }
                
                // ダウンロード中でなかったらリストに追加.
                if (!downloadList.Contains(item.Key))
                {
                    downloadList.Add(item.Key);
                }

				// ダウンロード実行.
				try
				{
                    await item.Value;
                }
				catch (Exception e)
				{
                    Debug.LogErrorFormat("Download task error : {0}\n{1}\n", item.Key, e);

                    OnError(e);
				}

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

        private IObservable<Unit> DownloadAssetBundle(string installPath,AssetInfo assetInfo, IProgress<float> progress = null)
        {
            var downloadUrl = BuildDownloadUrl(assetInfo);

            var filePath = GetFilePath(installPath, assetInfo);

			if (string.IsNullOrEmpty(filePath)){ return Observable.ReturnUnit(); }
			
            return ObservableEx.FromUniTask(cancelToken => FileDownload(downloadUrl, filePath, progress, cancelToken))
                    .Timeout(TimeoutLimit)
                    .OnErrorRetry((TimeoutException ex) => OnTimeout(assetInfo, ex), RetryCount, RetryDelaySeconds)
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
                    throw new Exception($"File download error : [{webRequest.responseCode}]{url}\n\n{webRequest.error}");
                }
            }
        }

        #endregion

        #region Dependencies

        private string[] GetAllDependencies(string assetBundleName)
        {
            // 既に登録済みの場合はそこから取得.
            var dependents = dependenciesTable.GetValueOrDefault(assetBundleName);

            if (dependents == null)
            {
                // 依存アセット一覧を再帰で取得.
                dependents = GetAllDependenciesInternal(assetBundleName);

                // 登録.
                if (dependents.Any())
                {
                    dependenciesTable.Add(assetBundleName, dependents);
                }
            }

            return dependents.ToArray();
        }

        /// <summary>
        /// 依存関係にあるアセット一覧取得.
        /// </summary>
        private string[] GetAllDependenciesInternal(string fileName, List<string> dependents = null)
        {
            var targets = dependenciesTable.GetValueOrDefault(fileName, new string[0]);

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
                var internalDependents = GetAllDependenciesInternal(targets[i], dependents);

                foreach (var internalDependent in internalDependents)
                {
                    // 既に列挙済みの場合は追加しない.
                    if (!dependents.Contains(internalDependent))
                    {
                        dependents.Add(internalDependent);
                    }
                }
            }

            return dependents.Distinct().ToArray();
        }

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
                UniTask<AssetBundle> task = default;

                // アセットバンドル名は小文字なので小文字に変換.
                var assetBundleName = assetInfo.AssetBundle.AssetBundleName.ToLower();

                var loadedAssetBundle = loadedAssetBundles.GetValueOrDefault(assetBundleName);

                if (loadedAssetBundle == null)
                {
                    var dependencies = GetAllDependencies(assetBundleName);

                    for (var i = 0; i < dependencies.Length; i++)
                    {
                        IncrementReferenceCount(dependencies[i]);    
                    }
                    
                    task = UniTask.Create(async () =>
                    {
						var loadDependenciesTasks = dependencies
							.Select(x => GetLoadTask(installPath, x).ToUniTask(cancellationToken: cancelToken))
							.ToArray();

						await UniTask.WhenAll(loadDependenciesTasks);

						return await GetLoadTask(installPath, assetBundleName).ToUniTask(cancellationToken: cancelToken);
                    });
                }
                else
                {
                    IncrementReferenceCount(assetBundleName);

					task = UniTask.FromResult(loadedAssetBundle);
                }

				try
				{
					var assetBundle = await task;

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
					await AndroidUtility.CopyStreamingToTemporary(filePath).AttachExternalCancellation(cancelToken);

					filePath = AndroidUtility.ConvertStreamingAssetsLoadPath(filePath);
				}
				catch (OperationCanceledException)
				{
					/* Canceled */
				}
			}

			#endif

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

						if (syncLoadFrameCount == Time.frameCount)
						{
							if (FrameSyncLoadFileSize < syncLoadFileSize || FrameSyncLoadFileNum < syncLoadFileCount)
							{
								syncLoadFileSize = 0;
								syncLoadFileCount = 0;

								if (cancelToken.IsCancellationRequested){ return null; }

								await UniTask.NextFrame(cancelToken);
							}
						}
						else
						{
							syncLoadFrameCount = Time.frameCount;
							syncLoadFileSize = 0;
							syncLoadFileCount = 0;
						}

						fileStream.Read(bytes, 0, bytes.Length);

						syncLoadFileSize += bytes.Length;
						syncLoadFileCount++;
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

            var dependencies = GetAllDependencies(assetBundleName);
            
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

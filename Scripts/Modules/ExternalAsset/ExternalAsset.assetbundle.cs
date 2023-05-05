
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Console;
using Modules.AssetBundles;

namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset
    {
        //----- params -----

		/// <summary> 最大同時ダウンロード数. </summary>
		private const uint AssetBundleDefaultInstallerCount = 8;

        //----- field -----

        // アセットバンドル管理.
        private AssetBundleManager assetBundleManager = null;

        // アセットバンドル名でグループ化したアセット情報.
        private Dictionary<string, List<AssetInfo>> assetInfosByAssetBundleName = null;

        //----- property -----

        //----- method -----

        private void InitializeAssetBundle()
        {
            assetBundleManager = AssetBundleManager.CreateInstance();
            assetBundleManager.Initialize(SimulateMode);
			assetBundleManager.SetMaxDownloadCount(AssetBundleDefaultInstallerCount);

			assetBundleManager.OnLoadAsObservable().Subscribe(x => OnLoadAssetBundle(x)).AddTo(Disposable);
            assetBundleManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
            assetBundleManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);
		}

		/// <summary> ファイルハンドラ設定. </summary>
		public void SetAssetBundleFileHandler(IAssetBundleFileHandler fileHandler)
		{
			assetBundleManager.SetFileHandler(fileHandler);
		}

		public void SetAssetBundleInstallerCount(uint installerCount)
		{
			assetBundleManager.SetMaxDownloadCount(installerCount);
		}

		private async UniTask UpdateAssetBundle(AssetInfo assetInfo, IProgress<float> progress = null, CancellationToken cancelToken = default)
		{
			// ローカルバージョンが最新の場合は更新しない.

			var requireUpdate = IsRequireUpdate(assetInfo);

			if (!requireUpdate) { return; }

			var assetBundleManager = instance.assetBundleManager;

			await assetBundleManager.UpdateAssetBundle(InstallDirectory, assetInfo, progress, cancelToken);
		}

        /// <summary> AssetBundleを読み込み (非同期) </summary>
        public static async UniTask<T> LoadAsset<T>(string resourcePath, bool autoUnload = true) where T : UnityEngine.Object
        {
            return await Instance.LoadAssetInternal<T>(resourcePath, autoUnload);
        }

        private async UniTask<T> LoadAssetInternal<T>(string resourcePath, bool autoUnload) where T : UnityEngine.Object
        {
            System.Diagnostics.Stopwatch sw = null;

            T result = null;

            if (assetInfoManifest == null)
            {
                var exception = new Exception("AssetInfoManifest is null.");

                OnError(exception);
				
				return null;
            }

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

				return null;
			}

            // 外部処理.

            if (loadAssetHandler != null)
            {
				try
				{
					await loadAssetHandler.OnLoadRequest(assetInfo, cancelSource.Token);
				}
				catch (OperationCanceledException)
				{
					/* Canceled */
				}
				catch (Exception e)
				{
					OnError(e);

					return null;
				}
			}

			if (cancelSource.IsCancellationRequested){ return null; }

            // 更新.

            var assetPath = GetAssetPathFromAssetInfo(externalAssetDirectory, shareAssetDirectory, assetInfo);

            if (!LocalMode && !SimulateMode)
            {
				var requireUpdate = IsRequireUpdate(assetInfo);

				// ローカルバージョンが古い場合はダウンロード.
                if (requireUpdate)
                {
					sw = System.Diagnostics.Stopwatch.StartNew();

					try
					{
						await UpdateAsset(resourcePath, cancelToken: cancelSource.Token);
					}
					catch (Exception e)
					{
						OnError(e);

						return null;
					}

					sw.Stop();

                    if (LogEnable && UnityConsole.Enable)
                    {
                        var builder = new StringBuilder();

                        var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

                        builder.AppendFormat("Update: {0} ({1:F2}ms)", Path.GetFileName(assetPath), sw.Elapsed.TotalMilliseconds).AppendLine();
                        builder.AppendLine();
                        builder.AppendFormat("LoadPath = {0}", assetPath).AppendLine();
                        builder.AppendFormat("FileName = {0}", assetInfo.FileName).AppendLine();
                        builder.AppendFormat("AssetBundleName = {0}", assetBundleName).AppendLine();

                        if (!string.IsNullOrEmpty(assetInfo.Hash))
                        {
                            builder.AppendFormat("Hash = {0}", assetInfo.Hash).AppendLine();
                        }

                        UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
                    }
                }
            }

            var isLoading = loadingAssets.Contains(assetInfo);

            if (!isLoading)
            {
                loadingAssets.Add(assetInfo);
            }

			// 読み込み実行 (読み込み中の場合は読み込み待ちのObservableが返る).

			try
			{
				sw = System.Diagnostics.Stopwatch.StartNew();

				result = await assetBundleManager.LoadAsset<T>(InstallDirectory, assetInfo, assetPath, autoUnload, cancelSource.Token);

				// 読み込み中リストから外す.

				if (loadingAssets.Contains(assetInfo))
				{
					loadingAssets.Remove(assetInfo);
				}

			}
			catch (Exception e)
			{
				OnError(e);

				return null;
			}

			// 外部処理.

            if (result != null && loadAssetHandler != null)
            {
				try
				{
					await loadAssetHandler.OnLoadFinish(assetInfo, cancelSource.Token);
				}
				catch (OperationCanceledException)
				{
					/* Canceled */
				}
				catch (Exception e)
				{
					OnError(e);

					return null;
				}

				if (cancelSource.IsCancellationRequested){ return null; }
			}

            // 時間計測終了.

            sw.Stop();

            // 読み込み中だった場合はログを表示しない.
            if (result != null && !isLoading)
            {
				if (LogEnable && UnityConsole.Enable)
                {
                    var builder = new StringBuilder();

                    var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

					builder.AppendFormat("Load: {0} ({1:F2}ms)", Path.GetFileName(assetPath), sw.Elapsed.TotalMilliseconds).AppendLine();
					builder.AppendLine();

					if (LocalMode)
					{
						builder.AppendLine("<color=#DC143C><b>[LocalMode]</b></color>");
					}

					builder.AppendFormat("LoadPath = {0}", assetPath).AppendLine();
					builder.AppendFormat("AssetBundleName = {0}", assetBundleName).AppendLine();
					builder.AppendFormat("Hash = {0}", assetInfo.Hash).AppendLine();

                    if (!string.IsNullOrEmpty(assetInfo.Group))
                    {
                        builder.AppendFormat("Group = {0}", assetInfo.Group).AppendLine();
                    }

                    if (assetInfo.AssetBundle.Dependencies.Any())
                    {
                        builder.AppendLine();
                        builder.AppendLine("Dependencies:");

                        foreach (var item in assetInfo.AssetBundle.Dependencies)
                        {
                            builder.AppendLine(item);
                        }
                    }

                    UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
                }

                if (onLoadAsset != null)
                {
                    onLoadAsset.OnNext(resourcePath);
                }
            }

			return result;
        }

		private void OnLoadAssetBundle(string assetBundleName)
		{
			if (InstallDirectory.StartsWith(UnityPathUtility.StreamingAssetsPath))
			{
				if (assetInfosByAssetBundleName != null)
				{
					var assetInfos = assetInfosByAssetBundleName.GetValueOrDefault(assetBundleName);

					if (assetInfos != null)
					{
						var assetInfo = assetInfos.FirstOrDefault();

						if (assetInfo != null)
						{
							var resourcePath = assetInfo.ResourcePath;

							DeleteVersion(resourcePath).Forget();
						}
					}
				}
			}
		}

		/// <summary> AssetBundleを解放 </summary>
		public static void UnloadAssetBundle(string resourcePath)
        {
            Instance.UnloadAssetInternal(resourcePath);
        }

        /// <summary> 全てのAssetBundleを解放 </summary>
        public static void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)
        {
            Instance.UnloadAllAssetsInternal(unloadAllLoadedObjects);
        }

        private void UnloadAllAssetsInternal(bool unloadAllLoadedObjects)
        {
            var assetBundleManager = Instance.assetBundleManager;

            if (onUnloadAsset != null)
            {
                var loadedAssets = GetLoadedAssets();

                foreach (var loadedAsset in loadedAssets)
                {
                    onUnloadAsset.OnNext(loadedAsset.Item1);
                }
            }

            if (assetBundleManager != null)
            {
                assetBundleManager.UnloadAllAsset(unloadAllLoadedObjects);
            }
        }

        /// <summary> 読み込み済みAssetBundle一覧取得 </summary>
        public static Tuple<string, int>[] GetLoadedAssets()
        {
            return Instance.assetBundleManager.GetLoadedAssetBundleNames();
        }

        private void UnloadAssetInternal(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) { return; }

            if (assetInfoManifest == null)
            {
                Debug.LogError("AssetInfoManifest is null.");
            }

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

			if (!assetInfo.IsAssetBundle)
            {
                Debug.LogErrorFormat("This file is not an assetBundle.\n{0}", resourcePath);
				return;
            }

            assetBundleManager.UnloadAsset(assetInfo.AssetBundle.AssetBundleName);

            if (onUnloadAsset != null)
            {
                onUnloadAsset.OnNext(resourcePath);
            }
        }

		/// <summary> アセットバンドル読み込み時イベント </summary>
		/// <returns> 該当アセットバンドルのFilePath </returns>
		public IObservable<string> OnLoadAssetBundleAsObservable()
		{
			if (assetBundleManager == null){ return Observable.Empty<string>(); }

			return assetBundleManager.OnLoadAsObservable();
		}
    }
}

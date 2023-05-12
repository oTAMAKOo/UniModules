
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using UnityEngine;

namespace Modules.ExternalAssets
{
    public sealed class FileAssetManager : Singleton<FileAssetManager>
    {
        //----- params -----

        //----- field -----

		// ダウンロード元URL.
		private string remoteUrl = null;
		private string versionHash = null;

		// シュミュレートモードか.
		private bool simulateMode = false;

		// ローカルモードか.
		private bool localMode = false;

		// ダウンローダー.
		private FileAssetDownLoader downLoader = null;

		// ダウンロードキュー.
		private Dictionary<string, AssetInfo> downloadQueueing = null;

		// イベント通知.
		private Subject<AssetInfo> onTimeOut = null;
		private Subject<Exception> onError = null;

		private bool isInitialized = false;

        //----- property -----

        //----- method -----

		private FileAssetManager() { }

		public void Initialize(bool simulateMode = false)
		{
			if (isInitialized) { return; }

			this.simulateMode = UnityUtility.isEditor && simulateMode;

			downloadQueueing = new Dictionary<string, AssetInfo>();

			downLoader = new FileAssetDownLoader();

			downLoader.Initialize();

			downLoader.OnTimeoutAsObservable()
				.Subscribe(x => OnTimeout(x))
				.AddTo(Disposable);

			downLoader.OnErrorAsObservable()
				.Subscribe(x => OnError(x))
				.AddTo(Disposable);

			isInitialized = true;
		}

		public void SetMaxDownloadCount(uint maxDownloadCount)
		{
			downLoader.SetMaxDownloadCount(maxDownloadCount);
		}

		/// <summary> ローカルモード設定. </summary>
		public void SetLocalMode(bool localMode)
		{
			this.localMode = localMode;
		}

		/// <summary> URLを設定. </summary>
		public void SetUrl(string remoteUrl, string versionHash)
		{
			this.remoteUrl = remoteUrl;
			this.versionHash = versionHash;
		}

		private string BuildDownloadUrl(AssetInfo assetInfo)
		{
			var platformName = PlatformUtility.GetPlatformTypeName();

			var url = PathUtility.Combine(remoteUrl, platformName, versionHash, assetInfo.FileName);

			return $"{url}?v={assetInfo.Hash}";
		}

		public async UniTask UpdateFileAsset(string installPath, AssetInfo assetInfo, IProgress<float> progress = null, CancellationToken cancelToken = default)
		{
			if (simulateMode) { return; }

			if (localMode) { return; }

			var url = BuildDownloadUrl(assetInfo);

			// 既にダウンロード中.
			if (downloadQueueing.ContainsKey(url)){ return; }

			try
			{
				var filePath = PathUtility.Combine(installPath, assetInfo.FileName);

				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}

				downloadQueueing[url] = assetInfo;

				await downLoader.Download(url, filePath, progress, cancelToken);
			}
			finally
			{
				downloadQueueing.Remove(url);
			}
		}
		
		public void ClearDownloadQueue()
		{
			downloadQueueing.Clear();
		}

		private void OnTimeout(string url)
		{
			var assetInfo = downloadQueueing.GetValueOrDefault(url);

			if (assetInfo == null) { return; }

			if (onTimeOut != null)
			{
				Debug.LogErrorFormat("[Download Timeout] \n{0}", url);

				onTimeOut.OnNext(assetInfo);
			}
		}

		private void OnError(Exception ex)
		{
			if (onError != null)
			{
				Debug.LogErrorFormat("[Download Error] \n{0}", ex);

				onError.OnNext(ex);
			}
		}

		/// <summary> タイムアウト時のイベント. </summary>
		public IObservable<AssetInfo> OnTimeOutAsObservable()
		{
			return onTimeOut ?? (onTimeOut = new Subject<AssetInfo>());
		}

		/// <summary> エラー時のイベント. </summary>
		public IObservable<Exception> OnErrorAsObservable()
		{
			return onError ?? (onError = new Subject<Exception>());
		}
    }
}
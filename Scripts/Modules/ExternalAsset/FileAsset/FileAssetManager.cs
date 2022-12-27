
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

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
		private FileAssetDownloader downloader = null;

		// ダウンロードキュー.
		private Dictionary<string, AssetInfo> downloadQueueing = null;

		// イベント通知.
		private Subject<AssetInfo> onTimeOut = null;
		private Subject<Exception> onError = null;

		private bool isInitialized = false;

        //----- property -----

        //----- method -----

		private FileAssetManager() { }

		public void Initialize(uint numInstallers, bool simulateMode = false)
		{
			if (isInitialized) { return; }

			this.simulateMode = UnityUtility.isEditor && simulateMode;

			downloadQueueing = new Dictionary<string, AssetInfo>();

			downloader = new FileAssetDownloader();

			downloader.Initialize((int)numInstallers);

			downloader.OnTimeoutAsObservable()
				.Subscribe(x => OnTimeout(x))
				.AddTo(Disposable);

			downloader.OnErrorAsObservable()
				.Subscribe(x => OnError(x))
				.AddTo(Disposable);

			isInitialized = true;
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

			var url = PathUtility.Combine(new string[] { remoteUrl, platformName, versionHash, assetInfo.FileName });

			return string.Format("{0}?v={1}", url, assetInfo.Hash);
		}

		public async UniTask UpdateFileAsset(string installPath, AssetInfo assetInfo, CancellationToken cancelToken, IProgress<float> progress = null)
		{
			if (simulateMode) { return; }

			if (localMode) { return; }

			var url = BuildDownloadUrl(assetInfo);

			// 既にダウンロード中.
			if (downloadQueueing.ContainsKey(url)){ return; }

			try
			{
				var filePath = PathUtility.Combine(installPath, assetInfo.FileName);

				downloadQueueing[url] = assetInfo;

				await downloader.Download(url, filePath, progress).ToUniTask(cancellationToken: cancelToken);
			}
			finally
			{
				downloadQueueing.Remove(url);
			}
		}

		private void OnTimeout(string url)
		{
			var assetInfo = downloadQueueing.GetValueOrDefault(url);

			if (assetInfo == null) { return; }

			if (onTimeOut != null)
			{
				onTimeOut.OnNext(assetInfo);
			}
		}

		private void OnError(Exception ex)
		{
			if (onError != null)
			{
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